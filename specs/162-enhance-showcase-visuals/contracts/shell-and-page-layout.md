# Contract: Shell and Page Layout

## Shell Regions

The AntShowcase shell owns five persistent regions:

- Top bar.
- Navigation rail.
- Content region.
- Feedback region.
- Status region.

Each region has stable bounds at `1600x1000` and `1280x800`. The implementation may use layout
controls, typed widgets, or local composition helpers, but the resulting rendered bounds must keep
these regions visually separate.

## Region Rules

- Top bar content stays inside the top bar.
- Navigation items stay inside the navigation rail.
- Page content stays inside the content region.
- Feedback content stays inside the feedback region or collapses according to its policy.
- Status text stays inside the status region.
- Region backgrounds/surfaces are intentionally painted in both Ant light and Ant dark.
- Region content uses clipping, scrolling, wrapping, pagination, or truncation instead of drawing
  into neighboring regions.

## Navigation Rules

- Every page in `PageRegistry.all` has one visible navigation item.
- Long labels preserve page identification through truncation, wrapping, or short labels.
- Current-page state remains visible without spilling into content.
- Navigation does not reset theme, page state, or saved feedback.

## Content Rules

- Catalog pages are organized into labeled demonstration sections.
- Dense pages use a bounded scroll/pagination layout.
- Large controls receive dedicated demonstration regions.
- Baseline screenshots keep accidental transient surfaces closed unless the page intentionally
  demonstrates the transient surface in a controlled region.
- Section overpaint, clipped primary labels, unreadable primary content, and content-footer
  collision are critical defects.

## Template Rules

- Workbench, list, detail, form, result, and exception templates show clear page purpose and
  primary action.
- Templates use catalog controls and realistic but deterministic content.
- The form template aligns fields, validation, terms control, and submit action.
- Result and exception templates balance outcome text, recovery action, and supporting details
  within the content region.

## Test Expectations

Focused tests must assert:

- Shell region bounds are disjoint at preferred size.
- Shell region bounds are disjoint at minimum size for representative pages.
- Navigation labels do not exceed navigation bounds.
- Content bounds do not overlap feedback or status bounds.
- Light and dark modes produce complete painted shell/content surfaces.
- Every page renders non-empty content at the preferred size.
