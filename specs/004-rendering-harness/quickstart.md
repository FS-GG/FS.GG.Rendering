# Quickstart / Validation: Rendering Test Harness (Stage R5)

Validated by **running the harness CLI** and inspecting its evidence artifacts, plus the pure
unit tests. No tier is required for routine product work (capability, not gate).

## Prerequisites

- Built solution (`dotnet build FS.GG.Rendering.slnx -c Release`).
- For T2/T3: `DISPLAY=:1`, X11 tools (`xdotool`, `maim`, `xrandr`); the harness unsets
  `WAYLAND_DISPLAY` for the viewer. For T-uinput: `/dev/uinput` (absent here → clean skip).

## Validation scenarios

### V1 — Deterministic core, no desktop (T0/T1) — SC-001/004

```bash
unset DISPLAY
dotnet run --project tests/Rendering.Harness -- offscreen --json
```
**Pass** if it completes in a few seconds and emits `run.json` with `proofLevel:"deterministic"`/
`"offscreen-pixels"`, a non-empty `notAuthoritativeFor`, a non-blank PNG, and a `summary.md`.

### V2 — Probe records backend (SC-007)

```bash
DISPLAY=:1 dotnet run --project tests/Rendering.Harness -- probe --json
```
**Pass** if `run.json.env` records display/GL/refresh/extensions and `effectiveBackend:"x11"`
(a Wayland-effective run would be classified, not silently `x11`).

### V3 — Live X11 smoke (T2) — SC-002

```bash
DISPLAY=:1 dotnet run --project tests/Rendering.Harness -- live-x11 --frames 120
```
**Pass** if the viewer launches (Wayland unset), the window is found, a **non-blank window PNG** is
captured, mouse+keyboard are injected via XTEST, and a **visible state change** is recorded.

### V4 — Faithful perf, no overclaim (T3) — SC-003

```bash
DISPLAY=:1 dotnet run --project tests/Rendering.Harness -- perf --mode paced-native --frames 600
```
**Pass** if `metrics.csv` + percentile metrics persist **with** present facts; remove the vblank
facts and confirm the run is **not** labeled `vsync-faithful`.

### V5 — Kernel input degrades cleanly (T-uinput) — SC-005

```bash
dotnet run --project tests/Rendering.Harness -- input --backend uinput --script scripts/click.json
```
**Pass** (where `/dev/uinput` is absent) if it exits 0 with `status:"skipped"`,
`skipReason` naming the opt-in/host-pass-through requirement — no crash.

### V6 — Pure logic unit tests

```bash
dotnet test tests/Rendering.Harness.Tests -c Release
```
**Pass** if the `RunPlan` proof/degradation logic and evidence-schema tests are green (these join
the default local tier).

## Done when

- V1–V6 pass; every artifact declares what it proves and what it does not (SC-004).
- No tier is required for a routine rendering change (FR-012); none depends on governance (FR-013).
- See [`contracts/`](./contracts/) for the CLI, `run.json`, and tier-matrix contracts.
