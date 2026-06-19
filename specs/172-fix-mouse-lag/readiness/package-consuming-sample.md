# Package-Consuming Sample

- Refresh command: `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase`.
- Refresh exit code: `0`.
- Package-feed status: `passed`.
- Packages reported: `14`.
- Pins reported: `18`.
- App rebuild command: `dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental`.
- App rebuild exit code: `0`.
- Post-refresh sample test command: `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore`.
- Post-refresh sample test exit code: `0`.

Logs:
- `specs/172-fix-mouse-lag/readiness/logs/package-feed.log`
- `specs/172-fix-mouse-lag/readiness/logs/package-feed-sample-tests.log`

## Post-Merge Package Bump

- Pack output: `~/.local/share/nuget-local/`.
- Bumped packages: `FS.GG.UI.*` projects to `0.1.34-preview.1`; template package to `0.1.17-preview.1`.
- Initial pack caveat: first `src/Input/Input.fsproj` pack failed because `Fable.Core.dll` was missing from the global NuGet cache after cache clearing; a forced restore repaired the cache and the retry packed all projects successfully.
- Post-merge package-feed proof: passed.
- Post-merge app rebuild exit code: `0`.
- Post-merge sample test exit code: `0`.

Logs:
- `specs/172-fix-mouse-lag/readiness/logs/post-merge-pack.log`
- `specs/172-fix-mouse-lag/readiness/logs/post-merge-package-feed.log`
