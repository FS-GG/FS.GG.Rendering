/// A tiny pure linear-congruential generator (research R4). All in-game randomness
/// (Tetris 7-bag order, Snake food placement, Pong serve direction) is drawn from this,
/// seeded from the CLI `--seed`. No `System.Random`, no `Math.random`, no `Guid`, no
/// wall-clock — so randomness is a referentially-transparent function of the seed and two
/// same-seed runs are byte-identical (FR-006/SC-002). The PRNG is threaded through each
/// game's `Model`, keeping `update` pure (Principle IV).
module SampleApps.Core.Prng

/// The generator state. A single 64-bit word advanced by the MMIX LCG recurrence.
type Prng = { State: uint64 }

// MMIX (Knuth) LCG constants — a full-period 64-bit recurrence.
let private multiplier = 6364136223846793005UL
let private increment = 1442695040888963407UL

/// Seed the generator. The same `seed` always yields the same stream. A fixed odd
/// scrambler avoids a degenerate all-zero start while staying a pure function of `seed`.
let seed (s: int): Prng =
    { State = (uint64 (uint32 s) ^^^ 0x9E3779B97F4A7C15UL) * multiplier + increment }

/// Draw the next 32-bit value and the advanced generator. Returns the high 32 bits of the
/// post-step state (the high bits of an LCG have the best distribution).
let next (p: Prng): uint32 * Prng =
    let nextState = p.State * multiplier + increment
    let output = uint32 (nextState >>> 32)
    output, { State = nextState }

/// Draw a non-negative `int` strictly below `n` (`n > 0`), unbiased enough for game use.
let nextBelow (n: int) (p: Prng): int * Prng =
    if n <= 0 then
        0, p
    else
        let v, p' = next p
        int (v % uint32 n), p'

/// Fisher–Yates shuffle of a list, threading the generator. Pure: same generator + list
/// ⇒ same permutation.
let shuffle (xs: 'a list) (p: Prng): 'a list * Prng =
    let arr = List.toArray xs
    let mutable gen = p
    for i in (arr.Length - 1) .. -1 .. 1 do
        let j, g = nextBelow (i + 1) gen
        gen <- g
        let tmp = arr.[i]
        arr.[i] <- arr.[j]
        arr.[j] <- tmp
    List.ofArray arr, gen
