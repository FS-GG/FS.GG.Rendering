# Feature Specification: Memoization Seam (DataGrid) (Feature 113)

**Feature Branch**: `113-memoization-seam`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C6** in the 2026-06-15 missing-features plan,
continuing the Workstream C pattern (091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 / 110).

Feature 113 adds a control-internal **memoization seam**: a `memoize` function that, keyed by a control's
stable `ControlId` and a structural **dependency**, returns the **same stored subtree instance** when the
dependency is unchanged (a `Hit`) or recomputes and stores a fresh subtree when it changed or is cold (a
`Miss`). The sole memoized site is the **DataGrid row/column projection** (a childless `data-grid` leaf),
whose dependency is `(theme, evaluated box, dataGridCells)`. Equality is **structural** (`=`), never object
identity, so two equal-but-distinct boxed dependencies still `Hit`. The reuse is observable as
`MemoHits`/`MemoMisses`, surfaced publicly as `FrameMetrics.MemoHitCount`/`MemoMissCount`. An always-miss
oracle (`MemoEnabled = false`) proves the rendered scene is **byte-identical** with the seam disabled
(memo-on ≡ memo-off). A companion advisory `Diagnostics.stabilityReport` flags reuse-breaking instability
(per-frame event closures, always-new attribute values, unstable keys).

The implementation (`MemoOutcome`/`MemoEntry`/`MemoCache`/`memoize` + the `Memo`/`MemoEnabled` fields in
`RetainedRender.fs`/`.fsi`; the public `FrameMetrics.MemoHitCount`/`MemoMissCount`; `Diagnostics.stabilityReport`)
and the four suites (`Feature113MemoSeamTests`, `Feature113MemoParityTests`, `Feature113StabilityDiagTests` in
`Controls.Tests`, `Feature113MemoMetricsTests` in `Elmish.Tests`) **already exist** in the imported source.
**No Spec Kit spec/plan/tasks describe this work**, and 113 imported with **no `readiness/`**. This document
backfills the contract.

The seam is **assembly-internal**; the only public touch is the additive `FrameMetrics.MemoHitCount`/`MemoMissCount`
fields on the already-baselined public `FrameMetrics`, so the backfill adds **zero new public-surface-baseline
delta** (type-granular baseline). Per the constitution's vertical-slice rule the in-assembly tests are the
user-reachable surface.

**Recorded finding (routed to Workstream E2, not fixed here).** The `MemoEnabled` doc-comment in
`RetainedRender.fsi` reads "a parity test flips it `false` to force every `memoize` call down the Miss path".
This is **misleading**: when `MemoEnabled = false`, the `&&` short-circuit **bypasses `memoize` entirely** —
both counters stay `0/0` (a true bypass), no `Miss` is recorded. The behaviour is correct; only the narrative
overstates. Consistent with how DF-1 is deferred, this doc-comment re-scope is recorded here and routed to
**Workstream E2**; it is **not** edited in this doc-only backfill.

**Scope boundary.** 113 owns the `memoize` seam, its `MemoCache`/`MemoEntry`/`MemoOutcome` types, the
`MemoEnabled` parity oracle, the `MemoHits`/`MemoMisses` metrics, and the advisory `stabilityReport`. The
neighbouring caches are owned by their own features: the **layout/measure cache** is 097, the **picture cache**
is 116 ("the natural analog of the data-grid-only memo cache"), the **text-measure cache** is 117, the
**virtualization counts** are 114, the **replay cache** is 120.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The memoize seam reuses an unchanged subtree without recomputing (Priority: P1)

A memoizable site whose structural dependency is unchanged returns the **same stored subtree instance**
without running its compute thunk (a `Hit`); a cold or changed dependency runs the thunk once and stores the
result (a `Miss`). Equality is structural, and the cache is per `ControlId`.

**Why this priority**: This is the core reuse mechanism — the MVP. Without a correct Hit/Miss the seam adds
nothing.

**Independent Test**: Call `memoize` with a cold dependency (Miss, thunk runs once, stored); the same
dependency (Hit, thunk not re-run, `ReferenceEquals` same instance); a changed dependency (Miss, thunk runs
again, fresh subtree); two equal-but-distinct boxed deps (Hit — structural equality, FR-005); a never-seen
`ControlId` (cold Miss even with an equal dependency).

**Acceptance Scenarios**:

1. **Given** a cold first call, **When** memoized, **Then** outcome = Miss, the thunk runs once, the result is
   stored (C2).
2. **Given** a stable dependency, **When** memoized again, **Then** outcome = Hit, the thunk is not re-run,
   and the reused subtree is the same instance (C1, FR-004).
3. **Given** a changed dependency, **When** memoized, **Then** outcome = Miss with a fresh subtree (C2/C3);
   two equal-but-distinct boxed deps Hit (FR-005); a never-seen id misses (per-`ControlId`).

---

### User Story 2 - Memo-on is byte-identical to memo-off, with no staleness (Priority: P1)

With the seam active vs the always-miss oracle (`MemoEnabled = false`), every frame's rendered scene is
**byte-identical**. The reuse is real (a forced rebuild with unchanged data is a Hit, not vacuous), and there
is **no staleness**: changing the grid's real inputs forces a Miss and a fresh, different scene that equals
the memo-off build.

**Why this priority**: Co-critical with US1. A cache that changes output or serves stale data is a bug; parity
+ no-staleness is what makes the reuse safe.

**Independent Test**: Force several rebuilds; assert memo-on scenes == memo-off scenes (FR-006/SC-002);
unchanged data accrues `MemoHits > 0`/`MemoMisses = 0`; changed inputs accrue `MemoMisses > 0` with a
different scene that equals the memo-off build (FR-007).

**Acceptance Scenarios**:

1. **Given** repeated frames, **When** built seam-active vs always-miss, **Then** every scene is
   byte-identical (C5/SC-002).
2. **Given** unchanged data, **When** rebuilt, **Then** it is a real Hit (`MemoHits > 0`, `MemoMisses = 0`);
   **Given** changed inputs, **Then** a Miss + fresh, different scene equal to the memo-off build (C6/FR-007).

---

### User Story 3 - Memo metrics are observable over a host script (Priority: P2)

Over `Perf.runScript`, steady-state unchanged data accrues `MemoHitCount > 0` with `MemoMissCount = 0`;
perturbed inputs accrue `MemoMissCount`; an idle frame and a host with no memoizable control report `0/0`.

**Why this priority**: P2 — the observability guard. The reuse must be measurable and honest (0/0 when there
is nothing to memoize).

**Independent Test**: Run scripts and assert the four metric regimes (steady-state hits; perturbed misses;
idle 0/0; no-memoizable-control 0/0).

**Acceptance Scenarios**:

1. **Given** steady-state unchanged data, **When** scripted, **Then** `MemoHitCount > 0`, `MemoMissCount = 0`
   (SC-004).
2. **Given** perturbed inputs / an idle frame / no memoizable control, **When** scripted, **Then**
   `MemoMissCount > 0` / `0/0` / `0/0` respectively (C7/C8).

---

### User Story 4 - The stability diagnostic flags reuse-breaking inputs (Priority: P2)

The advisory `Diagnostics.stabilityReport` flags inputs that would silently break memo reuse: a per-frame
event closure, an always-new attribute value, or an unstable key on the same logical node. A
structurally-equal rebuild reports **no** findings.

**Why this priority**: P2 — the authoring guard that keeps memoization effective in practice (an unstable
input would turn every Hit into a Miss). Advisory, not enforced.

**Independent Test**: Build a tree twice; assert a stable rebuild reports no findings (FR-012); an injected
per-frame `onClick` closure is flagged exactly once (`Code = UnstableReuseInput`, the control id/kind/message
named); an always-new value attribute and an unstable key are each flagged.

**Acceptance Scenarios**:

1. **Given** a structurally-equal rebuild, **When** reported, **Then** no findings (FR-012).
2. **Given** a per-frame event closure / always-new value / unstable key, **When** reported, **Then** exactly
   the offending node is flagged `UnstableReuseInput` (FR-011).

---

### Edge Cases

- **Equal-but-distinct boxed dependencies**: Hit (structural equality, not object identity).
- **Never-seen `ControlId`**: cold Miss even with an equal dependency (per-id cache).
- **Disabled seam (`MemoEnabled = false`)**: `memoize` is bypassed entirely — both counters stay `0/0` (see
  the recorded finding above), scene byte-identical to memo-on.
- **Idle frame / no memoizable control**: `0/0`.
- **Changed real inputs**: Miss + fresh different scene (no staleness).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A `Miss` MUST never reuse across an unequal or unknown dependency (cold/changed ⇒ recompute).
- **FR-003 / FR-004**: The memo store MUST be carried frame-to-frame; a `Hit` MUST return the **same stored
  instance** as last frame.
- **FR-005**: Equality MUST be F# **structural** `=`, never object identity.
- **FR-006**: An equal dependency MUST guarantee a **byte-identical** projection (memo-on ≡ memo-off scene).
- **FR-007**: Any real input change MUST shift to a `Miss` and a fresh, different scene (no staleness).
- **FR-008**: The `MemoEnabled` switch MUST act as an always-miss / parity oracle (the rendered scene is
  byte-identical with the seam disabled). *(See the recorded narrative finding — the disabled path is a 0/0
  bypass, routed to E2.)*
- **FR-009 / FR-010**: Memoizable reuse outcomes MUST be observable as `MemoHits`/`MemoMisses` → public
  `MemoHitCount`/`MemoMissCount`; both `0` on a frame with no memoizable control.
- **FR-011**: A reuse-breaking input (per-frame event closure, always-new value, unstable key) MUST be flagged
  by `stabilityReport` as `UnstableReuseInput`, naming the offending node.
- **FR-012**: A structurally-equal rebuild MUST report **no** stability findings.
- **FR-013**: The backfill MUST add **zero new public-surface-baseline delta** (the seam is internal;
  `MemoHitCount`/`MemoMissCount` are additive on the already-baselined public `FrameMetrics`).

### Key Entities *(include if feature involves data)*

- **MemoOutcome**: `Hit | Miss` — the resolution of one `memoize` call.
- **MemoEntry**: `{ Dependency: obj; Subtree: Scene list }` — the stored dependency + reusable subtree (a Hit
  returns the same `Subtree` instance).
- **MemoCache**: `Map<ControlId, MemoEntry>` — the per-frame store, keyed by stable `ControlId`; an absent key
  is a cold miss.
- **memoize**: `id -> dependency:obj -> compute:(unit -> Scene list) -> cache -> Scene list * MemoCache * MemoOutcome`.
- **MemoEnabled**: the always-miss/parity oracle switch (internal).
- **MemoHits / MemoMisses → MemoHitCount / MemoMissCount**: the per-frame reuse counters (public via FrameMetrics).
- **Diagnostics.stabilityReport**: the advisory reuse-stability checker (`UnstableReuseInput` findings).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-002**: Every frame's scene is byte-identical seam-active vs forced always-miss, 100% of cases.
- **SC-004**: Steady-state unchanged data accrues `MemoHitCount > 0` with `MemoMissCount = 0`, 100% of cases.

## Assumptions

- The retained render structure with stable `ControlId`, the DataGrid row/column projection, and the public
  `FrameMetrics` already exist. 113 is the **backfilled contract** for the memoize seam + its metrics +
  stability diagnostic, not new-from-scratch construction.
- The seam is **internal**; "users" are framework internals plus the in-assembly tests. The only public touch
  is the additive `MemoHitCount`/`MemoMissCount` on an already-baselined type ⇒ **zero new** public-surface delta.
- 113 imported with executable suites (Controls.Tests + Elmish.Tests, headless, no FsCheck) but **no
  `readiness/`** (tests do not self-write); authoring readiness is part of this backfill.
- The `MemoEnabled` doc-comment overstates the disabled path (it is a 0/0 bypass, not "every node a miss");
  this is a **recorded finding routed to Workstream E2**, not fixed in this doc-only backfill.
- This is the **C6** conformance backfill; `/speckit-plan`/`/speckit-tasks`/`/speckit-implement` reduce to a
  conformance pass.
</content>
