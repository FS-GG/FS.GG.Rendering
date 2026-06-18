namespace FS.GG.UI.SkiaViewer.Host

open System
open Elmish
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
// Open the host namespace last so the host's own diagnostic types take precedence over Scene's.
open FS.GG.UI.SkiaViewer.Host

module Viewer =
    let defaultConfiguration title initialSize =
        { Title = title
          InitialSize = initialSize
          ClearColor = Some Colors.black
          TargetFrameRate = Some 60
          Diagnostics = { Verbose = false }
          ConfigureWindow = None
          PresentMode = ViewerPresentMode.DirectToSwapchain }

    let create configuration init update view =
        { Configuration = configuration
          Init = init
          Update = update
          View = view
          EventMapper = fun _ -> None
          EffectMapper = fun _ -> None
          Subscriptions = fun _ -> [] }

    let withSubscription subscription program =
        { program with Subscriptions = subscription }

    let withEventMapping mapper program =
        { program with EventMapper = mapper }

    let withEffectMapping mapper program =
        { program with EffectMapper = mapper }

    let liveProofInterpreterSupported program =
        program.Configuration.InitialSize.Width > 0
        && program.Configuration.InitialSize.Height > 0

    let validate configuration =
        if String.IsNullOrWhiteSpace configuration.Title then
            Result.Error(Diagnostics.invalidConfiguration "Viewer title must not be empty.")
        elif configuration.InitialSize.Width <= 0 || configuration.InitialSize.Height <= 0 then
            Result.Error(Diagnostics.invalidConfiguration "Viewer initial size must be positive.")
        elif
            configuration.TargetFrameRate
            |> Option.exists (fun frameRate -> frameRate <= 0)
        then
            Result.Error(Diagnostics.invalidConfiguration "Target frame rate must be positive when supplied.")
        elif OperatingSystem.IsWindows() || OperatingSystem.IsLinux() then
            Ok()
        else
            Result.Error(Diagnostics.unsupportedPlatform (Environment.OSVersion.Platform.ToString()))

    let run program : Result<unit, RenderDiagnostic> =
        match validate program.Configuration with
        | Result.Error diagnostic -> Result.Error diagnostic
        | Ok() -> GlHost.run program
