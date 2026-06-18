# Contract: Defect Classification

## Purpose

Reviewer defect classification records visual quality decisions that automated completeness checks
cannot make reliably. It is required before visual readiness can be accepted.

## Required Defect Classes

- `shell-overlap`: top bar, navigation, content, feedback, or status regions overlap.
- `navigation-label-spill`: navigation text escapes the rail or covers content.
- `top-bar-displacement`: theme control or title appears in page content or loses its region.
- `content-footer-collision`: page content draws into feedback or status space.
- `unplanned-background-exposure`: large unintended black or unpainted area appears.
- `section-overpaint`: one demonstration section paints across another section.
- `clipped-primary-label`: section label or primary control label is unreadably clipped.
- `unreadable-primary-content`: primary control content cannot be inspected.
- `transient-surface-overprint`: menu, picker, popover, drawer, or similar surface hides unrelated
  content accidentally.
- `template-hierarchy-unclear`: enterprise template purpose, primary action, or main content is not
  identifiable.
- `lower-level-limitation`: defect appears to belong to a lower-level control, layout, theme, or
  viewer limitation rather than sample composition.

## Severity Values

- `none`: no defect for the screenshot.
- `minor`: visible issue but not readiness-blocking.
- `major`: significant issue requiring follow-up; may block page readiness depending on scope.
- `critical`: readiness-blocking.

## Reviewer Record Fields

- Evidence run id.
- Reviewer.
- Review date.
- Page id.
- Canonical theme id.
- Size.
- Defect class.
- Severity.
- Readiness-blocking flag.
- Notes.
- Lower-level owner/follow-up when applicable.

## Acceptance Rules

- Every required preferred-size screenshot must be covered by a defect row or an explicit all-clear
  summary that names the reviewed matrix.
- Any `critical` defect blocks accepted visual readiness.
- Any `lower-level-limitation` defect blocks the affected readiness claim until a bounded follow-up
  and owner are documented.
- Missing reviewer classification blocks accepted visual readiness even when screenshots are
  complete.
- Automated completeness status cannot override reviewer-recorded critical defects.
