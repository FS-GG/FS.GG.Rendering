module Audit_DamageTracking

// AUDIT (feature 006-verify-imported-mechanisms) — damage-rect mechanism.
//   * T007 sanity: the union-area helper + the `FrameMetrics` damage counters are reachable.
//   * T026 US2 correctness: `RetainedRender.unionArea` counts overlapping dirty rects ONCE — the result
//     equals the true union (independently computed), is < the naive per-rect sum when rects overlap,
//     and never exceeds `frameArea`. Discriminating: a constructed genuine-overlap case where the naive
//     sum strictly exceeds the union and `unionArea` returns the union (not the sum).
//   * T033 US3 effectiveness: a localized change drives the REAL frame path (`ControlsElmish.Perf.runScript`)
//     and reports `DirtyArea`/`DirtyRectCount` that are a small fraction of the full-repaint baseline
//     (every node changing). If damage equals full repaint that is a FINDING (no localization).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// --------------------------------------------------------------------------------------------------
// Independent reference: brute-force the integer-cell union of a set of rectangles. This shares NO code
// with the coordinate-compression `unionArea` under test, so agreement is a genuine cross-check.
// (All generated/constructed rects use integer coordinates, for which cell-count == geometric area.)
// --------------------------------------------------------------------------------------------------
let private bruteUnionArea (rects: Rect list) (frameArea: int) : int =
    let cells = System.Collections.Generic.HashSet<struct (int * int)>()
    for r in rects do
        for x in int r.X .. int (r.X + r.Width) - 1 do
            for y in int r.Y .. int (r.Y + r.Height) - 1 do
                cells.Add(struct (x, y)) |> ignore
    min cells.Count frameArea

let private naiveSum (rects: Rect list) : int =
    rects |> List.sumBy (fun r -> int r.Width * int r.Height)

let private rect x y w h : Rect = { X = float x; Y = float y; Width = float w; Height = float h }

// A deterministic generator of small integer rect sets (seeded — reproducible).
let private generatedCases () : Rect list list =
    let rng = Random(20260615)
    [ for _ in 1..200 ->
          let n = rng.Next(1, 6)
          [ for _ in 1..n ->
                rect (rng.Next(0, 15)) (rng.Next(0, 15)) (rng.Next(1, 10)) (rng.Next(1, 10)) ] ]

// ---- The real frame path (mirrors Feature116MetricsTests) ---------------------------------------
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

let private gridSize = 50

// Exactly ONE row's content tracks the model — a localized change.
let private localizedGrid (model: int) : Control<Msg> =
    wrap [ for i in 0 .. gridSize - 1 -> row (sprintf "r%d" i) (if i = 1 then sprintf "row-1-%d" model else sprintf "row-%d" i) ]

// EVERY row's content tracks the model — the full-repaint baseline.
let private fullChangeGrid (model: int) : Control<Msg> =
    wrap [ for i in 0 .. gridSize - 1 -> row (sprintf "r%d" i) (sprintf "row-%d-%d" i model) ]

[<Tests>]
let tests =
    testList "Audit damage-rect mechanism (T007 / T026 US2 / T033 US3)" [

        // ---- T007 sanity --------------------------------------------------------------------------
        test "Audit: damage seam reachable — unionArea + DirtyArea/DirtyRectCount counters touchable (T007)" {
            Expect.equal (RetainedRender.unionArea [] 1000) 0 "unionArea of no boxes is 0 (helper reachable)"
            let f = (runWith localizedGrid [ key () ]) |> List.last
            Expect.isTrue (f.DirtyRectCount >= 0 && f.DirtyArea >= 0) "FrameMetrics damage counters are reachable"
        }

        // ---- T026 US2 correctness (UNION area, overlaps counted once) -----------------------------
        test "Audit: unionArea counts a genuine overlap ONCE — returns the union, not the naive sum (T026, discriminating)" {
            // A = [0,100]x[0,100], B = [50,150]x[0,100]: they overlap in [50,100]x[0,100] (area 5000).
            let a = rect 0 0 100 100
            let b = rect 50 0 100 100
            let boxes = [ a; b ]
            let frameArea = 1000 * 1000
            let union = RetainedRender.unionArea boxes frameArea
            let sum = naiveSum boxes // 10000 + 10000 = 20000
            let expectedUnion = 150 * 100 // bounding strip [0,150]x[0,100] = 15000

            Expect.equal union expectedUnion "unionArea equals the true geometric union (15000)"
            Expect.isTrue (sum > union) "the rects genuinely overlap: naive sum (20000) strictly exceeds the union (15000)"
            Expect.notEqual union sum "DISCRIMINATING: unionArea returns the union, NOT the naive sum (a sum-based impl would fail here)"
            Expect.equal union (bruteUnionArea boxes frameArea) "independently-computed union agrees"
        }

        test "Audit: unionArea is clamped to frameArea (never exceeds the frame) (T026)" {
            let boxes = [ rect 0 0 100 100; rect 30 30 100 100 ]
            let frameArea = 500 // far smaller than any plausible union
            Expect.equal (RetainedRender.unionArea boxes frameArea) frameArea "clamped to the frame area"
        }

        test "Audit: unionArea matches an independent brute-force union across 200 generated rect sets (T026)" {
            let frameArea = 1000 * 1000
            let mismatches =
                generatedCases ()
                |> List.choose (fun boxes ->
                    let got = RetainedRender.unionArea boxes frameArea
                    let expected = bruteUnionArea boxes frameArea
                    let sum = naiveSum boxes
                    if got <> expected then Some (boxes, got, expected)
                    elif got > sum then Some (boxes, got, sum) // union can never exceed the naive sum
                    else None)
            Expect.isEmpty mismatches "every generated case: unionArea == brute-force union AND <= naive sum"

            // Confirm the generated corpus actually contains overlapping cases (where union < sum) so the
            // property has real discriminating power, not vacuous agreement on disjoint rects.
            let overlapping =
                generatedCases ()
                |> List.filter (fun boxes -> RetainedRender.unionArea boxes frameArea < naiveSum boxes)
            Expect.isNonEmpty overlapping "the corpus contains genuine overlaps (union strictly below the naive sum)"
        }

        // ---- T033 US3 effectiveness (localized damage << full repaint) ----------------------------
        test "Audit: a localized change damages a SMALL fraction of the full-repaint baseline (T033, effectiveness)" {
            let localized = runWith localizedGrid [ key (); key () ] |> List.last
            let full = runWith fullChangeGrid [ key (); key () ] |> List.last

            // The full-change frame is the genuine full-repaint baseline (every row repainted).
            Expect.equal full.DirtyRectCount gridSize (sprintf "baseline repaints all %d rows" gridSize)
            Expect.isTrue (full.DirtyArea > 0) "the baseline damages a positive area"

            // The localized frame repaints exactly the one changed row.
            Expect.equal localized.DirtyRectCount 1 "a localized change yields exactly one dirty rect"
            Expect.isTrue (localized.DirtyArea > 0) "the localized change damages some area"

            // FINDING gate: if the localized damage is NOT a small fraction, the damage seam does not localize.
            let areaFraction = float localized.DirtyArea / float full.DirtyArea
            let rectFraction = float localized.DirtyRectCount / float full.DirtyRectCount
            Expect.isLessThan areaFraction 0.10 (sprintf "localized DirtyArea (%d) is a small fraction of full (%d) — fraction %.3f" localized.DirtyArea full.DirtyArea areaFraction)
            Expect.isLessThan rectFraction 0.10 (sprintf "localized DirtyRectCount (%d) is a small fraction of full (%d)" localized.DirtyRectCount full.DirtyRectCount)
            Expect.notEqual localized.DirtyArea full.DirtyArea "localized damage is NOT a full repaint (would be a FINDING)"
        }
    ]
