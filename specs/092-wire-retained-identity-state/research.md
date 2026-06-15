# Phase 0 Research: Wire Retained Identity State onto the Live Path (Feature 092)

These are the design decisions the imported 092 wiring already embodies, recovered and recorded so the
backfilled contract explains *why* the code is shaped as it is. No decision here is open; each maps to
a functional requirement and a pinned test. (Backfill: research follows the implementation rather than
preceding it — see plan.md Complexity Tracking.)

## D1 — The live host READS and WRITES `StateByIdentity`, not just carries it

**Decision**: The Elmish adapter resolves and persists per-control state (focus, in-progress text
draft + line mode) through `retained.StateByIdentity[id]`, keyed by the node's stable `RetainedId`,
inside `resolveFocus` and `routeFocusedText`. 091 threaded the map frame-to-frame but never read it
on the live path; 092 makes the adapter the reader/writer.

**Rationale**: Identity is only useful if the thing that owns per-control state actually keys on it.
The old `ControlId` `hitTest` path is path-derived and resets on a positional shift; routing through
the identity map is the entire fix.

**Alternatives considered**: Keep `ControlId` keying and "stabilize" the path — rejected: any
positional shift still mints a different `ControlId`, and unkeyed same-kind siblings still collapse.
→ FR-001, FR-002; US1.

## D2 — Survival is proven through the real adapter seam, not a seeded fixture

**Decision**: The headline test (`Feature092LiveSurvivalTests`) drives focus + typing through the
actual `resolveFocus` + `routeFocusedText` + `RetainedRender.step` calls with **no** hand-seeded
focus/text state, then inserts a banner to shift the field and confirms the draft continues
(`hix` → `hixy`). A rebuild-every-frame baseline (`init` each frame) is run on the identical sequence
and loses the draft.

**Rationale**: A seeded `StateByIdentity` map would prove the map works, not that the *host* wires it.
The baseline branch is the fail-first evidence: same inputs, no carried identity, state lost.

**Alternatives considered**: Assert on a constructed map directly — rejected as not exercising the
seam the user actually hits. → FR-002, FR-003; SC-001; US1.

## D3 — Hit-test resolves the deepest node to a distinct identity per node

**Decision**: `retainedHitTest x y retained` returns the `RetainedId` of the deepest retained node
whose cached `Box` contains the point, and returns `None` outside the root. Keyed, unkeyed, and
keyed-container-wrapped fields each resolve to their **own** id — no shared-id collapse.

**Rationale**: Focus survival is only correct if the identity it lands on is the right one. The
`ControlId` path collapsed unkeyed same-kind siblings onto a shared id; per-node identity removes that
class of bug.

**Alternatives considered**: First-match / shallowest-match hit-testing — rejected: a leaf inside a
container must win over its ancestor. → FR-004; SC-002; US2.

## D4 — A pre-filled field appends on first keystroke; text-areas honor MultiLine

**Decision**: When `routeFocusedText` first sees a focused field, it seeds the identity's draft from
the control's **current value** (not an empty string), so the first keystroke **appends**; and it
preserves the control's `MultiLine` mode rather than forcing single-line.

**Rationale**: These are the concrete "090 defects" the `ControlId` path carried — an empty seed wiped
a pre-filled field on its first keystroke, and text-areas were hard-coded single-line. Seeding from
the live value and honoring the mode fixes both with zero character loss.

**Alternatives considered**: Seed empty and special-case "first edit" — rejected as more state for a
worse result. → FR-005; SC-002; US2.

## D5 — Every matched change handler fires on a single change

**Decision**: A control wired with more than one `onChanged` handler dispatches **all** matched
product messages for one change, not just the first.

**Rationale**: Dropping handlers silently is a correctness defect; the routing returns the full
message list. → FR-006; US2.

## D6 — Replace drops prior state; removal filters it out

**Decision**: On a `Replace` (kind change at the same key) the prior identity's `StateByIdentity`
entry is **dropped** (no false carry onto the replacement); a removed control's entry is **filtered
out** of the map (so focus clears), never left orphaned.

**Rationale**: Carrying state across a kind change would paste a stale focus/draft onto an unrelated
control; orphaned entries leak. The diff already classifies these transitions — 092 honors them when
updating the map. → FR-007; US1 (scenarios 3–4).

## D7 — Theme is part of the fragment reuse key

**Decision**: `RetainedRender<'msg>` carries the `Theme` the structure was painted under, and the
reuse decision includes theme equality. A theme change between otherwise-identical frames invalidates
**all** cached fragments and repaints; an unchanged tree under an unchanged theme reuses everything.

**Rationale**: Theme is a render-affecting input. If it weren't in the key, a light→dark switch would
silently keep stale colors. The reuse must produce a frame byte-identical to a full rebuild under the
new theme. → FR-008; SC-006; US3.

## D8 — Work reduction splits changed-vs-shifted honestly

**Decision**: Under a sibling-inserted-above layout shift, `WorkReductionRecord` reports
`RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount`, strictly less than
`BaselineNodeCount`. A relaid-out-but-unchanged leaf is counted as *shifted*, not hidden as free.

**Rationale**: 091 proved bounded work with no geometry shift; a shift forces a relayout that the
accounting must show. Honest accounting is the correctness guard on the efficiency claim. → FR-009;
SC-003; US4.

## D9 — `init` paints the first frame exactly once and surfaces frame-0 diagnostics

**Decision**: `init` returns `RetainedInit<'msg>` carrying the painted `Render` the adapter reuses
(instead of the adapter also calling `Control.renderTree`), and any first-frame duplicate-key
`KeyCollision` is surfaced in `Diagnostics` on **frame 0**. `init` stays total.

**Rationale**: 091 double-painted the first frame and reported frame-0 malformed input a frame late.
Returning the painted render removes the redundant paint; surfacing on frame 0 closes the diagnostic
lag without making `init` throw. → FR-010; SC-005; US5.

## D10 — Parity is structural; survival is draft continuity; pixels are out of scope

**Decision**: Per-frame render equivalence is judged by structural scene equality + bounds + node
count; survival is judged by the draft string continuing (`hix` → `hixy`) and the same `RetainedId`
recurring. `SceneEvidence.renderPng` is a capability-hash, not a pixel encoder — pixel-level and
desktop-visibility proofs are explicitly **not** claimed by the readiness evidence.

**Rationale**: The wired-vs-rebuild guarantees are structural facts that hold headless and
deterministically, independent of a GL surface; over-claiming pixels would be dishonest evidence
(Principle V). → FR-011; SC-004, SC-007; Assumptions.
