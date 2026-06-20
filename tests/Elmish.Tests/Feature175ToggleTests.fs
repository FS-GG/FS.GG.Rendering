module Feature175ToggleTests

// Feature 175 — reproduce the live "toggle turns off but not back on" report by driving the REAL
// retained pointer route (routeRetainedPointer → bindingMessagesFor) across a state flip. A toggle's
// onClick is `map (not IsOn)` baked at view time, so after the first click re-renders with the new
// IsOn, the SECOND click must dispatch the OPPOSITE value — i.e. the retained frame's event binding
// must update, not reuse the stale one.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = SetOn of bool

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 200 }

let private view (_: Size) (on: bool) : Control<Msg> =
    let toggle =
        FS.GG.UI.Controls.Typed.ToggleButton.view
            { FS.GG.UI.Controls.Typed.ToggleButton.defaults with
                Text = "T"
                IsOn = on
                OnToggle = Some(fun b -> SetOn b) }
        |> Widget.toControl
        |> Control.withKey "tog"
    Stack.create [ Stack.children [ toggle ] ]

let private host: InteractiveAppHost<bool, Msg> =
    { Init = fun () -> true, []
      Update = fun (SetOn b) _ -> b, []
      View = view
      Theme = theme
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }

// Drive a press+release (a click) at (x,y) over the retained frame; return dispatched messages.
let private click (retained: RetainedRender<Msg>) (render: ControlRenderResult<Msg>) (model: bool) x y =
    let s1, _, _, _ = ControlsElmish.routeRetainedPointer host retained render (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed x y)
    let _, msgs, _, _ = ControlsElmish.routeRetainedPointer host retained render s1 size model (pointer ViewerPointerPhaseKind.Released x y)
    msgs

[<Tests>]
let tests =
    testList "Feature175Toggle" [
        test "a toggle flips both ways — second click dispatches the OPPOSITE value" {
            // Frame 0: On = true.
            let r0 = RetainedRender.init theme size (view size true)
            let togRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "tog") |> snd
            let x, y = togRect.X + togRect.Width / 2.0, togRect.Y + togRect.Height / 2.0

            // First click (On=true) ⇒ dispatch SetOn false (turn off).
            let firstMsgs = click r0.Retained r0.Render true x y
            Expect.equal firstMsgs [ SetOn false ] "first click (on) turns it OFF"

            // Apply, re-render the new state, click again.
            let model1 = false
            let s = RetainedRender.step theme size r0.Retained (view size model1)
            let togRect1 = s.Render.Bounds |> List.find (fun (id, _) -> id = "tog") |> snd
            let x1, y1 = togRect1.X + togRect1.Width / 2.0, togRect1.Y + togRect1.Height / 2.0

            // Second click (On=false) ⇒ must dispatch SetOn TRUE (turn back on).
            let secondMsgs = click s.Retained s.Render model1 x1 y1
            Expect.equal secondMsgs [ SetOn true ] "second click (off) turns it back ON (not stuck off)"
        }

        test "a SWITCH flips both ways under click (the reported select-page bug)" {
            let switchView (_: Size) (on: bool) : Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Switch.create [ Switch.checked' on; Switch.onChanged (fun v -> SetOn v) ] |> Control.withKey "sw" ] ]
            let swHost = { host with View = switchView; Init = fun () -> true, [] }

            let r0 = RetainedRender.init theme size (switchView size true)
            let rect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "sw") |> snd
            let x, y = rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0

            let s1, _, _, _ = ControlsElmish.routeRetainedPointer swHost r0.Retained r0.Render (Pointer.init ()) size true (pointer ViewerPointerPhaseKind.Pressed x y)
            let _, firstMsgs, _, _ = ControlsElmish.routeRetainedPointer swHost r0.Retained r0.Render s1 size true (pointer ViewerPointerPhaseKind.Released x y)
            Expect.equal firstMsgs [ SetOn false ] "switch click (on) turns it OFF"

            let s = RetainedRender.step theme size r0.Retained (switchView size false)
            let rect1 = s.Render.Bounds |> List.find (fun (id, _) -> id = "sw") |> snd
            let x1, y1 = rect1.X + rect1.Width / 2.0, rect1.Y + rect1.Height / 2.0
            let s2, _, _, _ = ControlsElmish.routeRetainedPointer swHost s.Retained s.Render (Pointer.init ()) size false (pointer ViewerPointerPhaseKind.Pressed x1 y1)
            let _, secondMsgs, _, _ = ControlsElmish.routeRetainedPointer swHost s.Retained s.Render s2 size false (pointer ViewerPointerPhaseKind.Released x1 y1)
            Expect.equal secondMsgs [ SetOn true ] "switch click (off) turns it back ON"
        }
    ]
