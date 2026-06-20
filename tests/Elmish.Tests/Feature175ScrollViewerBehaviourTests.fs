module Feature175ScrollViewerBehaviourTests

// Feature 175 F5 — the ScrollViewer behaviour is FIRST-CLASS and COHESIVE. A product that launches
// through the shared host driver (`runInteractiveApp`) inherits persistent scroll for free, and the
// pieces are ONE unit driven by a single `ScrollState`:
//   * offset + clamp + thumb live in `FS.GG.UI.Controls.ScrollState` (pure),
//   * the wheel/key fold + stamp live in the shared host driver (every product inherits them),
//   * the offset-aware queryable layout is `RetainedRender.hitTestLayout` (F2).
// This contract test drives them together — wheel resolution → one ScrollState → clamp, thumb, AND
// the offset-aware hit-test all agree — so the cohesion cannot silently drift apart (the F2 class).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = RowClicked of int

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }
let private viewportHeight = 140.0
let private rowHeight = 30.0
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

let private pointer phase x y deltaY : ViewerPointerInput =
    { Phase = phase; X = x; Y = y; Button = None; DeltaX = 0.0; DeltaY = deltaY }

let private clickAt (retained: RetainedRender<Msg>) (render: ControlRenderResult<Msg>) x y =
    let prim = Some ViewerPointerButtonKind.Primary
    let p1, _, _, _ =
        ControlsElmish.routeRetainedPointer host retained render (Pointer.init ()) size 0
            { Phase = ViewerPointerPhaseKind.Pressed; X = x; Y = y; Button = prim; DeltaX = 0.0; DeltaY = 0.0 }
    let _, msgs, _, _ =
        ControlsElmish.routeRetainedPointer host retained render p1 size 0
            { Phase = ViewerPointerPhaseKind.Released; X = x; Y = y; Button = prim; DeltaX = 0.0; DeltaY = 0.0 }
    msgs

let private stampedFrame (prev: RetainedRender<Msg>) (svId: ControlId) (scroll: ScrollState) =
    let runtimeModel = { fst (ControlRuntime.init ()) with ScrollOffsets = Map.ofList [ svId, scroll ] }
    RetainedRender.step theme size prev (ControlRuntime.applyScrollOffsets runtimeModel (host.View size 0))

[<Tests>]
let tests =
    testList "Feature175ScrollViewerBehaviour" [
        test "offset + clamp + thumb + offset-aware hit-test are one cohesive unit driven by one ScrollState" {
            let r0 = RetainedRender.init theme size (host.View size 0)
            let svRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd

            // The host-driver-inherited wheel routing resolves the scroll-viewer's measured extent.
            let _, _, _, deltas =
                ControlsElmish.routeRetainedPointer host r0.Retained r0.Render (Pointer.init ()) size 0
                    (pointer ViewerPointerPhaseKind.Wheel (svRect.X + 10.0) (svRect.Y + 10.0) -40.0)
            let svId, _, contentH, viewportH = List.exactlyOne deltas
            Expect.isGreaterThan contentH viewportH "the region overflows (a real scroll range exists)"

            // ONE ScrollState owns offset + clamp + thumb. Scroll by exactly three rows (mid-range).
            let scroll =
                ScrollState.empty
                |> ScrollState.withExtent contentH viewportH
                |> ScrollState.applyScrollDelta (rowHeight * 3.0)

            // Clamp: offset stays within [0, maxOffset].
            Expect.isTrue (scroll.Offset > 0.0 && scroll.Offset <= ScrollState.maxOffset scroll) "the offset is clamped within range"
            // Thumb: tracks the offset, stays inside the track.
            let track = viewportHeight
            let thumb = ScrollState.thumbPosition track scroll
            Expect.isGreaterThan thumb 0.0 "the thumb moved off the top, tracking the offset"
            Expect.isLessThanOrEqual (thumb + ScrollState.thumbHeight scroll) (track + 0.001) "the thumb stays within the track"

            // The SAME ScrollState drives the offset-aware hit-test: clicking a fixed point resolves a
            // DIFFERENT (post-scroll) row than before scrolling.
            let px, py = svRect.X + svRect.Width / 2.0, svRect.Y + rowHeight + rowHeight / 2.0
            let before = clickAt r0.Retained r0.Render px py
            let s = stampedFrame r0.Retained svId scroll
            let after = clickAt s.Retained s.Render px py
            Expect.notEqual after before "the offset-aware hit-test resolves the post-scroll control (same offset, one unit)"
        }

        test "a huge scroll clamps to the bottom and pins the thumb to the track end (no overscroll)" {
            let r0 = RetainedRender.init theme size (host.View size 0)
            let svRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd
            let _, _, _, deltas =
                ControlsElmish.routeRetainedPointer host r0.Retained r0.Render (Pointer.init ()) size 0
                    (pointer ViewerPointerPhaseKind.Wheel (svRect.X + 10.0) (svRect.Y + 10.0) -40.0)
            let _, _, contentH, viewportH = List.exactlyOne deltas

            let scroll =
                ScrollState.empty
                |> ScrollState.withExtent contentH viewportH
                |> ScrollState.applyScrollDelta 1.0e9

            Expect.equal scroll.Offset (ScrollState.maxOffset scroll) "a huge delta clamps exactly to maxOffset (no overscroll)"
            let track = viewportHeight
            Expect.floatClose Accuracy.medium (ScrollState.thumbPosition track scroll) (track - ScrollState.thumbHeight scroll) "the thumb sits flush at the track end at max offset"
        }
    ]
