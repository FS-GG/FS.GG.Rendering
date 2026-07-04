namespace FS.GG.UI.Canvas

[<Struct>]
type Rng = { State: uint64 }

[<RequireQualifiedAccess>]
module Rng =

    // SplitMix64 (Steele/Lea/Flood). `advance` bumps the state by the golden-ratio odd constant;
    // `mix` is the finalizer that turns a state into a well-distributed 64-bit output. Both pure.
    [<Literal>]
    let private gamma = 0x9E3779B97F4A7C15UL

    let private mix (z0: uint64) : uint64 =
        let z1 = (z0 ^^^ (z0 >>> 30)) * 0xBF58476D1CE4E5B9UL
        let z2 = (z1 ^^^ (z1 >>> 27)) * 0x94D049BB133111EBUL
        z2 ^^^ (z2 >>> 31)

    // Offset the raw seed by the SplitMix64 gamma; the first draw then mixes it, so even a weak seed
    // (e.g. 0UL) yields a non-degenerate stream.
    let ofSeed (seed: uint64) : Rng = { State = seed + gamma }

    let private nextU64 (rng: Rng) : struct (uint64 * Rng) =
        let s = rng.State + gamma
        struct (mix s, { State = s })

    // Top 53 bits scaled into [0,1) — the standard double-in-unit-interval construction.
    let nextFloat (rng: Rng) : struct (float * Rng) =
        let struct (bits, next) = nextU64 rng
        struct (float (bits >>> 11) * (1.0 / 9007199254740992.0), next)

    // Inclusive [lo, hi]. Total on degenerate ranges: lo = hi ⇒ lo; lo > hi ⇒ lo.
    let nextInt (lo: int) (hi: int) (rng: Rng) : struct (int * Rng) =
        if lo >= hi then
            let struct (_, next) = nextU64 rng
            struct (lo, next)
        else
            let struct (bits, next) = nextU64 rng
            let span = uint64 (int64 hi - int64 lo + 1L)
            struct (lo + int (bits % span), next)

    // Two independent generators: the left continues the stream, the right is seeded from a mixed draw.
    let split (rng: Rng) : struct (Rng * Rng) =
        let struct (branch, next) = nextU64 rng
        struct (next, { State = mix (branch + gamma) })
