# T006 — Test + evidence scaffolding; breaking assertions pinpointed (Feature 213)

`specs/213-adopt-shared-build-config/readiness/` exists and is allowlisted in `.gitignore` (T001).

Two policy tests read the **root** config files and break on adoption (research R8):

## 1. `tests/Build.Tests/RestoreLockTests.fs`

Test: *"root Directory.Build.props carries the restore policy …"* (lines ~76–89).

- **Breaking line 82–83**: `Expect.stringContains props "ContinuousIntegrationBuild" …`.
  After adoption the canonical root `Directory.Build.props` spells the gate `GITHUB_ACTIONS`, so this
  assertion FAILS. → **T014**: change the substring and its message to `GITHUB_ACTIONS`.
- **Still holds** against the canonical root file (no change needed):
  - `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` (line 78)
  - `<RestoreLockedMode` (line 80)
  - `NU1603` in `<WarningsAsErrors>` regex (lines 85–88)
- The 38-project slnx membership / VR-1 / VR-2 assertions are unaffected (membership unchanged; every
  member keeps a regenerated lockfile).

## 2. `tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs`

Test: *"SkiaViewer references SkiaSharp.HarfBuzz through central package management"* (lines 15–21).

- **Breaking line 16**: `File.ReadAllText(Path.Combine(root, "Directory.Packages.props"))` then
  `Expect.stringContains central "SkiaSharp.HarfBuzz"`. After adoption the `SkiaSharp.HarfBuzz`
  `PackageVersion` lives in `Directory.Packages.local.props`, so reading the root canonical file
  FAILS. → **T007**: read `Directory.Packages.local.props` instead.
- The `fsproj` assertion (line 17/20) and the *"Scene package remains free of …"* test are unaffected.

No other test reads the root config for a moved value (`tests/Package.Tests/*` reads `template/base/…`,
out of scope).
