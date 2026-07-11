module Issue456HostKeyCaptureTests

// Issue #456 (epic FS-GG/.github#416 — the silent no-op family): `KeyboardEffect.RequestHostKeyCapture`
// had no host interpreter, and the one function that LOOKED like it lowered the request to a log string.
//
// Two halves, and these tests pin both:
//
// (1) THE DECOY IS DEAD. `interpretKeyboardEffect` used to lower `RequestHostKeyCapture key` to
//     `DispatchHostCommand $"capture-key:{key}"` — an `AdapterEffect` the framework never interprets
//     either, whose only consumer in the tree turns it into a printed string. No `ViewerEffect` case
//     carries a `KeyboardEffect` (`DispatchInput` is host->product only), so the request cannot reach a
//     host at all. It now yields a diagnostic that NAMES the uninterpreted effect.
//
// (2) THE CAPTURE COMPLETES. The reason it could not: `MapKey` is a closure fixed when the host record is
//     built and it NEVER sees the model, so `mapKeyOfKeymap` resolves against the keymap it closed over
//     and drops every key that keymap does not bind. A rebind capture needs exactly that — the key the
//     user presses next is by definition not bound yet — so the key the product waited for was the one key
//     the seam could not deliver. `ViewerKeyboard.mapKeyRaw` forwards the raw key instead; the product
//     routes it in `update`, where its keymap and capture state live.
//
// The capture tests drive the REAL routing (`Perf.runScriptToModel` folds a key script over the host's
// Update + retained step, consulting `host.MapKey` exactly as the live viewer does) rather than calling
// the helper in isolation — the gap being closed was a HOST-path gap, so a unit test of the helper would
// not have caught it.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private size: Size = { Width = 200; Height = 120 }
let private noMods = ViewerKeyboard.noModifiers

// ---------------------------------------------------------------------------------------------------
// (2) The rebind product: a keymap plus the capture state, BOTH in the model — which is the whole point.
// ---------------------------------------------------------------------------------------------------

/// The product's model. `Rebinding` is the armed capture: the command awaiting a new key.
type private Model =
    { Keymap: Keymap
      Rebinding: CommandId option
      Dispatched: CommandId list }

/// Every key-down and key-up arrives raw. The product — not the seam — decides what it means.
type private Msg = Key of KeyId * isDown: bool

let private mapKey = ViewerKeyboard.mapKeyRaw (fun key isDown -> Some(Key(key, isDown)))

/// The routing the model-blind `MapKey` seam CANNOT do, done where the model is in scope.
let private update (Key(key, isDown)) model =
    if not isDown then
        model, []
    else
        match model.Rebinding with
        // A capture is armed: the raw key becomes the new binding. `Escape` cancels instead of binding —
        // product policy, expressible precisely because the product sees the key.
        | Some _ when key = "Escape" -> { model with Rebinding = None }, []
        | Some command ->
            { model with
                Keymap = model.Keymap |> Keymap.rebind key command
                Rebinding = None },
            []
        // No capture armed: resolve the key through the keymap, as normal play.
        | None ->
            match Keymap.resolve model.Keymap key with
            | Some command ->
                { model with Dispatched = model.Dispatched @ [ command ] }, []
            | None -> model, []

let private hostFrom (initial: Model) : InteractiveAppHost<Model, Msg> =
    { Init = fun () -> initial, []
      Update = update
      View = fun _ _ -> Stack.create []
      Theme = Theme.light
      MapKey = mapKey
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// Drive keys through the real host path and return the resulting model.
let private run (initial: Model) (keys: ViewerKey list) : Model =
    let script = keys |> List.map (fun k -> FrameInput.Key(k, noMods))
    let model, _ = ControlsElmish.Perf.runScriptToModel (hostFrom initial) size script
    model

/// `w` -> MoveUp, and a rebind of MoveUp armed.
let private armed =
    { Keymap = Keymap.empty |> Keymap.add "w" "MoveUp"
      Rebinding = Some "MoveUp"
      Dispatched = [] }

let private idle = { armed with Rebinding = None }

[<Tests>]
let tests =
    testList
        "Issue 456 host key capture"
        [
          // -------------------------------------------------------------------------------------------
          // (2) The capture completes — the acceptance criterion, end to end through the live host path.
          // -------------------------------------------------------------------------------------------

          test "the OLD seam cannot see the key a capture waits for (the defect, pinned)" {
              // `mapKeyOfKeymap` is what issue 333 blessed and what a product would reach for. Resolve 'j'
              // — the unbound key a user presses to rebind — through it: it yields None. The product never
              // learns the key was pressed, so the capture it armed can never complete. This is the gap.
              let mapCommand cmd = Some(Key(cmd, true))
              let seam = ViewerKeyboard.mapKeyOfKeymap idle.Keymap mapCommand

              Expect.isNone
                  (seam (Letter 'j') true)
                  "the keymap-resolving seam drops an UNBOUND key — which is exactly the key a rebind capture is waiting for"
          }

          test "capture->rebind: an unbound key is captured and becomes the new binding (acceptance)" {
              // The user armed a rebind of MoveUp, then pressed 'j' — a key bound to nothing.
              let after = run armed [ Letter 'j' ]

              Expect.equal after.Rebinding None "the armed capture completed and disarmed"

              Expect.equal
                  (Keymap.resolve after.Keymap "j")
                  (Some "MoveUp")
                  "the captured key is now bound to the command that armed the capture — the capture FIRED"
          }

          test "the captured key routes live on the very next press (the rebind is real)" {
              // Arm, capture 'j', then press 'j' again — as a player would. The rebind must take effect
              // immediately, on the same host, with no reconstruction: the seam is model-blind, so this
              // only works because `update` (which SEES the model) does the resolving.
              let after = run armed [ Letter 'j'; Letter 'j' ]

              Expect.equal
                  after.Dispatched
                  [ "MoveUp" ]
                  "the key captured a moment ago now dispatches its command through the SAME live host"
          }

          test "a key press with no capture armed still routes through the keymap (no regression)" {
              let after = run idle [ Letter 'w' ]

              Expect.equal after.Dispatched [ "MoveUp" ] "ordinary play is unaffected: 'w' still resolves to MoveUp"
          }

          test "an unbound key with no capture armed dispatches nothing (no false capture)" {
              let after = run idle [ Letter 'j' ]

              Expect.equal after.Dispatched [] "an unbound key resolves to no command when no capture is armed"
              Expect.equal (Keymap.resolve after.Keymap "j") None "and it does NOT silently become a binding"
          }

          test "the product can DECLINE a captured key (Escape cancels, binding nothing)" {
              // `mapKeyRaw` forwards the key and imposes no policy, so 'cancel' is the product's to define.
              let after = run armed [ Escape ]

              Expect.equal after.Rebinding None "Escape cancelled the armed capture"
              Expect.equal (Keymap.resolve after.Keymap "Escape") None "Escape did NOT get bound to the command"
              Expect.equal (Keymap.resolve after.Keymap "w") (Some "MoveUp") "the original binding survives a cancel"
          }

          test "mapKeyRaw forwards key-UP too — the old seam dropped it silently" {
              let seam = ViewerKeyboard.mapKeyRaw (fun key isDown -> Some(Key(key, isDown)))

              Expect.equal (seam (Letter 'j') false) (Some(Key("j", false))) "a key-up reaches the product"
              Expect.equal (seam (Letter 'j') true) (Some(Key("j", true))) "a key-down reaches the product"
              Expect.isNone (ViewerKeyboard.mapKeyOfKeymap idle.Keymap (fun c -> Some c) (Letter 'w') false)
                  "the keymap seam, by contrast, drops key-up entirely"
          }

          // -------------------------------------------------------------------------------------------
          // (1) The decoy is dead: the request that no host interprets now SAYS so.
          // -------------------------------------------------------------------------------------------

          test "RequestHostKeyCapture raises a diagnostic naming the uninterpreted effect (acceptance)" {
              let command =
                  ControlsElmish.interpretKeyboardEffect id (RequestHostKeyCapture "j")

              let diagnostics = AdapterCmd.diagnostics command

              Expect.hasLength diagnostics 1 "the uninterpretable request produces exactly one diagnostic"

              let d = List.head diagnostics
              Expect.equal d.Source "keyboard-input" "the diagnostic is sourced to the keyboard package"
              Expect.equal d.Code "HostKeyCaptureNotInterpreted" "the code names the defect"

              Expect.stringContains
                  d.Message
                  "RequestHostKeyCapture"
                  "the message NAMES the effect that is not interpreted"

              Expect.stringContains
                  d.Message
                  "mapKeyRaw"
                  "and points at the seam that DOES capture a key, so the diagnostic is actionable"
          }

          test "RequestHostKeyCapture no longer lowers to a host command (the decoy is gone)" {
              let command =
                  ControlsElmish.interpretKeyboardEffect id (RequestHostKeyCapture "j")

              // The old arm produced `DispatchHostCommand "capture-key:j"` — an effect nothing interprets,
              // whose only consumer anywhere turns it into a log string. A request that cannot be served
              // must not look served.
              let hostCommands =
                  command
                  |> List.choose (function
                      | DispatchHostCommand name -> Some name
                      | _ -> None)

              Expect.isEmpty hostCommands "no DispatchHostCommand is emitted — the decoy lowering is gone"

              Expect.isEmpty
                  (AdapterCmd.productMessages command)
                  "and it dispatches no product message: nothing pretends the capture happened"
          }

          test "the other keyboard effects are untouched by the fix" {
              let resolved = ControlsElmish.interpretKeyboardEffect id (CommandResolved "MoveUp")

              Expect.equal
                  (AdapterCmd.productMessages resolved)
                  [ "MoveUp" ]
                  "CommandResolved still dispatches its command as a product message"

              Expect.isEmpty (AdapterCmd.diagnostics resolved) "and raises no diagnostic"

              let echo = ControlsElmish.interpretKeyboardEffect id (KeyStateChanged [ "w" ])
              Expect.isEmpty echo "a state-echo effect still carries no host action"
          } ]
