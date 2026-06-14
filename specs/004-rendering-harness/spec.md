# Feature Specification: Build the Rendering Test Harness (Migration Stage R5)

**Feature Branch**: `004-rendering-harness`

**Created**: 2026-06-14

**Status**: Draft

**Change Classification**: **Infrastructure** — adds a new, self-contained harness project and
its evidence-artifact contract; it consumes existing product seams and does **not** change the
product's public API. Any public harness module still carries a curated `.fsi` (constitution
Principle II).

**Input**: User description: "next part FS.gg"

## Context

Stage R5 of the migration. R1–R4 are done: the product source is imported and builds, and the
default local test tier passes. R5 builds the **comprehensive rendering / performance / mouse /
keyboard test harness** as deliberate infrastructure — the productive use of the time saved by
not bulk-importing the legacy suite.

The harness is a **capability, not a mandatory gate**: its fast deterministic tiers are the
default inner loop; the heavier live, performance, and kernel-input tiers are opt-in and run
only when a claim needs that level of evidence. *Comprehensive does not mean always fully used.*
A defining rule: **every artifact states what it proves and what it does not**, so screenshots
and timings can never overclaim.

It builds on the seams imported in R4 (verified present): `Viewer.captureScreenshotEvidence`,
`Viewer.runBounded`, `ControlsElmish.captureRespondsProof`, `ControlsElmish.Perf.runScript`,
`FrameMetrics`. "Users" are maintainers/contributors who need trustworthy rendering evidence
on demand, and the CI that may invoke selected tiers later (Stage R6).

### Tiers

| Tier | Purpose | Display dependency | Authoritative for |
|---|---|---|---|
| T0 | Pure scene/control render + retained routing | none | determinism, tree equality, routing, non-blank offscreen PNGs |
| T1 | Offscreen GPU/CPU screenshot readback | offscreen / Skia | renderer pixel output (not desktop visibility) |
| T2 | Live X11 window smoke + XTEST input | X11 server + WM | window creation, visibility, focus, real mouse/keyboard, desktop screenshot |
| T3 | Faithful frame pacing / performance | Xorg/KMS with real vblank | vsync, frame interval, paint/compose/swap timing |
| T-uinput | Kernel-level input fidelity (opt-in) | `/dev/uinput` + `/dev/input` | evdev/libinput input path |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic evidence on demand (T0/T1) (Priority: P1)

A contributor runs the harness with no live desktop and gets fast, deterministic evidence:
the scene/control render tree is stable and correctly routed (T0), and the offscreen renderer
produces non-blank pixel output (T1) — each run emitting a machine artifact and a human summary
that declare exactly what was proven.

**Why this priority**: T0/T1 are the **default inner loop** — the tiers run routinely. They need
no desktop, so they work in CI and headless dev. They are the minimum viable harness: even with
nothing else, deterministic + offscreen evidence is immediately useful and unblocks the rest.

**Independent Test**: With no `DISPLAY`, run the deterministic and offscreen subcommands; both
complete quickly and emit a `run.json` + `summary.md` declaring their proof level.

**Acceptance Scenarios**:

1. **Given** no live desktop, **When** the T0 subcommand runs, **Then** it verifies render-tree
   determinism, tree equality, and retained routing, and emits a non-blank offscreen PNG.
2. **Given** offscreen rendering, **When** the T1 subcommand runs, **Then** it captures renderer
   pixel output via readback and asserts the image is non-blank.
3. **Given** any harness run, **When** its evidence is written, **Then** the artifact declares
   `proofLevel`, what it **is** authoritative for, and what it is **not** (e.g. T1 is authoritative
   for renderer pixels, **not** desktop visibility).
4. **Given** the environment probe, **When** a run starts, **Then** it records display / GL /
   refresh / extension facts and the effective backend (X11 vs Wayland).

---

### User Story 2 - Live desktop evidence (T2) (Priority: P2)

A maintainer needs proof the viewer actually shows on a real desktop and responds to real input:
the harness launches the viewer on X11 (Wayland disabled for the process), finds the window,
captures a non-blank window screenshot, injects real mouse and keyboard via XTEST, and confirms a
visible state change.

**Why this priority**: T2 is the only tier that proves real desktop visibility, focus, and live
input — things offscreen tiers cannot. It is opt-in (heavier, needs a live X11 desktop) but is
the authoritative live-host evidence. Builds on US1's project + evidence schema.

**Independent Test**: With `DISPLAY` set, run the live-x11 subcommand; it produces a non-blank
window PNG and an artifact recording the injected input and the observed visible change.

**Acceptance Scenarios**:

1. **Given** a live X11 desktop, **When** the T2 subcommand runs, **Then** the viewer launches
   with Wayland disabled for the process, the window is discovered, and a non-blank window PNG is
   captured.
2. **Given** the live window, **When** mouse and keyboard input are injected via XTEST, **Then**
   the harness confirms and records a visible state change.
3. **Given** an effective Wayland backend (despite the request), **When** the run is classified,
   **Then** it is failed or flagged as Wayland — not silently accepted as X11.

---

### User Story 3 - Faithful performance evidence (T3) (Priority: P3)

A maintainer needs trustworthy frame-pacing/performance numbers: the harness runs a bounded
frame set in a chosen performance mode and persists per-frame and percentile metrics **together
with** the display and swap-control facts — and refuses to call a run "vsync faithful" when those
facts are absent.

**Why this priority**: Performance claims are the easiest to fake; T3's value is *faithful*
timing tied to real vblank/swap facts. Opt-in and timing-sensitive. Lower than live smoke because
it answers "how fast/faithful," not "does it work at all."

**Independent Test**: Run a perf mode over a bounded frame set; the artifact carries per-frame +
percentile metrics plus display/swap facts, and a run lacking vblank facts is **not** labeled
vsync-faithful.

**Acceptance Scenarios**:

1. **Given** a bounded frame set, **When** a T3 perf mode runs, **Then** per-frame and percentile
   (e.g. p50/p95/p99) metrics are persisted with the display and swap-control facts.
2. **Given** a run with missing vblank/swap facts, **When** it is labeled, **Then** it is
   **refused** the "vsync faithful" classification.
3. **Given** the perf modes (`throughput`, `paced-60`, `paced-native`, `stress-resize`,
   `input-latency`), **When** each runs, **Then** it declares whether it is deterministic,
   live-host, or timing evidence.

---

### User Story 4 - Kernel-input fidelity, opt-in (T-uinput) (Priority: P4)

A maintainer who needs evdev/libinput-level input fidelity can opt into the kernel-input tier;
when `/dev/uinput` is absent it degrades cleanly with a clear "opt-in, devices unavailable"
result instead of crashing.

**Why this priority**: Highest-fidelity input evidence, but needs host device pass-through that
the default dev environment lacks. Lowest priority — rarely needed and environment-gated — but
must fail gracefully.

**Independent Test**: Run the input subcommand with the `uinput` backend where `/dev/uinput` is
absent; it reports opt-in-unavailable cleanly and the harness continues.

**Acceptance Scenarios**:

1. **Given** `/dev/uinput` absent, **When** the `uinput` input backend is requested, **Then** the
   tier reports it is opt-in and unavailable (requires host device pass-through), without crashing.
2. **Given** the input-script model, **When** a script runs, **Then** it can target the `pure`
   backend (deterministic, mapped to `Perf.runScript` / `captureRespondsProof`), the `x11-xtest`
   backend (default live), or the opt-in `uinput` backend.

### Edge Cases

- No `DISPLAY` / headless → T0/T1 still run; T2/T3 report "skipped: no live desktop" with the
  probe facts, never a crash.
- `WAYLAND_DISPLAY` set → unset for the viewer process; record the effective backend; fail/flag a
  Wayland-effective run rather than silently proceeding.
- Offscreen PNG is blank → T0/T1 fail the non-blank assertion.
- Missing vblank/swap-control facts → T3 refuses the "vsync faithful" label.
- Window not discoverable on X11 → T2 fails with the probe facts, not a hang.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide a dedicated `tests/Rendering.Harness/` project, separate from any
  governance path, exposing CLI subcommands `probe`, `offscreen`, `live-x11`, `perf`, and `input`.
- **FR-002**: An environment probe MUST record display / GL / refresh / extension facts and the
  effective backend (X11 vs Wayland) for each run.
- **FR-003**: An evidence-artifact contract MUST be produced per run: `run.json` carrying
  `proofLevel`, `authoritativeFor`, `notAuthoritativeFor`, display/renderer/present facts, and
  timing percentiles, plus `metrics.csv` and a human `summary.md`. **Every artifact MUST state
  what it proves and what it does not.**
- **FR-004**: **T0** MUST verify pure scene/control render determinism, tree equality, and
  retained routing with no display, and produce non-blank offscreen PNGs.
- **FR-005**: **T1** MUST capture offscreen GPU/CPU screenshot readback and assert non-blank
  output; it is authoritative for renderer pixel output, **not** desktop visibility.
- **FR-006**: **T2** MUST launch the viewer on X11 with Wayland disabled for the process,
  discover the window, capture a non-blank window PNG, inject mouse and keyboard via XTEST, and
  confirm a visible state change.
- **FR-007**: **T3** MUST run a bounded frame set and persist per-frame and percentile metrics
  with the display and swap-control facts, and MUST refuse to label a run "vsync faithful" when
  those facts are missing.
- **FR-008**: **T-uinput** MUST be opt-in and degrade cleanly when `/dev/uinput` is absent
  (clear unavailable result, documented host device pass-through requirement) — never crash.
- **FR-009**: Performance modes `throughput`, `paced-60`, `paced-native`, `stress-resize`, and
  `input-latency` MUST each declare whether the run is deterministic, live-host, or timing
  evidence.
- **FR-010**: Declarative input scripts MUST support a `pure` backend (deterministic, mapped to
  `Perf.runScript` / `captureRespondsProof`), an `x11-xtest` backend (default live), and an
  opt-in `uinput` backend.
- **FR-011**: The harness MUST build on the R4-imported seams (`captureScreenshotEvidence`,
  `runBounded`, `captureRespondsProof`, `Perf.runScript`, `FrameMetrics`) and MUST NOT add new
  product public API.
- **FR-012**: The harness MUST be a capability, not a gate: T0/T1 are the default inner loop;
  T2/T3/T-uinput are opt-in; **no harness tier is required for a routine rendering change**.
- **FR-013**: No harness tier may depend on any governance repository; the project is separate
  from any governance path.
- **FR-014**: The harness MUST unset `WAYLAND_DISPLAY` for the viewer process, record the
  effective backend, and fail/classify a Wayland-effective run rather than silently proceeding.
- **FR-015**: A capability baseline for the development environment MUST be recorded.

### Key Entities *(include if feature involves data)*

- **Harness project**: the `tests/Rendering.Harness/` CLI and its tiers.
- **Environment probe record**: display/GL/refresh/extension facts + effective backend per run.
- **Evidence artifact**: `run.json` (+ `metrics.csv`, `summary.md`) with proof level and
  authoritative/non-authoritative scope.
- **Performance mode**: a named perf run declaring its evidence kind.
- **Input script**: a declarative input sequence with a selectable backend (pure / x11-xtest / uinput).
- **Tier**: T0/T1/T2/T3/T-uinput with its display dependency and authoritative scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: T0 and T1 run with **no** live desktop and are fast enough for the default inner
  loop — a single `offscreen` run completes in **under ~10 seconds** on the dev baseline —
  emitting a `run.json` + `summary.md` per run.
- **SC-002**: T2 launches the viewer on X11, discovers the window, captures a non-blank window
  PNG, injects mouse and keyboard, and confirms a visible state change.
- **SC-003**: T3 persists per-frame and percentile metrics with display/swap-control facts; a run
  missing those facts is **not** labeled "vsync faithful."
- **SC-004**: 100% of evidence artifacts declare `proofLevel` and what they are and are **not**
  authoritative for (zero artifacts that could overclaim).
- **SC-005**: The kernel-input tier degrades cleanly (clear opt-in-unavailable result) when
  `/dev/uinput` is absent — zero crashes.
- **SC-006**: No harness tier is required to make a routine rendering change, and no tier depends
  on a governance repository.
- **SC-007**: The environment probe records display/GL/refresh/backend per run; a Wayland-effective
  run is classified or failed, never silently accepted as X11.

## Assumptions

- **Scope = full R5 harness** (user-chosen): all tiers (T0–T3 + T-uinput), all five CLI
  subcommands, perf modes, input backends, the evidence schema, and the recorded capability
  baseline. CI wiring of tiers at chosen frequencies is Stage R6; bridge (R7) and rebrand (R8)
  are out of scope.
- The harness builds on the R4-imported viewer/controls/testing seams (verified present in
  `src/SkiaViewer` and `src/Controls.Elmish`).
- Dev-environment capability baseline (measured 2026-06-14): `DISPLAY=:1` live with XTEST/Present/
  RANDR/DRI3/XInput; real output `HDMI-A-1` 1920x1080 @ 119.93 Hz (T3 vsync feasible); hardware
  GL via AMD/Mesa (GL 4.6, direct rendering); X11/capture/perf toolchain installed; `/dev/uinput`
  and `/dev/input` **absent** (T-uinput opt-in); `WAYLAND_DISPLAY` set (harness unsets it).
- The 17 perf tests skipped during R4 (`Feature109` perf-corpus/baseline, see `SKIPPED-TESTS.md`)
  are re-homed under this harness's performance tier (T3 / perf modes).
- Wayland is unreliable for automation in this environment; **X11 is the automation path**.
