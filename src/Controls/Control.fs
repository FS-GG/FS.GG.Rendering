namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

type TransientWidgetMetadata =
    { SurfaceKind: TransientSurfaceKind
      SurfaceId: ControlId
      ParentSurfaceId: ControlId option
      TriggerId: ControlId
      AnchorId: ControlId
      LayerPriority: int
      DismissalPolicy: DismissalPolicy
      FocusScope: FocusScope
      Modal: bool
      SelectionDispatchKey: string option
      VisibilityState: bool
      TriggerEnabled: bool }

type WidgetActivationRequest =
    { TriggerId: ControlId
      SurfaceId: ControlId
      ActivationSource: OverlayActivationSource
      RequestedOpenState: bool
      Diagnostic: ControlDiagnostic option }

module TransientWidget =
    let private attrName = "transientWidgetMetadata"

    let attribute (metadata: TransientWidgetMetadata) : Attr<'msg> =
        Attr.create attrName Data (UntypedValue(metadata :> obj))

    let collect (control: Control<'msg>) : TransientWidgetMetadata list =
        let rec walk current =
            let here =
                current.Attributes
                |> List.choose (fun attr ->
                    if attr.Name = attrName then
                        match attr.Value with
                        | UntypedValue(:? TransientWidgetMetadata as metadata) -> Some metadata
                        | _ -> None
                    else
                        None)

            here @ (current.Children |> List.collect walk)

        walk control

    let private blank (value: string) = String.IsNullOrWhiteSpace value

    let validate (anchor: AnchorEvidence option) (metadata: TransientWidgetMetadata) : ControlDiagnostic list =
        let findings = System.Collections.Generic.List<ControlDiagnostic>()

        if not (OverlayState.supportedSurfaceKinds () |> List.contains metadata.SurfaceKind) then
            findings.Add(Diagnostics.invalidOverlayMessage (Some metadata.SurfaceId) $"Unsupported transient surface kind `{metadata.SurfaceKind}`.")

        if blank metadata.SurfaceId then
            findings.Add(Diagnostics.invalidOverlayMessage None "Transient widget metadata is missing a surface id.")

        if blank metadata.TriggerId then
            findings.Add(Diagnostics.invalidOverlayMessage (Some metadata.SurfaceId) "Transient widget metadata is missing a trigger id.")

        if blank metadata.AnchorId then
            findings.Add(Diagnostics.missingOverlayAnchor metadata.SurfaceId metadata.AnchorId)

        if metadata.FocusScope.SurfaceId <> metadata.SurfaceId then
            findings.Add(Diagnostics.staleOverlayFocusTarget (Some metadata.SurfaceId) metadata.FocusScope.SurfaceId)

        if metadata.VisibilityState then
            match anchor with
            | Some evidence when evidence.AnchorBounds.IsSome -> ()
            | Some evidence -> findings.Add(Diagnostics.missingOverlayAnchor metadata.SurfaceId evidence.AnchorId)
            | None -> findings.Add(Diagnostics.missingOverlayAnchor metadata.SurfaceId metadata.AnchorId)

        List.ofSeq findings

    let toSurface (anchor: AnchorEvidence) (metadata: TransientWidgetMetadata) : OverlaySurface =
        { Id =
            { SurfaceId = metadata.SurfaceId
              ParentSurfaceId = metadata.ParentSurfaceId
              TriggerId = metadata.TriggerId }
          Kind = metadata.SurfaceKind
          Trigger =
            { ControlId = metadata.TriggerId
              Enabled = metadata.TriggerEnabled
              ActivationSource = ProductOwnedOpen
              RecoveryTarget = metadata.FocusScope.RecoveryTarget }
          LayerPriority = metadata.LayerPriority
          Anchor = anchor
          DismissalPolicy = metadata.DismissalPolicy
          FocusScope = metadata.FocusScope
          Modal = metadata.Modal }

    let activationRequest source requestedOpenState metadata =
        let diagnostic =
            if requestedOpenState && not metadata.TriggerEnabled then
                Some(Diagnostics.disabledOverlayTrigger metadata.TriggerId metadata.SurfaceId)
            else
                None

        { TriggerId = metadata.TriggerId
          SurfaceId = metadata.SurfaceId
          ActivationSource = source
          RequestedOpenState = requestedOpenState && diagnostic.IsNone
          Diagnostic = diagnostic }

module StandardControlKindHelpers =
    let toControlKind kind =
        match kind with
        | FS.GG.UI.Controls.StandardControlKind.TextBlock -> "text-block"
        | FS.GG.UI.Controls.StandardControlKind.Button -> "button"
        | FS.GG.UI.Controls.StandardControlKind.TextBox -> "text-box"
        | FS.GG.UI.Controls.StandardControlKind.LineChart -> "line-chart"
        | FS.GG.UI.Controls.StandardControlKind.BarChart -> "bar-chart"
        | FS.GG.UI.Controls.StandardControlKind.PieChart -> "pie-chart"
        | FS.GG.UI.Controls.StandardControlKind.ScatterPlot -> "scatter-plot"
        | FS.GG.UI.Controls.StandardControlKind.GraphView -> "graph-view"
        | FS.GG.UI.Controls.StandardControlKind.DataGrid -> "data-grid"
        | FS.GG.UI.Controls.StandardControlKind.Custom value -> value

module internal ControlInternals =
    // Feature 189 (US1): the shared prelude + geometry now live in sibling internal modules
    // (`ControlPrimitives`/`ChartGeometry`/`WidgetGeometry`) compiled before this file. They are
    // opened so the residual `faithfulContent`/`render*`/layout/hash/assembly bodies resolve the
    // relocated helpers and `*Geom` producers unqualified (byte-identical). The thin re-exports
    // below preserve the `ControlInternals.<member>` names external callers (RetainedRender,
    // Controls.Elmish, the test assembly) already use, so no caller edit is required (FR-013).
    open ControlPrimitives
    open ChartGeometry
    open WidgetGeometry
    open LayoutEval
    open NodeAssembly

    // --- ControlPrimitives re-exports (preserve ControlInternals.<member> for external callers) ---
    let tryLast name attrs = ControlPrimitives.tryLast name attrs
    let textFrom attrs = ControlPrimitives.textFrom attrs
    let styleClassesOf attrs = ControlPrimitives.styleClassesOf attrs
    let visualStateOf attrs = ControlPrimitives.visualStateOf attrs
    let slotFill fills = ControlPrimitives.slotFill fills
    let slotFillsOf attrs = ControlPrimitives.slotFillsOf attrs
    let slotFor name attrs = ControlPrimitives.slotFor name attrs
    let lowerSlots control = ControlPrimitives.lowerSlots control
    let accessibility kind attrs text = ControlPrimitives.accessibility kind attrs text
    let childrenFrom attrs = ControlPrimitives.childrenFrom attrs
    let disabledOrReadOnly control = ControlPrimitives.disabledOrReadOnly control
    let eventBindings path control = ControlPrimitives.eventBindings path control
    let recursively collect control = ControlPrimitives.recursively collect control
    let ellipsize family size maxWidth label = ControlPrimitives.ellipsize family size maxWidth label
    let chartValues control = ControlPrimitives.chartValues control
    let measureText text font = ControlPrimitives.measureText text font
    let setMeasureTextHook hook = ControlPrimitives.setMeasureTextHook hook

    // --- US2 re-exports: hashScene (SceneHash), faithfulContent/dataGridCells (ContentRender) ---
    let hashScene scenes = SceneHash.hashScene scenes
    let faithfulContent theme box control = ContentRender.faithfulContent theme box control
    let dataGridCells control = ContentRender.dataGridCells control

    // --- US2 re-exports: layout evaluators (LayoutEval) ---
    let layoutAffectingAttrNames = LayoutEval.layoutAffectingAttrNames
    let applyScrollOffsets root result = LayoutEval.applyScrollOffsets root result
    let evaluateLayout size control = LayoutEval.evaluateLayout size control
    let sceneWithViewportBackground theme size scenes = LayoutEval.sceneWithViewportBackground theme size scenes
    let evaluateLayoutIncremental size control previous dirty = LayoutEval.evaluateLayoutIncremental size control previous dirty

    // --- US2 re-exports: node assembly + preview render (NodeAssembly) ---
    let renderNode theme y control = NodeAssembly.renderNode theme y control
    let renderScene theme control = NodeAssembly.renderScene theme control
    let paintNode theme boundsById path c = NodeAssembly.paintNode theme boundsById path c


    let rec layoutNode (theme: Theme) (control: Control<'msg>) : FS.GG.UI.Layout.LayoutNode =
        // Feature 102 (R8): this `Key ?? Kind` is the legacy 080 single-control *preview*/layout id, local
        // to this offscreen `layoutNode` path. It is intentionally NOT the R3-unified `Key ?? path`
        // dispatch/recovery id (feature 098) — the divergence R3 removed was on the live dispatch path, not
        // here. Left as-is so a future reader does not "fix" it into the path-based scheme.
        let id = control.Key |> Option.defaultValue control.Kind
        let width = floatValue AttrWidth 240.0 control.Attributes
        let height = floatValue AttrHeight 28.0 control.Attributes
        let content = renderScene theme control
        let children = control.Children |> List.map (layoutNode theme)

        { LayoutDefaults.layoutNode id with
            Intent =
                { LayoutDefaults.layoutIntent with
                    Size = { Width = Some width; Height = Some height } }
            Content = Some content
            Children = children }

    let duplicateDiagnostics (control: Control<'msg>) =
        control.Attributes
        |> List.countBy _.Name
        |> List.choose (fun (name, count) ->
            if count > 1 then
                Some(Diagnostics.duplicateAttribute control.Key control.Kind name)
            else
                None)

    let requiredDiagnostics (control: Control<'msg>) =
        required control.Kind
        |> List.choose (fun name ->
            if hasAttr name control.Attributes then
                None
            else
                Some(Diagnostics.missingRequired control.Key control.Kind name))

    let keyDiagnostics (control: Control<'msg>) =
        recursively (fun control -> [ control ]) control
        |> List.choose (fun control -> control.Key |> Option.map (fun key -> key, control.Kind))
        |> List.groupBy fst
        |> List.collect (fun (key, rows) ->
            if rows.Length > 1 then
                rows |> List.tail |> List.map (fun (_, kind) -> Diagnostics.keyCollision key kind)
            else
                [])

    let controlDiagnostics (control: Control<'msg>) =
        duplicateDiagnostics control
        @ requiredDiagnostics control
        @ Accessibility.validate control

    // Feature 091 (RETAINED-PATH-1 / PARTIAL-UPDATE-1): the per-node measure + paint of
    // `Control.renderTree`, factored OUT of the render body so a single node's painted
    // contribution is a reusable unit. `Control.renderTree` and `module internal
    // RetainedRender` BOTH drive their Scene from `evaluateLayout` + `paintNode`, so the
    // wired retained path is byte-for-byte identical to a full rebuild BY CONSTRUCTION
    // (FR-005, contract C2) — the only divergence point removed entirely.
    let composeContainerScene (box: Rect option) (own: Scene list) (childScenes: Scene list) : Scene list =
        match box, childScenes with
        | Some b, _ :: _ -> own @ [ Scene.clipped (RectClip b) (Scene.group childScenes) ]
        | _ -> own @ childScenes

    /// Feature 137 (US2) — a node authors a deferred z-top overlay/transient surface (built on the
    /// `Overlay` container). Such a node's painted subtree is collected OUT of the in-flow container-clip
    /// hierarchy and emitted last (above the flow, at true coordinates, escaping ancestor clips). A
    /// transient surface (date-picker calendar, etc.) floats by authoring its open content as an `Overlay`.
    let isOverlayNode (c: Control<'msg>) : bool = c.Kind = "overlay"

    let private compositionEntriesForControl (c: Control<'msg>) : Composition.ModifierEntry list =
        if isOverlayNode c then
            // Feature 184 (US2): the overlay path emits the modern modifier IR directly — the literal
            // entry the retired `Composition.legacyLower LegacyOverlay` produced (byte-stable: same
            // Source/Effect → same normalize/fingerprint; see Feature184OverlayByteStabilityTests).
            [ { Composition.Source = Composition.LegacyOverlaySource
                Composition.Effect = Composition.LayerHint "overlay" } ]
        else
            []

    // Feature 120/141: the exact structural scene fingerprint lives with Controls, and retained rendering
    // aliases it for replay/cache boundaries that need byte-sensitive content keys.

    // Feature 178 (US2): the local fnvOffset/fnvPrime constants now come from the shared Hashing
    // primitive; the fingerprint folds below mix exactly as before (byte-identical output).
    let private fingerprintParts (domain: int) (parts: uint64 list) =
        let mutable h = Hashing.offsetBasis
        let mix x = h <- Hashing.step h x
        mix (uint64 (uint32 domain))
        mix (uint64 (List.length parts))
        parts |> List.iter mix
        h

    let private fingerprintString (value: string) =
        let mutable h = Hashing.offsetBasis
        let mix x = h <- Hashing.step h x
        mix 0x535452494E47UL
        mix (uint64 value.Length)

        for c in value do
            mix (uint64 (uint16 c))

        h

    let private fingerprintFloat (value: float) =
        uint64 (System.BitConverter.DoubleToInt64Bits value)

    let private fingerprintBox =
        function
        | None -> fingerprintParts 0x1410 [ 0UL ]
        | Some box ->
            fingerprintParts
                0x1411
                [ fingerprintFloat box.X
                  fingerprintFloat box.Y
                  fingerprintFloat box.Width
                  fingerprintFloat box.Height ]

    let private fingerprintChildList domain children select =
        let mutable h = Hashing.offsetBasis
        let mix x = h <- Hashing.step h x
        mix (uint64 (uint32 domain))
        mix (uint64 (List.length children))

        children
        |> List.iteri (fun index child ->
            mix (uint64 index)
            mix (select child))

        h

    let private collectAssemblyScenes select assemblies =
        match assemblies with
        | [] -> []
        | [ single ] -> select single
        | _ -> assemblies |> List.collect select

    let private fingerprintSceneShape (scenes: Scene list) =
        let mutable sceneCount = 0
        let mutable nodeCount = 0

        for scene in scenes do
            sceneCount <- sceneCount + 1
            nodeCount <- nodeCount + scene.Nodes.Length

        fingerprintParts 0x141A [ uint64 sceneCount; uint64 nodeCount ]

    let private needsExactAssemblyFingerprint (control: Control<'msg>) =
        // The replay picture cache currently keys only data-grid rows. Ordinary retained nodes use
        // composable owner fingerprints for evidence/reuse bookkeeping; cache boundaries still receive
        // the exact structural scene hash that proves replay correctness.
        control.Kind = "data-grid-row"

    let private emptySceneListFingerprint = hashScene []

    /// Feature 141 (R1b): per-child metadata retained rendering stores as owner-produced assembly
    /// evidence. It is deliberately descriptive; scene semantics remain the assembled scene lists.
    type CurrentNodeChildContribution =
        { Index: int
          InFlowFingerprint: uint64
          OverlayFingerprint: uint64 }

    /// Feature 139 (R1a): one node's assembled contribution split into normal in-flow paint and the
    /// deferred overlay group. This is internal contract shape only; no public scene IR changes.
    type CurrentNodeAssemblyResult =
        { InFlowScene: Scene list
          OverlayScene: Scene list
          InFlowFingerprint: uint64
          OverlayFingerprint: uint64
          Fingerprint: uint64
          Diagnostics: ControlDiagnostic list
          ChildContributions: CurrentNodeChildContribution list }

    type CurrentNodeBoundsResult =
        { InFlowBounds: (ControlId * Rect) list
          OverlayBounds: (ControlId * Rect) list }

    /// Feature 139 (R1a): the single current-semantics assembly owner. It deliberately captures only
    /// today's own-paint + child-paint + container-clip + overlay-promotion behavior; R2/R1b work such as
    /// modifier algebra, portals, public IR changes, intrinsic layout, text shaping, compositor changes,
    /// and portable protocol changes stays outside this seam.
    let assembleCurrentNode
        (control: Control<'msg>)
        (box: Rect option)
        (ownScene: Scene list)
        (childAssemblies: CurrentNodeAssemblyResult list)
        : CurrentNodeAssemblyResult =
        let childInFlow = childAssemblies |> collectAssemblyScenes _.InFlowScene
        let childOverlay = childAssemblies |> collectAssemblyScenes _.OverlayScene
        let chain = compositionEntriesForControl control |> Composition.normalize
        let composed = composeContainerScene box ownScene childInFlow |> Composition.applyChain chain
        let ownFingerprint = fingerprintSceneShape ownScene
        let childInFlowFingerprint = fingerprintChildList 0x1412 childAssemblies _.InFlowFingerprint
        let childOverlayFingerprint = fingerprintChildList 0x1414 childAssemblies _.OverlayFingerprint
        let chainFingerprint = fingerprintString chain.FingerprintInput
        let controlFingerprint = fingerprintString control.Kind

        let composedFingerprint =
            fingerprintParts
                0x1416
                [ controlFingerprint
                  ownFingerprint
                  childInFlowFingerprint
                  uint64 childInFlow.Length
                  fingerprintBox box
                  chainFingerprint ]

        let childContributions =
            childAssemblies
            |> List.mapi (fun index child ->
                { Index = index
                  InFlowFingerprint = child.InFlowFingerprint
                  OverlayFingerprint = child.OverlayFingerprint })

        let diagnostics =
            chain.Diagnostics
            |> List.map (fun d ->
                { ControlId = control.Key
                  ControlKind = control.Kind
                  Code = UnsupportedStateCombination
                  Severity = ControlDiagnosticSeverity.Warning
                  Message = d.Message
                  EvidencePath = None })

        if isOverlayNode control then
            let overlayScene = composed @ childOverlay

            let overlayFingerprint =
                fingerprintParts 0x1417 [ composedFingerprint; childOverlayFingerprint; uint64 overlayScene.Length ]

            let inFlowFingerprint = emptySceneListFingerprint
            let fingerprint = fingerprintParts 0x1418 [ inFlowFingerprint; overlayFingerprint ]

            { InFlowScene = []
              OverlayScene = overlayScene
              InFlowFingerprint = inFlowFingerprint
              OverlayFingerprint = overlayFingerprint
              Fingerprint = fingerprint
              Diagnostics = diagnostics
              ChildContributions = childContributions }
        else
            let overlayFingerprint =
                fingerprintParts 0x1419 [ childOverlayFingerprint; uint64 childOverlay.Length ]

            let fingerprint =
                fingerprintParts
                    0x1418
                    [ composedFingerprint
                      overlayFingerprint
                      uint64 (List.length composed + List.length childOverlay) ]

            let inFlowFingerprint, overlayFingerprint, fingerprint =
                if needsExactAssemblyFingerprint control then
                    let combined = composed @ childOverlay
                    hashScene composed, hashScene childOverlay, hashScene combined
                else
                    composedFingerprint, overlayFingerprint, fingerprint

            { InFlowScene = composed
              OverlayScene = childOverlay
              InFlowFingerprint = inFlowFingerprint
              OverlayFingerprint = overlayFingerprint
              Fingerprint = fingerprint
              Diagnostics = diagnostics
              ChildContributions = childContributions }

    let assembleCurrentNodeBounds
        (control: Control<'msg>)
        (path: string)
        (box: Rect option)
        (childBounds: CurrentNodeBoundsResult list)
        : CurrentNodeBoundsResult =
        let controlId: ControlId = control.Key |> Option.defaultValue path
        let here =
            match box with
            | Some b -> [ controlId, b ]
            | None -> []

        let childInFlow = childBounds |> List.collect _.InFlowBounds
        let childOverlay = childBounds |> List.collect _.OverlayBounds

        if isOverlayNode control then
            { InFlowBounds = []
              OverlayBounds = here @ childInFlow @ childOverlay }
        else
            { InFlowBounds = here @ childInFlow
              OverlayBounds = childOverlay }

    /// The evaluated absolute box of a node, looked up by the same structural id `paintNode`
    /// uses (`Key |> defaultValue path`). `None` when the node was not laid out.
    let nodeBox
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (path: string)
        (c: Control<'msg>)
        : Rect option =
        let id = c.Key |> Option.defaultValue path

        Map.tryFind id boundsById
        |> Option.map (fun (b: FS.GG.UI.Layout.LayoutBounds) ->
            { X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }: Rect)

    /// The evaluated `Bounds` list (`ControlId * Rect`) `renderTree` surfaces, computed from a
    /// pre-evaluated `boundsById` so the retained path produces the identical list.
    let collectBoundsWith
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (control: Control<'msg>)
        : (ControlId * Rect) list =
        // Feature 137 (US2): `go` returns (in-flow, overlay) bounds. Overlay-surface subtrees are
        // collected last so the shipped topmost-wins (reverse-scan) `hitTest` consults the overlay group
        // BEFORE in-flow — a click where an open overlay covers an in-flow sibling returns the overlay.
        // For an overlay-free tree the overlay list is empty and the bounds are byte-identical pre-order.
        let rec go (path: string) (c: Control<'msg>) : (ControlId * Rect) list * (ControlId * Rect) list =
            // FR-001/FR-007 (feature 098): the emitted `ControlId` is the unified `Key ?? path`
            // (`layoutId`) — the same id `EventBindings`/`BoundIds`/recovery use — replacing the old
            // divergent `Key ?? Kind`. Keyed nodes are unchanged; unkeyed ids shift `Kind → path`.
            let layoutId = c.Key |> Option.defaultValue path
            let controlId: ControlId = layoutId

            let here =
                match Map.tryFind layoutId boundsById with
                | Some(b: FS.GG.UI.Layout.LayoutBounds) -> [ controlId, ({ X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }: Rect) ]
                | None -> []

            let childResults = c.Children |> List.mapi (fun index child -> go (path + "." + string index) child)
            let childInFlow = childResults |> List.collect fst
            let childOverlay = childResults |> List.collect snd

            if isOverlayNode c then
                [], here @ childInFlow @ childOverlay
            else
                here @ childInFlow, childOverlay

        let inFlow, overlay = go "0" control
        inFlow @ overlay

    /// The recursive `EventBindings` list `renderTree` surfaces, factored so the retained path
    /// emits the identical list. Path-aware (FR-001): re-derives each node's `parent + "." + index`
    /// path (root "0") so an unkeyed node's binding keys by its `path`, not its `Kind`.
    let eventBindingsOf (control: Control<'msg>) : ControlEventBinding<'msg> list =
        let rec go (path: string) (c: Control<'msg>) : ControlEventBinding<'msg> list =
            eventBindings path c
            @ (c.Children
               |> List.mapi (fun index child -> go (path + "." + string index) child)
               |> List.concat)

        go "0" control

    /// Feature 098 (FR-002) — the canonical ids (`Key ?? path`) of every node carrying ≥1 event
    /// binding, collected over the same positional path scheme as `eventBindingsOf`/`collectBoundsWith`.
    /// The single source for `ControlRenderResult.BoundIds` at every construction site (the full
    /// rebuild AND the retained frames), so the retained path is byte-identical by construction.
    let boundIdsOf (control: Control<'msg>) : Set<ControlId> =
        let rec go (path: string) (c: Control<'msg>) (acc: Set<ControlId>) : Set<ControlId> =
            let acc =
                if List.isEmpty (eventBindings path c) then
                    acc
                else
                    Set.add (c.Key |> Option.defaultValue path) acc

            c.Children
            |> List.mapi (fun index child -> index, child)
            |> List.fold (fun acc (index, child) -> go (path + "." + string index) child acc) acc

        go "0" control Set.empty

/// Feature 150 — source used to derive a `scroll-viewer` content extent.
type ScrollExtentSource =
    | EmptyContentExtent
    | IntrinsicContentExtent
    | MeasuredFallbackExtent
    | DiagnosticFallbackExtent

/// Feature 137/150 — read-back geometry of a `scroll-viewer` viewport.
type ScrollViewport =
    { Viewport: Rect
      ContentWidth: float
      ContentHeight: float
      OffsetX: float
      OffsetY: float
      Offset: float
      MaxHorizontalOffset: float
      MaxVerticalOffset: float
      ExtentSource: ScrollExtentSource
      Diagnostics: ControlDiagnostic list }

module Control =
    let create kind (attrs: Attr<'msg> list) =
        let text = ControlInternals.textFrom attrs
        let children = ControlInternals.childrenFrom attrs

        { Kind = kind
          Key = None
          Attributes = attrs
          Children = children
          Content = text
          Accessibility = ControlInternals.accessibility kind attrs text }

    let standard kind attrs =
        create (StandardControlKindHelpers.toControlKind kind) attrs

    let customControl kind attrs =
        create kind attrs

    let lowerStandard (control: Control<'msg>) =
        control

    let lowerCustom (control: Control<'msg>) =
        control

    let withKey key (control: Control<'msg>) =
        { control with Key = Some key }

    // Feature 108 (US5, FR-014): rewrite a single AttrValue's message type. Only the two
    // handler-bearing cases (`MessageValue`/`EventValue`) actually thread `f`; the nested-control
    // cases recurse; every data-only case is reconstructed verbatim in the `'b` DU. Total over the
    // closed `AttrValue` set.
    let rec private mapAttrValue (f: 'a -> 'b) (value: AttrValue<'a>) : AttrValue<'b> =
        match value with
        | TextValue v -> TextValue v
        | BoolValue v -> BoolValue v
        | FloatValue v -> FloatValue v
        | StringListValue v -> StringListValue v
        | ValidationValue v -> ValidationValue v
        | StyleClassesValue v -> StyleClassesValue v
        | VisualStateValue v -> VisualStateValue v
        | SlotFillsValue fills -> SlotFillsValue(fills |> List.map (fun (name, child) -> name, mapControl f child))
        | AccessibilityValue v -> AccessibilityValue v
        | ThemeValue v -> ThemeValue v
        | ChildValue child -> ChildValue(mapControl f child)
        | ChildrenValue children -> ChildrenValue(children |> List.map (mapControl f))
        | MessageValue msg -> MessageValue(f msg)
        | EventValue handler -> EventValue(handler >> f)
        | UntypedValue v -> UntypedValue v

    and mapControl (f: 'a -> 'b) (control: Control<'a>) : Control<'b> =
        { Kind = control.Kind
          Key = control.Key
          Attributes =
            control.Attributes
            |> List.map (fun attr ->
                { Name = attr.Name
                  Category = attr.Category
                  Value = mapAttrValue f attr.Value })
          Children = control.Children |> List.map (mapControl f)
          Content = control.Content
          Accessibility = control.Accessibility }

    let map (f: 'a -> 'b) (control: Control<'a>) : Control<'b> = mapControl f control

    let rec count (control: Control<'msg>) =
        1 + (control.Children |> List.sumBy count)

    let diagnostics (control: Control<'msg>) =
        ControlInternals.recursively ControlInternals.controlDiagnostics control
        @ ControlInternals.keyDiagnostics control

    let render (theme: Theme) (control: Control<'msg>) =
        { Scene = ControlInternals.renderScene theme control
          Layout = ControlInternals.layoutNode theme control
          // The 080 single-control PREVIEW does not expose per-control evaluated bounds;
          // that is a `renderTree` (nested layout) feature (FR-011). Kept empty here so the
          // preview Scene stays byte-identical (FR-010).
          Bounds = []
          Diagnostics = diagnostics control
          EventBindings = ControlInternals.eventBindingsOf control
          // FR-002 (feature 098): the preview keeps `Bounds = []` but DOES populate `BoundIds`
          // (mirroring its populated `EventBindings`) in the unified `Key ?? path` scheme.
          BoundIds = ControlInternals.boundIdsOf control
          NodeCount = count control }

    // Feature 085 (FR-001/FR-002/FR-003) — faithful NESTED-tree renderer.
    //
    // Unlike `render` (the 080 single-control preview, which flattens every descendant and
    // stacks them at fixed y offsets), `renderTree` runs a REAL recursive Yoga layout over the
    // nested tree at the supplied output `size`, then paints every node — containers AND their
    // children — at its COMPUTED bounds. Two structurally different trees therefore produce
    // visibly different scenes (SC-001). `render`/`Widget.render` are left untouched (FR-003).
    //
    // Feature 091: the per-node measure (`evaluateLayout`/`toLayout`) and paint (`paintNode`)
    // are factored into `ControlInternals` so `module internal RetainedRender` drives its
    // Scene from the SAME functions — the retained/partial render path is byte-for-byte
    // identical to this full rebuild by construction (the only divergence point removed).
    // `next frame is produced by diffing against the retained previous tree` (FR-005, C2).
    let renderTree (theme: Theme) (size: FS.GG.UI.Scene.Size) (control: Control<'msg>) =
        let root, boundsById, _ = ControlInternals.evaluateLayout size control

        // Feature 139 (R1a): the recursive paint walk delegates the current-node assembly rule to
        // `ControlInternals.assembleCurrentNode`, the same owner retained build and emit paths use.
        let rec paint (path: string) (c: Control<'msg>) : ControlInternals.CurrentNodeAssemblyResult =
            let own = ControlInternals.paintNode theme boundsById path c

            let childAssemblies =
                c.Children |> List.mapi (fun index child -> paint (path + "." + string index) child)

            ControlInternals.assembleCurrentNode c (ControlInternals.nodeBox boundsById path c) own childAssemblies

        let assembled = paint "0" control

        { Scene = (assembled.InFlowScene @ assembled.OverlayScene) |> ControlInternals.sceneWithViewportBackground theme size
          Layout = root
          Bounds = ControlInternals.collectBoundsWith boundsById control
          Diagnostics = diagnostics control
          EventBindings = ControlInternals.eventBindingsOf control
          BoundIds = ControlInternals.boundIdsOf control
          NodeCount = count control }

    // FR-012: resolve which rendered control (if any) contains the point (x, y), from the
    // public render result alone — `None` in a gap. Layered over `Layout.hitTestComputed` by
    // reconstructing a `LayoutResult` whose `NodeId`s ARE the `ControlId`s in `Bounds`, so the
    // shipped topmost-wins (reverse-scan) semantics return the deepest containing control.
    let hitTest (result: ControlRenderResult<'msg>) (x: float) (y: float) : ControlId option =
        let computed: FS.GG.UI.Layout.LayoutResult =
            { Bounds =
                result.Bounds
                |> List.map (fun (controlId, (rect: Rect)) ->
                    { NodeId = controlId
                      Bounds = { X = rect.X; Y = rect.Y; Width = rect.Width; Height = rect.Height }
                      Visibility = FS.GG.UI.Layout.Visible }: FS.GG.UI.Layout.ComputedBounds)
              Diagnostics = []
              Invalidated = []
              Revision = 0L }

        FS.GG.UI.Layout.Layout.hitTestComputed (LayoutDefaults.pixelSnapPolicy 1.0) computed x y

    // FR-004/FR-004a/FR-005 (feature 090): resolve a structural hit `ControlId` — the id a
    // `PointerInteraction`/`hitTest` carries: a `Key` for an authored node, else the positional
    // path `toLayout` assigns ("0", "0.1", …) — to the NEAREST ancestor (incl. self) the consumer
    // authored with a `withKey`, returned as that ancestor's authored `ControlId` (its `Key`). A
    // click inside a CONTAINER-KEYED composite therefore recovers the container's authored id
    // instead of an opaque inner positional id ("0.1"), so the host can route its binding. `None`
    // when no keyed ancestor exists anywhere on the hit node's path — the host then falls back to
    // `MapPointer` with the raw interaction and never invents a `Kind`/root id the consumer did not
    // author. A directly-keyed leaf's hit id IS its `Key`, so it resolves to itself (FR-005,
    // non-regressive — a fixed point).
    //
    // Pure/total/deterministic: walks the already-computed `result.Layout` tree, re-deriving each
    // node's positional path by the SAME `parent + "." + index` scheme as `toLayout`. A node is
    // authored exactly when its layout `Id` differs from that positional path (`toLayout` sets
    // `Id = Key |> defaultValue path`, so `Id <> path` ⇔ the node carries an explicit `Key`). No
    // clock/randomness; resume-safe; reads existing render data only — no layout-math change.
    let nearestAuthored (result: ControlRenderResult<'msg>) (hit: ControlId) : ControlId option =
        let rec search (path: string) (nearestKeyed: ControlId option) (node: FS.GG.UI.Layout.LayoutNode) : ControlId option =
            // FR-003 (feature 098): a node is *authored* when it is KEYED (`node.Id <> path`) OR its
            // canonical id is BOUND (`node.Id ∈ result.BoundIds`). `node.Id` is already `Key ?? path`,
            // so it IS the canonical id: a directly-keyed leaf stays a fixed point, and an unkeyed-bound
            // node now returns `Some node.Id` (its path) where it returned `None` before — a single
            // one-predicate widening, no control-flow restructure.
            let authoredHere =
                if node.Id <> path || Set.contains node.Id result.BoundIds then
                    Some node.Id
                else
                    None

            let nearestForChildren =
                match authoredHere with
                | Some _ -> authoredHere
                | None -> nearestKeyed

            if node.Id = hit then
                // nearest authored ancestor including self
                match authoredHere with
                | Some id -> Some id
                | None -> nearestKeyed
            else
                node.Children
                |> List.mapi (fun index child -> index, child)
                |> List.tryPick (fun (index, child) -> search (path + "." + string index) nearestForChildren child)

        search "0" None result.Layout

    let private scrollExtentSource source =
        match source with
        | FS.GG.UI.Layout.EmptyContent -> EmptyContentExtent
        | FS.GG.UI.Layout.IntrinsicResult -> IntrinsicContentExtent
        | FS.GG.UI.Layout.MeasuredFallback -> MeasuredFallbackExtent
        | FS.GG.UI.Layout.DiagnosticFallback -> DiagnosticFallbackExtent

    let private scrollDiagnostic controlId (diagnostic: FS.GG.UI.Layout.LayoutDiagnostic) =
        let code =
            match diagnostic.Code with
            | FS.GG.UI.Layout.UnsupportedIntrinsicQuery
            | FS.GG.UI.Layout.RejectedIntrinsicResult
            | FS.GG.UI.Layout.InsufficientDependencyEvidence
            | FS.GG.UI.Layout.ContradictoryIntrinsicExtent -> ScrollIntrinsicUnavailable
            | _ when diagnostic.FallbackApplied -> ScrollExtentFallback
            | _ -> LayoutConflict

        { ControlId = Some controlId
          ControlKind = "scroll-viewer"
          Code = code
          Severity =
            match diagnostic.Severity with
            | FS.GG.UI.Layout.DiagnosticSeverity.Info -> ControlDiagnosticSeverity.Info
            | FS.GG.UI.Layout.DiagnosticSeverity.Warning -> ControlDiagnosticSeverity.Warning
            | FS.GG.UI.Layout.DiagnosticSeverity.Error -> ControlDiagnosticSeverity.Error
          Message = diagnostic.Message
          EvidencePath = None }

    /// Feature 137/150 — read back the scroll geometry of a `scroll-viewer` from a render result.
    /// The content extent is derived from the Layout intrinsic protocol.
    let scrollViewport (result: ControlRenderResult<'msg>) (scrollViewerId: ControlId) : ScrollViewport option =
        let boundsMap = result.Bounds |> Map.ofList

        let rec find (node: FS.GG.UI.Layout.LayoutNode) : FS.GG.UI.Layout.LayoutNode option =
            if node.Id = scrollViewerId then
                Some node
            else
                node.Children |> List.tryPick find

        match find result.Layout, Map.tryFind scrollViewerId boundsMap with
        | Some node, Some viewport ->
            let extent = FS.GG.UI.Layout.Layout.contentExtent viewport.Width viewport.Height (node.Children |> List.tryHead)
            let diagnostics = extent.Diagnostics |> List.map (scrollDiagnostic scrollViewerId)

            Some
                { Viewport = viewport
                  ContentWidth = extent.ContentWidth
                  ContentHeight = extent.ContentHeight
                  OffsetX = 0.0
                  OffsetY = 0.0
                  Offset = 0.0
                  MaxHorizontalOffset = extent.MaxHorizontalOffset
                  MaxVerticalOffset = extent.MaxVerticalOffset
                  ExtentSource = scrollExtentSource extent.ExtentSource
                  Diagnostics = diagnostics }
        | _ -> None

    /// Feature 137 (US2) — the public entry to the deferred overlay render pass: does this control author
    /// a z-top overlay/transient surface (built on the `Overlay` container)? `renderTree` collects such a
    /// subtree OUT of the in-flow container-clip hierarchy and paints it last (above the flow, at true
    /// coordinates, escaping ancestor clips); `hitTest` consults the overlay group first. A transient
    /// surface (menu/combo/auto-complete/date-picker calendar) floats by authoring its open content as an
    /// `Overlay`. An empty overlay group renders byte-identically to a pure in-flow pass.
    let isOverlaySurface (control: Control<'msg>) : bool = ControlInternals.isOverlayNode control

    let dispatch (event: ControlEvent) (control: Control<'msg>) =
        // FR-001/D5 (feature 098): thread the positional path so the unkeyed `binding.ControlId`
        // matched here uses the unified `Key ?? path` scheme. Keyed callers (the whole
        // `InteractionTests.fs` suite) and the `event.ControlId = None` wildcard are byte-identical;
        // only the unkeyed `Kind`-id match (unused by any current consumer) shifts to the path scheme.
        let rec loop (path: string) (current: Control<'msg>) =
            let own =
                if ControlInternals.disabledOrReadOnly current then
                    []
                else
                    ControlInternals.eventBindings path current
                    |> List.filter (fun binding ->
                        binding.EventKind = event.Kind
                        && (event.ControlId.IsNone || event.ControlId = Some binding.ControlId))
                    |> List.map (fun binding -> binding.Dispatch event)

            own
            @ (current.Children
               |> List.mapi (fun index child -> index, child)
               |> List.collect (fun (index, child) -> loop (path + "." + string index) child))

        loop "0" control |> List.truncate 1

module TextBlock =
    let create attrs = Control.create "text-block" attrs
    let text value = Attr.text value

module Label =
    let create attrs = Control.create "label" attrs
    let text value = Attr.text value

module Image =
    let create attrs = Control.create "image" attrs
    let source value = Attr.value value

module Icon =
    let create attrs = Control.create "icon" attrs
    let name value = Attr.text value

module Separator =
    let create attrs = Control.create "separator" attrs

module Badge =
    let create attrs = Control.create "badge" attrs
    let text value = Attr.text value

module Button =
    let create attrs = Control.create "button" attrs
    let text value = Attr.text value
    let enabled value = Attr.enabled value
    let onClick msg = Attr.on "onClick" msg
    let onClickWith map = Attr.onWith "onClick" map

module IconButton =
    let create attrs = Control.create "icon-button" attrs
    let icon value = Attr.text value
    let onClick msg = Attr.on "onClick" msg

// Feature 105 (US1, FR-003): the per-kind `onChanged` builders below.
// Feature 184 (US3): read the typed `Nav` outcome (via the `ControlEvent` accessors) instead of
// parsing the retired stringly `Payload`. Hidden from consumers by absence from Control.fsi.
module ChangeAdapters =
    let onChangedBool (map: bool -> 'msg) : Attr<'msg> =
        // Feature 184 (US3): a boolean toggle reports its new state typed as `SteppedValue 1.0/0.0`.
        Attr.onWith "onChanged" (fun event -> ControlEvent.navValue event |> Option.exists (fun v -> v >= 0.5) |> map)

    let onChangedFloat (map: float -> 'msg) : Attr<'msg> =
        Attr.onWith "onChanged" (fun event -> ControlEvent.navValue event |> Option.defaultValue 0.0 |> map)

    let onChangedString (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith "onChanged" (fun event -> ControlEvent.navText event |> Option.defaultValue "" |> map)

module CheckBox =
    let create attrs = Control.create "check-box" attrs
    let text value = Attr.text value
    let checked' value = Attr.selected value
    let onChanged map = ChangeAdapters.onChangedBool map

module Switch =
    let create attrs = Control.create "switch" attrs
    let checked' value = Attr.selected value
    let onChanged map = ChangeAdapters.onChangedBool map

module Slider =
    let create attrs = Control.create "slider" attrs
    let value value = Attr.create "value" Content (FloatValue value)
    let onChanged map = ChangeAdapters.onChangedFloat map

module NumericInput =
    let create attrs = Control.create "numeric-input" attrs
    let value value = Attr.create "value" Content (FloatValue value)
    let onChanged map = ChangeAdapters.onChangedFloat map

module TextBox =
    let create attrs = Control.create "text-box" attrs
    let value value = Attr.value value
    let readOnly value = Attr.readOnly value
    let validation state = Attr.validation state
    let onChanged map = ChangeAdapters.onChangedString map

module TextArea =
    let create attrs = Control.create "text-area" attrs
    let value value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module RadioGroup =
    let create attrs = Control.create "radio-group" attrs
    let items values = Attr.items values
    let selected value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module Stack =
    let create attrs = Control.create "stack" attrs
    let children controls = Attr.children controls
    // FR-007: opt a stack into row layout. "horizontal" lays children along the row axis;
    // any other value (or omission) keeps the default vertical column.
    let orientation value = Attr.create "orientation" Layout (TextValue value)

module Grid =
    let create attrs = Control.create "grid" attrs
    let children controls = Attr.children controls

module Dock =
    let create attrs = Control.create "dock" attrs
    let children controls = Attr.children controls

module Wrap =
    let create attrs = Control.create "wrap" attrs
    let children controls = Attr.children controls

module Border =
    let create attrs = Control.create "border" attrs
    let child control = Attr.child control

module Panel =
    let create attrs = Control.create "panel" attrs
    let children controls = Attr.children controls

module ProgressBar =
    let create attrs = Control.create "progress-bar" attrs
    let value value = Attr.create "value" Content (FloatValue value)

module Spinner =
    let create attrs = Control.create "spinner" attrs

module ValidationMessage =
    let create attrs = Control.create "validation-message" attrs
    let text value = Attr.text value

module Tabs =
    let create attrs = Control.create "tabs" attrs
    let items values = Attr.items values
    let selected value = Attr.value value
    let onChanged map = ChangeAdapters.onChangedString map

module Menu =
    let create attrs = Control.create "menu" attrs
    let items values = Attr.items values
    let onSelected map = Attr.onWith "onSelected" (fun event -> ControlEvent.navText event |> Option.defaultValue "" |> map)

module Toolbar =
    let create attrs = Control.create "toolbar" attrs
    let children controls = Attr.children controls

module Tooltip =
    let create attrs = Control.create "tooltip" attrs
    let text value = Attr.text value

module Dialog =
    let create attrs = Control.create "dialog" attrs
    let children controls = Attr.children controls

module Toast =
    let create attrs = Control.create "toast" attrs
    let text value = Attr.text value

module Overlay =
    let create attrs = Control.create "overlay" attrs
    let child control = Attr.child control
