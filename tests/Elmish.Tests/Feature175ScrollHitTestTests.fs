module Feature175ScrollHitTestTests

// Feature 175 F2 — the regression the report flagged as missing: drive the LIVE retained pointer route
// (`routeRetainedPointer`) over SCROLLED content and assert it resolves the control actually under the
// pointer (post-scroll), not the stale pre-scroll one. The pre-existing scroll tests used the paint-side
// `Control.hitTest` or only the wheel→delta routing; none clicked through the live route over a scrolled
// region, so a regression in the offset-aware queryable layout (`RetainedRender.hitTestLayout`) would
// have been invisible. A second test locks the two-consumer parity: the pointer route (queryable layout)
// and `resolveFocus` (retained node boxes) must resolve the SAME scrolled control.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = RowClicked of int

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }
let private rowHeight = 30.0
let private viewportHeight = 140.0
let private rowCount = 30

let private view (_: Size) (_: int) : Control<Msg> =
    let rows =
        [ for i in 1..rowCount ->
            Button.create [ Button.text (sprintf "row %d" i); Button.onClick (RowClicked i); Attr.height rowHeight ]
            |> Control.withKey (sprintf "row%d" i) ]
    let content = Stack.create [ Stack.orientation "vertical"; Stack.children rows ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ Control.create "scroll-viewer" [ Attr.children [ content ]; Attr.height viewportHeight ]
                |> Control.withKey "sv" ] ]

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

/// Step to a frame scrolled by `offset` px (stamped through the real host bridge).
let private scrolledFrame (prev: RetainedRender<Msg>) (offset: float) =
    let scroll =
        ScrollState.empty
        |> ScrollState.withExtent (float rowCount * rowHeight) viewportHeight
        |> ScrollState.applyScrollDelta offset
    let runtimeModel = { fst (ControlRuntime.init ()) with ScrollOffsets = Map.ofList [ "sv", scroll ] }
    let stamped = ControlRuntime.applyScrollOffsets runtimeModel (host.View size 0)
    RetainedRender.step theme size prev stamped

let private rowY (render: ControlRenderResult<Msg>) i =
    render.Bounds |> List.find (fun (id, _) -> id = sprintf "row%d" i) |> snd |> fun (r: Rect) -> r.Y

[<Tests>]
let tests =
    testList "Feature175ScrollHitTest" [
        test "the live pointer route resolves the POST-scroll control over scrolled content (F2)" {
            let r0 = RetainedRender.init theme size (host.View size 0)
            let svRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd
            let px = svRect.X + svRect.Width / 2.0
            // Aim at the centre of row 2 (well inside the viewport at rest), and scroll by the exact row
            // pitch measured from the rendered bounds (robust to any inter-row spacing).
            let pitch = rowY r0.Render 3 - rowY r0.Render 2
            let py = rowY r0.Render 2 + rowHeight / 2.0

            let before = clickAt r0.Retained r0.Render px py
            let s = scrolledFrame r0.Retained pitch
            let after = clickAt s.Retained s.Render px py

            match before, after with
            | [ RowClicked a ], [ RowClicked b ] ->
                Expect.notEqual b a "the same screen point resolves to a DIFFERENT row after scrolling (not the stale pre-scroll control)"
                Expect.equal b (a + 1) "scrolling by one row pitch resolves exactly the next row at the same point"
            | _ -> failtestf "expected one RowClicked from each click, got %A then %A" before after
        }

        test "pointer route and focus resolution AGREE on the scrolled control (F2 two-consumer parity)" {
            let r0 = RetainedRender.init theme size (host.View size 0)
            let svRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd
            let px = svRect.X + svRect.Width / 2.0
            let pitch = rowY r0.Render 3 - rowY r0.Render 2
            let py = rowY r0.Render 2 + rowHeight / 2.0
            let s = scrolledFrame r0.Retained pitch

            let clickRow =
                match clickAt s.Retained s.Render px py with
                | [ RowClicked b ] -> sprintf "row%d" b
                | other -> failtestf "expected one RowClicked, got %A" other

            let focusRow =
                ControlsElmish.resolveFocus s.Retained px py
                |> Option.bind (fun id -> RetainedRender.authoredControlIds s.Render.BoundIds s.Retained |> Map.tryFind id)

            Expect.equal
                focusRow
                (Some clickRow)
                "focus resolution (node boxes) and the pointer route (queryable layout) resolve the SAME scrolled control"
        }
    ]
