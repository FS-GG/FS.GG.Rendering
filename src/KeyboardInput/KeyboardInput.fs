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

    let noModifiers =
        { Ctrl = false
          Alt = false
          Shift = false
          Meta = false }

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

    let normalizeEventWithModifiers event =
        let isDown =
            match event.Direction with
            | ViewerKeyDirection.KeyDown -> true
            | ViewerKeyDirection.KeyUp -> false

        let baseKey, mods = parseModifiers event.RawKey
        normalize baseKey, isDown, mods
