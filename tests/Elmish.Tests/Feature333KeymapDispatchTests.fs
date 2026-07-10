module Feature333KeymapDispatchTests

// Issue 333 (epic 330): the keystone — the keymap must drive LIVE dispatch. Before this, nothing read a
// keymap at runtime; a product's key->command routing was hand-written in `MapKey` match arms, so
// "rebinding" meant editing code. `ViewerKeyboard.mapKeyOfKeymap` backs the host `MapKey` seam with a
// `Keymap` (pure data, issue 331) resolved through `Keymap.resolve` (issue 332): editing the keymap
// re-routes a key with NO code change. These tests prove that through the REAL Controls.Elmish routing
// (`Perf.runScriptToModel` folds a key script over the host's Update + retained step, consulting the
// host's `MapKey` seam exactly as the live viewer does) rather than by calling the helper in isolation.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// The product's message vocabulary and command->msg mapping. This is the ONLY product "code" in play,
// and it is IDENTICAL across every keymap below — only the keymap DATA changes between runs.
type private Msg = Command of CommandId

let private mapCommand (cmd: CommandId) : Msg option = Some(Command cmd)

let private size: Size = { Width = 200; Height = 120 }

// A host whose key seam is a keymap. The model records the commands the live routing dispatched; the
// View is an empty stack (nothing focusable to intercept the key upstream of the key tier).
let private hostFor (keymap: Keymap) : InteractiveAppHost<CommandId list, Msg> =
    { Init = fun () -> [], []
      Update = fun (Command cmd) model -> model @ [ cmd ], []
      View = fun _ _ -> Stack.create []
      Theme = Theme.light
      MapKey = ViewerKeyboard.mapKeyOfKeymap keymap mapCommand
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private noMods = ViewerKeyboard.noModifiers

// Drive the SAME key through the SAME host code; return the commands the live routing dispatched.
let private commandsFor (keymap: Keymap) (key: ViewerKey) : CommandId list =
    let model, _ =
        ControlsElmish.Perf.runScriptToModel (hostFor keymap) size [ FrameInput.Key(key, noMods) ]

    model

[<Tests>]
let tests =
    testList
        "Issue 333 keymap-driven live dispatch"
        [ test "a bound key routes to its command through the live host path" {
              let keymap = Keymap.empty |> Keymap.add "w" "MoveUp"

              Expect.equal
                  (commandsFor keymap (Letter 'w'))
                  [ "MoveUp" ]
                  "the keymap-backed MapKey resolves the bound key to its command in live dispatch"
          }

          test "editing the keymap re-routes the SAME key with NO code change (acceptance)" {
              // Two keymaps, one bound differently. Host construction, Update, and mapCommand are byte-for-byte
              // identical between the two runs — ONLY the Keymap value differs (a pure-data edit).
              let before = Keymap.empty |> Keymap.add "w" "MoveUp"
              let after = Keymap.empty |> Keymap.add "w" "Jump"

              Expect.equal (commandsFor before (Letter 'w')) [ "MoveUp" ] "before the edit, 'w' -> MoveUp"

              Expect.equal
                  (commandsFor after (Letter 'w'))
                  [ "Jump" ]
                  "after a pure-data keymap edit, the same 'w' now routes to Jump — no code change"
          }

          test "an unbound key dispatches nothing" {
              let keymap = Keymap.empty |> Keymap.add "w" "MoveUp"

              Expect.equal
                  (commandsFor keymap (Letter 'x'))
                  []
                  "a key absent from the keymap resolves to no command"
          } ]
