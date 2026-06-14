module Feature120MetricsTests

// Feature 120 (US1, FR-001/FR-002, SC-001; US3, FR-014, SC-004) — the new `FrameMetrics` fields over the
// deterministic `ControlsElmish.Perf.runScript` path: the two per-phase timing fields are LIVE-ONLY, so
// on the deterministic path they are `TimeSpan.Zero` (excluded from the golden surface, SC-001); the
// replay counters coincide with the picture-cache outcomes (the replay cache is its load-bearing
// realization) and `ReplaySkippedNodeCount` is the node-level work-reduction signal on a stable grid.

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

let private row (key: string) (content: string) : Control<Msg> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 200.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some content
      Accessibility = None }

let private wrap (rows: Control<Msg> list) : Control<Msg> =
    { Kind = "stack"; Key = None; Attributes = []; Children = rows; Content = None; Accessibility = None }

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

let private stableGrid (n: int) (_: int) : Control<Msg> =
    wrap [ for i in 0 .. n - 1 -> row (sprintf "r%d" i) (sprintf "row-%d" i) ]

[<Tests>]
let tests =
    testList "Feature 120 timing + replay metrics over Perf.runScript (US1/US3)" [

        test "per-phase timing is excluded from the deterministic surface: PaintDuration/ComposeDuration are Zero (SC-001/FR-002)" {
            let frames = runWith (stableGrid 5) [ key (); key () ]
            for f in frames do
                Expect.equal f.PaintDuration TimeSpan.Zero "PaintDuration is live-only (Zero on the deterministic path)"
                Expect.equal f.ComposeDuration TimeSpan.Zero "ComposeDuration is live-only (Zero on the deterministic path)"
        }

        test "a stable grid frame: replay hits coincide with picture-cache hits and skip node work (FR-014/SC-004)" {
            // second model frame: all rows stable from the first → replay hits.
            let frames = runWith (stableGrid 8) [ key (); key () ]
            let f = List.last frames

            Expect.equal f.ReplayHitCount f.PictureCacheHitCount "replay hits coincide with picture-cache hits"
            Expect.equal f.ReplayMissCount f.PictureCacheMissCount "replay misses coincide with picture-cache misses"
            Expect.equal f.ReplayRecordCount f.ReplayMissCount "one record per miss"
            Expect.isTrue (f.ReplayHitCount > 0) "the stable grid produces replay hits"
            Expect.isTrue (f.ReplaySkippedNodeCount > 0) "replayed boundaries skip subtree paint nodes (SC-004 signal)"
            Expect.isTrue (f.ReplayCacheNativeBytes > 0) "native bytes are observable for resident pictures"
        }

        test "an idle frame reports zero replay work (FR-014)" {
            let frames = runWith (stableGrid 5) [ key (); FrameInput.Idle ]
            let idle = List.last frames
            Expect.equal idle.ReplayHitCount 0 "idle replays nothing"
            Expect.equal idle.ReplaySkippedNodeCount 0 "idle skips nothing"
            Expect.equal idle.ReplayCacheNativeBytes 0 "idle reports no live replay-cache bytes for this frame"
        }
    ]
