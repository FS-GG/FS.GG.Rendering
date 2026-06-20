# Phase 0 Research: Automated Control Pass for the Second AntShowcase

**Feature**: `176-test-antshowcase-controls` | **Date**: 2026-06-20

This document resolves the planning unknowns from `plan.md` into Decision / Rationale /
Alternatives, and records the **provisional, unverified** defect hypotheses that the early live
smoke run (scheduled by `/speckit-tasks`) must confirm or replace before any US3 fix lands.

---

## D1 — The control catalog is the completeness oracle

**Decision**: The authoritative "every control" set is the union of `CoverageMap.catalogIds ()`
(96 catalog controls across 13 catalog pages) and the controls reachable from the 6 enterprise
template pages (`Templates.fs`). The pass iterates this set; completeness (SC-001) is asserted by
diffing the set of emitted verdict records against `CoverageMap` — zero controls may be missing,
zero duplicated. Interactive-vs-display-only classification is sourced from the per-control
interaction contract (see D2): a control with at least one documented behavior is interactive;
one with none is display-only and is classified as such *with a reason* (FR-006).

**Rationale**: `CoverageMap` already exists as the catalog's single source of truth and is already
guarded by `CoverageTests`/`Feature172CoverageRegressionTests`; reusing it means "every control"
cannot silently drift from the catalog. Template-reachable controls are composition of catalog
controls, so they need exercising in their template context but classify against the same catalog
identity.

**Alternatives considered**: (a) Hand-maintained control list in the runner — rejected: a second
source of truth that drifts from the catalog (the exact friction Feature 175 flagged). (b) Reflect
over the rendered scene tree to discover controls — rejected: non-deterministic ordering and no
documented-behavior linkage.

---

## D2 — Documented behaviors come from the interaction contracts

**Decision**: Each interactive control's *full* behavior set is sourced from
`SecondAntShowcase.Core.InteractionContracts.fs` (extended where a control's documented behaviors
are incomplete). The pass drives **every** documented behavior of a control (e.g. a slider: drag,
keyboard-step, click-track; a switch: toggle on, toggle off) and asserts each resulting functional
state change against the showcase `Model`, not a single representative action (FR-002, SC-002).
Behavior completeness is asserted by `ControlPassCoverageTests` comparing exercised behaviors to
the contract's declared behaviors per control.

**Rationale**: The interaction contracts already enumerate per-control expected behavior and are
the natural place to declare "what full functionality means" for each control. Making the contract
the behavior oracle means SC-002 ("every documented behavior") is mechanically checkable rather
than a judgement call.

**Alternatives considered**: Driving one canonical action per control — rejected by FR-002. A
free-form per-control script with no declared expectation — rejected: not assertable as "complete".

---

## D3 — Runner composition reuses existing input + inspection + visual infrastructure

**Decision**: The runner is assembled, not invented:

- **Input** — `Rendering.Harness.Input` drives scripted `InputStep` sequences (Click/Key/Wait)
  through the showcase. The **Pure backend** is the default deterministic path (MVU replay, scene
  change detection, byte-reproducible); **X11XTest** is the live path for accepted live evidence on
  a visible desktop; **Uinput** honest-skips when `/dev/uinput` is absent. Backend selection is a
  CLI flag with Pure as the unattended default.
- **Functional/visual inspection** — `FS.GG.UI.Controls.ControlInspection.inspect` produces a
  `VisualInspectionArtifact` per control state (node tree, regions, text fit, paint coverage, clip
  facts, findings); `ControlInspection.inspectRetained` produces a `RetainedInspectionArtifact` for
  state transitions (retained/repainted/shifted nodes + damage region). Validation via
  `FS.GG.UI.Testing.VisualInspectionValidation.validate` /
  `RetainedInspectionValidation.validate` with `defaultRules`.
- **Visual matrix** — `FS.GG.UI.Testing.VisualCaptureMatrix.expand` builds the appearance × size
  target set; `VisualCompleteness.validate` + `VisualReadiness.evaluate` produce the
  `VisualReadinessReport` with per-target capture status and reviewer classification.
- **Live-responsiveness / render-lag** — the existing `Responsiveness.fs` and `RenderLagProbe.fs`
  CLI paths (Feature 173/174) supply continuous-feedback and follow-up-frame evidence for
  continuous-input controls.

The runner orchestrates these per control and folds the results into verdict records.

**Rationale**: Feature 175 already proved the "click here, then read back the result" loop by
composing `runInteractiveViewerScript` + offscreen readback; the spec's assumption is explicit that
existing infrastructure is reused, not rebuilt. Each piece already emits deterministic,
environment-aware evidence.

**Alternatives considered**: A new bespoke capture harness — rejected by the spec's reuse
assumption and by the maintenance cost of a parallel evidence path. Screenshot-diff-only (no
structural inspection) — rejected: misses node/region/clip facts and damage locality that the
inspection artifacts give for free.

---

## D4 — Interaction-state capture and transient/overlay surfaces

**Decision**: For each interactive control the pass drives it into every *supported* interaction
state — hover, focus, active, selected/checked, disabled, error/validation as applicable — and
verifies each captured state differs from the resting state (FR-004, US2 AC2), using the
`RetainedInspection` diff to prove the visual delta is real and damage-local. Overlay/transient
controls (tooltip, popover, drawer, dialog, tour, toast, popconfirm) are driven **via their
trigger**, and the pass asserts the transient surface *appears* and *dismisses* — capturing the
open state, the dismiss action, and focus return where applicable (FR-015, Edge Cases). The set of
supported states per control comes from the interaction contract (D2).

**Rationale**: "Differs from resting state" is mechanically checkable through the retained diff
(a state with no node delta vs rest is a dead-affordance defect — exactly the Feature 175 class).
Overlays need trigger-driven verification because the trigger control changing is not evidence the
overlay appeared.

**Alternatives considered**: Asserting only the resting appearance per control — rejected by FR-004.
Treating overlays as their trigger control only — rejected by FR-015 and the overlay edge case.

---

## D5 — Damage-locality is both required and recorded

**Decision**: Every state-change transition the pass drives is captured as a
`RetainedInspectionArtifact` and validated with `RetainedInspectionValidation.defaultRules`; the
resulting `DamageRegionInspection` (dirty rectangles, clipped union area, dirty percentage,
affected node/region ids) is stored as evidence per control (FR-005). A transition whose damage is
`Broad`/`FullSurface` without an `IntentionalDamageException` is a finding, not a silent pass.

**Rationale**: Feature 175's root cause was a missing repaint signal that *also* risked over-broad
repaint; the retained-damage inspection (Feature 170) exists precisely to make repaint scope
evidence. Recording it satisfies FR-005 and guards against a "fix" that greens behavior by
repainting the whole tree.

**Alternatives considered**: Asserting only that *a* repaint happened — rejected: that is the bug
class that hides full-tree repaint regressions.

---

## D6 — Determinism: pin time and seed evidence

**Decision**: All evidence-producing paths take an explicit seed (mirroring the existing
`evidence --seed`/`visual-readiness --seed` CLIs). Non-deterministic surfaces — calendar (today),
time-picker (now), carousel auto-advance, spinner/skeleton animation, tour step animation — are
**pinned** to fixed values via the showcase's existing deterministic clock/seed inputs, or, where
a surface is irreducibly time-dependent, the record is explicitly flagged `time-dependent` rather
than asserted byte-stable (FR-007, SC-005). The runner's verdict records and inspection artifacts
exclude wall-clock timestamps from the byte-stable comparison surface (timestamps live in a
separate, non-asserted metadata field, as the existing artifacts already do via `GeneratedAtUtc`).

**Rationale**: The repo already pins determinism through seeds and `GeneratedAtUtc` separation; the
pass must not introduce a new non-deterministic field. `DeterminismTests` already guards the
existing evidence path and the new path joins it.

**Alternatives considered**: Allowing timestamps in the compared surface and diffing "modulo
timestamps" — rejected: fragile and already solved by the `GeneratedAtUtc` convention.

---

## D7 — Environment-limited degradation

**Decision**: When no live window can be presented, live-only checks (X11XTest exercise, screenshot
capture) degrade to an explicit `environment-limited` record outcome with a well-defined non-zero
signal, never a silent pass/fail (FR-008). Detection reuses `SkiaViewer` window diagnostics
(`RenderableSurfaceAvailable`, `ViewerFailureClass.EnvironmentSession`) and the `ValidationLanes`
`EnvironmentLimited`/`EnvironmentLimitedReadiness` status; structural inspection (Pure backend +
`ControlInspection`) still runs and is recorded, so a headless run produces full functional +
structural-visual evidence with live-pixel checks marked environment-limited.

**Rationale**: This matches the constitution's "GL smoke failures MUST distinguish implementation
defects from a missing window-system" and the existing degradation tests (`DegradeTests`,
`Feature*EnvironmentLimit`). It lets CI produce meaningful evidence without a display while keeping
the live-pixel gap honest.

**Alternatives considered**: Failing the whole pass when no display — rejected: blocks all CI
signal. Silently passing structural-only — rejected by FR-008.

---

## D8 — Provisional defect hypotheses (UNVERIFIED — gated by the early live smoke run)

> These are candidate defects the pass is *expected* to surface, written so `/speckit-tasks` can
> schedule targeted verification. **None is confirmed.** Per the standing assumption, deterministic
> tests being green is not evidence the live app is correct (Feature 175). The early live smoke run
> drives the real app through the control-pass runner and confirms/replaces each before any fix.

- **H1 (framework, candidate)** — interaction states that passed scripted tests in 171–175 may
  still lack a *live* visual delta for some control kinds not covered by the 175 hover/focus fix
  (175 fixed the general repaint signal; per-kind `applyRuntimeVisualState` coverage may be
  incomplete for newer/display-adjacent kinds). Verify per kind via the retained diff.
- **H2 (framework, candidate)** — overlay/transient surfaces may dismiss without returning focus, or
  may not mark damage locally on open/close. Verify via D4 overlay path + D5 damage check.
- **H3 (sample/framework, candidate)** — continuous-input controls (slider/scroll drag) may show
  start/end states but not continuous feedback under live drag. Verify via the responsiveness
  runner's continuous-feedback evidence.
- **H4 (sample, candidate)** — a small number of controls may have an unbound `OnChanged` or
  template-context wiring gap (the Feature 175 minority class). Verify via functional state-change
  assertion.
- **H5 (framework, candidate)** — a missing testing-helper seam may force the runner to reach into
  internals to capture a state; if so, the helper addition is Tier 1 (a framework improvement the
  report records).

The report (US4) records *every* confirmed framework/library finding plus authoring-friction and
improvement opportunities discovered while building the runner, even ones with no code fix.

---

## D9 — Report conventions

**Decision**: The US4 report is `docs/reports/2026-06-20-feature-176-second-antshowcase-control-pass-report.md`,
following the established `YYYY-MM-DD-feature-NNN-<slug>.md` convention and mirroring the Feature 175
report structure: lead metadata (date/UTC, author, source merge SHA, status) → executive summary →
Part-structured findings (Part 1 Framework/Library, Part 2 Tooling, Part 3 Process, Part 4 Skills,
as applicable) where each finding carries Evidence / Root cause / Impact / Mitigation /
Recommendation + Effort/Risk → a prioritisation table (ID, Item, Severity, Effort, Leverage) →
phased roadmap → evidence appendix anchored to the readiness artifacts and code. Framework
improvements are separated from sample-local fixes and ordered by priority/impact (FR-014, SC-008).

**Rationale**: Reuse the exact convention so the report is discoverable alongside its peers and the
reader gets the familiar severity/leverage triage.

**Alternatives considered**: A timestamped-evidence-snapshot filename
(`YYYYMMDD-hhmmss+ZZZZ-...`) — that pattern exists for point-in-time evidence dumps; the planning
report is a curated framework retrospective, so it uses the feature-scoped pattern.

---

## Open items carried into Phase 1

- Exact `.fsi` seams for any Tier 1 fix are deferred to Phase 0-of-fix (after the early live smoke
  run confirms the defect) — the contracts describe the *obligations*, the surgical seam is named
  when the defect is real.
- The precise readiness artifact layout (one JSON per control vs an aggregated JSON + rendered
  markdown) is settled in `data-model.md` / the runner contract.
