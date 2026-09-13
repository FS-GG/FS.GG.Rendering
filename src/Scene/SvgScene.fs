namespace FS.GG.UI.Scene

open System
open System.Globalization
open System.Text

[<RequireQualifiedAccess>]
type SvgScenePropertyKind = Text | Number | Flag | Coordinate
type SvgScenePropertyDescriptor = { Key:string; Kind:SvgScenePropertyKind; Required:bool }
type SvgSceneKindDescriptor = { KindId:string; DisplayName:string; Properties:SvgScenePropertyDescriptor list }
type SvgSceneEnvelope = { Schema:string; Metadata:SvgSceneMetadata; Document:SvgDocument; Catalog:SvgAssetCatalog; Instances:SvgPrefabInstance list; Fonts:SvgFontResource list }

module private SvgSceneWire =
    let issue code location message = {Code=code;Location=location;Message=message}
    let invariant (value:float) = string value
    let finite value = not(Double.IsNaN value || Double.IsInfinity value)
    let utf8ByteCount (value:string) =
        let mutable count=0
        let mutable index=0
        while index<value.Length do
            let code=int value[index]
            if code<=0x7f then count<-count+1
            elif code<=0x7ff then count<-count+2
            elif code>=0xd800&&code<=0xdbff&&index+1<value.Length then count<-count+4;index<-index+1
            else count<-count+3
            index<-index+1
        count
    type Writer() =
        let value=StringBuilder()
        member _.Token(token:string)=value.Append(token.Length).Append(':').Append(token)|>ignore
        member this.Int(number:int)=this.Token(string number)
        member this.List(write,items:'a list)=this.Int items.Length;items|>List.iter write
        override _.ToString()=value.ToString()
    type Reader(value:string)=
        let mutable at=0
        member _.AtEnd=at=value.Length
        member _.Token() =
            let colon=value.IndexOf(':',at)
            if colon<at then failwith "missing token"
            let count=Int32.Parse(value.Substring(at,colon-at),CultureInfo.InvariantCulture)
            if count<0||colon+1+count>value.Length then failwith "token limit"
            let token=value.Substring(colon+1,count)
            at<-colon+1+count
            token
        member this.Int()=Int32.Parse(this.Token(),CultureInfo.InvariantCulture)
        member this.List(limit,read) =
            let count=this.Int()
            if count<0||count>limit then failwith "list limit"
            [for _ in 1..count->read()]
    let point (w:Writer) (p:Point)=w.Token(invariant p.X);w.Token(invariant p.Y)
    let readPoint (r:Reader)={X=Double.Parse(r.Token(),CultureInfo.InvariantCulture);Y=Double.Parse(r.Token(),CultureInfo.InvariantCulture)}
    let synthetic presentation =
        let element={Id="value";SemanticId=None;Visible=true;Transform=SvgAffine.identity;ClipId=None;MaskId=None;Presentation=presentation;Content=SvgElementContent.Group []}
        {Schema=SvgDocument.schema;Id="presentation";ViewBox={X=0.;Y=0.;Width=1.;Height=1.};Definitions=[];Children=[element]}
    let presentation value=SvgDocument.serialize(synthetic value)|>Result.defaultWith(fun _->failwith "presentation")
    let readPresentation value=SvgDocument.deserialize value|>Result.defaultWith(fun _->failwith "presentation")|>fun d->d.Children.Head.Presentation

module SvgScene =
    open SvgSceneWire
    let schema="fsgg.svg-scene/1"
    let validateDescriptors descriptors metadata =
        let issues=ResizeArray<SvgDocumentIssue>()
        descriptors|>List.countBy _.KindId|>List.iter(fun (id,count)->if String.IsNullOrWhiteSpace id||count>1 then issues.Add(issue "invalid-scene-kind-descriptor" "/descriptors" "kind ids must be nonblank and unique"))
        descriptors|>List.iteri(fun index descriptor->
            if String.IsNullOrWhiteSpace descriptor.DisplayName then issues.Add(issue "invalid-scene-kind-descriptor" $"/descriptors/{index}/displayName" "display name must not be blank")
            descriptor.Properties|>List.countBy _.Key|>List.iter(fun (key,count)->if String.IsNullOrWhiteSpace key||count>1 then issues.Add(issue "invalid-scene-property-descriptor" $"/descriptors/{index}/properties" "property keys must be nonblank and unique")))
        let kinds=descriptors|>List.map(fun d->d.KindId,d)|>Map.ofList
        metadata.Entities|>List.iteri(fun i entity->
            match kinds|>Map.tryFind entity.KindId with
            | None->issues.Add(issue "unknown-scene-kind" $"/metadata/entities/{i}/kindId" entity.KindId)
            | Some descriptor->
                let properties=entity.Properties|>List.map(fun p->p.Key,p)|>Map.ofList
                descriptor.Properties|>List.iter(fun expected->
                    match properties|>Map.tryFind expected.Key with
                    | None when expected.Required->issues.Add(issue "missing-scene-property" $"/metadata/entities/{i}/properties/{expected.Key}" expected.Key)
                    | Some property->
                        let actual=match property.Value with SvgScenePropertyValue.Text _->SvgScenePropertyKind.Text|SvgScenePropertyValue.Number _->SvgScenePropertyKind.Number|SvgScenePropertyValue.Flag _->SvgScenePropertyKind.Flag|SvgScenePropertyValue.Coordinate _->SvgScenePropertyKind.Coordinate
                        if actual<>expected.Kind then issues.Add(issue "scene-property-kind" $"/metadata/entities/{i}/properties/{expected.Key}" "property value has the wrong descriptor kind")
                    | _->())
                entity.Properties|>List.iter(fun p->if descriptor.Properties|>List.exists(fun d->d.Key=p.Key)|>not then issues.Add(issue "unknown-scene-property" $"/metadata/entities/{i}/properties/{p.Key}" p.Key)))
        issues|>Seq.toList
    let private validate value =
        match SvgAuthoring.tryCreateScene 0 value.Metadata value.Document value.Catalog value.Instances with
        | Error(SvgAuthoringError.InvalidTransaction issues)->issues
        | Error _->[issue "invalid-scene" "/" "scene could not be accepted"]
        | Ok _->
            let fontIssues=value.Fonts|>List.mapi(fun i font->match SvgResourceInterchange.notoSansLatin400 font.Base64 with Ok exact when exact=font->None|_->Some(issue "invalid-scene-resource" $"/fonts/{i}" "resource does not match its verified manifest"))|>List.choose id
            let known=value.Fonts|>List.map _.DefinitionId|>Set.ofList
            fontIssues @ (value.Metadata.ResourceReferences|>List.choose(fun id->if known.Contains id then None else Some(issue "unresolved-resource-reference" "/metadata/resourceReferences" id)))
    let serialize (value:SvgSceneEnvelope) =
        let issues=if value.Schema<>schema then [issue "unknown-scene-schema" "/schema" $"expected {schema}"] else validate value
        if not issues.IsEmpty then Error issues else
        try
            let w=Writer()
            w.Token "FSGGSVGSCENE1"
            w.Token value.Schema
            w.Token value.Metadata.SceneId
            w.List(w.Token,value.Metadata.Layers);w.List(w.Token,value.Metadata.ResourceReferences)
            match value.Metadata.Grid with None->w.Token "0"|Some grid->w.Token "1";point w grid.Origin;point w grid.Step
            w.List((fun (entity:SvgSceneEntity)->w.Token entity.EntityId;w.Token entity.KindId;w.Token(defaultArg entity.VisualElementId "");w.Token(defaultArg entity.PrefabInstanceId "");w.List((fun (p:SvgSceneProperty)->w.Token p.Key;match p.Value with SvgScenePropertyValue.Text x->w.Token "t";w.Token x|SvgScenePropertyValue.Number x->w.Token "n";w.Token(invariant x)|SvgScenePropertyValue.Flag x->w.Token "b";w.Token(if x then "1" else "0")|SvgScenePropertyValue.Coordinate x->w.Token "p";point w x),entity.Properties)),value.Metadata.Entities)
            w.Token(SvgDocument.serialize value.Document|>Result.defaultWith(fun _->failwith "document"));w.Token(SvgAsset.serializeCatalog value.Catalog|>Result.defaultWith(fun _->failwith "catalog"))
            w.List((fun (instance:SvgPrefabInstance)->w.Token instance.InstanceId;w.Token instance.AssetId;w.Int instance.AcceptedRevision;w.List((fun (o:SvgPrefabOverride)->w.Token o.ElementId;match o.Property,o.Value with SvgPrefabProperty.Transform,SvgPrefabOverrideValue.Transform x->w.Token "t";[x.A;x.B;x.C;x.D;x.E;x.F]|>List.iter(invariant>>w.Token)|SvgPrefabProperty.Visibility,SvgPrefabOverrideValue.Visibility x->w.Token "v";w.Token(if x then "1" else "0")|SvgPrefabProperty.Presentation,SvgPrefabOverrideValue.Presentation x->w.Token "p";w.Token(presentation x)|_->failwith "override kind"),instance.Overrides)),value.Instances)
            w.List((fun font->w.Token font.DefinitionId;w.Token font.Family;w.Token font.FileName;w.Token font.Sha256;w.Token font.License;w.Token font.Base64),value.Fonts)
            let encoded=w.ToString() in if utf8ByteCount encoded>4194304 then Error[issue "scene-byte-limit" "/" "serialized scene exceeds 4 MiB"] else Ok encoded
        with e->Error[issue "scene-serialization" "/" e.Message]
    let deserialize (serialized:string) =
        if utf8ByteCount serialized>4194304 then Error[issue "scene-byte-limit" "/" "serialized scene exceeds 4 MiB"] else
        try
            let r=Reader serialized
            if r.Token()<>"FSGGSVGSCENE1" then failwith "unknown scene wire header"
            let sceneSchema=r.Token()
            let sceneId=r.Token()
            let layers=r.List(512,r.Token)
            let resources=r.List(512,r.Token)
            let grid=
                match r.Token() with
                | "0"->None
                | "1"->Some{Origin=readPoint r;Step=readPoint r}
                | _->failwith "grid option"
            let readProperty() =
                let key=r.Token()
                let value=
                    match r.Token() with
                    | "t"->SvgScenePropertyValue.Text(r.Token())
                    | "n"->SvgScenePropertyValue.Number(Double.Parse(r.Token(),CultureInfo.InvariantCulture))
                    | "b"->
                        match r.Token() with
                        | "0"->SvgScenePropertyValue.Flag false
                        | "1"->SvgScenePropertyValue.Flag true
                        | _->failwith "boolean property"
                    | "p"->SvgScenePropertyValue.Coordinate(readPoint r)
                    | _->failwith "property kind"
                {Key=key;Value=value}
            let entities=r.List(10000,fun()->
                let id=r.Token()
                let kind=r.Token()
                let visual=match r.Token() with ""->None|x->Some x
                let prefab=match r.Token() with ""->None|x->Some x
                {EntityId=id;KindId=kind;VisualElementId=visual;PrefabInstanceId=prefab;Properties=r.List(100000,readProperty)})
            let document=SvgDocument.deserialize(r.Token())|>Result.defaultWith(fun _->failwith "document")
            let catalog=SvgAsset.deserializeCatalog(r.Token())|>Result.defaultWith(fun _->failwith "catalog")
            let readOverride() =
                let eid=r.Token()
                match r.Token() with
                | "t"->
                    let n()=Double.Parse(r.Token(),CultureInfo.InvariantCulture)
                    {ElementId=eid;Property=SvgPrefabProperty.Transform;Value=SvgPrefabOverrideValue.Transform{A=n();B=n();C=n();D=n();E=n();F=n()}}
                | "v"->{ElementId=eid;Property=SvgPrefabProperty.Visibility;Value=SvgPrefabOverrideValue.Visibility(r.Token()="1")}
                | "p"->{ElementId=eid;Property=SvgPrefabProperty.Presentation;Value=SvgPrefabOverrideValue.Presentation(readPresentation(r.Token()))}
                | _->failwith "override"
            let instances=r.List(10000,fun()->
                {InstanceId=r.Token();AssetId=r.Token();AcceptedRevision=r.Int();Overrides=r.List(100000,readOverride)})
            let fonts=r.List(2,fun()->{DefinitionId=r.Token();Family=r.Token();FileName=r.Token();Sha256=r.Token();License=r.Token();Base64=r.Token()})
            if not r.AtEnd then failwith "trailing tokens"
            let value={Schema=sceneSchema;Metadata={SceneId=sceneId;Layers=layers;Entities=entities;Grid=grid;ResourceReferences=resources};Document=document;Catalog=catalog;Instances=instances;Fonts=fonts}
            let issues=if value.Schema<>schema then [issue "unknown-scene-schema" "/schema" $"expected {schema}"] else validate value
            if issues.IsEmpty then Ok value else Error issues
        with e->Error[issue "invalid-scene-serialization" "/" e.Message]
    let migrateLegacy (document:SvgDocument) catalog instances fonts =
        let value={Schema=schema;Metadata={SceneId=document.Id;Layers=[];Entities=[];Grid=None;ResourceReferences=fonts|>List.map _.DefinitionId};Document=document;Catalog=catalog;Instances=instances;Fonts=fonts}
        let issues=validate value in if issues.IsEmpty then Ok value else Error issues

module SvgScenePlacement =
    open SvgSceneWire
    let freeform (point:Point) = if finite point.X&&finite point.Y then Ok point else Error[issue "invalid-freeform-point" "/point" "point must be finite"]
    let grid (grid:SvgSceneGrid) (point:Point) =
        if not(finite grid.Origin.X&&finite grid.Origin.Y&&finite grid.Step.X&&finite grid.Step.Y)||grid.Step.X<=0.||grid.Step.Y<=0. then Error[issue "invalid-scene-grid" "/grid" "grid origin and positive steps must be finite"]
        else
            let roundAway value = if value>=0.0 then Math.Floor(value+0.5) else Math.Ceiling(value-0.5)
            freeform point|>Result.map(fun p->{X=grid.Origin.X+roundAway((p.X-grid.Origin.X)/grid.Step.X)*grid.Step.X;Y=grid.Origin.Y+roundAway((p.Y-grid.Origin.Y)/grid.Step.Y)*grid.Step.Y})
