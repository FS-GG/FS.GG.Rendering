---
description: "Task list for Badge & Ring Alternative Symbology Grammars"
---

# Tasks: Badge & Ring Alternative Symbology Grammars

**Input**: Design documents from `/specs/195-symbology-badge-ring-grammars/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/symbology-grammars-api.md ✅, quickstart.md ✅

**Tests**: INCLUDED. The constitution (I. Spec → FSI → Semantic Tests → Implementation; V. Test Evidence Mandatory) and the plan/research mandate fail-before/pass-after Expecto batteries over the public surface. Test tasks are therefore first-class and precede each implementation within its story.

**Organization**: Tasks are grouped by user story (US1 Badge / US2 Ring / US3 Compare-board & governance) so each story is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 (user-story phases only; Setup/Foundational/Polish carry no story label)
- Exact file paths are included in every task.

## Path Conventions

Single multi-project F# solution (`FS.GG.Rendering.slnx`). The new public surface lands in the **existing** `src/Symbology/` package; tests extend the **existing** `tests/Symbology.Tests/` and `tests/SymbologyBoard.Tests/`. No new project. Repo root: `/home/developer/projects/FS.GG.Rendering`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline before touching any surface.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test project (solution + `tests/Package.Tests` + `samples/**/*.Tests`) so pre-existing reds (e.g. stale surface baselines, sample pins) are known up front and not mistaken for regressions at merge. Use the discovery-based runner — it globs `*.Tests.fsproj` so nothing silently drops out.

- [X] T001 Confirm clean restore/build of the existing surface: `dotnet build FS.GG.Rendering.slnx -c Debug` (expect clean; pre-existing reds in `tests/Package.Tests` / sample test projects are NOT regressions — they are recorded in T002)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/195-symbology-badge-ring-grammars/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Author the public surface (`.fsi`) first, stub the bodies, and prove the surface is usable end-to-end — BEFORE any story implementation.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

> **⚠️ Early smoke run (STANDING, do not omit) — FSI variant.** This feature is a greenfield pair of **pure, IO-free** functions (no GL/raster/IO), so the "drive the real running app" mandate is satisfied by its honest analogue: an **early FSI/test smoke** of the public surface (plan §"Standing assumption"). Treat T006 — not this plan's narrative — as the confirmation the surface is usable. Do not defer it to the per-story checkpoints.

- [X] T003 Author the contract seam FIRST (constitution I/II): add `[<RequireQualifiedAccess>] type Grammar = Token | Badge | Ring` and the new `val`s (`badge`, `ring`, `render`, `galleryIn`, `filmstripIn`, `animateIn`) to `src/Symbology/Symbology.fsi`, with the doc-comments from `contracts/symbology-grammars-api.md`. Existing `val`s (`defaultToken`/`token`/`animate`/`gallery`/`filmstrip`) left UNCHANGED.
- [X] T004 Add minimal compiling stubs for the new `val`s in `src/Symbology/Symbology.fs` (e.g. `badge`/`ring` → `placeholder token` for now; `render`/`galleryIn`/`filmstripIn`/`animateIn` dispatch `Grammar.Token` to the existing functions and `Badge`/`Ring` to the stubs). Do NOT edit the existing `token`/`animate`/`gallery`/`filmstrip` bodies.
- [X] T005 Pin existing-Token zero-drift guard (write BEFORE any further `.fs` change): add a golden/determinism test in `tests/Symbology.Tests/DeterminismTests.fs` that captures the **canonical SceneCodec bytes** of `token defaultToken` (and a small fixed roster via `gallery`/`filmstrip`) so any later drift on the Token grammar fails loudly (FR-010/SC-006).
- [X] T006 **Early FSI smoke**: run the contract's FSI smoke against the live surface (`open FS.GG.UI.Symbology`; call `Symbology.badge`/`ring`, `Symbology.render Grammar.Badge`/`Grammar.Ring`, and `Symbology.badge { t with R = 0.0 }`). Confirm each returns a non-empty `Scene`, the degenerate call returns the placeholder, and nothing throws. Record the smoke result before building US1/US2.

**Checkpoint**: `.fsi` curated, stubs compile, Token bytes pinned, surface proven usable in FSI — user-story implementation can begin (US1 then US2; US3 after both).

---

## Phase 3: User Story 1 - Render a roster in the Badge grammar (Priority: P1) 🎯 MVP

**Goal**: A designer points existing `Token` values at a new **Badge** grammar — a compact, screen-aligned framed emblem — and gets a legible symbol encoding every channel, with no `ChannelMap` change.

**Independent Test**: Build a `Token` set, render each via `Symbology.badge` (and `render Grammar.Badge`), and confirm (a) non-blank scene, (b) every channel — faction hue, class, sigil, state, threat, charge, speed, health, shield, heading — observably alters output when varied, (c) re-render is byte-identical, (d) degenerate `R<=0` → visible placeholder, no throw.

### Tests for User Story 1 (write FIRST — must FAIL before implementation) ⚠️

- [X] T007 [P] [US1] Extend the channel-presence battery for Badge in `tests/Symbology.Tests/ChannelPresenceTests.fs`: vary ONE channel at a time (faction incl. distinct `Custom`, threat, state, charge, health, speed, shield, sigil, klass, heading) and assert the canonical bytes of `badge` change per channel (SC-002/FR-003).
- [X] T008 [P] [US1] Extend the determinism battery for Badge in `tests/Symbology.Tests/DeterminismTests.fs`: render the same `Token` twice through `badge` and assert byte-identical canonical SceneCodec bytes (SC-003/FR-004). **Cross-process clause (SC-003):** purity guarantees cross-process equality, but assert it explicitly — pin the canonical bytes of `badge` for a fixed `Token` as a stable golden literal (the cross-process proxy), not only an in-process render-twice equality.
- [X] T009 [P] [US1] Extend the placeholder battery for Badge in `tests/Symbology.Tests/PlaceholderTests.fs`: `R <= 0` through `badge` ⇒ non-empty visible placeholder scene, no exception (SC-004/FR-005).

### Implementation for User Story 1

- [X] T010 [US1] Implement the `badge` grammar in `src/Symbology/Symbology.fs` per the Badge per-channel siting table (data-model §3 / research D3): frame hue=faction (`Custom` honoured), frame stroke width=threat, solid/dashed frame=state, interior radial-gradient alpha=charge, bottom health bar (length + green→red), speed pip row (0..4), corner shield mount, centre sigil, class-driven outline/corner profile, discrete edge heading pip (screen-aligned, FR-006). Reuse `clamp01`/`factionColor`/`lerpColor`/`placeholder`; `R<=0` → `placeholder token`. Make T007–T009 pass.
- [X] T011 [US1] Confirm `render Grammar.Badge token ≡ badge token` in `src/Symbology/Symbology.fs` (dispatcher path) and that `render Grammar.Token` still equals existing `token` (T005 stays green).

**Checkpoint**: Badge renders every channel legibly, deterministically, with safe degenerate behaviour — MVP demonstrable in isolation.

---

## Phase 4: User Story 2 - Render a roster in the Ring grammar (Priority: P2)

**Goal**: A designer renders the same `Token` values as a **Ring** — a centred radial gauge where continuous channels (health, charge) read as arc sweeps / rim fill — with no `ChannelMap` change.

**Independent Test**: Render a `Token` set via `Symbology.ring` (and `render Grammar.Ring`) and confirm non-blank output, observable per-channel variation, byte-identical re-render, degenerate placeholder, AND that the health arc sweep grows monotonically with the health value.

### Tests for User Story 2 (write FIRST — must FAIL before implementation) ⚠️

- [X] T012 [P] [US2] Extend the channel-presence battery for Ring in `tests/Symbology.Tests/ChannelPresenceTests.fs`: vary ONE channel at a time and assert `ring` canonical bytes change per channel incl. `Custom` faction (SC-002/FR-003).
- [X] T013 [P] [US2] Extend the determinism battery for Ring in `tests/Symbology.Tests/DeterminismTests.fs`: same `Token` twice through `ring` ⇒ byte-identical canonical bytes (SC-003/FR-004). **Cross-process clause (SC-003):** pin the canonical bytes of `ring` for a fixed `Token` as a stable golden literal (the cross-process proxy), in addition to the in-process render-twice equality.
- [X] T014 [P] [US2] Extend the placeholder battery for Ring in `tests/Symbology.Tests/PlaceholderTests.fs`: `R <= 0` through `ring` ⇒ non-empty placeholder, no exception (SC-004/FR-005).
- [X] T015 [P] [US2] Add the Ring health-arc monotonicity test in `tests/Symbology.Tests/GrammarTests.fs` (NEW file — **register it in `tests/Symbology.Tests/Symbology.Tests.fsproj` `<Compile>` in correct order, before `Program.fs`, or Expecto silently never runs it**): assert the health arc sweep is monotone non-decreasing across `Health ∈ [0,1]` (`sweep = maxSweep * clamp01 Health`) (FR-007/SC-002). Also add the **render-dispatch test** here (data-model §5): assert `render Grammar.Token t ≡ token t`, `render Grammar.Badge t ≡ badge t`, `render Grammar.Ring t ≡ ring t` by canonical bytes (G2).

### Implementation for User Story 2

- [X] T016 [US2] Implement the `ring` grammar in `src/Symbology/Symbology.fs` per the Ring per-channel siting table (data-model §3 / research D4): outer-ring hue=faction, ring thickness=threat, solid/dashed ring=state, radial interior gradient alpha=charge, **health arc sweep (monotone↑) + green→red**, rim speed beads (0..4), ring shield mount, centre sigil, class-driven inner glyph, heading needle from centre (screen-aligned, FR-006). `R<=0` → `placeholder token`. Make T012–T015 pass.
- [X] T017 [US2] Confirm `render Grammar.Ring token ≡ ring token` in `src/Symbology/Symbology.fs` (dispatcher Ring path); T005/T011 stay green.

**Checkpoint**: All three form factors render from one `Token`; Ring health is provably monotone. US1 + US2 work independently.

---

## Phase 5: User Story 3 - Compare grammars on a review board, governed unchanged (Priority: P3)

**Goal**: A designer assembles a review board (gallery / motion filmstrip) of a roster in a **selected grammar** to A/B form factors, and the existing legibility linter returns a **grammar-independent** verdict.

**Independent Test**: Render the same roster as a gallery in each grammar (each byte-reproducible, incl. empty/single roster), apply grammar-agnostic motion overlays deterministically, and confirm `Legibility.score` returns the **identical** report regardless of which grammar is selected.

### Tests for User Story 3 (write FIRST — must FAIL before implementation) ⚠️

- [X] T018 [P] [US3] Extend the gallery battery in `tests/Symbology.Tests/GalleryTests.fs`: `galleryIn` reproducible per grammar (Token/Badge/Ring); empty roster and single-unit roster render reproducibly; `galleryIn Grammar.Token` byte-identical to existing `gallery` (FR-008/FR-010).
- [X] T019 [P] [US3] Extend the motion/filmstrip batteries in `tests/Symbology.Tests/MotionTests.fs` and `tests/Symbology.Tests/FilmstripTests.fs`: `animateIn`/`filmstripIn` are deterministic on Badge/Ring with grammar-agnostic overlays (Pulse/Blink/Damage), directional motions degrade to the static base symbol, and `Grammar.Token` paths are byte-identical to existing `animate`/`filmstrip` (FR-014/FR-010).
- [X] T020 [P] [US3] Add the linter grammar-independence test in `tests/SymbologyBoard.Tests/` (new `LinterGrammarIndependenceTests.fs`, or extend existing `BoardTests.fs`; **if a new file, register it in `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` `<Compile>` in correct order, before `Program.fs`, or it silently never runs**): render one fixed roster in all three grammars (assert the **scenes differ**, so the test is meaningful) yet assert `Legibility.score roster` returns the **identical** `Report` (usage summary, findings, verdict) across grammars (SC-005/FR-009). No linter change.

### Implementation for User Story 3

- [X] T021 [US3] Implement `galleryIn` and `filmstripIn` in `src/Symbology/Symbology.fs`: draw the roster in the selected grammar via `render`; `Grammar.Token` shares/reproduces the existing `gallery`/`filmstrip` output byte-for-byte (DRY only where zero-drift is guaranteed — research D1). Make T018 pass.
- [X] T022 [US3] Implement `animateIn` in `src/Symbology/Symbology.fs`: apply only grammar-agnostic centre/radius overlays (Pulse/Blink/Damage) on Badge/Ring; directional motions degrade to the static base symbol; `Grammar.Token` reproduces existing `animate` byte-for-byte; total & deterministic (research D5). Make T019 pass.
- [X] T023 [US3] Confirm the linter is untouched and T020 passes — `Legibility.fsi`/`Legibility.fs` show zero change; grammar never enters scoring input (FR-009).
- [X] T024 [P] [US3] Update the canonical design-loop grammar section in `src/Symbology/skill/SKILL.md`: document **Badge** and **Ring** as selectable grammars alongside the Directional Token (when to prefer each; that the `ChannelMap` is UNCHANGED across grammars; update any "pick grammar (default + only v1: Directional Token)" line to name all three) (FR-013).
- [X] T025 [US3] Mirror the SKILL.md grammar edit to `template/product-skills/fs-gg-symbology/SKILL.md` (the `.claude/skills/fs-gg-symbology/` and `.agents/skills/fs-gg-symbology/` trees are pointer wrappers that inherit it) (FR-013).
- [X] T026 [P] [US3] (OPTIONAL — not a contract) Add a lightweight `samples/SymbologyBoard` grammar-compare demo that renders the same roster as a gallery in each grammar for A/B comparison; confirm each board is byte-reproducible (quickstart §7).

**Checkpoint**: Boards compare grammars at target size; the linter verdict is provably grammar-independent; the loop docs describe all three grammars.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Tier-1 surface governance, zero-drift proof, and quickstart validation across all stories.

- [X] T027 Regenerate the symbology surface baseline `readiness/surface-baselines/FS.GG.UI.Symbology.txt` (per repo tooling) and confirm the diff is EXACTLY `+ FS.GG.UI.Symbology.Grammar` and `+ FS.GG.UI.Symbology.Grammar+Tags`, with **zero drift** on every other baseline (`Scene`, `SkiaViewer`, `Controls`, `Canvas`, `Legibility`, …) — `git diff readiness/surface-baselines/` (FR-011/SC-006).
- [X] T028 Run the skill-parity check `dotnet fsi scripts/check-agent-skill-parity.fsx` and confirm critical=0, high=0 after the canonical + mirrored grammar-doc edits (SC-007).
- [X] T029 Run the full quickstart validation (`specs/195-symbology-badge-ring-grammars/quickstart.md` §1–§6): build, FSI smoke, `dotnet test tests/Symbology.Tests/...` + `tests/SymbologyBoard.Tests/...` all green, and walk the per-Success-Criterion table (SC-001…SC-007, FR-007). Confirm no prior assertion was weakened against the T002 baseline.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. T003 (`.fsi`) → T004 (stubs) → T005 (Token pin) → T006 (FSI smoke).
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 (P1) is the MVP. US2 (P2) is independent of US1 but follows it by priority. US3 (P3) depends on `badge`/`ring` existing (it renders boards in those grammars), so US3 implementation follows US1+US2; US3 tests T018–T020 can be authored earlier.
- **Polish (Phase 6)**: Depends on all desired user stories complete.

### Within Each User Story

- Tests (T007–T009 / T012–T015 / T018–T020) are written and MUST FAIL before the story's implementation.
- The existing `token`/`animate`/`gallery`/`filmstrip` bodies are NEVER edited (zero-drift guard T005).

### Parallel Opportunities

- **Foundational**: T003 before T004 before T006 (sequential — same `.fsi`/`.fs` then smoke). T005 is independent of T004 and may run alongside.
- **US1 tests**: T007, T008, T009 touch different files — run in parallel `[P]`.
- **US2 tests**: T012, T013, T014, T015 — different files (T015 is a new file) — run in parallel `[P]`.
- **US3 tests**: T018, T019, T020 — different files — run in parallel `[P]`.
- **US3 docs**: T024 (canonical) before T025 (mirror); T026 (optional sample) is independent `[P]`.
- **Cross-story**: implementation tasks T010/T016/T021–T022 all edit `src/Symbology/Symbology.fs` — they are NOT parallel with each other.

---

## Parallel Example: User Story 1

```bash
# Author all three Badge test batteries together (different files), then watch them fail:
Task: "Channel-presence for Badge in tests/Symbology.Tests/ChannelPresenceTests.fs"   # T007
Task: "Determinism for Badge in tests/Symbology.Tests/DeterminismTests.fs"            # T008
Task: "Placeholder for Badge in tests/Symbology.Tests/PlaceholderTests.fs"            # T009
# Then implement `badge` (T010) in src/Symbology/Symbology.fs to turn them green.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (baseline).
2. Phase 2: Foundational — including the **early FSI smoke (T006)** that proves the surface is usable before any story.
3. Phase 3: User Story 1 (Badge).
4. **STOP and VALIDATE**: Badge renders every channel, deterministically, safe on degenerate input.
5. Demo the second visual register (one vocabulary, two grammars).

### Incremental Delivery

1. Setup + Foundational → surface usable (FSI smoke green).
2. US1 (Badge) → test independently → demo MVP.
3. US2 (Ring) → test independently (incl. health monotonicity) → demo.
4. US3 (compare board + grammar-independent linter + loop docs) → test independently → demo.
5. Polish → surface baseline regen, skill parity, quickstart validation.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- [Story] label maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry no story label.
- **Purity is the hard constraint**: no wall-clock, randomness, or IO in any new function (FR-004); compare **canonical SceneCodec bytes**, not coarse readback hashes, for determinism claims.
- Verify each story's tests FAIL before implementing; commit after each task or logical group.
- The existing Token grammar and the `Legibility` linter are NEVER edited — zero behavioural drift (FR-010) and grammar-independence (FR-009) are guaranteed by not touching them.
