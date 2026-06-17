# Contract: Retained Renderer Unification

## Contract Type

Internal framework architecture contract plus public compatibility guarantees. The retained renderer stays
an implementation detail of `FS.GG.UI.Controls`; package consumers should observe compatible rendering,
diagnostics, event binding, hit-test, cache, and public surface behavior unless an intentional compatibility
decision says otherwise.

## Public Compatibility Contract

### Stable Consumer Surface

The feature should preserve:

- Public control authoring APIs in `FS.GG.UI.Controls`.
- Public Scene constructors and data shapes in `FS.GG.UI.Scene`.
- `Control.renderTree` result shape and semantics.
- Existing `ControlRenderResult` fields: `Scene`, `Layout`, `Bounds`, `Diagnostics`, `EventBindings`,
  `BoundIds`, and `NodeCount`.
- Existing `Control.hitTest` topmost-compatible behavior.
- Existing package ids and target framework.

Any intentional change requires:

- `.fsi` edit before `.fs` implementation.
- Semantic tests proving the new contract.
- Surface baseline update.
- Migration guidance.
- Versioning rationale.
- Pixel/diagnostic/metric disclosure entry when observable output changes.

## Single Assembly Owner Contract

Exactly one internal assembly producer owns current scene semantics.

Required properties:

- Direct rendering calls the assembly owner for each node/subtree.
- Retained initialization calls the same owner for each fresh node/subtree.
- Warm retained rebuild, shifted-subtree recomposition, replay emit, and active-animation emit call the same
  owner whenever they need assembled output.
- Retained state may store prior owner-produced results, but may not independently apply clipping, overlay,
  modifier, layer, portal, legacy lowering, cache-boundary, or glyph-run proof semantics.
- `RetainedRender.fs` must not regain retained-local helpers equivalent to old `composeRetainedScenes` or
  direct branching on overlay/composition semantics outside the shared owner or `Composition` evidence.

Recommended code-level guards:

- Keep assembly result constructors hidden or narrowly scoped where practical.
- Add source-level tests that fail if retained rendering reintroduces known old local composition helper names.
- Keep `Controls.Composition` as the shared evidence/classification source for retained invalidation.

## Retained Reuse Contract

Retained rendering may reuse a previous assembly result only when all relevant reuse inputs are equivalent.

Reuse inputs include:

- Reconciliation identity compatibility.
- Control kind, content, attributes, children, and authored key.
- Evaluated layout box.
- Theme and style-resolved paint inputs.
- Modifier/layer/portal/overlay inputs.
- Legacy lowering inputs.
- Text and glyph-run proof inputs.
- Cache-boundary and replay fingerprint inputs.
- Child order and child assembly results.

Reuse outcomes:

- `Reuse`: prior owner-produced assembly result is used unchanged.
- `Rebuild`: current scope is freshly assembled by the owner while preserving compatible retained identity.
- `Discard`: incompatible prior state is dropped and a fresh identity/result is created.
- `FallbackFresh`: unsafe or unclassified reuse path uses fresh assembly and records evidence.

Stale output is invalid: if any required input cannot be proven equivalent, retained rendering must rebuild
or discard rather than reuse.

## Direct/Cold/Warm Equivalence Contract

For equivalent inputs:

- `Control.renderTree theme size control`
- `RetainedRender.init theme size control`
- `RetainedRender.step theme size retained control`

must produce equivalent:

- Visible `Scene`.
- `Bounds`.
- `Diagnostics`.
- `EventBindings`.
- `BoundIds`.
- `NodeCount`.
- Structural fingerprints for comparable assembly results.
- Cache/replay transparency when cache flags are enabled or disabled.

Warm retained frames may additionally report reuse metrics. Those metrics must be deterministic and must
not change rendered output.

## Cache Transparency Contract

The existing cache-disable switches remain correctness oracles:

- `MemoEnabled = false`
- `PictureCacheEnabled = false`
- `TextCacheEnabled = false`

For the same input frame, cache-disabled and cache-enabled render output must remain equivalent. Disabled
cache variants may report different hit/miss counts, but they must not change scene, bounds, diagnostics,
event bindings, bound ids, or public surface.

## Atomic Commit Contract

A retained step builds the next frame as work-in-progress data and returns one complete next retained state.

Required properties:

- Previous retained state remains valid until the new step result exists.
- No partially updated retained tree, cache, or render result is externally visible.
- Fresh fallback is preferred over partial reuse when reuse evidence is incomplete.
- Diagnostics for abandoned/fresh fallback paths are actionable and deterministic.

## Verification Contract

Implementation readiness requires:

- Focused compatibility for at least these categories: empty content, nested clipping, transforms or offsets,
  cache boundaries, local z-order, portal layers, legacy lowering, and glyph-run proof data.
- Invalidation coverage for at least these categories: visual input, layout input, modifier/layer input,
  text proof input, explicit identity, and child ordering.
- Randomized direct/cold/warm equivalence over at least 200 generated control trees or composition chains.
- Determinism checks over at least three consecutive equivalent retained frames.
- Source/evidence review identifying one assembly owner and zero retained-only scene composition rule sets.
- Public surface check reporting zero public changes or documented changes with migration/versioning notes.
- Golden/pixel check reporting zero intentional baseline changes or documented baseline changes.
- Verification limitations recorded with command, status, environment facts, and attribution.

## Out-of-Scope Contract

The feature must not implement:

- Full text shaping.
- Overlay interaction state.
- Portable scene serialization.
- Compositor promotion or damage-scissored presentation.
- Intrinsic layout protocol.
- New public retained renderer APIs.
