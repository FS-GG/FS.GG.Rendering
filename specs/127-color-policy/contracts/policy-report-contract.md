# Contract: Policy Report (generated artifact + drift gate)

Mirrors F1's generate→idempotent→drift-gate discipline, but with **one evaluator**: the committed
report is rendered by `ColorPolicy.renderReport` — the same function the unit tests exercise — so the
report can never silently diverge from the policy rules (no second recompute path).

## Committed artifacts

- `docs/reports/color-policy-wcag.md`
- `docs/reports/color-policy-ant.md`

Each is plain markdown, deterministic, and carries a static "generated — do not edit; regenerate via
…" header (no clock/version stamp). Located under the existing `docs/reports/` tree.

## Report format (deterministic)

```
# Color Policy Report — <Label> (`<name>`)

> GENERATED — do not edit. Regenerate via: <documented command>
> Authority: <WCAG-certified | Ant Design expectation (not WCAG-certified)>

| Pairing | Foreground | Background | Role | Measured | Threshold | Verdict | Note |
|---------|-----------|-----------|------|----------|-----------|---------|------|
| text-on-canvas | #1f2937 | #f8fafc | Text | 12.34 | 4.50 | Aaa |  |
| … (one row per validated pairing, in fixed catalog order) |
| primary-fg-on-primary-bg | … | … | GraphicOrUi | … | … | … | ant: not WCAG-certified |

**Overall: PASS** (0 failing of N validated; M out-of-scope; K indeterminate)
```

Formatting rules (FR-008/SC-004):
- Rows in **fixed catalog order**; no sorting on measured values.
- Colors as lowercase `#rrggbb` (or `#rrggbbaa` when alpha < 255), invariant culture.
- `Measured`/`Threshold` with **fixed precision** (e.g. `F2`) and **invariant culture**.
- Line endings normalized to `\n` (matches `scripts/generate-design-tokens.fsx`).
- **No** wall-clock, random, environment, or culture-sensitive content.
- Out-of-scope pairings disclosed (`Verdict` column shows out-of-scope) — never silently dropped or
  shown as pass (FR-011).
- `ant` rows that WCAG would fail carry the authority note (FR-010).

## Invariants (must be tested in `Controls.Tests`, default local tier — no GL)

1. **Drift gate (FR-009)**: re-render both reports from current code/tokens and assert byte-equality
   with the committed files. On mismatch the test fails and names the divergent file. *(This is the
   `--check` equivalent and runs in the existing gate.)*
2. **Idempotency (SC-004)**: `renderReport` called twice on identical inputs returns byte-identical
   strings.
3. **Completeness (FR-008/SC-003)**: each report has one row per validated pairing; the `ant` report
   includes primary/success/warning/error/info/text-on-surface rows, each with rule + measured +
   verdict.
4. **Tamper detection (SC-004)**: a deliberately mutated committed file fails the drift gate (covered
   by the byte-equality assertion).

## Regeneration (on demand)

- **Supported path**: an env-gated update mode in the drift test — running the suite with
  `UPDATE_POLICY_REPORTS=1` writes both files via the same `renderReport` evaluator, then the normal
  (unset) run verifies them. Robust, no internal-access tricks.
- **Optional convenience**: `scripts/generate-policy-report.fsx` that `#r`s the built `Color.dll` /
  `DesignSystem.dll` and calls `renderReport`. Its ability to reach `module internal ColorPolicy` from
  the `dotnet fsi` dynamic assembly (`InternalsVisibleTo`) MUST be validated before relied upon
  (Research R3); if fragile on net10, the script documents/delegates to the env-gated test path rather
  than re-implementing evaluation.

## Neutrality (FR-012/SC-006)

Adding two files under `docs/reports/` and one internal `.fs` + one `InternalsVisibleTo` line does not
touch any `.fsi` or `tests/surface-baselines/*.txt`. The surface-drift gate stays green with no new
public rows; existing pass/skip counts and rendered/gallery output are unchanged.
