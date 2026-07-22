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
          }

          test "onRebind dispatches the activated command id from the event payload" {
              let control =
                  KeyRebind.ofKeymap (Keymap.empty |> Keymap.add "w" "MoveUp") [ KeyRebind.onRebind Command ]

              // The `onRebind` attribute normalizes to the `rebind` event kind (as `onClose` → `close`);
              // the activated command id rides the event's nav text.
              let ev: ControlEvent =
                  { Kind = "rebind"
                    ControlId = None
                    Origin = ControlEventOrigin.Pointer
                    Nav = Some(EditedText "MoveUp") }

              Expect.equal (Control.dispatch ev control) [ Command "MoveUp" ] "onRebind fires the activated command id"
          }

          test "action catalog keeps unbound rows visible with player labels and explicit order" {
              let catalog =
                  [ { Command = "fire"; Label = "Fire"; Order = 20; Binding = Some "Space"; DefaultBinding = Some "Space" }
                    { Command = "move-up"; Label = "Move Up"; Order = 10; Binding = None; DefaultBinding = Some "W" } ]

              let texts = renderedTexts (sz 300 200) (KeyRebind.ofActions catalog [])
              let moveIndex = texts |> List.findIndex ((=) "Move Up — Unbound")
              let fireIndex = texts |> List.findIndex ((=) "Fire — Space")
              Expect.isLessThan moveIndex fireIndex "explicit order wins over command/key ordering"
              Expect.contains texts "Reset controls to defaults" "defaults expose a reset affordance"
          }

          test "catalog projection and defaults preserve metadata while rebuilding bindings" {
              let catalog =
                  [ { Command = "move-up"; Label = "Move Up"; Order = 1; Binding = None; DefaultBinding = Some "W" }
                    { Command = "fire"; Label = "Fire"; Order = 2; Binding = None; DefaultBinding = None } ]
              let current = Keymap.empty |> Keymap.assignKey "Z" "move-up"
              let projected = KeyRebind.withBindings current catalog
              Expect.equal projected.Head.Binding (Some "Z") "the current binding projects onto stable metadata"
              Expect.equal projected.Tail.Head.Binding None "an absent action remains explicitly unbound"
              let defaults = KeyRebind.restoreDefaults projected
              Expect.equal (Keymap.resolve defaults "W") (Some "move-up") "reset restores the declared default"
              Expect.equal (Keymap.resolve defaults "Z") None "reset drops the player's previous binding"
          }

          test "onReset dispatches the fixed reset message" {
              let control = KeyRebind.ofActions [] [ KeyRebind.onReset (Command "reset") ]
              let ev: ControlEvent =
                  { Kind = "reset"; ControlId = None; Origin = ControlEventOrigin.Pointer; Nav = None }
              Expect.equal (Control.dispatch ev control) [ Command "reset" ] "reset event is addressable"
          }

          test "catalog-aware rebind maps the player label back to the stable command id" {
              let catalog =
                  [ { Command = "move-up"; Label = "Move Up"; Order = 1; Binding = None; DefaultBinding = Some "W" } ]
              let control = KeyRebind.ofActions catalog [ KeyRebind.onActionRebind catalog Command ]
              let ev: ControlEvent =
                  { Kind = "rebind"
                    ControlId = None
                    Origin = ControlEventOrigin.Pointer
                    Nav = Some(EditedText "Move Up — Unbound") }
              Expect.equal (Control.dispatch ev control) [ Command "move-up" ] "display text never leaks into the runtime command id"
          } ]
