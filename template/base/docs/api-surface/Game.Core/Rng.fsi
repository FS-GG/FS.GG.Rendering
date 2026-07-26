// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Rng.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A seeded, value-type pseudo-random generator (SplitMix64). Two `Rng` with equal `State` are equal
/// and produce identical continuations, so it can be stored inside an immutable MVU `Model` without a
/// shared mutable `System.Random` — replay and clone stay deterministic and structural equality of a
/// model implies equal RNG state.
[<Struct>]
type Rng = { State: uint64 }

/// Public contract module exposed by the FS.GG.Game.Core package.
[<RequireQualifiedAccess>]
module Rng =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Seed a generator from a raw seed. A weak seed (e.g. `0UL`) still yields a non-degenerate sequence.
    val ofSeed: seed: uint64 -> Rng

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Draw a float in `[0.0, 1.0)` (0 inclusive, 1 exclusive) and the advanced generator. Pure: the
    /// input generator is unchanged.
    val nextFloat: rng: Rng -> struct (float * Rng)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Draw an integer in `[lo, hi]` inclusive on both ends, and the advanced generator. Total on
    /// degenerate ranges: `lo = hi` yields `lo`; `lo > hi` yields `lo`. Reduces one 64-bit draw with a
    /// single modulo, so — unlike `nextBool` — it carries a residual modulo bias: when the span does not
    /// divide 2^64 evenly the lowest `2^64 mod span` results are over-represented by ~span/2^64
    /// (negligible for game-scale ranges; use `nextBool` when a modulus-free unbiased draw is required).
    val nextInt: lo: int -> hi: int -> rng: Rng -> struct (int * Rng)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Draw a boolean and the advanced generator from the high bit of one step — a fair, deterministic
    /// coin flip without a modulus. Pure: the input generator is unchanged.
    val nextBool: rng: Rng -> struct (bool * Rng)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Split into two sub-streams for divergent branches (e.g. per-entity generators). The left
    /// continues the current stream; the right is re-seeded from a mixed draw. Both share the global
    /// gamma, so the sub-streams are practically decorrelated (overlap risk ~2^-64) rather than the
    /// paper's independent-gamma split. Pure: the input generator is unchanged.
    val split: rng: Rng -> struct (Rng * Rng)
