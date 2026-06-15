/// Interactive windowed mode (US1, FR-007): wire the pure Core host through
/// `ControlsElmish.runInteractiveApp`. GL-gated via `Viewer.runtimeCapability` — on a
/// host with no live window/GL it discloses the reason and exits 0 without launching
/// (FR-011); it never fakes a successful interactive render.
module ControlsGallery.App.Interactive

open FS.GG.UI.Scene
open FS.GG.UI.Controls.Theming
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open ControlsGallery.Core

let run (mode: ThemeMode) (accent: Color): int =
    let capability = Viewer.runtimeCapability ()
    if not capability.PersistentWindow then
        printfn "controls-gallery: interactive mode skipped — no live window/GL host."
        let reasons = capability.UnsupportedHostReasons
        if not (List.isEmpty reasons) then
            printfn "  reason: %s" (String.concat "; " reasons)
        else
            printfn "  reason: renderer mode '%s' reports no persistent window." capability.RendererMode
        0
    else
        let host = Host.create mode accent
        let options: ViewerOptions =
            { Title = "Controls Gallery — Indigo & Teal on Slate"
              InitialSize = { Width = 1280; Height = 800 }
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }
        // `Result.Ok`/`Result.Error` are qualified: a viewer namespace also defines an
        // `Ok` union case which would otherwise shadow the F# Result constructors.
        match ControlsElmish.runInteractiveApp options host with
        | Result.Ok outcome ->
            printfn "controls-gallery: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "controls-gallery: interactive launch failed: %s" failure.Message
            1
