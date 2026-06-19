module SecondAntShowcase.App.RenderLagProbe

open System
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private size: Size = { Width = 1024; Height = 768 }

let private noMods: KeyModifiers =
    { Ctrl = false
      Alt = false
      Shift = false
      Meta = false }

let private options () =
    { Title = "Second Ant Showcase Render Lag Probe"
      InitialSize = size
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = Some 60 }

let private windowBehavior () =
    { Viewer.defaultWindowBehavior with
        StartupState = ViewerWindowStartupState.Normal
        StartupPosition = Some ViewerWindowPosition.Centered
        BackendPreference = Some ViewerBackendPreference.OpenGL }

let private flag name args =
    let rec loop items =
        match items with
        | key :: value :: _ when key = name -> Some value
        | _ :: rest -> loop rest
        | [] -> None

    loop args

let private scenario args =
    flag "--scenario" args |> Option.defaultValue "button-click"

let private theme args =
    match flag "--theme" args with
    | Some value ->
        match VisualConfig.resolveThemeAlias value with
        | Result.Ok(mode, _) -> mode
        | Result.Error _ -> Light
    | None -> Light

let private hostFor scenario theme =
    let baseHost = Host.create theme

    match scenario with
    | "page-change" ->
        { baseHost with
            MapKey =
                fun key pressed ->
                    match key, pressed with
                    | Function 2, true -> Some(NavigateTo "text-numeric-input")
                    | _ -> baseHost.MapKey key pressed }
    | _ -> baseHost

let private scriptFor scenario =
    let key =
        match scenario with
        | "page-change" -> Function 2
        | _ -> Enter

    [ FrameInput.Tick(TimeSpan.FromMilliseconds 16.0)
      FrameInput.Key(key, noMods)
      FrameInput.Tick(TimeSpan.FromMilliseconds 32.0)
      FrameInput.Idle ]

let run args =
    Environment.SetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE", "1")

    let scenario = scenario args
    let theme = theme args
    let host = hostFor scenario theme

    match ControlsElmish.Live.runScriptWithWindowBehavior (options ()) (windowBehavior ()) host (scriptFor scenario) with
    | Result.Ok result ->
        printfn
            "render-lag-probe: scenario=%s status=%s firstFramePresented=%b metrics=%d"
            scenario
            result.Outcome.Status
            result.Outcome.FirstFramePresented
            result.Metrics.Length

        0
    | Result.Error failure ->
        eprintfn "render-lag-probe: failed: %s" failure.Message
        1
