module Feature116MetricsTests

// Feature 116 (US5, FR-012/FR-013/FR-015, SC-006/SC-007) — the paint-cache contract is observable as
// the deterministic public `FrameMetrics` fields `RepaintedNodeCount` / `DirtyRectCount` / `DirtyArea`
// / `PictureCacheHitCount` / `PictureCacheMissCount` / `PictureCacheEntryCount`, produced on the
// `ControlsElmish.Perf.runScript` render path. A localized change reports small damage; an idle frame
// reports 0/0/0 + 0 hit/miss; a stable subtree reuses (hits); a localized change misses exactly that
// subtree; the cache entry count stays bounded by the cap; the counts re-run byte-identically.

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

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

// A grid that is IDENTICAL across model values — every row is a stable cached picture.
let private stableGrid (n: int) (_: int) : Control<Msg> =
    wrap [ for i in 0 .. n - 1 -> row (sprintf "r%d" i) (sprintf "row-%d" i) ]

// A grid where exactly ONE row's content tracks the model — a localized change.
let private localizedGrid (n: int) (model: int) : Control<Msg> =
    wrap [ for i in 0 .. n - 1 -> row (sprintf "r%d" i) (if i = 1 then sprintf "row-1-%d" model else sprintf "row-%d" i) ]

[<Tests>]
let tests =
    testList "Feature 116 paint-cache metrics over Perf.runScript (US5, FR-012/013/015)" [

        test "an idle frame reports damage 0/0/0 and hit/miss 0 (FR-012)" {
            let frames = runWith (stableGrid 5) [ key (); FrameInput.Idle ]
            let idle = List.last frames

            Expect.equal idle.RepaintedNodeCount 0 "idle repaints nothing"
            Expect.equal idle.DirtyRectCount 0 "idle has no dirty rectangle"
            Expect.equal idle.DirtyArea 0 "idle has zero dirty area"
            Expect.equal idle.PictureCacheHitCount 0 "idle reports no hit"
            Expect.equal idle.PictureCacheMissCount 0 "idle reports no miss"
        }

        test "a stable grid: the second frame reuses every row picture with zero damage (FR-005/FR-012)" {
            let frames = runWith (stableGrid 5) [ key (); key () ]
            let f2 = List.last frames

            Expect.equal f2.RepaintedNodeCount 0 "a stable second frame repaints no node"
            Expect.equal f2.DirtyArea 0 "a stable second frame damages no area"
            Expect.equal f2.PictureCacheHitCount 5 "every stable row is a picture hit"
            Expect.equal f2.PictureCacheMissCount 0 "a stable frame recomputes no picture"
        }

        test "a localized change: small damage + a single picture miss, the rest hit (FR-001/FR-006)" {
            let frames = runWith (localizedGrid 5) [ key (); key () ]
            let f2 = List.last frames

            Expect.isTrue (f2.RepaintedNodeCount >= 1) "the changed row repaints"
            Expect.isTrue (f2.RepaintedNodeCount < 5) "a localized change does not repaint every node"
            Expect.isTrue (f2.DirtyArea > 0) "a localized change damages some area"
            Expect.equal f2.PictureCacheMissCount 1 "exactly the changed row misses"
            Expect.equal f2.PictureCacheHitCount 4 "the four unchanged rows hit"
        }

        test "the cache entry count stays bounded by the cap under pressure (FR-009, SC-007)" {
            let frames = runWith (stableGrid 320) [ key (); key () ]
            let f2 = List.last frames

            Expect.isTrue (f2.PictureCacheEntryCount <= RetainedRender.PictureCacheCap) "entry count bounded by the cap"
            Expect.equal f2.PictureCacheEntryCount RetainedRender.PictureCacheCap "the cache is full at the cap under pressure"
        }

        test "the six metrics re-run byte-identically (FR-012, SC-006)" {
            let run () =
                runWith (localizedGrid 8) [ key (); key () ]
                |> List.map (fun f ->
                    f.RepaintedNodeCount, f.DirtyRectCount, f.DirtyArea,
                    f.PictureCacheHitCount, f.PictureCacheMissCount, f.PictureCacheEntryCount)

            Expect.equal (run ()) (run ()) "deterministic: identical scripts → identical metric tuples"
        }
    ]
