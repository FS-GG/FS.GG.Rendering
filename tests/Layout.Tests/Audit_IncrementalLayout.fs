module Audit_IncrementalLayout

// Audit of the imported incremental-layout mechanism (feature 006-verify-imported-mechanisms).
// Exercises the REAL `FS.GG.UI.Layout.Layout` seam (`evaluate` / `evaluateIncremental`) with
// proven discriminating power. Every test name is prefixed "Audit: " so the suite can be run via
// `--filter Audit`. A legitimately-red assertion here is a FINDING, never weakened to pass.

open Expecto
open FsCheck
open FS.GG.UI.Layout

// --- shared fixtures (mirrors Feature097IncrementalTests construction) ------------------------

let private available =
    { Width = 400.0
      WidthMode = Exactly
      Height = 300.0
      HeightMode = Exactly }

let private leaf id w h : LayoutNode =
    { Defaults.layoutNode id with
        Intent = { Defaults.layoutIntent with Size = { Width = Some w; Height = Some h } } }

/// A container with an explicit (content-independent) Size on both axes — a fixed-size boundary
/// that ABSORBS descendant size changes (dirt does not climb past it).
let private fixedBox id w h children : LayoutNode =
    { Defaults.layoutNode id with
        Intent = { Defaults.layoutIntent with Size = { Width = Some w; Height = Some h } }
        Children = children }

/// A content-sized container (no explicit Size) — NOT a boundary; dirt climbs through it.
let private autoBox id children : LayoutNode =
    { Defaults.layoutNode id with Children = children }

let private boundsMap (r: LayoutResult) =
    r.Bounds |> List.map (fun b -> b.NodeId, b) |> Map.ofList

let rec private setSize (id: LayoutNodeId) (w: float) (h: float) (n: LayoutNode) : LayoutNode =
    let n =
        if n.Id = id then
            { n with Intent = { n.Intent with Size = { Width = Some w; Height = Some h } } }
        else
            n
    { n with Children = n.Children |> List.map (setSize id w h) }

// --- T005: scaffold sanity --------------------------------------------------------------------

[<Tests>]
let scaffold =
    testList "Audit IncrementalLayout scaffold" [
        test "Audit: scaffold — evaluate/evaluateIncremental reachable and invalidated count accessible (T005)" {
            let frame = autoBox "0" [ fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" 50.0 20.0 ] ]
            // Both seam functions must be reachable against the real Layout module.
            let full = Layout.evaluate available frame
            let inc = Layout.evaluateIncremental full [] available frame
            // Touch the re-measured/invalidated count field on a LayoutResult (the audit metric).
            let baselineCount = List.length full.Invalidated
            let incCount = List.length inc.Invalidated
            Expect.isGreaterThanOrEqual baselineCount 0 "Invalidated count is accessible on a full evaluate"
            Expect.isGreaterThanOrEqual incCount 0 "Invalidated count is accessible on an incremental evaluate"
            Expect.isNonEmpty full.Bounds "evaluate produced geometry"
        }
    ]

// --- T024: US2 equivalence (with discriminating power) ----------------------------------------

[<Tests>]
let equivalence =
    testList "Audit IncrementalLayout equivalence" [
        test "Audit: equivalence — incremental == full across constructed change sets, with discriminating proof (T024, FR-006/SC-003)" {
            // A boundary-bearing tree plus a content-sized chain, so both reuse paths are covered.
            let tree w0 w1 w2 =
                autoBox "0" [
                    fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" w0 20.0; leaf "0.0.1" 50.0 20.0 ]
                    autoBox "0.1" [ leaf "0.1.0" w1 25.0 ]
                    leaf "0.2" w2 30.0 ]

            // Several cumulative change sets carried forward through the incremental cache.
            let edits =
                [ "0.0.0", 70.0
                  "0.1.0", 90.0
                  "0.2", 65.0
                  "0.0.0", 35.0 ]

            let mutable current = tree 50.0 40.0 55.0
            let mutable previous = Layout.evaluate available current
            for (id, w) in edits do
                current <- setSize id w 20.0 current
                let inc = Layout.evaluateIncremental previous [ id ] available current
                let full = Layout.evaluate available current
                Expect.equal (boundsMap inc) (boundsMap full) $"incremental == full after editing {id}"
                previous <- inc

            // ---- DISCRIMINATING POWER -------------------------------------------------------
            // The equivalence assertion above must have teeth: a deliberately-WRONG incremental
            // call (omit the genuinely-changed node id from changedNodeIds) MUST diverge from the
            // full evaluate. If the wrong variant matched too, the equivalence check would be
            // vacuous. We prove the wrong variant is RED and the right variant is GREEN on the
            // SAME final tree.
            let startTree = tree 50.0 40.0 55.0
            let prev = Layout.evaluate available startTree
            // Genuinely change a content-sized leaf (its bounds must move), then:
            let changedTree = setSize "0.1.0" 130.0 60.0 startTree
            let full = Layout.evaluate available changedTree
            let rightInc = Layout.evaluateIncremental prev [ "0.1.0" ] available changedTree
            let wrongInc = Layout.evaluateIncremental prev [] available changedTree // omit the changed id

            let rightMatches = boundsMap rightInc = boundsMap full
            let wrongDiverges = boundsMap wrongInc <> boundsMap full
            // The discriminating proof: right == full AND wrong <> full.
            let discriminatingProof = rightMatches && wrongDiverges
            eprintfn "AUDIT T024 DiscriminatingProof: %b (rightMatches=%b wrongDiverges=%b)" discriminatingProof rightMatches wrongDiverges
            Expect.isTrue rightMatches "correct incremental (changed id supplied) equals full evaluate"
            Expect.isTrue wrongDiverges "wrong incremental (changed id omitted) DIVERGES — proves the equivalence test has teeth"
        }

        test "Audit: equivalence — FsCheck byte-identity over generated edits (T024, FR-006)" {
            let leafIds = [ "0.0.0"; "0.0.1"; "0.1.0"; "0.2" ]
            let baseTree =
                autoBox "0" [
                    fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" 50.0 20.0; leaf "0.0.1" 50.0 20.0 ]
                    autoBox "0.1" [ leaf "0.1.0" 40.0 25.0 ]
                    leaf "0.2" 55.0 30.0 ]

            let prop (i: int) (w: int) (h: int) =
                let id = leafIds.[abs i % leafIds.Length]
                let w = 30.0 + float (abs w % 120)
                let h = 20.0 + float (abs h % 60)
                let prev = Layout.evaluate available baseTree
                let changed = setSize id w h baseTree
                let inc = Layout.evaluateIncremental prev [ id ] available changed
                let full = Layout.evaluate available changed
                boundsMap inc = boundsMap full

            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
        }
    ]

// --- T029: US3 effectiveness (localized change in a large tree) -------------------------------

[<Tests>]
let effectiveness =
    testList "Audit IncrementalLayout effectiveness" [
        test "Audit: effectiveness — localized change in a large tree re-measures a small subset (T029, FR-008/US3-AS1)" {
            // Build a LARGE tree: a content-sized root holding many fixed-size boundary boxes, each
            // wrapping several leaves. A change to one leaf is absorbed by its boundary box, so the
            // honest re-measured set is tiny relative to the full tree of N nodes.
            let boundaryCount = 100
            let leavesPerBox = 9
            let boundaries =
                [ for b in 0 .. boundaryCount - 1 ->
                      let bid = $"0.{b}"
                      let kids = [ for l in 0 .. leavesPerBox - 1 -> leaf $"{bid}.{l}" 40.0 20.0 ]
                      fixedBox bid 200.0 120.0 kids ]
            let root = autoBox "0" boundaries

            let baseline = Layout.evaluate available root
            // N = the full tree node count; a full evaluate must (re)measure all of these.
            let n = baseline.Bounds.Length

            // One localized change deep under a single boundary.
            let changedId = "0.50.4"
            let changed = setSize changedId 70.0 35.0 root
            let inc = Layout.evaluateIncremental baseline [ changedId ] available changed
            let remeasured = List.length inc.Invalidated

            eprintfn "AUDIT T029 Margin: %d/%d remeasured (N=%d)" remeasured n n

            // Sanity: incremental geometry still equals a full evaluate of the changed tree.
            let full = Layout.evaluate available changed
            Expect.equal (boundsMap inc) (boundsMap full) "incremental geometry equals full evaluate on the large tree"
            // The changed node is always part of the honest re-measured set.
            Expect.isTrue (List.contains changedId inc.Invalidated) "changed node is in the re-measured set"

            // EFFECTIVENESS: the re-measured set must be MUCH smaller than the whole tree. If it is
            // not (incremental re-measures everything), that is a FINDING (no-op) recorded by a RED
            // assertion — we do NOT weaken it to pass.
            Expect.isLessThan remeasured (n / 10) $"localized change re-measures a small subset ({remeasured}/{n}), not the whole tree"
        }
    ]
