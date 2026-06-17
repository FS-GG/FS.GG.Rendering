# Data Model: Shared Assembly Extraction

This feature has no persisted data model. The entities below are internal planning concepts used to describe
the current rendering assembly contract and verification evidence.

## CurrentNodeAssemblyInput

Represents the information needed to assemble one control node under today's rendering semantics.

**Fields**
- `control`: The current control node being assembled.
- `box`: The evaluated absolute box for the control, when layout produced one.
- `ownScene`: The scene list painted by the control itself, excluding child output.
- `childAssemblies`: Ordered child assembly results, each carrying in-flow and overlay contributions.

**Validation Rules**
- Child order must match authored child order after existing lowering.
- `ownScene` must depend only on the current node, current theme, evaluated box, and current attributes.
- Missing `box` must preserve flat composition behavior.
- Empty `childAssemblies` must preserve leaf composition behavior.

## CurrentNodeAssemblyResult

Represents the assembled output for one control node.

**Fields**
- `inFlowScene`: Scene contributions that remain in the normal parent clipping hierarchy.
- `overlayScene`: Scene contributions deferred to the z-top overlay group.

**Relationships**
- A parent consumes each child's `inFlowScene` as child content to compose and each child's `overlayScene`
  as deferred overlay content.
- A root render result is formed by appending root `inFlowScene` and root `overlayScene` in that order.

**Validation Rules**
- For a non-overlay node, `inFlowScene` is the node's own scene composed with child in-flow scenes through
  current container clipping rules; `overlayScene` is the ordered concatenation of child overlay scenes.
- For an overlay node, `inFlowScene` is empty; `overlayScene` is the node's composed own-and-child in-flow
  scene followed by child overlay scenes.
- Overlay-free trees must have an empty root `overlayScene`.

## AssemblyOwner

Represents the single internal owner of current assembly semantics.

**Fields**
- `name`: Human-readable name for the internal assembly boundary.
- `callers`: The immediate and retained render call sites that must use it.
- `scopeFence`: The list of later-phase semantics intentionally excluded from this feature.

**Validation Rules**
- Callers must include immediate render, retained first-frame build, retained fresh rebuild, retained carry
  rebuild, retained update rebuild, and retained cache/replay emit.
- Scope fence must exclude modifier algebra, portals, public IR changes, intrinsic layout protocol, text
  shaping, compositor changes, and portable protocol work.

## ParityObservation

Represents one verification comparison proving behavior did not change.

**Fields**
- `scenario`: The fixture category under test.
- `immediateScene`: Output from immediate rendering.
- `retainedInitialScene`: Output from retained initialization.
- `retainedWarmScene`: Output from a retained warm step.
- `cacheDisabledScene`: Output from relevant disabled-cache oracle, when applicable.
- `expectedRelation`: Equality or pixel-equivalence relation required for the scenario.

**Validation Rules**
- Scenarios must cover nested clipping, offsets, cache boundaries, overlays, empty content, and warm retained
  reuse.
- A parity observation must include a discriminating check where practical, proving that the equality oracle
  can detect a real difference.
- Any failure must be recorded as either attributable to this feature or as a pre-existing/environmental
  limitation with evidence.

## State Transitions

This feature adds no runtime state machine. The implementation transition is architectural:

1. Current duplicated call sites each assemble scenes independently.
2. Tests are added that describe the shared assembly contract and compatibility obligations.
3. Call sites are routed through the single current-node assembly boundary.
4. Existing parity and cache oracles prove equivalent rendered output.
