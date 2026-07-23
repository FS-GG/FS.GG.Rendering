namespace FS.GG.UI.SkiaViewer

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text.Json
open System.Threading
open Elmish
open FS.GG.Audio.Core
open FS.GG.UI.Canvas
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open SkiaSharp
open Silk.NET.Input
open Silk.NET.Maths
open Silk.NET.Windowing
// Viewer.Types.fs carved the public type block out of this file; re-open the package namespace
// AFTER the third-party opens so the Viewer types/DU-cases win unqualified-name resolution exactly
// as they did when defined in-file (record-field proximity + DU-case-vs-opened-type; byte-stable).
open FS.GG.UI.SkiaViewer

module Viewer =
    let timingPathToken path = ViewerRuntime.timingPathToken path
    let timingPathCanSupportClaim path proofReadbackIncluded validationReadbackIncluded = ViewerRuntime.timingPathCanSupportClaim path proofReadbackIncluded validationReadbackIncluded
    let damageDecisionToken decision = ViewerRuntime.damageDecisionToken decision
    let init options = ViewerRuntime.init options
    let initWithWindowBehavior options behavior = ViewerRuntime.initWithWindowBehavior options behavior
    let update msg model = ViewerRuntime.update msg model
    let initRun request = ViewerRuntime.initRun request
    let updateRun msg model = ViewerRuntime.updateRun msg model
    let defaultDiagnostics = ViewerRuntime.defaultDiagnostics
    let internal runtimeStateRepaint producedMessages current deriveScene = ViewerRuntime.runtimeStateRepaint producedMessages current deriveScene
    let internal dispatchPersistenceBatch sink mapOutcome dispatch batch = ViewerRuntime.dispatchPersistenceBatch sink mapOutcome dispatch batch
    let internal interpretViewerEffects audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink effects =
        ViewerRuntime.interpretViewerEffects audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink ignore ignore effects

    let internal interpretViewerEffectsWithLogicalCanvas audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink logicalCanvasSink effects =
        ViewerRuntime.interpretViewerEffects audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink ignore logicalCanvasSink effects

    let internal interpretViewerEffectsWithRuntimeWindow audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink windowBehaviorSink logicalCanvasSink effects =
        ViewerRuntime.interpretViewerEffects audioSink persistenceSink onScene onInputDispatch onDiagnostic evidenceSink windowBehaviorSink logicalCanvasSink effects

    let internal planRuntimeWindowBehavior behavior = ViewerRuntime.planRuntimeWindowBehavior behavior

    let internal pointerInProductSpace logicalSize windowSize surfaceSize input =
        ViewerRuntime.pointerInProductSpace logicalSize windowSize surfaceSize input
    let internal productEvidenceSink onDiagnostic sceneSize currentScene effect = ViewerRuntime.productEvidenceSink onDiagnostic sceneSize currentScene effect
    let internal traceStartCapture a0 = ViewerRuntime.traceStartCapture a0
    let internal traceDrainCapture a0 = ViewerRuntime.traceDrainCapture a0
    let internal traceEmit eventName fields = ViewerRuntime.traceEmit eventName fields
    let defaultResponsivenessBudget = ViewerResponsivenessReport.defaultResponsivenessBudget
    let defaultResponsivenessOptions = ViewerResponsivenessReport.defaultResponsivenessOptions
    let responsivenessInputKindToken kind = ViewerResponsivenessReport.responsivenessInputKindToken kind
    let responsivenessVisibleResponseToken response = ViewerResponsivenessReport.responsivenessVisibleResponseToken response
    let responsivenessEnvironmentStatusToken status = ViewerResponsivenessReport.responsivenessEnvironmentStatusToken status
    let responsivenessReadinessToken readiness = ViewerResponsivenessReport.responsivenessReadinessToken readiness
    let emptyInputQueue = ViewerRuntime.emptyInputQueue
    let inputQueueDepth queue = ViewerRuntime.inputQueueDepth queue
    let enqueueInput receivedAt inputKind payload queue = ViewerRuntime.enqueueInput receivedAt inputKind payload queue
    let drainInputQueue batchId drainReason queue = ViewerRuntime.drainInputQueue batchId drainReason queue
    let dirtyState productModelChanged runtimeStateChanged sizeChanged themeChanged dirtyRegion reason = ViewerRuntime.dirtyState productModelChanged runtimeStateChanged sizeChanged themeChanged dirtyRegion reason
    let dirtyStateRequiresRecompose dirty = ViewerRuntime.dirtyStateRequiresRecompose dirty
    let createResponsivenessRunId a0 = ViewerResponsivenessReport.createResponsivenessRunId a0
    let latencyRecordToJsonLine latency = ViewerResponsivenessReport.latencyRecordToJsonLine latency
    let summarizeResponsivenessRecords runId scope recordsPath startedUtc completedUtc budget records = ViewerResponsivenessReport.summarizeResponsivenessRecords runId scope recordsPath startedUtc completedUtc budget records
    let responsivenessSummaryToJson summary = ViewerResponsivenessReport.responsivenessSummaryToJson summary
    let responsivenessSummaryToMarkdown summary = ViewerResponsivenessReport.responsivenessSummaryToMarkdown summary
    let writeResponsivenessRun outputRoot summary records = ViewerResponsivenessReport.writeResponsivenessRun outputRoot summary records
    let defaultWindowBehavior = ViewerWindowClassify.defaultWindowBehavior
    let validateWindowBehavior request = ViewerWindowClassify.validateWindowBehavior request
    let validateWindowLaunchBehavior initialSize request = ViewerWindowClassify.validateWindowLaunchBehavior initialSize request
    let classifyWindowState diagnostic = ViewerWindowClassify.classifyWindowState diagnostic
    let shouldCaptureDiagnostic options diagnostic = ViewerRuntime.shouldCaptureDiagnostic options diagnostic
    let captureDiagnostic options diagnostic = ViewerRuntime.captureDiagnostic options diagnostic
    let productDefectDiagnostic phase message = ViewerWindowClassify.productDefectDiagnostic phase message
    let tryProductStep report phase step = ViewerRuntime.tryProductStep report phase step
    let failureFromDiagnostic diagnostic = ViewerWindowClassify.failureFromDiagnostic diagnostic
    let classifyWindowObservation outcome inputs = ViewerWindowClassify.classifyWindowObservation outcome inputs
    let desktopSessionDiagnostic a0 = ViewerRuntime.desktopSessionDiagnostic a0
    let runtimeCapability a0 = ViewerRuntime.runtimeCapability a0
    let run options scene = ViewerRuntime.run options scene
    let runApp options host = ViewerRuntime.runApp options host
    let runAppWithWindowBehavior options behavior host = ViewerRuntime.runAppWithWindowBehavior options behavior host
    let runAppWithAudio options audioSink host = ViewerRuntime.runAppWithAudio options audioSink host
    let runAppWithWindowBehaviorAndAudio options behavior audioSink host = ViewerRuntime.runAppWithWindowBehaviorAndAudio options behavior audioSink host
    let runAppWithPersistence options persistenceSink mapOutcome host = ViewerRuntime.runAppWithPersistence options persistenceSink mapOutcome host
    let runAppWithAudioAndPersistence options audioSink persistenceSink mapOutcome host = ViewerRuntime.runAppWithAudioAndPersistence options audioSink persistenceSink mapOutcome host
    let runAppWithWindowBehaviorAndAudioAndPersistence options behavior audioSink persistenceSink mapOutcome host = ViewerRuntime.runAppWithWindowBehaviorAndAudioAndPersistence options behavior audioSink persistenceSink mapOutcome host
    let runInteractiveViewer options host = ViewerRuntime.runInteractiveViewer options host
    let runInteractiveViewerWithWindowBehavior options behavior host = ViewerRuntime.runInteractiveViewerWithWindowBehavior options behavior host
    let runInteractiveViewerScript options script host = ViewerRuntime.runInteractiveViewerScript options script host
    let runInteractiveViewerScriptWithWindowBehavior options behavior script host = ViewerRuntime.runInteractiveViewerScriptWithWindowBehavior options behavior script host
    let runInteractiveViewerWithAudio options audioSink host = ViewerRuntime.runInteractiveViewerWithAudio options audioSink host
    let runInteractiveViewerWithWindowBehaviorAndAudio options behavior audioSink host = ViewerRuntime.runInteractiveViewerWithWindowBehaviorAndAudio options behavior audioSink host
    let runInteractiveViewerScriptWithAudio options script audioSink host = ViewerRuntime.runInteractiveViewerScriptWithAudio options script audioSink host
    let runInteractiveViewerScriptWithWindowBehaviorAndAudio options behavior script audioSink host = ViewerRuntime.runInteractiveViewerScriptWithWindowBehaviorAndAudio options behavior script audioSink host
    let runAppEvidence request options host = ViewerRuntime.runAppEvidence request options host
    let runBounded request options scene = ViewerRuntime.runBounded request options scene
    let runUntilFirstFrame options scene = ViewerRuntime.runUntilFirstFrame options scene
    let runForFrames frameCount options scene = ViewerRuntime.runForFrames frameCount options scene
    let captureScreenshotEvidence request options scene = ViewerRuntime.captureScreenshotEvidence request options scene
    let initEvidenceWorkflow request = ViewerRuntime.initEvidenceWorkflow request
    let updateEvidenceWorkflow msg model = ViewerRuntime.updateEvidenceWorkflow msg model

module GeneratedAppHost =
    let dispatchKey (host: GeneratedAppHost<'model, 'msg>) raw model =
        let key, isDown = ViewerKeyboard.normalizeEvent raw

        match host.MapKey key isDown with
        | Some msg -> host.Update msg model
        | None -> model, [ DispatchInput(key, isDown) ]

    /// Issue #911: the scene-host analogue of `ControlsElmish.Perf.runScriptToModel` (#461). Fold an
    /// ordered key-event script through THIS host's own routing — `Init`'s model first, then each event
    /// via `dispatchKey` (`normalizeEvent -> MapKey -> Update`) — to the FINAL model, returning it with
    /// every `ViewerEffect` requested in order (`Init`'s batch first, then each key's). No window, no
    /// device, no GL: it closes the "drive input through the host -> assert the resulting model" loop
    /// purely and headlessly, so a `game`-family product — which takes input through this scene-host,
    /// not the Controls click path — can write an end-to-end "played through the host" test.
    ///
    /// It routes through `dispatchKey`, the SAME fold the live runtime's `handleKey` performs
    /// (`ViewerRuntime`), so what it drives is the product's real routing, not a test-local
    /// re-derivation of it — which is the whole point. A hand-rolled fold in a test asserts what the
    /// TEST does; the defect this hunts is the product's `MapKey`/`Update` doing something else. That
    /// is precisely how Rougue1 shipped "v1 complete" with 108 green tests while unplayable: the suite
    /// injected messages straight into `update`, so nothing ever exercised `mapKey -> host -> update`
    /// and the missing routing could not turn a test red.
    ///
    /// This drives the KEY runtime source only. `Tick` is the host's other source; a product whose
    /// script needs a clock folds `host.Tick dt` itself, or checks the wiring with `reachableMessages`.
    ///
    /// The effects are `dispatchKey`'s: a key `MapKey` HANDLES contributes `Update`'s batch, and one it
    /// does NOT contributes a single `DispatchInput(key, isDown)` marker (dispatchKey's "seen but
    /// unhandled" record — the live loop instead flips runtime state and repaints). Filtering paths like
    /// `audioRequests` ignore the marker; only an effect-list-EQUALITY assertion need account for it.
    let runKeyScriptToModel (host: GeneratedAppHost<'model, 'msg>) (script: ViewerKeyEvent list) : 'model * ViewerEffect list =
        let initModel, initEffects = host.Init()
        let mutable model = initModel
        let effects = ResizeArray<ViewerEffect>(initEffects)

        for raw in script do
            let model', requested = dispatchKey host raw model
            model <- model'
            effects.AddRange requested

        model, List.ofSeq effects

    /// Issue #911: the scene-host analogue of `ControlRenderResult.BoundIds` — "a key that is bound is
    /// a key that dispatches". Partition a declared input surface (`probe` — the events a product CLAIMS
    /// to bind) into the events that route through `MapKey` to a product message and the DEAD ones that
    /// route to none. A non-empty `Dead` is a bound key that dispatches nothing. Reflection-free, total,
    /// and pure: it consults only `MapKey` (through `normalizeEvent`, exactly as the live runtime does)
    /// and never runs `Update`.
    let auditKeyWiring (host: GeneratedAppHost<'model, 'msg>) (probe: ViewerKeyEvent list) : SceneHostKeyWiring<'msg> =
        let wired, dead =
            probe
            |> List.fold
                (fun (wired, dead) raw ->
                    let key, isDown = ViewerKeyboard.normalizeEvent raw

                    match host.MapKey key isDown with
                    | Some msg -> (raw, msg) :: wired, dead
                    | None -> wired, raw :: dead)
                ([], [])

        { Wired = List.rev wired
          Dead = List.rev dead }

    /// Issue #911: every product message this host's runtime SOURCES can produce over `probe`, in probe
    /// order — the raw material for the stronger "handled-but-unwired" check. A `game`-family product
    /// takes input through two runtime sources, `MapKey` and `Tick`; a `Msg` case handled in `Update`
    /// but produced by NEITHER is dispatched by nothing (the Rougue1 defect: `StartRun`/`Menu*`/
    /// `TogglePause`/`EnterRoom`/`DescendFloor` all handled, dispatched from no source). This package
    /// does not use reflection, so it cannot enumerate the product's `Msg` universe; instead — exactly
    /// as `BoundIds` hands back a set the product checks its render against, rather than a self-contained
    /// pass/fail — it returns what IS reachable, and the product asserts its handled universe is covered.
    ///
    /// Pass `tickSample = Some dt` to include the message `Tick dt` produces (if any). A `Msg` reachable
    /// ONLY through `Tick` is not unwired, so omit the sample and a clock message reads as a false
    /// positive.
    ///
    /// The result is positional (probe order), NOT a set: a message several probe keys produce appears
    /// several times. The intended `handledUniverse - reachable` check runs through a `Set`, so this is
    /// immaterial there; a caller counting the raw list should de-dup first.
    let reachableMessages
        (host: GeneratedAppHost<'model, 'msg>)
        (probe: ViewerKeyEvent list)
        (tickSample: TimeSpan option)
        : 'msg list =
        let fromKeys = (auditKeyWiring host probe).Wired |> List.map snd
        let fromTick = tickSample |> Option.bind host.Tick |> Option.toList
        fromKeys @ fromTick

    let audioRequests (effects: ViewerEffect list) : AudioEffect list =
        effects
        |> List.collect (function
            | PlayAudio batch -> batch
            | _ -> [])

    let smoke (host: GeneratedAppHost<'model, 'msg>) (request: ViewerRunRequest) =
        let model, _ = host.Init()
        // Issue #396: guard the one-shot product View. GeneratedAppHost sits outside module `Viewer`,
        // so it cannot reach the private `tryFirstProductView`; guard inline through the public #365
        // seam instead (message kept in sync). A throw is an App-stage ProductDefect, not a crash.
        match
            (try
                Result.Ok(host.View model)
             with ex ->
                 let diagnostic =
                     { Viewer.productDefectDiagnostic "View" ex.Message with
                         Message =
                             sprintf
                                 "Product View raised an exception (%s) producing its first frame; the run cannot start (App-stage product defect, not a render failure)."
                                 ex.Message }

                 Viewer.captureDiagnostic host.Diagnostics diagnostic |> ignore
                 Result.Error(Viewer.failureFromDiagnostic diagnostic))
        with
        | Result.Error failure -> Result.Error failure
        | Result.Ok scene ->
            // Issue #885: this is a LIVENESS smoke — it proves the product's `View` yields a first
            // frame without raising, so the readback frame is deliberately 1×1 and NO viewable pixels
            // are captured here. The frame size below is therefore not a canvas: it does not, and is
            // not meant to, hold the scene the product authored.
            //
            // The distinction #885 warns about only bites a path that captures pixels to LOOK at
            // (`captureScreenshotEvidence`, the `.png` evidence writers). There, the readback rasterizes
            // the scene 1:1 into an `InitialSize` frame with NO logical→physical mapping unless
            // `ViewerOptions.LogicalSize` is set — set, the host scales/centers/letterboxes the logical
            // canvas into the frame (#246, applied upstream via `ViewerLaunchSupport.presentedFor`);
            // unset (as here), content authored beyond `InitialSize` is clipped with no scale, no
            // letterbox, and no warning. A pixel-capture caller whose scene is larger than its frame
            // must pass a `LogicalSize`, or it captures only the frame-sized corner of its content.
            let size =
                match request.Target with
                | FirstFrame
                | FrameCount _ -> { Width = 1; Height = 1 }
                | Duration _ -> { Width = 1; Height = 1 }

            Viewer.runBounded request { Title = "Generated App"; InitialSize = size; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None; LogicalSize = None } scene

/// Feature 136 (R2/FR-001/FR-002): the rendering-edge text seam. Hosts install the bundled-font
/// real-metrics measurer once before building/laying out control scenes so box sizing equals draw
/// width (no clip), and read back the per-page fallback/tofu disclosure after a render (T017).
module Text =

    /// Feature 221 (US1, FR-001/FR-004): inject the headless CPU PNG rasterizer into the
    /// dependency-light `SceneEvidence.renderPng` seam so scene→PNG evidence yields real pixels with no
    /// GPU/GL/X/display. Idempotent/last-wins (mirrors the measurer seam). Wired into the text-seam
    /// installers below so every host that installs the measurer at startup also gains real headless
    /// pixels. The injected entry is re-entrant (serialized in `ReferenceRendering`), so concurrent
    /// `renderPng` calls stay isolated and deterministic.
    let installPngRasterizer () =
        SceneEvidence.setRealPngRasterizer (Some ReferenceRendering.renderScenePngResult)

    /// Clear the headless PNG rasterizer seam, restoring the typed `UnsupportedEnvironment` failure of
    /// the uninjected path (never a success-shaped stub). Used to assert honest failure (US3).
    let clearPngRasterizer () =
        SceneEvidence.setRealPngRasterizer None

    /// Install the bundled-font real-metrics measurer into the `Scene` measurement seam so control
    /// box sizing uses true advances. Idempotent; call once at host startup before layout.
    let installMeasurer () =
        Fonts.installMeasurementSeam ()
        installPngRasterizer ()

    /// Install the HarfBuzz-backed shaping provider and matching shaped measurement seam.
    let installShapingProvider () =
        installPngRasterizer ()
        Fonts.installShapingProvider ()

    /// Clear the shaping provider and use explicit fallback text measurement/render evidence.
    let clearShapingProvider () = Fonts.clearShapingProvider ()

    /// Read the active shaping provider state and diagnostics.
    let shapingProviderStatus () = Fonts.shapingProviderStatus ()

    /// Shape a text value through the active provider/fallback path for diagnostic readback.
    let shapeText text font = Fonts.shapeText text font

    /// Clear the text-fallback disclosure accumulator (the screenshot path also clears it per capture).
    let resetFallbackDisclosure () = SceneRenderer.resetFallbackEvents ()

    /// Aggregate disclosure (substituted/tofu counts + affected code points) for the most recent render.
    let fallbackReport () : Fonts.FallbackReport =
        SceneRenderer.fallbackEvents |> List.ofSeq |> Fonts.report

    /// Structured diagnostic lines for every non-authored character in the most recent render (FR-001).
    let fallbackDiagnostics () : string list =
        SceneRenderer.fallbackEvents |> List.ofSeq |> Fonts.diagnostics
