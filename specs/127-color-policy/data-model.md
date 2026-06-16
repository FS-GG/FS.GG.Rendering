# Phase 1 Data Model: Color Validation Policies (wcag / ant)

All types are **internal** to `FS.GG.UI.Color` (`module internal ColorPolicy`, no `.fsi`), reusing
`Color`, `Role`, `Verdict`, and `ContrastResult` from the existing `FS.GG.UI.Color` surface. Shapes
are indicative; the authoritative signatures live in `contracts/color-policy-contract.md`.

## Entity: Color Policy

A named rule set governing how color pairings are judged.

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Stable identity, lowercase: `"wcag"` \| `"ant"`. Used for lookup; unknown → rejection (FR-006). |
| `Label` | `string` | Human-readable, for report headers (e.g. `"WCAG 2.x contrast"`, `"Ant Design contrast expectations"`). |
| `Authority` | `Authority` | `WcagCertified` \| `AntExpectation` — what the policy *claims* (drives no-overclaim disclosure, FR-010). |
| `Threshold` | `Role -> float` | Required minimum ratio per role. `wcag` = the existing `Contrast.verdict` gates; `ant` = the Ant table below. |
| `Classify` | `Role -> ratio:float -> Verdict` | `wcag` delegates to `Contrast.verdict`; `ant` maps ratio vs its threshold to a verdict. |

- **wcag** thresholds (reused from `Contrast`, MUST stay byte-identical):
  - `Text`: `7.0` (Aaa) / `4.5` (Aa) / `3.0` (AaLarge) / else `Fail`
  - `GraphicOrUi`: `3.0` (Aa) / else `Fail`
  - `Decorative`: `Exempt`
- **ant** thresholds (authored literals, provenance-traced; final numbers fixed in
  `contracts/color-policy-contract.md`): Ant Design's own body-text and component-foreground/border
  contrast expectations, deliberately distinct from WCAG's role gates so that at least one shared
  pairing reaches a different verdict under `ant` than under `wcag` (FR-005/SC-002).

**Registry**: a `byName : string -> Result<ColorPolicy, string>` (or `option`) that returns `wcag` /
`ant` and an explicit error/`None` for anything else (no silent fallback). `defaultPolicy = wcag`
(FR-003) — applied when no name is supplied.

## Entity: Validated Pairing (catalog entry + result)

A foreground/background (or semantic-feedback) relationship a policy evaluates. The **catalog** (the
list of entries) is assembled in `Controls.Tests` from `DesignTokens` (public primitives) and
`DesignTokensExt` (F1 Ant families).

| Field | Type | Notes |
|---|---|---|
| `PairingName` | `string` | Stable, e.g. `"text-on-canvas"`, `"primary-fg-on-primary-bg"`, `"feedback-error-text"`. |
| `Foreground` | `Color` | The fg token value (composited over bg if alpha present, via `Contrast.compositeOver`). |
| `Background` | `Color` | The bg token value. |
| `Role` | `Role` | `Text` \| `GraphicOrUi` \| `Decorative`. |

Scope is **not** a field on the pairing: whether a pairing is in a given policy's validated set is
decided by a standalone `inScope : policy -> pairing -> bool` operation (drives out-of-scope
disclosure, FR-011), per `contracts/color-policy-contract.md`.

**Result of evaluating one pairing under one policy:**

| Field | Type | Notes |
|---|---|---|
| `Pairing` | `PairingName` | |
| `Measured` | `float` | Contrast ratio from `Contrast.ratio` (post-composite); `nan` for `Indeterminate`. |
| `Threshold` | `float option` | The policy's required ratio for the role (`None` for `Decorative`/exempt). |
| `Outcome` | `PolicyOutcome` | `Passed` \| `Failed` \| `OutOfScope` \| `Indeterminate` (tri-state-plus, FR-011). |
| `Verdict` | `Verdict` | The underlying WCAG-style verdict where applicable. |
| `AuthorityNote` | `string option` | Set when the policy certifies a pairing WCAG would fail (FR-010 no-overclaim). |

**ant semantic families required in scope (SC-003)**: `primary`, `success`, `warning`, `error`,
`info`, and `text-on-surface`. Each MUST appear as ≥1 pairing in the `ant` report with a rule, a
measured value, and a verdict.

## Entity: Policy Report

Deterministic, human-readable artifact summarizing one policy's evaluation over its full catalog.

| Field | Type | Notes |
|---|---|---|
| (header) | text | Policy `Name`, `Label`, `Authority`; a static "generated — do not edit; regenerate via …" marker. |
| (rows) | one per pairing | `PairingName`, fg hex, bg hex, `Role`, measured ratio (fixed precision, invariant culture), threshold, `Outcome`/`Verdict`, authority note. |
| (summary) | text | Overall pass/fail (pass = no `Failed` rows; out-of-scope/indeterminate listed but not counted as fail). |

- **Determinism**: fixed row order (catalog order), fixed numeric format, `\n` line endings, no
  wall-clock/random/culture-sensitive content (FR-008/SC-004).
- **Committed at**: `docs/reports/color-policy-wcag.md`, `docs/reports/color-policy-ant.md`.
- **Drift-checked**: a gate-run test re-renders and compares byte-for-byte (FR-009).

## State / lifecycle

No mutable state. Pure data flow:

```
name ──byName──▶ ColorPolicy ─┐
                              ├─▶ evaluate(catalog) ─▶ PairingResult list ─▶ renderReport ─▶ string
catalog (from tokens) ────────┘
```

The only side effects are at the edge: reading the committed report (drift test) and writing it
(env-gated update / optional script).

## Validation rules (from requirements)

- Unknown policy name → explicit rejection, never substitution (FR-006/SC-005).
- No policy chosen → `wcag` (FR-003).
- Same pairing, `wcag` vs `ant` → may differ; difference attributable to policy, recorded with verdict
  (FR-005, FR-005-acceptance).
- Pairing outside a policy's set → `OutOfScope`, not `Passed` (FR-011).
- `ant` certifying a WCAG-failing pairing → permitted + `AuthorityNote` disclosure (FR-010).
- Semi-transparent fg → composited over bg before measurement via `Contrast.compositeOver` (edge case).
