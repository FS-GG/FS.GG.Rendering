# T004 — Property / pin partition map (Feature 213)

Reconciliation of the current root `Directory.Build.props` / `Directory.Packages.props` against the
canonical `../.github/dist/dotnet/` files and the `data-model.md` partition table. This is the
root-cause map US1/US2/US3 implement against.

## `Directory.Build.props` (current root) → destination

| Current setting | Destination | Reason |
|---|---|---|
| `TargetFramework=net10.0` | **local** (`Directory.Build.local.props`) | repo-specific |
| `LangVersion=latest`, `Nullable=enable`, `AllowUnsafeBlocks=true` | **local** | repo-specific |
| `TreatWarningsAsErrors=true` | **local** | repo-specific |
| `WarningsAsErrors=FS0025;FS0026;FS0052;FS0064` | **local — APPEND-form** `$(WarningsAsErrors);FS0025;…` | R3: must not clobber canonical `NU1603;NU1608` |
| `WarningsAsErrors=$(WarningsAsErrors);FS0078` | **local** (already append) | repo-specific (Principle II) |
| **`Restore (211)` group** — `RestorePackagesWithLockFile` | **DROP** (canonical owns) | now org-owned |
| **`Restore (211)` group** — `RestoreLockedMode` **CIB** gate | **DROP** (canonical owns, respelled `GITHUB_ACTIONS`) | THE MIGRATION; a surviving local CIB gate wins on last-import (R2/R5) |
| **`Restore (211)` group** — `WarningsAsErrors += NU1603;NU1608` | **DROP** (canonical owns) | now org-owned |
| `Package` metadata group (Version/Authors/Company/RepositoryType/PackageProjectUrl/RepositoryUrl/license/GenerateDocumentationFile/PackageReadmeFile) | **local** | repo-specific |
| `FsDocs` group (7 props) | **local** | repo-specific |
| `PackageReference Include="FSharp.Core"` (no version) | **local** | resolves via CPM baseline |
| `None Include README.md … Pack=true` group | **local** | repo-specific |

Canonical `Directory.Build.props` additionally provides `Deterministic=true`, CPM +
`CentralPackageTransitivePinningEnabled`, and imports `Directory.Build.local.props` **last**.

## `Directory.Packages.props` (current root) → destination

| Current item | Destination | Reason |
|---|---|---|
| `ManagePackageVersionsCentrally=true` property group | **DROP** (canonical owns + transitive pinning) | now org-owned |
| `FSharp.Core` `PackageVersion 10.1.301` | **DROP** | org baseline declares it once; duplicate → CPM `NU1504`/`NU1011` |
| Fable.Elmish 5.0.2; HarfBuzzSharp.NativeAssets.Linux/Win32 8.3.1.6-preview.3.1; Silk.NET.* 2.23.0; SkiaSharp* 4.147.0-preview.3.1; YamlDotNet 18.0.0; Yoga.Net 3.2.3 | **local** | non-baseline runtime pins |
| Expecto 10.2.2; Microsoft.NET.Test.Sdk 17.11.1; YoloDev.Expecto.TestSdk 0.15.3 | **local** | test-only pins |
| Fake.Core.Target 6.1.4; FSharp.SystemTextJson 1.4.36; XParsec 1.0.0; Microsoft.Extensions.FileSystemGlobbing 10.0.9; Fake.IO.FileSystem 6.1.4; Fake.Tools.Git 6.1.4; DiffPlex 1.9.0; FsCheck 3.3.3 | **local** | build-tooling pins |

Canonical `Directory.Packages.props` provides CPM + transitive pinning + `FSharp.Core 10.1.301`
baseline, imports `Directory.Packages.local.props` **last**.

## `.config/dotnet-tools.json`

No pre-existing file in this repo → straight copy of canonical (`fake-cli 6.1.4`). Matches the
`Fake.Core.* 6.1.4` library pin (INV-5).

## Root-cause hypotheses to confirm at the live smoke (T005)

1. **Duplicate `FSharp.Core`** — local `PackageVersion 10.1.301` + canonical baseline → CPM
   `NU1504`/`NU1011` on first restore unless dropped (T009).
2. **Local CIB gate clobber** — unpruned `RestoreLockedMode` (CIB) imported last would silently win
   and re-defeat the `GITHUB_ACTIONS` migration (T008/T015).
3. **`WarningsAsErrors` clobber** — a local absolute assignment would erase canonical `NU1603;NU1608`
   (append-form fix, T008; verified by substitution probe T021).
