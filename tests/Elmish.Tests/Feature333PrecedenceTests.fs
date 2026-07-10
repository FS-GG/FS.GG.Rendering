module Feature333PrecedenceTests

// Issue 333 follow-up (code-review on #345): prove the keymap-backed `MapKey`
// (`ViewerKeyboard.mapKeyOfKeymap`) sits at the LAST tier of the focus-first routing order —
// authored EventBindings → focus traversal → chord → key. `Feature333KeymapDispatchTests` drove the
// single-key `runScriptToModel` fast path, which consults `host.MapKey` directly and never exercises
// the focus tier; this drives the REAL `routeFocusedKey` seam (the same one `runInteractiveApp`'s
// `mapKey` wires, `ControlsElmish.fs:1897`) so the precedence claim is actually asserted:
//   - when a focused control consumes a key, the focus tier returns the activation and `mapKey` returns
//     it WITHOUT consulting `host.MapKey` — so a keymap-backed MapKey can never shadow a focused control;
//   - when the focus tier consumes nothing, `host.MapKey` (the keymap) is the fallthrough
//     (Feature094: "an unmatched key produces no product message → host then consults MapKey").

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | Activated
    | Command of CommandId

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 200 }
let private mapCommand (cmd: CommandId) : Msg option = Some(Command cmd)

// The keymap-backed host `MapKey`: it binds Enter (which the button ALSO activates on) and 'q' (which no
// focused control consumes). Same seam a product wires via issue 333.
let private keymap = Keymap.empty |> Keymap.add "Enter" "Confirm" |> Keymap.add "q" "Quit"
let private hostMapKey = ViewerKeyboard.mapKeyOfKeymap keymap mapCommand

let private view: Control<Msg> =
    Stack.create
        [ Stack.children [ Button.create [ Button.text "Go"; Button.onClick Activated ] |> Control.withKey "btn" ] ]

let private rinit (c: Control<'msg>) : RetainedRender<'msg> = (RetainedRender.init theme size c).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<'msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private order (r: RetainedRender<'msg>) : TabOrder = Focus.order r.Root.Control

// The product messages the focus tier dispatches for a delivered key (the tier `mapKey` consults before
// falling through to `host.MapKey`).
let private focusRoute (r: RetainedRender<Msg>) (focused: RetainedId option) key =
    let _, _, msgs = ControlsElmish.routeFocusedKey r focused (order r) key false
    msgs

[<Tests>]
let tests =
    testList
        "Issue 333 keymap precedence (focus tier wins over keymap-backed MapKey)"
        [ test "a focused control consumes a key the keymap also binds — focus wins, keymap never reached" {
              let r = rinit view
              let btn = idOfKey "btn" r

              // The keymap-backed MapKey DOES bind Enter (so the precedence is meaningful, not vacuous).
              Expect.equal
                  (hostMapKey ViewerKey.Enter true)
                  (Some(Command "Confirm"))
                  "the keymap-backed MapKey binds Enter"

              // ...but the focused button activates on Enter at the focus tier, which `mapKey` consults
              // FIRST; a non-empty result short-circuits, so `host.MapKey` (the keymap) is never reached.
              Expect.equal
                  (focusRoute r btn ViewerKey.Enter)
                  [ Activated ]
                  "focus traversal consumes Enter → activation, so the keymap-backed MapKey is never reached"
          }

          test "a key no focused control consumes falls through to the keymap-backed MapKey" {
              let r = rinit view
              let btn = idOfKey "btn" r

              // 'q' is neither an activation nor a navigation key, so the focus tier produces nothing...
              Expect.isEmpty (focusRoute r btn (ViewerKey.Letter 'q')) "the focus tier does not consume 'q'"

              // ...and `mapKey` then falls through to `host.MapKey` (the keymap), which resolves it.
              Expect.equal
                  (hostMapKey (ViewerKey.Letter 'q') true)
                  (Some(Command "Quit"))
                  "the keymap-backed MapKey is the fallthrough tier for a key no focused control consumes"
          } ]
