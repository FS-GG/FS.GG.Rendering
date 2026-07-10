// See skill: fs-gg-elmish
namespace FS.GG.UI.Controls.Elmish.Authoring

open FS.GG.UI.Controls.Elmish

/// Product-authoring no-op command alias (Elmish convention). `open` this namespace in a generated
/// product's `Model` to write `model, Cmd.none` instead of a bare `model, []`. Kept in a dedicated
/// sub-namespace (not the root `FS.GG.UI.Controls.Elmish`) so the `Cmd` name never shadows Fable
/// `Elmish.Cmd` inside the adapter package; a product opens this only when it wants the alias.
module Cmd =
    /// `Cmd.none = ([] : AdapterCommand<'msg>)`. Behaviour-identical to returning `[]`.
    val none: AdapterCommand<'msg>

/// Product-authoring no-op subscription-list alias (Elmish convention). Write `Sub.none` from a
/// product's `subscriptions` instead of a bare `[]`.
module Sub =
    /// `Sub.none = ([] : AdapterSubscription<'msg> list)`. Behaviour-identical to returning `[]`.
    val none: AdapterSubscription<'msg> list
