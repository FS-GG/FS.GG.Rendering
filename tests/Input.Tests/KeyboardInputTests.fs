module FS.Skia.UI.KeyboardInputTests

open Expecto
open FS.Skia.UI.Input
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer.Host

let registry =
    [ { Id = "move.left"; DisplayName = "Move left"; Category = Some "movement" }
      { Id = "move.right"; DisplayName = "Move right"; Category = Some "movement" }
      { Id = "select.line"; DisplayName = "Select line"; Category = Some "selection" }
      { Id = "copy.selection"; DisplayName = "Copy selection"; Category = Some "editing" }
      { Id = "delete.selection"; DisplayName = "Delete selection"; Category = Some "editing" }
      { Id = "open.palette"; DisplayName = "Open palette"; Category = Some "popup" } ]
    |> KeyboardInput.commandRegistry
    |> function
        | Result.Ok registry -> registry
        | Result.Error diagnostics -> failtestf "registry failed: %A" diagnostics

let modalYaml = """
version: 1
defaultLayout: qwerty
defaultMode: selection
layouts:
  - id: qwerty
    displayName: QWERTY
    labels:
      KeyH: h
      KeyL: l
      Space: space
      KeyC: c
      KeyD: d
      Digit1: "1"
    positions:
      - id: KeyH
        hand: right
        finger: index
        row: 2
        column: 6
      - id: KeyL
        hand: right
        finger: ring
        row: 2
        column: 9
      - id: Space
        hand: either
        finger: thumb
        row: 4
        column: 5
      - id: KeyC
        hand: left
        finger: middle
        row: 3
        column: 2
      - id: KeyD
        hand: left
        finger: middle
        row: 2
        column: 2
      - id: Digit1
        hand: left
        finger: pinky
        row: 0
        column: 0
  - id: dvorak
    displayName: Dvorak
    labels:
      KeyH: d
      KeyL: n
      Space: space
      KeyC: j
      KeyD: e
      Digit1: "1"
    positions:
      - id: KeyH
        hand: right
        finger: index
        row: 2
        column: 6
modes:
  - id: selection
    displayName: Selection
    kind: stateful
    states: [character, line]
    defaultState: character
  - id: space
    displayName: Space menu
    kind: popup
    cancelKeys: [Escape]
  - id: copy
    displayName: Copy
    kind: temporary
  - id: delete
    displayName: Delete
    kind: temporary
bindings:
  - mode: selection
    key: KeyH
    outcome: command
    command: move.left
    weight: 10
  - mode: selection
    key: KeyL
    outcome: command
    command: move.right
    weight: 9
  - mode: selection
    key: Digit1
    outcome: set-state
    targetMode: selection
    state: line
  - mode: selection
    key: Space
    outcome: popup
    targetMode: space
  - mode: selection
    key: KeyC
    outcome: temporary
    targetMode: copy
  - mode: selection
    key: KeyD
    outcome: temporary
    targetMode: delete
  - mode: space
    key: KeyH
    outcome: command
    command: open.palette
  - mode: copy
    key: KeyH
    outcome: command
    command: copy.selection
    held: [KeyC]
  - mode: delete
    key: KeyH
    outcome: command
    command: delete.selection
    held: [KeyD]
disambiguation:
  timeoutMilliseconds: 175
bigramProfile:
  suggestionLimit: 20
  weights:
    - first: move.left
      second: move.right
      weight: 120
    - first: copy.selection
      second: delete.selection
      weight: 40
display:
  showLayoutState: true
  showPendingSequence: true
commandIntents:
  - id: risky-delete
    command: delete.selection
    constraints: [approval-required]
"""

let canonical () =
    match KeyboardInput.parseYaml modalYaml with
    | Result.Error diagnostics -> failtestf "parse failed: %A" diagnostics
    | Result.Ok config ->
        match KeyboardInput.validate registry config with
        | Result.Ok model -> model
        | Result.Error diagnostics -> failtestf "validate failed: %A" diagnostics

let initialized () =
    match KeyboardInput.init "qwerty" (canonical ()) with
    | Result.Ok(runtime, effects) -> runtime, effects
    | Result.Error diagnostics -> failtestf "init failed: %A" diagnostics

let commandIds effects =
    effects
    |> List.choose (function
        | CommandResolved resolved -> Some resolved.CommandId
        | _ -> None)

let display options effects runtime =
    KeyboardInput.keyboardStateDisplay options effects runtime

[<Tests>]
let keyboardInputTests =
    testList "Keyboard input framework" [
        test "valid modal YAML parses and validates into inspectable canonical model" {
            let model = canonical ()
            Expect.equal model.Configuration.DefaultMode "selection" "default stateful mode is retained"
            Expect.equal model.Configuration.Layouts.Length 2 "multiple layouts are loaded"
            Expect.equal model.Configuration.CommandIntents.Length 1 "optional intent data is loaded separately"
            Expect.isTrue (model.Configuration.Bindings |> List.exists (fun b -> b.ModeId = "space")) "popup bindings are canonical"
        }

        test "stateful mode initializes with default state and layout-state display data" {
            let runtime, effects = initialized ()
            Expect.equal runtime.ModeStack.Head.ModeId "selection" "base mode is selection"
            Expect.equal runtime.ModeStack.Head.State (Some "character") "default state is explicit"
            Expect.isTrue (effects |> List.exists (function LayoutStateChanged _ -> true | _ -> false)) "init emits layout state"

            let view = KeyboardInput.layoutState runtime
            Expect.equal view.ActiveLayout.Id "qwerty" "layout state includes active layout"
            Expect.equal view.ActiveLabels.["KeyH"] "h" "layout state exposes labels"
        }

        test "normal, popup, temporary held, state transition, replay, and focus-loss paths run through public update" {
            let runtime, _ = initialized ()

            let afterMove, moveEffects = KeyboardInput.update (InputMsg.KeyDown "KeyH") runtime
            Expect.contains (commandIds moveEffects) "move.left" "normal binding resolves command"
            Expect.equal afterMove.ModeStack.Head.State (Some "character") "stateful mode remains inspectable"

            let afterState, _ = KeyboardInput.update (InputMsg.KeyDown "Digit1") afterMove
            Expect.equal afterState.ModeStack.Head.State (Some "line") "set-state binding changes the stateful frame"

            let afterPopup, _ = KeyboardInput.update (InputMsg.KeyDown "Space") afterState
            Expect.equal (afterPopup.ModeStack |> List.last).ModeId "space" "popup is pushed on top"
            let afterPopupCommand, popupEffects = KeyboardInput.update (InputMsg.KeyDown "KeyH") afterPopup
            Expect.contains (commandIds popupEffects) "open.palette" "popup command resolves"
            Expect.equal (afterPopupCommand.ModeStack |> List.last).ModeId "selection" "popup pops after command"
            Expect.equal afterPopupCommand.ModeStack.Head.State (Some "line") "underlying state is restored"

            let afterHold, _ = KeyboardInput.update (InputMsg.KeyDown "KeyC") afterPopupCommand
            Expect.equal (afterHold.ModeStack |> List.last).ModeId "copy" "temporary mode is pushed"
            let afterCopy, copyEffects = KeyboardInput.update (InputMsg.KeyDown "KeyH") afterHold
            Expect.contains (commandIds copyEffects) "copy.selection" "held binding resolves while key is pressed"
            let afterRelease, _ = KeyboardInput.update (InputMsg.KeyUp "KeyC") afterCopy
            Expect.isFalse (afterRelease.ModeStack |> List.exists (fun frame -> frame.ModeId = "copy")) "temporary mode pops on release"

            let replayed, replayEffects =
                KeyboardInput.replay runtime [ InputMsg.KeyDown "Space"; InputMsg.KeyDown "KeyH"; InputMsg.KeyDown "KeyC"; InputMsg.KeyDown "KeyH"; InputMsg.KeyUp "KeyC" ]
            Expect.contains (commandIds replayEffects) "open.palette" "replay preserves popup command"
            Expect.contains (commandIds replayEffects) "copy.selection" "replay preserves held command"
            Expect.isFalse (replayed.ModeStack |> List.exists (fun frame -> frame.ModeId = "copy")) "replay reaches same release state"

            let recovered, focusEffects = KeyboardInput.update InputMsg.FocusLost afterHold
            Expect.isEmpty recovered.PressedKeys "focus loss clears pressed keys"
            Expect.isTrue (focusEffects |> List.exists (function InputDiagnosticEmitted d when d.Code = LostKeyReleaseRecovered -> true | _ -> false)) "focus loss emits recovery diagnostic"
        }

        test "invalid YAML fixtures reject duplicate bindings, unregistered commands, host actions, bad layouts, and bad states" {
            let assertDiagnostic expected yaml =
                match KeyboardInput.parseYaml yaml with
                | Result.Error diagnostics -> Expect.isTrue (diagnostics |> List.exists (fun d -> d.Code = expected)) $"parse reported {expected}"
                | Result.Ok config ->
                    match KeyboardInput.validate registry config with
                    | Result.Ok _ -> failtestf "expected %A diagnostic" expected
                    | Result.Error diagnostics -> Expect.isTrue (diagnostics |> List.exists (fun d -> d.Code = expected)) $"validate reported {expected}"

            assertDiagnostic DuplicateBinding (modalYaml.Replace("disambiguation:", "  - mode: selection\n    key: KeyH\n    outcome: command\n    command: move.left\ndisambiguation:"))
            assertDiagnostic UnknownCommand (modalYaml.Replace("command: move.left", "command: app.unknown"))
            assertDiagnostic HostActionRejected (modalYaml.Replace("command: move.left", "command: host:shell.exec"))
            assertDiagnostic UnknownLayout (modalYaml.Replace("defaultLayout: qwerty", "defaultLayout: workman"))
            assertDiagnostic InvalidModeState (modalYaml.Replace("defaultState: character", "defaultState: block"))
        }

        test "layout changes and bigram analysis are public, deterministic, and non-mutating" {
            let runtime, _ = initialized ()
            let switched, switchEffects = KeyboardInput.update (InputMsg.SetLayout "dvorak") runtime
            Expect.equal switched.ActiveLayout "dvorak" "layout can change at runtime"
            Expect.isTrue (switchEffects |> List.exists (function LayoutStateChanged view when view.ActiveLayout.Id = "dvorak" -> true | _ -> false)) "layout state effect reports active layout"

            let before = canonical ()
            let report = KeyboardInput.analyzeBigrams before "qwerty"
            let after = canonical ()
            Expect.equal report.TopPairs.Length 2 "bigram report ranks top pairs"
            Expect.isTrue (report.Risks |> List.exists (fun risk -> risk.Kind = SameHandRepeat || risk.Kind = SameFinger || risk.Kind = LongTravel)) "ergonomic risks are flagged"
            Expect.equal before.Configuration.Bindings after.Configuration.Bindings "analysis does not rewrite keymaps"
        }

        test "viewer keyboard events can drive runtime and render an optional layout overlay scene" {
            let runtime, _ = initialized ()

            Expect.equal (KeyboardInput.viewerInputMsg (ViewerEvent.KeyDown "H")) (Some(InputMsg.KeyDown "KeyH")) "viewer H key maps to physical KeyH"
            Expect.equal (KeyboardInput.viewerInputMsg (ViewerEvent.KeyUp "Space")) (Some(InputMsg.KeyUp "Space")) "viewer Space key-up maps to input key-up"
            Expect.equal (KeyboardInput.viewerInputMsg (PointerMoved(10.0, 12.0))) None "pointer input is ignored by keyboard adapter"

            let next, effects = KeyboardInput.updateFromViewerEvent (ViewerEvent.KeyDown "H") runtime
            Expect.equal next.PressedKeys (Set.ofList [ "KeyH" ]) "window keydown is captured in input runtime"
            Expect.contains (commandIds effects) "move.left" "window keydown resolves command through keyboard input"

            let overlay = KeyboardInput.renderLayoutState next
            let kinds = Scene.describe overlay
            Expect.contains kinds RectangleElement "layout overlay includes a panel"
            Expect.contains kinds TextRunElement "layout overlay includes visible small text"
        }

        test "keyboard state display options expose compact, expanded, and hidden public models" {
            let runtime, effects = initialized ()

            let compact = display KeyboardInput.compactStateDisplayOptions effects runtime
            Expect.equal compact.Visibility KeyboardStateDisplayVisible "compact display is visible"
            Expect.equal compact.Density KeyboardStateDisplayCompact "compact density is selected"
            Expect.equal (compact.Layout |> Option.map _.Id) (Some "qwerty") "active layout id is projected"
            Expect.equal (compact.Layout |> Option.bind _.DisplayName) (Some "QWERTY") "active layout display name is projected"
            Expect.equal (compact.TopContext |> Option.map _.ModeId) (Some "selection") "top context is projected"
            Expect.equal compact.ActiveState (Some "character") "active state is projected"

            let expanded = display KeyboardInput.expandedStateDisplayOptions effects runtime
            Expect.equal expanded.Density KeyboardStateDisplayExpanded "expanded density is selected"
            Expect.isGreaterThanOrEqual expanded.Labels.Length compact.Labels.Length "expanded keeps at least as many labels as compact"

            let hiddenOptions =
                { KeyboardInput.defaultStateDisplayOptions with
                    Visibility = KeyboardStateDisplayHidden }

            let hidden = display hiddenOptions effects runtime
            Expect.equal hidden.Visibility KeyboardStateDisplayHidden "hidden model records hidden visibility"
            Expect.isNone hidden.Layout "hidden model omits layout"
            Expect.isEmpty hidden.Stack "hidden model omits stack"
            Expect.isEmpty hidden.Labels "hidden model omits labels"
            Expect.equal (KeyboardInput.renderKeyboardStateDisplay hiddenOptions effects runtime |> Scene.describe) [ EmptyElement ] "hidden display renders an empty scene"
        }

        test "keyboard state display follows public update effects for layout, state, command, and scene rendering" {
            let runtime, initEffects = initialized ()
            let compact = display KeyboardInput.compactStateDisplayOptions initEffects runtime
            Expect.equal (compact.Layout |> Option.map _.Id) (Some "qwerty") "initial display reflects init layout"

            let afterState, stateEffects = KeyboardInput.update (InputMsg.KeyDown "Digit1") runtime
            let stateDisplay = display KeyboardInput.compactStateDisplayOptions stateEffects afterState
            Expect.equal stateDisplay.ActiveState (Some "line") "display reflects update-driven state change"
            Expect.isTrue (stateEffects |> List.exists (function LayoutStateChanged _ -> true | _ -> false)) "state update emits display-relevant layout state"

            let afterMove, moveEffects = KeyboardInput.update (InputMsg.KeyDown "KeyH") afterState
            let commandDisplay = display KeyboardInput.compactStateDisplayOptions moveEffects afterMove
            Expect.equal (commandDisplay.RecentCommand |> Option.map _.CommandId) (Some "move.left") "most recent command comes from effects"

            let afterLayout, layoutEffects = KeyboardInput.update (InputMsg.SetLayout "dvorak") afterMove
            let layoutDisplay = display KeyboardInput.compactStateDisplayOptions layoutEffects afterLayout
            Expect.equal (layoutDisplay.Layout |> Option.map _.Id) (Some "dvorak") "display reflects update-driven layout change"

            let rendered = KeyboardInput.renderKeyboardStateDisplayAt (32.0, 44.0) KeyboardInput.compactStateDisplayOptions moveEffects afterMove
            let kinds = Scene.describe rendered
            Expect.contains kinds RectangleElement "state display scene includes a panel"
            Expect.contains kinds TextRunElement "state display scene includes text primitives"
        }

        test "keyboard state display distinguishes popup, held, stateful, persistent, unknown, and condensed stack entries" {
            let runtime, _ = initialized ()
            let afterPopup, _ = KeyboardInput.update (InputMsg.KeyDown "Space") runtime
            let popupDisplay = display KeyboardInput.expandedStateDisplayOptions [] afterPopup
            Expect.equal (popupDisplay.TopContext |> Option.map _.Kind) (Some DisplayPopupContext) "popup context is identified"
            Expect.equal (popupDisplay.TopContext |> Option.map _.IsPersistent) (Some false) "popup is not persistent"

            let afterHold, _ = KeyboardInput.update (InputMsg.KeyDown "KeyC") runtime
            let heldDisplay = display KeyboardInput.expandedStateDisplayOptions [] afterHold
            Expect.equal (heldDisplay.TopContext |> Option.map _.Kind) (Some DisplayTemporaryHeldContext) "temporary held context is identified"
            Expect.equal (heldDisplay.TopContext |> Option.bind _.EnteredBy) (Some "KeyC") "held entry records source key"

            let baseEntry = heldDisplay.Stack |> List.head
            Expect.equal baseEntry.Kind DisplayStatefulContext "base stateful context is identified"
            Expect.isTrue baseEntry.IsPersistent "stateful context is persistent"

            let unknownRuntime =
                { runtime with
                    ModeStack = runtime.ModeStack @ [ { ModeId = "missing-mode"; State = None; EnteredBy = None } ] }

            let unknownDisplay = display KeyboardInput.expandedStateDisplayOptions [] unknownRuntime
            Expect.equal (unknownDisplay.TopContext |> Option.map _.Kind) (Some DisplayUnknownContext) "unknown context is explicit"
            Expect.isTrue unknownDisplay.IsPartial "unknown context makes display partial"

            let deepRuntime =
                { runtime with
                    ModeStack =
                        [ { ModeId = "selection"; State = Some "character"; EnteredBy = None }
                          { ModeId = "space"; State = None; EnteredBy = Some "Space" }
                          { ModeId = "copy"; State = None; EnteredBy = Some "KeyC" }
                          { ModeId = "delete"; State = None; EnteredBy = Some "KeyD" }
                          { ModeId = "missing-mode"; State = None; EnteredBy = None } ] }

            let condensed = display KeyboardInput.compactStateDisplayOptions [] deepRuntime
            Expect.equal condensed.Stack.Length 4 "compact display condenses deep stacks"
            Expect.contains condensed.Omitted (OmittedStackEntries 1) "compact display records omitted stack entries"
        }

        test "keyboard state display derives current-context labels, caps, pending sequence, diagnostics, and invalid-layout partial state" {
            let runtime, _ = initialized ()
            let afterPopup, _ = KeyboardInput.update (InputMsg.KeyDown "Space") runtime
            let popupDisplay = display KeyboardInput.expandedStateDisplayOptions [] afterPopup
            Expect.equal (popupDisplay.Labels |> List.map _.CommandId) [ Some "open.palette" ] "labels come only from active top context"
            Expect.equal (popupDisplay.Labels |> List.map _.KeyPositionId) [ "KeyH" ] "top context label uses active layout key"

            let cappedOptions =
                { KeyboardInput.compactStateDisplayOptions with
                    MaxCompactLabels = 1 }

            let capped = display cappedOptions [] runtime
            Expect.equal capped.Labels.Length 1 "compact label cap is applied"
            Expect.isTrue (capped.Omitted |> List.exists (function OmittedLabels count when count > 0 -> true | _ -> false)) "omitted labels are recorded"

            let pendingRuntime =
                { runtime with
                    PendingSequence =
                        Some
                            { StartedAt = System.DateTimeOffset.UnixEpoch
                              Chords = [ { Position = "Space"; RequiredHeld = [] } ] } }

            let pendingDisplay = display KeyboardInput.expandedStateDisplayOptions [] pendingRuntime
            Expect.equal (pendingDisplay.PendingSequence |> Option.map _.TimeoutMilliseconds) (Some(Some 175)) "pending sequence includes timeout policy"

            let noPendingOptions =
                { KeyboardInput.expandedStateDisplayOptions with
                    ShowPendingSequence = false }

            let omittedPending = display noPendingOptions [] pendingRuntime
            Expect.isNone omittedPending.PendingSequence "option can omit pending sequence"
            Expect.contains omittedPending.Omitted OmittedPendingSequence "pending omission is recorded"

            let invalidRuntime = { runtime with ActiveLayout = "missing-layout" }
            let invalidDisplay = display KeyboardInput.expandedStateDisplayOptions [] invalidRuntime
            Expect.isTrue invalidDisplay.IsPartial "invalid active layout creates partial display"
            Expect.equal (invalidDisplay.Layout |> Option.map _.IsAvailable) (Some false) "invalid layout is still represented"
            Expect.equal (invalidDisplay.Diagnostic |> Option.map _.Code) (Some UnknownLayout) "invalid layout produces actionable diagnostic"

            let _, infoEffects = KeyboardInput.update (InputMsg.KeyDown "Escape") runtime
            let infoDisplay = display KeyboardInput.expandedStateDisplayOptions infoEffects runtime
            Expect.isNone infoDisplay.Diagnostic "non-actionable info diagnostics are filtered"
        }

        test "keyboard state display handles held-layer release recovery through public update" {
            let runtime, _ = initialized ()
            let stacked =
                { runtime with
                    PressedKeys = Set.ofList [ "KeyC"; "KeyD" ]
                    ModeStack =
                        runtime.ModeStack
                        @ [ { ModeId = "copy"; State = None; EnteredBy = Some "KeyC" }
                            { ModeId = "delete"; State = None; EnteredBy = Some "KeyD" } ] }

            let afterRelease, effects = KeyboardInput.update (InputMsg.KeyUp "KeyC") stacked
            let recovered = display KeyboardInput.expandedStateDisplayOptions effects afterRelease
            Expect.isFalse (recovered.Stack |> List.exists (fun entry -> entry.ModeId = "copy")) "released held layer is removed"
            Expect.isTrue (recovered.Stack |> List.exists (fun entry -> entry.ModeId = "delete")) "other held layer remains active"
            Expect.equal (recovered.TopContext |> Option.map _.ModeId) (Some "delete") "top context recovers after out-of-order release"
        }

        test "standard input works when command intent is absent and invalid intent data is diagnosed" {
            let withoutIntent = modalYaml.Replace("commandIntents:\n  - id: risky-delete\n    command: delete.selection\n    constraints: [approval-required]\n", "")
            let model =
                match KeyboardInput.parseYaml withoutIntent with
                | Result.Ok config ->
                    match KeyboardInput.validate registry config with
                    | Result.Ok model -> model
                    | Result.Error diagnostics -> failtestf "validate without intent failed: %A" diagnostics
                | Result.Error diagnostics -> failtestf "parse without intent failed: %A" diagnostics

            let runtime, _ =
                match KeyboardInput.init "qwerty" model with
                | Result.Ok value -> value
                | Result.Error diagnostics -> failtestf "init without intent failed: %A" diagnostics

            let _, effects = KeyboardInput.update (InputMsg.KeyDown "KeyH") runtime
            Expect.contains (commandIds effects) "move.left" "standard key input does not require command grammar"

            let badIntent = modalYaml.Replace("command: delete.selection", "command: automate.unknown")
            match KeyboardInput.parseYaml badIntent with
            | Result.Ok config ->
                match KeyboardInput.validate registry config with
                | Result.Ok _ -> failtest "expected bad command intent to fail validation"
                | Result.Error diagnostics -> Expect.isTrue (diagnostics |> List.exists (fun d -> d.Code = UnsatisfiedCommandIntent)) "invalid intent is diagnosed"
            | Result.Error diagnostics -> failtestf "parse bad intent failed: %A" diagnostics
        }
    ]
