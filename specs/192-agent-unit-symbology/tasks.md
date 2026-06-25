---
description: "Task list for Agent-Driven Unit-Symbology Design System (M1–M5)"
---

# Tasks: Agent-Driven Unit-Symbology Design System

**Input**: Design documents from `/specs/192-agent-unit-symbology/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: INCLUDED — the constitution mandates Spec → FSI → Semantic Tests → Implementation (Principle I),
and the spec's Independent Tests + plan's Testing section call out golden / determinism / channel-presence /
codec-fidelity / render-smoke tests. Semantic tests exercise the **packed/public** surface and MUST fail
before implementation and pass after.

**Organization**: Tasks are grouped by user story (US1 P1 → US2 P2 → US3 P3) so each story is an independently
testable increment. Milestone mapping: **US1 = M1**, **US2 = M2 + M3**, **US3 = M4 + M5**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task serves (US1, US2, US3); Setup/Foundational/Polish carry no story label
- Every task names an exact file path

## Path Conventions

- Pure library: `src/Symbology/` · Render helper: `src/Symbology.Render/`
- Tests: `tests/Symbology.Tests/`, `tests/Symbology.Render.Tests/`
- Surface baselines: `readiness/surface-baselines/` · Skill trees: `.claude/skills/`, `.agents/skills/`, `template/product-skills/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the no-regression baseline, scaffold the two new libraries + two test projects, register them in the solution.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T001 runs **every** `*.Tests.fsproj` (solution +
> `Package.Tests` + `samples/**`) via the discovery-based runner so pre-existing reds are known up front and
> not mistaken for regressions at merge. Run it BEFORE scaffolding new projects so the recorded set reflects
> the untouched tree.

- [X] T001 Establish the no-regression baseline FIRST (before scaffolding): `dotnet fsi scripts/baseline-tests.fsx --out specs/192-agent-unit-symbology/readiness/baseline.md` — runs EVERY test project (globs `*.Tests.fsproj`) and records the full red/green set
- [X] T002 [P] Create `src/Symbology/Symbology.fsproj` mirroring `src/Canvas/Canvas.Lib.fsproj`: `IsPackable=true`, `PackageId=FS.GG.UI.Symbology`, `net10.0`, a **single** `ProjectReference ..\Scene\Scene.fsproj`, `InternalsVisibleTo FS.GG.UI.Symbology.Tests`, and ordered compile items `Symbology.fsi` then `Symbology.fs` (author minimal compiling `namespace FS.GG.UI.Symbology` stubs so the project builds)
- [X] T003 [P] Create `src/Symbology.Render/Symbology.Render.fsproj` mirroring the Canvas two-layer precedent: `IsPackable=true`, `PackageId=FS.GG.UI.Symbology.Render`, `net10.0`, `ProjectReference` to `..\Symbology\Symbology.fsproj` **and** `..\SkiaViewer\SkiaViewer.fsproj`, `InternalsVisibleTo FS.GG.UI.Symbology.Render.Tests`, compile items `Render.fsi` then `Render.fs` (minimal compiling stubs)
- [X] T004 [P] Create `tests/Symbology.Tests/Symbology.Tests.fsproj` mirroring `tests/Canvas.Tests/Canvas.Tests.fsproj` (xUnit; `ProjectReference ..\..\src\Symbology\Symbology.fsproj`; reference `tests/TestSupport` if Canvas.Tests does)
- [X] T005 [P] Create `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` (xUnit; `ProjectReference ..\..\src\Symbology.Render\Symbology.Render.fsproj`)
- [X] T006 Register all four new projects (`src/Symbology`, `src/Symbology.Render`, `tests/Symbology.Tests`, `tests/Symbology.Render.Tests`) under the matching folders in `FS.GG.Rendering.slnx` and confirm `dotnet build FS.GG.Rendering.slnx` succeeds with the stubs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Prove the public render bridge works in THIS checkout (M0 spike) and author the shared `.fsi` type seam every story builds on.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ M0 render-bridge spike = the early live smoke run (STANDING, do not omit).** Per plan.md §"Standing
> assumption", this greenfield feature's analogue of the live-smoke mandate is the M0 spike: a throwaway
> script must drive the **real** public render path and confirm a non-blank PNG with `ReferencePassed` BEFORE
> any production render code. Deterministic unit tests can pass while the render bridge is broken — treat the
> spike's evidence, not the source-report PoC narrative, as the confirmation.

- [X] T007 **M0 render-bridge spike (FIRST, blocks US2's render work)**: from a scratch dir write a throwaway ~20-line FSI script that builds a one-token gallery `Scene`, calls `(SceneCodec.export scene).CanonicalBytes` → `FS.GG.UI.SkiaViewer.ReferenceRendering.run { PackageBytes=…; OutputDirectory=…; OutputSize=…; Resources=[] }`, and asserts `Verdict = ReferencePassed` with a non-blank PNG at `ImagePath`. Record the result (PNG + verdict) under `specs/192-agent-unit-symbology/readiness/m0-spike-evidence.md`. If `ReferenceFailed` / `ReferenceEnvironmentLimited` / `ImagePath=None` — STOP and record the broken-bridge finding before M1.
- [X] T008 Author the shared TYPE seam in `src/Symbology/Symbology.fsi` (FSI-first, per `contracts/FS.GG.UI.Symbology.fsi`): `namespace FS.GG.UI.Symbology`, `open FS.GG.UI.Scene`, the DUs `Faction` / `Klass` / `Sigil` / `TokenState` / `Motion`, the `Token` record (all 13 channel fields, incl. `Charge`), and the `[<RequireQualifiedAccess>] module Symbology` with `val defaultToken: Token` only (per-story `val`s added in US1/US2). No top-level visibility modifiers in `.fs` (Principle II).
- [X] T009 Implement the type seam + `Symbology.defaultToken` in `src/Symbology/Symbology.fs` (centre, unit radius, `Neutral`, `Mobile`, default sigil, `Confirmed`, mid threat/charge/health, zero speed, no shield) so the project compiles against the `.fsi` from T008

**Checkpoint**: Render bridge confirmed live (T007); shared `Token`/enum vocabulary compiles — user stories can begin.

---

## Phase 3: User Story 1 - Encode a unit roster as legible vector symbols (Priority: P1) 🎯 MVP

**Goal**: A pure, deterministic `token : Token -> Scene` that renders the full fixed channel grammar, plus a reproducible `gallery` review board — the minimum viable symbol vocabulary (M1).

**Independent Test**: Author a fixed token set spanning all channels (2 factions, 3 classes, varying threat/health/speed/heading, a status flag), build a `gallery`, render it headlessly, and assert (a) every channel observably alters output, (b) identical inputs → identical `Scene` + identical canonical bytes, (c) symbols are distinguishable at the target on-board size.

### Tests for User Story 1 (write FIRST — must FAIL before implementation) ⚠️

- [X] T010 [P] [US1] Determinism / stable-identity test (SC-001) in `tests/Symbology.Tests/DeterminismTests.fs`: same `Token` evaluated twice ⇒ equal `Scene`, and `(SceneCodec.export (Symbology.token t)).CanonicalBytes` byte-equal across runs; a `gallery` package identity is stable across runs
- [X] T011 [P] [US1] Channel-presence tests (SC-002) in `tests/Symbology.Tests/ChannelPresenceTests.fs`: for EACH channel in the data-model table (faction hue, class silhouette, sigil, state dash, threat stroke-width, charge interior-gradient, speed tail-beads, health belly-arc, heading rotation, shield mount), two `Token`s differing in only that field produce observably different output (readback via `fs-gg-diagnostics` at the target board size) and differ in only that channel
- [X] T012 [P] [US1] Codec-fidelity test (SC-003) in `tests/Symbology.Tests/CodecFidelityTests.fs`: export→import→raster of a `token` scene preserves Path geometry, the **radial** gradient `token` emits (with linear/sweep asserted as a codec-capability guard), `Dash` effects, `Arc`, and stroke width/cap/join with no loss
- [X] T013 [P] [US1] Zero-area placeholder test (FR-020) in `tests/Symbology.Tests/PlaceholderTests.fs`: a `Token` with `R <= 0` (or otherwise no drawable area) renders a visible placeholder, not a blank/crash
- [X] T014 [P] [US1] Gallery layout + legibility-at-size test (SC-007) in `tests/Symbology.Tests/GalleryTests.fs`: `gallery cols spacing tokens` lays out a reproducible grid; each rendered symbol is non-blank and faction + class are separable at the target on-board size (readback evidence)

### Implementation for User Story 1

- [X] T015 [US1] Extend `src/Symbology/Symbology.fsi`: add `val token: token: Token -> Scene` and `val gallery: cols: int -> spacing: float -> tokens: Token list -> Scene` to `module Symbology` (per `contracts/FS.GG.UI.Symbology.fsi`)
- [X] T016 [US1] Implement the silhouette + sigil tables and non-public helpers (`pathOf`, `place`, `strokePaint`, `lerpColor`) in `src/Symbology/Symbology.fs` (no top-level visibility modifiers — drive non-publicness via the `.fsi`)
- [X] T017 [US1] Implement the `token` body in `src/Symbology/Symbology.fs` rendering every FR-004 channel: stroke hue→faction, silhouette+centre sigil→class+identity, whole-body rotation→heading, stroke width→threat, interior `RadialGradient`→charge, screen-aligned belly `arc` length+hue→health, tail bead run→speed, stroke `Dash`→state, corner mount→shield; `R<=0` ⇒ visible placeholder (FR-020); gauges stay screen-aligned under heading rotation
- [X] T018 [US1] Implement the faction saturated palette and the `fs-gg-ant-design` status-token state colours in `src/Symbology/Symbology.fs` so faction hue and state semantics never share the hue channel (FR-019)
- [X] T019 [US1] Implement `gallery` (reproducible grid, pure layout) in `src/Symbology/Symbology.fs`
- [X] T020 [US1] Pin the new public surface: generate/update `readiness/surface-baselines/FS.GG.UI.Symbology.txt` and confirm the surface gate passes with the US1 surface (SC-004); existing core baselines unchanged
- [X] T021 [US1] Run `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and confirm T010–T014 now pass (fail-before/pass-after)

**Checkpoint**: The pure symbol vocabulary + gallery board is golden, deterministic, codec-faithful, and legible at size — US1 is independently shippable (MVP).

---

## Phase 4: User Story 2 - Animate symbols and see them as an image headlessly (Priority: P2)

**Goal**: Deterministic motion overlays (`animate`) + a `filmstrip` board (M2) **and** the public, scriptable, fail-loud Scene→PNG render bridge `FS.GG.UI.Symbology.Render` (M3).

**Independent Test**: Build a motion filmstrip and a multi-symbol board; render both through the public `Render.toPng`; assert (a) each filmstrip frame is byte-reproducible from its phase schedule with no wall-clock read, (b) produced images are non-blank and pass the verdict, (c) a non-passing render raises a diagnostic-bearing failure instead of a blank image, reaching no internal entry point.

### Tests for User Story 2 (write FIRST — must FAIL before implementation) ⚠️

- [X] T022 [P] [US2] Motion-overlay purity goldens in `tests/Symbology.Tests/MotionTests.fs`: for each rhythm (`Idle/Pulse/Spin/Blink/Damage/Moving`), `animate m t phase` overlays the rhythm on the base symbol and is pure in `(m, t, phase)` — identical inputs ⇒ identical `Scene`
- [X] T023 [P] [US2] Filmstrip reproducibility test (SC-006) in `tests/Symbology.Tests/FilmstripTests.fs`: `filmstrip samples entries` rendered twice ⇒ byte-identical frames; phase comes from the schedule alone, no wall-clock read
- [X] T024 [P] [US2] Render pass-path test (SC-008) in `tests/Symbology.Render.Tests/RenderPassTests.fs`: `Render.toPng size (Symbology.gallery …) dir` returns a path to a non-blank PNG with `ReferencePassed`, reaching no internal-only entry; assert the returned path is content-addressable / stable for an identical scene (FR-013)
- [X] T025 [P] [US2] Render fail-loud test (FR-012) in `tests/Symbology.Render.Tests/RenderFailLoudTests.fs`: a scene/verdict that does not pass (or `ImagePath = None`) ⇒ `Render.toPng` raises carrying the joined `Diagnostics`, never returns a blank image as success

### Implementation for User Story 2

- [X] T026 [US2] Extend `src/Symbology/Symbology.fsi`: add `val animate: motion: Motion -> token: Token -> phase: float -> Scene` and `val filmstrip: samples: int -> entries: (Motion * Token) list -> Scene` to `module Symbology`
- [X] T027 [US2] Implement `animate` in `src/Symbology/Symbology.fs`: the six rhythms overlaid on the base symbol, phase caller-owned (no wall-clock), gauges stay screen-aligned; one active motion per symbol
- [X] T028 [US2] Implement `filmstrip` in `src/Symbology/Symbology.fs`: motion sampled across `samples` phase steps from a deterministic schedule, byte-reproducible (FR-009/SC-006)
- [X] T029 [US2] Update `readiness/surface-baselines/FS.GG.UI.Symbology.txt` for the new `animate`/`filmstrip` vals; confirm core baselines still show zero drift
- [X] T030 [US2] Author `src/Symbology.Render/Render.fsi` (FSI-first, per `contracts/FS.GG.UI.Symbology.Render.fsi`): `namespace FS.GG.UI.Symbology.Render`, `[<RequireQualifiedAccess>] module Render` with `val toPng: size: Size -> scene: Scene -> dir: string -> string`
- [X] T031 [US2] Implement `Render.toPng` in `src/Symbology.Render/Render.fs`: `(SceneCodec.export scene).CanonicalBytes` → `ReferenceRendering.run { PackageBytes=…; OutputDirectory=dir; OutputSize=size; Resources=[] }`; return the path ONLY when `Verdict = ReferencePassed` AND `ImagePath = Some p`; otherwise RAISE with joined `Diagnostics` (covers `ReferenceFailed`, `ReferenceEnvironmentLimited`, and `ImagePath = None`) — never a blank success (FR-012, Principle VI)
- [X] T032 [US2] Pin `readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt` and confirm the surface gate shows zero drift on `Controls`/`Canvas`/`Scene`/`SkiaViewer` (SC-004)
- [X] T033 [US2] Run `dotnet test tests/Symbology.Tests` and `dotnet test tests/Symbology.Render.Tests` and confirm T022–T025 pass (fail-before/pass-after)

**Checkpoint**: Motion + filmstrip are deterministic and the public Scene→PNG bridge is scriptable + fail-loud — a designer/agent can now see a board headlessly. US1 + US2 both work independently.

---

## Phase 5: User Story 3 - Run the agent design loop end-to-end with provenance (Priority: P3)

**Goal**: Author the orchestrating `fs-gg-symbology` skill (M4) and prove the whole stack with an end-to-end loop dry-run that writes provenance and pins a golden board (M5).

**Independent Test**: Drive the loop on a 6–10 unit roster through ≥2 feedback rounds where only the `ChannelMap` changes; assert (a) the loop follows the fixed intake→map→render→critique→review→tweak→approve protocol, (b) every iteration writes a timestamped board + mapping snapshot, (c) on approval a final symbol-set module + rationale is emitted and a golden board pinned; confirm the skill is present and consistent across all three trees.

> **Note — US3 verification model (intended exception to the fail-first header).** Unlike US1/US2, US3
> ships no fail-first unit test: its verification is the **skill-parity gate** (T037, SC-005) plus the
> **dry-run audit-trail assertions** (T038–T040, SC-009). This is the correct discipline for skill
> authoring + a human-in-the-loop agent loop (no in-code `Model`/`update` surface to unit-test), and is
> the deliberate exception to the "Tests: INCLUDED / must fail before implementation" header above.

### Implementation for User Story 3

- [X] T034 [P] [US3] Author `.claude/skills/fs-gg-symbology/SKILL.md` encoding (per `contracts/agent-loop-protocol.md`): the fixed grammar + channel table, the legibility rules (assign-by-urgency, redundancy on critical state, one active motion, never critical state on dash alone, no faction/state hue collision), the library + `Render.toPng` API, the grammar-vs-mapping pattern, the FSI recipe (quickstart §M4), and the feedback protocol (FR-014)
- [X] T035 [P] [US3] Add the reference `.fsx` template under `.claude/skills/fs-gg-symbology/` (roster → `ChannelMap : 'stats -> Token` → `Symbology.gallery` → `Render.toPng` → read PNG back), matching the quickstart FSI recipe
- [X] T036 [US3] Mirror the authored skill (SKILL.md + reference `.fsx`) byte-for-byte to `.agents/skills/fs-gg-symbology/` and `template/product-skills/fs-gg-symbology/` (R4/G4)
- [X] T037 [US3] Run the skill-parity gate and confirm green (SC-005): `dotnet fsi scripts/check-agent-skill-parity.fsx --out /tmp/fs-gg-skill-parity --report /tmp/fs-gg-skill-parity/report.md --summary-json /tmp/fs-gg-skill-parity/summary.json --fail-on high`
- [X] T038 [US3] M5 dry-run — drive the loop on a real 6–10 unit roster across ≥2 feedback rounds where ONLY the `ChannelMap`/`Token` params change between rounds (never the grammar), following the fixed protocol; self-critique each board against the legibility checklist at the target size
- [X] T039 [US3] Write per-iteration provenance (FR-017) under a working directory `specs/192-agent-unit-symbology/readiness/dry-run/`: each iteration emits a *timestamped board PNG* (via `Render.toPng`) + a *mapping snapshot* (the `ChannelMap`/`Token` set used); the workflow stamps filenames (library/render read no clock)
- [X] T040 [US3] On approval (FR-018), emit the final symbol-set module (pure drawing-producing functions) + a design rationale (channel assignments + rejected alternatives + legibility notes) and pin a golden board with a stable `SceneCodec` identity under the dry-run dir

**Checkpoint**: A complete render→tweak→approve audit trail exists, the skill is parity-green across three trees, and the stack is proven end-to-end on a real roster.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full quickstart, confirm no regressions, pack the local feed, and record the gate decisions.

- [X] T041 Run the full quickstart validation (`specs/192-agent-unit-symbology/quickstart.md` M0–M5 coverage map) and confirm every success criterion (SC-001 … SC-009) is demonstrated
- [X] T042 Re-run the no-regression baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T001 — confirm zero new reds across the solution + `Package.Tests` + `samples/**` and zero core-surface drift (SC-004)
- [X] T043 [P] Pack the two new packages to the local feed `~/.local/share/nuget-local/` (`FS.GG.UI.Symbology`, `FS.GG.UI.Symbology.Render`) and confirm they restore
- [X] T044 [P] Record the G1–G4 gate decisions into the source report §11 (`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`) per research.md "Gate decision record"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001 baseline runs FIRST (before scaffolding).
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** T007 (M0 spike) gates US2's render work specifically and must pass before any production render code.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 (M1) is the MVP. US2 depends on the US1 `token`/`gallery` surface (it renders galleries and extends the same `.fsi`). US3 depends on US1+US2 (the loop drives `gallery` + `Render.toPng`).
- **Polish (Phase 6)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational. No dependency on other stories. Independently testable (M1).
- **US2 (P2)**: Builds on the US1 surface (`token`/`gallery`) and the M0-confirmed render bridge; independently testable once US1 exists.
- **US3 (P3)**: Depends on US1 + US2 (drives the public library + render path end-to-end).

### Within Each User Story

- Tests are written and MUST FAIL before implementation (Principle I).
- `.fsi` surface extension before `.fs` implementation (Principle I/II).
- Implementation before surface-baseline pinning.
- Story complete + checkpoint green before moving to the next priority.

### Parallel Opportunities

- Setup: T002–T005 (four distinct project files) run in parallel after T001.
- US1 tests T010–T014 (distinct test files) run in parallel; they must all fail before T015 begins.
- US2 tests T022–T025 (distinct test files) run in parallel.
- US3 T034–T035 (skill authoring) run in parallel; T036 mirrors after both.
- Polish T043–T044 run in parallel.

---

## Parallel Example: User Story 1 tests

```bash
# Author these five failing tests together (distinct files, no shared state):
Task: "Determinism / identity test in tests/Symbology.Tests/DeterminismTests.fs"
Task: "Channel-presence tests in tests/Symbology.Tests/ChannelPresenceTests.fs"
Task: "Codec-fidelity test in tests/Symbology.Tests/CodecFidelityTests.fs"
Task: "Zero-area placeholder test in tests/Symbology.Tests/PlaceholderTests.fs"
Task: "Gallery layout + legibility-at-size test in tests/Symbology.Tests/GalleryTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup (baseline + scaffold + slnx).
2. Phase 2 Foundational — **M0 render-bridge spike (T007) first**, then the shared `.fsi` type seam.
3. Phase 3 US1 (M1): the pure `token` + `gallery`, golden/determinism/channel-presence/codec/placeholder green, baseline pinned.
4. **STOP and VALIDATE**: US1 is independently shippable — a legible, goldenable symbol library.

### Incremental Delivery

1. Setup + Foundational → render bridge confirmed live, vocabulary compiles.
2. US1 (M1) → pure symbol library → MVP.
3. US2 (M2 + M3) → motion/filmstrip + public fail-loud render bridge → designer can see boards.
4. US3 (M4 + M5) → orchestrating skill + end-to-end dry-run with provenance → auditable design process.
5. Polish → full quickstart validation, no-regression, pack, gate record.

---

## Notes

- [P] = different files, no dependency on incomplete tasks.
- The `.fsi` grows per story (US1 adds `token`/`gallery` vals; US2 adds `animate`/`filmstrip`) — F# requires every `.fsi` val to have a `.fs` implementation, so the surface is extended where the implementation lands, and the baseline is re-pinned each story (T020, T029, T032).
- Determinism is the hard constraint: no wall-clock/IO in `token`/`animate`/`gallery`/`filmstrip`; phase is caller-owned; the workflow (not library code) stamps provenance filenames.
- The render bridge fails loud on any non-`ReferencePassed` verdict or `None` image — never a blank success.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
