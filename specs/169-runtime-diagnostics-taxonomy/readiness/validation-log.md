# Feature 169 Validation Log

Recorded on 2026-06-19 Europe/Vienna.

## Package Feed

- `dotnet pack src/Diagnostics/Diagnostics.fsproj -c Release --no-build -o ~/.local/share/nuget-local`: exit 0.
- Verified package: `/home/developer/.local/share/nuget-local/FS.GG.UI.Diagnostics.0.1.30-preview.1.nupkg`.
- `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode proof --isolated-cache specs/169-runtime-diagnostics-taxonomy/readiness/package-proof/nuget-cache --out specs/169-runtime-diagnostics-taxonomy/readiness/package-proof`: passed.
- Package proof reported 14 packages and 18 package pins. All AntShowcase `FS.GG.UI.*` pins were current at `0.1.30-preview.1`.
- Source proof status: `passed`; `FS.GG.UI.*` restored from `/home/developer/.local/share/nuget-local`.

## Validation Lanes

Command:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --out specs/169-runtime-diagnostics-taxonomy/readiness/lanes --include diagnostics
```

Result: exit 1 because required lane `controls` recorded `no-progress-timeout`.

- `build`: passed.
- `library-tests`: passed, 30 tests.
- `package-proof`: passed.
- `controls`: `no-progress-timeout`; last activity was `Skipped Typed standard controls contract.FSI transcript expectations cover typed front doors and custom escape hatch`; reason `lane exceeded no-progress timeout 00:02:00`.
- `rendering-harness`: passed, 18 tests.
- `antshowcase-sample`: passed, 87 tests.
- `diagnostics` optional lane: passed, 14 Feature169 tests.

The summary is committed under `specs/169-runtime-diagnostics-taxonomy/readiness/lanes/validation-20260619-142328-54905e/summary.md`. The non-passing Controls lane is not counted as green.

## Readiness Allowlist

- `.gitignore` allowlists `specs/169-runtime-diagnostics-taxonomy/readiness/**`.
- `git check-ignore -v` reports the negated allowlist pattern for the readiness files.
- Plain `git check-ignore` on representative readiness files exits 1 with no paths, confirming they are not ignored.

## Surface Evidence

- Refreshed public surface baselines were copied into `specs/169-runtime-diagnostics-taxonomy/readiness/surface-baselines/`.
- The Feature 169 public surface introduces `FS.GG.UI.Diagnostics` and additive adapter/helper APIs in Controls, SkiaViewer, Controls.Elmish, and Testing.

## Hooks

- `.specify/extensions.yml` only defines an optional post-implementation git commit hook. The hook was not run separately because the requested workflow commits the completed feature explicitly.
