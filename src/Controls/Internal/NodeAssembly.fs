namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module LayoutDefaults = FS.GG.UI.Layout.Defaults

/// Feature 189 (US2, FR-004/T018): node assembly + preview render — `renderNode`/`renderScene` (the
/// single-control preview walk) and `paintLeaf`/`paintNode` (the per-node retained paint unit),
/// relocated verbatim from `ControlInternals`. `module internal`; opens `ControlPrimitives`,
/// `ContentRender` (faithfulContent) and `LayoutEval` (toLayout) — all compiled before it, so no
/// back-edge (the residual `layoutNode` calls back into `renderScene` here, resolved by `open`).
module internal NodeAssembly =
    open ControlPrimitives
    open ChartGeometry
    open WidgetGeometry
    open ContentRender
    open LayoutEval
    let renderNode (theme: Theme) y (control: Control<'msg>) =
        let width = nodeWidth control
        let height = nodeHeight control
        let visible = boolValue "visible" true control.Attributes
        let label = control.Content |> Option.defaultValue control.Kind

        if not visible then
            Scene.group [ Scene.rectangle (0.0, y, width, height) Colors.transparent ]
        elif ControlKindRegistry.isRich control.Kind then
            // Title band on top; control-specific geometry below it (within the canvas).
            let pad = 10.0
            let titleH = 30.0
            let box: Rect = { X = pad; Y = y + titleH; Width = width - 2.0 * pad; Height = height - titleH - pad }
            // Title band shows the control's NAME (the schematic below shows its content); this
            // fixes composite-lowering title bleed and content duplication for rich families.
            let title =
                Scene.clipped
                    (RectClip { X = 0.0; Y = y; Width = width; Height = titleH })
                    (mkText theme 8.0 (y + 19.0) 13.0 theme.Foreground (prettyKind control.Kind))
            Scene.group (title :: faithfulContent theme box control)
        else
            // Text / container controls: the control IS its text, so box + clipped label is faithful.
            let fill =
                if disabledOrReadOnly control then theme.Muted
                elif boolValue "selected" false control.Attributes then theme.Accent
                else theme.Background
            let fontSize = fittedFontSize theme.FontSize 6.0 width height theme.FontFamily label
            let textY = y + (height + fontSize) * 0.5 - 3.0
            let labelRun =
                { Text = label
                  Position = { X = 8.0; Y = textY }
                  Font = { Family = theme.FontFamily; Size = fontSize; Weight = None }
                  Paint = Paint.fill theme.Foreground }
            Scene.group [
                Scene.rectangle (0.0, y, width, height) fill
                Scene.clipped
                    (RectClip { X = 0.0; Y = y; Width = width; Height = height })
                    (Scene.textRun labelRun)
            ]

    let renderScene (theme: Theme) (control: Control<'msg>) =
        let controls = recursively (fun control -> [ control ]) control

        ((0.0, []), controls)
        ||> List.fold (fun (y, scenes) control ->
            let height = nodeHeight control
            y + height + 4.0, renderNode theme y control :: scenes)
        |> snd
        |> List.rev
        |> Scene.group


    let paintLeaf (theme: Theme) (box: Rect) (c: Control<'msg>) : Scene list =
        // Feature 191 (US1, D1/D5/FR-001/FR-002/FR-013/FR-016): the `canvas` kind paints an
        // application-supplied immutable Scene into its laid-out box — authored in canvas-local
        // coordinates, translated to the box origin and clipped to the box; an optional viewport
        // transform pans/zooms the CONTENT only (layout size and hit-test box unchanged). Branches
        // BEFORE the rich-family check (the canvas is neither a rich widget nor a text leaf).
        if c.Kind = "canvas" then
            if box.Width <= 0.0 || box.Height <= 0.0 then
                // Zero-area / unmeasured box: paint nothing and never error (FR-013 safe failure).
                []
            else
                match sceneValueOf c with
                | Some content ->
                    let viewed =
                        match viewportOf c with
                        | Some vp -> Scene.withPerspective vp content
                        | None -> content

                    [ Scene.clipped (RectClip box) (Scene.translate box.X box.Y viewed) ]
                | None ->
                    // No scene supplied ⇒ a clear design-time placeholder (FR-013).
                    emptyState theme box "canvas"
        elif ControlKindRegistry.isRich c.Kind then
            let content = faithfulContent theme box c
            // Feature 136 (US3/T035): clip chart bodies to the control box so degenerate or
            // out-of-range data can never paint outside its bounds (the data is also finite-guarded
            // in `chartValues`). Non-chart rich families are unchanged.
            if ControlKindRegistry.isChart c.Kind then
                [ Scene.clipped (RectClip box) (Scene.group content) ]
            else
                content
        else
            let label = c.Content |> Option.defaultValue c.Kind

            let fill =
                if disabledOrReadOnly c then theme.Muted
                elif boolValue "selected" false c.Attributes then theme.Accent
                else theme.Background

            let fontSize =
                fittedFontSize theme.FontSize 6.0 box.Width box.Height theme.FontFamily label

            let textY = box.Y + (box.Height + fontSize) * 0.5 - 3.0

            // Feature 136 (T016A): if the label still overflows the box at the smallest fitted size,
            // ellipsize it (explicit `…`) rather than letting the clip rect silently drop characters.
            let shown =
                ellipsize theme.FontFamily fontSize (box.Width - 16.0) label

            let labelRun =
                { Text = shown
                  Position = { X = box.X + 8.0; Y = textY }
                  Font = { Family = theme.FontFamily; Size = fontSize; Weight = None }
                  Paint = Paint.fill theme.Foreground }

            [ Scene.rectangle (box.X, box.Y, box.Width, box.Height) fill
              Scene.clipped (RectClip box) (Scene.textRun labelRun) ]

    /// Paint ONE node's own contribution (`here`) at its computed box — the reusable unit a
    /// retained `RenderFragment` caches. Output depends ONLY on `theme`, the looked-up box,
    /// and the node's own `Kind`/`Content`/`Attributes`/has-children — never on its
    /// descendants — so caching it keyed by (own-paint identity, box) is sound (feature 091,
    /// contracts C2/C5). Returns `[]` for a node with no computed box.
    let paintNode
        (theme: Theme)
        (boundsById: Map<string, FS.GG.UI.Layout.LayoutBounds>)
        (path: string)
        (c: Control<'msg>)
        : Scene list =
        let id = c.Key |> Option.defaultValue path

        match Map.tryFind id boundsById with
        | None -> []
        | Some(b: FS.GG.UI.Layout.LayoutBounds) ->
            let box: Rect = { X = b.X; Y = b.Y; Width = b.Width; Height = b.Height }

            if List.isEmpty c.Children then
                paintLeaf theme box c
            else
                // Container: a faint frame so the nesting is visible; the real children are painted by
                // their own `paintNode` at their own computed bounds and clipped to this box by
                // `composeContainerScene` (feature 137 US1).
                let frame = [ Scene.rectangleWithPaint box (Paint.stroke theme.Muted 1.0) ]

                if c.Kind = "scroll-viewer" then
                    // Feature 150: the scroll affordance uses the same intrinsic extent path exposed by
                    // `Control.scrollViewport`, not a rendered descendant-bounds walk.
                    let layoutNode = toLayout path c
                    let extent = FS.GG.UI.Layout.Layout.contentExtent box.Width box.Height (layoutNode.Children |> List.tryHead)
                    // Feature 175: thread the live offset (stamped by the host) so the thumb tracks.
                    let scroll: ScrollState =
                        { Offset = scrollOffsetOf c.Attributes
                          ContentHeight = extent.ContentHeight
                          ViewportHeight = box.Height }

                    frame @ scrollAffordance theme box scroll
                else
                    frame

    /// Feature 137 (US1, the blocker) — the single shared container-clip composition rule
    /// (data-model §1). Composes a node's own paint with its assembled children: when there is a box
    /// AND at least one child scene, the children are wrapped in a `ClipNode` to the node's box (so no
    /// child paints past its container); a leaf or a box-less node composes flat — byte-identical to the
    /// pre-137 `own @ childScenes`. Used at EVERY paint-assembly site so full ≡ retained and
    /// cache-on ≡ cache-off hold by construction (the `assemble` emit walk was the feature-136 miss).
