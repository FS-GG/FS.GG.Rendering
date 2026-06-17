# Research: Shared Assembly Extraction

## Decision: Use an Internal Current-Node Assembly Boundary

**Decision**: Introduce one internal current-semantics assembly boundary that combines a control node's own
paint, evaluated box, and child assembly results into in-flow and overlay scene contributions.

**Rationale**: The existing duplication is not in low-level painting or layout; it is in the repeated
composition of own paint, child in-flow paint, child overlay paint, container clips, and overlay promotion.
A node-level boundary can be shared by immediate rendering, retained first-frame build, retained fresh/carry
rebuilds, and retained cache/replay emit walks without changing public scene data.

**Alternatives considered**:
- Extract a whole-tree builder immediately. Rejected for R1a because it would approach R1b scope and risk
  changing retained identity or cache behavior.
- Leave `composeContainerScene` as the only shared helper. Rejected because it does not own overlay splitting
  and therefore still leaves multiple hand-written assembly paths.
- Introduce modifier or portal semantics now. Rejected because those are P2/R2 responsibilities and would
  make R1a a semantic change.

## Decision: Keep the First Extraction Compatible with F# Compile Order

**Decision**: Prefer a `ControlInternals` helper for the first extraction. Use a new paired internal file only
if implementation can do so without moving large unrelated paint helpers or widening public surface.

**Rationale**: `Control.renderTree` lives in `Control.fs`, while `RetainedRender` is compiled later. Current
paint helpers such as layout evaluation, own-node paint, node boxes, container composition, and overlay
classification already live in `Control.fs` and are exposed internally through `Control.fsi`. A helper there
can be consumed by both `renderTree` and `RetainedRender` without introducing a circular compile-order
problem.

**Alternatives considered**:
- Add `src/Controls/Assemble.fsi/fs` before `Control.fs`. Rejected as the default because the new file would
  need paint helpers that currently live later in `Control.fs`, forcing a much larger move.
- Add `src/Controls/Assemble.fsi/fs` after `Control.fs`. Rejected as the default because `Control.renderTree`
  could not call it under F# file ordering.
- Move all painting into a new file in one cut. Deferred because it is larger than the behavior-preserving
  R1a goal.

## Decision: Preserve the Existing In-Flow and Overlay Pair Shape

**Decision**: Represent assembly output as the current pair of in-flow scene list and overlay scene list, or
an internal record with exactly those two meanings.

**Rationale**: Current full and retained rendering already use this conceptual shape. Preserving it minimizes
behavioral risk, keeps overlay-free trees byte-identical, and avoids premature public IR changes. A record may
improve readability; a tuple may reduce churn. The implementation can choose either as long as the internal
contract remains explicit and test-covered.

**Alternatives considered**:
- Store only a final flattened scene list. Rejected because retained fragments already need separate in-flow
  and overlay contributions for fast paths and emit walks.
- Change `RenderFragment` storage now. Rejected because fragment privacy and retained unification belong to
  R1b, not R1a.
- Add first-class layers. Rejected because that belongs to R2.

## Decision: Validation Must Combine New Focused Tests with Existing Oracles

**Decision**: Add focused Feature 139 tests for the new assembly boundary and keep existing parity/audit
oracles green.

**Rationale**: The new tests prove the extraction exists and covers edge categories named in the spec. The
existing tests prove compatibility: full retained parity, cache-on/cache-off parity, clipping, overlay
escape/order, warm retained reuse, and public surface stability.

**Alternatives considered**:
- Rely only on existing Feature 137 tests. Rejected because they cover important behavior but not the new
  ownership boundary.
- Rely only on code review. Rejected because the constitution requires automated evidence for behavior-
  changing or architecture-significant work.
- Run only broad preflight. Rejected because broad gates are useful but do not isolate R1a regressions.

## Decision: No External Contract Is Added

**Decision**: Document an internal compatibility contract rather than a public API contract.

**Rationale**: The feature is Tier 2 and must not change package consumers' authoring or scene contracts.
The contract that matters is the internal rule set for assembly and the verification obligations around it.

**Alternatives considered**:
- Add public scene builders or public assembly APIs. Rejected because public API expansion would make this a
  Tier 1 change and conflict with the spec.
- Skip contracts entirely. Rejected because an explicit internal compatibility contract helps later R2/R1b
  planning and gives `/speckit-tasks` a concrete artifact to reference.
