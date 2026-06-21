# Contract: US3 — Retire `ControlEvent.Payload` (Tier 1, FR-003)

## Scope
Remove the stringly-typed `Payload : string option` so control events have one typed representation
(`Nav : NavPayload option`), after migrating every reader and stopping the dual-set writers.

## Edits (migrate readers + writers BEFORE removing the field)
1. **Decide the typed accessor (FSI-first).** If multiple readers need the same projection, add one
   typed accessor to `Types.fsi` (e.g. `val navValue: ControlEvent -> float option`,
   `val navItem: ControlEvent -> string option`) backed by `Nav`; otherwise readers decode `ev.Nav`
   directly. Keep it typed — never reconstruct a string.
2. **Readers → `Nav`** (data-model.md reader map): `Interactive2.fs:6`, `Navigation2.fs:6`,
   `DataEntry2.fs:6`, `Widgets/WidgetLowering.fs:21/26`, `Control.fs:3408/3412/3415/3503`,
   `Widgets/DataGridWidget.fs:40`, `Widgets/Containers.fs:59`.
3. **Writers stop dual-setting** — `Controls.Elmish/ControlsElmish.fs` `dispatchBindings`@426-427,
   `dispatchNav`@941, `:558/863/954`; `OverlayState.fs:537`: construct `ControlEvent` with `Nav` only.
4. **Remove the field** — `Types.fsi:312-322` and `Types.fs:252-257`: delete `Payload: string option`.
5. **Migrate the 6 test readers** to `Nav`: `TypedMigrationTests.fs:337/357`,
   `Feature100NavigationTests.fs:113/177/197`, `Feature144ProductOwnedVisibilityTests.fs:23`.

## Invariants
- **Behavior (I1):** the moved item / stepped value / cell move reaching each handler is unchanged —
  asserted by the migrated event/widget/navigation tests at baseline parity.
- **Surface (I2):** `Types.fsi` diff = only the `Payload` line removed (+ any deliberate accessor
  `val`); baseline `.txt` unchanged. `Controls.Elmish` public `.fsi` unchanged (writers are internal)
  → recompile + re-pin, no Elmish bump.
- **Bump:** `FS.GG.UI.Controls` (combined with US1) + ledger entry (string `Payload` → typed `Nav`).

## Acceptance (spec US3)
1. Handlers read moved item/value from the typed payload; no code reads a string `Payload`.
2. `Payload` removed from the `ControlEvent` surface; package bumped; ledger documents the typed
   replacement.
3. Samples + template rebuild and pass (none read `Payload`; verified by FR-009 scan).
