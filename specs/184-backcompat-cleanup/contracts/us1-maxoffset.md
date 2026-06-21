# Contract: US1 — Remove `ScrollViewport.MaxOffset` (Tier 1, FR-001)

## Scope
Remove the vertical-compatibility alias `MaxOffset` so the vertical scroll maximum has exactly one
field, `MaxVerticalOffset`.

## Edits
1. `src/Controls/Control.fsi:283` — delete `MaxOffset: float` from the `ScrollViewport` record.
2. `src/Controls/Control.fs:3083` — delete the field; `Control.fs:3326` — delete the
   `MaxOffset = extent.MaxVerticalOffset` assignment.
3. Update the doc-comment at `Control.fsi:272-273` (drop the `MaxOffset` mention; keep `Offset`).
4. Retarget the 3 test readers to `MaxVerticalOffset`:
   - `tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs:16`
   - `tests/Controls.Tests/Feature151ScrollViewerCorpusTests.fs:36`
   - `tests/Controls.Tests/Feature137ClippingTests.fs:162`

## Invariants
- **Surface (I2):** `Control.fsi` diff = only the `MaxOffset` line; baseline `.txt` unchanged.
- **Behavior:** none computed from `MaxOffset` (read-only duplicate) — tests still assert the vertical
  max via `MaxVerticalOffset`.
- **Bump:** `FS.GG.UI.Controls` (combined with US3 in Polish) + ledger entry.

## Acceptance (spec US1)
1. Exactly one field (`MaxVerticalOffset`) exposes the vertical max; no `MaxOffset` on the surface.
2. `Control.fsi` updated; package bumped; ledger records `MaxOffset → MaxVerticalOffset`.
3. Samples + template rebuild (none referenced `MaxOffset`) and pass.
