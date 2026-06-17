# Feature Specification: Ant Design Controls Showcase (Ant restyle + enterprise templates)

**Feature Branch**: `135-antd-controls-showcase`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "create a comprehensive showcase of antd controls. use skills if available." → Workstream **G3** of `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md` — re-skin the controls showcase under the now-shipped **AntDesign theme** and realize the Ant enterprise page templates (workbench / list / detail / form / result / exception) as demonstrable pages. The Workstream **F** design-system arc (token taxonomy, color policy, `--design-system` parameter, central style resolver, public surface promotion, Ant pattern docs) and **D2.1** (feature 132, the concrete `FS.GG.UI.Themes.AntDesign` theme + the widened catalog of net-new Ant controls) have landed, so this is no longer dependency-blocked.

## Overview

The framework now ships a concrete **AntDesign theme** (`FS.GG.UI.Themes.AntDesign`, feature 132) and a **catalog widened with net-new Ant primitives** (Avatar, Tag, Alert, Collapse, Segmented, Rate, Timeline, Steps, Breadcrumb, Pagination, Card, Result, Empty, Drawer, Skeleton, …) — but there is **no runnable application that shows the Ant visual language in action**. The existing Controls Gallery (feature 123, Workstream G1) demonstrates the catalog only on the built-in Light/Dark themes; the games + productivity samples (feature 134, Workstream G2) are theme-light. Nothing yet proves that selecting the Ant theme restyles a real, multi-page application end to end, nothing exercises the net-new Ant controls in a populated layout, and nothing demonstrates the Ant **enterprise page templates** the whole design-system arc was building toward.

This feature delivers an **Ant Design Controls Showcase**: a single sample application, a navigable multi-page shell rendered under the **AntDesign theme** in both **light and dark** modes, that exercises **every catalog control** (including all net-new Ant primitives) with representative content, and that demonstrates the canonical Ant **enterprise page templates** — a workbench/dashboard, a list page, a detail page, a form page, a result page, and an exception page. Like its predecessors it runs in two modes — an **interactive** windowed mode for humans and a **headless deterministic evidence** mode that produces repeatable, disclosed outcomes for continuous integration.

This is a **sample consumer of the framework**, not product/library code. It builds against the **public package surface only** — including the public Ant theme and resolver surface promoted in Workstream F — with no privileged internal access, which is itself the proof that an app author can opt into the Ant look without forking controls. It is the visible payoff of the F + D design-system work: "one semantic control set, many appearances," demonstrated with the Ant appearance.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse every control rendered in the Ant visual language (Priority: P1)

A developer evaluating the framework's Ant support opens the showcase and navigates a left rail of pages. Each page groups a family of controls, and **every catalog control — including the net-new Ant primitives added in feature 132 — appears on exactly one page**, rendered under the AntDesign theme with representative content (Ant brand-blue primary, Ant control heights on the 8-unit grid, Ant radii/spacing/typography, Ant intent treatment). The developer can find and see any control working in the Ant style without reading source code.

**Why this priority**: This is the MVP and the core of the request — a comprehensive showcase of Ant-styled controls. A gallery that renders the full widened catalog under the Ant theme delivers the primary value (living proof the Ant theme styles the whole control set) even before enterprise templates, dark mode, or evidence runs are added.

**Independent Test**: Launch the showcase under the Ant theme, walk all pages, confirm every catalog control is present and rendered in the Ant style; run an automated coverage check that maps each catalog control id to exactly one page and fails if any control is unreferenced or duplicated.

**Acceptance Scenarios**:

1. **Given** the showcase is open on the first page under the Ant theme, **When** the user selects each page in the navigation rail, **Then** every page renders its grouped controls with representative content in the Ant visual language and with no rendering errors.
2. **Given** the full current catalog (the widened set, including net-new Ant controls), **When** the coverage check runs, **Then** every control id maps to exactly one page (none missing, none duplicated) and the check passes.
3. **Given** a control that requires data (a list, grid, tree, timeline, steps, chart, etc.), **When** its page renders, **Then** the page supplies representative seeded content so the control is shown populated, not empty.
4. **Given** any page, **When** it is displayed, **Then** every control on it is reachable from the navigation rail in at most two navigation actions (select page, scroll into view).

---

### User Story 2 - Demonstrate the Ant enterprise page templates (Priority: P2)

A developer planning a real enterprise application opens the showcase's **page-template** section and sees the canonical Ant enterprise layouts realized from the framework's semantic controls: a **workbench/dashboard** (overview cards, stats, activity), a **list page** (filter bar + paginated data collection), a **detail page** (descriptions/record view with related panels), a **form page** (sectioned form with validation and submit/result feedback), a **result page** (success/info outcome with follow-up actions), and an **exception page** (403/404/500-style states). Each template is a composition of catalog controls under the Ant theme — not new controls — proving the patterns documented in the Ant pattern docs are buildable on the public surface.

**Why this priority**: The enterprise templates are the distinguishing G3 deliverable beyond a flat control grid and the concrete justification for the design-system arc, but the showcase already delivers value by browsing controls before the templates are added.

**Independent Test**: Render each enterprise template page under the Ant theme; assert it is composed only of catalog controls (no bespoke control types), renders populated with seeded content, and matches the structure described in its page-template contract.

**Acceptance Scenarios**:

1. **Given** the showcase, **When** the user navigates to each enterprise template page (workbench, list, detail, form, result, exception), **Then** each renders a populated, cohesive Ant-styled layout composed from catalog controls.
2. **Given** the form template page, **When** invalid input is supplied, **Then** the form surfaces validation feedback and does not present a successful result; **When** valid input is submitted, **Then** it transitions to a success/result state.
3. **Given** the exception template page, **When** it renders, **Then** it presents a recognizable error state (e.g. 404/403/500) with a recovery action, using the Ant `Result`/feedback controls.

---

### User Story 3 - Switch between Ant light and dark with consistent, cohesive styling (Priority: P2)

A user toggles between **Ant light** and **Ant dark** from the top app bar. The whole showcase restyles to the corresponding Ant palette. Control **behavior and accessibility are unchanged** across modes — only the resolved visuals differ. This demonstrates the framework's "one semantic control set, many appearances" rule using the two Ant theme variants shipped in feature 132 (`antLight` / `antDark`).

**Why this priority**: Light/dark parity under a single concrete theme is a defining capability and the most visible interaction, but the showcase is already valuable for browsing Ant-styled controls before the toggle is wired.

**Independent Test**: Render the same page under Ant light and Ant dark; assert the control tree and accessibility contract are identical across variants while the resolved visual styling differs; assert no control branches on theme identity.

**Acceptance Scenarios**:

1. **Given** the showcase on any page in Ant light, **When** the user toggles to Ant dark, **Then** every control and the shell chrome restyle to the Ant dark palette and remain legible.
2. **Given** the same page rendered under Ant light and Ant dark, **When** their behavior and accessibility metadata are compared, **Then** they are identical and only the resolved visuals differ.
3. **Given** any showcased control, **When** it is rendered under either Ant variant, **Then** its appearance is produced purely by theme resolution with no theme-identity branching in the control.

---

### User Story 4 - Produce deterministic, disclosed per-page evidence headlessly (Priority: P3)

A continuous-integration job runs the showcase in headless evidence mode with an explicit seed. For each page it replays a seeded input script and emits a repeatable evidence record: the resulting frame/state outcome and a screenshot of the required surfaces, plus an honest disclosure of what the run does and does **not** prove. Running the same seed twice yields byte-identical evidence. When no display/GL is available, the run degrades cleanly — it skips or falls back to the headless path with a disclosed reason, never hangs, and never reports a fake pass. This reuses the deterministic seeded-evidence harness pattern the Controls Gallery (G1) and Sample Apps (G2) established.

**Why this priority**: Determinism and disclosed evidence turn the showcase from a demo into a *checked* artifact CI can rely on and that can later feed the perf corpus (G4). It depends on the showcase (P1) existing first.

**Independent Test**: Run the headless evidence mode twice with the same seed and diff the outputs for byte-identity; inspect each page's evidence record for a non-empty "not authoritative for" disclosure; run on a host without display/GL and confirm a clean skip/fallback (exit 0, disclosed reason, no hang).

**Acceptance Scenarios**:

1. **Given** a fixed seed, **When** the headless evidence mode runs over all pages twice, **Then** the two evidence sets are byte-identical.
2. **Given** any page's evidence record, **When** it is produced, **Then** it carries a non-empty disclosure of what the run is not authoritative for.
3. **Given** a host without a display or GL, **When** the showcase is asked to produce evidence, **Then** it skips or falls back with a disclosed reason and a success (non-hang) exit, rather than failing or fabricating a result.

---

### User Story 5 - Exercise pointer and keyboard interaction (Priority: P3)

A user (or a seeded input script) clicks buttons, types into text and numeric inputs, toggles switches/segmented controls, changes selections, paginates a list, expands a collapse, opens a drawer/overlay, and submits a form. Interactive controls respond visibly (the input → MVU → repaint path), per a documented pointer-interaction contract describing how each interactive family behaves, so the showcase exercises real input — not just static rendering.

**Why this priority**: Interaction proves the controls are live, but the showcase already demonstrates Ant-styled rendering and theming before interaction wiring is complete; interaction also overlaps with the seeded-evidence work (US4).

**Independent Test**: Drive a seeded pointer/keyboard script against an interactive page and assert the visible state change matches the documented interaction contract; static (display-only) controls are exempt and simply render.

**Acceptance Scenarios**:

1. **Given** an interactive control on a page, **When** a pointer or keyboard input targets it, **Then** its visible state changes according to the documented interaction contract.
2. **Given** a display-only control (e.g. label, separator, badge, empty, skeleton), **When** the page is interacted with, **Then** the control renders correctly and is not expected to respond to input.

---

### Edge Cases

- **Catalog grows or shrinks**: if a control is added to or removed from the catalog, the coverage check fails until the showcase is updated to show it on exactly one page. This keeps the showcase honest about catalog drift — important because the catalog just widened and may widen again.
- **Net-new Ant controls without rich demo data**: every net-new Ant primitive (timeline, steps, collapse, rate, segmented, pagination, drawer, skeleton, empty, result, etc.) is shown with representative seeded content/state so it never appears empty or broken.
- **No display / no GL host**: interactive mode is unavailable; headless evidence mode still runs (or skips cleanly with disclosure). The showcase never makes the CI gate depend on a display.
- **Theme switch mid-session**: switching between Ant light and Ant dark on any page restyles the entire showcase (chrome and all controls) consistently, with no stale styling left behind.
- **Overlay controls** (dialog, drawer, toast, tooltip, context menu, popover, overlay): shown in a state that makes them visible for the page's evidence without trapping navigation.
- **Form validation**: invalid form input on the form-template page is rejected with visible feedback and does not commit or produce a success result.
- **Color policy under Ant**: contrast/intent resolution follows the active color policy; the showcase relies on the shipped Ant theme's resolution and does not hard-code colors.
- **Identifier provenance**: source material referencing archived `FS.Skia.UI.*` identifiers is rebranded to `FS.GG.UI.*` on adoption.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The showcase MUST present a navigable multi-page shell (top app bar with an Ant light/dark mode toggle, a left navigation rail, a scrolling content region, and a status/footer strip) rendered under the **AntDesign theme**.
- **FR-002**: The showcase MUST organize **every control in the current catalog** (the widened set, including all net-new Ant primitives) across pages grouped by control family, with **every catalog control appearing on exactly one page** and reachable by navigation.
- **FR-003**: The showcase MUST provide an automated coverage check that maps each catalog control to its page and fails if any catalog control is unreferenced or referenced more than once.
- **FR-004**: The showcase MUST render every showcased control with representative content, supplying seeded data/state for controls that require items/values so none appears empty.
- **FR-005**: The showcase MUST demonstrate the Ant **enterprise page templates** — at minimum a workbench/dashboard, a list page, a detail page, a form page, a result page, and an exception page — each composed **only from catalog controls** (no bespoke control types) under the Ant theme.
- **FR-006**: The form-template page MUST reject invalid input with visible validation feedback and only present a successful result state on valid submission.
- **FR-007**: The showcase MUST support the two shipped Ant theme variants — **Ant light** and **Ant dark** — applying one cohesive palette across the shell and all controls, switchable at runtime.
- **FR-008**: Switching between Ant light and Ant dark MUST leave each control's behavior and accessibility contract unchanged while changing only the resolved visuals, and MUST NOT require any control to branch on theme identity.
- **FR-009**: The showcase MUST run in two modes: an interactive windowed mode and a headless deterministic evidence mode.
- **FR-010**: The headless evidence mode MUST accept an explicit seed, replay a seeded input script per page, and produce a repeatable per-page evidence record (frame/state outcome plus a screenshot of the required surfaces).
- **FR-011**: Headless evidence runs MUST be deterministic — using injected/seeded inputs and no wall-clock or randomness — so the same seed yields byte-identical evidence across runs.
- **FR-012**: Every evidence record MUST disclose what the run is **not** authoritative for (a non-empty disclosure), consistent with the project's no-overclaim evidence rule.
- **FR-013**: When a display or GL host is unavailable, the showcase MUST degrade and disclose — skip or fall back with a stated reason and a non-failing, non-hanging outcome — never fabricate a pass.
- **FR-014**: Interactive controls MUST respond visibly to pointer and keyboard input per a documented pointer-interaction contract; display-only controls are exempt and simply render.
- **FR-015**: The showcase MUST build and run against the framework's **public package surface only** (including the public `FS.GG.UI.Themes.AntDesign` theme and the public token/resolver surface promoted in Workstream F), with **no privileged internal access and no project references into `src/`**.
- **FR-016**: The showcase MUST NOT change any public product surface, design token value, or rendered output of the shipped packages — it is a pure consumer; the default theme and the Ant theme remain byte-identical to their shipped behavior.
- **FR-017**: Imported identifiers from source material MUST be rebranded from `FS.Skia.UI.*` to `FS.GG.UI.*`, and the adoption MUST be recorded in provenance documentation.
- **FR-018**: The headless evidence mode MUST be the continuous-integration-facing path so the showcase does not make the required gate depend on a display or GL.

### Key Entities

- **Showcase Page**: One page of the showcase — has a title, a control-family grouping or an enterprise-template identity, the set of catalog controls it shows, and the seeded demo state used to populate them.
- **Enterprise Template Page**: A specialization of a showcase page realizing one canonical Ant layout (workbench, list, detail, form, result, exception) as a composition of catalog controls.
- **Showcase Shell**: The application chrome — top app bar (Ant light/dark toggle), left navigation rail, scrolling content region, status/footer strip.
- **Ant Theme Variant**: One of the two shipped Ant variants (`antLight` / `antDark`) that resolves the cohesive palette applied across the showcase.
- **Coverage Map**: The mapping from each catalog control id to the single page that shows it, used by the coverage check.
- **Page Evidence Record**: The deterministic per-page output of headless mode — seeded input script, resulting frame/state outcome, screenshot of required surfaces, and the non-empty "not authoritative for" disclosure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the current catalog's controls are reachable across the showcase's navigable pages, each control on exactly one page; the coverage check confirms zero unreferenced and zero duplicated controls.
- **SC-002**: All six enterprise template pages (workbench, list, detail, form, result, exception) render populated under the Ant theme, each composed solely from catalog controls (zero bespoke control types).
- **SC-003**: For the same page, behavior and accessibility metadata are identical under Ant light and Ant dark while resolved visuals differ; no control branches on theme identity.
- **SC-004**: A seeded headless run of the showcase produces byte-identical evidence across two consecutive runs with the same seed (100% reproducible).
- **SC-005**: Every per-page evidence record carries a non-empty disclosure of what it does not prove; on a host without display/GL the evidence run skips or falls back with a disclosed reason and a non-hanging success outcome.
- **SC-006**: The showcase builds and runs using only the public package surface (no internal access, no `src/` project references), demonstrating the documented Ant-theme consumer path end to end.
- **SC-007**: The showcase introduces zero changes to any public product surface, design-token value, or surface-area baseline (both drift gates remain green).
- **SC-008**: A first-time viewer can reach any showcased control or template page in at most two navigation actions.
- **SC-009**: On the form-template page, invalid input is rejected with visible feedback and never reaches a success result, while valid input does.

## Assumptions

- **Workstream placement**: This feature is **Workstream G3 (Ant restyle + enterprise templates)** from `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md`. Its prerequisites (Workstream F design-system arc and feature 132's concrete `FS.GG.UI.Themes.AntDesign` theme + widened catalog) are landed, so it is no longer dependency-blocked. G4 (wiring sample runs into the perf/CI corpus) remains out of scope.
- **Theme source**: The showcase uses the shipped public `FS.GG.UI.Themes.AntDesign` theme (`antLight`, `antDark`, `resolve`) exactly as published by feature 132; it does not define new themes or alter token values.
- **Control set**: "Every catalog control" means the catalog as it stands in `src/Controls/catalog.yml` at implementation time (widened by feature 132 to include the net-new Ant primitives). The exact count and the per-page grouping are resolved in planning against the live catalog; the coverage check is the source of truth for completeness.
- **Enterprise templates**: The workbench/list/detail/form/result/exception templates derive from the Ant enterprise-pattern docs (`docs/product/ant-design/patterns/`) and the archived FS-Skia-UI adoption analysis; they are realized as compositions of existing catalog controls, not new controls.
- **Source material**: Page structure, the pointer-interaction contract, and per-page evidence requirements adopt and rebrand the archived FS-Skia-UI showcase specs (repo `EHotwagner/FS-Skia-UI`, `docs/testSpecs/Showcase/*`) where available; where a detail is unavailable, the local catalog, the Ant pattern docs, and the §10 plan grouping are authoritative.
- **Placement**: The showcase lives in the existing `samples/` tree as its own `FS.GG.UI.*`-consuming project (Core + App + Tests split, wired to the local NuGet feed), consistent with G1/G2 precedent and the plan's §10.3 recommendation. The exact project layout is a planning decision.
- **Reuse vs. new sample**: This is delivered as a **new Ant-focused showcase sample** (distinct from the G1 Controls Gallery, which stays on Light/Dark) rather than by adding an Ant mode to the existing gallery, so the two remain independently demonstrable and the G1 coverage assertion stays stable. Reusing the G1/G2 deterministic-evidence harness pattern is expected.
- **Modes**: Headless deterministic evidence mode is the CI path; interactive windowed mode is GL-gated and advisory, mirroring G1/G2.
- **Skills**: The `fs-gg-ant-design` advisory skill and the Ant pattern docs are the source of truth for applying Ant patterns during planning/implementation; Ant facts are drawn from the central hub `docs/product/ant-design/reference/ant-llms-sources.md`, not from memory or raw `ant.design` URLs.

## Out of Scope

- Wiring showcase runs into the perf/harness corpus and CI advisory tier (Workstream G4).
- Ant Design **Charts** dashboards beyond any chart controls already in the catalog — the dedicated Ant Charts work is its own follow-up (feature 133, `133-ant-design-charts`). Catalog chart controls are shown like any other control; advanced charting demos are not.
- Adding new controls, new themes, or design-specific kits (feature 132 already added the net-new Ant controls; this feature only *consumes* them).
- Any change to the public package surface, design tokens, or rendered output of the shipped packages (the showcase is a consumer, not a contributor to product API).
- Fluent/Material themes or any theme other than the shipped AntDesign light/dark variants.

## Dependencies

- The shipped public **`FS.GG.UI.Themes.AntDesign`** theme (feature 132) and the widened control catalog (`src/Controls/catalog.yml`).
- The public package surface promoted in Workstream F: token taxonomy + central style resolver + color policy, and the controls/layout/input/viewer/Elmish surfaces consumed by a downstream application.
- The deterministic seeded-evidence harness pattern established by G1 (feature 123) and G2 (feature 134).
- The Ant pattern docs (`docs/product/ant-design/patterns/`) and the central Ant reference hub, plus the `fs-gg-ant-design` skill, for realizing the enterprise templates faithfully.
- The project's no-overclaim evidence conventions (non-empty "not authoritative for", degrade-and-disclose) that the per-page evidence records follow.
