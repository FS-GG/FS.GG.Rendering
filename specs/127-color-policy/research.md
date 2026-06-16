# Phase 0 Research: Color Validation Policies (wcag / ant)

All NEEDS CLARIFICATION resolved. Each item: **Decision / Rationale / Alternatives considered**.

## R1 — Where does the `ColorPolicy` engine live? (load-bearing)

**Decision**: Host the engine as `module internal ColorPolicy` in **`src/Color/ColorPolicy.fs`**
(no `.fsi`), beside `Contrast`. The design-system **pairing catalog** and all evaluation/report
generation are driven from **`Controls.Tests`** (which already references `Color` and reaches the
public `DesignTokens` + internal `DesignTokensExt` via IVT). Add `InternalsVisibleTo Controls.Tests`
to `Color.fsproj`.

**Rationale**:
- The `wcag` policy must reproduce today's contrast behavior **byte-for-byte** (FR-002/SC-001). The
  only way to *guarantee* that is to reuse the existing `Contrast.verdict`/`check` rather than
  reimplement thresholds. `Contrast` lives in `Color`; reusing it in-assembly needs **no new
  dependency**.
- `DesignSystem` is intentionally **dep = Scene only** (F1's posture). Hosting the policy there would
  force a new `DesignSystem → Color` project reference, adding `FS.GG.UI.Color` to the `DesignSystem`
  package dependency closure — an inter-project contract change at odds with a behaviour-neutral,
  internal-first Tier-2 change.
- A contrast *policy* (role → required ratio) is a color-domain concept extending the contrast
  *primitive*; `Color` already owns `Role`/`Verdict`/`Contrast`, so the policy is at home there.
- The design-system-specific *pairing set* (which token pairs with which, under which role) does
  legitimately couple `DesignSystem` colors with `Color` roles. The one existing assembly that
  references both is `Controls.Tests`; per the repo's vertical-slice rule (the in-assembly test *is*
  the user-reachable surface for an internal slice), the catalog and evaluation live there until F3/F4
  wire a product consumer and F5 promotes public surface.

**Alternatives considered**:
- *Engine in `DesignSystem` + `DesignSystem → Color` projref* — clean locality (policy in the
  design-system layer) but introduces a new inter-project/package dependency and breaks F1's
  "dep = Scene only" invariant for a change that is supposed to be neutral. Rejected (recorded in
  plan Complexity Tracking).
- *Engine in `Themes.Default` + IVT from `DesignSystem`* — `Themes.Default` already → `DesignSystem`
  but not → `Color`; would still need a new `Color` dep plus IVT gymnastics to reach `DesignTokensExt`.
  Worse than both. Rejected.
- *New dedicated `FS.GG.UI.*.Policy` project* — new project ⇒ new package ⇒ new surface baseline and
  `.slnx`/refresh-script churn; overkill for internal-first; violates "minimize projects". Rejected.

## R2 — How is `wcag` reproduced exactly, and how does `ant` differ?

**Decision**: `wcag` delegates to the existing `Contrast.verdict` thresholds verbatim:
- `Text`: `>= 7.0 → Aaa`, `>= 4.5 → Aa`, `>= 3.0 → AaLarge`, else `Fail`
- `GraphicOrUi`: `>= 3.0 → Aa`, else `Fail`
- `Decorative`: `Exempt`

`ant` is a **distinct threshold/role table** (authored as F# literals with provenance comments,
traced to the FS-Skia-UI Ant adoption analysis; see Assumptions/Provenance in spec). It encodes Ant
Design's own contrast expectations (e.g. Ant's body-text and component-foreground expectations, which
do not coincide with WCAG's 4.5/3.0 role gates). The exact numeric table is fixed in `data-model.md`.
At least one pairing in the design system's set MUST receive a verdict under `ant` that differs from
its `wcag` verdict (FR-005/SC-002), and that pairing is identified in the report.

**Rationale**: Reuse (not reimplementation) is the byte-identical guarantee for the default; an
explicit, small, provenance-traced table for `ant` is the simplest honest encoding of "different rules,
same colors" and is the anti-scope-creep proof. The number of thresholds is small and conceptual, so
authored literals beat a JSON generator (which F1 used because it had ~80 token *values*).

**Alternatives considered**:
- *Generate the rule tables from JSON (mirror F1's `DesignTokensExt`)* — single-source via DTCG, but
  heavyweight for ~a dozen thresholds and would re-introduce a JSON-read path; the values are
  conceptual policy rules, not design tokens. Rejected; the **report** (not the rules) is what the
  spec requires generated/drift-checked.
- *Re-derive `wcag` thresholds in the policy module* — risks drift from `Contrast`. Rejected; `wcag`
  delegates to `Contrast` so the two can never diverge.

## R3 — How is the report generated and drift-checked? (one evaluator, gate-run check)

**Decision**:
- The report is produced by **one** deterministic internal function `ColorPolicy.renderReport`
  (pure `policy + pairings -> string`), the *same* code path the unit tests evaluate — so the
  committed report cannot silently diverge from the policy rules (no second JSON-recompute evaluator).
- Two committed artifacts: `docs/reports/color-policy-wcag.md` and `docs/reports/color-policy-ant.md`.
- **Drift check = a gate-run Expecto test** in `Controls.Tests` that re-renders both reports from the
  current code/tokens and asserts byte-equality with the committed files (fail → identifies which file
  diverged). This is the robust `--check` equivalent; it runs in the default local tier (no GL).
- **Idempotency** asserted by rendering twice and comparing (byte-identical).
- **On-demand regeneration**: an env-gated update path (e.g. the same test writes the files when
  `UPDATE_POLICY_REPORTS=1`) reusing the exact in-process evaluator, plus an optional thin
  `scripts/generate-policy-report.fsx` wrapper. The script's internal-access mechanism (`#r` the built
  `Color.dll` + reach `module internal ColorPolicy`) is **validated before being relied upon**; if
  `InternalsVisibleTo` for the `dotnet fsi` dynamic assembly proves fragile on net10, the env-gated
  test update mode is the supported regeneration path and the script defers to it.

**Rationale**: A single evaluator removes the classic "generator vs. compiled code disagree" failure
mode. Driving the check from a test means it runs in the existing gate with no new CI wiring and no GL.
The repo already locates the repo root in tests (e.g. `Package.Tests` `repositoryRoot`), so reading/
writing `docs/reports/*` by repo-relative path is established. The fsi-internal-access uncertainty is
explicitly flagged (consistent with the repo memory on verifying mechanisms before relying on them).

**Alternatives considered**:
- *Standalone `.fsx` recomputing verdicts from the DTCG JSON + `#r Color.dll`* — would be a **second**
  evaluator that must mirror the compiled one; divergence risk, and needs a JSON read. Rejected in
  favor of one evaluator; if a script is wanted it calls the same compiled function, not a re-impl.
- *Report rendered in product code* — unnecessary; nothing consumes the report at runtime, and it
  would push the design-system pairing catalog into a product assembly (the R1 dependency problem).
  Rejected.

## R4 — Determinism of the report

**Decision**: The report contains only: a stable title/policy identity/label/authority header, one row
per validated pairing (pairing name, fg/bg color hex, role, measured ratio, threshold, verdict,
authority-disclosure flag where `ant` diverges from WCAG), and an overall pass/fail summary. **No**
wall-clock, **no** random, **no** culture-sensitive formatting. Ratios are formatted with a fixed
invariant-culture pattern and fixed precision; rows are emitted in a fixed catalog order; line endings
normalized to `\n` (matching `generate-design-tokens.fsx`).

**Rationale**: Byte-identical regeneration (FR-008/SC-004) requires eliminating every nondeterministic
source. The F1 generator's LF-normalization and fixed-order emission are the proven precedent.

**Alternatives considered**: *Include a generation timestamp / tool version* — breaks byte-identity;
rejected (provenance lives in the file's static header text, not a clock).

## R5 — Out-of-scope and no-overclaim disclosure

**Decision**: A pairing not covered by a policy's validated set is rendered/returned as an explicit
**out-of-scope / unvalidated** state for that policy (not a pass) — `ColorPolicy` distinguishes
"passed", "failed", and "not validated by this policy" (FR-011). Where `ant` certifies a pairing that
WCAG would fail, the report row carries an explicit **authority = ant (not WCAG-certified)** disclosure
(FR-010). The existing `Verdict.Indeterminate` (non-solid paint) remains "neither pass nor fail" and is
surfaced as such.

**Rationale**: Silent passing is the exact failure mode the spec forbids; explicit tri-state + an
authority flag keeps the report honest and is the no-overclaim guarantee.

**Alternatives considered**: *Collapse out-of-scope into pass* — rejected (FR-011 forbids it).

## R6 — Neutrality / non-regression surface

**Decision**: No public `.fsi` changes anywhere; `tests/surface-baselines/*.txt` show zero delta;
existing render/gallery suites (`ThemeInvarianceTests`/`PageRenderTests`) and existing pass/skip counts
are unchanged. The only product change is one new internal `.fs` + one `InternalsVisibleTo` line in
`Color.fsproj` (IVT is not a package dependency and does not appear in surface baselines).

**Rationale**: FR-012/SC-006 require a behaviour- and surface-neutral landing; this is the F1 pattern.

**Alternatives considered**: none — neutrality is a hard requirement.
