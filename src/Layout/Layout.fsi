namespace FS.GG.UI.Layout

open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module Layout =
    /// Public contract function exposed by this FS.GG.UI package.
    val constraints:
        source: LayoutConstraintSource ->
        minWidth: float ->
        maxWidth: float option ->
        minHeight: float ->
        maxHeight: float option ->
            LayoutConstraints
    /// Public contract function exposed by this FS.GG.UI package.
    val constraintsFromAvailable: source: LayoutConstraintSource -> available: AvailableSpace -> LayoutConstraints
    /// Public contract function exposed by this FS.GG.UI package.
    val layoutInputKey: node: LayoutNode -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val intrinsicQuery:
        participantId: LayoutNodeId ->
        axis: IntrinsicAxis ->
        crossAxisConstraint: float option ->
        layoutInputKey: string ->
        source: IntrinsicQuerySource ->
            IntrinsicQuery
    /// Public contract function exposed by this FS.GG.UI package.
    val evaluateIntrinsic: query: IntrinsicQuery -> node: LayoutNode -> IntrinsicSizeResult
    /// Public contract function exposed by this FS.GG.UI package.
    val measureProtocol: constraints: LayoutConstraints -> node: LayoutNode -> MeasuredLayoutResult
    /// Public contract function exposed by this FS.GG.UI package.
    val cacheEntry:
        kind: LayoutCacheEntryKind ->
        participantId: LayoutNodeId ->
        constraintIdentity: string ->
        layoutInputKey: string ->
        childDependencyKeys: string list ->
        resultIdentity: string ->
            LayoutCacheEntry
    /// Public contract function exposed by this FS.GG.UI package.
    val contentExtent:
        viewportWidth: float ->
        viewportHeight: float ->
        content: LayoutNode option ->
            LayoutContentExtent
    /// Public contract function exposed by this FS.GG.UI package.
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
