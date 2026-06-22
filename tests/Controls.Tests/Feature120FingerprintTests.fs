module Feature120FingerprintTests

// Feature 120 (US3, FR-008/FR-010, SC-005) — the collision-resistant structural fingerprint
// (`RetainedRender.hashScene`) replacing the feature-116 truncation-prone `sprintf "%A"` digest, plus
// the `DirtyArea` union computation (US4, FR-015, SC-007). Reached over the REAL internal surface via
// `[<assembly: InternalsVisibleTo("Controls.Tests")>]`. Pure/deterministic — no window.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private sceneOf (nodes: SceneNode list) : Scene list = [ { Nodes = nodes } ]

let private rect x y w h : Rect = { X = x; Y = y; Width = w; Height = h }

let private blue: Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy }
let private red: Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }

[<Tests>]
let tests =
    testList "Feature 120 structural fingerprint + damage union (US3/US4, FR-008/010/015)" [

        test "identical scenes hash identically; the fingerprint is deterministic (FR-008)" {
            let a = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue); SceneNode.Text((1.0, 2.0), "hello", red) ]
            let b = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue); SceneNode.Text((1.0, 2.0), "hello", red) ]
            Expect.equal (RetainedRender.hashScene a) (RetainedRender.hashScene b) "equal scenes ⇒ equal fingerprint"
            Expect.equal (RetainedRender.hashScene a) (RetainedRender.hashScene a) "deterministic across calls"
        }

        test "any render-affecting change flips the fingerprint (FR-008/FR-010)" {
            let baseScene = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue) ]
            let h0 = RetainedRender.hashScene baseScene

            // geometry
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 11.0, 10.0), blue) ])) "geometry change misses"
            // color
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), red) ])) "color change misses"
            // text content
            let t0 = RetainedRender.hashScene (sceneOf [ SceneNode.Text((0.0, 0.0), "a", red) ])
            Expect.notEqual t0 (RetainedRender.hashScene (sceneOf [ SceneNode.Text((0.0, 0.0), "b", red) ])) "text change misses"
            // opacity (alpha) of fill
            let cFade = { blue with Alpha = 128uy }
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), cFade) ])) "opacity change misses"
            // transform (translate)
            let inner = { Nodes = [ Rectangle((0.0, 0.0, 10.0, 10.0), blue) ] }
            Expect.notEqual
                (RetainedRender.hashScene (sceneOf [ Translate((0.0, 0.0), inner) ]))
                (RetainedRender.hashScene (sceneOf [ Translate((5.0, 0.0), inner) ]))
                "transform change misses"
        }

        test "the key proof: a long-list difference the truncating %A digest collides on yields a different fingerprint (SC-005)" {
            // 200 chart values differing only at index 150 — well past the ~100-element %A truncation
            // point, so `sprintf "%A"` over the whole list produces the SAME (truncated) string for both,
            // a false hit. The structural fold walks every element, so the fingerprints differ.
            let values i = [ for k in 0 .. 199 -> if k = 150 then float i else float k ]
            let sceneA = sceneOf [ SceneNode.Chart(values 1) ]
            let sceneB = sceneOf [ SceneNode.Chart(values 2) ]

            // demonstrate the collision the old key would have suffered
            let truncatedA = sprintf "%A" sceneA
            let truncatedB = sprintf "%A" sceneB
            Expect.equal truncatedA truncatedB "the superseded `%A` digest TRUNCATES and collides on the two long lists"

            // the new fingerprint does not
            Expect.notEqual (RetainedRender.hashScene sceneA) (RetainedRender.hashScene sceneB) "hashScene distinguishes the structural difference the truncating key missed (SC-005)"
        }

        test "DirtyArea is the UNION of overlapping damage rects, never the sum, never over the frame (FR-015/SC-007)" {
            // two 100x100 boxes overlapping in a 50x50 region: sum = 20000, union = 17500.
            let a = rect 0.0 0.0 100.0 100.0
            let b = rect 50.0 50.0 100.0 100.0
            let frameArea = 1000 * 1000
            let union = CompositorPolicy.unionArea [ a; b ] frameArea
            Expect.equal union 17500 "union area counts the overlap once (not the 20000 sum)"

            // disjoint boxes: union = sum
            let c = rect 0.0 0.0 10.0 10.0
            let d = rect 100.0 100.0 10.0 10.0
            Expect.equal (CompositorPolicy.unionArea [ c; d ] frameArea) 200 "disjoint boxes union = sum"

            // clamp to frame area
            let huge = rect 0.0 0.0 5000.0 5000.0
            Expect.equal (CompositorPolicy.unionArea [ huge ] frameArea) frameArea "union never exceeds the frame area"

            // empty
            Expect.equal (CompositorPolicy.unionArea [] frameArea) 0 "no damage ⇒ 0"
        }
    ]
