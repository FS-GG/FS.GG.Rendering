namespace FS.GG.UI.Scene

open System
open System.Globalization
open System.Text

type SvgArtState = { Camera: SvgAffine; Selection: string list; GridSize: float; SnapToGrid: bool }

[<RequireQualifiedAccess>]
type SvgArtPrimitive = Rectangle of Rect | Ellipse of Rect | Polygon of Point list | Path of PathSpec

[<RequireQualifiedAccess>]
type SvgArtAlignment = Left | HorizontalCenter | Right | Top | VerticalCenter | Bottom

[<RequireQualifiedAccess>]
type SvgArtSiblingOrder = Back | Backward | Forward | Front

type SvgArtGuide = { Axis: string; Position: float }

[<RequireQualifiedAccess>]
type SvgArtError = InvalidInput of SvgDocumentIssue list | MissingElement of string | InvalidSelection of string

type SvgGeometryRequest =
    { OperationId: string; AcceptedRevision: int; InputContentHash: string; Operation: PathOperation
      MaximumDeviation: float; Subjects: Point list list; Clips: Point list list }

type SvgGeometryPrepared = { Request: SvgGeometryRequest; EncodedRequest: string; InputVertexCount: int }
type SvgGeometryResult = { OperationId: string; AcceptedRevision: int; InputContentHash: string; Contours: Point list list }

module private ArtCommon =
    let issue code location message = { Code = code; Location = location; Message = message }
    let finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let pointFinite (point: Point) = finite point.X && finite point.Y
    let validate (document: SvgDocument) =
        match SvgDocument.serialize document with
        | Ok _ -> Ok document
        | Error issues -> Error(SvgArtError.InvalidInput issues)
    let defaultPaint =
        { Fill = Some { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }; Stroke = None; Opacity = 1.0
          Antialias = true; BlendMode = BlendMode.SrcOver; Shader = None; ColorFilter = ColorFilter.NoColorFilter
          MaskFilter = MaskFilter.NoMaskFilter; ImageFilter = ImageFilter.NoImageFilter; PathEffect = PathEffect.NoPathEffect }
    let rec mapElement id (mapper: SvgElement -> SvgElement) (element: SvgElement) : SvgElement =
        let content = match element.Content with SvgElementContent.Group children -> SvgElementContent.Group(List.map (mapElement id mapper) children) | value -> value
        let current = { element with Content = content }
        if current.Id = id then mapper current else current
    let contains id (document: SvgDocument) =
        let rec find (element: SvgElement) = element.Id = id || match element.Content with SvgElementContent.Group children -> List.exists find children | _ -> false
        List.exists find document.Children
    let transformIds (ids: string list) affine (document: SvgDocument) =
        if ids.IsEmpty || not (SvgAffine.isFinite affine) then Error(SvgArtError.InvalidSelection "a finite transform and at least one element are required")
        else
            match ids |> List.tryFind (fun id -> not (contains id document)) with
            | Some id -> Error(SvgArtError.MissingElement id)
            | None ->
                let selected = Set.ofList ids
                let rec map (element: SvgElement) : SvgElement =
                    let content = match element.Content with SvgElementContent.Group children -> SvgElementContent.Group(List.map map children) | value -> value
                    let value = { element with Content = content }
                    if selected.Contains value.Id then { value with Transform = SvgAffine.compose affine value.Transform } else value
                validate { document with Children = List.map map document.Children }
    let pathBounds (path: PathSpec) =
        let points =
            path.Commands |> List.collect (function PathCommand.MoveTo p | PathCommand.LineTo p -> [p] | PathCommand.QuadTo(c,p) -> [c;p] | PathCommand.CubicTo(a,b,p) -> [a;b;p] | PathCommand.ArcTo(r,_,_) -> [{X=r.X;Y=r.Y};{X=r.X+r.Width;Y=r.Y+r.Height}] | PathCommand.Close -> [])
        match points with
        | [] -> None
        | first :: rest ->
            let minX,maxX,minY,maxY = rest |> List.fold (fun (a,b,c,d) p -> min a p.X,max b p.X,min c p.Y,max d p.Y) (first.X,first.X,first.Y,first.Y)
            Some { X=minX; Y=minY; Width=maxX-minX; Height=maxY-minY }
    let rec bounds (element: SvgElement) : Rect option =
        let local =
            match element.Content with
            | SvgElementContent.SceneLeaf { Nodes = [ SceneNode.PaintedRectangle(rect,_) ] }
            | SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Ellipse(rect,_) ] } -> Some rect
            | SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(path,_) ] } -> pathBounds path
            | SvgElementContent.Group children ->
                match children |> List.choose bounds with
                | [] -> None
                | first :: rest ->
                    rest
                    |> List.fold (fun a b ->
                        let x = min a.X b.X
                        let y = min a.Y b.Y
                        { X = x
                          Y = y
                          Width = max (a.X + a.Width) (b.X + b.Width) - x
                          Height = max (a.Y + a.Height) (b.Y + b.Height) - y }) first
                    |> Some
            | _ -> None
        local |> Option.map (fun r -> { r with X=r.X+element.Transform.E; Y=r.Y+element.Transform.F })

module SvgArt =
    open ArtCommon
    let initialState = { Camera = SvgAffine.identity; Selection = []; GridSize = 8.0; SnapToGrid = true }
    let snapPoint (state: SvgArtState) (point: Point) =
        if not (pointFinite point) || not (finite state.GridSize) || state.GridSize < 0.01 then Error(SvgArtError.InvalidInput [issue "invalid-grid" "/tool/gridSize" "grid size and point must be finite; grid size must be at least 0.01"])
        elif not state.SnapToGrid then Ok point
        else let snap v = Math.Round(v / state.GridSize, MidpointRounding.AwayFromZero) * state.GridSize in Ok {X=snap point.X;Y=snap point.Y}
    let guides state (points: Point list) =
        points |> List.map (snapPoint state) |> List.fold (fun acc value -> match acc,value with Ok values,Ok p -> Ok({Axis="x";Position=p.X}::{Axis="y";Position=p.Y}::values) | Error e,_ | _,Error e -> Error e) (Ok []) |> Result.map (List.distinct >> List.sortBy (fun g -> g.Axis,g.Position))
    let create elementId primitive presentation (document: SvgDocument) =
        if String.IsNullOrWhiteSpace elementId || contains elementId document then Error(SvgArtError.InvalidSelection "element identity must be nonblank and unique")
        else
            let content =
                match primitive with
                | SvgArtPrimitive.Rectangle rect -> SvgElementContent.SceneLeaf {Nodes=[SceneNode.PaintedRectangle(rect,defaultPaint)]}
                | SvgArtPrimitive.Ellipse rect -> SvgElementContent.SceneLeaf {Nodes=[SceneNode.Ellipse(rect,defaultPaint)]}
                | SvgArtPrimitive.Polygon points -> SvgElementContent.SceneLeaf {Nodes=[SceneNode.Path({Commands=(points |> List.mapi (fun i p -> if i=0 then PathCommand.MoveTo p else PathCommand.LineTo p)) @ [PathCommand.Close];FillType=presentation.FillRule},defaultPaint)]}
                | SvgArtPrimitive.Path path -> SvgElementContent.SceneLeaf {Nodes=[SceneNode.Path(path,defaultPaint)]}
            let element: SvgElement = {Id=elementId;SemanticId=Some elementId;Visible=true;Transform=SvgAffine.identity;ClipId=None;MaskId=None;Presentation=Some presentation;Content=content}
            validate {document with Children=document.Children @ [element]}
    let replacePath elementId path (document: SvgDocument) =
        if not (contains elementId document) then Error(SvgArtError.MissingElement elementId) else
        document.Children |> List.map (mapElement elementId (fun e -> match e.Content with SvgElementContent.SceneLeaf {Nodes=[SceneNode.Path(_,paint)]} -> {e with Content=SvgElementContent.SceneLeaf {Nodes=[SceneNode.Path(path,paint)]}} | _ -> e)) |> fun children -> validate {document with Children=children}
    let insertPathPoint elementId index point (document: SvgDocument) =
        if index < 0 || not (pointFinite point) then Error(SvgArtError.InvalidSelection "path index and point must be valid") else
        let edit (path: PathSpec) = if index > path.Commands.Length then None else Some {path with Commands=List.insertAt index (PathCommand.LineTo point) path.Commands}
        let mutable changed=false
        let mapper (element: SvgElement) =
            match element.Content with
            | SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(path, paint) ] } ->
                match edit path with
                | Some value ->
                    changed <- true
                    { element with Content = SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(value, paint) ] } }
                | None -> element
            | _ -> element
        let result={document with Children=List.map (mapElement elementId mapper) document.Children}
        if changed then validate result else Error(SvgArtError.MissingElement elementId)
    let removePathPoint elementId index (document: SvgDocument) =
        if index < 0 then Error(SvgArtError.InvalidSelection "path index must be non-negative") else
        let mutable changed=false
        let mapper (element: SvgElement) =
            match element.Content with
            | SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(path, paint) ] } when index < path.Commands.Length ->
                changed <- true
                { element with Content = SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path({ path with Commands = List.removeAt index path.Commands }, paint) ] } }
            | _ -> element
        let result={document with Children=List.map (mapElement elementId mapper) document.Children}
        if changed then validate result else Error(SvgArtError.MissingElement elementId)
    let transform ids affine document = transformIds ids affine document
    let translate ids x y document = transformIds ids (SvgAffine.translate x y) document
    let rotate ids degrees document = transformIds ids (SvgAffine.rotateDegrees degrees) document
    let scale ids x y document = transformIds ids (SvgAffine.scale x y) document
    let group groupId (ids: string list) (document: SvgDocument) =
        if String.IsNullOrWhiteSpace groupId || contains groupId document || ids.IsEmpty then Error(SvgArtError.InvalidSelection "group identity and selection are invalid") else
        let selected=Set.ofList ids
        let children=document.Children |> List.filter (fun (e: SvgElement) -> selected.Contains e.Id)
        if children.Length<>selected.Count then Error(SvgArtError.InvalidSelection "grouping currently requires distinct top-level elements") else
        let group: SvgElement = {Id=groupId;SemanticId=Some groupId;Visible=true;Transform=SvgAffine.identity;ClipId=None;MaskId=None;Presentation=None;Content=SvgElementContent.Group children}
        let first=document.Children |> List.findIndex (fun e -> selected.Contains e.Id)
        document.Children |> List.filter (fun e -> not(selected.Contains e.Id)) |> List.insertAt first group |> fun value -> validate {document with Children=value}
    let ungroup groupId document =
        match document.Children |> List.tryFindIndex (fun (e: SvgElement) -> e.Id=groupId) with
        | None -> Error(SvgArtError.MissingElement groupId)
        | Some index ->
            match document.Children[index].Content with
            | SvgElementContent.Group children ->
                let value = document.Children |> List.removeAt index |> List.insertManyAt index children
                validate { document with Children = value }
            | _ -> Error(SvgArtError.InvalidSelection "selected element is not a top-level group")
    let align alignment ids document =
        let selected=Set.ofList ids
        let found=document.Children |> List.filter (fun (e: SvgElement) -> selected.Contains e.Id) |> List.choose (fun e -> bounds e |> Option.map (fun b -> e.Id,b))
        if found.Length<>selected.Count || found.Length<2 then Error(SvgArtError.InvalidSelection "alignment requires two bounded top-level elements") else
        let target =
            match alignment with
            | SvgArtAlignment.Left -> found |> List.minBy (fun (_, b) -> b.X) |> snd |> fun b -> b.X
            | SvgArtAlignment.Right -> found |> List.maxBy (fun (_, b) -> b.X + b.Width) |> snd |> fun b -> b.X + b.Width
            | SvgArtAlignment.Top -> found |> List.minBy (fun (_, b) -> b.Y) |> snd |> fun b -> b.Y
            | SvgArtAlignment.Bottom -> found |> List.maxBy (fun (_, b) -> b.Y + b.Height) |> snd |> fun b -> b.Y + b.Height
            | SvgArtAlignment.HorizontalCenter -> found |> List.averageBy (fun (_, b) -> b.X + b.Width / 2.0)
            | SvgArtAlignment.VerticalCenter -> found |> List.averageBy (fun (_, b) -> b.Y + b.Height / 2.0)
        let offsetFor b =
            match alignment with
            | SvgArtAlignment.Left -> target - b.X
            | SvgArtAlignment.Right -> target - (b.X + b.Width)
            | SvgArtAlignment.HorizontalCenter -> target - (b.X + b.Width / 2.0)
            | SvgArtAlignment.Top -> target - b.Y
            | SvgArtAlignment.Bottom -> target - (b.Y + b.Height)
            | SvgArtAlignment.VerticalCenter -> target - (b.Y + b.Height / 2.0)
        let offsets = found |> List.map (fun (id, b) -> id, offsetFor b) |> Map.ofList
        let children =
            document.Children
            |> List.map (fun element ->
                match offsets.TryFind element.Id with
                | None -> element
                | Some delta ->
                    match alignment with
                    | SvgArtAlignment.Left | SvgArtAlignment.Right | SvgArtAlignment.HorizontalCenter ->
                        { element with Transform = SvgAffine.compose (SvgAffine.translate delta 0.0) element.Transform }
                    | _ -> { element with Transform = SvgAffine.compose (SvgAffine.translate 0.0 delta) element.Transform })
        validate {document with Children=children}
    let reorder id order document =
        match document.Children |> List.tryFindIndex (fun (e: SvgElement) -> e.Id=id) with
        | None -> Error(SvgArtError.MissingElement id)
        | Some index ->
            let item = document.Children[index]
            let rest = List.removeAt index document.Children
            let target =
                match order with
                | SvgArtSiblingOrder.Back -> 0
                | SvgArtSiblingOrder.Front -> rest.Length
                | SvgArtSiblingOrder.Backward -> max 0 (index - 1)
                | SvgArtSiblingOrder.Forward -> min rest.Length (index + 1)
            validate { document with Children = List.insertAt target item rest }
    let setPresentation ids presentation document =
        let selected=Set.ofList ids
        if ids.IsEmpty || ids |> List.exists (fun id -> not(contains id document)) then Error(SvgArtError.InvalidSelection "presentation selection is empty or missing") else
        let rec map (e: SvgElement) : SvgElement = let content=match e.Content with SvgElementContent.Group children->SvgElementContent.Group(List.map map children)|v->v in let value={e with Content=content} in if selected.Contains e.Id then {value with Presentation=Some presentation} else value
        validate {document with Children=List.map map document.Children}
    let putGradient id gradient document =
        if String.IsNullOrWhiteSpace id then Error(SvgArtError.InvalidSelection "gradient identity must not be blank") else
        let value: SvgDefinition = {Id=id;Content=SvgDefinitionContent.Gradient gradient}
        validate {document with Definitions=value::(document.Definitions |> List.filter(fun d->d.Id<>id))}

module SvgGeometry =
    open ArtCommon
    let defaultMaximumDeviation=0.25
    let maximumSubdivisionDepth=16
    let midpoint (a: Point) (b: Point) : Point = {X=(a.X+b.X)/2.0;Y=(a.Y+b.Y)/2.0}
    let distanceLine (p: Point) (a: Point) (b: Point) =
        let dx = b.X - a.X
        let dy = b.Y - a.Y
        let length = sqrt (dx * dx + dy * dy)
        if length = 0.0 then sqrt ((p.X-a.X) ** 2.0 + (p.Y-a.Y) ** 2.0)
        else abs (dy*p.X-dx*p.Y+b.X*a.Y-b.Y*a.X) / length
    let flatten deviation (path: PathSpec) =
        if path.Commands |> List.exists (function PathCommand.ArcTo _ -> true | _ -> false) then Error "arc contours require an explicit path conversion before Boolean geometry"
        else
        let output = ResizeArray<Point>()
        let mutable current = None
        let mutable start = None
        let rec quad depth p0 c p1 =
            if depth >= maximumSubdivisionDepth || distanceLine c p0 p1 <= deviation then output.Add p1
            else
                let a = midpoint p0 c
                let b = midpoint c p1
                let m = midpoint a b
                quad (depth + 1) p0 a m
                quad (depth + 1) m b p1
        let rec cubic depth p0 c1 c2 p1 =
            if depth >= maximumSubdivisionDepth || max (distanceLine c1 p0 p1) (distanceLine c2 p0 p1) <= deviation then output.Add p1
            else
                let a = midpoint p0 c1
                let b = midpoint c1 c2
                let c = midpoint c2 p1
                let d = midpoint a b
                let e = midpoint b c
                let m = midpoint d e
                cubic (depth + 1) p0 a d m
                cubic (depth + 1) m e c p1
        let mutable closed = false
        for command in path.Commands do
            match command, current with
            | PathCommand.MoveTo p, _ -> output.Add p; current <- Some p; start <- Some p
            | PathCommand.LineTo p, Some _ -> output.Add p; current <- Some p
            | PathCommand.QuadTo(c,p), Some p0 -> quad 0 p0 c p; current <- Some p
            | PathCommand.CubicTo(a,b,p), Some p0 -> cubic 0 p0 a b p; current <- Some p
            | PathCommand.Close, Some _ ->
                closed <- true
                start |> Option.iter (fun p -> if output[output.Count-1] <> p then output.Add p)
                current <- start
            | _ -> ()
        if not closed then Error "open contours are unsupported" elif output.Count<4 then Error "degenerate contour" else Ok(List.ofSeq output)
    let selfIntersects (points: Point list) =
        let cross (a: Point) (b: Point) (c: Point) = (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X)
        let intersects ((a,b): Point * Point) ((c,d): Point * Point) =
            let first = cross a b c
            let second = cross a b d
            let third = cross c d a
            let fourth = cross c d b
            first * second < 0.0 && third * fourth < 0.0
        let segments = points |> List.pairwise
        segments
        |> List.mapi (fun index segment -> index,segment)
        |> List.exists (fun (firstIndex, first) ->
            segments
            |> List.mapi (fun index segment -> index,segment)
            |> List.exists (fun (secondIndex, second) ->
                secondIndex > firstIndex + 1
                && not (firstIndex = 0 && secondIndex = segments.Length - 1)
                && intersects first second))
    let num (value: float) = string value
    let utf8ByteCount (value: string) =
        let mutable count = 0
        let mutable index = 0
        while index < value.Length do
            let code = int value[index]
            if code <= 0x7f then count <- count + 1
            elif code <= 0x7ff then count <- count + 2
            elif code >= 0xd800 && code <= 0xdbff && index + 1 < value.Length then count <- count + 4; index <- index + 1
            else count <- count + 3
            index <- index + 1
        count
    let encode (request: SvgGeometryRequest) =
        let points (contours: Point list list) = contours |> List.map(fun ps->"["+(ps|>List.map(fun p->"["+num p.X+","+num p.Y+"]")|>String.concat ",")+"]") |> String.concat ","
        let op=match request.Operation with PathOperation.Union->"union"|PathOperation.Intersect->"intersection"|PathOperation.Difference->"difference"|PathOperation.Xor->"xor"
        $"{{\"operationId\":\"{request.OperationId}\",\"acceptedRevision\":{request.AcceptedRevision},\"inputContentHash\":\"{request.InputContentHash}\",\"operation\":\"{op}\",\"subjects\":[{points request.Subjects}],\"clips\":[{points request.Clips}]}}"
    let prepare operationId revision operation subjects clips deviation =
        if String.IsNullOrWhiteSpace operationId || revision<0 || not(finite deviation) || deviation<0.01 || deviation>1.0 then Error(SvgArtError.InvalidInput [issue "invalid-geometry-request" "/geometry" "operation identity, revision and deviation (0.01–1.0) are required"])
        elif (subjects@clips) |> List.exists(fun p->p.FillType<>PathFillType.Winding) then Error(SvgArtError.InvalidInput [issue "unsupported-fill-rule" "/geometry" "polygon-clipping interchange requires winding fill"])
        else
            let flattened=(subjects@clips)|>List.map(flatten deviation)
            match flattened |> List.tryPick (function Error e -> Some e | _ -> None) with
            | Some message -> Error(SvgArtError.InvalidInput [issue "unsupported-contour" "/geometry" message])
            | None ->
                let values = flattened |> List.choose (function Ok v -> Some v | _ -> None)
                let subjectValues = values |> List.take subjects.Length
                let clipValues = values |> List.skip subjects.Length
                let count = values |> List.sumBy List.length
                if values |> List.exists selfIntersects then Error(SvgArtError.InvalidInput [issue "unsupported-self-intersection" "/geometry" "self-intersecting contours are unsupported"])
                elif values.Length > 128 || count > 10000 then Error(SvgArtError.InvalidInput [issue "geometry-budget" "/geometry" "input exceeds 128 contours or 10,000 vertices"])
                else
                    // Hash the canonical flattened wire values. F# structural
                    // formatting differs between .NET and Fable, so it cannot
                    // serve as a portable request identity.
                    let contourSource (contours: Point list list) =
                        contours
                        |> List.map (fun contour -> contour |> List.map (fun (point: Point) -> num point.X + "," + num point.Y) |> String.concat ";")
                        |> String.concat "|"
                    let operationSource =
                        match operation with
                        | PathOperation.Union -> "union"
                        | PathOperation.Intersect -> "intersection"
                        | PathOperation.Difference -> "difference"
                        | PathOperation.Xor -> "xor"
                    let source = operationSource + "#" + num deviation + "#" + contourSource subjectValues + "#" + contourSource clipValues
                    // Keep the request identity byte-for-byte portable. Fable's
                    // JavaScript number multiplication does not preserve the
                    // uint32 wraparound required by FNV-1a for every value.
                    // Adler-32 needs only bounded additions for this ASCII wire.
                    let mutable a = 1u
                    let mutable b = 0u
                    for character in source do
                        a <- (a + uint32 character) % 65521u
                        b <- (b + a) % 65521u
                    let hash = ((b <<< 16) ||| a).ToString("x8")
                    let request = {OperationId=operationId;AcceptedRevision=revision;InputContentHash=hash;Operation=operation;MaximumDeviation=deviation;Subjects=subjectValues;Clips=clipValues}
                    let encoded = encode request
                    if utf8ByteCount encoded > 1048576 then Error(SvgArtError.InvalidInput [issue "geometry-byte-limit" "/geometry" "encoded request exceeds 1 MiB"])
                    else Ok {Request=request;EncodedRequest=encoded;InputVertexCount=count}
    let transaction result prepared document =
        if result.OperationId<>prepared.Request.OperationId || result.AcceptedRevision<>prepared.Request.AcceptedRevision || result.InputContentHash<>prepared.Request.InputContentHash then Error(SvgArtError.InvalidInput [issue "stale-geometry-result" "/geometry" "worker result identity does not match the accepted request"])
        elif result.Contours.Length > 128 || (result.Contours |> List.sumBy List.length) > 20000 then
            Error(SvgArtError.InvalidInput [issue "geometry-result-budget" "/geometry" "result exceeds 128 contours or 20,000 vertices"])
        elif result.Contours |> List.exists (fun contour -> contour.Length < 4 || contour.Head <> contour[contour.Length - 1] || (contour |> List.exists (pointFinite >> not))) then
            Error(SvgArtError.InvalidInput [issue "invalid-geometry-result" "/geometry" "worker returned an open, degenerate or nonfinite contour"])
        else
            let commands=result.Contours |> List.collect(fun c->c|>List.mapi(fun i p->if i=0 then PathCommand.MoveTo p else PathCommand.LineTo p) |> fun cs->cs@[PathCommand.Close])
            let presentation=SvgDocument.defaultPresentation
            match SvgArt.create ("boolean-"+result.OperationId) (SvgArtPrimitive.Path {Commands=commands;FillType=PathFillType.Winding}) presentation document with
            | Error e->Error e
            | Ok candidate->Ok {Schema=SvgAuthoring.transactionSchema;Id=result.OperationId;Operations=[SvgAuthoringOperation.ReplaceDocument candidate]}
