# T001 — Local feed coherent set (recorded 2026-06-27)

Feed: `~/.local/share/nuget-local/`

Coherent published `FS.GG.UI.*` set at `0.1.51-preview.1` (17 packages incl. BOM `FS.GG.UI`):

```
FS.GG.UI.0.1.51-preview.1.nupkg
FS.GG.UI.Build.0.1.51-preview.1.nupkg
FS.GG.UI.Canvas.0.1.51-preview.1.nupkg
FS.GG.UI.Controls.0.1.51-preview.1.nupkg
FS.GG.UI.Controls.Elmish.0.1.51-preview.1.nupkg
FS.GG.UI.DesignSystem.0.1.51-preview.1.nupkg
FS.GG.UI.Diagnostics.0.1.51-preview.1.nupkg
FS.GG.UI.Elmish.0.1.51-preview.1.nupkg
FS.GG.UI.KeyboardInput.0.1.51-preview.1.nupkg
FS.GG.UI.Layout.0.1.51-preview.1.nupkg
FS.GG.UI.Scene.0.1.51-preview.1.nupkg
FS.GG.UI.SkiaViewer.0.1.51-preview.1.nupkg
FS.GG.UI.Symbology.0.1.51-preview.1.nupkg
FS.GG.UI.Symbology.Render.0.1.51-preview.1.nupkg
FS.GG.UI.Testing.0.1.51-preview.1.nupkg
FS.GG.UI.Themes.AntDesign.0.1.51-preview.1.nupkg
FS.GG.UI.Themes.Default.0.1.51-preview.1.nupkg
```

Template package present: FS.GG.UI.Template.0.1.17-preview.1.nupkg FS.GG.UI.Template.0.1.50-preview.1.nupkg 

A generated product pins 11 of these via the single version property; the rest (Canvas, Diagnostics,
Symbology, Symbology.Render, Themes.AntDesign, BOM) are not referenced by the generated tree.
Feed is sufficient for generate→restore→build verification.
