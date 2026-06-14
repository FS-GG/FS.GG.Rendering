module Feature112PrecedenceTests

// Feature 112 (US2, FR-003/SC-004) — the targeted stamp honours the existing visual-state precedence: a
// consumer-set non-Normal state (Disabled / Selected) wins over a derived hover/focus/press, so such a
// control is never re-stamped by a derived transition; a derived Normal emits NO visualState attribute.

open Expecto
open FS.Skia.UI.Controls

let private emptyModel = fst (ControlRuntime.init ())
let private modelWith f = f emptyModel
let private stampedUnder model fresh = ControlRuntime.applyRuntimeVisualState model fresh

// Find a control's resolved visual state by its `Key ?? Kind` id in a stamped tree.
let rec private findState (id: string) (c: Control<int>) : VisualState option =
    if (c.Key |> Option.defaultValue c.Kind) = id then
        Some(ControlInternals.visualStateOf c.Attributes)
    else
        c.Children |> List.tryPick (findState id)

let private tree: Control<int> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Go" ] |> Control.withKey "a"
                Button.create [ Button.text "off"; Attr.visualState Disabled ] |> Control.withKey "d" ] ]

[<Tests>]
let tests =
    testList "Feature 112 visual-state precedence preserved under targeting (US2, FR-003/SC-004)" [

        test "a consumer-set Disabled control keeps Disabled when hover moves onto it (FR-003)" {
            // prev: hover on "a"; cur: hover moves onto the Disabled "d".
            let prevModel = modelWith (fun m -> { m with HoveredControl = Some "a" })
            let curModel = modelWith (fun m -> { m with HoveredControl = Some "d" })
            let r = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel (stampedUnder prevModel tree) tree
            Expect.equal (findState "d" r.Stamped) (Some Disabled) "the consumer-set Disabled state wins over the derived Hover (SC-004)"
            // "d"'s final state is Disabled under both models -> it is never re-stamped (only "a" changes).
            Expect.equal r.RuntimeStateTouchedNodeCount 2 "only 'a' (Hover->Normal) + the root are rebuilt; the Disabled 'd' is untouched"
        }

        test "a consumer-set Selected control is not overridden by a derived focus" {
            let selTree: Control<int> =
                Stack.create [ Stack.children [ Button.create [ Button.text "s"; Attr.visualState Selected ] |> Control.withKey "s"; Button.create [ Button.text "o" ] |> Control.withKey "o" ] ]
            let prevModel = modelWith (fun m -> { m with FocusedControl = Some "o" })
            let curModel = modelWith (fun m -> { m with FocusedControl = Some "s" })
            let r = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel (stampedUnder prevModel selTree) selTree
            Expect.equal (findState "s" r.Stamped) (Some Selected) "consumer-set Selected wins over derived Focused"
        }

        test "a derived Normal emits NO visualState attribute (byte-identity at rest, FR-008)" {
            // Move hover OFF "a" -> "a" reverts to Normal with no visualState attr (matching the un-stamped node).
            let prevModel = modelWith (fun m -> { m with HoveredControl = Some "a" })
            let curModel = modelWith id
            let r = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel (stampedUnder prevModel tree) tree
            // The un-stamped "a" (in `tree`) has no visualState attr -> visualStateOf = Normal.
            Expect.equal (findState "a" r.Stamped) (Some Normal) "a control reverting to Normal carries no derived visualState (Normal)"
        }
    ]
