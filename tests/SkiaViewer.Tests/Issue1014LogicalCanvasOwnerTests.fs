module Issue1014LogicalCanvasOwnerTests

// Issue #1014 — the interactive path has one transform owner. These tests drive the same
// Viewer.pointerInProductSpace seam as the live loop, then the real retained Controls route. A
// resolution change is therefore proven by semantic activation, not by inspecting a persisted value.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = Activated

let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Activated model -> model + 1, []
      View =
        fun _ _ ->
            Stack.create
                [ Stack.children
                      [ Button.create [ Button.text "Semantic control"; Button.onClick Activated ]
                        |> Control.withKey "semantic-control" ] ]
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private size width height : Size = { Width = width; Height = height }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

let private controlCentre logical =
    let rendered = Control.renderTree host.Theme logical (host.View logical 0)
    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float logical.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float logical.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let layout = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let bounds = layout.Bounds |> List.find (fun item -> item.NodeId = "semantic-control")
    bounds.Bounds.X + bounds.Bounds.Width / 2.0, bounds.Bounds.Y + bounds.Bounds.Height / 2.0

let private activateAtCorrespondingPhysicalPoint logical surface =
    let logicalX, logicalY = controlCentre logical
    let fit = LogicalCanvas.fit logical surface
    let physicalX = logicalX * fit.Scale + fit.OffsetX
    let physicalY = logicalY * fit.Scale + fit.OffsetY

    let route phase =
        Viewer.pointerInProductSpace
            (Some logical)
            surface
            surface
            (pointer phase physicalX physicalY)

    let down = route ViewerPointerPhaseKind.Pressed
    let up = route ViewerPointerPhaseKind.Released
    Expect.floatClose Accuracy.high down.X logicalX "live inverse mapping restores the authored X"
    Expect.floatClose Accuracy.high down.Y logicalY "live inverse mapping restores the authored Y"

    let state, downMessages =
        ControlsElmish.routeInteractivePointer host (Pointer.init ()) logical 0 down

    let _, upMessages = ControlsElmish.routeInteractivePointer host state logical 0 up
    Expect.contains (downMessages @ upMessages) Activated "the same semantic control activates"

[<Tests>]
let logicalCanvasOwnerTests =
    testList "Issue 1014 interactive logical-canvas owner" [
        test "ApplyLogicalCanvas reaches the runtime owner in effect order" {
            let selected = ResizeArray<Size>()
            let effects =
                [ ApplyLogicalCanvas(size 1280 720)
                  ApplyLogicalCanvas(size 1920 1080) ]

            let closed =
                Viewer.interpretViewerEffectsWithLogicalCanvas
                    ignore ignore ignore ignore ignore ignore selected.Add effects

            Expect.isFalse closed "a logical-canvas change does not close the window"
            Expect.sequenceEqual
                selected
                [ size 1280 720; size 1920 1080 ]
                "the viewer receives every runtime selection in dispatch order"
        }

        test "1280x720 -> 1920x1080 keeps the retained control at its corresponding physical point" {
            let surface = size 1600 1000 // deliberately 16:10: both 16:9 canvases letterbox
            activateAtCorrespondingPhysicalPoint (size 1280 720) surface
            activateAtCorrespondingPhysicalPoint (size 1920 1080) surface
        }
    ]
