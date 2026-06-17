module Feature144OverlayPointerRoutingTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 overlay pointer routing" [
        test "topmost surface receives inside pointer before lower content" {
            let opened, _ = openOne (metadata TransientSurfaceKind.Menu "pointer-menu" 10 true true false)
            let result = Pointer.routeOverlay opened "down" [ "lower"; "pointer-menu-item-1" ] (Some "pointer-menu-item-1")

            Expect.equal result.Decision.ChosenTarget (Some "pointer-menu-item-1") "inside target kept"
            Expect.isNone result.Decision.OutsideOfSurface "inside target is not outside"
            Expect.exists result.Effects (function RecordTopmostHit _ -> true | _ -> false) "hit evidence recorded"
        }

        test "outside pointer dismisses non-modal surface before lower content" {
            let opened, _ = openOne (metadata TransientSurfaceKind.ContextMenu "pointer-context" 20 true true false)
            let result = Pointer.routeOverlay opened "outside" [ "lower" ] (Some "lower")

            Expect.equal result.Decision.OutsideOfSurface (Some "pointer-context") "outside targets active surface"
            Expect.isEmpty result.State.OpenSurfaces "surface dismissed"
            Expect.exists result.Effects (function RequestOpenStateChange("pointer-context", false) -> true | _ -> false) "close request emitted"
        }

        test "modal pointer blocks covered content" {
            let opened, _ = openOne (metadata TransientSurfaceKind.DialogModal "pointer-dialog" 100 true true true)
            let result = Pointer.routeOverlay opened "covered-click" [ "covered" ] (Some "covered")

            Expect.equal result.Decision.BlockedByModal (Some "pointer-dialog") "modal blocker recorded"
            Expect.isFalse result.PassThrough "covered content does not receive input"
            Expect.exists result.Diagnostics (fun diagnostic -> diagnostic.Code = LowerLayerBlocked) "blocking diagnostic emitted"
        }

        test "ignore-dismiss policy allows pass-through" {
            let item =
                { metadata TransientSurfaceKind.Menu "pass-through-menu" 10 true true false with
                    DismissalPolicy = { (OverlayState.defaultDismissalPolicy ()) with OutsidePointer = IgnoreDismiss } }

            let opened, _ = openOne item
            let result = Pointer.routeOverlay opened "outside" [ "lower" ] (Some "lower")

            Expect.isTrue result.PassThrough "ignore dismissal allows lower content"
            Expect.exists result.Effects (function AllowPassThrough -> true | _ -> false) "pass-through effect emitted"
        }
    ]
