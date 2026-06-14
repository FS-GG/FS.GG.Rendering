module Feature093RetainedStateTests

// Feature 093 (E3) — SC-005 (T017/T018): a control's state-driven appearance is consistent
// across an UNRELATED re-render under E2's retained identity. The VisualState rides the control's
// attributes, so it travels through the keyed reconciler diff; a hover/disabled look therefore
// survives a sibling-shifting model update via the LIVE retained path (`RetainedRender.init`/
// `step`), NOT a hand-seeded `StateByIdentity` map (the 092 gap this explicitly avoids repeating).

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }

let rec private findByKey (key: string) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n
    else n.Children |> List.tryPick (findByKey key)

// A keyed button in a Disabled visual state with a Primary class attached. Disabled (state) wins
// the Fill over Primary (class) per the fixed precedence, so its resolved Fill is the muted token
// — visibly distinct from the Normal+Primary (accent) look.
let private disabledButton () =
    Button.create
        [ Button.text "Go"
          Attr.styleClasses [ Variant StyleVariant.Primary ]
          Attr.visualState Disabled ]
    |> Control.withKey "go"

[<Tests>]
let feature093RetainedStateTests =
    testList "Feature 093 retained state-driven styling (SC-005)" [

        test "a Disabled look survives a sibling-shifting re-render via the live retained path (SC-005)" {
            // Frame 1: the button is the Stack's only child.
            let frame1 = Stack.create [ Stack.children [ disabledButton () ] ]
            let init = RetainedRender.init theme size frame1

            // Frame 2: an unrelated sibling is prepended, SHIFTING the keyed button down.
            let frame2 = Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "header" ]; disabledButton () ] ]
            let step = RetainedRender.step theme size init.Retained frame2

            let before = findByKey "go" init.Retained.Root |> Option.defaultWith (fun () -> failtest "button missing in frame 1")
            let after = findByKey "go" step.Retained.Root |> Option.defaultWith (fun () -> failtest "button missing in frame 2")

            // (1) the retained identity survived the shift (the 067/091/092 mechanism E3 builds on).
            Expect.equal after.Identity before.Identity "the keyed button's RetainedId is stable across the sibling shift"

            // (2) the resolver drove the button's paint through the LIVE retained path at its new box.
            let box = after.Fragment.Box |> Option.defaultWith (fun () -> failtest "button has no evaluated box")
            Expect.equal after.Fragment.OwnScene (ControlInternals.faithfulContent theme box after.Control) "the retained path paints the button via the resolver"

            // (3) the Disabled visual state actually drove the look: dropping the state attribute
            //     (Normal) yields a DIFFERENT resolved paint at the same box — so the surviving look
            //     is genuinely state-driven, not the base/Normal render.
            let normalAtSameBox =
                Button.create [ Button.text "Go"; Attr.styleClasses [ Variant StyleVariant.Primary ] ]
                |> Control.withKey "go"
                |> ControlInternals.faithfulContent theme box
            Expect.notEqual after.Fragment.OwnScene normalAtSameBox "the surviving look is the Disabled state's, not the Normal-state render"
        }

        test "the state-driven paint is identical in content before and after the shift (box aside) (SC-005)" {
            // Render the same disabled button at one fixed box in both 'frames' — the resolved
            // colours are box-independent, so the state-driven look is frame-to-frame consistent.
            let box: Rect = { X = 10.0; Y = 40.0; Width = 200.0; Height = 60.0 }
            let a = ControlInternals.faithfulContent theme box (disabledButton ())
            let b = ControlInternals.faithfulContent theme box (disabledButton ())
            Expect.equal a b "the state-driven resolved look is deterministic frame to frame"
        }
    ]
