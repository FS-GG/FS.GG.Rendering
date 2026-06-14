---
description: "Task list for Build the Rendering Test Harness (Migration Stage R5)"
---

# Tasks: Build the Rendering Test Harness (Migration Stage R5)

**Input**: Design documents from `/specs/004-rendering-harness/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: The harness's **pure** logic (`RunPlan`, `Evidence`, degradation/proof-level) is unit-
tested in `tests/Rendering.Harness.Tests` per constitution Principle V (FSI-first: `.fsi` → tests →
`.fs`). The tier *executors* (which need a desktop/GL) are validated via the CLI (quickstart V1–V6).

**Organization**: Tasks grouped by user story (the tiers) for independent delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: different files, no dependency. **[Story]**: US1=T0/T1, US2=T2, US3=T3, US4=T-uinput.
- Paths under `tests/Rendering.Harness/` (CLI) and `tests/Rendering.Harness.Tests/` (unit).
- Builds on R4 seams (`SkiaViewer.run`/`runBounded`/`captureScreenshotEvidence`,
  `ControlsElmish.Perf.runScript`/`captureRespondsProof`, `FrameMetrics`, `Testing` validators);
  shells to installed X11 tools. `artifacts/` is gitignored.

---

## Phase 1: Setup

- [x] T001 Create `tests/Rendering.Harness/Rendering.Harness.fsproj` (`OutputType Exe`, `net10.0`, ProjectReferences to `SkiaViewer`, `Controls.Elmish`, `Testing`, `Scene`, `Controls`; `SkiaSharp` PackageReference) and `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` (Expecto + a ProjectReference to the harness); add both to `FS.GG.Rendering.slnx`; `dotnet build` the two projects succeeds.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared pure core + evidence + probe + CLI every tier needs.

**⚠️ CRITICAL**: Blocks all tier work (US1–US4).

- [x] T002 Author `tests/Rendering.Harness/Evidence.fsi` then `Evidence.fs`: the `run.json`/`metrics.csv`/`summary.md` model + writers (`proofLevel`, `authoritativeFor`, `notAuthoritativeFor`, env/present facts, percentiles) per `contracts/run-json.schema.md`, reusing `Testing.parseScreenshotEvidenceRecord`/`validateScreenshotEvidence`.
- [x] T003 Author `tests/Rendering.Harness/Probe.fsi` then `Probe.fs`: env probe recording display/GL/refresh/extension facts + effective backend, shelling `xdpyinfo`/`xrandr`/`xinput` and detecting Wayland-vs-X11 (FR-002, FR-014).
- [x] T004 Author `tests/Rendering.Harness/RunPlan.fsi` then `RunPlan.fs` — **pure**: tier → `RequiredCapability`, `Assertions`, `ClaimableProof`, `NotAuthoritativeFor`, and `Degradation (Run|Skip|FailClassified)` computed from `ProbeFacts`; `vsync-faithful` claimable only when present facts complete (FR-007/008/012/014).
- [x] T005 [P] Author `tests/Rendering.Harness.Tests` unit tests for `RunPlan` (proof-level gating incl. vsync-faithful, degradation Run/Skip/FailClassified, `NotAuthoritativeFor` non-empty for non-probe tiers) and the `Evidence` schema — the FSI-first semantic tests; these join the default local tier (Principle V).
- [ ] T006 Author `tests/Rendering.Harness/X11.fsi` then `X11.fs`: edge interpreter shelling `xdotool` (window search + XTEST), `maim`/`xwd` (capture), `xrandr` (modes); unsets `WAYLAND_DISPLAY` for the viewer process.
- [x] T007 Author `tests/Rendering.Harness/Cli.fs` (`[<EntryPoint>]`): `argv` dispatch for `probe|offscreen|live-x11|perf|input` + common flags (`--out`/`--scene`/`--json`) + exit codes (0 pass/clean-skip, 1 fail, 2 usage) per `contracts/cli.schema.md`; wire the `probe` subcommand end-to-end.

**Checkpoint**: `probe` runs and writes a valid `run.json`; pure unit tests green.

---

## Phase 3: User Story 1 - Deterministic evidence (T0/T1) (Priority: P1) 🎯 MVP

**Goal**: Fast deterministic + offscreen evidence with no live desktop — the default inner loop.

**Independent Test**: With no `DISPLAY`, `offscreen` completes in seconds and emits a `run.json` (proof level + `notAuthoritativeFor`) + non-blank PNG + `summary.md` (quickstart V1).

### Implementation for User Story 1

- [x] T008 [US1] Author `tests/Rendering.Harness/Tiers.fsi` then `Tiers.fs` **T0** executor: pure scene/control render determinism + tree equality + retained routing via `ControlsElmish.Perf.runScript`/`captureRespondsProof` and `SkiaViewer.runBounded` (offscreen); assert non-blank offscreen PNG (FR-004); wire the `offscreen` subcommand T0 path.
- [x] T009 [US1] Add the **T1** executor in `Tiers.fs`: offscreen GPU/CPU readback via `SkiaViewer.captureScreenshotEvidence`; assert non-blank; emit `proofLevel:"offscreen-pixels"` with `notAuthoritativeFor:["desktop-visibility"]` (FR-005); extend the `offscreen` subcommand.
- [ ] T010 [US1] Author `tests/Rendering.Harness/Input.fsi` then `Input.fs` with the `pure` backend (deterministic, mapped to `Perf.runScript`/`captureRespondsProof`) and wire the `input --backend pure` path (FR-010).
- [x] T011 [US1] Author `tests/Rendering.Harness.Tests` in-process tests for the deterministic tiers: run the **T0** executor (determinism, tree equality, retained routing, non-blank offscreen PNG) and the headless-feasible **T1** offscreen readback in-process and assert outcomes, so the deterministic tiers join the default local tier — not just the CLI quickstart (Principle V). Live/timing tiers (T2/T3/T-uinput) remain CLI-validated (environment-blocked).
- [x] T012 [US1] Validate quickstart V1 (no `DISPLAY` → fast T0/T1, non-blank PNG, `run.json`+`summary.md` with proof scope — SC-001/004) and V2 (`probe` records `effectiveBackend` — SC-007).

**Checkpoint**: deterministic inner loop works headless — MVP.

---

## Phase 4: User Story 2 - Live desktop evidence (T2) (Priority: P2)

**Goal**: Prove real desktop visibility + live input on X11.

**Independent Test**: With `DISPLAY` set, `live-x11` finds the window, captures a non-blank window PNG, injects mouse/keyboard, and records a visible state change (quickstart V3).

### Implementation for User Story 2

- [ ] T013 [US2] Add the **T2** executor in `Tiers.fs`: launch `SkiaViewer.run` with `WAYLAND_DISPLAY` unset, discover the window (`X11.fs`/xdotool), capture a non-blank window PNG (maim), inject mouse+keyboard via XTEST, confirm + record a visible state change; classify a Wayland-effective run as failed (FR-006/014); wire the `live-x11` subcommand.
- [ ] T014 [US2] Add the `x11-xtest` input backend to `Input.fs` (xdotool, default live) and wire `input --backend x11-xtest` (FR-010).
- [ ] T015 [US2] Validate quickstart V3 (window found, non-blank PNG, input injected, visible change; Wayland-effective → classified/failed) (SC-002).

**Checkpoint**: live X11 smoke + real input works.

---

## Phase 5: User Story 3 - Faithful performance evidence (T3) (Priority: P3)

**Goal**: Trustworthy frame-pacing metrics that refuse to overclaim vsync.

**Independent Test**: A perf mode persists per-frame + percentile metrics with present facts; a run missing vblank facts is **not** labeled `vsync-faithful` (quickstart V4).

### Implementation for User Story 3

- [ ] T016 [US3] Author `tests/Rendering.Harness/Perf.fsi` then `Perf.fs` with modes `throughput`/`paced-60`/`paced-native`/`stress-resize`/`input-latency` (each declaring deterministic/live-host/timing) and the **T3** executor via `SkiaViewer.runBounded` + `FrameMetrics.Paint/ComposeDuration`; persist per-frame + p50/p95/p99 metrics with display/swap facts; refuse `vsync-faithful` when present facts are missing (FR-007/009); wire the `perf` subcommand.
- [ ] T017 [US3] Re-home the 17 R4-skipped `Feature109` perf tests as harness perf goldens written to the gitignored `artifacts/` run dir (not repo-relative); update `SKIPPED-TESTS.md` to reference the harness perf modes.
- [ ] T018 [US3] Validate quickstart V4 (`perf --mode paced-native` persists percentile + present facts; removing vblank facts → not `vsync-faithful`) (SC-003).

**Checkpoint**: faithful, non-overclaiming perf evidence.

---

## Phase 6: User Story 4 - Kernel-input fidelity, opt-in (T-uinput) (Priority: P4)

**Goal**: Opt-in kernel input that degrades cleanly when `/dev/uinput` is absent.

**Independent Test**: `input --backend uinput` where `/dev/uinput` is absent exits 0 with `status:"skipped"` + a `skipReason`, no crash (quickstart V5).

### Implementation for User Story 4

- [ ] T019 [US4] Add the `uinput` input backend to `Input.fs` (ydotool) and the **T-uinput** executor in `Tiers.fs`: opt-in; when `/dev/uinput` absent, emit `status:"skipped"` + `skipReason` naming host device pass-through — never crash (FR-008); wire `input --backend uinput`.
- [ ] T020 [US4] Validate quickstart V5 (uinput backend, `/dev/uinput` absent → exit 0, `status:"skipped"`, `skipReason`, no crash) (SC-005).

**Checkpoint**: kernel-input tier present and degrades cleanly.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T021 [P] Write `docs/harness/capability-baseline.md` — the recorded dev-environment capability baseline (FR-015), noting the probe re-measures per run.
- [ ] T022 Run the full quickstart V1–V6 and confirm SC-001..SC-007; confirm `tests/Rendering.Harness.Tests` is green in the default local tier, that **no tier is required for a routine rendering change** (FR-012), that no tier references a governance path (FR-013), and that this feature changed **no `src/**` `.fsi`** (no new product public API — FR-011).
- [ ] T023 [P] Update `README.md` (+ `docs/`) to document the harness — how to run each tier, the capability-not-gate framing, and the no-overclaim evidence contract; cross-reference `SKIPPED-TESTS.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** → **Foundational (P2)** → **User Stories (P3–P6)** → **Polish (P7)**.
- US1–US4 all depend on Foundational (Evidence/Probe/RunPlan/X11/Cli). After Foundational, the
  tiers are largely independent but share `Tiers.fs`/`Input.fs` (so same-file tasks sequence).

### Within / across user stories

- Foundational: T002/T003 parallel-ish; T004 (RunPlan) then T005 (its tests); T006 (X11), T007 (Cli).
- US1: T008 → T009 (same `Tiers.fs`, sequential) ; T010 (`Input.fs`) parallel to T008/T009 ; T011 (deterministic-tier tests, after T008/T009) ; T012 validate.
- US2: T013 (Tiers.fs) → T014 (Input.fs) → T015 validate. US3: T016 → T017 → T018. US4: T019 → T020.
- US3/US4 touch `Tiers.fs`/`Input.fs` so they sequence after US1/US2 edits to those files.

### Parallel Opportunities

- Foundational T002 and T003 run in parallel (different files); T005 `[P]` (test file).
- Polish T021 and T023 `[P]`; T022 runs after the tiers exist.

---

## Parallel Example: Foundational

```bash
Task: "Author Evidence.fsi/.fs (T002)"
Task: "Author Probe.fsi/.fs (T003)"
```

---

## Implementation Strategy

### MVP First (User Story 1 — T0/T1)

1. Setup (T001) + Foundational (T002–T007) → `probe` works, pure tests green.
2. US1 (T008–T012) → deterministic + offscreen evidence headless.
3. **STOP and VALIDATE**: quickstart V1 — the fast default inner loop is the usable MVP.

### Incremental Delivery

1. Setup + Foundational → CLI + evidence + probe + pure core.
2. US1 (T0/T1) → MVP. 3. US2 (T2 live). 4. US3 (T3 perf). 5. US4 (T-uinput).
6. Polish → capability baseline + full V1–V6 + docs.

---

## Notes

- FSI-first: author each module's `.fsi` before its `.fs`; pure logic gets semantic tests (Principle I/V).
- Capability, not a gate (FR-012): only `Rendering.Harness.Tests` (pure) joins the default local tier;
  the tiers run on demand.
- No overclaim (SC-004) and clean degradation (SC-005) are enforced by the pure `RunPlan`, so they
  are unit-tested without a desktop.
