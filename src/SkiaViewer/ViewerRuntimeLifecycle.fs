namespace FS.GG.UI.SkiaViewer

open FS.GG.UI.Scene
open Silk.NET.Maths
open Silk.NET.Windowing

/// Pure planning edge for the native-window lifecycle. The platform work-area lookup is injected so
/// startup and live-transition decisions can be characterized without creating a native window.
module internal ViewerRuntimeLifecycle =
    let private windowBehaviorDiagnostic level message =
        { Level = level
          Category = ViewerDiagnosticCategory.Window
          Message = message
          FrameIndex = None
          Stage = Some ViewerRunBlockedStage.Window
          Elapsed = None }

    let applyWindowBehaviorToOptions
        (resolveWorkArea: unit -> (Vector2D<int> * Vector2D<int>) option)
        behavior
        (windowOptions: WindowOptions)
        =
        let mutable applied = windowOptions

        match behavior.ResizePolicy with
        | Resizable -> applied.WindowBorder <- WindowBorder.Resizable
        | FixedSize -> applied.WindowBorder <- WindowBorder.Fixed

        match behavior.StartupState with
        | ViewerWindowStartupState.Normal -> applied.WindowState <- WindowState.Normal
        | ViewerWindowStartupState.Maximized -> applied.WindowState <- WindowState.Maximized
        | ViewerWindowStartupState.Minimized -> applied.WindowState <- WindowState.Minimized
        | ViewerWindowStartupState.Fullscreen -> applied.WindowState <- WindowState.Fullscreen
        | ViewerWindowStartupState.WindowedFullscreen ->
            applied.WindowBorder <- WindowBorder.Hidden
            applied.WindowState <- WindowState.Normal

            match resolveWorkArea () with
            | Some(origin, size) ->
                applied.Position <- origin
                applied.Size <- size
            | None -> ()

        match behavior.StartupPosition with
        | Some(Coordinates(x, y)) -> applied.Position <- Vector2D<int>(x, y)
        | Some Centered
        | None -> ()

        match behavior.BackendPreference with
        | Some ViewerBackendPreference.DefaultBackend
        | Some ViewerBackendPreference.OpenGL
        | None -> applied.API <- GraphicsAPI.Default
        | Some ViewerBackendPreference.Vulkan
        | Some ViewerBackendPreference.Software -> ()

        applied

    let planRuntimeWindowBehavior
        (resolveWorkArea: unit -> (Vector2D<int> * Vector2D<int>) option)
        (behavior: ViewerWindowBehaviorRequest)
        =
        let unsupported = ResizeArray<ViewerDiagnosticEvent>()

        match behavior.StartupState with
        | ViewerWindowStartupState.Minimized ->
            unsupported.Add(
                windowBehaviorDiagnostic
                    ViewerDiagnosticLevel.Error
                    "Runtime ApplyWindowOptions rejected minimized mode: a persistent visible host cannot apply it as a live display mode.")
        | _ -> ()

        match behavior.BackendPreference with
        | Some ViewerBackendPreference.Vulkan
        | Some ViewerBackendPreference.Software ->
            unsupported.Add(
                windowBehaviorDiagnostic
                    ViewerDiagnosticLevel.Error
                    $"Runtime ApplyWindowOptions rejected backend '{behavior.BackendPreference.Value}': an initialized OpenGL context cannot switch backend in place.")
        | _ -> ()

        match behavior.StartupPosition with
        | Some(Coordinates(x, y)) when x < 0 || y < 0 ->
            unsupported.Add(
                windowBehaviorDiagnostic
                    ViewerDiagnosticLevel.Error
                    $"Runtime ApplyWindowOptions rejected negative window coordinates {x},{y}.")
        | _ -> ()

        match behavior.MaximizePolicy with
        | NotMaximizable ->
            unsupported.Add(
                windowBehaviorDiagnostic
                    ViewerDiagnosticLevel.Error
                    "Runtime ApplyWindowOptions rejected NotMaximizable: the active Silk.NET host exposes no live maximize-capability mutation.")
        | Maximizable -> ()

        if unsupported.Count > 0 then
            None, List.ofSeq unsupported
        else
            let mode, token =
                match behavior.StartupState with
                | ViewerWindowStartupState.Normal -> Host.RuntimeWindowMode.Normal, "windowed"
                | ViewerWindowStartupState.Maximized -> Host.RuntimeWindowMode.Maximized, "maximized"
                | ViewerWindowStartupState.Fullscreen -> Host.RuntimeWindowMode.Fullscreen, "fullscreen"
                | ViewerWindowStartupState.WindowedFullscreen -> Host.RuntimeWindowMode.WindowedFullscreen, "borderless"
                | ViewerWindowStartupState.Minimized -> failwith "validated above"

            let border =
                match mode, behavior.ResizePolicy with
                | Host.RuntimeWindowMode.WindowedFullscreen, _
                | Host.RuntimeWindowMode.Fullscreen, _ -> WindowBorder.Hidden
                | _, Resizable -> WindowBorder.Resizable
                | _, FixedSize -> WindowBorder.Fixed

            let workArea =
                if mode = Host.RuntimeWindowMode.WindowedFullscreen then resolveWorkArea () else None

            let position =
                match behavior.StartupPosition, workArea with
                | Some(Coordinates(x, y)), _ -> Some(x, y)
                | _, Some(origin, _) -> Some(origin.X, origin.Y)
                | _ -> None

            let size = workArea |> Option.map (fun (_, extent) -> extent.X, extent.Y)
            let diagnostics = ResizeArray<ViewerDiagnosticEvent>()

            if mode = Host.RuntimeWindowMode.WindowedFullscreen && workArea.IsNone then
                diagnostics.Add(
                    windowBehaviorDiagnostic
                        ViewerDiagnosticLevel.Warning
                        "Runtime borderless mode could not resolve a monitor work area; chrome is hidden, but work-area geometry is unchanged.")

            let plan: Host.RuntimeWindowBehavior =
                { Mode = mode
                  Border = border
                  Position = position
                  Size = size
                  Token = token }

            Some plan, List.ofSeq diagnostics
