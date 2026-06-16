# Implementation Plan: Color Validation Policies (wcag / ant) — Workstream F, Phase F2

**Branch**: `127-color-policy` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/127-color-policy/spec.md`

## Summary

Introduce a named, selectable **color-policy** abstraction — a rule set that decides whether the
design system's color pairings are acceptable — and deliver two policies: **`wcag`** (the existing
contrast behavior, reproduced exactly and the default) and **`ant`** (Ant Design's own contrast
expectations, which differ from WCAG thresholds). Each policy is evaluable over the design system's
pairings to a per-pairing verdict + overall summary, and emits a deterministic, drift-checked
**policy report**.

The load-bearing technical decision, surfaced by a Phase-0 audit of the layering: **the contrast
machinery (`Contrast`/`Role`/`Verdict`) lives in `FS.GG.UI.Color` (dep = Scene only), while the
colors it judges live in `FS.GG.UI.DesignSystem` (dep = Scene only) — the two are siblings, and no
*product* code today runs contrast over design-system colors; that reuse happens only in
`Controls.Tests`** (the one assembly referencing both). To reuse the contrast machinery byte-for-byte
(the guarantee behind `wcag` ≡ today, FR-002/SC-001) **without** adding a new inter-project
dependency, the policy **engine** lands as `module internal ColorPolicy` **in `FS.GG.UI.Color`**,
beside `Contrast` (pure color-domain: identity, per-role thresholds, `evaluate`, deterministic
`renderReport`). The design-system **pairing catalog** (which token pairs with which, under which
role) and all evaluation/reporting are driven from `Controls.Tests`, which already reaches the public
`DesignTokens`, the internal `DesignTokensExt` (F1's Ant families) via IVT, and `Color`. This keeps
`DesignSystem` at **dep = Scene only** (exactly F1's posture), adds **zero new project dependency**,
**zero public-surface-baseline delta**, and **zero behavioral change** under the default policy.

`wcag` reuses `Contrast` directly (same `verdict` thresholds → byte-identical). `ant` is a distinct
threshold/role table (provenance-traced to the FS-Skia-UI Ant adoption analysis) proving the choice
changes *rules*, not colors (FR-005/SC-002). The **report** is rendered by one deterministic internal
function (a single evaluator — no second JSON-recompute path to drift), committed under `docs/reports/`,
and **drift-checked by a gate-run test** (the `--check` equivalent that needs no GL); on-demand
regeneration uses an env-gated update mode, with a thin `.fsx` wrapper whose internal-access mechanism
is validated in Phase 0.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo-wide `Directory.Build.props`). The report drift check
runs as an Expecto test in `Controls.Tests`; the optional on-demand regenerator is a `dotnet fsi` script.

**Primary Dependencies**: none new to any product/package assembly. The policy engine reuses the
already-present `FS.GG.UI.Color.Contrast` from within the `Color` assembly itself. No JSON parser is
added to any product assembly (the F1 forbidden-package guard is preserved); the policy *rule tables*
are authored F# literals with provenance comments, not generated from JSON (few values, conceptual —
generation would be heavier than the data; F1 generated ~80 tokens, this is ~a dozen thresholds).

**Storage**: committed report artifacts at `docs/reports/color-policy-wcag.md` and
`docs/reports/color-policy-ant.md` (human-readable markdown, deterministic, drift-checked). No runtime
storage. Color *values* trace to the existing DTCG source `src/Themes.Default/design-tokens.tokens.json`
via the F1-generated `DesignTokensExt` (not re-read here).

**Testing**: existing repo suites via the harness; new `Feature127ColorPolicyTests` in `Controls.Tests`
(default local tier, deterministic, no GL): wcag-≡-today parity, ant-≠-wcag divergence, unknown-name
rejection, out-of-scope disclosure, no-overclaim disclosure, report drift + idempotency. The
surface-drift gate (`scripts/refresh-surface-baselines.fsx` + `tests/surface-baselines/*.txt`) must
show **no delta**.

**Target Platform**: library packages consumed in-repo (tests today; the F3 CLI/template wiring and
F4 resolver consume the policy later).

**Project Type**: multi-project F# library/framework (single solution `FS.GG.Rendering.slnx`).

**Performance Goals**: none new — behaviour-neutral. No render-path or hot-path code; policy evaluation
runs only in tests / report generation.

**Constraints**:
- **Behaviour- and contract-neutral is the hard gate (FR-012/SC-001/SC-006)** — existing suite passes
  with identical pass/skip counts, render/gallery output byte-identical, public surface baselines
  unchanged, no new public type rows, no new project/package dependency.
- **`wcag` reuses, never reimplements, `Contrast`** — `wcag` verdicts come from the existing
  `Contrast.verdict`/`check`, guaranteeing byte-identical default behavior.
- **Internal-first** — the policy engine is `module internal ColorPolicy` (no `.fsi`); public
  promotion is deferred to F5.
- **One evaluator** — the report is rendered by the same internal function the unit tests evaluate, so
  the committed report cannot silently diverge from the policy rules.
- **Deterministic report** — no wall-clock / random / culture-sensitive content; byte-identical across
  regenerations; drift-checked in the gate.
- **Acyclic layering preserved** — engine in `Color` (dep = Scene only); `DesignSystem` stays
  dep = Scene only; pairing catalog + evaluation live in `Controls.Tests`.

**Scale/Scope**: 1 new internal `.fs` (`src/Color/ColorPolicy.fs`, no `.fsi`); 1 `InternalsVisibleTo`
line added to `Color.fsproj`; 1 new test file (`Feature127ColorPolicyTests.fs`); 2 committed report
artifacts under `docs/reports/`; 1 optional regenerator script. Two policies (`wcag`, `ant`); the
`ant` pairing set covers primary / success / warning / error / info / text-on-surface (SC-003).

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **PASS** | The new surface is **internal** (no public `.fsi`). The "FSI honesty" intent is met by the in-assembly Expecto tests *naming and exercising* `ColorPolicy` through the same calls the future F3/F4 consumers will use (via IVT). No public API is drafted because none is added (deferred to F5). Tests authored failing-first against the engine signature, then greened. |
| II. Visibility lives in `.fsi` | **PASS** | `module internal ColorPolicy` in `src/Color/ColorPolicy.fs` with **no** `.fsi` — the established internal pattern (`DesignTokensExt`, `Reconcile`, `RetainedRender`). No access modifiers on top-level bindings (so the `Directory.Build.props` FS0078-as-error, which targets files *with* a companion `.fsi`, does not fire). No public `.fsi` touched; per-package surface baselines unchanged. |
| III. Idiomatic simplicity | **PASS** | Plain F#: a policy is a record (name/label/authority + a `Role -> threshold` table), evaluation is `Contrast` + a comparison, the report is `string` building over a list. No operators, SRTP, reflection, CEs, type providers, or active patterns beyond simple discriminants. Rule tables are authored literals (simpler than a generator for ~a dozen numbers). |
| IV. Elmish/MVU boundary | **N/A** | No stateful / I-O workflow; pure functions over data + a deterministic report string. The only I/O (reading/writing the committed report file) lives at the test/script edge, not in `update`-style logic. |
| V. Test evidence | **PASS** | wcag-parity (verdicts byte-equal to `Contrast` today), ant-divergence (≥1 pairing differs, attributable to policy), unknown-name rejection, out-of-scope + no-overclaim disclosure, report drift (regenerate-then-compare) + idempotency (byte-identical twice). Real evidence (real `Contrast`, real tokens); no synthetic fixtures needed; no test removed/weakened/skipped. |
| VI. Observability & safe failure | **PASS (light)** | Unknown policy name is rejected explicitly with a clear message — no silent fallback (FR-006/SC-005); out-of-scope pairings are disclosed, not silently passed (FR-011); the report discloses `ant`'s authority where it diverges from WCAG (FR-010). No GL/IO critical path altered. |
| Change Classification | **Tier 2 (internal change)** | No public API type surface added/removed/modified; **no new product/package dependency** (engine reuses `Contrast` from inside `Color`); no inter-package contract change; no observable behaviour change under the default policy. Requires spec + tests; `.fsi` and baselines remain untouched. (The speckit chain still produces plan/tasks — exceeds the Tier-2 minimum, which is fine, and matches the F1 precedent.) |
| Engineering Constraints — layering clause | **PASS** | Policy engine sits in the color layer beside the contrast primitive it extends; design-system pairings stay in the design-system/test layer; no control fork; no theme dependency added to anything; `DesignSystem` keeps dep = Scene only. |
| Engineering Constraints — no-React/DOM/icon-font (FR-013) | **PASS** | Pure F# over the existing `Color`/`Scene` primitives; Ant is adopted as a *rule set* only. |

**Gate result: PASS** — no violations; Complexity Tracking not required. The single watch-item is the
**engine-placement decision** (Research R1): hosting `ColorPolicy` in `Color` (not `DesignSystem`) is
what lets `wcag` reuse `Contrast` byte-for-byte while adding zero new project dependency and keeping
`DesignSystem` at dep = Scene only. The secondary watch-item is the **report regeneration mechanism**
(Research R3): the drift *check* is a gate-run test (robust); on-demand *regeneration* uses an env-gated
update mode, with the `.fsx`/internal-access path validated before being relied upon.

## Project Structure

### Documentation (this feature)

```text
specs/127-color-policy/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — engine placement, wcag-reuse, report mechanism, ant thresholds
├── data-model.md        # Phase 1 — ColorPolicy / ValidatedPairing / PolicyReport shapes + the pairing catalog
├── quickstart.md        # Phase 1 — parity + divergence + drift validation runbook
├── contracts/
│   ├── color-policy-contract.md     # the internal policy engine surface + verdict semantics
│   └── policy-report-contract.md    # report format + determinism + drift-gate invariants
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Scene/                       # FS.GG.UI.Scene (unchanged — Color type)
├── Color/                       # FS.GG.UI.Color  (dep = Scene only — UNCHANGED deps)
│   ├── Color.fsproj             #   + Compile ColorPolicy.fs; + InternalsVisibleTo Controls.Tests
│   ├── Contrast.fsi/.fs         #   UNCHANGED (reused verbatim by the wcag policy)
│   ├── Palettes.fsi/.fs         #   unchanged
│   └── ColorPolicy.fs           #   NEW — `module internal`, NO .fsi (engine: policies, evaluate, renderReport)
├── DesignSystem/                # FS.GG.UI.DesignSystem (UNCHANGED — dep = Scene only)
│   ├── DesignTokens.fsi/.fs     #   public primitives (wcag pairings draw from these)
│   └── DesignTokensExt.fs       #   F1 internal Ant families (ant pairings draw from these; already IVT to Controls.Tests)
└── …                            # all other projects unchanged (no consumer wired in F2)

docs/
└── reports/
    ├── color-policy-wcag.md     # NEW — committed, generated, drift-checked
    └── color-policy-ant.md      # NEW — committed, generated, drift-checked

scripts/
├── generate-design-tokens.fsx       # unchanged (F1)
├── refresh-surface-baselines.fsx    # unchanged
└── generate-policy-report.fsx       # NEW (optional) — on-demand regen of the two committed reports

tests/
├── surface-baselines/*.txt          # UNCHANGED (zero delta — the neutrality proof)
└── Controls.Tests/
    └── Feature127ColorPolicyTests.fs    # NEW — parity, divergence, rejection, disclosure, report drift/idempotency
```

**Structure Decision**: Multi-project F# solution. The policy **engine** is one **internal** module in
the existing `FS.GG.UI.Color` project (beside `Contrast`, so `wcag` reuses it byte-for-byte; no new
project, no `.slnx`/refresh-script change → no surface-gate risk). `DesignSystem` is **untouched**
(stays dep = Scene only, preserving F1's posture). The design-system **pairing catalog** and all
evaluation/report generation are driven from `Controls.Tests`, which already references `Color` and
reaches `DesignTokens`/`DesignTokensExt` (IVT). Committed reports live under the existing `docs/reports/`
tree; their drift is gated by a deterministic, GL-free test. See `contracts/color-policy-contract.md`
for the engine surface and `contracts/policy-report-contract.md` for the report/drift contract;
`data-model.md` has the entity shapes and the pairing catalog.

## Complexity Tracking

> Not required — Constitution Check passed with no violations.

The one decision worth recording (not a violation): `ColorPolicy` is hosted in `Color` rather than
`DesignSystem`. The rejected alternative — hosting it in `DesignSystem` and adding a
`DesignSystem → Color` project reference — would have placed the policy in the design-system layer but
introduced a new inter-project dependency (adding `FS.GG.UI.Color` to the `DesignSystem` package's
dependency closure), contradicting F1's deliberately-maintained "dep = Scene only" posture for a
behaviour-neutral, internal-first change. Hosting in `Color` reuses `Contrast` in-assembly, adds zero
dependency, and is architecturally honest (a contrast *policy* is a color-domain concept extending the
contrast *primitive*). The design-system-specific *pairing catalog* legitimately couples both layers
and so lives in `Controls.Tests` (the existing both-referencing assembly), pending F3/F4/F5 promotion.
