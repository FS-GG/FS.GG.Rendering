# Quickstart: Second Ant Showcase Sample

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Repository restored from the project root.
- Local NuGet feed refreshed with the current `FS.GG.UI.*` packages before building the package-consuming sample.

Suggested setup from the repository root:

```sh
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx -c Release --no-restore
dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local
dotnet nuget locals global-packages --clear
```

## Build the Sample

```sh
cd samples/SecondAntShowcase
dotnet build SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
```

Expected outcome:

- App builds against packed `FS.GG.UI.*` packages from the local feed.
- Existing `samples/AntShowcase` remains present and unchanged.

## Coverage and Page List

```sh
dotnet run --project SecondAntShowcase.App -c Release -- coverage
dotnet run --project SecondAntShowcase.App -c Release -- list
```

Expected outcomes:

- Coverage reports every current catalog control exactly once.
- Missing, duplicated, or unknown controls make `coverage` exit non-zero.
- `list` prints 13 catalog pages and six template pages.

## Focused Tests

```sh
dotnet test SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
```

Expected outcomes:

- FSI surface tests pass for the pre-implementation transcript and public Core signatures.
- Public surface-baseline tests pass against `specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt`.
- Coverage tests pass.
- Interaction contracts cover all interactive controls and display-only reasons.
- Template behavior tests pass for workbench, list, detail, form, result, and exception pages.
- Theme invariance tests prove page and state preservation across Ant light/dark switching.
- Determinism tests prove the representative review path is stable for the same seed.
- Visual-readiness tests prove the required target matrix includes all pages, both themes, and both accepted sizes.
- Review finding tests prove unresolved findings block acceptance.
- Documentation-review tests prove the README/provenance cover SC-010.

## FSI and Surface Evidence

From the repository root:

```sh
dotnet fsi specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --filter "FsiSurface|PublicSurface|DocumentationReview"
```

Expected outputs:

- `specs/171-second-antshowcase-sample/readiness/fsi/README.md`
- `specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx`
- `specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt`
- `specs/171-second-antshowcase-sample/readiness/documentation-review.md`

Expected outcomes:

- Every planned public Core module has a curated `.fsi`.
- The FSI/prelude transcript exercises the intended public sample shape before final `.fs` bodies.
- The surface-baseline drift test fails for unreviewed public Core surface changes.
- The documentation-review evidence records whether SC-010 was met.

## Representative Evidence

```sh
dotnet run --project SecondAntShowcase.App -c Release -- evidence --seed 1 --out ../../specs/171-second-antshowcase-sample/readiness
```

Expected outputs:

- `specs/171-second-antshowcase-sample/readiness/evidence-summary.md`
- `specs/171-second-antshowcase-sample/readiness/evidence-summary.json`
- `specs/171-second-antshowcase-sample/readiness/coverage.md`
- `specs/171-second-antshowcase-sample/readiness/interaction-review.md`
- `specs/171-second-antshowcase-sample/readiness/limitations.md`

The run must disclose any synthetic or environment-limited evidence.

## Visual Readiness

Preferred size, all pages, both themes:

```sh
dotnet run --project SecondAntShowcase.App -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out ../../specs/171-second-antshowcase-sample/readiness/preferred
```

Minimum size, all pages, both themes:

```sh
dotnet run --project SecondAntShowcase.App -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out ../../specs/171-second-antshowcase-sample/readiness/minimum
```

Summarize both runs:

```sh
dotnet run --project SecondAntShowcase.App -c Release -- visual-readiness --summarize ../../specs/171-second-antshowcase-sample/readiness/preferred --minimum-size ../../specs/171-second-antshowcase-sample/readiness/minimum --out ../../specs/171-second-antshowcase-sample/readiness
```

Expected outcomes:

- The preferred run creates 38 targets: 19 pages x 2 themes.
- The minimum run creates 38 targets: 19 pages x 2 themes.
- The summary covers 76 required targets.
- Live accepted visual readiness requires complete captures, reviewer classifications, and zero unresolved findings.
- If no live visual environment is available, the command completes with environment-limited status and does not claim accepted visual fidelity.

## Review Findings Gate

```sh
dotnet run --project SecondAntShowcase.App -c Release -- review-findings --out ../../specs/171-second-antshowcase-sample/readiness --fail-on-unresolved
```

Expected outcomes:

- Exits `0` only when every finding is closed after affected targets were re-reviewed.
- Exits non-zero for open findings, fixed-but-unreviewed findings, malformed finding records, or missing reviewer classification.

## Interactive Smoke

```sh
dotnet run --project SecondAntShowcase.App -c Release -- interactive display-typography --theme light
dotnet run --project SecondAntShowcase.App -c Release -- interactive tpl-form --theme dark
```

Expected outcomes:

- On a live GL/display host, the sample opens at the requested page and appearance.
- On a host without live presentation support, it exits cleanly with a limitation message.

## Acceptance Checklist

Feature readiness is acceptable when:

- the full existing `samples/AntShowcase/` tree is still discoverable and unchanged by this feature
- FSI/prelude evidence exists and sample public surface-baseline tests pass
- `coverage` is clean
- focused sample tests pass
- representative evidence is deterministic for the same seed
- all 76 visual targets have live accepted capture evidence
- reviewer classifications exist for all targets
- unresolved visual findings count is zero
- limitations are disclosed and not overstated
- sample README and provenance explain the Ant guidance and relationship to existing AntShowcase
- documentation review records that purpose, AntShowcase relation, Ant guidance source, and visual-review status are identifiable in under 10 minutes
