# T007 — Early live smoke run (Foundational gate)

**Live**, on the validated environment (dotnet 10.0.301, local-feed at `~/.local/share/nuget-local`).

## Setup

- Packed a COHERENT feed at a fresh `V=0.1.49-preview.1`:
  `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=0.1.49-preview.1 -o ~/.local/share/nuget-local`
  → `FS.GG.UI.Build.0.1.49-preview.1.nupkg` created (the previously-missing producer is now real).
- Set `template/base/Directory.Packages.props` `FsSkiaUiVersion=0.1.49-preview.1`.
- Installed the template, generated the `governed` profile, ran `dotnet fsi build.fsx target EvidenceGraph`.

## Result — resolve/load/invoke path PROVEN

`EvidenceGraph` exited 0 and wrote a real `readiness/evidence-graph.md` (514 B). The build.fsx
reflection path resolved `FS.GG.UI.Build` from the local feed, loaded it via `Assembly.LoadFrom`, and
invoked `FS.GG.UI.Build.Evidence.GeneratedRunner.run "EvidenceGraph" <dir>`.

## Observed evidence surface at gate time (the "open runtime question", now answered)

**At gate time `readiness/` is empty** — the `Verify` target does NOT pre-produce the product's
evidence CLI artifacts before invoking EvidenceGraph. The graph reported:

```
- readiness files present: 0
- recognized evidence nodes: 0
## Sensed readiness files
_none — readiness/ is empty or absent_
## Evidence nodes
_no recognized evidence artifacts present; graphed the available surface above_
```

**Design consequence (confirmed, not assumed):** the engine graphs *what exists* and passes a
well-formed-but-sparse surface (exit 0); it does NOT require specific artifacts to be present (that
would abort Verify). The audit rules were built on this observed reality, not on the assumption that
artifacts exist (research.md "Open runtime question" → resolved here).
