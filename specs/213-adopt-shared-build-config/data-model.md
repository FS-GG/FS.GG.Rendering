# Phase 1 Data Model: Adopt org-shared .NET build config

The "data" of this feature is the set of build-configuration files and the partition of MSBuild
properties / package pins between the synced (canonical) layer and the repo-owned (local) layer.

## Entities

### Managed file (synced; DO NOT EDIT)
Taken verbatim from FS-GG/.github `dist/dotnet/`. Identified by the marker
`Source of truth: FS-GG/.github`. Any local edit is **drift** (`--check` exits non-zero).

| File | Provides |
|---|---|
| `Directory.Build.props` | `Deterministic`; CPM enablement + `CentralPackageTransitivePinningEnabled`; the `lockfile-restore-enforcement` block (`RestorePackagesWithLockFile`, `RestoreLockedMode` gated on `GITHUB_ACTIONS And Exists(lockfile)`, `WarningsAsErrors += NU1603;NU1608`); imports `Directory.Build.local.props` last. |
| `Directory.Packages.props` | CPM + transitive pinning; the org baseline `FSharp.Core 10.1.301`; imports `Directory.Packages.local.props` last. |
| `.config/dotnet-tools.json` | Pinned local tool manifest `fake-cli 6.1.4`. |

### Local override file (repo-owned)
Imported last by the managed file, so it may override any org default; the home for everything
specific to FS.GG.Rendering. **Not** touched by the sync; **not** checked by `--check`.

| File | Holds |
|---|---|
| `Directory.Build.local.props` | `TargetFramework=net10.0`, `LangVersion`, `Nullable`, `AllowUnsafeBlocks`; F# warning promotions (append-form, see rule below); package metadata; fsdocs; `FSharp.Core` `PackageReference`; README packing. |
| `Directory.Packages.local.props` | Every non-baseline `PackageVersion` (runtime + test-only + build-tooling). **No** `FSharp.Core`; **no** redundant CPM property group. |

### Org baseline
The single cross-repo coherence point, declared once in the canonical `Directory.Packages.props`:
`FSharp.Core 10.1.301`. A package pinned here MUST NOT be re-declared locally (CPM `NU1504`/`NU1011`).

### Unified restore-lock gate
`<RestoreLockedMode Condition="'$(GITHUB_ACTIONS)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>`
— locked in CI, never fail-closed locally, bootstrappable on first restore.

### Committed lockfile
`packages.lock.json` per locked project (39 in the repo). Regenerated under transitive pinning;
re-committed. Enforced in CI by `dotnet restore --locked-mode` (`gate.yml`).

## Property / pin partition

| Setting (current root file) | Destination after adoption | Note |
|---|---|---|
| `TargetFramework`, `LangVersion`, `Nullable`, `AllowUnsafeBlocks` | `Directory.Build.local.props` | repo-specific |
| `TreatWarningsAsErrors` | `Directory.Build.local.props` | repo-specific |
| `WarningsAsErrors = FS0025;FS0026;FS0052;FS0064` (+`FS0078`) | `Directory.Build.local.props` | **append-form** (`$(WarningsAsErrors);…`) to preserve canonical `NU1603;NU1608` |
| `RestorePackagesWithLockFile` | **canonical** (drop from local) | now org-owned |
| `RestoreLockedMode` (CIB gate) | **canonical, respelled `GITHUB_ACTIONS`** (drop CIB from local) | the migration |
| `WarningsAsErrors += NU1603;NU1608` | **canonical** (drop from local) | now org-owned |
| `Package` metadata (`Version`, `Authors`, repo URLs, license, readme) | `Directory.Build.local.props` | repo-specific |
| `FsDocs*` group | `Directory.Build.local.props` | repo-specific |
| `FSharp.Core` `PackageReference` (no version) | `Directory.Build.local.props` | resolves via CPM |
| README `None Include … Pack=true` group | `Directory.Build.local.props` | repo-specific |
| `ManagePackageVersionsCentrally` (Packages.props) | **canonical** (drop from local) | now org-owned (+ transitive pinning) |
| `FSharp.Core` `PackageVersion 10.1.301` | **DROPPED** (org baseline) | duplicate would error |
| all other `PackageVersion` items | `Directory.Packages.local.props` | versions unchanged |

## Invariants (testable)

- **INV-1 (drift-clean)**: the three managed files are byte-identical to canonical (`--check` = 0).
- **INV-2 (gate spelling)**: the effective restore-lock gate is `GITHUB_ACTIONS`, not `ContinuousIntegrationBuild`; Rendering matches the other three FS-GG repos.
- **INV-3 (baseline non-duplication)**: `FSharp.Core` is declared exactly once (org baseline), resolves to `10.1.301`, no CPM duplicate error.
- **INV-4 (enforcement preserved)**: `NU1603`/`NU1608` remain promotion-to-error in the *effective* build (substitution probe fails restore).
- **INV-5 (tool/library parity)**: `.config/dotnet-tools.json` `fake-cli` == `Fake.Core.*` library pin (`6.1.4`).
- **INV-6 (reproducible restore)**: a second restore leaves every lockfile unchanged.
- **INV-7 (scope boundary)**: `template/base/**` is byte-unchanged by this feature.
