module SvgStudioBrowserFixture

open System
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.Scene
open FS.GG.UI.Scene.SvgBrowser

[<ImportDefault("@fontsource/noto-sans/files/noto-sans-latin-400-normal.woff2?inline")>]
let notoDataUrl: string = jsNative

[<Emit("new Worker(new URL('../SvgGeometryWorkerEntry.js', import.meta.url), { type:'module' })")>]
let workerFactory () : obj = jsNative

let emptyDocument =
    { Schema=SvgDocument.schema; Id="studio-document"; ViewBox={X=0.0;Y=0.0;Width=100.0;Height=100.0}; Definitions=[]; Children=[] }

let mutable host: SvgStudioHost option = None
let mutable fontHost: SvgFontResourceHost option = None
let container: HTMLElement = document.getElementById("studio")

let createState () =
    SvgAuthoring.tryCreate 0 emptyDocument {Schema=SvgAsset.catalogSchema;Assets=[]} []
    |> Result.defaultWith (fun error -> failwithf "%A" error)

let mount () =
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    host <-
        SvgStudio.mount container {MountNamespace="studio-fixture";AccessibleLabel="SVG art studio";WorkerFactory=Some workerFactory} (createState()) ignore
        |> Result.map Some
        |> Result.defaultWith (fun error -> failwithf "%A" error)

let dispose () =
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    host <- None

let disposeFont () =
    fontHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    fontHost <- None

let resourceRoundtrip () =
    let prefix = "data:font/woff2;base64,"
    if not (notoDataUrl.StartsWith prefix) then failwith "Vite did not inline the verified font"
    let resource = SvgResourceInterchange.notoSansLatin400 (notoDataUrl.Substring prefix.Length) |> Result.defaultWith (fun issues -> failwithf "%A" issues)
    let text =
        { Id="offline-text"; SemanticId=Some "offline-text"; Visible=true; Transform=SvgAffine.identity; ClipId=None; MaskId=None; Presentation=None
          Content=SvgElementContent.SceneLeaf {Nodes=[SceneNode.TextRun {Text="Offline Noto";Position={X=4.0;Y=20.0};Font={Family=Some "Noto Sans";Size=16.0;Weight=Some 400};Paint={Fill=Some {Red=0uy;Green=0uy;Blue=0uy;Alpha=255uy};Stroke=None;Opacity=1.0;Antialias=true;BlendMode=BlendMode.SrcOver;Shader=None;ColorFilter=ColorFilter.NoColorFilter;MaskFilter=MaskFilter.NoMaskFilter;ImageFilter=ImageFilter.NoImageFilter;PathEffect=PathEffect.NoPathEffect}}]} }
    let font = {Id=resource.DefinitionId;Content=SvgDefinitionContent.Font {Family=resource.Family;Source=resource.FileName;Sha256=resource.Sha256;License=resource.License}}
    let document = {emptyDocument with Definitions=[font];Children=[text]}
    let xml = SvgResourceInterchange.exportSvg "offline" {Document=document;Fonts=[resource]} |> Result.defaultWith (fun issues -> failwithf "%A" issues)
    let restored = SvgResourceInterchange.importXml {AssetNamespace="offline";DocumentId="offline";Limits=SvgDocument.defaultLimits} xml |> Result.defaultWith (fun issues -> failwithf "%A" issues)
    fontHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    fontHost <- SvgStudio.activateFont restored.Fonts.Head |> Result.map Some |> Result.defaultWith (fun issues -> failwithf "%A" issues)
    createObj ["fonts" ==> restored.Fonts.Length; "children" ==> restored.Document.Children.Length; "embedded" ==> xml.Contains("data:font/woff2;base64,")]

let fontStatus () = createObj ["ready" ==> fontHost.Value.Ready; "diagnostic" ==> (fontHost.Value.Diagnostic |> Option.toObj)]

let addRectangle () =
    let value = host.Value
    let candidate = SvgArt.create "rectangle" (SvgArtPrimitive.Rectangle {X=8.0;Y=8.0;Width=20.0;Height=12.0}) SvgDocument.defaultPresentation value.State.Document |> Result.defaultWith (fun error -> failwithf "%A" error)
    let tx = {Schema=SvgAuthoring.transactionSchema;Id="create-rectangle";Operations=[SvgAuthoringOperation.ReplaceDocument candidate]}
    value.Preview tx |> ignore
    value.Preview tx |> ignore
    value.CommitGesture tx.Id |> ignore
    value.SetSelection ["rectangle"] |> ignore
    value.Observe()

let cancelGesture () =
    let value = host.Value
    let candidate = SvgArt.translate ["rectangle"] 4.0 0.0 value.State.Document |> Result.defaultWith (fun error -> failwithf "%A" error)
    let tx = {Schema=SvgAuthoring.transactionSchema;Id="cancelled-drag";Operations=[SvgAuthoringOperation.ReplaceDocument candidate]}
    value.Preview tx |> ignore
    let before = value.State.Undo.Length
    value.CancelGesture tx.Id |> ignore
    before, value.State.Undo.Length

let previewGesture () =
    let value = host.Value
    let before = value.State.Document
    let candidate = SvgArt.translate ["rectangle-1"] 4.0 0.0 before |> Result.defaultWith (fun error -> failwithf "%A" error)
    let tx = {Schema=SvgAuthoring.transactionSchema;Id="visible-drag";Operations=[SvgAuthoringOperation.ReplaceDocument candidate]}
    value.Preview tx |> Result.defaultWith (fun error -> failwithf "%A" error)
    let during = container.querySelector("[data-fsgg-element-id='rectangle-1']").getAttribute("transform")
    let acceptedUnchanged = value.State.Document = before && value.State.Undo.Length = 1
    value.CancelGesture tx.Id |> Result.defaultWith (fun error -> failwithf "%A" error)
    let after = container.querySelector("[data-fsgg-element-id='rectangle-1']").getAttribute("transform")
    createObj["during" ==> during; "after" ==> after; "acceptedUnchanged" ==> acceptedUnchanged]

let sceneRoundtrip () =
    let value=host.Value
    let metadata={SceneId="browser-scene";Layers=[];Grid=Some{Origin={X=3.0;Y=5.0};Step={X=8.0;Y=8.0}};ResourceReferences=[];Entities=[{EntityId="object-1";KindId="sample.object";VisualElementId=Some "rectangle-1";PrefabInstanceId=None;Properties=[{Key="name";Value=SvgScenePropertyValue.Text "Crate"}]}]}
    let envelope={Schema=SvgScene.schema;Metadata=metadata;Document=value.State.Document;Catalog=value.State.Catalog;Instances=value.State.Instances;Fonts=[]}
    let wire=SvgScene.serialize envelope|>Result.defaultWith(fun issues->failwithf "%A" issues)
    let restored=SvgScene.deserialize wire|>Result.defaultWith(fun issues->failwithf "%A" issues)
    let snapped=SvgScenePlacement.grid metadata.Grid.Value {X=14.0;Y=14.0}|>Result.defaultWith(fun issues->failwithf "%A" issues)
    createObj["entities"==>restored.Metadata.Entities.Length;"x"==>snapped.X;"y"==>snapped.Y;"schema"==>restored.Schema]

let camera () =
    let value=host.Value
    value.SetCamera {A=2.0;B=0.0;C=0.0;D=2.0;E=5.0;F=7.0}|>Result.defaultWith(fun error->failwithf "%A" error)
    let picked=value.Pick {X=25.0;Y=25.0}|>Result.defaultWith(fun error->failwithf "%A" error)
    createObj["style"==>(container.querySelector("svg").getAttribute("style"));"picked"==>(picked|>Option.toObj)]

let placement () =
    let value=host.Value
    createObj["revision"==>value.State.Revision;"children"==>value.State.Document.Children.Length;"freeform"==>value.State.Metadata.Grid.IsNone]

let geometry operation =
    let path points =
        { Commands =
            (points
             |> List.mapi (fun index point -> if index = 0 then PathCommand.MoveTo point else PathCommand.LineTo point))
            @ [ PathCommand.Close ]
          FillType = PathFillType.Winding }
    let outer = path [ {X=0.0;Y=0.0}; {X=12.0;Y=0.0}; {X=12.0;Y=12.0}; {X=0.0;Y=12.0} ]
    let overlap = path [ {X=4.0;Y=4.0}; {X=16.0;Y=4.0}; {X=16.0;Y=16.0}; {X=4.0;Y=16.0} ]
    let hole = path [ {X=3.0;Y=3.0}; {X=9.0;Y=3.0}; {X=9.0;Y=9.0}; {X=3.0;Y=9.0} ]
    let coincident = path [ {X=12.0;Y=0.0}; {X=20.0;Y=0.0}; {X=20.0;Y=12.0}; {X=12.0;Y=12.0} ]
    let curved =
        { Commands=[PathCommand.MoveTo {X=0.0;Y=0.0};PathCommand.QuadTo({X=6.0;Y=12.0},{X=12.0;Y=0.0});PathCommand.LineTo {X=0.0;Y=0.0};PathCommand.Close]
          FillType=PathFillType.Winding }
    let kind =
        match operation with
        | "union" -> PathOperation.Union
        | "intersection" -> PathOperation.Intersect
        | "difference" -> PathOperation.Difference
        | "xor" -> PathOperation.Xor
        | _ -> PathOperation.Union
    let subjects, clips =
        match operation with
        | "difference" -> [outer], [hole]
        | "xor" -> [outer], [coincident]
        | "curve" -> [curved], []
        | _ -> [outer], [overlap]
    let value = host.Value
    let prepared =
        SvgGeometry.prepare ("browser-" + operation) value.State.Revision kind subjects clips SvgGeometry.defaultMaximumDeviation
        |> Result.defaultWith (fun error -> failwithf "%A" error)
    JS.Constructors.Promise.Create (fun resolve reject ->
        match value.GeometryWorker with
        | None -> reject (Exception "geometry worker is unavailable")
        | Some worker ->
            match worker.Start(
                prepared,
                (fun result ->
                    match value.CommitGeometry(prepared,result) with
                    | Ok () ->
                        let duplicateRefused = value.CommitGeometry(prepared,result) |> Result.isError
                        resolve (createObj [ "contours" ==> result.Contours.Length; "operations" ==> 1; "revision" ==> value.State.Revision; "duplicateRefused" ==> duplicateRefused ])
                    | Error error -> reject (Exception(sprintf "%A" error))),
                (fun error -> reject (Exception error))) with
            | Ok () -> ()
            | Error error -> reject (Exception error))

let cancelGeometry () =
    let contour =
        { Commands=[PathCommand.MoveTo {X=0.0;Y=0.0};PathCommand.LineTo {X=4.0;Y=0.0};PathCommand.LineTo {X=0.0;Y=4.0};PathCommand.Close]
          FillType=PathFillType.Winding }
    let value = host.Value
    let prepared = SvgGeometry.prepare "cancelled-geometry" value.State.Revision PathOperation.Union [contour] [] 0.25 |> Result.defaultWith (fun error -> failwithf "%A" error)
    match value.GeometryWorker with
    | None -> false
    | Some worker ->
        worker.Start(prepared, ignore, ignore) |> ignore
        worker.Cancel()
        not worker.InFlight

let api =
    createObj [
        "mount" ==> fun () -> mount(); host.Value.Observe()
        "dispose" ==> fun () -> dispose(); container.children.length
        "addRectangle" ==> fun () -> addRectangle()
        "cancelGesture" ==> fun () -> cancelGesture()
        "previewGesture" ==> fun () -> previewGesture()
        "sceneRoundtrip" ==> fun () -> sceneRoundtrip()
        "camera" ==> fun () -> camera()
        "placement" ==> fun () -> placement()
        "geometry" ==> fun operation -> geometry operation
        "cancelGeometry" ==> fun () -> cancelGeometry()
        "resourceRoundtrip" ==> fun () -> resourceRoundtrip()
        "fontStatus" ==> fun () -> fontStatus()
        "disposeFont" ==> fun () -> disposeFont()
        "observation" ==> fun () -> host.Value.Observe()
    ]

[<Emit("window.svgStudioFixture = $0")>]
let expose (_value: obj) : unit = jsNative

mount()
expose api
