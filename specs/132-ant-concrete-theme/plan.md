# Implementation Plan: Concrete Ant Design theme with widened component coverage (D2.1)

**Branch**: `132-ant-concrete-theme` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/132-ant-concrete-theme/spec.md`

## Summary

Deliver the flagship **`FS.GG.UI.Themes.AntDesign`** theme package — a concrete `Theme` value plus a `StyleResolver.IntentPolicy` that drive Ant's visual language over the framework's *existing* semantic controls (no forks), opt-in and behaviour-neutral for current consumers. Widen coverage of [Ant's component overview](https://ant.design/components/overview/) to the maximum the chosen scope allows by (a) styling the 52 existing controls, (b) **adding net-new, generic, theme-agnostic controls** to `FS.GG.UI.Controls` for the gaps, and (c) realizing the rest as documented compositions. A machine-checked **coverage matrix** (one row per Ant overview component → existing / net-new / composition / not-applicable) plus an **honesty check** against the live public surface keeps "maximal" honest; a **parity test** proves one control tree renders behaviour-identically and visually-divergently under Default vs AntDesign. Charts are deferred to the already-recorded plan follow-up (Phase D2-Charts / task D2C.1).

This is a **Tier 1** change: a new public package + new public controls in `Controls`, requiring `.fsi` files, per-package surface baselines, and a decision record landed in lock-step.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: `FS.GG.UI.DesignSystem` (public `Theme`, `StyleResolver` + `IntentPolicy`, layered `DesignTokensExt` seed→map→alias→component + Space/Type/Density/Elevation, `Style`, `VisualState`, `StyleClass` — all promoted public in F5/130); `FS.GG.UI.Scene` (Color/geometry); `FS.GG.UI.Controls` (the semantic control set being styled and extended). SkiaSharp over OpenGL is the render backend (no new dependency added).

**Storage**: N/A. The DTCG token source (`design-tokens.tokens.json`) and generated `DesignTokensExt` already live in `Themes.Default` / `DesignSystem`; this feature composes existing taxonomy values rather than introducing a new store.

**Testing**: Expecto + `YoloDev.Expecto.TestSdk` + `Microsoft.NET.Test.Sdk` (+ FsCheck where property tests fit), in `tests/Controls.Tests/`. Precedent: `Feature093ParityTests.fs`, `Feature105ParityTests.fs`, `DesignTokenParityTests.fs`. New theme-parity, control-contract, and matrix-honesty suites follow these.

**Target Platform**: Cross-platform .NET library packages (`FS.GG.UI.*`); rendering exercised headlessly via the existing offscreen/Scene test path, GL-gated paths stay advisory.

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`), layered design-system / themes / controls packages — single repository, library-first.

**Performance Goals**: No new hot path. Theme construction and resolver calls are O(1) per control as today; the parity/honesty checks are build-time/test-time only. Render output for the Default theme must stay byte-identical (SC-005).

**Constraints**:
- **No control forks** (constitution): all Ant appearance lives in the theme/resolver/tokens; net-new controls are generic and theme-agnostic.
- **Default theme byte-identical**: AntDesign is opt-in; `StyleResolver.resolveDefault` / neutral policy path is untouched.
- **No existing token-value change**: design-token-drift gate stays green; any genuinely new token entries are additive.
- **Tier 1 discipline**: every new public module has a `.fsi`; surface baselines regenerated + committed in the same change.

**Scale/Scope**: ~70 Ant overview components dispositioned; ~25–35 net-new generic controls candidate (presentational majority + a few interactive); 1 new package; 2 regenerated baselines (`Themes.AntDesign` new, `Controls` grown); 1 decision record; matrix doc + honesty check + parity test. **Large** — see Complexity Tracking for the "maximal in one feature" risk and the internal phasing that keeps it shippable.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Status | How this plan satisfies it |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS (planned) | Each net-new control and the theme package start as `.fsi` signatures exercised in FSI, then semantic tests, then `.fs`. Theme/IntentPolicy shape validated through the public `DesignSystem` surface first. |
| **II. Visibility in `.fsi`, no access modifiers in `.fs`; per-module baselines** | PASS (planned) | New `Themes.AntDesign` modules and new `Controls` modules each get a curated `.fsi`. `scripts/refresh-surface-baselines.fsx` gains a `FS.GG.UI.Themes.AntDesign` row; `Controls` + `Themes.AntDesign` baselines regenerated and committed in the same change. |
| **III. Idiomatic Simplicity** | PASS (planned) | Theme is a flat `Theme` record value + a small `IntentPolicy` function; net-new controls follow the existing attribute+event render shape. No SRTP/reflection/type-providers. Any deviation justified at the use site. |
| **IV. Elmish/MVU boundary for stateful/I-O** | PASS (planned) | Presentational controls (Tag, Avatar, Alert, Card, Result, Empty, Statistic, Descriptions, Timeline, Skeleton, Divider, …) are pure render + attributes, like existing `Badge`/`CheckBox`/`Slider` (parent owns state via app Elmish). Genuinely stateful/workflow components (Cascader, Transfer, Upload) either follow the `DataGrid`/`Collections` `Model`/`Msg`/`Effect` pattern **or** are dispositioned composition/deferred in the matrix with rationale — never given ad-hoc internal mutable state. |
| **V. Test Evidence** | PASS (planned) | Parity test (Default vs AntDesign), per-control catalog/semantic/interaction/accessibility/rendering suites for every net-new control, matrix honesty check. Real offscreen-render evidence; any GL-gated bit honest-skipped with rationale. |
| **VI. Observability / safe failure** | N/A (mostly) | No new I/O or context creation; theme construction is pure. Resolver remains total (defined fallback for unknown kinds per `baseStyleFor`). |
| **Layer rule: one control set, many themes; no per-theme forks** | PASS (planned) | The central guardrail. AntDesign styles existing + net-new generic controls only through resolver/token seams; the parity test fails if any control branches on theme identity (FR-014). |
| **Change Classification: Tier 1** | DECLARED | New public package + new public controls. Full chain: spec ✓, plan (this), `.fsi` updates, baseline updates, tests, decision record, module-map update. |
| **Package identity `FS.GG.UI.*`** | PASS | Follows the established post-rebrand scheme used by `Themes.Default` (`PackageId`/`AssemblyName` = `FS.GG.UI.Themes.AntDesign`). |

**Result**: No unjustified violations. The only flagged scope risk ("maximal in one feature") is a planning/sequencing concern, tracked in Complexity Tracking, not a constitution violation.

## Project Structure

### Documentation (this feature)

```text
specs/132-ant-concrete-theme/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions (theme construction, intent policy, control scope batches, matrix format)
├── data-model.md        # Phase 1 — Theme value, IntentPolicy, net-new control entities, coverage-matrix schema
├── quickstart.md        # Phase 1 — how to render under AntDesign, run parity + honesty checks
├── contracts/
│   ├── antdesign-theme.md       # The Theme value + IntentPolicy public contract
│   ├── new-controls-catalog.md  # Catalog rows + .fsi shape for net-new controls
│   └── coverage-matrix.md       # Matrix row schema + honesty-check rules
├── checklists/
│   └── requirements.md  # (already created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── DesignSystem/                 # (unchanged) public Theme, StyleResolver+IntentPolicy, DesignTokensExt, Style, VisualState
├── Themes.Default/               # (unchanged) reference for the new theme package's shape
└── Themes.AntDesign/             # NEW package — depends only on DesignSystem
    ├── Themes.AntDesign.fsproj   #   PackageId/AssemblyName = FS.GG.UI.Themes.AntDesign
    ├── AntTheme.fsi / .fs        #   the concrete Theme value(s) (light/dark) from Ant-derived token entries
    └── AntIntentPolicy.fsi / .fs #   IntentPolicy mapping primary/default/dashed/text/link + danger

src/Controls/
├── catalog.yml                   # +rows for each net-new control (source of truth)
├── Catalog.fs / .fsi             # regenerated GENERATED rows for the new ids
├── <NewControls>.fsi / .fs       # net-new generic controls (presentational batch + interactive batch),
│                                 #   grouped into a few cohesive modules (e.g. Display2/Feedback2/Navigation2)
└── Control.fs                    # render/geom wiring for new kinds via StyleResolver (no theme branching)

docs/product/ant-design/
└── coverage/ant-component-coverage.md   # the coverage matrix (one row per Ant overview component)

docs/product/decisions/
└── 0006-antdesign-theme-and-new-controls.md   # Tier-1 decision record

scripts/refresh-surface-baselines.fsx          # +1 row: ("FS.GG.UI.Themes.AntDesign", "Themes.AntDesign")
tests/surface-baselines/
├── FS.GG.UI.Themes.AntDesign.txt              # NEW committed baseline
└── FS.GG.UI.Controls.txt                      # regenerated (grown with new controls)

tests/Controls.Tests/
├── Feature132ThemeParityTests.fs              # Default vs AntDesign: contract-identical, visuals-divergent
├── Feature132NewControlContractTests.fs       # catalog/semantic/interaction/accessibility/rendering per new control
└── Feature132CoverageMatrixTests.fs           # honesty check: every Ant component dispositioned; no dangling refs

FS.GG.Rendering.slnx                           # +Themes.AntDesign project (and its test wiring)
docs/product/module-map.md                     # AntDesign theme row: planned → owned assembly
```

**Structure Decision**: Mirror the existing `Themes.Default` package exactly for the new `Themes.AntDesign` package (same fsproj shape, `ProjectReference` to `DesignSystem` only, `.fsi`-first modules). Net-new controls live **inside `FS.GG.UI.Controls`** (not the theme package) because they are generic and theme-agnostic per the layer rule; they are grouped into a few cohesive modules rather than one file per control to keep the project file and compile order manageable. The coverage matrix lives under the existing `docs/product/ant-design/` tree (alongside the F6 reference hub and pattern docs) so it is discoverable with the other Ant material.

## Phased delivery (internal sequencing within this one feature)

Per the user's "maximal in one feature" decision, all phases land in this feature; this is the **internal order** that keeps the tree green at each step and avoids an unshippable in-between state.

- **P-A — Theme package skeleton (US1 core)**: create `Themes.AntDesign` (fsproj, `.slnx`, baseline row + empty baseline), `AntTheme` value + `AntIntentPolicy` over existing controls only. Parity test (existing controls) green. Default theme byte-identical. *Shippable MVP on its own.*
- **P-B — Coverage matrix + honesty check (US2)**: author the matrix dispositioning all ~70 components; wire the honesty check (fails on missing rows / dangling control-or-token refs). Drives P-C scope.
- **P-C — Net-new presentational controls (US3 batch 1)**: the stateless majority (Tag, Avatar, Alert, Card, Result, Empty, Statistic, Descriptions, Timeline, Skeleton, Divider/Segmented-as-render, Breadcrumb, Steps-as-render, …). Each: `.fsi` → catalog row → tests → `.fs`. Regenerate baselines.
- **P-D — Net-new interactive controls (US3 batch 2)**: the parent-state attribute+event controls (Collapse, Pagination, Rate, Segmented selection, Drawer open/close, Popover/Popconfirm overlays, Carousel, FloatButton, Anchor). Genuinely complex workflow components (Cascader/Transfer/Upload) follow `DataGrid` MVU or are dispositioned composition/deferred in the matrix.
- **P-E — Parity hardening + provenance (US4)**: extend the parity test to span every category incl. new families; decision record; module-map update; final baseline regen; full suite + both drift gates green.
- **Charts (US5)**: already recorded in the implementation plan (Phase D2-Charts / task D2C.1). No code here.

## Complexity Tracking

> Filled because the scope (not the constitution) carries justified risk.

| Item | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Maximal coverage in one feature** (~25–35 net-new controls) | User decision (2026-06-16): widen scope, maximal in one feature | A core-set-now / tail-later split was offered and declined. The internal phasing (P-A…P-E) + the coverage matrix preserve shippability and honesty without splitting the feature. |
| **Net-new public controls in `Controls` (Tier 1 surface growth)** | Many high-value Ant components (Tag, Alert, Card, Steps, Collapse…) have no repo analog; the chosen scope is "theme + new controls" | Theme-only or composition-only coverage was offered and declined — it cannot reach maximal coverage. |
| **A few stateful controls may need `Model`/`Msg`/`Effect`** | Constitution IV for workflow components (e.g. Cascader/Transfer/Upload) | Ad-hoc internal mutable state is forbidden; where MVU is too heavy for the value, the component is dispositioned composition/deferred in the matrix rather than faked. |

No 4th-project / repository-pattern / clever-abstraction violations introduced.
