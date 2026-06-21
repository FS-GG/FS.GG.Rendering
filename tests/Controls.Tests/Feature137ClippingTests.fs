module Feature137ClippingTests

// Feature 137 (US1, the blocker) — container clipping via the single shared
// `ControlInternals.composeContainerScene`, routed through ALL six paint-assembly sites. Two oracles:
//   * T004 — container-bounds non-overflow: a child laid out wider/taller than its container renders
//     with its drawn area confined to the container box (a `ClipNode` to the container bounds wraps the
//     children); a leaf / box-less node composes flat.
//   * T005 — full ≡ retained parity on a clipped tree: `Control.renderTree` and the retained
//     `init`/`step` produce byte-identical scenes for a container-with-children tree.
// (The `Audit_PictureCache` trio is the separate cache-on ≡ cache-off regression gate.)

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem
open Rendering.Harness

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 300 }

// Every `ClipNode` (clip, inner) anywhere in a scene (recursing through transparent wrappers).
let rec private clipNodes (s: Scene) : (Clip * Scene) list = s.Nodes |> List.collect clipNodesNode

and private clipNodesNode (n: SceneNode) : (Clip * Scene) list =
    match n with
    | ClipNode(c, inner) -> (c, inner) :: clipNodes inner
    | Group scenes -> scenes |> List.collect clipNodes
    | Translate(_, inner) -> clipNodes inner
    | ColorSpaceNode(_, inner) -> clipNodes inner
    | PerspectiveNode(_, inner) -> clipNodes inner
    | PictureNode p -> clipNodes p.Scene
    | CachedSubtree b -> clipNodes b.Scene
    | _ -> []

let private rectClipRects (s: Scene) : Rect list =
    clipNodes s
    |> List.choose (fun (c, _) ->
        match c with
        | RectClip r -> Some r
        | _ -> None)

let private rectClose (a: Rect) (b: Rect) =
    abs (a.X - b.X) < 0.5
    && abs (a.Y - b.Y) < 0.5
    && abs (a.Width - b.Width) < 0.5
    && abs (a.Height - b.Height) < 0.5

[<Tests>]
let tests =
    testList "Feature137 container clipping (US1, FR-001/002/003)" [

        // ---- T004: the shared composition rule (direct, deterministic) ----
        test "composeContainerScene clips children to the box when there is a box AND ≥1 child" {
            let box = { X = 10.0; Y = 20.0; Width = 100.0; Height = 50.0 }
            let own = [ Scene.filledRectangle box Colors.black ]
            let child = [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 999.0; Height = 999.0 } Colors.white ]

            let composed = ControlInternals.composeContainerScene (Some box) own child
            let clips = rectClipRects (Scene.group composed)
            Expect.equal clips.Length 1 "exactly one container clip wraps the children"
            Expect.isTrue (rectClose (List.head clips) box) "the clip rect is the container box"
        }

        test "composeContainerScene composes flat for a leaf (no children) and a box-less node" {
            let box = { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }
            let own = [ Scene.filledRectangle box Colors.black ]
            // leaf: a box but no children → flat (byte-identical to `own @ []`)
            Expect.equal (ControlInternals.composeContainerScene (Some box) own []) own "leaf composes flat"
            // box-less: children but no box → flat `own @ children`
            let child = [ Scene.filledRectangle box Colors.white ]
            Expect.equal (ControlInternals.composeContainerScene None own child) (own @ child) "box-less composes flat"
            Expect.isEmpty (rectClipRects (Scene.group (own @ child))) "no container clip is introduced flat"
        }

        // ---- T004: integration — a child laid out larger than its container is clipped to the box ----
        test "a child wider/taller than its container is wrapped in a ClipNode to the container box" {
            // The root always fills the viewport, so the constrained container is nested one level down;
            // the child is wider than the container on the cross axis (Yoga honors the explicit cross size,
            // so it genuinely overflows rather than being shrunk to fit).
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Stack.create
                                [ Attr.width 120.0
                                  Attr.height 80.0
                                  Stack.children
                                      [ Stack.create
                                            [ Attr.width 400.0
                                              Attr.height 40.0
                                              Stack.children [ TextBlock.create [ TextBlock.text "overflowing child" ] ] ] ] ] ] ]

            let rendered = Control.renderTree theme size tree
            let containerBox = rendered.Bounds |> List.find (fun (id, _) -> id = "0.0") |> snd
            let childBox = rendered.Bounds |> List.find (fun (id, _) -> id = "0.0.0") |> snd

            Expect.isTrue
                (childBox.Width > containerBox.Width || childBox.Height > containerBox.Height)
                "the child is genuinely laid out beyond the container (real overflow to confine)"

            let clips = rectClipRects rendered.Scene
            Expect.isTrue
                (clips |> List.exists (fun r -> rectClose r containerBox))
                "the children are wrapped in a ClipNode to the container box (no spill past bounds)"
        }

        // ---- T005: full ≡ retained parity on a clipped (container-with-children) tree ----
        test "full ≡ retained: renderTree, retained init, and an idle step are byte-identical on a clipped tree" {
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "header" ]
                            Stack.create
                                [ Stack.orientation "horizontal"
                                  Stack.children
                                      [ Button.create [ Button.text "A" ]
                                        Button.create [ Button.text "B" ] ] ]
                            TextBlock.create [ TextBlock.text "footer" ] ] ]

            let full = (Control.renderTree theme size tree).Scene
            let inited = RetainedRender.init theme size tree
            let initScene = inited.Render.Scene
            let stepped = (RetainedRender.step theme size inited.Retained tree).Render.Scene

            Expect.equal initScene full "retained init scene is byte-identical to the full renderTree scene"
            Expect.equal stepped full "retained step scene is byte-identical to the full renderTree scene"

            // and the clip is actually present (this tree has nested containers with children)
            Expect.isNonEmpty (rectClipRects full) "the clipped tree contains container clips (the rule fired)"
        }
    ]

// ---- US3: ScrollViewer viewport (FR-008) ----
let private tallScrollViewer () : Control<Msg> =
    // Many intrinsic-height rows so the content genuinely overflows the viewport (Yoga clamps the
    // direct child's box but lays the rows out past it — real, deterministic vertical overflow).
    let rows = [ for i in 1..40 -> TextBlock.create [ TextBlock.text (sprintf "row %d" i) ] ]
    let content = Stack.create [ Stack.children rows ]
    let sv = Control.create "scroll-viewer" [ Attr.children [ content ] ] |> Control.withKey "sv"
    Stack.create [ Stack.children [ sv ] ]

[<Tests>]
let scrollTests =
    testList "Feature137 ScrollViewer viewport (US3, FR-008)" [

        // ---- T018: viewport clips content, exposes a scroll offset, renders an affordance ----
        test "a scroll-viewer clips overflowing content to its box, exposes a scroll offset, and shows an affordance" {
            let rendered = Control.renderTree theme size (tallScrollViewer ())
            let svBox = rendered.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd

            // content is confined to the viewport box (a ClipNode to the sv box)
            Expect.isTrue
                (rectClipRects rendered.Scene |> List.exists (fun r -> rectClose r svBox))
                "the viewport content is wrapped in a ClipNode to the scroll-viewer box (clipped, not spilled)"

            // a scroll offset is exposed and reflects real overflow (content taller than the viewport)
            match Control.scrollViewport rendered "sv" with
            | Some vp ->
                Expect.isTrue (vp.ContentHeight > vp.Viewport.Height) "content is taller than the viewport (scrollable)"
                Expect.isTrue (vp.MaxVerticalOffset > 0.0) "a positive max scroll offset is exposed"
                Expect.equal vp.Offset 0.0 "the viewport rests at the top (offset 0)"
            | None -> failtest "scrollViewport should resolve the keyed scroll-viewer"

            // a scroll affordance is painted: a track plus a SHORTER thumb at the right edge (the thumb
            // is shorter than the track precisely because the content overflows).
            let barW = 10.0
            let trackX = svBox.X + svBox.Width - barW
            let barRects =
                TestAssertions.drawnBounds rendered.Scene
                |> List.filter (fun (r: Rect) -> abs (r.X - trackX) < 1.0 && abs (r.Width - barW) < 1.0)
            Expect.isGreaterThanOrEqual barRects.Length 2 "the affordance has a track and a thumb"
            let heights = barRects |> List.map (fun (r: Rect) -> r.Height)
            Expect.isLessThan (List.min heights) (List.max heights) "the thumb is shorter than the track (content overflows)"
        }

        // ---- T019: a page taller than its region is bounded (nothing escapes the viewport) ----
        test "a page taller than its content region is bounded by the viewport clip (nothing spills)" {
            let rendered = Control.renderTree theme size (tallScrollViewer ())
            let svBox = rendered.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd

            // a row near the end is laid out BELOW the viewport bottom (genuine overflow to bound)
            let lastRowBottom =
                rendered.Bounds
                |> List.map (fun (_, (r: Rect)) -> r.Y + r.Height)
                |> List.max
            Expect.isGreaterThan lastRowBottom (svBox.Y + svBox.Height) "content extends past the viewport bottom"

            // and that overflow is bounded: the content lives inside the viewport clip, so nothing paints
            // outside the region.
            Expect.isTrue
                (rectClipRects rendered.Scene |> List.exists (fun r -> rectClose r svBox))
                "the page content is confined to the viewport region by the clip"
        }
    ]

// ---- US2: deferred overlay pass (FR-004/005/006/007) ----

// Every text occurrence with the stack of RectClip rects enclosing it (escape-clip oracle).
let rec private textClipPaths (clips: Rect list) (s: Scene) : (string * Rect list) list =
    s.Nodes |> List.collect (textClipPathsNode clips)

and private textClipPathsNode (clips: Rect list) (n: SceneNode) : (string * Rect list) list =
    match n with
    | Text(_, t, _) -> [ t, clips ]
    | SizedText(_, t, _, _) -> [ t, clips ]
    | TextRun r -> [ r.Text, clips ]
    | ClipNode(RectClip r, inner) -> textClipPaths (clips @ [ r ]) inner
    | ClipNode(_, inner) -> textClipPaths clips inner
    | Group scenes -> scenes |> List.collect (textClipPaths clips)
    | Translate(_, inner) -> textClipPaths clips inner
    | ColorSpaceNode(_, inner) -> textClipPaths clips inner
    | PerspectiveNode(_, inner) -> textClipPaths clips inner
    | PictureNode p -> textClipPaths clips p.Scene
    | CachedSubtree b -> textClipPaths clips b.Scene
    | _ -> []

let private clipsFor (text: string) (s: Scene) : Rect list =
    textClipPaths [] s |> List.filter (fun (t, _) -> t.Contains text) |> List.collect snd

// A small clipping container holding an in-flow child plus an overlay child that must escape the clip.
let private overlayTree () : Control<Msg> =
    let wrap =
        Stack.create
            [ Attr.width 100.0
              Attr.height 60.0
              Stack.children
                  [ TextBlock.create [ TextBlock.text "INFLOWSIB" ]
                    (Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "OVERLAYITEM" ]) ]
                     |> Control.withKey "ov") ] ]

    Stack.create [ Stack.children [ wrap ] ]

[<Tests>]
let overlayTests =
    testList "Feature137 overlay pass (US2, FR-004/005/006/007)" [

        // ---- T010: z-order + escape ancestor clip ----
        test "an overlay paints above its in-flow sibling and escapes its ancestor container clip" {
            let rendered = Control.renderTree theme size (overlayTree ())
            let wrapBox = rendered.Bounds |> List.find (fun (id, _) -> id = "0.0") |> snd

            // z-order: the overlay item paints AFTER the in-flow sibling (z-top, painted last)
            let glyphs = TestAssertions.renderedGlyphs rendered.Scene
            let idxOf (needle: string) = glyphs |> List.findIndex (fun (g: string) -> g.Contains needle)
            Expect.isGreaterThan (idxOf "OVERLAYITEM") (idxOf "INFLOWSIB") "the overlay item paints after (above) the in-flow sibling"

            // the in-flow sibling IS confined by the wrap container clip ...
            Expect.isTrue
                (clipsFor "INFLOWSIB" rendered.Scene |> List.exists (fun r -> rectClose r wrapBox))
                "the in-flow sibling is clipped to its container box"
            // ... but the overlay item ESCAPES that ancestor clip (it is pulled out of the in-flow hierarchy)
            Expect.isFalse
                (clipsFor "OVERLAYITEM" rendered.Scene |> List.exists (fun r -> rectClose r wrapBox))
                "the overlay item is NOT wrapped by its ancestor container clip"
        }

        // ---- T011: hit-test consults the overlay group first ----
        test "hitTest/nearestAuthored prefer the overlay (its bounds are consulted before in-flow)" {
            let rendered = Control.renderTree theme size (overlayTree ())
            let ids = rendered.Bounds |> List.map fst

            // the overlay surface (and its descendants) are ordered AFTER in-flow, so the topmost-wins
            // reverse-scan returns the overlay at any shared point.
            let idxOf id = ids |> List.findIndex (fun i -> i = id)
            Expect.isGreaterThan (idxOf "ov") (idxOf "0.0.0") "the overlay surface is consulted after the in-flow sibling"

            // a click inside the overlay resolves to the overlay's authored id
            let ovBox = rendered.Bounds |> List.find (fun (id, _) -> id = "ov") |> snd
            let cx, cy = ovBox.X + ovBox.Width / 2.0, ovBox.Y + ovBox.Height / 2.0

            match Control.hitTest rendered cx cy with
            | Some hit -> Expect.equal (Control.nearestAuthored rendered hit) (Some "ov") "the hit resolves to the overlay surface"
            | None -> failtest "a point inside the overlay should hit something"
        }

        // ---- T012: parity — full ≡ retained with an overlay; empty overlay group ⇒ byte-identical ----
        test "full ≡ retained with an overlay present; the in-flow pass is unchanged without one" {
            let withOverlay = overlayTree ()
            let full = (Control.renderTree theme size withOverlay).Scene
            let inited = RetainedRender.init theme size withOverlay
            let stepped = (RetainedRender.step theme size inited.Retained withOverlay).Render.Scene
            Expect.equal inited.Render.Scene full "retained init ≡ full renderTree with an overlay present"
            Expect.equal stepped full "retained step ≡ full renderTree with an overlay present"

            // empty overlay group: a tree with NO overlay is byte-identical across full and retained, and
            // the overlay machinery introduces no trailing artifact (this is the pre-overlay in-flow pass).
            let plain: Control<Msg> =
                Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "a" ]; TextBlock.create [ TextBlock.text "b" ] ] ]
            let plainFull = (Control.renderTree theme size plain).Scene
            let plainRetained = (RetainedRender.init theme size plain).Render.Scene
            Expect.equal plainRetained plainFull "an overlay-free page renders byte-identically (empty overlay group)"
        }
    ]
