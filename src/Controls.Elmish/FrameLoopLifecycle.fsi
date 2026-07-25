namespace FS.GG.UI.Controls.Elmish

open System
open FS.GG.UI.Controls
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Mutable interpreter-edge lifecycle kept separate from product model state.
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

    val create<'model, 'msg> : unit -> State<'model, 'msg>
    val surfaceDiagnosticOnce: diagnostic: ControlDiagnostic -> state: State<'model, 'msg> -> bool
    val recordPointerSample: state: State<'model, 'msg> -> unit
    val takePendingMove: state: State<'model, 'msg> -> ViewerPointerInput option
    val deferMove: input: ViewerPointerInput -> state: State<'model, 'msg> -> unit
    val completeMoveBoundary: state: State<'model, 'msg> -> int
    val completeDiscreteBoundary: state: State<'model, 'msg> -> int
