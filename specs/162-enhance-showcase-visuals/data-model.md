# Data Model: AntShowcase Visual Overhaul

## Accepted Size

**Purpose**: Declared viewport size used for inspection and readiness evidence.

**Fields**:

- Width.
- Height.
- Role: `preferred` or `minimum`.
- Evidence scope.

**Validation rules**:

- Preferred size is exactly `1600x1000`.
- Minimum supported inspection size is exactly `1280x800`.
- Preferred readiness captures every page in canonical `antLight` and `antDark`.
- Minimum-size validation captures representative dense, large-control, and enterprise template
  pages in both themes.

## Theme Variant

**Purpose**: Ant visual mode applied to the same semantic control tree.

**Values**:

- `antLight`
- `antDark`

**CLI aliases**:

- `light` resolves to `antLight`
- `dark` resolves to `antDark`

**Validation rules**:

- Theme switching preserves current page and page state.
- Theme changes must repaint shell and content surfaces completely.
- No page may branch into a different control set solely because of theme identity.
- Evidence summaries and reviewer records store canonical theme ids, not aliases.

## Showcase Shell

**Purpose**: Persistent chrome containing all regions that frame the active page.

**Fields**:

- Top bar region.
- Navigation rail region.
- Content region.
- Feedback region.
- Status region.
- Accepted size.
- Canonical theme variant.

**Validation rules**:

- Regions are visually separate at every accepted size.
- Page content never draws under the top bar, navigation rail, feedback region, or status region.
- Feedback and status remain available without dominating ordinary page inspection.
- Light and dark modes paint complete intentional shell surfaces.

## Shell Region

**Purpose**: Bounded rectangle or layout allocation owned by one shell function.

**Fields**:

- Region id.
- Bounds.
- Minimum and preferred dimensions.
- Overflow policy.
- Background/surface role.
- Child content.

**Validation rules**:

- Region bounds do not overlap other shell regions.
- Overflow is handled by clipping, scrolling, wrapping, or pagination.
- Region content must not escape into neighboring regions.
- Status and feedback content must fit or collapse according to their region policy.

## Navigation Item

**Purpose**: One reachable page entry in the navigation rail.

**Fields**:

- Page id.
- Display title.
- Kind: catalog or template.
- Current-page state.
- Short label or truncation label.

**Validation rules**:

- Every page has one navigation item.
- Long labels remain inside the navigation rail.
- The current page state remains visible after truncation or wrapping.
- Navigation does not alter page-specific interactive state except current page.

## Showcase Page

**Purpose**: Navigable catalog or enterprise template page.

**Fields**:

- Page id.
- Title.
- Kind: catalog or template.
- Control ids.
- Visual profile.
- View function.

**Validation rules**:

- Catalog pages have non-empty control ids.
- Template pages have empty control ids and are exempt from the catalog bijection.
- Every page renders a non-empty tree at the preferred size and representative minimum-size pass.
- Page content is contained inside the content region.

## Page Visual Profile

**Purpose**: Per-page layout rules that make a page inspectable without changing the catalog map.

**Fields**:

- Density: compact, standard, dense, or large-visual.
- Section layout.
- Large demonstration regions.
- Transient-surface policy.
- Minimum-size behavior.
- Notes for reviewer inspection.

**Validation rules**:

- Dense pages use scrolling, pagination, or responsive section layout rather than overlap.
- Large-visual pages reserve enough space for charts, graphs, calendars, tables, media, overlays,
  and drawers.
- Transient surfaces are either intentionally shown in a controlled region or closed in baseline
  screenshots.

## Control Demonstration Section

**Purpose**: Labeled page area that demonstrates one catalog control or a small related group.

**Fields**:

- Section id.
- Label.
- Control ids shown.
- Bounds or layout allocation.
- Demonstration content.
- Visual profile flags.

**Validation rules**:

- Label and primary control content are readable.
- Section content stays inside the section allocation.
- A control appears on exactly one catalog page according to the coverage map.
- Section overpaint is a critical visual defect.

## Large Demonstration Region

**Purpose**: Dedicated region for controls whose primary visual output cannot fit in a compact row.

**Fields**:

- Region id.
- Control ids.
- Preferred dimensions.
- Minimum dimensions.
- Overflow policy.
- Theme surface role.

**Validation rules**:

- Large controls do not cross neighboring section boundaries.
- Charts and graphs show primary shapes, labels, and values.
- Data collections show headers and at least representative rows/items.
- Overlays and drawers are intentionally layered and bounded.

## Enterprise Template Page

**Purpose**: Composed Ant-styled application workflow page.

**Fields**:

- Template id.
- Workflow type: workbench, list, detail, form, result, or exception.
- Primary action.
- Main content region.
- Supporting content regions.
- Validation or result state when applicable.

**Validation rules**:

- A first-time viewer can identify page purpose and primary action from the screenshot.
- Templates are composed from catalog controls unless a lower-level limitation is documented.
- Form fields, validation, terms control, and submit action align without overlap.
- Result and exception pages balance outcome message, recovery action, and supporting details.

## Visual Evidence Run

**Purpose**: Durable proof package for visual-readiness review.

**Fields**:

- Run id.
- Command and options.
- Page count.
- Theme set.
- Accepted size.
- Screenshot artifacts.
- Contact sheets.
- Screenshot completeness result.
- Reviewer defect classification.
- Capture availability.
- Readiness status.
- Limitations.

**Validation rules**:

- Preferred run includes one screenshot per page per required theme.
- Required screenshots must be present, decodable, and match the declared size.
- Missing, degraded, stale, or unavailable screenshots prevent accepted visual readiness.
- Reviewer defect classification must be present before readiness can be accepted.

## Screenshot Artifact

**Purpose**: One captured page/theme/size image.

**Fields**:

- Page id.
- Canonical theme variant.
- Width.
- Height.
- Path.
- Capture status.
- Completeness status.
- File hash.
- Capture source.

**Validation rules**:

- Path is inside the requested evidence directory.
- Image dimensions equal the declared size.
- File is decodable and non-empty.
- A stale or missing file is a failed completeness result.

## Screenshot Completeness Result

**Purpose**: Automated check for required screenshot coverage and basic image validity.

**Fields**:

- Required page count.
- Required theme count.
- Required screenshot count.
- Present screenshot count.
- Missing screenshots.
- Degraded screenshots.
- Dimension mismatches.
- Capture-unavailable reason.
- Pass/fail status.

**Validation rules**:

- Preferred completeness passes only when all 38 screenshots are present and valid for 19 pages x
  2 themes.
- A capture-unavailable reason records environment-limited evidence and readiness status is not
  accepted.
- Completeness does not classify visual defects by itself.

## Visual Defect Classification

**Purpose**: Reviewer-recorded visual quality decision for the evidence run.

**Fields**:

- Reviewer.
- Review date.
- Evidence run id.
- Page id.
- Canonical theme variant.
- Size.
- Defect class.
- Severity: none, minor, major, or critical.
- Notes.
- Readiness-blocking flag.

**Defect classes**:

- `shell-overlap`
- `navigation-label-spill`
- `top-bar-displacement`
- `content-footer-collision`
- `unplanned-background-exposure`
- `section-overpaint`
- `clipped-primary-label`
- `unreadable-primary-content`
- `transient-surface-overprint`
- `template-hierarchy-unclear`
- `lower-level-limitation`

**Validation rules**:

- Readiness requires a classification record for every required screenshot or an explicit
  all-clear summary that names the reviewed page/theme/size matrix.
- Any critical defect blocks accepted visual readiness.
- Lower-level limitations must link affected pages, owner, readiness impact, and bounded
  follow-up.

## Readiness Summary

**Purpose**: Reviewer entry point for Feature 162 closeout.

**Fields**:

- Final visual-readiness status.
- Page count.
- Catalog control count.
- Theme count.
- Preferred and minimum sizes.
- Screenshot completeness status.
- Reviewer defect status.
- Coverage status.
- Package-feed validation status.
- Regression validation status.
- Full-validation status.
- Limitations and follow-ups.
- Artifact links.

**Validation rules**:

- Accepted status requires complete preferred-size evidence, no critical defects, clean catalog
  coverage, passing package-only sample validation, and current regression/full-validation notes.
- Environment-limited status is used when screenshots cannot be captured.
- Blocked status is used when visual defects or lower-level limitations remain.

## Workflow State Transitions

```text
specified -> shell-regions-designed -> page-profiles-designed -> templates-composed
          -> visual-readiness-command-added -> screenshots-captured
          -> completeness-checked -> contact-sheets-published
          -> reviewer-defects-recorded -> readiness-summarized
          -> accepted | blocked | environment-limited
```

Invalid transitions preserve diagnostics and do not set visual readiness to accepted.
