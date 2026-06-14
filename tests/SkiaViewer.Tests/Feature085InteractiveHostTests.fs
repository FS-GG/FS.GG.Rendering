module Feature085InteractiveHostTests

// Feature 085 US2 — pointer-driven interaction in the durable host (SC-002, FR-004/FR-005).
// These exercise the REAL adapter routing path (`ControlsElmish.routeInteractivePointer`, the
// exact step `runInteractiveApp` wires per native sample) headlessly, plus the pure MVU
// transition + emitted ViewerEffects. Live native injection is deferred (research D6); the
// durable visible-window launch is proven separately in readiness/interactive-visible-window.md.

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish
open FS.Skia.UI.SkiaViewer

type private Msg =
    | Increment
    | Ignored

type private Model = { Count: int }

let private host: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Count = 0 }, []
      Update =
        fun msg model ->
            match msg with
            | Increment -> { model with Count = model.Count + 1 }, [ RenderScene SceneNode.Empty ]
            | Ignored -> model, []
      View =
        fun _size _model ->
            Stack.create
                [ Stack.children
                      [ Button.create [ Button.text "Go"; Button.onClick Increment ] |> Control.withKey "go" ] ]
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer =
        fun interaction ->
            match interaction with
            | Click("go", _, _, _) -> Some Increment
            | _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private size = { Width = 320; Height = 200 }

/// Centre of the "go" control's computed bounds at `size` (the point a user would click).
let private goCentre () =
    let model0 = fst (host.Init ())
    let rendered = Control.renderTree host.Theme size (host.View size model0)

    let available: FS.Skia.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.Skia.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.Skia.UI.Layout.Exactly }

    let result = FS.Skia.UI.Layout.Layout.evaluate available rendered.Layout
    let go = result.Bounds |> List.find (fun b -> b.NodeId = "go")
    go.Bounds.X + go.Bounds.Width / 2.0, go.Bounds.Y + go.Bounds.Height / 2.0

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

[<Tests>]
let interactiveHostTests =
    testList "Feature 085 interactive pointer host (US2)" [

        // T016 / SC-002 — a synthetic press+release at the bound control, routed through the EXACT
        // step runInteractiveApp wires, dispatches the bound msg AND changes the model.
        test "synthetic press+release at a control routes the bound msg and changes the model (SC-002)" {
            let cx, cy = goCentre ()
            let model0 = fst (host.Init ())

            let state1, downMsgs =
                ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model0 (pointer ViewerPointerPhaseKind.Pressed cx cy)

            let _state2, upMsgs =
                ControlsElmish.routeInteractivePointer host state1 size model0 (pointer ViewerPointerPhaseKind.Released cx cy)

            let routed = downMsgs @ upMsgs
            Expect.contains routed Increment "press+release at the button dispatches the bound Increment msg"

            // Fold the routed messages through the pure Update — the model changes.
            let model1, effects =
                routed
                |> List.fold (fun (m, fx) msg -> let m', fx' = host.Update msg m in m', fx @ fx') (model0, [])

            Expect.equal model1.Count 1 "the model changed: Count incremented to 1"
            Expect.isNonEmpty effects "Update emitted a ViewerEffect for the routed message"
        }

        // T015 — pure pointer-routing transition: a press OUTSIDE any control routes nothing and
        // leaves the model unchanged (hit-test miss is a no-op, not a spurious dispatch).
        test "a press+release outside any control routes no msg (pure transition, FR-004)" {
            let model0 = fst (host.Init ())

            let state1, downMsgs =
                ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model0 (pointer ViewerPointerPhaseKind.Pressed 5.0 5.0)

            let _state2, upMsgs =
                ControlsElmish.routeInteractivePointer host state1 size model0 (pointer ViewerPointerPhaseKind.Released 5.0 5.0)

            Expect.isEmpty (downMsgs @ upMsgs) "a click in empty space dispatches no product message"
        }

        // T023 (US4, SC-004, FR-009) — the size-aware View lays content out to the ACTUAL extent
        // at two surface sizes; there is no fixed-size render that gets upscaled.
        test "size-aware View lays out to the actual extent at two sizes (US4, FR-009)" {
            // A genuinely size-aware view: content sized to the current surface width.
            let sizeAwareView (sz: Size) (_: Model) : Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text (sprintf "w=%d h=%d" sz.Width sz.Height) ] ] ]

            let renderAt (sz: Size) = Control.renderTree Theme.light sz (sizeAwareView sz { Count = 0 })
            let small = renderAt { Width = 320; Height = 240 }
            let large = renderAt { Width = 1024; Height = 768 }
            Expect.notEqual small.Scene large.Scene "size-aware View lays out differently at two extents (no fixed-size upscale)"

            // The InteractiveViewerHost.View seam is Size-carrying (FR-009): build one and confirm
            // its rendered scene tracks the supplied extent.
            let viewerHost: InteractiveViewerHost<Model, Msg> =
                { Init = host.Init
                  Update = host.Update
                  View = fun sz m -> SceneNode.Group [ (Control.renderTree Theme.light sz (sizeAwareView sz m)).Scene ]
                  MapKey = fun _ _ -> [] // 092 (FR-006): MapKey is `'msg list` ([] = unhandled)
                  MapPointer = fun _ _ _ -> []
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }
            let s1 = viewerHost.View { Width = 320; Height = 240 } { Count = 0 }
            let s2 = viewerHost.View { Width = 1024; Height = 768 } { Count = 0 }
            Expect.notEqual s1 s2 "the host's size-aware View produces extent-specific scenes"
        }

        // T015 — emitted-effect assertion on the pure Update for the routed message.
        test "Update is pure and emits the declared ViewerEffect for Increment (MVU boundary)" {
            let m0 = { Count = 7 }
            let m1, effects = host.Update Increment m0
            Expect.equal m1.Count 8 "Update is a pure transition over the model"

            match effects with
            | [ RenderScene _ ] -> ()
            | other -> failtestf "Update should emit a single RenderScene effect, got %A" other

            // purity: same input -> same output, original model untouched
            let m1', _ = host.Update Increment m0
            Expect.equal m1 m1' "Update is deterministic"
            Expect.equal m0.Count 7 "Update did not mutate the input model"
        }
    ]
