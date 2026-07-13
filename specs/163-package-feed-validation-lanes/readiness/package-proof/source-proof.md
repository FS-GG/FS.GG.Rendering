# Package Source Proof

- Status: `passed`
- Local feed: `artifacts/package-feed`
- Package cache: `artifacts/package-feed-cache`
- Global cache cleared: `false`
- Selected samples: `samples/AntShowcase, samples/ControlsGallery, samples/SampleApps, samples/SecondAntShowcase`
- Restore command: `dotnet restore samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj --configfile specs/163-package-feed-validation-lanes/readiness/package-proof/source-rules.nuget.config --packages artifacts/package-feed-cache`
- Restore log: `specs/163-package-feed-validation-lanes/readiness/package-proof/restore.log`
- Build log: `specs/163-package-feed-validation-lanes/readiness/package-proof/build.log`

## Source Rules

- `FS.GG.UI.*` -> `artifacts/package-feed`
- `*` -> `https://api.nuget.org/v3/index.json`

## Violations

- None.
