// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Dice.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A discrete integer probability **distribution** — a map from outcome to a positive integer weight
/// (int64, so a convolution of many dice does not overflow). Opaque: built only through `Dice`
/// constructors/combinators, which maintain the invariant that every weight is `> 0`; read it back
/// with `Dice.outcomes` / `Dice.totalWeight`. Structural equality makes it a golden-testable value.
type Distribution

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Dice / damage **distribution** math (roadmap 3.1): build integer dice distributions, combine them
/// (`convolve` for the sum of independent rolls, `advantage`/`disadvantage` for max/min), read exact
/// moments (`mean`/`variance`), and draw seeded samples through the shipped `Rng`. Pure integer values
/// — deterministic combat math; `sample` is deterministic given the `Rng` seed (no ambient RNG).
[<RequireQualifiedAccess>]
module Dice =

    /// The outcome -> weight pairs, in ascending outcome order.
    val outcomes: d: Distribution -> (int * int64) list

    /// The total weight (sum over all outcomes).
    val totalWeight: d: Distribution -> int64

    /// The single-outcome distribution `{ v -> 1 }`.
    val constant: v: int -> Distribution

    /// The uniform distribution over `lo..hi` (each weight 1); empty when `hi < lo`.
    val uniform: lo: int -> hi: int -> Distribution

    /// A single `sides`-sided die — the uniform distribution over `1..sides`.
    val die: sides: int -> Distribution

    /// The **sum** distribution of two independent rolls (`X + Y`): outcome `va + vb` gets weight
    /// `wa * wb`, summed over collisions. Associative and commutative.
    val convolve: a: Distribution -> b: Distribution -> Distribution

    /// `d` convolved with itself `n` times — the distribution of the sum of `n` independent rolls of
    /// `d`. `repeat 0 d = constant 0`; a negative `n` is treated as `0`.
    val repeat: n: int -> d: Distribution -> Distribution

    /// The distribution of `max(X, Y)` for independent `X ~ a`, `Y ~ b` — roll twice, keep the higher
    /// ("advantage"). Its `mean` is `>=` either input's.
    val advantage: a: Distribution -> b: Distribution -> Distribution

    /// The distribution of `min(X, Y)` for independent `X ~ a`, `Y ~ b` — roll twice, keep the lower
    /// ("disadvantage"). Its `mean` is `<=` either input's.
    val disadvantage: a: Distribution -> b: Distribution -> Distribution

    /// The exact weighted **mean** `Sum(v * w) / Sum(w)` (a deterministic `float`; `0.0` for an empty
    /// distribution).
    val mean: d: Distribution -> float

    /// The exact weighted **variance** `Sum((v - mean)^2 * w) / Sum(w)` (a deterministic `float`; `0.0`
    /// for an empty distribution).
    val variance: d: Distribution -> float

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Draw an outcome with probability proportional to its weight, threading and returning the `Rng`.
    /// **Deterministic given the seed** — the same `Rng` reproduces the same sample sequence — and the
    /// empirical mean over many draws converges to `mean`. An empty distribution returns `(0, rng)`, a
    /// documented total fallback (constructors never yield an empty distribution except a degenerate
    /// `uniform hi lo`).
    val sample: d: Distribution -> rng: Rng -> struct (int * Rng)
