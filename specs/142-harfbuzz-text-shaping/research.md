# Research: HarfBuzz Text Shaping (Feature 142)

## Decision: Use SkiaSharp.HarfBuzz at the SkiaViewer edge

Use `SkiaSharp.HarfBuzz` as the HarfBuzz integration point and pin it through central package management to the
same SkiaSharp version train already used by the repository. For the current checkout, that means planning
against `SkiaSharp.HarfBuzz` `4.147.0-preview.3.1` because `Directory.Packages.props` already pins `SkiaSharp`
`4.147.0-preview.3.1`.

**Rationale**: The NuGet package is explicitly the SkiaSharp text-shaping add-on, supports `net10.0`, and depends
on compatible `SkiaSharp` and `HarfBuzzSharp` versions for the same preview train. The Microsoft Learn API shows
`SKShaper.Shape` overloads that shape either a `HarfBuzzSharp.Buffer` or a string with an `SKFont`; the
non-obsolete overloads use `SKFont`, which matches the repository's SkiaSharp 4 drawing style.

**Alternatives considered**:
- Direct `HarfBuzzSharp` integration: more control, but more native-handle and glyph-position plumbing than
  needed for this Skia-backed viewer.
- Stable `SkiaSharp.HarfBuzz` 3.x: lower preview risk, but mismatched with the repository's current SkiaSharp
  4.x preview train.
- No new dependency and keep per-character Skia drawing: preserves current behavior but cannot satisfy ligature,
  kerning, complex-script, or measure/draw parity requirements.

Sources:
- NuGet `SkiaSharp.HarfBuzz` `4.147.0-preview.3.1`: https://www.nuget.org/packages/SkiaSharp.HarfBuzz/4.147.0-preview.3.1
- Microsoft Learn `SKShaper.Shape`: https://learn.microsoft.com/en-us/dotnet/api/skiasharp.harfbuzz.skshaper.shape

## Decision: Keep Scene dependency-light and evolve GlyphRunData as stable shaped evidence

`src/Scene` will not reference HarfBuzzSharp, SkiaSharp, or SkiaViewer. It will own only the stable package data:
source text, font spec, run metadata, glyph ids, advances, offsets, clusters, fallback decisions, aggregate
metrics, diagnostics, and deterministic fingerprint.

**Rationale**: The Scene package skill and constitution require Scene to stay dependency-light. The roadmap also
needs shaped text to become future portable IR, so the Scene contract must be serializable data rather than
native objects. Feature 140 already added `GlyphRunData`, `GlyphRunGlyph`, `measureGlyphRun`, and
`glyphRunFingerprint`; this feature should widen that data shape instead of adding an unrelated text system.

**Alternatives considered**:
- Store `SKTextBlob`, `SKTypeface`, `SKFont`, or `SKShaper.Result` in Scene: simpler for Skia drawing, but breaks
  Scene package boundaries and future portable-rendering goals.
- Keep the current proof data unchanged: too small for fallback font identity, provider availability, direction,
  script, missing-glyph disclosure, and cache invalidation.

## Decision: Shape per homogeneous run, not per arbitrary string

The provider must itemize text into homogeneous runs before shaping: same font face, size, weight, direction,
script/language when known, and fallback decision. Existing single-style strings produce one run unless fallback
or direction changes require segmentation.

**Rationale**: HarfBuzz shapes a single line using one font and run context. Its own manual states that bidi,
multi-font segmentation, line breaking, hyphenation, and justification remain caller responsibilities. The plan
therefore treats HarfBuzz as the glyph selection and positioning engine, while this feature owns bounded
single-line itemization, fallback segmentation, diagnostics, and deterministic cache keys.

**Alternatives considered**:
- Send full mixed-direction or mixed-font strings straight to HarfBuzz: incorrect for mixed runs and contradicts
  HarfBuzz's documented scope.
- Build a full paragraph layout engine now: too broad for P4 and explicitly out of scope for line breaking,
  editing, caret, selection, and portable protocol work.

Sources:
- HarfBuzz manual, "What HarfBuzz doesn't do": https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html
- HarfBuzz manual, "What is HarfBuzz?": https://manpagez.com/html/harfbuzz/harfbuzz-2.5.1/what-is-harfbuzz.php

## Decision: Implement fixture-backed single-line bidi support and disclose unsupported paragraph cases

Support right-to-left and mixed-direction fixtures by deterministic single-line run itemization and explicit
direction on each shaped run. Full Unicode Bidirectional Algorithm paragraph layout, isolates, line reordering,
selection, and caret behavior remain out of scope. Unsupported bidi controls or paragraph-level cases must emit
diagnostics and use an explicit fallback path.

**Rationale**: The spec requires right-to-left and mixed-direction fixture evidence. HarfBuzz does not perform
bidi processing, so the feature needs a bounded itemizer before shaping. Keeping that itemizer single-line and
fixture-backed satisfies the rendering target without accidentally taking on a complete text-layout engine.

**Alternatives considered**:
- Add ICU4N for full bidi/break behavior immediately: broad alpha dependency and larger scope than required for
  this feature.
- Ignore mixed direction until a later phase: fails FR-005 and acceptance scenarios.

Sources:
- Unicode UAX #9 overview: https://unicode.org/reports/tr9/
- HarfBuzz manual, "What HarfBuzz doesn't do": https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html

## Decision: Preserve pure fallback as the no-provider contract

When the shaping provider is absent, cleared, fails to load native assets, or declines a run, existing
deterministic fallback measurement/drawing remains available. Shaping-enabled code must never silently return
incorrect shaped metrics; it either returns a shaped result, returns a declared fallback result, or emits a
diagnostic failure result.

**Rationale**: Existing pure goldens and feature-136/140 evidence depend on deterministic fallback behavior.
The spec requires pure fallback mode to stay compatible, and the constitution requires safe failure with
diagnostics.

**Alternatives considered**:
- Make shaping mandatory: improves fidelity but breaks pure callers and no-native-provider environments.
- Fall back silently when shaping fails: hides missing fonts/native assets and can recreate measure/draw drift.

## Decision: Cache shaped results by every shaping-affecting input

Shape cache keys include source text, requested family, resolved family/face, size, weight, direction, script or
script bucket, language if introduced, fallback result, provider availability/version bucket, and any shaping
feature flags. Cache-disabled verification remains the oracle for output and metrics.

**Rationale**: Feature 117 and retained rendering already treat text measurement as cache-sensitive. Shaped data
is more input-sensitive than width-only measurement; stale shaped glyphs are visually worse than stale width.

**Alternatives considered**:
- Key only by `(text, font)`: misses direction/script/fallback/provider changes.
- Avoid caching shaped results: simpler, but fails reuse/evidence goals and can be too expensive for warm
  retained text-heavy scenes.

## Decision: Draw from shaped glyph positions rather than reshaping during paint

Measurement reads aggregate advances from the shaped result, and drawing emits the glyph ids/positions recorded
in the same result. The painter must not reshape the original string as an implementation shortcut.

**Rationale**: The main requirement is eliminating measure-versus-draw drift by construction. Skia's text API
overview frames shaping as the process of determining glyphs, order, and positions; exposing that result allows
renderers to draw shaped text efficiently without recomputing layout.

**Alternatives considered**:
- Measure from shaped data but draw the original string: retains drift and fails FR-001.
- Shape independently in measure and draw paths: can diverge under fallback, provider, or cache changes.

Sources:
- Skia text API overview: https://skia.org/docs/dev/design/text_overview/

## Decision: Treat pixel changes as expected but ledger-gated

Shaping-enabled baselines may change for kerning, ligatures, combining marks, contextual scripts, emoji
fallback, and right-to-left runs. Every intentional pixel, diagnostic, or surface delta needs a reason,
migration note if applicable, and readiness ledger entry before implementation is accepted.

**Rationale**: Better shaping changes visible output. The spec accepts intentional changes only when disclosed,
while pure fallback mode must continue reporting zero baseline changes.

**Alternatives considered**:
- Require all text pixels to remain byte-identical: incompatible with the feature's purpose.
- Rebaseline broadly without a ledger: hides regressions and violates Tier 1 evidence requirements.
