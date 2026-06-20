module Feature175ActivationValueTests

// Feature 175 F3 — the activation-value contract. `bindingMessagesFor` consults a per-kind registry
// (`activationValueComputers`) to turn a click into the control's `changed` payload, instead of an
// `if kind = …` cascade. This locks the three behaviours the registry must keep:
//   * a REGISTERED value-from-position kind (slider) reports its value computed from the click x,
//   * a REGISTERED toggle kind (check-box) reports the FLIPPED boolean (Feature 175), and
//   * an UNREGISTERED command (a plain button's onClick) falls through to `Payload = None`.
// Drives the real retained route; reaches the internal seams via InternalsVisibleTo.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | SliderChanged of float
    | BoolChanged of bool
    | Clicked

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 220 }

let private view (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ Slider.create [ Slider.value 0.0; Slider.onChanged SliderChanged; Attr.width 200.0; Attr.height 24.0 ]
                |> Control.withKey "sld"
                CheckBox.create [ CheckBox.text "c"; CheckBox.checked' false; CheckBox.onChanged BoolChanged ]
                |> Control.withKey "chk"
                Button.create [ Button.text "b"; Button.onClick Clicked ] |> Control.withKey "btn" ] ]

let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun _ m -> m, []
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

let private clickAt (retained: RetainedRender<Msg>) (render: ControlRenderResult<Msg>) x y =
    let p1, _, _, _ =
        ControlsElmish.routeRetainedPointer host retained render (Pointer.init ()) size 0 (pointer ViewerPointerPhaseKind.Pressed x y)
    let _, msgs, _, _ =
        ControlsElmish.routeRetainedPointer host retained render p1 size 0 (pointer ViewerPointerPhaseKind.Released x y)
    msgs

let private rectOf (render: ControlRenderResult<Msg>) id =
    render.Bounds |> List.find (fun (cid, _) -> cid = id) |> snd

[<Tests>]
let tests =
    testList "Feature175ActivationValue" [
        test "the registry computes each value-bearing kind's payload; commands fall through" {
            let r = RetainedRender.init theme size (host.View size 0)

            // Slider (registered, value-from-x): click at the horizontal CENTRE → value ≈ 0.5.
            let sld = rectOf r.Render "sld"
            match clickAt r.Retained r.Render (sld.X + sld.Width / 2.0) (sld.Y + sld.Height / 2.0) with
            | [ SliderChanged v ] -> Expect.floatClose Accuracy.medium v 0.5 "a slider click reports the value computed from x"
            | other -> failtestf "expected one SliderChanged, got %A" other

            // Check-box (registered toggle, currently false): click → FLIPPED to true.
            let chk = rectOf r.Render "chk"
            match clickAt r.Retained r.Render (chk.X + chk.Width / 2.0) (chk.Y + chk.Height / 2.0) with
            | [ BoolChanged true ] -> ()
            | other -> failtestf "expected [BoolChanged true] (flip), got %A" other

            // Button (unregistered command): the registry returns None → generic Payload=None onClick.
            let btn = rectOf r.Render "btn"
            match clickAt r.Retained r.Render (btn.X + btn.Width / 2.0) (btn.Y + btn.Height / 2.0) with
            | [ Clicked ] -> ()
            | other -> failtestf "expected [Clicked] (command falls through), got %A" other
        }
    ]
