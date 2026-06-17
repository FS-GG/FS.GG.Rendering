# Research: Retained Renderer Unification

## Decision: Keep the Feature Internal to Controls Retained Rendering

**Rationale**: The active P3/R1b roadmap item is to remove the retained renderer as a second assembly
producer. Feature 139 already introduced `ControlInternals.assembleCurrentNode`, and Feature 140 already
introduced internal `Controls.Composition` modifier/layer/portal evidence without broad public Scene IR
churn. The next safe step is to unify retained rendering over those internal boundaries, not to add public
authoring or Scene protocol surface.

**Alternatives considered**:
- Add public Scene modifier/layer/portal nodes now. Rejected for this feature because Feature 140 already
  proved an internal composition foundation and the spec explicitly excludes later public protocol work.
- Rework SkiaViewer first. Rejected because the drift bug class lives in Controls assembly ownership; the
  viewer should remain an interpreter of final Scene output.

## Decision: Treat `ControlInternals.assembleCurrentNode` or Its Successor as the Single Assembly Owner

**Rationale**: Direct `Control.renderTree` already delegates recursive paint assembly to
`assembleCurrentNode`, and retained build/emit paths already call it through `assembleRetainedNode`.
The remaining risk is that retained state still owns fragment construction, carry/buildFresh/update
branches, and an emit walk that can reassemble output from cached pieces. Feature 141 should make retained
paths consume a single authoritative assembly result and store previous results for reuse, rather than
reconstructing equivalent rules locally.

**Alternatives considered**:
- Keep retained `build`, `buildFresh`, `carry`, and emit assembly as parallel code with tests. Rejected
  because the report identifies this parallel path as the bug class P3 must close.
- Make `Control.renderTree` call retained rendering directly without reshaping assembly ownership. Rejected
  because that would hide duplication rather than removing retained-owned composition semantics.

## Decision: Preserve Public Authoring and Scene Contracts by Default

**Rationale**: The feature is a Tier 1 architecture change, but the spec requires consumer behavior to
remain compatible unless a separate compatibility decision documents the change. Existing public
`ControlRenderResult`, Scene constructors, glyph-run proof surface, diagnostics, and control authoring APIs
should remain stable. Any accidental surface change must fail public surface checks.

**Alternatives considered**:
- Use the unification to expose new retained renderer APIs. Rejected because retained rendering is currently
  `internal` and is a framework implementation detail.
- Change Scene output shape to simplify retained storage. Rejected unless implementation proves an
  unavoidable need and documents migration/versioning impact.

## Decision: Retained State May Store Assembly Results, Not Own Composition Rules

**Rationale**: Retained rendering still needs identities, previous `RenderFragment`-like results, layout
state, memo/picture/text caches, fingerprints, animation/text UI state, and reuse metrics. These are
performance and reconciliation concerns. They are valid only when they store or replay what the shared
assembly owner produced for equivalent inputs. Invalidation evidence must state why a stored result was
reused, rebuilt, or discarded.

**Alternatives considered**:
- Drop retained state entirely and make retained mode an alias for direct rendering. Rejected because the
  feature must keep retained reuse evidence and warm-frame work reduction.
- Keep `RenderFragment` as a freely constructible composition unit. Rejected because constructor freedom is
  how a second builder can reappear. The implementation should hide constructors or otherwise make retained
  fragments unforgeable outside the assembly owner where practical.

## Decision: Use Existing Reconciliation and Cache Infrastructure

**Rationale**: `Reconcile.diff`, retained identities, layout dirty sets, memo cache, picture cache, text
measure cache, structural `hashScene`, replay boundaries, and work metrics are already covered by focused
audit tests. Feature 141 should reshape ownership around them rather than replacing these validated parts.
Cache-disabled parity switches remain essential: `MemoEnabled=false`, `PictureCacheEnabled=false`, and
`TextCacheEnabled=false` prove reuse is transparent.

**Alternatives considered**:
- Replace reconciliation with a new algorithm. Rejected because the spec targets assembly drift, not keyed
  diff semantics.
- Remove cache parity switches as implementation detail. Rejected because they are the existing oracles for
  cache transparency and stale-output prevention.

## Decision: Add Randomized Direct/Cold/Warm Equivalence Coverage

**Rationale**: Focused compatibility fixtures cover known bug classes, while the spec requires at least
200 generated control trees or composition chains. Randomized checks should compare direct rendering,
cold retained initialization, warm retained stepping, cache-disabled variants, diagnostics, fingerprints,
and deterministic reuse evidence across repeated runs.

**Alternatives considered**:
- Rely only on existing Feature 139/140 fixtures. Rejected because P3 is meant to prove a class of drift is
  closed, not only preserve named examples.
- Use pixel-only golden evidence. Rejected because many retained drift failures are structural,
  diagnostic, cache, or fingerprint differences that should be caught headlessly before pixel rendering.

## Decision: Atomic Retained Frame Commit

**Rationale**: The spec requires failed or abandoned retained updates to avoid exposing partial frames.
The retained step should compute the next retained root, caches, diagnostics, metrics, and render result as
a complete work-in-progress value, then return it as the only committed state. Existing function-returned
state makes this achievable without introducing external mutable state.

**Alternatives considered**:
- Mutate retained state incrementally during traversal. Rejected because it risks exposing partial state and
  complicates failure evidence.
- Catch and swallow assembly failures. Rejected by the constitution's safe-failure principle; unsupported
  cases should either rebuild fresh or report actionable diagnostics.

## Decision: Record Verification Limitations Separately from Feature Behavior

**Rationale**: Previous feature readiness notes record wrapper absence, stale package-surface filters, and
environment-dependent GL limits. Feature 141 must similarly distinguish pre-existing/tooling limitations
from retained renderer behavior. This avoids overstating evidence while keeping deterministic checks useful.

**Alternatives considered**:
- Treat unavailable GL/offscreen evidence as pass. Rejected because it would overclaim pixel readiness.
- Block all planning on environment limitations. Rejected because headless semantic/parity evidence can
  still be definitive for most of the retained unification contract.
