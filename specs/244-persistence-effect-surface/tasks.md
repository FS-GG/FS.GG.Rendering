---
description: "Task list for feature 244 — persistence (save/load) effect surface + fs-gg-persistence product skill"
---

# Tasks: Persistence (save/load) effect surface + fs-gg-persistence product skill

**Input**: Design documents from `specs/244-persistence-effect-surface/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/Persistence.fsi, quickstart.md

**Tests**: INCLUDED — the spec mandates semantic tests (FR-015) and the constitution requires them
(Principles I & V). Test tasks are written to FAIL before the implementation that makes them pass.

**Organization**: Grouped by user story (US1 pure request surface, US2 headless record-only
interpreter, US3 skill materialization). US1 and US2 share `src/Canvas/Persistence.fsi` /
`Persistence.fs` / `tests/Canvas.Tests/PersistenceTests.fs`, so they are ordered, not parallel,
against each other.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Wire the new files into the build and capture the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** test project via
> the discovery runner, not a hand-picked subset — the solution deliberately omits
> `tests/Package.Tests` (release-only; owns the public-surface gate that this Tier-1 change trips)
> and the `samples/**/*.Tests` feed consumers, which is exactly where Feature 175's surprises hid.

- [X] T001 Add `Persistence.fsi` then `Persistence.fs` (empty stubs) to the compile `<ItemGroup>` in `src/Canvas/Canvas.Lib.fsproj`, ordered after `Audio.fs`, and create an empty `tests/Canvas.Tests/PersistenceTests.fs` wired into `tests/Canvas.Tests/Canvas.Tests.fsproj`; confirm the solution still builds.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/244-persistence-effect-surface/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set so pre-existing reds are known now, not discovered at merge).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Lock the public surface (`.fsi`-first, Principle I) and observe the **real scaffold**
before building on it.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Live-observation task (STANDING requirement, adapted).** This feature is greenfield, so there
> is no root-cause map / running-app defect to confirm. Per the plan's live-verification note, the
> equivalent honest-observation risk is **scaffold materialization**: deterministic manifest tests
> can be green while `dotnet new` still misplaces or leaks the skill (the lesson of Features 175,
> 228 & 243). T005 pulls that real instantiation forward as the pre-change baseline; do not rely on
> unit tests alone for US3.

- [X] T003 Finalize the public surface `src/Canvas/Persistence.fsi` from `specs/244-persistence-effect-surface/contracts/Persistence.fsi` (DUs `PersistenceEffect`/`SaveSlot`/`SavePayload`, record `SaveEnvelope`, record `PersistenceEvidence`, module `Persistence` with `minVersion`/`clampVersion`/smart-ctors/`record`/`interpret`). No `.fs` body yet.
- [X] T004 FSI shape check (Principle I): build Canvas, `#r` the dll in `dotnet fsi`, and exercise the drafted surface per quickstart §1; save the transcript to `specs/244-persistence-effect-surface/readiness/fsi-sketch.md`. Adjust `Persistence.fsi` if the shape is awkward before any `.fs` exists.
- [X] T005 **Live scaffold baseline**: run `dotnet new fs-gg-ui -o /tmp/244-base-game --profile game` and `--profile app` on the CURRENT (pre-persistence) template; record the skill-root listing to `specs/244-persistence-effect-surface/readiness/scaffold-baseline.md` so the post-wiring diff in US3 (T016) is trustworthy.

**Checkpoint**: Surface signed off in FSI; current scaffold state captured. User stories can begin.

---

## Phase 3: User Story 1 — Request a save/load from pure product code (Priority: P1) 🎯 MVP

**Goal**: A product author emits `PersistenceEffect` values from pure `update` for game events
(checkpoint save, continue-game load, erase-save delete), with zero IO.

**Independent Test**: Drive a tiny pure model through a set of game events; assert the exact sequence
of emitted `PersistenceEffect` values (slot, version, opaque payload); confirm no IO in `update`.

### Tests for User Story 1 ⚠️ (write first, must FAIL)

- [X] T006 [US1] Semantic tests in `tests/Canvas.Tests/PersistenceTests.fs`: a pure model whose `update` maps events → `PersistenceEffect` requests; assert the exact emitted values (`Save`/`Load`/`DeleteSlot`), assert the `saveEnvelope` smart ctor clamps a negative version to `minVersion` and carries the opaque payload verbatim, assert `update` performs no IO. Test names carry no `Synthetic` token (evidence is real). Confirm RED.

### Implementation for User Story 1

- [X] T007 [US1] Implement `src/Canvas/Persistence.fs` — `SaveSlot`/`SavePayload`/`SaveEnvelope`/`PersistenceEffect` types, `minVersion`/`clampVersion`, and smart constructors `saveEnvelope`/`save`/`load`/`deleteSlot` against `Persistence.fsi`. Make T006 GREEN. No access modifiers in `.fs` (Principle II).

**Checkpoint**: US1 independently functional — pure code can request save/load/delete.

---

## Phase 4: User Story 2 — Interpret persistence requests headlessly, safely (Priority: P1)

**Goal**: The record-only interpreter folds requested effects into ordered `PersistenceEvidence`
with no filesystem, never blocking or throwing, carrying opaque payloads verbatim.

**Independent Test**: Feed a known request sequence to `Persistence.interpret`; assert recorded
order, normalized versions, unknown-slot `Load`/`DeleteSlot` recorded (no error), payload preserved,
and no exception — with no writable save location.

### Tests for User Story 2 ⚠️ (write first, must FAIL)

- [X] T008 [US2] Extend `tests/Canvas.Tests/PersistenceTests.fs`: assert `Persistence.emptyEvidence`, `record`, and `interpret [..]` produce `PersistenceEvidence.Requested` in dispatch order with normalized `Save` versions and payloads carried verbatim; assert an unknown-slot `Load`/`DeleteSlot` is recorded as a well-defined no-op-class request and never throws (Principle VI). Confirm RED. (Same file as T006 → runs after US1, not parallel.)

### Implementation for User Story 2

- [X] T009 [US2] Implement `PersistenceEvidence` + `Persistence.emptyEvidence`/`record`/`interpret` (pure fold, total, normalizes carried `Save` versions, carries payload verbatim) in `src/Canvas/Persistence.fs`. Make T008 GREEN.

**Checkpoint**: US1 + US2 complete — the pure request surface and its headless-safe evidence boundary both work; this is the shippable library MVP.

---

## Phase 5: User Story 3 — Discover & apply persistence via the fs-gg-persistence skill (Priority: P2)

**Goal**: Scaffolding a `game`/`sample-pack` product materializes a coherent `fs-gg-persistence`
skill that cites the real surface; non-persistence profiles get nothing.

**Independent Test**: Instantiate `--profile game` (skill present) and `--profile app` (absent);
confirm manifest/template/parity coherence and that references resolve to shipped API.

### Implementation for User Story 3

- [X] T010 [US3] Regenerate the surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`; commit the new `readiness/surface-baselines/FS.GG.UI.Canvas.txt` rows (`Persistence`, `PersistenceEffect`, `SaveEnvelope`, `SaveSlot`, `SavePayload`, `PersistenceEvidence`). Depends on US1+US2 surface existing.
- [ ] T011 [P] [US3] Author `template/product-skills/fs-gg-persistence/SKILL.md` mirroring `fs-gg-audio`/`fs-gg-game-core` (front-matter `name`/`description`; teach the request → record-only-interpret pattern and the versioned-envelope recipe — serialize the pure `Model`, stamp a version, keep I/O at the host, reuse the game-core seeded state as the snapshot target; cite the shipped `Persistence` surface via `docs/api-surface/Canvas/Persistence.fsi`; consumer vocabulary only — no framework-process terms).
- [ ] T012 [P] [US3] Add the wrapper pair: `.agents/skills/fs-gg-product-persistence/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-persistence/SKILL.md` (Claude-active), each a thin pointer to the canonical body (byte-identical except the Codex/Claude token), matching the other `fs-gg-product-*` wrappers.
- [ ] T013 [US3] Add the `fs-gg-persistence` copy block to `.template.config/template.json`: `condition: "(profile == \"game\" || profile == \"sample-pack\")"`, `source: "template/product-skills/fs-gg-persistence/"`, `target: ".agents/skills/fs-gg-persistence/"`, `copyOnly: ["**/*"]`.
- [ ] T014 [US3] Regenerate the skill manifest: `dotnet fsi scripts/generate-skill-manifest.fsx`; commit the new `fs-gg-persistence` row (`sha256`, `materializes-when: "profile in [game, sample-pack]"`, `supplied-by`, `resolvablePath`). Depends on T011. Confirm the predicate matches T013's condition exactly.
- [ ] T015 [US3] Update the Canvas gate comment in `template/base/src/Product/Product.fsproj` (lines ~21-24) to note the `Persistence` module ships via the existing `FS.GG.UI.Canvas` game/sample-pack gate — no new PackageReference added (FR-010 satisfied by the existing gate).
- [ ] T016 [US3] **Live scaffold check**: `dotnet new fs-gg-ui -o /tmp/244-game --profile game` (assert `.agents/skills/fs-gg-persistence/SKILL.md` present) and `--profile app` (assert absent); diff against T005's baseline to confirm no leak into non-persistence profiles and that the skill body byte-matches the manifest sha256. Record to `specs/244-persistence-effect-surface/readiness/scaffold-after.md`.
- [ ] T017 [US3] Run the coherence gates: `dotnet test tests/Package.Tests/Package.Tests.fsproj` (Feature231SkillManifestTests + SurfaceAreaTests) and the SkillParity harness (`tools/Rendering.Harness`); confirm manifest/template predicate parity, wrapper parity across `.agents/` + `.claude/`, no dangling API references, and no vocabulary leak (SC-004).

**Checkpoint**: All three stories functional; scaffold verified against the real template.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T018 [P] Ship the doc copy `template/base/docs/api-surface/Canvas/Persistence.fsi` alongside the existing `Loop.fsi`/`Rng.fsi`/`Audio.fsi` doc copies (kept in sync with the shipped `src/Canvas/Persistence.fsi`).
- [ ] T019 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` convention into `specs/244-persistence-effect-surface/feedback/` (process friction, generalizable-code candidates).
- [ ] T020 Run the full `quickstart.md` validation end-to-end (§1–§5) and confirm every expected outcome; note any `environment-limited` substitutions.
- [ ] T021 On merge readiness: comment implementation status on FS-GG/FS.GG.Rendering#93 and move its Coordination board item to `In review` → `Done`.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)**: no deps — start immediately.
- **Foundational (P2)**: after Setup — BLOCKS all user stories (surface + live baseline).
- **US1 (P3)**: after Foundational.
- **US2 (P4)**: after US1 (shares `Persistence.fsi`/`Persistence.fs`/`PersistenceTests.fs`).
- **US3 (P5)**: after US1+US2 (skill must cite the shipped surface; baseline regen needs the types).
- **Polish (P6)**: after US3.

### Within / across stories

- Tests before implementation (T006→T007, T008→T009), each RED before GREEN.
- US1 and US2 are **not** parallel with each other (same files).
- T010–T017 mostly sequential; **T011 and T012 are [P]** (distinct skill files), but T014 (manifest regen) depends on T011, and T016 (live check) depends on T013+T014.

### Parallel opportunities

- Setup: none meaningful (T001 gates T002).
- US3: T011 ∥ T012 (canonical skill body vs. wrapper pair — different files).
- Polish: T018 ∥ T019.

## Parallel example: User Story 3

```bash
# After the surface exists (US1+US2) and the baseline is regenerated (T010):
Task: "Author template/product-skills/fs-gg-persistence/SKILL.md (T011)"
Task: "Add .agents + .claude fs-gg-product-persistence wrapper pair (T012)"
# then T013 (template block) → T014 (manifest regen) → T016 (live scaffold check)
```

## Implementation Strategy

### MVP (library first)

1. Phase 1 Setup → Phase 2 Foundational (`.fsi` signed off in FSI + live scaffold baseline).
2. US1 + US2 → the pure persistence request surface and its headless-safe evidence boundary. **STOP
   & VALIDATE**: `dotnet test tests/Canvas.Tests` green; the game default can now *request* save/load.
3. US3 → deliver the discoverable skill and prove the real scaffold materializes it correctly.
4. Polish → docs, feedback, full quickstart, issue/board close.

### Notes

- Commit after each task or logical group; keep the 38 pre-existing `packages.lock.json` drifts
  **unstaged** (known local hash drift — do not commit).
- The real file-backed backend (SkiaViewer host) and its load-*result* `Msg` are out of scope — an
  explicit deferral behind the seam (plan.md "Deferred"). Do not add a hollow SkiaViewer arm.
- This is a **Tier 1** change: `.fsi` + surface baseline + tests + docs are all required for done.
