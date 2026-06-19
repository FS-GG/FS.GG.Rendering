# Feature Specification: Second Ant Showcase Sample

**Feature Branch**: `171-second-antshowcase-sample`

**Created**: 2026-06-19

**Status**: Draft

**Change Classification**: Tier 1 (contracted sample and evidence workflow addition; no planned `FS.GG.UI.*` product public API change)

**Input**: User description: "create a new, second antshowcase sample like https://github.com/FS-GG/FS.GG.Rendering/tree/main/samples/AntShowcase take special care to follow antd design rules, color palettes, spacing. wire all controls up to show they work. check iteratively if there are visual problems/antd violations and loop back to fix them. use the available skills and ressources."

## Overview

Create a second Ant showcase sample that stands beside the existing AntShowcase sample without replacing or weakening it. The new sample must make the current control catalog easier to inspect as a complete Ant Design experience: every current control is reachable, populated with credible example content, and either visibly interactive or clearly display-only.

The sample must give special attention to Ant Design fidelity. Palette roles, spacing rhythm, typography hierarchy, component states, and page composition must be reviewed in both light and dark appearances. Visual problems and Ant Design violations are not accepted as known defects; each finding must feed a fix-and-review loop until the accepted review set has zero unresolved issues.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Explore every control as a live Ant sample (Priority: P1)

A maintainer opens the second Ant showcase and browses the control-family pages. Each current control appears with representative content in the Ant visual language. Interactive controls visibly respond to pointer or keyboard input, while display-only controls render clearly without pretending to be interactive.

**Why this priority**: This is the central value of the feature. A second showcase is useful only if it proves the current controls are complete, discoverable, and demonstrably live under Ant styling.

**Independent Test**: Review the control-family pages and run the coverage check. Confirm every current control appears exactly once, every interactive control has a visible state transition, and every display-only control is identified as display-only.

**Acceptance Scenarios**:

1. **Given** the second showcase is open, **When** the user visits each control-family page, **Then** every current control appears once with representative content and Ant styling.
2. **Given** an interactive control is visible, **When** the user performs its documented primary interaction, **Then** the control shows a visible value, state, selection, navigation, overlay, validation, or feedback change.
3. **Given** a display-only control is visible, **When** the page is reviewed, **Then** the control renders its intended content and does not present a misleading interaction affordance.
4. **Given** a control needs data, choices, progress, media, graph content, or chart series, **When** its page renders, **Then** it uses representative seeded content rather than an empty placeholder.

---

### User Story 2 - Verify Ant Design visual fidelity iteratively (Priority: P1)

A reviewer inspects every page in Ant light and Ant dark appearances using the accepted review sizes. They check color palette roles, spacing, typography, alignment, control states, contrast, clipping, overlap, and page rhythm. Any visual problem or Ant Design violation is recorded, fixed, and reviewed again until no unresolved findings remain.

**Why this priority**: The user explicitly requested care around Ant Design rules, palettes, spacing, and iterative visual correction. This quality loop is required before the sample can be considered accepted.

**Independent Test**: Produce the visual review set for all pages and both appearances, inspect it against the Ant visual checklist, and verify the finding log reaches zero unresolved issues.

**Acceptance Scenarios**:

1. **Given** the full visual review set, **When** the reviewer checks each page, **Then** every page passes palette, spacing, typography, state, and contrast expectations for the active Ant appearance.
2. **Given** a visual issue is found, **When** the fix is applied, **Then** the affected page and any related pages are reviewed again before the finding is closed.
3. **Given** the final accepted review set, **When** unresolved findings are counted, **Then** the count is zero.
4. **Given** the sample is shown at the accepted minimum size, **When** dense pages, overlays, long labels, and data-heavy controls are inspected, **Then** no text is clipped, no controls overlap, and the page remains usable.

---

### User Story 3 - Demonstrate complete enterprise Ant page patterns (Priority: P2)

A developer planning an enterprise application opens the template section and sees realistic Ant-style pages: workbench, list, detail, form, result, and exception. Each template uses the same showcased controls as composition pieces and includes enough interaction to prove the page is not a static mockup.

**Why this priority**: The existing AntShowcase proves template breadth. The second sample must keep that value while making the pages more convincing as live examples.

**Independent Test**: Review each template page independently, interact with its primary controls, and confirm the page uses known showcased controls rather than one-off visual stand-ins.

**Acceptance Scenarios**:

1. **Given** the template section is open, **When** the user visits each template page, **Then** all six templates render as complete, populated Ant-style pages.
2. **Given** the form template is visible, **When** invalid values are submitted, **Then** validation feedback appears and success is not shown; **When** valid values are submitted, **Then** a success result is shown.
3. **Given** the list template is visible, **When** the user changes filtering, selection, or pagination, **Then** the list state visibly updates.
4. **Given** the exception template is visible, **When** the user selects a recovery action, **Then** the sample records or displays the intended recovery path.

---

### User Story 4 - Switch Ant light and Ant dark without behavior drift (Priority: P2)

A user switches between Ant light and Ant dark while staying on the same page. The sample restyles the shell, controls, overlays, and template content cohesively while preserving navigation position and current control state.

**Why this priority**: Ant visual fidelity must hold across both appearances, and the theme switch is the fastest way to expose stale colors, spacing mistakes, and state coupling.

**Independent Test**: Start from representative pages with active state, switch appearances, and verify state is preserved while the visual palette changes consistently.

**Acceptance Scenarios**:

1. **Given** any page is open in Ant light, **When** the user switches to Ant dark, **Then** the same page remains selected and all visible regions restyle to the dark appearance.
2. **Given** text has been entered, a selection made, or an overlay opened, **When** the user switches appearances, **Then** the user-visible state is preserved.
3. **Given** the same page is inspected in both appearances, **When** behavior is compared, **Then** behavior is identical and only visual treatment changes.

---

### User Story 5 - Produce honest repeatable review evidence (Priority: P3)

A maintainer or automated review path can exercise representative interactions and produce repeatable review evidence. The evidence makes clear what it proves, what it does not prove, and whether the host environment limited the review.

**Why this priority**: Evidence keeps the sample maintainable over time, but browsing and visual acceptance deliver the primary value first.

**Independent Test**: Run the representative review twice with the same inputs and compare the page outcomes, visual summaries, and limitation disclosures.

**Acceptance Scenarios**:

1. **Given** a fixed representative review input set, **When** the review is run twice, **Then** the same pages, interactions, outcomes, and pass/fail summaries are produced.
2. **Given** review evidence is produced, **When** a maintainer reads it, **Then** it clearly states any limitations and does not claim live visual acceptance when the host could not provide it.
3. **Given** the host cannot show a live window, **When** review evidence is requested, **Then** the request completes without hanging and reports the limitation clearly.

### Edge Cases

- The current control set grows or shrinks: coverage fails until the second showcase is updated so every current control appears exactly once.
- A control is display-only: it is excluded from interaction-state requirements but still must render representative content with Ant styling.
- A control opens an overlay, popover, menu, drawer, tooltip, or dialog: the visual review must include a stable visible state that does not block navigation or hide unrelated content.
- Long labels, dense data, narrow accepted review size, and dark appearance: text must remain readable, controls must not overlap, and spacing must remain intentional.
- Invalid form data: validation feedback must be visible and success output must not appear.
- Theme switch during active state: entered values, selection, open overlays, and current page must remain stable.
- Host environment cannot provide live visual review: the sample must report the limitation and avoid treating the run as accepted visual evidence.
- A reviewer finds a palette, spacing, contrast, typography, clipping, overlap, stale-state, or Ant Design violation: the issue remains open until a fix is reviewed against the affected surfaces.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST deliver a second Ant showcase sample that is independently discoverable and does not replace, rename, or reduce the existing AntShowcase sample.
- **FR-002**: The sample MUST provide a navigable shell with control-family pages, template pages, Ant light/dark appearance switching, clear current-page indication, and a visible review/status area.
- **FR-003**: The sample MUST include every current control exactly once across the control-family pages, with a coverage result that identifies missing or duplicated controls.
- **FR-004**: Every control demonstration MUST include representative content, state, or data so the control appears realistic rather than empty.
- **FR-005**: Every interactive control demonstration MUST include at least one visible state-changing interaction; display-only controls MUST be identified as non-interactive demonstrations.
- **FR-006**: Interaction coverage MUST include buttons/actions, text entry, numeric input, date/time selection, sliders/rating, toggles, single and multi-selection, navigation, paging, menus, overlays, feedback, validation, data collections, charts, graphs, and custom surfaces where those controls exist in the current catalog.
- **FR-007**: The sample MUST follow the repo's Ant Design adoption guidance: Ant is used as a design language over the existing semantic controls, without introducing duplicate Ant-only control behavior.
- **FR-008**: The sample MUST use cohesive Ant palette roles for primary, neutral, success, warning, error, and information states in both light and dark appearances.
- **FR-009**: The sample MUST follow the established Ant spacing rhythm for shell chrome, page sections, cards, rows, form fields, control groups, gutters, and dense data regions.
- **FR-010**: The sample MUST show clear visual treatment for normal, hover, active, focus, disabled, selected, checked, error, validation, loading, and overlay states wherever a showcased control supports those states.
- **FR-011**: Switching between Ant light and Ant dark MUST preserve current page, entered values, selection state, expanded/collapsed state, overlay state, and validation state while restyling visible surfaces.
- **FR-012**: The sample MUST include six enterprise template pages: workbench, list, detail, form, result, and exception.
- **FR-013**: Each enterprise template page MUST be composed from showcased controls and include at least one meaningful interaction or state transition unless the page is inherently display-only.
- **FR-014**: The visual review workflow MUST inspect all pages in both Ant light and Ant dark at the accepted preferred and minimum review sizes.
- **FR-015**: The visual review workflow MUST record palette, spacing, typography, contrast, clipping, overlap, alignment, state, and Ant Design conformance findings.
- **FR-016**: A finding from the visual review workflow MUST remain unresolved until the affected page or control has been fixed and reviewed again.
- **FR-017**: The feature MUST reach zero unresolved visual findings before the sample is accepted.
- **FR-018**: The review evidence MUST disclose limitations, including whether a live visual environment was available, and MUST not overstate synthetic or environment-limited results.
- **FR-019**: The sample MUST provide a repeatable representative review path that exercises primary interactions and reports the same outcomes when run again with the same inputs.
- **FR-020**: The sample MUST document its relationship to the existing AntShowcase sample, the Ant Design source guidance used for review, and any review limitations.
- **FR-021**: The feature MUST NOT add new product controls, new product themes, or alter existing product behavior solely to make the sample pass.

### Key Entities

- **Second Ant Showcase Sample**: The new standalone sample experience that demonstrates current controls and enterprise page patterns under Ant Design styling.
- **Control Demonstration**: A page section showing one current control with representative content, expected state, and any required interaction.
- **Interaction Contract**: The user-visible behavior expected when an interactive control is exercised.
- **Ant Appearance**: Either the light or dark Ant visual appearance used by the sample.
- **Enterprise Template Page**: A realistic page pattern such as workbench, list, detail, form, result, or exception.
- **Visual Review Finding**: A recorded palette, spacing, typography, contrast, clipping, overlap, alignment, state, or Ant Design conformance issue.
- **Review Evidence Record**: A repeatable record of reviewed pages, exercised interactions, outcomes, findings, and environment limitations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of current controls are reachable across the control-family pages, with zero missing controls and zero duplicated controls in the final coverage result.
- **SC-002**: 100% of interactive controls have at least one verified visible state-changing interaction; 100% of display-only controls are explicitly treated as display-only demonstrations.
- **SC-003**: All six enterprise template pages render populated and at least five of the six include a meaningful interaction or state transition.
- **SC-004**: 100% of pages pass final visual review in both Ant light and Ant dark at the accepted preferred and minimum review sizes.
- **SC-005**: The final visual review has zero unresolved findings for palette, spacing, typography, contrast, clipping, overlap, alignment, control state, or Ant Design conformance.
- **SC-006**: Switching between Ant light and Ant dark preserves current page and active user-visible state in 100% of representative theme-switch checks.
- **SC-007**: A reviewer can reach any control demonstration or enterprise template page in no more than two navigation actions from the sample shell.
- **SC-008**: Running the representative review path twice with the same inputs produces the same reviewed page set, interaction outcomes, pass/fail summary, and limitation disclosures.
- **SC-009**: When a live visual environment is unavailable, the review request completes in under 30 seconds with a clear limitation disclosure and no false acceptance of visual fidelity.
- **SC-010**: A maintainer unfamiliar with the new sample can identify its purpose, relation to the existing AntShowcase, reviewed Ant guidance, and current visual-review status in under 10 minutes.

## Assumptions

- The new sample is intentionally a second showcase, not a replacement for the existing AntShowcase.
- "Every current control" means the control set available at implementation time; the exact count may change, and the coverage result is responsible for detecting drift.
- The existing AntShowcase sample is the behavioral baseline for breadth, light/dark support, enterprise templates, and honest evidence, while this feature raises the bar for live interaction coverage and visual conformance review.
- The local Ant Design source hub, pattern docs, and `fs-gg-ant-design` guidance are the accepted Ant design references for this repository.
- The accepted visual review sizes include a preferred desktop inspection size and a smaller minimum size so dense pages and layout constraints are exercised.
- If a host cannot provide live visual inspection, environment-limited evidence is acceptable only as a disclosed limitation, not as final visual acceptance.

## Out of Scope

- Replacing, deleting, or reducing the existing AntShowcase sample.
- Adding new product controls, product themes, or design-system behavior.
- Treating Ant as anything other than a design-language reference for this sample.
- Turning the sample into a performance benchmark or release gate beyond the review evidence described here.
- Creating unrelated themes or non-Ant showcase variants.

## Dependencies

- The existing AntShowcase sample and its documented coverage, template, light/dark, and evidence expectations.
- The repository's Ant Design source hub and pattern documentation for palette, spacing, semantic regions, and state treatment.
- The current rendered control set and existing sample-review capabilities available during planning and implementation.
