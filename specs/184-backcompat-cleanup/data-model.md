# Phase 1 Data Model: Backward-Compatibility Shim Removal

This feature has no new runtime entities — it **removes** four. The "data model" is the ledger of each
deprecated identity (before → after), its consumers, and its modern replacement, as defined by the
spec's Key Entities.

---

## Entity: Deprecated identity (the four removals)

### US1 — `ScrollViewport.MaxOffset`

| Attribute | Value |
|---|---|
| Owning package | `FS.GG.UI.Controls` (`0.1.45-preview.1`) |
| Definition | `Control.fsi:283` / `Control.fs:3083` — `MaxOffset: float` (record field) |
| Public-surface status | **Public** (field of public type `ScrollViewport`) |
| Current consumers | 3 test-only readers; **no** src/sample/template reader |
| Modern replacement | `MaxVerticalOffset` (literal duplicate — `Control.fs:3326` sets `MaxOffset = extent.MaxVerticalOffset`) |
| Migration note | `MaxOffset` → `MaxVerticalOffset` (one-for-one) |

**Before → after (record):**
```fsharp
// before                              // after
{ …                                    { …
  MaxVerticalOffset: float               MaxVerticalOffset: float
  MaxOffset: float            // gone    …
  … }                                    … }
```
> Note: the sibling legacy `Offset: float` (mirrors `OffsetY`) is **out of scope** — the spec names
> only `MaxOffset`. Leave `Offset` unless a future item targets it.

### US2 — `Composition` legacy node-form layer

| Attribute | Value |
|---|---|
| Owning package | `FS.GG.UI.Controls` (`module internal Composition`) |
| Definition | `Composition.fsi:125-139` / `Composition.fs:367-399` |
| Public-surface status | **Internal** (not on any baseline) |
| Current consumers | 1 production caller (`Control.fs:2398-2402`, overlay) → migrated; Feature-140 legacy tests → deleted |
| Modern replacement | the in-memory modifier IR (`ModifierEntry`/`ModifierEffect`/`ModifierSource`) |
| Migration note | overlay path emits the literal `ModifierEntry` `legacyLower` used to produce |

**Removed identities:** `LegacyForm` (DU: `LegacyClipping`/`LegacyTranslation`/`LegacyPerspective`/
`LegacyCachedSubtree`/`LegacyText`/`LegacyOverlay`), `LegacyCompatibilityStatus` (DU:
`SupportedUnchanged`/`DeprecatedWithMigration`/`IntentionallyChanged`), `legacyLower`,
`compatibilityEvidence`.

**Retained (FR-010, live despite name):** `ModifierSource.LegacyOverlaySource` (overlay fingerprint
input) and the other `ModifierSource.Legacy*Source` cases (modern IR provenance; prune optional).

### US3 — `ControlEvent.Payload`

| Attribute | Value |
|---|---|
| Owning package | `FS.GG.UI.Controls` (`0.1.45-preview.1`); writers in `FS.GG.UI.Controls.Elmish` |
| Definition | `Types.fsi:312-322` / `Types.fs:252-257` — `Payload: string option` |
| Public-surface status | **Public** (field of public type `ControlEvent`) |
| Current consumers | ~7 src readers, dual-set writers (Elmish + OverlayState), 6 test readers |
| Modern replacement | `Nav: NavPayload option` — `SteppedValue of float` / `MovedSelection of int * string option` / `MovedCell of int * int` (`Types.fs:247-250`) |
| Migration note | read the typed `Nav` payload instead of parsing the string `Payload` |

**Before → after (record):**
```fsharp
type ControlEvent =                     type ControlEvent =
    { Kind: string                          { Kind: string
      ControlId: ControlId option             ControlId: ControlId option
      Origin: ControlEventOrigin              Origin: ControlEventOrigin
      Payload: string option   // gone        Nav: NavPayload option }
      Nav: NavPayload option }
```

**Reader-migration map (string `Payload` → typed `Nav`):**

| Reader (file:line) | Reads today | Typed source |
|---|---|---|
| `Interactive2.fs:6`, `Navigation2.fs:6`, `DataEntry2.fs:6` (`onPayload`) | `ev.Payload` string | `ev.Nav` (decode the relevant `NavPayload` case) |
| `Widgets/WidgetLowering.fs:21` (`onString`) | string | `Nav` → `MovedSelection`'s `item` / value-as-string |
| `Widgets/WidgetLowering.fs:26` (`onStringList`) | string | `Nav` typed list source |
| `Control.fs:3408/3412/3415` (`onChangedBool/Float/String`) | string parse | `Nav` → `SteppedValue` (float/bool) |
| `Control.fs:3503` (menu `onSelected`) | string item id | `Nav` → `MovedSelection(index, Some item)` |
| `Widgets/DataGridWidget.fs:40` (selection) | string cell id | `Nav` → `MovedCell(row, col)` |
| `Widgets/Containers.fs:59` (float parse) | string→float | `Nav` → `SteppedValue value` |

> **FSI-first (Principle I):** if several readers want the string form, introduce one typed accessor
> (e.g. `ControlEvent.navItem`/`navValue`) in the `.fsi` and route readers through it, rather than
> re-deriving strings at each site. Decide in US3's contract; keep the accessor typed, not stringly.

**Writers to stop dual-setting** (`Controls.Elmish/ControlsElmish.fs` `dispatchBindings`@426-427,
`dispatchNav`@941, `:558/863/954`; `OverlayState.fs:537`): construct `ControlEvent` with `Nav` only.

### US4 — Untyped flat-chart fallback

| Attribute | Value |
|---|---|
| Owning package | `FS.GG.UI.Controls` |
| Definition | `Control.fs:482-483` (fallback arms inside `chartValues`) |
| Public-surface status | **Internal** (branch in a helper) |
| Current consumers | **none** (zero flat-list authors in src/samples/template — research D4) |
| Modern replacement | typed front door `ChartSeries list` / `ChartPoint list` (`Control.fs:479-481`, `Charts.fs:19-20`) |
| Migration note | author charts with `LineChart.series`/`BarChart.series`/`PieChart.values` (already the only in-tree pattern) |

**Removed arms:**
```fsharp
| UntypedValue(:? (float list) as values) -> Some(indexed values)            // gone
| UntypedValue(:? (float array) as values) -> Some(indexed (Array.toList values)) // gone
```

---

## Entity: Consumer (classification drives free-delete vs migrate-then-delete)

| Story | production-src | test-only | sample | template | Verdict |
|---|---|---|---|---|---|
| US1 | 0 | 3 | 0 | 0 | **retarget tests, then delete** |
| US2 | 1 (overlay) | Feature-140 family | 0 | 0 | **migrate 1 caller, delete tests + layer** |
| US3 | ~7 | 6 | 0 | 0 | **migrate all readers + writers, then delete** |
| US4 | 0 | 1 | 0 | 0 | **delete test + branch (descope condition met)** |

---

## Entity: CompatibilityLedger entry (US1 + US3 only — the public removals)

`specs/184-backcompat-cleanup/readiness/compatibility-ledger.md`, format per
`specs/147-…/readiness/compatibility-ledger.md`:

- **US1** — `FS.GG.UI.Controls` `0.1.45 → 0.1.46`: removed `ScrollViewport.MaxOffset`; migrate to
  `MaxVerticalOffset` (identical value). Baseline `.txt` unchanged (type-granular); surface delta is
  the `Control.fsi` diff.
- **US3** — `FS.GG.UI.Controls` (same bump): removed `ControlEvent.Payload : string option`; read the
  typed `Nav : NavPayload option` instead. Surface delta is the `Types.fsi` diff.

> US2 and US4 are internal (Tier 2) → **no ledger entry, no bump** (research D1). Their FR-010
> retentions are recorded in `readiness/post-change/retentions.md` instead.
