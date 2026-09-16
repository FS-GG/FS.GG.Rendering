namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

type SvgWorkspaceCommandRow =
    {
        Command: CommandId
        Label: string
        Available: bool
        Gestures: InputGesture list
        ShortcutText: string option
        AriaKeyShortcuts: string option
        InPalette: bool
        InMenu: bool
        PointerLabels: string list
    }

type SvgWorkspaceRebindPreview =
    {
        Command: CommandId
        Gesture: InputGesture
        DisplacedCommands: CommandId list
        CandidateProfile: InputProfile
        EffectiveProfile: EffectiveInputProfile
    }

[<RequireQualifiedAccess>]
module SvgWorkspaceCommands =
    let private modifiersText (modifiers: InputModifiers) =
        [
            if modifiers.Ctrl then
                "Control"
            if modifiers.Meta then
                "Meta"
            if modifiers.Alt then
                "Alt"
            if modifiers.Shift then
                "Shift"
            if modifiers.AltGraph then
                "AltGraph"
        ]

    let private keyText =
        function
        | InputKeyIdentity.LogicalKey value -> value
        | InputKeyIdentity.PhysicalCode value -> "Physical " + value

    let private chordText (identity: InputKeyIdentity, modifiers: InputModifiers) =
        modifiersText modifiers @ [ keyText identity ] |> String.concat "+"

    let gestureText gesture =
        match gesture with
        | InputGesture.KeyChord(identity, modifiers) -> chordText (identity, modifiers)
        | InputGesture.KeySequence steps -> steps |> List.map chordText |> String.concat " then "
        | InputGesture.Pointer value -> "Pointer " + value
        | InputGesture.Touch value -> "Touch " + value
        | InputGesture.Gamepad value -> "Gamepad " + value

    let ariaKeyShortcuts gesture =
        match gesture with
        | InputGesture.KeyChord(InputKeyIdentity.LogicalKey key, modifiers) when not modifiers.AltGraph ->
            Some(modifiersText modifiers @ [ key ] |> String.concat "+")
        | _ -> None

    let project (catalog: InputCatalog) (profile: EffectiveInputProfile) activeContexts isAvailable =
        let active = Set.ofList activeContexts

        catalog.Commands
        |> List.map (fun command ->
            let availableInContext = command.Contexts |> List.exists active.Contains

            let gestures =
                profile.Bindings
                |> List.filter (fun binding -> binding.Command = command.Id && active.Contains binding.Context)
                |> List.map _.Gesture

            let alternatives = command.Alternatives
            let shortcutText = gestures |> List.tryHead |> Option.map gestureText
            let aria = gestures |> List.tryPick ariaKeyShortcuts

            {
                Command = command.Id
                Label = command.Label
                Available = availableInContext && isAvailable command
                Gestures = gestures
                ShortcutText = shortcutText
                AriaKeyShortcuts = aria
                InPalette = alternatives |> List.contains CommandAlternative.Palette
                InMenu = alternatives |> List.contains CommandAlternative.Menu
                PointerLabels =
                    alternatives
                    |> List.choose (function
                        | CommandAlternative.Pointer label -> Some label
                        | _ -> None)
            })

    let paletteRows rows =
        rows |> List.filter (fun row -> row.Available && row.InPalette)

    let helpRows rows = rows |> List.filter _.Available

    let pointerRows rows =
        rows |> List.filter (fun row -> row.Available && not row.PointerLabels.IsEmpty)

    let previewRebind catalog profile command context gesture =
        CommandInput.compile catalog profile
        |> Result.bind (fun current ->
            let displaced =
                current.Bindings
                |> List.filter (fun binding ->
                    binding.Context = context
                    && binding.Gesture = gesture
                    && binding.Command <> command)
                |> List.map _.Command
                |> List.distinct
                |> List.sort

            let preserveDisplaced =
                displaced
                |> List.map (fun displacedCommand ->
                    let remaining =
                        current.Bindings
                        |> List.filter (fun binding ->
                            binding.Command = displacedCommand
                            && not (binding.Context = context && binding.Gesture = gesture))

                    if remaining.IsEmpty then
                        InputBindingOverride.UnbindCommand displacedCommand
                    else
                        InputBindingOverride.ReplaceCommand(displacedCommand, remaining))

            let replacement =
                {
                    Gesture = gesture
                    Command = command
                    Context = context
                }

            let candidate =
                { profile with
                    Overrides =
                        profile.Overrides
                        @ preserveDisplaced
                        @ [ InputBindingOverride.ReplaceCommand(command, [ replacement ]) ]
                }

            CommandInput.compile catalog candidate
            |> Result.map (fun effective ->
                {
                    Command = command
                    Gesture = gesture
                    DisplacedCommands = displaced
                    CandidateProfile = candidate
                    EffectiveProfile = effective
                }))
