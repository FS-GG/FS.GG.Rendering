namespace FS.GG.UI.Controls.Elmish

open System
open FS.GG.UI.Controls
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Mutable interpreter-edge lifecycle kept separate from product model state. The narrow transition
/// functions are the only writers for pointer batching and one-time diagnostic disclosure.
module internal FrameLoopLifecycle =
    type State<'model, 'msg> =
        { mutable PointerState: PointerState
          mutable Focused: RetainedId option
          mutable Retained: RetainedRender<'msg> option
          mutable LastRender: ControlRenderResult<'msg> option
          mutable LastView: (Size * 'model * Control<'msg>) option
          mutable LastRuntimeModel: ControlRuntimeModel option
          mutable ScrollOffsets: Map<ControlId, ScrollState>
          mutable SurfacedDiagnostics: Set<string>
          mutable PendingMove: ViewerPointerInput option
          mutable PointerSampleCount: int
          mutable LastWorkReduction: WorkReductionRecord option
          mutable LastPresentTiming: TimeSpan * TimeSpan }

    let create<'model, 'msg> () : State<'model, 'msg> =
        { PointerState = Pointer.init ()
          Focused = None
          Retained = None
          LastRender = None
          LastView = None
          LastRuntimeModel = None
          ScrollOffsets = Map.empty
          SurfacedDiagnostics = Set.empty
          PendingMove = None
          PointerSampleCount = 0
          LastWorkReduction = None
          LastPresentTiming = (TimeSpan.Zero, TimeSpan.Zero) }

    let surfaceDiagnosticOnce (diagnostic: ControlDiagnostic) (state: State<'model, 'msg>) =
        let key = sprintf "%A|%A|%s" diagnostic.Code diagnostic.ControlId diagnostic.Message

        if Set.contains key state.SurfacedDiagnostics then
            false
        else
            state.SurfacedDiagnostics <- Set.add key state.SurfacedDiagnostics
            true

    let recordPointerSample (state: State<'model, 'msg>) =
        state.PointerSampleCount <- state.PointerSampleCount + 1

    let takePendingMove (state: State<'model, 'msg>) =
        let pending = state.PendingMove
        state.PendingMove <- None
        pending

    let deferMove input (state: State<'model, 'msg>) =
        state.PendingMove <- Some input

    let completeMoveBoundary (state: State<'model, 'msg>) =
        let completed = state.PointerSampleCount - 1
        state.PointerSampleCount <- 1
        completed

    let completeDiscreteBoundary (state: State<'model, 'msg>) =
        let completed = state.PointerSampleCount
        state.PointerSampleCount <- 0
        completed
