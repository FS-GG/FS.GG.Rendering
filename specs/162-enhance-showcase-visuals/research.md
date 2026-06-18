# Research: AntShowcase Visual Overhaul

## Decision: Declare `1600x1000` preferred and `1280x800` minimum supported inspection sizes

**Rationale**: The feature clarification selects `1600x1000` as the preferred showcase size and
`1280x800` as the minimum supported inspection size. The current sample uses `1024x768` for
evidence and `1280x800` for interactive launch; the audit shows dense pages are unreadable at the
smaller evidence size. A larger preferred size makes catalog and template inspection realistic
while preserving a documented lower bound.

**Alternatives considered**:

- Keep `1024x768`: rejected because current screenshots already show severe compression and shell
  collisions.
- Use only `1600x1000`: rejected because the spec requires a minimum supported size and resizing
  must not become an undocumented workaround.
- Use responsive arbitrary-size guarantees: rejected for this feature because bounded accepted
  sizes make evidence deterministic and reviewable.

## Decision: Add a separate visual-readiness evidence path

**Rationale**: Existing AntShowcase evidence proves deterministic state and sometimes an offscreen
non-blank PNG, but it explicitly does not prove visual layout quality. A dedicated
`visual-readiness` command can capture the full page/theme matrix, write contact sheets, run
automated screenshot completeness checks, and require reviewer defect classification without
weakening the existing deterministic evidence contract.

**Alternatives considered**:

- Reuse the existing `evidence` command unchanged: rejected because it would blur deterministic
  proof with visual-readiness proof.
- Treat non-blank screenshot evidence as visual readiness: rejected because it cannot catch shell
  overlap, section overpaint, clipped labels, or theme-surface defects.
- Make visual readiness a test-only helper: rejected because maintainers need durable artifacts
  under the feature readiness tree.

## Decision: Replace nested-stack shell composition with explicit bounded regions

**Rationale**: The current shell composes top bar, nav rail, content, feedback, and status as nested
vertical/horizontal stacks. That structure leaves region ownership implicit and allows labels,
content, and feedback to draw into each other. Explicit shell regions with stable dimensions and
content clipping/scrolling give tests and reviewers a concrete contract: top bar, nav, content,
feedback, and status never overlap at accepted sizes.

**Alternatives considered**:

- Add padding to the existing stacks: rejected because it does not create a durable containment
  contract for long labels, footers, and dense content.
- Remove the feedback area: rejected because feedback capture remains valuable and the spec keeps
  the feedback/status experience in scope.
- Split every page into a separate window: rejected because the showcase must stay navigable as one
  sample.

## Decision: Use page visual profiles for dense and large controls

**Rationale**: Compact controls can be shown in tighter sections, but charts, graphs, calendars,
tables, data collections, media, drawers, overlays, and multi-item selectors need larger bounded
regions or dedicated rows. Page visual profiles let each page declare density, section grid, large
demonstration regions, transient-surface policy, and minimum-size behavior without hardcoding one
layout for every control family.

**Alternatives considered**:

- Give every section the same fixed size: rejected because it wastes space on compact controls and
  still under-sizes charts and collections.
- Split large controls into many extra pages: rejected because the feature keeps the current page
  set and live catalog coverage expectations.
- Hand-place every control with unrelated coordinates: rejected because page profiles provide
  enough control while keeping the sample maintainable.

## Decision: Preserve Ant as a design language over one semantic control set

**Rationale**: Repo guidance and the Ant source-of-truth hub state that FS.GG adopts Ant as a
design language only. Ant light and Ant dark are theme/style resolutions over the same semantic
control set. The visual overhaul must improve composition, tokens, resolver usage, and page
hierarchy without creating `Ant*` behavior forks or taking a React/DOM dependency.

**Alternatives considered**:

- Create Ant-specific control wrappers for showcase fixes: rejected because it would violate the
  one semantic control set rule and hide defects in the actual catalog controls.
- Import upstream Ant implementation mechanics: rejected because the repository explicitly does not
  adopt React components, `classNames`, CSS, or DOM structure.
- Use separate page logic for light and dark themes: rejected because theme switching must preserve
  page and state while changing only resolved visuals.

## Decision: Automate screenshot completeness, keep defect classification reviewer-recorded

**Rationale**: The spec requires automated screenshot completeness but reviewer-recorded visual
defect classification. Automated completeness can verify page/theme/size coverage, dimensions,
file decodability, non-zero dimensions, and capture availability. Human review remains responsible
for visual defect classes such as overlap, overpaint, clipped labels, and unreadable content.
Readiness is accepted only when both sides are present and no critical defect is recorded.

**Alternatives considered**:

- Fully automate visual defect detection: rejected because layout aesthetics and overlap
  classification are not reliable enough for this feature without a large image-analysis system.
- Use reviewer notes without automated completeness: rejected because missing or degraded
  screenshots could be overlooked.
- Allow unclassified screenshots to pass: rejected because the spec explicitly requires
  reviewer-recorded classification.

## Decision: Fail closed when screenshot capture is unavailable

**Rationale**: Visual readiness requires real screenshots. When offscreen or live screenshot
capture is unavailable, the command should still write an environment-limited report with page
counts, intended theme/size matrix, and the reason capture was unavailable, but it must not claim
visual readiness.

**Alternatives considered**:

- Fall back to deterministic scene evidence and mark ready: rejected because deterministic scene
  output does not prove visual layout in screenshots.
- Fail before writing artifacts: rejected because reviewers need to know why evidence is
  unavailable.
- Leave stale screenshots in place: rejected because stale artifacts would make readiness
  misleading.

## Decision: Fix sample composition first and document lower-level limitations explicitly

**Rationale**: Most audited defects are composition and evidence-contract problems in
AntShowcase. The implementation should first fix shell bounds, page layout, feedback/status
placement, transient-surface presentation, and template hierarchy in the sample. If a lower-level
control, layout, viewer, or theme limitation prevents a correct sample fix, that limitation must be
documented with an owner, affected pages, readiness impact, and bounded follow-up before the
affected claim can be accepted.

**Alternatives considered**:

- Start with broad lower-level refactors: rejected because they increase blast radius before the
  sample proves a need.
- Ignore lower-level defects in readiness: rejected because the spec forbids marking affected pages
  visually ready.
- Remove affected controls from the showcase: rejected because catalog coverage must remain
  complete.
