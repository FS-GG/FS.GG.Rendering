# Internal Contracts — Cross-Cutting Dedup + State Records

This phase exposes **no new external/public contract**. The "contracts" here are the invariants the
refactor must hold, since the public package surface is frozen (Tier-2, FR-009). They are what the
tests in `quickstart.md` assert.

## C-PUBLIC-SURFACE — the public surface is byte-identical

- The four affected `.fsi` files are **unchanged**:
  `src/Controls/RetainedRender.fsi`, `src/Controls.Elmish/ControlsElmish.fsi`,
  `src/Testing/TestingVisual.fsi`*, `src/Testing/TestingRetainedInspection.fsi`.
  - *`TestingVisual.fsi` MAY gain new **`internal`** declarations (the shared validation +
    managed-section helpers) inside a `module internal …`. `internal` symbols are **not** part of the
    public surface and do **not** appear in the surface baseline (verified: `module internal
    ReadinessFormatting` is absent from `readiness/surface-baselines/FS.GG.UI.Testing.txt`). No public
    `val`/`type` signature is added, changed, or removed.
- All surface baselines under `readiness/surface-baselines/` are byte-identical (empty diff).
- No package version is bumped (SC-006).
- **Verify**: `tests/Package.Tests` surface-area tests, `tests/Controls.Tests/PublicSurfaceTests.fs`,
  `Feature170RetainedInspectionSurfaceTests.fs`, and `git diff -- '**/*.fsi'` show no public change.

## C-METRICS-ONE-SITE — FrameMetrics built at exactly one site (FR-001/SC-001)

- The full 32-field `FrameMetrics` record is spelled out at **1** builder site; the former 2 full
  construction sites (`ControlsElmish.fs:1423–1460`, `1957–1990`) delegate to it.
- Adding one new metric field requires editing **only** the builder for it to appear on every
  frame-emit path (SC-007 — walkthrough).
- **Verify**: search confirms one full-field construction; metrics tests in `tests/Elmish.Tests` and
  `tests/Controls.Tests` pass with byte-identical emitted metrics.

## C-STEP-STATE — step/init over one named state record (FR-002/FR-003/SC-002)

- `RetainedRender.step` declares **0** loose frame-accumulator `let mutable` bindings for the 19
  accumulators now in `FrameState`; same fields, **same update order**, same final values.
- `RetainedRender.init` seeds its cold-start cache state onto the same record; cold-start seeding and
  first-frame output are byte-identical.
- **Verify**: `tests/Controls.Tests` retained-render + first-frame suites pass; rendered frames
  byte-identical; `grep -c 'let mutable' …step-region` shows 0 for the migrated accumulators.

## C-SCRIPT-STATE — runScriptCore metric carriers in one record (FR-004/SC-002)

- `runScriptCore` declares **0** loose metric-carrier mutables (the 7 carriers now in
  `FrameScriptState`); the record feeds the FrameMetricsBuilder.
- **Verify**: `tests/Elmish.Tests` metrics suites pass; emitted per-frame metrics byte-identical.

## C-VALIDATION-ONE-DEF — one inspection-validation routine (FR-005/SC-003)

- The validate-exceptions→compute-unused/invalid→diagnostics→status algorithm exists at **1**
  internal definition site; both public `validateCheck` functions delegate to it.
- **Severity asymmetry preserved**: retained admits `Warning` and derives `ReviewRequired`; visual
  admits neither — the parameterization neither widens nor narrows either family (Edge Cases).
- **Verify**: `tests/Testing.Tests` Feature165 (visual) + Feature170 (retained) validation suites
  pass with identical red/green; a `Warning`-severity retained finding is handled as before and the
  visual path still rejects/omits `Warning`.

## C-SECTION-ONE-DEF — one managed-section updater (FR-006/SC-003)

- The `(0,0)→append / (1,1)→replace / else→fail-loud` algorithm exists at **1** internal definition
  site; all three `updateManagedSection` writers delegate to it.
- **Fail-loud preserved**: duplicate/imbalanced markers report an error / refuse to write — never a
  silent last-wins overwrite (FR-011).
- **Verify**: `tests/Testing.Tests` managed-section suites pass; re-emitted readiness/inspection
  summary artifacts are byte-identical including the fail-loud branch.

## C-NO-BEHAVIOR-CHANGE — byte/semantic equivalence (FR-007/FR-008/SC-004/SC-005)

- Rendered frames and per-frame metrics are **byte-identical** to the pre-refactor baseline.
- Emitted readiness/evidence/inspection artifacts are byte-identical where the deduplicated logic was
  already identical; otherwise semantically equivalent.
- The red/green test set across `tests/Controls.Tests`, `tests/Elmish.Tests`, `tests/Testing.Tests`,
  `tests/Rendering.Harness.Tests` is **identical** to baseline; no assertion weakened.
- **Verify**: baseline-vs-after diff per `quickstart.md`.

## C-NO-NEW-DEP — no new project/dependency (FR-010)

- No new project, external dependency, or inter-project reference is added; all extraction stays in
  the existing `src/Controls`, `src/Controls.Elmish`, `src/Testing` modules.
- **Verify**: `git diff -- '**/*.fsproj' '**/*.slnx'` shows no new `<ProjectReference>` /
  `<PackageReference>` / `<Compile>` of a new project.
