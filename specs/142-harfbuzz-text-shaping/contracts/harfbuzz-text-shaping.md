# Contract: HarfBuzz Text Shaping

Feature 142 makes shaped text the authoritative contract for measurement, drawing, fingerprints, cache/reuse
evidence, fallback diagnostics, and baseline disclosure while preserving deterministic pure fallback behavior.

## 1. Provider Boundary

Contract:

1. `src/Scene` must not reference HarfBuzzSharp, SkiaSharp, SkiaViewer, Silk.NET, Yoga, Controls, Elmish, or
   native host packages.
2. HarfBuzz and Skia typeface/font/shaper objects are owned by `src/SkiaViewer`.
3. Provider installation must be explicit and clearable.
4. Provider availability, provider failure, and provider version bucket must be visible in diagnostics or
   shaped-result evidence.
5. When the provider is absent or cleared, existing pure fallback measurement/drawing remains available and
   compatible.

## 2. Shaped Result Contract

Contract:

1. A shaped result contains source text, requested font, provider evidence, run metadata, glyph ids, advances,
   offsets, clusters, resolved face/fallback identity, aggregate metrics, diagnostics, and a deterministic
   fingerprint.
2. Measurement reads aggregate metrics from the shaped result.
3. Drawing emits the glyph ids and positions from the shaped result.
4. The painter must not reshape or draw the original string as a substitute for a successful shaped result.
5. Recomputing the fingerprint from equivalent shaped data must produce the same value across repeated runs.

## 3. Run Itemization Contract

Contract:

1. Each HarfBuzz buffer represents one homogeneous run: same font face, size, weight, direction, and script
   bucket.
2. Font fallback can split a source string into multiple runs.
3. Right-to-left and mixed-direction fixtures must record deterministic run order and direction evidence.
4. Unsupported bidi controls, paragraph layout, line breaking, hyphenation, justification, caret, selection, and
   editing concerns are out of scope and must produce diagnostics if encountered by validation fixtures.

## 4. Measure/Draw Parity Contract

Contract:

1. For shaping-enabled fixtures, measured advance and drawn advance differ by no more than one rendered pixel.
2. Combining marks, ligatures, right-to-left runs, mixed-direction runs, emoji/symbol sequences, and
   representative complex scripts fit inside their expected bounds.
3. Layout and drawing use the same shaped result instance or equivalent cached shaped data.
4. A shaping failure must return explicit fallback evidence or diagnostics, not crash or silently return
   incorrect metrics.

## 5. Cache and Reuse Contract

Contract:

1. Shaped cache keys include every input that can alter output: source text, requested family, resolved family
   or face, size, weight, direction, script, language if present, fallback outcome, provider availability,
   provider version bucket, and shaping feature flags.
2. Cache-enabled and cache-disabled rendering produce equivalent output, metrics, diagnostics, and fingerprints.
3. Retained warm frames may reuse unchanged shaped data and must record reuse evidence.
4. Relevant input changes must miss the cache and prevent stale reuse.
5. Repeated stable frames produce at most one fresh shaped result per unique unchanged text input.

## 6. Fallback and Missing-Glyph Contract

Contract:

1. Font fallback and missing-glyph outcomes produce actionable diagnostics identifying affected text, code
   points or clusters, requested font, and resolved fallback or missing-glyph path.
2. Negative fixtures must identify 100% of affected code points or segments.
3. Reading fallback diagnostics must not mutate successful render output.
4. Missing glyphs must never be reported as authored glyph success.

## 7. Pure Fallback Compatibility Contract

Contract:

1. With the provider absent or cleared, existing pure fallback text measurement and drawing remain compatible.
2. Pure fallback verification reports zero baseline changes unless a separate compatibility decision is made.
3. Pure fallback fingerprints and diagnostics remain deterministic across repeated runs.
4. Shaping-enabled improvements must not erase the no-provider code path.

## 8. Public Surface and Dependency Contract

Contract:

1. Any public `.fsi` change is designed before `.fs` implementation.
2. Semantic tests exercise the public surface through the curated `.fsi` API.
3. Surface baselines are updated for every intentional public delta.
4. Dependency changes are pinned through `Directory.Packages.props` and documented with versioning rationale.
5. Any downstream exhaustive-match risk from `SceneNode` or DU changes is called out in readiness.

## 9. Baseline Disclosure Contract

Contract:

1. Every intentional text pixel, golden, diagnostic, or surface change has a ledger entry.
2. Ledger entries state the fixture/scenario, reason, migration impact, versioning rationale, and evidence
   command/output.
3. Shaping-enabled deltas are acceptable only when tied to shaped glyph selection, positioning, fallback, or
   diagnostics.
4. Pre-existing limitations and environment failures are separated from feature regressions.

## 10. Evidence Mapping

| Contract clause | Required evidence |
|---|---|
| Provider boundary | Scene project dependency check; SkiaViewer provider install/clear tests |
| Shaped result | Scene shaped data tests for glyphs, metrics, diagnostics, fingerprint |
| Run itemization | Fixture tests for LTR, RTL, mixed direction, fallback split, unsupported bidi diagnostics |
| Measure/draw parity | SkiaViewer rendered bounds tests and advance comparison <= 1 pixel |
| Cache/reuse | Cache-on/off oracle, retained cold/warm parity, stale-prevention tests |
| Fallback/missing glyph | Negative fixtures with diagnostics covering all affected code points or segments |
| Pure fallback | No-provider verification with zero pure fallback baseline changes |
| Public surface/dependency | Package surface check, central package pin, migration/versioning note |
| Baseline disclosure | Readiness ledger for every intentional shaped pixel/diagnostic/surface delta |

## 11. Non-Goals

- Full paragraph layout engine
- Full line breaking, hyphenation, or justification
- Text editing, selection, caret movement, or input method behavior
- Portable scene serialization or browser rendering
- Overlay interaction state
- Compositor promotion or damage-scissored presentation
- Intrinsic layout protocol
