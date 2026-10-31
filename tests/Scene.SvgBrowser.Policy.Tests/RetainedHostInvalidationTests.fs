module Scene.SvgBrowser.Policy.Tests.RetainedHostInvalidationTests

// FS-GG/FS.GG.Rendering#1498: the retained host owns DOM caches per layer, object and control. Which cached
// parts a transition invalidates must be explicit, so a camera pan or a selection change does not
// reconcile every object. The checks are decisions and counts over the real reducer's transitions, not
// timings.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Scene.SvgBrowser

let private camera = { PanX = 0.0; PanY = 0.0; Zoom = 1.0 }

let private objectValue id selectable : SemanticSceneObject =
    {
        Id = id
        Selectable = selectable
        AccessibleLabel = $"Label {id}"
        Content = { Nodes = [] }
    }

let private sceneOf revision layers : RetainedScene =
    {
        RootId = "root"
        Revision = revision
        Camera = camera
        Layers = layers
    }

let private layers count =
    [
        {
            Id = "units"
            Visible = true
            Objects = [ for i in 0 .. count - 1 -> objectValue $"o{i}" (i % 2 = 0) ]
        }
    ]

let private start count =
    match SvgRetained.tryCreateInteraction (sceneOf 1 (layers count)) with
    | Ok state -> state
    | Error error -> failwithf "fixture refused: %A" error

let private step message (state: RetainedInteractionState) =
    let result = SvgRetained.update message state
    Expect.isNone result.Error $"{message} accepted"
    result.State

/// The number of scene objects the host reconciles for a transition: all of them when the scene is
/// invalidated, none otherwise (camera and interaction updates touch one transform / the controls).
let private objectsReconciled (dependencies: Invalidation.Dependencies) (state: RetainedInteractionState) =
    if dependencies.Scene then
        state.Scene.Layers |> List.sumBy (fun layer -> layer.Objects.Length)
    else
        0

let private classify before after = Invalidation.classify before after

let private frame : PointerMapping.Frame =
    { Left = 40.0; Top = 25.0; Width = 400.0; Height = 150.0 }

let private options : SvgBrowserOptions =
    { Width = 800.0; Height = 300.0; AccessibleLabel = "Scene"; WheelZoomFactor = 1.1 }

[<Tests>]
let tests =
    testList
        "SVG retained host invalidation"
        [
            test "a camera-only transition invalidates only the camera" {
                let before = start 8
                let after = step (RetainedInteractionMessage.SetCamera(1, { camera with PanX = 5.0 })) before
                Expect.equal (classify before after) { Scene = false; Camera = true; Interaction = false } "camera only"
            }

            test "selection and focus invalidate interaction only" {
                let before = start 8
                let selected = step (RetainedInteractionMessage.Select(1, "o2")) before
                Expect.equal (classify before selected) { Scene = false; Camera = false; Interaction = true } "select"
                let focused = step (RetainedInteractionMessage.FocusNext 1) selected
                Expect.equal (classify selected focused) { Scene = false; Camera = false; Interaction = true } "focus"
                let cleared = step (RetainedInteractionMessage.ClearSelection 1) focused
                Expect.equal (classify focused cleared) { Scene = false; Camera = false; Interaction = true } "clear"
            }

            test "pointer capture and release invalidate nothing" {
                let before = start 8
                let captured = step (RetainedInteractionMessage.CapturePointer(1, 7)) before
                Expect.equal (classify before captured) { Scene = false; Camera = false; Interaction = false } "capture"
                let released = step (RetainedInteractionMessage.ReleasePointer(1, 7)) captured
                Expect.equal (classify captured released) { Scene = false; Camera = false; Interaction = false } "release"
            }

            test "replacing the scene invalidates the scene, even when the layer list is shared" {
                let before = start 8
                // Same layer list instance, new revision: nothing structural changed but the revision marker must advance.
                let sharedLayers = { before.Scene with Revision = 2 }
                Expect.isTrue (obj.ReferenceEquals(sharedLayers.Layers, before.Scene.Layers)) "layer list is shared"
                let after = step (RetainedInteractionMessage.ReplaceScene sharedLayers) before
                Expect.isTrue (classify before after).Scene "revision advance"

                let rebuilt = step (RetainedInteractionMessage.ReplaceScene(sceneOf 3 (layers 9))) after
                Expect.isTrue (classify after rebuilt).Scene "new layer list"

                let renamed = { sceneOf 4 rebuilt.Scene.Layers with RootId = "other" }
                let renamedState = step (RetainedInteractionMessage.ReplaceScene renamed) rebuilt
                Expect.isTrue (classify rebuilt renamedState).Scene "new root identity"
            }

            test "a changed layer list invalidates the scene even when root and revision are unchanged" {
                let before = start 8
                // Built directly: the reducer always advances the revision with a new layer list, so only the
                // layer-list reference comparison can detect this.
                let after = { before with Scene = { before.Scene with Layers = layers 9 } }
                Expect.equal (classify before after) { Scene = true; Camera = false; Interaction = false } "new layer list, same revision"
            }

            test "a shared layer list with a changed revision still invalidates the scene" {
                let before = start 8
                let after = { before with Scene = { before.Scene with Revision = before.Scene.Revision + 1 } }
                Expect.isTrue (obj.ReferenceEquals(before.Scene.Layers, after.Scene.Layers)) "layer list is shared"
                Expect.equal (classify before after) { Scene = true; Camera = false; Interaction = false } "revision only"
            }

            test "a camera pan reconciles no scene objects while a scene replacement reconciles all of them" {
                // The operation count that the host acts on: objects reconciled per transition.
                let counts = [ 100; 400; 1600 ]

                for count in counts do
                    let before = start count
                    let panned = step (RetainedInteractionMessage.SetCamera(1, { camera with PanX = 3.0 })) before
                    Expect.equal (objectsReconciled (classify before panned) panned) 0 $"pan at {count} objects"

                    let replaced = step (RetainedInteractionMessage.ReplaceScene(sceneOf 2 (layers count))) before
                    Expect.equal (objectsReconciled (classify before replaced) replaced) count $"replace at {count} objects"
            }

            test "a long drag costs no object reconciliation regardless of length" {
                let mutable state = start 1000
                let mutable reconciled = 0

                for i in 1..200 do
                    let next =
                        step (RetainedInteractionMessage.SetCamera(1, { camera with PanX = float i })) state

                    reconciled <- reconciled + objectsReconciled (classify state next) next
                    state <- next

                Expect.equal reconciled 0 "200 pointer-move pans over 1000 objects reconcile 0 objects"
            }
        ]

[<Tests>]
let pointerMappingTests =
    testList
        "SVG retained host pointer mapping"
        [
            test "one measured frame maps client to local exactly as the per-call formula did" {
                for clientX, clientY in [ 40.0, 25.0; 240.0, 100.0; 440.0, 175.0; 0.0, 0.0; -10.0, 400.0 ] do
                    let local = PointerMapping.toLocal frame options clientX clientY
                    Expect.equal local.X ((clientX - 40.0) * 800.0 / 400.0) $"x of {clientX}"
                    Expect.equal local.Y ((clientY - 25.0) * 300.0 / 150.0) $"y of {clientY}"
            }

            test "client and local conversions round-trip through the same frame" {
                for screen in [ 0.0, 0.0; 123.5, 77.25; 800.0, 300.0 ] do
                    let clientX = PointerMapping.toClientX frame options (fst screen)
                    let clientY = PointerMapping.toClientY frame options (snd screen)
                    let local = PointerMapping.toLocal frame options clientX clientY
                    Expect.floatClose Accuracy.high local.X (fst screen) "x round trip"
                    Expect.floatClose Accuracy.high local.Y (snd screen) "y round trip"
            }

            test "a collapsed frame does not divide by zero" {
                let collapsed = { frame with Width = 0.0; Height = 0.0 }
                let local = PointerMapping.toLocal collapsed options 50.0 60.0
                Expect.isTrue (System.Double.IsFinite local.X && System.Double.IsFinite local.Y) "finite local point"
            }
        ]
