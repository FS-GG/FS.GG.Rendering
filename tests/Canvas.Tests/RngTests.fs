module Canvas.Tests.RngTests

// Feature 239 (US2): value-type seeded PRNG (SplitMix64). Deterministic replay, pure draws
// (input state unchanged), in-range integers/floats, splittable. All real pure computation.

open Expecto
open FsCheck
open FS.GG.UI.Canvas

// Draw `n` ints in [0,1000] by threading the generator; returns the value list.
let private drawSeq n (start: Rng) =
    let mutable rng = start
    [ for _ in 1..n ->
        let struct (v, next) = Rng.nextInt 0 1000 rng
        rng <- next
        v ]

[<Tests>]
let tests =
    testList "Feature 239 Rng SplitMix64 (US2, FR-006..FR-008)" [

        test "identical seed reproduces an identical sequence" {
            let s1 = drawSeq 32 (Rng.ofSeed 42UL)
            let s2 = drawSeq 32 (Rng.ofSeed 42UL)
            Expect.equal s1 s2 "same seed ⇒ byte-identical sequence"
        }

        test "different seeds produce different sequences" {
            let s1 = drawSeq 32 (Rng.ofSeed 1UL)
            let s2 = drawSeq 32 (Rng.ofSeed 2UL)
            Expect.notEqual s1 s2 "distinct seeds ⇒ distinct sequences (overwhelmingly likely)"
        }

        test "a draw is pure: the input generator is unchanged and reproduces its own next draw" {
            let r0 = Rng.ofSeed 7UL
            let struct (a, _r1) = Rng.nextInt 1 6 r0
            let struct (b, _r1b) = Rng.nextInt 1 6 r0
            Expect.equal a b "drawing twice from the SAME state yields the SAME value (no mutation)"
        }

        test "a zero seed still yields a non-degenerate sequence" {
            let s = drawSeq 16 (Rng.ofSeed 0UL)
            Expect.isTrue (List.distinct s |> List.length > 1) "0UL seed is not stuck on a constant"
        }

        test "structural equality of Rng implies an identical continuation (SC-002)" {
            let r0 = Rng.ofSeed 99UL
            let struct (_, r1) = Rng.nextInt 0 1000 r0
            let clone = { State = r1.State } // a copied value
            Expect.equal r1 clone "equal state ⇒ equal Rng"
            Expect.equal (drawSeq 20 r1) (drawSeq 20 clone) "equal state ⇒ identical continuation"
        }

        testCase "nextInt stays within [lo, hi] inclusive (FsCheck ≥1000 cases)" <| fun () ->
            let prop (seed: uint64) (a: int) (b: int) =
                let lo = min (a % 1000) (b % 1000)
                let hi = max (a % 1000) (b % 1000)
                let struct (v, _) = Rng.nextInt lo hi (Rng.ofSeed seed)
                v >= lo && v <= hi
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

        test "nextInt with lo = hi returns lo" {
            let struct (v, _) = Rng.nextInt 5 5 (Rng.ofSeed 3UL)
            Expect.equal v 5 "degenerate single-value range ⇒ lo"
        }

        test "nextInt with lo > hi returns lo (total, non-throwing)" {
            let struct (v, _) = Rng.nextInt 10 2 (Rng.ofSeed 3UL)
            Expect.equal v 10 "inverted range ⇒ lo, no exception"
        }

        testCase "nextFloat stays in [0.0, 1.0) (FsCheck ≥1000 cases)" <| fun () ->
            let prop (seed: uint64) =
                let struct (f, _) = Rng.nextFloat (Rng.ofSeed seed)
                f >= 0.0 && f < 1.0
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

        test "split yields two generators with differing streams" {
            let struct (l, r) = Rng.split (Rng.ofSeed 123UL)
            Expect.notEqual (drawSeq 24 l) (drawSeq 24 r) "the two sub-streams diverge"
            Expect.notEqual l.State r.State "the two states differ"
        }
    ]
