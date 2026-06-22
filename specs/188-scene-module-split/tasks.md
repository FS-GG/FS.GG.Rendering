---
description: "Task list for Scene.fs Module Split (Pattern E + finish FR-006 inspection dedup)"
---

# Tasks: Scene.fs Module Split (Pattern E, finish FR-006 inspection dedup)

**Input**: Design documents from `/specs/188-scene-module-split/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/module-topology.md, quickstart.md

**Tests**: No NEW test tasks. This is a structural refactor (US1/US3 surface-neutral, US2 byte-identical) validated by the **existing** suites against the captured baseline. The only intentional behavior delta (FR-006 dedup) is gated by a reviewed expected-output update to existing inspection/evidence suites, not by new tests (FR-008 / SC-005).

**Organization**: Tasks are grouped by user story (US1 → US2 → US3, the plan's risk-sequenced order) so each slice is independently buildable, testable, and shippable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Single F# library project. Production code in `src/Scene/`; surface baseline in `readiness/surface-baselines/`; feature artifacts in `specs/188-scene-module-split/`. All commands run from repo root; GL suites require `DISPLAY=:1`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Re-confirm the current tree and capture the immutable pre-refactor baseline BEFORE any production edit (FR-011 — load-bearing for every later diff).

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project (solution + `tests/Package.Tests` + `samples/**/*.Tests`) and record the full red/green set,
> so pre-existing failures are known up front and not mistaken for regressions at merge. Use the
> discovery-based runner `scripts/baseline-tests.fsx` (globs `*.Tests.fsproj`) — do NOT hand-pick a
> subset; `dotnet test FS.GG.Rendering.slnx` deliberately omits Package.Tests and the sample suites,
> which is exactly where Feature 175's surprises hid.

- [X] T001 Re-confirm current-tree line/location references (Assumption: may drift): verify `src/Scene/Scene.fs` ≈ 2,084 lines (`wc -l`), the type wall (`Size`…`RetainedInspectionSummary`, ~L7–779), glyph trio (`buildGlyphRun`/`buildFallbackShapedText`/`glyphRunDataFromShapedText`), the `realTextMeasurer` seam, and the four namespace-level modules (`SceneEvidence`/`LayoutEvidence`/`VisualInspection`/`RetainedInspection`); record the confirmed line ranges in `specs/188-scene-module-split/readiness/tree-confirm.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/188-scene-module-split/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T003 [P] Capture the pre-refactor surface snapshot: `dotnet build FS.GG.Rendering.slnx -c Release` then `dotnet fsi scripts/refresh-surface-baselines.fsx`; copy `readiness/surface-baselines/FS.GG.UI.Scene.txt` to `specs/188-scene-module-split/readiness/surface-baseline.before.txt` as the immutable reference (do NOT commit drift here)
- [X] T004 [P] Record the current `<Version>` of `src/Scene/Scene.fsproj` (`0.1.37-preview.1`) into `specs/188-scene-module-split/readiness/version-gate.md` as the version-bump gate's starting point

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the live render/inspection corpus and lock the compile-order contract before any
file split. **No user story may begin until this phase is complete.**

> **⚠️ Early live render/artifact corpus (STANDING, do not omit).** For this library refactor the
> "real running app" evidence is the rendered-frame + glyph-fingerprint + inspection/evidence artifact
> corpus produced by actually exercising the render/inspection paths — NOT just unit-test pass/fail.
> Capture it BEFORE any production edit and treat it as the semantic/byte baseline every story diffs
> against. (Feature 175 lesson: the deterministic core passed while real output stayed wrong; only
> running the real paths surfaced the truth.) This is the FR-011 artifact corpus — record it live, or
> mark `environment-limited` with a disclosed substitute if a GL path cannot run.

- [X] T005 Capture the live FR-011 artifact corpus BEFORE any edit: run the render/inspection/evidence paths (`DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` plus any `tools/Rendering.Harness`/`Testing` evidence suites) and archive the emitted reference frames, glyph fingerprints, and `VisualInspection`/`RetainedInspection`/`SceneEvidence`/`LayoutEvidence` artifacts into `specs/188-scene-module-split/readiness/corpus.before/` (mark `environment-limited` + disclosed substitute for any GL path that cannot run)
- [X] T006 Lock the compile-order contract (contracts/module-topology.md §C1) in `src/Scene/Scene.fsproj`: plan the target `<Compile>` order (`Types.* → TextShaping.* → Scene.* → Inspection.* → Evidence.* → SceneWire/SceneCodec/Animation` tail), noting the `TextShaping.*` vs `Scene.*` relative order is fixed empirically by US2's delegation direction; record the planned order in `specs/188-scene-module-split/readiness/compile-order.md` (no FS0039 / back-edge — Edge Case "F# root-position back-edge")
- [X] T007 [P] Define the FR-006 dedup verification protocol (FR-012, parent report §7 applied as method): document in `specs/188-scene-module-split/readiness/dedup-review.md` the semantic-artifact diff procedure (parsed status/counts/headers/finding-sets vs `corpus.before/`) and the reviewed-and-approved expected-output sign-off record the US3 dedup delta must satisfy (SC-007) — gate, not byte-equality

**Checkpoint**: Baseline + live corpus captured, compile-order contract locked, dedup review protocol
defined — user-story work can now begin (in priority order; US2 builds on US1 types, US3 is independent of US2).

---

## Phase 3: User Story 1 - Extract `Scene.Types`; `Scene` becomes builders-only root (Priority: P1) 🎯 MVP

**Goal**: Lift the ~770-line namespace-level type wall (`Size`…`RetainedInspectionSummary`) out of
`Scene.fs` into a new `Types.fs` that **stays in `namespace FS.GG.UI.Scene`** with namespace-level
type declarations (NOT a nested module), so CLR `FullName`s are unchanged → surface-neutral re-home,
zero consumer churn, no version bump (research Decision 1).

**Independent Test**: Whole solution + all ~17 consumers compile against the re-homed types; `Scene`
round-trip/package/inspection suites stay green; regenerated `FS.GG.UI.Scene` surface baseline diff is
**empty** (Acceptance US1 #1–#3).

### Implementation for User Story 1

- [X] T008 [US1] Author `src/Scene/Types.fsi` declaring `namespace FS.GG.UI.Scene` with the namespace-level public type signatures for the full type wall (`Size`, `Color`, `Point`, `Rect`, stroke/paint/path/shader/filter types, `Paint`, `PathSpec`, `Clip`, `Region`, text+shaping types `FontSpec`…`GlyphRun`, `Vertex`, `SceneElementKind`, `SceneNode`, layout-evidence types, all `VisualInspection*`/`Retained*`/`Damage*` records+unions, `RenderDiagnostic`, …`RetainedInspectionSummary`) — verbatim signatures, no `module` wrapper (Constitution II: visibility lives in `.fsi`)
- [X] T009 [US1] Create `src/Scene/Types.fs` as `namespace FS.GG.UI.Scene` and MOVE the type-wall definitions out of `src/Scene/Scene.fs` into it verbatim (no representation change — re-home location only; FR-001), deleting the moved block from `Scene.fs`
- [X] T010 [US1] Update `src/Scene/Scene.fsproj` `<Compile>` order to place `Types.fsi`/`Types.fs` FIRST (before `Scene.fsi`/`Scene.fs`), per the locked compile-order contract (T006); confirm `Scene.fsi`/`Scene.fs` no longer declare the moved types
- [X] T011 [US1] Build the full solution `dotnet build FS.GG.Rendering.slnx -c Release` and resolve any FS0039/back-edge/ordering error so all ~17 consumers (14 ProjectReference + `samples/AntShowcase`, `samples/SecondAntShowcase`, `template/base`) resolve every type from `FS.GG.UI.Scene.*` (Edge Case "namespace-type resolution after re-home"; Acceptance US1 #1)
- [X] T012 [US1] Run the Scene suites `DISPLAY=:1 dotnet test tests/Scene.Tests -c Release` (round-trip / package / inspection — `Feature140/142/146/165`) and confirm scene values + rendered frames are equivalent to `corpus.before/` (Acceptance US1 #2; FR-006 byte-equivalence where construction guarantees it)
- [X] T013 [US1] Regenerate the surface baseline `dotnet fsi scripts/refresh-surface-baselines.fsx` and confirm `git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt` is **EMPTY** vs `surface-baseline.before.txt`; if non-empty, the namespace-level mechanism was violated — fix before proceeding (Acceptance US1 #3; SC-006; no version bump)

**Checkpoint**: `Scene.fs` shrunk by the type wall; solution + all consumers green; empty surface diff;
US1 independently shippable.

---

## Phase 4: User Story 2 - Unify the glyph-shaping trio into `Text.Shaping`; isolate the measurer seam (Priority: P2)

**Goal**: Collapse the ~60%-duplicated shaping trio behind ONE private parameterized shaped-text core
in a new `module FS.GG.UI.Scene.Text.Shaping` (`TextShaping.fs`), and relocate the mutable
`realTextMeasurer` seam there as its single owner — keeping `module Scene` public entry points as thin
delegations so glyph runs / fingerprints / measurement stay **byte-identical** (research Decision 2).

**Independent Test**: Glyph runs, shaped-text results, and fingerprints are byte-identical before/after;
`setRealTextMeasurer` set/clear/measure lifecycle is unchanged (Acceptance US2 #1–#2; contract C4).

### Implementation for User Story 2

- [X] T014 [US2] Author `src/Scene/TextShaping.fsi` declaring `module FS.GG.UI.Scene.Text.Shaping` — public signatures for the shaping entry points that move here (and any newly public helpers the gate requires) plus the `realTextMeasurer` accessor (`setRealTextMeasurer`) signatures (FR-003 single owner)
- [X] T015 [US2] Create `src/Scene/TextShaping.fs` (`module Text.Shaping`) with ONE private parameterized shaped-text core; re-express `buildGlyphRun`, `buildFallbackShapedText`, and `glyphRunDataFromShapedText` over it, collapsing the duplicated logic incl. shared `glyphRunFingerprintOf`/`shapedTextFingerprintOf`/`directionOf`/`scriptOf` helpers — PRESERVING field/accumulation order so fingerprints stay byte-identical (FR-002; SC-002 3→1; Edge Case "float/accumulation order")
- [X] T016 [US2] Move the `realTextMeasurer` mutable cell + its set/measure logic from `src/Scene/Scene.fs` into `src/Scene/TextShaping.fs` as the single owner, preserving set/clear/measure semantics (FR-003; Edge Case "realTextMeasurer lifecycle")
- [X] T017 [US2] Reduce the shaping/measurer bulk in `src/Scene/Scene.fs` to thin public delegations (`module Scene` entry points call into `Text.Shaping`) so public names are preserved; delete the moved implementation bulk from `Scene.fs`
- [X] T018 [US2] Insert `TextShaping.fsi`/`TextShaping.fs` into `src/Scene/Scene.fsproj` `<Compile>` order at the empirically-correct position relative to `Scene.*` (delegation direction decides; if `module Scene` shims call `Text.Shaping`, `TextShaping.*` compiles BEFORE `Scene.*`) — resolve by compiling (research Decision 5)
- [X] T019 [US2] Build `dotnet build FS.GG.Rendering.slnx -c Release` (no FS0039/back-edge) and run shaping suites `DISPLAY=:1 dotnet test tests/Scene.Tests -c Release` — `Feature140GlyphRunTests`, `Feature142*` (determinism/itemization/pure-fallback), `Feature136MeasurementSeamTests` — confirming glyph runs / shaped-text / fingerprints byte-identical and the measurer lifecycle unchanged (Acceptance US2 #1–#2; contract C4; SC-004)
- [X] T020 [US2] Regenerate `dotnet fsi scripts/refresh-surface-baselines.fsx`, review `git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt`: it MUST show ONLY the intended shaping/measurer relocation and no incidental drift (FR-007). **Version-bump gate**: bump `<Version>` in `src/Scene/Scene.fsproj` iff the reviewed diff is non-empty; record the decision in `specs/188-scene-module-split/readiness/version-gate.md`

**Checkpoint**: Shaping unified 3→1, measurer single-owner, output byte-identical; surface diff empty
or bumped-and-recorded; US1+US2 both independently green.

---

## Phase 5: User Story 3 - Extract `Scene.Inspection` + `Scene.Evidence`; finish the FR-006 dedup (Priority: P3)

**Goal**: Move the four namespace-level modules into two new files **keeping their module names**
(`module FS.GG.UI.Scene.VisualInspection`/`RetainedInspection` → `Inspection.fs`; `SceneEvidence`/
`LayoutEvidence` → `Evidence.fs`) — surface-neutral — then **finish** the FR-006 dedup so findings
sharing a `stableFindingId` collapse uniformly across BOTH inspection paths (the only behavior change;
research Decisions 3–4).

**Independent Test**: Inspection/evidence suites green; unchanged-finding inputs ⇒ artifacts
semantically equivalent to `corpus.before/`; duplicate-finding inputs ⇒ collapsed exactly per the
approved expected-output record; degenerate scene ⇒ genuine finding still emitted with same fail-loud
diagnostic (Acceptance US3 #1–#3; contract C3).

### Implementation for User Story 3 — surface-neutral module moves

- [X] T021 [P] [US3] Author `src/Scene/Inspection.fsi` declaring `module FS.GG.UI.Scene.VisualInspection` + `module FS.GG.UI.Scene.RetainedInspection` with the existing public signatures verbatim (names preserved → surface-neutral)
- [X] T022 [P] [US3] Author `src/Scene/Evidence.fsi` declaring `module FS.GG.UI.Scene.SceneEvidence` + `module FS.GG.UI.Scene.LayoutEvidence` with the existing public signatures verbatim (names preserved → surface-neutral)
- [X] T023 [US3] Create `src/Scene/Inspection.fs` and MOVE `module VisualInspection` + `module RetainedInspection` out of `src/Scene/Scene.fs` into it verbatim (keep `module FS.GG.UI.Scene.*` names); delete the moved blocks from `Scene.fs`
- [X] T024 [US3] Create `src/Scene/Evidence.fs` and MOVE `module SceneEvidence` + `module LayoutEvidence` out of `src/Scene/Scene.fs` into it verbatim (keep `module FS.GG.UI.Scene.*` names); delete the moved blocks from `Scene.fs`
- [X] T025 [US3] Insert `Inspection.fsi`/`Inspection.fs`/`Evidence.fsi`/`Evidence.fs` into `src/Scene/Scene.fsproj` `<Compile>` order AFTER `Scene.*` and before the `SceneWire/SceneCodec/Animation` tail (per locked contract T006)

### Implementation for User Story 3 — finish the FR-006 dedup (behavior change)

- [X] T026 [US3] Verify the FR-006 hypothesis against the baseline: confirm `stableFindingId` (+ `cleanToken`) builds a stable identity token and `duplicateIds` currently only emits diagnostic strings without collapsing findings; record the confirmed current behavior in `specs/188-scene-module-split/readiness/dedup-review.md` BEFORE editing (plan "do not build the dedup on the unverified hypothesis alone")
- [X] T027 [US3] Apply the SAME collapse rule (dedupe by `FindingId`, keep first occurrence, preserve every unique finding) on both the visual and retained paths in `src/Scene/Inspection.fs` (data-model "SC-003 uniformity"). NOTE: "uniform" means the same *collapse rule*, NOT an identical identity-key *function* — the visual `stableFindingId` (`ruleId`,`affectedIds`) and retained `stableFindingId` (`ruleId`,`transitionId`,`affectedIds`) legitimately differ; preserve each path's own identity scope (the retained path keeps `transitionId` in its key). Only factor a shared `cleanToken` helper if the normalization (not the key shape) is genuinely identical
- [X] T028 [US3] Finish the dedup collapse in `src/Scene/Inspection.fs`: for both `VisualInspection` findings and `RetainedInspectionArtifact.Findings`, collapse findings sharing a `FindingId` to one (keep first occurrence, preserve order, preserve every unique finding) — uniformly on BOTH paths (FR-005; SC-003: zero paths still emit known-duplicate findings). NOTE: the retained path already applies `Findings |> List.sortBy _.FindingId` (Scene.fs:1877), so on that path "preserve order" means preserve the existing **post-sort** order (collapse adjacent equal-`FindingId` runs); do not introduce a second reordering. The visual path preserves insertion order
- [X] T029 [US3] Enforce the dedup guards (FR-009; Edge Case "dedup must not silence unique findings"): collapse duplicates only — never drop a unique real finding, never reorder in a meaning-changing way, never weaken/silence a fail-loud diagnostic; a malformed/degenerate scene still surfaces its genuine finding with the same diagnostic (Acceptance US3 #3)

### Validation for User Story 3

- [X] T030 [US3] Build `dotnet build FS.GG.Rendering.slnx -c Release` and run the full suite `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` (inspection/evidence + consumer + `Rendering.Harness`/`Testing` evidence suites); regenerate `dotnet fsi scripts/refresh-surface-baselines.fsx` and confirm `git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt` is **EMPTY** (module names preserved; Acceptance US3 #1)
- [X] T031 [US3] Perform the semantic-artifact diff vs `corpus.before/` (contract C3; FR-012): unchanged-finding inputs ⇒ artifacts semantically equivalent (status/counts/headers/finding-sets); duplicate-finding inputs ⇒ collapsed as expected; record the dedup delta as an explicitly reviewed-and-approved expected-output change in `specs/188-scene-module-split/readiness/dedup-review.md` (SC-007), and update the affected suites' expected output to the approved set (FR-008 — explicit, reviewed, recorded; no assertion weakened)

**Checkpoint**: Four modules re-homed surface-neutrally; FR-006 dedup finished + uniform across both
paths; semantic diff approved and recorded; all three stories independently green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature gates spanning all three stories.

- [X] T032 Verify SC-001 size guideline: `wc -l src/Scene/Scene.fs src/Scene/Types.fs src/Scene/TextShaping.fs src/Scene/Inspection.fs src/Scene/Evidence.fs` — each at or below the ~1,500-line guideline; confirm `Scene.fs` no longer contains the type wall, glyph trio, `realTextMeasurer` seam, or the four inspection/evidence modules
- [X] T032a Verify FR-010 (no new project/dependency/inter-project reference): `git diff --stat` shows no new `.fsproj`; `git diff -- src/Scene/Scene.fsproj` adds only `<Compile>` entries (no new `<PackageReference>`/`<ProjectReference>`); only `<Compile>` files were added under `src/Scene` — record the check result alongside the size gate
- [X] T033 Final test-parity gate (SC-005 / FR-008): re-run `dotnet fsi scripts/baseline-tests.fsx --out specs/188-scene-module-split/readiness/baseline.after.md` and diff vs `readiness/baseline.md` — red/green set identical EXCEPT the reviewed FR-006 expected-output updates; no test deleted/skipped, no assertion weakened
- [X] T034 [P] Finalize the version-bump gate (SC-006 / FR-007): confirm `src/Scene/Scene.fsproj` `<Version>` reflects the US2 surface decision (bumped iff non-empty reviewed diff, else unchanged) and that `readiness/version-gate.md` records the rationale
- [X] T035 [P] Run the quickstart end-to-end (`specs/188-scene-module-split/quickstart.md` Steps 0–4) as the final acceptance pass; confirm SC-002 (3→1 builder + single measurer owner), SC-003 (dedup uniform, zero known-duplicate paths), SC-004 (byte-equivalence), and SC-007 (approved dedup delta recorded)
- [X] T036 Capture per-phase feedback into `specs/188-scene-module-split/feedback/` (process friction, generalizable-code candidates, severity) per the `fs-gg-feedback-capture` discipline

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T002/T003/T004 capture the immutable baseline; **no production edit may precede them** (FR-011).
- **Foundational (Phase 2)**: Depends on Setup. Captures the live corpus (T005), locks compile order (T006), defines the dedup review protocol (T007). **BLOCKS all user stories.**
- **User Stories (Phase 3–5)**: All depend on Foundational. Sequenced by risk P1 → P2 → P3.
- **Polish (Phase 6)**: Depends on all three stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent — establishes the `Types.fs` floor. The MVP.
- **US2 (P2)**: Builds on US1's re-homed types (shaping references them) — start after US1 compiles. Independently testable (byte-identical shaping).
- **US3 (P3)**: Independent of US2 (operates on inspection/evidence + dedup). Could proceed in parallel with US2 after US1, but sequenced last because it carries the only behavior change. Independently testable.

### Within Each User Story

- `.fsi` authored before `.fs` body (Constitution I/II).
- Move/extract before build-order edit; build-order edit before full-solution build; build before suite run; suite run before surface regen.
- US3: surface-neutral module moves (T021–T025) before the dedup behavior change (T026–T029); hypothesis confirmation (T026) before any dedup edit.

### Parallel Opportunities

- Setup: T003, T004 parallel (different artifacts); T001/T002 sequential-ish (T002 needs a clean read).
- Foundational: T007 parallel with T005/T006.
- US3: T021 ∥ T022 (separate `.fsi` files); T023/T024 are separate `.fs` files but both edit `Scene.fs` (the source of the moved blocks) so serialize the `Scene.fs` deletions.
- Polish: T034 ∥ T035.

---

## Parallel Example: User Story 3 `.fsi` authoring

```bash
# Author both inspection/evidence signature files together (separate files, no dependency):
Task: "Author src/Scene/Inspection.fsi (module VisualInspection + RetainedInspection)"
Task: "Author src/Scene/Evidence.fsi (module SceneEvidence + LayoutEvidence)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup — capture the immutable baseline + live corpus FIRST.
2. Phase 2 Foundational — lock compile order + dedup review protocol.
3. Phase 3 US1 — extract `Types.fs` (surface-neutral).
4. **STOP and VALIDATE**: full solution + 17 consumers compile; Scene suites green; **empty** surface diff.
5. US1 is independently shippable (biggest size reduction, zero surface/behavior change).

### Incremental Delivery

1. Setup + Foundational → baseline + corpus + contracts ready.
2. US1 (Types.fs) → empty surface diff → ship (MVP).
3. US2 (Text.Shaping) → byte-identical shaping → version-bump gate decision → ship.
4. US3 (Inspection/Evidence + FR-006 dedup) → semantic diff approved + recorded → ship.

---

## Notes

- [P] = different files, no dependency on incomplete tasks.
- The FR-011 baseline (T002/T003/T005) is load-bearing — every later diff references it; do NOT edit production code before it is captured.
- US1 and US3 target **empty** surface diffs (namespace-level / preserved module names); only US2 may legitimately change the surface, gated by review + version bump.
- The FR-006 dedup (T026–T029) is the ONLY behavior change — gated by semantic-artifact diff + reviewed sign-off (FR-012), never byte-equality, and must never silence a unique finding.
- Commit after each story checkpoint.
