# Contract: Sample Architecture

## Sample Boundary

The feature adds a new sample under:

```text
samples/SecondAntShowcase/
```

It must not rename, delete, reduce, or change the acceptance meaning of:

```text
samples/AntShowcase/
```

## Package Consumption

The sample consumes `FS.GG.UI.*` packages through the local NuGet feed, matching the existing sample pattern:

- `nuget.config` points to `~/.local/share/nuget-local/`.
- `Directory.Build.props` shadows repo-root build settings where the existing sample does.
- `Directory.Packages.props` pins sample package versions without relying on root central package management.
- Sample projects do not reference `src/` projects directly.

## Project Split

Required projects:

- `SecondAntShowcase.Core`: pure sample model, reducers, page registry, coverage, seeded content, visual config, review finding model, and evidence shaping.
- `SecondAntShowcase.App`: executable edge for CLI dispatch, interactive GL/window host, screenshot capture, filesystem evidence, and diagnostics.
- `SecondAntShowcase.Tests`: focused Expecto tests for the sample contract.

## Public Sample Surface

The public Core surface consumed by tests or App code must be declared through `.fsi` signatures. The initial public module inventory is:

- `Model`
- `DemoState`
- `AntTheme`
- `PageRegistry`
- `CoverageMap`
- `InteractionContracts`
- `ReviewFindings`
- `VisualConfig`
- `VisualReadinessWorkflow`
- `Evidence`
- `Shell`
- `Pages`
- `Templates`

If implementation adds another public Core module, the module must be added to this inventory, receive a curated `.fsi`, and be included in the sample surface baseline before any `.fs` body is completed.

The App project is an executable edge. Tests should exercise App behavior through the CLI process contract. If an App helper is referenced by tests or another project as a library surface, it must be promoted to this inventory with a matching `.fsi` and surface-baseline entry first.

FSI evidence must be recorded before implementation in:

```text
specs/171-second-antshowcase-sample/readiness/fsi/README.md
specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx
```

Sample surface-baseline evidence must be recorded with per-module sections and automatically checked against the curated `.fsi` files in:

```text
specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt
samples/SecondAntShowcase/SecondAntShowcase.Tests/PublicSurfaceTests.fs
```

Product package public surfaces under `src/` are not expected to change. If implementation does require a product package public change, the change must add or update the relevant `.fsi`, surface baseline, semantic tests, and compatibility notes before completion.

## Ant Design Layering

The sample must:

- use the existing semantic control set from `FS.GG.UI.Controls`
- use shipped Ant light/dark theme resolution
- draw Ant facts from `docs/product/ant-design/reference/ant-llms-sources.md` and `docs/product/ant-design/patterns/`
- avoid React, DOM, HTML/CSS, Ant-specific behavior forks, new product controls, and new product themes
- validate color and visual roles through existing theme/token/resolver/color-policy machinery where available

## CLI Surface

Required subcommands:

```text
coverage
list
evidence --seed <int> [--out <dir>] [--page <page-id>]
visual-readiness --seed <int> --size <width>x<height> --themes <list> [--pages <list>] [--out <dir>]
visual-readiness --summarize <preferred-dir> --minimum-size <minimum-dir> [--out <dir>]
review-findings [--out <dir>] [--fail-on-unresolved]
interactive [<page-id>] [--theme light|dark]
```

Required behavior:

- `coverage` exits `0` only for clean coverage and non-zero for drift.
- `list` prints every catalog and template page with stable ids.
- `evidence` runs deterministic scripted interactions and writes disclosed evidence.
- `visual-readiness` captures or records environment-limited target evidence without hanging.
- `review-findings --fail-on-unresolved` exits non-zero when findings remain open, fixed-but-unreviewed, or reviewed-but-not-closed.
- `interactive` degrades cleanly when no live GL/display host is available.

## Documentation

The sample README must explain:

- that this is the second Ant showcase, not a replacement
- how it relates to `samples/AntShowcase`
- how to refresh the local feed
- how to run coverage, tests, evidence, visual-readiness, review-findings, and interactive mode
- what environment-limited evidence does and does not prove

The documentation review must record whether a maintainer unfamiliar with the sample can identify its purpose, relation to `samples/AntShowcase`, Ant guidance source, and visual-review status in under 10 minutes:

```text
specs/171-second-antshowcase-sample/readiness/documentation-review.md
samples/SecondAntShowcase/SecondAntShowcase.Tests/DocumentationReviewTests.fs
```
