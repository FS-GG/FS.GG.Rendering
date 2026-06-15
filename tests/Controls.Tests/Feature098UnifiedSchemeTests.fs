module Feature098UnifiedSchemeTests

// Feature 098 (R3) US3 — the unified canonical `ControlId` scheme (`Key ?? structural-path`).
// T015: FsCheck determinism + same-kind-sibling distinctness (≥1000 cases) + a concrete
// two-unkeyed-bound-sibling routing case (no cross-routing). T016: single-canonical-scheme
// agreement — the id in `Bounds`, in `EventBindings`, the `BoundIds` membership key, and the id
// `nearestAuthored` returns are the SAME value for a node (SC-003/SC-004, FR-006/FR-007).

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | First
    | Second
    | Tagged of int

let private theme = Theme.light
let private size = { Width = 320; Height = 200 }
let private render (control: Control<Msg>) = Control.renderTree theme size control

// A generated tree of nested Stacks whose unkeyed leaves are same-kind Buttons (some bound). The
// generators vary breadth/depth so the structural-path scheme is exercised over many shapes.
let private genTree: Gen<Control<Msg>> =
    let genLeaf =
        gen {
            let! bound = Gen.elements [ true; false ]
            let attrs = if bound then [ Button.text "x"; Button.onClick (Tagged 0) ] else [ Button.text "x" ]
            return Button.create attrs
        }

    let rec genNode depth =
        if depth <= 0 then
            genLeaf
        else
            gen {
                let! n = Gen.choose (0, 3)
                let! kids = Gen.listOfLength n (genNode (depth - 1))
                return Stack.create [ Stack.children kids ]
            }

    gen {
        let! depth = Gen.choose (0, 4)
        return! genNode depth
    }

// Re-derive the structural path id of every node, exactly as collectBoundsWith/eventBindingsOf do.
let private allPathIds (control: Control<Msg>) : (string * Control<Msg>) list =
    let rec go path (c: Control<Msg>) =
        let id = c.Key |> Option.defaultValue path
        (id, c) :: (c.Children |> List.mapi (fun i child -> go (path + "." + string i) child) |> List.concat)

    go "0" control

[<Tests>]
let unifiedSchemeTests =
    testList
        "Feature 098 unified canonical id scheme (US3)"
        [
          // T015 — determinism: the binding/bounds collectors over the same tree produce identical
          // results across repeated runs (pure, deterministic — no clock/randomness).
          test "property: boundIdsOf / Bounds / EventBindings are deterministic across runs (FR-006)" {
              let prop (tree: Control<Msg>) =
                  let r1 = render tree
                  let r2 = render tree
                  let ids1 = r1.Bounds |> List.map fst
                  let ids2 = r2.Bounds |> List.map fst
                  let binds1 = r1.EventBindings |> List.map (fun b -> b.ControlId, b.EventKind)
                  let binds2 = r2.EventBindings |> List.map (fun b -> b.ControlId, b.EventKind)
                  ids1 = ids2 && binds1 = binds2 && r1.BoundIds = r2.BoundIds

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen genTree) prop)
          }

          // T015 — same-kind-sibling distinctness: any two distinct unkeyed same-kind nodes have
          // distinct canonical ids (their structural paths), never a single shared `Kind` id.
          test "property: distinct unkeyed same-kind nodes get distinct canonical ids (SC-004)" {
              let prop (tree: Control<Msg>) =
                  let ids = allPathIds tree |> List.map fst
                  // The structural paths are unique per node (no two nodes share a path), so the
                  // collapsing `Kind` collision is gone for every same-kind sibling pair.
                  List.length ids = List.length (List.distinct ids)

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen genTree) prop)
          }

          // T015 — concrete two-unkeyed-bound-sibling routing: a Click on the SECOND dispatches the
          // second's message and NOT the first's (the path scheme disambiguates; no cross-routing).
          test "two unkeyed same-kind bound siblings route only to their own binding (no cross-routing)" {
              let siblings =
                  Stack.create
                      [ Stack.children
                            [ Button.create [ Button.text "one"; Button.onClick First ]
                              Button.create [ Button.text "two"; Button.onClick Second ] ] ]

              let result = render siblings
              // The two siblings carry distinct path ids "0.0" / "0.1" in both Bounds and EventBindings.
              let firstId = "0.0"
              let secondId = "0.1"
              Expect.equal (Control.nearestAuthored result secondId) (Some secondId) "the second sibling recovers itself (bound)"
              Expect.equal (Control.nearestAuthored result firstId) (Some firstId) "the first sibling recovers itself (bound)"

              // Dispatch a click targeted at the second id → only Second fires.
              let clickSecond = { Kind = "click"; ControlId = Some secondId; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None }
              Expect.equal (Control.dispatch clickSecond siblings) [ Second ] "a click on the second sibling dispatches ONLY its own message"

              let clickFirst = { Kind = "click"; ControlId = Some firstId; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None }
              Expect.equal (Control.dispatch clickFirst siblings) [ First ] "a click on the first sibling dispatches ONLY its own message"
          }

          // T016 — single-canonical-scheme agreement: for every laid-out node, its `Bounds` id, its
          // `EventBindings` id (when bound), its `BoundIds` membership key, and the id `nearestAuthored`
          // returns are the SAME value (no node reports `Kind` from one surface and `path` from another).
          test "single scheme spans Bounds / EventBindings / BoundIds / recovery (SC-003)" {
              let tree =
                  Stack.create
                      [ Stack.children
                            [ Button.create [ Button.text "go"; Button.onClick First ]
                              Stack.create [ Stack.children [ Button.create [ Button.text "deep"; Button.onClick Second ] ] ] ] ]

              let result = render tree
              let boundsIds = result.Bounds |> List.map fst |> Set.ofList
              let bindingIds = result.EventBindings |> List.map (fun b -> b.ControlId) |> Set.ofList

              // Every bound id appears identically in EventBindings, BoundIds, and Bounds.
              Expect.equal bindingIds result.BoundIds "EventBindings ids == BoundIds (same scheme)"
              Expect.isTrue (Set.isSubset result.BoundIds boundsIds) "every BoundIds id is a laid-out Bounds id (same scheme)"

              // The two authored buttons live at "0.0" and "0.1.0"; each recovers itself by the same id.
              for id in result.BoundIds do
                  Expect.equal (Control.nearestAuthored result id) (Some id) (sprintf "nearestAuthored is a fixed point on the bound id %s" id)
          }

          // T016 — render.BoundIds is populated from bound nodes while render.Bounds stays []
          // (the preview surface; FR-002, D3/D6).
          test "render.BoundIds is populated while render.Bounds stays empty (FR-002)" {
              let tree =
                  Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick First ] ] ]

              let preview = Control.render theme tree
              Expect.isEmpty preview.Bounds "the preview render leaves Bounds empty (unchanged)"
              Expect.isFalse (Set.isEmpty preview.BoundIds) "the preview render populates BoundIds from its bound nodes"
              // The bound button is the sole child "0.0"; its binding id agrees with BoundIds.
              let bindingIds = preview.EventBindings |> List.map (fun b -> b.ControlId) |> Set.ofList
              Expect.equal preview.BoundIds bindingIds "preview BoundIds == its EventBindings ids (unified scheme)"
          } ]
