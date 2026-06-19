# Package Source Proof

- Status: `passed`
- Local feed: `/home/developer/.local/share/nuget-local`
- Package cache: `/home/developer/projects/FS.GG.Rendering/specs/174-fix-render-lag/readiness/package-proof/nuget-cache`
- Global cache cleared: `true`
- Selected samples: `samples/SecondAntShowcase`
- Restore command: `dotnet restore /home/developer/projects/FS.GG.Rendering/samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj --configfile specs/174-fix-render-lag/readiness/package-proof/source-rules.nuget.config --packages /home/developer/projects/FS.GG.Rendering/specs/174-fix-render-lag/readiness/package-proof/nuget-cache`
- Restore log: `specs/174-fix-render-lag/readiness/package-proof/restore.log`

## Source Rules

- `FS.GG.UI.*` -> `/home/developer/.local/share/nuget-local`
- `*` -> `https://api.nuget.org/v3/index.json`

## Violations

- None.
