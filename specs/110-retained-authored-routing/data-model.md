# Phase 1 — Data Model: Retained Pointer Routing → Authored Control ID (Feature 110)

The 110-in-scope entities. The routing functions are **assembly-internal**; the one public touch is the
additive `FrameMetrics.FullRenderFallbackCount` field on the already-baselined public `FrameMetrics` type.
All pure/total/deterministic.

## authoredControlIds (internal)

`boundIds: Set<ControlId> -> retained: RetainedRender<'msg> -> Map<RetainedId, ControlId>`. For each retained
node, the nearest ancestor (including self) that is keyed (`Key ?? path <> path`) OR whose canonical id is in
`boundIds` — reproducing `Control.nearestAuthored`'s climb (098) from retained identity. A node with no
authored ancestor has **no entry**. Pure / total / deterministic.

## routeRetainedInteraction / routeRetainedPointer (internal)

The routing entry points (in `Controls.Elmish`). Given an already-resolved `PointerInteraction` (or a raw
pointer sample), resolve the authored binding from the retained frame and dispatch — running no `host.View`
and no full render. Return a **fallback count**: `1` on an unresolvable bindable hit (the oracle fallback
fired), `0` on the normal path.

## FrameMetrics (public — additive field)

| Field | Type | Meaning |
|---|---|---|
| `FullRenderFallbackCount` | `int` | Feature 110: times retained routing fell back to a full render this frame; `0` for every normal scripted scenario (SC-005); `+1` on an unresolvable bindable hit (SC-006). |

110 also **narrows** the existing `FullRenderCount` / `ViewCalled` so retained routing increments neither
(FR-002/FR-008). No new public type.

## The oracle (preserved)

`host.View` + `Control.renderTree` + `Control.nearestAuthored` / `MapPointer`. Retained only as the parity
reference (FR-006) and the counted escape hatch (FR-007/FR-009).

## Reused (not owned by 110)

- **retainedHitTest** (092): point → `RetainedId`.
- **keyed-OR-in-`BoundIds` authored-id scheme + `MapPointer`** (098).
- **PointerMovesProcessed / coalescing infra**: ≤ 1 processed move per burst (FR-012, SC-009).

## Relationships

```text
raw pointer sample ──▶ retainedHitTest (092) ──▶ RetainedId
                                                    │
                          authoredControlIds(boundIds, retained) : Map<RetainedId, ControlId>
                                                    │
                       routeRetainedInteraction ───┼─ resolved ─▶ dispatch authored binding (no View, no full render; ViewCalled=false)
                                                    └─ unresolvable bindable hit ─▶ oracle full render + FullRenderFallbackCount += 1
                                                                                      (dispatch == oracle, FR-006)
```
</content>
