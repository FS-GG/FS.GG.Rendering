# Phase 1 Data Model — Central Visual-State Style Resolver (F4)

The feature adds **no persisted data** and **no public type**. The "entities" below are the
inputs/outputs of the internal resolution path. Existing types are reused verbatim; only the
`IntentPolicy` seam is new (and internal).

## Reused entities (unchanged, from DesignSystem)

### `ResolvedStyle` — the concrete draw style (output)
`src/DesignSystem/Types.DesignSystem.fsi:57`
```fsharp
type ResolvedStyle =
    { Foreground: Color
      Fill: Color
      Stroke: Color
      StrokeWidth: float
      FontFamily: string option
      FontSize: float
      FontWeight: int option }
```
The single output of the resolution path. Byte-equality on this record (and on the emitted
`Scene`) is the parity oracle.

### `VisualState` — interaction/validation status (input)
`src/DesignSystem/Types.DesignSystem.fsi:15` — `Normal | Disabled | Hover | Pressed | Focused |
Selected | Loading | Validation of ValidationState`. Consumed by the back half unchanged.

### `StyleClass` / `StyleVariant` — class overlay (input, unchanged precedence)
`src/DesignSystem/Types.DesignSystem.fsi:45,32`. The back half folds these between base and
state. F4 does not change how classes compose; it only supplies the `baseStyle` they overlay.

### `Theme` — active palette (input, shape UNCHANGED — FR-012)
`src/DesignSystem/Types.DesignSystem.fsi:69`. F4 reads existing fields only (`Accent`,
`Background`, `Danger`, `Foreground`, `FontFamily`, … incl. `Success`/`Warning` from 125). **No
field added or removed.**

### `Style.resolve` — the back half (reused verbatim)
`src/DesignSystem/Style.fsi:30` —
`theme → baseStyle → classes → state → ResolvedStyle`. Precedence: `baseStyle < classes (attach
order) < visual state`. F4 calls this unchanged for the class+state overlay.

## Control-layer entities (reused, from Controls)

### `ButtonIntent` — semantic intent (input at the control boundary)
`src/Controls/Widgets/Primitives.fsi:6` — `Primary | Secondary | Danger | Ghost`. Lowered to a
string by `LegacyControls.intentStyle` (`Primitives.fs:48`). F4 does **not** change this type or
its lowering; it makes the lowered string **read** at the renderer.

### Control kind — string discriminator (input)
The render dispatch key (`Control.fs:1072`). F4 keys `baseStyleFor` on it: `"button"` (filled),
`"icon-button"` (outline), with a defined fallback for any other/unknown kind. No new enum.

## New entity (internal, no `.fsi`, no public surface)

### `IntentPolicy` — the overridable (kind, intent) → base mapping seam
Declared in `module internal StyleResolver` (`src/DesignSystem/StyleResolver.fs`):
```fsharp
type IntentPolicy =
    { /// theme → lowered-intent-string → structural base → adjusted base.
      ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }
```
| Field | Meaning | Default (`neutralPolicy`) | Divergent (test) |
|-------|---------|---------------------------|------------------|
| `ApplyIntent` | how an intent perturbs the kind's structural base | `fun _ _ s -> s` (identity — intent ignored ⇒ byte-identical) | maps `"danger"` to `{ s with Fill = theme.Danger; Stroke = theme.Danger; Foreground = theme.Background }` ⇒ differs from `Primary` |

**Validation rules / invariants**
- **Totality**: `baseStyleFor` is total over all `kind` strings (fallback = filled base); the full
  path raises no exception for any `(kind, intent, classes, state)` (FR-004, SC-003).
- **Determinism**: pure function of arguments — no clock/random/global state; same inputs → same
  `ResolvedStyle`.
- **Neutrality**: with `neutralPolicy`, `resolve` ≡ `Style.resolve theme (baseStyleFor theme kind)
  classes state` ≡ the pre-migration call (FR-003, SC-001).
- **Seam admits divergence**: with a non-default policy, ≥1 intent resolves to a style strictly
  different from `Primary`, reachable through the resolver alone (FR-005, SC-007).

## Resolution path (the single function)

```fsharp
// module internal StyleResolver  (src/DesignSystem/StyleResolver.fs, no .fsi)
val baseStyleFor : Theme -> kind: string -> ResolvedStyle           // front-half structural base
val neutralPolicy : IntentPolicy                                    // default, intent-agnostic
val resolve :
    policy: IntentPolicy ->
    theme: Theme ->
    kind: string ->
    intent: string ->
    classes: StyleClass list ->
    state: VisualState ->
        ResolvedStyle
// convenience: resolveDefault = resolve neutralPolicy   (what buttonGeom calls)
```
`resolve policy theme kind intent classes state =
   Style.resolve theme (policy.ApplyIntent theme intent (baseStyleFor theme kind)) classes state`

## Data-flow (button)

```
Button.view props
  └─ Attr.style (intentStyle props.Intent)         "primary"/"danger"/… (lowered; now READ)
        │  (attribute bus, keyed reconciler — unchanged)
        ▼
faithfulContent: kind=control.Kind, intent=textValueOf "style", classes, state
  └─ "button"/"icon-button" → buttonGeom theme box classes state kind intent label
        └─ StyleResolver.resolveDefault theme kind intent classes state
              ├─ baseStyleFor theme kind            (filled | outline structural base)
              ├─ neutralPolicy.ApplyIntent          (identity ⇒ intent ignored, default-neutral)
              └─ Style.resolve theme base classes state   (093 back half, unchanged precedence)
                    ▼
              ResolvedStyle → Scene (rectangle/stroke + text)   byte-identical to today
```
