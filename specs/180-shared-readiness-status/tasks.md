---
description: "Task list for Shared ReadinessStatus (Code-Health Refactoring Phase 3)"
---

# Tasks: Shared ReadinessStatus (Code-Health Refactoring Phase 3)

**Input**: Design documents from `/specs/180-shared-readiness-status/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: No new test tasks. This is a **behavior-preserving** refactor; the oracle is the
**existing** test suite plus a **byte-for-byte diff** of every serialized readiness/evidence artifact
against a baseline captured before any edit (FR-006, FR-008). Do **not** weaken any assertion to green a
build — if output must change, the consolidation forcing it is out of scope (preserve via a thin
per-domain projection instead).

> **Early live smoke run → resolved N/A.** The plan's Standing Assumption resolves the template's
> mandatory early-live-smoke clause as **N/A** for this feature: it carries no defect/root-cause
> hypothesis to confirm against a running app. The first Foundational task is **baseline capture**
> instead, and every story is gated on a byte-for-byte artifact diff + a full `dotnet test` showing no
> new failures relative to baseline.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Which user story the task serves (US1, US2, US3)
- All paths are repository-root relative.

## Path Conventions

- Shared home (additive surface + helpers): `src/Diagnostics/Diagnostics.fs` (+ `.fsi`)
- Consumer migration: `src/Testing/Testing.fs` (`.fsi` UNCHANGED)
- Surface baselines: `readiness/surface-baselines/`
- Feature evidence: `specs/180-shared-readiness-status/readiness/`

---

## Phase 1: Setup (Baseline & Snapshot)

**Purpose**: Capture the diff oracle before any edit. Byte-stability is the binding acceptance signal.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Use the discovery-based runner — it globs
> **every** `*.Tests.fsproj`, including the release-only `tests/Package.Tests` (public-surface gate) and
> the `samples/**/*.Tests` package-feed consumers that the solution omits. Hand-picking a subset is how
> Feature 175's surprises (stale surface baselines, stale sample pins, missing-report failures) hid.

- [X] T001 Create the feature evidence directory `specs/180-shared-readiness-status/readiness/` per plan
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/180-shared-readiness-status/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**/*.Tests` — and records the full red/green set)
- [X] T003 Snapshot the serialized readiness/evidence artifacts into `specs/180-shared-readiness-status/readiness/artifacts-baseline/` for per-story byte-diffing. The artifact set is the JSON + Markdown emitted by these specific modules (the same outputs the existing golden test assertions cover — the in-suite golden assertions are the *primary* oracle; this snapshot is the complementary byte-diff used by T014/T020/T026/T028):
  - `src/Testing/Testing.fs`: `VisualReadinessMarkdown` (~L1231), `VisualInspectionMarkdown` (~L1879), `RetainedInspectionMarkdown` (~L2579) — Markdown + their JSON emitters; and the `Feature159Readiness`/`Feature160ThroughputReadiness`/`Feature161HostLaneReadiness` modules (~L4194/4300/4418) — status + diagnostics + missing-artifact text.
  - `src/Diagnostics/Diagnostics.fs`: the `readinessStatusToken`-based JSON readiness report (~L458) and Markdown readiness report (~L497, ~L552).
  Capture each via the test/FSI surface that produces it (per `quickstart.md`); store one file per artifact so T028's diff is file-by-file. Do **not** narrow the set — completeness of this snapshot is what makes the byte oracle trustworthy.

---

## Phase 2: Foundational (Blocking Gate)

**Purpose**: Lock the evidence contract that every story depends on.

**⚠️ CRITICAL**: No user-story work begins until this gate is complete.

- [X] T004 In `specs/180-shared-readiness-status/readiness/baseline.md`, record the allowed pre-existing non-green lanes (`tests/Package.Tests`, `samples/ControlsGallery/ControlsGallery.Tests`) as **baseline-not-regression**; the bar is "no new failures vs. this capture," not "all green from zero"
- [X] T005 Document in `specs/180-shared-readiness-status/readiness/baseline.md` that the early-live-smoke clause is resolved **N/A** (behavior-preserving refactor, no root-cause hypothesis) and that the byte-for-byte artifact diff + full `dotnet test` is the standing evidence contract for every story
- [X] T006 Sanity-confirm the baseline reproduces: `dotnet build FS.GG.Rendering.slnx -c Release` clean, then `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` shows the same pass/fail set as `baseline.md`

**Checkpoint**: Baseline captured, allowed reds recorded, evidence contract locked — stories may begin (US1 → US2; US3 independent).

---

## Phase 3: User Story 1 - One shared readiness-status vocabulary (Priority: P1) 🎯 MVP

**Goal**: Introduce one canonical `ReadinessStatus` vocabulary in `FS.GG.UI.Diagnostics` with a single
`statusToken`, single `blocksAcceptance` default, and `tryParse`; migrate `ReadinessDiagnosticStatus`
and representative `Testing.fs` DUs onto it; delete the duplicated generic mappers — all serialized
status strings byte-identical. **Tier 1** (additive public surface).

**Independent Test**: Shared `ReadinessStatus` exists in Diagnostics; ≥1 representative per-domain DU is
migrated; the duplicate generic `statusText`/`blocksAcceptance` mapper bodies are deleted; solution
builds; full test suite matches baseline; every readiness report (JSON + Markdown) is byte-identical to
baseline; `FS.GG.UI.Diagnostics.txt` moved additively while `FS.GG.UI.Testing.txt` is unchanged.

- [X] T007 [US1] Draft the additive surface in `src/Diagnostics/Diagnostics.fsi`: `type ReadinessStatus` (12 cases per data-model.md) + `module ReadinessStatus` with `statusToken`, `blocksAcceptance`, `tryParse` (per contracts/readiness-status-surface.md) — seam before implementation
- [X] T008 [US1] Implement `ReadinessStatus` DU + module body in `src/Diagnostics/Diagnostics.fs`: `statusToken` total and byte-matching the existing per-domain token table (C-RS-1); `blocksAcceptance` canonical default (Accepted & EnvironmentLimited non-blocking); `tryParse` as inverse satisfying `tryParse (statusToken s) = Some s` (C-RS-3)
- [X] T009 [US1] Migrate `ReadinessDiagnosticStatus` in `src/Diagnostics/Diagnostics.fs` to reuse/alias the shared cases; route `readinessStatusToken` to `statusToken` (or thin wrapper) with identical output (`accepted`/`blocked`/`review-required`/`environment-limited`), preserving `review-required` as a domain projection; subsume `tryParseReadinessStatus` (C-RS-4)
- [X] T010 [US1] Migrate the representative per-domain readiness DUs in `src/Testing/Testing.fs` (`VisualReadinessStatus`, `RetainedInspectionStatus`, `CompositorReadinessStatus`) — keep public case names (Testing.fsi unchanged) and add a private `toShared : DomainStatus -> ReadinessStatus`; preserve genuinely domain-specific cases (`PendingReview`, `Skipped`, `SyntheticOnly`, `CompatibilityBlocked`, `FallbackGated`, `NonBeneficial`) with their existing literal strings (C-RS-5)
- [X] T011 [US1] Delete the duplicated generic `statusText`/`blocksAcceptance` mapper bodies in `src/Testing/Testing.fs` and `src/Diagnostics/Diagnostics.fs` that the shared `statusToken`/`blocksAcceptance` now replace (C-RS-2, SC-001)
- [X] T012 [US1] Add the one-line documented per-domain `blocksAcceptance` override where the rule diverges from default (Feature159: `EnvironmentLimited` blocks) in `src/Testing/Testing.fs` (C-RS-6)
- [X] T013 [US1] Regenerate the additive surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`; confirm via `git diff --stat readiness/surface-baselines/` that `FS.GG.UI.Diagnostics.txt` changed (additive) and `FS.GG.UI.Testing.txt` is UNCHANGED (C-RS-7)
- [X] T014 [US1] Story gate — `dotnet build FS.GG.Rendering.slnx -c Release` clean; `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` no new failures vs baseline (all readiness golden assertions green); byte-diff readiness artifacts vs the T003 snapshot (zero changes); `rg -n "statusToken|blocksAcceptance" src/Diagnostics src/Testing` confirms exactly one shared definition each (SC-001)

**Checkpoint**: US1 independently shippable — repo green relative to baseline, surface drift additive only.

---

## Phase 4: User Story 2 - One parameterized readiness validator (Priority: P2)

**Goal**: Replace `Feature159Readiness`, `Feature160ThroughputReadiness`, `Feature161HostLaneReadiness`
with one `validateReadiness` driven by a per-feature config record; delete the three original module
bodies. Each feature's status + diagnostics + missing-artifact output stays byte-identical. **Tier 2**
(internal). **Depends on US1** (uses the shared `ReadinessStatus` vocabulary).

**Independent Test**: One parameterized validator drives all three features via config records; the three
original `Feature*Readiness` module bodies are gone; solution builds; full test suite matches baseline;
the 159/160/161 readiness verdict + diagnostics + missing-artifact output is byte-identical to baseline.

- [X] T015 [US2] Define `ReadinessValidatorConfig` record + the single `validateReadiness` body in `src/Testing/Testing.fs` per contracts/validator-config.md (required scenarios, required artifacts, domain checks, unsupported facts, status derivation, blocks rule, result builder)
- [X] T016 [US2] Express `feature159Config` in `src/Testing/Testing.fs`: parity-passed, net-saved-work count, non-beneficial vs fallback-only decision, and the EnvironmentLimited-blocks override (C-VC-3)
- [X] T017 [US2] Express `feature160Config` in `src/Testing/Testing.fs`: `WarmupCount = 3`, `MeasuredRepetitions = 5`, sample-policy accepted, full-validation status
- [X] T018 [US2] Express `feature161Config` in `src/Testing/Testing.fs`: host-lane facts (display-server, display/renderer identity, direct-rendering, refresh, driver, package-version-set, cpu/gpu load, host profile, run/scenario/policy identities, artifact paths), unsupported facts (missing-display, indirect-rendering, software-raster, unknown-renderer, virtualized-presentation, stale-package), prior-gate statuses
- [X] T019 [US2] Delete the three original `Feature159Readiness` / `Feature160ThroughputReadiness` / `Feature161HostLaneReadiness` validator bodies in `src/Testing/Testing.fs` (~lines 4194 / 4300 / 4418); rewire any public `validate` entry points as thin source-compatible wrappers `validate = validateReadiness featureNNNConfig` (C-VC-2, C-VC-5)
- [X] T020 [US2] Story gate — build clean; `DISPLAY=:1 dotnet test` no new failures vs baseline (159/160/161 status + diagnostics + missing-artifact golden assertions green — the byte oracle, C-VC-1); `rg -n "module Feature159Readiness|module Feature160ThroughputReadiness|module Feature161HostLaneReadiness" src/Testing` confirms no remaining module bodies (SC-002)

**Checkpoint**: US1 + US2 both independently functional; a same-shaped feature is now a single config entry (SC-006).

---

## Phase 5: User Story 3 - One shared Markdown/JSON formatting helper (Priority: P3)

**Goal**: Extract the three byte-identical `esc`/`q`/`jsonStringArray`/`jsonCounts`/`countsText` copies in
`Testing.fs` into one shared module; point all call sites at it; reconcile the `Diagnostics.fs`
`System.Text.Json` variant **only** where bytes are unchanged. **Tier 2** (internal). **Independent of
US1/US2** — may land in any order.

**Independent Test**: One shared definition of each formatting helper; the three `Testing.fs` copies are
deleted; solution builds; full test suite matches baseline; every serialized readiness/evidence artifact
is byte-identical to baseline.

- [X] T021 [US3] Add the shared formatting helpers (private — not in any `.fsi`) in `src/Diagnostics/Diagnostics.fs` reproducing the `Testing.fs` form byte-for-byte: `esc` (`\`, `"`, `\r`, `\n`), `q`, `jsonStringArray` (**comma-space** separator), and `jsonCounts`/`countsText` (indentation/separators/ordering verbatim) per contracts/formatting-helpers.md
- [X] T022 [US3] Point `VisualReadinessMarkdown` call sites in `src/Testing/Testing.fs` at the shared helpers and delete its local copies (`esc`/`q`/`jsonStringArray`/`jsonCounts`/`statusCountsText`, ~lines 1235–1248)
- [X] T023 [US3] Point `VisualInspectionMarkdown` call sites in `src/Testing/Testing.fs` at the shared helpers and delete its local copies (~lines 1883–1896)
- [X] T024 [US3] Point `RetainedInspectionMarkdown` call sites in `src/Testing/Testing.fs` at the shared helpers and delete its local copies (~lines 2583–2596)
- [X] T025 [US3] Reconcile the `Diagnostics.fs` `System.Text.Json`-based variant (`src/Diagnostics/Diagnostics.fs` ~357–483) **only** where a helper is provably byte-equivalent; otherwise leave intact and add a one-line comment documenting it is behaviorally distinct (no comma-space `jsonStringArray`; `tokenOf`-parameterized `jsonCounts`/`countsText`) — any reconciliation changing a single emitted byte is rejected (C-FH-3)
- [X] T026 [US3] Story gate — build clean; `DISPLAY=:1 dotnet test` no new failures vs baseline (Visual / VisualInspection / RetainedInspection readiness golden assertions green — the byte oracle); byte-diff evidence artifacts vs T003 snapshot (zero changes); `rg -n "let esc |let q |let jsonStringArray |let countsText |let statusCountsText " src/Testing` confirms one definition each (SC-003)

**Checkpoint**: All three stories independently functional; one escaping edit now propagates from a single source (C-FH-4).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final whole-repo verification of the success criteria.

- [X] T027 Capture post-change evidence: `dotnet build FS.GG.Rendering.slnx -c Release` then `dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/180-shared-readiness-status/readiness/post-change.md`; `diff` against `baseline.md` and confirm **no new failures** (FR-008)
- [X] T028 Byte-diff every serialized readiness/evidence artifact against the T003 baseline snapshot — confirm 100% byte-identical (SC-004)
- [X] T029 [P] Verify success criteria: SC-001 (one `statusToken` + one `blocksAcceptance`), SC-002 (one validator + 3 config entries), SC-003 (one definition per helper), SC-005 (net source-line reduction across touched reporting code vs baseline), SC-006 (same-shaped feature addable as one config entry)
- [X] T030 [P] Run the `specs/180-shared-readiness-status/quickstart.md` validation end-to-end and confirm every "Success (all must hold)" item passes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (baseline is the diff oracle).
- **User Stories (Phase 3–5)**: All depend on the Foundational gate.
  - **US1 (P1)**: After Foundational. No story dependencies.
  - **US2 (P2)**: After Foundational **and US1** (consumes the shared `ReadinessStatus` vocabulary).
  - **US3 (P3)**: After Foundational. **Independent of US1/US2** — may land in any order.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### Within Each User Story

- US1: `.fsi` seam (T007) → shared impl (T008) → consumer migrations (T009–T010) → delete duplicates (T011) → override (T012) → surface refresh (T013) → gate (T014).
- US2: config record + validator (T015) → three config entries (T016–T018) → delete originals + wrappers (T019) → gate (T020).
- US3: shared helper (T021) → call-site migrations (T022–T024) → Diagnostics reconcile (T025) → gate (T026).

### Parallel Opportunities

- **Across stories**: Once Foundational is done, **US1 and US3 can proceed in parallel** (different
  concerns; US3 touches the Markdown modules, US1 touches the status DUs). US2 must follow US1.
- **Within a story**: limited — US1 T009–T010, US2 T016–T018, and US3 T022–T024 all edit the same files
  (`Testing.fs` / `Diagnostics.fs`), so they are **not** `[P]` (same-file conflict).
- **Polish**: T029 and T030 are `[P]` (independent verification passes).

---

## Parallel Example: cross-story (after Foundational)

```bash
# US1 and US3 are independent and touch different code regions — run in parallel:
Story US1: "Introduce shared ReadinessStatus in src/Diagnostics/Diagnostics.fs(+.fsi) and migrate status DUs"
Story US3: "Extract shared formatting helpers and migrate the three Testing.fs Markdown modules"
# US2 starts once US1's shared vocabulary is in place.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup) — baseline + artifact snapshot.
2. Complete Phase 2 (Foundational gate) — allowed reds recorded, evidence contract locked.
3. Complete Phase 3 (US1) — shared `ReadinessStatus`.
4. **STOP and VALIDATE**: T014 gate — build + test match baseline, artifacts byte-identical, surface
   drift additive only.
5. Ship US1 independently if desired (FR-009).

### Incremental Delivery

1. Setup + Foundational → diff oracle ready.
2. US1 → byte-diff + surface check → ship (MVP, Tier 1).
3. US2 → 159/160/161 byte oracle → ship (Tier 2).
4. US3 → Markdown byte oracle → ship (Tier 2, order-independent).
5. Polish → post-change.md diff + SC-001…SC-006.

### Notes

- `[P]` = different files, no dependency on incomplete tasks.
- Every story ends with a build + full-test gate and a byte-for-byte artifact diff — that diff, not a
  green compile, is the acceptance signal.
- Never weaken an assertion to pass; if output must change, preserve the existing string via a per-domain
  projection (out-of-scope consolidations are intentionally not pursued).
- Commit after each story (or logical group) for a clean per-story diff.
