# Contract: Named Flag Records (US3 / FR-004) — Tier 1, bump Scene + SkiaViewer

## Invariant

The positional `bool` / positional-tail parameters of six functions are replaced by small **named**
records so each flag is named at the call site (a transposition becomes a compile error), while the
**values passed and the results produced are unchanged** (byte-identical verdicts/regions/decisions).

## Function ledger (public/internal split drives the bumps)

| Function | Pkg | Vis. | Record | Bump | Call sites to update |
|---|---|---|---|---|---|
| `validateDamage` (`OpenGl.fs:522`/`.fsi:299`) | SkiaViewer | **public** | `DamageValidationFlags` (5 bools) | **SkiaViewer** | `OpenGl.fs:562` (internal) |
| `classifyWindowObservation` (`SkiaViewer.fs:935`/`.fsi:118`) | SkiaViewer | **public** | `WindowObservationInputs` (2 bool + 2 bool option) | **SkiaViewer** | tests only (`SkiaViewer.Tests/Tests.fs:399,444`) |
| `damageRegion` (`Scene.fs:2000`/`.fsi:1276`) | Scene | **public** | `DamageNodeCounts` (3 int counters) | **Scene** | **cross-package** `Controls/Inspection.fs:460` + ~6 test files |
| `promotionDecision` (`RetainedRender.fs:768`/`.fsi:618`) | Controls | `internal` | `PromotionInputs` | no public bump | tests (`Feature147/148/149*`) |
| `damageRegionSet` (`RetainedRender.fs:731`/`.fsi:593`) | Controls | `internal` | `DamageSetInputs` | no public bump | `RetainedRender.fs:756` + tests |
| `popoverGeom` (`Control.fs:1755`) | Controls | `private` | small record / `PopoverKind` DU | none | `Control.fs:2009,2010,2011` |

## Must hold

1. **Identical results.** For fixed inputs each converted function returns byte-identical output (damage
   validation verdict, window observation, damage region, promotion decision, popover geometry) —
   behavior-invariance §A.4. The records only rename arguments.
2. **Call-site lockstep.** Every call site (including the **cross-package** `Controls/Inspection.fs:460`
   for `damageRegion`, and all test sites) compiles and passes unchanged. `damageRegion`'s caller updates
   to the new record but `FS.GG.UI.Controls` public surface is unchanged → Controls is **not** bumped.
3. **Exact surface diff.** `OpenGl.fsi`/`SkiaViewer.fsi`/`Scene.fsi` show only the planned signatures +
   the new public record types; `FS.GG.UI.SkiaViewer.txt`/`FS.GG.UI.Scene.txt` gain only those type
   names; Controls baseline unchanged (behavior-invariance §B). The `internal`/`private` records do not
   touch any public baseline.
4. **Bumps.** `FS.GG.UI.SkiaViewer` and `FS.GG.UI.Scene` bumped (Scene shared with US2); feed/samples/
   template aligned in polish (FR-007).

## Notes / FR-010 triggers

- `damageRegion` has no bools but 3 adjacent transposable `int` counters — the **minimal** fix groups
  just those into `DamageNodeCounts`, leaving ids/cause/threshold as named params. If grouping ripples
  into evidence-artifact shape (changing emitted JSON/markdown), prefer the smaller change or retain and
  record why (byte-stability wins).
- `popoverGeom` is private and internal to `Control.fs`; a 2-case `PopoverKind` DU (`Plain | WithActions`)
  is the idiomatic option but a `{ WithActions: bool }` record is acceptable — pick the clearer at the
  3 call sites.
