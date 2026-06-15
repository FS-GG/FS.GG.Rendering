# Feature Specification: Wire the Keyed Reconciler onto the Render Path (Feature 091)

**Feature Branch**: `091-wire-reconciler-render-path`

**Created**: 2026-06-15

**Status**: Final

**Input**: User description: "next phase in fs.gg"

## Context

This is the first **product-feature** specification authored in FS.GG.Rendering after the R1→R8
migration completed (the migration imported the runtime source and froze/rebranded its identity).
The imported source already carries a **parked keyed reconciler** (`module internal Reconcile`,
historical feature 067) and a working retained render structure (`module internal RetainedRender`)
with executable property tests (`tests/Controls.Tests/Feature091RetainedRenderTests.fs`) and
captured readiness evidence under `specs/091-wire-reconciler-render-path/readiness/`. **No Spec Kit
spec/plan/tasks have ever described this work.** This document backfills the contract for the code
that is already present, so the capability is governed by `Spec → .fsi → semantic tests →
implementation` like any other feature rather than living as undocumented imported behavior.

The capability itself: today a frame is produced by rebuilding a fresh control structure every time
(`Control.renderTree`), which mints a **fresh, path-derived identity** for every node each frame.
Any per-control state keyed to that identity — keyboard focus, an in-flight animation clock, a text
edit model — is **lost** whenever an unrelated change shifts a control's position in the tree. Feature
091 **wires the parked keyed reconciler onto the live render path**: each frame diffs the next
control tree against the previous retained tree, confers a **stable identity** on every matched node,
re-keys per-control state to that stable identity, and **reuses the cached render fragments** of
unchanged subtrees instead of repainting them.

The whole surface is **assembly-internal** (`internal`), exactly like the reconciler it wires. It is
a contract between framework internals and the property tests, not a consumer API — it adds **zero**
public-surface-baseline delta. Per the constitution's vertical-slice rule, the in-assembly
Expecto/FsCheck tests (reaching the internals via `InternalsVisibleTo("Controls.Tests")`) **are** the
user-reachable surface for these internal user stories.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A control keeps its identity across an unrelated re-render (Priority: P1)

When something elsewhere in the UI changes, a control that did **not** change keeps the **same stable
identity** from one frame to the next — even if its position in the tree shifted because a sibling was
inserted or removed. A control whose essential kind changed is treated as a different control and
gets a fresh identity (no false identity reuse).

**Why this priority**: Stable identity is the foundation everything else (focus survival, animation
survival, subtree reuse) is keyed on. Without it, none of the other stories can hold. This is the MVP
slice — wiring the diff so it confers a carried identity is independently valuable and testable.

**Independent Test**: Render frame 0, then a frame 1 that changes only an unrelated sibling (or
inserts one above the target), and confirm the unchanged control reports the same stable identity in
both frames and was *matched* (not replaced) by the diff. Separately, change a control's kind and
confirm it receives a new identity.

**Acceptance Scenarios**:

1. **Given** a tree `[a, editor]` rendered at frame 0, **When** frame 1 changes only `a` (editor
   untouched), **Then** `editor` carries the same stable identity and the diff matches it (never
   `Replace`).
2. **Given** a tree `[editor]`, **When** frame 1 inserts `banner` above it so `editor`'s positional
   path shifts, **Then** `editor` still carries the same stable identity.
3. **Given** a keyed control, **When** frame 1 keeps its key but changes its kind, **Then** the
   control is replaced and receives a **fresh** identity (no false reuse).

---

### User Story 2 - Focus and an in-flight animation survive an unrelated re-render (Priority: P1)

Per-control state that is keyed to the stable identity — keyboard focus and an animation clock that
is mid-tween — **survives** an unrelated change that shifts the control's position, and the carried
clock **continues from where it was** rather than resetting. A rebuild-every-frame baseline (the
pre-091 behavior) **fails** the same proof, demonstrating the wiring is what fixes it.

**Why this priority**: This is the user-visible payoff of stable identity: typing focus doesn't jump
and animations don't stutter when an unrelated part of the screen updates. It is co-critical with US1
because it is the concrete defect class the feature exists to close.

**Independent Test**: Seed focus + a started clock on a control keyed by its stable identity, apply an
unrelated update that shifts its position, and confirm the state is still present under the same
identity and that advancing the clock continues (elapsed increases) rather than restarting. Run the
same scenario against a rebuild-every-frame baseline and confirm it loses the state.

**Acceptance Scenarios**:

1. **Given** `editor` with focus and an in-flight clock (250 ms into a 1 s fade) keyed by its stable
   identity, **When** an unrelated sibling is inserted above it, **Then** the focus/clock state is
   still found under `editor`'s (unchanged) identity and advancing the clock increases its elapsed
   time.
2. **Given** the same positional shift, **When** the tree is rebuilt fresh every frame with no
   retained identity, **Then** `editor`'s identity differs frame-to-frame and the state is lost
   (the baseline fails the proof).

---

### User Story 3 - A localized change repaints only the changed subtree, with identical output (Priority: P2)

A small, localized change repaints **only** the changed subtree and reuses the cached render of
everything else, so the per-frame work is **strictly smaller** than rebuilding the whole tree — while
the produced frame is **byte-identical** to a full rebuild of that same frame (correct, not merely
cheaper).

**Why this priority**: This is the efficiency payoff and the correctness guard on reuse. It depends on
US1's stable identity to know what is unchanged, so it is P2. It is independently demonstrable via the
work-reduction and golden-parity evidence.

**Independent Test**: Apply a single localized leaf change over a wide fixed tree and assert the
recomputed-node count is bounded by the changed subtree and strictly less than the total node count
(`N`); separately assert the wired frame's scene, bounds, and node count equal a full rebuild of the
same next frame.

**Acceptance Scenarios**:

1. **Given** a wide tree with one localized leaf change (no geometry shift), **When** the frame is
   produced via the wired path, **Then** the recomputed-node count ≤ the changed-subtree bound and
   < the total node count `N`.
2. **Given** any next frame, **When** produced via the wired path, **Then** its scene, bounds, and
   node count are byte-identical to `Control.renderTree` of that same next frame (golden parity).

---

### User Story 4 - The reconciler invariants hold on the live path, and malformed input is reported, not fatal (Priority: P2)

The properties the parked reconciler guaranteed in isolation continue to hold once it is wired live:
the output round-trips to a full rebuild, identical inputs are deterministic, the step is **total**
(never throws) for any pair of frames, and structurally identical frames are a true no-op (zero
re-measure, no identity churn). Malformed input — e.g. duplicate sibling keys — is surfaced as an
observable **diagnostic** (a warning) through the existing channel rather than throwing or silently
corrupting identity.

**Why this priority**: These are the safety/robustness guarantees that let the feature be trusted on
the real path. They are P2 because they protect US1–US3 rather than delivering a new user journey
themselves, and they are verified by high-volume property tests (≥1000 cases).

**Independent Test**: Property-test `step` over generated `(prev, next)` pairs for round-trip equality,
determinism, totality, and identity-at-rest; feed a deliberately duplicate-keyed sibling list and
confirm a `KeyCollision` warning is emitted while the step still completes without throwing.

**Acceptance Scenarios**:

1. **Given** any generated `(prev, next)` frame pair, **When** stepped via the wired path, **Then**
   the produced render equals a full rebuild of `next` (scene + bounds + node count), the run is
   deterministic, and it never throws.
2. **Given** two structurally identical frames, **When** the second is stepped against the first,
   **Then** zero nodes are recomputed, no new identities are minted, and no diagnostics are produced.
3. **Given** a sibling list with duplicate keys, **When** stepped, **Then** a `KeyCollision`
   diagnostic of severity *Warning* is surfaced and the step completes without throwing.

---

### Edge Cases

- **Positional shift**: an inserted/removed sibling that moves a node's path — must still match and
  carry identity (the core defect class).
- **Kind change under the same key**: must replace with a fresh identity, never falsely reuse.
- **Duplicate sibling keys**: must surface a `KeyCollision` warning and stay total (no throw).
- **Structurally identical consecutive frames**: must be a genuine no-op — no recompute, no id churn,
  no diagnostics.
- **Arbitrary malformed/degenerate trees**: `step` must be total for any `(prev, next)`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST confer, on every matched node across a re-render, a **stable identity**
  that is independent of the node's path-derived position, so it is preserved across a positional
  shift.
- **FR-002**: The system MUST replace (not reuse) a node whose essential kind changed, minting a
  **fresh** identity for the replacement (no false identity reuse).
- **FR-003**: Per-control state (keyboard focus, animation clock, text model) MUST be keyed to the
  **stable identity** so it survives an unrelated re-render that shifts the control's position; a
  carried in-flight animation clock MUST continue from its prior elapsed time, not reset.
- **FR-004**: A node at rest (unchanged, unshifted) MUST reuse its **cached render fragment** rather
  than re-measuring or repainting, and MUST paint byte-identically to a static full render.
- **FR-005**: Reuse decisions MUST be based on **structural** equality of inputs (never object
  identity), so identical inputs are always treated as unchanged.
- **FR-006**: For any `(prev, next)` frame pair, the wired render output MUST be **byte-identical** to
  a full rebuild of `next` — scene, bounds, and node count all equal `Control.renderTree next`
  (round-trip parity).
- **FR-007**: A localized change MUST recompute only the **changed subtree**: the recomputed-node
  count MUST be ≤ the changed-subtree bound and strictly < the total node count.
- **FR-008**: The step MUST be **total** (never throw) and **deterministic** (identical frame
  sequences produce identical output and identical minted identities — minting uses a per-host counter
  with no clock/randomness).
- **FR-009**: Malformed input (e.g. duplicate sibling keys) MUST be surfaced as an observable
  **diagnostic** of severity *Warning* through the existing control-diagnostic channel, without
  throwing and without corrupting identity.
- **FR-010**: The entire surface MUST remain **assembly-internal** — zero public-surface-baseline
  delta — and remain exercised only through the in-assembly property tests via `InternalsVisibleTo`.
  (Verified directly by the surface-drift check; this requirement has no separate Success Criterion.)

### Key Entities *(include if feature involves data)*

- **RetainedId**: the stable, path-independent identity conferred on a matched node; monotonic within
  a host loop, minted deterministically from a per-host counter.
- **RetainedNode**: one retained control node — its stable identity, the control it was built from,
  its cached render fragment, and its retained children (mirroring child order).
- **RenderFragment**: the cached, reusable unit of measure + paint for a node (its own painted scene,
  its subtree's painted scene, its evaluated box, and a structural fingerprint) reused verbatim when
  the subtree is unchanged and unshifted.
- **Per-identity state**: the focus / animation-clock / text state map keyed by `RetainedId` so it
  survives positional shifts.
- **Diagnostic**: an observable warning (e.g. `KeyCollision`) emitted through the control-diagnostic
  channel for malformed input.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An unchanged control carries the **same** stable identity across an unrelated re-render
  in 100% of cases, including when its position shifts; a kind change yields a different identity in
  100% of cases.
- **SC-002**: Focus and an in-flight animation clock survive an unrelated, position-shifting
  re-render in 100% of cases, and the carried clock advances (does not reset); the rebuild-every-frame
  baseline loses the state in 100% of the same cases.
- **SC-003**: For a localized single-leaf change over a wide tree, the recomputed-node count is ≤ the
  changed-subtree bound and strictly less than the total node count `N`.
- **SC-004**: The wired frame is byte-identical (zero diff) to a full rebuild of the same next frame
  for the localized-change scenario (golden parity).
- **SC-005**: Across ≥1000 generated frame pairs, the wired step is byte-identical to a full rebuild,
  deterministic, total (never throws), and a true no-op (zero recompute, zero identity churn, zero
  diagnostics) for structurally identical frames — 100% pass.
- **SC-006**: A duplicate-keyed sibling list surfaces a `KeyCollision` warning through the diagnostic
  channel while the step completes without throwing, in 100% of cases.

## Assumptions

- The parked keyed reconciler (`module internal Reconcile`, feature 067) and the `RetainedRender`
  structure already exist in the imported source; this feature is the **backfilled contract** for
  wiring them onto the live render path, not new-from-scratch construction.
- The surface stays **internal**; "users" of these stories are framework internals plus the
  in-assembly property tests (per the constitution's vertical-slice rule), not external package
  consumers. No public API is added.
- Per-control state (focus/animation/text) is the state worth preserving across re-renders; it is
  keyed by stable identity. Live animation-clock advancement is owned by the host loop (feature 099)
  and is out of scope here beyond proving the carried clock survives and continues.
- Render output equivalence is judged by structural scene equality (plus bounds and node count), the
  authoritative parity proof; pixel-level/desktop-visibility proofs are out of scope (the readiness
  evidence explicitly does not claim them).
- The existing readiness evidence under `readiness/` (retained-parity, work-reduction,
  survives-proof) corresponds to SC-002/SC-003/SC-004 and is the captured artifact for those outcomes.
