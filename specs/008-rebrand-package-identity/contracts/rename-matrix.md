# Contract: Rename matrix (old → new identity)

The single authoritative old→new identity mapping for R8. Decision `0001` records this map; every
file change conforms to it. All four facets of a row move **together**.

## Runtime modules (×10)

For each module, `PackageId == AssemblyName == Title == root namespace`, transformed identically:

| Module | Old identity (`FS.Skia.UI.*`) | New identity (`FS.GG.UI.*`) | New version |
|---|---|---|---|
| Color            | `FS.Skia.UI.Color`            | `FS.GG.UI.Color`            | `0.1.0-preview.1` |
| Scene            | `FS.Skia.UI.Scene`            | `FS.GG.UI.Scene`            | `0.1.0-preview.1` |
| Layout           | `FS.Skia.UI.Layout`           | `FS.GG.UI.Layout`           | `0.1.0-preview.1` |
| Input            | `FS.Skia.UI.Input`            | `FS.GG.UI.Input`            | `0.1.0-preview.1` |
| KeyboardInput    | `FS.Skia.UI.KeyboardInput`    | `FS.GG.UI.KeyboardInput`    | `0.1.0-preview.1` |
| SkiaViewer¹      | `FS.Skia.UI.SkiaViewer`       | `FS.GG.UI.SkiaViewer`       | `0.1.0-preview.1` |
| Elmish           | `FS.Skia.UI.Elmish`           | `FS.GG.UI.Elmish`           | `0.1.0-preview.1` |
| Controls         | `FS.Skia.UI.Controls`         | `FS.GG.UI.Controls`         | `0.1.0-preview.1` |
| Controls.Elmish  | `FS.Skia.UI.Controls.Elmish`  | `FS.GG.UI.Controls.Elmish`  | `0.1.0-preview.1` |
| Testing          | `FS.Skia.UI.Testing`          | `FS.GG.UI.Testing`          | `0.1.0-preview.1` |

¹ **`SkiaViewer` is descriptive** (a SkiaSharp-backed viewer). Only the `FS.Skia.UI.` **prefix**
changes; the module name `SkiaViewer` is retained → `FS.GG.UI.SkiaViewer`.

## Template

| Artifact | Old | New |
|---|---|---|
| Template package ID / `identity` | `FS.Skia.UI.Template` | `FS.GG.UI.Template` |
| Template package fsproj file name | `.template.package/FS.Skia.UI.Template.fsproj` | `.template.package/FS.GG.UI.Template.fsproj` |
| `dotnet new` short name | `fs-skia-ui` | `fs-gg-ui` |
| `packagePrefix` default | `FS.Skia.UI` | `FS.GG.UI` |
| Skill folders | `template/**/fs-skia-<x>` | `template/**/fs-gg-<x>` |
| Verbatim `api-surface/**/*.fsi` FQ names | `FS.Skia.UI.*` | `FS.GG.UI.*` |

## Transformation rule

- **Brand prefix only**: replace the token `FS.Skia.UI` (dotted) and the kebab brand `fs-skia-ui` /
  `fs-skia-<x>`. Do **not** touch:
  - the descriptive module name `SkiaViewer`;
  - `SkiaSharp` / standalone `Skia` technology references and `using`/`open SkiaSharp`;
  - the descriptive `skia` package **tag** in `<PackageTags>`.
- **Coherence rule**: a row is applied across all its facets in one change. A package ID renamed
  without its namespace (or a baseline file renamed without its contents) is a defect.
- **Runtime kebab literals**: the kebab brand also appears as runtime string literals in two `.fs`
  bodies — `src/Elmish/AnimationTick.fs` (`"fs-skia-ui"`) and `src/SkiaViewer/SkiaViewer.fs`
  (`"fs-skia-ui-runtime"`). These are brand tokens and ARE rebranded (`→ fs-gg-ui` /
  `fs-gg-ui-runtime`); the AnimationTick literal's mirrored test assertion moves in lockstep.

## Out of scope (history — do not rewrite)

`specs/**`, `docs/imported/**`, `docs/audit/**`, and any `bin/**`/`obj/**`. These retain the
identity that was true when written.
