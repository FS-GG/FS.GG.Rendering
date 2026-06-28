# Quickstart: validate the shared-build-config adoption

Runnable end-to-end validation for Feature 213. Assumes a sibling `../.github` checkout (source of
truth) alongside this repo. Implementation detail lives in `tasks.md`; this is the run/validation
guide. Contract IDs (C-n) and invariants (INV-n) refer to `contracts/` and `data-model.md`.

## Prerequisites

- `../.github/dist/dotnet/` and `../.github/scripts/sync-build-config.sh` present (verified).
- .NET SDK `net10.0`; this repo at branch `213-adopt-shared-build-config`.

## 1. Adopt the canonical files (C-1)

```sh
../.github/scripts/sync-build-config.sh --adopt .
```

Expect: `adopted: Directory.Build.props -> Directory.Build.local.props`,
`adopted: Directory.Packages.props -> Directory.Packages.local.props`, and the three managed files
written. Then **prune** the two generated `*.local.props` per `data-model.md` (drop the restore block
+ CIB gate from `Directory.Build.local.props`; drop the CPM group + `FSharp.Core` pin from
`Directory.Packages.local.props`; make the F# warning line append-form, `$(WarningsAsErrors);…`).

## 2. Drift check — managed files pristine (C-1 / INV-1)

```sh
../.github/scripts/sync-build-config.sh --check .
```

Expect: `ok: Directory.Build.props`, `ok: Directory.Packages.props`, `ok: .config/dotnet-tools.json`,
exit 0. Any `DRIFT` line means a managed file was edited — move that content to its `*.local.props`.

## 3. Regenerate + commit lockfiles (transitive pinning) (C-6 / INV-6)

```sh
dotnet restore FS.GG.Rendering.slnx --force-evaluate     # regenerate all 39 lockfiles
git add '**/packages.lock.json'
dotnet restore FS.GG.Rendering.slnx --locked-mode        # second restore: must be reproducible
git diff --quiet -- '**/packages.lock.json' && echo "REPRODUCIBLE" || echo "LOCKFILE CHURN — investigate"
```

Expect: first restore succeeds (no `NU1504`/`NU1011` from a duplicate `FSharp.Core`); second restore
succeeds under `--locked-mode`; `REPRODUCIBLE`.

## 4. Build + test green (C-6 / C-7 / R8)

```sh
dotnet build FS.GG.Rendering.slnx -c Debug --no-restore
dotnet test  tests/Build.Tests/Build.Tests.fsproj
dotnet test  tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
```

Expect: build green; the updated `RestoreLockTests` (gate asserts `GITHUB_ACTIONS`) and
`Feature142SurfaceAndDependencyTests` (reads `Directory.Packages.local.props`) pass.

## 5. Gate-condition probes (C-2 / SC-002)

```sh
# Locked in CI: with the lockfile present and the CI signal set, locked restore engages.
GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode && echo "CI: locked OK"
# Fresh local clone: no CI signal → not blocked (and with no lockfile a first restore bootstraps).
env -u GITHUB_ACTIONS dotnet restore FS.GG.Rendering.slnx && echo "LOCAL: not blocked"
```

## 6. Deliberate-substitution probe — enforcement preserved (C-4 / INV-4)

Temporarily change one `PackageVersion` in `Directory.Packages.local.props` (e.g. bump `YamlDotNet`)
**without** regenerating the lockfile, then:

```sh
GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode    # MUST FAIL (graph ≠ lockfile / NU1603)
```

Expect a non-zero exit with the `gate.yml` regenerate hint. **Revert the change** afterwards. This
confirms the `WarningsAsErrors` append rule (R3) did not silently disable `NU1603`/`NU1608`.

## 7. Tool/library parity + scope boundary (C-5 / C-8 / INV-5 / INV-7)

```sh
grep -n '"fake-cli"' -A2 .config/dotnet-tools.json      # 6.1.4
grep -n 'Fake.Core' Directory.Packages.local.props      # 6.1.4 — must match
git diff --quiet -- template/base && echo "template/base UNCHANGED" || echo "SCOPE LEAK into template/base"
```

## Success = all of

- §2 zero drift · §3 `REPRODUCIBLE` · §4 build+tests green · §5 both probes as labeled ·
  §6 substitution fails then reverted · §7 `6.1.4`==`6.1.4` and `template/base UNCHANGED`.
- Maps to SC-001…SC-006 in `spec.md`.
