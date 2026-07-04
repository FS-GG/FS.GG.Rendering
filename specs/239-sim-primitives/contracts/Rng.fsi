// CONTRACT SKETCH for src/Canvas/Rng.fsi — the intended public surface (Phase 1).
// SplitMix64 (research D4). Value type so it lives inline in an immutable Model with no shared
// mutable state; every draw returns (value, nextState) and never mutates the input.
namespace FS.GG.UI.Canvas

/// Public contract type exposed by this FS.GG.UI package.
/// A seeded, value-type pseudo-random generator. Two `Rng` with equal state are equal and
/// produce identical continuations (structural equality ⇒ equal RNG state), so it can be stored
/// in an immutable MVU `Model` without breaking replay/clone determinism.
[<Struct>]
type Rng = { State: uint64 }

/// Public contract module exposed by this FS.GG.UI package.
[<RequireQualifiedAccess>]
module Rng =

    /// Seed a generator. A weak seed (e.g. 0UL) still yields a non-degenerate sequence.
    val ofSeed: seed: uint64 -> Rng

    /// Draw a float in [0.0, 1.0) (0 inclusive, 1 exclusive) and the advanced generator.
    val nextFloat: rng: Rng -> struct (float * Rng)

    /// Draw an integer in [lo, hi] inclusive on both ends, and the advanced generator.
    /// Degenerate ranges are total: `lo = hi` yields `lo`; `lo > hi` yields `lo`.
    val nextInt: lo: int -> hi: int -> rng: Rng -> struct (int * Rng)

    /// Derive two independent generators from the current state (for sub-streams).
    val split: rng: Rng -> struct (Rng * Rng)
