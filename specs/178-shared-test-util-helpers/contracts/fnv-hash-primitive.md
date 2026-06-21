# Contract: Shared FNV-1a Hash Primitive

**Visibility**: assembly-internal to `FS.GG.UI.Controls`. `module internal Hashing` in
`src/Controls/Internal/Hashing.fs`, **no `.fsi`** (the `Internal/AttrKeys.fs` precedent). Adds **no**
public/package surface and appears in no surface-area baseline.

## Shape (illustrative F# — finalize during implementation against byte-identity)

```fsharp
namespace FS.GG.UI.Controls

module internal Hashing =
    [<Literal>]
    let offsetBasis = 0xcbf29ce484222325UL
    [<Literal>]
    let prime = 0x100000001b3UL          // = 1099511628211UL

    /// Core FNV-1a step: xor then multiply.
    let inline step (h: uint64) (x: uint64) : uint64 = (h ^^^ x) * prime

    /// Fold over raw bytes (UTF-8/byte convention) from a given seed.
    let foldBytes (seed: uint64) (bytes: byte seq) : uint64

    // Char/value mix helpers are provided as needed so each existing site keeps
    // its exact convention (uint16-char widening vs int-char widening, length/
    // domain prefixes, separators). They MUST NOT change any site's output.
```

## Behavioral contract

1. **Constants are single-sourced** — `offsetBasis`/`prime` exist in exactly one place after
   migration; a repo-wide search for `0xcbf29ce484222325UL` outside `Hashing` returns zero (SC-003).
2. **Byte-identity per site** — each of the four migrated folds produces a bitwise-identical `uint64`
   to its pre-refactor result for **every** input, including empty byte sequence and empty string:
   - `Composition.fnv1a` — UTF-8 byte fold.
   - `Control.hashScene` — `uint64` mix with typed mixers (`mixStr` widens `char` via `uint16`).
   - `Control.fingerprintParts` / `fingerprintString` — `uint64` mix with domain/length prefixes.
   - `RetainedRender.feature159Hash` — per-char `int ch` fold, **separate** xor/multiply statements,
     `'|'` separator between parts; preserves the Phase-0-corrected baseline.
3. **No single universal `hash`** — the module is a primitive; site-specific mixing stays at the site
   (research R2). Do not collapse the four into one signature.
4. **Performance** — `step` is `inline` and allocation-free; hot-path sites keep their `mutable h`
   accumulator and current loop shape.

## Verification (no absolute-constant assertions)
Feature 159 identity/reuse/promotion suites + composition/control fingerprint tests stay green,
proving relational hash identity is unchanged.

## Acceptance mapping
- FR-004, FR-005; Acceptance Scenarios 1–3 of User Story 2; SC-003; FNV edge case (empty inputs).
