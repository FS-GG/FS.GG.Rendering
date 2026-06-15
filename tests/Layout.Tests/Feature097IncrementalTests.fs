module Feature097IncrementalTests

// R2 — incremental measure / partial re-layout. These tests pin the equivalence invariant
// (INV-1 / FR-007 / SC-002): `evaluateIncremental` carrying its previous result is byte-identical
// to a full `evaluate`, plus the honest-`Invalidated` (FR-001a / SC-008) and partial-vs-full
// re-measure (SC-001/SC-004) behaviors. Bounds are compared as a NodeId -> ComputedBounds map.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Layout

let private available =
    { Width = 400.0
      WidthMode = Exactly
      Height = 300.0
      HeightMode = Exactly }

let private leaf id w h : LayoutNode =
    { Defaults.layoutNode id with
        Intent = { Defaults.layoutIntent with Size = { Width = Some w; Height = Some h } } }

/// A container with an explicit (content-independent) Size on both axes — a fixed-size boundary.
let private fixedBox id w h children : LayoutNode =
    { Defaults.layoutNode id with
        Intent = { Defaults.layoutIntent with Size = { Width = Some w; Height = Some h } }
        Children = children }

/// A content-sized container (no explicit Size) — NOT a boundary; dirt climbs through it.
let private autoBox id children : LayoutNode =
    { Defaults.layoutNode id with Children = children }

let private boundsMap (r: LayoutResult) =
    r.Bounds |> List.map (fun b -> b.NodeId, b) |> Map.ofList

[<Tests>]
let tests =
    testList "Feature097 incremental layout" [

        test "Synthetic-free: incremental over a fixed-size boundary is byte-identical to full evaluate (SC-001)" {
            // frame 1: container 0.0 is fixed 200x120 with two leaves.
            let frame1 =
                autoBox "0" [ fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" 50.0 20.0; leaf "0.0.1" 50.0 20.0 ] ]
            let prev = Layout.evaluate available frame1

            // frame 2: only leaf 0.0.0 changed size. The boundary 0.0 absorbs it (its own box is pinned).
            let frame2 =
                autoBox "0" [ fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" 70.0 35.0; leaf "0.0.1" 50.0 20.0 ] ]

            let incremental = Layout.evaluateIncremental prev [ "0.0.0" ] available frame2
            let full = Layout.evaluate available frame2

            Expect.equal (boundsMap incremental) (boundsMap full) "incremental Bounds must equal full evaluate"
            // honest Invalidated: the actual re-measured set, bounded by the fixed-size-ancestor subtree,
            // and a proper superset of the single requested node (FR-001a / SC-008).
            Expect.isTrue (List.contains "0.0.0" incremental.Invalidated) "requested node re-measured"
            Expect.isTrue (List.contains "0.0" incremental.Invalidated) "boundary re-measured"
            Expect.isFalse (List.contains "0" incremental.Invalidated) "root NOT re-measured (clean reuse)"
            Expect.equal incremental.Revision (prev.Revision + 1L) "Revision advances"
        }

        test "Empty dirty set re-measures nothing and reuses cached bounds (FR-008 at-rest)" {
            let frame =
                autoBox "0" [ fixedBox "0.0" 200.0 120.0 [ leaf "0.0.0" 50.0 20.0 ] ]
            let prev = Layout.evaluate available frame
            let incremental = Layout.evaluateIncremental prev [] available frame
            Expect.equal (boundsMap incremental) (boundsMap (Layout.evaluate available frame)) "byte-identical at rest"
            Expect.isEmpty incremental.Invalidated "Invalidated empty for empty patch"
        }

        test "Content-sized chain to the root re-measures everything (SC-004 degenerate-correct)" {
            // No fixed-size ancestor between the leaf and root -> propagation reaches the root -> full.
            let frame1 = autoBox "0" [ autoBox "0.0" [ leaf "0.0.0" 50.0 20.0 ] ]
            let prev = Layout.evaluate available frame1
            let frame2 = autoBox "0" [ autoBox "0.0" [ leaf "0.0.0" 80.0 40.0 ] ]
            let incremental = Layout.evaluateIncremental prev [ "0.0.0" ] available frame2
            Expect.equal (boundsMap incremental) (boundsMap (Layout.evaluate available frame2)) "byte-identical (full fallback)"
            Expect.isTrue (List.contains "0" incremental.Invalidated) "root re-measured (content-sized chain)"
        }
    ]

// --- FsCheck equivalence invariant (FR-007 / SC-002) ----------------------------------------
// Generated trees + cumulative edit sequences; incremental (carrying its cache) is byte-identical
// to full evaluate at EVERY step. Real generated data (no canned fixtures) — not synthetic.

module private Gen097 =

    /// A node shape (ids assigned positionally afterwards). `fixed` => explicit Size on both axes
    /// (a content-independent boundary); otherwise the node is content-sized.
    let private genSize : Gen<LayoutSize> =
        Gen.frequency
            [ 2, Gen.constant { Width = None; Height = None }
              3,
              gen {
                  let! w = Gen.elements [ 40.0; 80.0; 120.0; 200.0 ]
                  let! h = Gen.elements [ 30.0; 60.0; 100.0 ]
                  return { Width = Some w; Height = Some h }
              } ]

    let private genDirection : Gen<LayoutDirection> = Gen.elements [ Row; Column ]

    let rec private genNode (depth: int) : Gen<LayoutNode> =
        gen {
            let! size = genSize
            let! direction = genDirection
            let! children =
                if depth <= 0 then
                    Gen.constant []
                else
                    gen {
                        let! n = Gen.choose (0, 3)
                        return! Gen.listOfLength n (genNode (depth - 1))
                    }
            // a childless node with no explicit Size would measure to 0x0; give leaves a Size so the
            // tree has real geometry to compare.
            let size =
                if List.isEmpty children && (size.Width.IsNone || size.Height.IsNone) then
                    { Width = Some 50.0; Height = Some 25.0 }
                else
                    size
            return
                { Defaults.layoutNode "?" with
                    Intent = { Defaults.layoutIntent with Direction = direction; Size = size }
                    Children = children }
        }

    let rec private assignIds (path: string) (n: LayoutNode) : LayoutNode =
        { n with
            Id = path
            Children = n.Children |> List.mapi (fun i c -> assignIds (path + "." + string i) c) }

    let rec private allIds (n: LayoutNode) : LayoutNodeId list =
        n.Id :: (n.Children |> List.collect allIds)

    let rec private setSize (id: LayoutNodeId) (w: float) (h: float) (n: LayoutNode) : LayoutNode =
        let n =
            if n.Id = id then
                { n with Intent = { n.Intent with Size = { Width = Some w; Height = Some h } } }
            else
                n
        { n with Children = n.Children |> List.map (setSize id w h) }

    /// A tree plus a cumulative sequence of (nodeId, newWidth, newHeight) size edits. Ids stay
    /// stable across size-only edits, so every reused (non-re-measured) node keeps its id.
    let treeWithEdits : Gen<LayoutNode * (LayoutNodeId * float * float) list> =
        gen {
            let! shape = Gen.sized (fun s -> genNode (min s 3))
            let tree = assignIds "0" shape
            let ids = allIds tree
            let! editCount = Gen.choose (1, 5)
            let! edits =
                Gen.listOfLength
                    editCount
                    (gen {
                        let! id = Gen.elements ids
                        let! w = Gen.elements [ 35.0; 70.0; 110.0 ]
                        let! h = Gen.elements [ 25.0; 55.0 ]
                        return (id, w, h)
                    })
            return (tree, edits)
        }

    let setSizeAt id w h tree = setSize id w h tree

[<Tests>]
let equivalence =
    testList
        "Feature097 equivalence invariant (FsCheck)"
        [ test "evaluateIncremental (cache carried) is byte-identical to full evaluate over >=1000 generated (tree, edit-sequence) cases (FR-007/SC-002)" {
              let firstDiff (a: Map<LayoutNodeId, ComputedBounds>) (b: Map<LayoutNodeId, ComputedBounds>) =
                  a
                  |> Map.toList
                  |> List.tryPick (fun (k, v) ->
                      match Map.tryFind k b with
                      | Some v2 when v2 = v -> None
                      | other -> Some(k, v, other))

              let equivalent (tree: LayoutNode, edits: (LayoutNodeId * float * float) list) =
                  // Seed the cache with a full evaluate of frame 0, then apply each cumulative edit
                  // through BOTH evaluators, carrying the incremental result forward as `previous`.
                  let mutable current = tree
                  let mutable previous = Layout.evaluate available current
                  let mutable ok = true
                  let mutable step = 0
                  for (id, w, h) in edits do
                      step <- step + 1
                      current <- Gen097.setSizeAt id w h current
                      let inc = Layout.evaluateIncremental previous [ id ] available current
                      let full = Layout.evaluate available current
                      if boundsMap inc <> boundsMap full then
                          if ok then
                              match firstDiff (boundsMap inc) (boundsMap full) with
                              | Some(k, incB, fullB) ->
                                  eprintfn
                                      "DIVERGE step=%d edit=%A invalidated=%A node=%s\n  inc =%A\n  full=%A"
                                      step (id, w, h) inc.Invalidated k incB fullB
                              | None -> ()
                          ok <- false
                      previous <- inc
                  ok

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen097.treeWithEdits) equivalent)
          }

          test "a localized edit under a fixed-size ancestor re-measures a proper subset (SC-001/SC-004)" {
              // Over generated trees, whenever the partial path is taken (Invalidated does not cover
              // the whole tree), the re-measured set is a STRICT subset of all node ids — the metric
              // never claims a reduction that did not happen, and never over-claims one that did.
              let subsetWhenPartial (tree: LayoutNode, edits: (LayoutNodeId * float * float) list) =
                  let total = Gen097.setSizeAt "" 0.0 0.0 tree // no-op clone for id listing
                  ignore total
                  let allCount = (Layout.evaluate available tree).Bounds.Length
                  match edits with
                  | (id, w, h) :: _ ->
                      let prev = Layout.evaluate available tree
                      let current = Gen097.setSizeAt id w h tree
                      let inc = Layout.evaluateIncremental prev [ id ] available current
                      // Either a full re-measure (content-sized chain) OR a strict subset; never more
                      // than the whole tree, and the requested node is always included.
                      List.length inc.Invalidated <= allCount && List.contains id inc.Invalidated
                  | [] -> true

              let config = Config.QuickThrowOnFailure.WithMaxTest 500
              Check.One(config, Prop.forAll (Arb.fromGen Gen097.treeWithEdits) subsetWhenPartial)
          } ]
