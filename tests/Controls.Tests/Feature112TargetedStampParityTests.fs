module Feature112TargetedStampParityTests

// Feature 112 (US2, FR-002/FR-005/FR-008) — the TARGETED stamp's final rendered scene is byte-identical
// to the preserved full-tree `applyRuntimeVisualState` oracle, for hover-move / focus-move / press-toggle
// over keyed / nested / unkeyed-same-kind-sibling / consumer-set trees. Also asserts the live route
// choice (`runtimeStampFor`): a prior stamped frame takes the targeted route (scene == oracle), and a
// first/model-changing frame takes the full-tree oracle route — so FR-002's selection is covered without
// driving the live loop. Controls have no value equality; `Scene` does, so compare the rendered scenes.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }
let private emptyModel = fst (ControlRuntime.init ())
let private modelWith f = f emptyModel
let private sceneOf (c: Control<int>) = (Control.renderTree theme size c).Scene
let private stampedUnder model fresh = ControlRuntime.applyRuntimeVisualState model fresh

let private btn (k: string) : Control<int> = Button.create [ Button.text "Go" ] |> Control.withKey k

// ---- representative trees ----------------------------------------------------------------------

let private flat: Control<int> =
    Stack.create [ Stack.children [ for i in 0 .. 7 -> btn (sprintf "b%d" i) ] ]

let private nested: Control<int> =
    Stack.create
        [ Stack.children
              [ Stack.create [ Stack.children [ btn "a"; btn "b" ] ] |> Control.withKey "g1"
                Stack.create [ Stack.children [ btn "c"; btn "d" ] ] |> Control.withKey "g2" ] ]

// Unkeyed same-kind siblings (both resolve to the same `Kind` id — the full oracle treats them
// identically, so the targeted stamp must too).
let private unkeyedSiblings: Control<int> =
    Stack.create [ Stack.children [ Button.create [ Button.text "x" ]; Button.create [ Button.text "y" ] ] ]

// A tree with a consumer-set Disabled control.
let private consumerSet: Control<int> =
    Stack.create
        [ Stack.children
              [ btn "a"
                Button.create [ Button.text "off"; Attr.visualState Disabled ] |> Control.withKey "d" ] ]

// Assert the targeted stamp's scene equals the full-tree oracle's, for a prev->cur transition.
let private assertParity (name: string) (fresh: Control<int>) (prevModel: ControlRuntimeModel) (curModel: ControlRuntimeModel) =
    let prevStamped = stampedUnder prevModel fresh
    let targeted = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel prevStamped fresh
    let oracle = stampedUnder curModel fresh
    Expect.equal (sceneOf targeted.Stamped) (sceneOf oracle) (sprintf "%s: targeted scene == full-tree oracle scene (FR-005)" name)

[<Tests>]
let tests =
    testList "Feature 112 targeted stamp is scene-identical to the full-tree oracle (US2, FR-005/SC-002)" [

        test "hover move parity over flat / nested / consumer-set trees" {
            assertParity "flat hover b1->b6" flat (modelWith (fun m -> { m with HoveredControl = Some "b1" })) (modelWith (fun m -> { m with HoveredControl = Some "b6" }))
            assertParity "nested hover a->d" nested (modelWith (fun m -> { m with HoveredControl = Some "a" })) (modelWith (fun m -> { m with HoveredControl = Some "d" }))
            assertParity "consumer-set hover a->d(Disabled)" consumerSet (modelWith (fun m -> { m with HoveredControl = Some "a" })) (modelWith (fun m -> { m with HoveredControl = Some "d" }))
        }

        test "focus move parity over flat / nested trees" {
            assertParity "flat focus b0->b7" flat (modelWith (fun m -> { m with FocusedControl = Some "b0" })) (modelWith (fun m -> { m with FocusedControl = Some "b7" }))
            assertParity "nested focus b->c" nested (modelWith (fun m -> { m with FocusedControl = Some "b" })) (modelWith (fun m -> { m with FocusedControl = Some "c" }))
        }

        test "press-toggle parity (press then release)" {
            assertParity "press a" nested (modelWith id) (modelWith (fun m -> { m with PressedControls = Set.ofList [ "a" ] }))
            assertParity "release a" nested (modelWith (fun m -> { m with PressedControls = Set.ofList [ "a" ] })) (modelWith id)
        }

        test "unkeyed same-kind siblings: targeted == oracle (both collapse onto the Kind id, identically)" {
            assertParity "unkeyed hover->none" unkeyedSiblings (modelWith (fun m -> { m with HoveredControl = Some "button" })) (modelWith id)
        }

        test "at-rest parity: an emptyModel->emptyModel stamp equals the un-bridged build (FR-008)" {
            let prevStamped = stampedUnder emptyModel flat
            let targeted = ControlRuntime.applyRuntimeVisualStateTargeted emptyModel emptyModel prevStamped flat
            Expect.equal (sceneOf targeted.Stamped) (sceneOf flat) "at rest the stamp emits nothing — byte-identical to the un-stamped tree (FR-008)"
        }

        // ---- FR-002 / FR-006: the live route choice via runtimeStampFor ----

        test "runtimeStampFor picks the targeted route on a model-unchanged frame (scene == oracle)" {
            let prevModel = modelWith (fun m -> { m with HoveredControl = Some "b2" })
            let curModel = modelWith (fun m -> { m with HoveredControl = Some "b5" })
            let prevStamped = stampedUnder prevModel flat
            let routed = ControlRuntime.runtimeStampFor (Some(prevModel, prevStamped)) curModel flat
            let oracle = stampedUnder curModel flat
            Expect.equal (sceneOf routed.Stamped) (sceneOf oracle) "the targeted route's scene equals the oracle (FR-002)"
            Expect.isTrue (routed.RuntimeStateTouchedNodeCount < Control.count flat) "the targeted route touches fewer than all nodes"
        }

        test "runtimeStampFor falls back to the full-tree oracle on a first/model-changing frame (prior = None)" {
            let curModel = modelWith (fun m -> { m with HoveredControl = Some "b3" })
            let routed = ControlRuntime.runtimeStampFor None curModel flat
            let oracle = stampedUnder curModel flat
            Expect.equal (sceneOf routed.Stamped) (sceneOf oracle) "the oracle route's scene equals the full-tree stamp (FR-006)"
            Expect.equal routed.RuntimeStateTouchedNodeCount (Control.count flat) "the oracle route counts the whole tree"
        }
    ]
