# Contract: Restore policy (`Directory.Build.props` + scope boundary)

The build-configuration contract this feature establishes. "Contract" here = the MSBuild surface other
projects and CI depend on, plus the scope guarantee.

## Root `Directory.Build.props` — added properties

```xml
<PropertyGroup>
  <!-- 211 (FR-001/FR-002/FR-003): mirror template/base/Directory.Build.props (Feature 204) so the
       repo locks restore exactly like the products it generates (FR-007). RestoreLockedMode is gated
       to CI AND an existing lockfile, so a fresh clone / first restore is never blocked locally and
       bootstrap generates the lockfile instead of failing. -->
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  <RestoreLockedMode Condition="'$(ContinuousIntegrationBuild)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>
  <!-- 211 (FR-004): silent version substitution (NU1603) and out-of-range (NU1608) become errors.
       VERIFY empirically — restore-phase NU16xx are not reliably promoted by TreatWarningsAsErrors. -->
  <WarningsAsErrors>$(WarningsAsErrors);NU1603;NU1608</WarningsAsErrors>
</PropertyGroup>
```

### Guarantees
- **G1 (FR-001)**: every project that inherits root (i.e. does not shadow it) writes/uses a
  `packages.lock.json`.
- **G2 (FR-002)**: when `ContinuousIntegrationBuild=true` and a lockfile is present, restore is
  locked; a graph differing from the lockfile fails restore.
- **G3 (FR-003)**: when `ContinuousIntegrationBuild` is unset (local) OR no lockfile exists yet,
  locked mode is off — never blocks.
- **G4 (FR-004)**: NU1603/NU1608 fail the restore/build (subject to empirical confirmation; fallback
  is `-warnaserror:NU1603` on the explicit restore step — see gate-restore contract).

## `tests/Package.Tests/Package.Tests.fsproj` — opt-out

```xml
<PropertyGroup>
  <!-- 211 (FR-006): release-only lane; consumes packed FS.GG.UI.* at TEST RUNTIME and is restored in
       release.yml under ContinuousIntegrationBuild=true. Opt out of lockfile so it is never locked. -->
  <RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>
  <RestoreLockedMode>false</RestoreLockedMode>
</PropertyGroup>
```

### Guarantee
- **G5 (FR-006)**: `Package.Tests` never produces or is gated by a lockfile.

## Scope boundary contract

| Set | Members | Lockfile committed? |
|-----|---------|---------------------|
| LOCKED | `FS.GG.Rendering.slnx` membership (incl. `CanvasDemo`, `SymbologyBoard`); excludes `Package.Tests` | **yes** |
| EXCLUDED | 4 standalone samples (shadow root), `Package.Tests` (opt-out), template products (out of tree) | **no** |

- **G6 (FR-006/SC-006)**: the 4 standalone samples are excluded *with no edit* because their existing
  `Directory.Build.props` shadows the root. Any change that makes them inherit root would break this
  guarantee and MUST be rejected.

## Consumer expectations
- A developer running `dotnet build`/`dotnet restore` locally is **not** affected (G3).
- CI (`gate.yml`, `release.yml`) inherits G2/G4 automatically via `ContinuousIntegrationBuild=true`.
- Intentional dependency updates use the regenerate command (see `gate-restore.md` / quickstart).
