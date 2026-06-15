# Phase 0 — Research: Virtualization Counts & Overscan (Feature 114)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `RetainedRender.fs` (`countVirtual`), `Collections`, `Types`, `ControlsElmish`, and the
five suites.

## Decision 1 — A read-only count walk, render output unchanged

- **Decision**: `countVirtual` walks the lowered tree and tallies materialized `data-grid-row` nodes
  (`VirtualMaterialized`) and the sum of each `data-grid`'s logical `Total` (`VirtualTotal`). It is read-only;
  the render output is unchanged.
- **Rationale**: Observability must not perturb rendering. A pure post-build walk yields the counts without
  touching the scene.
- **Alternatives considered**: Counting during the build — rejected: entangles measurement with construction
  and risks changing output.

## Decision 2 — Materialization is bounded by `visible + 2 × overscan`, never the total

- **Decision**: The realized window is `visibleCount + 2 × overscan`, edge-clamped; the materialized count is
  identical across 100/1000/10000 rows at a fixed window + overscan (FR-003/FR-014).
- **Rationale**: Bounded work independent of data size is the entire value of virtualization; the
  non-scaling property is the headline guarantee.
- **Alternatives considered**: Materializing a fixed fraction of the total — rejected: scales with data,
  defeating virtualization.

## Decision 3 — Overscan default 0 is byte-identical; opt-in adds only real rows

- **Decision**: `Overscan` defaults to 0 (the historic visible slice, byte-identical); a positive value adds
  only real, contiguous, edge-clamped adjacent rows; negative clamps to 0. `visibleRange` takes overscan as
  an additive trailing parameter.
- **Rationale**: The feature must be a no-op by default (no regression) and add only genuine rows when opted
  in — never fabricated or out-of-range rows.
- **Alternatives considered**: Always applying a default overscan — rejected: changes existing output; opt-in
  preserves byte-identity (FR-006/SC-002).

## Decision 4 — Offscreen rows are logically addressable without materializing

- **Decision**: Select/toggle/focus/relocate on an offscreen row updates the logical model
  (`DataGrid.update`) without materializing the path; a boundary-crossing relocate lands on the correct next
  logical row and advances the window (relocate, not expand — O4).
- **Rationale**: Virtualization must not make offscreen content unreachable; logical addressing keeps the
  full model interactive while only the window is realized.
- **Alternatives considered**: Materializing the target to address it — rejected: would break the bound and
  the non-scaling guarantee.

## Decision 5 — Accessibility reports logical total + position

- **Decision**: `CollectionPosition { TotalItems; FocusedIndex }` on `AccessibilityMetadata.Collection`
  reports the logical values, computed from the model, never the realized slice; non-collection controls
  report `Collection = None` (at-rest a11y byte-identical).
- **Rationale**: Assistive tech must hear the true size/position, not the windowed subset. `None` for
  non-collections keeps existing controls' a11y unchanged.
- **Alternatives considered**: Reporting the realized slice size — rejected: misreports the collection to a11y.

## Renderer-mode / evidence honesty

All proofs are deterministic and headless (counts, byte-identity of overscan-0, logical-model assertions,
metric regimes). "Offscreen" is logically-off-window, **not** GL-offscreen — no GL gating. Readiness
(authored in `/speckit-implement`, since 114 imported without it) makes no pixel/desktop claim.
</content>
