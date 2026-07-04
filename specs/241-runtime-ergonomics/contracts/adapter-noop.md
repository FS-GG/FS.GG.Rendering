# Contract: `Cmd.none` / `Sub.none` product-facing no-ops (§3.5)

Package: `FS.GG.UI.Controls.Elmish`. Additive, Tier 1 (public surface).

## `.fsi` sketch (to add to `src/Controls.Elmish/ControlsElmish.fsi`)

```fsharp
/// The product-facing no-op command: an empty `AdapterCommand` (Elmish-convention alias for `[]`).
/// A product `update` that issues no command returns `model, Cmd.none`.
module Cmd =
    /// `Cmd.none = ([] : AdapterCommand<'msg>)`. Behaviour-identical to returning `[]`.
    val none: AdapterCommand<'msg>

/// The product-facing no-op subscription list (Elmish-convention alias for `[]`).
/// A product `subscriptions` with no subscriptions returns `Sub.none`.
module Sub =
    /// `Sub.none = ([] : AdapterSubscription<'msg> list)`.
    val none: AdapterSubscription<'msg> list
```

## Laws (FSI-exercisable, become the semantic test)

1. `Cmd.none = ([] : AdapterCommand<'msg>)`.
2. `AdapterCmd.productMessages Cmd.none = []` (no product messages carried).
3. `Sub.none = ([] : AdapterSubscription<'msg> list)`.
4. Behavioural identity: a product `update` returning `model, Cmd.none` is indistinguishable from one
   returning `model, []` (same model, same effects); likewise `subscriptions` returning `Sub.none`.

## Constraints

- Definitions in `.fs` carry no access modifiers (Principle II); visibility is the `.fsi`.
- `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` gains exactly `FS.GG.UI.Controls.Elmish.Cmd`
  and `FS.GG.UI.Controls.Elmish.Sub`; `SurfaceAreaTests` stays green.
- Name-collision note (see research D2): resolves to these in a generated product (no `open Elmish`);
  qualified fallback documented for a product that also opens Fable.Elmish.
