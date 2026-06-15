# Internal Contract: `RetainedRender` (Feature 091)

Feature 091 exposes **no public/external interface** — the entire surface is `internal`, with zero
public-surface-baseline delta (FR-010). Per the constitution's vertical-slice rule, the in-assembly
property tests reaching this surface via `[<assembly: InternalsVisibleTo("Controls.Tests")>]` **are**
the contract's audience. This document records that internal contract — the operations and the
invariants the property tests pin — so the contract is explicit, not implicit in the `.fsi`.

Source: `src/Controls/RetainedRender.fsi` (091 slice) + `src/Controls/Reconcile.fsi` (067).
Authoritative tests: `tests/Controls.Tests/Feature091RetainedRenderTests.fs`.

## Operations (091 slice)

### `Reconcile.diff : prev:Control<'msg> -> next:Control<'msg> -> ReconcileResult<'msg>`
Pure, total, deterministic keyed diff. Children match by `Key` first, then unkeyed residuals
positionally; a `Kind` mismatch on a matched pair → whole-subtree `Replace`; duplicate sibling keys →
`KeyCollision` `Warning` in `Diagnostics`. Never throws.

### `Reconcile.apply : prev:Control<'msg> -> patch:NodePatch<'msg> -> Control<'msg>`
Round-trip oracle: `apply prev (diff prev next).Patch` is structurally equal to `next` (up to
attribute ordering, which the diff canonicalizes by `Name`). Pure.

### `RetainedRender.init : theme:Theme -> size:Size -> control:Control<'msg> -> RetainedInit<'msg>`
Seeds the retained structure from the first frame (`NextId` seeded, `StateByIdentity` empty), paints
it once, and surfaces any first-frame diagnostics. Tests project `.Retained` via the `rinit` helper.

### `RetainedRender.step : theme:Theme -> size:Size -> r:RetainedRender<'msg> -> next:Control<'msg> -> RetainedRenderStep<'msg>`
The wired frame transition. Diffs `next` against `r.Root`, confers/carries identities, reuses
unchanged-unshifted fragments, re-keys `StateByIdentity`, and returns `{ Retained; Render;
Diagnostics; WorkReduction }`. **Pure, total, deterministic.**

### `RetainedRender.advance : delta:TimeSpan -> clock:AnimationClock -> AnimationClock`
Advances a carried clock by an injected delta (used by US2 to prove the carried clock *continues*, not
resets). Pure, total; non-positive delta is a no-op; positive delta accumulates `Elapsed` clamped to
duration. (Live ticking that *calls* this each frame is feature 099, out of 091 scope.)

## Contract invariants → requirements → tests

| # | Invariant | Maps to | Pinned by (testList) |
|---|---|---|---|
| C1 | An unchanged matched node carries the **same** `RetainedId` across an unrelated re-render, including a positional shift; it is matched (`ChildKeep`/`ChildMove`), never `Replace`. | FR-001 / SC-001 | `091 US1 identity survives an unrelated re-render` |
| C2 | A node whose `Kind` changed (same `Key`) is `Replace`d with a **fresh** `RetainedId` (no false reuse). | FR-002 / SC-001 | `091 US1 …` (Kind-change cases) |
| C3 | `RetainedUiState` keyed by `RetainedId` (focus + in-flight clock) **survives** a positional shift; `advance` on the carried clock increases `Elapsed` (no reset). The rebuild-every-frame baseline **loses** it. | FR-003 / SC-002 | `091 US2 focus + animation survive an unrelated re-render` |
| C4 | A localized change recomputes only the changed subtree: `RecomputedNodeCount ≤ ChangedSubtreeBound < BaselineNodeCount (N)`. | FR-007 / SC-003 | `091 US3 partial update + golden parity` |
| C5 | The wired `Render` is byte-identical (structural `Scene` + `Bounds` + `NodeCount`) to `Control.renderTree next`. | FR-006 / SC-004, SC-005 | `091 US3 …` + `091 US4 … round-trip` |
| C6 | `step` is **deterministic** (identical sequences → identical render + ids) and **total** (never throws) for any `(prev, next)`. | FR-008 / SC-005 | `091 US4 invariants on the wired path (FsCheck, ≥1000 cases)` |
| C7 | Structurally identical consecutive frames are a true no-op: `RecomputedNodeCount = 0`, `NextId` unchanged, ids unchanged, **no** diagnostics. | FR-004 / SC-005 | `091 US4 … identity-at-rest` |
| C8 | Reuse decisions use F# **structural** equality, never object identity. | FR-005 | implied by C5/C7 (equal-but-reallocated trees still reuse) |
| C9 | A duplicate-keyed sibling list surfaces a `KeyCollision` `Warning` through `Diagnostics`, and `step` still completes (no throw). | FR-009 / SC-006 | `091 US4 … Synthetic: duplicate-keyed sibling list` |
| C10 | The entire surface is `internal` — zero public-surface-baseline delta. | FR-010 | surface-drift check (`tests/surface-baselines`) unchanged |

## Out of contract (091)

Not asserted by 091; owned by later features and documented there: live clock ticking + retarget
(099), cross-fade snapshot/composite (103), theme-change fragment invalidation + `RetainedInit`
first-frame-paint-once + `ShiftedNodeCount` (092), incremental layout cache (097), memoization (113),
virtualization (114), picture cache (116), text-measure cache (117), structural fingerprint + replay
(120). The `.fsi` carries these inseparably; 091's contract is the C1–C10 subset above.
