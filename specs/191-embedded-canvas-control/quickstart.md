# Quickstart & Validation Guide: Embedded Canvas Control

**Feature**: 191-embedded-canvas-control | **Date**: 2026-06-25

Runnable scenarios that prove the feature end-to-end, mapped to the three user stories. Implementation
detail lives in [contracts/canvas-control.md](./contracts/canvas-control.md) and the eventual
`tasks.md`; this file is the run/validation guide.

## Prerequisites

- .NET `net10.0` SDK; repository builds with the standard solution tooling.
- GL-dependent (live paint) suites require an X11 display: run under `DISPLAY=:1`.
- Pure suites (`tests/Canvas.Tests`: `Elements`, `Loop`) run headless without GL.

## Build & test commands

```bash
# Build the affected projects
dotnet build src/Controls/Controls.fsproj
dotnet build src/Canvas/Canvas.Lib.fsproj

# Pure logic — no GL needed
dotnet test tests/Canvas.Tests

# Render / cache / input — GL-gated
DISPLAY=:1 dotnet test tests/Controls.Tests --filter Feature191
DISPLAY=:1 dotnet test tests/Controls.Tests --filter SurfaceArea   # Tier-1 baseline check

# Run the embedded sample
DISPLAY=:1 dotnet run --project samples/CanvasDemo
```

## Scenario 0 — Decision spike (Foundational, before US2)

**Goal**: confirm the byte-identity + fingerprint-sensitivity hypotheses before cache-isolation work
depends on them.

1. Author a static scene (red rectangle + circle) and place a `Canvas.create [ Canvas.scene s ]` inside
   a `stack` with a sibling button/label.
2. Render through a real host (`DISPLAY=:1`).

**Expected**: the scene paints inside the canvas box, translated to the box origin, clipped to the box;
the sibling chrome lays out normally. Changing the scene changes `hashScene` (cache miss); re-rendering
the same scene keeps the fingerprint (cache hit).

## Scenario 1 — Paint an embedded canvas (US1, P1)

**Validates**: FR-001, FR-002, FR-003, FR-013; SC-001, SC-002.

1. Build a control tree with a `canvas` carrying an application scene, embedded among themed controls.
2. Render headlessly twice with the same model.

**Expected**:
- The supplied drawing appears, positioned at the box origin and clipped to the box.
- Explicit `width`/`height` size the control; siblings lay out around it.
- A canvas with **no** `scene` shows a placeholder (no crash, no blank gap).
- Both renders produce **byte-identical** emitted scenes and identical fingerprints (golden-scene
  assertion, mirroring `RenderingTests.fs` / `Feature120FingerprintTests.fs`).

## Scenario 2 — Animate + interact without disturbing chrome (US2, P2)

**Validates**: FR-004, FR-005, FR-006, FR-007; SC-003, SC-004, SC-005.

1. Mark the canvas `Canvas.volatile'` and redraw its scene every frame; surround it with static chrome.
2. Drive pointer move/press/release/wheel inside the box and key events with the canvas focused.

**Expected**:
- Raw `PointerSample`s reach the `onPointer` handler for in-box events (and **not** for out-of-box
  events); raw key events reach the `onKey` handler only when the canvas is focused.
- Across frames where only the canvas changes, surrounding chrome stays `PictureCacheHits` and
  `WorkReduction.RepaintedNodeCount` excludes the chrome (cache-isolation assertion, mirroring
  `Feature116PictureCacheTests.fs`). Target: **0** chrome repaints (SC-003).
- A canvas whose scene is unchanged between two frames is recognized as a cache hit (not repainted).

## Scenario 3 — Element library + deterministic game loop (US3, P3)

**Validates**: FR-008, FR-009, FR-010, FR-011, FR-014; SC-006, SC-007.

1. Compose a canvas scene entirely from `Elements` (`rect`/`circle`/`sprite`/`at`/`layer`).
2. Drive a small sample (bouncing sprites / Pong) with `Loop.advance` from a fixed seed and a scripted
   input sequence; render `lerp Previous Current (Loop.alpha dt state)` into `Canvas.scene`.

**Expected**:
- Each `Elements` function is pure: same props ⇒ identical `Scene` (golden assertion).
- `Loop.advance` performs a deterministic number of `integrate` steps for a given accumulated time;
  an injected oversized `frameTime` (e.g. `5.0`) is clamped to `0.25` (no unbounded catch-up).
- The seeded sample run reproduces identical world state, scenes, and fingerprints across runs
  (repeatable seeded evidence).

## Held-input reconstruction pattern (D7 / FR-010)

`Canvas.onKey` delivers a raw `ViewerKey` + `KeyModifiers` per key event; `Canvas.onPointer` delivers a
raw `PointerSample`. Neither carries an explicit key-up, so a game reconstructs *level* (held) state in
its model and feeds it to the pure simulation:

- Keep the reconstructed input as model state — e.g. a paddle target, or a `Set<ViewerKey>` of keys
  believed held. Update it from the raw `Msg`s (`Key`/`Point`) inside `update`, never inside the
  simulation step.
- With a real key-up channel, maintain `Set<ViewerKey>` level state and a per-tick edge set cleared
  once per fixed step; distribute pointer-wheel deltas across the substeps a single `Loop.advance` runs.
- The fixed-step `integrate` reads **only** `(world, dt)` plus the captured input level — never the wall
  clock and never the raw event stream directly — so a seed + a scripted `Msg` sequence reproduces an
  identical world every run.

`samples/CanvasDemo/Game.fs` implements this: `PaddleTarget` is the reconstructed level state, updated
from `Key`/`Point` messages; `integrate` is a pure `(target) -> world -> dt -> world` transition; the
`evidence` entry point folds a scripted sequence from a seed and emits a reproducible fingerprint.

## Done-when (acceptance)

- [ ] Scenario 0 spike paints a static authored scene and shows fingerprint sensitivity.
- [ ] Scenario 1 golden + placeholder + determinism tests pass.
- [ ] Scenario 2 input-forwarding + cache-isolation tests pass (0 chrome repaints).
- [ ] Scenario 3 element-purity + loop-determinism/clamp + seeded-sample tests pass.
- [ ] Surface-area baselines updated and the drift test passes with the deliberate Tier-1 additions.
- [ ] No existing test deleted, skipped, or weakened; no swallowed exceptions at paint/route seams.
