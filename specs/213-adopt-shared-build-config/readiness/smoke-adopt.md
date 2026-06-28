# T005 — Early live smoke run (build-level live proof) (Feature 213)

Date: 2026-06-28

## Step A — `../.github/scripts/sync-build-config.sh --adopt .`

```
adopted: Directory.Build.props -> Directory.Build.local.props
wrote: Directory.Build.props
adopted: Directory.Packages.props -> Directory.Packages.local.props
wrote: Directory.Packages.props
wrote: .config/dotnet-tools.json
Done (adopt).
```

Result: the two hand-authored root files were renamed to `*.local.props` (UNPRUNED — still the full
old contents), the three canonical managed files were written, and the tool manifest was created.

## Step B — first `dotnet restore FS.GG.Rendering.slnx --force-evaluate` BEFORE prune

Observed raw failures (exit non-zero):

1. **Duplicate `FSharp.Core` — `NU1506` on every project** (not `NU1504`/`NU1011` as the plan
   hypothesized; the actual SDK 10.0.301 diagnostic for a duplicate CPM `PackageVersion` is **NU1506**
   "Duplicate 'PackageVersion' items found … FSharp.Core 10.1.301, FSharp.Core 10.1.301"). Cause: the
   unpruned `Directory.Packages.local.props` still declares `FSharp.Core 10.1.301` while the canonical
   `Directory.Packages.props` org baseline declares it too. → resolved by **T009** (drop the local
   `FSharp.Core` `PackageVersion`).

2. **`error : Invalid framework identifier ''` at `NuGet.targets(198,5)`** (fatal, aborts the slnx
   restore). This is the cascade from the duplicate-pin graph failure above under the now-enabled
   `CentralPackageTransitivePinningEnabled`; the slnx restore cannot evaluate a TFM once CPM
   resolution fails. → resolved together with the duplicate removal (T009) once the prune completes.

## Hypothesis reconciliation (plan §Standing assumption)

| Plan hypothesis | Verdict | Actual |
|---|---|---|
| Duplicate `FSharp.Core` → CPM `NU1504`/`NU1011` | **CONFIRMED (code refined)** | surfaces as `NU1506` (duplicate PackageVersion) + a fatal `Invalid framework identifier ''` cascade |
| "local clobbers the gate" (CIB `RestoreLockedMode` wins on last-import) | **CONFIRMED structurally** | the unpruned local still carries the `ContinuousIntegrationBuild` `RestoreLockedMode`; because the canonical imports the local LAST it would override the `GITHUB_ACTIONS` gate → must be dropped by T008/T015 |
| "local clobbers NU1603" (`WarningsAsErrors` absolute assignment) | **CONFIRMED structurally** | the unpruned local's first `WarningsAsErrors` line is an absolute assignment (`FS0025;…`) that, imported last, would erase the canonical `NU1603;NU1608` → must become append-form (T008), verified by the substitution probe (T021) |

The prune (US1 T008/T009) is therefore the fix for all three; the post-prune restore (T011) is the
confirmation.
