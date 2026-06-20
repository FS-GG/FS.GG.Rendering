# Contract: Visual Evidence Matrix

**Feature**: `176-test-antshowcase-controls`

Defines visual-fidelity coverage: every control captured across both appearances and both
representative sizes, interactive controls additionally captured per interaction state, each cell
carrying a fidelity verdict, and each state-change repaint proven damage-local.

## Matrix

For each cataloged control:

```text
appearances = { antLight, antDark }            # FR-003
sizes       = { preferred (1600x1000),
                minimum   (1280x800) }          # FR-003
states (display-only)  = { Rest }
states (interactive)   = { Rest } ∪ supported { Hover, Focus, Active,
                                                Selected/Checked, Disabled, Error }   # FR-004
```

Targets are expanded with `FS.GG.UI.Testing.VisualCaptureMatrix.expand`; completeness validated
with `VisualCompleteness.validate`; the run rolls up via `VisualReadiness.evaluate` into a
`VisualReadinessReport`. Each cell is a **Visual Evidence Item** (see data-model.md).

## Rules

- **M-1 (rest coverage)**: every control has Complete evidence for both appearances × both sizes at
  `Rest` (FR-003, SC-003).
- **M-2 (state coverage)**: every interactive control has evidence for each *supported* interaction
  state (sourced from the interaction contract), and each captured state is verified to **differ
  from `Rest`** via the retained diff (FR-004, US2 AC2, SC-003).
- **M-3 (fidelity verdict)**: each cell carries `Approved | NeedsReview | Blocked`. Any
  non-`Approved` cell carries ≥1 human-readable reason — missing affordance, wrong palette, clipped
  content, misalignment, wrong contrast (US2 AC4).
- **M-4 (damage-local)**: every state transition is captured as a `RetainedInspectionArtifact`; its
  `DamageRegionInspection` MUST be `Localized` (only the affected region changed) or carry an
  `IntentionalDamageException` with reviewer reason. `Broad`/`FullSurface` without exception ⇒
  finding (FR-005, US2 AC3).
- **M-5 (overlay/transient)**: transient surfaces (tooltip, popover, drawer, dialog, tour, toast,
  popconfirm) are captured in their **open** state (driven via trigger) and verified to dismiss,
  with focus return where applicable (FR-015).
- **M-6 (degradation)**: `CaptureStatus ∈ {Degraded, Blocked}` ⇒ cell verdict `EnvironmentLimited`,
  never silent `Approved` (FR-008). On a headless host, structural inspection still yields
  node/region/clip facts; only pixel capture is environment-limited.
- **M-7 (determinism)**: captures are byte-stable for the same seed/build where the framework
  guarantees determinism; time/animation surfaces are pinned or the cell is flagged
  `time-dependent` (FR-007).

## Reasons taxonomy (for non-approved fidelity)

`missing-affordance` · `wrong-palette` · `clipped-content` · `misalignment` · `low-contrast` ·
`state-equals-rest` · `overlay-no-appear` · `overlay-no-dismiss` · `focus-not-returned` ·
`broad-damage` · `environment-limited`.

## Test obligations

- Matrix completeness (M-1, M-2) asserted via `VisualReadinessReport` target/capture status.
- State-differs-from-rest (M-2) and damage-locality (M-4) asserted via the retained diff in
  `ControlPassRunnerTests` for representative controls of each interactive family.
- Overlay appear/dismiss (M-5) asserted for each transient family.
