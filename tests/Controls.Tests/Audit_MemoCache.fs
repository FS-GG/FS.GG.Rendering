module Audit_MemoCache

// AUDIT (feature 006, T004 sanity + T019 + T030) — the memo cache seam (`MemoEnabled` oracle +
// `RetainedRender.memoize` + `WorkReductionRecord.MemoHits/MemoMisses`).
//   * PARITY (FR-004): a representative scene rendered with `MemoEnabled = true` vs `false` is
//     byte-identical, with a DISCRIMINATING proof that a stale/forced-wrong memo entry WOULD diverge.
//   * CACHE-KEY COMPLETENESS (FR-009): two inputs differing in one render-affecting field do NOT
//     collide — a changed dependency MISSES (the seam never reuses across an unequal dependency).
//   * EFFECTIVENESS (FR-008, T030): repeated unchanged render ⇒ `MemoHits` rises to near-100%
//     steady-state while misses→0; the margin vs the `MemoEnabled = false` baseline is recorded.
//
// Reaches `internal RetainedRender` via InternalsVisibleTo. Scenes have structural equality.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private scene (tag: float) : Scene list = [ Scene.rectangle (tag, 0.0, 10.0, 10.0) Colors.black ]

// A childless data-grid leaf driving the real production projection; its cells the `items` attribute.
let private dataGrid (items: string list) : Control<int> =
    { Kind = "data-grid"
      Key = Some "grid"
      Attributes =
        [ { Name = "items"; Category = AttrCategory.Data; Value = StringListValue items }
          { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 220.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 140.0 } ]
      Children = []
      Content = None
      Accessibility = None }

// Changing `rootKey` forces a Replace + rebuild so the data-grid re-paints through the memo seam.
let private viewTree (rootKey: string) (items: string list) : Control<int> =
    { Kind = "stack"; Key = Some rootKey; Attributes = []; Children = [ dataGrid items ]; Content = None; Accessibility = None }

let private step prev next = RetainedRender.step theme size prev next

[<Tests>]
let tests =
    testList "Audit: Memo cache parity + key-completeness + effectiveness (FR-004/008/009)" [

        // ---- T004 scaffold sanity ----
        test "Audit: Memo scaffold reachability — MemoEnabled + MemoHits/MemoMisses (T004)" {
            let init = RetainedRender.init theme size (viewTree "r0" [ "a"; "1" ])
            let off = { init.Retained with MemoEnabled = false } // oracle field reachable
            let s = step off (viewTree "r1" [ "a"; "1" ])
            Expect.isTrue (s.WorkReduction.MemoHits >= 0 && s.WorkReduction.MemoMisses >= 0) "memo counters reachable"
            Expect.isFalse off.MemoEnabled "MemoEnabled oracle field reachable + settable"
        }

        // ---- T019 PARITY: memo-on ≡ memo-off byte-identical ----
        test "Audit: memo-on ≡ memo-off renders byte-identical scenes (FR-004)" {
            let items = [ "Name"; "Qty"; "A"; "1"; "B"; "2" ]
            let on0 = RetainedRender.init theme size (viewTree "r0" items)
            let off0 = { on0.Retained with MemoEnabled = false }
            let frames = [ viewTree "r1" items; viewTree "r2" items; viewTree "r3" items ]

            let render seed =
                frames |> List.fold (fun (prev, acc) next -> let s = step prev next in s.Retained, acc @ [ s.Render.Scene ]) (seed, []) |> snd

            List.iteri2
                (fun i a b -> Expect.equal a b (sprintf "frame %d byte-identical memo-on vs memo-off" i))
                (render on0.Retained) (render off0)
        }

        // ---- DISCRIMINATING POWER: a stale memo HIT would diverge from the fresh compute ----
        test "Audit: DISCRIMINATING — a memo HIT returns the OLD subtree even if compute changed (proves parity has teeth)" {
            // Seed an entry under dep "k" producing scene 1; re-call with the SAME dep but a compute that
            // would produce scene 2. The seam HITS and returns scene 1 (reference-reused) — NOT scene 2.
            // This is exactly the divergence the parity test would catch if the dependency failed to
            // capture a real change: memo-on (stale) would differ from memo-off (fresh).
            let mutable calls = 0
            let first, cache1, o1 = RetainedRender.memoize "grid" ("k" :> obj) (fun () -> calls <- calls + 1; scene 1.0) Map.empty
            let hit, _, o2 = RetainedRender.memoize "grid" ("k" :> obj) (fun () -> calls <- calls + 1; scene 2.0) cache1
            Expect.equal o1 Miss "cold dependency misses"
            Expect.equal o2 Hit "equal dependency hits"
            Expect.equal calls 1 "the HIT did not run compute"
            Expect.isTrue (System.Object.ReferenceEquals(first, hit)) "the hit reuses the SAME instance"
            // the discriminating fact: the reused (stale) subtree differs from what compute WOULD produce.
            Expect.notEqual hit (scene 2.0) "a stale hit diverges from the fresh compute — the parity oracle is discriminating"
        }

        // ---- CACHE-KEY COMPLETENESS: one differing render-affecting field ⇒ Miss (no collision) ----
        test "Audit: cache-key completeness — a changed dependency does NOT collide (Miss) (FR-009)" {
            // (a) direct seam: structurally-different dependency misses
            let _, cache1, _ = RetainedRender.memoize "grid" ([ "Name"; "A"; "1" ] :> obj) (fun () -> scene 1.0) Map.empty
            let _, _, o = RetainedRender.memoize "grid" ([ "Name"; "A"; "2" ] :> obj) (fun () -> scene 2.0) cache1
            Expect.equal o Miss "a one-cell dependency change does not collide (Miss, no stale reuse)"

            // (b) wired path: one changed grid cell ⇒ a frame Miss and a different, fresh scene.
            let on0 = RetainedRender.init theme size (viewTree "r0" [ "Name"; "Qty"; "A"; "1" ])
            let stable = step on0.Retained (viewTree "r1" [ "Name"; "Qty"; "A"; "1" ])
            let changed = step stable.Retained (viewTree "r2" [ "Name"; "Qty"; "ZZZ"; "9" ])
            Expect.isTrue (changed.WorkReduction.MemoMisses > 0) "a real input change misses (no collision)"
            Expect.notEqual changed.Render.Scene stable.Render.Scene "the changed inputs render a different scene (no staleness)"
        }

        // ---- T030 EFFECTIVENESS: steady-state hit-rate ≫ disabled baseline ----
        test "Audit: EFFECTIVENESS — repeated unchanged render drives MemoHits→~100%, misses→0 vs disabled baseline (T030)" {
            let items = [ "Name"; "Qty"; "A"; "1"; "B"; "2"; "C"; "3" ]
            let frameCount = 30
            let frames = [ for i in 1 .. frameCount -> viewTree (sprintf "r%d" i) items ]

            let accumulate enabled =
                let init = RetainedRender.init theme size (viewTree "r0" items)
                let seed = { init.Retained with MemoEnabled = enabled }
                frames
                |> List.fold (fun (prev, hits, misses) next ->
                    let s = step prev next
                    s.Retained, hits + s.WorkReduction.MemoHits, misses + s.WorkReduction.MemoMisses)
                    (seed, 0, 0)
                |> fun (_, h, m) -> h, m

            let onHits, onMisses = accumulate true
            let offHits, offMisses = accumulate false

            let onRate = float onHits / float (onHits + onMisses)
            Expect.equal offHits 0 "the disabled baseline produces ZERO memo hits (no reuse — the effectiveness margin)"
            // FINDING (instrumentation): with `MemoEnabled = false` the wired step BYPASSES the memoize
            // seam entirely (RetainedRender.fs paintOwn `else` branch) and recomputes directly WITHOUT
            // incrementing MemoMisses — so the disabled path reports 0/0, NOT "all misses". The FSI doc
            // ("forces every memoize call down the Miss path") describes the net effect, not the counters.
            // The effectiveness is still proven: enabled reuses (hits≫0, misses→0), disabled reuses
            // nothing (hits=0, every node recomputed but uncounted).
            Expect.equal offMisses 0 "FINDING: disabled bypasses the memo seam — recomputes are UNCOUNTED (MemoMisses stays 0), not tallied as misses"
            Expect.equal onMisses 0 "the enabled steady-state stops missing (unchanged data ⇒ all hits)"
            Expect.isTrue (onRate > 0.95) (sprintf "enabled steady-state hit-rate is near-100%% (got %.3f over %d frames)" onRate frameCount)
            // Recorded margin: onHits/onMisses vs offHits (printed to the run log for the report).
            printfn "AUDIT-MARGIN MemoCache: enabled hits=%d misses=%d rate=%.3f | disabled hits=%d misses=%d (disabled bypasses seam: recomputes uncounted)" onHits onMisses onRate offHits offMisses
        }
    ]
