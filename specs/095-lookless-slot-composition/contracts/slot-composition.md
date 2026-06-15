# Contract: Slot Composition Seam + Typed Front Door (Feature 095)

The interface this feature exposes is split in two: a **closed, typed front door** (the only
consumer-facing authoring path) and an **internal slot seam** (reachable from `Controls.Tests` via
`InternalsVisibleTo`). The **only public-surface entry** is the `SlotFillsValue` case, already
committed in `tests/surface-baselines/FS.GG.UI.Controls.txt`. This contract is what the
`Feature095SlotCompositionTests` suite pins.

## 1. Public surface (committed; zero new delta)

```fsharp
// Types.fsi — on the already-public AttrValue<'msg>
type AttrValue<'msg> =
    | ...
    | SlotFillsValue of (string * Control<'msg>) list   // line 415

type AttrCategory =
    | ...
    | Slot                                               // line 375
```

**Contract**: `SlotFillsValue` is the surface entry; everything else below is `internal` or `private`.
The surface-drift check (`tests/surface-baselines/FS.GG.UI.Controls.txt`) MUST pass **unchanged**.

## 2. Typed front door (consumer authoring path — the only one)

```fsharp
// Widgets/Primitives.fsi
type ButtonProps<'msg> =
    { ...
      Leading: Widget<'msg> option
      Trailing: Widget<'msg> option }

// Widgets/Containers.fsi
type PanelProps<'msg> =
    { ...
      Header: Widget<'msg> option
      Footer: Widget<'msg> option }
```

**Contract**:
- Filling a typed field builds the `Slot` carrier internally via `ControlInternals.slotFill`; the
  consumer never names a slot string.
- A `None` field contributes **no** fill entry. An all-`None` control builds **no** `Slot` attribute
  (→ unfilled identity path).
- There is **no** public free-form slot builder, `Attr.slot`, or consumer slot-name string (FR-001,
  SC-006). The `typedClosure` test list verifies this against the public surface.

## 3. Internal slot seam (`Control.fsi`; `InternalsVisibleTo("Controls.Tests")`)

```fsharp
module ControlInternals =
    val slotFill    : fills: (string * Control<'msg>) list -> Attr<'msg>
    val slotFillsOf : attrs: Attr<'msg> list -> (string * Control<'msg>) list
    val slotFor     : name: string -> attrs: Attr<'msg> list -> Control<'msg> option
    val lowerSlots  : control: Control<'msg> -> Control<'msg>
```

### `slotFill`
- **Contract**: builds the single `Slot`-category `Attr` carrying `SlotFillsValue fills`. The only
  producer of a `Slot` attribute.

### `slotFillsOf`
- **Contract**: returns the full ordered fill list, or `[]` when no `Slot` attribute is present. A
  control carries **at most one** `Slot` attribute; **last-writer-wins** if more than one.

### `slotFor name`
- **Contract**: `Some` for a region present in the list (**including a present-but-empty fill**);
  `None` for an absent region. **Absent ≠ empty.**

### `lowerSlots`
- **Contract**:
  1. **Identity** — no `Slot` attribute ⇒ returns the control **verbatim / byte-identical** (FR-003,
     SC-002).
  2. **Injection + order** — places each present region's fill into `Children` ordered
     `[leading regions; intrinsic children; trailing regions]`, per the kind's `slotRegions`
     partition (FR-004, SC-001).
  3. **Consumption** — removes the `Slot` carrier from `Attributes`; no residue (FR-004).
  4. **Scoped** — only `button` and `panel` declare regions; any other kind picks nothing and is
     unaffected (FR-007, SC-007).
  5. **Pure / total / deterministic** — no clock/randomness/I/O; identical `(kind, fills)` → identical
     output; **never throws** for any combination (FR-006, SC-005).
  6. **E1–E4 by construction** — fills are ordinary `Children`, so they inherit flat per-`ControlId`
     dispatch (E1), retained identity (E2), the feature-093 resolver (E3), and focus routing (E4),
     with no slot-specific special-casing (FR-004, FR-005, SC-003, SC-004).

## 4. Conformance mapping (contract clause → success criterion → test list)

| Contract clause | SC | Test list (`Feature095SlotCompositionTests`) |
|---|---|---|
| Injection + order + `slotFor` absent≠empty | SC-001 | `slotPlacement` |
| Unfilled byte-identity + frozen-scene parity (both themes); panel==legacy; CheckBox unchanged | SC-002, SC-007 | `unfilledParity` |
| E1 dispatch / E3 style / E4 focus on slotted content | SC-003 | `compose` |
| E2 retained identity via live `RetainedRender` | SC-004 | `retainedIdentity` |
| Purity / determinism / totality ≥1000 cases + no-slot identity | SC-005 | `loweringProperties` (FsCheck `Gen095`) |
| No public free-form slot path | SC-006 | `typedClosure` |
| Frozen-oracle evidence capture | SC-002 | `evidence` (writes `readiness/parity/*.scene.txt`) |

## 5. Non-goals (permanent, not deferrals)

- No data-bound slot templates or deferred expressions — fills are static `Control<'msg>` (FR-008).
- No selector matching, specificity, or cascade — slot composition is single-control structural
  injection only; styling is feature 093, runtime state is feature 096.
