module Feature138TextMetricsTests

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

let private repeatedTextGrid (n: int) (model: int) : Control<Msg> =
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
              Content = Some "same repeated label"
              Accessibility = None } ]
      Content = None
      Accessibility = None }

let private metricsTuple (f: FrameMetrics) =
    f.TextMeasureCacheHitCount,
    f.TextMeasureCacheMissCount,
    f.LayoutInvalidatedNodeCount,
    f.RemeasuredNodeCount

[<Tests>]
let tests =
    testList "Feature138TextMetrics" [
        test "cold repeated-text frame has zero hits, then warm equivalent frame has hits and zero misses" {
            let frames = runWith (repeatedTextGrid 6) [ key (); key (); key () ]
            let cold = frames.[1]
            let warm = frames.[2]

            Expect.equal cold.TextMeasureCacheHitCount 0 "cold frame has no prior-frame text reuse"
            Expect.isTrue (cold.TextMeasureCacheMissCount > 0) "cold frame records fresh text measurements"
            Expect.isTrue (warm.TextMeasureCacheHitCount > 0) "warm frame reports prior-frame reuse"
            Expect.equal warm.TextMeasureCacheMissCount 0 "warm frame has no fresh text measurements"
        }

        test "style-only over warm text and idle report zero text/layout work" {
            let styleFrames = runWith (repeatedTextGrid 6) [ key (); key (); key () ]
            let styleOnly = styleFrames.[2]

            Expect.equal styleOnly.TextMeasureCacheMissCount 0 "style-only frame has zero text misses"
            Expect.equal styleOnly.LayoutInvalidatedNodeCount 0 "style-only frame has zero layout invalidations"
            Expect.equal styleOnly.RemeasuredNodeCount 0 "style-only frame has zero remeasured nodes"

            let idle = runWith (repeatedTextGrid 6) [ key (); FrameInput.Idle ] |> List.last
            Expect.equal idle.TextMeasureCacheHitCount 0 "idle frame has zero text hits"
            Expect.equal idle.TextMeasureCacheMissCount 0 "idle frame has zero text misses"
            Expect.equal idle.LayoutInvalidatedNodeCount 0 "idle frame has zero layout invalidations"
            Expect.equal idle.RemeasuredNodeCount 0 "idle frame has zero remeasured nodes"
        }

        test "repeated captures are byte-identical" {
            let run () =
                runWith (repeatedTextGrid 6) [ key (); key (); key (); FrameInput.Idle ]
                |> List.map metricsTuple

            Expect.equal (run ()) (run ()) "same script produces identical metric tuples"
        }
    ]
