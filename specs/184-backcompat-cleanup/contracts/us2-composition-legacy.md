# Contract: US2 — Retire the `Composition` legacy node-form layer (Tier 2, FR-002)

## Scope
Remove the internal `LegacyForm` compatibility layer so each modifier has one expression, after
migrating the single overlay caller onto the modern modifier IR **byte-stably**.

## Edits
1. **Migrate the caller first** — `src/Controls/Control.fs:2398-2402`
   (`compositionEntriesForControl`): replace
   `Composition.legacyLower Composition.LegacyOverlay` with the literal it produces:
   ```fsharp
   [ { Composition.Source = Composition.LegacyOverlaySource
       Composition.Effect = Composition.LayerHint "overlay" } ]
   ```
2. Delete from `src/Controls/Composition.fsi:125-139` and `Composition.fs:367-399`: `LegacyForm`,
   `LegacyCompatibilityStatus`, `legacyLower`, `compatibilityEvidence`.
3. **Retain** (FR-010) `ModifierSource.LegacyOverlaySource` (and the other `Legacy*Source` cases —
   modern IR provenance; pruning them is an optional separate follow-up).
4. Delete `tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs` (asserts the removed lowering).
   Verify the other `Feature140*` tests (ZOrder, ModifierLayer, ModifierNormalization, PortalLayer,
   LegacyCacheTextOverlay) do **not** reference `LegacyForm`/`legacyLower`; if any do, migrate or delete
   the specific assertions (no weakening).

## Invariants
- **Byte-stable (I1):** the overlay node's normalized `ModifierEntry list` + `Composition.fingerprint`
  equal baseline. The migrated entry value is identical to `legacyLower LegacyOverlay`'s output.
- **Surface (I2):** **no public change** (`Composition` is `module internal`); baseline `.txt`
  unchanged; **no bump, no ledger entry** (Tier 2 — research D1).
- **Tests (I3):** legacy-lowering tests deleted; overlay behavior still covered by the modern
  Feature-140 modifier/portal/z-order tests.

## Acceptance (spec US2)
1. Overlay-lowering produces a byte-identical chain without any `legacy*` helper.
2. `LegacyForm`/`LegacyCompatibilityStatus`/`legacyLower`/`compatibilityEvidence` gone from
   `Composition` (internal) — *(note: never were on the public baseline; see research D1).*
3. Feature-140 legacy-compat tests deleted; retained overlay behavior covered on the modern path.
