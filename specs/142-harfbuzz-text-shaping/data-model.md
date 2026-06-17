# Data Model: HarfBuzz Text Shaping (Feature 142)

This feature has no persisted data model. The entities below describe public shaped-text evidence, provider
state, cache/reuse state, diagnostics, and readiness evidence that implementation and tests must pin.

## ShapingProvider

Represents the installed text-shaping capability at the rendering edge.

**Fields**
- `availability`: Installed, cleared, unavailable, or failed.
- `providerId`: Stable provider label, for example `harfbuzz-skiasharp`.
- `versionBucket`: Stable dependency/version evidence folded into fingerprints and diagnostics.
- `failure`: Optional native asset, package, typeface, or shaping failure diagnostic.

**Relationships**
- `SkiaViewer.Text` installs or clears the provider.
- `ShapedTextResult` records the provider that shaped or declined the run.
- Pure fallback mode is selected when no provider is installed or a run falls back explicitly.

**Validation Rules**
- Provider absence must produce deterministic fallback behavior.
- Provider failure must be diagnostic, not silent.
- Provider availability changes invalidate shaped cache entries and retained reuse evidence.

## ShapingRequest

Represents one request to shape a text value.

**Fields**
- `text`: Original source text.
- `font`: Existing `FontSpec` request.
- `paint`: Optional paint context when shaping needs antialias or fill/stroke evidence.
- `direction`: Auto, left-to-right, or right-to-left.
- `script`: Auto or detected script bucket for fixture evidence.
- `language`: Optional language tag if introduced by implementation.
- `sourcePath`: Origin such as Scene text, TextRun, SizedText, Controls text block, rich text run, or text input.

**Relationships**
- Controls and Scene constructors produce shaping requests before layout/draw.
- A request itemizes into one or more `TextShapeRun` values.
- Cache keys are derived from the request plus provider/fallback outcomes.

**Validation Rules**
- Empty, whitespace-only, and newline-only strings must shape or fallback deterministically.
- Changes to any shaping-affecting field must invalidate prior shaped evidence.
- Unsupported paragraph/text-editing concerns remain outside the request.

## TextShapeRun

Represents one homogeneous run sent to HarfBuzz.

**Fields**
- `textRange`: Source start and length in scalar or UTF-16 index terms chosen by implementation.
- `sourceText`: The source substring for diagnostics.
- `resolvedFont`: Stable family/face/weight/size identity used for shaping.
- `direction`: Concrete direction used for the run.
- `script`: Concrete script bucket when available.
- `fallbackDecision`: Authored face, substituted face, missing glyph, or pure fallback.
- `glyphs`: Ordered shaped glyphs.
- `advance`: Sum of glyph advances for the run.
- `diagnostics`: Run-local fallback, unsupported bidi, missing glyph, or shaping failure messages.

**Relationships**
- A `ShapedTextResult` owns one or more runs.
- A run contributes `ShapedGlyph` records to `GlyphRunData`.
- Bidi and font fallback itemization produce run boundaries.

**Validation Rules**
- Every run sent to HarfBuzz has one font, size, weight, direction, and script bucket.
- Mixed-direction fixtures must record visual run ordering evidence.
- Unsupported bidi controls or paragraph cases must be disclosed and must not return false precision.

## ShapedGlyph

Represents one drawable glyph in stable package-owned data.

**Fields**
- `glyphId`: Glyph id returned by the shaping provider or deterministic fallback glyph id.
- `sourceCluster`: Source cluster index from HarfBuzz or deterministic fallback mapping.
- `sourceText`: Source text segment represented by the glyph.
- `resolvedFace`: Stable face/fallback identity needed to draw and diagnose the glyph.
- `advance`: Horizontal advance in device-independent units.
- `offset`: Per-glyph x/y offset.
- `position`: Glyph position relative to the shaped result origin.
- `missing`: Whether this glyph represents missing-glyph/tofu output.

**Relationships**
- `GlyphRunData.Glyphs` stores shaped glyphs.
- `SceneRenderer` draws glyph ids and positions from this data instead of drawing the original string.

**Validation Rules**
- The aggregate of advances equals result metrics within one rendered pixel.
- Combining marks and ligatures preserve cluster evidence.
- Missing glyphs carry diagnostics with source context.

## ShapedTextMetrics

Represents measurement derived from shaped output.

**Fields**
- `advance`: Total shaped advance.
- `width`: Visual or layout width chosen by implementation.
- `height`: Height used by layout.
- `baseline`: Baseline used by layout and drawing.
- `bounds`: Optional visible bounds if implementation can record them.

**Relationships**
- `TextMetrics` projections read from this entity.
- Controls layout uses this entity through the widened measurement seam.

**Validation Rules**
- Layout and drawing must use this same metrics source.
- Pure fallback metrics remain byte-compatible when provider is absent.
- Bounds must include combining marks and offsets for fixture cases.

## ShapedTextResult

The authoritative output for one shaped text request.

**Fields**
- `text`: Source text.
- `font`: Requested `FontSpec`.
- `provider`: Provider evidence.
- `runs`: Ordered `TextShapeRun` values.
- `glyphs`: Flattened stable glyph list for Scene/portable evidence.
- `metrics`: `ShapedTextMetrics`.
- `diagnostics`: Aggregated diagnostics.
- `fingerprint`: Deterministic fingerprint over every render-affecting input and output.
- `fallbackMode`: Shaped, pure fallback, provider unavailable, or shaping failed fallback.

**Relationships**
- Evolves current `GlyphRunData`.
- Measurement, drawing, cache/reuse, diagnostics, and readiness evidence all reference this result.
- `GlyphRun` Scene nodes wrap this result for drawing.

**Validation Rules**
- Fingerprints are byte-stable across repeated equivalent runs.
- Provider/fallback/input changes produce different fingerprints when output can change.
- A result may not claim shaped success if drawing later ignores glyph ids or positions.

## ShapingCacheEntry

Represents reusable shaped output.

**Fields**
- `key`: Text, requested font, resolved font/fallback, direction, script, language, provider bucket, and feature flags.
- `result`: `ShapedTextResult`.
- `lastUsed`: Deterministic LRU or retained frame counter.
- `reuseEvidence`: Hit/miss/fresh/stale-prevented information.

**Relationships**
- Retained rendering and Controls text paths consume this entry.
- Cache-disabled verification bypasses entries but must produce equivalent output.

**Validation Rules**
- Entries are bounded if stored across frames.
- Cache-disabled and cache-enabled output/metrics remain equivalent.
- A relevant input change must miss rather than reuse stale shaped data.

## FallbackDisclosure

Represents actionable evidence for substituted or missing glyphs.

**Fields**
- `sourceText`: Affected text or segment.
- `codePoints`: Affected code points or cluster identifiers.
- `requestedFont`: Requested family/weight/size.
- `resolvedFont`: Substitute family/face if any.
- `resolution`: Authored, substituted, missing glyph, pure fallback, or provider failure.
- `message`: Human-readable diagnostic.

**Relationships**
- `ShapedTextResult.Diagnostics` aggregates disclosures.
- `SkiaViewer.Text.fallbackReport` and `fallbackDiagnostics` expose disclosures after rendering.
- Readiness evidence summarizes fixture and negative-fixture disclosure coverage.

**Validation Rules**
- Negative fixtures identify 100% of affected code points or segments.
- Successful rendering is not changed by merely reading diagnostics.
- Missing glyphs must never be silently treated as authored glyphs.

## TextParityOracle

Represents comparison evidence across render modes.

**Fields**
- `fixtureId`: Text fixture or generated scene id.
- `mode`: Direct, cold retained, warm retained, cache-enabled, cache-disabled, pure fallback, or shaping-enabled.
- `metrics`: Captured shaped or fallback metrics.
- `fingerprint`: Captured glyph-run fingerprint.
- `diagnostics`: Captured diagnostics.
- `pixelEvidence`: Optional screenshot/hash/bounds evidence.
- `status`: Pass, intentional delta, environment limitation, or pre-existing limitation.

**Relationships**
- Quickstart validation and readiness reports produce parity records.
- Baseline disclosure ledger references intentional deltas.

**Validation Rules**
- Equivalent inputs across modes produce equivalent visible output, metrics, diagnostics, and fingerprints.
- Pure fallback mode reports zero fallback-baseline changes.
- Any environment limitation must be separated from feature regressions.

## BaselineDisclosureLedger

Represents intentional surface, diagnostic, or pixel changes.

**Fields**
- `scenario`: Fixture or public surface affected.
- `changeType`: Pixel, golden, diagnostic, dependency, public surface, or package surface.
- `reason`: Why the shaping behavior changes the baseline.
- `migration`: Required downstream action, if any.
- `versioning`: Preview-compatible, minor signal, major signal, or deferred decision.
- `evidence`: Command and artifact paths that prove the change.

**Relationships**
- Required for every intentional text golden, diagnostic, or public surface delta.
- Referenced by readiness before implementation can be accepted.

**Validation Rules**
- No intentional delta is accepted without a ledger entry.
- Pure fallback deltas must remain zero unless a separate compatibility decision is made.

## State Transitions

1. No provider installed: existing pure fallback measurement/drawing remains active.
2. SkiaViewer installs the shaping provider and declares provider availability.
3. Existing text paths create `ShapingRequest` values before measurement.
4. Requests itemize into homogeneous `TextShapeRun` values.
5. HarfBuzz shapes each eligible run; unsupported or failed runs return explicit fallback results.
6. `ShapedTextResult` is fingerprinted, cached, and used for measurement.
7. Scene drawing emits `GlyphRun` shaped data.
8. SkiaViewer draws glyph ids and positions from shaped data.
9. Retained/cached frames reuse unchanged shaped results and disclose reuse evidence.
10. Readiness records parity, fallback diagnostics, and baseline ledger entries.
