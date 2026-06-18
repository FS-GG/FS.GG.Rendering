# Feature Specification: AntShowcase Visual Overhaul

**Feature Branch**: `162-enhance-showcase-visuals`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "create specs to comprehensively fix and enhance the showcase as much as possible. size can also be bigger if preferable."

## Context

The AntShowcase is current for catalog coverage, but a fresh all-page screenshot audit shows it is not visually ready. The current pages exhibit severe visual and layout defects: navigation labels draw into page content, the theme control appears inside page content, light-theme screenshots expose black unpainted surfaces, dense pages compress controls into unreadable rows, chart pages overpaint neighboring sections, and the global feedback area collides with page content.

This feature makes the AntShowcase a polished, inspectable showcase rather than only a catalog-completeness proof. It may increase the default interactive and evidence capture size if the larger size is declared and improves readability. The result must still be bounded, deterministic, and reviewable across the whole page set.

## Clarifications

### Session 2026-06-18

- Q: What accepted showcase sizes should the feature require for visual readiness? → A: Preferred 1600x1000; minimum 1280x800.
- Q: How should the readiness evidence classify visual defects? → A: Screenshot completeness is automated; defect classification is reviewer-recorded.
- Q: What theme names should artifacts use? → A: Canonical theme ids are `antLight` and `antDark`; the CLI may accept `light,dark` aliases, but summaries and internal records must resolve them to the canonical ids.
- Q: How should subjective visual readiness be repeatable? → A: Reviewer-recorded defects must use a rubric that records severity, defect class, affected page/theme, and whether the issue blocks readiness.

## Change Classification

**Tier 1 (observable sample behavior)**. This changes visible sample behavior, user-facing showcase evidence, and readiness expectations. No public product interface change is intended. If planning discovers that public library surface changes are required to make the showcase correct, those changes must be explicitly called out and justified in the plan.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse every showcase page without shell collisions (Priority: P1)

A developer or reviewer opens the AntShowcase and can move through every page without the application shell obscuring the page. The top bar, navigation rail, content area, feedback area, and status text each occupy a stable readable region. Long navigation labels stay within the navigation region, and page content never draws underneath the navigation rail or footer.

**Why this priority**: The shell collisions are present on nearly every captured page and make the showcase unusable regardless of catalog coverage. Fixing the shell is the minimum viable improvement.

**Independent Test**: Capture every current showcase page at the accepted showcase size and inspect the images. The test passes only if no page has navigation text, top-bar controls, content, feedback, or status text overlapping another shell region.

**Acceptance Scenarios**:

1. **Given** any showcase page, **When** the page is displayed, **Then** navigation, top bar, content, feedback, and status regions are visually separate and readable.
2. **Given** a long page label such as a chart or exception-page label, **When** it appears in the navigation rail, **Then** the label remains inside the navigation region while preserving enough text for the user to identify the page.
3. **Given** content taller than the visible content region, **When** the page is displayed, **Then** content is constrained to the content region and remains reachable without drawing into the footer or status area.

---

### User Story 2 - Inspect every control family in a readable presentation (Priority: P1)

A developer evaluating the Ant control set can inspect each family page and understand what each control demonstrates. Each section has readable labels, enough spacing, and an appropriately sized demonstration area. Large controls such as charts, calendars, tables, lists, media, overlays, and drawers get larger regions or dedicated rows so their output does not overpaint other controls.

**Why this priority**: The current showcase proves that controls are mapped, but many mapped controls are not inspectable. A showcase that cannot be visually inspected does not fulfill its purpose.

**Independent Test**: For each catalog page, review the page screenshot and confirm every section label and demonstrated control is readable, contained inside its intended section, and not overpainted by another demonstrated control.

**Acceptance Scenarios**:

1. **Given** a catalog page with compact controls, **When** the page is displayed, **Then** each control has readable spacing and does not collide with adjacent controls.
2. **Given** a page with large visual controls, **When** the page is displayed, **Then** every large control has enough dedicated area to show its primary shape, labels, and value without crossing another section boundary.
3. **Given** an open transient surface such as a picker, menu, popover, or drawer, **When** it is shown for demonstration, **Then** it appears intentionally layered and does not accidentally hide unrelated content or create unreadable overprint.

---

### User Story 3 - Review polished enterprise template pages (Priority: P2)

A product developer views the six enterprise template pages and sees credible Ant-styled application examples rather than debug-like component stacks. The workbench, list, detail, form, result, and exception pages use clear hierarchy, alignment, spacing, readable content, and realistic density. Each template remains recognizably tied to its intended workflow.

**Why this priority**: The templates are the most useful proof that the control set can compose into real application pages. They currently inherit shell collisions and also need stronger hierarchy and page composition.

**Independent Test**: Capture the six template pages and review whether a first-time viewer can identify the page purpose, primary action, main content, and supporting content from the page itself.

**Acceptance Scenarios**:

1. **Given** any enterprise template page, **When** it is displayed, **Then** the page purpose and primary action are visually clear.
2. **Given** the form template, **When** it is displayed, **Then** fields, validation area, terms control, and submit action are aligned and readable without text overlap.
3. **Given** result and exception templates, **When** they are displayed, **Then** the outcome message, recovery action, and supporting details are balanced within the page rather than isolated in a narrow debug-looking panel.

---

### User Story 4 - Validate visual readiness with complete screenshot evidence (Priority: P2)

A maintainer can run a visual evidence pass and receive screenshots and a summary that are strong enough to detect the classes of problems seen in the audit. The evidence covers every page, records the chosen display size, includes both Ant light and Ant dark modes, automatically verifies screenshot completeness, supports reviewer-recorded defect classification, and distinguishes visual-readiness evidence from weaker non-blank screenshot evidence.

**Why this priority**: Existing evidence says pages are deterministic and non-blank, but explicitly does not prove visual fidelity. The feature needs a gate that can catch overlap, overpaint, unreadable surfaces, and shell collisions.

**Independent Test**: Run the visual evidence pass twice with the same inputs and confirm it produces complete per-page screenshots, contact sheets, automated screenshot completeness results, and a reviewer-recorded visual-defect summary for the accepted page set and theme set. Visual readiness is not accepted if screenshots are missing, degraded, or missing the required reviewer defect classification.

**Acceptance Scenarios**:

1. **Given** the visual evidence pass, **When** it completes on a host capable of screenshot capture, **Then** it writes one screenshot per page per required theme, at the declared accepted size.
2. **Given** the visual evidence summary, **When** a reviewer records a critical defect, **Then** the summary identifies the affected page and the defect class instead of reporting visual readiness.
3. **Given** screenshot capture is unavailable, **When** evidence is requested, **Then** the run discloses the unavailable capture and does not claim visual readiness.

---

### User Story 5 - Keep the showcase usable across accepted sizes and themes (Priority: P3)

A user can inspect the showcase in the preferred larger size and in the minimum supported inspection size without losing page content. Both Ant light and Ant dark modes paint complete, intentional surfaces with readable contrast. Resizing within the accepted range does not reintroduce shell collisions or hidden content.

**Why this priority**: The user explicitly allows a larger size, but a larger default should not become an undocumented workaround. The supported sizes and theme behavior must be declared and verified.

**Independent Test**: Capture representative pages at the preferred size and minimum supported inspection size in both themes. Confirm the pages remain readable, shell regions remain separated, and theme surfaces are complete.

**Acceptance Scenarios**:

1. **Given** the preferred showcase size, **When** any page is displayed, **Then** the page is visually balanced and inspectable without critical overlap.
2. **Given** the minimum supported inspection size, **When** a dense page is displayed, **Then** the page uses scrolling or responsive layout rather than allowing content to draw outside its region.
3. **Given** Ant light mode, **When** any page is displayed, **Then** the visible app surface is intentionally painted and does not expose large unplanned black regions.
4. **Given** Ant dark mode, **When** any page is displayed, **Then** text, controls, and structural surfaces remain readable and cohesive.

### Edge Cases

- The catalog page with the most controls must remain inspectable without collapsing section bodies into unreadable rows.
- Long navigation labels must not cover content, even when active, focused, or selected.
- Large chart and graph controls must not paint across neighboring sections or into the footer.
- Open transient surfaces must be either intentionally showcased in a controlled region or closed in the baseline screenshot; accidental overlap is a defect.
- The feedback area must not consume enough height to make ordinary page inspection impossible.
- Missing optional assets must degrade into a readable placeholder rather than creating broken layout or black empty regions.
- If the current page count or catalog count changes before completion, evidence and coverage expectations must update to the live page and catalog set.
- If a defect is caused by a lower-level library limitation rather than sample composition, the limitation must be documented with a bounded follow-up and the affected page must not be marked visually ready.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The showcase MUST declare the preferred inspection size as 1600x1000 and the minimum supported inspection size as 1280x800.
- **FR-002**: The showcase MUST ensure top bar, navigation rail, content region, feedback area, and status text occupy separate visible regions with no overlap on every page at every accepted size.
- **FR-003**: The navigation rail MUST keep every page label within the rail while preserving page identification and current-page state.
- **FR-004**: The content region MUST contain all page content through layout, scrolling, or pagination, so page content never draws into the navigation rail, top bar, feedback area, or status area.
- **FR-005**: Ant light and Ant dark modes MUST paint complete intentional shell and content surfaces. Light mode MUST NOT expose large unplanned black background regions.
- **FR-006**: Every catalog page MUST present controls in readable sections with clear labels, consistent spacing, and enough area for the demonstrated control to be inspected.
- **FR-007**: Large or high-density controls MUST receive dedicated demonstration regions sized for their content, including charts, graphs, calendars, data collections, media, overlays, drawers, and multi-item selectors.
- **FR-008**: Demonstrated transient surfaces MUST be intentionally positioned and readable; accidental overprint across unrelated controls is a visual defect.
- **FR-009**: The global feedback and status experience MUST remain available without dominating the page or colliding with page content.
- **FR-010**: The six enterprise templates MUST be visually enhanced into cohesive application examples with clear page purpose, primary action, hierarchy, alignment, and realistic content density.
- **FR-011**: Theme switching MUST preserve the current page and visible state while repainting all shell and content surfaces coherently for the selected theme.
- **FR-012**: The showcase MUST continue to cover every current catalog control exactly once across catalog pages, with all current template pages still reachable.
- **FR-013**: Visual evidence MUST capture every showcase page in both required themes at the declared preferred size, and MUST produce a contact sheet or equivalent overview for review.
- **FR-014**: Visual evidence MUST automate screenshot completeness checks and MUST provide reviewer-recorded classification and reporting for at least these critical defect classes: shell overlap, navigation label spill, top-bar displacement, content-footer collision, unplanned background exposure, section overpaint, clipped primary label, unreadable primary content, transient-surface overprint, and template hierarchy unclear.
- **FR-015**: Visual readiness MUST NOT be accepted when required screenshots are missing, degraded, or explicitly not authoritative for visual layout.
- **FR-016**: Visual evidence MUST record the page count, theme set, accepted size, and any unavailable capture capability so reviewers know exactly what was proven.
- **FR-017**: If a visual defect cannot be fixed within the sample alone, the feature MUST document the owning limitation and exclude the affected claim from readiness until a bounded follow-up exists.
- **FR-018**: The completed showcase MUST remain navigable through visible in-app affordances without requiring users to read external instructions to find pages, identify the current page, switch themes, or inspect controls.

### Key Entities

- **Showcase Shell**: The persistent app chrome containing the top bar, navigation rail, content region, feedback area, and status text.
- **Showcase Page**: A catalog or template page that users can navigate to and inspect.
- **Control Demonstration Section**: A labeled area within a catalog page that presents one control or a closely related control group with representative content.
- **Large Demonstration Region**: A larger bounded area reserved for controls whose primary visual content cannot fit in a compact row.
- **Enterprise Template Page**: A composed page that demonstrates a complete application workflow such as workbench, list, detail, form, result, or exception.
- **Visual Evidence Run**: A complete capture of the required page and theme set at the declared accepted size, with automated screenshot completeness results and enough summary output for reviewer-recorded defect classification.
- **Visual Defect**: A classified issue that prevents a screenshot from being considered visually ready, such as overlap, overpaint, clipping, missing surface, or unreadable content.
- **Reviewer Defect Rubric**: The repeatable reviewer checklist for visual evidence. It records page id, canonical theme id, severity, defect class, readiness impact, reviewer, timestamp, and notes.
- **Accepted Size**: A declared showcase size used for readiness evidence, including the preferred inspection size and minimum supported inspection size.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of current showcase pages are captured in Ant light and Ant dark at the preferred inspection size, with zero missing or degraded screenshots in the readiness run.
- **SC-002**: Across the readiness screenshots, there are zero critical shell defects: navigation/content overlap, top-bar/content overlap, content/footer collision, or status/content collision.
- **SC-003**: Across all catalog pages, 100% of demonstrated controls have readable labels and visible primary content within their intended section or demonstration region.
- **SC-004**: Chart, graph, data collection, calendar, overlay, and other large-control pages show zero section overpaint defects in the readiness screenshots.
- **SC-005**: Ant light screenshots show zero pages with large unplanned black background exposure; Ant dark screenshots show zero pages with unreadable primary text caused by theme contrast.
- **SC-006**: 100% of the live catalog controls remain mapped exactly once to a catalog page, and all six enterprise template pages remain reachable.
- **SC-007**: A reviewer can identify the purpose and primary action of each enterprise template page from its screenshot alone.
- **SC-008**: The visual evidence summary lists the page count, theme count, accepted size, automated screenshot completeness result, and reviewer-recorded defect result, and it does not mark visual readiness accepted if any critical defect is recorded.
- **SC-009**: The preferred 1600x1000 inspection size and minimum supported 1280x800 inspection size are documented, and representative dense pages remain readable at both sizes.
- **SC-010**: A first-time reviewer can identify the current page, available page navigation, current theme, theme switcher, and primary inspection region from the live showcase or a readiness screenshot without external instructions.

## Assumptions

- The current showcase baseline is 19 pages: 13 catalog pages and 6 enterprise template pages.
- The current catalog baseline is 96 controls, but completion must validate against the live catalog at the time the feature is accepted.
- The default showcase and evidence size may increase to the preferred 1600x1000 inspection size, while 1280x800 remains the minimum supported inspection size.
- Visual readiness requires real screenshots. Non-blank deterministic evidence alone is not sufficient for this feature.
- The feature should fix sample composition first. Lower-level library changes are allowed only when a visual defect cannot be correctly fixed by sample layout and must be called out during planning.
- The existing feedback capture remains valuable, but it may be repositioned, resized, collapsed, or otherwise redesigned to stop it from damaging page inspection.
- Existing Ant coverage and template scope remain in force; this feature improves visual quality and evidence strength rather than reducing the showcased control set.

## Out of Scope

- Removing existing catalog coverage to make pages easier to lay out.
- Claiming pixel-perfect parity with upstream Ant Design.
- Adding new product themes unrelated to Ant light and Ant dark.
- Treating a degraded or missing screenshot run as accepted visual readiness.
