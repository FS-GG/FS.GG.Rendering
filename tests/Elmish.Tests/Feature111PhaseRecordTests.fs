module Feature111PhaseRecordTests

// Feature 111 (US2, FR-002) — every produced frame reports which work phases ran vs were skipped: the
// four booleans `{ ViewCalled (view), DiffRan, LayoutRan, PaintRan }`. These tests drive the
// deterministic `ControlsElmish.Perf.runScript` path and assert the phase record per frame class
// (idle = all false; animation-only tick = view false / paint true; model frame = all true; model frame
// with no visual diff = layout false), SC-002 / SC-004.

open System
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }
let private noMods = ViewerKeyboard.noModifiers
let private tick (ms: float) = FrameInput.Tick(TimeSpan.FromMilliseconds ms)
let private key () = FrameInput.Key(Enter, noMods)

let private mkHost (view: int -> Control<Msg>) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = fun k _ -> (match k with | Enter -> Some Bump | _ -> None)
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

// A view whose Switch enters Hover at model >= 2 (animation clock) — used for the animation-only tick.
let private animView (model: int) : Control<Msg> =
    Stack.create [ Stack.children [ Switch.create [ Attr.visualState (if model >= 2 then Hover else Normal) ] |> Control.withKey "sw" ] ]

// A view whose root orientation toggles on model parity — a model change re-measures the whole nest.
let private geomView (model: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation (if model % 2 = 0 then "vertical" else "horizontal")
          Stack.children [ Button.create [ Button.text (string model) ] |> Control.withKey "b" ] ]

// A view independent of the model — a model change produces no visual difference (zero remeasure).
let private constView (_: int) : Control<Msg> =
    Stack.create [ Stack.children [ Button.create [ Button.text "fixed" ] |> Control.withKey "b" ] ]

[<Tests>]
let tests =
    testList "Feature 111 phase-invalidation record (US2, FR-002/SC-002/SC-004)" [

        test "an idle frame runs no phase — all four phase fields false (SC-002)" {
            let f = (ControlsElmish.Perf.runScript (mkHost constView) size [ FrameInput.Idle ]).[0]
            Expect.isFalse f.ViewCalled "view skipped"
            Expect.isFalse f.DiffRan "diff skipped"
            Expect.isFalse f.LayoutRan "layout skipped"
            Expect.isFalse f.PaintRan "paint skipped"
        }

        test "an animation-only tick skips the view phase and runs the paint phase (FR-002/FR-004)" {
            let frames = ControlsElmish.Perf.runScript (mkHost animView) size [ key (); key (); tick 16.0 ]
            let f = frames.[2]
            Expect.isFalse f.ViewCalled "view phase skipped (no host.View)"
            Expect.isFalse f.DiffRan "no new view tree reconciled"
            Expect.isFalse f.LayoutRan "no remeasure"
            Expect.isTrue f.PaintRan "paint phase ran (overlay re-sampled)"
        }

        test "a geometry-changing model frame runs view + diff + layout + paint (SC-004)" {
            // First key seeds (init, remeasure 0); the second key toggles orientation -> remeasure > 0.
            let frames = ControlsElmish.Perf.runScript (mkHost geomView) size [ key (); key () ]
            let f = frames.[1]
            Expect.isTrue f.ViewCalled "view ran"
            Expect.isTrue f.DiffRan "a new view tree was reconciled"
            Expect.isTrue f.LayoutRan "the layout phase re-measured (geometry changed)"
            Expect.isTrue f.PaintRan "paint ran"
        }

        test "a model frame with no visual difference runs view+diff+paint but NOT layout (FR-002)" {
            let frames = ControlsElmish.Perf.runScript (mkHost constView) size [ key (); key () ]
            let f = frames.[1]
            Expect.isTrue f.ViewCalled "view ran (the model changed)"
            Expect.isTrue f.DiffRan "a new view tree was reconciled"
            Expect.isFalse f.LayoutRan "no node re-measured — layout phase did no work (LayoutRan = false)"
            Expect.isTrue f.PaintRan "paint ran"
            Expect.equal f.RemeasuredNodeCount 0 "LayoutRan is consistent with RemeasuredNodeCount = 0"
        }
    ]
