# Internal Contracts — Harness Data-Table Refactor

The harness exposes no `FS.GG.UI.*` package surface, but it has compiler-enforced internal `.fsi`
contracts (consumed by `Cli.fs` + the test projects) and an observable CLI contract. These are the
contracts this refactor must preserve or evolve deliberately.

## C-1 — Descriptor SSOT contract (`FeatureCatalog`)

- **Producer**: `FeatureCatalog.catalog : FeatureDescriptor list` (12 rows at HEAD).
- **Consumers (after refactor)**: `Compositor.Config/FeatureState/Render`, `Cli.runReadiness`, and
  the harness/package test projects.
- **Guarantees**:
  - Every per-feature constant the harness needs (slug, aliases, directories, required headers,
    policy/profile/scenario config, emitted variants) is reachable from a single descriptor row
    (FR-001). No standalone per-feature directory/header constant remains (FR-002, SC-003).
  - `CliAliases` are unique across the catalog (FR-011) — duplicate → build/first-use failure.
  - `FeatureDescriptor.readinessDirectory`/`variantDirectory`/`*Path` reproduce the **exact** prior
    path byte-strings (these are CI-grepped — byte-identity required, FR-008, edge "Directory-path drift").

## C-2 — Generic renderer contract (`Compositor.Render`)

- **Signature (shape)**: `renderFeature : FeatureDescriptor -> ReportVariant -> <report>`, dispatching
  over `d.Variants`; divergent variants resolved via `d.Renderers` (`FeatureRenderHooks`).
- **Guarantees**:
  - A descriptor with variants `{ValidationSummary; Timing; Parity}` produces exactly those three
    reports, semantically identical to the prior per-function output (FR-003, US2-AS1).
  - A genuinely divergent feature output is supplied via an explicit `Renderers` hook, **not** a
    separate top-level `renderFeatureNNN…` function (US2-AS2). Zero `renderFeature*` top-level
    functions remain (SC-003).
  - `renderPackageValidation`/`renderRegressionValidation` dispatch by descriptor `Id` lookup, not
    `match featureNum` (FR-004, US2-AS3).
  - A `Variant` with no template and no hook fails loud (edge "Variant declared but no template path").
- **Semantic-equivalence bar (FR-008)**: identical status, counts, required headers, meaningful entry
  ordering. Harmless wording/ordering may differ; CI-grepped literal paths/headers are byte-identical.

## C-3 — CLI behavior contract (`Cli.runReadiness` + command table)

- **Guarantees (observable, FR-007 / SC-006)**:
  - Invoking a feature by `id`, `feature<N>`, or `slug` alias dispatches through `runReadiness` and
    produces the **same** artifacts, paths, and exit code as the prior dedicated handler (US3-AS1).
  - An unknown alias reports the error and exit code exactly as before — no silent success, no changed
    code (US3-AS2, edge "CLI alias collision" / "Feature not in the catalog").
  - A feature added as a descriptor row gets its CLI command with **no** new per-feature handler
    function (US3-AS3, SC-002).
  - Run-id strings may differ only where they already embed a wall-clock timestamp (FR-007).

## C-4 — Lane-stage contract (`ValidationLanes`)

- **Guarantees (FR-006)**:
  - A normal lane's `LaneResult` (status, captured output, timing) matches the pre-refactor result
    (US4-AS1).
  - A lane exceeding its timeout is terminated and reported as a timeout exactly as before, with the
    timeout logic isolated in `TimeoutManager` (US4-AS2). `TimedOut` vs `NoProgressTimedOut` preserved.
  - `LaneResult` shape and `runLanes`/`runLane` caller contract are unchanged (decomposition is internal).

## C-5 — Internal `.fsi` contract (Constitution II)

- Each split module (`Compositor.Types/Config/FeatureState/Render`, lane units) carries a curated
  `.fsi`; no access modifiers in `.fs`.
- `Compositor.fsi` is rewritten: removes the 85 `renderFeature*` + 110 directory/`Id` vals; exposes
  the parametric renderer + descriptor-driven surface. Test call-sites referencing removed vals are
  retargeted to the descriptor equivalents (FR-010), not weakened/deleted.
- The harness is **not** in `readiness/surface-baselines/` (package-only) — no baseline regen,
  ledger entry, or version bump (R-1).

## Verification (cross-contract)

- Pre-refactor artifact baseline + per-story semantic diff (status/counts/headers/ordering) — FR-008.
- Byte-identity check on the fixed CI-grepped path/header literal set — FR-008.
- Full Release `*.Tests.fsproj` sweep ends at the baseline red/green set (SC-004); known pre-existing
  reds recorded as not-regression.
