module Feature232FocusDispatchTests

// Feature 232 (#44) — unify control-id schemes onto `Key ?? path`, exercised through the REAL host
// focus seam `ControlsElmish.routeFocusedKey` and the retained-id resolver. The headline fix: an
// UNKEYED focused control now dispatches its activation binding on keypress (pre-232 the focus id was
// `Key ?? Kind` while the binding id was `Key ?? path`, so the filter matched nothing and the keypress
// dropped). Keyed controls are the regression guard.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Activated

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 200 }

let private rinit (c: Control<'msg>) : RetainedRender<'msg> = (RetainedRender.init theme size c).Retained
let private order (r: RetainedRender<'msg>) : TabOrder = Focus.order r.Root.Control

// A focusable, Enter/Space-activated control (no authored Key — the regressed case).
let private focusable: Attr<Msg> =
    Attr.accessibility (
        Accessibility.metadata
            AccessibilityRole.Button
            "go"
            [ "normal" ]
            None
            (Accessibility.keyboard true [ "Enter"; "Space" ] [])
            None
            None)

// The RetainedId of the first focusable node in the tree (found without relying on a Key).
let rec private firstFocusableId (n: RetainedNode<Msg>) : RetainedId option =
    let isFocusable =
        n.Control.Accessibility |> Option.map (fun m -> m.Keyboard.Focusable) |> Option.defaultValue false
    if isFocusable then Some n.Identity else n.Children |> List.tryPick firstFocusableId

let private routeEnter (r: RetainedRender<Msg>) (focused: RetainedId option) =
    let _, _, msgs = ControlsElmish.routeFocusedKey r focused (order r) ViewerKey.Enter false
    msgs

[<Tests>]
let feature232FocusDispatchTests =
    testList "Feature232 focus dispatch via routeFocusedKey" [

        // ---- US1 (T008 / SC-001) — an UNKEYED focused control dispatches on Enter ----
        test "US1 T008: an unkeyed focused control dispatches its activation binding on Enter" {
            let tree: Control<Msg> =
                Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Activated; focusable ] ] ]

            let r = rinit tree
            let focused = firstFocusableId r.Root
            Expect.isSome focused "the unkeyed button is focusable"
            let msgs = routeEnter r focused
            Expect.contains msgs Activated "Enter on the unkeyed focused control fired its activation binding (was silently dropped pre-232)"
        }

        test "US1 T008: a KEYED focused control still dispatches (regression guard)" {
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "go"; Button.onClick Activated; focusable ] |> Control.withKey "btn" ] ]

            let r = rinit tree
            let focused = firstFocusableId r.Root
            let msgs = routeEnter r focused
            Expect.contains msgs Activated "the keyed focused control dispatches unchanged"
        }

        // ---- T006 — the retained-id -> Key ?? path resolver ----
        test "T006: retainedCanonicalId resolves an unkeyed node to its path and a keyed node to its key" {
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "a" ] |> Control.withKey "keyed"
                            Button.create [ Button.text "b" ] ] ] // unkeyed -> path "0.1"

            let r = rinit tree

            let idOfKind kind =
                let rec find (path: string) (n: RetainedNode<Msg>) =
                    if n.Control.Kind = kind && n.Control.Key = None then Some(n.Identity, path)
                    else n.Children |> List.mapi (fun i c -> i, c) |> List.tryPick (fun (i, c) -> find (path + "." + string i) c)
                find "0" r.Root

            // the unkeyed button resolves to its structural path
            match idOfKind "button" with
            | Some(rid, path) -> Expect.equal (RetainedRender.retainedCanonicalId rid r) (Some path) "unkeyed node -> Key ?? path (its path)"
            | None -> failtest "expected an unkeyed button node"

            // a keyed node resolves to its Key
            let keyedId =
                let rec find (n: RetainedNode<Msg>) =
                    if n.Control.Key = Some "keyed" then Some n.Identity else n.Children |> List.tryPick find
                find r.Root
            Expect.equal (RetainedRender.retainedCanonicalId (Option.get keyedId) r) (Some "keyed") "keyed node -> its Key"
        }
    ]
