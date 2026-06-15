# Package & template identity — migration note

> Originally a Stage R7 bridge note. **Updated at Stage R8**: the identity has since been
> **rebranded** `FS.Skia.UI.*` → `FS.GG.UI.*`. The "nothing changed / retained as `FS.Skia.UI.*`"
> statement below was true of the R7 *repository move* and is preserved as history — it is **no
> longer the current identity**.

## Current identity (since Stage R8): rebranded to `FS.GG.UI.*`

At migration **Stage R8** the deferred package-identity decision was resolved to an accepted
**rebrand**: every runtime package ID, root namespace, assembly name, and the `dotnet new` template
identity moved from `FS.Skia.UI.*` to `FS.GG.UI.*` as one coherent matrix. Only the `FS.Skia.UI.`
brand prefix changed; descriptive Skia/SkiaSharp technology references (the `SkiaViewer` module name,
genuine `SkiaSharp` references, the descriptive `skia` package tag) are retained. The public API
surface differs only by the namespace prefix. The new lineage starts at `0.1.0-preview.1`; the old
`FS.Skia.UI.*` IDs freeze at their last published version and are deprecated (not deleted) with a
forward pointer.

Authoritative record: [`docs/product/decisions/0001-package-identity.md`](../product/decisions/0001-package-identity.md)
(status: **accepted**). Deprecation/redirect of old IDs:
[`package-deprecation-notice.md`](./package-deprecation-notice.md).

### Old → new mapping

| Old identity (`FS.Skia.UI.*`) | New identity (`FS.GG.UI.*`) |
|---|---|
| `FS.Skia.UI.Color`           | `FS.GG.UI.Color`           |
| `FS.Skia.UI.Scene`           | `FS.GG.UI.Scene`           |
| `FS.Skia.UI.Layout`          | `FS.GG.UI.Layout`          |
| `FS.Skia.UI.Input`           | `FS.GG.UI.Input`           |
| `FS.Skia.UI.KeyboardInput`   | `FS.GG.UI.KeyboardInput`   |
| `FS.Skia.UI.SkiaViewer`      | `FS.GG.UI.SkiaViewer`      |
| `FS.Skia.UI.Elmish`          | `FS.GG.UI.Elmish`          |
| `FS.Skia.UI.Controls`        | `FS.GG.UI.Controls`        |
| `FS.Skia.UI.Controls.Elmish` | `FS.GG.UI.Controls.Elmish` |
| `FS.Skia.UI.Testing`         | `FS.GG.UI.Testing`         |
| `FS.Skia.UI.Template`        | `FS.GG.UI.Template`        |

(Source of truth for the new IDs: each `src/<Module>/*.fsproj`, `Directory.Build.props`, and
`.template.config/template.json`. Import-path lineage for the imported files is in
[`PROVENANCE.md`](../../PROVENANCE.md), where the import-time `FS.Skia.UI.*` identifiers are mapped to
their `FS.GG.UI.*` form and traced to their source.)

---

## Feature 125: design-system / default-theme namespace relocation out of `FS.GG.UI.Controls`

The design-system primitives and the default theme were **carved out of `FS.GG.UI.Controls`** into
two new, separately-referenceable packages, and the relocated types **changed namespace** to match.
This is a pre-1.0, in-repo-only relocation with **no backward-source-compat shims** (no
`TypeForwardedTo`, no aliases): in-repo consumers add an `open` at the point of use.

| Symbol(s) | Old namespace (`FS.GG.UI.Controls`) | New package / namespace |
|---|---|---|
| `ValidationState`, `VisualState`, `StyleVariant`, `StyleClass`, `ResolvedStyle`, `Theme` (type), `DesignTokens`, `Style.resolve` | `FS.GG.UI.Controls` | **`FS.GG.UI.DesignSystem`** (`FS.GG.UI.DesignSystem`) |
| `Theme` (module: `light`/`dark`/`withDensity`/`withAccent`/`resolve`) | `FS.GG.UI.Controls` | **`FS.GG.UI.Themes.Default`** (`FS.GG.UI.Themes.Default`) |
| `ThemeMode`, `RolePalette`, `Theming` | `FS.GG.UI.Controls.Theming` | **`FS.GG.UI.Themes.Default`** (`FS.GG.UI.Themes.Default.Theming`) |

Consumer migration: add `open FS.GG.UI.DesignSystem` wherever a relocated primitive is named, and
`open FS.GG.UI.Themes.Default` (and `open FS.GG.UI.Themes.Default.Theming`) wherever the default
theme values or the live-theming surface are used. `FS.GG.UI.Controls` itself now references
`FS.GG.UI.DesignSystem` and **must not** reference any theme package (the acyclic layering edge).
`Theme` additionally gains two **additive** roles, `Success`/`Warning` (token-sourced; no existing
field value or render output changes).

Rationale and the no-shim decision: [`docs/product/decisions/0003-designsystem-namespace-relocation.md`](../product/decisions/0003-designsystem-namespace-relocation.md).

---

## History — Stage R7: the repository move did not rename anything

> Retained as history. True of the R7 move; superseded by the R8 rebrand above.

Moving the rendering product from `EHotwagner/FS-Skia-UI` to
[`FS.GG.Rendering`](https://github.com/FS-GG/FS.GG.Rendering) was a **repository** change, not a
**package** change. At that point every package ID, root namespace, assembly name, and the template
package ID was **retained exactly as imported** — a consumer of any `FS.Skia.UI.*` package was
unaffected by the move. Only ownership metadata (`Authors`/`Company`/repository URLs) was re-pointed
to FS-GG at import; the package *identity* was not touched **until R8**.

The R7 note recorded that mapping so nobody would mistake "the repo moved" for "the packages were
renamed," and so that if R8 chose a rename it would start from a documented baseline and publish
replacement packages before deprecating old IDs (per the constitution's package-identity constraint).
**Stage R8 took that rename** — see the current identity above.
