# Implementation Plan: Ant Design Charts adoption (D2C.1)

**Branch**: `133-ant-design-charts` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/133-ant-design-charts/spec.md`

## Summary

Extend the framework's existing chart controls toward the **Ant Design Charts** catalog, adopting Ant
Design Charts **as a design language only** — a machine-checked **chart-type coverage matrix** plus a
**token/visual mapping** over the repo's own chart controls and the Ant-derived token taxonomy. Three
moves, mirroring D2.1 (feature 132): (a) the existing five chart controls (`line-chart`/`bar-chart`/
`pie-chart`/`scatter-plot`/`graph-view`) render in Ant's visual language under the **existing**
`FS.GG.UI.Themes.AntDesign` theme through the resolver/token seams (opt-in, Default byte-identical);
(b) **net-new, generic, theme-agnostic chart controls** are added to `FS.GG.UI.Controls` for the
high-value Ant Charts gaps (area/column/histogram/box-plot/heatmap/radar/rose/waterfall/funnel/gauge/
sankey/chord/treemap/sunburst); (c) the rest are documented `composition` (combo/dual-axis) or
`not-applicable` (geo/map — no geospatial dependency). A **coverage matrix + honesty check** keeps
"maximal" honest, and a **chart parity test** proves one chart tree renders behaviour-identically and
visually-divergently under Default vs Ant. **No JS/React/AntV charting dependency**; charts render
through the existing Skia + F# chart-control path.

This is a **Tier 1** change: new public controls in `Controls` (new `.fsi`, grown surface baseline,
decision record) — but **no new package** (the Ant chart styling rides the existing
`Themes.AntDesign`).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: `FS.GG.UI.DesignSystem` (public `Theme`, `StyleResolver` + `IntentPolicy`,
`DesignTokensExt` taxonomy, `Style`, `VisualState`); `FS.GG.UI.Scene` (Color/geometry/`Path`/`textRun`);
`FS.GG.UI.Controls` (the chart controls being styled and extended); `FS.GG.UI.Themes.AntDesign`
(feature 132 — the concrete Ant theme reused unchanged). SkiaSharp over OpenGL is the render backend.
**No AntV (G2/G6/L7), React, or JS charting/geospatial dependency is added.**

**Storage**: N/A. Composes the existing token taxonomy; no new token store.

**Testing**: Expecto + `YoloDev.Expecto.TestSdk` + `Microsoft.NET.Test.Sdk` in `tests/Controls.Tests/`.
Precedent: `Feature132*` (theme parity, net-new control contract, coverage-matrix honesty), the chart
geometry in `Control.fs` (`lineGeom`/`barGeom`/`pieGeom`/`scatterGeom`/`graphGeom`), `Charts.fs(i)`.

**Target Platform**: Cross-platform .NET library packages (`FS.GG.UI.*`); rendering exercised
headlessly via the existing offscreen/Scene test path; GL-gated paths stay advisory.

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`), layered design-system / themes /
controls packages — single repository, library-first.

**Performance Goals**: No new hot path. Chart geometry is a bounded, deterministic schematic built from
existing `Scene` primitives; the matrix/honesty/parity checks are test-time only.

**Constraints**:
- **No chart-control forks** (constitution): all Ant chart appearance lives in the theme/resolver/
  tokens; net-new chart controls are generic and theme-agnostic.
- **Default theme byte-identical**: the existing five charts' Default render path is untouched; their
  Ant divergence rides the already theme-`Accent`-driven primary series + theme-role axis/grid/legend.
  Net-new charts have no pre-feature baseline, so they are fully theme-role-driven.
- **No existing token-value change**: the design-token-drift gate stays green; any genuinely new chart
  token entries are additive.
- **No charting/geospatial dependency**: geo/flow charts are `not-applicable`/deferred; charts render
  through the existing Skia + F# path only.
- **Tier 1 discipline**: every new public chart module has a `.fsi`; the `Controls` surface baseline is
  regenerated + committed in the same change; decision record landed in lock-step.

**Scale/Scope**: ~30–40 Ant Charts overview entries dispositioned; ~14 net-new generic chart controls;
0 new packages; 1 regenerated baseline (`Controls` grown); 1 decision record; matrix doc + honesty
check + chart parity test + per-control contract suite. **Medium-large** — see Complexity Tracking.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Status | How this plan satisfies it |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS (planned) | Each net-new chart control starts as an `.fsi` signature, then the parameterized contract suite (write-to-fail), then the `.fs` body + render geometry. |
| **II. Visibility in `.fsi`, no access modifiers in `.fs`; per-module baselines** | PASS (planned) | New `Controls` chart modules get a curated `.fsi`; the `FS.GG.UI.Controls` baseline is regenerated + committed in the same change. No new package row needed (no new assembly). |
| **III. Idiomatic Simplicity** | PASS (planned) | Net-new charts follow the existing `Charts` attribute+geometry shape; mark colours come from a theme-role-derived palette (a pure function), not SRTP/reflection/type-providers. |
| **IV. Elmish/MVU boundary for stateful/I-O** | PASS | Charts are pure render + data attributes (parent owns data), exactly like the existing `LineChart`/`BarChart`. No internal mutable state, no I/O. |
| **V. Test Evidence** | PASS (planned) | Chart parity test (Default vs Ant), per-control contract suite for every net-new chart, matrix honesty check. Real offscreen-render evidence; GL-gated bits honest-skipped. |
| **VI. Observability / safe failure** | N/A (mostly) | No new I/O or context creation. Empty/unknown chart data resolves to a defined visible fallback (the resolver/geometry stays total). |
| **Layer rule: one control set, many themes; no per-theme forks** | PASS (planned) | The central guardrail. Ant styles existing + net-new generic chart controls only through resolver/token seams; the parity test fails if any chart branches on theme identity (FR-007). |
| **Change Classification: Tier 1** | DECLARED | New public controls in `Controls`. Full chain: spec ✓, plan (this), `.fsi` updates, baseline update, tests, decision record. |
| **Package identity `FS.GG.UI.*` / no new dependency** | PASS | No new package or dependency; reuses `Themes.AntDesign`. Guards the "design language, not a charting engine" posture (FR-008). |

**Result**: No unjustified violations. The "maximal in one feature" scope risk is a planning/sequencing
concern (Complexity Tracking), not a constitution violation.

## Project Structure

### Documentation (this feature)

```text
specs/133-ant-design-charts/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions (Ant-styling seam, Default byte-identical strategy,
│                        #   net-new vs composition vs not-applicable split, snapshot source)
├── data-model.md        # Phase 1 — net-new chart control entity, chart-coverage-matrix row, palette mapping
├── quickstart.md        # Phase 1 — render charts under Ant, run parity + honesty + contract checks
├── contracts/
│   ├── new-chart-controls.md   # Catalog rows + .fsi shape for net-new chart controls
│   └── chart-coverage-matrix.md# Matrix row schema + honesty-check rules
├── checklists/
│   └── requirements.md  # (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/
├── catalog.yml                   # +rows for each net-new chart control (source of truth)
├── Catalog.fs / .fsi             # regenerated GENERATED rows for the new chart ids
├── Charts2.fsi / .fs             # NEW net-new generic chart controls (area/column/histogram/box/
│                                 #   heatmap/radar/rose/waterfall/funnel/gauge/sankey/chord/treemap/sunburst)
└── Control.fs                    # render/geom wiring for new chart kinds (theme-role palette; no theme branch)

docs/product/ant-design/
├── coverage/ant-chart-coverage.md         # the chart coverage matrix (one row per Ant Charts overview entry)
└── reference/ant-llms-sources.md          # +Ant Charts overview snapshot section (hub owns the date)

docs/product/decisions/
└── 0007-antdesign-charts-adoption.md      # Tier-1 decision record

tests/surface-baselines/FS.GG.UI.Controls.txt   # regenerated (grown with new chart controls)

tests/Controls.Tests/
├── Feature133ChartParityTests.fs          # Default vs Ant over a chart tree: contract-identical, visuals-divergent
├── Feature133NewChartControlContractTests.fs   # catalog/semantic/accessibility/rendering per net-new chart
└── Feature133ChartCoverageMatrixTests.fs  # honesty check: every Ant Charts entry dispositioned; no dangling refs
```

**Structure Decision**: Net-new chart controls live **inside `FS.GG.UI.Controls`** alongside the
existing chart controls (they are generic and theme-agnostic per the layer rule), grouped into one new
`Charts2` module pair. The Ant chart styling reuses the **existing** `FS.GG.UI.Themes.AntDesign` theme
+ the `StyleResolver`/token seams — **no new package**. The coverage matrix lives under the existing
`docs/product/ant-design/coverage/` tree next to the D2.1 component matrix, and the Ant Charts overview
snapshot is pinned through the central reference hub.

## Phased delivery (internal sequencing within this one feature)

- **P-A — Ant-styled existing charts (US1 core)**: confirm/extend that the five existing charts render
  Ant-divergently under `AntTheme` through the already theme-`Accent`-driven primary series + theme-role
  axis/grid/legend, with the Default render path byte-identical. Parity test (existing charts) green.
  *Shippable MVP on its own.*
- **P-B — Chart coverage matrix + honesty check (US2)**: author the matrix dispositioning every Ant
  Charts overview entry; wire the honesty check (fails on missing rows / dangling chart-control-or-token
  refs / blank disposition). Drives P-C scope.
- **P-C — Net-new generic chart controls (US3)**: add the ~14 net-new charts (`.fsi` → catalog row →
  contract tests → `.fs` + render geometry with a theme-role-derived palette). Regenerate the baseline.
- **P-D — Parity hardening + provenance (US4)**: extend the chart parity test to span every chart family
  incl. net-new; decision record 0007; hub snapshot section; final baseline regen; full suite + both
  drift gates green.
- **No-dependency guard (US5)**: a test/inspection asserting no AntV/React/JS charting dependency was
  added (FR-008, SC-006).

## Complexity Tracking

> Filled because the scope (not the constitution) carries justified risk.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Maximal Ant-Charts coverage in one feature** (~14 net-new charts) | The recorded D2C.1 follow-up; mirrors the D2.1 "maximal in one feature" decision | A core-set-now / tail-later split adds ceremony; the coverage matrix + internal phasing (P-A…P-D) preserve shippability and honesty without splitting the feature. |
| **Net-new public chart controls in `Controls` (Tier 1 surface growth)** | Many high-value Ant Charts types (area/histogram/box/heatmap/radar/sankey/treemap/…) have no repo analog | Styling the existing five charts cannot reach maximal coverage; theme-only or composition-only coverage was rejected for the same reason in D2.1. |
| **Theme-role-derived chart palette (net-new charts only)** | Ant divergence must flow through the theme/token seam, not a theme-identity branch (FR-007) | A literal per-theme palette would either change Default output (breaks SC-004) or require a theme-identity branch (breaks FR-007); deriving net-new palettes from theme roles keeps Default untouched and Ant divergent. |

No new package, no 4th-project, no charting-engine dependency introduced.
