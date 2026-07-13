// See skill: fs-gg-elmish
namespace FS.GG.UI.Controls.Elmish

open System
open FS.GG.Audio.Core
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Elmish
open FS.GG.UI.DesignSystem

/// Public contract type exposed by this FS.GG.UI package.
type AdapterDiagnostic =
    { Code: string
      Message: string
      Source: string }

/// Public contract type exposed by this FS.GG.UI package.
type AdapterEffect<'msg> =
    | DispatchProductMessage of 'msg
    | DispatchControlRuntimeMessage of ControlRuntimeMsg
    | DispatchKeyboardMessage of KeyboardMsg
    | DispatchHostCommand of string
    | ReportAdapterDiagnostic of AdapterDiagnostic

/// Public contract type exposed by this FS.GG.UI package.
type AdapterCommand<'msg> = AdapterEffect<'msg> list

/// Public contract type exposed by this FS.GG.UI package.
type AdapterSubscription<'msg> =
    { Id: string
      Subscribe: unit -> AdapterCommand<'msg> }

/// Public contract type exposed by this FS.GG.UI package.
type AdapterProgram<'model, 'msg> =
    { Init: unit -> 'model * AdapterCommand<'msg>
      Update: 'msg -> 'model -> 'model * AdapterCommand<'msg>
      View: 'model -> Control<'msg>
      Subscriptions: 'model -> AdapterSubscription<'msg> list }

[<RequireQualifiedAccess>]
/// Feature 111 (US1, FR-001): the closed TRIGGER taxonomy naming WHY a frame ran. The scheduler
/// classifies each produced frame from the input that caused it and runs only the phases that cause
/// requires (`FrameMetrics.ViewCalled`/`DiffRan`/`LayoutRan`/`PaintRan`). `RequireQualifiedAccess` —
/// the case names `Key`/`Tick`/`Idle` would otherwise shadow a consumer's own `Msg` cases when it
/// `open`s this namespace, so they must be qualified (`FrameCause.Tick` etc.), exactly as `FrameInput`
/// requires. `Resize`/`Theme` are live-scheduler causes (a window resize / theme switch between
/// paints); the deterministic `Perf.runScript` corpus produces only `Idle`/`PointerMove`/
/// `PointerDiscrete`/`Key`/`Tick` (a model-driven theme change is a `Key` frame with the theme changed
/// as an effect, not a `Theme` cause).
type FrameCause =
    | Idle
    | PointerMove
    | PointerDiscrete
    | Key
    | Tick
    | Resize
    | Theme

/// Feature 108/109/110/111 (US1, FR-001/002): the per-frame structured work/timing signal the host
/// loop and the deterministic `Perf.runScript` driver both produce. The count/bool fields are the
/// byte-stable determinism surface (FR-007/SC-005); `FrameDuration` is reported for real perf
/// observation but EXCLUDED from golden assertions (it varies run to run, FR-012). Feature 109
/// replaced the conflating `ViewRebuilt` with the two precise booleans `ProductModelChanged` +
/// `ViewCalled` and added the integer `FullRenderCount`. Feature 110 added `FullRenderFallbackCount`
/// and narrowed `FullRenderCount`/`ViewCalled` so retained routing increments NEITHER. Feature 111
/// added `FrameCause` + the per-phase booleans `DiffRan`/`LayoutRan`/`PaintRan` (the VIEW phase is
/// `ViewCalled`) and narrowed `ViewCalled`/`FullRenderCount` to `false`/`0` on a model-unchanged frame
/// (the scheduler reuses the already-produced view tree, FR-003/FR-011).
type FrameMetrics =
    { /// A product message actually changed the model this frame (the reference identity of the folded
      /// model changed across `host.Update`). `false` for a no-message frame, a pure hover/focus
      /// frame, and an animation-only tick (FR-001/003/005).
      ProductModelChanged: bool
      /// THE VIEW PHASE: `host.View size model` actually ran this frame to (re)produce a tree. Feature
      /// 111 narrows this — it is `false` on a model-unchanged frame (including an animation-only tick,
      /// which formerly reported `true`) because the scheduler reuses the already-produced view tree and
      /// skips `host.View` (FR-003/FR-011); the overlay/paint fact moves to `PaintRan`. Still equals
      /// `FullRenderCount > 0`. Feature 110: retained pointer routing does not set it true either.
      ViewCalled: bool
      /// Number of full `host.View` + `Control.renderTree` materializations this frame performed — the
      /// retained-step render where it occurs, plus any oracle fallback render. Feature 110 narrowed
      /// this: routing a pointer event via the retained path increments NEITHER this nor `ViewCalled`
      /// (the per-sample routing full render is removed from the hot path, FR-008); a model-driven
      /// re-render after a dispatched message still counts.
      FullRenderCount: int
      /// Nodes re-measured this frame (from `WorkReductionRecord.RemeasuredNodeCount`); 0 on an idle
      /// frame, bounded (overlay-assembly, not whole-tree) on an animation-only frame.
      RemeasuredNodeCount: int
      /// Feature 113 (Phase 5, FR-009/FR-010): memoized-projection HITS while building this frame — a
      /// memoizable control (the DataGrid row/column projection) whose declared dependency was
      /// unchanged and whose previously-lowered subtree was reused without recomputing. `0` on an idle
      /// frame or any frame that evaluates no memoizable control. Deterministic, golden-asserted via
      /// `Perf.runScript`.
      MemoHitCount: int
      /// Feature 113 (Phase 5, FR-009/FR-010): memoized-projection MISSES while building this frame — a
      /// memoizable control whose dependency changed, or a cold first evaluation, so the projection was
      /// recomputed and stored. `0` on an idle frame or any frame that evaluates no memoizable control.
      /// Deterministic, golden-asserted via `Perf.runScript`.
      MemoMissCount: int
      /// Feature 114 (Phase 6, FR-013): the number of repeated-control row items actually MATERIALIZED
      /// this frame — the count of `data-grid-row` nodes the virtualized control(s) realized. Bounded by
      /// `visibleCount + 2 * overscan` and does NOT scale with the total logical row count: a 100-, 1000-,
      /// and 10000-row grid with the same viewport + overscan all report the same materialized count.
      /// `0` on a frame that evaluates no virtualized control; aggregates across virtualized controls.
      /// Deterministic, golden-asserted via `Perf.runScript`.
      VirtualItemsMaterialized: int
      /// Feature 114 (Phase 6, FR-013): the total LOGICAL item count the virtualized control(s) represent
      /// this frame (the sum of each `data-grid`'s logical `Total`). Equals `VirtualItemsMaterialized` only
      /// when the whole collection fits the realized window; otherwise it scales with the data while
      /// `VirtualItemsMaterialized` stays bounded. `0` on a frame with no virtualized control. Deterministic,
      /// golden-asserted via `Perf.runScript`.
      VirtualItemsTotal: int
      /// Feature 116 (Phase 7, FR-001/FR-002, US1): the number of nodes whose paint was REPAINTED this
      /// frame — the damage set: the changed node(s) plus any genuinely-shifted nodes. A localized
      /// visual-state change reports a small count (the changed control + its immediate shifted
      /// neighbours, `<= 4` for a leaf hover, `< TotalNodeCount`); a theme switch that invalidates all
      /// paint reports every node; an idle frame reports `0`. Deterministic, golden-asserted via
      /// `Perf.runScript`.
      RepaintedNodeCount: int
      /// Feature 116 (Phase 7, FR-001/FR-004, US1): the number of DISTINCT axis-aligned damage rectangles
      /// this frame — one per repainted node's evaluated box, identical boxes deduplicated (`None` boxes
      /// contribute none), so `<= RepaintedNodeCount`. `0` on an idle frame. Deterministic integer,
      /// golden-asserted via `Perf.runScript`.
      DirtyRectCount: int
      /// Feature 116 (Phase 7, FR-001/FR-004, US1); Feature 120 (FR-015) corrected the computation: the
      /// integer area of the **union** of distinct damage rectangles this frame (no longer the sum of their
      /// areas), so overlapping damage is counted once and the value never exceeds the frame area. A
      /// localized change covers only the changed box(es) (`< FrameArea`); a theme switch covers the frame;
      /// an idle frame reports `0`. Deterministic integer, golden-asserted via `Perf.runScript`.
      DirtyArea: int
      /// Feature 116 (Phase 7, FR-005/FR-007, US2): picture-cache HITS this frame — cacheable boundaries
      /// (a `data-grid-row` identity) whose full correctness key was unchanged and whose cached picture was
      /// still resident, reused without recomputing. `0` on a frame with no cacheable picture or under the
      /// always-miss oracle. Deterministic, golden-asserted via `Perf.runScript`.
      PictureCacheHitCount: int
      /// Feature 116 (Phase 7, FR-006/FR-010, US2/US3): picture-cache MISSES this frame — a cacheable
      /// boundary recomputed because its correctness key changed, the identity was cold, or its entry had
      /// been evicted. `0` on a frame with no cacheable picture. Deterministic, golden-asserted via
      /// `Perf.runScript`.
      PictureCacheMissCount: int
      /// Feature 116 (Phase 7, FR-009, US3): the live bounded-LRU picture-cache entry count after this
      /// frame — `<= PictureCacheCap` at all times, even under eviction pressure (more distinct cacheable
      /// pictures than the cap). A steady cache may retain entries across an idle frame, so this reflects
      /// live size, not necessarily `0`. Deterministic, golden-asserted via `Perf.runScript`.
      PictureCacheEntryCount: int
      /// Feature 117/138: text-measure cache HITS this frame — measurements `(text, font)` whose key was
      /// resident before this frame's measurement window began, reused without re-invoking
      /// `Scene.measureText`. Same-frame duplicate text may reuse the cache internally, but is not reported
      /// as a hit. `0` on a frame that measures no text or under the always-miss oracle. A warm text-heavy
      /// frame whose text inputs did not change reports `> 0`. Deterministic, golden-asserted via
      /// `Perf.runScript`.
      TextMeasureCacheHitCount: int
      /// Feature 117 (Phase 8, FR-001/FR-005, US1): text-measure cache MISSES this frame — measurements
      /// whose key was not resident before the frame and therefore required a fresh measurement. `0` on a
      /// frame that measures no text; `> 0` on a cold frame and on a style-only frame only if new text
      /// appeared. Deterministic, golden-asserted via `Perf.runScript`.
      TextMeasureCacheMissCount: int
      /// Feature 117 (Phase 8, FR-006, US2): the size of the layout dirty set fed into incremental layout
      /// this frame (the patch-derived self-dirty nodes BEFORE fixed-size-ancestor propagation). Distinct
      /// from `RemeasuredNodeCount` (the POST-pinning set actually re-measured); because propagation expands
      /// each dirty node to its first fixed-size ancestor's whole subtree, `LayoutInvalidatedNodeCount <=
      /// RemeasuredNodeCount`. `0` on an idle / style-only / visual-state-only frame; bounded and explainable
      /// on a geometry frame. Deterministic, golden-asserted via `Perf.runScript`.
      LayoutInvalidatedNodeCount: int
      /// Raw pointer samples that arrived this frame, including deferred/queued moves carried from a
      /// prior boundary (K before coalescing) (FR-008).
      PointerSamplesReceived: int
      /// Pointer MOVES actually applied after coalescing — at most one per frame (FR-009/SC-002).
      PointerMovesProcessed: int
      /// Feature 110 (FR-009): how many times retained pointer routing fell back to a full render to
      /// route an event this frame. `0` for every normal scripted pointer scenario (SC-005); non-zero
      /// only when the retained frame could not resolve a bindable hit and the preserved full-render
      /// oracle had to run (a counted correctness escape hatch, never the normal path). Deterministic,
      /// golden-asserted.
      FullRenderFallbackCount: int
      /// Feature 111 (FR-001): the trigger that caused this frame (idle / pointer-move / pointer-discrete
      /// / key / tick / resize / theme). Deterministic, golden-asserted. Names the trigger, not the
      /// effect — a key that changes the model is `FrameCause.Key` with `ProductModelChanged = true`.
      FrameCause: FrameCause
      /// Feature 111 (FR-002): the DIFF/reconcile phase ran — a newly-produced view tree was reconciled
      /// against the retained tree this frame (the retained step ran on a fresh `host.View`). An
      /// animation-only tick re-samples the overlay WITHOUT producing a new tree, so it reports `false`.
      DiffRan: bool
      /// Feature 111 (FR-002): the LAYOUT phase ran — at least one node was re-measured this frame
      /// (equivalent to `RemeasuredNodeCount > 0`, but set explicitly as part of the phase record).
      LayoutRan: bool
      /// Feature 111 (FR-002): the PAINT phase ran — the painted scene (a model render) or the animation
      /// overlay was (re)assembled this frame. `true` on model frames AND animation-only ticks; `false`
      /// on idle and pure routing frames. (Hit-test is intentionally NOT a phase field — clarified
      /// 2026-06-12: the deterministic path does not hit-test coalesced moves; routing work stays in
      /// `PointerSamplesReceived`/`PointerMovesProcessed`/`FullRenderFallbackCount`.)
      PaintRan: bool
      /// Wall-clock duration of the frame's work — reported, EXCLUDED from the golden/determinism
      /// surface (FR-012).
      FrameDuration: TimeSpan
      /// Feature 120 (US1, FR-001/FR-002): scene→canvas paint-walk time. Live diagnostic only — EXCLUDED
      /// from count goldens (mirrors `FrameDuration`); `TimeSpan.Zero` on the deterministic `Perf.runScript`
      /// path so adding it leaves every golden byte-identical (SC-001).
      PaintDuration: TimeSpan
      /// Feature 120 (US1, FR-001/FR-002): flush + buffer-swap present/compose time. Live diagnostic only;
      /// non-golden; `TimeSpan.Zero` on the deterministic path.
      ComposeDuration: TimeSpan
      /// Feature 120 (US3, FR-014): replay HITS this frame — `CachedSubtree` boundaries whose recorded
      /// picture was resident and whose fingerprint matched, so the recorded draw commands were replayed
      /// instead of re-walked. `0` on a frame with no cacheable boundary or under the replay-disable oracle.
      /// Deterministic, golden-asserted via `Perf.runScript`.
      ReplayHitCount: int
      /// Feature 120 (US3, FR-014): replay MISSES this frame — boundaries (re)recorded because the identity
      /// was cold, its fingerprint changed, or its entry had been evicted. `0` on a frame with no cacheable
      /// boundary. Deterministic, golden-asserted.
      ReplayMissCount: int
      /// Feature 120 (US3, FR-014): pictures recorded this frame (one per miss). Deterministic, golden-asserted.
      ReplayRecordCount: int
      /// Feature 120 (US3, FR-014/SC-004): subtree paint-nodes skipped by replay this frame — the summed
      /// node count of every replayed (hit) boundary's recorded subtree, i.e. the draw-call walk avoided.
      /// The work-reduction signal. `0` on a frame with no replay hit. Deterministic, golden-asserted.
      ReplaySkippedNodeCount: int
      /// Feature 120 (US3, FR-013): native bytes held by the replay cache after this frame — a deterministic
      /// model estimate (resident recorded-picture subtree node counts), bounded by the cap so a memory
      /// regression is observable. Deterministic, golden-asserted. The live backend additionally reports its
      /// real `SKPicture` native byte total in the non-golden timing baseline.
      ReplayCacheNativeBytes: int }

/// Feature 147: derived compositor diagnostics over the existing per-frame metrics. This keeps
/// `FrameMetrics` source-compatible while giving readiness reviewers named damage, fallback,
/// promotion/reuse, and snapshot-budget fields.
type CompositorFrameDiagnostics =
    { ProofStatus: string
      DamageUnionArea: int
      ScissorCandidateArea: int
      FallbackReason: string option
      PromotionDecisionCount: int
      ReuseHitCount: int
      ReuseMissCount: int
      DemotionCount: int
      SnapshotResourceBytes: int }

/// Feature 150: deterministic layout/intrinsic work projection for Controls.Elmish consumers.
type LayoutWorkMetrics =
    { LayoutWorkCount: int
      IntrinsicQueryWorkCount: int
      IntrinsicCacheHitCount: int
      IntrinsicCacheMissCount: int
      IntrinsicInvalidationCount: int }

/// Feature 167: adapter contribution to one responsiveness latency record.
type ResponsivenessTimingContribution =
    { RoutingDuration: TimeSpan
      UpdateDuration: TimeSpan
      RetainedStepDuration: TimeSpan
      LayoutDuration: TimeSpan
      TextDuration: TimeSpan
      ProductMessageCount: int
      ProductModelChanged: bool
      RuntimeStateChanged: bool
      NoVisibleResponseReason: string option }

/// Feature 167: deterministic compatibility verdict when diagnostics are disabled.
type DiagnosticsDisabledCompatibility =
    { FrameMetricsUnchanged: bool
      RecordsWritten: int
      ClockFreePerfScript: bool }

[<RequireQualifiedAccess>]
/// Feature 108 (US3, FR-009): one ordered step of the deterministic perf driver. `Key` carries the
/// parsed base key + held modifiers; `Pointer` carries an already-resolved `PointerInteraction`;
/// `Tick` advances animation clocks by an injected delta; `Idle` is a no-input frame.
/// `RequireQualifiedAccess` — the generic case names (`Key`/`Pointer`/`Tick`/`Idle`) would otherwise
/// shadow a consumer's own `Msg` cases when it `open`s this namespace, so they must be qualified
/// (`FrameInput.Tick` etc.).
type FrameInput<'msg> =
    | Key of ViewerKey * KeyModifiers
    | Pointer of PointerInteraction
    | Tick of TimeSpan
    | Idle

/// Result of a bounded live script delivered through the GL-backed interactive viewer.
type LiveScriptRunResult =
    { Outcome: ViewerLaunchOutcome
      Metrics: FrameMetrics list }

/// Pointer-routing, size-aware durable host (feature 085, research D3-AMEND). Mirrors
/// `GeneratedAppHost` field-for-field PLUS a `MapPointer` seam over `PointerInteraction` and a
/// size-carrying `View` that returns a `Control<'msg>` tree (so `Control.renderTree` yields the
/// `Scene` + `Layout` + `EventBindings` the host routes). Lives in Controls.Elmish — not SkiaViewer —
/// because `PointerInteraction`/`interpretPointerOutcome` are Controls surface and the viewer is
/// host-independent. `Theme` drives `renderTree`. Feature 090: a hit control's authored
/// `EventBindings` (`onClick`/`onChanged`) are dispatched in the live window; `MapKey` gains a
/// focus-aware text-routing seam for the focused text control (see `routeInteractivePointer`,
/// `routeFocusedText`, and `runInteractiveApp`). Feature 108: the additive `MapKeyChord` /
/// `OnFrameMetrics` fields carry inert defaults (at-rest byte-identical).
type InteractiveAppHost<'model, 'msg> =
    { Init: unit -> 'model * ViewerEffect list
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: Size -> 'model -> Control<'msg>
      Theme: Theme
      MapKey: ViewerKey -> bool -> 'msg option
      MapPointer: PointerInteraction -> 'msg option
      Tick: TimeSpan -> 'msg option
      /// Feature 108 (US5, FR-016): an additive modifier-aware key seam consulted BEFORE `MapKey`.
      /// The default (`fun _ _ -> None`) ignores modifiers and defers to `MapKey`, so unmodified
      /// keys route exactly as today (at-rest byte-identical, SC-012).
      MapKeyChord: ViewerKey -> KeyModifiers -> 'msg option
      /// Feature 108 (US2, FR-006): an additive opt-in observability sink called once per frame with
      /// that frame's `FrameMetrics`. The default (`ignore`) is inert, so a host that does not
      /// observe metrics is byte-identical to its pre-108 behaviour (SC-012).
      OnFrameMetrics: FrameMetrics -> unit
      Diagnostics: ViewerDiagnosticsOptions }

/// Verdict of a responds-proof (feature 090, FR-006): `Responsive` when a real input applied to the
/// running host produced a visible change in the rendered output (`Before` ≠ `After`), `Inert` when
/// it did not. An inert host (renders but does not respond) can only yield `Inert`.
type RespondsVerdict =
    | Responsive
    | Inert

/// A captured input→visible-change responds-proof (feature 090, FR-006/FR-007): the `Before` frame,
/// the `After` frame produced by applying a real dispatched interaction (route → `host.Update` fold →
/// re-render, exactly as the live repaint loop), and the `Verdict`. A distinct evidence class from a
/// render-only screenshot (one frame, no interaction) and from the offscreen `runInteractivePointerOnce`
/// route probe (model layer only): an app that renders but does not respond yields identical frames and
/// an `Inert` verdict, so "renders" cannot be passed off as "responds".
type RespondsProof =
    { Before: Scene
      After: Scene
      Verdict: RespondsVerdict }

/// Pure, total bridge between the adapter's effect-list command model
/// (`AdapterCommand<'msg>`) and Elmish `Cmd<'msg>` (068, additive).
module AdapterCmd =
    /// The Elmish no-op command (= `Cmd.none`). Law: `toCmd route [] = none`.
    val none: Cmd<'msg>
    /// Lift a single product message into an `AdapterCommand`
    /// (= `[ DispatchProductMessage msg ]`). Law: `productMessages (ofMessage m) = [ m ]`.
    val ofMessage: msg: 'msg -> AdapterCommand<'msg>
    /// The ordered `DispatchProductMessage` payloads carried by the command
    /// (the round-trip oracle); no other effect case contributes. NOTE that this DISCARDS
    /// every other effect, diagnostics included — a routing site that extracts messages with
    /// it and nothing else silently drops whatever the interpreter reported (issue #457).
    /// Pair it with `diagnostics`, as the pointer routing sites now do.
    val productMessages: command: AdapterCommand<'msg> -> 'msg list
    /// Issue #457: the ordered `ReportAdapterDiagnostic` payloads carried by the command —
    /// the companion `productMessages` never had, so a host can route what an interpreter
    /// reported (a pointer hit-test miss, a stale target, an unresolved control id) to an
    /// observer instead of filtering it out. Law: `diagnostics (ofMessage m) = []`.
    val diagnostics: command: AdapterCommand<'msg> -> AdapterDiagnostic list
    /// Total conversion to an Elmish `Cmd<'msg>`: `route` maps EVERY `AdapterEffect`
    /// case (product and non-product) to a `'msg`, preserving list order; `[]` ->
    /// `Cmd.none`. Pure to construct; never throws. FR-003/FR-008.
    val toCmd: route: (AdapterEffect<'msg> -> 'msg) -> command: AdapterCommand<'msg> -> Cmd<'msg>

/// Public contract module exposed by this FS.GG.UI package.
module ControlsElmish =

    /// Lower one `KeyboardEffect` to an `AdapterCommand`. `CommandResolved` becomes a product message;
    /// the state-echo cases (`KeyStateChanged`, `LayoutChanged`, `ModeChanged`, `PendingSequenceChanged`,
    /// `StateDisplayChanged`) carry no host action and yield `[]`; `ReportKeyboardDiagnostic` becomes a
    /// `ReportAdapterDiagnostic`.
    ///
    /// Issue #456 (epic FS-GG/.github#416): `RequestHostKeyCapture` yields a
    /// `keyboard-input/HostKeyCaptureNotInterpreted` diagnostic — NOT a capture. No `ViewerEffect` case
    /// carries a `KeyboardEffect` (`DispatchInput` is host->product only), so the request cannot reach a
    /// host and the capture it arms never fires. This arm previously lowered it to
    /// `DispatchHostCommand "capture-key:{key}"`, an effect the framework never interprets either — a
    /// decoy that made an inert request look wired. To actually capture a rebind key, forward the RAW
    /// key out of the host's `MapKey` and route it in `update`, where the product's keymap and capture
    /// state live (`MapKey` itself never sees the model, so it cannot resolve either):
    ///
    ///     MapKey = fun key isDown -> Some(YourMsg(ViewerKeyboard.toKeyId key, isDown))
    ///
    /// The lambda IS the seam — `MapKey` is only a function, so a product writes it inline and loses
    /// nothing. `toKeyId` is exported by the `FS.GG.UI.KeyboardInput` your product pins, so this compiles
    /// against the released package and against `main` alike (#598).
    ///
    /// Surface the diagnostic with `AdapterCmd.diagnostics`: `AdapterCmd.productMessages` keeps only
    /// product messages and would drop it.
    val interpretKeyboardEffect: mapCommand: (CommandId -> 'msg) -> effect: KeyboardEffect -> AdapterCommand<'msg>
    /// Public contract function exposed by this FS.GG.UI package.
    val interpretControlEffect: mapRuntime: (ControlRuntimeMsg -> 'msg) -> effect: ControlRuntimeEffect -> AdapterCommand<'msg>
    /// Interpret one overlay effect at the host boundary. Open/close requests
    /// and product dispatches are mapped to product messages; focus requests
    /// always update ControlRuntime and may also emit a product focus message.
    val interpretOverlayEffect:
        mapOpen: (ControlId -> bool -> 'msg) ->
        mapDispatch: (ControlId -> string option -> 'msg) ->
        mapFocus: (ControlId option -> 'msg option) ->
        effect: OverlayEffect ->
            AdapterCommand<'msg>
    /// Interpret an ordered overlay effect list, preserving dispatch order.
    val interpretOverlayOutcome:
        mapOpen: (ControlId -> bool -> 'msg) ->
        mapDispatch: (ControlId -> string option -> 'msg) ->
        mapFocus: (ControlId option -> 'msg option) ->
        effects: OverlayEffect list ->
            AdapterCommand<'msg>
    /// Lower a single pointer interaction (075) into adapter commands. Diagnostics
    /// lower to `ReportAdapterDiagnostic`; every other interaction is offered to the
    /// consumer router `mapInteraction` (a `None` result is a no-op `[]`). Mirrors
    /// `interpretKeyboardEffect`/`interpretControlEffect`; no new `AdapterEffect`
    /// case is required. FR-001/FR-010/FR-011.
    val interpretPointerEffect:
        mapInteraction: (PointerInteraction -> 'msg option) -> interaction: PointerInteraction -> AdapterCommand<'msg>
    /// Convenience: lower the `(PointerInteraction list, ControlRuntimeMsg list)`
    /// produced by `Pointer.update` in one call — runtime messages through
    /// `DispatchControlRuntimeMessage` (applied first to keep `ControlRuntime`
    /// state consistent), then interactions through `interpretPointerEffect`.
    val interpretPointerOutcome:
        mapInteraction: (PointerInteraction -> 'msg option) ->
        interactions: PointerInteraction list ->
        runtimeMessages: ControlRuntimeMsg list ->
            AdapterCommand<'msg>
    /// Feature 147: derive compositor readiness diagnostics from existing `FrameMetrics`.
    val compositorDiagnostics:
        proofReady: bool ->
        fallbackReason: string option ->
        metrics: FrameMetrics ->
            CompositorFrameDiagnostics
    /// Feature 150: project layout and intrinsic cache work from a frame metrics record.
    val layoutMetrics: metrics: FrameMetrics -> LayoutWorkMetrics
    /// Feature 167: project existing frame metrics into a latency-record timing contribution.
    val responsivenessTimingContribution: metrics: FrameMetrics -> ResponsivenessTimingContribution
    /// Feature 167: verify disabled diagnostics leave deterministic frame metrics unchanged.
    val diagnosticsDisabledCompatibility:
        before: FrameMetrics list ->
        after: FrameMetrics list ->
            DiagnosticsDisabledCompatibility
    /// Public contract function exposed by this FS.GG.UI package.
    val subscriptions: keyboard: AdapterSubscription<'msg> list -> controls: AdapterSubscription<'msg> list -> AdapterSubscription<'msg> list
    /// Public contract function exposed by this FS.GG.UI package.
    val program:
        init: (unit -> 'model * AdapterCommand<'msg>) ->
        update: ('msg -> 'model -> 'model * AdapterCommand<'msg>) ->
        view: ('model -> Control<'msg>) ->
        subscriptions: ('model -> AdapterSubscription<'msg> list) ->
            AdapterProgram<'model, 'msg>
    /// Public contract function exposed by this FS.GG.UI package.
    val diagnostic: source: string -> code: string -> message: string -> AdapterDiagnostic
    /// Converts an adapter diagnostic into the shared runtime diagnostics taxonomy.
    val adapterDiagnosticToRuntimeDiagnostic:
        context: FS.GG.UI.Diagnostics.DiagnosticContext ->
        diagnostic: AdapterDiagnostic ->
            FS.GG.UI.Diagnostics.RuntimeDiagnostic
    /// Adapt a typed (`Widget<'msg>`-returning) view to the `Control<'msg>` view the
    /// program record expects (= `view >> Widget.toControl`). Lets typed authoring
    /// compose through the adapter with no boundary shim in product code. FR-001/FR-004.
    val widgetView: view: ('model -> Widget<'msg>) -> ('model -> Control<'msg>)
    /// Build a program whose view is authored with the typed front door (returns
    /// `Widget<'msg>`); the adapter lowers internally via `Widget.toControl`. Equivalent
    /// to `program init update (widgetView view) subscriptions`. FR-001/FR-004.
    val programOfWidget:
        init: (unit -> 'model * AdapterCommand<'msg>) ->
        update: ('msg -> 'model -> 'model * AdapterCommand<'msg>) ->
        view: ('model -> Widget<'msg>) ->
        subscriptions: ('model -> AdapterSubscription<'msg> list) ->
            AdapterProgram<'model, 'msg>

    /// The single pointer-routing step the interactive host performs per native pointer sample:
    /// renders `host.View size model` via `Control.renderTree host.Theme size`, hit-tests the
    /// laid-out bounds through the shipped 075 pipeline (`Pointer.update`, incl. the 4px click/drag
    /// fold), then routes each emitted interaction (feature 090, FR-001/FR-003): a hit control's
    /// authored `EventBindings` (`onClick`/`onChanged`) are dispatched — the authored control id is
    /// recovered via `Control.nearestAuthored` (so a click inside a container-keyed composite resolves
    /// to the authored container) and joined with `rendered.EventBindings` by `(ControlId, EventKind)`.
    /// An authored binding wins and consumes the interaction; `host.MapPointer` is the fallback,
    /// consulted ONLY for interactions no authored binding matched (no double-dispatch). A control with
    /// no authored binding behaves exactly as before (additive). Returns the advanced `PointerState`
    /// (threaded across samples) plus the product messages. `runInteractiveApp` wires exactly this;
    /// exposed so a headless test exercises the real adapter path without opening a window (research D6).
    val routeInteractivePointer:
        host: InteractiveAppHost<'model, 'msg> ->
        state: PointerState ->
        size: Size ->
        model: 'model ->
        input: ViewerPointerInput ->
            PointerState * 'msg list

    /// Build a responds-proof verdict from a before/after frame pair (feature 090, FR-006):
    /// `Responsive` when the frames differ, `Inert` when identical. The reusable core the pointer and
    /// text responds-proof captures share.
    val respondsProofOf: before: Scene -> after: Scene -> RespondsProof

    /// Capture an input→visible-change responds-proof for a pointer interaction on the running host
    /// (feature 090, FR-006/FR-007): render the BEFORE frame, route the interaction through the real
    /// `routeInteractivePointer` adapter path, fold the produced messages with `host.Update`, render
    /// the AFTER frame, and emit both frames + a verdict. A host whose live window is inert (an
    /// authored binding dropped) yields identical frames and an `Inert` verdict — it cannot be passed
    /// off as a responds-proof. Reuses the production render path; no live Vulkan window required.
    val captureRespondsProof:
        host: InteractiveAppHost<'model, 'msg> ->
        state: PointerState ->
        size: Size ->
        model: 'model ->
        input: ViewerPointerInput ->
            RespondsProof

    /// Launch `host` as a durable, pointer-routing, size-aware window (feature 085). Each frame
    /// renders `host.View size model` through `Control.renderTree host.Theme size`; native pointer
    /// samples are hit-tested through `Pointer.update` (incl. the shipped 4px click/drag fold) and
    /// routed by `routeInteractivePointer` — a hit control's authored `EventBindings` are dispatched
    /// (authored binding wins; `host.MapPointer` is the fallback for unconsumed interactions, feature
    /// 090 FR-001/FR-003), and keystrokes are routed focus-first (feature 094 / E4): each native key
    /// is offered to the E1 `routeFocusedText` seam (a focused TEXT control's printable keys), then
    /// to `routeFocusedKey` (the general activation / navigation / Tab-traversal seam over the
    /// focused control's `KeyboardOperation` and the `Focus.order` tab order), and finally falls
    /// through to `host.MapKey` for any key no focused control and no traversal consumed. A pointer
    /// press sets focus to the focusable control under it (FR-006), so a later key reaches it; a
    /// press on a non-focusable region leaves focus unchanged. Reuses `Viewer.runInteractiveViewer`;
    /// the durable `Viewer.runApp` literal is untouched.
    ///
    /// Feature 091 (E2, behavioral note — signature unchanged): the host no longer rebuilds the
    /// whole tree every frame. It holds a retained previous tree (`module internal RetainedRender`,
    /// the wired 067 reconciler) and produces each frame by `Reconcile.diff`-ing the next tree
    /// against it and reusing the unchanged subtrees' cached render fragments — O(changed-subtree),
    /// byte-for-byte identical to a full rebuild (FR-004/FR-005). Per-control state re-keys to the
    /// stable diff-conferred identity so it survives an unrelated re-render (FR-003); diff
    /// diagnostics (e.g. `KeyCollision`) surface through the host diagnostics channel, never
    /// dropped (FR-007). The consumer `Init`/`Update`/`View`/`MapKey`/`MapPointer`/`Tick`/`Theme`/
    /// `Diagnostics` contract is unchanged — an existing consumer needs zero changes to benefit
    /// (FR-008).
    val runInteractiveApp:
        options: ViewerOptions -> host: InteractiveAppHost<'model, 'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Feature 122 (FR-003/005): as `runInteractiveApp` with an explicit `ViewerWindowBehaviorRequest`
    /// threaded into the live launch (startup-state / resize / maximize / position / backend), so a
    /// generated app's parsed `--window-startup normal` actually applies to the controls window instead
    /// of only the options report. Delegates to `Viewer.runInteractiveViewerWithWindowBehavior`;
    /// `runInteractiveApp` stays the default windowed-fullscreen path, so existing consumers are
    /// unaffected.
    val runInteractiveAppWithWindowBehavior:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        host: InteractiveAppHost<'model, 'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #429: as `runInteractiveApp`, with an audio sink. This is the entry point for a product
    /// that needs BOTH a pointer and sound — a start screen, a volume slider, click-to-target — which
    /// until now had to choose: `runAppWithAudio` has no pointer and cannot author Controls, and the
    /// interactive host silently discarded `PlayAudio`. `audioSink` receives every batch a product's
    /// `update` emits, in dispatch order (the template hands in `FS.GG.Audio.Host.Audio.play backend`).
    /// `runInteractiveApp` (no sink) is unchanged.
    val runInteractiveAppWithAudio:
        options: ViewerOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveAppHost<'model, 'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #429: `runInteractiveAppWithAudio` with an explicit `ViewerWindowBehaviorRequest` — the
    /// audio-capable sibling of `runInteractiveAppWithWindowBehavior`.
    val runInteractiveAppWithWindowBehaviorAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveAppHost<'model, 'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #641 — every sound request in an effect batch, flattened in dispatch order; non-audio
    /// effects are dropped. The Controls-family name for the narrowing the generated-app family has had
    /// since #245 (`GeneratedAppHost.audioRequests`), so `… |> audioRequests |> Audio.interpret` yields
    /// the same `AudioEvidence` on both families and a product that changes host family does not have to
    /// change its audio assertions.
    ///
    /// Until this existed, an `app` product could REQUEST sound (#429/#436) and could not ASSERT it: the
    /// narrowing was `GeneratedAppHost`-only, and every audio-capable Controls path needed a live GL
    /// window. Pair it with `Perf.runScriptToEffects`, the headless fold that produces the effect list.
    val audioRequests: effects: ViewerEffect list -> AudioEffect list

    /// Launch `host` through the live GL-backed viewer, deliver a bounded `FrameInput` script through
    /// the viewer input queue, and return the live frame metrics observed by the adapter.
    module Live =
        val runScript:
            options: ViewerOptions ->
            host: InteractiveAppHost<'model, 'msg> ->
            script: FrameInput<'msg> list ->
                Result<LiveScriptRunResult, ViewerRunFailure>

        val runScriptWithWindowBehavior:
            options: ViewerOptions ->
            behavior: ViewerWindowBehaviorRequest ->
            host: InteractiveAppHost<'model, 'msg> ->
            script: FrameInput<'msg> list ->
                Result<LiveScriptRunResult, ViewerRunFailure>

        /// Issue #438 — `runScript` with an audio sink. The scripted Live runners are what the evidence
        /// and responsiveness tooling drives, and until now they handed the viewer core `ignore`: a
        /// scripted product could request sound and the batch was discarded with no error and no
        /// diagnostic — the same silent discard #429 removed from the non-scripted paths. `audioSink`
        /// receives every `PlayAudio` batch in dispatch order; `runScript` (no sink) is unchanged, and
        /// both share one scripted body so they cannot drift.
        val runScriptWithAudio:
            options: ViewerOptions ->
            audioSink: (AudioEffect list -> unit) ->
            host: InteractiveAppHost<'model, 'msg> ->
            script: FrameInput<'msg> list ->
                Result<LiveScriptRunResult, ViewerRunFailure>

        /// Issue #438 — `runScriptWithAudio` with an explicit window behavior, completing the pairing
        /// the sinkless scripted runners already have.
        val runScriptWithWindowBehaviorAndAudio:
            options: ViewerOptions ->
            behavior: ViewerWindowBehaviorRequest ->
            audioSink: (AudioEffect list -> unit) ->
            host: InteractiveAppHost<'model, 'msg> ->
            script: FrameInput<'msg> list ->
                Result<LiveScriptRunResult, ViewerRunFailure>

    /// Feature 108 (US3, FR-009/010): the pure, headless, deterministic frame driver. Folds an
    /// ordered `FrameInput` script over the host's pure `Update` + `RetainedRender.step`, advancing
    /// one frame per step (consecutive pointer-MOVE inputs coalesce into a single frame) and
    /// accumulating the per-frame `FrameMetrics`. A regression that un-coalesces moves or reintroduces
    /// a per-hover full rebuild fails the byte-stable count golden (SC-003/004/005) rather than
    /// shipping. The four count/bool fields are identical across repeated runs of the same script;
    /// `FrameDuration` is not asserted.
    ///
    /// WHAT IS SHARED WITH THE LIVE `runInteractiveApp` LOOP, precisely (issue #460 — this used to say
    /// "no parallel logic", which was false, and a false claim here is worse than none because it
    /// retires the reader's suspicion about the one thing they must check):
    ///
    ///   * SHARED, same functions: message→update→`RetainedRender.step`, binding resolution
    ///     (`routeRetainedInteraction`), `buildFrameMetrics`, `interpretPointerEffect`, clock advance,
    ///     and the `Coalescing` definition of a move frame.
    ///   * NOT SHARED: the frame loop. This is an independent fold; the live loop has its own. They
    ///     also coalesce different alphabets — the live loop drops raw SAMPLES before the hit-test and
    ///     `Pointer.update` re-derives whatever the surviving sample implies, whereas a script is
    ///     written in already-derived INTERACTIONS, which nothing re-derives.
    ///
    /// What IS guaranteed across the two: no state transition is lost. A coalesced frame drops only
    /// superseded POSITIONS, never a `HoverLeave`, press, release, scroll or drag boundary — so an
    /// interaction a product would see live is one a script sees too. Frame COUNTS are NOT claimed
    /// equal (a script is an abstraction over samples, and `FrameMetrics` counts script inputs); do
    /// not read the goldens as a statement about how many frames the live host renders.
    ///
    /// That asymmetry is real and cannot be refactored away, so it is GATED instead: `Coalescing.Parity`
    /// in Elmish.Tests drives the real `Pointer.update` and fails if the two sides ever disagree about
    /// what a coalesced frame drops. Trust that test, not a docstring.
    module Perf =
        /// Fold an ordered `FrameInput` script over the host's pure `Update` + `RetainedRender.step`,
        /// returning the per-frame `FrameMetrics` (consecutive pointer-MOVE inputs coalesce into one
        /// frame). Pure, headless, byte-stable in its count/bool fields (SC-003/004/005).
        val runScript:
            host: InteractiveAppHost<'model, 'msg> ->
            size: Size ->
            script: FrameInput<'msg> list ->
                FrameMetrics list

        /// As `runScript`, but also returns the FINAL folded model so a caller can render the
        /// POST-interaction frame — e.g. capture an offscreen screenshot of the scene AFTER a
        /// scroll/hover/focus/click script, closing the "drive interaction → see resulting frame" loop
        /// without a live window (Feature 175 S1). Same pure, headless, byte-stable fold as `runScript`.
        val runScriptToModel:
            host: InteractiveAppHost<'model, 'msg> ->
            size: Size ->
            script: FrameInput<'msg> list ->
                'model * FrameMetrics list

        /// Issue #641 — as `runScriptToModel`, but ALSO returns every `ViewerEffect` the script's `Init`
        /// and `Update` calls REQUESTED, in dispatch order (`Init`'s batch first). Same pure, headless,
        /// byte-stable fold: no window, no GL, no device.
        ///
        /// This is the Controls family's record-only assertion path, and the model cannot substitute for
        /// it. A restored volume the mixer was never TOLD about is indistinguishable, from inside the
        /// model, from one that was applied — so the `Started` trap (a product that flips a `Started`
        /// flag but never emits the `PlayAudio`) is not merely untested on this family without it, it is
        /// structurally UNCATCHABLE. Assert at the sink, not at the model:
        ///
        ///     let _, effects, _ = Perf.runScriptToEffects host size [ FrameInput.Pointer click ]
        ///     effects |> ControlsElmish.audioRequests |> Audio.interpret   // AudioEvidence
        ///
        /// REQUESTED, not PERFORMED. The list is what the product ASKED FOR; nothing here interprets it,
        /// and this host does not honour all of it — a `Persist` is DROPPED live, with a warning
        /// diagnostic, because the Controls family owns no persistence seam (#535). A recorded effect is
        /// not evidence that it would happen.
        ///
        /// And it is NOT a claim of frame-for-frame parity with the live loop's sink. What holds is what
        /// this fold already guarantees above: no state transition is lost, and the `Update` calls are
        /// the product's own. The two loops coalesce different alphabets, so a MOVE-derived request may
        /// be counted differently live — do not read this as "what the live sink would receive, effect
        /// for effect". What it IS: the product's REAL fold rather than a test-local re-derivation of it,
        /// and that is the distinction that matters — a hand-rolled fold in a test asserts what the TEST
        /// does, while the bug being hunted is the product loop doing something else.
        val runScriptToEffects:
            host: InteractiveAppHost<'model, 'msg> ->
            size: Size ->
            script: FrameInput<'msg> list ->
                'model * ViewerEffect list * FrameMetrics list
