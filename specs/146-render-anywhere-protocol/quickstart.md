# Quickstart: Render-Anywhere Scene Protocol Validation

This guide lists the validation scenarios expected after implementation. It is intentionally a run
guide, not implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native Skia/HarfBuzz dependencies already used by `SkiaViewer`.
- A capable display/headless environment for accepted PNG reference evidence, or an environment
  that can honestly report `environment-limited`.
- Browser or WASM host prerequisites for the browser feasibility command when available.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Round-Trip and Deterministic Package Validation

```bash
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature146
```

Expected outcome:

- Representative corpus exports and imports with no semantic mismatches.
- Repeated exports produce byte-identical package bytes.
- Unsupported version, unknown required tag, missing resource, hash mismatch, and unsupported
  capability cases fail safely before rendering.

## Reference Rendering Oracle Validation

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature146
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-reference --out specs/146-render-anywhere-protocol/readiness/reference
```

Expected outcome:

- Accepted packages produce decodable, non-blank PNG artifacts and metadata.
- Missing resources and unsupported capabilities do not produce accepted artifacts.
- Unsupported host conditions produce environment-limited records.

## Browser Feasibility Validation

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature146
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-browser-feasibility --out specs/146-render-anywhere-protocol/readiness/browser
```

Expected outcome:

- At least three representative showcase scenes are evaluated against the reference oracle.
- Each comparison records tolerance, artifact identities, diff metric, and verdict.
- The report ends with either accepted candidate path or documented fallback path.

## Public Contract and Compatibility Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature146
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
```

Expected outcome:

- Public surface baselines reflect intentional Tier 1 additions.
- Compatibility ledger names public contract changes and migration guidance.
- Package tests confirm no undocumented public surface drift.

## Full Readiness Pass

```bash
dotnet test FS.GG.Rendering.slnx
dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local
```

Expected outcome: all relevant tests pass, package output succeeds, and readiness artifacts under
`specs/146-render-anywhere-protocol/readiness/` contain round-trip, reference, browser feasibility,
surface, and compatibility evidence.
