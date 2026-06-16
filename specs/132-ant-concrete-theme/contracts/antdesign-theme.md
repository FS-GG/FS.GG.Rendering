# Contract: `FS.GG.UI.Themes.AntDesign` public surface

New package; depends only on `FS.GG.UI.DesignSystem`. Mirrors `FS.GG.UI.Themes.Default`.

## Module `AntTheme` (`AntTheme.fsi`)

```fsharp
namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Ant Design concrete theme: brand-blue primary, Ant functional families, 8-unit grid,
/// Ant control sizing/radii/type — all composed from the generated Ant-derived DesignTokensExt.
module AntTheme =
    /// Ant light Theme (Ant-derived DesignTokensExt light entries).
    val antLight: Theme
    /// Ant dark Theme (Ant-derived DesignTokensExt dark entries).
    val antDark: Theme
    /// Resolve the effective Ant Theme: caller overrides if present, else antLight.
    val resolve: overrides: Theme option -> Theme
```

## Module `AntIntentPolicy` (`AntIntentPolicy.fsi`)

```fsharp
namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// The Ant intent policy: makes primary/default/dashed/text/link/danger visually distinct,
/// driving intent divergence through the F4 seam with no control forks.
module AntIntentPolicy =
    /// The Ant IntentPolicy (use with StyleResolver.resolve to style Ant intents).
    val policy: StyleResolver.IntentPolicy
```

## Contract guarantees

- **C1 (opt-in)**: nothing in this package is referenced unless a consumer selects the Ant theme; Default-theme output is byte-identical (SC-005).
- **C2 (no literals)**: every `Theme` field and intent color derives from a `DesignTokensExt` entry; no inline hex/size at use sites (FR-002).
- **C3 (total intent)**: `AntIntentPolicy.policy.ApplyIntent` returns a defined `ResolvedStyle` for every intent string incl. `""`/unknown; never raises (composes with `StyleResolver.resolve`).
- **C4 (no fork)**: this package contains a `Theme` value + an `IntentPolicy` only — no control types (FR-005).
- **C5 (baseline)**: `FS.GG.UI.Themes.AntDesign.txt` committed; `scripts/refresh-surface-baselines.fsx` has the new row.

## Surface-baseline expectation (new file)

`tests/surface-baselines/FS.GG.UI.Themes.AntDesign.txt` — expected types:

```
FS.GG.UI.Themes.AntDesign.AntIntentPolicy
FS.GG.UI.Themes.AntDesign.AntTheme
```
