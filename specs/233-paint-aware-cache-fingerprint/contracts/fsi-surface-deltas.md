# Contract / `.fsi` surface deltas

**Result: none.** This feature changes no public surface.

- `src/Canvas/Elements.fsi` — **unchanged**. `Elements.cached: key: string -> scene: Scene -> Scene`
  keeps its signature; the fix is entirely inside the private fold.
- All new mixers (`mixPaint`, `mixShader`, `mixStroke`, `mixClip`, `mixGlyphRun`, …) are `private` to the
  `Elements` module and never appear in the `.fsi`.
- No `Scene` type changed; `CacheBoundary` shape is untouched (only the *value* placed in
  `Fingerprint` becomes comprehensive).

No cross-repo contract (`fs-gg-ui-template`, registry `dependencies.yml`) is touched. The behavioural
fix ships in the next batched `fs-gg-ui` coherent-set release; no version bump in this feature merge.
