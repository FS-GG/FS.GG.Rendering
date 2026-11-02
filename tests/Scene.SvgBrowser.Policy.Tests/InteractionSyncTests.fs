module Scene.SvgBrowser.Policy.Tests.InteractionSyncTests

// FS-GG/FS.GG.Rendering#1497: retained interaction synchronization must read the scene by identity in one
// pass, not rescan the selectable objects for every retained element. The checks are operation counts
// (scene objects examined), which do not depend on time, machine or load.

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

let private layer id visible objects : RetainedSceneLayer =
    {
        Id = id
        Visible = visible
        Objects = objects
    }

let private stateOf layers =
    let scene : RetainedScene =
        {
            RootId = "root"
            Revision = 1
            Camera = camera
            Layers = layers
        }

    match SvgRetained.tryCreateInteraction scene with
    | Ok state -> state
    | Error error -> failwithf "fixture scene refused: %A" error

let private select id (state: RetainedInteractionState) =
    let result = SvgRetained.update (RetainedInteractionMessage.Select(state.Scene.Revision, id)) state
    Expect.isNone result.Error $"selecting {id}"
    result.State

/// The synchronization before #1497, kept only as the oracle: every lookup scans all objects of all layers.
/// `scanned` counts the objects its `tryFind` examines.
let private scanningOracle (state: RetainedInteractionState) (scanned: int ref) =
    let selectableObject id =
        state.Scene.Layers
        |> List.collect (fun layer -> layer.Objects)
        |> List.tryFind (fun candidate ->
            scanned.Value <- scanned.Value + 1
            candidate.Id = id && candidate.Selectable)

    let everyObjectId =
        state.Scene.Layers
        |> List.collect (fun layer -> layer.Objects |> List.map _.Id)

    let perObject = everyObjectId |> List.map (fun id -> id, selectableObject id |> Option.map _.AccessibleLabel)

    let controls =
        state.Scene.Layers
        |> List.filter (fun layer -> layer.Visible)
        |> List.collect (fun layer -> layer.Objects)
        |> List.filter (fun value -> value.Selectable)
        |> List.map _.Id

    let selectedLabel =
        state.SelectedObjectId
        |> Option.bind selectableObject
        |> Option.map _.AccessibleLabel
        |> Option.defaultValue "No object selected"

    perObject, controls, selectedLabel

let private projected (state: RetainedInteractionState) =
    let projection = InteractionSync.project state

    let everyObjectId =
        state.Scene.Layers
        |> List.collect (fun layer -> layer.Objects |> List.map _.Id)

    let perObject =
        everyObjectId
        |> List.map (fun id ->
            match projection.Selectable.TryGetValue id with
            | true, value -> id, Some value.AccessibleLabel
            | _ -> id, None)

    projection, perObject, (projection.Controls |> List.map _.Id), projection.SelectedLabel

let private mixedState () =
    stateOf
        [
            layer "base" true [ objectValue "a" true; objectValue "b" false; objectValue "c" true ]
            layer "hidden" false [ objectValue "d" true; objectValue "e" false ]
            layer "top" true [ objectValue "f" true ]
        ]

let private wide count =
    // `count` objects over eight layers; every third object is not selectable and every fifth layer-block is hidden.
    let perLayer = count / 8

    [
        for l in 0..7 ->
            layer
                $"layer{l}"
                (l % 3 <> 2)
                [
                    for i in 0 .. perLayer - 1 ->
                        let n = l * perLayer + i
                        objectValue $"o{n}" (n % 3 <> 0)
                ]
    ]
    |> stateOf

[<Tests>]
let tests =
    testList
        "SVG retained interaction synchronization"
        [
            test "the single-pass projection equals the scanning synchronization for every selection" {
                let selections = [ None; Some "a"; Some "c"; Some "d"; Some "f"; Some "e" ]

                for chosen in selections do
                    let state =
                        match chosen with
                        | Some id ->
                            // Unselectable "b" and "e" are refused by the reducer; the state then keeps no selection.
                            let result = SvgRetained.update (RetainedInteractionMessage.Select(1, id)) (mixedState ())
                            result.State
                        | None -> mixedState ()

                    let scanned = ref 0
                    let expectedObjects, expectedControls, expectedLabel = scanningOracle state scanned
                    let _, actualObjects, actualControls, actualLabel = projected state
                    let name = $"selection {chosen}"
                    Expect.equal actualObjects expectedObjects $"per-object accessible projection, {name}"
                    Expect.equal actualControls expectedControls $"HTML control order and visibility, {name}"
                    Expect.equal actualLabel expectedLabel $"status text, {name}"
            }

            test "selectable objects of hidden layers keep their object projection but get no control" {
                let state = mixedState () |> select "d"
                let projection, objects, controls, label = projected state
                Expect.equal (objects |> List.find (fst >> (=) "d") |> snd) (Some "Label d") "hidden selectable object is projected"
                Expect.equal (objects |> List.find (fst >> (=) "b") |> snd) None "unselectable object is not projected"
                Expect.equal controls [ "a"; "c"; "f" ] "controls cover visible selectable objects in order"
                Expect.equal label "Label d" "selected object in a hidden layer still names the status"
                Expect.equal projection.ObjectsVisited 6 "every scene object is examined once"
            }

            test "with a duplicate identity the first selectable object wins, as the scanning lookup did" {
                // Built directly: the reducer refuses duplicate identities, but the projection must not depend on that.
                let withLabel id selectable label = { objectValue id selectable with AccessibleLabel = label }

                let scene : RetainedScene =
                    {
                        RootId = "root"
                        Revision = 1
                        Camera = camera
                        Layers =
                            [
                                layer "a" true [ withLabel "dup" false "unselectable first"; withLabel "dup" true "selectable first" ]
                                layer "b" true [ withLabel "dup" true "selectable second" ]
                            ]
                    }

                let state : RetainedInteractionState =
                    {
                        Scene = scene
                        SelectedObjectId = Some "dup"
                        FocusedObjectId = None
                        CapturedPointerId = None
                    }

                let projection = InteractionSync.project state
                Expect.equal (projection.Selectable.["dup"]).AccessibleLabel "selectable first" "first selectable wins the identity"
                Expect.equal projection.SelectedLabel "selectable first" "status names the first selectable"
                Expect.equal (projection.Controls |> List.map _.AccessibleLabel) [ "selectable first"; "selectable second" ] "every selectable gets a control"
                let oracleObjects, _, _ = scanningOracle state (ref 0)
                Expect.equal (oracleObjects |> List.map snd |> List.distinct) [ Some "selectable first" ] "the scanning lookup agrees"
            }

            test "projection examines each scene object once: operations are linear in the object count" {
                let counts = [ 400; 800; 1600 ]

                let visited =
                    counts
                    |> List.map (fun count ->
                        let state = wide count |> select "o1"
                        let projection = InteractionSync.project state
                        Expect.equal projection.ObjectsVisited count $"{count} objects examined exactly once"
                        projection.ObjectsVisited)

                Expect.equal visited counts "objects examined equals the object count at every size"
            }

            test "the scanning synchronization it replaces is quadratic, so the count discriminates" {
                let scanned = ref 0
                let state = wide 800 |> select "o1"
                scanningOracle state scanned |> ignore
                let projection = InteractionSync.project state
                // 800 per-object scans each stop at its own position, plus two selected-label scans: about n^2 / 2.
                Expect.isGreaterThan scanned.Value (10 * projection.ObjectsVisited) $"scanning examined {scanned.Value} objects against {projection.ObjectsVisited}"
            }
        ]
