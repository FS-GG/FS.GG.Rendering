# T004 — Root-cause confirmation (research.md R0–R4 vs repo HEAD)

Confirmed against repo HEAD on branch `202-fix-build-fsx-engine`.

| # | Claim | Command / evidence | Verdict |
|---|-------|--------------------|---------|
| R1 | Stale pre-rebrand cache probe | `grep -n fs.skia.ui.build template/base/build.fsx` → line 126 `path [ nugetPackages; "fs.skia.ui.build"; version; "lib"; "net10.0"; "FS.GG.UI.Build.dll" ]` | **Confirmed** — wrong folder id (`fs.skia.ui.build` vs `fs.gg.ui.build`) |
| R2a | No `FS.GG.UI.Build` producer | `grep -rl 'PackageId>FS.GG.UI.Build<\|GeneratedRunner' --include=*.fsproj --include=*.fs --include=*.fsi src/ tools/` → no matches | **Confirmed** — no producer / no `GeneratedRunner` source anywhere |
| R2b | Absent from local feed | `ls ~/.local/share/nuget-local/ | grep -i FS.GG.UI.Build` → ABSENT (feed carries `FS.GG.UI.*` libs to `0.1.48-preview.1` + `FS.GG.Governance.Cli`) | **Confirmed** |
| R2c | Absent from global cache | `ls ~/.nuget/packages/ | grep -i 'fs.gg.ui.build\|fs.skia.ui.build'` → ABSENT | **Confirmed** |

## Environment

- `dotnet --version` → `10.0.301`
- NuGet sources: `nuget.org` [Enabled], `local-feed` → `/home/developer/.local/share/nuget-local` [Enabled]
- Max per-project `<Version>` in `src/**` and feed: `0.1.48-preview.1` ⇒ fresh coherent pack version `V=0.1.49-preview.1`.

**Conclusion:** Both root causes hold. The fix requires (1) producing an in-repo `FS.GG.UI.Build`
engine that exposes `GeneratedRunner.run`, and (2) correcting the `fs.skia.ui.build` → `fs.gg.ui.build`
cache probe. Re-pathing alone (R1) is necessary but insufficient because the producer is missing (R2).
