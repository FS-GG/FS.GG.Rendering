# Data Model: Retained Renderer Unification

## Authoritative Assembly Producer

**Purpose**: The single owner that turns the current control tree, evaluated layout boxes, own-node paint,
child assembly results, and Feature 140 composition evidence into final in-flow and overlay/layer scene
contributions.

**Likely representation**:
- Existing starting point: `ControlInternals.assembleCurrentNode`
- Possible evolved contract: an internal `AssemblyResult` record with in-flow scene, overlay/layer scene,
  diagnostics, fingerprint input, and child contribution metadata

**Fields / Inputs**:
- `Control`: current `Control<'msg>` node
- `Box`: evaluated `Rect option`
- `OwnScene`: `Scene list` produced by `ControlInternals.paintNode`
- `ChildAssemblies`: ordered assembly results from children
- `CompositionEvidence`: normalized modifier/layer/legacy evidence from `Controls.Composition`

**Validation rules**:
- Must be pure and deterministic for equivalent inputs.
- Must be the only place that applies current clipping, overlay, layer, portal, cache-boundary, modifier,
  legacy lowering, and glyph-run proof assembly semantics.
- Must not depend on retained identity or prior frame state to determine scene semantics.
- Must produce direct, cold retained, and warm retained equivalent output for equivalent inputs.

**Relationships**:
- Consumed by `Control.renderTree`.
- Consumed by retained initialization, retained warm step, replay emit, and any direct/cold retained wrappers.
- Uses `Controls.Composition` evidence but does not expose public composition API by default.

## Assembly Result

**Purpose**: The immutable output of the authoritative producer for a node or subtree.

**Fields**:
- `InFlowScene`: scene list that remains in normal parent composition.
- `OverlayScene` or layer contribution list: scene list deferred above in-flow content under current
  compatibility semantics.
- `Box`: evaluated box used to produce the result.
- `Fingerprint`: deterministic structural fingerprint of the result that backend replay/cache evidence can
  use.
- `Diagnostics`: assembly diagnostics, if the implementation widens the current result shape.

**Validation rules**:
- Equal inputs produce equal scene output and equal fingerprints.
- Fingerprints cover every render-affecting field used by the result.
- Empty scenes, empty children, and absent boxes produce deterministic empty-compatible output.
- Diagnostics do not change user-visible scene descriptions unless explicitly documented.

**Relationships**:
- Stored by retained state as reusable prior output.
- Compared by parity oracles against direct `Control.renderTree` output.
- Used by replay boundaries and cache evidence.

## Retained Renderer

**Purpose**: The rendering mode that carries prior state to avoid repeated work while preserving the same
semantics as direct rendering.

**Fields**:
- `Root`: retained tree rooted at a retained node.
- `NextId`: deterministic monotonic identity counter.
- `StateByIdentity`: UI state keyed by retained identity.
- `Theme`: theme used for cached paint results.
- `Memo`: memo cache for memoizable projections.
- `Layout`: previous layout result for incremental layout.
- `PictureCache`: bounded picture cache with deterministic recency.
- `TextCache`: bounded text measure cache with deterministic recency.
- Cache parity flags: `MemoEnabled`, `PictureCacheEnabled`, `TextCacheEnabled`.

**Validation rules**:
- Must never expose partial next state if a step is abandoned.
- Must invalidate cached assembly results on relevant identity, visual, layout, modifier, layer, portal,
  text proof, theme, or cache-boundary changes.
- Must preserve state only for compatible reconciled children.
- Must fall back to fresh assembly for any scope that cannot be safely reused.

**Relationships**:
- Uses `Reconcile.diff` to compare previous and next control trees.
- Uses the authoritative assembly producer for all fresh or recomposed output.
- Emits `ControlRenderResult` compatible with `Control.renderTree`.

## Retained Node

**Purpose**: Per-control retained state for identity, previous control description, previous assembly output,
and retained children.

**Fields**:
- `Identity`: stable `RetainedId`.
- `Control`: previous/current matched `Control<'msg>`.
- `Assembly` or `Fragment`: assembly result produced by the authoritative producer.
- `Children`: ordered retained child nodes.

**Validation rules**:
- Identity is preserved only when `Reconcile` reports compatible kind/key semantics.
- Reordered children preserve only compatible retained nodes.
- Removed children drop retained state and cache entries as appropriate.
- Duplicate keys produce existing key-collision diagnostics and do not permit stale output.

**Relationships**:
- Parent retained nodes compose child assembly results only by calling the assembly owner.
- Retained UI state is keyed by `Identity`.

## Reconciliation Identity

**Purpose**: Stable evidence used to decide whether retained state may be preserved across frames.

**Fields**:
- `RetainedId`: deterministic monotonic internal id.
- Authored key or positional path evidence from `Reconcile`.
- Compatibility facts: kind match, key match, child operation, and path/order movement.

**Validation rules**:
- Equivalent frame sequences mint equivalent identity sequences.
- Kind/key replacement creates a new identity.
- Child insertion/removal/reordering cannot leak stale output into incompatible positions.
- Positional shifts must either preserve compatible identity with recomputed layout/assembly or rebuild.

## Invalidation Evidence

**Purpose**: Human and test-readable reason that a retained scope was reused, rebuilt, discarded, or freshly
assembled.

**Fields**:
- `Scope`: control id/path/retained id for the affected node or subtree.
- `Decision`: reused, rebuilt, discarded, fresh fallback, cache hit, cache miss, replay hit, replay miss.
- `Reason`: visual input, layout input, modifier/layer input, portal/overlay input, text proof input,
  cache-boundary input, theme input, explicit identity, child ordering, removal, insertion, or unsafe reuse.
- `FingerprintBefore` / `FingerprintAfter`, where available.
- `BoxBefore` / `BoxAfter`, where available.

**Validation rules**:
- Must cover at least the six categories required by the spec: visual input, layout input, modifier or
  layer input, text proof input, explicit identity, and child ordering.
- Must be deterministic across equivalent runs.
- Must not alter rendered output.

## Parity Oracle

**Purpose**: A comparison proving two rendering modes or cache modes are semantically equivalent for the
same input.

**Comparison dimensions**:
- Direct `Control.renderTree`.
- Cold retained initialization.
- Warm retained step with unchanged inputs.
- Warm retained step after relevant changes.
- Cache-enabled versus cache-disabled variants.
- Scene output, bounds, diagnostics, event bindings, bound ids, node count, fingerprints, cache evidence,
  and work metrics where applicable.

**Validation rules**:
- Direct, cold retained, and warm retained output must match for equivalent inputs.
- Cache-disabled output must match cache-enabled output.
- Randomized verification must cover at least 200 generated trees or composition chains with zero
  equivalence failures.
- Determinism verification must repeat equivalent retained frames at least three times.

## Unification Evidence

**Purpose**: The artifact set showing the second retained builder drift class is closed.

**Fields**:
- Single assembly owner name and file path.
- Removed or prohibited retained-only composition responsibilities.
- Compatibility results for P1/P2 areas: shared assembly, modifiers, local z-order, portal layers, legacy
  lowering, cached subtrees, and glyph-run proof data.
- Public surface result and migration/versioning notes if any surface changed.
- Pixel/golden disclosure entries for intentional baseline changes.
- Verification limitations and pre-existing failures.

**Validation rules**:
- Must identify exactly one authoritative assembly producer.
- Must identify zero retained-only scene composition rule sets.
- Must explicitly confirm excluded work stayed out of scope.
