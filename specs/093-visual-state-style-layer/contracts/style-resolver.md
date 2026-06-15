# Contract — Style Resolver (Feature 093)

The public surface this feature pins. All entries already exist in
`tests/surface-baselines/FS.GG.UI.Controls.txt`; this contract is the human-readable statement of
what the four semantic suites enforce. Backfilling it adds **zero** surface-baseline delta.

## Public surface

Namespace `FS.GG.UI.Controls`.

### `Style.resolve`  (`Style.fsi`)

```fsharp
val resolve:
    theme: Theme ->
    baseStyle: ResolvedStyle ->
    classes: StyleClass list ->
    state: VisualState ->
        ResolvedStyle
```

### Styling types  (`Types.fsi`)

```fsharp
type ValidationState = Valid | Invalid of string | Pending of string

type VisualState =
    | Normal | Disabled | Hover | Pressed | Focused | Selected | Loading
    | Validation of ValidationState

[<RequireQualifiedAccess>]
type StyleVariant = Primary | Danger | Ghost | Neutral | Success | Warning

type StyleClass = Variant of StyleVariant | Custom of string

type ResolvedStyle =
    { Foreground: Color; Fill: Color; Stroke: Color; StrokeWidth: float
      FontFamily: string option; FontSize: float; FontWeight: int option }
```

### Styling attributes  (`Attributes.fs`, declared in `Attributes.fsi`)

```fsharp
val styleClasses: classes: StyleClass list -> Attr<'msg>   // → AttrValue.StyleClassesValue
val visualState:  state: VisualState     -> Attr<'msg>     // → AttrValue.VisualStateValue
```

## Behavioral contract (what callers may rely on)

| ID | Guarantee | Pinned by |
|---|---|---|
| C-1 | **Total** — returns a `ResolvedStyle` for every `(theme, base, classes, state)`; never throws. Every `StyleVariant` matched; every `VisualState` matched; any `Custom` string accepted. | FR-002, FR-004 — `Feature093StylePropertyTests` (totality), `Feature093StyleResolverTests` |
| C-2 | **Pure & deterministic** — no clock/randomness/I-O; identical inputs → identical output. | FR-006 — `Feature093StylePropertyTests` (≥1000 cases) |
| C-3 | **Fixed precedence**, last-writer-wins per field: `base < classes-in-attach-order < state`; state is provably outermost; later class wins over earlier. | FR-003 — `Feature093StylePropertyTests`, `Feature093StyleResolverTests` |
| C-4 | **Identity default** — `resolve theme base [] Normal = base` exactly; `Loading` paint == `Normal` paint. | FR-005 — `Feature093StyleResolverTests`, `Feature093ParityTests` |
| C-5 | **Unknown `Custom` ⇒ identity delta** — resolves back to the base; no exception, no silent field drop. | FR-004 edge — `Feature093StyleResolverTests` |
| C-6 | **Variant identity** — six built-ins pairwise-distinguishable; `Primary` from accent family, `Danger` from danger family. | SC-001 — `Feature093StyleResolverTests` |
| C-7 | **Token-sourced colours** — every emitted colour traces to a `Theme`/`DesignTokens` token; a theme swap re-paints; no inline literals. | FR-008, SC-006 — code review + `Feature093ParityTests` (both themes) |
| C-8 | **Additive parity** — migrated `Button`/`CheckBox` default no-class paint is structurally scene-equal to the frozen procedural baseline per (kind, theme, state). | FR-007, SC-003 — `Feature093ParityTests` + `readiness/parity/*.scene.txt` |
| C-9 | **Scoped migration** — attaching a class to an **unmigrated** kind yields no render delta. | SC-007 — `Feature093ParityTests` |
| C-10 | **State survives re-render** — a control's state-driven paint travels through the keyed reconciler diff and survives an unrelated position-shifting re-render (live retained path). | SC-005 — `Feature093RetainedStateTests` |

## Non-goals (permanent, not deferrals)

- No selector matching, no specificity algebra, no cross-control cascade — styling is single-control.
- `ResolvedStyle` governs paint/typography only; geometry is out of scope and computed as before.

## Known gaps (recorded, bounded follow-ups)

- **DF-1** *(resolved)*: `Style.fs` helpers formerly carried redundant `private` modifiers; stripped in
  the 093 conformance pass so visibility is `.fsi`-driven alone (FS0078-as-error keeps them private by
  omission). Behavior-neutral, confirmed by a clean build.
- **DF-2**: Six controls call `Style.resolve` (`Button`, `CheckBox`, `RadioGroup`, `Slider`,
  `Switch`, `TextBox`) but only `Button`/`CheckBox` have a frozen-oracle parity scene; the other four
  rely on the totality/purity proofs. Adding their parity scenes is a bounded follow-up.

## Out of scope for verification

Pixel-level output and desktop visibility — the parity proof is **structural scene equality** only,
as the readiness evidence discloses.
