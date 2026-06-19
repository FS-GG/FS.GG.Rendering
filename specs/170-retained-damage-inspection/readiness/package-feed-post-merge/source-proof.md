# Package Source Proof

- Status: `passed`
- Local feed: `/home/developer/.local/share/nuget-local`
- Package cache: `/home/developer/projects/FS.GG.Rendering/specs/170-retained-damage-inspection/readiness/package-feed-post-merge/nuget-cache`
- Global cache cleared: `false`
- Selected samples: `samples/AntShowcase`
- Restore command: `dotnet restore /home/developer/projects/FS.GG.Rendering/samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj --configfile specs/170-retained-damage-inspection/readiness/package-feed-post-merge/source-rules.nuget.config --packages /home/developer/projects/FS.GG.Rendering/specs/170-retained-damage-inspection/readiness/package-feed-post-merge/nuget-cache`
- Restore log: `specs/170-retained-damage-inspection/readiness/package-feed-post-merge/restore.log`

## Source Rules

- `FS.GG.UI.*` -> `/home/developer/.local/share/nuget-local`
- `*` -> `https://api.nuget.org/v3/index.json`

## Violations

- None.
