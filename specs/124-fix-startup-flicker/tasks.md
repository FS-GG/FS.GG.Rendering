---

description: "Task list for feature 124 — Fix Startup Flicker (Interactive Gallery Window)"
---

# Tasks: Fix Startup Flicker (Interactive Gallery Window)

**Input**: Design documents from `/specs/124-fix-startup-flicker/`

> **Post-implementation note.** The original task list pursued a *startup-priming* theory
> (`planStartupPriming`, hidden-create, swapchain priming) that was disproven. It has been
> replaced with the tasks actually performed. The real root cause was a **double buffer-swap**
> per frame; see [research.md](./research.md) (probe ladder) and
> [plan.md](./plan.md). Tier: **2 (internal change)** — no public surface touched.

## Phase 1: Diagnose (completed)

- [x] T001 Reproduce the flicker in the gallery interactive window on the live X11 desktop; characterize it (intermittent black flashes, longer on button click).
- [x] T002 Rule out the environment: confirm real GPU (AMD radeonsi/Mesa), Xwayland-under-KWin; confirm external capture can't see the DRI3 GL surface (so verification must be by-eye).
- [x] T003 Build a minimal renderer probe (`Silk.NET` + GL/Skia, no framework) and climb complexity rung by rung (bare GL → Skia clear → +Snapshot → borderless → framelike → framelike-noautoswap), observing each on the live desktop.
- [x] T004 Isolate the cause: only the `framelike` rung (Silk auto-swap **and** explicit `SwapBuffers`) flickers; `framelike-noautoswap` (single swap) is steady. Red-clear diagnostic confirms the flashes are an undrawn buffer, not our clear. → **double buffer-swap**.

## Phase 2: Fix — flicker (completed)

- [x] T005 In `src/SkiaViewer/Host/OpenGl.fs` `createWindow`, set `options.ShouldSwapAutomatically <- false` so the explicit present in the `GlHost` path is the only swap per frame (FR-001).
- [x] T006 Audit all `SwapBuffers` / `ShouldSwapAutomatically` sites: confirm manual swaps exist only in the `GlHost` present routines (all covered) and the headless/bounded paths don't manually swap (no other double-swap).

## Phase 3: Fix — input lag/rubberbanding (completed)

- [x] T007 In `src/SkiaViewer/SkiaViewer.fs` `runPresentedPersistentWindow`, change the `LegacyPointer` arm to update the model only (no per-event `RenderFrame`); the paced `RenderTick` presents — matching the `LegacyKey` path. Removes the ~3×-frame-cap repaint flood on mouse movement.

## Phase 4: Verify & protect (completed)

- [x] T008 Regression: `dotnet test tests/SkiaViewer.Tests` — present/pacing suites (120/121/122) stay green (94/94).
- [x] T009 SC-004 / FR-006: gallery `Determinism` + `Degrade` suites stay green (6/6); headless path byte-identical and degrade-and-disclose unchanged.
- [x] T010 Live perceptual verification on the X11 desktop: steady from first frame, idle-steady, no black flash on click, smooth mouse — confirmed by the author.
- [x] T011 Clean up: remove the standalone probe and all experimental scaffolding; final tracked change is the two framework edits only.

## Out of scope (separate follow-ups)

- [ ] Sub-control interactivity in the gallery (slider/toggle respond to the mouse): the gallery stubs `MapPointer = fun _ -> None` and `HitTest = fun _ _ -> false` (showcase-level gap; the framework already supports pointer routing and the model has the messages). Not part of this flicker fix.

## Notes

- Both fixes are **framework-level** (`FS.GG.UI.SkiaViewer`) — they fix every consumer of the
  interactive window. The gallery is unchanged; it is the verification vehicle (re-pack →
  consume → observe).
- The flicker is compositor-visible and not observable in-process, so the deterministic gate
  is the regression suites + the reproducible probe; the perceptual outcome is observed live
  (the spec's stated verification approach).
