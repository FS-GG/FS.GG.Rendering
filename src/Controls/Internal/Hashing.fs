namespace FS.GG.UI.Controls

// Feature 178 (US2): the single source of the FNV-1a constants and core mixing step shared by the
// four `src/Controls` hash folds (Composition.fnv1a, Control.hashScene, Control.fingerprint*,
// RetainedRender.feature159Hash). `module internal` with no `.fsi` — assembly-internal, off the
// public surface (the `Internal/AttrKeys.fs` precedent), compiled before `Composition.fs`.
//
// This is a PRIMITIVE, not a single universal hash: each call site keeps its own mixing
// convention (UTF-8 bytes vs UTF-16 char widening vs per-`int ch` fold, length/domain prefixes,
// separators) and draws only the constants and the core `step` from here, so every fold stays
// byte-identical to its pre-refactor output.
module internal Hashing =

    /// FNV-1a 64-bit offset basis.
    [<Literal>]
    let offsetBasis = 0xcbf29ce484222325UL

    /// FNV-1a 64-bit prime (= 1099511628211UL).
    [<Literal>]
    let prime = 0x100000001b3UL

    /// Core FNV-1a step: xor then multiply. `inline` and allocation-free so hot-path folds keep
    /// their `mutable` accumulator shape.
    let inline step (h: uint64) (x: uint64) : uint64 = (h ^^^ x) * prime

    /// Fold the FNV-1a step over raw bytes (the UTF-8/byte convention) from a given seed.
    let foldBytes (seed: uint64) (bytes: byte seq) : uint64 =
        let mutable h = seed // mutable: hot path / FNV-1a accumulator
        for b in bytes do
            h <- step h (uint64 b)
        h
