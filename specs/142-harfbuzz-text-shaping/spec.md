# Feature Specification: HarfBuzz Text Shaping

**Feature Branch**: `142-harfbuzz-text-shaping`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report says P0, P1, P2, and P3 have already shipped or been
implemented through Feature 141. The next unstarted roadmap item is P4: Text, also described
as R7 real text shaping.

Feature 140 introduced a deterministic glyph-run proof surface, and earlier work added a
measurement seam so rendered text can size boxes from renderer-owned metrics. This feature turns
that proof into production text shaping behavior: the same shaped text result should drive
measurement, drawing, fingerprints, cache/reuse evidence, and diagnostics. Generated apps and
framework consumers should see text that fits its allocated space, supports complex scripts and
font fallback more reliably, and preserves deterministic fallback behavior when the shaping
provider is not installed.

This is a Tier 1 rendering and package-contract feature because it can introduce dependency,
surface, diagnostic, and pixel-baseline changes. It must preserve existing pure fallback behavior
unless an intentional compatibility decision is documented. It must stay bounded to text shaping
and must not take on portable scene serialization, overlay interaction state, compositor promotion,
damage-scissored rendering, or the intrinsic layout protocol.

The roadmap names HarfBuzz as the selected shaping engine. This specification defines the
observable text-rendering outcomes and verification obligations; implementation details belong in
the plan.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Measure and Draw From One Shaped Result (Priority: P1)

A generated application author renders labels, data values, captions, and rich text containing
kerning pairs, ligatures, combining marks, and non-Latin scripts. The text is measured for layout
and then drawn from the same shaped result, so it fits its allocated box and the drawn advance does
not diverge from the measured advance.

**Why this priority**: This is the core P4 value. The report identifies the current per-character
and heuristic paths as a source of measure-versus-draw drift. Closing that bug class is required
before portable text or broader backend work can rely on text output.

**Independent Test**: Render a fixture set containing Latin kerning and ligatures, combining marks,
right-to-left text, mixed-direction text, emoji sequences, Arabic contextual forms, Devanagari
conjuncts, and Thai mark or vowel positioning.
Compare the advance used for layout with the advance used for drawing, and assert that text remains
inside its expected bounds.

**Acceptance Scenarios**:

1. **Given** text containing kerning pairs or ligatures, **When** it is laid out and drawn with the
   shaping provider installed, **Then** measurement and drawing use one shaped result and the text
   fits without mid-word clipping.
2. **Given** text containing combining marks, **When** it is shaped and drawn, **Then** marks are
   positioned with their base characters and the measured bounds include the visible result.
3. **Given** text whose content, family, size, weight, direction, or fallback result changes,
   **When** the next frame is produced, **Then** the affected text shape and layout evidence update
   rather than reusing stale output.
4. **Given** the shaping provider is not installed, **When** the same scene is measured and drawn,
   **Then** the existing deterministic pure fallback behavior remains available and compatible.

---

### User Story 2 - Render International Text With Actionable Fallback Evidence (Priority: P1)

A framework consumer needs product screens to render international text predictably, including
right-to-left or mixed-direction content, script-specific shaping, emoji or symbol sequences, and
missing-glyph cases. When a font fallback or missing-glyph substitution occurs, maintainers can see
clear diagnostics that identify what happened.

**Why this priority**: Text shaping only delivers user value if it improves real-world text, not
just simple ASCII labels. Diagnostics are required so missing fonts and unsupported glyphs fail
visibly instead of silently degrading.

**Independent Test**: Exercise representative fixture strings and negative fixtures with known
missing coverage. Verify expected glyph fallback, missing-glyph disclosure, direction handling, and
cluster mapping outcomes.

**Acceptance Scenarios**:

1. **Given** right-to-left text and mixed left-to-right/right-to-left text, **When** it is rendered,
   **Then** visual ordering and metrics are stable and match the expected fixture evidence.
2. **Given** a script that requires contextual shaping, **When** it is rendered, **Then** glyph
   choices, advances, and offsets reflect the shaped result rather than one-character-at-a-time
   drawing.
3. **Given** emoji or symbol sequences that require fallback fonts, **When** they are rendered,
   **Then** the result records which fallback path was used and does not corrupt surrounding text
   measurement.
4. **Given** a code point with no available glyph, **When** rendering completes, **Then** the
   missing glyph is disclosed with enough context for maintainers to identify the affected text.

---

### User Story 3 - Preserve Rendering Parity, Caches, and Determinism (Priority: P1)

A rendering maintainer validates the feature across direct rendering, cold retained rendering, warm
retained rendering, cache-enabled mode, and cache-disabled mode. All paths produce equivalent text
output for equivalent inputs, and repeated runs produce stable glyph-run fingerprints and
diagnostics.

**Why this priority**: The previous roadmap phases made parity and deterministic evidence core
contracts. Text shaping must join those contracts rather than becoming a new hidden source of drift.

**Independent Test**: Run text-heavy scenes through direct, cold retained, and warm retained
rendering, with text caching and replay-related caches enabled and disabled. Compare output,
metrics, fingerprints, fallback disclosure, and reuse evidence.

**Acceptance Scenarios**:

1. **Given** a text-heavy scene, **When** direct rendering, first-frame retained rendering, and warm
   retained rendering are compared, **Then** text output, metrics, diagnostics, and fingerprints are
   equivalent for equivalent inputs.
2. **Given** text caching is enabled or disabled, **When** the same text scene renders, **Then** the
   visible output and measured bounds remain equivalent.
3. **Given** a retained frame reuses unchanged shaped text, **When** the frame is produced, **Then**
   reuse evidence is recorded without changing visible output.
4. **Given** identical shaped text inputs across repeated runs, **When** fingerprints and
   diagnostics are collected, **Then** they remain stable.

---

### User Story 4 - Keep Pure Fallback and Baseline Changes Auditable (Priority: P2)

A package maintainer upgrades the text system and reviews all public surface, documentation,
diagnostic, and pixel-baseline changes. Intentional changes are documented with migration guidance,
and pure fallback scenes remain deterministic when no shaping provider is installed.

**Why this priority**: Text shaping is expected to improve pixels for some text. That is acceptable
only if the compatibility story is explicit and pure fallback goldens remain trustworthy.

**Independent Test**: Run existing pure fallback and golden verification without an installed
shaping provider, then run shaping-enabled verification. Confirm fallback baselines remain
compatible and every intentional shaping-enabled baseline or surface change has a disclosure entry.

**Acceptance Scenarios**:

1. **Given** pure fallback mode, **When** existing deterministic text scenes render, **Then** their
   baseline output remains compatible with the pre-feature fallback behavior.
2. **Given** shaping-enabled text changes an expected baseline, **When** readiness evidence is
   reviewed, **Then** the baseline change has a documented reason and migration note if needed.
3. **Given** public text or scene contracts need to change, **When** surface verification runs,
   **Then** every change is documented with compatibility impact and versioning rationale.
4. **Given** a later portable-rendering phase needs shaped text evidence, **When** this feature is
   complete, **Then** the shaped text data is documented as ready for planning without implementing
   the portable protocol in this feature.

### Edge Cases

- Empty strings, whitespace-only strings, and strings containing newline code points should
  measure and draw deterministically without errors. Newline code points are single-line control
  characters in this feature; they must not trigger paragraph layout or line breaking.
- Combining marks without a visible base character should produce stable metrics and diagnostics.
- Zero-width joiner emoji sequences, variation selectors, and symbol fallback should not corrupt
  surrounding text advances.
- Mixed-direction runs should preserve stable visual order, metrics, and cluster evidence.
- Missing font families, unavailable weights, and missing glyphs should fall back or disclose
  explicitly rather than silently producing incorrect metrics.
- Very long text runs and repeated identical text should remain deterministic and should not reuse
  stale shaped data after a relevant input changes.
- Cache-enabled and cache-disabled rendering should remain equivalent for shaped text, fallback
  text, and missing-glyph scenarios.
- Pure fallback mode should remain available for deterministic callers and environments where the
  shaping provider is absent.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: In shaping-enabled mode, the system MUST derive both text measurement and text drawing
  from the same shaped text result.
- **FR-002**: A shaped text result MUST include enough stable data to describe glyph choices,
  advances, offsets, source clusters, aggregate metrics, diagnostics, and a deterministic
  fingerprint.
- **FR-003**: Text created through existing scene and control text paths MUST be eligible for shaping
  before measurement or drawing when the shaping provider is installed.
- **FR-004**: Pure fallback mode MUST remain available and MUST preserve the existing deterministic
  fallback behavior when the shaping provider is absent or intentionally cleared.
- **FR-005**: The system MUST support fixture-verified shaping for ligatures, kerning, combining
  marks, right-to-left text, mixed-direction text, emoji or symbol sequences, and representative
  complex scripts, including Arabic contextual forms, Devanagari conjuncts, and Thai mark or vowel
  positioning.
- **FR-006**: Font fallback and missing-glyph outcomes MUST produce actionable diagnostics that
  identify the affected text or code points without changing successful rendering behavior.
- **FR-007**: Text reuse and caching MUST distinguish all inputs that can alter shaped output,
  including text content, font family, size, weight, direction, script behavior, fallback outcome,
  and shaping-provider availability.
- **FR-008**: Direct rendering, first-frame retained rendering, and warm retained rendering MUST
  produce equivalent shaped text output, metrics, diagnostics, fingerprints, and fallback evidence
  for equivalent inputs.
- **FR-009**: Cache-enabled and cache-disabled verification paths MUST remain equivalent for shaped
  text and pure fallback text.
- **FR-010**: Repeated equivalent shaped text inputs MUST produce deterministic fingerprints,
  diagnostics, and aggregate metrics across runs.
- **FR-011**: Verification MUST cover both positive international text fixtures and negative
  fallback or missing-glyph fixtures.
- **FR-012**: Public text, scene, or diagnostic contract changes MUST include compatibility impact,
  migration guidance, surface-baseline evidence, and versioning rationale.
- **FR-013**: Any intentional pixel, golden, or diagnostic baseline change MUST be recorded with the
  reason for the change before the feature is considered ready.
- **FR-014**: If shaping fails for a specific run, the system MUST fail safely by using an explicit
  fallback path or diagnostic instead of crashing or silently returning incorrect metrics.
- **FR-015**: The feature MUST NOT implement portable scene serialization, browser rendering,
  overlay interaction state, compositor promotion, damage-scissored presentation, intrinsic layout,
  or text editing and selection workflows.
- **FR-016**: Readiness evidence MUST distinguish new shaping behavior from pre-existing text cache,
  retained rendering, or package-surface limitations encountered during validation.

### Key Entities

- **Shaped text result**: The authoritative text output for a run, containing glyph-level data,
  aggregate metrics, diagnostics, and stable identity evidence.
- **Glyph-run data**: The package-owned representation used to carry shaped text evidence for
  measurement, drawing, diagnostics, cache/reuse, and future portable-rendering planning.
- **Text measurement**: The width, height, and baseline used by layout and fitting decisions.
- **Font fallback decision**: The selected substitute font or missing-glyph path for code points
  not covered by the requested font.
- **Fallback disclosure**: Diagnostics that identify substituted or missing text so maintainers can
  act on font coverage problems.
- **Shaping cache entry**: Reusable shaped text evidence keyed by every input that can affect the
  shaped result.
- **Pure fallback mode**: The deterministic text path used when no shaping provider is installed.
- **Text parity oracle**: Verification comparing shaped output across direct, retained,
  cache-enabled, cache-disabled, and pure fallback paths.
- **Baseline disclosure ledger**: Readiness evidence that explains intentional text pixel,
  diagnostic, or surface changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fixture corpus of at least 40 text cases across at least eight text categories
  passes with zero unexpected shaping, metric, diagnostic, or missing-glyph failures.
- **SC-002**: For 100% of shaping-enabled fixture cases, the measured text advance and drawn text
  advance differ by no more than one rendered pixel.
- **SC-003**: 100% of combining-mark, ligature, right-to-left, mixed-direction, emoji, Arabic,
  Devanagari, and Thai fixtures fit inside their expected text bounds.
- **SC-004**: Pure fallback verification reports zero baseline changes when the shaping provider is
  absent or cleared.
- **SC-005**: Direct, first-frame retained, and warm retained rendering match for at least 100
  text-heavy fixture or generated scenes in shaping-enabled mode.
- **SC-006**: Cache-enabled and cache-disabled text verification reports zero output or metric
  differences for shaped text and fallback text.
- **SC-007**: Repeated equivalent shaped text inputs produce byte-identical fingerprints and
  diagnostics across at least three consecutive runs.
- **SC-008**: Missing-glyph and fallback diagnostics identify 100% of affected negative-fixture
  code points or text segments.
- **SC-009**: Warm repeated text workloads report no stale reuse and no more than one fresh shaped
  result per unique unchanged text input in a stable frame.
- **SC-010**: Public surface verification reports either zero public contract changes or 100%
  documented changes with migration guidance and versioning rationale.
- **SC-011**: Every intentional text golden, pixel, or diagnostic baseline change has a disclosure
  ledger entry before readiness is accepted.
- **SC-012**: Scope review confirms zero implementation tasks for portable serialization, browser
  rendering, overlay interaction state, compositor promotion, damage-scissored presentation,
  intrinsic layout, and text editing or selection workflows.

## Assumptions

- The report's status update supersedes its closing sentence: Feature 141/P3 is implemented, so the
  next unstarted roadmap item is P4 Text/R7.
- Feature 140's glyph-run proof surface exists and should be evolved or reused rather than replaced
  with an unrelated text evidence shape.
- The shaping provider is installed at the rendering edge; pure callers and no-provider environments
  continue to use deterministic fallback behavior.
- Existing text cache, retained rendering, fingerprint, surface-baseline, and golden verification
  suites remain the primary compatibility oracles when supplemented by new shaping-specific
  fixtures.
- Some shaping-enabled text pixels are expected to change because glyph selection and positioning
  improve; such changes are acceptable only when disclosed and tied to the new shaping behavior.
- Text editing, selection, caret movement, and user-facing text input behavior are out of scope for
  this feature.
