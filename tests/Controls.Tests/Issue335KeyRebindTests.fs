module ControlsIssue335KeyRebindTests

// Issue 335 (epic 330): the key-rebind config-screen control. Proves, deterministically and headlessly:
//   - it LISTS the current bindings at a fixed width (render → visible text rows);
//   - it SURFACES a keymap conflict (Keymap.validate, issue 332) as a visible row;
//   - a CAPTURE→REBIND edit shows the new key AND re-routes the key in live dispatch through
//     `ViewerKeyboard.mapKeyOfKeymap` (issue 333, R3) — the config screen and the live host agree.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Themes.Default
open Rendering.Harness.TestAssertions

let private theme = Theme.light
let private sz w h : Size = { Width = w; Height = h }
let private renderedTexts size control =
    (Control.renderTree theme size control).Scene |> renderedText |> List.map (fun r -> r.Text)

// The product's command→msg mapping used to prove live re-routing (issue 333). Fixed code across edits.
type private Msg = Command of CommandId
let private mapCommand (cmd: CommandId) : Msg option = Some(Command cmd)

[<Tests>]
let tests =
    testList
        "Issue 335 key-rebind config screen"
        [ test "lists the current command→key bindings as fixed-width text rows" {
              let keymap = Keymap.empty |> Keymap.add "w" "MoveUp" |> Keymap.add "s" "MoveDown"
              let texts = renderedTexts (sz 240 160) (KeyRebind.ofKeymap keymap [])

              Expect.contains texts "MoveUp — w" "the MoveUp binding is listed"
              Expect.contains texts "MoveDown — s" "the MoveDown binding is listed"
          }

          test "surfaces a keymap conflict (Keymap.validate) as a visible row" {
              // Two keys bound to one command — a SharedCommandBinding conflict (issue 332).
              let keymap = Keymap.empty |> Keymap.add "w" "MoveUp" |> Keymap.add "up" "MoveUp"
              let texts = renderedTexts (sz 240 160) (KeyRebind.ofKeymap keymap [])

              Expect.isTrue
                  (texts |> List.exists (fun t -> t.StartsWith "conflict:"))
                  "a conflict row is rendered when validate reports a shared-command binding"
          }

          test "capture→rebind shows the new key and re-routes it in live dispatch (R3, #333)" {
              let before = Keymap.empty |> Keymap.add "w" "MoveUp"

              // The user rebinds MoveUp: the host captures key "j" (RequestHostKeyCapture), the product
              // drops the old key and rebinds — a pure Keymap edit, no control code change.
              let after = before |> Keymap.remove "w" |> Keymap.rebind "j" "MoveUp"

              // (a) the config screen now shows the new binding
              let texts = renderedTexts (sz 240 160) (KeyRebind.ofKeymap after [])
              Expect.contains texts "MoveUp — j" "the rebound key is shown"
              Expect.isFalse (List.contains "MoveUp — w" texts) "the old binding is gone"

              // (b) the SAME edit re-routes the key live via issue 333's seam — 'j' now resolves, 'w' no longer.
              let mapKey = ViewerKeyboard.mapKeyOfKeymap after mapCommand
              Expect.equal (mapKey (Letter 'j') true) (Some(Command "MoveUp")) "'j' now routes to MoveUp in live dispatch"
              Expect.equal (mapKey (Letter 'w') true) None "'w' no longer routes to any command"
          } ]
