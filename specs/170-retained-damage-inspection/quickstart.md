# Quickstart: Retained Render Damage Inspection

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Repository restored from the project root.
- Existing local feed/sample package proof refreshed when package-consuming sample validation is part of the run.

Suggested setup:

```sh
dotnet restore FS.GG.Rendering.slnx
```

## Focused Validation

Run retained/damage unit and contract checks:

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature170
dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature170
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --filter Feature170
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter Feature170
```

Expected outcomes:

- Controls tests classify retained, reused, repainted, shifted, added, removed, unaffected, empty-damage, broad-damage, full-surface, and unsupported fixture cases.
- Testing tests validate dirty-region union semantics, broad/full-surface findings, shifted/repainted separation, intentional exceptions, and stable finding ids.
- Harness tests prove the `retained-inspection` lane is listed and wired to the intended commands/artifacts.
- AntShowcase tests prove the selected `charts-statistical` shell assertion consumes structured retained inspection evidence in light and dark themes at preferred size.

## Canonical Readiness Command

Run the maintained validation entry point:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes
```

Expected outputs:

- `specs/170-retained-damage-inspection/readiness/lanes/summary.md`
- `specs/170-retained-damage-inspection/readiness/lanes/summary.json`
- `specs/170-retained-damage-inspection/readiness/lanes/retained-inspection/log.txt`
- `specs/170-retained-damage-inspection/readiness/lanes/retained-inspection/result.json`
- `specs/170-retained-damage-inspection/readiness/lanes/retained-inspection/diagnostics.md`
- retained inspection JSON/Markdown artifacts discovered from the lane output

The lane result should fail clearly if prerequisites are missing, including an unknown lane id, missing restored assets, missing AntShowcase project, stale package proof, or unwritable output directory.

## Artifact Review

Review retained inspection evidence:

```sh
ls specs/170-retained-damage-inspection/readiness/retained-inspection
```

Expected files:

- `summary.md`
- `summary.json`
- `validation-log.md`
- `compatibility.md`
- `antshowcase-adoption.md`
- `artifacts/*.retained.json`
- `artifacts/*.retained.md`
- `findings/blocking-findings.md` when findings exist

The summary must show:

- command, result status, elapsed time, and artifact root
- dirty area percentage by transition
- repainted node count
- shifted node count
- unaffected node count
- affected visual regions
- unsupported or not-inspected damage facts
- accepted/invalid intentional exceptions
- related screenshot or visual-readiness evidence links when available

## Surface and Compatibility Checks

Run package/surface checks after `.fsi` changes:

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --filter Surface
```

Expected outcomes:

- Surface baselines are updated for every changed public package.
- `compatibility.md` states whether `Scene.fsi`, `Inspection.fsi`, `Testing.fsi`, and surface baselines changed.
- The compatibility artifact states that existing `VisualInspectionArtifact`, `VisualInspectionValidation`, `CompositorDamageReadiness`, and screenshot readiness behavior remain source-compatible unless a deliberate exception is documented.

## AntShowcase Screenshot Preservation

Run the sample readiness target parity test:

```sh
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter "VisualReadiness"
```

Expected outcomes:

- Preferred screenshot matrix remains 38 targets.
- Minimum screenshot matrix remains 12 targets.
- Reviewer classification requirements remain unchanged unless `compatibility.md` documents a deliberate change.

## Readiness Acceptance

Feature readiness is acceptable when:

- focused tests pass
- the `retained-inspection` lane passes
- retained/damage summaries include command, status, elapsed time, and artifact paths
- broad/full-surface localized damage is blocked or covered by a scoped intentional exception
- unsupported/not-inspected facts are explicit
- AntShowcase structured evidence adoption is present
- screenshot readiness counts are preserved
- public surface and migration notes are complete
