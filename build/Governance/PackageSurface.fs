namespace FS.GG.Rendering.Build.Governance

module PackageSurface =
    let packLocalPackages =
        [ "src/Scene/Scene.fsproj", "FS.GG.UI.Scene"
          "src/SkiaViewer/SkiaViewer.fsproj", "FS.GG.UI.SkiaViewer"
          "src/Layout/Layout.fsproj", "FS.GG.UI.Layout"
          "src/Controls.Elmish/Controls.Elmish.fsproj", "FS.GG.UI.Controls.Elmish"
          "src/Controls/Controls.fsproj", "FS.GG.UI.Controls" ]

    let surfaceBaselines =
        [ "readiness/surface-baselines/FS.GG.UI.Scene.txt"
          "readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt"
          "readiness/surface-baselines/FS.GG.UI.Elmish.txt"
          "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt"
          "readiness/surface-baselines/FS.GG.UI.Layout.txt"
          "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt"
          "readiness/surface-baselines/FS.GG.UI.Controls.txt"
          "readiness/surface-baselines/FS.GG.UI.Testing.txt" ]
