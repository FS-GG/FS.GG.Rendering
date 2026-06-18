# Feature 152 Package Validation

Status: `accepted-with-tooling-limitation`

## Commands

| Command | Result |
|---------|--------|
| `dotnet test FS.GG.Rendering.slnx --no-restore` | passed |
| `./fake.sh build -t PackageSurfaceCheck` | blocked: root `./fake.sh` is absent in this checkout |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface --no-restore` | passed, 18 tests |
| `./fake.sh build -t PackLocal` | blocked: root `./fake.sh` is absent in this checkout |
| `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local --no-restore` | passed before merge for source packages at `0.1.14-preview.1`; passed after post-merge bump for source packages at `0.1.15-preview.1` |
| `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local` | passed after post-merge bump for template package at `0.1.9-preview.1` |

## Landing Status

- Feature branch commit: `4151f24`
- Squash merge on `main`: `8ea61c4`
- Post-merge package bump: `61d1ce8`
- Pushed state: `origin/main` received the Feature 152 squash merge before the package bump; the package bump and this evidence update are the final post-merge push scope.

## Package Impact

- `FS.GG.UI.SkiaViewer` exposes Feature 152 proof-set vocabulary.
- `FS.GG.UI.Testing` exposes Feature 152 compositor readiness helpers.
- No accepted partial-redraw or performance claim is recorded from the current environment-limited evidence.

The root Fake wrapper limitation is repository/tooling state already seen by prior features in this checkout, not a Feature 152 implementation failure.
