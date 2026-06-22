namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

/// Feature 189 (US2, FR-004): the layout evaluators — `toLayout` (control→Yoga node), `evaluateLayout`
/// / `evaluateLayoutIncremental` (+ scroll-offset shifting and the layout-affecting attr set), relocated
/// verbatim from `ControlInternals`. `module internal`; opens `ControlPrimitives`. No render/paint dep
/// (the offscreen `layoutNode` render-path helper stays in ControlInternals), so no back-edge. INV-1:
/// bounds byte-identical.
module internal LayoutEval =
    open ControlPrimitives
    let orientationOf (c: Control<'msg>) =
        tryLast AttrOrientation c.Attributes
        |> Option.bind (fun attr ->
            match attr.Value with
            | TextValue value -> Some value
            | _ -> None)

    let directionOf (c: Control<'msg>) =
        // Feature 183 (US1): the horizontal-layout kind set (data-grid + its row/header, toolbar,
        // split-view, wrap, grid, dock — Feature 136 US3/T031) now reads the single ControlKindRegistry
        // SSOT (byte-identical, incl. the non-catalog `data-grid-row`/`data-grid-header` arms).
        if ControlKindRegistry.layoutRow c.Kind then
            FS.GG.UI.Layout.Row
        else
            match orientationOf c with
            | Some "horizontal" -> FS.GG.UI.Layout.Row
            | _ -> FS.GG.UI.Layout.Column

    let wrapOf kind =
        match kind with
        | "wrap"
        | "grid" -> FS.GG.UI.Layout.Wrap
        | _ -> FS.GG.UI.Layout.NoWrap

    /// Feature 097 (R2): the attribute NAMES the incremental dirty-set classifier (`layoutDirtySet`)
    /// keys on, so a change to a geometry-driving attribute re-measures while a content/style/state/
    /// visual-state change does not (SC-004). These are the same names `toLayout` (below) reads to
    /// derive geometry — `Size` from `width`/`height`, `Direction` from `orientation`, spacing
    /// fields from padding/margin/gap, flex fields, alignment, and min/max constraints.
    ///
    /// Feature 101 (R7): this literal is a SEPARATE, hot-path `Set` from `toLayout`'s reads — it is NOT
    /// auto-derived from them, so the two agree by maintenance discipline alone. That agreement is now
    /// *gated*, not merely asserted: the behavioral-probe equality gate in
    /// `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs` toggles each candidate attribute on
    /// representative fixtures, observes which names actually change the real `evaluateLayout` output,
    /// and fails the build the instant this set drifts from what `toLayout` reads (either direction).
    /// The shared `AttrKeys` name tokens remove typo drift; the gate makes membership drift
    /// impossible to ship. (A change tagged `AttrCategory.Layout` is honoured by `layoutDirtySet`
    /// independently of this name set, so a future categorised attr needs no edit here — that
    /// independence is pinned by the same test file.)
    let layoutAffectingAttrNames: Set<string> =
        Set.ofList
            [ AttrKeys.LayoutWidth
              AttrKeys.LayoutHeight
              AttrKeys.LayoutOrientation
              AttrKeys.LayoutPadding
              AttrKeys.LayoutMargin
              AttrKeys.LayoutGap
              AttrKeys.LayoutSpacing
              AttrKeys.LayoutAlignItems
              AttrKeys.LayoutAlignSelf
              AttrKeys.LayoutJustifyContent
              AttrKeys.LayoutFlexGrow
              AttrKeys.LayoutFlexShrink
              AttrKeys.LayoutFlexBasis
              AttrKeys.LayoutMinWidth
              AttrKeys.LayoutMinHeight
              AttrKeys.LayoutMaxWidth
              AttrKeys.LayoutMaxHeight ]

    let rec toLayout (path: string) (c: Control<'msg>) : FS.GG.UI.Layout.LayoutNode =
        let id = c.Key |> Option.defaultValue path
        let isLeaf = List.isEmpty c.Children

        let size: FS.GG.UI.Layout.LayoutSize =
            if isLeaf then
                { Width = Some(nodeWidth c)
                  Height = Some(nodeHeight c) }
            else
                { Width = (if hasAttr AttrWidth c.Attributes then Some(nodeWidth c) else None)
                  Height = (if hasAttr AttrHeight c.Attributes then Some(nodeHeight c) else None) }

        let attrs = c.Attributes
        let padding = tryFloat AttrKeys.LayoutPadding attrs |> Option.map uniformSpacing |> Option.defaultValue (uniformSpacing 8.0)
        let margin = tryFloat AttrKeys.LayoutMargin attrs |> Option.map uniformSpacing |> Option.defaultValue LayoutDefaults.padding
        let gap = tryFloatAny [ AttrKeys.LayoutGap; AttrKeys.LayoutSpacing ] attrs |> Option.map uniformGap |> Option.defaultValue (uniformGap 8.0)
        let minSize: FS.GG.UI.Layout.LayoutSize =
            { Width = tryFloat AttrKeys.LayoutMinWidth attrs
              Height = tryFloat AttrKeys.LayoutMinHeight attrs }
        let maxSize: FS.GG.UI.Layout.LayoutSize =
            { Width = tryFloat AttrKeys.LayoutMaxWidth attrs
              Height = tryFloat AttrKeys.LayoutMaxHeight attrs }

        { LayoutDefaults.layoutNode id with
            Intent =
                { LayoutDefaults.layoutIntent with
                    Direction = directionOf c
                    Wrap = wrapOf c.Kind
                    AlignItems = tryAlign AttrKeys.LayoutAlignItems attrs |> Option.defaultValue LayoutDefaults.layoutIntent.AlignItems
                    AlignSelf = tryAlign AttrKeys.LayoutAlignSelf attrs
                    JustifyContent = tryAlign AttrKeys.LayoutJustifyContent attrs |> Option.defaultValue LayoutDefaults.layoutIntent.JustifyContent
                    Padding = padding
                    Margin = margin
                    Gap = gap
                    Size = size
                    MinSize = minSize
                    MaxSize = maxSize
                    FlexGrow = tryFloat AttrKeys.LayoutFlexGrow attrs |> Option.defaultValue LayoutDefaults.layoutIntent.FlexGrow
                    FlexShrink = tryFloat AttrKeys.LayoutFlexShrink attrs |> Option.defaultValue LayoutDefaults.layoutIntent.FlexShrink
                    FlexBasis = tryFloat AttrKeys.LayoutFlexBasis attrs }
            Children = c.Children |> List.mapi (fun index child -> toLayout (path + "." + string index) child) }

    /// Build the nested Yoga layout tree for `control` at `size`, evaluate it, and return the
    /// root `LayoutNode` plus the evaluated absolute bounds keyed by the SAME collision-free
    /// structural id (`Key |> defaultValue path`) the paint/bounds passes look up.
    let availableOf (size: FS.GG.UI.Scene.Size) : FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let boundsByIdOf (result: FS.GG.UI.Layout.LayoutResult) =
        result.Bounds
        |> List.map (fun (b: FS.GG.UI.Layout.ComputedBounds) -> b.NodeId, b.Bounds)
        |> Map.ofList

    // Feature 175 (FR-001/FR-009): the live scroll offset stamped onto a `scroll-viewer` node by the
    // host (absent ⇒ 0.0). Read by both the bounds transform below and the thumb paint, so paint and
    // hit-test (which BOTH read `boundsById`) agree by construction.
    let scrollOffsetOf (attrs: Attr<'msg> list) : float =
        tryFloat AttrKeys.ScrollOffset attrs |> Option.defaultValue 0.0

    let rec subtreeLayoutIds (path: string) (c: Control<'msg>) : string list =
        let id = c.Key |> Option.defaultValue path
        id :: (c.Children |> List.mapi (fun i ch -> subtreeLayoutIds (path + "." + string i) ch) |> List.concat)

    // Accumulate, per layout id, the total vertical scroll offset contributed by every ancestor
    // `scroll-viewer` carrying a positive `scrollOffset` attr (nested viewers add up). EMPTY when no
    // node is scrolled, so `applyScrollOffsets` is the identity at rest (byte-identical, FR-014).
    let rec collectScrollOffsets (path: string) (c: Control<'msg>) : Map<string, float> =
        let here =
            match scrollOffsetOf c.Attributes with
            | delta when delta > 0.0 ->
                c.Children
                |> List.mapi (fun i ch -> subtreeLayoutIds (path + "." + string i) ch)
                |> List.concat
                |> List.map (fun id -> id, delta)
                |> Map.ofList
            | _ -> Map.empty

        c.Children
        |> List.mapi (fun i ch -> collectScrollOffsets (path + "." + string i) ch)
        |> List.fold
            (fun acc m -> Map.fold (fun a k v -> Map.add k (v + (Map.tryFind k a |> Option.defaultValue 0.0)) a) acc m)
            here

    /// Feature 175: shift each scrolled `scroll-viewer` descendant's bounds up by its accumulated
    /// offset IN THE `LayoutResult`, so EVERYTHING that derives from it agrees (FR-009): the paint
    /// `boundsById` (derived below), `Control.hitTest`'s `Bounds` list, AND the live retained pointer
    /// route (`retained.Layout` = this `LayoutResult`, hit-tested by `Layout.hitTestComputed`).
    /// Identity when nothing is scrolled (byte-identical at rest). The viewport CLIP is unchanged —
    /// `composeContainerScene` still clips children to the `scroll-viewer` box.
    let applyScrollOffsets (root: Control<'msg>) (result: FS.GG.UI.Layout.LayoutResult) =
        let offsets = collectScrollOffsets "0" root
        if Map.isEmpty offsets then
            result
        else
            { result with
                Bounds =
                    result.Bounds
                    |> List.map (fun (b: FS.GG.UI.Layout.ComputedBounds) ->
                        match Map.tryFind b.NodeId offsets with
                        | Some delta -> { b with Bounds = { b.Bounds with Y = b.Bounds.Y - delta } }
                        | None -> b) }

    let evaluateLayout (size: FS.GG.UI.Scene.Size) (control: Control<'msg>) =
        let root = toLayout "0" control
        let result = FS.GG.UI.Layout.Layout.evaluate (availableOf size) root
        // Feature 175: `boundsById` is offset-shifted (paint + `Control.hitTest` agree), but the RAW
        // `result` is returned — it is threaded as the incremental layout cache (`prev.Layout`) AND
        // stored as `retained.Layout`. Shifting the threaded copy would DOUBLE-SHIFT reused descendant
        // bounds each frame, so the live retained pointer route instead re-applies `applyScrollOffsets`
        // to `retained.Layout` at hit-test time (idempotent, FR-009).
        root, boundsByIdOf (applyScrollOffsets control result), result

    let sceneWithViewportBackground (theme: Theme) (size: FS.GG.UI.Scene.Size) (scenes: Scene list) : Scene =
        (Scene.rectangle (0.0, 0.0, float size.Width, float size.Height) theme.Background :: scenes)
        |> Scene.group

    /// Feature 097 (R2): the incremental render-path seam (contract C4). Drives layout through
    /// `Layout.evaluateIncremental`, threading the previous frame's `LayoutResult` (the bounds cache)
    /// and the patch-derived `dirty` set. Returns the same `root, boundsById` shape `evaluateLayout`
    /// returns (so the reuse-driven paint walk is unchanged) plus the new `LayoutResult` to carry
    /// forward. `Bounds` are byte-identical to a full `evaluateLayout` (INV-1).
    let evaluateLayoutIncremental
        (size: FS.GG.UI.Scene.Size)
        (control: Control<'msg>)
        (previous: FS.GG.UI.Layout.LayoutResult)
        (dirty: Set<FS.GG.UI.Layout.LayoutNodeId>)
        =
        let root = toLayout "0" control
        let result = FS.GG.UI.Layout.Layout.evaluateIncremental previous (Set.toList dirty) (availableOf size) root
        // Feature 175: boundsById shifted (paint); RAW result threaded/stored (see `evaluateLayout`).
        root, boundsByIdOf (applyScrollOffsets control result), result

