# Implementation Plan: Fix Startup Flicker (Interactive Gallery Window)

**Branch**: `124-fix-startup-flicker` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/124-fix-startup-flicker/spec.md`

> **Post-implementation note.** This plan was originally written around a *startup-priming*
> hypothesis (an undrawn-swapchain-buffer theory + a `planStartupPriming` capability). That
> hypothesis was disproven during implementation. This document has been rewritten to record
> the **actual root cause and fix**. See [research.md](./research.md) for the investigation
> (a minimal renderer probe climbed from bare GL to the framework's structure and isolated
> the cause).

## Summary

The interactive gallery window flickered with intermittent **black flashes** (longer on
larger paints such as a button click). **Root cause:** the live `GlHost` present path
**swapped the GL buffers twice per frame**. Silk.NET's `WindowOptions.ShouldSwapAutomatically`
defaults to `true`, so `window.DoRender()` swaps automatically *after* the render callback —
but the framework's present routines (`renderFrameDirect` / `representLastGoodFrame` /
`renderFrameReadback`) also call `window.SwapBuffers()` explicitly. The second swap presented
an **undefined back buffer**, which the compositor showed as a black flash.

**Fix (framework-level, `FS.GG.UI.SkiaViewer`):** set `options.ShouldSwapAutomatically <- false`
in `GlHost.createWindow` so exactly one explicit present happens per frame. A secondary
framework fix removes a per-pointer-event repaint flood that caused input lag/rubberbanding
(the `LegacyPointer` arm now updates the model and lets the paced `RenderTick` present, like
the key path already does). The **gallery sample is unchanged** — both fixes are in the
framework, so every consumer of the interactive window benefits.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`TreatWarningsAsErrors`, incl. `FS0078`).

**Primary Dependencies**: `Silk.NET.Windowing` (the `ShouldSwapAutomatically` behavior),
SkiaSharp-over-GL. No new dependency.

**Storage**: N/A.

**Testing**: Expecto. Existing `SkiaViewer.Tests` present/pacing suites (features 120/121/122)
as regression (94/94 pass); gallery headless `Determinism`/`Degrade` suites for SC-004 / FR-006
(6/6 pass). Perceptual outcome verified by observation on the live X11 desktop, backed by the
standalone probe experiment (research R1).

**Target Platform**: Linux X11/GL (Xwayland under KWin on the dev desktop) for the interactive
path; headless evidence path unaffected.

**Project Type**: Framework rendering fix in `src/SkiaViewer` (the `FS.GG.UI.SkiaViewer`
package), verified through the `samples/ControlsGallery` consumer.

**Performance Goals**: Restore correct single-present-per-frame; remove the ~3×-frame-cap
repaint flood on pointer movement. Idle-skip / frame-pacing optimizations (120/121/122) are
preserved unchanged.

**Constraints**: Live-window-only change; headless determinism preserved (SC-004); no-GL
degrade preserved (FR-006); interactive mode stays advisory/GL-gated.

**Scale/Scope**: Two small framework edits — one config line in `OpenGl.fs`, one effect change
in `SkiaViewer.fs`. No public `.fsi` change, no surface-baseline change, no gallery change.

## Constitution Check

*GATE: passed.*

**Change Classification**: **Tier 2 (internal change).** The fix alters internal present
behavior only — it adds/changes **no public API surface**, touches **no `.fsi`** and **no
surface-area baseline**, and preserves observable contracts (determinism, degrade). Tier 2
requires spec + tests, both present.

| Principle | Status |
|-----------|--------|
| **I. Spec → FSI → Semantic tests → Impl** | **PASS (adapted).** No public surface added; the change is internal to the present host. Behavior validated by the controlled probe experiment + regression suites + live observation, in that order. |
| **II. Visibility in `.fsi`** | **PASS.** No public symbols added or changed; no access modifiers added to `.fs`. |
| **III. Idiomatic simplicity** | **PASS.** One option assignment + replacing a per-event effect with `Cmd.none`. Nothing exotic. |
| **IV. Elmish/MVU boundary** | **PASS.** The pointer change keeps `update` pure and moves rendering to the paced tick (the existing edge interpreter); no new I/O in `update`. |
| **V. Test evidence** | **PASS.** Regression suites fail-safe the change (94/94 + 6/6); the perceptual fix is verified by live observation (the spec's stated method) and the reproducible probe ladder. The flicker is a compositor-visible artifact not observable in-process (external capture of the DRI3 GL surface fails), so a headless unit assertion of "no flicker" is not feasible — disclosed here rather than faked. |
| **VI. Observability & safe failure** | **PASS.** No diagnostics removed; no-GL degrade-and-disclose unchanged. |

**Engineering Constraints**: `net10.0` ✓ · SkiaSharp-over-GL, no Vulkan ✓ · no new dependency
✓ · `.fsi`/baseline untouched ✓ · gallery/package identity unchanged ✓.

**Result: PASS — no violations. Complexity Tracking empty.**

## Project Structure

```text
src/SkiaViewer/
└── Host/OpenGl.fs    # FIX 1: options.ShouldSwapAutomatically <- false in createWindow
src/SkiaViewer/
└── SkiaViewer.fs     # FIX 2: LegacyPointer arm no longer emits a per-event RenderFrame

samples/ControlsGallery/   # UNCHANGED — re-packed FS.GG.UI.SkiaViewer is consumed to verify
specs/124-fix-startup-flicker/
├── plan.md · research.md · spec.md · tasks.md · quickstart.md
└── contracts/present-invariant.md   # the "one present per frame" invariant
```

**Structure Decision**: Both fixes belong in the **framework** because both defects live in
the shared `FS.GG.UI.SkiaViewer` present host, not in the gallery. A sample-side workaround
would have been impossible (no public knob) and wrong (every consumer hits the same bug). The
gallery stays a pure consumer and serves as the verification vehicle.

## Complexity Tracking

> No Constitution Check violations. No entries required.
