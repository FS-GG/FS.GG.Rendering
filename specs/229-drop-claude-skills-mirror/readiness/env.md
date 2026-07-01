# Environment (T001) — Feature 229

- Branch: `229-drop-claude-skills-mirror`
- .NET SDK: `10.0.301` (net10.0)
- Template live-test: packed the working tree via `dotnet pack .template.package/FS.GG.UI.Template.fsproj`
  and `dotnet new install`ed the dev nupkg before each live scaffold (per the "Template live-test
  workflow" memory — `dotnet new` uses the installed package, not the working tree). Dev versions used:
  `0.1.58-dev229pre.1` (pre-fix repro), `0.1.58-dev229post.2` (fully-confined post-fix).
- Template package version bumped `0.1.58-preview.1` → `0.1.59-preview.1` (FR-008 re-release vehicle).
- Framework pin `FsGgUiVersion` unchanged at `0.1.58-preview.1` (no `src/` change; Feature 209 coherence gate green).
