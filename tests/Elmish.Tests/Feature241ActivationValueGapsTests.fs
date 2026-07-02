module Feature241ActivationValueGapsTests

// Feature 241 (Review P10 / C3) — close the activation-value registry's KNOWN GAPS. Before this,
// clicks on the value-bearing kinds `radio-group`/`tabs`/`numeric-input` fell through to the generic
// `Payload = None` path, so `onChangedString` saw `""` and `onChangedFloat` saw `0.0` — a live
// wrong-value dispatch shipped as an audit comment. This locks the corrected behaviour:
//   * a `radio-group` click reports the option UNDER the cursor (index from y),
//   * a `tabs` click reports the tab UNDER the cursor (index from x),
//   * a `numeric-input` click echoes the control's CURRENT value (never resets it to 0.0).
// Drives the real retained pointer route; reaches the internal seams via InternalsVisibleTo.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | RadioChanged of string
    | TabChanged of string
    | NumChanged of float

let private theme = Theme.light
let private size: Size = { Width = 480; Height = 320 }

let private radioItems = [ "red"; "green"; "blue" ]
let private tabItems = [ "one"; "two"; "three" ]

let private view (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ RadioGroup.create
                    [ RadioGroup.items radioItems
                      RadioGroup.selected "red"
                      RadioGroup.onChanged RadioChanged
                      Attr.width 200.0
                      Attr.height 84.0 ]
                |> Control.withKey "rad"
                Tabs.create
                    [ Tabs.items tabItems
                      Tabs.selected "one"
                      Tabs.onChanged TabChanged
                      Attr.width 300.0
                      Attr.height 30.0 ]
                |> Control.withKey "tab"
                NumericInput.create [ NumericInput.value 5.0; NumericInput.onChanged NumChanged; Attr.width 120.0; Attr.height 32.0 ]
                |> Control.withKey "num" ] ]

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
    testList "Feature241ActivationValueGaps" [
        test "radio-group / tabs / numeric-input clicks report the right value, not the default" {
            let r = RetainedRender.init theme size (host.View size 0)

            // radio-group (vertical, index from y): click the SECOND option's band → "green".
            // rowH mirrors `radioGeom`: `min 28 (height / n)`; band i centre is `Y + rowH*(i + 0.5)`.
            let rad = rectOf r.Render "rad"
            let rowH = min 28.0 (rad.Height / float (List.length radioItems))
            match clickAt r.Retained r.Render (rad.X + rad.Width / 2.0) (rad.Y + rowH * 1.5) with
            | [ RadioChanged "green" ] -> ()
            | other -> failtestf "expected [RadioChanged \"green\"], got %A" other

            // tabs (horizontal, index from x): click the THIRD tab's column → "three".
            // tabWidth mirrors `tabsGeom`: `width / n`; column i centre is `X + tw*(i + 0.5)`.
            let tab = rectOf r.Render "tab"
            let tw = tab.Width / float (List.length tabItems)
            match clickAt r.Retained r.Render (tab.X + tw * 2.5) (tab.Y + tab.Height / 2.0) with
            | [ TabChanged "three" ] -> ()
            | other -> failtestf "expected [TabChanged \"three\"], got %A" other

            // numeric-input: a click echoes the CURRENT value (5.0), it never resets to 0.0.
            let num = rectOf r.Render "num"
            match clickAt r.Retained r.Render (num.X + num.Width / 2.0) (num.Y + num.Height / 2.0) with
            | [ NumChanged v ] -> Expect.floatClose Accuracy.medium v 5.0 "a numeric-input click reports its current value"
            | other -> failtestf "expected one NumChanged 5.0, got %A" other
        }
    ]
