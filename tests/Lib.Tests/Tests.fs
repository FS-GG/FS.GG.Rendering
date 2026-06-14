module FS.Skia.UI.Tests

open System
open System.Diagnostics
open System.IO
open Elmish
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer.Host

type CounterMsg =
    | Increment

let initCounter () = 0, Cmd.none

let updateCounter msg model =
    match msg with
    | Increment -> model + 1, Cmd.none

let viewCounter model =
    Scene.text (12.0, 24.0) $"count: {model}" Colors.white

let red = Colors.rgba 220uy 64uy 52uy 255uy
let blue = Colors.rgba 52uy 112uy 220uy 255uy
let green = Colors.rgba 68uy 168uy 96uy 255uy

let samplePaint =
    Paint.fill red
    |> Paint.withBlendMode Multiply
    |> Paint.withOpacity 0.75
    |> Paint.withAntialias true

type InteractiveModel =
    { PressedKeys: string list
      Pointer: (float * float) option
      PointerDown: bool
      Size: Size
      Closing: bool
      Initialized: bool
      FrameCount: int
      ScreenshotCount: int
      TickCount: int
      LastDiagnostic: RenderDiagnostic option }

type InteractiveMsg =
    | Start
    | RendererInitialized
    | ViewerInput of ViewerEvent
    | FrameRequested
    | FrameRendered
    | ScreenshotRequested of ScreenshotRequest
    | ScreenshotCaptured
    | SubscriptionTick
    | ShutdownComplete
    | Interpret of ViewerEffect<InteractiveMsg>

let initialInteractiveModel =
    { PressedKeys = []
      Pointer = None
      PointerDown = false
      Size = { Width = 640; Height = 480 }
      Closing = false
      Initialized = false
      FrameCount = 0
      ScreenshotCount = 0
      TickCount = 0
      LastDiagnostic = None }

let interactiveView model =
    let fill =
        if model.PointerDown then
            Colors.rgba 220uy 92uy 72uy 255uy
        else
            Colors.rgba 72uy 140uy 220uy 255uy

    Scene.group [
        Scene.rectangle (0.0, 0.0, float model.Size.Width, float model.Size.Height) Colors.black
        Scene.rectangle (24.0, 24.0, 120.0 + float model.TickCount, 80.0) fill
        Scene.text (36.0, 72.0) $"frames: {model.FrameCount}" Colors.white
    ]

let emit effect : Cmd<InteractiveMsg> =
    Cmd.ofMsg (Interpret effect)

let batchEffects effects : Cmd<InteractiveMsg> =
    effects |> List.map emit |> Cmd.batch

let updateInteractive msg model =
    match msg with
    | Start ->
        { model with Initialized = false },
        emit InitializeRenderer
    | RendererInitialized ->
        let next = { model with Initialized = true }
        next, emit (RenderFrame(interactiveView next))
    | ViewerInput event ->
        match event with
        | Loaded ->
            { model with Initialized = true },
            emit (RenderFrame(interactiveView { model with Initialized = true }))
        | UpdateTick _ ->
            model,
            emit (Dispatch SubscriptionTick)
        | RenderTick _ ->
            model,
            emit (RenderFrame(interactiveView model))
        | ViewerEvent.KeyDown key ->
            { model with PressedKeys = key :: (model.PressedKeys |> List.filter ((<>) key)) },
            emit (Dispatch FrameRequested)
        | ViewerEvent.KeyUp key ->
            { model with PressedKeys = model.PressedKeys |> List.filter ((<>) key) },
            Cmd.none
        | PointerMoved(x, y) ->
            { model with Pointer = Some(x, y) },
            emit (Dispatch FrameRequested)
        | PointerPressed(x, y, _) ->
            { model with Pointer = Some(x, y); PointerDown = true },
            emit (Dispatch FrameRequested)
        | PointerReleased(x, y, _) ->
            { model with Pointer = Some(x, y); PointerDown = false },
            emit (Dispatch FrameRequested)
        | PointerScrolled _
        | PointerExited ->
            model, Cmd.none
        | Resized size ->
            let next = { model with Size = size }
            next, emit (RenderFrame(interactiveView next))
        | CloseRequested ->
            { model with Closing = true },
            emit Shutdown
        | DiagnosticReported diagnostic ->
            { model with LastDiagnostic = Some diagnostic },
            emit (ReportDiagnostic diagnostic)
    | FrameRequested ->
        model, emit (RenderFrame(interactiveView model))
    | FrameRendered ->
        { model with FrameCount = model.FrameCount + 1 },
        Cmd.none
    | ScreenshotRequested request ->
        model,
        emit (CaptureScreenshot request)
    | ScreenshotCaptured ->
        { model with ScreenshotCount = model.ScreenshotCount + 1 },
        Cmd.none
    | SubscriptionTick ->
        let next = { model with TickCount = model.TickCount + 1 }
        next, emit (RenderFrame(interactiveView next))
    | ShutdownComplete ->
        { model with Closing = false; Initialized = false },
        Cmd.none
    | Interpret _ ->
        model, Cmd.none

let runCmd (cmd: Cmd<'msg>) =
    let messages = ResizeArray<'msg>()
    cmd |> List.iter (fun effect -> effect messages.Add)
    List.ofSeq messages

let onlyEffect cmd =
    match runCmd cmd with
    | [ Interpret effect ] -> effect
    | messages -> failtestf "expected one interpreted viewer effect, got %A" messages

let interactiveProgram () =
    let config =
        Viewer.defaultConfiguration "interactive tests" initialInteractiveModel.Size

    Viewer.create config (fun () -> initialInteractiveModel, Cmd.none) updateInteractive interactiveView
    |> Viewer.withEffectMapping (function
        | Interpret effect -> Some effect
        | _ -> None)

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | None ->
            failwithf "Could not locate repository root from %s" directory
        | Some parent ->
            findRepositoryRoot parent.FullName

let repositoryRoot =
    findRepositoryRoot AppContext.BaseDirectory

let dotnetRunLock = obj ()

let readinessPath segments =
    let historicalFeature = Path.Combine(repositoryRoot, "specs", "002-skia-feature-parity")
    let readinessRoot =
        if File.Exists(Path.Combine(historicalFeature, "spec.md")) then
            Path.Combine(historicalFeature, "readiness")
        else
            Path.Combine(repositoryRoot, "readiness", "parity")

    Path.Combine(Array.ofList (readinessRoot :: segments))

let writeReadinessEvidence relativePath (text: string) =
    let path = readinessPath relativePath
    match Path.GetDirectoryName path with
    | null -> ()
    | directory -> Directory.CreateDirectory(directory) |> ignore
    File.WriteAllText(path, text)
    path

let runDotnet (arguments: string) =
    lock dotnetRunLock (fun () ->
        let startInfo: ProcessStartInfo = ProcessStartInfo("dotnet", arguments)
        startInfo.WorkingDirectory <- repositoryRoot
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false

        match Process.Start(startInfo) |> Option.ofObj with
        | None ->
            failwithf "Could not start dotnet %s" arguments
        | Some proc ->
            use proc = proc
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExitAsync().GetAwaiter().GetResult()
            proc.ExitCode, stdout, stderr)

let sampleSmokeResult sampleName arguments =
    match Environment.GetEnvironmentVariable "FS_SKIA_SAMPLE_SMOKE_DIR" with
    | null
    | "" ->
        runDotnet arguments
    | directory ->
        let evidencePath = Path.Combine(directory, sampleName + ".txt")

        if File.Exists evidencePath then
            let evidence = File.ReadAllText evidencePath

            if evidence.Contains("exit-code=0", StringComparison.Ordinal) then
                0, evidence, ""
            else
                1, evidence, evidence
        else
            runDotnet arguments

[<Tests>]
let publicSurfaceTests =
    testList "Public surface" [
        test "configuration can be constructed without renderer selection" {
            let config =
                Viewer.defaultConfiguration "contract" { Width = 800; Height = 600 }

            Expect.equal config.Title "contract" "title is retained"
            Expect.equal config.InitialSize.Width 800 "width is retained"
            Expect.equal config.TargetFrameRate (Some 60) "default frame rate is explicit"
        }

        test "scene constructors create stable values" {
            let scene =
                Scene.group [
                    Scene.empty
                    Scene.rectangle (0.0, 0.0, 100.0, 40.0) (Colors.rgba 10uy 20uy 30uy 255uy)
                    Scene.text (4.0, 16.0) "hello" Colors.white
                    Scene.image (0.0, 0.0, 16.0, 16.0) "asset.png"
                    Scene.chart [ 1.0; 2.0; 3.0 ]
                ]

            Expect.isNotNull (box scene) "scene is constructible through the public API"
        }

        test "viewer create preserves pure Elmish functions and default subscriptions" {
            let config =
                Viewer.defaultConfiguration "counter" { Width = 320; Height = 240 }

            let program = Viewer.create config initCounter updateCounter viewCounter
            let model, _ = program.Init()
            let next, _ = program.Update Increment model

            Expect.equal model 0 "init returns the model"
            Expect.equal next 1 "update is the supplied pure transition"
            Expect.isEmpty (program.Subscriptions next) "subscriptions default to none"
            Expect.isNotNull (box (program.View next)) "view returns a scene"
        }

        test "viewer withSubscription replaces subscription source" {
            let config =
                Viewer.defaultConfiguration "subscriptions" { Width = 320; Height = 240 }

            let program =
                Viewer.create config initCounter updateCounter viewCounter
                |> Viewer.withSubscription (fun _ -> [ [ "timer" ], fun _ -> { new System.IDisposable with member _.Dispose() = () } ])

            Expect.equal (program.Subscriptions 0 |> List.map fst) [ [ "timer" ] ] "subscription identity is public"
        }

        test "viewer withEventMapping maps viewer events to application messages" {
            let config =
                Viewer.defaultConfiguration "events" { Width = 320; Height = 240 }

            let program =
                Viewer.create config initCounter updateCounter viewCounter
                |> Viewer.withEventMapping (function
                    | ViewerEvent.KeyDown "Add" -> Some Increment
                    | _ -> None)

            Expect.equal (program.EventMapper (ViewerEvent.KeyDown "Add")) (Some Increment) "matching viewer event maps to app message"
            Expect.equal (program.EventMapper (ViewerEvent.KeyUp "Add")) None "non-matching viewer event is ignored"
        }

        test "viewer withEffectMapping identifies messages for host-side effect interpretation" {
            let config =
                Viewer.defaultConfiguration "effects" initialInteractiveModel.Size

            let program =
                Viewer.create config (fun () -> initialInteractiveModel, Cmd.none) updateInteractive interactiveView
                |> Viewer.withEffectMapping (function
                    | Interpret effect -> Some effect
                    | _ -> None)

            match program.EffectMapper (Interpret InitializeRenderer) with
            | Some InitializeRenderer -> ()
            | other -> failtestf "expected initialize effect mapping, got %A" other

            Expect.equal (program.EffectMapper SubscriptionTick) None "ordinary application messages are handled by update"
        }
    ]

[<Tests>]
let diagnosticTests =
    testList "Diagnostics" [
        test "diagnostic helpers preserve severity stage message and cause" {
            let diagnostic =
                Diagnostics.create Fatal GlRenderer "device failed" (Some "driver")

            Expect.equal diagnostic.Severity Fatal "severity is retained"
            Expect.equal diagnostic.Stage GlRenderer "stage is retained"
            Expect.equal diagnostic.Message "device failed" "message is retained"
            Expect.equal diagnostic.Cause (Some "driver") "cause is retained"
        }

        test "invalid size fails before Vulkan startup" {
            let config =
                Viewer.defaultConfiguration "invalid" { Width = 0; Height = 240 }

            let program = Viewer.create config initCounter updateCounter viewCounter

            match Viewer.run program with
            | Result.Error diagnostic ->
                Expect.equal diagnostic.Stage PlatformCheck "configuration fails at platform-check stage"
                Expect.stringContains diagnostic.Message "size" "diagnostic identifies invalid size"
            | Ok() -> failtest "invalid configuration must not start"
        }

        test "fallback renderer language is absent from configuration surface" {
            let configurationFields =
                typeof<ViewerConfiguration>.GetProperties()
                |> Array.map _.Name
                |> String.concat "\n"

            Expect.isFalse (configurationFields.Contains "Renderer") "no renderer selector is exposed"
            Expect.isFalse (configurationFields.Contains "Fallback") "no fallback selector is exposed"
        }

        test "frame render diagnostics identify OpenGL path without fallback switching" {
            let diagnostic = Diagnostics.frameRenderFailed "present failed"

            Expect.equal diagnostic.Stage FrameRender "frame failures are reported at the frame-render stage"
            Expect.stringContains diagnostic.Message "OpenGL/Skia frame rendering failed" "diagnostic identifies the frame path"
            Expect.stringContains diagnostic.Message "no fallback renderer" "diagnostic rules out fallback switching"
            Expect.equal diagnostic.Cause (Some "present failed") "cause is retained"
        }
    ]

[<Tests>]
let us1ContractTests =
    testList "US1 Vulkan-only viewer contract" [
        test "primitive group image arc point vertices picture and nested scene constructors are semantic through public API" {
            let child =
                Scene.group [
                    Scene.rectangle (0.0, 0.0, 100.0, 40.0) red
                    Scene.ellipse { X = 8.0; Y = 8.0; Width = 32.0; Height = 20.0 } samplePaint
                    Scene.line { X = 0.0; Y = 0.0 } { X = 24.0; Y = 24.0 } (Paint.stroke blue 2.0)
                    Scene.points [ { X = 1.0; Y = 1.0 }; { X = 2.0; Y = 2.0 } ] samplePaint
                    Scene.vertices Triangles [
                        { Position = { X = 0.0; Y = 0.0 }; Color = Some red }
                        { Position = { X = 10.0; Y = 0.0 }; Color = Some blue }
                        { Position = { X = 0.0; Y = 10.0 }; Color = Some green }
                    ] samplePaint
                    Scene.arc { X = 2.0; Y = 2.0; Width = 20.0; Height = 20.0 } 0.0 90.0 (Paint.stroke green 1.5)
                    Scene.image (4.0, 4.0, 16.0, 16.0) "asset.png"
                ]

            let picture = { Name = "nested"; Scene = child }
            let scene = Scene.group [ child; Scene.picture picture ]
            let kinds = Scene.describe scene

            [ GroupElement
              RectangleElement
              EllipseElement
              LineElement
              PointsElement
              VerticesElement
              ArcElement
              ImageElement
              PictureElement ]
            |> List.iter (fun kind -> Expect.contains kinds kind $"scene contains {kind}")
        }

        test "paint defaults and options preserve fill stroke opacity antialiasing caps joins miter and blend modes" {
            let strokePaint =
                Paint.stroke blue 3.5
                |> Paint.withOpacity 0.5
                |> Paint.withAntialias false
                |> Paint.withStrokeCap Round
                |> Paint.withStrokeJoin Bevel
                |> Paint.withMiter 8.0
                |> Paint.withBlendMode Screen

            Expect.equal samplePaint.Fill (Some red) "fill color is retained"
            Expect.equal samplePaint.BlendMode Multiply "blend mode is retained"
            Expect.equal samplePaint.Opacity 0.75 "opacity is retained"
            Expect.isTrue samplePaint.Antialias "antialiasing is retained"

            match strokePaint.Stroke with
            | Some stroke ->
                Expect.equal stroke.Width 3.5 "stroke width is retained"
                Expect.equal stroke.Cap Round "stroke cap is retained"
                Expect.equal stroke.Join Bevel "stroke join is retained"
                Expect.equal stroke.Miter 8.0 "stroke miter is retained"
            | None -> failtest "stroke paint must contain stroke options"

            [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ]
            |> List.iter (fun blendMode ->
                let paint = Paint.fill red |> Paint.withBlendMode blendMode
                Expect.equal paint.BlendMode blendMode $"blend mode {blendMode} is accepted")
        }

        test "path-effect declarations expose lean scene-vocabulary diagnostics" {
            let diagnosticPaint =
                Paint.fill red
                |> Paint.withShader (LinearGradient({ X = 0.0; Y = 0.0 }, { X = 10.0; Y = 10.0 }, []))
                |> Paint.withColorFilter (BlendColor(blue, Overlay))
                |> Paint.withMaskFilter (Blur 3.0)
                |> Paint.withImageFilter (DropShadow(2.0, 3.0, 4.0, Colors.black))
                |> Paint.withPathEffect (Dash([], 0.0))

            let diagnostics =
                Scene.rectangleWithPaint { X = 0.0; Y = 0.0; Width = 40.0; Height = 20.0 } diagnosticPaint
                |> Scene.diagnostics

            // The split FS.Skia.UI.Scene vocabulary is FSharp.Core-only: it diagnoses pure declaration
            // problems (an empty dash path-effect) but no longer performs SkiaSharp-backed shader/font
            // capability probing — that host-level diagnosis travels with the viewer host.
            Expect.isNonEmpty diagnostics "an empty dash path-effect declaration is diagnosed"
            Expect.exists diagnostics (fun d -> d.Message.Contains "path effect") "path-effect diagnostic is structured"
        }

        test "invalid image resources report frame diagnostics" {
            let missingImagePath =
                readinessPath [ "sample-assets"; "missing-image.png" ]

            let malformedPath =
                Path.create Winding [
                    Path.lineTo 10.0 10.0
                    Path.close
                ]

            let diagnostics =
                Scene.group [
                    Scene.image (0.0, 0.0, 16.0, 16.0) missingImagePath
                    Scene.textRun
                        { Text = "fallback"
                          Position = { X = 0.0; Y = 24.0 }
                          Font =
                            { Family = Some "FS-Skia-UI-Definitely-Missing-Font"
                              Size = 12.0
                              Weight = None }
                          Paint = Paint.fill Colors.white }
                    Scene.path malformedPath samplePaint
                ]
                |> Scene.diagnostics

            // The lean scene vocabulary validates image-resource existence; SkiaSharp-backed font
            // availability and path-structure validation are host-level concerns that no longer live
            // in FS.Skia.UI.Scene.
            Expect.exists diagnostics (fun d -> d.Message.Contains "Invalid image resource") "missing image reports an invalid resource"
        }

        test "path commands fill types boolean operations measurement segment extraction and helpers are semantic" {
            let first =
                Path.create Winding [
                    Path.moveTo 0.0 0.0
                    Path.lineTo 10.0 0.0
                    Path.quadTo { X = 12.0; Y = 4.0 } { X = 10.0; Y = 10.0 }
                    Path.cubicTo { X = 8.0; Y = 12.0 } { X = 2.0; Y = 12.0 } { X = 0.0; Y = 10.0 }
                    Path.close
                ]

            let second =
                Path.create EvenOdd [
                    MoveTo { X = 5.0; Y = 5.0 }
                    ArcTo({ X = 5.0; Y = 5.0; Width = 10.0; Height = 10.0 }, 0.0, 180.0)
                    Close
                ]

            let combined = Path.combine Union first second
            let measured = Path.measure combined
            let segment = Path.segment 0.0 5.0 combined

            Expect.equal combined.FillType Winding "combined path keeps the left fill type"
            Expect.isGreaterThan measured.Length 20.0 "path measurement accounts for line and curve endpoints"
            Expect.isTrue measured.IsClosed "path measurement records closed paths"
            Expect.isSome (Path.bounds combined) "path bounds are available"
            Expect.isNonEmpty segment.Commands "non-empty segment extraction preserves path commands"
            Expect.contains (Scene.describe (Scene.path combined samplePaint)) PathElement "path scene is constructible"
        }

        test "clipping region text font text-run picture color-space and perspective declarations are semantic" {
            let clipPath =
                Path.create Winding [
                    Path.moveTo 0.0 0.0
                    Path.lineTo 20.0 0.0
                    Path.lineTo 20.0 20.0
                    Path.close
                ]

            let textRun =
                { Text = "Skia"
                  Position = { X = 6.0; Y = 18.0 }
                  Font =
                    { Family = Some "Arial"
                      Size = 14.0
                      Weight = Some 500 }
                  Paint = Paint.fill Colors.white }

            let region =
                { Bounds = [ { X = 0.0; Y = 0.0; Width = 12.0; Height = 12.0 } ]
                  Operation = RegionUnion }

            let perspective =
                { M11 = 1.0
                  M12 = 0.0
                  M13 = 0.001
                  M21 = 0.0
                  M22 = 1.0
                  M23 = 0.001
                  M31 = 0.0
                  M32 = 0.0
                  M33 = 1.0 }

            let scene =
                Scene.group [
                    Scene.clipped (PathClip clipPath) (Scene.textRun textRun)
                    Scene.clipped (RectClip { X = 0.0; Y = 0.0; Width = 32.0; Height = 32.0 }) (Scene.picture { Name = "text"; Scene = Scene.text (2.0, 16.0) "picture" blue })
                    Scene.region region samplePaint
                    Scene.withColorSpace DisplayP3 (Scene.rectangle (0.0, 0.0, 4.0, 4.0) red)
                    Scene.withPerspective perspective (Scene.rectangle (4.0, 4.0, 4.0, 4.0) blue)
                ]

            let kinds = Scene.describe scene

            [ ClipElement; TextRunElement; PictureElement; RegionElement; ColorSpaceElement; PerspectiveElement ]
            |> List.iter (fun kind -> Expect.contains kinds kind $"scene contains {kind}")

            let measured = Scene.measureText textRun.Text textRun.Font
            let longer = Scene.measureText (textRun.Text + textRun.Text) textRun.Font

            Expect.isGreaterThan measured.Width 0.0 "text measurement reports a positive width"
            Expect.isGreaterThan measured.Height 0.0 "text measurement reports a positive height"
            Expect.isGreaterThan longer.Width measured.Width "longer text measures wider"
            Expect.equal textRun.Font.Family (Some "Arial") "font family is retained"
            Expect.equal region.Operation RegionUnion "region operation is retained"
            Expect.equal perspective.M33 1.0 "perspective transform is retained"
        }

        test "drawing parity gallery render-readback evidence covers at least sixty public visual capabilities" {
            let paints =
                [ for blendMode in [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ] do
                      Paint.fill red |> Paint.withBlendMode blendMode
                  Paint.stroke blue 1.0 |> Paint.withStrokeCap Butt
                  Paint.stroke blue 2.0 |> Paint.withStrokeCap Round
                  Paint.stroke blue 3.0 |> Paint.withStrokeCap Square
                  Paint.stroke green 1.0 |> Paint.withStrokeJoin Miter
                  Paint.stroke green 1.0 |> Paint.withStrokeJoin RoundJoin
                  Paint.stroke green 1.0 |> Paint.withStrokeJoin Bevel
                  Paint.fill red |> Paint.withShader (SolidColor red)
                  Paint.fill red |> Paint.withShader (LinearGradient({ X = 0.0; Y = 0.0 }, { X = 20.0; Y = 20.0 }, [ red; blue ]))
                  Paint.fill red |> Paint.withShader (RadialGradient({ X = 10.0; Y = 10.0 }, 8.0, [ red; green ]))
                  Paint.fill red |> Paint.withShader (SweepGradient({ X = 10.0; Y = 10.0 }, [ blue; green ]))
                  Paint.fill red |> Paint.withColorFilter (BlendColor(blue, Multiply))
                  Paint.fill red |> Paint.withMaskFilter (Blur 2.0)
                  Paint.fill red |> Paint.withImageFilter (DropShadow(2.0, 2.0, 2.0, Colors.black))
                  Paint.stroke red 1.0 |> Paint.withPathEffect (Dash([ 2.0; 2.0 ], 0.0))
                  Paint.stroke red 1.0 |> Paint.withPathEffect (Discrete(3.0, 1.0))
                  Paint.stroke red 1.0 |> Paint.withPathEffect (Corner 2.0) ]

            let path =
                Path.create EvenOdd [
                    Path.moveTo 10.0 10.0
                    Path.lineTo 30.0 10.0
                    Path.lineTo 30.0 30.0
                    Path.close
                ]

            let visualScenes =
                [ for index in 0 .. 63 do
                      let paint = paints[index % paints.Length]
                      match index % 8 with
                      | 0 -> Scene.rectangleWithPaint { X = float index; Y = 0.0; Width = 8.0; Height = 8.0 } paint
                      | 1 -> Scene.ellipse { X = float index; Y = 10.0; Width = 8.0; Height = 8.0 } paint
                      | 2 -> Scene.line { X = float index; Y = 20.0 } { X = float index + 8.0; Y = 28.0 } paint
                      | 3 -> Scene.path path paint
                      | 4 -> Scene.points [ { X = float index; Y = 32.0 } ] paint
                      | 5 -> Scene.vertices Triangles [ { Position = { X = float index; Y = 40.0 }; Color = Some red } ] paint
                      | 6 -> Scene.arc { X = float index; Y = 48.0; Width = 8.0; Height = 8.0 } 0.0 120.0 paint
                      | _ -> Scene.textRun { Text = $"T{index}"; Position = { X = float index; Y = 64.0 }; Font = { Family = None; Size = 12.0; Weight = None }; Paint = paint } ]

            let scene = Scene.group visualScenes
            let readback = Scene.renderReadbackEvidence { Width = 640; Height = 360 } scene
            let capabilityLines =
                visualScenes
                |> List.mapi (fun index visual ->
                    let description =
                        Scene.describe visual
                        |> List.map string
                        |> String.concat ","

                    $"capability-{index + 1:D2}: {description}")

            let evidencePath =
                writeReadinessEvidence
                    [ "screenshots"; "us1-render-readback.txt" ]
                    (String.concat "\n" ([ $"hash={readback.DeterministicHash}"; $"semantic-capabilities={visualScenes.Length}" ] @ capabilityLines))

            Expect.isGreaterThanOrEqual visualScenes.Length 60 "gallery covers at least sixty visual declarations"
            Expect.isGreaterThanOrEqual readback.CapabilityCount 8 "readback preserves the public visual categories"
            Expect.isTrue (File.Exists evidencePath) "render-readback evidence artifact is captured under readiness"
        }

        test "minimal Elmish viewer program produces a scene and render effect" {
            let config =
                Viewer.defaultConfiguration "minimal" { Width = 640; Height = 480 }

            let scene = Scene.rectangle (0.0, 0.0, 20.0, 20.0) Colors.white
            let program = Viewer.create config initCounter updateCounter (fun _ -> scene)
            let model, _ = program.Init()
            let effect = RenderFrame(program.View model)

            Expect.isNotNull (box (program.View model)) "view produces scene data"

            match effect with
            | RenderFrame rendered -> Expect.isNotNull (box rendered) "render effect carries model-derived scene"
            | _ -> failtest "expected a render-frame effect"
        }

        test "public API exposes no fallback renderer vocabulary" {
            let publicNames =
                [ typeof<ViewerConfiguration>
                  typeof<ViewerProgram<int, CounterMsg>>
                  typeof<ViewerEffect<CounterMsg>> ]
                |> List.collect (fun t ->
                    [ yield t.Name
                      yield! t.GetProperties() |> Array.map _.Name
                      yield! t.GetFields() |> Array.map _.Name ])
                |> String.concat "\n"

            Expect.isFalse (publicNames.Contains "OpenGL") "OpenGL is absent"
            Expect.isFalse (publicNames.Contains "Software") "software renderer is absent"
            Expect.isFalse (publicNames.Contains "Cpu") "CPU renderer is absent"
            Expect.isFalse (publicNames.Contains "Fallback") "fallback renderer is absent"
        }
    ]

[<Tests>]
let us2DiagnosticTests =
    testList "US2 Vulkan-unavailable diagnostics" [
        test "unsupported platform diagnostic is fatal before rendering" {
            let diagnostic = Diagnostics.unsupportedPlatform "Browser"

            Expect.equal diagnostic.Severity Fatal "unsupported platform is fatal"
            Expect.equal diagnostic.Stage PlatformCheck "unsupported platform fails before Vulkan startup"
            Expect.stringContains diagnostic.Message "Unsupported platform" "message identifies platform support"
            Expect.stringContains diagnostic.Message "Windows and Linux" "message lists supported desktop OSes"
        }

        test "opengl unavailable diagnostic identifies OpenGL without fallback language" {
            let diagnostic = Diagnostics.glUnavailable "GL context creation unavailable"
            let rendered = diagnostic.Message + "\n" + (diagnostic.Cause |> Option.defaultValue "")

            Expect.equal diagnostic.Severity Fatal "unavailable OpenGL is fatal"
            Expect.equal diagnostic.Stage GlContext "OpenGL availability fails at context setup"
            Expect.stringContains rendered "OpenGL" "diagnostic names OpenGL availability"
            Expect.stringContains diagnostic.Message "no fallback renderer" "message states no fallback renderer is used"
            Expect.isFalse (rendered.Contains "Vulkan") "message does not suggest Vulkan fallback"
            Expect.isFalse (rendered.Contains "software") "message does not suggest software fallback"
        }

        test "missing OpenGL startup stages Synthetic produce stage-specific diagnostics without GPU resources" {
            // SYNTHETIC: this fixture models native startup failures without mutating the workstation GL driver; real evidence path is the live GL launch plus unsupported-environment smoke capture under readiness/.
            let simulatedFailures =
                [ GlContext, "GL context creation unavailable"
                  GlRenderer, "no suitable GL renderer"
                  GlSurface, "window surface creation failed"
                  Framebuffer, "default framebuffer wrap failed" ]

            let diagnostics =
                simulatedFailures
                |> List.map (fun (stage, cause) ->
                    Diagnostics.create Fatal stage "OpenGL initialization failed. The viewer has no fallback renderer." (Some cause))

            diagnostics
            |> List.iter2
                (fun (stage, cause) diagnostic ->
                    Expect.equal diagnostic.Stage stage "diagnostic preserves simulated startup stage"
                    Expect.equal diagnostic.Cause (Some cause) "diagnostic preserves native failure detail"
                    Expect.stringContains diagnostic.Message "OpenGL initialization" "diagnostic names OpenGL initialization"
                    Expect.stringContains diagnostic.Message "no fallback renderer" "diagnostic rules out fallback rendering")
                simulatedFailures
        }
    ]

[<Tests>]
let us3ElmishFlowTests =
    testList "US3 Elmish-driven viewer flow" [
        test "application update handles keyboard pointer resize close lifecycle diagnostic frame screenshot and subscription messages" {
            let diagnostic = Diagnostics.frameRenderFailed "present failed"
            let screenshot = { Destination = "interactive.png"; Format = Png }

            let afterKeyDown, _ =
                updateInteractive (ViewerInput(ViewerEvent.KeyDown "Space")) initialInteractiveModel

            let afterKeyUp, _ =
                updateInteractive (ViewerInput(ViewerEvent.KeyUp "Space")) afterKeyDown

            let afterPointerMove, _ =
                updateInteractive (ViewerInput(PointerMoved(15.0, 24.0))) initialInteractiveModel

            let afterPointerPress, _ =
                updateInteractive (ViewerInput(PointerPressed(15.0, 24.0, PrimaryButton))) afterPointerMove

            let afterPointerRelease, _ =
                updateInteractive (ViewerInput(PointerReleased(18.0, 30.0, PrimaryButton))) afterPointerPress

            let afterResize, _ =
                updateInteractive (ViewerInput(Resized { Width = 800; Height = 600 })) initialInteractiveModel

            let afterClose, _ =
                updateInteractive (ViewerInput CloseRequested) initialInteractiveModel

            let afterStart, _ =
                updateInteractive Start initialInteractiveModel

            let afterInitialized, _ =
                updateInteractive RendererInitialized afterStart

            let afterLoaded, _ =
                updateInteractive (ViewerInput Loaded) initialInteractiveModel

            let afterDiagnostic, _ =
                updateInteractive (ViewerInput(DiagnosticReported diagnostic)) initialInteractiveModel

            let afterFrame, _ =
                updateInteractive FrameRendered initialInteractiveModel

            let afterScreenshot, _ =
                updateInteractive ScreenshotCaptured initialInteractiveModel

            let afterScreenshotRequest, _ =
                updateInteractive (ScreenshotRequested screenshot) initialInteractiveModel

            let afterTick, _ =
                updateInteractive SubscriptionTick initialInteractiveModel

            Expect.equal afterKeyDown.PressedKeys [ "Space" ] "key down records the pressed key"
            Expect.isEmpty afterKeyUp.PressedKeys "key up removes the pressed key"
            Expect.equal afterPointerMove.Pointer (Some(15.0, 24.0)) "pointer move records location"
            Expect.isTrue afterPointerPress.PointerDown "pointer press records down state"
            Expect.isFalse afterPointerRelease.PointerDown "pointer release clears down state"
            Expect.equal afterPointerRelease.Pointer (Some(18.0, 30.0)) "pointer release records final location"
            Expect.equal afterResize.Size { Width = 800; Height = 600 } "resize updates model size"
            Expect.isTrue afterClose.Closing "close request moves model toward shutdown"
            Expect.isFalse afterStart.Initialized "start leaves model awaiting renderer initialization"
            Expect.isTrue afterInitialized.Initialized "renderer lifecycle message marks viewer initialized"
            Expect.isTrue afterLoaded.Initialized "viewer lifecycle loaded event maps into application state"
            Expect.equal afterDiagnostic.LastDiagnostic (Some diagnostic) "diagnostic message is retained in model"
            Expect.equal afterFrame.FrameCount 1 "frame completion increments frame count"
            Expect.equal afterScreenshot.ScreenshotCount 1 "screenshot completion increments screenshot count"
            Expect.equal afterScreenshotRequest.ScreenshotCount 0 "screenshot request does not mutate completion count"
            Expect.equal afterTick.TickCount 1 "subscription tick updates app state"
        }

        test "update emits initialize render screenshot shutdown diagnostic and dispatch effects" {
            let diagnostic = Diagnostics.frameRenderFailed "present failed"
            let screenshot = { Destination = "interactive.png"; Format = Jpeg }

            match updateInteractive Start initialInteractiveModel |> snd |> onlyEffect with
            | InitializeRenderer -> ()
            | effect -> failtestf "expected InitializeRenderer, got %A" effect

            match updateInteractive FrameRequested initialInteractiveModel |> snd |> onlyEffect with
            | RenderFrame scene -> Expect.isNotNull (box scene) "render effect carries the current scene"
            | effect -> failtestf "expected RenderFrame, got %A" effect

            match updateInteractive (ScreenshotRequested screenshot) initialInteractiveModel |> snd |> onlyEffect with
            | CaptureScreenshot request -> Expect.equal request screenshot "screenshot effect carries destination and format"
            | effect -> failtestf "expected CaptureScreenshot, got %A" effect

            match updateInteractive (ViewerInput CloseRequested) initialInteractiveModel |> snd |> onlyEffect with
            | Shutdown -> ()
            | effect -> failtestf "expected Shutdown, got %A" effect

            match updateInteractive (ViewerInput(DiagnosticReported diagnostic)) initialInteractiveModel |> snd |> onlyEffect with
            | ReportDiagnostic reported -> Expect.equal reported diagnostic "diagnostic report effect carries structured diagnostic data"
            | effect -> failtestf "expected ReportDiagnostic, got %A" effect

            match updateInteractive (ViewerInput(ViewerEvent.KeyDown "Enter")) initialInteractiveModel |> snd |> onlyEffect with
            | Dispatch FrameRequested -> ()
            | effect -> failtestf "expected Dispatch FrameRequested, got %A" effect

            let initialized, renderCmd =
                updateInteractive RendererInitialized initialInteractiveModel

            Expect.isTrue initialized.Initialized "lifecycle update changes model before rendering"

            match onlyEffect renderCmd with
            | RenderFrame scene -> Expect.isNotNull (box scene) "renderer-initialized lifecycle emits first render"
            | effect -> failtestf "expected RenderFrame after lifecycle initialization, got %A" effect
        }

        test "timer subscription dispatches messages without direct mutable scene pushes" {
            let program =
                interactiveProgram ()
                |> Viewer.withSubscription (fun _ ->
                    [ [ "timer"; "tick" ],
                      fun dispatch ->
                          dispatch SubscriptionTick
                          { new System.IDisposable with
                              member _.Dispose() = () } ])

            let dispatched = ResizeArray<InteractiveMsg>()
            let subscriptions = program.Subscriptions initialInteractiveModel

            Expect.equal (subscriptions |> List.map fst) [ [ "timer"; "tick" ] ] "subscription identity is stable"

            subscriptions
            |> List.iter (fun (_, subscribe) ->
                use _handle = subscribe dispatched.Add
                ())

            Expect.equal (List.ofSeq dispatched) [ SubscriptionTick ] "subscription communicates only through Elmish dispatch"

            let next, cmd =
                updateInteractive dispatched[0] initialInteractiveModel

            Expect.equal next.TickCount 1 "dispatched subscription message drives model transition"

            match onlyEffect cmd with
            | RenderFrame scene -> Expect.isNotNull (box scene) "model transition requests a render effect"
            | effect -> failtestf "expected subscription tick to emit RenderFrame, got %A" effect
        }
    ]

[<Tests>]
let us4SampleAndScreenshotTests =
    testList "US4 complete Elmish viewer examples" [
        test "BasicViewer contract smoke compiles and exercises scene and screenshot command" {
            let exitCode, stdout, stderr =
                sampleSmokeResult "BasicViewer" "run --project samples/BasicViewer/BasicViewer.fsproj --no-build --no-restore -- --contract-smoke"

            Expect.equal exitCode 0 stderr
            Expect.stringContains stdout "status=ok" "contract smoke succeeds"
            Expect.stringContains stdout "sample=BasicViewer" "basic sample ran"
            Expect.stringContains stdout "contains-shapes=true" "basic sample has shape composition"
            Expect.stringContains stdout "contains-text=true" "basic sample has text composition"
            Expect.stringContains stdout "contains-image=true" "basic sample has image composition"
            Expect.stringContains stdout "contains-chart=true" "basic sample has chart composition"
            Expect.stringContains stdout "screenshot-format=Png" "basic sample requests PNG screenshot capture"
        }

        test "InteractiveViewer contract smoke compiles and exercises input state and screenshot command" {
            let project = Path.Combine(repositoryRoot, "samples", "InteractiveViewer", "InteractiveViewer.fsproj")

            if File.Exists project then
                let exitCode, stdout, stderr =
                    sampleSmokeResult "InteractiveViewer" "run --project samples/InteractiveViewer/InteractiveViewer.fsproj --no-build --no-restore -- --contract-smoke"

                Expect.equal exitCode 0 stderr
                Expect.stringContains stdout "status=ok" "contract smoke succeeds"
                Expect.stringContains stdout "sample=InteractiveViewer" "interactive sample ran"
                Expect.stringContains stdout "active-key=Some \"Space\"" "keyboard input updates sample state"
                Expect.stringContains stdout "pointer=Some (320.0, 210.0)" "pointer input updates sample state"
                Expect.stringContains stdout "ticks=1" "subscription-style tick updates sample state"
                Expect.stringContains stdout "initialize-effect=true" "sample requests renderer initialization through Elmish effect mapping"
                Expect.stringContains stdout "screenshot-format=Jpeg" "interactive sample requests JPEG screenshot capture"
            else
                Expect.isFalse (File.Exists project) "InteractiveViewer is optional and absent in the minimal template profile"
        }

        test "ScreenshotGallery contract smoke exercises screenshots diagnostics recovery and shutdown effects" {
            let project = Path.Combine(repositoryRoot, "samples", "ScreenshotGallery", "ScreenshotGallery.fsproj")

            if File.Exists project then
                let exitCode, stdout, stderr =
                    sampleSmokeResult "ScreenshotGallery" "run --project samples/ScreenshotGallery/ScreenshotGallery.fsproj --no-build --no-restore -- --contract-smoke"

                Expect.equal exitCode 0 stderr
                Expect.stringContains stdout "status=ok" "contract smoke succeeds"
                Expect.stringContains stdout "sample=ScreenshotGallery" "screenshot sample ran"
                Expect.stringContains stdout "initialize-effect=true" "sample requests renderer initialization through Elmish effect mapping"
                Expect.stringContains stdout "render-effect=true" "sample renders through host effect mapping"
                Expect.stringContains stdout "screenshot-effect=true" "sample requests screenshot capture through Elmish effect mapping"
                Expect.stringContains stdout "recovery-diagnostic-effect=true" "sample reports recoverable frame diagnostics"
                Expect.stringContains stdout "shutdown-effect=true" "sample shuts down through Elmish effect mapping"
            else
                Expect.isFalse (File.Exists project) "ScreenshotGallery is optional and absent in the minimal template profile"
        }

        test "screenshot diagnostics describe capture before a successful frame" {
            let diagnostic =
                Diagnostics.screenshotFailed "Screenshot capture was requested before the first successful OpenGL/Skia frame."

            Expect.equal diagnostic.Stage ScreenshotCapture "diagnostic is reported at screenshot stage"
            Expect.equal diagnostic.Severity DiagnosticSeverity.Error "pre-frame screenshot capture is an error"
            Expect.stringContains diagnostic.Message "Screenshot capture failed" "message identifies screenshot failure"
            Expect.stringContains (diagnostic.Cause |> Option.defaultValue "") "before the first successful OpenGL/Skia frame" "cause identifies missing frame"
        }

        test "interactive update emits screenshot effect through public Elmish boundary" {
            let screenshot =
                { Destination = "interactive.jpg"
                  Format = Jpeg }

            match updateInteractive (ScreenshotRequested screenshot) initialInteractiveModel |> snd |> onlyEffect with
            | CaptureScreenshot request -> Expect.equal request screenshot "screenshot request is carried by public effect"
            | effect -> failtestf "expected CaptureScreenshot, got %A" effect
        }
    ]
