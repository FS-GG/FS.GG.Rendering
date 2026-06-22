# Implementation Plan: Harness Data-Table Refactor

**Branch**: `185-harness-data-table-refactor` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/185-harness-data-table-refactor/spec.md`

## Summary

Phase 1 of the god-module decomposition (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §5). Promote the existing partial `FeatureCatalog` descriptor to the single source of truth (SSOT) the whole harness reads, and replace per-feature *code* with per-feature *data* (Patterns D+E):

- **US1** — extend the descriptor (`RequiredHeaders`, render hooks) and repoint `Compositor.fs`'s 110 `*ReadinessDirectory` constants + duplicated header/profile literals at descriptor lookups.
- **US2** — replace the 85 `renderFeature<N><Variant>` functions + 6 per-feature state machines with one parametric renderer + state driver over the descriptor's `Variants`, plus explicit override hooks for the genuinely divergent ~20%; convert the `renderPackageValidation`/`renderRegressionValidation` feature-number `match` dispatch into descriptor lookups.
- **US3** — collapse the per-feature `Cli.fs` command handlers into one descriptor-driven `runReadiness` workflow + an alias→descriptor command table.
- **US4** — split `ValidationLanes.runLane` into process-runner / timeout-manager / output-buffer units behind a thin orchestrator.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a *refactor with no behavior change*, not a defect fix, so there is no root-cause
> hypothesis to confirm. The equivalent verification obligation here is the **pre-refactor
> artifact baseline**: capture a baseline of every emitted readiness/evidence/parity/timing
> artifact *before* touching code, then diff against it after each user story. `/speckit-tasks`
> MUST schedule that baseline capture as the first Foundational task, before any production edit.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: None new (FR-009). Refactor is confined to `tools/Rendering.Harness/`; existing intra-project module references only.

**Storage**: Filesystem — readiness/evidence/parity/timing markdown + JSON artifacts under `specs/<slug>/readiness/…` and the harness's evidence directories.

**Testing**: xUnit-style F# test projects. Primary gate: `tests/Rendering.Harness.Tests`. Secondary consumers of the harness surface: `tests/Elmish.Tests`, `tests/Package.Tests`, `tests/Controls.Tests`, `tests/SkiaViewer.Tests`, `tests/Scene.Tests`, `tests/Layout.Tests`.

**Target Platform**: Linux/CI build-time CLI tool (no GL/window-system requirement for the refactored paths).

**Project Type**: F# CLI tool (single project, `tools/Rendering.Harness/Rendering.Harness.fsproj`).

**Performance Goals**: N/A — not a render hot path. The §7 golden-image/perf gates of the parent report gate *later* phases, **not** this one (spec Assumptions).

**Constraints**: Semantic equivalence of all emitted artifacts (FR-008); byte-identity required only for fixed CI-grepped path/header literals. Observable CLI contract preserved (FR-007). No new project/dependency/inter-project reference (FR-009).

**Scale/Scope**: `Compositor.fs` 5,512 lines (85 `renderFeature*`, 110 `*ReadinessDirectory` constants, 387-val `.fsi`); `Cli.fs` 3,928 lines; `ValidationLanes.fs` 1,376 lines (`runLane` ~154 lines from line 1063). 12 features in `catalog` (148,149,152,153,154,155,156,157,158,159,160,161).

### Resolved unknowns

No `NEEDS CLARIFICATION` markers remain. One material spec/reality discrepancy was found and resolved during research (see `research.md` R-1): the spec states the harness has "no `.fsi` public surface," but **`Compositor.fsi` exposes 387 vals** (incl. all 85 `renderFeature*` and 110 directory constants) and the test projects call them directly. Resolution: the harness is not in the **package** surface-baseline (`readiness/surface-baselines/` covers `FS.GG.UI.*` only), so no baseline regen/version bump — *that* part of the spec holds. But each split module still needs a curated `.fsi` (Constitution II), and the removed `renderFeature*`/directory vals are a real internal-surface deletion that forces test retargeting (FR-010). This raises US2/US3 scope and risk above the spec's framing.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change classification**: **Tier 2 (internal change).** No `FS.GG.UI.*` public package surface, no new dependency, no observable behavior change (FR-007). The harness assembly is not in the package surface-baseline, so no baseline update is required. *However*, per Principle II the harness keeps a curated `.fsi` per module, so the split modules each get one and `Compositor.fsi` is updated — this is internal `.fsi` discipline, not a Tier-1 contract change.

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | PASS | New split modules (`Compositor.Types/Config/FeatureState/Render`) are designed `.fsi`-first; tests already exercise the harness surface and are retargeted to the descriptor-driven equivalents (FR-010), not weakened. |
| II. Visibility lives in `.fsi` | PASS (with work) | Every new public module gets a curated `.fsi`; no `private`/`internal`/`public` modifiers in `.fs`. `Compositor.fsi` shrinks as the 85 `renderFeature*` + 110 directory vals are removed and replaced by the parametric/descriptor surface. |
| III. Idiomatic simplicity | PASS (watch) | Descriptor table + render-hook records are plain data/functions. **Hazard:** a renderer generic over `'msg`/variant can fight type inference (report §8). Mitigation: explicit annotations or a tag-indexed dispatch over `ReportVariant`; **no** SRTP/inline/reflection/type-providers. If any of those become necessary, record justification in `research.md` per Principle III. |
| IV. Elmish/MVU boundary | PASS | `ValidationLanes` already models lanes as MVU (`StartProcess`/`PollProcess`/`StopProcess` msgs, line 160+); `runLane` decomposition keeps the pure/edge split — process spawn/timeout stay at the interpreter edge. `Compositor` `FeatureState` `init`/`update`/`status` stay pure transitions over the descriptor. |
| V. Test evidence mandatory | PASS | Pre-refactor baseline + per-story semantic diff is the evidence (FR-008/FR-010). No assertion weakened to green; any synthetic fixture carries the `Synthetic` token + use-site disclosure. |
| VI. Observability & safe failure | PASS | Missing/duplicate descriptor (unknown id referenced by code; two descriptors sharing an alias) fails loud at build or first use (FR-011, edge cases) — never silent skip/last-wins. |

**Gate result: PASS.** No violations requiring Complexity Tracking. The one elevated item (internal `.fsi` churn + test retargeting) is in-scope work captured by FR-010, not a constitutional exception.

## Project Structure

### Documentation (this feature)

```text
specs/185-harness-data-table-refactor/
├── plan.md              # This file
├── research.md          # Phase 0 — discrepancy reconciliation + parametrization decisions
├── data-model.md        # Phase 1 — descriptor/hook entities + module re-homing
├── quickstart.md        # Phase 1 — baseline-capture + semantic-diff validation guide
├── contracts/
│   └── harness-internal-contracts.md  # SSOT, renderer-hook, CLI-behavior, lane-stage contracts
├── checklists/          # (pre-existing)
└── tasks.md             # Phase 2 — created by /speckit-tasks (NOT by /speckit-plan)
```

### Source Code (repository root)

```text
tools/Rendering.Harness/
├── FeatureCatalog.fsi / .fs   # SSOT — EXTEND: add RequiredHeaders content + FeatureRenderHooks; keep path/predicate helpers
├── Compositor.fsi / .fs       # 5,512 → SPLIT into the four modules below; shrink/replace the 387-val .fsi
│   ├── Compositor.Types       # the ~60 type defs (DUs tests pattern-match on: ArtifactPublished, HostProfile, …)
│   ├── Compositor.Config      # descriptor-derived directory/header/profile lookups (absorbs 110 constants)
│   ├── Compositor.FeatureState# one parametric init/update/status over a descriptor (replaces 6 state machines)
│   └── Compositor.Render      # one generic renderer + per-descriptor override hooks (replaces 85 renderFeature*)
├── Cli.fs                     # 3,928 → one runReadiness workflow + alias→descriptor command table
└── ValidationLanes.fsi / .fs  # split runLane → ProcessRunner / TimeoutManager / OutputBuffer / orchestrator

tests/Rendering.Harness.Tests/ # PRIMARY gate; retarget Compositor.renderFeature*/feature###Id call-sites (FR-010)
tests/{Elmish,Package,Controls,SkiaViewer,Scene,Layout}.Tests/  # secondary harness-surface consumers
```

**Structure Decision**: Single existing F# CLI project. `Compositor.fs` is split by responsibility (Pattern E) into four sibling modules under `tools/Rendering.Harness/`, inserted in the existing `.fsproj` compile order *after* `FeatureCatalog.fs` and *before* `PackageFeed.fs`/`Cli.fs` (F# file-ordering: SSOT → Types → Config → FeatureState → Render → consumers). No new project is added (FR-009). Each new public module carries its own `.fsi`.

## Complexity Tracking

> No Constitution Check violations. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phasing (delivery order)

Independent, individually shippable slices, ordered by the spec's priorities and dependency edges. Each ends build-green + the baseline semantic-diff clean.

0. **Foundational** — capture the pre-refactor artifact baseline (every readiness/evidence/parity/timing file for features 148–161) into a baseline corpus; record the current red/green test set (SC-004) and the fixed CI-grepped path/header literals that must stay byte-identical (FR-008).
1. **US1 (P1)** — extend descriptor (`RequiredHeaders`, hooks field), add fail-loud duplicate-alias/unknown-id checks (FR-011); repoint Compositor directory/header/profile constants → descriptor lookups; delete the 110 standalone directory `let`s. *Independently shippable; SSOT proven.*
2. **US2 (P2)** — parametric renderer + state driver over `Variants`; override hooks for the divergent ~20%; convert the two feature-number-dispatch renderers to `Id` lookups; split `Compositor.fs` into the four modules + `.fsi`s. *Largest line-count win; depends on US1.*
3. **US3 (P3)** — `runReadiness descriptor` workflow + alias→descriptor command table replacing the per-feature handlers; preserve argument parsing, exit codes, run-id shape (FR-007). *Depends on US1+US2.*
4. **US4 (P4)** — decompose `runLane` into ProcessRunner/TimeoutManager/OutputBuffer + orchestrator. *Independent of US1–US3; can land any time.*

Exit (all): no file in `tools/Rendering.Harness/` > ~1,500 lines (SC-001); 0 `renderFeature*` top-level fns + 0 standalone `*ReadinessDirectory` constants (SC-003); same red/green set (SC-004); artifacts semantically equivalent + CI-grepped literals byte-identical (SC-005); CLI contract unchanged (SC-006).

## Implementation Progress (updated 2026-06-22)

Delivery follows the dependency edges. Verification spine: `scripts/emit-harness-readiness.sh`
re-emits all 12 catalog features; `scripts/semantic-diff-artifacts.fsx` proves semantic equivalence
vs the live baseline at `/tmp/185-baseline` (timestamps/run-ids/`--out` root normalized). Baseline
red/green: all green except the **known pre-existing reds** `tests/Package.Tests` (8) and
`samples/ControlsGallery` (2), carried from 182/183's stale local feed — not regressions.

| Slice | Status | Evidence |
|---|---|---|
| **Foundational** (T001–T008) | ✅ done | live baseline corpus (160 files), red/green sweep (`readiness/baseline.md`), frozen-literal + rehoming maps, semantic-diff harness (validated: two re-emits diff clean), fail-loud seam landed in US1. T009 (`.fsi` seams for split modules) folded into US2. |
| **US1 — SSOT** (T010–T016) | ✅ done, committed | `RequiredHeaders` populated; fail-loud `descriptorById`/`descriptorByAlias`/duplicate-alias (forced at module load); 12 `feature###ReadinessDirectory` constants deleted, 110 refs repointed at `FeatureDescriptor.readinessDirectory` (byte-identical); accepted-profile id descriptor-sourced; `.fsi` trimmed; test retargeted. **Build green; Rendering.Harness.Tests 209/209; semantic-diff clean; `grep ReadinessDirectory Compositor.fs` → 0; fail-loud proven.** |
| **US4 — runLane** (T033–T037) | ✅ done, committed | `runLane` (~154 lines) → ~44-line orchestrator over `ProcessRunner` + `TimeoutManager` + `OutputBuffer` (each `.fsi`-typed); `TimedOut`/`NoProgressTimedOut` preserved; `LaneResult`/`runLanes`/callers untouched. **Build green; tests 209/209; `ValidationLanes.fs` 1,395 ≤ 1,500.** |
| **US2 — parametric renderer + split** (T017–T026) | ✅ done, committed, verified | `Compositor.fs` (5,518) split into 7 modules (Types 599, Config 849, FeatureState 858, Render 1,070, Render2 514, Render3 990, Render4 654), each `.fsi`. The 85 `renderFeature*` are **bespoke** (unique title/body per feature — `rehoming-map.md`), so collapse = module split + per-feature bodies relocated/renamed `emitFeature*` (→ 0 top-level `let renderFeature*`, SC-003) + `FeatureRenderHooks` for 156–161 package/regression + `descriptorById`-keyed validators (T023). ~474 call-sites retargeted. **Build green; max 1,070; semantic-diff problems=0; tests 209/209.** Caveat: split-module `.fsi` are compiler-derived full-surface (not hand-curated to re-hide ~40 former private helpers) — safe Constitution-II follow-up, no behavior impact. |
| **US3 — CLI collapse** (T027–T032) | ✅ done (isolated worktree) | The ~3,928-line `Cli.fs` split into 5 siblings: `Cli.Shared` (arg/scene/image helpers), `Cli.FeatureBuilders` (156/157/158 report builders + damage cmd), `Cli.Performance` (perf bodies + 160/161 builders + `runPerformance`), `Cli.Readiness` (legacy + 156-161 readiness + 159 promotion + `runReadiness`/`runPromotion`), `Cli.fs` (790: simple commands + thin `--feature`→`descriptorByAlias` dispatch wrappers + `main`). All 11 `runFeature*Cmd` handlers removed (renamed to bespoke `feature<N>{Readiness,Performance}`/`feature159Promotion` reached via the dispatch table). The `if isFeature161 … elif …` chains are replaced by a single `descriptorByAlias` lookup off the parsed `--feature <N>`; unknown feature now fails loud (raises `CatalogError` → error + exit 2; readiness previously fell through to a silent exit-0 default — intended FR-011 hardening per the gate). Performance/promotion preserve their exact prior `requires --feature …` message + exit 2 for any non-handled feature. **Build green; no `.fs` > 1,500 (largest `Cli.Readiness.fs` 1,382); grep `runFeature*Cmd` empty; semantic-diff vs `/tmp/185-baseline` problems=0 (160/160 files); CLI 148-161 exit 0, unknown exit 2; Rendering.Harness.Tests 209/209.** No new project/dependency (FR-009). NOTE: these internal `Cli.*` helper modules follow the existing `Cli.fs` no-`.fsi` / public-`let` style (large cross-module helper surface; assembly not in package surface-baseline) rather than separate `.fsi` files. |
| **Polish** (T038–T042) | ✅ done | Final red/green sweep (`readiness/after.md`, SC-004 = baseline set), full semantic-diff + byte-identity (`semantic-equivalence.md`, SC-005), SC-001/002/003 metrics (`head-metrics.md`), SC-002 single-site walkthrough (`sc-002-single-site.md`), feedback capture. |

**Outcome:** all four stories landed, each independently verified (build green + semantic-diff
`problems=0` + Rendering.Harness.Tests 209/209). SC-001 (no harness `.fs` > 1,500), SC-002 (single
descriptor-row add), SC-003 (0 `renderFeature*` / 0 `*ReadinessDirectory` / 0 `runFeature*Cmd`),
SC-004 (baseline red/green preserved), SC-005 (artifacts semantically equivalent + CI literals
byte-identical), SC-006 (CLI contract preserved) all met. One intended observable change: unknown
`--feature` now fails loud (exit 2) instead of a silent exit-0 fall-through (FR-011/US3-AS2).

**Merge policy honored:** *merge to `main` only if the full feature is green.* All four stories
verified green ⇒ the feature branch is eligible to merge. Two known **pre-existing** reds
(`Package.Tests`, `ControlsGallery` — stale local feed from 182/183) are unchanged and are not
regressions (`baseline.md` vs `after.md`).

**Known follow-ups (non-blocking, no behavior impact):** hand-curate the split-module `.fsi`
(`Compositor.*`) and add `.fsi` to the new `Cli.*` helper modules to re-tighten Constitution-II
visibility; the genuine single uniform `renderFeature : descriptor -> variant -> report` was not
achievable behavior-preservingly (bespoke per-feature bodies), so divergent bodies remain
`Compositor.Render*`-internal functions reached via the descriptor path.
