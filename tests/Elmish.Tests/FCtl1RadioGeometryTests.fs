module FCtl1RadioGeometryTests

// Review finding F-CTL-1 — the `radio-group` activation-value computer must cap its row height on
// the SAME `theme.ControlHeight` the painter (`WidgetGeometry.radioGeom`) uses, NOT a hardcoded
// literal. The shipped bug capped the click at `min 28.0 (height/n)` while the painter caps at
// `min theme.ControlHeight (height/n)` (32.0 in every shipped theme), so for any radio group laid
// out taller than `theme.ControlHeight` per row — the normal legible case — the painted bands and
// the click bands diverged and a click landed in the wrong option's band.
//
// The Feature 241 test could not catch this: its group is height 84 with 3 items, so `height/n = 28`
// and `min 28` == `min 32` == 28 — the cap never binds and the two formulas coincide. This test uses
// a genuinely TALL group (rows > `theme.ControlHeight`) so the cap binds, derives every expectation
// from `theme.ControlHeight` (not a magic number), and probes the exact band where the old `28.0`
// and the correct `32.0` disagree.
//
// Drives the real retained pointer route; reaches the internal seams via InternalsVisibleTo.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = RadioChanged of string

let private theme = Theme.light
let private size: Size = { Width = 480; Height = 320 }

let private radioItems = [ "red"; "green"; "blue" ]

// Height chosen so each row is taller than `theme.ControlHeight`: 120 / 3 = 40 > 32, so the painter's
// `min theme.ControlHeight (height/n)` cap binds at 32 and the (removed) `min 28.0` cap would bind at
// 28 — the divergence this test exists to lock.
let private groupHeight = 120.0

let private view (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ RadioGroup.create
                    [ RadioGroup.items radioItems
                      RadioGroup.selected "red"
                      RadioGroup.onChanged RadioChanged
                      Attr.width 200.0
                      Attr.height groupHeight ]
                |> Control.withKey "rad" ] ]

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
    testList "FCtl1RadioGeometry" [
        test "tall radio-group click reports the option in the PAINTED band, capped on theme.ControlHeight" {
            let r = RetainedRender.init theme size (host.View size 0)
            let rad = rectOf r.Render "rad"
            let n = List.length radioItems

            // Guard against a vacuous test: the cap must actually bind, i.e. rows are taller than
            // `theme.ControlHeight`. If a future layout makes rows short, `min` picks height/n and the
            // 28-vs-32 divergence disappears — this assertion reds so the test can't silently go vacuous.
            Expect.isGreaterThan (rad.Height / float n) theme.ControlHeight "rows must exceed theme.ControlHeight for the cap to bind"

            // The painter's row height (WidgetGeometry.radioGeom). Derived from the token, not a literal.
            let paintRowH = min theme.ControlHeight (rad.Height / float n)
            let cx = rad.X + rad.Width / 2.0

            // (1) Each painted band's centre dispatches its own option — general correctness lock.
            radioItems
            |> List.iteri (fun i item ->
                let y = rad.Y + paintRowH * (float i + 0.5)
                match clickAt r.Retained r.Render cx y with
                | [ RadioChanged v ] -> Expect.equal v item (sprintf "band %d centre must select %s" i item)
                | other -> failtestf "band %d centre: expected [RadioChanged \"%s\"], got %A" i item other)

            // (2) The divergence probe: a click in the LOWER part of the "green" (index 1) painted band,
            // still inside [paintRowH, 2*paintRowH). With the correct 32.0 cap this is green; with the
            // removed 28.0 cap `floor(yProbe / 28)` overshoots into index 2 ("blue"). This is the exact
            // wrong-option dispatch F-CTL-1 describes — the assertion the pre-fix code fails.
            let yProbe = rad.Y + paintRowH * 1.9   // 60.8 for 32px rows: green under 32, blue under 28
            Expect.isLessThan yProbe (rad.Y + paintRowH * 2.0) "probe must stay inside the green band under the correct cap"
            match clickAt r.Retained r.Render cx yProbe with
            | [ RadioChanged "green" ] -> ()
            | [ RadioChanged "blue" ] -> failtest "F-CTL-1 regression: lower green band dispatched 'blue' — click cap is not theme.ControlHeight"
            | other -> failtestf "expected [RadioChanged \"green\"], got %A" other
        }
    ]
