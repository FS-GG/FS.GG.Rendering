# Quickstart: Complete P8 Layout Acceptance Validation

This guide lists validation scenarios for the implementation phase. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Existing SkiaSharp/OpenGL native dependencies when viewer or rendering harness evidence is run.
- Feature150 intrinsic layout protocol and readiness surfaces on the current branch.
- Local package feed path `~/.local/share/nuget-local/`.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: restore succeeds and the solution builds with warnings as errors.

## Representative Layout Corpus

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151RepresentativeCorpus
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151Diagnostics
```

Expected outcome:

- Required layout corpus cases record expected bounds, placements, diagnostics, and verdicts.
- Invalid, contradictory, unsupported, stale, or fallback cases emit actionable diagnostics.
- No required corpus case is missing expected result fields.

## ScrollViewer Corpus

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature151ScrollViewerCorpus
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151ScrollLayoutProtocol
```

Expected outcome:

- Empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested
  scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and
  invalid intrinsic fallback cases are covered.
- Viewport bounds stay fixed while content extent and max offsets match expected natural size.
- ScrollViewer evidence uses the Layout intrinsic/content extent path.

## Measured and Intrinsic Reuse

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151MeasuredReuse
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151IntrinsicReuse
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151FullIncrementalParity
```

Expected outcome:

- Warm equivalent runs accept measured and intrinsic reuse with recorded dependency keys.
- Constraint, viewport, content identity, measurement behavior, layout-affecting attributes,
  visibility, child order, intrinsic dependency, and revision changes reject stale entries.
- Full, cold incremental, warm incremental, changed-input incremental, and disabled-cache outcomes
  remain equivalent for accepted cases where those modes apply.

## Broad Regression Evidence

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature097|Feature101|Feature117|Feature137|Feature138|Feature141|Feature150|Feature151"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature092|Feature096|Feature099|Feature110|Feature142|Feature147|Feature148|Feature149|Feature150|Feature151"
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature142|Feature146|Feature147|Feature148|Feature149|Feature151"
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature151
```

Expected outcome:

- Retained rendering, default layout, disabled-cache parity, overlay, render-anywhere, text shaping,
  compositor readiness, and viewer-related checks are accepted or explicitly classified.
- Environment-limited visual/compositor checks do not claim accepted host behavior.
- Any unrelated pre-existing failure is named in readiness before final P8 status is decided.

## Public Surface and Compatibility

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature151|Surface"
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature151
```

Expected outcome:

- Public surface baselines reflect only intentional deltas.
- Compatibility ledger lists all consumer-visible layout or diagnostic changes.
- `FS.GG.UI.Testing.LayoutReadiness` validates the final readiness package shape.

## Full Solution and Package Validation

```bash
dotnet test FS.GG.Rendering.slnx
dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local
dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local
```

Expected outcome:

- Full solution test is accepted or every failure is classified before readiness is accepted.
- Source packages and template package pack to the local feed.
- Package validation verdicts are recorded in `readiness/package-validation.md`.

## Readiness Package

```bash
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature151Readiness
```

Expected outcome:

- `specs/151-complete-p8-layout/readiness/validation-summary.md` links corpus, ScrollViewer,
  reuse, parity, regression, compatibility, package, and limitation evidence.
- Final P8 status is accepted only when all required evidence is present and non-blocking.

## Repository Tooling Note

The layout skill names FAKE targets such as `CapabilityCheck`, `DependencyReport`,
`PackageSurfaceCheck`, and `GeneratedProductCheck`. This checkout currently has no root `./fake.sh`;
use the runnable `dotnet` commands above as the validation path. If the FAKE wrapper is restored,
run equivalent targets and add their output to the readiness package.
