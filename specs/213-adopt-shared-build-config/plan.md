# Implementation Plan: Adopt org-shared .NET build config (unified restore-lock gate)

**Branch**: `213-adopt-shared-build-config` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/213-adopt-shared-build-config/spec.md`

**Board item**: [FS.GG.Rendering#11](https://github.com/FS-GG/FS.GG.Rendering/issues/11) — *H3 · rendering — Adopt shared-build-config; migrate RestoreLockedMode gate CIB→GITHUB_ACTIONS*. Contract: `shared-build-config`. Source of truth: FS-GG/.github `dist/dotnet/` (ADR-0006, `.github#19`, merged).

## Summary

Stop maintaining a forked .NET build baseline. Take the org-shared `Directory.Build.props`,
`Directory.Packages.props`, and `.config/dotnet-tools.json` **verbatim** from FS-GG/.github
`dist/dotnet/` via `scripts/sync-build-config.sh --adopt`, and relocate everything specific to
FS.GG.Rendering into repo-owned `Directory.Build.local.props` / `Directory.Packages.local.props`
(imported last by the canonical files). The canonical `Directory.Build.props` unifies the
`RestoreLockedMode` gate on **`GITHUB_ACTIONS`** instead of `ContinuousIntegrationBuild` (Rendering
is the lone outlier — ADR-0006), drops the duplicate `FSharp.Core` pin in favour of the org baseline
(`10.1.301`), and enables `CentralPackageTransitivePinningEnabled` — which regenerates all 39
committed lockfiles. Two policy tests that read the root config files are updated, restore→build→test
is re-proven green and reproducible, and the post-adoption drift check (`--check`) reports zero drift.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a build-configuration change with **no application behavior**, so the "live app" smoke is
> replaced by the equivalent build-level live proof. The Foundational phase MUST, before any test
> edits are committed as done, run the real toolchain end-to-end: `dotnet restore --force-evaluate`
> (regenerate lockfiles) → `dotnet restore --locked-mode` → `dotnet build` → `dotnet test` →
> `sync-build-config.sh --check`, plus the two gate-condition probes (locked under `GITHUB_ACTIONS`,
> unlocked on a fresh clone) and a deliberate-substitution probe. The "the local clobbers the gate /
> clobbers NU1603" hypotheses below are provisional until that run confirms or replaces them.

## Technical Context

**Language/Version**: F# on .NET `net10.0`. No application F# source changes; only build-config files
and two Expecto policy tests change.

**Primary Dependencies**: MSBuild + Central Package Management (CPM); NuGet `packages.lock.json`
locked restore; the FS-GG org-shared build config (`.github/dist/dotnet/`, distributed by
`scripts/sync-build-config.sh`, ADR-0006); FAKE `6.1.4` (compiled front-end `build/Build.fsproj`,
run via `dotnet run`); Expecto for the policy tests.

**Storage**: N/A — repo configuration files only.

**Testing**: Expecto policy assertions (`tests/Build.Tests/RestoreLockTests.fs`,
`tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs`); the live restore→build→test cycle;
`sync-build-config.sh --check` drift gate; `dotnet restore --locked-mode` (the existing CI
enforcement point in `gate.yml`).

**Target Platform**: GitHub Actions CI (where `GITHUB_ACTIONS` is set) and local Linux dev clones.

**Project Type**: Build-infrastructure / single-repo configuration change.

**Performance Goals**: N/A. The relevant property is determinism — byte-reproducible restore (an
unchanged lockfile on a second restore).

**Constraints**:
- Managed files (the three synced files) MUST be byte-identical to the canonical source (drift-clean).
- A fresh local clone (no `GITHUB_ACTIONS`, possibly no lockfile) MUST NOT be blocked by locked restore.
- No `.fsi` / public F# API surface change; SkiaSharp/preview/Silk/Yoga pins keep their exact versions.
- `template/base/` emitted files are out of scope and MUST NOT change.

**Scale/Scope**: 3 managed files + 2 new local override files at repo root; 39 committed lockfiles
regenerated (transitive pinning); 2 test files updated; 0 template/base changes.

**Change Classification**: **Tier 1** — adopts a cross-repo contract (`shared-build-config`) and
changes observable build/restore behavior (the gate spelling, transitive pinning). It does **not**
touch any `.fs`/`.fsi` public surface, so surface-area baselines remain untouched; the Tier 1
artifact chain here is spec + plan + updated policy tests + the contract doc + reproducible-restore
evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **N/A (adapted)** | No new F# public surface, so the FSI-sketch step does not apply. The evidence order is preserved at the build-config level: the two policy tests are updated to fail-before/pass-after, and the live restore proof is the semantic test. |
| II. Visibility lives in `.fsi` | **N/A** | No `.fs`/`.fsi` changes. |
| III. Idiomatic simplicity | **PASS** | Sync-not-fork is the simplest adoption; no bespoke build machinery is introduced. The decision to NOT add a one-off CI drift step (deferring to the org reusable workflow `.github#18`) is the simpler path (see research R6). |
| IV. Elmish/MVU boundary | **N/A** | No stateful/I-O feature code. |
| V. Test evidence is mandatory | **PASS** | The two breaking policy tests are updated so they assert the new contract (fail before / pass after). Real evidence: live restore→build→test, `--check`, gate-condition probes, deliberate-substitution probe. No synthetic evidence. |
| VI. Observability & safe failure | **PASS** | Locked restore fails LOUD in CI (existing `gate.yml` error annotation); `--check` exits non-zero on drift; NU1603/NU1608 promotion keeps silent substitution fatal. |
| Engineering Constraints | **PASS** | `net10.0` preserved; SkiaSharp/preview/Silk/Yoga pins keep exact versions (moved to local); `FSharp.Core` resolves to the same `10.1.301` via org baseline; no new dependency (the `fake-cli 6.1.4` manifest matches the existing `Fake.Core.* 6.1.4` library pin); pack output location unaffected. |

**Result**: PASS — no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/213-adopt-shared-build-config/
├── plan.md              # This file
├── research.md          # Phase 0 — adoption mechanics + the local-prune correctness rules
├── data-model.md        # Phase 1 — the config-file entities + the property partition table
├── contracts/
│   └── shared-build-config-adoption.md   # the adoption invariants this feature must satisfy
├── quickstart.md        # Phase 1 — runnable end-to-end validation
└── checklists/
    └── requirements.md  # spec quality checklist (already passing)
```

### Source Code (repository root)

```text
Directory.Build.props            # REPLACED by canonical (synced; DO NOT EDIT) — GITHUB_ACTIONS gate
Directory.Build.local.props      # NEW (repo-owned) — TargetFramework/lang, F# warning promotions,
                                 #   package metadata, fsdocs, README packing, FSharp.Core ref
Directory.Packages.props         # REPLACED by canonical (synced) — CPM + transitive pinning + baseline
Directory.Packages.local.props   # NEW (repo-owned) — every non-baseline PackageVersion (NO FSharp.Core)
.config/
└── dotnet-tools.json            # NEW — synced fake-cli 6.1.4 (matches Fake.Core.* 6.1.4)

**/packages.lock.json            # 39 files REGENERATED (transitive pinning) and re-committed

tests/Build.Tests/RestoreLockTests.fs                       # gate assertion CIB → GITHUB_ACTIONS
tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs  # read pin from *.local.props

template/base/**                 # OUT OF SCOPE — unchanged (separate template contract)
```

**Structure Decision**: This is a repo-root configuration change. The "source" is the four `*.props`
files + the tool manifest at the repo root, plus 39 regenerated lockfiles and two policy-test edits.
No new projects or source directories are created.

## Complexity Tracking

*No Constitution Check violations — section intentionally empty.*
