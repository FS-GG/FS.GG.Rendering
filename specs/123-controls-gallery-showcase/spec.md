# Feature Specification: Controls Gallery Showcase (Light/Dark)

**Feature Branch**: `123-controls-gallery-showcase`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan" → Workstream **G1** of `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md` — the flagship Controls Gallery sample application, a multi-page showcase that exercises every catalog control on the existing Light/Dark themes, landable early and independently of the design-system (F) and layer-split (D) work because it needs only the controls that ship today.

## Overview

The framework publishes a catalog of **52 controls** but ships **no runnable application** that demonstrates them. There is nothing a newcomer can open to see the controls working, nothing that proves the whole stack (controls + layout + input + viewer + Elmish) composes into a real tool, and nothing that holds the catalog honest as it changes.

This feature delivers a **Controls Gallery**: a single sample application with a navigable multi-page shell that renders all 52 catalog controls, themeable between Light and Dark with an accent selector, and runnable in two modes — an **interactive** windowed mode for humans and a **headless deterministic evidence** mode that produces repeatable, disclosed outcomes for continuous integration. It is the highest-leverage validation surface in the plan: living documentation, an end-to-end integration test, and deterministic perf-corpus material, all at once.

This is a **sample consumer of the framework**, not product/library code. It builds against the public package surface only — no privileged internal access — which is itself the proof that the documented consumption path works end to end.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse every control across a navigable gallery (Priority: P1)

A developer evaluating or learning the framework opens the gallery and navigates a left rail of pages. Each page groups a family of controls (display, buttons, text/numeric input, selection/toggles, data/collections, layout/containers, navigation/menus, overlays/feedback, charts, and a pointer-playground/custom page). Every one of the 52 catalog controls appears on exactly one page, rendered with representative content, and is reachable purely by navigation. The developer can find and see any control working without reading source code.

**Why this priority**: This is the MVP. A gallery that renders all controls across navigable pages delivers the core value — living documentation and proof the controls compose — even with a single theme and no evidence mode. Everything else enriches this.

**Independent Test**: Launch the gallery, walk all pages, confirm every catalog control is present and rendered; run an automated coverage check that maps each catalog control id to exactly one page and fails if any control is unreferenced or duplicated.

**Acceptance Scenarios**:

1. **Given** the gallery is open on the first page, **When** the user selects each page in the navigation rail, **Then** every page renders its grouped controls with representative content and no rendering errors.
2. **Given** the full catalog of 52 controls, **When** the coverage check runs, **Then** every control id maps to exactly one page (none missing, none duplicated) and the check passes.
3. **Given** a control that requires data (e.g. a list, grid, tree, or chart), **When** its page renders, **Then** the page supplies representative seeded content so the control is shown populated, not empty.
4. **Given** any page, **When** it is displayed, **Then** every control on it is reachable from the navigation rail in at most two navigation actions (select page, scroll into view).

---

### User Story 2 - Switch theme and accent with consistent, cohesive styling (Priority: P2)

A user toggles between Light and Dark from the top app bar and picks an accent from a selector. The whole gallery restyles to one cohesive palette ("Indigo & Teal on Slate") in the chosen mode and accent. Control **behavior and accessibility are unchanged** across themes — only the resolved visuals differ. This demonstrates the framework's "one semantic control set, many appearances" rule using the themes that exist today (Light/Dark), without depending on the not-yet-built Ant/Fluent/Material themes.

**Why this priority**: Theme switching is a defining capability of the framework and the most visible interaction in any controls gallery, but the gallery is already valuable for browsing controls before it is added.

**Independent Test**: Render the same page under Light, Dark, and each accent; assert the control tree and accessibility contract are identical across variants while the resolved visual styling differs.

**Acceptance Scenarios**:

1. **Given** the gallery on any page in Light mode, **When** the user toggles to Dark, **Then** every control and the shell chrome restyle to the Dark palette and remain legible.
2. **Given** any theme mode, **When** the user selects a different accent, **Then** accent-driven elements update consistently across all controls and pages.
3. **Given** the same page rendered under two different theme/accent variants, **When** their behavior and accessibility metadata are compared, **Then** they are identical and only the resolved visuals differ.

---

### User Story 3 - Produce deterministic, disclosed per-page evidence headlessly (Priority: P3)

A continuous-integration job runs the gallery in headless evidence mode with an explicit seed. For each page it replays a seeded input script and emits a repeatable evidence record: the resulting frame/state outcome and a screenshot of the required surfaces, plus an honest disclosure of what the run does and does **not** prove. Running the same seed twice yields byte-identical evidence. When no display/GL is available, the run degrades cleanly — it skips or falls back to the headless path with a disclosed reason, never hangs and never reports a fake pass.

**Why this priority**: Determinism and disclosed evidence turn the gallery from a demo into a *checked* artifact that CI can rely on and that feeds the perf corpus. It depends on the gallery (P1) existing first.

**Independent Test**: Run the headless evidence mode twice with the same seed and diff the outputs for byte-identity; inspect each page's evidence record for a non-empty "not authoritative for" disclosure; run on a host without display/GL and confirm a clean skip/fallback (exit 0, disclosed reason, no hang).

**Acceptance Scenarios**:

1. **Given** a fixed seed, **When** the headless evidence mode runs over all pages twice, **Then** the two evidence sets are byte-identical.
2. **Given** any page's evidence record, **When** it is produced, **Then** it carries a non-empty disclosure of what the run is not authoritative for.
3. **Given** a host without a display or GL, **When** the gallery is asked to produce evidence, **Then** it skips or falls back with a disclosed reason and a success (non-hang) exit, rather than failing or fabricating a result.
4. **Given** a page's acceptance criteria from its source spec, **When** its seeded run completes, **Then** the captured state/frame outcome satisfies those criteria.

---

### User Story 4 - Exercise pointer and keyboard interaction (Priority: P3)

A user (or a seeded input script) clicks buttons, types into text and numeric inputs, toggles checkboxes/switches, changes selections, and interacts with overlays. Interactive controls respond visibly (the input → MVU → repaint path), and a documented pointer-interaction contract describes how each interactive family behaves so the showcase exercises real input, not just static rendering.

**Why this priority**: Interaction proves the controls are live, but the gallery already demonstrates rendering and theming before interaction wiring is complete; interaction also overlaps with the seeded-evidence work (US3).

**Independent Test**: Drive a seeded pointer/keyboard script against an interactive page and assert the visible state change matches the documented interaction contract; static (display-only) controls are exempt and simply render.

**Acceptance Scenarios**:

1. **Given** an interactive control on a page, **When** a pointer or keyboard input targets it, **Then** its visible state changes according to the documented interaction contract.
2. **Given** a display-only control (e.g. label, separator, badge), **When** the page is interacted with, **Then** the control renders correctly and is not expected to respond to input.

---

### Edge Cases

- **Catalog grows or shrinks**: if a control is added to or removed from the catalog, the coverage check fails until the gallery is updated to show it on exactly one page. This is intentional — it keeps the gallery honest about catalog drift.
- **A control needs populated data**: data/collection and chart controls are shown with representative seeded content so they never appear empty or broken.
- **No display / no GL host**: interactive mode is unavailable; headless evidence mode still runs (or skips cleanly with disclosure). The gallery never makes the CI gate depend on a display.
- **Theme switch mid-session**: switching theme or accent on any page restyles the entire gallery (chrome and all controls) consistently, with no stale styling left behind.
- **Overlapping/overlay controls** (dialog, toast, tooltip, context menu, overlay): shown in a state that makes them visible for the page's evidence without trapping navigation.
- **Identifier provenance**: the source specs reference the archived `FS.Skia.UI.*` identifiers; on adoption these are rebranded to `FS.GG.UI.*`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The gallery MUST present a navigable multi-page shell consisting of a top app bar (with a Light/Dark theme toggle and an accent selector), a left navigation rail, a scrolling content region, and a bottom status strip.
- **FR-002**: The gallery MUST organize all 52 catalog controls across exactly 10 pages grouped by control family, with **every catalog control appearing on exactly one page** and reachable by navigation.
- **FR-003**: The gallery MUST provide an automated coverage check that maps each catalog control to its page and fails if any catalog control is unreferenced or referenced more than once.
- **FR-004**: The gallery MUST render every showcased control with representative content, supplying seeded data for controls that require items/values so none appears empty.
- **FR-005**: The gallery MUST support Light and Dark themes and an accent selection, applying one cohesive palette across the shell and all controls.
- **FR-006**: Switching theme or accent MUST leave each control's behavior and accessibility contract unchanged while changing only the resolved visuals.
- **FR-007**: The gallery MUST run in two modes: an interactive windowed mode and a headless deterministic evidence mode.
- **FR-008**: The headless evidence mode MUST accept an explicit seed, replay a seeded input script per page, and produce a repeatable per-page evidence record (frame/state outcome plus a screenshot of the required surfaces).
- **FR-009**: Headless evidence runs MUST be deterministic — using injected/seeded inputs and no wall-clock or randomness — so the same seed yields byte-identical evidence across runs.
- **FR-010**: Every evidence record MUST disclose what the run is **not** authoritative for (a non-empty disclosure), consistent with the project's no-overclaim evidence rule.
- **FR-011**: When a display or GL host is unavailable, the gallery MUST degrade and disclose — skip or fall back with a stated reason and a non-failing, non-hanging outcome — never fabricate a pass.
- **FR-012**: Interactive controls MUST respond visibly to pointer and keyboard input per a documented pointer-interaction contract; display-only controls are exempt and simply render.
- **FR-013**: The gallery MUST build and run against the framework's **public package surface only**, with no privileged internal access.
- **FR-014**: The gallery MUST use only controls and themes that exist today (the catalog controls and the Light/Dark themes); it MUST NOT depend on the not-yet-built Ant/Fluent/Material themes or design-specific kits.
- **FR-015**: Imported identifiers from the source specifications MUST be rebranded from `FS.Skia.UI.*` to `FS.GG.UI.*`, and the adoption MUST be recorded in provenance documentation.
- **FR-016**: The headless evidence mode MUST be the continuous-integration-facing path so the gallery does not make the required gate depend on a display or GL.

### Key Entities

- **Gallery Page**: One of the 10 pages — has a title, a control-family grouping, the set of catalog controls it showcases, and the seeded demo state used to populate them.
- **Showcase Shell**: The application chrome — top app bar (theme toggle + accent selector), left navigation rail, scrolling content region, bottom status strip.
- **Theme Variant**: A combination of mode (Light/Dark) and accent that resolves the one cohesive palette applied across the gallery.
- **Coverage Map**: The mapping from each catalog control id to the single page that showcases it, used by the coverage check.
- **Page Evidence Record**: The deterministic per-page output of headless mode — seeded input script, resulting frame/state outcome, screenshot of required surfaces, and the non-empty "not authoritative for" disclosure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 52 catalog controls are reachable across exactly 10 navigable pages, each control on exactly one page; the coverage check confirms zero unreferenced and zero duplicated controls.
- **SC-002**: A seeded headless run of the gallery produces byte-identical evidence across two consecutive runs with the same seed (100% reproducible).
- **SC-003**: For the same page, behavior and accessibility metadata are identical across every theme/accent variant while resolved visuals differ.
- **SC-004**: Every per-page evidence record carries a non-empty disclosure of what it does not prove; on a host without display/GL the evidence run skips or falls back with a disclosed reason and a non-hanging success outcome.
- **SC-005**: The gallery builds and runs using only the public package surface (no internal access), demonstrating the documented consumer path end to end.
- **SC-006**: A first-time viewer can reach any showcased control in at most two navigation actions.
- **SC-007**: Each page's seeded run satisfies the acceptance criteria carried by its source showcase specification.

## Assumptions

- **Source material**: The 10-page structure, the "Indigo & Teal on Slate" palette, the pointer-interaction contract, and the per-page evidence requirements derive from the archived FS-Skia-UI showcase specs (`docs/testSpecs/Showcase/01`–`10`) referenced in the implementation plan. Those specs live in the archive (repo `EHotwagner/FS-Skia-UI`); their content is adopted and rebranded here. Where a detail is unavailable from the archive, the plan's §10.1 grouping and the local `src/Controls/Catalog.fs` catalog are authoritative.
- **Page grouping**: The 10 pages map to the catalog's control families — display/typography, buttons, text/numeric input, selection/toggles, data/collections, layout/containers, navigation/menus, overlays/feedback, charts, and pointer-playground/custom — with the 11 catalog categories (display, input, selection, layout, navigation, overlay, feedback, data, chart, graph, custom) distributed across those 10 pages so each control lands on exactly one page.
- **Themes**: Only the existing Light/Dark themes (and their accent variants) are used; this feature is deliberately independent of Workstreams D and F.
- **Placement**: The gallery lives in a new `samples/` tree as its own `FS.GG.UI.*`-consuming project, outside the default test tier — consistent with the plan's §10.3 recommendation. The exact project layout and any template-generated variant are design decisions for the planning phase.
- **Modes**: Headless deterministic evidence mode is the CI path; interactive windowed mode is GL-gated and advisory, mirroring the rest of the harness.
- **Scope boundary**: This feature is **G1 only** — the Controls Gallery showcase on Light/Dark. Games and productivity samples (G2), the Ant restyle and enterprise templates (G3), and wiring samples into the perf corpus (G4) are out of scope.

## Out of Scope

- Games and productivity sample apps (Workstream G2).
- Ant-theme restyle and enterprise page templates (Workstream G3), which depend on Workstreams F and D.
- Feeding gallery runs into the perf/harness corpus and CI advisory tier (Workstream G4).
- Any new controls, new themes, or design-specific kits.
- Changes to the public package surface (the gallery is a consumer, not a contributor to product API).

## Dependencies

- The published control catalog (`src/Controls/Catalog.fs`, 52 controls) and the existing Light/Dark themes.
- The public package surface of the framework (controls, layout, input, viewer, Elmish) as consumed by a downstream application.
- The project's no-overclaim evidence conventions (non-empty "not authoritative for", degrade-and-disclose) that the per-page evidence records follow.
</content>
</invoke>
