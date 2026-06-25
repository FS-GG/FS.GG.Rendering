# Feature Specification: Symbology Live Board Sample (M6)

**Feature Branch**: `193-symbology-live-board`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan." → the roadmap milestone **M6 — Live board sample**, the first deferred item after the completed M1–M5 of spec 192 (agent-driven unit-symbology design system).

> **Roadmap context.** Spec 192 shipped the pure `FS.GG.UI.Symbology` grammar, the `FS.GG.UI.Symbology.Render` headless bridge, the `fs-gg-symbology` skill, and an approved per-game symbol set from the M5 dry-run (`specs/192-agent-unit-symbology/readiness/dry-run/FinalSymbolSet.fsx`). The grammar produces still galleries today. M6 is the demonstration milestone: put that approved set on a **live, moving board** so a viewer can watch the symbols animate, and prove the board is byte-for-byte reproducible from a seed. M7 (legibility linter, Badge/Ring grammars, label text) remains backlog and is out of scope here.

## Change Classification

**Tier 2 (additive, internal).** This feature adds an `IsPackable=false` sample executable plus a matching test project; it consumes the existing `FS.GG.UI.*` public surface only and makes **no public API change** — no `.fsi` edits, no surface-area baseline changes (FR-012, SC-005). **Public API impact:** none. **Verification approach:** semantic tests over the sample's deterministic core (reproducibility, seed-sensitivity, on-board invariant) plus a captured seeded-evidence readiness artifact and an early real-run evidence smoke (Success Criteria below). This classification is consistent with the `samples/CanvasDemo` precedent and is re-stated in [plan.md](./plan.md) (Constitution Check).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Watch the approved roster move on a live board (Priority: P1)

A designer or stakeholder who approved a symbol set in the M5 loop wants to *see it in motion* — the same units, on a board, animating continuously — rather than only as a static gallery PNG. They run the sample and a window opens showing the roster of unit-symbols laid out on a board, each animating per its motion channel (e.g. a heading-driven sweep or speed-driven pulse), updating smoothly frame to frame.

**Why this priority**: This is the entire point of M6 — turning the approved still set into a moving board is the new, demonstrable value. Without it there is no milestone. Everything else supports or proves this.

**Independent Test**: Launch the sample in interactive mode on a host with a live window; observe the roster rendered on a board with continuous, smooth motion that does not freeze, flicker, or drift off-board. On a headless host the interactive mode degrades to a clear "skipped — no live window" message and a zero exit, exactly like the CanvasDemo precedent.

**Acceptance Scenarios**:

1. **Given** a host with a live window/GL capability, **When** the sample is launched in interactive mode, **Then** a board appears showing every unit in the approved roster as its fixed-grammar symbol, each animating continuously and smoothly between frames.
2. **Given** a host with no live window (headless/CI), **When** the sample is launched in interactive mode, **Then** the sample prints a clear "interactive mode skipped — no live window" notice and exits successfully without error.
3. **Given** the board is running, **When** motion advances over many frames, **Then** each symbol stays legible and on-board (no symbol drifts off the visible area, collapses to zero area, or loses its identity channels).

---

### User Story 2 - Reproduce the board deterministically from a seed (Priority: P1)

An engineer needs the live board to be *evidence-grade*: the same seed and the same scripted tick/input sequence must produce a byte-identical board every run, so the sample can be validated in headless CI with no wall clock and no GPU. They run the sample's headless evidence mode and get a single canonical fingerprint; running it again yields the same fingerprint.

**Why this priority**: Determinism is the hard constraint inherited from spec 192 (no wall-clock, no IO in the simulation/scene path) and is what lets M6 close on captured evidence per the constitution. A board that looks nice but cannot be reproduced is not an acceptable milestone exit.

**Independent Test**: Run the sample's evidence subcommand twice from the same seed; confirm both runs print the same fingerprint and the process reports reproducibility (byte-identical), returning a zero exit. Run from a different seed and confirm the fingerprint differs (the seed actually drives the world).

**Acceptance Scenarios**:

1. **Given** a fixed seed and scripted tick sequence, **When** the headless evidence mode runs twice, **Then** both runs emit the same canonical board fingerprint and the sample reports "reproducible".
2. **Given** two different seeds, **When** the headless evidence mode runs for each, **Then** the two fingerprints differ (the seed materially affects the board).
3. **Given** the evidence run completes, **When** its exit status is inspected, **Then** it is zero on reproducible success and non-zero if the two runs ever diverge.

---

### User Story 3 - Build, run, and register the sample like the other samples (Priority: P2)

A developer building the solution expects the new board sample to be a first-class, registered project: it builds as part of the solution, references the symbology libraries the same way the existing samples reference their libraries (in-tree project references now, package-feed swap deferred to publish), and runs from a single documented command with discoverable subcommands.

**Why this priority**: Discoverability and parity with the existing sample set (`samples/CanvasDemo`, etc.) make the milestone usable and maintainable, but the demonstrable value (US1) and the evidence (US2) are what *define* M6; this story packages them.

**Independent Test**: From a clean checkout, build the solution and confirm the new sample project is included and compiles; run it with no arguments (or its evidence subcommand) and confirm it produces the reproducible-board output; run it with an unknown subcommand and confirm a clear usage message.

**Acceptance Scenarios**:

1. **Given** the solution is built, **When** the build completes, **Then** the new board sample project is part of the solution and compiles with no new errors.
2. **Given** the sample, **When** it is run with no arguments, **Then** it runs the deterministic headless evidence path by default (matching the CanvasDemo convention).
3. **Given** the sample, **When** it is run with an unrecognized subcommand, **Then** it prints a clear usage hint listing the supported subcommands and exits non-zero.

---

### Edge Cases

- **Headless host, interactive request**: interactive mode must degrade gracefully (notice + zero exit), never crash or block waiting for a window.
- **Empty or single-unit roster**: the board must still render and remain reproducible; a degenerate roster must not produce an empty/blank board passed off as success. (The shipped roster is a fixed 6–10-unit literal, so an *empty* roster is structurally unreachable in this sample; the **single-unit/degenerate** path is the one exercised by a test — see FR-011 and the non-empty-board test.)
- **Symbol reaches a board boundary under motion**: motion must keep every symbol within the visible board (bounce, wrap, or clamp — chosen and documented), never silently clip a unit out of view.
- **Zero-area or degenerate symbol** (a unit whose channels collapse): must fall back to a visible placeholder rather than rendering nothing, consistent with the symbology grammar's zero-area behavior.
- **Two runs diverge**: any non-reproducible evidence run must fail loud with a non-zero exit and a diff-style message, never be reported as success.
- **Motion phase source**: motion must be driven by the simulation's accumulated step/phase, never by a wall clock, so headless replay is exact.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The sample MUST present the **approved M5 symbol set** (the per-game roster→symbol mapping converged on in the spec-192 dry-run) as a board of unit-symbols, reusing the fixed symbology grammar unchanged.
- **FR-002**: The board MUST animate continuously when run interactively, advancing each symbol's motion every frame and rendering smoothly between simulation steps (no per-frame jump or freeze).
- **FR-003**: Motion MUST be derived solely from a fixed-timestep simulation advanced by accumulated elapsed steps — no wall-clock, randomness-at-render, or external IO in the simulation or scene-production path.
- **FR-004**: The sample MUST provide a **headless evidence mode** that folds a seeded, scripted tick/input sequence and emits a single canonical board fingerprint.
- **FR-005**: Two evidence runs from the same seed and script MUST produce byte-identical fingerprints; the sample MUST report reproducibility and exit zero on match, non-zero on any divergence.
- **FR-006**: Different seeds MUST produce different board states (the seed MUST materially drive the simulation), so the evidence is not trivially constant.
- **FR-007**: The sample MUST provide an interactive mode that opens a live board window when the host supports a persistent window, and MUST degrade to a clear "skipped — no live window" notice with a zero exit when it does not.
- **FR-008**: The sample MUST be a registered project in the solution, building as part of the normal solution build with no new build errors.
- **FR-009**: The sample MUST reference the symbology libraries via in-tree project references for now, with the package-feed swap explicitly deferred to publish (same convention as the existing canvas sample).
- **FR-010**: The sample MUST expose discoverable subcommands (at minimum an evidence/default path and an interactive path) and MUST print a clear usage hint and non-zero exit for an unrecognized subcommand.
- **FR-011**: The sample MUST keep every symbol legible and within the visible board across the full motion run — no symbol may drift off-board, collapse to zero area without a placeholder, or lose its identity channels.
- **FR-012**: The sample MUST NOT modify the public surface of the existing symbology libraries or any other core package; it consumes the existing public API only (zero surface drift on existing baselines).
- **FR-013**: The milestone MUST close on captured, reproducible **seeded evidence** (the fingerprint plus a record that two runs matched), stored as a readiness artifact for the feature.
- **FR-014**: Optional raw input handling (pointer/key affecting the board) MAY be included; if present it MUST be reconstructed deterministically so headless replay remains exact, and MUST NOT be required for either the evidence path or the milestone exit.

### Key Entities *(include if data involved)*

- **Approved symbol set**: the per-game roster of units and their stat→channel mapping approved in the M5 loop; the input the board renders. Fixed for this milestone (the loop already converged on it).
- **Board**: the spatial arrangement of the roster's unit-symbols (a laid-out collection of symbols) that the sample renders and animates.
- **Simulation world**: the deterministic, seed-initialized state advanced by a fixed timestep that supplies each symbol's motion phase/position for the current frame.
- **Evidence fingerprint**: the canonical, content-addressable identity of a rendered board, used to prove reproducibility across runs.
- **Seed + script**: the seed and the scripted tick/input sequence that together fully determine a headless run's board.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Running the sample's headless evidence mode twice from the same seed yields the **same** board fingerprint in 100% of runs, and the sample reports reproducibility with a zero exit.
- **SC-002**: Running the evidence mode from two different seeds yields **different** fingerprints, confirming the seed drives the board.
- **SC-003**: On a live-window host, the interactive board displays **every** unit in the approved roster, animating continuously, with no symbol leaving the visible board over a sustained run.
- **SC-004**: On a headless host, interactive mode exits successfully (zero) with a clear "skipped — no live window" notice and never blocks or crashes.
- **SC-005**: The sample builds as a registered solution project with **zero** new build errors and **zero** drift on existing package surface baselines.
- **SC-006**: A reproducible seeded-evidence artifact for the board is captured under the feature's readiness directory and is regenerable from the documented command.
- **SC-007**: A first-time developer can build and produce the reproducible-board evidence using a single documented command, with unrecognized subcommands producing a clear usage message.

## Assumptions

- The "next item in the plan" is the source design report's roadmap **M6 — Live board sample**, the first item deferred by spec 192 (which scoped only M1–M5); M7 (legibility linter, Badge/Ring grammars, label text) remains backlog and out of scope.
- The approved symbol set and its roster→channel mapping from the M5 dry-run are reused as-is; this milestone demonstrates, it does not re-open, the symbology grammar or the per-game mapping.
- The sample mirrors the established `samples/CanvasDemo` precedent: a pure fixed-timestep simulation, a volatile animated canvas surface, a default headless evidence subcommand, and a graceful interactive fallback — that precedent and its live-clock/canvas prerequisites (spec 191) are already shipped.
- Headless/CI hosts have no live window or GPU; interactive mode is expected to skip there, and all milestone evidence is produced via the headless deterministic path.
- In-tree project references are the integration mechanism for now; swapping to package-feed references is deferred to publish, consistent with how the existing samples are wired.
- "Continuous, smooth motion" means interpolating between fixed simulation steps for rendering, as the existing canvas sample does; no fixed frame-rate or fps guarantee is asserted beyond visual smoothness.
- Raw interactive input on the board is optional and, if added, is purely a demonstrative extra that must not affect the deterministic evidence path.

## Dependencies

- The pure symbology grammar library and its headless render bridge shipped by spec 192 (M1–M3), consumed via their existing public API only.
- The approved final symbol-set mapping from the spec-192 M5 dry-run.
- The embedded animated canvas control and fixed-timestep loop primitives shipped by spec 191, used the same way the existing canvas sample uses them.
- The existing sample/solution conventions (project registration, default-evidence subcommand, graceful interactive fallback, package surface gate).
