module Feature111FrameCauseTests

// Feature 111 (US1, FR-001) — every produced frame carries a closed `FrameCause` naming the trigger
// that caused it. These tests drive the deterministic `ControlsElmish.Perf.runScript` path and assert
// the per-frame cause for each input class (idle / coalesced-move / discrete-pointer / key /
// animation-only tick), byte-stable across repeated runs (SC-001, SC-005).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }
let private noMods = ViewerKeyboard.noModifiers
let private hover id x y = FrameInput.Pointer(HoverEnter(id, x, y))
let private tick (ms: float) = FrameInput.Tick(TimeSpan.FromMilliseconds ms)

// A Bump-counter host whose Switch enters Hover once the model reaches 2 — crossing Normal->Hover starts
// a per-identity animation clock, so a following Tick is an animation-only (paint-only) frame.
let private view (model: int) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text (string model); Button.onClick Bump ] |> Control.withKey "btn"
                Switch.create [ Attr.visualState (if model >= 2 then Hover else Normal) ] |> Control.withKey "sw" ] ]

let private host: InteractiveAppHost<int, Msg> =
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

let private causesOf (script: FrameInput<Msg> list) =
    ControlsElmish.Perf.runScript host size script |> List.map (fun f -> f.FrameCause)

[<Tests>]
let tests =
    testList "Feature 111 FrameCause classification (US1, FR-001/SC-001/SC-005)" [

        test "an idle frame's cause is Idle" {
            Expect.equal (causesOf [ FrameInput.Idle ]) [ FrameCause.Idle ] "idle -> FrameCause.Idle"
        }

        test "a coalesced pointer-move burst's cause is PointerMove" {
            let burst = [ for i in 0 .. 9 -> hover "btn" (float i) (float i) ]
            Expect.equal (causesOf burst) [ FrameCause.PointerMove ] "a move burst is one frame caused by PointerMove"
        }

        test "a discrete pointer interaction's cause is PointerDiscrete" {
            let causes = causesOf [ FrameInput.Pointer(PressedDown("btn", PointerButton.Primary, 5.0, 5.0)) ]
            Expect.equal causes [ FrameCause.PointerDiscrete ] "a discrete press -> FrameCause.PointerDiscrete"
        }

        test "a key frame's cause is Key" {
            Expect.equal (causesOf [ FrameInput.Key(Enter, noMods) ]) [ FrameCause.Key ] "a key -> FrameCause.Key"
        }

        test "an animation-only tick's cause is Tick" {
            // Two Enters take the model to 2 (Switch -> Hover, clock starts); the Tick advances that clock.
            let causes = causesOf [ FrameInput.Key(Enter, noMods); FrameInput.Key(Enter, noMods); tick 16.0 ]
            Expect.equal causes [ FrameCause.Key; FrameCause.Key; FrameCause.Tick ] "the tick frame is caused by Tick"
        }

        test "the per-frame cause sequence is byte-stable across repeated runs (SC-005)" {
            let script =
                [ FrameInput.Idle
                  hover "btn" 3.0 3.0
                  FrameInput.Pointer(PressedDown("btn", PointerButton.Primary, 5.0, 5.0))
                  FrameInput.Key(Enter, noMods)
                  FrameInput.Key(Enter, noMods)
                  tick 16.0
                  FrameInput.Idle ]

            let r1 = causesOf script
            let r2 = causesOf script
            Expect.equal r1 r2 "identical script -> identical cause sequence (deterministic)"
            Expect.equal
                r1
                [ FrameCause.Idle; FrameCause.PointerMove; FrameCause.PointerDiscrete; FrameCause.Key; FrameCause.Key; FrameCause.Tick; FrameCause.Idle ]
                "each frame is classified by its trigger"
        }
    ]
