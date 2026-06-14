module Feature117CacheBoundTests

// Feature 117 (Phase 8, US1, FR-003, SC-005) — the text-measure cache is bounded by entry count with
// deterministic LRU eviction, reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]` over
// the pure `RetainedRender.measureTextCached` lookup. Measuring more distinct strings than the cap keeps
// `Entries.Count <= cap` at all times; eviction is deterministic (same input order → same surviving
// entries + same hit/miss sequence); an evicted key re-misses (fresh, correct measure) when next needed,
// never a stale hit. Render-only / deterministic ([[fs-skia-evidence-mode]]).

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private empty: TextMeasureCache = { Entries = Map.empty; Clock = 0 }
let private font: FontSpec = { Family = Some "Inter"; Size = 15.0; Weight = None }
let private cap = RetainedRender.TextMeasureCacheCap

// Measure `text-<i>` for i in 0..n-1 in order, returning the final cache and the (hit/miss) sequence.
let private sweep (n: int) (start: TextMeasureCache) : TextMeasureCache * bool list =
    ((start, []), [ 0 .. n - 1 ])
    ||> List.fold (fun (c, hits) i ->
        let _, c', hit = RetainedRender.measureTextCached c true (sprintf "text-%d" i) font
        c', hit :: hits)
    |> fun (c, hits) -> c, List.rev hits

[<Tests>]
let tests =
    testList "Feature 117 bounded text-measure cache (US1, FR-003, SC-005)" [

        test "Entries.Count never exceeds the cap, even under eviction pressure (FR-003/SC-005)" {
            let overCap = cap + 64
            Expect.isTrue (overCap > cap) "scenario drives more distinct strings than the cap"

            // Assert the bound holds at EVERY step, not just at the end.
            let mutable c = empty
            for i in 0 .. overCap - 1 do
                let _, c', _ = RetainedRender.measureTextCached c true (sprintf "text-%d" i) font
                c <- c'
                Expect.isTrue (c.Entries.Count <= cap) (sprintf "after %d inserts the entry count %d is bounded by the cap %d" (i + 1) c.Entries.Count cap)

            Expect.equal c.Entries.Count cap "the cache is full at the cap under pressure"
        }

        test "eviction is deterministic: same input order → same surviving entries + same hit/miss sequence (FR-003/SC-005)" {
            let run () =
                let c, hits = sweep (cap + 64) empty
                (c.Entries |> Map.toList |> List.map fst |> List.sort), hits

            Expect.equal (run ()) (run ()) "the surviving-key set and the hit/miss sequence reproduce exactly"
        }

        test "an evicted key re-misses with a fresh correct measure, never a stale hit (FR-003/SC-005)" {
            // After overflowing the cap starting from text-0, the LRU (text-0, first inserted) is evicted.
            let c, _ = sweep (cap + 1) empty
            Expect.isFalse (c.Entries.ContainsKey { Text = "text-0"; Family = font.Family; Size = font.Size; Weight = font.Weight })
                "the least-recently-used key (text-0) was evicted under pressure"

            let m, _, hit = RetainedRender.measureTextCached c true "text-0" font
            Expect.isFalse hit "the evicted key re-MISSES (never a stale hit)"
            Expect.equal m (Scene.measureText "text-0" font) "the re-miss returns a fresh, correct measure"
        }

        test "a sweep within the cap evicts nothing and re-hits everything (FR-003)" {
            let underCap = cap / 2
            let c, firstHits = sweep underCap empty
            Expect.equal c.Entries.Count underCap "no eviction below the cap"
            Expect.isFalse (List.contains true firstHits) "the cold sweep is all misses"

            let _, secondHits = sweep underCap c
            Expect.isTrue (List.forall id secondHits) "the warm sweep is all hits (nothing evicted)"
        }
    ]
