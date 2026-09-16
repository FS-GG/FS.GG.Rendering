module SvgStudioBrowserFixture

open System
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.Scene.SvgBrowser

[<ImportDefault("@fontsource/noto-sans/files/noto-sans-latin-400-normal.woff2?inline")>]
let notoDataUrl: string = jsNative

[<Emit("new Worker(new URL('../SvgGeometryWorkerEntry.js', import.meta.url), { type:'module' })")>]
let workerFactory () : obj = jsNative

let emptyDocument =
    {
        Schema = SvgDocument.schema
        Id = "studio-document"
        ViewBox =
            {
                X = 0.0
                Y = 0.0
                Width = 100.0
                Height = 100.0
            }
        Definitions = []
        Children = []
    }

let mutable host: SvgStudioHost option = None
let mutable fontHost: SvgFontResourceHost option = None
let mutable inputHost: SvgInputHost option = None
let mutable inputEffects: string list = []
let mutable pads: SvgGamepadSnapshot list = []
let container: HTMLElement = document.getElementById ("studio")
do container.setAttribute ("tabindex", "-1")

let createState () =
    SvgAuthoring.tryCreate
        0
        emptyDocument
        {
            Schema = SvgAsset.catalogSchema
            Assets = []
        }
        []
    |> Result.defaultWith (fun error -> failwithf "%A" error)

let mount () =
    inputHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    inputHost <- None
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())

    host <-
        SvgStudio.mount
            container
            {
                MountNamespace = "studio-fixture"
                AccessibleLabel = "SVG art studio"
                WorkerFactory = Some workerFactory
            }
            (createState ())
            ignore
        |> Result.map Some
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    let noMods = CommandInput.noModifiers
    let ctrl = { noMods with Ctrl = true }
    let meta = { noMods with Meta = true }
    let ctrlAlt = { noMods with Ctrl = true; Alt = true }

    let command id label trigger =
        {
            Id = id
            Label = label
            Contexts = [ "workspace" ]
            AvailabilityKey = None
            Trigger = trigger
            Argument = CommandArgumentPolicy.NoArgument
            Alternatives = [ CommandAlternative.Palette; CommandAlternative.Pointer label ]
        }

    let catalog =
        {
            Contexts =
                [
                    {
                        Id = "workspace"
                        Priority = 1
                        Exclusive = false
                        Overlaps = []
                    }
                ]
            Commands =
                [
                    command "workspace.palette" "Palette" CommandTriggerPolicy.OncePerPress
                    command "workspace.physical" "Physical" CommandTriggerPolicy.OncePerPress
                    command "workspace.ctrl-alt" "Ctrl Alt" CommandTriggerPolicy.OncePerPress
                    command "workspace.sequence" "Save sequence" CommandTriggerPolicy.OncePerPress
                    command "workspace.pointer" "Pointer" CommandTriggerPolicy.OncePerPress
                    command "workspace.touch" "Touch" CommandTriggerPolicy.OncePerPress
                    command "workspace.gamepad" "Gamepad" CommandTriggerPolicy.Continuous
                ]
            ReservedGestures = []
            AllowTerminalPrefixes = false
        }

    let profile =
        {
            Schema = CommandInput.profileSchema
            Id = "browser"
            Overrides = []
            Defaults =
                [
                    {
                        Gesture = InputGesture.KeyChord(InputKeyIdentity.LogicalKey "k", ctrl)
                        Command = "workspace.palette"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.KeyChord(InputKeyIdentity.LogicalKey "k", meta)
                        Command = "workspace.palette"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.KeyChord(InputKeyIdentity.PhysicalCode "KeyQ", noMods)
                        Command = "workspace.physical"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.KeyChord(InputKeyIdentity.LogicalKey "z", ctrlAlt)
                        Command = "workspace.ctrl-alt"
                        Context = "workspace"
                    }
                    {
                        Gesture =
                            InputGesture.KeySequence
                                [ InputKeyIdentity.LogicalKey "x", ctrl; InputKeyIdentity.LogicalKey "s", ctrl ]
                        Command = "workspace.sequence"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.Pointer "primary"
                        Command = "workspace.pointer"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.Touch "primary"
                        Command = "workspace.touch"
                        Context = "workspace"
                    }
                    {
                        Gesture = InputGesture.Gamepad "button-0"
                        Command = "workspace.gamepad"
                        Context = "workspace"
                    }
                ]
        }

    let effective =
        CommandInput.compile catalog profile
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    let effectText =
        function
        | CommandResolverEffect.InvokeCommand value -> "invoke:" + value.Command
        | CommandResolverEffect.HeldActionChanged(command, held) -> $"held:{command}:{held}"
        | CommandResolverEffect.PreventDefault _ -> "prevent"
        | CommandResolverEffect.CapturedGesture gesture -> "captured:" + CommandInput.gestureId gesture
        | CommandResolverEffect.ResolverDiagnostic code -> "diagnostic:" + code
        | _ -> "host"

    inputEffects <- []

    inputHost <-
        Some(
            new SvgInputHost(
                container,
                catalog,
                CommandResolver.init [ "workspace" ] effective,
                (fun () -> catalog.Commands |> List.map _.Id),
                (effectText >> fun value -> inputEffects <- inputEffects @ [ value ]),
                { SvgInputHost.defaultOptions with
                    PollGamepads = false
                    Gamepads = (fun () -> pads)
                }
            )
        )

let dispose () =
    inputHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    inputHost <- None
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    host <- None

let disposeFont () =
    fontHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    fontHost <- None

let resourceRoundtrip () =
    let prefix = "data:font/woff2;base64,"

    if not (notoDataUrl.StartsWith prefix) then
        failwith "Vite did not inline the verified font"

    let resource =
        SvgResourceInterchange.notoSansLatin400 (notoDataUrl.Substring prefix.Length)
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    let text =
        {
            Id = "offline-text"
            SemanticId = Some "offline-text"
            Visible = true
            Transform = SvgAffine.identity
            ClipId = None
            MaskId = None
            Presentation = None
            Content =
                SvgElementContent.SceneLeaf
                    {
                        Nodes =
                            [
                                SceneNode.TextRun
                                    {
                                        Text = "Offline Noto"
                                        Position = { X = 4.0; Y = 20.0 }
                                        Font =
                                            {
                                                Family = Some "Noto Sans"
                                                Size = 16.0
                                                Weight = Some 400
                                            }
                                        Paint =
                                            {
                                                Fill =
                                                    Some
                                                        {
                                                            Red = 0uy
                                                            Green = 0uy
                                                            Blue = 0uy
                                                            Alpha = 255uy
                                                        }
                                                Stroke = None
                                                Opacity = 1.0
                                                Antialias = true
                                                BlendMode = BlendMode.SrcOver
                                                Shader = None
                                                ColorFilter = ColorFilter.NoColorFilter
                                                MaskFilter = MaskFilter.NoMaskFilter
                                                ImageFilter = ImageFilter.NoImageFilter
                                                PathEffect = PathEffect.NoPathEffect
                                            }
                                    }
                            ]
                    }
        }

    let font =
        {
            Id = resource.DefinitionId
            Content =
                SvgDefinitionContent.Font
                    {
                        Family = resource.Family
                        Source = resource.FileName
                        Sha256 = resource.Sha256
                        License = resource.License
                    }
        }

    let document =
        { emptyDocument with
            Definitions = [ font ]
            Children = [ text ]
        }

    let xml =
        SvgResourceInterchange.exportSvg
            "offline"
            {
                Document = document
                Fonts = [ resource ]
            }
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    let restored =
        SvgResourceInterchange.importXml
            {
                AssetNamespace = "offline"
                DocumentId = "offline"
                Limits = SvgDocument.defaultLimits
            }
            xml
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    fontHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())

    fontHost <-
        SvgStudio.activateFont restored.Fonts.Head
        |> Result.map Some
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    createObj
        [
            "fonts" ==> restored.Fonts.Length
            "children" ==> restored.Document.Children.Length
            "embedded" ==> xml.Contains("data:font/woff2;base64,")
        ]

let fontStatus () =
    createObj
        [
            "ready" ==> fontHost.Value.Ready
            "diagnostic" ==> (fontHost.Value.Diagnostic |> Option.toObj)
        ]

let addRectangle () =
    let value = host.Value

    let candidate =
        SvgArt.create
            "rectangle"
            (SvgArtPrimitive.Rectangle
                {
                    X = 8.0
                    Y = 8.0
                    Width = 20.0
                    Height = 12.0
                })
            SvgDocument.defaultPresentation
            value.State.Document
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    let tx =
        {
            Schema = SvgAuthoring.transactionSchema
            Id = "create-rectangle"
            Operations = [ SvgAuthoringOperation.ReplaceDocument candidate ]
        }

    value.Preview tx |> ignore
    value.Preview tx |> ignore
    value.CommitGesture tx.Id |> ignore
    value.SetSelection [ "rectangle" ] |> ignore
    value.Observe()

let cancelGesture () =
    let value = host.Value

    let candidate =
        SvgArt.translate value.ToolState.Selection 4.0 0.0 value.State.Document
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    let tx =
        {
            Schema = SvgAuthoring.transactionSchema
            Id = "cancelled-drag"
            Operations = [ SvgAuthoringOperation.ReplaceDocument candidate ]
        }

    value.Preview tx |> ignore
    let before = value.State.Undo.Length
    value.CancelGesture tx.Id |> ignore
    before, value.State.Undo.Length

let previewGesture () =
    let value = host.Value
    let before = value.State.Document

    let candidate =
        SvgArt.translate [ "rectangle-1" ] 4.0 0.0 before
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    let tx =
        {
            Schema = SvgAuthoring.transactionSchema
            Id = "visible-drag"
            Operations = [ SvgAuthoringOperation.ReplaceDocument candidate ]
        }

    value.Preview tx |> Result.defaultWith (fun error -> failwithf "%A" error)

    let during =
        container.querySelector("[data-fsgg-element-id='rectangle-1']").getAttribute("transform")

    let acceptedUnchanged = value.State.Document = before && value.State.Undo.Length = 1

    value.CancelGesture tx.Id
    |> Result.defaultWith (fun error -> failwithf "%A" error)

    let after =
        container.querySelector("[data-fsgg-element-id='rectangle-1']").getAttribute("transform")

    createObj["during" ==> during
              "after" ==> after
              "acceptedUnchanged" ==> acceptedUnchanged]

let sceneRoundtrip () =
    let value = host.Value

    let metadata =
        {
            SceneId = "browser-scene"
            Layers = []
            Grid =
                Some
                    {
                        Origin = { X = 3.0; Y = 5.0 }
                        Step = { X = 8.0; Y = 8.0 }
                    }
            ResourceReferences = []
            Entities =
                [
                    {
                        EntityId = "object-1"
                        KindId = "sample.object"
                        VisualElementId = Some "rectangle-1"
                        PrefabInstanceId = None
                        Properties =
                            [
                                {
                                    Key = "name"
                                    Value = SvgScenePropertyValue.Text "Crate"
                                }
                            ]
                    }
                ]
        }

    let envelope =
        {
            Schema = SvgScene.schema
            Metadata = metadata
            Document = value.State.Document
            Catalog = value.State.Catalog
            Instances = value.State.Instances
            Fonts = []
        }

    let wire =
        SvgScene.serialize envelope
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    let restored =
        SvgScene.deserialize wire
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    let snapped =
        SvgScenePlacement.grid metadata.Grid.Value { X = 14.0; Y = 14.0 }
        |> Result.defaultWith (fun issues -> failwithf "%A" issues)

    createObj["entities" ==> restored.Metadata.Entities.Length
              "x" ==> snapped.X
              "y" ==> snapped.Y
              "schema" ==> restored.Schema]

let camera () =
    let value = host.Value

    value.SetCamera
        {
            A = 2.0
            B = 0.0
            C = 0.0
            D = 2.0
            E = 5.0
            F = 7.0
        }
    |> Result.defaultWith (fun error -> failwithf "%A" error)

    let picked =
        value.Pick { X = 25.0; Y = 25.0 }
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    createObj["style" ==> (container.querySelector("svg").getAttribute("style"))
              "picked" ==> (picked |> Option.toObj)]

let placement () =
    let value = host.Value

    createObj["revision" ==> value.State.Revision
              "children" ==> value.State.Document.Children.Length
              "freeform" ==> value.State.Metadata.Grid.IsNone]

let workspaceJourney () =
    let value = host.Value
    let authoringBefore = value.State
    let cameraBefore = value.ToolState.Camera

    value.UpdateWorkspace(SvgWorkspaceMessage.SetMode SvgWorkspaceMode.Review)
    |> ignore

    value.UpdateWorkspace(SvgWorkspaceMessage.SetViewportWidth 400.0) |> ignore

    value.UpdateWorkspace(SvgWorkspaceMessage.OpenHelp "studio-fixture--scene")
    |> ignore

    let modalFocus = value.WorkspaceState.FocusTarget
    value.UpdateWorkspace SvgWorkspaceMessage.CloseOverlay |> ignore
    let closed = value.WorkspaceState

    let restored, _ =
        SvgWorkspace.update (SvgWorkspaceMessage.ImportLayout(SvgWorkspace.encodeLayout closed.Layout)) closed

    createObj
        [
            "mode"
            ==> (match closed.Mode with
                 | SvgWorkspaceMode.Review -> "review"
                 | _ -> "other")
            "sideCollapsed"
            ==> (closed.Layout.Panels
                 |> List.filter (fun panel -> panel.Id <> "timeline")
                 |> List.forall (fun panel -> panel.Effective = SvgPanelPlacement.Collapsed))
            "modalFocus" ==> modalFocus
            "restoredFocus" ==> closed.FocusTarget
            "layoutRoundtrip" ==> (restored.Layout = closed.Layout)
            "authoringPreserved" ==> (value.State = authoringBefore)
            "cameraPreserved" ==> (value.ToolState.Camera = cameraBefore)
        ]

let inputReset () = inputEffects <- []
let inputObserved () = inputEffects |> List.toArray
let inputLifecycle () = inputHost.Value.Observe()

let inputBeginCapture () =
    inputHost.Value.Update(CommandResolverObservation.BeginCapture "studio")
    |> ignore

let disposeInput () =
    let value = inputHost.Value
    (value :> IDisposable).Dispose()
    inputHost <- None
    value.Observe()

let inputPads values =
    pads <-
        values
        |> Array.toList
        |> List.mapi (fun index pressed ->
            {
                Source = $"gamepad:{index}"
                Buttons = [ pressed ]
            })

    inputHost.Value.PollGamepadsOnce()

let nativeGamepads () =
    SvgInputHost.browserGamepads ()
    |> List.map (fun value -> createObj [ "source" ==> value.Source; "buttons" ==> List.toArray value.Buttons ])
    |> List.toArray

let geometry operation =
    let path points =
        {
            Commands =
                (points
                 |> List.mapi (fun index point ->
                     if index = 0 then
                         PathCommand.MoveTo point
                     else
                         PathCommand.LineTo point))
                @ [ PathCommand.Close ]
            FillType = PathFillType.Winding
        }

    let outer =
        path
            [
                { X = 0.0; Y = 0.0 }
                { X = 12.0; Y = 0.0 }
                { X = 12.0; Y = 12.0 }
                { X = 0.0; Y = 12.0 }
            ]

    let overlap =
        path
            [
                { X = 4.0; Y = 4.0 }
                { X = 16.0; Y = 4.0 }
                { X = 16.0; Y = 16.0 }
                { X = 4.0; Y = 16.0 }
            ]

    let hole =
        path
            [
                { X = 3.0; Y = 3.0 }
                { X = 9.0; Y = 3.0 }
                { X = 9.0; Y = 9.0 }
                { X = 3.0; Y = 9.0 }
            ]

    let coincident =
        path
            [
                { X = 12.0; Y = 0.0 }
                { X = 20.0; Y = 0.0 }
                { X = 20.0; Y = 12.0 }
                { X = 12.0; Y = 12.0 }
            ]

    let curved =
        {
            Commands =
                [
                    PathCommand.MoveTo { X = 0.0; Y = 0.0 }
                    PathCommand.QuadTo({ X = 6.0; Y = 12.0 }, { X = 12.0; Y = 0.0 })
                    PathCommand.LineTo { X = 0.0; Y = 0.0 }
                    PathCommand.Close
                ]
            FillType = PathFillType.Winding
        }

    let kind =
        match operation with
        | "union" -> PathOperation.Union
        | "intersection" -> PathOperation.Intersect
        | "difference" -> PathOperation.Difference
        | "xor" -> PathOperation.Xor
        | _ -> PathOperation.Union

    let subjects, clips =
        match operation with
        | "difference" -> [ outer ], [ hole ]
        | "xor" -> [ outer ], [ coincident ]
        | "curve" -> [ curved ], []
        | _ -> [ outer ], [ overlap ]

    let value = host.Value

    let prepared =
        SvgGeometry.prepare
            ("browser-" + operation)
            value.State.Revision
            kind
            subjects
            clips
            SvgGeometry.defaultMaximumDeviation
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    JS.Constructors.Promise.Create(fun resolve reject ->
        match value.GeometryWorker with
        | None -> reject (Exception "geometry worker is unavailable")
        | Some worker ->
            match
                worker.Start(
                    prepared,
                    (fun result ->
                        match value.CommitGeometry(prepared, result) with
                        | Ok() ->
                            let duplicateRefused = value.CommitGeometry(prepared, result) |> Result.isError

                            resolve (
                                createObj
                                    [
                                        "contours" ==> result.Contours.Length
                                        "operations" ==> 1
                                        "revision" ==> value.State.Revision
                                        "duplicateRefused" ==> duplicateRefused
                                    ]
                            )
                        | Error error -> reject (Exception(sprintf "%A" error))),
                    (fun error -> reject (Exception error))
                )
            with
            | Ok() -> ()
            | Error error -> reject (Exception error))

let cancelGeometry () =
    let contour =
        {
            Commands =
                [
                    PathCommand.MoveTo { X = 0.0; Y = 0.0 }
                    PathCommand.LineTo { X = 4.0; Y = 0.0 }
                    PathCommand.LineTo { X = 0.0; Y = 4.0 }
                    PathCommand.Close
                ]
            FillType = PathFillType.Winding
        }

    let value = host.Value

    let prepared =
        SvgGeometry.prepare "cancelled-geometry" value.State.Revision PathOperation.Union [ contour ] [] 0.25
        |> Result.defaultWith (fun error -> failwithf "%A" error)

    match value.GeometryWorker with
    | None -> false
    | Some worker ->
        worker.Start(prepared, ignore, ignore) |> ignore
        worker.Cancel()
        not worker.InFlight

let api =
    createObj
        [
            "mount"
            ==> fun () ->
                mount ()
                host.Value.Observe()
            "dispose"
            ==> fun () ->
                dispose ()
                container.children.length
            "addRectangle" ==> fun () -> addRectangle ()
            "cancelGesture" ==> fun () -> cancelGesture ()
            "previewGesture" ==> fun () -> previewGesture ()
            "sceneRoundtrip" ==> fun () -> sceneRoundtrip ()
            "camera" ==> fun () -> camera ()
            "placement" ==> fun () -> placement ()
            "workspaceJourney" ==> fun () -> workspaceJourney ()
            "inputReset" ==> fun () -> inputReset ()
            "inputObserved" ==> fun () -> inputObserved ()
            "inputLifecycle" ==> fun () -> inputLifecycle ()
            "inputBeginCapture" ==> fun () -> inputBeginCapture ()
            "disposeInput" ==> fun () -> disposeInput ()
            "inputPads" ==> fun values -> inputPads values
            "nativeGamepads" ==> fun () -> nativeGamepads ()
            "geometry" ==> fun operation -> geometry operation
            "cancelGeometry" ==> fun () -> cancelGeometry ()
            "resourceRoundtrip" ==> fun () -> resourceRoundtrip ()
            "fontStatus" ==> fun () -> fontStatus ()
            "disposeFont" ==> fun () -> disposeFont ()
            "observation" ==> fun () -> host.Value.Observe()
        ]

[<Emit("window.svgStudioFixture = $0")>]
let expose (_value: obj) : unit = jsNative

mount ()
expose api
