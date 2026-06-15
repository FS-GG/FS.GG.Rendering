# Quickstart — Validating Feature 099 (Live Animation Clock)

This is a **conformance backfill**: the implementation, both suites, and the captured `readiness/` evidence
already exist. "Validating" 099 means confirming the suites are green, the surface delta is zero, and the
readiness evidence maps to the success criteria — not building anything.

## Prerequisites

- .NET `net10.0` SDK; the solution `FS.GG.Rendering.slnx` builds clean (`dotnet build -c Release` → 0/0).
- No GL context required — 099's proofs run headless in `DeterministicRenderOnly` mode.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

Expected: 0 warnings, 0 errors.

## 2. Run the pure clock-core suite (US3 — determinism, edges, identity-at-rest)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature099"
```

Confirms:
- **Determinism** — a fixed 12-frame injected-delta sequence replayed twice is byte-identical (both settle at
  150 ms); 1000 FsCheck cases over random delta sequences (SC-004).
- **Edges** — non-positive delta is a no-op (never rewinds); a very-large delta clamps to `Duration` (settles
  at End, no overshoot); a mid-flight retarget re-aims from the current sampled value (no snap to start); a
  settled return-to-`Normal` clock is dropped; multiple clocks advance independently.
- **Identity-at-rest** — a no-active-clock frame is byte-identical to the full `Control.renderTree` static
  rebuild, with at-rest recompute and remeasure counts both 0 (SC-003).

## 3. Run the live-seam suite (US1/US2/US4/US5 — through the real host seam)

```bash
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature099"
```

Confirms, driven through `ControlRuntime.applyRuntimeVisualState` + `RetainedRender.advance` (Tick) +
`RetainedRender.step` with injected 16 ms deltas:
- **US1 animate-not-snap** — the first sampled frame is **not** the snapped target; ≥1 structurally-distinct
  intermediate frame precedes a frame byte-equal to the static snapped target; the sequence converges exactly.
  A no-seam build snaps on frame 0 and fails the intermediate-frame assertion (SC-001).
- **US2 survival** — hover → tick 3 frames (mid-flight) → insert a banner above (sibling shift) → continue to
  completion: identity stable, elapsed **continues** (32 ms → 48 ms, not reset), shifted trajectory
  byte-identical to the unshifted trajectory, **no hand-seeded clock** (SC-002).
- **US4 GC** — hover (clock active) → re-render with the control removed: the next frame's `StateByIdentity`
  has no clock for the removed identity (SC-005).
- **US5 scoped repaint** — a steady-state animating frame reports `WorkReduction` steady-state recompute = 0 and
  remeasure = 0 while the frame still changes (the clock was sampled) (SC-006).

## 4. Confirm zero public-surface delta (FR-012)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx --check
```

Expected: `tests/surface-baselines/FS.GG.UI.Controls.txt` (and `FS.GG.UI.Controls.Elmish.txt`) **byte-unchanged**.
The entire 099 seam is `internal`, so the drift gate stays green.

## 5. Map readiness evidence → success criteria

| Readiness file | Success criterion |
|---|---|
| `readiness/us1-animates-vs-snaps.md` | SC-001 (animate, not snap) |
| `readiness/us2-survival.md` | SC-002 (survives a shift, completes) |
| `readiness/us3-determinism.md` | SC-004 (determinism + edges) |
| `readiness/us3-identity-at-rest.md` | SC-003 (byte-identical at rest, zero recompute) |
| `readiness/us4-gc.md` | SC-005 (removed clock GC'd) |
| `readiness/scoped-repaint.md` | SC-006 (one animation ≠ whole-tree repaint) |

All evidence is `DeterministicRenderOnly`, judged by **structural scene equality** + clock-trajectory
byte-equality. It explicitly does **not** claim pixel-level or desktop-visibility proof.

## Done When

- Both `Feature099` suites are green.
- The surface-drift check passes byte-unchanged.
- Each `readiness/` file regenerates and maps to its SC above.
- `/speckit-analyze` reports cross-artifact consistency (spec ↔ plan ↔ tasks).
