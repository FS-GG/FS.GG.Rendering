---
phase: implement
date: 2026-06-21
severity: minor
---

## Process friction

The single highest-leverage instruction in this feature was the **standing "early live smoke run
before any fix" assumption** (plan.md / research §D8). It converted five unverified defect
hypotheses (H1–H5) into a confirmed "healthy app" result instead of a fix built on green-but-wrong
unit tests — the Feature 175 failure mode. Recommendation: keep that assumption standing for every
control-exercise feature; it paid for itself immediately by surfacing that there were *no* defects to
fix, saving the entire US3 fix budget.

Minor friction: `Viewer.runtimeCapability().PersistentWindow` (what the runner reads as
`renderable`) reports `false` headless, yet `Viewer.captureScreenshotEvidence` with
`OffscreenReadback` still produces real PNGs headless. The two capabilities are distinct (persistent
live window vs offscreen readback) and conflating them under one "renderable" flag was initially
confusing. Worth a short doc note in the SkiaViewer skill clarifying that offscreen readback does not
require a persistent window.

A naming hazard cost a compile cycle: a sample DU case named `Error` (interaction-state kind) and the
`open FS.GG.UI.SkiaViewer`'s `ViewerDiagnosticLevel.Error` both shadow `Result.Error`, so
`Result.Error`/pattern positions silently rebound. Renaming the case to `ErrorState` fixed it.

## Generalizable code

Skill family/topic: FS.GG.UI testing / visual readiness.

Two non-defect framework improvement opportunities surfaced while composing the runner (recorded in
the US4 report, Part 1):

- **F1** — fold continuous-input drag continuity (the existing `ResponsivenessDragContinuity` series)
  into the per-control verdict record so continuous-input verdicts are self-contained rather than a
  pointer to the responsiveness CLI artifacts.
- **F2** — promote a degrade-aware `VisualCaptureMatrix.capture` (target → `VisualCaptureRecord` with
  `CaptureStatus`) into `FS.GG.UI.Testing`, since both the visual-readiness CLI and the control pass
  hand-roll the identical `renderTree → SceneNode.Group → captureScreenshotEvidence` degrade-aware
  capture. Tier 1 when undertaken.

## Skill gaps

none

## Research links

none
