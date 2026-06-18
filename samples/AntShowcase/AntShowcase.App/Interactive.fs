/// Interactive windowed mode (contracts/cli.md): wire the pure Core host through
/// `ControlsElmish.runInteractiveApp`, starting on a chosen page under the Ant theme.
/// GL-gated via `Viewer.runtimeCapability` — on a host with no live window/GL it discloses
/// the reason and exits 0 without launching (FR-013); it never fakes a successful render.
module AntShowcase.App.Interactive

open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open AntShowcase.Core
open AntShowcase.Core.Model

let run (mode: ThemeMode) (startPage: string): int =
    let capability = Viewer.runtimeCapability ()
    if not capability.PersistentWindow then
        printfn "ant-showcase: interactive mode skipped — no live window/GL host."
        let reasons = capability.UnsupportedHostReasons
        if not (List.isEmpty reasons) then
            printfn "  reason: %s" (String.concat "; " reasons)
        else
            printfn "  reason: renderer mode '%s' reports no persistent window." capability.RendererMode
        0
    else
        let baseHost = Host.create mode
        let page = PageRegistry.byId startPage
        // App-edge persistence: seed the model with previously-saved feedback, and after each
        // pure update detect a newly-submitted entry (the model's feedback grew) and append it
        // to the log. Core's `update` stays pure; the file write lives only here (Principle IV).
        let host =
            { baseHost with
                Init = fun () -> { Host.initModel with Mode = mode; CurrentPage = page.Id; Feedback = FeedbackStore.load () }, []
                Update =
                    fun msg model ->
                        let model', effects = baseHost.Update msg model
                        if List.length model'.Feedback > List.length model.Feedback then
                            FeedbackStore.append (List.head model'.Feedback)
                        model', effects }
        let options: ViewerOptions =
            { Title = "Ant Design Controls Showcase"
              InitialSize = VisualConfig.preferredSize
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }
        // `Result.Ok`/`Result.Error` are qualified: a viewer namespace also defines an
        // `Ok` union case which would otherwise shadow the F# Result constructors.
        match ControlsElmish.runInteractiveApp options host with
        | Result.Ok outcome ->
            printfn "ant-showcase: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "ant-showcase: interactive launch failed: %s" failure.Message
            1
