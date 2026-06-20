module Audit_Fingerprint

// AUDIT (feature 006, T004 sanity + T022) — the structural scene fingerprint (`RetainedRender.hashScene`).
//   * DETERMINISM (FR-007): identical scenes hash identically across repeated runs.
//   * COLLISION PROBE (FR-007): over single-field render-affecting diffs (geometry, color, text, opacity,
//     transform) the fingerprint MUST DIFFER — enumerated mutations + an FsCheck property over random
//     geometry diffs.
//   * DISCRIMINATING POWER both directions: a no-op rebuild of an identical scene hashes EQUAL (so the
//     `notEqual` collision assertions are meaningful, not a hash that is merely always-different).

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private sceneOf (nodes: SceneNode list) : Scene list = [ { Nodes = nodes } ]
let private blue: Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy }
let private red: Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }

[<Tests>]
let tests =
    testList "Audit: Scene fingerprint determinism + collision probe (FR-007)" [

        // ---- T004 scaffold sanity ----
        test "Audit: Fingerprint scaffold reachability — hashScene + counters (T004)" {
            let h = RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue) ])
            Expect.isTrue (h = h) "hashScene seam reachable"
            // a WorkReductionRecord counter is reachable on the wired path
            let theme = Theme.light
            let size: Size = { Width = 320; Height = 240 }
            let view: Control<int> = { Kind = "stack"; Key = None; Attributes = []; Children = []; Content = None; Accessibility = None }
            let s = RetainedRender.step theme size (RetainedRender.init theme size view).Retained view
            Expect.isTrue (s.WorkReduction.DirtyRectCount >= 0) "WorkReductionRecord counter reachable"
        }

        // ---- DETERMINISM ----
        test "Audit: identical scenes hash identically + deterministic across calls (FR-007)" {
            let a = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue); SceneNode.Text((1.0, 2.0), "hello", red) ]
            let b = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue); SceneNode.Text((1.0, 2.0), "hello", red) ]
            Expect.equal (RetainedRender.hashScene a) (RetainedRender.hashScene b) "structurally-equal scenes ⇒ equal fingerprint (the notEqual probes are meaningful)"
            Expect.equal (RetainedRender.hashScene a) (RetainedRender.hashScene a) "deterministic across repeated calls"
        }

        test "Audit: Feature141 assembly-result fingerprints are owner-produced and deterministic" {
            let own = sceneOf [ Rectangle((0.0, 0.0, 12.0, 8.0), blue) ]
            let child = sceneOf [ SceneNode.Text((1.0, 2.0), "child", red) ]
            let control: Control<int> = { Kind = "stack"; Key = Some "owner"; Attributes = []; Children = []; Content = None; Accessibility = None }
            let box = { X = 0.0; Y = 0.0; Width = 120.0; Height = 60.0 }

            let childAssembly =
                ControlInternals.assembleCurrentNode control (Some box) child []

            let assembledA = ControlInternals.assembleCurrentNode control (Some box) own [ childAssembly ]
            let assembledB = ControlInternals.assembleCurrentNode control (Some box) own [ childAssembly ]

            Expect.equal assembledA.Fingerprint assembledB.Fingerprint "equivalent owner assemblies produce stable fingerprints"
            Expect.equal assembledA.InFlowFingerprint assembledB.InFlowFingerprint "equivalent owner assemblies produce stable in-flow fingerprints"
            Expect.equal assembledA.OverlayFingerprint assembledB.OverlayFingerprint "equivalent owner assemblies produce stable overlay fingerprints"
            Expect.equal (RetainedRender.hashScene assembledA.InFlowScene) (ControlInternals.hashScene assembledA.InFlowScene) "RetainedRender.hashScene aliases the owner-side fingerprint"
            Expect.equal assembledA.ChildContributions.Length 1 "assembly metadata records child contribution count"
            Expect.equal assembledA.ChildContributions.Head.InFlowFingerprint childAssembly.InFlowFingerprint "child contribution stores child in-flow fingerprint"
            Expect.equal assembledA.ChildContributions.Head.OverlayFingerprint childAssembly.OverlayFingerprint "child contribution stores child overlay fingerprint"
        }

        // ---- COLLISION PROBE: enumerated single-field render-affecting mutations ----
        test "Audit: COLLISION PROBE — any single render-affecting change flips the fingerprint (FR-007)" {
            let baseScene = sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), blue) ]
            let h0 = RetainedRender.hashScene baseScene
            // geometry
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 11.0, 10.0), blue) ])) "geometry change flips the fingerprint"
            // color
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), red) ])) "color change flips the fingerprint"
            // text content
            let t0 = RetainedRender.hashScene (sceneOf [ SceneNode.Text((0.0, 0.0), "a", red) ])
            Expect.notEqual t0 (RetainedRender.hashScene (sceneOf [ SceneNode.Text((0.0, 0.0), "b", red) ])) "text change flips the fingerprint"
            // opacity (alpha)
            let cFade = { blue with Alpha = 128uy }
            Expect.notEqual h0 (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, 10.0, 10.0), cFade) ])) "opacity change flips the fingerprint"
            // transform (translate)
            let inner = { Nodes = [ Rectangle((0.0, 0.0, 10.0, 10.0), blue) ] }
            Expect.notEqual
                (RetainedRender.hashScene (sceneOf [ Translate((0.0, 0.0), inner) ]))
                (RetainedRender.hashScene (sceneOf [ Translate((5.0, 0.0), inner) ]))
                "transform change flips the fingerprint"
        }

        // ---- COLLISION PROBE: FsCheck over random geometry diffs ----
        test "Audit: COLLISION PROBE (FsCheck) — distinct rectangle widths never collide over >=500 cases (FR-007)" {
            let widthGen = Gen.choose (1, 400) |> Gen.map float
            let pairGen = gen { let! a = widthGen in let! b = widthGen in return (a, b) }
            let noCollision (a: float, b: float) =
                (a <> b)
                ==> lazy (RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, a, 10.0), blue) ])
                          <> RetainedRender.hashScene (sceneOf [ Rectangle((0.0, 0.0, b, 10.0), blue) ]))
            let config = Config.QuickThrowOnFailure.WithMaxTest 500
            Check.One(config, Prop.forAll (Arb.fromGen pairGen) noCollision)
        }
    ]
