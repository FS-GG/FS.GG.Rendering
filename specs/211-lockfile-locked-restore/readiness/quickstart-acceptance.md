# Quickstart acceptance (T015) — Scenarios A–G

Feature 211 · 2026-06-28 · SDK 10.0.301. Consolidated end-to-end acceptance. Each scenario maps to the
real-restore evidence gathered during implementation (no synthetic results). Detailed logs in
[`restore-proof.md`](./restore-proof.md) and [`baseline.md`](./baseline.md).

| Scenario | Maps to | Command | Expected | Result |
|---|---|---|---|---|
| **A** Locked CI restore succeeds | US1 / SC-001 | `ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode` | restore succeeds against committed lockfiles | ✅ PASS (restore-proof §a, exit 0) |
| **B** Drift is fail-closed | US1 / SC-002 | perturb a `Directory.Packages.props` version, re-run A | restore **fails** (locked-mode mismatch) | ✅ PASS (restore-proof §b — `NU1004`, exit 1; reverted) |
| **C** Silent substitution → error | US2 / SC-003 | pin below the feed, `dotnet restore … --force-evaluate` | substitution **fails** as an error, not a warning | ✅ PASS (restore-proof §c — `error NU1601: Warning As Error`; props alone; reverted) |
| **D** Fresh-clone local not blocked | US3 / SC-004 | `dotnet build FS.GG.Rendering.slnx -c Debug` (CI unset) | builds; locked mode OFF locally | ✅ PASS (restore-proof §T013a — Build succeeded, exit 0) |
| **E** One-command regenerate | US3 / SC-005 | `dotnet restore … --force-evaluate` + `git status` | changed lockfiles appear as a reviewable diff | ✅ PASS (restore-proof §T013b — `AM`/`M` lockfile diff; reverted) |
| **F** Scope boundary holds | FR-006 / SC-006 | `dotnet test tests/Build.Tests` + lockfile inventory | RestoreLock case green; LOCKED set only, no excluded lane locked | ✅ PASS (Build.Tests 10/10; 38 lockfiles, 0 in excluded lanes) |
| **G** No regression to gate/release lanes | FR-009 / SC-006 | build + `refresh-surface-baselines.fsx` + `validate-version-coherence.fsx` + baseline re-run | all pass exactly as before | ✅ PASS (surface: no drift; coherence: COHERENT; baseline: identical 21/21 green) |

## Scope inventory (F)

LOCKED set = 38 committed `packages.lock.json` = exact `FS.GG.Rendering.slnx` membership (18 `src/` · 17
`tests/` · 2 in-tree `samples/` · 1 `tools/`). EXCLUDED (no lockfile, confirmed): `tests/Package.Tests`
(fsproj opt-out) and `samples/{AntShowcase,SampleApps,SecondAntShowcase,ControlsGallery}` (shadow root
`Directory.Build.props`, zero edits). The standalone samples and Package.Tests still build/test green
(baseline re-run unchanged), proving the policy never reached them.

## Verdict

All seven scenarios pass against real restore. Locked, reproducible CI restore (US1), silent-drift
fail-closed (US2), and frictionless local + one-command regenerate (US3) are all delivered with no
regression to the existing gate/release/sample lanes.
