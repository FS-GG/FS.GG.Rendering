module Feature113MemoSeamTests

// Feature 113 (US1, FR-001/FR-004/FR-005, contract C1–C3) — the control-internal memoization seam
// `RetainedRender.memoize`. Reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]`; per
// the vertical-slice rule the in-assembly Expecto test IS the user-reachable surface for this internal
// user story. A HIT reuses the stored subtree instance WITHOUT running the thunk (the thunk is
// instrumented to assert non-invocation); a changed or cold dependency MISSES; equality is structural,
// never object identity; the cache is keyed per `ControlId`.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private scene (tag: float) : Scene list = [ Scene.rectangle (tag, 0.0, 10.0, 10.0) Colors.black ]

[<Tests>]
let tests =
    testList "Feature 113 memo seam (US1, FR-001/FR-004/FR-005, C1–C3)" [

        test "a cold first call is a Miss: runs the thunk once and stores the result (C2)" {
            let mutable calls = 0

            let _, cache, outcome =
                RetainedRender.memoize "grid" ("a" :> obj) (fun () -> calls <- calls + 1; scene 1.0) Map.empty

            Expect.equal outcome Miss "a cold dependency is a Miss"
            Expect.equal calls 1 "the thunk ran exactly once on the miss"
            Expect.isTrue (Map.containsKey "grid" cache) "the miss stored an entry under the ControlId"
        }

        test "a stable dependency HITS: reuses the stored subtree instance, thunk NOT run (C1)" {
            let mutable calls = 0

            let firstSubtree, cache1, _ =
                RetainedRender.memoize "grid" ("a" :> obj) (fun () -> calls <- calls + 1; scene 1.0) Map.empty

            let secondSubtree, _, outcome =
                RetainedRender.memoize "grid" ("a" :> obj) (fun () -> calls <- calls + 1; scene 2.0) cache1

            Expect.equal outcome Hit "an equal dependency is a Hit"
            Expect.equal calls 1 "the thunk did NOT run on the hit (still 1 from the miss)"
            Expect.isTrue (System.Object.ReferenceEquals(firstSubtree, secondSubtree)) "the hit returns the SAME subtree instance (reference-reused)"
        }

        test "a changed dependency MISSES: re-runs the thunk and stores a fresh subtree (C2/C3)" {
            let mutable calls = 0

            let _, cache1, _ =
                RetainedRender.memoize "grid" ("a" :> obj) (fun () -> calls <- calls + 1; scene 1.0) Map.empty

            let fresh, _, outcome =
                RetainedRender.memoize "grid" ("b" :> obj) (fun () -> calls <- calls + 1; scene 2.0) cache1

            Expect.equal outcome Miss "an unequal dependency is a Miss (never reuses across an unequal dep)"
            Expect.equal calls 2 "the thunk ran again for the changed dependency"
            Expect.equal fresh (scene 2.0) "the fresh subtree reflects the recompute"
        }

        test "equality is STRUCTURAL, not object identity: two equal-but-distinct boxed deps HIT (FR-005)" {
            let mutable calls = 0
            let depA = (("x", 1) :> obj)
            let depB = (("x", 1) :> obj) // structurally equal, a distinct boxed instance

            Expect.isFalse (System.Object.ReferenceEquals(depA, depB)) "the two boxes are distinct instances"

            let _, cache1, _ = RetainedRender.memoize "grid" depA (fun () -> calls <- calls + 1; scene 1.0) Map.empty
            let _, _, outcome = RetainedRender.memoize "grid" depB (fun () -> calls <- calls + 1; scene 2.0) cache1

            Expect.equal outcome Hit "structurally-equal dependencies hit (structural =, never reference)"
            Expect.equal calls 1 "no recompute for a structurally-equal dependency"
        }

        test "the cache is per ControlId: a never-seen id misses even with an equal dependency" {
            let _, cache1, _ = RetainedRender.memoize "gridA" ("a" :> obj) (fun () -> scene 1.0) Map.empty
            let _, _, outcome = RetainedRender.memoize "gridB" ("a" :> obj) (fun () -> scene 2.0) cache1
            Expect.equal outcome Miss "a different ControlId is an independent cold Miss"
        }
    ]
