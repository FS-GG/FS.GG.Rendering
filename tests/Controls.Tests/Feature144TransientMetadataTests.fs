module Feature144TransientMetadataTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 transient metadata coverage" [
        test "all eight supported categories are represented by shared metadata fixtures" {
            let kinds = allMetadata |> List.map _.SurfaceKind |> Set.ofList

            Expect.equal
                kinds
                (OverlayState.supportedSurfaceKinds () |> Set.ofList)
                "fixtures cover every supported transient surface category"
        }

        test "typed widget lowering carries transient metadata for all supported categories" {
            let found =
                widgetControls ()
                |> List.collect TransientWidget.collect
                |> List.map _.SurfaceKind
                |> Set.ofList

            Expect.equal found (OverlayState.supportedSurfaceKinds () |> Set.ofList) "typed authoring paths carry all categories"
        }

        test "metadata exposes product-owned visibility without runtime mutation" {
            let split =
                widgetControls ()
                |> List.collect TransientWidget.collect
                |> List.find (fun metadata -> metadata.SurfaceKind = TransientSurfaceKind.SplitButtonMenu)

            Expect.isTrue split.VisibilityState "split-button open state is observed from props"
            Expect.equal split.TriggerEnabled true "trigger enabled state is explicit"
            Expect.equal split.SelectionDispatchKey (Some "onSelected") "selection dispatch mapping is explicit"
        }
    ]
