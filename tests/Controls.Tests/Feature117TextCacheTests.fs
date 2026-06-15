module Feature117TextCacheTests

// Feature 117 (Phase 8, US1, FR-001/FR-002/FR-004, SC-001/SC-002/SC-004) — the bounded text-measure
// cache over `Scene.measureText`, reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]`.
// A cold key is a miss; an identical key is a hit served WITHOUT re-invoking `Scene.measureText`;
// perturbing EXACTLY ONE keyed input (text | family | size | weight) independently forces a miss with
// the correct fresh metrics (FR-002); a hit's metrics are byte-identical to the un-cached measure; the
// always-miss oracle (`enabled = false`) produces identical measured values (cache-on ≡ cache-off,
// FR-004); empty/whitespace text caches without error; and a `fittedFontSize` caption's distinct
// candidate sizes are distinct keys with the chosen size unchanged. Render-only / deterministic
// ([[fs-gg-evidence-mode]]); the byte-identity authority is the un-cached measure ([[fs-gg-reconciliation]]).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private empty: TextMeasureCache = { Entries = Map.empty; Clock = 0 }
let private font: FontSpec = { Family = Some "Inter"; Size = 15.0; Weight = None }

// A stack of `n` labels whose `selected` style flips with model parity — every row repaints each step
// (re-measuring its UNCHANGED text), so a step exercises the cache without any layout change.
let private styleToggle (n: int) (model: int) : Control<int> =
    { Kind = "stack"
      Key = None
      Attributes = []
      Children =
        [ for i in 0 .. n - 1 ->
            { Kind = "data-grid-row"
              Key = Some(sprintf "r%d" i)
              Attributes =
                [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 200.0 }
                  { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 }
                  { Name = "selected"; Category = AttrCategory.Style; Value = BoolValue(model % 2 = 0) } ]
              Children = []
              Content = Some(sprintf "label-%d" i)
              Accessibility = None } ]
      Content = None
      Accessibility = None }

[<Tests>]
let tests =
    testList "Feature 117 text-measure cache (US1, FR-001/002/004)" [

        test "a cold key is a miss; the identical key is a hit with byte-identical metrics (FR-001/SC-002→SC-001/FR-004)" {
            let m1, c1, hit1 = RetainedRender.measureTextCached empty true "Hello" font
            Expect.isFalse hit1 "the first measurement of a cold key is a miss"
            Expect.equal m1 (Scene.measureText "Hello" font) "a miss returns the un-cached measure (byte-identical)"

            let m2, _, hit2 = RetainedRender.measureTextCached c1 true "Hello" font
            Expect.isTrue hit2 "the second identical-key measurement is a hit"
            Expect.equal m2 m1 "a hit's metrics are byte-identical to the un-cached measure (FR-004/SC-004)"
        }

        test "perturbing EXACTLY ONE keyed input independently forces a miss (FR-002)" {
            // Seed the cache with the base key, then change each field in turn from the SAME warm cache.
            let _, warm, _ = RetainedRender.measureTextCached empty true "Hello" font

            let differs (text: string) (f: FontSpec) (why: string) =
                let m, _, hit = RetainedRender.measureTextCached warm true text f
                Expect.isFalse hit (sprintf "a differing %s must MISS (no stale hit, FR-002)" why)
                Expect.equal m (Scene.measureText text f) (sprintf "the %s miss returns the correct fresh metrics" why)

            differs "World" font "text"
            differs "Hello" { font with Family = Some "Roboto" } "font family"
            differs "Hello" { font with Size = 22.0 } "font size"
            differs "Hello" { font with Weight = Some 700 } "font weight"

            // ...and the base key is still resident (a hit), proving the misses did not displace it.
            let _, _, hitBase = RetainedRender.measureTextCached warm true "Hello" font
            Expect.isTrue hitBase "the unchanged base key still hits"
        }

        test "the always-miss oracle re-measures every request (cache-on ≡ cache-off, FR-004/SC-004)" {
            let m1, c1, hit1 = RetainedRender.measureTextCached empty false "Hello" font
            let m2, _, hit2 = RetainedRender.measureTextCached c1 false "Hello" font
            Expect.isFalse hit1 "oracle: the first request misses"
            Expect.isFalse hit2 "oracle: even a repeated request misses (never consults the cache)"
            Expect.equal m1 (Scene.measureText "Hello" font) "oracle metrics equal the un-cached measure"
            Expect.equal m2 m1 "oracle re-measure is byte-identical (the value never changes)"
            Expect.isEmpty c1.Entries "oracle never populates the cache"
        }

        test "empty and whitespace text cache without error and stay byte-identical (edge case)" {
            for t in [ ""; "   "; "\t" ] do
                let m, c, hit = RetainedRender.measureTextCached empty true t font
                Expect.isFalse hit "first measurement of whitespace/empty is a miss"
                Expect.equal m (Scene.measureText t font) "whitespace/empty measure is byte-identical to the un-cached value"
                let m2, _, hit2 = RetainedRender.measureTextCached c true t font
                Expect.isTrue hit2 "a second identical whitespace/empty measurement hits"
                Expect.equal m2 m "the whitespace/empty hit is byte-identical"
        }

        test "a fitted-caption's distinct candidate sizes are distinct keys (edge case)" {
            // `fittedFontSize` probes the same text at several distinct sizes; each size is a distinct key,
            // so the first sweep misses each and a second identical sweep hits each (cross-frame help).
            let caption = "A long caption that does not fit at the maximum size"
            let sizes = [ 18.0; 12.0; 9.0; 7.5 ]

            let cacheAfterSweep =
                sizes
                |> List.fold (fun c s ->
                    let _, c', hit = RetainedRender.measureTextCached c true caption { font with Size = s }
                    Expect.isFalse hit (sprintf "cold candidate size %g misses" s)
                    c') empty

            for s in sizes do
                let _, _, hit = RetainedRender.measureTextCached cacheAfterSweep true caption { font with Size = s }
                Expect.isTrue hit (sprintf "warm candidate size %g hits (same search path across frames)" s)
        }

        test "the always-miss oracle yields a byte-identical SCENE and layout (cache-on ≡ cache-off, FR-004/SC-004)" {
            // Drive a real retained step that re-measures text (a style flip repaints every row), once with
            // the cache enabled and once disabled; the emitted scene, bounds, and remeasure count match.
            let v0 = styleToggle 6 0
            let v1 = styleToggle 6 1

            let r0 = (RetainedRender.init theme size v0).Retained
            let on = RetainedRender.step theme size r0 v1
            let off = RetainedRender.step theme size { r0 with TextCacheEnabled = false } v1

            Expect.equal off.Render.Scene on.Render.Scene "cache-off scene is byte-identical to cache-on"
            Expect.equal off.Render.Bounds on.Render.Bounds "cache-off bounds are byte-identical to cache-on"
            Expect.equal off.WorkReduction.RemeasuredNodeCount on.WorkReduction.RemeasuredNodeCount "remeasure count is unaffected by the cache"

            // The only observable delta is the hit/miss accounting: cache-on warms after the first row,
            // cache-off always misses (never a hit).
            Expect.equal off.WorkReduction.TextMeasureCacheHits 0 "the oracle reports zero hits"
            Expect.isTrue (on.WorkReduction.TextMeasureCacheMisses + on.WorkReduction.TextMeasureCacheHits > 0) "the cache-on step measured text"

            // ...and equals a fresh full rebuild (the standing parity oracle).
            Expect.equal on.Render.Scene (Control.renderTree theme size v1).Scene "cache-on scene equals a fresh full rebuild"
        }
    ]
