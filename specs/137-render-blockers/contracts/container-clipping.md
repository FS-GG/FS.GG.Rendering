# Contract — Container Clipping & Picture-Cache Parity (`FS.GG.UI.Controls`)

Covers FR-001, FR-002, FR-003. The crux of the feature.

## Composition rule (single source)

- `ControlInternals.composeContainerScene (box) (own) (childScenes)` composes a node's own paint with its
  children, clipping the children to the node's box when there is a box and ≥1 child scene; otherwise flat.
- EVERY paint-assembly site uses it: `Control.renderTree` paint, the four `RetainedRender` build/carry sites,
  and the `RetainedRender.assemble` emit walk (the feature-136 miss).

## Clipping contract (FR-001)

- No child's drawn area paints past its container's bounds (right/bottom spill, nav-label bleed eliminated).
- A leaf or a box-less node composes flat — byte-identical to the pre-137 `own @ children`.

## Parity contract (FR-002 / FR-003) — the hard gate

- Full render (`Control.renderTree`) and the incremental retained render produce byte-identical scenes.
- With the picture cache enabled, `cache-on ≡ cache-off` is byte-identical (`flat off.Render = flat on.Render`).
- Picture-cache hit counts and effectiveness are unchanged (cacheable rows are leaves; their fingerprints do
  not change). `hashScene`/`pictureKeyOf`/`PictureReplayCache` are NOT modified.

## Test oracle

- `tests/Controls.Tests/Audit_PictureCache.fs` (the 3-row grid): `flat off = flat on`, hits `=3`, misses `=0`,
  steady-state effectiveness margin preserved.
- New: a container whose child is laid out beyond its bounds renders with the child clipped to the container;
  a full ≡ retained parity check on a clipped tree.
