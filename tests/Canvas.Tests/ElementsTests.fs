module Canvas.Tests.ElementsTests

// Feature 191 (US3, T032, C3/FR-008, SC-007): every Elements combinator is a pure `'props -> Scene` —
// identical props yield a byte-identical (structurally equal) immutable Scene, and they compose.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Canvas

let private red = Colors.rgb 220uy 40uy 40uy
let private paint = Paint.fill red

[<Tests>]
let tests =
    testList "Feature 191 Elements purity + composition (US3, FR-008)" [

        test "rect is pure: identical props ⇒ identical Scene, authored at the local origin" {
            Expect.equal (Elements.rect 10.0 20.0 paint) (Elements.rect 10.0 20.0 paint) "rect is deterministic"
            match (Elements.rect 10.0 20.0 paint).Nodes with
            | [ PaintedRectangle(b, _) ] ->
                Expect.equal (b.X, b.Y) (0.0, 0.0) "rect is authored at origin (0,0)"
                Expect.equal (b.Width, b.Height) (10.0, 20.0) "rect carries its size"
            | other -> failtestf "expected a single PaintedRectangle, got %A" other
        }

        test "circle is pure and centred at the local origin" {
            Expect.equal (Elements.circle 5.0 red) (Elements.circle 5.0 red) "circle is deterministic"
            match (Elements.circle 5.0 red).Nodes with
            | [ Circle(c, r, _) ] ->
                Expect.equal c { X = 0.0; Y = 0.0 } "circle centred at origin"
                Expect.equal r 5.0 "circle radius preserved"
            | other -> failtestf "expected a single Circle, got %A" other
        }

        test "sprite is pure and sized from the local origin" {
            Expect.equal (Elements.sprite "ball.png" 8.0 8.0) (Elements.sprite "ball.png" 8.0 8.0) "sprite is deterministic"
            match (Elements.sprite "ball.png" 8.0 8.0).Nodes with
            | [ Image((x, y, w, h), src) ] ->
                Expect.equal (x, y, w, h) (0.0, 0.0, 8.0, 8.0) "sprite sized at origin"
                Expect.equal src "ball.png" "sprite source preserved"
            | other -> failtestf "expected a single Image, got %A" other
        }

        test "polyline connects its points; degenerate inputs collapse to empty" {
            let pts = [ { X = 0.0; Y = 0.0 }; { X = 10.0; Y = 5.0 }; { X = 20.0; Y = 0.0 } ]
            Expect.equal (Elements.polyline pts paint) (Elements.polyline pts paint) "polyline is deterministic"
            match (Elements.polyline pts paint).Nodes with
            | [ Path(spec, _) ] ->
                Expect.equal (List.length spec.Commands) 3 "one MoveTo + two LineTo"
            | other -> failtestf "expected a single Path, got %A" other
            Expect.equal (Elements.polyline [ { X = 1.0; Y = 1.0 } ] paint) Scene.empty "a single point cannot form a line"
            Expect.equal (Elements.polyline [] paint) Scene.empty "no points ⇒ empty"
        }

        test "at translates a sub-scene and composes additively" {
            let placed = Elements.at 5.0 7.0 (Elements.circle 3.0 red)
            match placed.Nodes with
            | [ Translate((dx, dy), _) ] -> Expect.equal (dx, dy) (5.0, 7.0) "at offsets by (x,y)"
            | other -> failtestf "expected a Translate, got %A" other
            // Composition: at over at nests two translations.
            let nested = Elements.at 5.0 0.0 (Elements.at 3.0 0.0 (Elements.circle 1.0 red))
            match nested.Nodes with
            | [ Translate(_, inner) ] ->
                match inner.Nodes with
                | [ Translate _ ] -> ()
                | other -> failtestf "expected nested Translate, got %A" other
            | other -> failtestf "expected an outer Translate, got %A" other
        }

        test "layer groups sub-scenes in paint order" {
            let scene = Elements.layer [ Elements.rect 2.0 2.0 paint; Elements.at 5.0 5.0 (Elements.circle 1.0 red) ]
            match scene.Nodes with
            | [ Group children ] -> Expect.equal (List.length children) 2 "layer preserves both children in order"
            | other -> failtestf "expected a Group, got %A" other
        }

        test "cached wraps a fragment as a replay boundary keyed on identity + content" {
            let frag = Elements.layer [ Elements.rect 2.0 2.0 paint ]
            // Same key + same content ⇒ identical boundary (a cache hit replays).
            Expect.equal (Elements.cached "hud" frag) (Elements.cached "hud" frag) "cached is deterministic"
            match (Elements.cached "hud" frag).Nodes with
            | [ CachedSubtree b ] -> Expect.equal b.Scene frag "the wrapped scene is transparent (recurses into content)"
            | other -> failtestf "expected a CachedSubtree, got %A" other
            // A render-affecting content change flips the fingerprint (forces a re-record / cache miss).
            let fpOf (s: Scene) = match s.Nodes with | [ CachedSubtree b ] -> b.Fingerprint | _ -> 0UL
            let changed = Elements.layer [ Elements.rect 3.0 2.0 paint ]
            Expect.notEqual (fpOf (Elements.cached "hud" frag)) (fpOf (Elements.cached "hud" changed)) "changed content ⇒ changed fingerprint"
            // A different key under identical content takes a distinct cache slot.
            let idOf (s: Scene) = match s.Nodes with | [ CachedSubtree b ] -> b.CacheId | _ -> 0UL
            Expect.notEqual (idOf (Elements.cached "hud" frag)) (idOf (Elements.cached "bg" frag)) "distinct keys ⇒ distinct cache slots"
        }
    ]
