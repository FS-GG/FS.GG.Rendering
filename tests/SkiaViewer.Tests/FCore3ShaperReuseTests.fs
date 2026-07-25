module FCore3ShaperReuseTests

// F-CORE-3 (2026-07-15 review): the shaping path had two per-call costs on the animated text hot path —
// it `new`-ed an `SKShaper` on every shape call, and it converted the shaper's `Codepoints`/`Clusters`/
// `Points` arrays to lists and then indexed them positionally with `List.tryItem` inside a per-glyph
// `List.mapi`, making glyph assembly O(n^2). The fix caches one shaper per bundled typeface and indexes
// the outputs as arrays (O(1)). These guards pin (1) that shaping many strings reuses a bounded set of
// shapers rather than one per call, and (2) that the array-indexed assembly still produces a correct,
// boundary-consistent glyph run.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private sans: FontSpec = { Family = Some "Noto Sans"; Size = 18.0; Weight = None }
let private mono: FontSpec = { Family = Some "Noto Sans Mono"; Size = 18.0; Weight = None }

// A long enough string that an accidental O(n^2) regression or an off-by-one in positional indexing
// would show up; kept pure ASCII so a shaper cluster (a UTF-8 byte offset) equals its char index and
// stays in `String.Length` range without surrogate/multi-byte handling.
let private longText = "The quick brown fox jumps over the lazy dog 0123456789 twice over again."

[<Tests>]
let tests =
    testSequenced
    <| testList "F-CORE-3 shaper reuse and O(1) glyph assembly" [
        test "shaping reuses one cached shaper per typeface, not one per call" {
            Fonts.withInstalledShapingProvider (fun () ->
                Fonts.disposeCaches ()

                // Many shape calls over a single font must not grow the shaper cache past that font's one
                // typeface — proving the shaper is reused, not `new`-ed per call.
                for _ in 1..50 do
                    Text.shapeText longText sans |> ignore

                Expect.equal (Fonts.shaperCacheCount ()) 1 "one shaper is cached and reused across 50 shape calls"

                // A second, distinct typeface adds exactly one more shaper; further reuse does not grow it.
                for _ in 1..50 do
                    Text.shapeText longText mono |> ignore

                Expect.equal (Fonts.shaperCacheCount ()) 2 "a distinct typeface adds exactly one shaper (still bounded by typefaces)"

                // Non-vacuity: the cache-count helper is not stuck at a constant — teardown empties it.
                Fonts.disposeCaches ()
                Expect.equal (Fonts.shaperCacheCount ()) 0 "disposeCaches empties the shaper cache")
        }

        test "array-indexed glyph assembly stays boundary-consistent on a long string" {
            Fonts.withInstalledShapingProvider (fun () ->
                let shaped = Text.shapeText longText sans

                Expect.equal shaped.Provider.Availability ProviderInstalled "installed shaping path under test"
                Expect.isNonEmpty shaped.Glyphs "the long string produces glyphs"

                // Every glyph's source cluster indexes into the source text (an off-by-one in the array
                // indexing that replaced `List.tryItem` would push a cluster out of range).
                Expect.isTrue
                    (shaped.Glyphs |> List.forall (fun g -> g.SourceCluster >= 0 && g.SourceCluster < longText.Length))
                    "every glyph cluster indexes into the source text"

                // LTR shaping: clusters never move backwards, and advances are non-negative.
                let clusters = shaped.Glyphs |> List.map (fun g -> g.SourceCluster)
                Expect.equal clusters (List.sort clusters) "clusters are non-decreasing across the run (LTR)"
                Expect.isTrue
                    (shaped.Glyphs |> List.forall (fun g -> g.Advance >= 0.0))
                    "all glyph advances are non-negative"

                // Boundary case: the last glyph's advance falls back to the run width (index+1 == count).
                // If the array boundary handling were wrong, the trailing advance would be negative or the
                // run would not reach the total width.
                let last = List.last shaped.Glyphs
                Expect.floatClose
                    Accuracy.medium
                    (last.Position.X + last.Advance)
                    shaped.Metrics.Advance
                    "the run reaches its total advance at the final glyph (width boundary)")
        }
    ]
