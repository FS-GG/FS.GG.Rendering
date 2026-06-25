module Feature191CanvasTests

// Feature 191 (US1, T010–T013a) — the embedded `canvas` control kind paints an application-supplied
// immutable Scene into its laid-out box (box-origin local coordinates, clipped), honours explicit
// width/height with siblings laying out around it, fingerprints content-sensitively and renders
// byte-identically for an identical model, falls back to a placeholder when no scene is supplied,
// degrades safely on a zero-area box, leaves CustomControl untouched, and applies a viewport transform
// to the CONTENT only. Render-only / deterministic — exercised through the real `Control.renderTree`
// paint path (paintLeaf), no live GL.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 400 }

let private red = Colors.rgb 220uy 40uy 40uy
let private white = Colors.white

// Author content, in canvas-local coordinates (origin top-left).
let private authorScene: Scene =
    { Nodes = [ Rectangle((0.0, 0.0, 40.0, 30.0), red); Circle({ X = 20.0; Y = 15.0 }, 10.0, white) ] }

let private canvasCtl (extra: Attr<int> list) (scene: Scene option) : Control<int> =
    let sceneAttrs = scene |> Option.map (fun s -> [ Canvas.scene s ]) |> Option.defaultValue []
    // Build through Canvas.create so accessibility metadata is inferred (canvas is an interactive
    // kind); override the Key so the test can locate its laid-out box by id.
    { Canvas.create ([ Attr.width 120.0; Attr.height 80.0 ] @ sceneAttrs @ extra) with Key = Some "cv" }

let private treeWith (canvas: Control<int>) : Control<int> =
    // Built through the constructors so accessibility metadata is inferred for every node.
    let chrome = { TextBlock.create [ Attr.text "chrome" ] with Key = Some "chrome" }
    Stack.create [ Attr.children [ canvas; chrome ] ]

let private render (canvas: Control<int>) = Control.renderTree theme size (treeWith canvas)

let private boundsOf (id: ControlId) (r: ControlRenderResult<int>) : Rect =
    r.Bounds |> List.find (fun (cid, _) -> cid = id) |> snd

let private approx a b = abs (a - b) < 0.6
let private approxRect (r: Rect) (b: Rect) = approx r.X b.X && approx r.Y b.Y && approx r.Width b.Width && approx r.Height b.Height

// Descend through every transparent grouping node, collecting all leaf+container nodes.
let rec private descend (s: Scene) : SceneNode list =
    s.Nodes
    |> List.collect (fun n ->
        n
        :: (match n with
            | Group ss -> ss |> List.collect descend
            | ClipNode(_, inner)
            | Translate(_, inner)
            | PerspectiveNode(_, inner)
            | ColorSpaceNode(_, inner) -> descend inner
            | CachedSubtree b -> descend b.Scene
            | PictureNode p -> descend p.Scene
            | _ -> []))

// The sub-scene painted under the canvas's box clip (the canvas content), if any.
let rec private clipBody (box: Rect) (s: Scene) : Scene option =
    s.Nodes
    |> List.tryPick (fun n ->
        match n with
        | ClipNode(RectClip r, inner) when approxRect r box -> Some inner
        | ClipNode(_, inner)
        | Translate(_, inner)
        | PerspectiveNode(_, inner)
        | ColorSpaceNode(_, inner) -> clipBody box inner
        | Group ss -> ss |> List.tryPick (clipBody box)
        | CachedSubtree b -> clipBody box b.Scene
        | PictureNode p -> clipBody box p.Scene
        | _ -> None)

let private hasAuthorRect (nodes: SceneNode list) =
    nodes |> List.exists (function Rectangle((0.0, 0.0, 40.0, 30.0), _) -> true | _ -> false)

// The canvas must never raise an authoring-blocking (Error) diagnostic; advisory Info/Warning
// (e.g. MissingStableKey on the bare test stack) are orthogonal to the canvas behaviour under test.
let private errors (r: ControlRenderResult<int>) =
    r.Diagnostics |> List.filter (fun d -> d.Severity = ControlDiagnosticSeverity.Error)

[<Tests>]
let tests =
    testList "Feature 191 embedded canvas (US1)" [

        // T010 — golden paint + clip + box-origin translation; explicit size; siblings lay out around it.
        test "canvas content paints at the box origin, clipped to the box; explicit size is honoured" {
            let r = render (canvasCtl [] (Some authorScene))
            Expect.isEmpty (errors r) "a painted canvas raises no Error diagnostics"

            let box = boundsOf "cv" r
            Expect.isTrue (approx box.Width 120.0 && approx box.Height 80.0) "explicit width/height size the control box"

            match clipBody box r.Scene with
            | Some body ->
                let nodes = descend body
                Expect.isTrue (hasAuthorRect nodes) "the author's local-origin rectangle paints inside the box clip"
                Expect.isTrue
                    (nodes |> List.exists (function Circle(_, 10.0, _) -> true | _ -> false))
                    "the author's circle paints inside the box clip"
                Expect.isTrue
                    (descend r.Scene |> List.exists (function Translate((dx, dy), _) -> approx dx box.X && approx dy box.Y | _ -> false))
                    "content is translated to the box origin"
            | None -> failtest "expected the canvas content clipped to its box"

            // A sibling lays out normally (its own non-empty box, distinct from the canvas).
            let chrome = boundsOf "chrome" r
            Expect.isTrue (chrome.Height > 0.0) "the sibling chrome lays out with a real box"
            Expect.notEqual (chrome.Y) (box.Y) "the sibling does not overlap the canvas origin"
        }

        // T011 — fingerprint sensitivity + byte-identity determinism.
        test "an identical model renders a byte-identical Scene + fingerprint; a scene change flips it" {
            let r1 = render (canvasCtl [] (Some authorScene))
            let r2 = render (canvasCtl [] (Some authorScene))
            Expect.equal r1.Scene r2.Scene "same model ⇒ byte-identical emitted Scene"
            Expect.equal (RetainedRender.hashScene [ r1.Scene ]) (RetainedRender.hashScene [ r2.Scene ]) "same model ⇒ identical fingerprint"

            let changed = { Nodes = [ Rectangle((0.0, 0.0, 40.0, 30.0), Colors.rgb 10uy 200uy 10uy) ] } // colour changed
            let r3 = render (canvasCtl [] (Some changed))
            Expect.notEqual
                (RetainedRender.hashScene [ r1.Scene ])
                (RetainedRender.hashScene [ r3.Scene ])
                "a render-affecting scene change flips the fingerprint"
        }

        // T012 — placeholder for a missing scene; safe failure for a zero-area box.
        test "a canvas with no scene paints a placeholder; a zero-area canvas paints nothing and does not error" {
            let placeholder = render (canvasCtl [] None)
            Expect.isEmpty (errors placeholder) "the placeholder raises no Error diagnostics"
            let withScene = render (canvasCtl [] (Some authorScene))
            Expect.notEqual placeholder.Scene withScene.Scene "the placeholder differs from painted content"
            Expect.isFalse (hasAuthorRect (descend placeholder.Scene)) "no author content paints when no scene is supplied"

            // A zero-area / unmeasured box paints nothing and does not error. Exercised at the paint
            // unit directly: Yoga clamps explicit 0 sizes to minimums, so a genuinely zero box only
            // arises from an unmeasured node — paintLeaf must guard it (FR-013).
            let zeroBox: Rect = { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
            let painted = NodeAssembly.paintLeaf theme zeroBox (canvasCtl [] (Some authorScene))
            Expect.isEmpty painted "a zero-area canvas box paints nothing (no crash, no content)"
        }

        // T013 — CustomControl placeholder behaviour is unchanged by the new kind.
        test "the existing custom-control placeholder behaviour is unchanged (FR-001)" {
            let custom: Control<int> =
                { Control.create "custom-control" [ Attr.width 100.0; Attr.height 40.0; Attr.text "widget" ] with Key = Some "cc" }
            let r = Control.renderTree theme size custom
            Expect.isEmpty (errors r) "custom-control still renders its placeholder without Error diagnostics"
            Expect.isFalse (hasAuthorRect (descend r.Scene)) "custom-control does not paint canvas author content"
        }

        // T013a — viewport transforms the CONTENT only; the laid-out box and clip are unchanged (FR-016).
        test "a viewport pans/zooms the content only — the box size and clip are unchanged" {
            let vp: PerspectiveTransform =
                { M11 = 1.5; M12 = 0.0; M13 = 0.0; M21 = 0.0; M22 = 1.5; M23 = 0.0; M31 = 0.0; M32 = 0.0; M33 = 1.0 }
            let plain = render (canvasCtl [] (Some authorScene))
            let zoomed = render (canvasCtl [ Canvas.viewport vp ] (Some authorScene))

            // Same laid-out box (viewport does not touch layout size or the hit-test box).
            Expect.equal (boundsOf "cv" zoomed) (boundsOf "cv" plain) "the viewport leaves the laid-out box unchanged"

            let box = boundsOf "cv" zoomed
            match clipBody box zoomed.Scene with
            | Some body ->
                Expect.isTrue
                    (descend body |> List.exists (function PerspectiveNode _ -> true | _ -> false))
                    "the viewport transform wraps the content (content-only)"
                Expect.isTrue (hasAuthorRect (descend body)) "the content still paints under the box clip"
            | None -> failtest "expected the zoomed canvas content clipped to its box"

            // Absent viewport ⇒ no content-level perspective transform.
            match clipBody (boundsOf "cv" plain) plain.Scene with
            | Some body -> Expect.isFalse (descend body |> List.exists (function PerspectiveNode _ -> true | _ -> false)) "no viewport ⇒ no extra transform"
            | None -> failtest "expected the plain canvas content clipped to its box"
        }
    ]

// ─────────────────────────────── User Story 2 ───────────────────────────────
// A volatile canvas repainting every frame leaves surrounding cached chrome stable; raw pointer/key
// input reaches the bound model. Cache-isolation is exercised over the real wired RetainedRender.step
// (mirroring Feature116PictureCacheTests); chrome is data-grid-rows (the cacheable picture kind).

let private dgRow (key: string) (content: string) : Control<int> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes = [ Attr.width 200.0; Attr.height 24.0 ]
      Children = []
      Content = Some content
      Accessibility = None }

let private volatileCanvas (scene: Scene) : Control<int> =
    { Canvas.create [ Attr.width 120.0; Attr.height 80.0; Canvas.volatile'; Canvas.scene scene ] with Key = Some "cv" }

let private nonVolatileCanvas (scene: Scene) : Control<int> =
    { Canvas.create [ Attr.width 120.0; Attr.height 80.0; Canvas.scene scene ] with Key = Some "cv" }

let private chromeTreeWith (canvas: Control<int>) (r0: string) : Control<int> =
    { Kind = "stack"; Key = None; Attributes = []; Children = [ canvas; dgRow "r0" r0; dgRow "r1" "one" ]; Content = None; Accessibility = None }

let private chromeTree (canvas: Control<int>) : Control<int> = chromeTreeWith canvas "zero"

let private frameScene (n: int) : Scene = { Nodes = [ Rectangle((0.0, 0.0, float n, 10.0), red) ] }
let private rinit (c: Control<int>) = (RetainedRender.init theme size c).Retained

// Normalize away the transparent grouping/replay-boundary layers (Group / CachedSubtree) that a
// reuse-stable frame adds, so byte-identity is asserted on the leaf paint stream (cf. Feature116).
let rec private strip (s: Scene) : Scene =
    { Nodes =
        s.Nodes
        |> List.collect (fun n ->
            match n with
            | CachedSubtree b -> (strip b.Scene).Nodes
            | Group ss -> ss |> List.collect (fun s2 -> (strip s2).Nodes)
            | ClipNode(c, inner) -> [ ClipNode(c, strip inner) ]
            | Translate(o, inner) -> [ Translate(o, strip inner) ]
            | PerspectiveNode(t, inner) -> [ PerspectiveNode(t, strip inner) ]
            | ColorSpaceNode(cs, inner) -> [ ColorSpaceNode(cs, strip inner) ]
            | PictureNode p -> [ PictureNode { p with Scene = strip p.Scene } ]
            | other -> [ other ]) }

[<Tests>]
let us2Tests =
    testList "Feature 191 embedded canvas (US2)" [

        // T020 — cache isolation: a volatile canvas repainting every frame leaves chrome cache-stable.
        test "a volatile canvas repainting every frame leaves chrome at cache hits (0 chrome repaints, SC-003)" {
            let r0 = rinit (chromeTree (volatileCanvas (frameScene 1)))
            let s = RetainedRender.step theme size r0 (chromeTree (volatileCanvas (frameScene 2)))
            Expect.equal s.WorkReduction.PictureCacheHits 2 "both chrome rows stay picture-cache hits while the canvas repaints"
            Expect.equal s.WorkReduction.PictureCacheMisses 0 "no chrome row is repainted in a canvas-only frame"
        }

        // T021 — an unchanged (non-volatile) canvas scene is recognised as a cache hit (not repainted).
        test "a non-volatile canvas with an identical scene between frames is reused, not repainted (FR-003)" {
            let scene = frameScene 5
            let r0 = rinit (chromeTree (nonVolatileCanvas scene))
            let s = RetainedRender.step theme size r0 (chromeTree (nonVolatileCanvas scene))
            // Chrome stays cached AND the identical-scene canvas adds no picture-cache miss.
            Expect.equal s.WorkReduction.PictureCacheMisses 0 "an identical-scene frame recomputes no cached picture"
            // The reused frame is byte-identical (leaf stream) to a fresh full paint — the canvas was
            // reused, not dropped or re-emitted differently.
            let fresh = Control.renderTree theme size (chromeTree (nonVolatileCanvas scene))
            Expect.equal (strip s.Render.Scene) (strip fresh.Scene) "a reused identical frame is byte-identical to a fresh paint"
        }

        // The volatile' marker is observable as always-dirty: when a sibling changes (so the parent
        // re-diffs and visits the canvas node), a volatile canvas repaints though its own scene is
        // unchanged, whereas a non-volatile canvas with the same scene is reused.
        test "volatile' is always-dirty: repaints when a sibling changes though its own scene is unchanged (D4/FR-004)" {
            let scene = frameScene 7
            let nv =
                RetainedRender.step theme size
                    (rinit (chromeTreeWith (nonVolatileCanvas scene) "zero"))
                    (chromeTreeWith (nonVolatileCanvas scene) "ZERO-CHANGED")
            let v =
                RetainedRender.step theme size
                    (rinit (chromeTreeWith (volatileCanvas scene) "zero"))
                    (chromeTreeWith (volatileCanvas scene) "ZERO-CHANGED")
            Expect.isTrue
                (v.WorkReduction.RepaintedNodeCount > nv.WorkReduction.RepaintedNodeCount)
                "a volatile canvas repaints when a sibling changes; a non-volatile one with the same scene is reused"
        }
    ]
