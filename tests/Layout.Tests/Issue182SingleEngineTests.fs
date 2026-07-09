module Issue182SingleEngineTests

open Expecto
open FS.GG.UI.Layout

// `evaluate` used to run a hand-written flex engine over the whole tree on every call, purely to
// collect diagnostics, and throw its geometry away. These tests pin what that removal must preserve
// (the intent diagnostics) and what it must stop doing (measuring through a second engine).

let private countingMeasure (calls: int ref) width height =
    fun _ ->
        calls.Value <- calls.Value + 1
        { Width = width; Height = height; Diagnostics = [] }

[<Tests>]
let singleEngineTests =
    testList "Layout runs a single engine" [
        // Yoga only sets a measure func on a childless node, so it never measures a container. The
        // removed pure engine did — via `preferredMainSize` and again in its child fold — so a
        // container's callback ran twice per frame for a measurement no engine ever used.
        test "a container's measure callback is not invoked on the Yoga success path" {
            let calls = ref 0

            let container =
                { Defaults.layoutNode "container" with
                    Measure = Some(countingMeasure calls 50.0 50.0)
                    Children = [ { Defaults.layoutNode "leaf" with Intent = { Defaults.layoutIntent with Size = { Width = Some 10.0; Height = Some 10.0 } } } ] }

            let root = { Defaults.layoutNode "root" with Children = [ container ] }

            Layout.evaluate (Defaults.availableSpace 160.0 60.0) root |> ignore

            Expect.equal calls.Value 0 "no engine measures a container, so nothing should call its measure callback"
        }

        // The pure engine measured every leaf a second time, in addition to Yoga. Yoga may legitimately
        // probe a leaf more than once while resolving flex, so this asserts the bound that removal
        // guarantees rather than an exact count: a fixed-size leaf needs no measurement at all.
        test "a fixed-size leaf is not measured" {
            let calls = ref 0

            let leaf =
                { Defaults.layoutNode "leaf" with
                    Intent = { Defaults.layoutIntent with Size = { Width = Some 32.0; Height = Some 16.0 } }
                    Measure = Some(countingMeasure calls 32.0 16.0) }

            let root = { Defaults.layoutNode "root" with Children = [ leaf ] }

            Layout.evaluate (Defaults.availableSpace 160.0 60.0) root |> ignore

            Expect.equal calls.Value 0 "both axes are concrete, so no measurement is needed"
        }

        // The diagnostics below were the pure engine's only surviving contribution. `validateIntent`
        // must still produce them on the Yoga success path, where Yoga itself reports none of them.
        test "intent diagnostics survive on the Yoga success path" {
            let child =
                { Defaults.layoutNode "child" with
                    Intent =
                        { Defaults.layoutIntent with
                            Size = { Width = Some 40.0; Height = Some 20.0 }
                            MinSize = { Width = Some 80.0; Height = None }
                            MaxSize = { Width = Some 20.0; Height = None }
                            Padding = { Left = nan; Top = 0.0; Right = 0.0; Bottom = 0.0 } } }

            let root = { Defaults.layoutNode "root" with Children = [ child ] }
            let result = Layout.evaluate (Defaults.availableSpace 160.0 60.0) root

            Expect.isFalse
                (result.Diagnostics |> List.exists (fun item -> item.Code = FallbackBoundsApplied && item.Severity = FS.GG.UI.Layout.DiagnosticSeverity.Error))
                "Yoga succeeded"
            Expect.exists
                result.Diagnostics
                (fun item -> item.Code = UnsatisfiedConstraint && item.NodeId = Some "child" && item.Constraint = Some "width")
                "min/max width conflict is still diagnosed"
            Expect.exists
                result.Diagnostics
                (fun item -> item.Code = InvalidLayoutValue && item.NodeId = Some "child" && item.Constraint = Some "padding-left")
                "invalid padding is still diagnosed"
        }

        // A collapsed subtree is never laid out, so its intent was never validated. Removal must not
        // quietly start reporting diagnostics for geometry that does not exist.
        test "a collapsed subtree contributes its own diagnostics but not its descendants'" {
            let grandchild =
                { Defaults.layoutNode "grandchild" with
                    Intent = { Defaults.layoutIntent with Padding = { Left = nan; Top = 0.0; Right = 0.0; Bottom = 0.0 } } }

            let collapsed =
                { Defaults.layoutNode "collapsed" with
                    Visibility = Collapsed
                    Intent = { Defaults.layoutIntent with Gap = { Row = nan; Column = 0.0 } }
                    Children = [ grandchild ] }

            let root = { Defaults.layoutNode "root" with Children = [ collapsed ] }
            let result = Layout.evaluate (Defaults.availableSpace 160.0 60.0) root

            Expect.exists
                result.Diagnostics
                (fun item -> item.NodeId = Some "collapsed" && item.Constraint = Some "row-gap")
                "the collapsed node's own intent is validated"
            Expect.isFalse
                (result.Diagnostics |> List.exists (fun item -> item.NodeId = Some "grandchild"))
                "a collapsed node's descendants are not validated"
        }
    ]
