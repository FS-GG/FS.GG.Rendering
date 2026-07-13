// WHAT THIS IS, AND WHAT IT IS NOT (#666).
//
// This file is an inert DECLARATION. **No project compiles it and nothing executes it** — no `.fsproj`
// in the repo includes it, and no code opens the `FS.GG.Rendering.Build.Governance` namespace. Do not
// read it as a governance *check*: it checks nothing, reads nothing, and packs nothing. Comments
// elsewhere used to call it a "governance check" that READ `readiness/surface-baselines/`. It does not.
//
// It is the orphaned metadata of the FAKE build front-end that Feature 045 "relocated into compiled
// build/Governance modules". That relocation never finished: the engine source is unrecoverable
// (`specs/202-fix-build-fsx-engine/research.md` R3), there is no `./fake.sh` at the repo root, and so the
// `PackLocal` / `PackageSurfaceCheck` targets these lists were written to feed cannot be run at all. The
// real build front-end today is `.github/workflows/gate.yml` plus plain `dotnet`.
//
// So why keep it? Because it is **load-bearing as text**. `tests/Package.Tests/Tests.fs` (`buildFrontEnd`)
// reads every `.fs` under `build/Governance/` and asserts against the concatenated string — eight
// assertions across three tests: that the packages below are declared, that three of the baselines below
// are declared, and that the retired Charts package is named NOWHERE in this text (it must not come back).
//
// CAREFUL, and this is not hypothetical — it caught the commit that wrote this header: those Charts
// assertions scan the WHOLE FILE, COMMENTS INCLUDED. Spelling the retired package's id out in prose here
// — even to say it must not appear — puts the literal string in the text and reds the guard. Name it
// descriptively, as this paragraph does; never as the package id.
//
// Deleting this file does not remove dead code; it removes the SUBJECT of those guards. The Charts
// assertions are negative, so an absent subject would make them pass by vacuity — `buildFrontEnd` now
// fails loudly on a missing subject precisely so that cannot happen (#666).
//
// Retiring the FAKE remnants properly — re-pointing these guards at whatever actually packs, then
// deleting this — is tracked separately. Until then: keep the lists TRUE, because tests assert them and
// readers believe them.

namespace FS.GG.Rendering.Build.Governance

module PackageSurface =
    /// The packages PackLocal was written to pack. Asserted by `tests/Package.Tests/Tests.fs`
    /// ("active packages are declared for PackLocal"), which also asserts the retired Charts package is
    /// absent from this list. Do not write that package's id here, even in a comment — see the header.
    let packLocalPackages =
        [ "src/Scene/Scene.fsproj", "FS.GG.UI.Scene"
          "src/SkiaViewer/SkiaViewer.fsproj", "FS.GG.UI.SkiaViewer"
          "src/Layout/Layout.fsproj", "FS.GG.UI.Layout"
          "src/Controls.Elmish/Controls.Elmish.fsproj", "FS.GG.UI.Controls.Elmish"
          "src/Controls/Controls.fsproj", "FS.GG.UI.Controls" ]

    /// Every committed public-surface baseline. This named EIGHT of the sixteen that exist — it had not
    /// been touched since the repo grew past eight packages, and nothing noticed, because nothing consumes
    /// it (the 2026-07-02 code-quality review flagged the 8-of-16 gap as T5). A list a reader takes for
    /// "the surface baselines" must not silently be a subset of them, so it is now the full set: the same
    /// sixteen packages the `packages` table in `scripts/refresh-surface-baselines.fsx` writes, and exactly
    /// the contents of `readiness/surface-baselines/*.txt`.
    let surfaceBaselines =
        [ "readiness/surface-baselines/FS.GG.UI.Build.txt"
          "readiness/surface-baselines/FS.GG.UI.Canvas.txt"
          "readiness/surface-baselines/FS.GG.UI.Controls.txt"
          "readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt"
          "readiness/surface-baselines/FS.GG.UI.DesignSystem.txt"
          "readiness/surface-baselines/FS.GG.UI.Diagnostics.txt"
          "readiness/surface-baselines/FS.GG.UI.Elmish.txt"
          "readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt"
          "readiness/surface-baselines/FS.GG.UI.Layout.txt"
          "readiness/surface-baselines/FS.GG.UI.Scene.txt"
          "readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt"
          "readiness/surface-baselines/FS.GG.UI.Symbology.txt"
          "readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt"
          "readiness/surface-baselines/FS.GG.UI.Testing.txt"
          "readiness/surface-baselines/FS.GG.UI.Themes.AntDesign.txt"
          "readiness/surface-baselines/FS.GG.UI.Themes.Default.txt" ]
