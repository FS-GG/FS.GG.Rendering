namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

module KeyRebind =
    // The kind renders through the generic items path (a text row per entry), exactly as `menu` /
    // `descriptions` do — the rebindable command rows plus any conflict rows all ride the `items` attr.
    let create attrs = Control.create "key-rebind" attrs

    let private rowLabel (command: CommandId) (key: KeyId) = sprintf "%s — %s" command key

    let commands (rows: (CommandId * KeyId) list) =
        Attr.items [ for (command, key) in rows -> rowLabel command key ]

    // Mirrors the `onPayload` idiom (Interactive2.fs): the activated command id rides the event's nav text.
    let onRebind (map: CommandId -> 'msg) : Attr<'msg> =
        Attr.onWith "onRebind" (fun ev -> map (ControlEvent.navText ev |> Option.defaultValue ""))

    let ofKeymap (keymap: Keymap) (extra: Attr<'msg> list) : Control<'msg> =
        let bindingRows =
            keymap |> Keymap.toBindings |> List.map (fun b -> rowLabel b.Command b.Key)

        let conflictRows =
            keymap |> Keymap.validate |> List.map (fun d -> sprintf "conflict: %s" d.Message)

        create (Attr.items (bindingRows @ conflictRows) :: extra)
