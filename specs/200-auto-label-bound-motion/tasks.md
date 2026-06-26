---
description: "Task list for Symbology Auto-Label & Label-Bound Motion (channel-projected labels + motion-timeline label animation)"
---

# Tasks: Symbology Auto-Label & Label-Bound Motion (channel-projected labels + motion-timeline label animation)

**Input**: Design documents from `/specs/200-auto-label-bound-motion/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/symbology-auto-label-motion-api.md ✓, quickstart.md ✓, constitution.md ✓

**Tests**: INCLUDED — the constitution (Principle I: Spec → FSI → Semantic Tests → Implementation; Principle V: Test Evidence Mandatory) and the plan require fail-before/pass-after semantic tests over the public surface. Test tasks are first-class and ordered before implementation within each story.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- All paths are repo-relative from `/home/developer/projects/FS.GG.Rendering/`

## Path Conventions

This is a multi-project F# solution (`FS.GG.Rendering.slnx`). The change is internal to the existing
`src/Symbology/` library + its curated `.fsi`, plus additive tests in existing test projects and the
mirrored skill trees. No new project, no new sample, no new font file (FR-019/FR-021). The two new
capabilities resolve into the existing `labelDispatch` and ride the existing per-grammar regions /
motion timeline — there is no second label channel and no new board/motion entry point (FR-001/FR-005).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build graph and pin the no-regression baseline before any change.

- [X] T001 Confirm the affected projects build clean on the branch (no edits yet): `dotnet build src/Symbology/Symbology.fsproj`, `dotnet build src/Symbology.Render/Symbology.Render.fsproj`, `dotnet build tests/Symbology.Tests/Symbology.Tests.fsproj`, `dotnet build tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` (no new project is added — change lands in the existing `Symbology.fsproj` per plan Structure Decision)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/200-auto-label-bound-motion/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds incl. any stale surface/sample pins are flagged HERE, not discovered at merge)
- [X] T003 Record the pre-change symbology surface baseline snapshot for later diff: confirm `git status -- readiness/surface-baselines/` is clean and note current `readiness/surface-baselines/FS.GG.UI.Symbology.txt` content as the comparison point (only this file may move; `FS.GG.UI.Symbology.Render.txt` and all others MUST stay byte-stable — FR-020/SC-007)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft the public surface first (Constitution I — FSI before implementation), make the tree
compile against it with zero-drift defaults, and prove the capabilities work with an early smoke BEFORE
building out any user story.

**⚠️ CRITICAL**: No user-story work (US1/US2/US3) can begin until this phase is complete.

> **⚠️ Early FSI/test smoke (STANDING, do not omit).** This is a greenfield-additive completion, not a defect
> fix, so there is no root-cause map; per plan §"Standing assumption", the analogue of the live-smoke mandate
> is an **early FSI/public-surface smoke** (T008 below). Treat that smoke — and later the render-bridge tofu
> test (T017/T039) — NOT this plan's narrative, as the confirmation the capabilities actually work. Pull it
> forward; do not defer evidence to the per-story checkpoints.

- [X] T004 Draft the public-surface delta in `src/Symbology/Symbology.fsi` FIRST (Constitution I/II, FR-020, contract §1): add `type AutoField = FactionCode | KlassCode | StateCode | HealthTier | ThreatTier | SpeedPips | ShieldFlag`; add `type AutoLabelSpec = { Fields: AutoField list; Separator: string }`; add `type LabelMotion = TypeOn | Fade | Pulse | Scroll`; add two `None`-defaulted optional fields to `Token` (`AutoLabel: AutoLabelSpec option`, `LabelMotion: LabelMotion option`); add ctors `val autoLabel: AutoField list -> AutoLabelSpec`, `val autoLabelSep: string -> AutoField list -> AutoLabelSpec`, `val labelMotion: LabelMotion -> LabelMotion`. Keep every existing type (`Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`/`LabelRun`/`LabelAlign`/`LabelParagraph`/`LabelText`/`Grammar`) and EVERY board/motion entry-point signature (`token`/`render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn`/`badge`/`ring`/`defaultToken` + label ctors) byte-stable (FR-005/FR-017)
- [X] T005 Make `src/Symbology/Symbology.fs` compile against the new `.fsi` with zero-drift defaults: add the `AutoField`/`AutoLabelSpec`/`LabelMotion` types; add the two `None`-defaulted fields to the `Token` record; extend `defaultToken` with `AutoLabel = None; LabelMotion = None`; implement ctors `autoLabel` (`Separator = " "`), `autoLabelSep`, `labelMotion` (identity). Do NOT yet wire projection/motion into dispatch — at this point `resolveLabel` ≡ `t.Label` and the label path is byte-identical to 199 (zero drift)
- [X] T006 [P] Update every raw `Token` record literal (no `with`-copy) in `src/Symbology/` and `tests/Symbology.Tests/` + `tests/Symbology.Render.Tests/` to include the two new `None` fields — value-preserving, additive only (fixtures built via `Symbology.defaultToken` + `with`-copy are unaffected; only literal `Token` constructions need the new fields). Confirm all four projects still build
- [X] T007 [P] Scaffold the two new battery files — `tests/Symbology.Tests/AutoLabelTests.fs` (empty Expecto `testList "AutoLabel"`) and `tests/Symbology.Tests/LabelMotionTests.fs` (empty `testList "LabelMotion"`) — and register BOTH in `tests/Symbology.Tests/Symbology.Tests.fsproj` (compile order before `Program.fs`); confirm the project still builds and the (empty) lists are discovered
- [X] T008 **Early FSI/public-surface smoke** (the live-smoke analogue; quickstart §"FSI smoke"): through the PUBLIC surface (`Symbology.render`/`token`/`animate` + `SceneCodec.export(...).CanonicalBytes`) confirm BEFORE any story — (1) a `defaultToken` with `AutoLabel = None`, `LabelMotion = None`, `Label = None` equals the pinned spec-199/pre-feature golden; (2) `Some (Plain "HMR-7")` with the two new fields present-as-`None` still equals the pinned spec-197 golden (opt-out zero drift, C5); and that constructing `{ defaultToken with AutoLabel = Some (Symbology.autoLabel [FactionCode]); LabelMotion = Some TypeOn }` does not throw. (Auto-projection / motion behaviour is exercised in US1/US2 once wired — here only the layered opt-out byte-identity and no-throw construction are asserted.) Record the FSI session output as evidence
- [X] T009 Regenerate the symbology surface baseline now the `.fsi` is final-shape (FR-020): run the repo surface-baseline workflow, then `git status -- readiness/surface-baselines/` MUST show ONLY `FS.GG.UI.Symbology.txt` changed (new `AutoField`/`AutoLabelSpec`/`LabelMotion`, the two `Token` fields, the three ctors) and `FS.GG.UI.Symbology.Render.txt` + every other baseline byte-stable. Final verification is re-run in Polish (T042)

**Checkpoint**: Public surface drafted + baseline regenerated, tree compiles, early smoke proves layered
opt-out zero-drift and no-throw construction — user stories can now begin (US1 first; US2 after US1's
`resolveLabel`/dispatch wiring exists; US3 after US1+US2).

---

## Phase 3: User Story 1 - Auto-derived labels projected from a Token's channels (Priority: P1) 🎯 MVP

**Goal**: A `Token` can opt into an auto-derived label (`AutoLabel = Some spec`) that the library projects
from that `Token`'s OWN encoded channels (faction/class/state/health/threat/speed/shield codes) using the
existing label vocabulary — deterministic, tofu-free, fitted, in every grammar — reading NEVER a game's raw
stats. An explicit `Label` always overrides the projection. A `Token` that does not opt in is byte-identical
to spec 199.

**Independent Test**: Build `Token`s that (a) opt into auto-label with no explicit label, (b) opt into
auto-label AND supply an explicit label, (c) do neither; render each in each grammar through the render
bridge; confirm (a) renders a styled label projected from its channels (real glyphs, fitted), (b) renders the
EXPLICIT label (explicit wins), (c) is byte-identical to spec 199, and two `Token`s differing in one projected
channel produce different auto-labels while identical channels produce byte-identical ones.

### Tests for User Story 1 (write FIRST, ensure they FAIL before T015–T019)

> **NOTE**: Each new assertion must be at least as strong as what it adds (Constitution V — no existing
> assertion weakened or deleted). Opt-out `Token`s MUST stay byte-identical to the pinned 199/198/197 goldens.

- [X] T010 [P] [US1] In `tests/Symbology.Tests/AutoLabelTests.fs` add the projection-presence battery: a `Token` with `AutoLabel = Some (Symbology.autoLabel [FactionCode; HealthTier])` and `Label = None` produces a NON-EMPTY label whose canonical bytes DIFFER from the same `Token` with `AutoLabel = None`, in each grammar; neither raises (C1, SC-001/SC-002)
- [X] T011 [P] [US1] In `tests/Symbology.Tests/AutoLabelTests.fs` add the channel-determinism battery: two `Token`s identical except one channel a selected `AutoField` reads (e.g. `Health` for `HealthTier`, `Faction` for `FactionCode`) yield DIFFERING auto-label bytes; two `Token`s with identical channels yield BYTE-IDENTICAL auto-label bytes (deterministic pure projection — C3, FR-004/SC-002)
- [X] T012 [P] [US1] In `tests/Symbology.Tests/AutoLabelTests.fs` add the explicit-overrides-auto battery: a `Token` with BOTH `AutoLabel = Some _` and `Label = Some (Symbology.plainLabel "BRAVO-6")` renders the EXPLICIT label (byte-identical to the same `Token` with `AutoLabel = None`) and NOT the projection — exactly one resolved label, no exception (C2, FR-003/SC-005)
- [X] T013 [P] [US1] In `tests/Symbology.Tests/AutoLabelTests.fs` add the degenerate-projection battery: `AutoLabel = Some (Symbology.autoLabel [])` (empty fields), and `AutoLabel = Some (Symbology.autoLabel [ShieldFlag])` with `Shield = false` (projects to whitespace/nothing) ⇒ NO label node, no exception, in every grammar (treated as no label, exactly like an empty hand-authored label — C4, FR-004/FR-012)
- [X] T014 [P] [US1] In `tests/Symbology.Tests/ChannelPresenceTests.fs` add: two `Token`s whose ONLY difference is a projected channel value yield differing canonical bytes via the auto-label (the projection is observable); AND in `tests/Symbology.Tests/RichLabelTests.fs` (or `DeterminismTests.fs`) add the opt-out assertion: a `Token` with `AutoLabel = None`, `LabelMotion = None` carrying a `Plain`/`Rich`/`Laid`/no label is byte-identical to the pinned spec-199 golden (C5, SC-003) — extend, do not weaken, the existing 199 identity tests
- [X] T015 [P] [US1] In `tests/Symbology.Render.Tests/RenderLabelTests.fs` add the auto-label tofu test: rasterise (via `Render.toPng` under the real `SkiaViewer.Fonts` measurer) a `Token` with `AutoLabel = Some _` and `Label = None`; assert EVERY resolved run is non-tofu (`TofuCount = 0`) and the board is non-blank (C1/FR-010, SC-002)

### Implementation for User Story 1

- [X] T016 [US1] Add the pure `projectAutoLabel : Token -> AutoLabelSpec -> LabelText option` in `src/Symbology/Symbology.fs`: a fold over `spec.Fields`, each arm reading ONLY the named `Token` channel and rendering its fixed game-agnostic code per data-model §`AutoField` (`FactionCode`→ALY/ENY/NEU/CUS, `KlassCode`→MOB/HVY/SCT, `StateCode`→CFM/SUS, `HealthTier`→"H"+round(Health*100), `ThreatTier`→bucket Threat→T0..T4, `SpeedPips`→"S"+Speed, `ShieldFlag`→"SHD" when true else dropped); join the rendered codes with `spec.Separator`; return `Some (LabelText.Plain joined)`, or `None` when `Fields = []` or the joined text is empty/all-whitespace (FR-002/FR-004) — no wall-clock/randomness/IO (FR-015). Makes T010/T011/T013 pass
- [X] T017 [US1] Add `resolveLabel : Token -> LabelText option` in `src/Symbology/Symbology.fs`: `t.Label |> Option.orElseWith (fun () -> t.AutoLabel |> Option.bind (projectAutoLabel t))` — explicit wins, exactly one resolved label or none (FR-003) — makes T012 pass
- [X] T018 [US1] Feed `resolveLabel t` (NOT `t.Label`) into the three per-grammar label helpers `tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes` (`src/Symbology/Symbology.fs` ~ lines 850–860), keeping `labelDispatch` (~ lines 815–844) and the per-grammar regions/budgets (Token≤3, Badge≤2, Ring≤2) UNCHANGED — the projected label rides the existing fit/wrap/cap path in every grammar (FR-001/FR-009/FR-011). Makes T010/T014/T015 pass
- [X] T019 [US1] Verify opt-out zero drift: a `Token` with `AutoLabel = None` reaches `labelDispatch` with `resolveLabel t = t.Label` and hits the EXACT spec-199 path (re-run T014); confirm the pinned 199/198/197/pre-feature goldens stay green (`dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj`). Add a render-twice byte-equal assertion (in-process) for an auto-labelled `Token` in `tests/Symbology.Tests/DeterminismTests.fs` (FR-015) — covers projection determinism before US2's cross-process golden

**Checkpoint**: US1 fully functional and independently testable — a `Token` opts into a channel-projected
label (real glyphs, fitted, tofu-free at the render edge T015), explicit labels override the projection
(T012), identical/differing channels produce identical/differing labels (T011), and opt-out is byte-identical
to 199 (T014/T019). This is the MVP — the "auto label" half of the deferred 199 FR-019, with per-game stats
kept out of the library.

---

## Phase 4: User Story 2 - Label-bound motion over the symbology motion timeline (Priority: P2)

**Goal**: A `Token` can bind its RESOLVED label to the existing motion timeline (`LabelMotion = Some kind`,
kind ∈ `TypeOn | Fade | Pulse | Scroll`) so the label's runs animate as a pure function of the motion phase
the board already supplies (`filmstripIn`/`animateIn`) — deterministic, tofu-free, fitted at every phase. At
the identity/rest phase a motion-bound label is byte-identical to the static spec-199 label; a label that
binds no motion is byte-identical to 199 across the whole timeline. NO board/motion signature change.

**Independent Test**: Render `filmstripIn`/`animateIn` sequences of `Token`s whose labels bind each motion
kind (incl. labels longer than the region for `Scroll`); confirm each label's drawn state is a deterministic
function of phase (same phase ⇒ byte-identical, in-/cross-process); at the rest phase the motion-bound label
equals the static 199 label; the animated label stays fitted at every phase (no mid-glyph clip, no overflow,
capped lines); and a no-motion `Token` is byte-identical to 199 across the sequence.

**Depends on**: US1 (the `resolveLabel` + dispatch wiring `motionLabelNodes` animates).

### Tests for User Story 2 (write FIRST, ensure they FAIL before T026–T030)

- [X] T020 [P] [US2] In `tests/Symbology.Tests/LabelMotionTests.fs` add the rest-phase-≡-static battery: a `Token` with `LabelMotion = Some kind` (each of TypeOn/Fade/Pulse/Scroll) rendered via `animate Idle t 0.0` (and `filmstrip`'s first sample, phase 0) is BYTE-IDENTICAL to the same `Token` with `LabelMotion = None` (the static spec-199 label) — motion is additive; rest = static (C6, FR-007/SC-003)
- [X] T021 [P] [US2] In `tests/Symbology.Tests/LabelMotionTests.fs` add the motion-advances battery: each kind at a NON-REST phase (e.g. `animate Idle t 0.5`) produces canonical bytes DIFFERING from the rest frame, and the symbol's other channels are unaffected (only the label nodes change) (C7, SC-002)
- [X] T022 [P] [US2] In `tests/Symbology.Tests/LabelMotionTests.fs` add the per-phase-determinism battery: the same `(Token, phase)` rendered twice (in-process) is byte-identical for each kind; AND a NEW pinned motion-frame cross-process golden (a `TypeOn`/`Scroll` token at a fixed non-rest phase) — existing 199/198/197 goldens UNCHANGED (C9, FR-006/SC-004)
- [X] T023 [P] [US2] In `tests/Symbology.Tests/LabelMotionTests.fs` add the scroll-stays-fitted battery: a `LabelMotion = Some Scroll` over content LONGER than the region, sampled across phases, keeps every drawn segment WITHIN the region span (`centerX ± regionWidth/2`) — no mid-glyph clip, no overflow into adjacent channels — and the drawn line count stays capped to the per-grammar budget at every phase (C8, FR-011/SC-005); add an equivalent fitted-at-every-phase check for `Pulse` (scaled label still ≤ region) and `TypeOn` (prefix on whole-glyph boundaries)
- [X] T024 [P] [US2] In `tests/Symbology.Tests/LabelMotionTests.fs` add the no-motion-≡-199 battery: a `Token` with `LabelMotion = None` rendered across a `filmstrip`/`filmstripIn` sequence has EVERY frame's label byte-identical to spec 199 (zero drift when motion unused — C5, FR-008/SC-003); AND the auto+motion composition test: a `Token` with BOTH `AutoLabel = Some _` and `LabelMotion = Some _` resolves the projection first then animates it — deterministic, fitted, tofu-free (C10, FR-013)
- [X] T025 [P] [US2] In `tests/Symbology.Tests/PlaceholderTests.fs` add a degenerate-token-with-motion case: `R <= 0` carrying an `AutoLabel`/`LabelMotion` ⇒ visible placeholder, auto/motion suppressed, no exception at any phase (placeholder rule wins — C11, FR-014); AND in `tests/Symbology.Tests/LabelMotionTests.fs` a motion-bound-empty-label no-op (binding motion to a label resolving to no glyphs draws nothing, no throw, every phase — FR-012)

### Implementation for User Story 2

- [X] T026 [US2] Add `restPhase = 0.0` and `motionLabelNodes : LabelMotion -> float -> (unit -> Scene list) -> Scene list` in `src/Symbology/Symbology.fs`: at `restPhase` (and for the rest-is-identity values) return the static node list verbatim (FR-007); else apply the kind's per-phase transform of the already-fitted resolved label — `TypeOn` reveal a measured whole-glyph PREFIX sized by `ph` (rest = full); `Fade` scale run paint ALPHA by `ph` (rest = full alpha); `Pulse` size/alpha factor `1 + k·sin(ph·2π)` with `k` capped so the scaled label still fits the region (rest = 1.0); `Scroll` translate an overlong line by an X-offset clipped to the region extent (rest = 0). Reuse the existing `glyphRunProof`/`withPerspective`/`Paint` primitives — NO new scene primitive (FR-019). Makes T021/T023 pass
- [X] T027 [US2] Thread a `labelPhase: float` parameter (default `restPhase`) through `tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes` and their `drawSymbol`/`drawBadge`/`drawRing` callers (`src/Symbology/Symbology.fs`): when `t.LabelMotion = Some kind` AND `labelPhase <> restPhase`, route through `motionLabelNodes kind labelPhase (fun () -> labelDispatch … (resolveLabel t))`; else call `labelDispatch … (resolveLabel t)` directly (zero drift — FR-008). The placeholder guard (`R <= 0`) stays BEFORE label resolution so it still wins (FR-014). Makes T020/T024/T025 pass
- [X] T028 [US2] Wire the phase at the entry points WITHOUT changing any public signature (FR-005): the static entry points (`token`/`render`/`gallery`/`galleryIn`) pass `restPhase`; `animate`/`animateIn` (`src/Symbology/Symbology.fs` ~ lines 883/1191) pass the normalised `ph = phase - floor phase` they already compute into the base-symbol label draw (so the label animates alongside the existing overlay); `filmstrip`/`filmstripIn` (~ lines 953/1202) pass their per-sample `phase` into the same path. Makes T021/T022 pass
- [X] T029 [US2] Confirm the pure-fallback path (no real measurer installed — the default `Symbology.Tests` path): a motion-bound/auto `Token` still yields a deterministic scene carrying the resolved label's styled nodes + the recorded phase and NEVER throws (FR-016) — add/extend an assertion in `tests/Symbology.Tests/DeterminismTests.fs` alongside the cross-process golden
- [X] T030 [US2] Re-run the full `tests/Symbology.Tests/Symbology.Tests.fsproj` battery: confirm the pinned 199/198/197/pre-feature goldens are still byte-stable (layered zero drift — rest = static, no-motion = 199 via the structural routing, SC-003) and the new motion-frame cross-process golden (T022) reproduces

**Checkpoint**: US2 fully functional and independently testable — labels bind type-on/fade/pulse/scroll over
the existing timeline, rest phase = static 199 byte-for-byte (T020), motion advances deterministically per
phase (T021/T022), scroll/pulse/type-on stay fitted at every phase (T023), no-motion = 199 across the
sequence (T024), and degenerate/empty cases are safe (T025). US1 + US2 together deliver both halves of the
deferred 199 FR-019, with NO board/motion signature change.

---

## Phase 5: User Story 3 - Auto-labelled / motion-bound rosters on review boards, governed unchanged (Priority: P3)

**Goal**: A roster mixing auto-derived and motion-bound labels renders reproducibly on gallery/filmstrip
boards in every grammar with no board/motion signature change; the spec-194 legibility linter stays
grammar-independent and treats the label (however derived/animated) as inspection-detail (pre-attentive
governance unchanged); the design-loop skill documents auto-label + label-bound motion and passes the parity
check.

**Independent Test**: Render a roster mixing auto-derived and motion-bound labels as a gallery/filmstrip in
each grammar (reproducible per grammar under a fixed provider and fixed phase sampling); confirm the linter's
report is grammar-independent and unchanged in pre-attentive governance vs the same roster with 199-era static
hand-authored labels; confirm the skill documents the capabilities and passes parity.

**Depends on**: US1 + US2 (boards/timeline render whatever the dispatch/motion produces).

### Tests for User Story 3 (write FIRST, ensure they FAIL/are-pending before T037–T040)

- [X] T031 [P] [US3] In `tests/Symbology.Tests/GalleryTests.fs` add: a roster mixing `AutoLabel` and `LabelMotion` `Token`s rendered via `galleryIn` (and a `filmstripIn` motion sequence) draws every unit's label (projected and/or animated) in the selected grammar and is byte-reproducible per grammar under a fixed provider + fixed phase sampling, with NO signature change to the board/motion entry points; AND assert the projection reads ONLY `Token` channels — never a game's raw stats (the per-game mapping stays the caller's) (C1/C9, FR-002/FR-017/SC-001)
- [X] T032 [P] [US3] In `tests/Symbology.Tests/LegibilityTests.fs` add the linter-invariance assertion: `Legibility.score`/`scoreAnimated` over a roster WITH auto-derived / motion-bound labels equals the verdict over the SAME roster with 199-era static hand-authored labels, and is identical across grammars — auto/motion does NOT change pre-attentive governance (C13, FR-018/SC-006)
- [X] T033 [P] [US3] In `tests/Symbology.Render.Tests/RenderLabelTests.fs` add the auto+motion render-bridge tofu test — **EXTENDS T015's auto-label case with a bound `LabelMotion` sampled at non-rest phases rather than repeating it**: rasterise a `Token` with `AutoLabel = Some _` and `LabelMotion = Some _` through `Render.toPng` under the real measurer at sampled phases; assert EVERY resolved run is non-tofu (`TofuCount = 0`) and the board is non-blank at each phase (C7/C10/FR-010, SC-002)

### Implementation for User Story 3

- [X] T034 [US3] Confirm `Legibility.fs`/`Legibility.fsi` are UNCHANGED (they do not read `Token.Label`/`AutoLabel`/`LabelMotion`); make T032 green by relying on the existing grammar-independent verdict — if the test reveals any coupling, that is a defect to fix in `src/Symbology/Legibility.fs` (plan asserts none exists post-199)
- [X] T035 [US3] Author the auto-label + label-motion section canonically in `src/Symbology/skill/SKILL.md` (FR-022): when to auto-derive a label vs hand-author one; that auto-labels project ONLY from `Token` channels (never per-game stats) and are overridable by an explicit label; the supported motion kinds (type-on/fade/pulse/overflow-scroll) and that they sample deterministically from the motion phase (rest = static); that both require the real measurer for tofu-free output; keep auto-projections compact + motion restrained; don't impersonate faction/state pre-attentive encodings or crowd the region; how surplus/overflow degrades (cap+ellipsis, scroll within region); complements (never replaces) the vector sigil
- [X] T036 [P] [US3] Mirror the SKILL.md update to the skill trees: `.claude/skills/fs-gg-symbology/SKILL.md`, `.agents/skills/fs-gg-symbology/SKILL.md`, and `template/product-skills/fs-gg-symbology/SKILL.md` (adapted copy) — keep `src/Symbology/skill/reference.fsx` in sync if it enumerates label capabilities
- [X] T037 [US3] Run skill parity: `dotnet fsi scripts/check-agent-skill-parity.fsx` ⇒ expect `critical=0 high=0` (FR-022/SC-007); fix any drift between canonical and mirrored copies

**Checkpoint**: All three stories independently functional — auto-labelled / motion-bound rosters render
reproducibly on boards in every grammar, the linter's governance is provably unchanged, and the skill is
documented + parity-clean.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across all stories, surface/baseline integrity, and quickstart sign-off.

- [X] T038 Run the full feature test sweep: `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and `dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` — all existing + new batteries green
- [X] T039 Re-confirm the auto+motion render-bridge tofu evidence (T015/T033) under the real measurer is recorded as readiness evidence (every resolved run non-tofu at sampled phases; non-blank board) (FR-010/SC-002)
- [X] T040 Final surface-baseline integrity check (FR-020/SC-007): `git status -- readiness/surface-baselines/` shows ONLY `FS.GG.UI.Symbology.txt` moved (new types/fields/ctors) and EVERY other baseline byte-stable — re-confirms T009 after all edits
- [X] T041 Re-run the comprehensive no-regression baseline and diff against T002: `dotnet fsi scripts/baseline-tests.fsx --out specs/200-auto-label-bound-motion/readiness/baseline-after.md` — no NEW reds vs the pre-change baseline (incl. `Package.Tests` + `samples/**`)
- [X] T042 Execute quickstart.md per-Success-Criterion validation (SC-001…SC-007) and record evidence: gallery/filmstrip scenes per grammar, render-bridge tofu + projection observability, layered zero-drift goldens, rest-phase = static, cross-process motion-frame golden, fit/scroll/pulse/type-on/empty/placeholder cases, explicit-overrides-auto, linter invariance, baseline + parity reports
- [X] T043 [P] Final review of `src/Symbology/Symbology.fs` for idiomatic simplicity (Constitution III): pure folds (projection, motion transforms), no `mutable`, `Option.defaultValue`/`Option.orElseWith` defaults, internal helpers (`projectAutoLabel`/`resolveLabel`/`motionLabelNodes`/`restPhase`) omitted from `.fsi`; confirm no new scene primitive / font file / GPU path was introduced (FR-019/FR-021) and no board/motion signature changed (FR-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. The `.fsi` (T004) is drafted before any implementation (Constitution I); the early smoke (T008) gates the stories.
- **User Stories (Phase 3–5)**: All depend on Foundational. Ordered by priority and by code reuse:
  - **US1 (P1)** — the projection + `resolveLabel` + dispatch wiring; the MVP (auto label).
  - **US2 (P2)** — `motionLabelNodes` animates the resolved label `resolveLabel`/dispatch produces ⇒ start after US1's wiring (T017–T018) exists.
  - **US3 (P3)** — boards/linter/skill render whatever US1+US2 produce ⇒ after US2 (linter test T032 + skill T035 can be drafted earlier in parallel as they touch no shared source).
- **Polish (Phase 6)**: Depends on all desired stories.

### Within Each User Story

- Tests (T010–T015 / T020–T025 / T031–T033) are written and MUST FAIL before the implementation tasks in that story (Constitution I/V).
- In US1: `projectAutoLabel` (T016) → `resolveLabel` (T017) → feed into dispatch (T018) → zero-drift re-verify (T019).
- In US2: `motionLabelNodes` + `restPhase` (T026) → thread `labelPhase` through the drawers (T027) → wire entry-point phase (T028) → pure-fallback (T029) → re-verify (T030).
- Implementation tasks editing the SAME file (`Symbology.fs`) run sequentially within a story; test tasks in different files are `[P]`.

### Parallel Opportunities

- **Setup**: T001/T002 sequential (baseline after build); T003 independent.
- **Foundational**: T004 first (others depend on the `.fsi`); T006 `[P]` (Token literals) and T007 `[P]` (test scaffolds) can run alongside each other after T005.
- **US1 tests**: T010–T015 — T010–T013 share `AutoLabelTests.fs` (coordinate as one edit or sequence); T014 (`ChannelPresenceTests.fs`/`RichLabelTests.fs`) and T015 (`RenderLabelTests.fs`) are separate files, truly `[P]`.
- **US2 tests**: T020–T024 share `LabelMotionTests.fs` (sequence or single edit); T025 (`PlaceholderTests.fs` + a `LabelMotionTests.fs` no-op) overlaps — sequence the `LabelMotionTests.fs` portion.
- **US3**: T031/T032/T033 `[P]` (three different test files); T036 `[P]` (mirror trees) after T035.
- **Cross-story**: once Foundational is done, US3's skill doc (T035/T036) and linter test (T032) can be drafted in parallel with US1/US2 implementation (no shared source).

---

## Parallel Example: User Story 1 tests

```bash
# Different files — launch together (write FIRST, expect FAIL):
Task: "ChannelPresenceTests.fs — differing projected channel ⇒ differing bytes + opt-out ≡ 199 (T014)"
Task: "RenderLabelTests.fs — auto-label tofu test, every resolved run non-tofu (T015)"
# AutoLabelTests.fs additions (T010–T013) touch one file — sequence or do as one edit.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — including the **early FSI/public-surface smoke (T008)** that proves layered opt-out zero-drift before any story.
2. Complete Phase 3 (US1): channel-projected auto-labels resolving through the existing dispatch, explicit override.
3. **STOP and VALIDATE**: auto-labels tofu-free (T015), differing/identical channels differ/match (T011), explicit overrides auto (T012), opt-out ≡ 199 (T014/T019).
4. This is a shippable increment — the "auto label" half of the deferred 199 FR-019, per-game stats kept out of the library.

### Incremental Delivery

1. Setup + Foundational → surface drafted, baseline regenerated, smoke green.
2. US1 → channel-projected auto-label → validate independently → demo (MVP).
3. US2 → label-bound motion over the existing timeline → validate independently → demo.
4. US3 → boards + linter invariance + skill → validate independently → demo.
5. Polish → full sweep, baseline integrity, quickstart sign-off.

### Notes

- `[P]` = different files, no dependency on incomplete tasks.
- `[Story]` maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- Verify story tests FAIL before implementing (Constitution I/V); never weaken or delete an existing assertion (Constitution V).
- Layered zero-drift is the hard constraint (FR-008/SC-003): keep the pinned 199/198/197/pre-feature goldens green at every step; opt-out, no-motion, and rest-phase paths route structurally to the 199 dispatch.
- The projection reads ONLY `Token` channels (FR-002) and explicit always overrides auto (FR-003) — exactly one resolved label.
- No board/motion signature change (FR-005): the phase reaches the label via an internal `labelPhase` (default `restPhase`), not a new parameter on a public entry point.
- Commit after each task or logical group; stop at any checkpoint to validate the story independently.
