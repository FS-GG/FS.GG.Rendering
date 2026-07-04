---
description: "Task list for feature 243 — audio effect surface + fs-gg-audio product skill"
---

# Tasks: Audio effect surface + fs-gg-audio product skill

**Input**: Design documents from `specs/243-audio-effect-surface/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/Audio.fsi, quickstart.md

**Tests**: INCLUDED — the spec mandates semantic tests (FR-014) and the constitution requires
them (Principles I & V). Test tasks are written to FAIL before the implementation that makes them
pass.

**Organization**: Grouped by user story (US1 pure request surface, US2 headless record-only
interpreter, US3 skill materialization). US1 and US2 share `src/Canvas/Audio.fsi` / `Audio.fs` /
`tests/Canvas.Tests/AudioTests.fs`, so they are ordered, not parallel, against each other.

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

- [ ] T001 Add `Audio.fsi` then `Audio.fs` (empty stubs) to the compile `<ItemGroup>` in `src/Canvas/Canvas.Lib.fsproj`, ordered after `Rng`/`FixedStep`, and create an empty `tests/Canvas.Tests/AudioTests.fs` wired into `tests/Canvas.Tests/Canvas.Tests.fsproj`; confirm the solution still builds.
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/243-audio-effect-surface/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set so pre-existing reds are known now, not discovered at merge).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Lock the public surface (`.fsi`-first, Principle I) and observe the **real scaffold**
before building on it.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Live-observation task (STANDING requirement, adapted).** This feature is greenfield, so there
> is no root-cause map / running-app defect to confirm. Per the plan's live-verification note, the
> equivalent honest-observation risk is **scaffold materialization**: deterministic manifest tests
> can be green while `dotnet new` still misplaces or leaks the skill (the lesson of Features 175 &
> 228). T005 pulls that real instantiation forward as the pre-change baseline; do not rely on unit
> tests alone for US3.

- [ ] T003 Finalize the public surface `src/Canvas/Audio.fsi` from `specs/243-audio-effect-surface/contracts/Audio.fsi` (DUs `AudioEffect`/`SoundId`/`TrackId`, record `AudioEvidence`, module `Audio` with clamp/smart-ctors/`record`/`interpret`). No `.fs` body yet.
- [ ] T004 FSI shape check (Principle I): build Canvas, `#r` the dll in `dotnet fsi`, and exercise the drafted surface per quickstart §1; save the transcript to `specs/243-audio-effect-surface/readiness/fsi-sketch.md`. Adjust `Audio.fsi` if the shape is awkward before any `.fs` exists.
- [ ] T005 **Live scaffold baseline**: run `dotnet new fs-gg-ui -o /tmp/243-base-game --profile game` and `--profile app` on the CURRENT (pre-audio) template; record the skill-root listing to `specs/243-audio-effect-surface/readiness/scaffold-baseline.md` so the post-wiring diff in US3 (T016) is trustworthy.

**Checkpoint**: Surface signed off in FSI; current scaffold state captured. User stories can begin.

---

## Phase 3: User Story 1 — Request a sound from pure product code (Priority: P1) 🎯 MVP

**Goal**: A product author emits `AudioEffect` values from pure `update` for game events, with zero IO.

**Independent Test**: Drive a tiny pure model through a set of game events; assert the exact
sequence of emitted `AudioEffect` values; confirm no IO in `update`.

### Tests for User Story 1 ⚠️ (write first, must FAIL)

- [ ] T006 [US1] Semantic tests in `tests/Canvas.Tests/AudioTests.fs`: a pure model whose `update` maps events → `AudioEffect` requests; assert the exact emitted values (`PlaySfx`/`PlayMusic`/`StopMusic`/`SetMasterVolume`), assert volume clamping via smart ctors, assert `update` performs no IO. Test names carry no `Synthetic` token (evidence is real). Confirm RED.

### Implementation for User Story 1

- [ ] T007 [US1] Implement `src/Canvas/Audio.fs` — `SoundId`/`TrackId`/`AudioEffect` types, `minVolume`/`maxVolume`/`clampVolume`, and smart constructors `playSfx`/`playMusic`/`stopMusic`/`setMasterVolume` against `Audio.fsi`. Make T006 GREEN. No access modifiers in `.fs` (Principle II).

**Checkpoint**: US1 independently functional — pure code can request sound.

---

## Phase 4: User Story 2 — Interpret audio requests headlessly, safely (Priority: P1)

**Goal**: The record-only interpreter folds requested effects into ordered `AudioEvidence` with no
device, never blocking or throwing.

**Independent Test**: Feed a known request sequence to `Audio.interpret`; assert recorded order,
clamped volumes, `StopMusic`-when-idle no-op, and no exception — with no audio hardware.

### Tests for User Story 2 ⚠️ (write first, must FAIL)

- [ ] T008 [US2] Extend `tests/Canvas.Tests/AudioTests.fs`: assert `Audio.emptyEvidence`, `record`, and `interpret [..]` produce `AudioEvidence.Requested` in dispatch order with normalized volumes; assert `StopMusic` with nothing playing is a well-defined no-op and out-of-range volume never throws (Principle VI). Confirm RED. (Same file as T006 → runs after US1, not parallel.)

### Implementation for User Story 2

- [ ] T009 [US2] Implement `AudioEvidence` + `Audio.emptyEvidence`/`record`/`interpret` (pure fold, total, clamps carried volumes) in `src/Canvas/Audio.fs`. Make T008 GREEN.

**Checkpoint**: US1 + US2 complete — the pure request surface and its headless-safe evidence boundary both work; this is the shippable library MVP.

---

## Phase 5: User Story 3 — Discover & apply audio via the fs-gg-audio skill (Priority: P2)

**Goal**: Scaffolding a `game`/`sample-pack` product materializes a coherent `fs-gg-audio` skill
that cites the real surface; non-audio profiles get nothing.

**Independent Test**: Instantiate `--profile game` (skill present) and `--profile app` (absent);
confirm manifest/template/parity coherence and that references resolve to shipped API.

### Implementation for User Story 3

- [ ] T010 [US3] Regenerate the surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`; commit the new `readiness/surface-baselines/FS.GG.UI.Canvas.txt` rows (`Audio`, `AudioEffect`, `AudioEvidence`, `SoundId`, `TrackId`). Depends on US1+US2 surface existing.
- [ ] T011 [P] [US3] Author `template/product-skills/fs-gg-audio/SKILL.md` mirroring `fs-gg-game-core` (front-matter `name`/`description`; teach the request → record-only-interpret pattern; cite the shipped `Audio` surface via `docs/api-surface/Canvas/Audio.fsi`; consumer vocabulary only — no framework-process terms).
- [ ] T012 [P] [US3] Add the wrapper pair: `.agents/skills/fs-gg-product-audio/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-audio/SKILL.md` (Claude-active), each a thin pointer to the canonical body (byte-identical except the Codex/Claude token), matching the other ten `fs-gg-product-*` wrappers.
- [ ] T013 [US3] Add the `fs-gg-audio` copy block to `.template.config/template.json`: `condition: "(profile == \"game\" || profile == \"sample-pack\")"`, `source: "template/product-skills/fs-gg-audio/"`, `target: ".agents/skills/fs-gg-audio/"`, `copyOnly: ["**/*"]`.
- [ ] T014 [US3] Regenerate the skill manifest: `dotnet fsi scripts/generate-skill-manifest.fsx`; commit the new `fs-gg-audio` row (`sha256`, `materializes-when: "profile in [game, sample-pack]"`, `supplied-by`, `resolvablePath`). Depends on T011. Confirm the predicate matches T013's condition exactly.
- [ ] T015 [US3] Update the Canvas gate comment in `template/base/src/Product/Product.fsproj` (lines ~21-24) to note the `Audio` module ships via the existing `FS.GG.UI.Canvas` game/sample-pack gate — no new PackageReference added (FR-009 satisfied by the existing gate).
- [ ] T016 [US3] **Live scaffold check**: `dotnet new fs-gg-ui -o /tmp/243-game --profile game` (assert `.agents/skills/fs-gg-audio/SKILL.md` present) and `--profile app` (assert absent); diff against T005's baseline to confirm no leak into non-audio profiles and that the skill body byte-matches the manifest sha256. Record to `specs/243-audio-effect-surface/readiness/scaffold-after.md`.
- [ ] T017 [US3] Run the coherence gates: `dotnet test tests/Package.Tests/Package.Tests.fsproj` (Feature231SkillManifestTests + SurfaceAreaTests) and the SkillParity harness (`tools/Rendering.Harness`); confirm manifest/template predicate parity, wrapper parity across `.agents/` + `.claude/`, no dangling API references, and no vocabulary leak (SC-004).

**Checkpoint**: All three stories functional; scaffold verified against the real template.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T018 [P] Ship the doc copy `template/base/docs/api-surface/Canvas/Audio.fsi` alongside the existing `Loop.fsi`/`Rng.fsi` doc copies (kept in sync with the shipped `src/Canvas/Audio.fsi`).
- [ ] T019 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` convention into `specs/243-audio-effect-surface/feedback/` (process friction, generalizable-code candidates).
- [ ] T020 Run the full `quickstart.md` validation end-to-end (§1–§5) and confirm every expected outcome; note any `environment-limited` substitutions.
- [ ] T021 On merge readiness: comment implementation status on FS-GG/FS.GG.Rendering#92 and move its Coordination board item to `In review` → `Done`; confirm #93 remains parked.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)**: no deps — start immediately.
- **Foundational (P2)**: after Setup — BLOCKS all user stories (surface + live baseline).
- **US1 (P3)**: after Foundational.
- **US2 (P4)**: after US1 (shares `Audio.fsi`/`Audio.fs`/`AudioTests.fs`).
- **US3 (P5)**: after US1+US2 (skill must cite the shipped surface; baseline regen needs the types).
- **Polish (P6)**: after US3.

### Within/ across stories

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
Task: "Author template/product-skills/fs-gg-audio/SKILL.md (T011)"
Task: "Add .agents + .claude fs-gg-product-audio wrapper pair (T012)"
# then T013 (template block) → T014 (manifest regen) → T016 (live scaffold check)
```

## Implementation Strategy

### MVP (library first)

1. Phase 1 Setup → Phase 2 Foundational (`.fsi` signed off in FSI + live scaffold baseline).
2. US1 + US2 → the pure audio request surface and its headless-safe evidence boundary. **STOP &
   VALIDATE**: `dotnet test tests/Canvas.Tests` green; the game default can now *request* sound.
3. US3 → deliver the discoverable skill and prove the real scaffold materializes it correctly.
4. Polish → docs, feedback, full quickstart, issue/board close.

### Notes

- Commit after each task or logical group; keep the 38 pre-existing `packages.lock.json` drifts
  **unstaged** (known local hash drift — do not commit).
- The real audio-*output* backend (SkiaViewer host) is out of scope — an explicit deferral behind
  the seam (plan.md "Deferred"). Do not add a hollow SkiaViewer arm.
- This is a **Tier 1** change: `.fsi` + surface baseline + tests + docs are all required for done.
