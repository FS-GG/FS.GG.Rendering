module Audit_TextCache

// AUDIT (feature 006, T004 sanity + T021 + T032) — the text-measure cache (`TextCacheEnabled` oracle +
// `RetainedRender.measureTextCached` + `WorkReductionRecord.TextMeasureCacheHits/Misses`).
//   * PARITY (FR-004): cache-on ≡ cache-off byte-identical rendered scene/bounds, with a DISCRIMINATING
//     proof that the equality oracle catches a real divergence.
//   * KEY-COMPLETENESS adversarial (FR-009): across text+family+size+weight, a single-field difference
//     MISSES and returns a fresh measurement equal to the un-cached `Scene.measureText`.
//   * EFFECTIVENESS (FR-008, T032): repeated identical measure ⇒ high text-cache hit-rate; margin vs
//     the disabled oracle is recorded.

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private empty: TextMeasureCache = { Entries = Map.empty; Clock = 0 }
let private font: FontSpec = { Family = Some "Inter"; Size = 15.0; Weight = None }

// A stack of labels whose `selected` style flips with model parity — every row repaints each step
// (re-measuring its UNCHANGED text), exercising the cache without any layout change.
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
    testList "Audit: Text-measure cache parity + key-completeness + effectiveness (FR-004/008/009)" [

        // ---- T004 scaffold sanity ----
        test "Audit: TextCache scaffold reachability — TextCacheEnabled + counters (T004)" {
            let r0 = (RetainedRender.init theme size (styleToggle 3 0)).Retained
            let off = { r0 with TextCacheEnabled = false }
            Expect.isFalse off.TextCacheEnabled "TextCacheEnabled oracle reachable + settable"
            let s = RetainedRender.step theme size r0 (styleToggle 3 1)
            Expect.isTrue (s.WorkReduction.TextMeasureCacheHits >= 0 && s.WorkReduction.TextMeasureCacheMisses >= 0) "text-cache counters reachable"
        }

        // ---- T021 KEY-COMPLETENESS adversarial ----
        test "Audit: key-completeness — a single-field difference (text|family|size|weight) MISSES with correct fresh metrics (FR-009)" {
            let _, warm, hit0 = RetainedRender.measureTextCached empty true "Hello" font
            Expect.isFalse hit0 "the cold base key is a miss"

            let differs (text: string) (f: FontSpec) (why: string) =
                let m, _, hit = RetainedRender.measureTextCached warm true text f
                Expect.isFalse hit (sprintf "a differing %s must MISS (no collision/stale hit)" why)
                Expect.equal m (Scene.measureText text f) (sprintf "the %s miss returns the correct un-cached metrics" why)

            differs "World" font "text"
            differs "Hello" { font with Family = Some "Roboto" } "font family"
            differs "Hello" { font with Size = 22.0 } "font size"
            differs "Hello" { font with Weight = Some 700 } "font weight"

            // the base key survives the misses (a hit) — they did not displace it.
            let _, _, hitBase = RetainedRender.measureTextCached warm true "Hello" font
            Expect.isTrue hitBase "the unchanged base key still hits after the single-field misses"
        }

        // ---- DISCRIMINATING POWER: a HIT returns the right entry, not a stale neighbour ----
        test "Audit: DISCRIMINATING — a warm cache serves the matching key and the equality oracle catches a divergence" {
            // Warm two distinct keys; each subsequent identical request must HIT and return ITS OWN
            // metrics. A measurement that differs in length yields different metrics, proving the
            // "equals the un-cached measure" oracle is not vacuous (it would catch a stale wrong hit).
            let short = "Hi"
            let long = "A much much much longer line of caption text"
            let _, c1, _ = RetainedRender.measureTextCached empty true short font
            let _, c2, _ = RetainedRender.measureTextCached c1 true long font

            let mShort, _, hShort = RetainedRender.measureTextCached c2 true short font
            let mLong, _, hLong = RetainedRender.measureTextCached c2 true long font
            Expect.isTrue (hShort && hLong) "both warm keys hit"
            Expect.equal mShort (Scene.measureText short font) "the short hit equals its own un-cached measure"
            Expect.equal mLong (Scene.measureText long font) "the long hit equals its own un-cached measure"
            Expect.notEqual mShort mLong "the two measurements genuinely differ — the equality oracle is discriminating (a stale cross-hit would be caught)"
        }

        // ---- T021 PARITY (wired) with discriminating proof ----
        test "Audit: cache-on ≡ cache-off byte-identical scene + bounds, with a discriminating divergence check (FR-004)" {
            let v0 = styleToggle 6 0
            let v1 = styleToggle 6 1
            let r0 = (RetainedRender.init theme size v0).Retained
            let on = RetainedRender.step theme size r0 v1
            let off = RetainedRender.step theme size { r0 with TextCacheEnabled = false } v1

            Expect.equal off.Render.Scene on.Render.Scene "cache-off scene is byte-identical to cache-on"
            Expect.equal off.Render.Bounds on.Render.Bounds "cache-off bounds are byte-identical to cache-on"
            Expect.equal off.WorkReduction.TextMeasureCacheHits 0 "the disabled oracle reports zero hits"
            Expect.isTrue (on.WorkReduction.TextMeasureCacheMisses + on.WorkReduction.TextMeasureCacheHits > 0) "the cache-on step measured text"

            // DISCRIMINATING: a different model parity yields a different scene, so the equality oracle
            // above is not vacuously true.
            let onDifferent = RetainedRender.step theme size r0 (styleToggle 6 0)
            Expect.notEqual onDifferent.Render.Scene on.Render.Scene "a genuinely different render is caught (discriminating)"
        }

        // ---- T032 EFFECTIVENESS: repeated identical measure ⇒ high hit-rate vs disabled ----
        test "Audit: EFFECTIVENESS — repeated identical measure yields a high text-cache hit-rate vs the disabled oracle (T032)" {
            let requests = 30
            let key = "label-effectiveness"

            let drive enabled =
                [ 1 .. requests ]
                |> List.fold (fun (cache, hits, misses) _ ->
                    let _, c', hit = RetainedRender.measureTextCached cache enabled key font
                    c', (if hit then hits + 1 else hits), (if hit then misses else misses + 1))
                    (empty, 0, 0)
                |> fun (_, h, m) -> h, m

            let onHits, onMisses = drive true
            let offHits, offMisses = drive false
            let onRate = float onHits / float (onHits + onMisses)

            Expect.equal offHits 0 "the disabled oracle never hits (re-measures every request)"
            Expect.equal offMisses requests "the disabled oracle misses every request"
            Expect.equal onMisses 1 "the enabled cache misses exactly once (cold) then hits"
            Expect.isTrue (onRate > 0.95) (sprintf "enabled steady-state hit-rate near-100%% (got %.3f over %d requests)" onRate requests)
            printfn "AUDIT-MARGIN TextCache: enabled hits=%d misses=%d rate=%.3f | disabled hits=%d misses=%d" onHits onMisses onRate offHits offMisses
        }
    ]
