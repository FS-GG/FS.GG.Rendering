namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

type KeyRebindAction =
    { Command: CommandId
      Label: string
      Order: int
      Binding: KeyId option
      DefaultBinding: KeyId option }

module KeyRebind =
    // The kind renders through the generic items path (a text row per entry), exactly as `menu` /
    // `descriptions` do — the rebindable command rows plus any conflict rows all ride the `items` attr.
    let create attrs = Control.create "key-rebind" attrs

    let private rowLabel (command: CommandId) (key: KeyId) = sprintf "%s — %s" command key

    let commands (rows: (CommandId * KeyId) list) =
        Attr.items [ for (command, key) in rows -> rowLabel command key ]

    let private ordered catalog = catalog |> List.sortBy (fun action -> action.Order, action.Command)

    let private actionLabel action =
        sprintf "%s — %s" action.Label (action.Binding |> Option.defaultValue "Unbound")

    let actions catalog =
        catalog |> ordered |> List.map actionLabel |> Attr.items

    // Mirrors the `onPayload` idiom (Interactive2.fs): the activated command id rides the event's nav text.
    let onRebind (map: CommandId -> 'msg) : Attr<'msg> =
        Attr.onWith "onRebind" (fun ev -> map (ControlEvent.navText ev |> Option.defaultValue ""))

    let onActionRebind catalog (map: CommandId -> 'msg) : Attr<'msg> =
        let commandByPayload =
            catalog
            |> List.collect (fun action -> [ actionLabel action, action.Command; action.Command, action.Command ])
            |> Map.ofList

        Attr.onWith "onRebind" (fun ev ->
            let payload = ControlEvent.navText ev |> Option.defaultValue ""
            map (commandByPayload |> Map.tryFind payload |> Option.defaultValue payload))

    let onReset msg : Attr<'msg> = Attr.on "onReset" msg

    let withBindings (keymap: Keymap) catalog =
        let byCommand =
            keymap
            |> Keymap.toBindings
            |> List.groupBy (fun binding -> binding.Command)
            |> List.map (fun (command, bindings) -> command, (bindings |> List.map (fun binding -> binding.Key) |> List.sort |> List.tryHead))
            |> Map.ofList

        catalog
        |> List.map (fun action ->
            { action with
                Binding = byCommand |> Map.tryFind action.Command |> Option.flatten })

    let restoreDefaults catalog =
        catalog
        |> ordered
        |> List.fold (fun keymap action ->
            match action.DefaultBinding with
            | Some key -> Keymap.replaceCommandBinding action.Command key keymap
            | None -> keymap) Keymap.empty

    let ofActions catalog extra =
        let rows = catalog |> ordered |> List.map actionLabel
        let resetRows =
            if catalog |> List.exists (fun action -> action.DefaultBinding.IsSome) then
                [ "Reset controls to defaults" ]
            else
                []

        create (Attr.items (rows @ resetRows) :: extra)

    let ofKeymap (keymap: Keymap) (extra: Attr<'msg> list) : Control<'msg> =
        let bindingRows =
            keymap |> Keymap.toBindings |> List.map (fun b -> rowLabel b.Command b.Key)

        let conflictRows =
            keymap |> Keymap.validate |> List.map (fun d -> sprintf "conflict: %s" d.Message)

        create (Attr.items (bindingRows @ conflictRows) :: extra)
