namespace FS.GG.UI.KeyboardInput

type CommandId = string
type KeyId = string

type ViewerKey =
    | ArrowLeft
    | ArrowRight
    | ArrowUp
    | ArrowDown
    | Enter
    | Space
    | Escape
    | Backspace
    | Letter of char
    | Digit of int
    | Function of int
    | Unknown of raw: string

type ViewerKeyDirection =
    | KeyDown
    | KeyUp

type ViewerKeyEvent =
    { RawKey: string
      Direction: ViewerKeyDirection }

type KeyboardBinding =
    { Key: KeyId
      Command: CommandId }

type KeyboardDiagnostic =
    { Code: string
      Severity: string
      Message: string
      Key: KeyId option }

type KeyboardStateDisplay =
    { PressedKeys: KeyId list
      ActiveLayout: string
      ActiveModeStack: string list
      PendingSequence: KeyId list
      LastCommand: CommandId option }

type KeyboardEffect =
    | CommandResolved of CommandId
    | KeyStateChanged of KeyId list
    | LayoutChanged of string
    | ModeChanged of string list
    | PendingSequenceChanged of KeyId list
    | StateDisplayChanged of KeyboardStateDisplay
    | ReportKeyboardDiagnostic of KeyboardDiagnostic
    | RequestHostKeyCapture of KeyId

type KeyboardModel =
    { Bindings: KeyboardBinding list
      PressedKeys: Set<KeyId>
      LastCommand: CommandId option
      ActiveLayout: string
      ActiveModeStack: string list
      PersistentModeState: Map<string, string>
      PendingSequence: KeyId list
      Diagnostics: KeyboardDiagnostic list
      RecentEffects: KeyboardEffect list
      StateDisplay: KeyboardStateDisplay }

type KeyboardMsg =
    | KeyDown of KeyId
    | KeyUp of KeyId
    | FocusLost
    | Reset
    | SetActiveLayout of string
    | PushTemporaryMode of string
    | PopTemporaryMode
    | SetPersistentMode of key: string * value: string
    | ResolvePendingSequence of KeyId list

module Keyboard =
    let stateDisplay model =
        { PressedKeys = model.PressedKeys |> Set.toList
          ActiveLayout = model.ActiveLayout
          ActiveModeStack = model.ActiveModeStack
          PendingSequence = model.PendingSequence
          LastCommand = model.LastCommand }

    let attachState effects model =
        let display = stateDisplay model
        { model with StateDisplay = display; RecentEffects = effects }, effects

    let init bindings =
        let display =
            { PressedKeys = []
              ActiveLayout = "default"
              ActiveModeStack = []
              PendingSequence = []
              LastCommand = None }

        let effects = [ StateDisplayChanged display ]

        { Bindings = bindings
          PressedKeys = Set.empty
          LastCommand = None
          ActiveLayout = "default"
          ActiveModeStack = []
          PersistentModeState = Map.empty
          PendingSequence = []
          Diagnostics = []
          RecentEffects = effects
          StateDisplay = display },
        effects

    let update msg model =
        match msg with
        | KeyDown key ->
            let pressed = model.PressedKeys |> Set.add key

            let command =
                model.Bindings
                |> List.tryFind (fun binding -> binding.Key = key)
                |> Option.map _.Command

            let effects =
                [ KeyStateChanged(Set.toList pressed)
                  match command with
                  | Some command -> CommandResolved command
                  | None -> KeyStateChanged(Set.toList pressed) ]
                |> List.distinct

            { model with PressedKeys = pressed; LastCommand = command }
            |> attachState effects
        | KeyUp key ->
            let pressed = model.PressedKeys |> Set.remove key
            let effects = [ KeyStateChanged(Set.toList pressed) ]
            { model with PressedKeys = pressed } |> attachState effects
        | FocusLost ->
            let diagnostic =
                { Code = "FocusLostRecovered"
                  Severity = "Warning"
                  Message = "Focus loss cleared pressed keys and temporary modes."
                  Key = None }

            let effects =
                [ KeyStateChanged []
                  ModeChanged []
                  PendingSequenceChanged []
                  ReportKeyboardDiagnostic diagnostic ]

            { model with
                PressedKeys = Set.empty
                ActiveModeStack = []
                PendingSequence = []
                Diagnostics = diagnostic :: model.Diagnostics }
            |> attachState effects
        | Reset ->
            let effects =
                [ KeyStateChanged []
                  ModeChanged []
                  PendingSequenceChanged [] ]

            { model with
                PressedKeys = Set.empty
                LastCommand = None
                ActiveModeStack = []
                PendingSequence = []
                PersistentModeState = Map.empty
                Diagnostics = [] }
            |> attachState effects
        | SetActiveLayout layout ->
            { model with ActiveLayout = layout }
            |> attachState [ LayoutChanged layout ]
        | PushTemporaryMode mode ->
            let modes = mode :: model.ActiveModeStack
            { model with ActiveModeStack = modes }
            |> attachState [ ModeChanged modes ]
        | PopTemporaryMode ->
            let modes =
                match model.ActiveModeStack with
                | _ :: rest -> rest
                | [] -> []

            { model with ActiveModeStack = modes }
            |> attachState [ ModeChanged modes ]
        | SetPersistentMode(key, value) ->
            let state = model.PersistentModeState |> Map.add key value
            { model with PersistentModeState = state }
            |> attachState [ ModeChanged model.ActiveModeStack ]
        | ResolvePendingSequence sequence ->
            { model with PendingSequence = sequence }
            |> attachState [ PendingSequenceChanged sequence ]

// Issue 331 (epic 330): the keymap mechanism. Representation is a `Map<KeyId, CommandId>` so a key
// binds to a single command by construction; it is hidden by the .fsi (the type is opaque), so a keymap
// can only be built through the `Keymap` module. Map gives structural equality and a deterministic,
// key-ordered enumeration for free.
type Keymap = { Bindings: Map<KeyId, CommandId> }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Keymap =
    let empty = { Bindings = Map.empty }

    // Fold left so a key bound more than once takes its LAST binding (Map.add overwrites) — total on
    // duplicates, and the natural inverse of `toBindings`.
    let ofBindings (bindings: KeyboardBinding list) =
        { Bindings =
            bindings
            |> List.fold (fun acc binding -> Map.add binding.Key binding.Command acc) Map.empty }

    // Map enumerates in key order, so the result is deterministic across runs.
    let toBindings (keymap: Keymap) =
        keymap.Bindings
        |> Map.toList
        |> List.map (fun (key, command) -> { Key = key; Command = command })

    let tryFind key (keymap: Keymap) = keymap.Bindings |> Map.tryFind key

    let count (keymap: Keymap) = keymap.Bindings.Count

    // Non-destructive: only binds when the key is absent, so an existing binding is never clobbered.
    let add key command (keymap: Keymap) =
        if keymap.Bindings.ContainsKey key then
            keymap
        else
            { keymap with Bindings = Map.add key command keymap.Bindings }

    let remove key (keymap: Keymap) =
        { keymap with Bindings = Map.remove key keymap.Bindings }

    // Update-only: leaves an unbound key untouched (the complement of `add`).
    let replace key command (keymap: Keymap) =
        if keymap.Bindings.ContainsKey key then
            { keymap with Bindings = Map.add key command keymap.Bindings }
        else
            keymap

    // Key-indexed upsert: binds a fresh key, and overwrites an already-bound one. It deliberately
    // leaves any OTHER key assigned to the same command intact.
    let assignKey key command (keymap: Keymap) =
        { keymap with Bindings = Map.add key command keymap.Bindings }

    let rebind key command keymap = assignKey key command keymap

    // Player-facing command replacement uses a single-binding policy. Removing by command first
    // also makes a formerly multiply-bound command converge to the documented invariant.
    let replaceCommandBinding command key (keymap: Keymap) =
        let withoutCommand =
            keymap.Bindings
            |> Map.filter (fun _ boundCommand -> boundCommand <> command)

        { keymap with Bindings = Map.add key command withoutCommand }

    let clear (_: Keymap) = empty

    // Issue 332 (epic 330): one "many keys -> one command" diagnostic per command reachable from two
    // or more DISTINCT keys. Deterministic — commands sorted, keys sorted within each.
    let private sharedCommandDiagnostics (bindings: KeyboardBinding list) : KeyboardDiagnostic list =
        bindings
        |> List.groupBy (fun binding -> binding.Command)
        |> List.sortBy fst
        |> List.choose (fun (command, group) ->
            let keys = group |> List.map (fun binding -> binding.Key) |> List.distinct |> List.sort

            if List.length keys > 1 then
                Some
                    { Code = "SharedCommandBinding"
                      Severity = "Info"
                      Message =
                        sprintf "Command '%s' is bound to multiple keys: %s." command (String.concat ", " keys)
                      Key = None }
            else
                None)

    // Issue 332 (epic 330): one "duplicate key" diagnostic per key that appears in more than one
    // binding — the conflict `ofBindings` silently collapses last-wins. Deterministic, keys sorted; the
    // named winner mirrors `ofBindings` (the LAST binding in the list).
    let private duplicateKeyDiagnostics (bindings: KeyboardBinding list) : KeyboardDiagnostic list =
        bindings
        |> List.groupBy (fun binding -> binding.Key)
        |> List.sortBy fst
        |> List.choose (fun (key, group) ->
            if List.length group > 1 then
                let commands = group |> List.map (fun binding -> binding.Command)

                Some
                    { Code = "DuplicateKeyBinding"
                      Severity = "Warning"
                      Message =
                        sprintf
                            "Key '%s' is bound %d times (commands: %s); last-wins keeps '%s'."
                            key
                            (List.length group)
                            (String.concat ", " commands)
                            (List.last commands)
                      Key = Some key }
            else
                None)

    // Issue 332 (epic 330): the named resolution entry point the live dispatch path (issue 333) will
    // consult. Keymap-first, so a host binds one keymap and resolves varying keys; `tryFind` flipped.
    let resolve (keymap: Keymap) (key: KeyId) : CommandId option = keymap.Bindings |> Map.tryFind key

    // Issue 332 (epic 330): a built keymap indexes by key, so it can hold NO duplicate-key conflict;
    // only many-keys->one-command is surfaced. Use `validateBindings` for the raw-list case.
    let validate (keymap: Keymap) : KeyboardDiagnostic list =
        keymap |> toBindings |> sharedCommandDiagnostics

    // Issue 332 (epic 330): conflicts in a raw binding list, BEFORE `ofBindings` collapses duplicate
    // keys. Duplicate-key diagnostics first (by key), then shared-command diagnostics (by command).
    let validateBindings (bindings: KeyboardBinding list) : KeyboardDiagnostic list =
        duplicateKeyDiagnostics bindings @ sharedCommandDiagnostics bindings

// Feature 108 (US5, FR-016): modifier state recovered at the key boundary (see KeyboardInput.fsi).
type KeyModifiers =
    { Ctrl: bool
      Alt: bool
      Shift: bool
      Meta: bool }

module ViewerKeyboard =
    let normalize (raw: string) =
        let value =
            if System.String.IsNullOrEmpty raw then
                ""
            elif raw = " " then
                raw
            else
                raw.Trim()

        let lower = value.ToLowerInvariant()

        match lower with
        | "left"
        | "arrowleft"
        | "leftarrow" -> ArrowLeft
        | "right"
        | "arrowright"
        | "rightarrow" -> ArrowRight
        | "up"
        | "arrowup"
        | "uparrow" -> ArrowUp
        | "down"
        | "arrowdown"
        | "downarrow" -> ArrowDown
        | "enter"
        | "return" -> Enter
        | "space"
        | "spacebar"
        | " " -> Space
        | "escape"
        | "esc" -> Escape
        | "backspace"
        | "back" -> Backspace
        // Feature 085 (FR-007/FR-008) — toolkit key-name families. Browser/toolkit codes spell
        // digits as Number5/Digit5/Keypad5/Key5 and letters as KeyL; map them to the existing
        // Digit n / Letter X cases. The terminal `Unknown raw` arm below is preserved (totality).
        | _ when (lower.StartsWith "number" || lower.StartsWith "keypad") && lower.Length = 7 && System.Char.IsDigit lower[6] ->
            Digit(int lower[6] - int '0')
        | _ when lower.StartsWith "digit" && lower.Length = 6 && System.Char.IsDigit lower[5] ->
            Digit(int lower[5] - int '0')
        | _ when lower.StartsWith "key" && lower.Length = 4 ->
            // Key{n} / Key{X}: classify the single trailing char (resolves Key5-vs-KeyL in one arm).
            let c = value[value.Length - 1]
            if System.Char.IsDigit c then Digit(int c - int '0')
            elif System.Char.IsLetter c then Letter(System.Char.ToUpperInvariant c)
            else Unknown raw
        | _ when value.Length = 1 && System.Char.IsLetter value[0] ->
            Letter(System.Char.ToUpperInvariant value[0])
        | _ when value.Length = 1 && System.Char.IsDigit value[0] ->
            Digit(int value[0] - int '0')
        | _ when lower.StartsWith("f") ->
            match System.Int32.TryParse(value.Substring 1) with
            | true, number when number > 0 -> Function number
            | _ -> Unknown raw
        | _ -> Unknown raw

    let noModifiers =
        { Ctrl = false
          Alt = false
          Shift = false
          Meta = false }

    // Issue 183: the modifier keys themselves. A host must never decorate one with its own held
    // state — `ControlLeft` pressed while Ctrl is down is `ControlLeft`, not `Ctrl+ControlLeft`.
    // Both the toolkit spellings (`ControlLeft`) and the bare tokens `parseModifiers` accepts.
    let private modifierKeyNames =
        set
            [ "shift"
              "shiftleft"
              "shiftright"
              "ctrl"
              "control"
              "controlleft"
              "controlright"
              "alt"
              "option"
              "altleft"
              "altright"
              "meta"
              "cmd"
              "command"
              "win"
              "super"
              "superleft"
              "superright" ]

    let isModifierKey (raw: string) =
        not (System.String.IsNullOrEmpty raw)
        && modifierKeyNames.Contains(raw.Trim().ToLowerInvariant())

    // FR-016: split the raw key on '+'; the final segment is the base key, every preceding segment
    // is a modifier token classified case-insensitively (any order, repeats tolerated). A raw key
    // with no '+' has no modifiers and its base IS the raw key, so routing is byte-identical to
    // `normalize`. Pure, total.
    let private parseModifiers (raw: string) : string * KeyModifiers =
        if System.String.IsNullOrEmpty raw then
            raw, noModifiers
        else
            let parts = raw.Split('+')

            if parts.Length <= 1 then
                raw, noModifiers
            else
                let baseKey = parts.[parts.Length - 1]
                let mutable mods = noModifiers

                for i in 0 .. parts.Length - 2 do
                    match parts.[i].Trim().ToLowerInvariant() with
                    | "ctrl"
                    | "control" -> mods <- { mods with Ctrl = true }
                    | "alt"
                    | "option" -> mods <- { mods with Alt = true }
                    | "shift" -> mods <- { mods with Shift = true }
                    | "meta"
                    | "cmd"
                    | "command"
                    | "win"
                    | "super" -> mods <- { mods with Meta = true }
                    | _ -> ()

                baseKey, mods

    // Issue 183: the inverse of `parseModifiers`, and the ONLY producer of the `Ctrl+L` wire format.
    // `parseModifiers` classifies in any order, so the order here is merely canonical, not load-bearing.
    let formatChord (modifiers: KeyModifiers) (baseKey: string) : string =
        if System.String.IsNullOrEmpty baseKey then
            baseKey
        else
            let prefixes =
                [ if modifiers.Ctrl then "Ctrl"
                  if modifiers.Alt then "Alt"
                  if modifiers.Shift then "Shift"
                  if modifiers.Meta then "Meta" ]

            if List.isEmpty prefixes then
                baseKey
            else
                String.concat "+" (prefixes @ [ baseKey ])

    // Issue 183: deliberately does NOT strip modifiers. A chord reaches the `MapKeyChord` seam as
    // `ViewerKey.Unknown "Ctrl+L"` and `chordFallthrough` recovers the modifiers from that raw string,
    // so stripping here would silently dissolve every chord before the seam ever sees it. Consumers
    // that want the base key call `normalizeEventWithModifiers` (or strip a prefix, as
    // `normalizeFocusKey` does for `Shift+Tab`).
    let normalizeEvent event =
        let isDown =
            match event.Direction with
            | ViewerKeyDirection.KeyDown -> true
            | ViewerKeyDirection.KeyUp -> false

        normalize event.RawKey, isDown

    let toKeyId key =
        match key with
        | ArrowLeft -> "ArrowLeft"
        | ArrowRight -> "ArrowRight"
        | ArrowUp -> "ArrowUp"
        | ArrowDown -> "ArrowDown"
        | Enter -> "Enter"
        | Space -> "Space"
        | Escape -> "Escape"
        | Backspace -> "Backspace"
        | Letter value -> string value
        | Digit value -> string value
        | Function value -> $"F{value}"
        | Unknown raw -> raw

    let normalizeEventWithModifiers event =
        let isDown =
            match event.Direction with
            | ViewerKeyDirection.KeyDown -> true
            | ViewerKeyDirection.KeyUp -> false

        let baseKey, mods = parseModifiers event.RawKey
        normalize baseKey, isDown, mods

    // Issue 333 (epic 330): the R3 "wire the keymap into live dispatch" seam. A `Keymap` is pure data
    // (issue 331) and `Keymap.resolve` maps a `KeyId` to a `CommandId` (issue 332); this composes the
    // ViewerKey->KeyId bridge (`toKeyId`) with that resolve and a product `mapCommand` into the host
    // `MapKey : ViewerKey -> bool -> 'msg option` seam. Set a host's `MapKey` to this and editing the
    // keymap re-routes a key with no code change. Only key-DOWN resolves a command (matching the reducer's
    // `CommandResolved`-on-`KeyDown`); key-up and unbound/unmapped keys yield `None`.
    let mapKeyOfKeymap (keymap: Keymap) (mapCommand: CommandId -> 'msg option) : ViewerKey -> bool -> 'msg option =
        fun key isDown ->
            if isDown then
                Keymap.resolve keymap (toKeyId key) |> Option.bind mapCommand
            else
                None

    // Issue 456 (epic FS-GG/.github#416): the seam that loses nothing — and the one a rebind CAPTURE
    // needs, because `mapKeyOfKeymap` cannot serve one.
    //
    // `MapKey` is a closure fixed when the host record is built, and it never sees the model. So
    // `mapKeyOfKeymap` resolves against the keymap it CLOSED OVER, and it drops two things on the way:
    // key-up, and any key the keymap does not bind. A rebind capture needs exactly what it drops — the
    // key the user presses next is, by definition, one that is not bound yet — so the key the product is
    // waiting for is the one key that seam cannot deliver.
    //
    // The fix is not more state in the viewer: it is to stop resolving in the model-blind seam. Forward
    // the raw key and let the product route it in `update`, where the keymap and the capture state live.
    // A capture is then an ordinary model transition, and a rebind re-routes the very next key — with no
    // mutable closure and no new `ViewerEffect`.
    let mapKeyRaw (onKey: KeyId -> bool -> 'msg option) : ViewerKey -> bool -> 'msg option =
        fun key isDown -> onKey (toKeyId key) isDown
