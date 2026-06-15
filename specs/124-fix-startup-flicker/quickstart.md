# Quickstart — Verify the Startup-Flicker Fix

A run/verify guide. Root cause and fix details are in [plan.md](./plan.md) /
[research.md](./research.md); the behavioral contract is in
[contracts/present-invariant.md](./contracts/present-invariant.md).

## Prerequisites

- Linux desktop with a live X11 display + GL (the dev desktop).
- .NET `net10.0` SDK; local NuGet feed `~/.local/share/nuget-local/`.

## 1. Regression (no display required)

```bash
dotnet test tests/SkiaViewer.Tests                              # present/pacing (120/121/122): 94/94
dotnet test samples/ControlsGallery/ControlsGallery.Tests --filter "Determinism|Degrade"   # SC-004 / FR-006: 6/6
```
**Expected**: all pass — the fix preserves idle-skip/pacing and byte-identical headless output.

## 2. Re-pack so the gallery sees the fix

```bash
dotnet pack src/SkiaViewer/SkiaViewer.fsproj -c Release -o ~/.local/share/nuget-local/
# clear the cached extract so the same-version re-pack is picked up:
rm -rf ~/.nuget/packages/fs.gg.ui.skiaviewer/0.1.0-preview.1
rm -rf samples/ControlsGallery/ControlsGallery.App/{bin,obj}
dotnet build samples/ControlsGallery/ControlsGallery.App -c Release
```

## 3. Perceptual verification on the live desktop (real evidence)

```bash
dotnet run --project samples/ControlsGallery/ControlsGallery.App -c Release --no-build -- interactive
```

| Check | Expected | Maps to |
|-------|----------|---------|
| Window appears, untouched | Steady, no black flashes | FR-001, SC-001/002 |
| Idle ≥ 30 s | Still steady | FR-002, SC-003 |
| Click / navigate | No black flash on paint | FR-001 |
| Move mouse quickly over window | Smooth, no lag/stutter/rubberbanding | (input regression found during the hunt) |
| Light/dark × indigo/teal | Steady for every option | FR-007 |

## 4. No-GL host unchanged (FR-006)

```bash
dotnet run --project samples/ControlsGallery/ControlsGallery.App -c Release -- evidence --seed 1
```
**Expected**: clean degrade-and-disclose, unchanged.

## Done When

- [x] `ShouldSwapAutomatically <- false` in `GlHost.createWindow` (one present per frame).
- [x] `LegacyPointer` no longer emits a per-event `RenderFrame` (paced render).
- [x] SkiaViewer present/pacing suites pass (94/94); gallery determinism + degrade pass (6/6).
- [x] Live desktop: steady startup, idle-steady, no click flash, smooth mouse — observed & confirmed.
