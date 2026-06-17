# Feature 137 — Render Blockers (Clipping, Overlay & Scroll): compatibility / migration note

Feature 137 lands the three rendering fixes feature 136 deferred. It is a **Tier-1** change: it alters the
observable rendered output of shared controls and adds a small amount of public surface. This note records
what changed for consumers and how to adopt it.

## What changed (behavior)

1. **Container clipping (US1, the blocker).** Every container now clips its children to its evaluated box via
   one shared rule, `ControlInternals.composeContainerScene`, routed through **all six** paint-assembly sites
   (the full `Control.renderTree` paint, the four `RetainedRender` build/carry sites, and the
   `RetainedRender.assemble` emit walk). A child laid out beyond its container no longer spills past the
   bounds. The `assemble` emit walk was the feature-136 miss that broke `cache-on ≡ cache-off`; routing it
   through the same rule makes full ≡ retained and cache-on ≡ cache-off hold **by construction**. The
   `Audit_PictureCache` trio is green with clipping enabled.

2. **Deferred overlay pass (US2).** A control built on the existing `Overlay` container (e.g. a date-picker's
   open calendar) is now collected **out** of the in-flow container-clip hierarchy and painted **last** (z-top)
   at true coordinates, so it floats above neighbours and escapes ancestor clips. `Control.hitTest` /
   `nearestAuthored` consult the overlay group first (topmost wins). An **empty** overlay group renders
   byte-identically to the pre-overlay pass — pages without an open transient surface are unchanged.

3. **ScrollViewer viewport (US3).** A `scroll-viewer` is now a real clipping viewport: its content is clipped
   to its box (scrollable, not spilled) and it paints a scroll affordance whose thumb reflects the overflow.

## New public surface (`FS.GG.UI.Controls`)

- `type Control.ScrollViewport = { Viewport: Rect; ContentHeight: float; Offset: float; MaxOffset: float }`
  — read-back geometry of a `scroll-viewer` viewport. `MaxOffset > 0` ⇒ the content overflows (scrollable).
- `val Control.scrollViewport : result -> scrollViewerId -> ScrollViewport option` — derive the viewport
  metrics for a keyed `scroll-viewer` from a render result.
- `val Control.isOverlaySurface : control -> bool` — the entry to the overlay pass: does this control author a
  deferred z-top overlay surface (an `Overlay`)?

The surface-area baseline `tests/surface-baselines/FS.GG.UI.Controls.txt` gains exactly one line
(`FS.GG.UI.Controls.ScrollViewport`); `FS.GG.UI.Layout` is unchanged.

## Migration

No source changes are required to adopt the fixes — they are render-path improvements behind the existing
`renderTree` / `Overlay` / `scroll-viewer` APIs. Two things to be aware of:

- **Rendered output of containers changes** (children are now clipped). If you froze a golden of a control
  whose children previously spilled, re-establish it as an intended change.
- **To float a transient surface**, author its open content as an `Overlay` (the date-picker calendar already
  does); it will paint last and win hit-tests automatically.
