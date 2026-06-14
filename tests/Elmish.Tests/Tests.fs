module ElmishCapabilityTests

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Elmish

[<Tests>]
let tests =
    testList "Elmish adapter contract" [
        test "init maps viewer effects" {
            let scene = Empty
            let _, effects = ElmishAdapter.init { Title = "Product"; InitialSize = { Width = 320; Height = 240 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } 0 scene

            match effects with
            | [ DispatchViewer(OpenWindow(title, size))
                DispatchViewer(ApplyWindowOptions behavior)
                DispatchViewer(EmitDiagnostic diagnostic) ] ->
                Expect.equal title "Product" "viewer title is mapped"
                Expect.equal size { Width = 320; Height = 240 } "viewer size is mapped"
                Expect.equal behavior Viewer.defaultWindowBehavior "viewer startup behavior is mapped"
                Expect.equal diagnostic.Category Startup "startup diagnostic category is mapped"
                Expect.equal diagnostic.Stage (Some Window) "startup diagnostic stage is mapped"
            | other -> failtestf "Expected DispatchViewer OpenWindow, ApplyWindowOptions, and startup diagnostic effects, got %A" other
        }
    ]
