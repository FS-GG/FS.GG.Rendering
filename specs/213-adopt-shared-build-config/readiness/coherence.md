# US3 + reproducible-restore coherence (T012/T018–T020) (Feature 213)

Date: 2026-06-28

## Reproducible restore (T012 / INV-6 / SC-004 / C-6)

- `dotnet restore FS.GG.Rendering.slnx --force-evaluate` → regenerated all lockfiles, exit 0.
- The two samples outside the slnx (`samples/CanvasDemo`, `samples/SymbologyBoard`) regenerated too.
- `dotnet restore FS.GG.Rendering.slnx --locked-mode` → exit 0.
- `git status --short -- '**/packages.lock.json'` → **0 changed lockfiles** → REPRODUCIBLE.

### Empirical finding vs research R4 (transitive pinning churn)

Research R4 expected `CentralPackageTransitivePinningEnabled` to regenerate all ~39 lockfiles with
**changed content**. The actual result: **zero lockfile content change**. NuGet lockfiles already
record the full transitive closure; with no floating transitive version conflicts and `FSharp.Core`
unchanged at `10.1.301` (org baseline == old local pin), the resolved graph is identical, so transitive
pinning added nothing to commit. This is a cleaner outcome and trivially satisfies INV-6; the R4
hypothesis is replaced by this observation (plan §Standing assumption — hypotheses unverified until run).

## FSharp.Core single-sourced (T018 / T019 / C-3 / INV-3 / SC-006)

| Check | Expected | Observed |
|---|---|---|
| `FSharp.Core` `PackageVersion` in `Directory.Packages.local.props` | 0 | 0 |
| `FSharp.Core` baseline in canonical `Directory.Packages.props` | 1 | 1 |
| version-less `FSharp.Core` `PackageReference` in `Directory.Build.local.props` | 1 | 1 |
| clean restore duplicate error (`NU1506`/`NU1504`/`NU1011`) | none | none (exit 0) |
| lockfile `FSharp.Core` resolved version | `10.1.301` | `"resolved": "10.1.301"` (Direct) |

## Tool/library parity (T020 / C-5 / INV-5)

- `.config/dotnet-tools.json` `fake-cli` → `6.1.4`.
- `Directory.Packages.local.props` `Fake.Core.Target` → `6.1.4`. **Match.**
- The compiled-FAKE front-end `src/Build/FS.GG.UI.Build.fsproj` is a slnx member that built green in
  the T013 slnx build and restores up-to-date (`dotnet restore src/Build/...` exit 0) — the tool
  manifest does not disturb the existing front-end.
