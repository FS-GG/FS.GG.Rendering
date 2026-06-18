module Feature150LayoutDiagnosticsTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature150LayoutDiagnostics" [
        test "scroll viewport exposes an empty-content extent without diagnostics" {
            let empty = Control.create "scroll-viewer" [] |> Control.withKey "sv"
            let rendered = Feature150ScrollFixtures.render empty

            match Control.scrollViewport rendered "sv" with
            | Some viewport ->
                Expect.equal viewport.ExtentSource EmptyContentExtent "empty content source"
                Expect.isEmpty viewport.Diagnostics "empty viewport has no intrinsic failure diagnostics"
            | None -> failtest "scroll viewport not found"
        }
    ]

