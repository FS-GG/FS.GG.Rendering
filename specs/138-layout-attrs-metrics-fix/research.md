# Research: Layout Attributes and Metrics Green

## Decision: Implement layout authoring at the Controls boundary, not in the Layout engine

**Rationale**: `FS.GG.UI.Layout.LayoutIntent` already contains `AlignItems`, `AlignSelf`,
`JustifyContent`, `Padding`, `Margin`, `Gap`, `Size`, `MinSize`, `MaxSize`, `FlexGrow`,
`FlexShrink`, and `FlexBasis`. `Layout.fs` already maps those fields to Yoga. The missing link is
`Control.toLayout`, which currently reads only width, height, and orientation, then injects hardcoded
padding and gap values. Keeping the change at the Controls boundary minimizes risk and preserves the
Layout package dependency boundary.

**Alternatives considered**:
- Add new Layout fields or Yoga wiring. Rejected because the existing lower-level model already has the
  required fields.
- Start the larger intrinsic-size protocol from the architecture report. Rejected as out of P0 scope.
- Replace Yoga or add a new layout engine. Rejected by the feature scope and constitution constraints.

## Decision: Add canonical `Attr` builders and keep compatibility aliases where they already exist

**Rationale**: `Attr.width`, `Attr.height`, `Attr.padding`, and `Attr.margin` are already the public
builder pattern. This feature should add canonical builders for `gap`, `alignItems`, `alignSelf`,
`justifyContent`, `flexGrow`, `flexShrink`, `flexBasis`, `minWidth`, `minHeight`, `maxWidth`, and
`maxHeight`. Existing typed surfaces that lower `Spacing` to the `"spacing"` attribute should either lower
through `Attr.gap` or be treated as a compatibility alias for gap, so already-authored typed stack/wrap
spacing starts driving layout instead of staying a no-op.

Alignment builders should use the existing public `FS.GG.UI.Layout.LayoutAlign` vocabulary and lower to a
stable attribute value that `Control.toLayout` can parse without adding duplicate Controls enums. Padding,
margin, and gap remain uniform values for P0; edge-specific builders are deferred.

**Alternatives considered**:
- Add duplicate Controls-specific alignment enums. Rejected because `LayoutAlign` is already public and
  Controls already references Layout.
- Use only free-form `Attr.create` strings. Rejected because the feature is about reliable public authoring,
  not an escape hatch.
- Add edge-specific padding/margin/gap builders now. Rejected by the spec's explicit P0 boundary.

## Decision: Preserve current no-authored-value geometry with Controls compatibility defaults

**Rationale**: The current `Control.toLayout` behavior injects implicit padding and gap. FR-003 requires
screens that do not author the newly supported values to keep their existing bounds. Therefore omitted
values must keep the current Controls compatibility baseline, while explicit authored values, including
zero, override that baseline. This distinguishes "omitted" from "authored zero" and satisfies both
backward compatibility and the explicit-zero edge cases.

**Alternatives considered**:
- Switch omitted padding/gap to `LayoutDefaults.layoutIntent` zero values. Rejected because it would move
  existing screens.
- Treat explicit zero as omitted. Rejected because FR-004 and the edge cases require explicit zero to be
  honored.

## Decision: Expand the layout dirty-set name guard in lock-step with `toLayout`

**Rationale**: Incremental layout correctness depends on `RetainedRender.layoutDirtySet` recognizing every
attribute name that `Control.toLayout` reads. The repository already has `ControlInternals.layoutAffectingAttrNames`
and `Feature101LayoutDriftGuardTests` to prevent drift. This feature should expand that set and the probe
corpus to include every newly read canonical name plus any compatibility alias such as `"spacing"`.
Category-based `AttrCategory.Layout` invalidation remains an independent channel.

**Alternatives considered**:
- Auto-derive the name set from `toLayout`. Rejected because the existing design intentionally keeps a
  hot-path set and gates it behaviorally.
- Dirty layout for every attribute change. Rejected because it would regress style-only/content-only work
  reduction and violate FR-006.

## Decision: Report text-cache hits only for measurements resident before the frame window

**Rationale**: The known defect is that the first cold text-heavy frame can report hits when repeated text
is encountered after the cache was populated earlier in the same frame. The metric contract needs "hit" to
mean prior-frame reuse. A cold frame with no prior resident measurements must report zero hits; a warm
equivalent frame after that must report hits and zero misses. The retained renderer can keep using the same
deterministic cache for correctness, but metric classification must use the frame-start resident key set.

**Alternatives considered**:
- Keep counting same-frame duplicate measurements as hits. Rejected because it violates the cold-frame
  metric contract.
- Disable same-frame cache reuse entirely. Rejected unless required by implementation simplicity, because
  the behavioral requirement is metric truth, not removal of a transparent accelerator.
- Add a new public metric field for same-frame reuse. Rejected as unnecessary for this P0 fix.

## Decision: Keep verification headless and deterministic

**Rationale**: The layout, retained render, and `ControlsElmish.Perf.runScript` paths are deterministic and
headless. The feature can be proven without GL readbacks: bounds comparisons, scene parity, dirty-set counts,
and frame metric tuples are sufficient and less fragile than screenshot evidence for this slice.

**Alternatives considered**:
- Use SkiaViewer/GL smoke tests as primary proof. Rejected because no native window-system behavior changes.
- Rely only on broad `Verify`. Rejected because the feature needs targeted failing tests that localize
  layout authoring and metrics regressions.
