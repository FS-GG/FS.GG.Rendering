# Quickstart: HarfBuzz Text Shaping Validation

This guide describes the validation expected after implementation. It does not include implementation code.

## Prerequisites

- .NET SDK for `net10.0`.
- Repository dependencies restored through central package management.
- `SkiaSharp.HarfBuzz` pinned to the same SkiaSharp train as the repository's `SkiaSharp` package.
- A shell with normal repo build commands available.
- GL/offscreen support for screenshot evidence, or an explicit environment-limitation report when unavailable.

## Focused Validation

1. Restore and build the solution.

   ```bash
   dotnet restore FS.GG.Rendering.slnx
   dotnet build FS.GG.Rendering.slnx
   ```

   Expected outcome: all projects restore with one SkiaSharp/HarfBuzz version train and no Scene dependency on
   SkiaViewer, SkiaSharp, HarfBuzzSharp, Controls, Elmish, Yoga, or Silk.NET.

2. Run Scene shaped-data tests.

   ```bash
   dotnet test tests/Scene.Tests/Scene.Tests.fsproj
   ```

   Expected outcome: shaped data records deterministic glyphs, metrics, diagnostics, and fingerprints; pure
   fallback still matches existing fallback metrics when the provider is absent.

3. Run SkiaViewer shaping tests.

   ```bash
   dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
   ```

   Expected outcome: provider install/clear behavior is explicit; HarfBuzz-shaped fixtures draw from glyph ids
   and positions; fallback and missing-glyph diagnostics identify affected code points or clusters.

4. Run Controls text path tests.

   ```bash
   dotnet test tests/Controls.Tests/Controls.Tests.fsproj
   ```

   Expected outcome: labels, buttons, text blocks, rich text, data values, and text input display paths use shaped
   metrics where eligible; cache-enabled and cache-disabled paths remain equivalent.

5. Run Elmish/retained metrics tests.

   ```bash
   dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj
   ```

   Expected outcome: direct, cold retained, and warm retained frames report equivalent text metrics,
   fingerprints, diagnostics, and no stale shaped reuse.

## Fixture Corpus Validation

Run or add the text fixture corpus through the rendering harness.

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
```

Expected corpus coverage:

- At least 40 cases.
- At least eight categories.
- Latin kerning.
- Latin ligatures.
- Combining marks.
- Right-to-left text.
- Mixed-direction text.
- Emoji or zero-width-joiner sequences.
- Symbol fallback.
- Representative complex scripts.
- Negative missing-glyph fixtures.

Acceptance:

- Measured and drawn advance differ by no more than one rendered pixel for shaping-enabled fixtures.
- Fixture text stays inside expected bounds.
- Repeated runs produce byte-identical fingerprints and diagnostics.
- Negative fixtures disclose 100% of affected code points or text segments.

## Fallback Validation

Run the same fixture set with the shaping provider cleared or unavailable.

Expected outcome:

- Existing deterministic fallback behavior remains available.
- Pure fallback verification reports zero baseline changes.
- Fallback fingerprints and diagnostics remain deterministic.
- Provider absence is visible in evidence and does not crash validation.

## Parity Validation

Compare all relevant render modes:

- Direct rendering.
- First-frame retained rendering.
- Warm retained rendering.
- Cache-enabled text rendering.
- Cache-disabled text rendering.
- Shaping-enabled mode.
- Pure fallback mode.

Expected outcome:

- Equivalent inputs produce equivalent visible output, metrics, diagnostics, and fingerprints.
- Warm repeated workloads produce no stale reuse.
- Stable frames shape at most once per unique unchanged text input.

## Surface and Package Validation

Run the package/surface gates used for Tier 1 changes.

```bash
./fake.sh build -t CapabilityCheck
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
```

Expected outcome:

- Public `.fsi` deltas are intentional and baselined.
- Dependency additions are pinned and documented.
- Generated product/package checks do not pull HarfBuzz into Scene or pure generated-product code paths unless
  the selected profile includes SkiaViewer.

If the `fake.sh` wrapper is unavailable in this checkout, record that limitation in readiness and run the nearest
focused `dotnet build`/`dotnet test`/surface-refresh commands available.

Observed local limitation on 2026-06-17: this checkout has no root `fake.sh`; only `template/base/fake.sh`
exists. The nearest local checks are `dotnet build FS.GG.Rendering.slnx --no-restore`,
`dotnet fsi scripts/refresh-surface-baselines.fsx`, and the focused test projects. `Package.Tests` is not a
clean Feature 142 gate in this checkout because it still expects historical readiness artifacts outside this
feature's scope.

## Readiness Evidence

Before accepting implementation, collect:

- Fixture corpus report.
- Measure-vs-draw advance report.
- Fallback and missing-glyph diagnostics report.
- Cache-on/off parity report.
- Direct/cold/warm retained parity report.
- Pure fallback zero-baseline report.
- Public surface baseline diff or zero-diff report.
- Baseline disclosure ledger for every intentional pixel, diagnostic, dependency, or surface delta.
- Environment limitation report for unavailable GL/offscreen validation, if applicable.
