# Contract: Removal Invariance (cross-story, binding)

The oracle every story is gated on. Maps FR-005 (byte-stable retained path), FR-006 (intentional/exact
surface), FR-008 (no weakened tests), FR-011 (same red/green).

## I1 — Byte-stable retained production path (FR-005)

For any removal that touches a production code path, the **retained** path's output MUST be byte-identical
to a baseline captured immediately before the edit.

- **US2 (overlay):** the `ModifierEntry list` for an overlay node, its `Composition.normalize` result,
  and `Composition.fingerprint` MUST equal baseline. Concretely the migrated entry is
  `{ Source = LegacyOverlaySource; Effect = LayerHint "overlay" }` — the exact value
  `legacyLower LegacyOverlay` produced. Captured via a focused test that builds an overlay control and
  compares the normalized chain + fingerprint to the pre-edit snapshot.
- **US4 (chart):** `chartValues` output (the `ChartPoint list`) for every typed-front-door chart in the
  test corpus MUST equal baseline. Captured by the existing chart extraction/render tests staying green
  plus a value-level diff for the typed arms.
- **US1, US3:** US1 removes a read-only duplicate field (no production path computes from it). US3
  changes which field handlers read, but the dispatched **behavior** (the moved item/value reaching the
  handler) MUST be unchanged — asserted by the migrated event/widget/navigation tests.

**Gate:** any drift blocks the removal — narrow or descope (per spec Edge Cases / Feature-182 FR-009
precedent), never ship drift.

## I2 — Intentional, exact public-surface diff (FR-006)

- **US1:** `git diff src/Controls/Control.fsi` shows **only** the `MaxOffset: float` line removed.
- **US3:** `git diff src/Controls/Types.fsi` shows **only** the `Payload: string option` line removed
  (plus any new typed accessor `val` deliberately added per US3's contract).
- **US2, US4:** **no** public `.fsi` change (internal). `Composition.fsi` (internal module) loses the
  4 legacy identities; `Control.fs` loses 2 chart arms; no public signature moves.
- **All:** `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff
  readiness/surface-baselines/` MUST be **empty** (type-granular oracle — research D2). A non-empty
  baseline diff means an unintended type-level change → blocks the story.

## I3 — No weakened tests; removed-behavior tests deleted (FR-008)

- Delete (not loosen): `Feature140LegacyCompatibilityTests.fs` (US2), the
  `Feature080ExtractionTests.fs:62-71` flat-list test (US4), and any assertion that the removed
  `MaxOffset`/`Payload` exists.
- Retarget/migrate (keep coverage): the 3 `MaxOffset` readers → `MaxVerticalOffset` (US1); the 6
  `Payload` test readers → `Nav` (US3).
- No assertion may be weakened to green the build; narrow scope and document instead.

## I4 — Same red/green as baseline (FR-011 / SC-004)

Post-feature full sweep (`dotnet fsi scripts/baseline-tests.fsx --config Release`) MUST reproduce the
baseline set **exactly**: `Package.Tests` 8-fail + `ControlsGallery` 2-fail (stale-feed,
pre-existing), all 14 other `*.Tests.fsproj` green. No new red, no flipped green. After the bump +
`dev-repack`, the re-pinned sample (`SecondAntShowcase`) passes against the freshly-packed package.
