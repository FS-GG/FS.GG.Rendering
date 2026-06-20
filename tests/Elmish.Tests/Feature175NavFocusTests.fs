module Feature175NavFocusTests

// Feature 175 (US2) — reproduce the LIVE "first click navigates, second click focuses" report
// headlessly, by replaying the host's exact focus-on-click → navigate → re-render flow over the
// internal seams (resolveFocus + routeRetainedPointer + assembleRuntimeModel logic +
// applyRuntimeVisualState + RetainedRender.step). Also checks the keying fix: focusing one keyed nav
// button must NOT mark its sibling.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Navigate of string

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }

// A shell that mirrors the real showcase: a keyed nav rail + a content subtree that is COMPLETELY
// DIFFERENT per page (different kinds, counts, keys) — exactly the structural swap a navigation does.
let private view (_: Size) (page: string) : Control<Msg> =
    let nav id =
        Button.create [ Button.text id; Button.onClick (Navigate id) ] |> Control.withKey ("nav-" + id)
    let content =
        match page with
        | "A" ->
            Stack.create [ Stack.children [ for i in 1..4 -> TextBlock.create [ TextBlock.text (sprintf "A-row-%d" i) ] |> Control.withKey (sprintf "a-%d" i) ] ]
        | _ ->
            Stack.create [ Stack.children [ for i in 1..9 -> Button.create [ Button.text (sprintf "B-btn-%d" i) ] |> Control.withKey (sprintf "b-%d" i) ] ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children [ nav "A"; nav "B"; content ] ]

let private host: InteractiveAppHost<string, Msg> =
    { Init = fun () -> "A", []
      Update = fun (Navigate p) _ -> p, []
      View = view
      Theme = theme
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private press x y : ViewerPointerInput = { Phase = ViewerPointerPhaseKind.Pressed; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }
let private release x y : ViewerPointerInput = { Phase = ViewerPointerPhaseKind.Released; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let rec private findByIdentity (id: RetainedId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Identity = id then Some n else n.Children |> List.tryPick (findByIdentity id)

let private stateOf (key: ControlId) (r: RetainedRender<Msg>) =
    findByKey key r.Root |> Option.map (fun n -> ControlInternals.visualStateOf n.Control.Attributes)

[<Tests>]
let tests =
    testList "Feature175NavFocus" [
        test "clicking a nav button focuses it on the SAME click as the navigation" {
            // Frame 0 on page A (no focus yet).
            let model0 = "A"
            let r0 = RetainedRender.init theme size (ControlRuntime.applyRuntimeVisualState (fst (ControlRuntime.init ())) (view size model0))

            // The centre of the "nav-B" button (the click target).
            let bRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "nav-B") |> snd
            let x, y = bRect.X + bRect.Width / 2.0, bRect.Y + bRect.Height / 2.0

            // PRESS — focus-on-click (the host's exact logic): resolve the RetainedId under the point.
            let focusedRid = ControlsElmish.resolveFocus r0.Retained x y
            Expect.isSome focusedRid "press resolves a focus target under the nav button"
            // The host sets focus only if the resolved node is FOCUSABLE.
            let focusedNode = focusedRid |> Option.bind (fun rid -> findByIdentity rid r0.Retained.Root)
            let isFocusable = focusedNode |> Option.exists (fun n -> n.Control.Accessibility |> Option.exists (fun m -> m.Keyboard.Focusable))
            Expect.isTrue isFocusable "the nav button is focusable (so focus-on-click takes)"
            // assembleRuntimeModel resolves the focused ControlId from the PREV tree (r0).
            let focusedControlId = focusedNode |> Option.map (fun n -> n.Control.Key |> Option.defaultValue n.Control.Kind)
            Expect.equal focusedControlId (Some "nav-B") "focus resolves to the nav-B control id (not the bare kind)"

            // The host stores focus as a RetainedId and resolves it against the PREVIOUS tree each
            // frame. Replay the live frame sequence faithfully: render the intermediate page-A frame
            // WITH focus (the continuous loop does this between press and release), THEN navigate.
            let renderFrame (prev: RetainedRender<Msg>) (page: string) =
                // assembleRuntimeModel's exact logic: focused ControlId = Key of the node at the stored
                // RetainedId in the PREVIOUS tree.
                let fcid = focusedRid |> Option.bind (fun rid -> findByIdentity rid prev.Root) |> Option.map (fun n -> n.Control.Key |> Option.defaultValue n.Control.Kind)
                let runtimeModel = { fst (ControlRuntime.init ()) with FocusedControl = fcid }
                let stamped = ControlRuntime.applyRuntimeVisualState runtimeModel (view size page)
                (RetainedRender.step theme size prev stamped).Retained

            // Frame after PRESS: still page A, but focus stamped (focused.Value resolved against r0).
            let rA = renderFrame r0.Retained "A"
            Expect.equal (stateOf "nav-B" rA) (Some Focused) "nav-B focuses on press (still page A)"

            // RELEASE → Navigate B; the next frame renders page B with prev = the focus-stamped page-A
            // frame (rA). focused.Value is STILL the press-time RetainedId.
            let s1, _, _, _ = ControlsElmish.routeRetainedPointer host r0.Retained r0.Render (Pointer.init ()) size model0 (press x y)
            let _, msgs, _, _ = ControlsElmish.routeRetainedPointer host r0.Retained r0.Render s1 size model0 (release x y)
            let model1 = msgs |> List.fold (fun m msg -> fst (host.Update msg m)) model0
            Expect.equal model1 "B" "the click navigated to page B"
            let rB = renderFrame rA "B"

            // THE ASSERTION: nav-B must STILL be Focused on the navigation frame (one click, not two).
            Expect.equal (stateOf "nav-B" rB) (Some Focused) "nav-B is focused on the navigation frame (one click, not two)"
            Expect.equal (stateOf "nav-A" rB) (Some Normal) "the sibling nav-A is NOT focused (keying isolates focus)"
        }
    ]
