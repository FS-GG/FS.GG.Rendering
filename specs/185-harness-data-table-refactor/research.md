# Phase 0 Research — Harness Data-Table Refactor

Confirms the spec's premises against the tree at HEAD (re-confirmed 2026-06-22) and records the
parametrization decisions. All `NEEDS CLARIFICATION` are resolved here.

## R-1 — The harness DOES have curated `.fsi` files (spec/reality discrepancy)

- **Finding**: The spec, FR-009, and Assumptions repeatedly state the harness has "no `.fsi` public
  surface." Reality: **every** public harness module has a `.fsi` (17 of them), and
  `Compositor.fsi` is 44 KB exposing **387 vals**, including all **85** `renderFeature*` vals, the
  **110** `*ReadinessDirectory`/path constants, and the `feature###Id` constants. The test projects
  call these directly: `Compositor.renderFeature…`, `Compositor.feature156Id`,
  `Compositor.WriteFeature`, `Compositor.ArtifactPublished`, `Compositor.HostProfile`,
  `Compositor.evaluateTier`, etc. (grep of `tests/Rendering.Harness.Tests/`).
- **Decision**: Reconcile the two halves of the claim:
  1. *True half* — the harness is **not** in the package surface-baseline. `readiness/surface-baselines/`
     covers `FS.GG.UI.*` packages only; the harness is a build-time tool with no package consumers.
     So **no surface-baseline regen, no CompatibilityLedger entry, no version bump** (Assumptions hold).
  2. *False half* — there **is** a compiler-enforced internal `.fsi` per module. Per Constitution
     Principle II, each split module (`Compositor.Types/Config/FeatureState/Render`) gets its own
     curated `.fsi`, and `Compositor.fsi` is rewritten (it shrinks: the 85 `renderFeature*` + 110
     directory vals are deleted and replaced by the parametric renderer + descriptor lookups).
- **Consequence**: Deleting the `renderFeature*`/`feature###*Directory`/`feature###Id` vals is a
  real **internal-surface deletion** that breaks the test call-sites above. Per **FR-010** those
  tests are *retargeted* to the descriptor-driven equivalents (e.g. `Compositor.renderFeature` over a
  descriptor + variant; `FeatureCatalog.FeatureDescriptor.readinessDirectory d`), never weakened or
  deleted. This raises US2/US3 scope and risk above the spec's "no surface" framing. Captured in the
  plan's Constitution Check and Technical Context.
- **Alternatives considered**: (a) Keep all 387 vals as thin forwarders to the descriptor — rejected:
  defeats SC-003 (constants must drop to 0) and SC-001 (`.fsi` stays huge). (b) Keep `feature###Id`
  string constants as a compatibility shim — rejected: feature 184 just *removed* backcompat shims;
  re-introducing one contradicts the campaign. Tests move to `string descriptor.Id` / alias lookup.

## R-2 — `FeatureCatalog` is more complete than the spec implies

- **Finding**: `FeatureCatalog.fs` already defines `ReportVariant` (11 cases), `FeatureConfig`
  (`PolicyId`/`AcceptedProfileId`/`RequiredScenarioIds`), `FeatureDescriptor`
  (`Id`/`Slug`/`CliAliases`/`Variants`/`RequiredHeaders`/`Config`), the 12-row `catalog`, and the
  `FeatureDescriptor` helper module (`readinessDirectory`, `variantDirectory`, the four `*Path`
  helpers, `supports`, `tryByAlias`). It is consumed by `Cli.fs` today. The `.fsi` comments already
  reference FR-007/FR-008/SC-005/C-FD-2 — the table was seeded in feature 181.
- **Decision**: US1's remaining work is therefore narrower than "build the table":
  1. `RequiredHeaders` exists on the record but is **`[]` for all 12 rows** — populate it from the
     header literals currently inline in `Compositor.fs`.
  2. Add a render-hooks field (`FeatureRenderHooks`, R-4) to the descriptor.
  3. Repoint `Compositor.fs`'s 110 directory constants + duplicated header/profile literals at the
     existing `FeatureDescriptor.*` helpers; delete the standalone `let`s (SC-003).
- **Rationale**: Extend the seeded table (spec FR-001 "rather than introducing a parallel structure"),
  do not rebuild it. `variantDirectory` already encodes the directory-per-variant mapping that the
  110 constants spell out feature-by-feature, so the repoint is largely a substitution.

## R-3 — Counts re-confirmed at HEAD

- `Compositor.fs`: **5,512 lines**; `grep -cE '^\s*let\s+renderFeature'` → **85** functions (spec
  says 88, parent report says 86 — use **85**, the measured value); **110** `*ReadinessDirectory`
  references; `Compositor.fsi` **387** vals.
- `Cli.fs`: **3,928 lines**; per-feature handlers are `runFeature{156,158,160,161}PerformanceCmd`,
  `runFeature{156,157,158,159,160,161}ReadinessCmd`, `runFeature159PromotionCmd` plus dispatch
  fan-out (report's "26 commands" counts dispatch arms too).
- `ValidationLanes.fs`: **1,376 lines**; `runLane` at **line 1063** (~154 lines); the file already
  carries an MVU model (`StartProcess`/`PollProcess`/`StopProcess` at line 160+) — the decomposition
  reuses that boundary rather than inventing one.
- **Decision**: Tasks reference measured counts; SC-003's "88→0 / 110→0" is read as "all
  `renderFeature*` top-level functions → 0" and "all standalone `*ReadinessDirectory` constants → 0"
  (the spec's 88 was an estimate; the *goal* is zero, which is count-independent).

## R-4 — Parametric renderer + override hooks design (US2)

- **Decision**: One generic `renderFeature : FeatureDescriptor -> ReportVariant -> …` that dispatches
  over the descriptor's `Variants`. The genuinely divergent ~20% (e.g. 159 promotion, 160 throughput,
  161 lane-ledger — already flagged "stay explicit" in `FeatureCatalog.fsi`) are supplied via a
  `FeatureRenderHooks` record of `option` function fields on the descriptor; the generic path runs
  the template, an override replaces it. The two feature-number-dispatch renderers
  (`renderPackageValidation`/`renderRegressionValidation`) become `FeatureDescriptor.tryById`-keyed
  lookups (Pattern A).
- **Rationale**: Records-of-functions are plain F# (Principle III); `option` hooks make "template vs
  override" explicit and compiler-checked. Avoids SRTP/inline. A variant listed with no
  template-or-hook must fail loud (edge case: "Variant declared but no template path").
- **Hazard mitigation (Principle III, report §8)**: a renderer generic over `'msg` can fight type
  inference. Keep the renderer concrete over `ReportVariant` + the harness's existing record types;
  add explicit annotations rather than reaching for SRTP. If inference still forces an inline/SRTP
  construct, that is a Principle-III justification to record before merging.
- **Alternatives considered**: a `Map<ReportVariant, render fn>` per descriptor — rejected: the
  `Variants` set + a shared template covers the common 80%; per-descriptor maps re-introduce
  per-feature data volume the refactor is removing.

## R-5 — `runReadiness` workflow (US3)

- **Decision**: One `runReadiness (d: FeatureDescriptor)` pipeline: `probe → mkdirs (from
  FeatureDescriptor.* paths) → build reports → write the N variant files (d.Variants) → render`.
  A command table maps each `d.CliAliases` entry → `runReadiness d`. Unknown alias → the *same*
  error/exit code as today (FR-007, edge case); duplicate alias across descriptors → build/first-use
  failure (FR-011).
- **Rationale**: The alias table already exists (`FeatureDescriptor.tryByAlias`); the workflow reads
  the same descriptor the renderer does, so CLI and renderer can't drift. Argument parsing, exit
  codes, and run-id formatting are preserved verbatim except where a run-id already embeds a
  wall-clock timestamp (FR-007).

## R-6 — `runLane` decomposition (US4)

- **Decision**: Extract three single-responsibility units behind a thin orchestrator, reusing the
  file's existing MVU edge:
  - `ProcessRunner` — build `ProcessStartInfo`, spawn, expose stdout/stderr streams + exit.
  - `TimeoutManager` — wall-clock + no-progress timeout → terminate decision (the `TimedOut` /
    `NoProgressTimedOut` distinction already in `LaneStatus`).
  - `OutputBuffer` — accumulate captured output (the `AppendLaneLog` concern).
  - `orchestrator` — compose the three into a `LaneResult`, identical to today's.
- **Rationale**: Localizes failures and makes timeout logic unit-testable in isolation (FR-006);
  keeps I/O at the interpreter edge (Principle IV). `LaneResult` shape is unchanged so `runLanes`
  (line 1367) and all callers are untouched.

## R-7 — Verification strategy (the "live run" equivalent)

- **Decision**: Because this is a behavior-preserving refactor, the verification spine is a
  **pre-refactor artifact baseline** captured in the Foundational phase, then a **semantic diff**
  (parsed status/counts/required-headers/meaningful-ordering) after each user story, plus a
  **byte-identity check** on the fixed CI-grepped path/header literals (FR-008). The full Release
  `*.Tests.fsproj` sweep must end at the same red/green set as baseline (SC-004); known pre-existing
  reds (stale-feed `Package.Tests`/`ControlsGallery` from 182/183) are recorded as
  baseline-not-regression, not masked.
- **Rationale**: Substitutes for the plan template's "early live smoke run" — a deterministic test
  pass can stay green while artifacts silently drift, so the artifact baseline is the real guard.
