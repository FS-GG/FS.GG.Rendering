# Contract — Retained Pointer Routing → Authored Control ID (Feature 110)

The **internal** routing seam the suites pin (reached via `InternalsVisibleTo`; routing IS the production
pointer path), plus the one additive public `FrameMetrics` field. Signatures from `RetainedRender.fsi` /
`ControlsElmish.fsi`; behaviour clauses are what the three `Feature110*Tests` suites assert.

## C1 — `authoredControlIds` (the retained-id → authored-binding lookup)

```fsharp
val internal authoredControlIds:
    boundIds: Set<ControlId> -> retained: RetainedRender<'msg> -> Map<RetainedId, ControlId>
```

- Each node maps to the nearest ancestor (incl. self) that is keyed OR whose canonical id is in `boundIds` —
  reproducing `Control.nearestAuthored`'s climb (098) from retained identity.
- A node with no authored ancestor has **no entry**. Pure / total / deterministic.

*Pins*: FR-003, FR-005, FR-006. *Used by*: US1, US2.

## C2 — `routeRetainedInteraction` / `routeRetainedPointer` (the routing entry points)

```fsharp
val internal routeRetainedInteraction: (* resolve + dispatch a PointerInteraction from the retained frame *)
val internal routeRetainedPointer:     (* route a raw pointer sample from the retained frame *)
```

- MUST run **no** `host.View` and **no** `Control.renderTree` (`ViewCalled = false`, `FullRenderCount = 0`).
- MUST dispatch the message list the oracle would (FR-006), including `MapPointer` fallback + focus identity.
- MUST coalesce move bursts to ≤ 1 processed move (FR-012).
- Return a fallback count: `1` on an unresolvable bindable hit (the oracle fired), else `0`.

*Pins*: FR-001, FR-002, FR-004, FR-006, FR-008, FR-012. *Used by*: US1, US2, US3.

## C3 — `FrameMetrics.FullRenderFallbackCount` (the counted escape hatch — public, additive)

```fsharp
// additive field on the already-public FrameMetrics record:
FullRenderFallbackCount: int
```

- `0` for every normal scripted pointer scenario, every frame (SC-005).
- `+1` (exactly one) on an unresolvable bindable hit, whose fallback dispatch matches the oracle (SC-006).

*Pins*: FR-007, FR-009. *Used by*: US3.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-013): `authoredControlIds`/`routeRetained*` are `internal`;
  `FullRenderFallbackCount` is an additive field on the already-baselined public `FrameMetrics` (the baseline
  is type-granular). `FS.GG.UI.Controls.txt` / `FS.GG.UI.Controls.Elmish.txt` stay byte-unchanged.
</content>
