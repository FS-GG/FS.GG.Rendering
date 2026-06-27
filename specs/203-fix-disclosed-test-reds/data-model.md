# Phase 1 — Data Model

This feature is test-debt cleanup; its "entities" are the artifacts and signals the fix manipulates,
drawn from the spec's Key Entities. No persistent storage schema — these are repository facts and
test-observable values.

## Entity: Comprehensive baseline (signal)

The full red/green set produced by `scripts/baseline-tests.fsx` across every test project (solution +
`tests/Package.Tests` + `samples/**/*.Tests` + `tests/SkiaViewer.Tests`).

- **Fields**: project name; pass/fail/skip counts; per-project status.
- **States**: `red` (≥1 failure) → `green` (0 failures, residue only as explicit skips).
- **Validation (target)**: 0 red projects (SC-001); any residue is an explicit skip with stated count
  (SC-005). Deterministic across repeated runs (SC-004).
- **Source of truth**: the runner itself; not modified by this feature.

## Entity: Sample package pin

A sample project's `<PackageReference Include="FS.GG.UI.*" Version="…">`.

- **Fields**: sample project path; package id; pinned version.
- **Relationship**: MUST equal the **source-controlled version** = `src/**/<pkg>.fsproj` `<Version>`
  (the oracle the *Feature163* gate reads). MUST exist in the local feed `~/.local/share/nuget-local/`.
- **Validation**: `pin.version == sourceVersion[pin.packageId]` for every pin in every sample
  (FR-001); enforced by the extended *Feature163* gate + per-sample pin checks.
- **State transition**: `stale` → `current` via `package-feed --mode refresh --pack` (forward-only;
  never roll the source version back — edge case).

## Entity: Design-system validation report

The generated readiness artifact the *Feature128* gate audits:
`specs/128-design-system-template-param/readiness/design-system-template-validation.md`.

- **Fields (gate-relevant tokens)**: `covered-values:` (= template designSystem choices, e.g. wcag,
  ant); per-choice `<value>: build=pass`; WCAG `diff-vs-today=none` / `overall=FAIL` /
  `authority=WcagCertified`; ANT `record=ant` / `overall=PASS` / `authority=AntExpectation`;
  `divergent-pairing:` line; `no-overclaim-note:` line; `result: pass`.
- **Lifecycle**: transient (path is gitignored). MUST be present-and-current whenever the gate runs.
- **Validation**: GV-1..GV-7 pass; `overall=PASS` for the ANT record (FR-002).
- **State transition**: `absent` → `present(current)` produced by the verdict-core path as gate
  self-provisioning (R2), not by a manual contributor step.

## Entity: Drifted assertion

A sample test assertion whose expected literal no longer matches current reality (catalog 96→97).

- **Fields**: file:line; asserted expression; expected literal; true current value; kind
  (count | bijection/coverage | contract-classification).
- **Instances (confirmed by early baseline run)**:
  - `AntShowcase.Tests/CoverageTests.fs:22` — count `96` → true value.
  - `SecondAntShowcase.Tests/CoverageTests.fs:22` — count `96` → true value; plus
    `Unreferenced`/`MissingContractOrReason` emptiness requiring the 97th control placed + classified.
  - `ControlsGallery.Tests/CoverageTests.fs:20,22,23` — `52` subset covenant; true value per intent
    (R3).
- **Validation**: corrected to the **true** value; the assertion still verifies a real property
  (bijection / `Set.equal` / contract coverage) — not weakened, broadened, or deleted (FR-003,
  Constitution V).

## Entity: Flaky GL test

A `SkiaViewer.Tests` case whose pass/fail varies run-to-run in one environment.

- **Fields**: file:test-name; required capability (offscreen raster surface | native window+GL
  context); current handling (unguarded branch | direct `SKSurface.Create`).
- **Candidate set (confirmed by early baseline run)**: `Tests.fs` "runApp"/"persistent run" live cases;
  `Feature063/086/136/140` screenshot/raster cases.
- **States**: `flaky` (intermittent red) → `deterministic` = `pass` (capability present) **xor**
  `skipped-with-rationale` (capability absent, Constitution VI).
- **Validation**: identical pass set across 5 consecutive runs (SC-004); skip count stated (SC-005);
  no genuine defect masked as "unsupported" (Constitution VI).
- **Idiom**: `rasterAvailable` probe + `skiptest`/`tierSkip` per `Audit_ReplayCache.fs`.
