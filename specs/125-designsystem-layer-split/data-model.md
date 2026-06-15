# Phase 1 Data Model — Type-by-Type Relocation Map

This feature defines **no new domain data**; it relocates existing types across package boundaries.
The "data model" here is therefore the **carve map**: which existing type/value lands in which
package, the one additive field, and the resulting dependency graph. This is the authoritative
checklist for the move — every row must be accounted for, none dropped (FR-012 / SC-005).

## Target package graph (acyclic)

```text
FS.GG.UI.Scene ──◄── FS.GG.UI.DesignSystem ──◄── FS.GG.UI.Controls ──◄── (Controls.Elmish, SkiaViewer, …)
                              ▲
                              └────────────── FS.GG.UI.Themes.Default
```

- `FS.GG.UI.DesignSystem` → depends on `FS.GG.UI.Scene` **only**.
- `FS.GG.UI.Themes.Default` → depends on `FS.GG.UI.DesignSystem` **only**.
- `FS.GG.UI.Controls` → depends on `FS.GG.UI.DesignSystem` (+ its existing `Scene`/`Layout`/
  `KeyboardInput` refs). It MUST NOT depend on `Themes.Default`.
- **No back-edges**: DesignSystem never references Controls or any theme. Enforced by the build
  (a cycle fails to compile) — see `contracts/layering-contract.md`.

## Relocation map

### → `FS.GG.UI.DesignSystem` (namespace `FS.GG.UI.DesignSystem`)

| Symbol | Kind | Source today | Depends on | Notes |
|---|---|---|---|---|
| `ValidationState` | union | `Controls/Types.fsi` | — | moves because `VisualState` wraps it |
| `VisualState` | union | `Controls/Types.fsi` | `ValidationState` | the visual-state vocabulary |
| `StyleVariant` | `[<RQA>]` union | `Controls/Types.fsi` | — | Primary/Danger/Ghost/Neutral/Success/Warning |
| `StyleClass` | union | `Controls/Types.fsi` | `StyleVariant` | `Variant`/`Custom` |
| `ResolvedStyle` | record | `Controls/Types.fsi` | `Color` (Scene) | **declared before `Theme`** (R1) |
| `Theme` | record | `Controls/Types.fsi` | `Color` (Scene) | **gains `Success`/`Warning`** (R7); declared after `ResolvedStyle` |
| `DesignTokens` (+ `Light`/`Dark`) | module | `Controls/DesignTokens.fsi/.fs` | `Color` (Scene) | generated `.fs` committed here; JSON source moves to Themes.Default (R5) |
| `Style` (`resolve`) | module | `Controls/Style.fsi/.fs` | `Theme`,`ResolvedStyle`,`StyleClass`,`VisualState` | the pure resolver |

### → `FS.GG.UI.Themes.Default` (namespace `FS.GG.UI.Themes.Default`)

| Symbol | Kind | Source today | Depends on | Notes |
|---|---|---|---|---|
| `Theme` (module: `light`/`dark`/`withDensity`/`withAccent`/`resolve`) | module | `Controls/Theme.fsi/.fs` | `Theme` type, `DesignTokens` | concrete default values; sets new `Success`/`Warning` from tokens |
| `ThemeMode` | union | `Controls/Theming.fsi` | — | `Light`/`Dark` |
| `RolePalette` | record | `Controls/Theming.fsi` | `Color` (Scene) | child-namespace isolation preserved (R1/R4) |
| `Theming` (`resolve`/`toTheme`) | module | `Controls/Theming.fsi/.fs` | `Theme` type+module, `RolePalette` | mode+accent derivation |
| `design-tokens.tokens.json` + generation tooling | asset/script | `Controls/` | — | source of the generated `DesignTokens` (R5) |

### stays in `FS.GG.UI.Controls` (namespace `FS.GG.UI.Controls`)

All control-semantic types — none referenced by the design-system types, so none move:

`ControlId`, `ControlKind`, `ChartPoint`, `ChartSeries`, `KnownControl`, `KnownEvent`,
`KnownAttribute`, `StandardControlKind`, `StandardEventKind`, `StandardAttributeName`,
`StandardAttributeValue<'msg>`, `ControlSchema`, `ControlDiagnosticSeverity`,
`ControlDiagnosticCode`, `AccessibilityRole`, `KeyboardOperation`, `ContrastEvidence`, `NavRange`,
`CollectionPosition`, `AccessibilityMetadata`, `ControlEventOrigin`, `NavPayload`, `ControlEvent`,
`AttrCategory`, `Control<'msg>`, `Attr<'msg>`, `AttrValue<'msg>`, `ControlDiagnostic`,
`ControlEventBinding<'msg>`, `ControlRenderResult<'msg>` — plus every other Controls module
(`Attributes`, `Control`, `Reconcile`, `Catalog`, `Widget`/`Widgets`, `TextInput`, `DataGrid`,
`Charts`, `RichText`, `Pointer`, `Focus`, `RetainedRender`, `Diagnostics`, `Accessibility`,
`ControlRuntime`, `CustomControl`, `Collections`).

> `AttrValue<'msg>` keeps its `ThemeValue of Theme`, `StyleClassesValue of StyleClass list`,
> `VisualStateValue of VisualState`, and `ValidationValue of ValidationState` cases — these now
> reference the DesignSystem types via `open FS.GG.UI.DesignSystem`. This is the concrete arrow
> that makes `Controls → DesignSystem` necessary.

## The one additive shape change (FR-004)

`Theme` record gains two fields, sourced from existing tokens — purely additive:

```text
type Theme =
    { … existing fields … ,
      Success: Color    // NEW — from DesignTokens.{Light,Dark}.success
      Warning: Color }  // NEW — from DesignTokens.{Light,Dark}.warning
```

**Construction sites that must set the new fields** (compiler-enumerated):
`Theme.light`, `Theme.dark` (Themes.Default), `Theming.toTheme` (carries non-colour fields from
`Theme.light`; sets Success/Warning from the light defaults or palette), and any test/sample that
builds a `Theme` literal. **Validation rule**: no existing field value changes; no render path
reads `Success`/`Warning` in D1 (so output is identical — additive-only argument, R7).

## Declaration-order invariant (must hold post-move)

Within the DesignSystem types file the order is **`ValidationState` → `VisualState` →
`StyleVariant` → `StyleClass` → `ResolvedStyle` → `Theme`**. `ResolvedStyle` immediately precedes
`Theme` so the shared bare fields (`Foreground`, `FontFamily`, `FontSize`) bind to `Theme` under
F#'s last-declared-wins rule (Research R1). `DesignTokens` compiles before `Theme` (Theme reads
token values). On the theme side, `RolePalette` stays in a child namespace so its role field names
do not poison `Theme` inference where the theme package's own code constructs themes.

## Surface-baseline data (the drift-gate inputs)

| Baseline file | Action | Expected content |
|---|---|---|
| `tests/surface-baselines/FS.GG.UI.DesignSystem.txt` | **add** | the relocated DesignSystem public types/modules |
| `tests/surface-baselines/FS.GG.UI.Themes.Default.txt` | **add** | `Theme` module, `ThemeMode`, `RolePalette`, `Theming` |
| `tests/surface-baselines/FS.GG.UI.Controls.txt` | **regenerate** | today's content **minus** the relocated rows |

A before/after diff of these three files is the SC-005 "relocations only, no removals" evidence:
every line removed from `FS.GG.UI.Controls.txt` must appear (under its new namespace) in one of the
two new baselines.
