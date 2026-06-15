# Phase 1 — Data Model: Visual-State Style Layer (Feature 093)

The styling vocabulary already ships on `src/Controls/Types.fsi`/`.fs` and `Style.fsi`/`.fs`. This
document records the entities, their fields/cases, validation rules drawn from the requirements, and
the fold semantics — as a contract, not a build instruction.

## Entities

### VisualState  (`Types.fs:184-192`)

The control's current interaction/render state consumed by the resolver. A closed DU of eight cases:

| Case | Meaning | Resolver behavior |
|---|---|---|
| `Normal` | resting | identity (no delta) — `resolve … [] Normal = base` exactly |
| `Disabled` | non-interactive | muted token paint; wins over any class on shared fields |
| `Hover` | pointer over | distinct token-derived delta |
| `Pressed` | active press | distinct token-derived delta |
| `Focused` | keyboard/focus ring | distinct token-derived delta |
| `Selected` | selected item | distinct token-derived delta |
| `Loading` | busy | **inherits `Normal`'s paint** (parity preservation, FR-005) |
| `Validation of ValidationState` | validation feedback | delegated to `applyValidation` |

**Rule**: total over all eight cases (FR-004); the differentiated states are pairwise distinguishable
while `Loading == Normal` (SC-002).

### ValidationState  (`Types.fs:179-182`)

Carried by `VisualState.Validation`. Three cases: `Valid`, `Invalid of string`, `Pending of string`.
The carried strings are diagnostic messages; the resolver maps the validation *kind* to a token-
derived paint via `applyValidation` (`Style.fs:65`).

### StyleVariant  (`Types.fs:195-201`)

The closed, typed set of built-in semantic variants — `[<RequireQualifiedAccess>]` DU of six cases:
`Primary`, `Danger`, `Ghost`, `Neutral`, `Success`, `Warning`.

**Rules**: `applyVariant` (`Style.fs:26`) is a **total match** over all six (FR-002, no unmatched
case); `Primary` derives from the accent family, `Danger` from the danger family (SC-001); the six
yield pairwise-distinguishable results on one theme (SC-001).

### StyleClass  (`Types.fs:203-205`)

One attached class entry. Two cases:

- `Variant of StyleVariant` — a typed built-in variant.
- `Custom of string` — a free-form, consumer-defined class name.

A control carries a `StyleClass list` **in attach order** (list position is attach order). An unknown
`Custom` name resolves to an **identity delta** (`applyCustom`, `Style.fs:44`) — never throws, never
drops a field (FR-004, edge case).

### ResolvedStyle  (`Types.fs:207-214`)

The flat per-control output record — paint and typography only:

| Field | Type | Notes |
|---|---|---|
| `Foreground` | `Color` | text/icon colour |
| `Fill` | `Color` | background/fill |
| `Stroke` | `Color` | border/outline |
| `StrokeWidth` | `float` | border width |
| `FontFamily` | `string option` | `None` = inherit |
| `FontSize` | `float` | |
| `FontWeight` | `int option` | `None` = inherit |

**Rules**: geometry is deliberately excluded (D3) so the migration is additive and parity is a plain
structural record comparison; last-writer-wins is a per-field record update. Declared on `Types.fsi`
**before** `Theme` so overlapping bare field names resolve to `Theme` at render sites (D7).

### Theme  (existing)

The active DTCG-generated token palette/metrics. Every colour a `ResolvedStyle` carries from the
variant/state layers originates here (`theme.Accent`/`Danger`/`Muted`/`Background`/`Foreground` and
`DesignTokens.*`) — no inline literals (FR-008, SC-006).

### Style.resolve  (`Style.fs:83-86`)

The single pure/total/deterministic function
`theme → baseStyle → classes → state → ResolvedStyle`. Composed of `private` helpers `isDark`,
`successColor`, `warningColor`, `applyVariant`, `applyCustom`, `applyClass`, `applyValidation`,
`applyState`.

## Carriers (attributes)  (`Attributes.fs:71-72`)

Styling intent rides a control through two public attribute builders:

- `Attr.styleClasses : StyleClass list -> Attr<'msg>` → `AttrValue.StyleClassesValue`.
- `Attr.visualState : VisualState -> Attr<'msg>` → `AttrValue.VisualStateValue`.

These carry the `(classes, state)` the resolver consumes; because they ride the control's attributes,
the visual state survives a keyed-reconciler re-render (US3, delegated to feature 092).

## Fold semantics (precedence)

```
resolve theme baseStyle classes state =
    baseStyle
    |> (fold each class left-to-right: applyClass theme cls)   // earlier < later
    |> applyState theme state                                   // state is outermost
```

Precedence, last-writer-wins per `ResolvedStyle` field (FR-003):

```
baseStyle  <  class[0]  <  class[1]  < … <  class[n-1]  <  visualState
```

Invariants the suites pin:
- **Identity**: `resolve theme base [] Normal = base` exactly (SC-003/SC-004).
- **Outermost state**: applying classes then the state equals re-resolving the class-folded style
  under that state with no classes (SC-004, ≥1000 cases).
- **Purity/determinism**: identical inputs → identical output (SC-004).
- **Totality**: returns a `ResolvedStyle` for every `(theme, base, classes, state)` without throwing
  (FR-002/FR-004, SC-001/SC-004).

## State transitions

`Style.resolve` is a stateless pure fold — it has no state machine of its own. The only "transition"
relevant to 093 is the survival of a control's `VisualState` across an unrelated re-render, which is
owned by feature 092's `RetainedRender` MVU boundary and merely *consumed* here (US3/SC-005).
