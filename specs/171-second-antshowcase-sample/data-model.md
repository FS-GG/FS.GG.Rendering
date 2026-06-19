# Data Model: Second Ant Showcase Sample

## Second Ant Showcase Sample

Standalone sample experience under `samples/SecondAntShowcase`.

Fields:

- `SampleId`: stable id, initially `second-ant-showcase`.
- `DisplayName`: reviewer-facing sample name.
- `Purpose`: concise relationship to `samples/AntShowcase`.
- `Pages`: catalog and template pages in navigation order.
- `Appearance`: active Ant appearance.
- `Coverage`: latest control coverage result.
- `ReviewStatus`: latest visual review readiness status.
- `Limitations`: environment or evidence limitations.

Validation rules:

- The sample must be discoverable without replacing or renaming `samples/AntShowcase`.
- The sample must document its relationship to the existing AntShowcase sample.
- The sample must not introduce new product controls, product themes, or product behavior.

## Showcase Page

Navigable page in the sample shell.

Fields:

- `PageId`: stable CLI/navigation id.
- `Title`: displayed page title.
- `Kind`: `catalog` or `template`.
- `Family`: Ant pattern family or enterprise template kind.
- `ControlIds`: catalog control ids assigned to the page when `Kind = catalog`.
- `Sections`: ordered demonstration sections.
- `PrimaryInteractions`: representative interactions available on the page.

Validation rules:

- Catalog pages participate in the coverage bijection.
- Template pages are excluded from the coverage bijection and validated as compositions.
- Every page must be reachable from the shell in no more than two navigation actions.
- Every page must render in Ant light and Ant dark at both accepted sizes.

## Control Demonstration

Representative demonstration of one catalog control.

Fields:

- `ControlId`: id from `Catalog.supportedControls`.
- `PageId`: owning catalog page.
- `DisplayName`: reviewer-facing name.
- `Interactive`: true when the control supports a meaningful state-changing interaction.
- `DisplayOnlyReason`: reason when the control is display-only.
- `SeedContent`: deterministic content, options, rows, series, media, graph data, or status data.
- `InteractionContractId`: linked interaction contract when interactive.
- `AntPatternRefs`: local Ant pattern doc references used for visual review.

Validation rules:

- Each current catalog control appears on exactly one catalog page.
- Each demonstration has representative content and must not render as an empty placeholder.
- Interactive demonstrations require at least one visible state transition.
- Display-only demonstrations must not show misleading interaction affordances.

## Control Coverage Result

Machine-checkable bijection between current catalog controls and catalog pages.

Fields:

- `CatalogCount`: count returned by `Catalog.supportedControls`.
- `AssignedCount`: number of assigned catalog page ids, including duplicates.
- `CatalogPageCount`: number of catalog pages.
- `TemplatePageCount`: number of template pages.
- `MissingControlIds`: catalog ids not assigned to any catalog page.
- `DuplicatedControlIds`: ids assigned to more than one catalog page.
- `UnknownControlIds`: assigned ids no longer present in the catalog.
- `Status`: `passed` or `failed`.

Validation rules:

- Accepted coverage requires zero missing, zero duplicated, and zero unknown controls.
- Template pages do not count as assignments.
- The CLI exits non-zero when coverage fails.

## Interaction Contract

Expected visible behavior for an interactive demonstration.

Fields:

- `ContractId`: stable id.
- `ControlIds`: controls covered by the contract.
- `StartingState`: seeded state before interaction.
- `Action`: pointer, keyboard, command, or scripted action.
- `ExpectedStateChange`: changed value, selection, navigation, overlay, validation, feedback, or progress.
- `VisibleEvidence`: text/status/visual affordance that proves the change.
- `ThemeInvariant`: whether the same behavior is required in both appearances.

Validation rules:

- Every interactive control maps to at least one interaction contract.
- The expected state change must be visible to a reviewer.
- The same contract must behave identically in Ant light and Ant dark except for visual styling.
- Scripted contracts must be deterministic for the same seed.

## Demo State

Pure state owned by the sample Core.

Fields:

- `CurrentPage`: selected page id.
- `Appearance`: `light` or `dark`.
- `ControlValues`: text, numeric, date/time, slider, rating, upload, selection, navigation, paging, overlay, feedback, chart, graph, and custom-surface values.
- `ExpandedState`: open panels, menus, drawers, overlays, and popovers.
- `ValidationState`: form validation phase and field errors.
- `ReviewFindingState`: current visual finding lifecycle summary.
- `Feedback`: reviewer status or saved notes when supported by the sample.

Validation rules:

- Theme switching preserves `CurrentPage`, `ControlValues`, `ExpandedState`, `ValidationState`, and active overlays.
- `update` is pure and deterministic.
- App edge effects never become the source of truth for control state.

## Ant Appearance

Visual appearance selected by the sample.

Fields:

- `AppearanceId`: `antLight` or `antDark`.
- `Alias`: CLI aliases such as `light` or `dark`.
- `ThemeSource`: shipped Ant theme resolver.
- `PaletteRoles`: primary, neutral, success, warning, error, and information role usage.

Validation rules:

- Only shipped `AntTheme.antLight` and `AntTheme.antDark` are used.
- Appearance changes must not change behavior.
- Palette roles must be reviewed against local Ant guidance and color-policy expectations.

## Enterprise Template Page

Realistic Ant-style page pattern composed from showcased controls.

Fields:

- `TemplateId`: one of `workbench`, `list`, `detail`, `form`, `result`, `exception`.
- `PageId`: navigation id.
- `ComposedControlIds`: known catalog controls used by the template.
- `SeedData`: deterministic business-like data.
- `PrimaryInteraction`: meaningful interaction or display-only reason.
- `ValidationRules`: template-specific validation, filtering, pagination, or recovery rules.

Validation rules:

- All six templates are present and populated.
- At least five of six templates include a meaningful interaction or state transition.
- The form template rejects invalid submission and shows success only after valid submission.
- The list template visibly updates filtering, selection, or pagination.
- The exception template records or displays the selected recovery path.
- Template controls must map to known catalog ids.

## Visual Review Target

One page/theme/size visual review obligation.

Fields:

- `TargetId`: stable id from page, appearance, and size role.
- `PageId`: target page.
- `AppearanceId`: `antLight` or `antDark`.
- `SizeRole`: `preferred` or `minimum`.
- `Width`: target width.
- `Height`: target height.
- `Required`: true for all feature targets.
- `CaptureStatus`: complete, degraded, missing, blocked, or environment-limited.
- `ReviewerStatus`: pending, clear, has-findings, or blocked.

Validation rules:

- Required target matrix is all pages x both appearances x both accepted sizes.
- Current planned count is 76 targets.
- Missing, degraded, blocked, or environment-limited capture cannot be accepted as final live visual evidence.
- Reviewer classification must exist before a target is accepted.

## Visual Review Finding

Recorded visual or Ant conformance issue.

Fields:

- `FindingId`: stable id.
- `TargetIds`: affected visual targets.
- `Category`: palette, spacing, typography, contrast, clipping, overlap, alignment, state, Ant conformance, or stale state.
- `Severity`: info, warning, blocking.
- `Status`: open, fixed, reviewed, closed.
- `Description`: reviewer-readable issue.
- `Expected`: expected Ant or repo behavior.
- `Actual`: observed behavior.
- `FixReference`: commit/task/change reference when fixed.
- `ReviewedAt`: review timestamp or run id when rechecked.

Validation rules:

- Open or fixed-but-not-reviewed findings keep the final review unresolved.
- A finding can close only after affected targets are re-reviewed.
- Final acceptance requires zero unresolved findings.

## Review Evidence Record

Repeatable record of coverage, interactions, visual review, and limitations.

Fields:

- `RunId`: deterministic or run-local id.
- `Seed`: input seed.
- `Command`: exact command.
- `Pages`: reviewed pages.
- `Interactions`: exercised interaction contracts and outcomes.
- `VisualTargets`: target statuses.
- `Coverage`: coverage result.
- `Findings`: visual findings.
- `Limitations`: live display, GL, screenshot, headless, or synthetic limitations.
- `OverallStatus`: accepted, blocked, review-required, environment-limited, or failed.
- `Artifacts`: generated JSON, Markdown, PNG, contact sheet, and log paths.

Validation rules:

- Running with the same inputs produces the same page set, interactions, outcomes, and limitation disclosures except for explicitly dynamic timestamps/paths.
- Limitations must not be summarized as accepted visual fidelity.
- The command must complete without hanging when live visual review is unavailable.

## State Transitions

Theme switching:

```text
light -> dark -> light
```

The transition preserves page and control state.

Interaction contract:

```text
seeded -> action-applied -> visible-state-changed -> recorded
seeded -> action-applied -> validation-error
seeded -> action-applied -> overlay-open
overlay-open -> action-applied -> overlay-closed
```

Visual finding:

```text
detected -> open
open -> fixed
fixed -> reviewed
reviewed -> closed
reviewed -> open
```

Review evidence:

```text
not-run -> running -> accepted
not-run -> running -> review-required
not-run -> running -> blocked
not-run -> running -> environment-limited
```
