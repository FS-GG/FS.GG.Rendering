module Feature113MemoParityTests

// Feature 113 (US2, FR-006/FR-007, contract C5/C6) — the memoized build is byte-identical to the
// non-memoized build. Each frame's rendered scene with the memo seam active equals the scene built
// always-miss (`MemoEnabled = false`, the FR-008 parity oracle), and a real input change to the
// memoized DataGrid produces a Miss + a fresh, different scene (no staleness). Scenes have structural
// equality, so they are compared directly. Reaches `RetainedRender` via InternalsVisibleTo.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

// A childless `data-grid` leaf driving the real production projection (`faithfulContent` → `gridGeom`),
// its cells controlled by the `items` attribute the projection reads.
let private dataGrid (items: string list) : Control<int> =
    { Kind = "data-grid"
      Key = Some "grid"
      Attributes =
        [ { Name = "items"; Category = AttrCategory.Data; Value = StringListValue items }
          { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 220.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 140.0 } ]
      Children = []
      Content = None
      Accessibility = None }

// A keyed root holding the data-grid; changing `rootKey` forces a Replace + rebuild so the data-grid is
// re-painted through the memo seam (a genuine reuse opportunity, not a vacuous all-Keep frame).
let private viewTree (rootKey: string) (items: string list) : Control<int> =
    { Kind = "stack"
      Key = Some rootKey
      Attributes = []
      Children = [ dataGrid items ]
      Content = None
      Accessibility = None }

let private step (prev: RetainedRender<int>) (next: Control<int>) = RetainedRender.step theme size prev next

[<Tests>]
let tests =
    testList "Feature 113 memo-on ≡ memo-off parity + no staleness (US2, FR-006/FR-007, C5/C6)" [

        test "every frame's scene is byte-identical with the seam active vs forced always-miss (C5/SC-002)" {
            let items = [ "Name"; "Qty"; "A"; "1"; "B"; "2" ]
            let on0 = RetainedRender.init theme size (viewTree "r0" items)
            let off0 = { on0.Retained with MemoEnabled = false } // the FR-008 always-miss oracle

            let frames = [ viewTree "r1" items; viewTree "r2" items; viewTree "r3" items ]

            let _, onScenes =
                frames
                |> List.fold (fun (prev, acc) next -> let s = step prev next in s.Retained, acc @ [ s.Render.Scene ]) (on0.Retained, [])

            let _, offScenes =
                frames
                |> List.fold (fun (prev, acc) next -> let s = step prev next in s.Retained, acc @ [ s.Render.Scene ]) (off0, [])

            List.iteri2
                (fun i a b -> Expect.equal a b (sprintf "frame %d scene is byte-identical memo-on vs memo-off" i))
                onScenes
                offScenes
        }

        test "the reuse is real: a forced rebuild with unchanged data is a memo HIT (not vacuous)" {
            let items = [ "Name"; "Qty"; "A"; "1" ]
            let on0 = RetainedRender.init theme size (viewTree "r0" items)
            let s1 = step on0.Retained (viewTree "r1" items)
            Expect.isTrue (s1.WorkReduction.MemoHits > 0) "the forced rebuild with unchanged data is a memo hit"
            Expect.equal s1.WorkReduction.MemoMisses 0 "no miss when the data is unchanged"
        }

        test "no staleness: changing the grid's real inputs forces a Miss and a fresh, different scene (C6/FR-007)" {
            let on0 = RetainedRender.init theme size (viewTree "r0" [ "Name"; "Qty"; "A"; "1" ])
            let sStable = step on0.Retained (viewTree "r1" [ "Name"; "Qty"; "A"; "1" ])
            let sChanged = step sStable.Retained (viewTree "r2" [ "Name"; "Qty"; "ZZZ"; "9" ])

            Expect.isTrue (sChanged.WorkReduction.MemoMisses > 0) "a real input change is a Miss (no stale reuse)"
            Expect.notEqual sChanged.Render.Scene sStable.Render.Scene "the changed inputs produce a different scene (no staleness)"

            // The memo-off oracle produces the identical changed scene — proof the dependency is not too coarse.
            let off0 = { on0.Retained with MemoEnabled = false }
            let offStable = step off0 (viewTree "r1" [ "Name"; "Qty"; "A"; "1" ])
            let offChanged = step offStable.Retained (viewTree "r2" [ "Name"; "Qty"; "ZZZ"; "9" ])
            Expect.equal sChanged.Render.Scene offChanged.Render.Scene "the memo-on changed scene equals the memo-off build (dependency captures the change)"
        }
    ]
