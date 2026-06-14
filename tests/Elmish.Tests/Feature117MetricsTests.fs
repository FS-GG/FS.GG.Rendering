module Feature117MetricsTests

// Feature 117 (Phase 8, US1/US2/US3, FR-005/FR-006/FR-007/FR-010, SC-001/SC-002/SC-003/SC-006) — the
// text-measure cache + layout-invalidated contract is observable as the deterministic public
// `FrameMetrics` fields `TextMeasureCacheHitCount` / `TextMeasureCacheMissCount` /
// `LayoutInvalidatedNodeCount`, produced on the `ControlsElmish.Perf.runScript` render path. A cold
// text-heavy frame reports misses; a warm frame (unchanged text) reports hits + zero misses; a
// style-only frame reports zero misses / zero invalidated / zero re-measured; an idle frame reports all
// three `0`; a geometry frame reports `LayoutInvalidatedNodeCount <= RemeasuredNodeCount`; the counts
// re-run byte-identically (a silent re-shape or widened dirty set would fail a golden).

open System
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 1024; Height = 768 }
let private noMods = ViewerKeyboard.noModifiers
let private key () = FrameInput.Key(Enter, noMods)

let private style name value = { Name = name; Category = AttrCategory.Style; Value = value }

let private runWith (view: int -> Control<Msg>) (script: FrameInput<Msg> list) : FrameMetrics list =
    let host: InteractiveAppHost<int, Msg> =
        { Init = fun () -> 0, []
          Update = fun Bump model -> model + 1, []
          View = fun _ model -> view model
          Theme = Theme.light
          MapKey = fun k _ -> match k with | Enter -> Some Bump | _ -> None
          MapPointer = fun _ -> None
          Tick = fun _ -> None
          MapKeyChord = fun _ _ -> None
          OnFrameMetrics = ignore
          Diagnostics = Viewer.defaultDiagnostics }

    ControlsElmish.Perf.runScript host size script

// A text-heavy stack whose rows carry FIXED labels; a `selected` style flips with model parity so each
// step repaints (and so re-measures the UNCHANGED text) without any layout change — a style-only frame.
let private styleGrid (n: int) (model: int) : Control<Msg> =
    { Kind = "stack"
      Key = None
      Attributes = [ style "width" (FloatValue 240.0); style "height" (FloatValue 400.0) ]
      Children =
        [ for i in 0 .. n - 1 ->
            { Kind = "data-grid-row"
              Key = Some(sprintf "r%d" i)
              Attributes =
                [ style "width" (FloatValue 200.0)
                  style "height" (FloatValue 24.0)
                  style "selected" (BoolValue(model % 2 = 0)) ]
              Children = []
              Content = Some(sprintf "label-%d" i)
              Accessibility = None } ]
      Content = None
      Accessibility = None }

// A geometry stack where row 1's WIDTH tracks the model (a layout-affecting change).
let private geomGrid (n: int) (model: int) : Control<Msg> =
    { Kind = "stack"
      Key = None
      Attributes = [ style "width" (FloatValue 240.0); style "height" (FloatValue 400.0) ]
      Children =
        [ for i in 0 .. n - 1 ->
            { Kind = "data-grid-row"
              Key = Some(sprintf "r%d" i)
              Attributes =
                [ style "width" (FloatValue(if i = 1 then 120.0 + float (model % 3) * 20.0 else 200.0))
                  style "height" (FloatValue 24.0) ]
              Children = []
              Content = Some(sprintf "label-%d" i)
              Accessibility = None } ]
      Content = None
      Accessibility = None }

[<Tests>]
let tests =
    testList "Feature 117 text-cache + layout-invalidated metrics over Perf.runScript (FR-005/006/010)" [

        test "a cold text-heavy frame reports misses; the warm frame reports hits + zero misses (SC-001/SC-002)" {
            // frame[1] = first step (cache empty → cold); frame[2] = second step over unchanged text (warm).
            let frames = runWith (styleGrid 6) [ key (); key (); key () ]
            let cold = frames.[1]
            let warm = frames.[2]

            Expect.isTrue (cold.TextMeasureCacheMissCount > 0) "the cold frame reports text-measure misses"
            Expect.equal cold.TextMeasureCacheHitCount 0 "the cold frame has no first-seen hits"

            Expect.isTrue (warm.TextMeasureCacheHitCount > 0) "the warm frame reuses cached measurements (hits)"
            Expect.equal warm.TextMeasureCacheMissCount 0 "the warm frame re-measures nothing (zero misses)"
        }

        test "a style-only frame reports zero misses / zero invalidated / zero re-measured (FR-007, SC-003)" {
            let frames = runWith (styleGrid 6) [ key (); key (); key () ]
            let warm = frames.[2] // a style-only repaint over warm, unchanged text

            Expect.equal warm.TextMeasureCacheMissCount 0 "style-only over unchanged text: zero text-cache misses"
            Expect.equal warm.LayoutInvalidatedNodeCount 0 "style-only: zero layout-invalidated nodes"
            Expect.equal warm.RemeasuredNodeCount 0 "style-only: zero re-measured nodes"
        }

        test "an idle frame reports all three new fields 0 (FR-005/FR-006)" {
            let frames = runWith (styleGrid 6) [ key (); FrameInput.Idle ]
            let idle = List.last frames

            Expect.equal idle.TextMeasureCacheHitCount 0 "idle: zero text-cache hits"
            Expect.equal idle.TextMeasureCacheMissCount 0 "idle: zero text-cache misses"
            Expect.equal idle.LayoutInvalidatedNodeCount 0 "idle: zero layout-invalidated nodes"
        }

        test "a geometry frame reports LayoutInvalidatedNodeCount <= RemeasuredNodeCount, both bounded (FR-006, SC-006)" {
            let frames = runWith (geomGrid 5) [ key (); key () ]
            let geom = List.last frames

            Expect.isTrue (geom.LayoutInvalidatedNodeCount >= 1) "a geometry change invalidates at least one node"
            Expect.isTrue
                (geom.LayoutInvalidatedNodeCount <= geom.RemeasuredNodeCount)
                (sprintf "invalidated %d <= re-measured %d (propagation expands the pre-pinning set)" geom.LayoutInvalidatedNodeCount geom.RemeasuredNodeCount)
        }

        test "the three new metrics re-run byte-identically (FR-005/FR-006, regression sentinel)" {
            let run () =
                runWith (styleGrid 6) [ key (); key (); key () ]
                |> List.map (fun f -> f.TextMeasureCacheHitCount, f.TextMeasureCacheMissCount, f.LayoutInvalidatedNodeCount)

            Expect.equal (run ()) (run ()) "deterministic: identical scripts → identical metric tuples"
        }
    ]
