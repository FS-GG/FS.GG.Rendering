namespace FS.GG.UI.Canvas

/// Public contract type exposed by this FS.GG.UI package.
/// A seeded, value-type pseudo-random generator (SplitMix64). Two `Rng` with equal `State` are equal
/// and produce identical continuations, so it can be stored inside an immutable MVU `Model` without a
/// shared mutable `System.Random` — replay and clone stay deterministic and structural equality of a
/// model implies equal RNG state.
[<Struct>]
type Rng = { State: uint64 }

/// Public contract module exposed by this FS.GG.UI package.
[<RequireQualifiedAccess>]
module Rng =

    /// Public contract function exposed by this FS.GG.UI package.
    /// Seed a generator from a raw seed. A weak seed (e.g. `0UL`) still yields a non-degenerate sequence.
    val ofSeed: seed: uint64 -> Rng

    /// Public contract function exposed by this FS.GG.UI package.
    /// Draw a float in `[0.0, 1.0)` (0 inclusive, 1 exclusive) and the advanced generator. Pure: the
    /// input generator is unchanged.
    val nextFloat: rng: Rng -> struct (float * Rng)

    /// Public contract function exposed by this FS.GG.UI package.
    /// Draw an integer in `[lo, hi]` inclusive on both ends, and the advanced generator. Total on
    /// degenerate ranges: `lo = hi` yields `lo`; `lo > hi` yields `lo`.
    val nextInt: lo: int -> hi: int -> rng: Rng -> struct (int * Rng)

    /// Public contract function exposed by this FS.GG.UI package.
    /// Derive two independent generators from the current state, for splitting into sub-streams.
    val split: rng: Rng -> struct (Rng * Rng)
