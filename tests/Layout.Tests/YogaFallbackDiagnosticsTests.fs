module YogaFallbackDiagnosticsTests

open System
open Expecto
open FS.GG.UI.Layout

// These tests mutate a PROCESS-GLOBAL AppContext switch (`ForceYogaFailure`). Expecto runs tests in
// parallel by default, so without sequencing a concurrent test (e.g. the feature 097 incremental
// suite, which depends on the real Yoga path) would observe the forced failure and diverge. Run them
// in the sequenced (non-parallel) phase so the global mutation is isolated.
[<Tests>]
let yogaFallbackDiagnosticsTests =
    testSequenced
    <| testList "Yoga failure diagnostics" [
        test "Yoga execution failure collapses geometry and reports an error rather than substituting a second engine" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ { Defaults.layoutNode "child" with Intent = { Defaults.layoutIntent with Size = { Width = Some 96.0; Height = Some 24.0 } } } ] }

            AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", true)

            try
                let result = Layout.evaluate (Defaults.availableSpace 320.0 180.0) root

                Expect.equal result.Bounds.Length 2 "every node still has an entry, so callers keep their node/bounds correspondence"
                Expect.all
                    result.Bounds
                    (fun item -> item.Bounds.Width = 0.0 && item.Bounds.Height = 0.0 && item.Bounds.X = 0.0 && item.Bounds.Y = 0.0)
                    "geometry is empty, not invented by a second engine"
                Expect.exists
                    result.Diagnostics
                    (fun item ->
                        item.Code = FallbackBoundsApplied
                        && item.Severity = FS.GG.UI.Layout.DiagnosticSeverity.Error
                        && item.Constraint = Some "yoga"
                        && item.FallbackApplied
                        && item.NodeId = Some "root"
                        && item.Message.Contains("no geometry was computed"))
                    "Yoga failure surfaces as an Error through existing public fields"
            finally
                AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", false)
        }

        // Regression: the removed pure fallback engine ignored SpaceBetween/SpaceAround/SpaceEvenly and
        // re-derived each child's box from `Intent.Size` after positioning it at its flex-SHRUNK offset,
        // so an overflowing row came back with overlapping children spilling their container. Empty
        // bounds cannot express that; assert nothing overlaps or escapes the root on the failure path.
        test "Yoga execution failure never emits overlapping or container-overflowing geometry" {
            let child id width =
                { Defaults.layoutNode id with
                    Intent =
                        { Defaults.layoutIntent with
                            Size = { Width = Some width; Height = Some 20.0 }
                            FlexShrink = 1.0 } }

            let root =
                { Defaults.layoutNode "root" with
                    Intent =
                        { Defaults.layoutIntent with
                            Direction = Row
                            Size = { Width = Some 100.0; Height = Some 50.0 } }
                    Children = [ child "a" 80.0; child "b" 80.0 ] }

            AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", true)

            try
                let result = Layout.evaluate (Defaults.availableSpace 100.0 50.0) root
                let boundsOf id = result.Bounds |> List.find (fun item -> item.NodeId = id) |> fun item -> item.Bounds
                let rootBounds = boundsOf "root"
                let a = boundsOf "a"
                let b = boundsOf "b"

                Expect.isLessThanOrEqual (a.X + a.Width) b.X "children do not overlap"
                Expect.isLessThanOrEqual (b.X + b.Width) (rootBounds.X + rootBounds.Width) "children do not overflow their container"
            finally
                AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", false)
        }
    ]
