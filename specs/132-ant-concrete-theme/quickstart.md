# Quickstart / Validation Guide: Concrete Ant Design theme (D2.1)

Prerequisites: .NET `net10.0` SDK; repo builds green (`dotnet build -c Debug`). All commands from repo root.

## 1. Render a control tree under the Ant theme (US1)

In FSI or a sample, resolve a control's style with the Ant theme + intent policy instead of the default:

```fsharp
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.AntDesign

// Default (neutral) — unchanged behaviour:
let d = StyleResolver.resolveDefault AntTheme.antLight "button" "primary" [] VisualState.Normal
// Ant intent divergence (brand-blue primary; danger -> red) with no control fork:
let a = StyleResolver.resolve AntIntentPolicy.policy AntTheme.antLight "button" "danger" [] VisualState.Normal
// a differs from the neutral resolution of the same inputs; d matches today's output.
```

**Expected**: `AntTheme.antLight` is a `Theme` with Ant brand-blue `Accent`; `AntIntentPolicy.policy` makes `"primary"`/`"danger"`/`"default"`/`"dashed"`/`"text"`/`"link"` resolve to distinct styles; unknown/`""` intent is identity (total).

## 2. Run the theme-parity test (US4 / SC-001)

```bash
dotnet test tests/Controls.Tests -c Debug --filter "Feature132ThemeParity"
```

**Expected**: one control tree spanning every category (incl. net-new families) resolves under Default and Ant; behaviour/accessibility contract asserted identical, ≥1 visual property asserted divergent; fails if any control branches on theme identity.

## 3. Run the per-control contract tests for net-new controls (US3 / SC-004)

```bash
dotnet test tests/Controls.Tests -c Debug --filter "Feature132NewControlContract"
```

**Expected**: every net-new control passes the same Catalog/Semantic/Interaction/Accessibility/Rendering families as existing controls; each renders under both themes.

## 4. Run the coverage-matrix honesty check (US2 / SC-002, SC-003)

```bash
dotnet test tests/Controls.Tests -c Debug --filter "Feature132CoverageMatrix"
```

**Expected**: passes only when every Ant overview component has a disposition and every covered row references a control id in `Catalog` and a token entry in the `DesignSystem` surface; fails on any gap or dangling reference.

## 5. Surface + token drift gates (SC-005, SC-006)

```bash
dotnet build -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx     # regenerates baselines incl. new FS.GG.UI.Themes.AntDesign.txt
git diff --stat tests/surface-baselines/              # review: only intended new package + new control rows
dotnet test tests/Controls.Tests -c Debug --filter "DesignTokenParity"   # token-drift gate: no value change
```

**Expected**: `FS.GG.UI.Themes.AntDesign.txt` is new with `AntTheme` + `AntIntentPolicy`; `FS.GG.UI.Controls.txt` grows only by the net-new control modules; no other baseline churn; design-token-drift green (no existing token value changed). With the Ant theme unselected, existing rendering goldens are byte-identical.

## 6. Charts follow-up (US5 — already done, no code)

Confirm the plan records the charts follow-up (no chart work in this feature):

```bash
grep -n "D2-Charts\|D2C.1\|ant-design-charts" docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md
```

**Expected**: Phase D2-Charts (§7.4b) and task D2C.1 present, scoped as design-language adoption over existing chart controls, sequenced after D2.1.
