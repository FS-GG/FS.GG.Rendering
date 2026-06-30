# T016 — Generated `game` product builds + passes governance, zero edits (FR-004, SC-003)

In the feed-scaffolded `GameProbe` (pins `FsGgUiVersion=0.1.54-preview.1`):

```
$ dotnet build GameProbe.slnx -c Debug
  Restored src/GameProbe/GameProbe.fsproj            # FS.GG.UI.* @0.1.54 from the org feed
  Restored tests/GameProbe.Tests/GameProbe.Tests.fsproj
  GameProbe -> …/GameProbe.dll
Build succeeded.  0 Warning(s)  0 Error(s)

$ dotnet test GameProbe.slnx -c Debug --no-build
Passed!  - Failed: 0, Passed: 26, Skipped: 0, Total: 26 - GameProbe.Tests.dll (net10.0)
```

- Build: **0 warn / 0 err**, restoring the published `0.1.54-preview.1` set from the feed.
- Governance: **26/26 pass** with **zero `GovernanceTests` edits** — the family-agnostic entrypoint
  assertion (`game → Viewer.runApp generatedHost`, no `-- pong` flag) is satisfied by the stock
  skeleton (Feature 220), proven here against the published set. FR-004 / SC-003 ✅
