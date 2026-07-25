namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.Audio.Core
open FS.GG.UI.Canvas
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module Viewer =
    /// Public contract function exposed by this FS.GG.UI package.
    val timingPathToken: path: ViewerTimingPath -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val timingPathCanSupportClaim: path: ViewerTimingPath -> proofReadbackIncluded: bool -> validationReadbackIncluded: bool -> bool
    /// Feature 157: stable token for damage render decisions in readiness artifacts.
    val damageDecisionToken: decision: ViewerDamageDecision -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val init: options: ViewerOptions -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val update: msg: ViewerMsg -> model: ViewerModel -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initRun: request: ViewerRunRequest -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateRun: msg: ViewerRunMsg -> model: ViewerRunModel -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultDiagnostics: ViewerDiagnosticsOptions

    /// F1 (Feature 175 general repaint signal): the single "runtime-state changed → repaint" policy
    /// shared by every viewer loop. `internal` — exposed only so the regression can assert the policy
    /// deterministically (the loops are GL/timing-bound). Returns `current` when the input produced
    /// product messages (dispatch already re-derived) and `deriveScene ()` otherwise (runtime state may
    /// have changed with no model change — focus/hover/scroll — so re-derive on THIS input).
    val internal runtimeStateRepaint: producedMessages: bool -> current: 'scene -> deriveScene: (unit -> 'scene) -> 'scene

    /// Issue #429: the single `ViewerEffect` interpretation both persistent loops perform. It was two
    /// byte-identical folds, and they drifted — the interactive copy discarded `PlayAudio`, so a
    /// pointer-driven product got silence (#429). `internal` for the same reason as
    /// `runtimeStateRepaint`: the live loops are GL/timing-bound, so this is the seam that lets the
    /// regression assert the policy — notably that audio reaches the sink, and that evidence reaches
    /// `evidenceSink` rather than the floor (#444) — deterministically.
    /// Each loop passes in its own mutations; the returned flag is "this batch requested a close".
    /// #535 — the outcome dispatch, extracted so a test can reach it.
    ///
    /// `internal` for exactly the reason `interpretViewerEffects` is: `runAppWithPersistence` bails at
    /// `capability.PersistentWindow` long before it builds this wiring, so headless CI can never drive it
    /// through the public entry point. A test that re-implements the launch body's shape locally pins the
    /// author's MODEL of the code and not the code — which is how the seam's first ordering bug shipped with
    /// 350 green tests attached. Returns whether any dispatched message asked to close.
    val internal dispatchPersistenceBatch:
        sink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        dispatch: ('msg -> bool) ->
        batch: PersistenceEffect list ->
            bool

    val internal interpretViewerEffects:
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> unit) ->
        onScene: (SceneNode -> unit) ->
        onInputDispatch: (unit -> unit) ->
        onDiagnostic: (ViewerDiagnosticEvent -> unit) ->
        evidenceSink: (ViewerEffect -> unit) ->
        effects: ViewerEffect list ->
            bool

    /// Issue #1014: the effect fold with the live logical-canvas owner exposed as a deterministic
    /// sink for headless contract tests.
    val internal interpretViewerEffectsWithLogicalCanvas:
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> unit) ->
        onScene: (SceneNode -> unit) ->
        onInputDispatch: (unit -> unit) ->
        onDiagnostic: (ViewerDiagnosticEvent -> unit) ->
        evidenceSink: (ViewerEffect -> unit) ->
        logicalCanvasSink: (Size -> unit) ->
        effects: ViewerEffect list ->
            bool

    /// Issue #1022: the shared persistent interpreter with both live presentation owners exposed.
    /// `ApplyWindowOptions` reaches `windowBehaviorSink`; `ApplyLogicalCanvas` independently reaches
    /// `logicalCanvasSink`, preserving effect order and preventing either request from being dropped.
    val internal interpretViewerEffectsWithRuntimeWindow:
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> unit) ->
        onScene: (SceneNode -> unit) ->
        onInputDispatch: (unit -> unit) ->
        onDiagnostic: (ViewerDiagnosticEvent -> unit) ->
        evidenceSink: (ViewerEffect -> unit) ->
        windowBehaviorSink: (ViewerWindowBehaviorRequest -> unit) ->
        logicalCanvasSink: (Size -> unit) ->
        effects: ViewerEffect list ->
            bool

    /// Validate and translate a public runtime window request into the native loop-thread command.
    /// Unsupported modes/backends yield no plan and explicit Window diagnostics.
    val internal planRuntimeWindowBehavior:
        behavior: ViewerWindowBehaviorRequest ->
            Host.RuntimeWindowBehavior option * ViewerDiagnosticEvent list

    /// Issue #1014: the exact native-window -> framebuffer -> logical-product route used by the
    /// interactive loop, exposed internally so retained Controls activation can be proven headlessly.
    val internal pointerInProductSpace:
        logicalSize: Size option ->
        windowSize: Size ->
        surfaceSize: Size ->
        input: ViewerPointerInput ->
            ViewerPointerInput

    /// Issue #444: the evidence sink both persistent loops hand to `interpretViewerEffects`. Writes the
    /// four evidence effects a product can emit — `CaptureScreenshot`, `CaptureImageEvidence`,
    /// `WriteVisualEvidence`, `WriteRunEvidence` — which the fold used to discard silently: no file, no
    /// error, run reports success. A failed write is reported as an `Error`/`Screenshot`/`ArtifactWrite`
    /// diagnostic naming the effect, the path and the reason, so evidence never fails silently again;
    /// it never throws, because evidence I/O must not take a live render loop down with it.
    /// `internal` — the seam the #444 regression asserts on, for the same GL/timing reason as above.
    val internal productEvidenceSink:
        onDiagnostic: (ViewerDiagnosticEvent -> unit) ->
        sceneSize: (unit -> Size) ->
        currentScene: (unit -> SceneNode) ->
        effect: ViewerEffect ->
            unit

    /// S3 (Feature 175) live-trace read-back. `traceStartCapture` begins in-memory capture of
    /// `RenderLagTrace` events (focus/hover/scroll/dispatch/timing); `traceDrainCapture` stops and
    /// returns them as `(event, fields)` tuples; `traceEmit` records one event. Lets a test or tool
    /// observe live state programmatically — without the FS_GG_RENDER_LAG_TRACE env var and without a
    /// repack-to-instrument loop. `internal` — diagnostic seam, not a product contract.
    val internal traceStartCapture: unit -> unit
    val internal traceDrainCapture: unit -> (string * (string * string) list) list
    val internal traceEmit: eventName: string -> fields: (string * string) list -> unit
    /// Default readiness budget for responsiveness diagnostics.
    val defaultResponsivenessBudget: ViewerResponsivenessBudget
    /// Default disabled responsiveness options.
    val defaultResponsivenessOptions: ViewerResponsivenessOptions
    /// Stable JSON/readiness token for input kinds.
    val responsivenessInputKindToken: kind: ViewerResponsivenessInputKind -> string
    /// Stable JSON/readiness token for visible responses.
    val responsivenessVisibleResponseToken: response: ViewerResponsivenessVisibleResponse -> string
    /// Stable JSON/readiness token for environment statuses.
    val responsivenessEnvironmentStatusToken: status: ViewerResponsivenessEnvironmentStatus -> string
    /// Stable JSON/readiness token for summary readiness.
    val responsivenessReadinessToken: readiness: ViewerResponsivenessReadiness -> string
    /// Empty scheduler queue with the next sequence id starting at 1.
    val emptyInputQueue: ViewerInputQueue
    /// Queue depth visible to a newly received input.
    val inputQueueDepth: queue: ViewerInputQueue -> int
    /// Enqueue an input, assigning sequence id, priority lane, receipt depth, and coalescing state.
    val enqueueInput:
        receivedAt: DateTimeOffset ->
        inputKind: ViewerResponsivenessInputKind ->
        payload: string ->
        queue: ViewerInputQueue ->
            ViewerInputEnvelope * ViewerInputQueue
    /// Enqueue using the same explicit continuous-pointer policy as the interactive live host.
    val enqueueInputWithPointerPolicy:
        policy: ViewerContinuousPointerPolicy ->
        receivedAt: DateTimeOffset ->
        inputKind: ViewerResponsivenessInputKind ->
        payload: string ->
        queue: ViewerInputQueue ->
            ViewerInputEnvelope * ViewerInputQueue
    /// Drain pending inputs for one frame/update pass.
    val drainInputQueue: batchId: int64 -> drainReason: string -> queue: ViewerInputQueue -> ViewerFrameDrain * ViewerInputQueue
    /// Build the dirty-state decision from product/runtime/size/theme change facts.
    val dirtyState:
        productModelChanged: bool ->
        runtimeStateChanged: bool ->
        sizeChanged: bool ->
        themeChanged: bool ->
        dirtyRegion: ViewerResponsivenessDirtyRegion option ->
        reason: string list ->
            ViewerDirtyState
    /// True when the dirty-state requires retained-scene recomposition.
    val dirtyStateRequiresRecompose: dirty: ViewerDirtyState -> bool
    /// Create a stable-ish run id with the `resp-` prefix.
    val createResponsivenessRunId: unit -> string
    /// Encode one latency record as a JSONL line using stable lowercase tokens.
    val latencyRecordToJsonLine: latency: ViewerLatencyRecord -> string
    /// Summarize latency records into budget/readiness evidence.
    val summarizeResponsivenessRecords:
        runId: string ->
        scope: string ->
        recordsPath: string ->
        startedUtc: DateTimeOffset ->
        completedUtc: DateTimeOffset ->
        budget: ViewerResponsivenessBudget ->
        records: ViewerLatencyRecord list ->
            ViewerResponsivenessSummary
    /// Encode a responsiveness summary as machine-readable JSON.
    val responsivenessSummaryToJson: summary: ViewerResponsivenessSummary -> string
    /// Encode a responsiveness summary as reviewer-readable Markdown.
    val responsivenessSummaryToMarkdown: summary: ViewerResponsivenessSummary -> string
    /// Write records.jsonl, summary.json, summary.md, and environment.md under the output root/run id.
    val writeResponsivenessRun:
        outputRoot: string ->
        summary: ViewerResponsivenessSummary ->
        records: ViewerLatencyRecord list ->
            string list
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultWindowBehavior: ViewerWindowBehaviorRequest
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowBehavior: request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowLaunchBehavior: initialSize: Size -> request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowState: diagnostic: ViewerWindowStateDiagnostic -> ViewerLifecycleState
    /// Public contract function exposed by this FS.GG.UI package.
    val shouldCaptureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> bool
    /// Public contract function exposed by this FS.GG.UI package.
    val captureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> ViewerDiagnosticEvent option

    /// Issue #365: the `App`-stage diagnostic reported when a presented product's `Update`/`View`
    /// raises. `Error`-level, `Scene` category, `Stage = Some App` — deliberately NOT a `Frame`/render
    /// failure, because the fault is application code, not the draw. `phase` names the step.
    val productDefectDiagnostic: phase: string -> message: string -> ViewerDiagnosticEvent

    /// Issue #365: run a presented-host product `Update`/`View` step that must not kill the persistent
    /// window. On success, `Some` its result; on a product-raised exception, `None` plus exactly one
    /// `App`-stage diagnostic through `report`. The offending step is dropped (a product-code fault is
    /// deterministic, so it is not retried) and the window kept alive on its last-good state. This is
    /// the guard the live interactive loop performs, exposed so it can be driven directly.
    val tryProductStep:
        report: (ViewerDiagnosticEvent -> unit) -> phase: string -> step: (unit -> 'a) -> 'a option
    /// Public contract function exposed by this FS.GG.UI package.
    val failureFromDiagnostic: diagnostic: ViewerDiagnosticEvent -> ViewerRunFailure
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowObservation: outcome: ViewerLaunchOutcome -> inputs: WindowObservationInputs -> ViewerWindowObservationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val desktopSessionDiagnostic: unit -> ViewerDesktopSessionDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val runtimeCapability: unit -> ViewerRuntimeCapability
    /// Public contract function exposed by this FS.GG.UI package.
    val run: options: ViewerOptions -> scene: SceneNode -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #444 — EVIDENCE EFFECTS ARE HONORED HERE. A host that emits `CaptureScreenshot`,
    /// `CaptureImageEvidence`, `WriteVisualEvidence` or `WriteRunEvidence` from `Init`/`Update` gets the
    /// file written. Before #444 all four were discarded by the launch loop — no file, no error, and the
    /// run still reported success — which made a green "evidence collected" verdict unfalsifiable.
    ///
    /// What lands on disk. `CaptureScreenshot`/`CaptureImageEvidence` — and `WriteRunEvidence` when its
    /// path ends in `.png`, the same rule `runBounded` applies — rasterize the CURRENT SCENE offscreen
    /// through the shared CPU painter, so the image depicts the scene the product drew, NOT the presented
    /// GL framebuffer: read it as "the product rendered this", not as "the desktop showed this".
    /// `WriteRunEvidence` to any other path writes the textual run summary, and `WriteVisualEvidence`
    /// always serializes the artifact record it carries (rasterizing would discard that payload).
    ///
    /// A write that fails does not throw and does not take the window down; it raises an
    /// `Error`/`Screenshot`/`ArtifactWrite` diagnostic naming the effect, the path and the reason, so the
    /// failure is observable on `Diagnostics` rather than silent.
    val runApp: options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runApp` (including the Issue #444 evidence-effect handling), with an explicit window behavior.
    val runAppWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #245 — as `runApp`, but every `ViewerEffect.PlayAudio` batch the host emits is handed to
    /// `audioSink` in dispatch order instead of being discarded. This is the seam from a product's pure
    /// `update` to real playback: pass `FS.GG.Audio.Host.Audio.play backend` and a scaffolded game's
    /// sound requests reach the device with no edit to the durable `Program.fs`. Additive —
    /// `runApp`/`runAppWithWindowBehavior` stay intact and keep discarding audio (FR-006), and the viewer
    /// still owns no audio device: the backend's lifetime belongs to the caller.
    val runAppWithAudio: options: ViewerOptions -> audioSink: (AudioEffect list -> unit) -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #245 — `runAppWithAudio` with an explicit window behavior, completing the pairing that
    /// `runApp`/`runAppWithWindowBehavior` already have. The generated game template uses this when a
    /// `--window-*` flag is supplied and `runAppWithAudio` otherwise.
    val runAppWithWindowBehaviorAndAudio: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> audioSink: (AudioEffect list -> unit) -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #535 — the launch that gives a product's save/load requests somewhere to GO, and an answer
    /// to come BACK on.
    ///
    /// `persistenceSink` PERFORMS the batch: it is the only thing in the process that owns a save
    /// location, because the framework deliberately does not own the `SaveSlot` -> path mapping. Every
    /// `PersistenceOutcome` it returns is handed to `mapOutcome` and, if that yields a message, dispatched
    /// into `update` exactly as a key or a tick would be — so a `Load` is finally answerable. An outcome
    /// `mapOutcome` returns `None` for is dropped: the host has still answered, and inventing a message
    /// would be the framework deciding what "no save" means to a game.
    ///
    /// THE SINK RUNS ON THE WINDOW THREAD. It is called from the effect fold, which the tick and key
    /// handlers drive, so a slow save STALLS THE FRAME for as long as the write takes. Unlike the audio sink
    /// (fire-and-forget, `-> unit`), this one must return an answer, so it cannot be made async without a
    /// different seam. Keep it fast, or hand the work to your own worker and answer later with a message.
    ///
    /// DO NOT emit a persistence effect from the handler for a persistence outcome. The dispatch is
    /// SYNCHRONOUS RECURSION, not the Elmish dispatch queue: it recurses on one stack and terminates in a
    /// StackOverflowException, which .NET cannot catch — the process dies with no diagnostic.
    ///
    /// Before this existed, no `ViewerEffect` carried a `PersistenceEffect` at all: a product could request
    /// a save and no host could ever see it. `runApp` and `runAppWithAudio` still discard `Persist` — but
    /// they now say so on the diagnostics channel rather than dropping it in silence (#416).
    val runAppWithPersistence:
        options: ViewerOptions ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #535 — sound AND saves. Without this pairing, adopting persistence would mean giving up
    /// audio, which is the kind of forced choice that gets a seam worked around instead of used.
    val runAppWithAudioAndPersistence:
        options: ViewerOptions ->
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #979 — window behavior AND sound AND saves, the last corner of the generated-app launcher
    /// matrix. `runAppWithWindowBehaviorAndAudio` (audio, no saves) and `runAppWithAudioAndPersistence`
    /// (saves, but `defaultWindowBehavior`) each drop one capability the other keeps, so a `--window-*`
    /// launch that also needs durable saves had no path and silently lost one on whichever branch it took.
    /// This threads all four — `behavior`, `audioSink`, `persistenceSink`, `mapOutcome` — through the same
    /// generated-app launch as its siblings; the audio and persistence sinks behave exactly as they do in
    /// `runAppWithAudioAndPersistence` (see there for the sink contract). Additive: the existing overloads
    /// stay intact and keep their defaults.
    val runAppWithWindowBehaviorAndAudioAndPersistence:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Feature 085 — pointer-aware, size-aware durable launch. Routes native pointer events
    /// and window resizes to the host and renders the size-aware `View`; additive to
    /// `runApp`/`runAppWithWindowBehavior`, which stay intact (FR-004/FR-006/FR-009).
    val runInteractiveViewer: options: ViewerOptions -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewer` with an explicit window behavior.
    val runInteractiveViewerWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Default retained pointer policy: latest `Moved` sample per presented-frame boundary; discrete
    /// events are lossless. The callback is `ignore`.
    val defaultPointerPacingOptions: ViewerPointerPacingOptions
    /// `runInteractiveViewer` with an explicit continuous-pointer policy and live pacing counters.
    val runInteractiveViewerWithPointerPacing:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Pointer pacing plus explicit window behavior on the same retained launch path.
    val runInteractiveViewerWithWindowBehaviorAndPointerPacing:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        pointerPacing: ViewerPointerPacingOptions ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Launch `host` in the live persistent viewer, deliver a bounded script through the viewer input queue,
    /// wait for the final scripted response to present, then close.
    val runInteractiveViewerScript:
        options: ViewerOptions ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewerScript` with an explicit window behavior.
    val runInteractiveViewerScriptWithWindowBehavior:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    val runInteractiveViewerScriptWithPointerPacing:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #429 — `runInteractiveViewer` with an audio sink, so the pointer/size-aware host family
    /// can request sound. Before this, audio was reachable only through `runAppWithAudio`, whose
    /// `GeneratedAppHost` has no pointer: a product that needed both got silence, because the
    /// interactive loop discarded `PlayAudio`. `audioSink` receives every batch in dispatch order,
    /// exactly as it does under `runAppWithAudio`; `runInteractiveViewer` (no sink) is unchanged.
    val runInteractiveViewerWithAudio:
        options: ViewerOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #429 — `runInteractiveViewerWithAudio` with an explicit window behavior, completing the
    /// pairing the sinkless interactive runners already have.
    val runInteractiveViewerWithWindowBehaviorAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Pointer pacing and audio share the same interactive launch fold.
    val runInteractiveViewerWithPointerPacingAndAudio:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Full interactive launch: explicit window behavior, pointer pacing metrics, and audio.
    val runInteractiveViewerWithWindowBehaviorAndPointerPacingAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        pointerPacing: ViewerPointerPacingOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #438 — `runInteractiveViewerScript` with an audio sink. #429 gave the interactive family a
    /// sink but only on its NON-scripted entry points; the scripted runners kept passing `ignore`, so a
    /// scripted product's `PlayAudio` was still dropped with no error and no diagnostic. That mattered
    /// more than the count of entry points suggests: the scripted runners are what the evidence and
    /// responsiveness tooling drives, so "audio was requested during a scripted run" was the one thing
    /// about sound that could not be observed. `audioSink` receives every batch in dispatch order, from
    /// the same shared fold the live loops use; `runInteractiveViewerScript` (no sink) is unchanged.
    val runInteractiveViewerScriptWithAudio:
        options: ViewerOptions ->
        script: ViewerScriptInput list ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #438 — `runInteractiveViewerScriptWithAudio` with an explicit window behavior, completing
    /// the pairing the sinkless scripted runners already have.
    val runInteractiveViewerScriptWithWindowBehaviorAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        script: ViewerScriptInput list ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppEvidence: request: ViewerRunRequest -> options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Drives a real bounded Silk.NET window and reports `FramesRendered` = the number of frame
    /// callbacks the window fired. The window itself is NOT painted with `scene` (on-screen
    /// presentation is `run`/`runApp`); instead, when the request's `EvidencePath` names a `.png`
    /// the scene is rasterized to real pixels through the shared CPU painter, so image evidence
    /// genuinely depicts `scene`. Read `FramesRendered` as window/frame-cadence proof, not as
    /// "the scene was presented on screen" (P6 / R4).
    val runBounded: request: ViewerRunRequest -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Bounded run stopping at the first frame callback; see `runBounded` for what the evidence
    /// proves (window/frame cadence; scene depicted only in `.png` evidence, not on the live surface).
    val runUntilFirstFrame: options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Bounded run stopping after `frameCount` frame callbacks; see `runBounded` for what the
    /// evidence proves (window/frame cadence; scene depicted only in `.png` evidence).
    val runForFrames: frameCount: int -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val captureScreenshotEvidence: request: ScreenshotEvidenceRequest -> options: ViewerOptions -> scene: SceneNode -> ScreenshotEvidenceResult
    /// Public contract function exposed by this FS.GG.UI package.
    val initEvidenceWorkflow: request: ScreenshotEvidenceRequest -> EvidenceWorkflowModel * EvidenceWorkflowEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateEvidenceWorkflow: msg: EvidenceWorkflowMsg -> model: EvidenceWorkflowModel -> EvidenceWorkflowModel * EvidenceWorkflowEffect list

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedAppHost =
    /// Public contract function exposed by this FS.GG.UI package.
    val dispatchKey: host: GeneratedAppHost<'model,'msg> -> raw: ViewerKeyEvent -> model: 'model -> 'model * ViewerEffect list
    /// Issue #911: the scene-host analogue of `ControlsElmish.Perf.runScriptToModel` (#461). Fold an
    /// ordered key-event script through the host's own `dispatchKey` (`normalizeEvent -> MapKey ->
    /// Update`) from `Init`'s model to the FINAL model, returning it with every `ViewerEffect` requested
    /// in order (`Init`'s batch first). No window, no device, no GL — it lets a `game`-family product,
    /// which routes input through this scene-host rather than the Controls click path, write an
    /// end-to-end "played through the host" test purely and headlessly. It drives the KEY source only;
    /// `Tick` is folded by the caller, or checked via `reachableMessages`.
    val runKeyScriptToModel: host: GeneratedAppHost<'model,'msg> -> script: ViewerKeyEvent list -> 'model * ViewerEffect list
    /// Issue #911: the scene-host analogue of `ControlRenderResult.BoundIds` — "a key that is bound is a
    /// key that dispatches". Partition a declared input surface into the events that route through
    /// `MapKey` to a product message (`Wired`) and the DEAD ones that route to none (`Dead`). A non-empty
    /// `Dead` over a product's declared keymap is a bound key that dispatches nothing. Reflection-free,
    /// total, pure — consults only `MapKey` (via `normalizeEvent`, as the live runtime does), never `Update`.
    val auditKeyWiring: host: GeneratedAppHost<'model,'msg> -> probe: ViewerKeyEvent list -> SceneHostKeyWiring<'msg>
    /// Issue #911: every product message the host's runtime SOURCES (`MapKey` over `probe`, and `Tick`
    /// when `tickSample` is `Some dt`) can produce, in probe order — the raw material for the stronger
    /// "handled-but-unwired" check. A `Msg` case handled in `Update` but absent from this list is
    /// dispatched by no source (the Rougue1 defect). This package uses no reflection, so it returns what
    /// IS reachable — as `BoundIds` returns a set the product checks against — and the product asserts its
    /// handled `Msg` universe is covered.
    val reachableMessages: host: GeneratedAppHost<'model,'msg> -> probe: ViewerKeyEvent list -> tickSample: TimeSpan option -> 'msg list
    /// Issue #245 — every sound request in an effect batch, flattened in dispatch order; non-audio
    /// effects are dropped. This is exactly what `runAppWithAudio` feeds its sink, exposed as a pure
    /// function so a product can assert what a frame requested without opening a window or a device:
    /// `dispatchKey host raw model |> snd |> audioRequests |> Audio.interpret` yields `AudioEvidence`.
    val audioRequests: effects: ViewerEffect list -> AudioEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val smoke: host: GeneratedAppHost<'model,'msg> -> request: ViewerRunRequest -> Result<ViewerRunEvidence, ViewerRunFailure>

/// Feature 136 (R2/FR-001/FR-002): the rendering-edge text seam — install the bundled-font
/// real-metrics measurer (so control box sizing equals draw width) and read back per-page text
/// fallback/tofu disclosure after a render.
module Text =
    /// Feature 221 (US1): inject the headless CPU PNG rasterizer into `SceneEvidence.renderPng` so
    /// scene→PNG evidence yields real pixels with no GPU/GL/display. Idempotent; wired into the
    /// measurer/shaping installers so host startup gains the headless PNG path automatically.
    val installPngRasterizer: unit -> unit
    /// Clear the headless PNG rasterizer seam, restoring the typed `UnsupportedEnvironment` failure
    /// (no success-shaped stub). Used to assert honest failure (US3).
    val clearPngRasterizer: unit -> unit
    /// Install the bundled-font real-metrics measurer into the `Scene` measurement seam. Idempotent;
    /// call once at host startup before laying out control scenes.
    val installMeasurer: unit -> unit
    /// Install the HarfBuzz-backed shaping provider and matching shaped measurement seam.
    val installShapingProvider: unit -> Fonts.TextShapingProviderStatus
    /// Clear the shaping provider and use explicit fallback text measurement/render evidence.
    val clearShapingProvider: unit -> Fonts.TextShapingProviderStatus
    /// Read the active shaping provider state and diagnostics.
    val shapingProviderStatus: unit -> Fonts.TextShapingProviderStatus
    /// Shape a text value through the active provider/fallback path for diagnostic readback.
    val shapeText: text: string -> font: FontSpec -> ShapedTextResult
    /// Clear the text-fallback disclosure accumulator (the screenshot path also clears it per capture).
    val resetFallbackDisclosure: unit -> unit
    /// Aggregate disclosure (substituted/tofu counts + affected code points) for the most recent render.
    val fallbackReport: unit -> Fonts.FallbackReport
    /// Structured diagnostic lines for every non-authored character in the most recent render (FR-001).
    val fallbackDiagnostics: unit -> string list
