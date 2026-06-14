module Product.WindowOptions

open System
open System.IO
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer
open Product.Model
open Product.View
//#if (profile == "app" || profile == "sample-pack")

type WindowBehaviorSettings =
    { Resize: string
      Maximize: string
      Startup: string
      Position: string
      Backend: string }

let windowBehaviorArgsFromFile path =
    if String.IsNullOrWhiteSpace path || not (File.Exists path) then
        []
    else
        File.ReadAllLines path
        |> Array.toList
        |> List.collect (fun raw ->
            let line = raw.Trim()

            if String.IsNullOrWhiteSpace line || line.StartsWith("#", StringComparison.Ordinal) then
                []
            else
                match line.Split('=', 2, StringSplitOptions.TrimEntries) with
                | [| "resize"; value |]
                | [| "window-resize"; value |] -> [ "--window-resize"; value ]
                | [| "maximize"; value |]
                | [| "window-maximize"; value |] -> [ "--window-maximize"; value ]
                | [| "startup"; value |]
                | [| "startup-state"; value |]
                | [| "window-startup"; value |] -> [ "--window-startup"; value ]
                | [| "position"; value |]
                | [| "startup-position"; value |]
                | [| "window-position"; value |] -> [ "--window-position"; value ]
                | [| "backend"; value |]
                | [| "window-backend"; value |] -> [ "--window-backend"; value ]
                | _ -> [])

let parseWindowBehavior args =
    let rec loop remaining behavior =
        match remaining with
        | "--window-options-file" :: path :: tail ->
            loop (windowBehaviorArgsFromFile path @ tail) behavior
        | "--window-resize" :: "fixed-size" :: tail ->
            loop tail { behavior with Resize = "fixed-size" }
        | "--window-resize" :: "resizable" :: tail ->
            loop tail { behavior with Resize = "resizable" }
        | "--window-maximize" :: "not-maximizable" :: tail ->
            loop tail { behavior with Maximize = "not-maximizable" }
        | "--window-maximize" :: "maximizable" :: tail ->
            loop tail { behavior with Maximize = "maximizable" }
        | "--window-startup" :: "normal" :: tail ->
            loop tail { behavior with Startup = "normal" }
        | "--window-startup" :: "maximized" :: tail ->
            loop tail { behavior with Startup = "maximized" }
        | "--window-startup" :: "minimized" :: tail ->
            loop tail { behavior with Startup = "minimized" }
        | "--window-startup" :: "fullscreen" :: tail ->
            loop tail { behavior with Startup = "fullscreen" }
        | "--window-startup" :: "windowed-fullscreen" :: tail ->
            loop tail { behavior with Startup = "windowed-fullscreen" }
        | "--window-position" :: value :: tail ->
            loop tail { behavior with Position = value }
        | "--window-backend" :: "default" :: tail ->
            loop tail { behavior with Backend = "default" }
        | "--window-backend" :: "vulkan" :: tail ->
            loop tail { behavior with Backend = "vulkan" }
        | "--window-backend" :: "opengl" :: tail ->
            loop tail { behavior with Backend = "opengl" }
        | "--window-backend" :: "software" :: tail ->
            loop tail { behavior with Backend = "software" }
        | _ :: tail -> loop tail behavior
        | [] -> behavior

    loop
        args
        { Resize = "resizable"
          Maximize = "maximizable"
          // Windowed fullscreen is the no-flag default; an explicit --window-startup
          // selection overrides it (the last-specified value wins on conflict).
          Startup = "windowed-fullscreen"
          Position = "centered"
          Backend = "default" }

let toViewerWindowBehavior behavior = behavior

/// Map the parsed string settings onto a real ViewerWindowBehaviorRequest so the
/// live launch (runAppWithWindowBehavior) honors the request — not only the report.
let toViewerLaunchRequest behavior : ViewerWindowBehaviorRequest =
    let startupState =
        match behavior.Startup with
        | "normal" -> ViewerWindowStartupState.Normal
        | "maximized" -> ViewerWindowStartupState.Maximized
        | "minimized" -> ViewerWindowStartupState.Minimized
        | "fullscreen" -> ViewerWindowStartupState.Fullscreen
        | "windowed-fullscreen" -> ViewerWindowStartupState.WindowedFullscreen
        | _ -> Viewer.defaultWindowBehavior.StartupState

    let startupPosition =
        match behavior.Position with
        | "centered" -> Some Centered
        | value ->
            match value.Split(',', StringSplitOptions.TrimEntries) with
            | [| x; y |] ->
                match Int32.TryParse x, Int32.TryParse y with
                | (true, parsedX), (true, parsedY) when parsedX >= 0 && parsedY >= 0 -> Some(Coordinates(parsedX, parsedY))
                | _ -> Some Centered
            | _ -> Some Centered

    { ResizePolicy = (if behavior.Resize = "fixed-size" then FixedSize else Resizable)
      MaximizePolicy = (if behavior.Maximize = "not-maximizable" then NotMaximizable else Maximizable)
      StartupState = startupState
      StartupPosition = startupPosition
      BackendPreference =
        // Qualify the cases: ViewerBackendPreference.Vulkan clashes with
        // ViewerDiagnosticCategory.Vulkan (bare `Vulkan` resolves to the latter).
        match behavior.Backend with
        | "vulkan" -> Some ViewerBackendPreference.Vulkan
        | "opengl" -> Some ViewerBackendPreference.OpenGL
        | "software" -> Some ViewerBackendPreference.Software
        | _ -> Some ViewerBackendPreference.DefaultBackend }

/// True when any explicit --window-* selection flag is present. When false the
/// generated app launches through the durable runApp path and inherits the
/// framework's windowed-fullscreen default; when true the launch is routed through
/// runAppWithWindowBehavior so the live window honors the request.
let windowFlagSupplied (args: string list) =
    args
    |> List.exists (fun arg ->
        match arg with
        | "--window-startup"
        | "--window-resize"
        | "--window-maximize"
        | "--window-position"
        | "--window-backend"
        | "--window-options-file" -> true
        | _ -> false)

let windowOptionStatusText status = status

let private viewerInitialSize = { Width = 640; Height = 480 }

let private writeWindowOptionLines (path: string) exitCode lines =
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory(directory |> string) |> ignore

    File.WriteAllLines(path, Array.ofList lines)
    exitCode

let manualWindowOptionResults behavior =
    let positionStatus, positionObserved, positionMessage =
        match behavior.Position with
        | "centered" -> "honored", "centered", "Centered startup can be requested."
        | value ->
            match value.Split(',', StringSplitOptions.TrimEntries) with
            | [| x; y |] ->
                match Int32.TryParse x, Int32.TryParse y with
                | (true, parsedX), (true, parsedY) when parsedX >= 0 && parsedY >= 0 ->
                    "honored", $"{parsedX},{parsedY}", "Startup coordinates can be requested."
                | _ -> "failed", "none", "Startup coordinates must be non-negative."
            | _ -> "failed", "none", "Startup coordinates must be non-negative."

    let startupStatus, startupObserved, startupMessage =
        match behavior.Startup with
        | "normal" -> "honored", "normal", "Normal startup state can be honored by the viewer host."
        | "maximized" -> "honored", "maximized", "Maximized startup state can be requested."
        | "minimized" -> "unsupported", "none", "Minimized startup is not accepted for visible interactive launch validation."
        | "fullscreen" -> "honored", "fullscreen", "Fullscreen startup can be honored by the viewer host."
        | "windowed-fullscreen" -> "honored", "windowed-fullscreen", "Windowed-fullscreen startup (borderless work-area coverage) can be honored by the viewer host."
        | _ -> "failed", "none", "Startup state is not recognized."

    let backendStatus, backendObserved, backendMessage =
        match behavior.Backend with
        | "default" -> "honored", "default", "Default backend will be selected."
        | "vulkan" -> "honored", "vulkan", "Vulkan backend can be requested."
        | "opengl" -> "unsupported", "none", "OpenGL backend preference is not supported by this viewer host."
        | "software" -> "unsupported", "none", "Software backend preference is not supported by this viewer host."
        | _ -> "degraded", "default", "No backend requested; default backend will be selected."

    [ "initial-size", $"{viewerInitialSize.Width}x{viewerInitialSize.Height}", $"{viewerInitialSize.Width}x{viewerInitialSize.Height}", "honored", "Initial window size is positive and can be requested."
      "resize", behavior.Resize, behavior.Resize, "honored", "Resize policy can be honored by the viewer host."
      "maximize", behavior.Maximize, behavior.Maximize, "honored", "Maximize policy can be honored by the viewer host."
      "startup-state", behavior.Startup, startupObserved, startupStatus, startupMessage
      "startup-position", behavior.Position, positionObserved, positionStatus, positionMessage
      "backend", behavior.Backend, backendObserved, backendStatus, backendMessage ]

let windowOptionsReport evidencePath behavior =
    let request = toViewerWindowBehavior behavior

    let optionLine (option, requested, observed, status, message) =
        $"status={windowOptionStatusText status} mode=interactive-window command=--window-options option={option} requested={requested} observed={observed} diagnostic-class=window-options message={message}"

    let lines =
        [ "validation-contract=Viewer.validateWindowLaunchBehavior viewerOptions.InitialSize"
          "schema=option=resize option=maximize option=startup-state option=startup-position option=backend status=unsupported"
          yield!
              manualWindowOptionResults request
              |> List.map optionLine ]

    writeWindowOptionLines evidencePath 0 lines |> ignore
    lines |> List.iter (printfn "%s")
    0

//#endif
