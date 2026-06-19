# Quickstart: Runtime Diagnostics Taxonomy Validation

This guide describes the validation scenarios for the implementation phase.
Commands assume the repository root as the working directory.

## Prerequisites

- .NET SDK for `net10.0`
- local package feed at `~/.local/share/nuget-local/`
- feature branch `169-runtime-diagnostics-taxonomy`

## 1. Restore and Build

```sh
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx -c Release --no-restore
```

Expected outcome: solution builds with the new `FS.GG.UI.Diagnostics` package
and all updated project references.

## 2. FSI Semantic Check

Use the package/prelude path created during implementation to exercise the
public diagnostics API:

```sh
dotnet fsi scripts/diagnostics-prelude.fsx
```

Expected outcome: FSI can create diagnostics, aggregate repeated entries,
render console lines, render JSON/Markdown, and evaluate blocker vs
non-blocking summaries using only public `.fsi` surfaces.

## 3. Focused Classification Tests

```sh
dotnet test tests/Diagnostics.Tests/Diagnostics.Tests.fsproj -c Release --no-restore --filter "Feature169"
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --filter "Feature169"
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --filter "Feature169"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --no-restore --filter "Feature169"
```

Expected outcome:

- environment, backend-cost, rendering-limitation, developer-action, and
  readiness-blocker fixtures classify to expected severities/categories;
- expected backend-cost diagnostics are informational and non-blocking;
- blocker diagnostics change readiness outcome;
- unclassified diagnostics require review;
- 100 repeated diagnostics aggregate into one group with occurrence count 100.

## 4. Artifact and Console Tests

```sh
dotnet test tests/Diagnostics.Tests/Diagnostics.Tests.fsproj -c Release --no-restore --filter "Artifact|Console|Readiness"
```

Expected outcome:

- `diagnostics-summary.json` matches the contract schema;
- Markdown contains reviewer counts and artifact links;
- default console output for the mixed fixture is at most 12 lines;
- verbose output exposes details without changing status;
- artifact write failure creates a developer-action warning.

## 5. Surface Baseline Check

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-restore --filter "Surface"
```

Expected outcome: baselines include the new `FS.GG.UI.Diagnostics` surface and
document additive changes to SkiaViewer, Controls, Controls.Elmish, and Testing.

## 6. Sample Package Consumer Check

After packing updated packages to the local feed, validate a sample command that
uses default and verbose diagnostics:

```sh
dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample --json
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose --verbose
```

Expected outcome: default output is compact and grouped; verbose output exposes
diagnostic details; JSON and Markdown artifacts are written.

## 7. Validation Lane Integration

```sh
dotnet fsi scripts/run-validation-lanes.fsx --out specs/169-runtime-diagnostics-taxonomy/readiness/lanes --include diagnostics
```

Expected outcome: validation summary links to diagnostic artifacts, computes
readiness from typed diagnostic status, and does not parse raw console text.

## 8. Readiness Evidence

Record implementation evidence under:

```text
specs/169-runtime-diagnostics-taxonomy/readiness/
```

Expected evidence:

- focused test logs
- FSI transcript or command output summary
- surface-baseline delta
- fixture JSON/Markdown artifacts
- default and verbose sample output
- validation-lane summary
- migration notes for any reclassified diagnostic
