# Implementation Plan: Design-System Layer Split (Workstream D, Phase D1)

**Branch**: `125-designsystem-layer-split` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/125-designsystem-layer-split/spec.md`

## Summary

Carve the **design-system primitives** (token model, `Theme` record, `ResolvedStyle`,
`StyleVariant`/`StyleClass`, `VisualState`/`ValidationState`, and the `Style.resolve` resolver)
and the **default Light/Dark theme** (the `Theme` value module + `Theming` mode/accent derivation +
the DTCG token source) out of the monolithic `FS.GG.UI.Controls` assembly into two new,
separately-referenceable packages — `FS.GG.UI.DesignSystem` and `FS.GG.UI.Themes.Default` — making
the four-layer architecture (`scene → design-system → theme/controls`) physically true.

This is a **Tier-1, behaviour-neutral** structural move: every public type/value that exists today
keeps existing and behaving identically; it simply relocates to the layer (and namespace) it
belongs to. The technical approach is a compiler-driven carve that preserves the load-bearing F#
record-field declaration order, adds the already-tokenised `Theme.Success`/`Theme.Warning` roles,
relocates the design-system namespace from `FS.GG.UI.Controls` to `FS.GG.UI.DesignSystem`
(documented, no compat shims — pre-1.0 in-repo consumers only), and lands the solution + surface-
baseline + doc updates **atomically** so CI is green at the same commit. Concrete themes, kits, the
enriched token taxonomy, and the resolver redesign are explicitly out of scope (D2/D3/F).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo-wide `Directory.Build.props`).

**Primary Dependencies**: SkiaSharp-over-OpenGL stack; the moved code's only runtime dependency is
the scene primitives (`FS.GG.UI.Scene`, for `Color`). No new third-party dependency is introduced.

**Storage**: N/A.

**Testing**: Existing repo suites run through the project's harness (`Controls.Tests`,
`Elmish.Tests`, `SkiaViewer.Tests`, sample `ControlsGallery.Tests`); the public-surface drift gate
(`scripts/refresh-surface-baselines.fsx` + `tests/surface-baselines/*.txt`) and the
`DesignTokenDrift` check. No new test framework.

**Target Platform**: Library packages consumed in-repo (samples, template, framework tests).

**Project Type**: Multi-project F# library/framework (single solution `FS.GG.Rendering.slnx`).

**Performance Goals**: None new — behaviour-neutral. Rendered-output and render-path timing
behaviour MUST be identical pre/post split (no hot-path code changes).

**Constraints**:
- **Behaviour-neutral is the hard gate** — existing suite passes unchanged, render output and
  accessibility contract byte-identical where deterministic.
- **Acyclic layering made physical**: `design-system → scene`; `theme → design-system`;
  `controls → design-system`. The design system MUST NOT depend back on controls or any theme.
- **Atomic surface-gate landing**: two new baselines + regenerated (smaller) Controls baseline
  committed in the *same* change; the drift gate fails on any untracked/changed baseline.
- **No backward-source-compat shims** (no `TypeForwardedTo`/aliases) — clean namespace relocation
  recorded in a decision record (pre-1.0, in-repo-only consumers).
- **F# record-field inference order must be preserved** — `ResolvedStyle` is declared *before*
  `Theme` so unannotated `theme.Foreground`/`FontFamily`/`FontSize` accesses bind to `Theme`; the
  `Theming.RolePalette` child-namespace isolation must be preserved on the theme side.

**Scale/Scope**: 3 packages touched (2 new, 1 refactored); ~5 source files relocated/split; the
public surface relocates ~10 design-system types + 1 token module + 1 resolver module + the default
theme/Theming surface; ~85 consumer files (`Controls.Elmish`, the full `Controls.Tests`/
`Elmish.Tests` suites, `SkiaViewer.Tests`, the `ControlsGallery` sample, and the `template/`) gain
an `open FS.GG.UI.DesignSystem` (and, where they use the default theme/Theming,
`open FS.GG.UI.Themes.Default`). Plan tasks D1.1–D1.5.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **PASS** | `.fsi` is the carve unit: the split is *defined* by relocating curated `.fsi` signatures into the new packages first, then the paired `.fs`. No new behaviour to FSI-sketch; the existing semantic suite is the behaviour oracle (it must stay green), satisfying the "tests exercise the packed public surface" intent. |
| II. Visibility lives in `.fsi` | **PASS** | Every relocated module keeps its curated `.fsi`; no access modifiers added to `.fs`. New per-package surface baselines are added (the Principle II drift guard) and the Controls baseline regenerated. |
| III. Idiomatic simplicity | **PASS** | Pure structural move — no new abstractions, operators, SRTP, reflection, or CEs. If the cross-assembly field-inference risk forces explicit type annotations, those are plain and disclosed (see Research R1). |
| IV. Elmish/MVU boundary | **N/A** | No stateful/I-O workflow added; the moved code is pure data + a pure resolver. |
| V. Test evidence | **PASS** | Behaviour-neutrality is proven by the *existing* suite passing unchanged + render-identity evidence; no test removed, weakened, or newly skipped (SC-001/FR-006). No synthetic evidence introduced. |
| VI. Observability & safe failure | **N/A** | No change to diagnostics/critical paths; no GL/IO path altered. |
| Change Classification | **Tier 1** | Moves public API across package/namespace boundaries and adds two `Theme` roles → requires the full chain: spec, plan, `.fsi` relocations, surface-baseline updates (2 new + 1 regenerated), test evidence (existing suite), and docs (decision record + module-map + template/bridge). FR-004's `Success`/`Warning` addition is purely additive. |
| Engineering Constraints — layering clause | **PASS (this feature realises it)** | Converts the "controls, design-system, themes, kits are distinct layers" clause from documentation into compiled structure; no control forks; package identity follows the `FS.GG.UI.*` scheme (decision 0001). |

**Gate result: PASS** — no violations; Complexity Tracking not required. The single watch-item is
the cross-assembly record-field inference behaviour (Research R1), resolved by build-green +
explicit annotation only where the compiler demands it.

## Project Structure

### Documentation (this feature)

```text
specs/125-designsystem-layer-split/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — carve decisions & risk resolutions
├── data-model.md        # Phase 1 — type-by-type relocation map + dependency graph
├── quickstart.md        # Phase 1 — behaviour-neutrality validation guide
├── contracts/
│   ├── layering-contract.md       # acyclic dependency-direction contract
│   └── public-surface-migration.md # before/after public-surface relocation table
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Scene/                       # FS.GG.UI.Scene  (unchanged — the only dep of DesignSystem)
├── DesignSystem/                # NEW  FS.GG.UI.DesignSystem  (assembly folder == row in refresh script)
│   ├── DesignSystem.fsproj      #   ref: Scene only
│   ├── Types.DesignSystem.fsi/.fs  # the design-system slice of today's Controls/Types
│   │                            #   (ValidationState, VisualState, StyleVariant, StyleClass,
│   │                            #    ResolvedStyle, Theme) — namespace FS.GG.UI.DesignSystem
│   ├── DesignTokens.fsi/.fs     # generated token model (exposed here; source lives in Themes.Default)
│   └── Style.fsi/.fs            # Style.resolve
├── Themes.Default/              # NEW  FS.GG.UI.Themes.Default
│   ├── Themes.Default.fsproj    #   ref: DesignSystem only
│   ├── design-tokens.tokens.json# DTCG source + generation tooling (assumption: stays with default theme)
│   ├── Theme.fsi/.fs            # Theme.light/dark/withDensity/withAccent/resolve
│   └── Theming.fsi/.fs          # ThemeMode/RolePalette/Theming (mode+accent derivation)
├── Controls/                    # FS.GG.UI.Controls (refactored — design-system files removed)
│   ├── Controls.fsproj          #   + ProjectReference DesignSystem; Types/DesignTokens/Theme/
│   │                            #     Theming/Style Compile entries removed
│   └── … (Control.fs etc. now `open FS.GG.UI.DesignSystem`)
└── Controls.Elmish/, SkiaViewer/, …  # consumers gain `open FS.GG.UI.DesignSystem` (+ Themes.Default where used)

tests/
├── surface-baselines/
│   ├── FS.GG.UI.DesignSystem.txt        # NEW committed baseline
│   ├── FS.GG.UI.Themes.Default.txt      # NEW committed baseline
│   └── FS.GG.UI.Controls.txt            # REGENERATED (now smaller)
└── …                                    # existing suites gain namespace `open`s only

scripts/refresh-surface-baselines.fsx    # + 2 rows (DesignSystem, Themes.Default)
FS.GG.Rendering.slnx                      # + 2 projects
docs/product/
├── decisions/0003-designsystem-namespace-relocation.md  # NEW decision record (FR-008)
└── module-map.md                         # design-system/theme rows → "owned assembly"
```

**Structure Decision**: Multi-project F# solution. Two new `src/` library projects are added whose
**folder name equals the refresh-script row's project slug** (`DesignSystem`, `Themes.Default`) and
whose `AssemblyName`/`PackageId` follow the `FS.GG.UI.*` scheme
(`FS.GG.UI.DesignSystem`, `FS.GG.UI.Themes.Default`). The design-system types relocate to the new
`FS.GG.UI.DesignSystem` namespace (the documented relocation, FR-008/SC-005); the default theme +
Theming relocate to `FS.GG.UI.Themes.Default`. `FS.GG.UI.Controls` drops the moved Compile entries
and gains a single `ProjectReference` to `DesignSystem`. See `contracts/layering-contract.md` for
the enforced dependency direction and `data-model.md` for the type-by-type carve.

## Complexity Tracking

> Not required — Constitution Check passed with no violations.
