# Implementation Plan: Locked, reproducible dependency restore (repo lockfiles + locked-mode CI)

**Branch**: `211-lockfile-locked-restore` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/211-lockfile-locked-restore/spec.md`

## Summary

Bring the FS.GG.Rendering repo's **own** restore up to the bar it already hands generated products
(template Feature 204): commit a `packages.lock.json` for every project in the gate solution
(`FS.GG.Rendering.slnx`), restore in **locked mode** in CI, and promote silent version-substitution
(NU1603) to a build error. The mechanism mirrors the proven template pattern exactly
(`RestorePackagesWithLockFile` + a CI-and-lockfile-gated `RestoreLockedMode`) so the repo and the
products it generates behave identically (FR-007).

The whole feature is **build/restore configuration** — MSBuild props, committed lockfiles, one CI
step, one opt-out line, docs, and a deterministic test that asserts the policy holds. There is **no
F# public surface change**: no `.fs`/`.fsi` edits, no new shipped dependency, no behavior change to
any library. It locks the *resolution* of the already-pinned dependency graph; it does not change the
graph.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.** Not applicable
> in the usual "drive the running app" sense (this is a CI/restore policy, not a runtime defect). The
> equivalent honesty discipline here: **the locked-restore and NU1603-as-error behaviors MUST be
> proven by actually running restore** — a clean locked restore succeeds, a perturbed graph fails, an
> un-pinnable version fails with NU1603 — not assumed from the props being present. `/speckit-tasks`
> MUST schedule that empirical restore proof in the Foundational phase (see research R3/R4 — NuGet's
> handling of restore-phase warnings under `TreatWarningsAsErrors` is the specific thing not to take
> on faith).

## Technical Context

**Language/Version**: F# on .NET `net10.0`; the artifacts changed here are MSBuild props
(`Directory.Build.props`, `.fsproj`), GitHub Actions YAML (`.github/workflows/gate.yml`), committed
`packages.lock.json` files, and one Expecto test in `tests/Build.Tests`.

**Primary Dependencies**: NuGet Central Package Management (`Directory.Packages.props`) +
NuGet's lockfile mechanism (`RestorePackagesWithLockFile`, `RestoreLockedMode`, `--locked-mode`,
`--force-evaluate`). No new package dependency is introduced.

**Storage**: N/A (source-controlled files only — the lockfiles ARE the persisted artifact).

**Testing**: Expecto (`tests/Build.Tests`) for the deterministic policy/coverage assertion; the
gate's own restore step for the live locked-restore proof; a manual perturbation check (quickstart)
for the fail-closed and NU1603 paths.

**Target Platform**: GitHub Actions `ubuntu-latest` (sets `ContinuousIntegrationBuild=true`, which the
gated locked mode keys off) and developer Linux/Windows local builds (not in CI → not locked).

**Project Type**: Build/CI infrastructure for a single-repo F# product (the `FS.GG.Rendering.slnx`
gate solution).

**Performance Goals**: No measurable runtime impact. Locked restore is equal-or-faster than open
restore (no graph re-resolution); the only cost is committing/maintaining lockfiles.

**Constraints**: MUST NOT destabilize the local-feed lanes (4 standalone samples + `Package.Tests` +
template-instantiated products) whose FS.GG.UI.* preview versions churn every merge (FR-006). MUST
keep all existing gate steps green (FR-009). MUST use a single documented regenerate command (FR-008).

**Scale/Scope**: 38 projects in the slnx (18 `src/`, 17 `tests/`, 1 `tools/`, + 2 in-tree samples
`CanvasDemo`/`SymbologyBoard` = 38) get a committed lockfile. 4 standalone samples + `Package.Tests` are
excluded.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Notes |
|-----------|---------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | **N/A (justified)** | No F# public surface is added or changed. The "implementation" is MSBuild props + committed lockfiles + one CI step. There is no `.fsi` to sketch. Evidence discipline is honored via Principle V (a real Build.Tests assertion) and the live restore proof. |
| II. Visibility lives in `.fsi` | **N/A** | No `.fs`/`.fsi` files touched. |
| III. Idiomatic Simplicity | **PASS** | Plainest possible mechanism: reuse the template's two-property pattern verbatim; no custom MSBuild targets, scripts, or operators. The one Build.Tests addition is straight filesystem assertions. |
| IV. Elmish/MVU boundary | **N/A** | No stateful/I-O F# workflow. (Restore is a build-tool behavior, not application I/O.) |
| V. Test Evidence Is Mandatory | **PASS** | A deterministic Build.Tests case asserts the policy holds (every slnx project has a committed lockfile; excluded lanes do not; root props carry the policy). The behavioral guarantee (locked restore fails on drift / NU1603) is proven by the gate's restore step + the quickstart perturbation — real evidence, no synthetic. |
| VI. Observability and Safe Failure | **PASS** | Locked-mode and NU1603 failures are loud and fail-closed by design (restore aborts, gate blocks). The new gate step emits a clear `::error::` annotation pointing at the regenerate command. |

**Change Classification**: **Tier 2 (internal change)** — no public API surface, no new shipped
dependency, no observable library behavior change; it pins resolution of the existing graph and adds
a CI guard. `spec.md` declares this tier explicitly (Change Classification field), satisfying the
constitution's requirement that every spec name its tier. Per Tier 2, `.fsi` and surface-area
baselines remain untouched (and indeed are not affected).

**Initial gate: PASS.** No violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/211-lockfile-locked-restore/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions on placement, scoping, NU1603, regenerate cmd
├── data-model.md        # Phase 1 — the config "entities" (props knobs, lockfile, scope sets)
├── quickstart.md        # Phase 1 — runnable validation (locked pass / perturb-fail / NU1603 / fresh clone / regenerate)
├── contracts/
│   ├── restore-policy.md   # The Directory.Build.props restore contract + scope boundary
│   └── gate-restore.md     # The gate.yml locked-restore step contract
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

Files this feature touches (no new source projects):

```text
Directory.Build.props                         # ADD: RestorePackagesWithLockFile + gated RestoreLockedMode + NU1603/NU1608-as-error
tests/Package.Tests/Package.Tests.fsproj      # ADD one line: opt OUT of lockfile (FR-006, release-only/local-feed lane)
.github/workflows/gate.yml                     # ADD: explicit "Restore (locked)" step; add --no-restore to the existing build step
tests/Build.Tests/RestoreLockTests.fs (new)   # ADD: deterministic policy/coverage assertion (Principle V)
tests/Build.Tests/Build.Tests.fsproj           # register the new test file
CONTRIBUTING / docs note                        # ADD: the single regenerate command (FR-008)

# Generated + committed (one per slnx project — 38 files):
src/**/packages.lock.json
tests/**/packages.lock.json   (except tests/Package.Tests)
tools/Rendering.Harness/packages.lock.json
samples/CanvasDemo/packages.lock.json
samples/SymbologyBoard/packages.lock.json

# Explicitly NOT created (excluded lanes — FR-006):
samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}/packages.lock.json   # shadow root → never inherit policy
tests/Package.Tests/packages.lock.json                                                  # opted out in fsproj
```

**Structure Decision**: Single-repo build-config change. The locked unit is exactly the gate solution
`FS.GG.Rendering.slnx`. The scope boundary is achieved **structurally, with almost no per-project
work**, because of two existing facts discovered during planning:

1. The 4 standalone local-feed samples (`AntShowcase`, `SampleApps`, `SecondAntShowcase`,
   `ControlsGallery`) each already carry a `Directory.Build.props` that **shadows** the repo root
   (MSBuild stops at the nearest one walking up). The lock policy added to the root therefore **never
   reaches them** — zero changes, automatically excluded.
2. The 2 in-tree samples in the slnx (`CanvasDemo`, `SymbologyBoard`) use **ProjectReferences only**
   (no local-feed `nuget.config`), so their restore graph is the same stable external set as `src/`
   — safe to lock, and correctly included by root inheritance.

That leaves exactly one project needing an explicit opt-out: `tests/Package.Tests` (no own
`Directory.Build.props`, not in the slnx, release-only, consumes packed FS.GG.UI.* at *test runtime*).
It gets one `<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>` line.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
