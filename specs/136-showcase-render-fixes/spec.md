# Feature Specification: Showcase Rendering Defect Fixes

**Feature Branch**: `136-showcase-render-fixes`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "create specs to fix those problems, by improvements at the framework level if possible, by the sample if necessary."

## Context

A visual audit of the Ant Design Controls Showcase (feature 135) captured a screenshot of every one of
the 19 pages (full application shell, 1024×768, antLight) and found pervasive rendering defects that make
the showcase illegible and misrepresent the controls. The defects fall into four user-visible classes:

1. **Wrong/garbled text** — certain characters render as a different glyph (e.g. `@` shows as `7`, so
   `ada@example.com` reads `ada7example.com`; the em-dash `—`, `#`, `▸`, and `·` are likewise substituted),
   and several control labels are truncated mid-word (`Stable`→`STABL`, `Upload`→`UPLOA`, `Refresh`→`REFRES`).
2. **Overlapping content** — the app bar overlaps the page content; nav-rail labels spill across the
   content; stacked controls bleed into one another; and the feedback/status chrome is clipped.
3. **Mis-structured composite controls** — menu items overprint each other, the data-grid stacks its
   columns vertically instead of as a table, combo-box/auto-complete/date-picker dropdowns paint in-flow
   over their neighbours, descriptions overlap adjacent controls, charts overrun their boxes, and the
   QR-code renders empty.
4. **Unbounded content** — controls and the nav rail spill past the right edge of their containing region
   and past the window; tall pages do not scroll cleanly.

The reporter's directive sets the remediation policy: **fix at the framework level wherever the defect is
caused by a shared control or the renderer (so every consuming application benefits), and fix in the sample
only where the defect is purely a consequence of how the sample composes controls.**

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every character and label renders legibly (Priority: P1)

A person evaluating the controls reads the text on every page — control names, seeded values, button
labels, form fields. Every character must be the character that was authored, and no label or value may be
cut off mid-word.

**Why this priority**: Wrong text is the most damaging defect — it makes the showcase actively misleading
(an email field that shows `7` instead of `@`) and, because the substitution and truncation come from
shared controls/renderer, the same corruption reaches any application built on the framework. Fixing it is
the highest-value, framework-level outcome.

**Independent Test**: Render every page (both themes) and confirm that each authored string appears in full
with no substituted glyphs and no mid-word truncation; specifically verify the previously-broken cases
(`@`, `—`, `#`, `▸`, `·`, and the `Stable`/`Upload`/`Refresh`/numeric-input labels).

**Acceptance Scenarios**:

1. **Given** a form field seeded with `ada@example.com`, **When** the page renders, **Then** the field
   displays `ada@example.com` exactly — the `@` is not replaced by any other glyph.
2. **Given** a page title containing an em-dash (e.g. "Charts I — Statistical") or a build row containing
   `#` and `—`, **When** it renders, **Then** the punctuation displays as authored, not as a substitute.
3. **Given** a `tag` seeded "Stable", an `upload` labelled "Upload", and a `button` labelled "Refresh",
   **When** they render, **Then** each shows its full label, not a truncated form.
4. **Given** any catalog control whose content is wider than its current box, **When** it renders, **Then**
   the control accommodates the text (grows, wraps, or shows an explicit ellipsis affordance) rather than
   silently cutting characters.

---

### User Story 2 - Controls and chrome never overlap (Priority: P1)

A person scans a page top to bottom. Every control, every section, and every piece of application chrome
(app bar, navigation, content, feedback, status) occupies its own space; nothing is painted on top of
anything else.

**Why this priority**: Overlapping content is the most visible defect on every page and makes individual
controls impossible to read or assess. It blocks the showcase's core purpose. Much of it is sample-level
composition, but some (menu/dropdown overprint) is control-level.

**Independent Test**: Render every page and confirm that no two sibling controls', and no two layout
regions', drawn areas overlap; the app bar, nav rail, content region, feedback section, and status strip
are visually disjoint.

**Acceptance Scenarios**:

1. **Given** the application shell, **When** any page renders, **Then** the app bar, nav rail, scrolling
   content, feedback section, and status strip occupy non-overlapping regions.
2. **Given** a page that stacks several controls vertically, **When** it renders, **Then** each control's
   drawn area is fully below the previous one with no vertical bleed into the next section's label.
3. **Given** a control that opens a transient surface (menu, combo-box list, date-picker calendar,
   auto-complete suggestions), **When** that surface is shown, **Then** its items are laid out distinctly
   and do not overprint each other or the controls beneath.

---

### User Story 3 - Composite controls show their expected structure (Priority: P2)

A person inspects the richer controls — tables, menus, dropdowns, descriptions lists, charts, QR codes —
to judge whether the control set is production-credible. Each must render in its canonical structure.

**Why this priority**: These controls are the differentiated, "is this real?" part of the catalog. They
are control-level (framework) concerns. Lower than P1 because they affect specific controls rather than
every page's basic legibility.

**Independent Test**: Render the pages hosting each composite control and confirm its internal structure:
a data-grid shows columns side-by-side as a table; a menu lays items out without overprint; a descriptions
list aligns label/value pairs; a chart fits inside its box; a QR-code shows a non-empty module grid.

**Acceptance Scenarios**:

1. **Given** a `data-grid` with named columns and rows, **When** it renders, **Then** the columns appear
   side-by-side as a table with aligned cells, not stacked one per row.
2. **Given** a `menu`/`context-menu` with several items, **When** it renders, **Then** the items are laid
   out in distinct positions, none overprinting another.
3. **Given** a `descriptions` term list and a `qr-code`, **When** they render, **Then** the label/value
   pairs are aligned and readable and the QR-code shows a populated module grid (not blank).
4. **Given** any chart control with seeded data, **When** it renders, **Then** the chart body stays within
   its allotted box and does not overrun the adjacent control's title.

---

### User Story 4 - Pages stay within the window and scroll when long (Priority: P3)

A person opens a content-dense page in a normally-sized window. Content stays inside the content region;
when a page is taller than the viewport, it scrolls rather than spilling past the window edges.

**Why this priority**: Bounded/scrollable content is important for usability but secondary to fixing the
text and overlap defects that block reading any single control. Largely sample-level (the content host and
nav rail need a width/height budget).

**Independent Test**: Render each page at the default window size and confirm no control is painted outside
the content region's right edge or below the window; a page with more controls than fit vertically exposes
a scroll affordance and all controls remain reachable.

**Acceptance Scenarios**:

1. **Given** the navigation rail, **When** any page renders, **Then** every rail label stays within the
   rail's width and does not cross into the content region.
2. **Given** a page whose controls exceed the content region's height, **When** it renders, **Then** the
   content scrolls and no control is painted outside the content region's bounds.

---

### Edge Cases

- **Longest labels**: the longest catalog control name / seeded value must still render fully (grow, wrap,
  or ellipsize) without truncation or overlap.
- **Dark mode parity**: every fix must hold identically under antDark — the defects and their fixes must
  not be theme-dependent (consistent with feature 135's theme-invariance guarantee).
- **Non-ASCII punctuation**: if framework glyph coverage cannot include a given character, the chosen
  fallback MUST be a legible, intentional substitute (e.g. an ASCII equivalent), never an arbitrary wrong
  glyph.
- **Densest pages**: the pages with the most controls (navigation-menus, feedback-status, cards-stats-media,
  charts) must satisfy every requirement, not just sparse pages.
- **Regression boundary**: fixes to shared controls/renderer must not change the rendered output of the
  existing G1 Controls Gallery sample in a way that breaks its golden evidence, unless that change is an
  intended, re-baselined correctness fix.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every authored character MUST render as that character on every page in both themes; no
  character may be displayed as a different glyph. Where the active font lacks a glyph, the system MUST use
  a deliberate, legible fallback rather than an arbitrary substitute, and MUST disclose that a fallback was
  applied.
- **FR-002**: Every control label and value MUST render in full; a control whose content exceeds its box
  MUST grow, wrap, or present an explicit truncation affordance (e.g. ellipsis) — never silently drop
  characters.
- **FR-003**: On every page, the application chrome regions (app bar, navigation rail, content area,
  feedback section, status strip) MUST occupy mutually non-overlapping areas.
- **FR-004**: Sibling controls composed in a sequence MUST occupy non-overlapping areas; no control may be
  painted over an adjacent control or section label.
- **FR-005**: Transient/overlay surfaces (menu items, combo-box and auto-complete lists, date-picker
  calendars) MUST render their items in distinct, non-overprinting positions.
- **FR-006**: The `data-grid` MUST render its columns side-by-side as a table with aligned header and body
  cells.
- **FR-007**: The `descriptions` control MUST render aligned label/value pairs without overlapping
  neighbouring controls, and the `qr-code` control MUST render a non-empty module grid for a non-empty
  payload.
- **FR-008**: Chart and graph controls MUST render their bodies within their allotted box without
  overrunning adjacent controls.
- **FR-009**: Navigation rail labels MUST stay within the rail's width; page content MUST stay within the
  content region's width and MUST scroll (not spill) when taller than the viewport.
- **FR-010**: The status strip and feedback controls MUST render their full text within the visible window.
- **FR-011 (remediation policy)**: Each defect MUST be remediated at the layer that owns its cause — a
  shared control or the renderer when the defect reproduces independently of how it is composed, and the
  sample when the defect is solely a consequence of the sample's composition. A framework-level fix MUST be
  preferred whenever the defect is framework-caused, so all consuming applications benefit.
- **FR-012**: Any framework-level change MUST NOT regress the existing Controls Gallery (G1) or Sample Apps
  (G2) samples' rendered output except where the change is an intended correctness fix, in which case the
  affected golden/baseline evidence MUST be re-established and disclosed.
- **FR-013**: The corrected rendering MUST be demonstrated by re-capturing the per-page screenshot evidence
  for all 19 showcase pages and confirming the absence of every audited defect.

### Key Entities

- **Rendered page**: the full application shell for one showcase page — its control tree, the laid-out
  bounds of every control, and the regions (app bar, nav, content, feedback, status). The unit a defect is
  observed and verified against.
- **Defect class**: one of {wrong-glyph, truncated-text, region-overlap, control-overlap, overlay-overprint,
  composite-structure, unbounded-content}. Each audited finding maps to one class and to a remediation
  layer (framework or sample).
- **Remediation layer**: framework (shared control or renderer) or sample (composition) — the layer where a
  given defect class is fixed, per FR-011.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Across all 19 pages in both antLight and antDark, 100% of authored characters render as the
  authored character (zero wrong-glyph substitutions), verified against the specific audited cases
  (`@`, `—`, `#`, `▸`, `·`).
- **SC-002**: Across all 19 pages, zero control labels or values are truncated mid-word; the previously
  broken `Stable`, `Upload`, `Refresh`, and numeric-input cases render in full.
- **SC-003**: Across all 19 pages, no two layout regions and no two sibling controls have overlapping drawn
  bounds (zero overlap), and no control is painted outside its content region.
- **SC-004**: Each of the composite controls (`data-grid`, `menu`/`context-menu`, `combo-box`,
  `auto-complete`, `date-picker`, `descriptions`, `qr-code`, and the chart family) renders in its canonical
  structure on its page, confirmed by re-captured screenshots.
- **SC-005**: A re-run of the per-page screenshot capture for all 19 pages shows none of the seven audited
  defect classes; a reviewer comparing before/after confirms every flagged issue is resolved.
- **SC-006**: At least the defects whose cause is a shared control or the renderer are fixed at the
  framework level (not worked around in the sample); the count of defects fixed in the sample is limited to
  those that are purely composition-specific, and the split is recorded.
- **SC-007**: The existing G1 and G2 samples either render byte-identically to before or, where a shared
  fix intentionally changes their output, their baselines are re-established and the change is disclosed.

## Assumptions

- The audit findings captured from the 19-page screenshot evidence (feature 135, seed 1, 1024×768, antLight)
  are the authoritative defect list; antDark exhibits the same defects by theme-invariance.
- "Framework if possible, sample if necessary" (the user directive) is the binding remediation policy: the
  default is a framework fix for any framework-caused defect; sample-only fixes are reserved for
  composition-caused defects. Where a framework fix proves infeasible or disproportionate (to be determined
  in planning/research), the fallback is a disclosed sample-level fix.
- Glyph coverage: the preferred fix for wrong-glyph defects is expanding the framework's text rendering to
  cover the affected characters (notably `@`); if that is infeasible, the sample may substitute legible
  ASCII equivalents for decorative punctuation (`—`, `▸`, `·`, `•`), but `@` in an email value must render
  correctly.
- The default window size for verification is 1024×768 (the evidence capture size); fixes should also hold
  at the interactive window size (1280×800).
- This feature is a correctness/quality fix; it does not add new controls, themes, or showcase pages.
- The per-page screenshot evidence harness from feature 135 is reused as the verification vehicle; no new
  evidence mechanism is required.

## Out of Scope

- New controls, themes, charts, or showcase pages.
- Pixel-level fidelity matching against upstream antd (explicitly disclaimed by the showcase's evidence
  records).
- Right-to-left layout, localization, or fonts beyond covering the audited characters.
- Live pointer hit-testing behaviour beyond what the existing seeded scripts exercise.
