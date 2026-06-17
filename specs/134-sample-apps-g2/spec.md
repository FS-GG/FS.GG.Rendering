# Feature Specification: Games + Productivity Sample Apps — curated G2 slice

**Feature Branch**: `134-sample-apps-g2`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "now do the samples" → Workstream **G2** of
`docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md` (§10) — a curated,
representative slice of the archived games + productivity sample apps, built as runnable `FS.GG.UI.*`
consumers with the same deterministic seeded-evidence harness the Controls Gallery (G1, feature 123)
established. G1 is shipped; this is the next sample work.

## Overview

The framework now ships a flagship **Controls Gallery** (G1) that exercises every catalog control, but
no sample that proves the stack composes into *real applications* — a live interactive loop, keyboard
input, deterministic step timing, forms with validation, and inline-edited data collections. The
archive carries **22 such specs** (12 games, 10 productivity apps), deliberately deferred at import.

This feature adopts a **curated, representative slice** of those specs — **three games**
(Tetris, Snake, Pong) and **three productivity apps** (Kanban board, Todo/task manager, Calendar
scheduler) — chosen to **maximize distinct control and input coverage** in the smallest set: the games
exercise the persistent interactive loop, deterministic step timing, keyboard input, and grid/continuous
rendering; the productivity apps exercise forms, data grids, lists, validation, and inline edit — the
enterprise patterns Workstreams F and D target. Each sample runs in two modes — an **interactive**
windowed mode for humans and a **headless deterministic seeded-evidence** mode for continuous
integration — reusing the no-overclaim evidence conventions from G1.

The remaining ~16 archived specs are an **explicitly disclosed backlog**, not a batch: the plan's
guidance is "build G1 + a curated G2 slice first; treat the rest as a backlog." These samples are
**consumers of the public package surface only** — no privileged internal access — which is itself the
proof that the documented consumption path works for real applications, not just a control showcase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A game sample runs as a live, deterministic interactive loop (Priority: P1) 🎯 MVP

A developer launches a game sample (Tetris) and plays it: pieces fall on a fixed step cadence, keyboard
input moves/rotates/drops them, completed rows clear and score updates, and the game ends on a defined
condition. The same sample also runs headlessly from a **seeded input script**, replaying the identical
sequence of moves to a repeatable frame/occupancy/score outcome with screenshot evidence.

**Why this priority**: This is the standalone MVP. One complete game proves the hardest new capability
the gallery never exercised — a *persistent interactive loop* with deterministic step timing and
keyboard input driving an MVU state model to a checkable outcome — and establishes the seeded-evidence
pattern every other sample reuses.

**Independent Test**: Run the game in headless evidence mode with a fixed seed and input script; confirm
the final board occupancy, cleared-row count, and score match the spec's stated outcome, that two runs
of the same seed are byte-identical, and that interactive mode (when a display/GL host is available)
renders and responds to live keyboard input.

**Acceptance Scenarios**:

1. **Given** a game sample and a fixed seed + seeded input script, **When** it runs headlessly to
   completion, **Then** it produces a repeatable evidence record (final grid occupancy / score / frame
   outcome + screenshot of the required surfaces) that satisfies the acceptance criteria carried by the
   sample's source spec.
2. **Given** the same seed and input script, **When** the headless run is repeated, **Then** the two
   evidence sets are byte-identical (no wall-clock, no randomness outside the seed).
3. **Given** a display/GL host, **When** the game runs interactively, **Then** keyboard input visibly
   advances game state (move/rotate/drop, score, game-over) through the input → MVU → repaint path.

---

### User Story 2 - A productivity sample exercises forms, lists, and inline-edited data (Priority: P1)

A user opens a productivity sample (Kanban board / Todo manager / Calendar scheduler) and works with it:
adding and editing items through forms with immediate validation, moving items between columns or dates,
toggling completion, and editing entries inline within lists and grids. The same sample runs headlessly
from a seeded input script to a repeatable data-state outcome with screenshot evidence.

**Why this priority**: It proves the *other* half of the framework that a controls gallery cannot — the
enterprise interaction patterns (forms, data grids, lists, validation, inline edit) that Workstreams F
and D explicitly target — composed into a real tool, and it does so through the same seeded-evidence
harness as US1.

**Independent Test**: Drive a seeded input script that creates, edits, validates, and reorders items in
a productivity sample; confirm the resulting data model (e.g. board column contents, task list with
completion flags, calendar entries) matches the spec's stated outcome and is byte-identical across two
seeded runs.

**Acceptance Scenarios**:

1. **Given** a productivity sample and a seeded input script that adds/edits/reorders items, **When** it
   runs headlessly, **Then** the resulting data state matches the source spec's stated outcome with
   screenshot evidence of the required surfaces.
2. **Given** an input that submits invalid form data, **When** validation runs, **Then** the sample
   surfaces the validation outcome and does not commit the invalid entry.
3. **Given** an inline-edited list or grid entry, **When** the edit is committed via seeded input,
   **Then** the entry's displayed value and underlying data state both reflect the edit.

---

### User Story 3 - The full curated slice builds and passes its own acceptance criteria (Priority: P1)

A maintainer builds all six curated samples (Tetris, Snake, Pong; Kanban, Todo, Calendar) and runs each
one's headless seeded-evidence mode. Every sample builds against the public package surface, runs to a
repeatable outcome, and satisfies the acceptance criteria carried by its source spec.

**Why this priority**: The breadth claim ("a representative slice") is only honest if every sample in
the slice actually builds and passes — not just the two MVP exemplars. This story is what turns two
proofs-of-concept into a curated, checked set.

**Independent Test**: Run the slice's evidence suite; confirm each of the six samples builds, produces a
non-empty evidence record, and meets its source-spec acceptance criteria; the suite fails if any sample
is missing, fails to build, or produces no evidence.

**Acceptance Scenarios**:

1. **Given** the curated slice of six samples, **When** the evidence suite runs, **Then** each sample
   builds and produces an evidence record satisfying its source-spec acceptance criteria.
2. **Given** any sample in the slice, **When** it is built, **Then** it compiles against the public
   package surface only (no privileged internal access).

---

### User Story 4 - Determinism and disclosed evidence make the samples CI-trustworthy (Priority: P2)

A continuous-integration job runs the curated slice headlessly. Every evidence record is reproducible
from its seed, discloses what the run does **not** prove (a non-empty "not authoritative for"), and on a
host without a display or GL the run degrades cleanly — skips or falls back with a disclosed reason and a
non-failing, non-hanging outcome — never fabricating a pass.

**Why this priority**: Determinism and disclosed evidence are what let CI depend on the samples and what
feed the perf corpus (Workstream B/G4). It hardens US1–US3 rather than adding new samples, so it is P2.

**Independent Test**: Run the evidence suite twice with the same seed and diff for byte-identity; inspect
each record for a non-empty disclosure; run on a host without display/GL and confirm a clean
skip/fallback (exit success, disclosed reason, no hang, no fake pass).

**Acceptance Scenarios**:

1. **Given** a fixed seed, **When** the curated slice's evidence suite runs twice, **Then** the two
   evidence sets are byte-identical.
2. **Given** any sample's evidence record, **When** it is produced, **Then** it carries a non-empty
   disclosure of what the run is not authoritative for.
3. **Given** a host without display/GL, **When** evidence is requested, **Then** every sample skips or
   falls back with a disclosed reason and a non-hanging success exit, never a fabricated result.

---

### User Story 5 - Coverage is maximized, measured, and the backlog is disclosed (Priority: P2)

A maintainer opens a coverage report for the curated slice showing which catalog controls and which
input modalities (keyboard, pointer, timing-driven step) each sample exercises, confirming the slice was
chosen to maximize distinct coverage. The same artifact discloses, honestly, which of the 22 archived
specs are **adopted** in this slice and which remain a **deferred backlog**.

**Why this priority**: It proves the curation was principled (coverage-maximizing, not arbitrary) and
keeps the "representative slice" claim honest by naming exactly what is and isn't built — but the samples
deliver value before the report exists, so it is P2.

**Independent Test**: Open the coverage/backlog report; confirm each curated sample lists the controls
and input modalities it exercises, that the six samples together cover keyboard + pointer + timing-driven
input and a documented set of catalog controls, and that every one of the 22 archived specs is marked
adopted or deferred with none unaccounted for.

**Acceptance Scenarios**:

1. **Given** the curated slice, **When** the coverage report is generated, **Then** it lists per sample
   the catalog controls and input modalities exercised, and the union spans keyboard, pointer, and
   timing-driven step input.
2. **Given** the 22 archived game/productivity specs, **When** the backlog disclosure is checked,
   **Then** every spec is marked **adopted** (in this slice) or **deferred** (backlog) with a stated
   reason, and none is unaccounted for.

---

### Edge Cases

- **No display / no GL host**: interactive mode is unavailable; headless seeded-evidence mode still runs
  (or skips cleanly with disclosure). No sample makes the CI gate depend on a display.
- **Game-over / win condition**: each game reaches a defined terminal state from its seeded script and
  records it; the loop never hangs or runs unbounded.
- **Invalid form input** (productivity): rejected with a surfaced validation outcome; the invalid entry
  is not committed to the data model.
- **Empty data state**: a productivity sample with no items renders a defined empty state, not a crash.
- **Seed/script mismatch**: a seeded script that cannot apply (e.g. an input with no valid target) fails
  loudly with a disclosed reason rather than silently diverging.
- **Identifier provenance**: source specs reference archived `FS.Skia.UI.*` identifiers; on adoption they
  are rebranded to `FS.GG.UI.*` and the adoption is recorded in provenance documentation.
- **Catalog/surface drift**: because samples consume the public surface only, a breaking surface change
  fails the sample build — an intentional signal, budgeted for.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST adopt a curated slice of **three games** (Tetris, Snake, Pong) and
  **three productivity apps** (Kanban board, Todo/task manager, Calendar scheduler), each as a runnable
  `FS.GG.UI.*`-consuming sample application.
- **FR-002**: Each sample MUST run in two modes: an **interactive** windowed mode and a **headless
  deterministic seeded-evidence** mode, consistent with the Controls Gallery (G1).
- **FR-003**: Each game sample MUST implement a persistent interactive loop with deterministic step
  timing and keyboard input driving an MVU state model to a defined terminal (game-over/win) state.
- **FR-004**: Each productivity sample MUST exercise forms with validation, data grids/lists, and inline
  edit, driving an MVU data model that reflects committed edits and rejects invalid input.
- **FR-005**: The headless evidence mode MUST accept an explicit seed and a seeded input script, and
  produce a repeatable per-sample evidence record (frame/occupancy/score or data-state outcome plus a
  screenshot of the required surfaces).
- **FR-006**: Headless evidence runs MUST be deterministic — seeded inputs only, no wall-clock and no
  randomness outside the seed — so the same seed yields byte-identical evidence across runs.
- **FR-007**: Every evidence record MUST disclose what the run is **not** authoritative for (a non-empty
  disclosure), consistent with the project's no-overclaim evidence rule.
- **FR-008**: When a display or GL host is unavailable, each sample MUST degrade and disclose — skip or
  fall back with a stated reason and a non-failing, non-hanging outcome — never fabricate a pass.
- **FR-009**: Each sample MUST satisfy the acceptance criteria carried by its source specification; the
  evidence suite MUST fail if any curated sample is missing, fails to build, or produces no evidence.
- **FR-010**: Every sample MUST build and run against the framework's **public package surface only**,
  with no privileged internal access.
- **FR-011**: The feature MUST provide a coverage report showing, per sample, the catalog controls and
  input modalities (keyboard, pointer, timing-driven step) exercised, demonstrating the slice maximizes
  distinct coverage; the curated set together MUST cover keyboard, pointer, and timing-driven input.
- **FR-012**: The feature MUST disclose an **adopted-vs-deferred** backlog covering all 22 archived
  game/productivity specs, with each spec marked adopted (in this slice) or deferred (backlog) with a
  stated reason and none unaccounted for.
- **FR-013**: Identifiers imported from the source specifications MUST be rebranded from `FS.Skia.UI.*`
  to `FS.GG.UI.*`, and the adoption MUST be recorded in provenance documentation.
- **FR-014**: The headless evidence mode MUST be the continuous-integration-facing path so no sample
  makes the required gate depend on a display or GL; interactive mode is GL-gated and advisory.
- **FR-015**: The samples MUST use only controls and themes that exist today; this feature MUST NOT
  introduce new controls, new themes, or design-specific kits, and MUST NOT change the public package
  surface (the samples are consumers, not contributors to product API).

### Key Entities

- **Game Sample**: One curated game (Tetris/Snake/Pong) — has a goal, a control scheme, an MVU state
  model, a deterministic step cadence, a terminal condition, and the seeded input script + expected
  outcome used for evidence.
- **Productivity Sample**: One curated app (Kanban/Todo/Calendar) — has a data model, forms with
  validation rules, list/grid/inline-edit interactions, and the seeded input script + expected
  data-state outcome used for evidence.
- **Seeded Input Script**: The deterministic per-sample sequence of inputs (keyboard/pointer/step ticks)
  with an explicit seed that replays to a repeatable outcome.
- **Sample Evidence Record**: The deterministic per-sample output of headless mode — seeded script, the
  resulting outcome (occupancy/score or data state), screenshot of required surfaces, and the non-empty
  "not authoritative for" disclosure.
- **Coverage / Backlog Report**: The artifact mapping each curated sample to the controls and input
  modalities it exercises, and marking all 22 archived specs adopted or deferred.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All six curated samples (three games, three productivity) build and run, each producing a
  non-empty evidence record that satisfies its source-spec acceptance criteria.
- **SC-002**: A seeded headless run of every curated sample produces byte-identical evidence across two
  consecutive runs with the same seed (100% reproducible).
- **SC-003**: Every per-sample evidence record carries a non-empty disclosure of what it does not prove;
  on a host without display/GL every sample skips or falls back with a disclosed reason and a non-hanging
  success outcome (zero fabricated passes, zero hangs).
- **SC-004**: The curated slice together exercises keyboard, pointer, and timing-driven step input, and
  the coverage report names the catalog controls each sample uses (100% of samples reported).
- **SC-005**: 100% of the 22 archived game/productivity specs are marked adopted or deferred in the
  backlog disclosure, with zero unaccounted-for specs.
- **SC-006**: Every sample builds using only the public package surface (no internal access),
  demonstrating the documented consumer path for real applications end to end.
- **SC-007**: Each game reaches its defined terminal state from its seeded script within a bounded number
  of steps (no unbounded or hanging loop); each productivity sample rejects invalid form input without
  committing it.

## Assumptions

- **Source material**: The per-sample goal, control scheme, state model, determinism/evidence
  requirements, and acceptance criteria derive from the archived FS-Skia-UI specs
  (`docs/testSpecs/Games/*.md`, `docs/testSpecs/Productivity/*.md`) referenced in the implementation
  plan §10. Those specs live in the archive (repo `EHotwagner/FS-Skia-UI`) and are **not present in this
  repository**; their content is adopted and rebranded here. Where a detail is unavailable from the
  archive, the plan's §10.1–§10.3 description and the local public surface are authoritative.
- **Curated slice**: The specific six samples (Tetris, Snake, Pong; Kanban, Todo, Calendar) are the
  plan's explicit G2 example set, chosen to maximize distinct control/input coverage in the smallest
  representative set. The remaining ~16 specs are a disclosed deferred backlog, not part of this feature.
- **Evidence harness**: The deterministic seeded-evidence mode, the no-overclaim disclosure convention,
  and the degrade-and-disclose behavior are reused from the Controls Gallery (G1, feature 123) and the
  harness conventions, not invented here.
- **Themes**: Samples use the existing themes (Light/Dark and their accents) only; this feature is
  independent of the Ant/Fluent/Material themes (Workstreams D/F) and the Ant restyle (G3).
- **Placement**: The samples live in the existing `samples/` tree as their own `FS.GG.UI.*`-consuming
  projects, outside the default test tier — consistent with the plan's §10.3 recommendation and the G1
  layout. The exact project layout (per-sample vs shared core) is a planning decision.
- **Modes**: Headless deterministic seeded-evidence mode is the CI path; interactive windowed mode is
  GL-gated and advisory, mirroring the rest of the harness and G1.

## Out of Scope

- The remaining ~16 archived game/productivity specs beyond the curated six (disclosed deferred backlog).
- The Controls Gallery showcase (Workstream G1, feature 123 — already shipped).
- The Ant-theme restyle and enterprise page templates (Workstream G3), which depend on Workstreams F/D.
- Wiring sample runs into the perf/harness corpus and CI advisory tier (Workstream G4).
- Any new controls, new themes, or design-specific kits.
- Changes to the public package surface (the samples are consumers, not contributors to product API).

## Dependencies

- The public package surface of the framework (controls, layout, input, viewer, Elmish) as consumed by a
  downstream application — the same surface the Controls Gallery (G1) consumes.
- The deterministic seeded-evidence harness and no-overclaim evidence conventions established by G1 and
  the rendering harness (Workstream A/B).
- The existing Light/Dark themes and the published control catalog.
