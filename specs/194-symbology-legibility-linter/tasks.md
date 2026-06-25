---
description: "Task list for Symbology Legibility Linter (M7 — linter thread)"
---

# Tasks: Symbology Legibility Linter

**Input**: Design documents from `/specs/194-symbology-legibility-linter/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/legibility-api.md ✓, quickstart.md ✓

**Tests**: INCLUDED. This feature is **specification-first / Tier 1** (Constitution I) — the public `.fsi`
is authored before any `.fs` body and Expecto semantic tests over the public surface must **fail-before /
pass-after**. Test tasks are therefore not optional here.

**Organization**: Tasks are grouped by user story. US1 (the pure linter) is the MVP and is independently
testable; US2 (loop integration + roster-clean agreement) builds on US1.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 (Setup, Foundational, Polish carry no story label)
- Exact file paths are included in every task.

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). The linter lands as a **new module in the existing
pure package** `src/Symbology/` (no new project, no new dependency). Tests land in two existing test
projects. Paths below are repo-root-relative.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the working tree and capture the no-regression baseline before any change.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task runs **every** test project
> (solution + `Package.Tests` + `samples/**/*.Tests`) via the discovery-based runner so pre-existing reds
> are known up front and never mistaken for regressions at merge.

- [X] T001 Confirm working tree is on branch `194-symbology-legibility-linter` and the four spec
  artifacts (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are present under
  `specs/194-symbology-legibility-linter/`; `dotnet build FS.GG.Rendering.slnx` succeeds clean before any edit.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/194-symbology-legibility-linter/readiness/baseline.md`
  (globs every `*.Tests.fsproj` — solution + `Package.Tests` + samples — and records the full red/green
  set; any pre-existing red is flagged here, not discovered at merge).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Author the public contract (`.fsi`) and prove the new surface loads and runs end-to-end
**before** building out any check. This is a greenfield additive pure module — there is no defect/root-cause
map; the analogue of the live-smoke mandate is an **early FSI/test smoke** over the public surface.

**⚠️ CRITICAL**: No user-story work begins until T006 (the FSI smoke) confirms `score`/`scoreAnimated` run
end-to-end on a hand-built board and return a `Report`.

> **⚠️ Early smoke run (STANDING, do not omit).** Treat the plan's narrative as **unverified** until the
> surface is exercised: once `Legibility.fsi` + a first `.fs` stub compile, load the public surface in FSI
> (or run a single Expecto test) and confirm `score` and `scoreAnimated` return a `Report` on a hand-built
> `Token list` — before US1/US2. (Pure CPU logic, no GL/raster/IO, so this is fully headless — quickstart.md "FSI smoke".)

- [X] T003 Author the public contract `src/Symbology/Legibility.fsi` — `[<RequireQualifiedAccess>] module Legibility`
  with types `Channel`, `ChannelKind`, `Severity`, `Finding`, `ChannelSpec`, `ChannelUsage`, `Verdict`,
  `Report` and vals `table`, `score: Token list -> Report`, `scoreAnimated: (Motion * Token) list -> Report`,
  exactly per `contracts/legibility-api.md`. Interface-first; no `.fs` body yet (Constitution I/II).
- [X] T004 Register the two new files in `src/Symbology/Symbology.fsproj` in compile order
  `Symbology.fsi → Symbology.fs → Legibility.fsi → Legibility.fs` (the linter references the channel/`Token`
  types but not the `Symbology` rendering functions). Purity (FR-012) is guaranteed by this closure —
  `Symbology.fsproj` references only `FS.GG.UI.Scene`; **add no raster/GL/IO/`Render` reference**, so the
  pure-layer constraint holds structurally.
- [X] T005 Create a minimal compiling stub `src/Symbology/Legibility.fs`: the `table : ChannelSpec list`
  populated from research D2 with **one row per per-unit channel (11 rows; `Motion` excluded — it has no
  `ChannelKind`)** (Faction 7 / Klass 6 / Sigil 12 / State 3 / Shield 3 / Speed 4 + the 5 Continuous rows)
  and `score`/`scoreAnimated` returning a `{ Findings = []; Usage = <all 11 per-unit channels at 0>; Verdict = Clean }`
  so the package builds and the surface loads. `dotnet build src/Symbology/Symbology.fsproj` succeeds.
- [X] T006 **Early FSI smoke**: build the package, then run the `quickstart.md` "FSI smoke" snippet
  (`#r FS.GG.UI.Scene.dll` + `FS.GG.UI.Symbology.dll`; `open FS.GG.UI.Symbology`) — call `Legibility.score`
  on a tiny within-capacity board and `Legibility.scoreAnimated` on a 1-unit board, confirm a `Report`
  returns end-to-end with no exception. Record the result as the surface-usable confirmation BEFORE US1/US2.
- [X] T007 [P] Stand up the test scaffold: add `tests/Symbology.Tests/LegibilityTests.fs` (empty Expecto
  `testList "Legibility" []`) to `tests/Symbology.Tests/Symbology.Tests.fsproj` (before `Program.fs` in
  compile order) and register the list in `tests/Symbology.Tests/Program.fs`; confirm `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` stays green.

**Checkpoint**: Public `.fsi` frozen, surface loads, `score`/`scoreAnimated` run end-to-end, test scaffold
green — user-story implementation can begin.

---

## Phase 3: User Story 1 - Score a symbol set for legibility overload and get an actionable report (Priority: P1) 🎯 MVP

**Goal**: A pure, deterministic `Legibility.score` / `scoreAnimated` that scores a produced symbol set
against the fixed §4 capacities and returns a structured `Report` (per-channel usage, findings with
machine-readable channel + severity + units, overall verdict). Advisory: never mutates, never raises on
valid input.

**Independent Test**: Hand-build (a) a within-capacity board → `Clean`, `Findings = []`; (b) a board
exceeding one categorical channel (e.g. 8 distinct factions, or >4 distinct Speed bead counts) → one
`Warning` naming that channel with used-vs-capacity and contributing units; (c) a board with an
out-of-domain unit (magnitude outside `[0,1]`, Speed outside `[0,6]`, or `R <= 0`) → one `Error` naming the
channel and unit, scan completes. Score the same set twice → structurally equal `Report`.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

> Each maps to a behavioural-contract row (C1–C12, C14) in `contracts/legibility-api.md`. All live in
> `tests/Symbology.Tests/LegibilityTests.fs` (same file → not [P] with each other), exercising only the
> public `Legibility` surface.

- [X] T008 [US1] C1/C10/C9 — within-capacity board → `Findings = []`, `Verdict = Clean`; all-identical
  roster → `DistinctLevels = 1` per categorical channel, `Clean`; empty `score []`/`scoreAnimated []` →
  `Findings = []`, `Clean`, usage all 0. In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T009 [US1] C2 — per **categorical** channel (Faction>7 incl. distinct `Custom` colours, Klass>6,
  Sigil>12, State>3, Shield>3) a crafted over-capacity set yields exactly one `Warning` on that `Channel`
  with used-vs-capacity in `Message` and the contributing `Units`; in-capacity channels emit nothing
  (no false positives). In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T010 [US1] C3 — a set with >4 distinct `Speed` bead counts → one `Warning` on `Speed` naming the
  units (Ordered overload). In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T011 [US1] C4/C5/C6 — out-of-domain `Threat`/`Charge`/`Health` outside `[0,1]` or `Speed` outside
  `[0,6]` → one `Error` on that channel+unit, scan completes; `R <= 0` degenerate → one `Error` on `Size`,
  remaining units still scored; non-finite float (NaN/±∞) on any field → one `Error` on that channel, no
  exception. In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T012 [US1] C9-continuous-exempt — a board with many distinct `Threat`/`Charge`/`Size`/`Health`/`Heading`
  values (continuous) emits **no** overload finding (FR-009). In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T013 [US1] C11/C12 — `scoreAnimated` with >1 distinct non-`Idle` rhythm → one `Warning` on `Motion`,
  `Units = []`; one rhythm across many moving units → no `Motion` finding. In `tests/Symbology.Tests/LegibilityTests.fs`.
- [X] T014 [US1] C7/C8/C14 — determinism (`score s = score s`, structural equality); machine-actionable
  (filter findings by `Channel` and `Severity` and derive `Verdict` without parsing `Message`); advisory
  (scoring a valid-but-overloaded set returns a report, never throws, input list unmutated). In
  `tests/Symbology.Tests/LegibilityTests.fs`. Run the suite and confirm these tests **FAIL** against the T005 stub.

### Implementation for User Story 1

- [X] T015 [US1] Replace the T005 stub `table` with the final fixed capacity table in `src/Symbology/Legibility.fs`:
  one `ChannelSpec` for **each of the 11 per-unit channels** in §4 table order with the research-D2
  `Kind`/`Capacity` (Categorical: Faction 7, Klass 6, Sigil 12, State 3, Shield 3; Ordered: Speed 4;
  Continuous: Size/Threat/Charge/Health/Heading). **`Motion` has no `ChannelKind` and is NOT a `table`
  row** — its budget-1 board check lives in `scoreAnimated` (T022). Keep `Legibility.table` exposing it read-only.
- [X] T016 [US1] Implement the per-`Token` channel-value extractors + distinct-level counting (via
  `Set`/`List.distinct`) for the categorical/ordered channels, counting each distinct `Custom` faction
  colour separately and `Sigil`'s `Mark` paths by value, in `src/Symbology/Legibility.fs`.
- [X] T017 [US1] Implement the **level-overload** check (FR-003): for each `Categorical`/`Ordered` channel
  emit one `Warning` `Finding` when distinct levels `>` capacity, with channel identity, used-vs-capacity
  `Message`, and the contributing unit indices; continuous channels exempt (FR-009). In `src/Symbology/Legibility.fs`.
- [X] T018 [US1] Implement the **out-of-domain / non-finite** check (FR-004): per unit, `Error` findings for
  `Threat`/`Charge`/`Health` outside `[0,1]`, `Speed` outside `[0,6]`, and any non-finite float on a scored
  field; naming the channel and the offending unit index. Scan continues (FR-008). In `src/Symbology/Legibility.fs`.
- [X] T019 [US1] Implement the **degenerate-unit** check (FR-005): `R <= 0` → `Error` on `Size` for that
  unit, scan continues, the unit's categorical channel values still contribute to distinct-level counts
  (research D6). In `src/Symbology/Legibility.fs`.
- [X] T020 [US1] Implement `ChannelUsage` assembly + `Verdict` derivation (FR-007): one `ChannelUsage` per
  per-unit channel (11; `Motion` excluded) in table order (`DistinctLevels` = distinct count; for Continuous,
  the informational count of distinct raw values — never drives a finding); `Verdict` = `Clean` iff
  `Findings = []` else `HasWarnings`. In `src/Symbology/Legibility.fs`.
- [X] T021 [US1] Wire `score : Token list -> Report` — run overload + domain + degenerate checks, assemble
  usage/verdict, emit `Findings` in deterministic order (table order, then ascending unit index); motion-load
  skipped. In `src/Symbology/Legibility.fs`.
- [X] T022 [US1] Implement the **whole-board motion-load** check + wire `scoreAnimated : (Motion * Token) list -> Report`
  (FR-010): score the `Token`s as in `score`, then count distinct non-`Idle` `Motion` values across the board;
  if `> 1` add one `Warning` on `Motion` with `Units = []`; a single rhythm never flags. In `src/Symbology/Legibility.fs`.
- [X] T023 [US1] Run `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and confirm T008–T014 now
  **pass** (fail-before/pass-after closed); re-run the T006 FSI smoke to confirm the live surface still
  returns a `Report`.

**Checkpoint**: US1 is fully functional and independently testable — the linter exists, is pure/deterministic,
and produces actionable reports. This is the MVP. **STOP and VALIDATE** before US2.

---

## Phase 4: User Story 2 - Use the linter as the mechanical backstop in the design loop's CRITIQUE step (Priority: P2)

**Goal**: Wire the linter into the `fs-gg-symbology` design loop's CRITIQUE step (mirrored across all skill
trees, parity-checked) and prove the linter agrees with prior human approval — the approved M5/M6 roster
lints clean.

**Independent Test**: `score (roster |> List.map Roster.mapUnit)` over the in-tree M5/M6 roster → `Clean`,
`Findings = []` (C13); a deliberately overloaded variant of that mapping's output surfaces concrete findings;
the CRITIQUE-step linter guidance is present and consistent across every mirrored skill tree and
`scripts/check-agent-skill-parity.fsx` passes.

### Tests for User Story 2 (write FIRST) ⚠️

- [X] T024 [US2] C13 roster-clean (FR-014/SC-005): add a test in `tests/SymbologyBoard.Tests/BoardTests.fs`
  asserting `Legibility.score (roster |> List.map Roster.mapUnit)` over the in-tree 8-unit M6 roster yields
  `Verdict = Clean`, `Findings = []` (reuses the already-compiled `Roster.mapUnit`). Confirm it FAILS only if
  the linter mis-scores; it should pass once US1 is complete.
- [X] T025 [US2] Overloaded-variant test in `tests/SymbologyBoard.Tests/BoardTests.fs`: take a deliberately
  overloaded derivative of the mapped roster (e.g. remap to >7 distinct factions) and assert the linter
  surfaces a concrete `Warning` an agent could act on by tweaking the mapping (proves the check is not vacuous).

### Implementation for User Story 2

- [X] T026 [US2] Update the canonical `src/Symbology/skill/SKILL.md` CRITIQUE step (the "fixed feedback loop",
  step 4) so it **invokes the linter** on the symbol set the current mapping produces — the mechanical
  complement to the human eyeball check — keeping the unit of change the per-game mapping, never the grammar
  (research D10). Include the run recipe (`Legibility.score (roster |> List.map mapUnit)` → inspect `Verdict`/`Findings`).
- [X] T027 [P] [US2] Mirror the CRITIQUE update into the full copy at
  `template/product-skills/fs-gg-symbology/SKILL.md` (the `.claude/` and `.agents/` wrappers are thin
  pointers to the canonical file and inherit the edit automatically).
- [X] T028 [US2] Run `dotnet fsx scripts/check-agent-skill-parity.fsx` and confirm **PASS** — the CRITIQUE-step
  linter guidance is present and consistent across all mirrored skill trees (SC-008).
- [X] T029 [US2] Run `dotnet test tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` and confirm T024
  (roster lints clean) and T025 (overloaded variant flags) both pass.

**Checkpoint**: US1 + US2 both work — the linter exists AND the design loop calls it; mechanical check agrees
with prior human approval.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Tier 1 surface-baseline regeneration with zero drift elsewhere, full-suite green, and
quickstart validation.

- [X] T030 Regenerate the symbology surface baseline: `dotnet fsx scripts/refresh-surface-baselines.fsx`,
  then `git diff --stat readiness/surface-baselines/` and confirm **only** `readiness/surface-baselines/FS.GG.UI.Symbology.txt`
  changed (gains `Legibility.*` entries) — `FS.GG.UI.Symbology.Render.txt`, `FS.GG.UI.Scene.txt`,
  `FS.GG.UI.Controls.txt`, `FS.GG.UI.SkiaViewer.txt`, `FS.GG.UI.Canvas.txt` and the existing
  `FS.GG.UI.Symbology.txt` entries show **zero drift** (FR-013/SC-007).
- [X] T031 Run the full no-regression suite (`dotnet fsi scripts/baseline-tests.fsx`, or
  `dotnet test FS.GG.Rendering.slnx` + `Package.Tests` + samples) and compare to the T002 baseline: no new
  reds; the existing `token`/`animate`/`gallery`/`filmstrip` symbology tests stay green (rendering behaviour unchanged, SC-007).
- [X] T032 Run the full `quickstart.md` validation path (build → FSI smoke → both test projects → baseline
  refresh diff → skill-parity) and tick each per-Success-Criterion row (SC-001…SC-008).
- [X] T033 [P] Run `/speckit-agent-context-update` (or `dotnet ...` agent-context refresh) so the managed
  Spec Kit section reflects the new `Legibility` surface, if the agent-context extension is active.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup. **Blocks all user stories.** T003 (.fsi) → T004 (.fsproj) →
  T005 (stub) → T006 (FSI smoke) is a strict chain; T007 (test scaffold) is [P] after T005 compiles.
- **US1 (Phase 3)**: depends on Foundational. MVP. Independently testable.
- **US2 (Phase 4)**: depends on US1 (the linter must score before the roster-clean test and the loop call
  are meaningful). Independently testable once US1 lands.
- **Polish (Phase 5)**: depends on US1 + US2 complete.

### Within US1

- Tests T008–T014 written and FAILING (T014 confirms fail-before against the stub) **before** implementation
  T015–T022.
- Implementation order: capacity table (T015) → extractors/counting (T016) → overload (T017) →
  domain/non-finite (T018) → degenerate (T019) → usage/verdict (T020) → `score` wiring (T021) →
  motion-load + `scoreAnimated` (T022) → green confirmation (T023).

### Within US2

- T024/T025 (tests) before/alongside T026–T027 (skill edits); T028 parity after T026/T027; T029 green after US1.

### Parallel Opportunities

- T007 (test scaffold) runs in parallel with later Foundational verification once the stub compiles.
- US1 test tasks T008–T014 all touch the **same** file (`LegibilityTests.fs`) → author sequentially, not [P].
- US1 implementation T015–T022 all touch the **same** file (`Legibility.fs`) → sequential, not [P].
- T027 (template SKILL.md mirror) is [P] vs T026 only in the sense of a different file, but content depends
  on T026's wording — do T026 first, then T027.
- T033 (agent-context) is [P] with the rest of Polish.

---

## Parallel Example

```bash
# Foundational: once the stub (T005) compiles, the test scaffold can be stood up alongside the FSI smoke:
Task T006: "FSI smoke — load FS.GG.UI.Symbology.dll, run score/scoreAnimated end-to-end"
Task T007: "[P] Add empty LegibilityTests.fs + register in Program.fs, confirm green"
```

> Note: this feature is single-file-heavy (one `.fsi`, one `.fs`, one test file per project), so most
> within-story tasks are intentionally **sequential** to avoid same-file conflicts. The parallelism is
> across the two test projects and the skill mirror, not within `Legibility.fs`.

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup (T001–T002).
2. Phase 2 Foundational (T003–T007) — **including the early FSI smoke (T006)** that proves the surface is
   usable before any check is built.
3. Phase 3 US1 (T008–T023): tests fail-before → implement → pass-after.
4. **STOP and VALIDATE**: the linter is a complete, deterministic, advisory MVP on its own.

### Incremental Delivery

1. Setup + Foundational → surface frozen and smoke-confirmed.
2. US1 → the pure linter ships and is independently testable (MVP).
3. US2 → loop integration + roster-clean agreement.
4. Polish → Tier 1 baseline regeneration (zero drift), full-suite green, quickstart validation.

---

## Notes

- [P] = different files, no incomplete dependencies. Most US1/US2 work is single-file → sequential.
- [Story] label (US1/US2) maps each task to its user story; Setup/Foundational/Polish carry none.
- Spec-first is mandatory: `Legibility.fsi` (T003) precedes any `.fs` body; tests fail-before/pass-after.
- Purity is the hard constraint (FR-001/FR-012): no wall-clock, randomness, or IO in `Legibility.fs`.
- Zero surface drift outside the symbology baseline (FR-013/SC-007) is verified in T030.
- Commit after each task or logical group; stop at the US1 checkpoint to validate the MVP independently.
</content>
</invoke>
