module Feature112TouchedCountTests

// Feature 112 (US1/US3, FR-001/FR-004/FR-007) — the TARGETED runtime visual-state stamp re-stamps only
// the controls whose final state changed (the affected identities + ancestor paths), reusing every
// unchanged subtree. These tests assert `RuntimeStateTouchedNodeCount` is far below the node count for a
// localized hover/focus change, `0` for a no-change / at-rest frame, and that the count is the
// regression guard (the full-tree oracle route reports the whole node count).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private emptyModel = fst (ControlRuntime.init ())
let private modelWith f = f emptyModel

// A flat stack of N keyed buttons (each a leaf) — root + N nodes.
let private flat (n: int) : Control<int> =
    Stack.create [ Stack.children [ for i in 0 .. n - 1 -> Button.create [ Button.text "Go" ] |> Control.withKey (sprintf "b%d" i) ] ]

// The previous frame's stamped tree = the full stamp under the previous model (what the live host holds).
let private stampedUnder model fresh = ControlRuntime.applyRuntimeVisualState model fresh

[<Tests>]
let tests =
    testList "Feature 112 targeted-stamp touched-node count (US1/US3, FR-001/FR-004/FR-007)" [

        test "a hover move A->B touches only A, B and their shared ancestor — far below the node count (SC-001)" {
            let fresh = flat 20
            let total = Control.count fresh // root + 20 buttons = 21
            let prevModel = modelWith (fun m -> { m with HoveredControl = Some "b3" })
            let curModel = modelWith (fun m -> { m with HoveredControl = Some "b14" })
            let prevStamped = stampedUnder prevModel fresh

            let r = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel prevStamped fresh
            // b3 (Hover->Normal) + b14 (Normal->Hover) + the root (their shared ancestor) = 3 rebuilt.
            Expect.equal r.RuntimeStateTouchedNodeCount 3 "touched = the two changed leaves + their shared ancestor"
            Expect.isTrue (r.RuntimeStateTouchedNodeCount < total) "touched is far below the total node count (SC-001)"
        }

        test "a focus move A->B touches only the affected identities + ancestors (SC-001)" {
            let fresh = flat 20
            let prevModel = modelWith (fun m -> { m with FocusedControl = Some "b1" })
            let curModel = modelWith (fun m -> { m with FocusedControl = Some "b18" })
            let r = ControlRuntime.applyRuntimeVisualStateTargeted prevModel curModel (stampedUnder prevModel fresh) fresh
            Expect.equal r.RuntimeStateTouchedNodeCount 3 "focus move touches the two leaves + their shared ancestor"
        }

        test "a no-change frame (hover persists on the same control) touches 0 and reuses the tree (SC-003/FR-004)" {
            let fresh = flat 20
            let model = modelWith (fun m -> { m with HoveredControl = Some "b5" })
            let prevStamped = stampedUnder model fresh
            let r = ControlRuntime.applyRuntimeVisualStateTargeted model model prevStamped fresh
            Expect.equal r.RuntimeStateTouchedNodeCount 0 "a persistent hover changes no final state -> 0 touched"
            Expect.isTrue (obj.ReferenceEquals(r.Stamped, prevStamped)) "the whole tree is reused (the prev-stamped instance is returned)"
        }

        test "a fully at-rest frame touches 0 (SC-003)" {
            let fresh = flat 20
            let prevStamped = stampedUnder emptyModel fresh
            let r = ControlRuntime.applyRuntimeVisualStateTargeted emptyModel emptyModel prevStamped fresh
            Expect.equal r.RuntimeStateTouchedNodeCount 0 "at rest, nothing is stamped or re-stamped"
        }

        test "across a hover sweep the touched counts stay proportional to the affected controls, not N (SC-006)" {
            let fresh = flat 50
            let total = Control.count fresh
            // Sweep hover b0 -> b1 -> ... -> b9; each step re-stamps only the two leaves + the root.
            let counts =
                [ for i in 0 .. 8 ->
                      let prevM = modelWith (fun m -> { m with HoveredControl = Some(sprintf "b%d" i) })
                      let curM = modelWith (fun m -> { m with HoveredControl = Some(sprintf "b%d" (i + 1)) })
                      (ControlRuntime.applyRuntimeVisualStateTargeted prevM curM (stampedUnder prevM fresh) fresh).RuntimeStateTouchedNodeCount ]
            Expect.isTrue (counts |> List.forall (fun c -> c <= 3)) "every sweep step touches <= 3 nodes regardless of the 51-node tree (SC-006)"
            Expect.isTrue (counts |> List.forall (fun c -> c < total)) "the touched count never scales with the control count"
        }

        test "the count is the regression guard: the full-tree oracle route reports the whole node count (FR-007)" {
            let fresh = flat 30
            let curModel = modelWith (fun m -> { m with HoveredControl = Some "b5" })
            // The oracle route (prior = None) re-stamps the whole tree -> count == node count.
            let oracleRoute = ControlRuntime.runtimeStampFor None curModel fresh
            Expect.equal oracleRoute.RuntimeStateTouchedNodeCount (Control.count fresh) "a whole-tree stamp makes the count jump to the node count"
            // The targeted route (a model-unchanged repaint) stays small.
            let prevModel = modelWith (fun m -> { m with HoveredControl = Some "b4" })
            let targetedRoute = ControlRuntime.runtimeStampFor (Some(prevModel, stampedUnder prevModel fresh)) curModel fresh
            Expect.isTrue (targetedRoute.RuntimeStateTouchedNodeCount < oracleRoute.RuntimeStateTouchedNodeCount) "the targeted route touches far fewer nodes than the whole-tree oracle"
        }
    ]
