# Contract: Assembly Compatibility

This is an internal compatibility contract. Feature 139 adds no public API, no public scene node, no package
surface, and no external protocol.

## Current-Node Assembly Rule

Given:

- a control node
- the node's evaluated box, if any
- the node's own scene contribution
- ordered child assembly results, each split into in-flow and overlay contributions

The authoritative current-semantics assembly boundary must produce:

- an in-flow scene contribution
- an overlay scene contribution

The rule is:

1. Concatenate child in-flow contributions in authored child order.
2. Compose the node's own scene with the child in-flow contribution using the existing container clipping
   rule:
   - if the node has a box and at least one child scene, child content is clipped to the node box;
   - otherwise composition is flat.
3. Concatenate child overlay contributions in authored child order.
4. If the current node is an overlay node, return empty in-flow contribution and return the composed scene
   followed by child overlay contributions as overlay contribution.
5. If the current node is not an overlay node, return the composed scene as in-flow contribution and return
   child overlay contributions as overlay contribution.

The final root scene is the root in-flow contribution followed by the root overlay contribution.

## Required Callers

The authoritative boundary must be used by all current assembly callers:

- immediate rendering
- retained first-frame build
- retained fresh rebuild for inserted or unmatched nodes
- retained carry rebuild for structurally identical shifted nodes
- retained update rebuild for changed matched nodes
- retained cache/replay emit walk

## Compatibility Obligations

- Overlay-free output remains byte-identical to current in-flow output.
- Overlay output still paints after in-flow output and escapes ancestor container clipping.
- Nested clipping still clips child content to the container box and leaves leaves/box-less nodes flat.
- Cache boundaries remain transparent to rendered output and diagnostics.
- Warm retained reuse must not serve stale assembly when layout, theme, visual state, content, or children
  change.
- Public surface checks must report no intentional public contract changes.
- Golden or pixel checks must report no intentional baseline changes.

## Out-of-Scope Semantics

The boundary must not introduce:

- modifier algebra
- first-class portals or layer hosts
- public scene IR changes
- intrinsic layout protocol changes
- text shaping changes
- compositor or damage-scissor changes
- portable scene serialization changes
