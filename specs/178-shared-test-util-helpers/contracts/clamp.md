# Contract: Shared `clamp`

**Visibility**: assembly-internal to each consumer. `module internal` in `src/Shared/Numeric.fs`,
**no `.fsi`**, **linked** (`<Compile Include="..\Shared\Numeric.fs"><Link>Shared/Numeric.fs</Link>`)
into `src/Controls` and `src/SkiaViewer`. Adds **no** public/package surface; introduces no new
project/package and no project-reference edge.

## Shape (illustrative F#)

```fsharp
namespace FS.GG.UI            // or the consumer's root namespace; one source, linked per assembly

module internal Numeric =
    /// value constrained to [lo, hi]. Argument order: (lo, hi, value).
    let inline clamp lo hi value = min hi (max lo value)
```

## Behavioral contract

1. **Semantics** — `clamp lo hi value = min hi (max lo value)`; identical to all three removed local
   copies (each was already `min high (max low value)`), including boundary and inverted-range cases.
2. **Argument order** — `(lo, hi, value)`. (`TextInput`'s `value |> max low |> min high` is the same
   value with the same order; call sites read identically after migration.)
3. **Single definition** — after migration a repo-wide search finds exactly one `let clamp` (SC-004).
4. **Out of scope** — `Layout.clampNonNegative` (single-arg, returns `0.0` for negatives) is a
   different function and is left untouched; inline `max/min` expressions and BCL `Math.Clamp` uses
   are not `clamp` definitions and need not be migrated.

## Consumers (migration target)
`src/SkiaViewer/Host/OpenGl.fs:461`, `src/Controls/RetainedRender.fs:714`,
`src/Controls/TextInput.fs:45` — delete the local `let clamp`, use `Numeric.clamp`.

## Verification
Layout-sizing, text-caret, and viewer-scaling tests stay green; clamped values unchanged.

## Acceptance mapping
- FR-006; Acceptance Scenarios 1–2 of User Story 3; SC-004.
