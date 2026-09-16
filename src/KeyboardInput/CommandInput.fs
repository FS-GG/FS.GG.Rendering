namespace FS.GG.UI.KeyboardInput

open System
open System.Text

type InputModifiers =
    {
        Ctrl: bool
        Meta: bool
        Alt: bool
        Shift: bool
        AltGraph: bool
    }

[<RequireQualifiedAccess>]
type InputKeyIdentity =
    | LogicalKey of string
    | PhysicalCode of string

[<RequireQualifiedAccess>]
type InputGesture =
    | KeyChord of InputKeyIdentity * InputModifiers
    | KeySequence of (InputKeyIdentity * InputModifiers) list
    | Pointer of string
    | Touch of string
    | Gamepad of string

[<RequireQualifiedAccess>]
type CommandTriggerPolicy =
    | OncePerPress
    | RepeatWhileHeld
    | Continuous

[<RequireQualifiedAccess>]
type CommandArgumentPolicy =
    | NoArgument
    | RequiresArgument of schemaId: string

[<RequireQualifiedAccess>]
type CommandAlternative =
    | Palette
    | Menu
    | Pointer of label: string

type InputContextDescriptor =
    {
        Id: string
        Priority: int
        Exclusive: bool
        Overlaps: string list
    }

type CommandDescriptor =
    {
        Id: CommandId
        Label: string
        Contexts: string list
        AvailabilityKey: string option
        Trigger: CommandTriggerPolicy
        Argument: CommandArgumentPolicy
        Alternatives: CommandAlternative list
    }

type InputBinding =
    {
        Gesture: InputGesture
        Command: CommandId
        Context: string
    }

[<RequireQualifiedAccess>]
type InputBindingOverride =
    | ReplaceCommand of command: CommandId * bindings: InputBinding list
    | AddAlias of InputBinding
    | UnbindCommand of command: CommandId

type InputProfile =
    {
        Schema: string
        Id: string
        Defaults: InputBinding list
        Overrides: InputBindingOverride list
    }

type EffectiveInputProfile =
    {
        Id: string
        Bindings: InputBinding list
    }

type InputCatalog =
    {
        Contexts: InputContextDescriptor list
        Commands: CommandDescriptor list
        ReservedGestures: InputGesture list
        AllowTerminalPrefixes: bool
    }

type InputProfileDiagnostic =
    {
        Code: string
        Location: string
        Message: string
    }

[<RequireQualifiedAccess>]
module CommandInput =
    let profileSchema = "fsgg.input-profile/1"

    let noModifiers =
        {
            Ctrl = false
            Meta = false
            Alt = false
            Shift = false
            AltGraph = false
        }

    let private modifierBits modifiers =
        (if modifiers.Ctrl then 1 else 0)
        + (if modifiers.Meta then 2 else 0)
        + (if modifiers.Alt then 4 else 0)
        + (if modifiers.Shift then 8 else 0)
        + (if modifiers.AltGraph then 16 else 0)

    let private keyId =
        function
        | InputKeyIdentity.LogicalKey value -> "logical:" + value
        | InputKeyIdentity.PhysicalCode value -> "physical:" + value

    let private stepId (identity, modifiers) =
        keyId identity + ":" + string (modifierBits modifiers)

    let gestureId gesture =
        match gesture with
        | InputGesture.KeyChord(identity, modifiers) -> "key:" + stepId (identity, modifiers)
        | InputGesture.KeySequence values -> "sequence:" + (values |> List.map stepId |> String.concat ",")
        | InputGesture.Pointer value -> "pointer:" + value
        | InputGesture.Touch value -> "touch:" + value
        | InputGesture.Gamepad value -> "gamepad:" + value

    let private diagnostic code location message =
        {
            Code = code
            Location = location
            Message = message
        }

    let private isBlank (value: string) = String.IsNullOrWhiteSpace value

    let private keyValue =
        function
        | InputKeyIdentity.LogicalKey value
        | InputKeyIdentity.PhysicalCode value -> value

    let private keySteps =
        function
        | InputGesture.KeyChord(identity, modifiers) -> Some [ identity, modifiers ]
        | InputGesture.KeySequence values -> Some values
        | _ -> None

    let private gestureDiagnostics location gesture =
        let errors = ResizeArray<InputProfileDiagnostic>()

        match gesture with
        | InputGesture.KeyChord(identity, _) ->
            let value = keyValue identity

            if isBlank value then
                errors.Add(diagnostic "empty-key-identity" location "Key identity must be non-empty.")

            if value.Contains "+" then
                errors.Add(
                    diagnostic
                        "collapsed-modifiers"
                        location
                        "Modifiers must use typed fields instead of a collapsed key string."
                )
        | InputGesture.KeySequence values ->
            if List.isEmpty values || values.Length > 4 then
                errors.Add(
                    diagnostic "invalid-sequence-length" location "Key sequences must contain one to four steps."
                )

            values
            |> List.iteri (fun index (identity, _) ->
                let stepLocation = location + "/steps/" + string index
                let value = keyValue identity

                if isBlank value then
                    errors.Add(diagnostic "empty-key-identity" stepLocation "Key identity must be non-empty.")

                if value.Contains "+" then
                    errors.Add(
                        diagnostic
                            "collapsed-modifiers"
                            stepLocation
                            "Modifiers must use typed fields instead of a collapsed key string."
                    ))
        | InputGesture.Pointer value
        | InputGesture.Touch value
        | InputGesture.Gamepad value ->
            if isBlank value then
                errors.Add(diagnostic "empty-device-control" location "Device control identity must be non-empty.")

        List.ofSeq errors

    let private hasDiscoverableAlternative command =
        command.Alternatives
        |> List.exists (function
            | CommandAlternative.Palette
            | CommandAlternative.Menu -> true
            | _ -> false)

    let private isPrefix left right =
        match keySteps left, keySteps right with
        | Some prefix, Some sequence when prefix.Length < sequence.Length ->
            (prefix, sequence |> List.take prefix.Length) ||> List.forall2 (=)
        | _ -> false

    let compile (catalog: InputCatalog) (profile: InputProfile) =
        let contextsById =
            catalog.Contexts |> List.map (fun value -> value.Id, value) |> Map.ofList

        let commandsById =
            catalog.Commands |> List.map (fun value -> value.Id, value) |> Map.ofList

        let errors = ResizeArray<InputProfileDiagnostic>()
        let add values = values |> List.iter errors.Add

        let reportDuplicates code location label values =
            values
            |> List.groupBy id
            |> List.iter (fun (value, group) ->
                if group.Length > 1 then
                    errors.Add(diagnostic code location (sprintf "%s '%s' is declared more than once." label value)))

        if profile.Schema <> profileSchema then
            errors.Add(diagnostic "unsupported-profile-schema" "/schema" (sprintf "Expected '%s'." profileSchema))

        if isBlank profile.Id then
            errors.Add(diagnostic "empty-profile-id" "/id" "Profile identity must be non-empty.")

        if catalog.Contexts.Length > 64 then
            errors.Add(
                diagnostic "context-budget-exceeded" "/catalog/contexts" "A catalog may contain at most 64 contexts."
            )

        if catalog.Commands.Length > 512 then
            errors.Add(
                diagnostic "command-budget-exceeded" "/catalog/commands" "A catalog may contain at most 512 commands."
            )

        if profile.Defaults.Length > 2048 then
            errors.Add(
                diagnostic "binding-budget-exceeded" "/defaults" "A profile may contain at most 2,048 default bindings."
            )

        if profile.Overrides.Length > 2048 then
            errors.Add(
                diagnostic "override-budget-exceeded" "/overrides" "A profile may contain at most 2,048 overrides."
            )

        reportDuplicates "duplicate-context" "/catalog/contexts" "Context" (catalog.Contexts |> List.map _.Id)
        reportDuplicates "duplicate-command" "/catalog/commands" "Command" (catalog.Commands |> List.map _.Id)

        catalog.Contexts
        |> List.iteri (fun index context ->
            let location = "/catalog/contexts/" + string index

            if isBlank context.Id then
                errors.Add(diagnostic "empty-context-id" (location + "/id") "Context identity must be non-empty.")

            context.Overlaps
            |> List.iter (fun related ->
                if not (contextsById.ContainsKey related) then
                    errors.Add(
                        diagnostic
                            "unknown-overlap-context"
                            (location + "/overlaps")
                            (sprintf "Unknown context '%s'." related)
                    )))

        catalog.Commands
        |> List.iteri (fun index command ->
            let location = "/catalog/commands/" + string index

            if isBlank command.Id then
                errors.Add(diagnostic "empty-command-id" (location + "/id") "Command identity must be non-empty.")

            if isBlank command.Label then
                errors.Add(diagnostic "empty-command-label" (location + "/label") "Command label must be non-empty.")

            if List.isEmpty command.Contexts then
                errors.Add(
                    diagnostic
                        "missing-command-context"
                        (location + "/contexts")
                        "Command must name at least one context."
                )

            command.Contexts
            |> List.iter (fun context ->
                if not (contextsById.ContainsKey context) then
                    errors.Add(
                        diagnostic
                            "unknown-command-context"
                            (location + "/contexts")
                            (sprintf "Unknown context '%s'." context)
                    ))

            match command.Argument with
            | CommandArgumentPolicy.RequiresArgument schema when isBlank schema ->
                errors.Add(
                    diagnostic
                        "empty-argument-schema"
                        (location + "/argument")
                        "Required argument schema must be non-empty."
                )
            | _ -> ())

        let validateBinding location binding =
            add (gestureDiagnostics (location + "/gesture") binding.Gesture)

            match commandsById.TryFind binding.Command with
            | None ->
                errors.Add(
                    diagnostic
                        "unknown-binding-command"
                        (location + "/command")
                        (sprintf "Unknown command '%s'." binding.Command)
                )
            | Some command when not (command.Contexts |> List.contains binding.Context) ->
                errors.Add(
                    diagnostic
                        "command-context-mismatch"
                        (location + "/context")
                        (sprintf "Command '%s' is unavailable in context '%s'." binding.Command binding.Context)
                )
            | _ -> ()

            if not (contextsById.ContainsKey binding.Context) then
                errors.Add(
                    diagnostic
                        "unknown-binding-context"
                        (location + "/context")
                        (sprintf "Unknown context '%s'." binding.Context)
                )

            if catalog.ReservedGestures |> List.exists ((=) binding.Gesture) then
                errors.Add(
                    diagnostic
                        "reserved-gesture"
                        (location + "/gesture")
                        (sprintf "Gesture '%s' is reserved by the host." (gestureId binding.Gesture))
                )

            match binding.Gesture, commandsById.TryFind binding.Command with
            | InputGesture.KeySequence values, Some command when
                values.Length > 1 && not (hasDiscoverableAlternative command)
                ->
                errors.Add(
                    diagnostic
                        "sequence-needs-alternative"
                        (location + "/gesture")
                        "A multi-step sequence requires a palette or menu alternative."
                )
            | _ -> ()

        profile.Defaults
        |> List.iteri (fun index binding -> validateBinding ("/defaults/" + string index) binding)

        profile.Overrides
        |> List.iteri (fun index value ->
            let location = "/overrides/" + string index

            match value with
            | InputBindingOverride.ReplaceCommand(command, bindings) ->
                if not (commandsById.ContainsKey command) then
                    errors.Add(
                        diagnostic
                            "unknown-override-command"
                            (location + "/command")
                            (sprintf "Unknown command '%s'." command)
                    )

                if bindings |> List.exists (fun binding -> binding.Command <> command) then
                    errors.Add(
                        diagnostic
                            "replacement-command-mismatch"
                            location
                            "Every replacement binding must name the replaced command."
                    )

                bindings
                |> List.iteri (fun bindingIndex binding ->
                    validateBinding (location + "/bindings/" + string bindingIndex) binding)
            | InputBindingOverride.AddAlias binding -> validateBinding (location + "/binding") binding
            | InputBindingOverride.UnbindCommand command ->
                if not (commandsById.ContainsKey command) then
                    errors.Add(
                        diagnostic
                            "unknown-override-command"
                            (location + "/command")
                            (sprintf "Unknown command '%s'." command)
                    ))

        let effective =
            profile.Overrides
            |> List.fold
                (fun bindings change ->
                    match change with
                    | InputBindingOverride.ReplaceCommand(command, replacements) ->
                        (bindings |> List.filter (fun value -> value.Command <> command)) @ replacements
                    | InputBindingOverride.AddAlias binding -> bindings @ [ binding ]
                    | InputBindingOverride.UnbindCommand command ->
                        bindings |> List.filter (fun value -> value.Command <> command))
                profile.Defaults

        let contextsOverlap (left: InputContextDescriptor) (right: InputContextDescriptor) =
            left.Id = right.Id
            || List.contains right.Id left.Overlaps
            || List.contains left.Id right.Overlaps

        for leftIndex, left in List.indexed effective do
            for rightIndex, right in List.indexed effective do
                if leftIndex < rightIndex && left.Gesture = right.Gesture then
                    match contextsById.TryFind left.Context, contextsById.TryFind right.Context with
                    | Some leftContext, Some rightContext when
                        leftContext.Priority = rightContext.Priority
                        && contextsOverlap leftContext rightContext
                        ->
                        errors.Add(
                            diagnostic
                                "ambiguous-gesture"
                                "/effective"
                                (sprintf
                                    "Gesture '%s' ambiguously binds '%s' and '%s' at priority %d."
                                    (gestureId left.Gesture)
                                    left.Command
                                    right.Command
                                    leftContext.Priority)
                        )
                    | _ -> ()

                if
                    not catalog.AllowTerminalPrefixes
                    && leftIndex <> rightIndex
                    && isPrefix left.Gesture right.Gesture
                then
                    match contextsById.TryFind left.Context, contextsById.TryFind right.Context with
                    | Some leftContext, Some rightContext when contextsOverlap leftContext rightContext ->
                        errors.Add(
                            diagnostic
                                "terminal-prefix-conflict"
                                "/effective"
                                (sprintf
                                    "Gesture '%s' is a terminal prefix of '%s'."
                                    (gestureId left.Gesture)
                                    (gestureId right.Gesture))
                        )
                    | _ -> ()

        if errors.Count > 0 then
            Error(List.ofSeq errors)
        else
            Ok
                {
                    Id = profile.Id
                    Bindings = effective
                }

    let ofKeymap profileId contextId keymap =
        {
            Schema = profileSchema
            Id = profileId
            Defaults =
                keymap
                |> Keymap.toBindings
                |> List.map (fun value ->
                    {
                        Gesture = InputGesture.KeyChord(InputKeyIdentity.LogicalKey value.Key, noModifiers)
                        Command = value.Command
                        Context = contextId
                    })
            Overrides = []
        }

[<RequireQualifiedAccess>]
module InputProfileCodec =
    let formatId = "fsgg.input-profile"
    let formatVersion = 1

    let private diagnostic code location message =
        {
            Code = code
            Location = location
            Message = message
        }

    let private encodeString (value: string) =
        Encoding.UTF8.GetBytes value |> Convert.ToBase64String

    let private decodeString (value: string) : string =
        Convert.FromBase64String value
        |> fun bytes -> Encoding.UTF8.GetString(bytes: byte[])

    let private modifierBits modifiers =
        (if modifiers.Ctrl then 1 else 0)
        + (if modifiers.Meta then 2 else 0)
        + (if modifiers.Alt then 4 else 0)
        + (if modifiers.Shift then 8 else 0)
        + (if modifiers.AltGraph then 16 else 0)

    let private modifiers bits =
        {
            Ctrl = bits &&& 1 <> 0
            Meta = bits &&& 2 <> 0
            Alt = bits &&& 4 <> 0
            Shift = bits &&& 8 <> 0
            AltGraph = bits &&& 16 <> 0
        }

    let private encodeKey =
        function
        | InputKeyIdentity.LogicalKey value -> "l," + encodeString value
        | InputKeyIdentity.PhysicalCode value -> "p," + encodeString value

    let private encodeStep (identity, modifierSet) =
        encodeKey identity + "," + string (modifierBits modifierSet)

    let private encodeGesture =
        function
        | InputGesture.KeyChord(identity, modifierSet) -> "k;" + encodeStep (identity, modifierSet)
        | InputGesture.KeySequence values -> "s;" + (values |> List.map encodeStep |> String.concat ";")
        | InputGesture.Pointer value -> "m;" + encodeString value
        | InputGesture.Touch value -> "t;" + encodeString value
        | InputGesture.Gamepad value -> "g;" + encodeString value

    let private encodeBinding prefix binding =
        String.concat
            "\t"
            [
                prefix
                encodeString binding.Context
                encodeString binding.Command
                encodeGesture binding.Gesture
            ]

    let encode (profile: InputProfile) =
        let lines = ResizeArray<string>()
        lines.Add(formatId + "\t" + string formatVersion)
        lines.Add("schema\t" + encodeString profile.Schema)
        lines.Add("id\t" + encodeString profile.Id)

        profile.Defaults
        |> List.iter (fun binding -> lines.Add(encodeBinding "default" binding))

        profile.Overrides
        |> List.iter (function
            | InputBindingOverride.ReplaceCommand(command, bindings) ->
                lines.Add("replace\t" + encodeString command + "\t" + string bindings.Length)

                bindings
                |> List.iter (fun binding -> lines.Add(encodeBinding "binding" binding))
            | InputBindingOverride.AddAlias binding -> lines.Add(encodeBinding "alias" binding)
            | InputBindingOverride.UnbindCommand command -> lines.Add("unbind\t" + encodeString command))

        lines.Add("end")
        String.concat "\n" lines + "\n" |> Encoding.UTF8.GetBytes

    let private parseKey kind encoded =
        match kind with
        | "l" -> InputKeyIdentity.LogicalKey(decodeString encoded)
        | "p" -> InputKeyIdentity.PhysicalCode(decodeString encoded)
        | _ -> raise (FormatException(sprintf "unknown key kind '%s'" kind))

    let private parseStep (value: string) =
        match value.Split(',') with
        | [| kind; encoded; rawBits |] ->
            let bits = Int32.Parse rawBits

            if bits < 0 || bits > 31 then
                raise (FormatException "modifier bits must be between 0 and 31")

            parseKey kind encoded, modifiers bits
        | _ -> raise (FormatException "malformed key step")

    let private parseGesture (value: string) =
        match value.Split(';') |> Array.toList with
        | [ "m"; encoded ] -> InputGesture.Pointer(decodeString encoded)
        | [ "t"; encoded ] -> InputGesture.Touch(decodeString encoded)
        | [ "g"; encoded ] -> InputGesture.Gamepad(decodeString encoded)
        | [ "k"; step ] ->
            let identity, modifierSet = parseStep step
            InputGesture.KeyChord(identity, modifierSet)
        | "s" :: values when not (List.isEmpty values) -> InputGesture.KeySequence(values |> List.map parseStep)
        | kind :: _ -> raise (FormatException(sprintf "unknown gesture kind '%s'" kind))
        | [] -> raise (FormatException "empty gesture")

    let private parseBinding fields =
        match fields with
        | [ context; command; gesture ] ->
            {
                Context = decodeString context
                Command = decodeString command
                Gesture = parseGesture gesture
            }
        | _ -> raise (FormatException "malformed binding")

    let decode (bytes: byte[]) =
        try
            if bytes.Length > 1048576 then
                raise (FormatException "profile envelope exceeds 1 MiB")

            let rows =
                Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Split('\n')
                |> Array.filter (fun value -> value <> "")

            if rows.Length = 0 || rows[0] <> formatId + "\t" + string formatVersion then
                Error
                    [
                        diagnostic "unsupported-envelope" "/header" "Unsupported input profile envelope."
                    ]
            else
                let defaults = ResizeArray<InputBinding>()
                let overrides = ResizeArray<InputBindingOverride>()
                let mutable schema: string option = None
                let mutable profileId: string option = None
                let mutable index = 1
                let mutable ended = false

                while index < rows.Length && not ended do
                    let fields = rows[index].Split('\t') |> Array.toList

                    match fields with
                    | [ "schema"; encoded ] ->
                        schema <- Some(decodeString encoded)
                        index <- index + 1
                    | [ "id"; encoded ] ->
                        profileId <- Some(decodeString encoded)
                        index <- index + 1
                    | "default" :: bindingFields ->
                        defaults.Add(parseBinding bindingFields)
                        index <- index + 1
                    | "alias" :: bindingFields ->
                        overrides.Add(InputBindingOverride.AddAlias(parseBinding bindingFields))
                        index <- index + 1
                    | [ "unbind"; encoded ] ->
                        overrides.Add(InputBindingOverride.UnbindCommand(decodeString encoded))
                        index <- index + 1
                    | [ "replace"; encoded; rawCount ] ->
                        let count = Int32.Parse rawCount

                        if count < 0 || index + count >= rows.Length then
                            raise (FormatException "replacement count exceeds envelope")

                        let bindings =
                            [
                                for offset in 1..count do
                                    match rows[index + offset].Split('\t') |> Array.toList with
                                    | "binding" :: bindingFields -> yield parseBinding bindingFields
                                    | _ -> raise (FormatException "replacement row is not a binding")
                            ]

                        overrides.Add(InputBindingOverride.ReplaceCommand(decodeString encoded, bindings))
                        index <- index + count + 1
                    | [ "end" ] ->
                        ended <- true
                        index <- index + 1
                    | _ -> raise (FormatException(sprintf "unknown profile row %d" (index + 1)))

                match schema, profileId, ended with
                | Some profileSchema, Some id, true ->
                    Ok
                        {
                            Schema = profileSchema
                            Id = id
                            Defaults = List.ofSeq defaults
                            Overrides = List.ofSeq overrides
                        }
                | None, _, _ ->
                    Error
                        [
                            diagnostic "missing-profile-schema" "/schema" "Profile schema row is required."
                        ]
                | _, None, _ -> Error [ diagnostic "missing-profile-id" "/id" "Profile identity row is required." ]
                | _, _, false -> Error [ diagnostic "missing-end" "/" "Profile envelope is incomplete." ]
        with error ->
            Error [ diagnostic "malformed-envelope" "/" error.Message ]
