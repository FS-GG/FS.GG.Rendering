// Feature 178 (US3): the single shared `clamp`, linked (not project-referenced) into
// `src/Controls` and `src/SkiaViewer` as `module internal` with no `.fsi`. One source definition,
// assembly-internal in each consumer, zero public/package surface and no new project edge.
//
// Declared in the global namespace so the linked source resolves as `Numeric.clamp` in every
// consuming file without an extra `open`. `Layout.clampNonNegative` is a different (single-arg)
// function and is intentionally left untouched.
module internal Numeric

/// `value` constrained to `[lo, hi]`. Argument order: `(lo, hi, value)`;
/// `clamp lo hi value = min hi (max lo value)`.
let inline clamp lo hi value = min hi (max lo value)
