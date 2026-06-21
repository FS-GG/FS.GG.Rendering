# Rendering test harness — infrastructure justification

> Migration Stage R3 deliverable. The rendering / performance / mouse / keyboard test harness
> is recorded here as **deliberate infrastructure** — explicitly **not** an imported legacy
> test. "Deliberately light" (not bulk-importing the legacy suite) does not mean skimping on
> infrastructure: the harness is a first-class capability, built at Stage R5.

## Record

| Field | Value |
|---|---|
| Item | Rendering test harness (`tools/Rendering.Harness/`, planned) |
| Classification | **Deliberate infrastructure** — not an imported legacy test |
| Decision | **Build at Stage R5.** Its display-agnostic parts — environment probe, CLI skeleton, evidence schema — MAY scaffold earlier (as early as R3). Live (T2) and performance (T3) tiers come online once the viewer is imported (Stage R4). |
| Owner | rendering maintainer |
| Capability, not a gate | Fast deterministic tiers (T0/T1) are the default inner loop; heavier tiers are opt-in and run only when a claim needs that evidence. No harness tier is required for a routine rendering change. |

## Tiers (reference; full design at Stage R5)

| Tier | Purpose | Display dependency | Authoritative for |
|---|---|---|---|
| T0 | Pure scene/control render + retained routing | none | determinism, tree equality, routing, non-blank offscreen PNGs |
| T1 | Offscreen GPU/CPU screenshot readback | offscreen / Skia | renderer pixel output (not desktop visibility) |
| T2 | Live X11 window smoke + XTEST input | X11 server + WM | window creation, visibility, focus, real mouse/keyboard, desktop screenshot |
| T3 | Faithful frame pacing / performance | Xorg/KMS with real vblank | vsync, frame interval, paint/compose/swap timing |
| T-uinput | Kernel-level input fidelity (opt-in) | `/dev/uinput` + `/dev/input` | evdev/libinput input path |

## Relationship to imported tests

- `Parity.Tests` (rewrite-pending) and `ControlsPreview.Harness` (deferred) are **prior art**
  to fold into the harness tiers (notably T1 offscreen readback) — they are not imported as
  legacy tests. See [`deferral-ledger.md`](./deferral-ledger.md).
- The harness builds on seams from the imported viewer/controls/testing code at Stage R4
  (`Viewer.captureScreenshotEvidence`, `Viewer.runBounded`,
  `ControlsElmish.captureRespondsProof`, `ControlsElmish.Perf.runScript`, `FrameMetrics`).

## Out of scope here (Stage R3)

This record is a **decision**, not an implementation. No harness code, CLI, probe, or
evidence schema is written at this stage — that is Stage R5 (with optional display-agnostic
scaffolding once Stage R4 source lands).
