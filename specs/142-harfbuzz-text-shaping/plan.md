# Implementation Plan: HarfBuzz Text Shaping (Feature 142)

**Branch**: `142-harfbuzz-text-shaping` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/142-harfbuzz-text-shaping/spec.md`

## Summary

Feature 142 implements P4/R7 from the radical rendering roadmap: production text shaping with HarfBuzz so
measurement, drawing, fingerprints, cache/reuse evidence, and diagnostics all come from one shaped text result.
Feature 140 already introduced deterministic `GlyphRunData` proof data; this feature evolves that proof into
the authoritative text payload used by existing `Text`, `TextRun`, `SizedText`, rich text, and Controls text
paths when the shaping provider is installed.

The technical approach is edge-installed and fallback-preserving. Keep `src/Scene` dependency-light by storing
stable shaped glyph-run data as records and DUs only. Put HarfBuzz/SkiaSharp integration, font fallback, native
asset loading, and glyph drawing in `src/SkiaViewer`. Thread the widened shaped-text seam through Controls so
layout reads the same aggregate advances the painter draws. The no-provider path keeps the existing deterministic
pure fallback behavior and remains the oracle for pure goldens.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings-as-errors.

**Primary Dependencies**: Existing `FS.GG.UI.Scene`, `FS.GG.UI.Controls`, `FS.GG.UI.SkiaViewer`,
`FS.GG.UI.Controls.Elmish`, `FS.GG.UI.Testing`, Expecto test projects, bundled Noto/Inter/DejaVu fonts, and
SkiaSharp. Add `SkiaSharp.HarfBuzz` pinned through `Directory.Packages.props` to the same SkiaSharp train as
the repository's current `SkiaSharp` pin (`4.147.0-preview.3.1` unless the implementation deliberately bumps the
whole Skia family). `SkiaSharp.HarfBuzz` brings `HarfBuzzSharp` transitively. Do not reference HarfBuzz,
SkiaSharp, Silk.NET, Yoga, Controls, or viewer packages from `src/Scene`. The maintenance owner for the new
dependency is the `src/SkiaViewer` package owner because HarfBuzz installation, native asset handling, and glyph
drawing stay at that interpreter edge.

**Storage**: N/A for persistence. Runtime state is limited to bounded shaped-run caches, fallback/disclosure
accumulators, and retained-render evidence carried frame-to-frame or owned by the SkiaViewer interpreter edge.

**Testing**: Expecto and existing harnesses. Focused tests land in `tests/Scene.Tests`,
`tests/SkiaViewer.Tests`, `tests/Controls.Tests`, and `tests/Elmish.Tests`; broad evidence uses
`tests/Rendering.Harness`, package-surface checks, cache parity audits, retained parity audits, and screenshot or
pixel evidence where GL/offscreen capture is available.

**Target Platform**: Linux/dev and CI for deterministic headless tests. GL/offscreen screenshot evidence remains
environment-sensitive and must disclose unsupported host conditions rather than silently passing. Public package
targets remain `net10.0`.

**Project Type**: F# UI framework/library with dependency-light Scene contracts, declarative Controls, retained
rendering, and an OpenGL-backed Skia viewer.

**Performance Goals**: Shape each unique unchanged text input at most once per stable retained frame; produce no
stale shaped reuse; keep cache-enabled and cache-disabled output equivalent; keep measured-vs-drawn advance
within one rendered pixel for 100% of shaping-enabled fixtures.

**Constraints**:
- Tier 1 contracted rendering/package change. Public surface, dependency, diagnostics, and pixel changes require
  `.fsi`-first design, semantic tests, surface baselines, compatibility notes, versioning rationale, and ledger
  entries for intentional deltas.
- `Scene` remains dependency-light and stores shaped evidence only as stable data.
- `SkiaViewer` owns HarfBuzz, Skia typefaces/fonts/shapers, native asset failures, and provider installation.
- Existing no-provider fallback is preserved and must stay byte-compatible for pure fallback verification.
- Text shaping is single-line text output. Newline code points are handled as deterministic control characters
  and must not introduce paragraph layout or line breaking. Full paragraph layout, line breaking, hyphenation,
  justification, text editing, caret movement, selection, portable serialization, browser rendering, compositor
  promotion, damage-scissored presentation, and intrinsic layout are out of scope.
- Mixed-direction support must be fixture-verified through deterministic run itemization. Any unsupported bidi
  control or paragraph-layout case must produce diagnostics and fall back explicitly.

**Scale/Scope**: One rendering slice across:

```text
src/Scene/
|-- Scene.fsi / Scene.fs                       # stable shaped glyph-run records, fingerprinting, pure fallback

src/SkiaViewer/
|-- SkiaViewer.fsi / SkiaViewer.fs             # provider install/clear/status and diagnostics surface
|-- Fonts.fs                                   # font fallback, HarfBuzz shaping provider, shaped-run cache keys
`-- SceneRenderer.fs                           # draw glyphs from shaped data, not the original string

src/Controls/
|-- Control.fs                                 # text and rich-text measure/draw paths consume shaped result
`-- RetainedRender.fsi / RetainedRender.fs     # shaped reuse/cache evidence and invalidation keys

tests/Scene.Tests/
|-- Feature142ShapedTextTests.fs               # data shape, metrics, fingerprints, pure fallback

tests/SkiaViewer.Tests/
|-- Feature142HarfBuzzShapingTests.fs          # provider, glyph drawing, fallback/missing glyph diagnostics

tests/Controls.Tests/
|-- Feature142ControlsTextShapingTests.fs      # labels, buttons, text blocks, rich text, cache parity

tests/Elmish.Tests/
|-- Feature142TextMetricsTests.fs              # retained/direct/warm frame metrics and deterministic reuse

tests/Rendering.Harness/
`-- text fixture corpus and parity evidence
```

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted rendering/package change)**. This feature introduces a new
runtime dependency, can widen public Scene/SkiaViewer contracts, and intentionally changes shaped text pixels.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec and this plan define the Tier 1 outcomes. Any new public or cross-file surface must be sketched in `.fsi`, exercised by semantic tests, then implemented. |
| II. Visibility lives in `.fsi` | PASS | Public shaped data, provider functions, diagnostics, and retained metrics must be declared in existing curated `.fsi` files before `.fs` implementation. |
| III. Idiomatic simplicity | PASS | Use records, DUs, pure functions, and small caches. HarfBuzz/Skia object lifetime stays at the interpreter edge; any mutation must be disclosed as cache or native-handle management. |
| IV. Elmish/MVU boundary | PASS | No new UI workflow is introduced. Retained state and metrics remain model data; provider installation and native shaping are SkiaViewer edge effects. |
| V. Test evidence mandatory | PASS | Fixture corpus, measure/draw parity, fallback/no-provider parity, cache parity, retained parity, deterministic fingerprints, surface checks, and disclosure ledgers are required. |
| VI. Observability and safe failure | PASS | Missing native assets, unsupported bidi/font cases, fallback substitutions, missing glyphs, and shaping failures produce actionable diagnostics or explicit fallback results. |

**Gate result**: PASS. No unresolved clarification markers remain.

**Post-design re-check**: PASS. Phase 0/1 artifacts keep HarfBuzz and native concerns out of `Scene`, preserve
pure fallback behavior, and define the required compatibility evidence before implementation.

## Project Structure

### Documentation (this feature)

```text
specs/142-harfbuzz-text-shaping/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- harfbuzz-text-shaping.md
`-- tasks.md                         # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
Directory.Packages.props                         # pin SkiaSharp.HarfBuzz with the SkiaSharp train

src/Scene/Scene.fsi / Scene.fs                   # shaped glyph-run data, measurement, fingerprint, fallback data
src/SkiaViewer/SkiaViewer.fsi / SkiaViewer.fs    # provider install/clear/status and diagnostics readback
src/SkiaViewer/Fonts.fs                          # HarfBuzz shaping, font fallback, missing-glyph disclosure
src/SkiaViewer/SceneRenderer.fs                  # draw shaped glyph positions; exhaustive SceneNode match
src/Controls/Control.fs                          # control/rich-text text paths use shaped metrics
src/Controls/RetainedRender.fsi / .fs            # cache/reuse invalidation keys and frame evidence

tests/Scene.Tests/Feature142ShapedTextTests.fs
tests/SkiaViewer.Tests/Feature142HarfBuzzShapingTests.fs
tests/Controls.Tests/Feature142ControlsTextShapingTests.fs
tests/Elmish.Tests/Feature142TextMetricsTests.fs
tests/Rendering.Harness/                         # fixture corpus and rendered parity evidence
readiness/ or specs/142-harfbuzz-text-shaping/readiness/
                                                   # surface, pixel, fixture, fallback, and limitation evidence
```

**Structure Decision**: Single F# solution. `Scene` defines the portable shaped-data contract and fallback
fingerprint logic without depending on HarfBuzz or Skia. `SkiaViewer` installs and interprets the shaping
provider. Controls and retained rendering consume shaped metrics/evidence through existing seams and cache keys.

## Phase 0: Research Summary

See [research.md](./research.md). All technology and boundary questions are resolved for planning.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/harfbuzz-text-shaping.md](./contracts/harfbuzz-text-shaping.md),
and [quickstart.md](./quickstart.md). The public contract centers on `GlyphRunData`/shaped-result evidence,
provider installation, diagnostics, and parity validation.

## Complexity Tracking

No constitution violations require justification.
