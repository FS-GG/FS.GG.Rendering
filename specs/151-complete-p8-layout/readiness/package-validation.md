# Feature151 Package Validation

Status: `accepted`

| Validation | Status | Evidence |
|---|---|---|
| `dotnet restore FS.GG.Rendering.slnx` | `accepted` | Local validation command completed. |
| `dotnet build FS.GG.Rendering.slnx --no-restore` | `accepted` | Solution build completed. |
| `dotnet test FS.GG.Rendering.slnx` | `accepted` | Full solution validation completed. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature151|Surface"` | `accepted` | Package compatibility and surface checks completed. |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature151` | `accepted` | Readiness helper checks completed. |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | `accepted` | No unexpected Feature151 public surface drift. |
| `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local` | `accepted` | Source packages packed at `0.1.13-preview.1`. |
| `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local` | `accepted` | Template package packed at `0.1.7-preview.1`. |

## Package Verdicts

- Full solution result: `accepted`.
- Package surface result: `accepted`.
- Local source pack result: `accepted` at `0.1.13-preview.1`.
- Template pack result: `accepted` at `0.1.7-preview.1`.
- Local feed path: `~/.local/share/nuget-local/`.
- Failed results: none.
- Skipped results: none.
- Environment-limited results: compositor live partial-redraw proof remains a P7 limitation and is not counted as accepted P8 behavior.
