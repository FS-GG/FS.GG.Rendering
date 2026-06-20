# Framework / Library Report — Control Pass of the Second AntShowcase

- Report date (UTC): 2026-06-21
- Author: Feature 176 control pass (`SecondAntShowcase control-pass`)
- Source: feature 176 + merge SHA (filled at merge; pre-merge base `98ac3d0`)
- Status: Delivered — pass clean, zero control defects, two non-defect framework improvement opportunities

## Executive summary

An automated, no-human-input control pass drove **all 96 cataloged controls** of the Second
AntShowcase through their documented behaviors and captured visual + structural + damage-locality
evidence per control. The pass surfaced **zero functional, visual, interaction-state, or
damage-locality defects** — the showcase, hardened across Features 171–175, is confirmed healthy on
a live-rendering host. The durable framework output is therefore not a defect backlog but two
**improvement opportunities** in the *evidence infrastructure* the runner composed.

## Background

The control pass (`samples/SecondAntShowcase/SecondAntShowcase.App/ControlPassRunner.fs`, planned by
the pure `SecondAntShowcase.Core/ControlPass.fs`) is assembled from existing surfaces, not a new
harness (research §D3):

- **Functional (US1)** — every documented behavior from `InteractionContracts.fs` is driven through
  the pure showcase MVU (`Model.update`) and asserted to change the model. Deterministic, headless,
  byte-stable.
- **Structural interaction-state + damage (US2)** — `ControlInspection.inspectRetained` over the
  Shell render of each driven transition yields a real retained delta vs rest and a
  `DamageRegionInspection` (damage-locality), with no GL window required.
- **Live-pixel visual matrix (US2)** — `Viewer.captureScreenshotEvidence` (offscreen readback) over
  `VisualCaptureMatrix` cells: antLight/antDark × preferred(1600×1000)/minimum(1280×800) at rest.
  Fail-closes to `environment-limited` when no renderable surface is present (FR-008).

Run evidence (this host, `renderable=true`):

```
control-pass: 96 records, 45 interactive, 51 display-only, 0 findings, renderable=true  (exit 0)
```

52 real screenshots (49–67 KB PNGs), determinism confirmed byte-identical across same-seed runs on
the deterministic surface, damage `Localized` on every driven transition (e.g. slider 1.77% dirty).

## Part 1 — Framework / library

These are **non-defect improvement opportunities** in the shared evidence infrastructure surfaced
*while building* the runner. Neither blocks the feature; both raise the fidelity/ergonomics of future
control passes. Classification per finding-log: both `FrameworkShared`.

### F1. Continuous-input feedback is proven in a separate CLI, not folded into the per-control record. Severity: Low

- Evidence: `samples/SecondAntShowcase/SecondAntShowcase.App/ControlPassRunner.fs` (continuous-input
  diagnostics) and `readiness/verdict-records/slider.json` — the slider record carries a localized
  state delta and a diagnostic pointing to the Feature 173/174 `responsiveness` / `render-lag-probe`
  evidence rather than embedding a continuous-feedback sample series.
- Root cause: live drag continuous-feedback (offset-tracks-input, no catch-up lag) is owned by the
  existing live-responsiveness runner (`Responsiveness.fs`/`RenderLagProbe.fs`); the control pass
  composes a pointer to it instead of duplicating the drag-sampling loop.
- Impact: a reader of a single slider/scroll verdict record must follow a pointer to the
  responsiveness artifacts to see the continuous-feedback proof; the per-control record is complete
  for functional + damage but defers continuous-feedback.
- Shipped mitigation: each continuous-input record discloses, in `diagnostics`, exactly where the
  continuous-feedback evidence lives (never a silent omission). Asserted by `ControlPassRunnerTests`
  ("continuous-input slider … discloses its continuous-feedback evidence path").
- Recommendation: add a thin `RenderLagProbe`/`Responsiveness` adapter that returns a
  `ResponsivenessDragContinuity` sample series for a single control, and merge it into the
  continuous-input verdict records so the record is self-contained.
- Effort: M. Risk: low.

### F2. No published per-control visual-capture-matrix helper; the runner re-implements the page-cell capture. Severity: Low

- Evidence: `ControlPassRunner.capturePageCell` mirrors `SecondAntShowcase.App/VisualReadiness.fs`
  `captureScreenshot` — both hand-roll `renderTree → SceneNode.Group → captureScreenshotEvidence`
  with degrade-and-disclose. `FS.GG.UI.Testing.VisualCaptureMatrix.expand` produces the *targets*
  but not the *capture* of a cell.
- Root cause: the capture step (offscreen readback per target + completeness classification) is not
  yet a published `FS.GG.UI.Testing` helper, so each consumer re-derives it.
- Impact: capture/degradation logic is duplicated across the visual-readiness CLI and the control
  pass; a fix to degradation classification must be made in two places.
- Shipped mitigation: none required for this feature (the duplication is small and both sites
  degrade-and-disclose identically). Recorded here as an improvement opportunity (H5-adjacent).
- Recommendation: promote a `VisualCaptureMatrix.capture` (target → `VisualCaptureRecord` with
  `CaptureStatus`) into `FS.GG.UI.Testing` so consumers share one degrade-aware capture path. This
  is a **Tier 1** addition (`.fsi` + surface baseline + failing-first test) when undertaken.
- Effort: M. Risk: low.

## Part 2 — Tooling & developer loop

No tooling defects surfaced. The comprehensive baseline runner (`scripts/baseline-tests.fsx`) and the
package-feed-consuming sample build/test loop worked as designed; the pre-run baseline was fully green
(`readiness/baseline.md`).

## Part 3 — Spec Kit / process

No process defects surfaced. The standing "early live smoke run before any fix" assumption
(`plan.md` / research §D8) was the highest-leverage instruction: it converted five *unverified*
defect hypotheses into a confirmed "healthy app" result instead of a fix built on green-but-wrong
unit tests (the Feature 175 failure mode). Recommendation: keep that assumption standing for every
control-exercise feature.

## Part 4 — Agent tools & skills

No skill gaps surfaced; the runner used only documented `fs-gg-*` surfaces.

## Part 5 — Sample-local fixes (separated)

**None.** The pass surfaced zero sample-local defects: all 45 interactive controls produce a model
change for every documented behavior (0 dead affordances), and all 51 display-only controls classify
with a reason and `NotApplicable` functional verdict. This part is intentionally empty and is kept
visually separate from the framework Part 1 above (R-3).

## Prioritisation

| ID | Item | Severity | Effort | Leverage |
|----|------|----------|--------|----------|
| F1 | Fold continuous-input drag continuity into the per-control record | Low | M | Self-contained continuous-input verdicts; removes a cross-artifact hop |
| F2 | Publish a degrade-aware `VisualCaptureMatrix.capture` helper (Tier 1) | Low | M | De-duplicates capture/degradation logic across the visual-readiness CLI and the control pass |

Ordering: both are Low severity; F1 ranks above F2 by leverage (it closes a record-completeness gap;
F2 is a refactor of working, duplicated code).

## Suggested phased roadmap

- Phase A — (optional) F1: add the single-control responsiveness/drag-continuity adapter and merge
  its series into continuous-input verdict records.
- Phase B — (optional) F2: promote the degrade-aware capture into `FS.GG.UI.Testing` as a Tier 1
  helper (`.fsi` + baseline + failing-first test), then re-point both consumers at it.
- Phase C — none required; the pass is clean and re-runs deterministically with no regression.

## Appendix — evidence anchors

- Pure plan + schema: `samples/SecondAntShowcase/SecondAntShowcase.Core/ControlPass.fsi` / `.fs`.
- Runner: `samples/SecondAntShowcase/SecondAntShowcase.App/ControlPassRunner.fs`.
- CLI dispatch: `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs` (`control-pass`).
- Tests: `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassCoverageTests.fs`,
  `ControlPassRunnerTests.fs`, `DocumentationReviewTests.fs`.
- Verdict records (96): `specs/176-test-antshowcase-controls/readiness/verdict-records/` (`_index.md`).
- Visual evidence (52 cells): `specs/176-test-antshowcase-controls/readiness/visual-evidence/`.
- Finding log + smoke-run verification: `specs/176-test-antshowcase-controls/readiness/finding-log.md`.
- No-regression baseline: `specs/176-test-antshowcase-controls/readiness/baseline.md` (all green).
- Feature spec/plan/contracts: `specs/176-test-antshowcase-controls/`.
- Pre-merge base SHA: `98ac3d0` (merge SHA filled at merge).
