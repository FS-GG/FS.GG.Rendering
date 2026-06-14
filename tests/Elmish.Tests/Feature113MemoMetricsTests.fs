module Feature113MemoMetricsTests

// Feature 113 (US3, FR-009/FR-010, contract C7/C8) — memo work is observable as deterministic
// `FrameMetrics.MemoHitCount` / `MemoMissCount` over `ControlsElmish.Perf.runScript`. A steady-state
// scenario (a memoizable DataGrid whose data is unchanged across forced rebuilds) accrues hits with no
// misses; a perturbed scenario (data changed each frame) accrues misses; an idle frame and a host with
// no memoizable control report both counts 0.

open System
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 640; Height = 480 }
let private noMods = ViewerKeyboard.noModifiers
let private key () = FrameInput.Key(Enter, noMods)

// A childless `data-grid` leaf — the sole memoized site — its cells driven by the `items` attribute.
let private dataGrid (items: string list) : Control<Msg> =
    { Kind = "data-grid"
      Key = Some "grid"
      Attributes =
        [ { Name = "items"; Category = AttrCategory.Data; Value = StringListValue items }
          { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 220.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 140.0 } ]
      Children = []
      Content = None
      Accessibility = None }

// Steady data: each Bump only changes the ROOT KEY, forcing a Replace + rebuild so the grid is
// re-evaluated through the memo seam with an UNCHANGED dependency → a memo hit.
let private steadyView (model: int) : Control<Msg> =
    { Kind = "stack"
      Key = Some(sprintf "root-%d" model)
      Attributes = []
      Children = [ dataGrid [ "Name"; "Qty"; "A"; "1" ] ]
      Content = None
      Accessibility = None }

// Perturbed data: each Bump CHANGES the grid's cells, so every rebuild is a memo miss.
let private perturbedView (model: int) : Control<Msg> =
    { Kind = "stack"
      Key = Some(sprintf "root-%d" model)
      Attributes = []
      Children = [ dataGrid [ "Name"; "Qty"; sprintf "v%d" model; string model ] ]
      Content = None
      Accessibility = None }

// No memoizable control at all — a plain button stack.
let private plainView (model: int) : Control<Msg> =
    Stack.create [ Stack.children [ Button.create [ Button.text (string model) ] |> Control.withKey "b" ] ]

let private mkHost (view: int -> Control<Msg>) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey =
        fun k _ ->
            match k with
            | Enter -> Some Bump
            | _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

[<Tests>]
let tests =
    testList "Feature 113 memo metrics over Perf.runScript (US3, FR-009/FR-010, C7/C8)" [

        test "steady-state unchanged data accrues MemoHitCount > 0 with MemoMissCount = 0 (SC-004)" {
            let frames = ControlsElmish.Perf.runScript (mkHost steadyView) size [ key (); key (); key () ]
            // frame 0 seeds the cache via init (no work record); frames 1+ step over a forced rebuild.
            let steady = frames.[2]
            Expect.isTrue (steady.MemoHitCount > 0) "the steady rebuild with unchanged data is a memo hit"
            Expect.equal steady.MemoMissCount 0 "no misses when the data is unchanged"
        }

        test "perturbed inputs accrue MemoMissCount (the dependency changed each frame)" {
            let frames = ControlsElmish.Perf.runScript (mkHost perturbedView) size [ key (); key (); key () ]
            let perturbed = frames.[2]
            Expect.isTrue (perturbed.MemoMissCount > 0) "changed data each frame is a memo miss"
            Expect.equal perturbed.MemoHitCount 0 "no hits when the data changes every frame"
        }

        test "an idle frame reports both memo counts 0 (C8/FR-009)" {
            let frames = ControlsElmish.Perf.runScript (mkHost steadyView) size [ key (); FrameInput.Idle ]
            let idle = frames.[1]
            Expect.equal idle.MemoHitCount 0 "idle frame has no memo hits"
            Expect.equal idle.MemoMissCount 0 "idle frame has no memo misses"
        }

        test "a host with no memoizable control reports 0/0 on every frame" {
            let frames = ControlsElmish.Perf.runScript (mkHost plainView) size [ key (); key () ]
            frames
            |> List.iteri (fun i f ->
                Expect.equal f.MemoHitCount 0 (sprintf "frame %d: no memoizable control -> 0 hits" i)
                Expect.equal f.MemoMissCount 0 (sprintf "frame %d: no memoizable control -> 0 misses" i))
        }
    ]
