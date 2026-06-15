# Phase 1 Data Model: Lookless Slot Composition (Feature 095)

The "data" here is the slot vocabulary and the lowering semantics — all already declared in the
source. This document records the entities, their fields/relationships, validation rules, and the
lowering's state transition, cross-referenced to the contract in `contracts/slot-composition.md`.

## Entities

### `AttrCategory.Slot` (discriminant)

- **Where**: `src/Controls/Types.fsi:375` (`| Slot`).
- **Role**: marks an `Attr<'msg>` as the slot carrier. Used by the readers to find the lone slot
  attribute and by `lowerSlots` to consume it.
- **Cardinality**: a control carries **at most one** `Slot`-category attribute. When more than one is
  present, the attribute readers' last-writer-wins behavior selects the last (FR-002).

### `SlotFillsValue` (the public-surface carrier)

- **Where**: `src/Controls/Types.fsi:415` —
  `| SlotFillsValue of (string * Control<'msg>) list`, a case on the already-public `AttrValue<'msg>`.
- **Fields**: an **ordered association list** from region name (`string`, internal wire projection)
  to fill sub-tree (`Control<'msg>`).
- **Validation rules**:
  - A region **absent** from the list is unfilled (default chrome) ⇒ `slotFor name = None`.
  - A region **present** is filled, **even when the fill sub-tree is empty** ⇒ `slotFor name = Some _`.
    **Absent ≠ empty.**
  - Order is significant only relative to a kind's declared region order (lowering re-picks by
    `slotRegions`, so author insertion order does not reorder placement).
- **Surface note**: this case is the feature's **only** public-surface entry, already committed in
  `tests/surface-baselines/FS.GG.UI.Controls.txt` (`FS.GG.UI.Controls.AttrValue\`1+SlotFillsValue`).
  Backfilling the spec adds **zero** new baseline delta.
- **Mapping**: `mapControl` recurses fills under `f` (`Control.fs:1499`), so `Control.map` carries
  slot fills through a message-type change like any other child.

### `SlotName` (internal region enum)

- **Where**: `src/Controls/Control.fs:133-136` (private DU): `Leading | Trailing | Header | Footer`.
- **`slotName` projection** (`:138`): `Leading→"leading"`, `Trailing→"trailing"`, `Header→"header"`,
  `Footer→"footer"` — the **only** place a region becomes a string, at the lowering edge.
- **Visibility**: private (no `.fsi` entry); never reaches a consumer.

### Typed slot props (the front door)

- **`ButtonProps.Leading` / `Trailing`**: `Widgets/Primitives.fsi:38-39`,
  `Widget<'msg> option`; defaulted `None` (`Primitives.fs:80-81`); lowered to
  `("leading"/"trailing", Widget.toControl w)` and `ControlInternals.slotFill` (`:88-108`).
- **`PanelProps.Header` / `Footer`**: `Widgets/Containers.fsi:30-31`, `Widget<'msg> option`;
  defaulted `None` (`Containers.fs:119`); lowered to `("header"/"footer", …)` + `slotFill`
  (`:126-137`).
- **Validation rule**: a `None` field contributes no entry to the fills list (so an all-`None`
  control builds **no** `Slot` attribute → the unfilled identity path); a `Some w` field contributes
  one entry.

### `slotRegions` (per-kind declared regions)

- **Where**: `src/Controls/Control.fs:148` (private). `"button" → ([Leading],[Trailing])`,
  `"panel" → ([Header],[Footer])`, `_ → ([],[])`.
- **Role**: defines which regions a kind exposes and partitions them into leading (before intrinsic
  children) and trailing (after). The `_ → ([],[])` default is what makes lowering **total** and
  exposure **scoped** (FR-007).

## Readers

| Reader | Signature (`Control.fsi`) | Returns |
|---|---|---|
| `slotFillsOf` | `attrs: Attr<'msg> list -> (string * Control<'msg>) list` | the full ordered fill list (`[]` when no slot attribute) |
| `slotFor` | `name: string -> attrs: Attr<'msg> list -> Control<'msg> option` | `Some` for a present region (incl. present-but-empty); `None` for absent — **absent ≠ empty** |

## State transition: `lowerSlots`

`lowerSlots : Control<'msg> -> Control<'msg>` (`Control.fsi`; impl `Control.fs:163`).

```
lowerSlots control =
  match slotFillsOf control.Attributes with
  | []    -> control                                  // identity fast path (FR-003 / SC-002)
  | fills ->
      let pick names = names |> choose (region present in fills, in declared order)
      let leadingNames, trailingNames = slotRegions control.Kind
      { control with
          Attributes = control.Attributes without the Slot carrier   // consume (FR-004)
          Children   = pick leadingNames @ control.Children @ pick trailingNames }  // [leading; intrinsic; trailing]
```

**Properties** (pinned by the suite):
- **Identity** — no `Slot` attribute ⇒ returns `control` referentially unchanged (byte-identity).
- **Ordering** — children become `[leading regions; intrinsic; trailing regions]`; never swapped.
- **Carrier consumption** — the `Slot` attribute is filtered out; no residue.
- **Totality** — every kind resolves a (possibly empty) region pair; absent regions pick nothing;
  never throws for any `(kind, fills)`.
- **Purity/determinism** — no clock/randomness/I/O; identical inputs → identical output.

## Relationships to other features

- **E1 (flat dispatch)**, **E2 (retained identity)**, **E3 (feature-093 `Style.resolve`)**, **E4
  (focus/tab routing)**: inherited by the lowered fills **by construction** because they are ordinary
  `Children` — 095 adds no slot-specific handling for any of them (FR-004/FR-005).
- **Feature 093**: 095 composes *with* the visual-state resolver for styling slotted content; it adds
  no styling of its own.
- **Feature 096**: runtime visual-state derivation is out of 095's scope; 095 is structure-only.

See `contracts/slot-composition.md` for the authoritative seam + front-door contract the suite pins.
