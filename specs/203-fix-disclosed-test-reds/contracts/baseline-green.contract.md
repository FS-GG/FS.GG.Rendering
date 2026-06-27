# Contract: Deterministic green comprehensive baseline

This feature exposes **no new public API** (Tier 2). Its observable contract is the **behavior of the
comprehensive baseline and the four affected gates** — what a maintainer can run and the outcome they
must see. This is the externally-checkable interface this feature delivers.

## C1 — Comprehensive baseline is green and deterministic

- **Command**: `dotnet fsi scripts/baseline-tests.fsx`
- **Guarantee**: **0 red projects**. Every previously-disclosed red is resolved; any irreducible
  environment residue appears only as **explicit skips with written rationale**, and the skip count is
  reported.
- **Determinism**: repeated runs in the same environment yield the **same** per-project pass/fail/skip
  set (no project flips between runs).
- **Maps to**: FR-006, SC-001, SC-005.

## C2 — Package-feed pin coherence (all samples)

- **Command**: `dotnet test tests/Package.Tests` filtered to *Feature163*; and each sample's own pin
  check.
- **Guarantee**: for **every** sample (AntShowcase, SampleApps, SecondAntShowcase, ControlsGallery) and
  every `FS.GG.UI.*` pin, `pin.version == sourceVersion[packageId]`. No "pin does not match
  source-controlled version" failure remains. The gate covers all four samples (not only AntShowcase).
- **Maps to**: FR-001, US1, SC-002 (the Feature163 portion).

## C3 — Feature128 design-system gate self-provisions

- **Command**: `dotnet test tests/Package.Tests` filtered to *Feature128* — run **from a clean checkout
  with no pre-existing readiness report**.
- **Guarantee**: GV-1..GV-7 all pass; the ANT record reports `overall=PASS`; the gate is **not red by
  default** because the report is absent — the verdict-core report is produced by the gate's own setup.
  The heavy live scaffold+build remains opt-in via `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1`.
- **Maps to**: FR-002, US2, SC-002 (the Feature128 portion).

## C4 — Sample suites green with assertions intact

- **Command**: `dotnet test` for `AntShowcase.Tests`, `ControlsGallery.Tests`,
  `SecondAntShowcase.Tests`.
- **Guarantee**: 100% pass. Each previously-drifted assertion is set to its **true current value** and
  still verifies a real property (count + bijection/`Set.equal` + contract-coverage all intact). No
  assertion weakened (`=` not relaxed to `>`/`>=`), broadened, or deleted.
- **Maps to**: FR-003, US3, SC-003.

## C5 — SkiaViewer GL determinism

- **Command**: `dotnet test tests/SkiaViewer.Tests` run **5 consecutive times**.
- **Guarantee**: identical pass set every run (0 flaky outcomes). Each GL/window-system-sensitive test
  either passes (capability present) or is **deterministically skipped with a Constitution-VI
  rationale** (capability absent) — never an intermittent failure. A genuine GL defect inside an
  available context still fails loudly (not masked as "unsupported").
- **Maps to**: FR-004, FR-005, US4, SC-004, SC-005.

## C6 — No regression; feature-202 disclosures obsolete

- **Guarantee**: no previously-passing test regresses; public-surface, governance, and evidence gates
  stay green; the feature-202 readiness ledger's pre-existing-red and flaky disclosures no longer apply
  (conditions resolved or reduced to bounded explicit skips).
- **Maps to**: FR-007, FR-008, SC-006.
