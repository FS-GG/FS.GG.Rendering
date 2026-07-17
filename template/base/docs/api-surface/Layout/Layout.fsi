// See skill: fs-gg-layout
namespace FS.GG.UI.Layout

open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module Layout =
    /// Public contract function exposed by this FS.GG.UI package.
    ///
    /// Lays `root` out with Yoga, the only layout engine. There is no second engine to fall back on:
    /// if Yoga fails, every node comes back with empty bounds alongside an `Error`-severity
    /// `FallbackBoundsApplied` diagnostic constrained to `"yoga"`. Callers that render geometry
    /// should check for it rather than assume the bounds are meaningful.
    ///
    /// NOT thread-safe. All layout trees are built against one process-wide native Yoga config, with
    /// no locking; concurrent `evaluate` calls are undefined behaviour. Call it from one thread.
    ///
    /// NOT a pure function of its arguments. When the process-global `AppContext` switch
    /// `"FS.GG.UI.Layout.ForceYogaFailure"` is set, Yoga execution is forced to fail and this returns
    /// the empty-bounds error result above. The switch exists to exercise that path from tests; setting
    /// it changes the result of every `evaluate` in the process.
    val evaluate : available: AvailableSpace -> root: LayoutNode -> LayoutResult
    /// Public contract function exposed by this FS.GG.UI package.
    val evaluateIncremental :
        previous: LayoutResult ->
        changedNodeIds: LayoutNodeId list ->
        available: AvailableSpace ->
        root: LayoutNode ->
            LayoutResult

    /// Public contract function exposed by this FS.GG.UI package.
    val renderComputed : result: LayoutResult -> root: LayoutNode -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val snapBounds : policy: PixelSnapPolicy -> bounds: LayoutBounds -> LayoutBounds
    /// Public contract function exposed by this FS.GG.UI package.
    val hitTestComputed : policy: PixelSnapPolicy -> result: LayoutResult -> x: float -> y: float -> LayoutNodeId option
    /// Public contract function exposed by this FS.GG.UI package.
    val initWorkflow : available: AvailableSpace -> root: LayoutNode -> LayoutWorkflowModel * LayoutWorkflowEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateWorkflow : msg: LayoutWorkflowMsg -> model: LayoutWorkflowModel -> LayoutWorkflowModel * LayoutWorkflowEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val interpretWorkflowEffect : effect: LayoutWorkflowEffect -> model: LayoutWorkflowModel -> LayoutWorkflowMsg
    /// Public contract function exposed by this FS.GG.UI package.
    val horizontalStack : config: StackConfig -> children: LayoutChild list -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val verticalStack : config: StackConfig -> children: LayoutChild list -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val dock : config: DockConfig -> children: LayoutChild list -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val measureHorizontal : config: StackConfig -> children: LayoutChild list -> LayoutBounds list
    /// Public contract function exposed by this FS.GG.UI package.
    val measureVertical : config: StackConfig -> children: LayoutChild list -> LayoutBounds list
