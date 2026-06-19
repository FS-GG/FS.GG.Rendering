# Quickstart: Package Feed Validation Lanes

## Prerequisites

- .NET SDK for `net10.0`.
- Repository dependencies restored.
- Local package feed path available at `~/.local/share/nuget-local/`.
- AntShowcase selected as the first package-consuming sample.

## 1. Build and Pack Current Packages

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
dotnet pack FS.GG.Rendering.slnx -c Release --no-restore -o ~/.local/share/nuget-local
```

Expected:

- Packable `FS.GG.UI.*` projects produce packages in the local feed.
- No sample restore/build is required before package-pin checks run.

## 2. Check Selected Sample Package Pins

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode check \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof
```

Expected:

- Current `FS.GG.UI.*` package versions are listed from source projects.
- AntShowcase `FS.GG.UI.*` package pins are listed with package id, sample path, declared version,
  and expected version.
- A stale pin fails with package id, expected version, actual version, and sample path before any
  sample build/test starts.

## 3. Refresh Pins When Needed

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode refresh \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof
```

Expected:

- Stale selected `FS.GG.UI.*` pins are updated to their package-specific expected versions.
- Changed files and before/after versions are recorded.
- No compatibility exception is created silently.

## 4. Prove Package Source Selection

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode proof \
  --isolated-cache specs/163-package-feed-validation-lanes/readiness/package-proof/nuget-cache \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof
```

Expected:

- Restore uses generated package source mapping.
- `FS.GG.UI.*` packages are constrained to `~/.local/share/nuget-local/`.
- Third-party packages resolve only from approved external sources.
- Evidence records feed path, cache path, source rules, selected samples, resolved versions,
  restore logs, and whether global caches were cleared.
- Default proof does not clear global NuGet caches.

## 5. Run Named Validation Lanes

```bash
dotnet fsi scripts/run-validation-lanes.fsx \
  --lane package-proof \
  --lane antshowcase-sample \
  --lane controls \
  --lane rendering-harness \
  --out specs/163-package-feed-validation-lanes/readiness/lanes
```

Expected:

- Each lane has separate log, result, diagnostics, and generated output paths.
- Passed, failed, timed-out, hung, skipped, canceled, not-run, and environment-limited statuses are
  preserved distinctly.
- A timed-out, hung, canceled, skipped, not-run, or environment-limited required lane is not counted
  as green.
- Aggregate full-solution validation is optional and displayed separately from focused lanes. Add
  `--lane aggregate-solution` when full-solution validation is required for a release gate.

## 6. Run Focused Feature Tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature163"
dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature163"
```

Expected:

- Package discovery, stale pin detection, refresh behavior, source proof, lane classification,
  timeout/no-progress handling, output isolation, and summary gating tests pass.
- If public or package-visible `.fsi` files changed, surface-baseline validation is recorded in
  `specs/163-package-feed-validation-lanes/readiness/package-validation.md`.

## 7. Read Final Summary

Open:

```text
specs/163-package-feed-validation-lanes/readiness/validation-summary.md
```

Expected:

- Current package versions, selected samples, local feed, package cache, source proof, lane status
  table, aggregate solution status, caveats, and incomplete evidence are visible.
- Overall readiness is not `ready` when any required proof or lane is failed, timed out, hung,
  skipped, canceled, not run, or environment-limited without an accepted exception.

## Implementation Notes

Validated on 2026-06-19:

- `package-feed --mode check`: passed for `samples/AntShowcase`.
- `package-feed --mode proof`: passed with isolated cache and no global cache clearing.
- Focused lanes `package-proof`, `antshowcase-sample`, `controls`, and `rendering-harness`: passed.
- `aggregate-solution`: not selected for focused readiness and remains a separate optional lane.
