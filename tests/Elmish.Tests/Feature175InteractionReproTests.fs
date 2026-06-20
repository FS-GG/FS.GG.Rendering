module Feature175InteractionReproTests

// Feature 175 S2 — a reusable deterministic interaction-repro harness.
//
// The nav-focus and toggle live-bug reconstructions (`Feature175NavFocusTests`,
// `Feature175ToggleTests`) each hand-assembled the same boilerplate: build a host, `RetainedRender.init`
// the first frame, find a control's rect in `render.Bounds`, synthesise press+release pointer inputs,
// call the internal `routeRetainedPointer` twice, fold the dispatched messages into the model, then
// `RetainedRender.step` the next frame — repeated per click. `InteractionRepro` collapses that to a
// threaded `Session` so reproducing a live interaction bug (and turning it into a permanent
// regression) is a few lines: `start host |> click "id"` then assert on `.LastMsgs` / `.Focus` / `.Scene`.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

/// Reusable harness: drive clicks through the REAL retained pointer route and read back the
/// dispatched messages, resolved focus, and scene, threading retained + pointer + model state.
/// `internal` because it threads the framework's internal `RetainedRender`/`RetainedId` (the same
/// seam the Feature175 templates reach through `InternalsVisibleTo`).
module internal InteractionRepro =

    type Session<'model, 'msg> =
        { Host: InteractiveAppHost<'model, 'msg>
          Size: Size
          Model: 'model
          Retained: RetainedRender<'msg>
          Render: ControlRenderResult<'msg>
          Pointer: PointerState
          /// Messages the most recent `click` dispatched (empty if the click hit nothing bindable).
          LastMsgs: 'msg list
          /// Scroll deltas (scroll-viewer id, deltaY, contentHeight, viewportHeight) the most recent
          /// click resolved — the value the host folds into its persistent scroll offset.
          LastScrollDeltas: (ControlId * float * float * float) list }

    /// Start a session: init the host and render the first retained frame.
    let start (size: Size) (host: InteractiveAppHost<'model, 'msg>) : Session<'model, 'msg> =
        let model = fst (host.Init())
        let r0 = RetainedRender.init host.Theme size (host.View size model)
        { Host = host
          Size = size
          Model = model
          Retained = r0.Retained
          Render = r0.Render
          Pointer = Pointer.init ()
          LastMsgs = []
          LastScrollDeltas = [] }

    let private centerOf (id: ControlId) (s: Session<'model, 'msg>) =
        match s.Render.Bounds |> List.tryFind (fun (cid, _) -> cid = id) with
        | Some(_, rect) -> rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0
        | None ->
            failwithf
                "InteractionRepro: no control with id '%s' in the current frame (have: %s)"
                id
                (s.Render.Bounds |> List.map fst |> String.concat ", ")

    let private pointerAt phase x y : ViewerPointerInput =
        { Phase = phase; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }

    /// Click the control with id `id` (press + release at its center): route both phases through the
    /// retained pointer path, fold the dispatched messages into the model, re-render, and return the
    /// next session. `LastMsgs` carries what the click dispatched.
    let click (id: ControlId) (s: Session<'model, 'msg>) : Session<'model, 'msg> =
        let x, y = centerOf id s
        let p1, _, _, _ =
            ControlsElmish.routeRetainedPointer s.Host s.Retained s.Render s.Pointer s.Size s.Model (pointerAt ViewerPointerPhaseKind.Pressed x y)
        let p2, msgs, _, scrolls =
            ControlsElmish.routeRetainedPointer s.Host s.Retained s.Render p1 s.Size s.Model (pointerAt ViewerPointerPhaseKind.Released x y)
        let model' = msgs |> List.fold (fun m msg -> fst (s.Host.Update msg m)) s.Model
        let stepped = RetainedRender.step s.Host.Theme s.Size s.Retained (s.Host.View s.Size model')
        { s with
            Model = model'
            Retained = stepped.Retained
            Render = stepped.Render
            Pointer = p2
            LastMsgs = msgs
            LastScrollDeltas = scrolls }

    /// The stable focus identity resolved at the center of control `id` in the CURRENT frame —
    /// distinguishes unkeyed same-kind siblings (the Feature 175 nav-focus bleed the templates caught).
    let focusAt (id: ControlId) (s: Session<'model, 'msg>) : RetainedId option =
        let x, y = centerOf id s
        ControlsElmish.resolveFocus s.Retained x y

    /// The current frame's scene — for rendered-output assertions or offscreen screenshot capture
    /// (`Viewer.captureScreenshotEvidence`), closing the drive → capture loop (S1).
    let scene (s: Session<'model, 'msg>) : Scene = s.Render.Scene

// --- demonstration: the toggle bug, reproduced via the harness in a few lines -------------------

type private Msg = SetOn of bool

let private size: Size = { Width = 320; Height = 200 }

let private toggleHost: InteractiveAppHost<bool, Msg> =
    let view (_: Size) (on: bool) : Control<Msg> =
        FS.GG.UI.Controls.Typed.ToggleButton.view
            { FS.GG.UI.Controls.Typed.ToggleButton.defaults with
                Text = "T"
                IsOn = on
                OnToggle = Some(fun b -> SetOn b) }
        |> Widget.toControl
        |> Control.withKey "tog"
        |> fun t -> Stack.create [ Stack.children [ t ] ]
    { Init = fun () -> true, []
      Update = fun (SetOn b) _ -> b, []
      View = view
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

[<Tests>]
let tests =
    testList "Feature175InteractionRepro" [
        test "the reusable harness reproduces the toggle flip-both-ways bug in a few lines" {
            let s1 = InteractionRepro.start size toggleHost |> InteractionRepro.click "tog"
            Expect.equal s1.LastMsgs [ SetOn false ] "first click (on) turns it OFF"

            let s2 = InteractionRepro.click "tog" s1
            Expect.equal s2.LastMsgs [ SetOn true ] "second click (off) turns it back ON (not stuck off)"
        }
    ]
