module Feature144FsiSemanticTests

open System.IO
open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private text path =
    File.ReadAllText(Path.Combine(repositoryRoot, path))

[<Tests>]
let tests =
    testList "Feature144 public FSI semantic contracts" [
        test "Controls contract exposes transient metadata and activation request helpers" {
            let contract = text "src/Controls/Control.fsi"

            [ "type TransientWidgetMetadata"
              "type WidgetActivationRequest"
              "module TransientWidget"
              "val toSurface"
              "val activationRequest" ]
            |> List.iter (fun token -> Expect.stringContains contract token $"Control.fsi contains {token}")
        }

        test "runtime pointer and focus contracts expose overlay bridge helpers" {
            let runtimeContract = text "src/Controls/ControlRuntime.fsi"
            let pointerContract = text "src/Controls/Pointer.fsi"
            let focusContract = text "src/Controls/Focus.fsi"

            Expect.stringContains runtimeContract "OverlayRuntimeDispatchRecord" "runtime exposes dispatch records"
            Expect.stringContains pointerContract "PointerOverlayRoutingResult" "pointer exposes overlay routing result"
            Expect.stringContains focusContract "FocusRecoveryDecision" "focus exposes recovery evidence"
        }

        test "metadata validates and translates through the public coordinator surface" {
            let item = metadata TransientSurfaceKind.Menu "fsi-menu" 10 true true false
            let anchor = anchorFor (item.AnchorId)
            let surface = TransientWidget.toSurface anchor item

            Expect.isEmpty (TransientWidget.validate (Some anchor) item) "complete metadata validates"
            Expect.equal surface.Kind TransientSurfaceKind.Menu "surface kind carried"
            Expect.equal surface.Id.SurfaceId "fsi-menu" "surface id carried"
            Expect.equal surface.Anchor.AnchorBounds anchor.AnchorBounds "anchor evidence supplied at host frame"
        }
    ]
