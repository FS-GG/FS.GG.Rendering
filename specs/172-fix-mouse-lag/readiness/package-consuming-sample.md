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
