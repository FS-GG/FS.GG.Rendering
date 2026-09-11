namespace FS.GG.UI.Scene

open System
open System.Collections.Generic
open System.Globalization
open System.Text

type SvgAffine =
    { A: float
      B: float
      C: float
      D: float
      E: float
      F: float }

[<RequireQualifiedAccess>]
type SvgAffineError =
    | NonFinite
    | Singular

[<RequireQualifiedAccess>]
module SvgAffine =
    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let identity = { A = 1.0; B = 0.0; C = 0.0; D = 1.0; E = 0.0; F = 0.0 }
    let translate x y = { identity with E = x; F = y }
    let scale x y = { A = x; B = 0.0; C = 0.0; D = y; E = 0.0; F = 0.0 }

    let rotateDegrees angle =
        let radians = angle * Math.PI / 180.0
        let cosine = Math.Cos radians
        let sine = Math.Sin radians
        { A = cosine; B = sine; C = -sine; D = cosine; E = 0.0; F = 0.0 }

    let skewXDegrees angle =
        { identity with C = Math.Tan(angle * Math.PI / 180.0) }

    let skewYDegrees angle =
        { identity with B = Math.Tan(angle * Math.PI / 180.0) }

    let compose parent local =
        { A = parent.A * local.A + parent.C * local.B
          B = parent.B * local.A + parent.D * local.B
          C = parent.A * local.C + parent.C * local.D
          D = parent.B * local.C + parent.D * local.D
          E = parent.A * local.E + parent.C * local.F + parent.E
          F = parent.B * local.E + parent.D * local.F + parent.F }

    let transformPoint (transform: SvgAffine) (point: Point) =
        { X = transform.A * point.X + transform.C * point.Y + transform.E
          Y = transform.B * point.X + transform.D * point.Y + transform.F }

    let isFinite transform =
        finite transform.A && finite transform.B && finite transform.C && finite transform.D
        && finite transform.E && finite transform.F

    let tryInverse transform =
        if not (isFinite transform) then
            Error SvgAffineError.NonFinite
        else
            let determinant = transform.A * transform.D - transform.B * transform.C
            if not (finite determinant) || abs determinant <= 1e-12 then
                Error SvgAffineError.Singular
            else
                Ok
                    { A = transform.D / determinant
                      B = -transform.B / determinant
                      C = -transform.C / determinant
                      D = transform.A / determinant
                      E = (transform.C * transform.F - transform.D * transform.E) / determinant
                      F = (transform.B * transform.E - transform.A * transform.F) / determinant }

[<RequireQualifiedAccess>]
type SvgCoordinateUnits =
    | UserSpaceOnUse
    | ObjectBoundingBox

[<RequireQualifiedAccess>]
type SvgSpreadMethod =
    | Pad
    | Repeat
    | Reflect

type SvgGradientStop =
    { Offset: float
      Color: Color
      StopOpacity: float }

[<RequireQualifiedAccess>]
type SvgGradientGeometry =
    | Linear of Start: Point * Finish: Point
    | Radial of Center: Point * Radius: float * Focal: Point option

type SvgGradientDefinition =
    { Geometry: SvgGradientGeometry
      Units: SvgCoordinateUnits
      Transform: SvgAffine
      Spread: SvgSpreadMethod
      Stops: SvgGradientStop list
      InheritFrom: string option }

[<RequireQualifiedAccess>]
type SvgPaintSource =
    | Solid of Color
    | Definition of string

type SvgStrokePresentation =
    { Source: SvgPaintSource
      Width: float
      Cap: StrokeCap
      Join: StrokeJoin
      Miter: float
      Dash: float list
      DashOffset: float }

type SvgPresentation =
    { FillSource: SvgPaintSource option
      StrokeStyle: SvgStrokePresentation option
      OverallOpacity: float
      FillRule: PathFillType }

[<RequireQualifiedAccess>]
type SvgMaskKind =
    | Alpha
    | Luminance

[<RequireQualifiedAccess>]
type SvgClipShape =
    | Rectangle of Rect
    | Path of PathSpec
    | Intersection of definitionIds: string list

type SvgFontReference =
    { Family: string
      Source: string
      Sha256: string
      License: string }

type SvgElement =
    { Id: string
      SemanticId: string option
      Visible: bool
      Transform: SvgAffine
      ClipId: string option
      MaskId: string option
      Presentation: SvgPresentation option
      Content: SvgElementContent }

and [<RequireQualifiedAccess>] SvgElementContent =
    | SceneLeaf of Scene
    | Group of SvgElement list
    | SymbolInstance of definitionId: string * viewport: Rect option

type SvgDefinition =
    { Id: string
      Content: SvgDefinitionContent }

and [<RequireQualifiedAccess>] SvgDefinitionContent =
    | Symbol of viewBox: Rect option * children: SvgElement list
    | Clip of units: SvgCoordinateUnits * shapes: SvgClipShape list
    | Mask of units: SvgCoordinateUnits * region: Rect * kind: SvgMaskKind * children: SvgElement list
    | Gradient of SvgGradientDefinition
    | Font of SvgFontReference

type SvgDocument =
    { Schema: string
      Id: string
      ViewBox: Rect
      Definitions: SvgDefinition list
      Children: SvgElement list }

type SvgDocumentLimits =
    { MaxSerializedBytes: int
      MaxNodes: int
      MaxPathSegments: int
      MaxDefinitions: int
      MaxReferenceDepth: int
      MaxExpandedSymbolNodes: int }

type SvgDocumentIssue =
    { Code: string
      Location: string
      Message: string }

[<RequireQualifiedAccess>]
type SvgRuntimeSupport =
    | Supported
    | ContractOnly of reason: string
    | Unsupported of reason: string

type SvgAssetDescriptor =
    { AssetId: string
      Version: string
      Sha256: string
      License: string
      Document: SvgDocument }

type SvgBuildExtensionDescriptor =
    { ExtensionId: string
      Version: string
      EntryPoint: string
      Capabilities: string list
      Support: SvgRuntimeSupport }

[<RequireQualifiedAccess>]
module SvgDocument =
    let schema = "fsgg.svg-document/1"

    let defaultLimits =
        { MaxSerializedBytes = 4 * 1024 * 1024
          MaxNodes = 10000
          MaxPathSegments = 100000
          MaxDefinitions = 512
          MaxReferenceDepth = 32
          MaxExpandedSymbolNodes = 50000 }

    let defaultPresentation =
        { FillSource = None
          StrokeStyle = None
          OverallOpacity = 1.0
          FillRule = PathFillType.Winding }

    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let private hex value =
        (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F')
    let private issue code path message = { Code = code; Location = path; Message = message }
    let private validRect rect =
        finite rect.X && finite rect.Y && finite rect.Width && finite rect.Height
        && rect.Width >= 0.0 && rect.Height >= 0.0

    let private validPoint (point: Point) = finite point.X && finite point.Y

    let private validPath (path: PathSpec) =
        path.Commands
        |> List.forall (function
            | PathCommand.MoveTo point
            | PathCommand.LineTo point -> validPoint point
            | PathCommand.QuadTo(control, point) -> validPoint control && validPoint point
            | PathCommand.CubicTo(first, second, point) -> validPoint first && validPoint second && validPoint point
            | PathCommand.ArcTo(bounds, startAngle, sweepAngle) -> validRect bounds && bounds.Width > 0.0 && bounds.Height > 0.0 && finite startAngle && finite sweepAngle
            | PathCommand.Close -> true)

    let private childPath parent index = $"{parent}/children/{index}"

    let rec private sceneCounts (scene: Scene) =
        let rec nodeCount node =
            match node with
            | SceneNode.Group scenes -> 1 + (scenes |> List.sumBy (sceneCounts >> fst))
            | SceneNode.Translate(_, child)
            | SceneNode.ClipNode(_, child)
            | SceneNode.ColorSpaceNode(_, child) -> 1 + fst (sceneCounts child)
            | SceneNode.PictureNode picture -> 1 + fst (sceneCounts picture.Scene)
            | SceneNode.CachedSubtree cached -> 1 + fst (sceneCounts cached.Scene)
            | _ -> 1
        let rec segmentCount node =
            match node with
            | SceneNode.Path(path, _) -> path.Commands.Length
            | SceneNode.Group scenes -> scenes |> List.sumBy (sceneCounts >> snd)
            | SceneNode.Translate(_, child)
            | SceneNode.ClipNode(_, child)
            | SceneNode.ColorSpaceNode(_, child) -> snd (sceneCounts child)
            | SceneNode.PictureNode picture -> snd (sceneCounts picture.Scene)
            | SceneNode.CachedSubtree cached -> snd (sceneCounts cached.Scene)
            | _ -> 0
        scene.Nodes |> List.sumBy nodeCount, scene.Nodes |> List.sumBy segmentCount

    let private refsOfElement (element: SvgElement) =
        [ yield! element.ClipId |> Option.toList
          yield! element.MaskId |> Option.toList
          match element.Presentation with
          | Some presentation ->
              let sourceRef = function SvgPaintSource.Definition id -> [ id ] | _ -> []
              yield! presentation.FillSource |> Option.toList |> List.collect sourceRef
              yield! presentation.StrokeStyle |> Option.toList |> List.collect (fun stroke -> sourceRef stroke.Source)
          | None -> ()
          match element.Content with
          | SvgElementContent.SymbolInstance(id, _) -> yield id
          | _ -> () ]

    let rec private elementCounts (element: SvgElement) =
        let ownNodes, ownSegments =
            match element.Content with
            | SvgElementContent.SceneLeaf scene -> sceneCounts scene
            | _ -> 0, 0
        match element.Content with
        | SvgElementContent.Group children ->
            let nestedNodes, nestedSegments =
                children |> List.map elementCounts |> List.fold (fun (n, s) (cn, cs) -> n + cn, s + cs) (0, 0)
            1 + ownNodes + nestedNodes, ownSegments + nestedSegments
        | _ -> 1 + ownNodes, ownSegments

    let validate serializedByteCount (limits: SvgDocumentLimits) (document: SvgDocument) =
        let mutable issues: SvgDocumentIssue list = []
        let add code path message = issues <- issue code path message :: issues
        let definitions: Map<string, SvgDefinition> = document.Definitions |> List.map (fun value -> value.Id, value) |> Map.ofList

        let definitionKind (content: SvgDefinitionContent) =
            match content with
            | SvgDefinitionContent.Symbol _ -> "symbol"
            | SvgDefinitionContent.Clip _ -> "clip"
            | SvgDefinitionContent.Mask _ -> "mask"
            | SvgDefinitionContent.Gradient _ -> "gradient"
            | SvgDefinitionContent.Font _ -> "font"

        let requireDefinition expected path reference =
            match definitions |> Map.tryFind reference with
            | None -> add "missing-reference" path $"definition does not exist: {reference}"
            | Some definition when definitionKind definition.Content <> expected ->
                add "wrong-reference-kind" path $"expected {expected} definition but {reference} is {definitionKind definition.Content}"
            | Some _ -> ()

        if document.Schema <> schema then add "unknown-schema" "/schema" $"expected {schema}"
        if String.IsNullOrWhiteSpace document.Id then add "blank-document-id" "/id" "document id must not be blank"
        if not (validRect document.ViewBox) || document.ViewBox.Width <= 0.0 || document.ViewBox.Height <= 0.0 then
            add "invalid-view-box" "/viewBox" "viewBox must be finite with positive width and height"
        if serializedByteCount < 0 || serializedByteCount > limits.MaxSerializedBytes then
            add "document-byte-limit" "/" $"serialized document exceeds {limits.MaxSerializedBytes} bytes"
        if document.Definitions.Length > limits.MaxDefinitions then
            add "definition-limit" "/definitions" $"document exceeds {limits.MaxDefinitions} definitions"

        let duplicateIds kind path ids =
            ids |> List.countBy id |> List.iter (fun (id, count) -> if count > 1 then add "duplicate-id" path $"{kind} id is duplicated: {id}")
        duplicateIds "definition" "/definitions" (document.Definitions |> List.map _.Id)

        let mutable allElementIds = []
        let mutable allSemanticIds = []
        let rec validateElement path (element: SvgElement) =
            allElementIds <- element.Id :: allElementIds
            element.SemanticId |> Option.iter (fun id -> allSemanticIds <- id :: allSemanticIds)
            if String.IsNullOrWhiteSpace element.Id then add "blank-element-id" (path + "/id") "element id must not be blank"
            if element.SemanticId |> Option.exists String.IsNullOrWhiteSpace then
                add "blank-semantic-id" (path + "/semanticId") "semantic id must not be blank"
            if not (SvgAffine.isFinite element.Transform) then add "non-finite-transform" (path + "/transform") "transform must be finite"
            element.ClipId |> Option.iter (requireDefinition "clip" path)
            element.MaskId |> Option.iter (requireDefinition "mask" path)
            match element.Presentation with
            | Some presentation ->
                let requirePaint = function SvgPaintSource.Definition reference -> requireDefinition "gradient" path reference | _ -> ()
                presentation.FillSource |> Option.iter requirePaint
                presentation.StrokeStyle |> Option.iter (fun stroke ->
                    requirePaint stroke.Source
                    if not (finite stroke.Width && finite stroke.Miter && finite stroke.DashOffset) || stroke.Width < 0.0 || stroke.Miter < 0.0
                       || stroke.Dash |> List.exists (fun value -> not (finite value) || value <= 0.0) then
                        add "invalid-stroke-presentation" (path + "/presentation/stroke") "stroke width, miter, dash and dash offset must be finite and non-negative; dash entries must be positive")
                if not (finite presentation.OverallOpacity) || presentation.OverallOpacity < 0.0 || presentation.OverallOpacity > 1.0 then
                    add "invalid-presentation-opacity" (path + "/presentation/opacity") "opacity must be finite and between zero and one"
            | None -> ()
            match element.Content with
            | SvgElementContent.Group children -> children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgElementContent.SceneLeaf scene ->
                let validationScene =
                    { RootId = "document-validation"
                      Revision = 0
                      Camera = { PanX = 0.0; PanY = 0.0; Zoom = 1.0 }
                      Layers =
                        [ { Id = "document"
                            Visible = true
                            Objects =
                              [ { Id = element.Id
                                  Selectable = element.SemanticId.IsSome
                                  AccessibleLabel = element.SemanticId |> Option.defaultValue element.Id
                                  Content = scene } ] } ] }
                match SvgRetained.project validationScene with
                | SvgAdapterResult.Rendered _ -> ()
                | SvgAdapterResult.Invalid adapterIssues
                | SvgAdapterResult.Unsupported adapterIssues ->
                    adapterIssues
                    |> List.iter (fun adapterIssue ->
                        let nodePath = adapterIssue.NodePath |> List.map string |> String.concat "/"
                        let location = if nodePath = "" then path + "/scene" else path + "/scene/nodes/" + nodePath
                        add "unsupported-scene-leaf" location adapterIssue.Reason)
            | SvgElementContent.SymbolInstance(reference, viewport) ->
                viewport
                |> Option.iter (fun value ->
                    if not (validRect value) || value.Width < 0.0 || value.Height < 0.0 then
                        add "invalid-symbol-viewport" (path + "/viewport") "symbol viewport must be finite and non-negative")
                requireDefinition "symbol" path reference

        let rec validateDefinition path (definition: SvgDefinition) =
            if String.IsNullOrWhiteSpace definition.Id then add "blank-definition-id" (path + "/id") "definition id must not be blank"
            match definition.Content with
            | SvgDefinitionContent.Symbol(Some viewBox, _) when not (validRect viewBox) || viewBox.Width <= 0.0 || viewBox.Height <= 0.0 ->
                add "invalid-symbol-view-box" (path + "/viewBox") "symbol viewBox must be finite with positive width and height"
            | SvgDefinitionContent.Symbol(_, children) ->
                children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgDefinitionContent.Clip(_, shapes) ->
                shapes |> List.iteri (fun index shape ->
                    match shape with
                    | SvgClipShape.Rectangle rect when not (validRect rect) || rect.Width < 0.0 || rect.Height < 0.0 -> add "invalid-clip-rectangle" $"{path}/shapes/{index}" "clip rectangle must be finite and non-negative"
                    | SvgClipShape.Path pathSpec when not (validPath pathSpec) -> add "invalid-clip-path" $"{path}/shapes/{index}" "clip path geometry must be finite with positive arc bounds"
                    | SvgClipShape.Intersection references ->
                        if references.IsEmpty then add "empty-clip-intersection" $"{path}/shapes/{index}" "clip intersection must reference at least one clip"
                        references |> List.iter (requireDefinition "clip" path)
                    | _ -> ())
            | SvgDefinitionContent.Mask(_, region, _, children) ->
                if not (validRect region) || region.Width < 0.0 || region.Height < 0.0 then add "invalid-mask-region" (path + "/region") "mask region must be finite and non-negative"
                children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgDefinitionContent.Gradient gradient ->
                if not (SvgAffine.isFinite gradient.Transform) then add "non-finite-transform" (path + "/transform") "gradient transform must be finite"
                match gradient.Geometry with
                | SvgGradientGeometry.Linear(first, second) when not (validPoint first && validPoint second) ->
                    add "invalid-gradient-geometry" (path + "/geometry") "linear gradient points must be finite"
                | SvgGradientGeometry.Radial(center, radius, focal) when not (validPoint center && finite radius && radius > 0.0 && (focal |> Option.forall validPoint)) ->
                    add "invalid-gradient-geometry" (path + "/geometry") "radial gradient center/focal must be finite and radius must be positive"
                | _ -> ()
                gradient.InheritFrom |> Option.iter (requireDefinition "gradient" path)
                gradient.Stops |> List.iteri (fun index stop ->
                    if not (finite stop.Offset) || stop.Offset < 0.0 || stop.Offset > 1.0 then add "invalid-gradient-stop" $"{path}/stops/{index}" "offset must be between zero and one"
                    if not (finite stop.StopOpacity) || stop.StopOpacity < 0.0 || stop.StopOpacity > 1.0 then add "invalid-gradient-stop" $"{path}/stops/{index}" "opacity must be between zero and one")
                if gradient.Stops <> (gradient.Stops |> List.sortBy _.Offset) then add "unordered-gradient-stops" (path + "/stops") "gradient stops must be ordered"
            | SvgDefinitionContent.Font font ->
                if String.IsNullOrWhiteSpace font.Family || String.IsNullOrWhiteSpace font.Source || String.IsNullOrWhiteSpace font.Sha256 || String.IsNullOrWhiteSpace font.License then
                    add "invalid-font-reference" path "font family, source, sha256 and license are required"
                let safeFamily = font.Family |> Seq.forall (fun value -> Char.IsLetterOrDigit value || value = ' ' || value = '_' || value = '-')
                if not safeFamily then
                    add "invalid-font-family" (path + "/family") "font family may contain only letters, digits, spaces, underscores and hyphens"
                let source = font.Source.Replace('\\', '/')
                let safeSource = source |> Seq.forall (fun value -> Char.IsLetterOrDigit value || value = '/' || value = '.' || value = '_' || value = '-')
                if source.StartsWith("/") || source.Contains(":") || (source.Split('/') |> Array.exists ((=) "..")) || not safeSource || not (source.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)) then
                    add "external-font-source" (path + "/source") "font source must be a local relative WOFF2 path"
                if font.Sha256.Length <> 64 || font.Sha256 |> Seq.exists (hex >> not) then
                    add "invalid-font-sha256" (path + "/sha256") "font sha256 must be 64 hexadecimal characters"

        document.Children |> List.iteri (fun index child -> validateElement (childPath "" index) child)
        document.Definitions |> List.iteri (fun index definition -> validateDefinition $"/definitions/{index}" definition)
        duplicateIds "element" "/children" allElementIds
        duplicateIds "semantic" "/children" allSemanticIds
        duplicateIds "document" "/" (document.Id :: ((document.Definitions |> List.map _.Id) @ allElementIds))

        let definitionRefs (definition: SvgDefinition) =
            let rec fromElements (elements: SvgElement list) =
                elements |> List.collect (fun (element: SvgElement) ->
                    refsOfElement element @
                    (match element.Content with SvgElementContent.Group children -> fromElements children | _ -> []))
            match definition.Content with
            | SvgDefinitionContent.Symbol(_, children)
            | SvgDefinitionContent.Mask(_, _, _, children) -> fromElements children
            | SvgDefinitionContent.Clip(_, shapes) -> shapes |> List.collect (function SvgClipShape.Intersection ids -> ids | _ -> [])
            | SvgDefinitionContent.Gradient gradient -> gradient.InheritFrom |> Option.toList
            | SvgDefinitionContent.Font _ -> []

        let rec visit stack depth id =
            if depth > limits.MaxReferenceDepth then add "reference-depth" "/definitions" $"reference depth exceeds {limits.MaxReferenceDepth}: {id}"
            elif List.contains id stack then
                let cycle = String.concat " -> " (List.rev (id :: stack))
                add "cyclic-reference" "/definitions" $"definition cycle: {cycle}"
            else definitions |> Map.tryFind id |> Option.iter (fun definition -> definitionRefs definition |> List.iter (visit (id :: stack) (depth + 1)))
        document.Definitions |> List.iter (fun definition -> visit [] 1 definition.Id)

        let directNodes, pathSegments =
            document.Children @
            (document.Definitions |> List.collect (fun definition ->
                match definition.Content with
                | SvgDefinitionContent.Symbol(_, children)
                | SvgDefinitionContent.Mask(_, _, _, children) -> children
                | _ -> []))
            |> List.map elementCounts
            |> List.fold (fun (n, s) (cn, cs) -> n + cn, s + cs) (0, 0)
        if directNodes > limits.MaxNodes then add "node-limit" "/children" $"document exceeds {limits.MaxNodes} nodes"
        if pathSegments > limits.MaxPathSegments then add "path-segment-limit" "/children" $"document exceeds {limits.MaxPathSegments} path segments"

        let rec expanded stack depth (element: SvgElement) =
            if depth > limits.MaxReferenceDepth then 0
            else
                match element.Content with
                | SvgElementContent.Group children -> 1 + (children |> List.sumBy (expanded stack (depth + 1)))
                | SvgElementContent.SymbolInstance(id, _) when not (List.contains id stack) ->
                    match definitions |> Map.tryFind id with
                    | Some { Content = SvgDefinitionContent.Symbol(_, children) } -> 1 + (children |> List.sumBy (expanded (id :: stack) (depth + 1)))
                    | _ -> 1
                | _ -> 1
        let expandedNodes = document.Children |> List.sumBy (expanded [] 1)
        if expandedNodes > limits.MaxExpandedSymbolNodes then add "expanded-symbol-limit" "/children" $"expanded symbols exceed {limits.MaxExpandedSymbolNodes} nodes"
        List.rev issues

    let ofRetainedScene (viewBox: Rect) (scene: RetainedScene) =
        match SvgRetained.project scene with
        | SvgAdapterResult.Invalid adapterIssues
        | SvgAdapterResult.Unsupported adapterIssues ->
            adapterIssues
            |> List.map (fun value ->
                { Code = "retained-scene"
                  Location = value.NodePath |> List.map string |> String.concat "/" |> fun path -> "/scene/" + path
                  Message = value.Reason })
            |> Error
        | SvgAdapterResult.Rendered accepted ->
            let presentation = defaultPresentation
            let children =
                accepted.Layers
                |> List.map (fun layer ->
                    { Id = "layer:" + layer.Id
                      SemanticId = None
                      Visible = layer.Visible
                      Transform = SvgAffine.identity
                      ClipId = None
                      MaskId = None
                      Presentation = None
                      Content =
                        SvgElementContent.Group(
                            layer.Objects
                            |> List.map (fun value ->
                                { Id = "object:" + value.Id
                                  SemanticId = Some value.Id
                                  Visible = true
                                  Transform = SvgAffine.identity
                                  ClipId = None
                                  MaskId = None
                                  Presentation = Some presentation
                                  Content = SvgElementContent.SceneLeaf value.Content })) })
            let document =
                { Schema = schema
                  Id = accepted.RootId
                  ViewBox = viewBox
                  Definitions = []
                  Children = children }
            let issues = validate 0 defaultLimits document
            if issues.IsEmpty then Ok document else Error issues

    // The codec is deliberately small and dependency-free so the exact implementation runs under
    // both .NET and Fable. Each value is a length-prefixed token; authored strings therefore need no
    // escape convention and cannot alter the grammar. SVG XML is a separate, one-way export below.
    let private wireHeader = "FSGG-SVG-DOCUMENT-1"

    let private canonicalNumber (value: float) =
        if value = 0.0 then "0"
        else
            let formatted = sprintf "%.12f" value
            let trimmed = formatted.TrimEnd('0').TrimEnd('.')
            if trimmed = "-0" then "0" else trimmed

    type private TokenWriter() =
        let output = StringBuilder()
        member _.Token(value: string) = output.Append(value.Length).Append(':').Append(value) |> ignore
        member this.Int(value: int) = this.Token(value.ToString(CultureInfo.InvariantCulture))
        member this.Float(value: float) = this.Token(canonicalNumber value)
        member this.Bool(value: bool) = this.Token(if value then "1" else "0")
        member this.Option(write: 'a -> unit, value: 'a option) =
            this.Bool value.IsSome
            value |> Option.iter write
        member this.List(write: 'a -> unit, values: 'a list) =
            this.Int values.Length
            values |> List.iter write
        override _.ToString() = output.ToString()

    type private TokenReader(serialized: string) =
        let mutable offset = 0
        member _.AtEnd = offset = serialized.Length
        member _.Token() =
            let separator = serialized.IndexOf(':', offset)
            if separator < offset then failwith "missing token length separator"
            let lengthText = serialized.Substring(offset, separator - offset)
            let mutable length = 0
            if not (Int32.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, &length)) || length < 0 then
                failwith "invalid token length"
            let start = separator + 1
            if start + length > serialized.Length then failwith "truncated token"
            offset <- start + length
            serialized.Substring(start, length)
        member this.Int() =
            let mutable value = 0
            if not (Int32.TryParse(this.Token(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, &value)) then failwith "invalid integer token"
            value
        member this.Float() =
            let mutable value = 0.0
            if not (Double.TryParse(this.Token(), NumberStyles.Float, CultureInfo.InvariantCulture, &value)) then failwith "invalid number token"
            value
        member this.Bool() =
            match this.Token() with
            | "0" -> false
            | "1" -> true
            | _ -> failwith "invalid boolean token"
        member this.Option(read: unit -> 'a) = if this.Bool() then Some(read ()) else None
        member this.List(read: unit -> 'a) =
            let count = this.Int()
            if count < 0 || count > defaultLimits.MaxPathSegments + defaultLimits.MaxNodes + defaultLimits.MaxDefinitions then
                failwith "collection length exceeds decoder bound"
            List.init count (fun _ -> read ())

    let private writeColor (writer: TokenWriter) (value: Color) =
        writer.Int(int value.Red); writer.Int(int value.Green); writer.Int(int value.Blue); writer.Int(int value.Alpha)

    let private readColor (reader: TokenReader) =
        let channel () =
            let value = reader.Int()
            if value < 0 || value > 255 then failwith "color channel is outside byte range"
            byte value
        { Red = channel (); Green = channel (); Blue = channel (); Alpha = channel () }

    let private writePoint (writer: TokenWriter) (value: Point) = writer.Float value.X; writer.Float value.Y
    let private readPoint (reader: TokenReader) = { X = reader.Float(); Y = reader.Float() }
    let private writeRect (writer: TokenWriter) (value: Rect) = writer.Float value.X; writer.Float value.Y; writer.Float value.Width; writer.Float value.Height
    let private readRect (reader: TokenReader) = { X = reader.Float(); Y = reader.Float(); Width = reader.Float(); Height = reader.Float() }
    let private writeAffine (writer: TokenWriter) (value: SvgAffine) =
        writer.Float value.A; writer.Float value.B; writer.Float value.C; writer.Float value.D; writer.Float value.E; writer.Float value.F
    let private readAffine (reader: TokenReader) =
        { A = reader.Float(); B = reader.Float(); C = reader.Float(); D = reader.Float(); E = reader.Float(); F = reader.Float() }

    let private writeCap (writer: TokenWriter) = function StrokeCap.Butt -> writer.Token "butt" | StrokeCap.Round -> writer.Token "round" | StrokeCap.Square -> writer.Token "square"
    let private readCap (reader: TokenReader) = match reader.Token() with "butt" -> StrokeCap.Butt | "round" -> StrokeCap.Round | "square" -> StrokeCap.Square | _ -> failwith "unknown stroke cap"
    let private writeJoin (writer: TokenWriter) = function StrokeJoin.Miter -> writer.Token "miter" | StrokeJoin.RoundJoin -> writer.Token "round" | StrokeJoin.Bevel -> writer.Token "bevel"
    let private readJoin (reader: TokenReader) = match reader.Token() with "miter" -> StrokeJoin.Miter | "round" -> StrokeJoin.RoundJoin | "bevel" -> StrokeJoin.Bevel | _ -> failwith "unknown stroke join"

    let private writeStroke (writer: TokenWriter) (value: Stroke) =
        writer.Float value.Width; writeCap writer value.Cap; writeJoin writer value.Join; writer.Float value.Miter
    let private readStroke (reader: TokenReader) =
        { Width = reader.Float(); Cap = readCap reader; Join = readJoin reader; Miter = reader.Float() }

    let private writeShader (writer: TokenWriter) = function
        | Shader.SolidColor color -> writer.Token "solid"; writeColor writer color
        | Shader.LinearGradient(startPoint, finish, colors) -> writer.Token "linear"; writePoint writer startPoint; writePoint writer finish; writer.List(writeColor writer, colors)
        | Shader.RadialGradient(center, radius, colors) -> writer.Token "radial"; writePoint writer center; writer.Float radius; writer.List(writeColor writer, colors)
        | Shader.SweepGradient _ -> failwith "unsupported shader reached serializer"
    let private readShader (reader: TokenReader) =
        match reader.Token() with
        | "solid" -> Shader.SolidColor(readColor reader)
        | "linear" -> Shader.LinearGradient(readPoint reader, readPoint reader, reader.List(fun () -> readColor reader))
        | "radial" -> Shader.RadialGradient(readPoint reader, reader.Float(), reader.List(fun () -> readColor reader))
        | _ -> failwith "unknown shader tag"

    let private writePaint (writer: TokenWriter) (value: Paint) =
        writer.Option(writeColor writer, value.Fill)
        writer.Option(writeStroke writer, value.Stroke)
        writer.Float value.Opacity
        writer.Bool value.Antialias
        writer.Token "src-over"
        writer.Option(writeShader writer, value.Shader)
        match value.PathEffect with
        | PathEffect.NoPathEffect -> writer.Token "none"
        | PathEffect.Dash(intervals, phase) -> writer.Token "dash"; writer.List(writer.Float, intervals); writer.Float phase
        | _ -> failwith "unsupported path effect reached serializer"
    let private readPaint (reader: TokenReader) =
        let fill = reader.Option(fun () -> readColor reader)
        let stroke = reader.Option(fun () -> readStroke reader)
        let opacity = reader.Float()
        let antialias = reader.Bool()
        if reader.Token() <> "src-over" then failwith "unsupported blend mode"
        let shader = reader.Option(fun () -> readShader reader)
        let pathEffect =
            match reader.Token() with
            | "none" -> PathEffect.NoPathEffect
            | "dash" -> PathEffect.Dash(reader.List(reader.Float), reader.Float())
            | _ -> failwith "unknown path effect"
        { Fill = fill; Stroke = stroke; Opacity = opacity; Antialias = antialias; BlendMode = BlendMode.SrcOver
          Shader = shader; ColorFilter = ColorFilter.NoColorFilter; MaskFilter = MaskFilter.NoMaskFilter
          ImageFilter = ImageFilter.NoImageFilter; PathEffect = pathEffect }

    let private writeFillType (writer: TokenWriter) = function PathFillType.Winding -> writer.Token "nonzero" | PathFillType.EvenOdd -> writer.Token "evenodd"
    let private readFillType (reader: TokenReader) = match reader.Token() with "nonzero" -> PathFillType.Winding | "evenodd" -> PathFillType.EvenOdd | _ -> failwith "unknown fill rule"

    let private writePath (writer: TokenWriter) (value: PathSpec) =
        writeFillType writer value.FillType
        writer.List((fun command ->
            match command with
            | PathCommand.MoveTo point -> writer.Token "M"; writePoint writer point
            | PathCommand.LineTo point -> writer.Token "L"; writePoint writer point
            | PathCommand.QuadTo(control, point) -> writer.Token "Q"; writePoint writer control; writePoint writer point
            | PathCommand.CubicTo(first, second, point) -> writer.Token "C"; writePoint writer first; writePoint writer second; writePoint writer point
            | PathCommand.ArcTo(bounds, startAngle, sweepAngle) -> writer.Token "A"; writeRect writer bounds; writer.Float startAngle; writer.Float sweepAngle
            | PathCommand.Close -> writer.Token "Z"), value.Commands)
    let private readPath (reader: TokenReader) =
        let fillType = readFillType reader
        let commands = reader.List(fun () ->
            match reader.Token() with
            | "M" -> PathCommand.MoveTo(readPoint reader)
            | "L" -> PathCommand.LineTo(readPoint reader)
            | "Q" -> PathCommand.QuadTo(readPoint reader, readPoint reader)
            | "C" -> PathCommand.CubicTo(readPoint reader, readPoint reader, readPoint reader)
            | "A" -> PathCommand.ArcTo(readRect reader, reader.Float(), reader.Float())
            | "Z" -> PathCommand.Close
            | _ -> failwith "unknown path command")
        { Commands = commands; FillType = fillType }

    let rec private writeScene (writer: TokenWriter) (scene: Scene) = writer.List(writeNode writer, scene.Nodes)
    and private writeNode (writer: TokenWriter) = function
        | SceneNode.Empty -> writer.Token "empty"
        | SceneNode.Group scenes -> writer.Token "group"; writer.List(writeScene writer, scenes)
        | SceneNode.Rectangle((x, y, width, height), color) -> writer.Token "rect"; writeRect writer { X = x; Y = y; Width = width; Height = height }; writeColor writer color
        | SceneNode.PaintedRectangle(bounds, paint) -> writer.Token "painted-rect"; writeRect writer bounds; writePaint writer paint
        | SceneNode.Circle(center, radius, color) -> writer.Token "circle"; writePoint writer center; writer.Float radius; writeColor writer color
        | SceneNode.FilledEllipse(bounds, color) -> writer.Token "filled-ellipse"; writeRect writer bounds; writeColor writer color
        | SceneNode.Ellipse(bounds, paint) -> writer.Token "ellipse"; writeRect writer bounds; writePaint writer paint
        | SceneNode.Line(first, second, paint) -> writer.Token "line"; writePoint writer first; writePoint writer second; writePaint writer paint
        | SceneNode.Path(path, paint) -> writer.Token "path"; writePath writer path; writePaint writer paint
        | SceneNode.Arc(bounds, startAngle, sweepAngle, paint) -> writer.Token "arc"; writeRect writer bounds; writer.Float startAngle; writer.Float sweepAngle; writePaint writer paint
        | SceneNode.Text((x, y), text, color) -> writer.Token "text"; writePoint writer { X = x; Y = y }; writer.Token text; writeColor writer color
        | SceneNode.TextRun run ->
            writer.Token "text-run"; writer.Token run.Text; writePoint writer run.Position
            writer.Option(writer.Token, run.Font.Family); writer.Float run.Font.Size; writer.Option(writer.Int, run.Font.Weight); writePaint writer run.Paint
        | SceneNode.ClipNode(clip, child) ->
            writer.Token "clip"
            match clip with Clip.RectClip bounds -> writer.Token "rect"; writeRect writer bounds | Clip.PathClip path -> writer.Token "path"; writePath writer path
            writeScene writer child
        | SceneNode.ColorSpaceNode(ColorSpace.Srgb, child) -> writer.Token "srgb"; writeScene writer child
        | SceneNode.Translate((x, y), child) -> writer.Token "translate"; writer.Float x; writer.Float y; writeScene writer child
        | SceneNode.SizedText((x, y), text, size, color) -> writer.Token "sized-text"; writePoint writer { X = x; Y = y }; writer.Token text; writer.Float size; writeColor writer color
        | SceneNode.CachedSubtree cached -> writer.Token "cache"; writer.Token(string cached.CacheId); writer.Token(string cached.Fingerprint); writeScene writer cached.Scene
        | _ -> failwith "unsupported Scene node reached serializer"

    let rec private readScene (reader: TokenReader) = { Nodes = reader.List(fun () -> readNode reader) }
    and private readNode (reader: TokenReader) =
        match reader.Token() with
        | "empty" -> SceneNode.Empty
        | "group" -> SceneNode.Group(reader.List(fun () -> readScene reader))
        | "rect" -> let value = readRect reader in SceneNode.Rectangle((value.X, value.Y, value.Width, value.Height), readColor reader)
        | "painted-rect" -> SceneNode.PaintedRectangle(readRect reader, readPaint reader)
        | "circle" -> SceneNode.Circle(readPoint reader, reader.Float(), readColor reader)
        | "filled-ellipse" -> SceneNode.FilledEllipse(readRect reader, readColor reader)
        | "ellipse" -> SceneNode.Ellipse(readRect reader, readPaint reader)
        | "line" -> SceneNode.Line(readPoint reader, readPoint reader, readPaint reader)
        | "path" -> SceneNode.Path(readPath reader, readPaint reader)
        | "arc" -> SceneNode.Arc(readRect reader, reader.Float(), reader.Float(), readPaint reader)
        | "text" -> let point = readPoint reader in SceneNode.Text((point.X, point.Y), reader.Token(), readColor reader)
        | "text-run" ->
            let text = reader.Token()
            let position = readPoint reader
            let font = { Family = reader.Option(reader.Token); Size = reader.Float(); Weight = reader.Option(reader.Int) }
            SceneNode.TextRun { Text = text; Position = position; Font = font; Paint = readPaint reader }
        | "clip" ->
            let clip = match reader.Token() with "rect" -> Clip.RectClip(readRect reader) | "path" -> Clip.PathClip(readPath reader) | _ -> failwith "unknown clip"
            SceneNode.ClipNode(clip, readScene reader)
        | "srgb" -> SceneNode.ColorSpaceNode(ColorSpace.Srgb, readScene reader)
        | "translate" -> SceneNode.Translate((reader.Float(), reader.Float()), readScene reader)
        | "sized-text" -> let point = readPoint reader in SceneNode.SizedText((point.X, point.Y), reader.Token(), reader.Float(), readColor reader)
        | "cache" -> SceneNode.CachedSubtree { CacheId = UInt64.Parse(reader.Token(), CultureInfo.InvariantCulture); Fingerprint = UInt64.Parse(reader.Token(), CultureInfo.InvariantCulture); Scene = readScene reader }
        | _ -> failwith "unknown Scene node"

    let private writePaintSource (writer: TokenWriter) = function
        | SvgPaintSource.Solid color -> writer.Token "solid"; writeColor writer color
        | SvgPaintSource.Definition id -> writer.Token "definition"; writer.Token id
    let private readPaintSource (reader: TokenReader) = match reader.Token() with "solid" -> SvgPaintSource.Solid(readColor reader) | "definition" -> SvgPaintSource.Definition(reader.Token()) | _ -> failwith "unknown paint source"
    let private writeUnits (writer: TokenWriter) = function SvgCoordinateUnits.UserSpaceOnUse -> writer.Token "user" | SvgCoordinateUnits.ObjectBoundingBox -> writer.Token "bbox"
    let private readUnits (reader: TokenReader) = match reader.Token() with "user" -> SvgCoordinateUnits.UserSpaceOnUse | "bbox" -> SvgCoordinateUnits.ObjectBoundingBox | _ -> failwith "unknown coordinate units"

    let private writePresentation (writer: TokenWriter) (value: SvgPresentation) =
        writer.Option(writePaintSource writer, value.FillSource)
        writer.Option((fun (stroke: SvgStrokePresentation) ->
            writePaintSource writer stroke.Source; writer.Float stroke.Width; writeCap writer stroke.Cap; writeJoin writer stroke.Join
            writer.Float stroke.Miter; writer.List(writer.Float, stroke.Dash); writer.Float stroke.DashOffset), value.StrokeStyle)
        writer.Float value.OverallOpacity; writeFillType writer value.FillRule
    let private readPresentation (reader: TokenReader) =
        let fill = reader.Option(fun () -> readPaintSource reader)
        let stroke = reader.Option(fun () ->
            { Source = readPaintSource reader; Width = reader.Float(); Cap = readCap reader; Join = readJoin reader
              Miter = reader.Float(); Dash = reader.List(reader.Float); DashOffset = reader.Float() })
        { FillSource = fill; StrokeStyle = stroke; OverallOpacity = reader.Float(); FillRule = readFillType reader }

    let rec private writeElement (writer: TokenWriter) (value: SvgElement) =
        writer.Token value.Id; writer.Option(writer.Token, value.SemanticId); writer.Bool value.Visible; writeAffine writer value.Transform
        writer.Option(writer.Token, value.ClipId); writer.Option(writer.Token, value.MaskId); writer.Option(writePresentation writer, value.Presentation)
        match value.Content with
        | SvgElementContent.SceneLeaf scene -> writer.Token "scene"; writeScene writer scene
        | SvgElementContent.Group children -> writer.Token "group"; writer.List(writeElement writer, children)
        | SvgElementContent.SymbolInstance(id, viewport) -> writer.Token "symbol"; writer.Token id; writer.Option(writeRect writer, viewport)
    let rec private readElement (reader: TokenReader) =
        let id = reader.Token()
        let semanticId = reader.Option(reader.Token)
        let visible = reader.Bool()
        let transform = readAffine reader
        let clipId = reader.Option(reader.Token)
        let maskId = reader.Option(reader.Token)
        let presentation = reader.Option(fun () -> readPresentation reader)
        let content =
            match reader.Token() with
            | "scene" -> SvgElementContent.SceneLeaf(readScene reader)
            | "group" -> SvgElementContent.Group(reader.List(fun () -> readElement reader))
            | "symbol" -> SvgElementContent.SymbolInstance(reader.Token(), reader.Option(fun () -> readRect reader))
            | _ -> failwith "unknown element content"
        { Id = id; SemanticId = semanticId; Visible = visible; Transform = transform; ClipId = clipId; MaskId = maskId; Presentation = presentation; Content = content }

    let private writeDefinition (writer: TokenWriter) (value: SvgDefinition) =
        writer.Token value.Id
        match value.Content with
        | SvgDefinitionContent.Symbol(viewBox, children) -> writer.Token "symbol"; writer.Option(writeRect writer, viewBox); writer.List(writeElement writer, children)
        | SvgDefinitionContent.Clip(units, shapes) ->
            writer.Token "clip"; writeUnits writer units
            writer.List((fun shape -> match shape with SvgClipShape.Rectangle rect -> writer.Token "rect"; writeRect writer rect | SvgClipShape.Path path -> writer.Token "path"; writePath writer path | SvgClipShape.Intersection ids -> writer.Token "intersection"; writer.List(writer.Token, ids)), shapes)
        | SvgDefinitionContent.Mask(units, region, kind, children) ->
            writer.Token "mask"; writeUnits writer units; writeRect writer region; writer.Token(match kind with SvgMaskKind.Alpha -> "alpha" | SvgMaskKind.Luminance -> "luminance"); writer.List(writeElement writer, children)
        | SvgDefinitionContent.Gradient gradient ->
            writer.Token "gradient"
            match gradient.Geometry with SvgGradientGeometry.Linear(first, second) -> writer.Token "linear"; writePoint writer first; writePoint writer second | SvgGradientGeometry.Radial(center, radius, focal) -> writer.Token "radial"; writePoint writer center; writer.Float radius; writer.Option(writePoint writer, focal)
            writeUnits writer gradient.Units; writeAffine writer gradient.Transform
            writer.Token(match gradient.Spread with SvgSpreadMethod.Pad -> "pad" | SvgSpreadMethod.Repeat -> "repeat" | SvgSpreadMethod.Reflect -> "reflect")
            writer.List((fun stop -> writer.Float stop.Offset; writeColor writer stop.Color; writer.Float stop.StopOpacity), gradient.Stops)
            writer.Option(writer.Token, gradient.InheritFrom)
        | SvgDefinitionContent.Font font -> writer.Token "font"; writer.Token font.Family; writer.Token font.Source; writer.Token font.Sha256; writer.Token font.License

    let private readDefinition (reader: TokenReader) =
        let id = reader.Token()
        let content =
            match reader.Token() with
            | "symbol" -> SvgDefinitionContent.Symbol(reader.Option(fun () -> readRect reader), reader.List(fun () -> readElement reader))
            | "clip" ->
                let units = readUnits reader
                let shapes = reader.List(fun () -> match reader.Token() with "rect" -> SvgClipShape.Rectangle(readRect reader) | "path" -> SvgClipShape.Path(readPath reader) | "intersection" -> SvgClipShape.Intersection(reader.List(reader.Token)) | _ -> failwith "unknown clip shape")
                SvgDefinitionContent.Clip(units, shapes)
            | "mask" ->
                let units = readUnits reader
                let region = readRect reader
                let kind = match reader.Token() with "alpha" -> SvgMaskKind.Alpha | "luminance" -> SvgMaskKind.Luminance | _ -> failwith "unknown mask kind"
                SvgDefinitionContent.Mask(units, region, kind, reader.List(fun () -> readElement reader))
            | "gradient" ->
                let geometry = match reader.Token() with "linear" -> SvgGradientGeometry.Linear(readPoint reader, readPoint reader) | "radial" -> SvgGradientGeometry.Radial(readPoint reader, reader.Float(), reader.Option(fun () -> readPoint reader)) | _ -> failwith "unknown gradient geometry"
                let units = readUnits reader
                let transform = readAffine reader
                let spread = match reader.Token() with "pad" -> SvgSpreadMethod.Pad | "repeat" -> SvgSpreadMethod.Repeat | "reflect" -> SvgSpreadMethod.Reflect | _ -> failwith "unknown spread method"
                let stops = reader.List(fun () -> { Offset = reader.Float(); Color = readColor reader; StopOpacity = reader.Float() })
                SvgDefinitionContent.Gradient { Geometry = geometry; Units = units; Transform = transform; Spread = spread; Stops = stops; InheritFrom = reader.Option(reader.Token) }
            | "font" -> SvgDefinitionContent.Font { Family = reader.Token(); Source = reader.Token(); Sha256 = reader.Token(); License = reader.Token() }
            | _ -> failwith "unknown definition content"
        { Id = id; Content = content }

    let serialize (document: SvgDocument) =
        let issues = validate 0 defaultLimits document
        if not issues.IsEmpty then Error issues
        else
            try
                let writer = TokenWriter()
                writer.Token wireHeader; writer.Token document.Schema; writer.Token document.Id; writeRect writer document.ViewBox
                writer.List(writeDefinition writer, document.Definitions); writer.List(writeElement writer, document.Children)
                let serialized = writer.ToString()
                let byteCount =
                    let mutable count = 0
                    let mutable index = 0
                    while index < serialized.Length do
                        let code = int serialized[index]
                        if code <= 0x7f then count <- count + 1
                        elif code <= 0x7ff then count <- count + 2
                        elif code >= 0xd800 && code <= 0xdbff && index + 1 < serialized.Length && int serialized[index + 1] >= 0xdc00 && int serialized[index + 1] <= 0xdfff then count <- count + 4; index <- index + 1
                        else count <- count + 3
                        index <- index + 1
                    count
                let sizeIssues = validate byteCount defaultLimits document
                if sizeIssues.IsEmpty then Ok serialized else Error sizeIssues
            with error -> Error [ issue "unsupported-serialization" "/" error.Message ]

    let private utf8ByteCount (serialized: string) =
        let mutable count = 0
        let mutable index = 0
        while index < serialized.Length do
            let code = int serialized[index]
            if code <= 0x7f then count <- count + 1
            elif code <= 0x7ff then count <- count + 2
            elif code >= 0xd800 && code <= 0xdbff && index + 1 < serialized.Length && int serialized[index + 1] >= 0xdc00 && int serialized[index + 1] <= 0xdfff then count <- count + 4; index <- index + 1
            else count <- count + 3
            index <- index + 1
        count

    let deserialize (serialized: string) =
        if utf8ByteCount serialized > defaultLimits.MaxSerializedBytes then
            Error [ issue "document-byte-limit" "/" $"serialized document exceeds {defaultLimits.MaxSerializedBytes} bytes" ]
        else
            try
                let reader = TokenReader(serialized)
                if reader.Token() <> wireHeader then failwith "unknown typed document wire header"
                let document =
                    { Schema = reader.Token(); Id = reader.Token(); ViewBox = readRect reader
                      Definitions = reader.List(fun () -> readDefinition reader)
                      Children = reader.List(fun () -> readElement reader) }
                if not reader.AtEnd then failwith "trailing tokens after document"
                let issues = validate (utf8ByteCount serialized) defaultLimits document
                if issues.IsEmpty then Ok document else Error issues
            with error -> Error [ issue "invalid-serialization" "/" error.Message ]

    let private xmlEscape (value: string) =
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;")

    let private svgNumber (value: float) = canonicalNumber value
    let private svgColor (value: Color) = $"rgb({value.Red} {value.Green} {value.Blue})"
    let private svgOpacity (value: Color) = svgNumber(float value.Alpha / 255.0)
    let private svgMatrix (value: SvgAffine) =
        $"matrix({svgNumber value.A} {svgNumber value.B} {svgNumber value.C} {svgNumber value.D} {svgNumber value.E} {svgNumber value.F})"

    let private encodedId (mountNamespace: string) (documentId: string) (localId: string) =
        let encode (value: string) =
            value |> Seq.map (fun character -> (int character).ToString("x4", CultureInfo.InvariantCulture)) |> String.concat ""
        $"fsgg-{encode mountNamespace}-{encode documentId}-{encode localId}"

    let private appendAttribute (output: StringBuilder) (name: string) (value: string) =
        output.Append(' ').Append(name).Append("=\"").Append(xmlEscape value).Append('"') |> ignore

    let private pathData (path: PathSpec) =
        let output = StringBuilder()
        let mutable current: Point option = None
        let append (command: string) = output.Append(command).Append(' ') |> ignore
        let pointAt (bounds: Rect) angle =
            let radians = angle * Math.PI / 180.0
            { X = bounds.X + bounds.Width / 2.0 + bounds.Width / 2.0 * Math.Cos radians
              Y = bounds.Y + bounds.Height / 2.0 + bounds.Height / 2.0 * Math.Sin radians }
        let closeEnough (first: Point) (second: Point) = abs (first.X - second.X) <= 1e-9 && abs (first.Y - second.Y) <= 1e-9
        let appendArc (bounds: Rect) startAngle sweepAngle =
            let startPoint = pointAt bounds startAngle
            match current with
            | None -> append $"M {svgNumber startPoint.X} {svgNumber startPoint.Y}"
            | Some value when not (closeEnough value startPoint) -> append $"L {svgNumber startPoint.X} {svgNumber startPoint.Y}"
            | _ -> ()
            let pieces = max 1 (int (Math.Ceiling(abs sweepAngle / 180.0)))
            let pieceSweep = sweepAngle / float pieces
            for index in 1 .. pieces do
                let finish = pointAt bounds (startAngle + pieceSweep * float index)
                let largeArc = if abs pieceSweep > 180.0 then 1 else 0
                let sweep = if pieceSweep >= 0.0 then 1 else 0
                append $"A {svgNumber (abs bounds.Width / 2.0)} {svgNumber (abs bounds.Height / 2.0)} 0 {largeArc} {sweep} {svgNumber finish.X} {svgNumber finish.Y}"
                current <- Some finish
        for command in path.Commands do
            match command with
            | PathCommand.MoveTo point -> append $"M {svgNumber point.X} {svgNumber point.Y}"; current <- Some point
            | PathCommand.LineTo point -> append $"L {svgNumber point.X} {svgNumber point.Y}"; current <- Some point
            | PathCommand.QuadTo(control, point) -> append $"Q {svgNumber control.X} {svgNumber control.Y} {svgNumber point.X} {svgNumber point.Y}"; current <- Some point
            | PathCommand.CubicTo(first, second, point) -> append $"C {svgNumber first.X} {svgNumber first.Y} {svgNumber second.X} {svgNumber second.Y} {svgNumber point.X} {svgNumber point.Y}"; current <- Some point
            | PathCommand.ArcTo(bounds, startAngle, sweepAngle) -> appendArc bounds startAngle sweepAngle
            | PathCommand.Close -> append "Z"
        output.ToString().TrimEnd()

    let exportSvg (mountNamespace: string) (document: SvgDocument) =
        let namespaceIssues =
            if String.IsNullOrWhiteSpace mountNamespace then [ issue "blank-mount-namespace" "/mountNamespace" "mount namespace must not be blank" ] else []
        let issues = namespaceIssues @ validate 0 defaultLimits document
        if not issues.IsEmpty then Error issues
        else
            try
                let definitions = StringBuilder()
                let body = StringBuilder()
                let mutable generatedGradient = 0
                let id local = encodedId mountNamespace document.Id local
                let url local = $"url(#{id local})"
                let beginTag (output: StringBuilder) (name: string) = output.Append('<').Append(name) |> ignore
                let endOpen (output: StringBuilder) = output.Append('>') |> ignore
                let closeTag (output: StringBuilder) (name: string) = output.Append("</").Append(name).Append('>') |> ignore
                let emptyTag (output: StringBuilder) = output.Append("/>") |> ignore
                let units = function SvgCoordinateUnits.UserSpaceOnUse -> "userSpaceOnUse" | SvgCoordinateUnits.ObjectBoundingBox -> "objectBoundingBox"
                let spread = function SvgSpreadMethod.Pad -> "pad" | SvgSpreadMethod.Repeat -> "repeat" | SvgSpreadMethod.Reflect -> "reflect"

                let appendPaintSource output attribute opacityAttribute = function
                    | SvgPaintSource.Solid color -> appendAttribute output attribute (svgColor color); appendAttribute output opacityAttribute (svgOpacity color)
                    | SvgPaintSource.Definition reference -> appendAttribute output attribute (url reference)

                let appendPresentation output (presentation: SvgPresentation) =
                    match presentation.FillSource with
                    | Some source -> appendPaintSource output "fill" "fill-opacity" source
                    | None -> appendAttribute output "fill" "none"
                    match presentation.StrokeStyle with
                    | Some stroke ->
                        appendPaintSource output "stroke" "stroke-opacity" stroke.Source
                        appendAttribute output "stroke-width" (svgNumber stroke.Width)
                        appendAttribute output "stroke-linecap" (match stroke.Cap with StrokeCap.Butt -> "butt" | StrokeCap.Round -> "round" | StrokeCap.Square -> "square")
                        appendAttribute output "stroke-linejoin" (match stroke.Join with StrokeJoin.Miter -> "miter" | StrokeJoin.RoundJoin -> "round" | StrokeJoin.Bevel -> "bevel")
                        appendAttribute output "stroke-miterlimit" (svgNumber stroke.Miter)
                        if not stroke.Dash.IsEmpty then appendAttribute output "stroke-dasharray" (stroke.Dash |> List.map svgNumber |> String.concat " ")
                        if stroke.DashOffset <> 0.0 then appendAttribute output "stroke-dashoffset" (svgNumber stroke.DashOffset)
                    | None -> appendAttribute output "stroke" "none"
                    appendAttribute output "opacity" (svgNumber presentation.OverallOpacity)
                    appendAttribute output "fill-rule" (match presentation.FillRule with PathFillType.Winding -> "nonzero" | PathFillType.EvenOdd -> "evenodd")

                let appendStops output stops =
                    stops |> List.iter (fun stop ->
                        beginTag output "stop"
                        appendAttribute output "offset" (svgNumber stop.Offset)
                        appendAttribute output "stop-color" (svgColor stop.Color)
                        appendAttribute output "stop-opacity" (svgNumber (stop.StopOpacity * float stop.Color.Alpha / 255.0))
                        emptyTag output)

                let appendTextLayout output =
                    // Existing Scene text has start/auto semantics. Emit those defaults explicitly so
                    // standalone export does not inherit a host page's text layout declarations.
                    appendAttribute output "text-anchor" "start"
                    appendAttribute output "direction" "auto"

                let appendGeneratedGradient (paint: Paint) =
                    match paint.Shader with
                    | Some(Shader.LinearGradient(first, second, colors)) ->
                        generatedGradient <- generatedGradient + 1
                        let local = $"generated-gradient-{generatedGradient}"
                        beginTag definitions "linearGradient"; appendAttribute definitions "id" (id local); appendAttribute definitions "gradientUnits" "userSpaceOnUse"; appendAttribute definitions "color-interpolation" "sRGB"
                        appendAttribute definitions "x1" (svgNumber first.X); appendAttribute definitions "y1" (svgNumber first.Y); appendAttribute definitions "x2" (svgNumber second.X); appendAttribute definitions "y2" (svgNumber second.Y); endOpen definitions
                        let count = colors.Length
                        colors |> List.iteri (fun index color ->
                            let offset = if count <= 1 then 0.0 else float index / float (count - 1)
                            appendStops definitions [ { Offset = offset; Color = color; StopOpacity = 1.0 } ])
                        closeTag definitions "linearGradient"
                        Some local
                    | Some(Shader.RadialGradient(center, radius, colors)) ->
                        generatedGradient <- generatedGradient + 1
                        let local = $"generated-gradient-{generatedGradient}"
                        beginTag definitions "radialGradient"; appendAttribute definitions "id" (id local); appendAttribute definitions "gradientUnits" "userSpaceOnUse"; appendAttribute definitions "color-interpolation" "sRGB"
                        appendAttribute definitions "cx" (svgNumber center.X); appendAttribute definitions "cy" (svgNumber center.Y); appendAttribute definitions "r" (svgNumber radius); endOpen definitions
                        let count = colors.Length
                        colors |> List.iteri (fun index color ->
                            let offset = if count <= 1 then 0.0 else float index / float (count - 1)
                            appendStops definitions [ { Offset = offset; Color = color; StopOpacity = 1.0 } ])
                        closeTag definitions "radialGradient"
                        Some local
                    | _ -> None

                let appendPaint output isLine (paint: Paint) =
                    let generated = appendGeneratedGradient paint
                    let source =
                        match generated, paint.Shader, paint.Fill with
                        | Some gradient, _, _ -> Choice2Of2 gradient
                        | None, Some(Shader.SolidColor color), _ -> Choice1Of2 color
                        | None, _, Some color -> Choice1Of2 color
                        | _ -> Choice1Of2 { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 0uy }
                    let setSource attribute opacityAttribute =
                        match source with
                        | Choice1Of2 color -> appendAttribute output attribute (svgColor color); appendAttribute output opacityAttribute (svgOpacity color)
                        | Choice2Of2 gradient -> appendAttribute output attribute (url gradient)
                    match paint.Stroke with
                    | Some stroke ->
                        appendAttribute output "fill" "none"; setSource "stroke" "stroke-opacity"
                        appendAttribute output "stroke-width" (svgNumber stroke.Width)
                        appendAttribute output "stroke-linecap" (match stroke.Cap with StrokeCap.Butt -> "butt" | StrokeCap.Round -> "round" | StrokeCap.Square -> "square")
                        appendAttribute output "stroke-linejoin" (match stroke.Join with StrokeJoin.Miter -> "miter" | StrokeJoin.RoundJoin -> "round" | StrokeJoin.Bevel -> "bevel")
                        appendAttribute output "stroke-miterlimit" (svgNumber stroke.Miter)
                    | None when isLine -> appendAttribute output "fill" "none"; setSource "stroke" "stroke-opacity"
                    | None -> setSource "fill" "fill-opacity"; appendAttribute output "stroke" "none"
                    match paint.PathEffect with
                    | PathEffect.Dash(intervals, phase) -> appendAttribute output "stroke-dasharray" (intervals |> List.map svgNumber |> String.concat " "); appendAttribute output "stroke-dashoffset" (svgNumber phase)
                    | _ -> ()
                    appendAttribute output "opacity" (svgNumber paint.Opacity)

                let rec appendScene output prefix presentation (scene: Scene) =
                    scene.Nodes |> List.iteri (fun index node -> appendNode output $"{prefix}-{index}" presentation node)
                and appendNode output prefix presentation node =
                    let paintAttrs isLine paint = match presentation with Some value -> appendPresentation output value | None -> appendPaint output isLine paint
                    match node with
                    | SceneNode.Empty -> ()
                    | SceneNode.Group scenes -> beginTag output "g"; appendAttribute output "data-fsgg-node" prefix; endOpen output; scenes |> List.iteri (fun index scene -> appendScene output $"{prefix}-g{index}" presentation scene); closeTag output "g"
                    | SceneNode.Rectangle((x, y, width, height), color) -> beginTag output "rect"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x" (svgNumber x); appendAttribute output "y" (svgNumber y); appendAttribute output "width" (svgNumber width); appendAttribute output "height" (svgNumber height); (match presentation with Some value -> appendPresentation output value | None -> appendPaintSource output "fill" "fill-opacity" (SvgPaintSource.Solid color)); emptyTag output
                    | SceneNode.PaintedRectangle(rect, paint) -> beginTag output "rect"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x" (svgNumber rect.X); appendAttribute output "y" (svgNumber rect.Y); appendAttribute output "width" (svgNumber rect.Width); appendAttribute output "height" (svgNumber rect.Height); paintAttrs false paint; emptyTag output
                    | SceneNode.Circle(center, radius, color) -> beginTag output "circle"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "cx" (svgNumber center.X); appendAttribute output "cy" (svgNumber center.Y); appendAttribute output "r" (svgNumber radius); (match presentation with Some value -> appendPresentation output value | None -> appendPaintSource output "fill" "fill-opacity" (SvgPaintSource.Solid color)); emptyTag output
                    | SceneNode.FilledEllipse(rect, color) -> beginTag output "ellipse"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "cx" (svgNumber (rect.X + rect.Width / 2.0)); appendAttribute output "cy" (svgNumber (rect.Y + rect.Height / 2.0)); appendAttribute output "rx" (svgNumber (abs rect.Width / 2.0)); appendAttribute output "ry" (svgNumber (abs rect.Height / 2.0)); (match presentation with Some value -> appendPresentation output value | None -> appendPaintSource output "fill" "fill-opacity" (SvgPaintSource.Solid color)); emptyTag output
                    | SceneNode.Ellipse(rect, paint) -> beginTag output "ellipse"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "cx" (svgNumber (rect.X + rect.Width / 2.0)); appendAttribute output "cy" (svgNumber (rect.Y + rect.Height / 2.0)); appendAttribute output "rx" (svgNumber (abs rect.Width / 2.0)); appendAttribute output "ry" (svgNumber (abs rect.Height / 2.0)); paintAttrs false paint; emptyTag output
                    | SceneNode.Line(first, second, paint) -> beginTag output "line"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x1" (svgNumber first.X); appendAttribute output "y1" (svgNumber first.Y); appendAttribute output "x2" (svgNumber second.X); appendAttribute output "y2" (svgNumber second.Y); paintAttrs true paint; emptyTag output
                    | SceneNode.Path(path, paint) -> beginTag output "path"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "d" (pathData path); appendAttribute output "fill-rule" (match path.FillType with PathFillType.Winding -> "nonzero" | PathFillType.EvenOdd -> "evenodd"); paintAttrs false paint; emptyTag output
                    | SceneNode.Arc(bounds, startAngle, sweepAngle, paint) -> let path = { Commands = [ PathCommand.ArcTo(bounds, startAngle, sweepAngle) ]; FillType = PathFillType.Winding } in beginTag output "path"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "d" (pathData path); paintAttrs false paint; emptyTag output
                    | SceneNode.Text((x, y), text, color) -> beginTag output "text"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x" (svgNumber x); appendAttribute output "y" (svgNumber y); appendTextLayout output; (match presentation with Some value -> appendPresentation output value | None -> appendPaintSource output "fill" "fill-opacity" (SvgPaintSource.Solid color)); endOpen output; output.Append(xmlEscape text) |> ignore; closeTag output "text"
                    | SceneNode.TextRun run -> beginTag output "text"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x" (svgNumber run.Position.X); appendAttribute output "y" (svgNumber run.Position.Y); appendAttribute output "font-size" (svgNumber run.Font.Size); appendTextLayout output; run.Font.Family |> Option.iter (appendAttribute output "font-family"); run.Font.Weight |> Option.iter (string >> appendAttribute output "font-weight"); paintAttrs false run.Paint; endOpen output; output.Append(xmlEscape run.Text) |> ignore; closeTag output "text"
                    | SceneNode.ClipNode(clip, child) ->
                        let clipLocal = $"generated-clip-{prefix}"
                        beginTag definitions "clipPath"; appendAttribute definitions "id" (id clipLocal); appendAttribute definitions "clipPathUnits" "userSpaceOnUse"; endOpen definitions
                        match clip with Clip.RectClip rect -> beginTag definitions "rect"; appendAttribute definitions "x" (svgNumber rect.X); appendAttribute definitions "y" (svgNumber rect.Y); appendAttribute definitions "width" (svgNumber rect.Width); appendAttribute definitions "height" (svgNumber rect.Height); emptyTag definitions | Clip.PathClip path -> beginTag definitions "path"; appendAttribute definitions "d" (pathData path); appendAttribute definitions "fill-rule" (match path.FillType with PathFillType.Winding -> "nonzero" | PathFillType.EvenOdd -> "evenodd"); emptyTag definitions
                        closeTag definitions "clipPath"; beginTag output "g"; appendAttribute output "clip-path" (url clipLocal); endOpen output; appendScene output $"{prefix}-clip" presentation child; closeTag output "g"
                    | SceneNode.ColorSpaceNode(ColorSpace.Srgb, child) -> appendScene output $"{prefix}-srgb" presentation child
                    | SceneNode.Translate((x, y), child) -> beginTag output "g"; appendAttribute output "transform" $"translate({svgNumber x} {svgNumber y})"; endOpen output; appendScene output $"{prefix}-translate" presentation child; closeTag output "g"
                    | SceneNode.SizedText((x, y), text, size, color) -> beginTag output "text"; appendAttribute output "data-fsgg-node" prefix; appendAttribute output "x" (svgNumber x); appendAttribute output "y" (svgNumber y); appendAttribute output "font-size" (svgNumber size); appendTextLayout output; (match presentation with Some value -> appendPresentation output value | None -> appendPaintSource output "fill" "fill-opacity" (SvgPaintSource.Solid color)); endOpen output; output.Append(xmlEscape text) |> ignore; closeTag output "text"
                    | SceneNode.CachedSubtree cached -> appendScene output $"{prefix}-cache" presentation cached.Scene
                    | _ -> failwith "unsupported Scene node reached SVG export"

                let rec appendElement output (element: SvgElement) =
                    beginTag output "g"; appendAttribute output "id" (id element.Id); appendAttribute output "data-fsgg-element-id" element.Id
                    element.SemanticId |> Option.iter (appendAttribute output "data-fsgg-semantic-id")
                    appendAttribute output "transform" (svgMatrix element.Transform)
                    if not element.Visible then appendAttribute output "display" "none"
                    element.ClipId |> Option.iter (url >> appendAttribute output "clip-path")
                    element.MaskId |> Option.iter (url >> appendAttribute output "mask")
                    element.Presentation |> Option.iter (appendPresentation output)
                    endOpen output
                    match element.Content with
                    | SvgElementContent.SceneLeaf scene -> appendScene output element.Id element.Presentation scene
                    | SvgElementContent.Group children -> children |> List.iter (appendElement output)
                    | SvgElementContent.SymbolInstance(reference, viewport) ->
                        beginTag output "use"; appendAttribute output "href" $"#{id reference}"
                        viewport |> Option.iter (fun rect -> appendAttribute output "x" (svgNumber rect.X); appendAttribute output "y" (svgNumber rect.Y); appendAttribute output "width" (svgNumber rect.Width); appendAttribute output "height" (svgNumber rect.Height))
                        emptyTag output
                    closeTag output "g"

                let appendExplicitDefinition (definition: SvgDefinition) =
                    match definition.Content with
                    | SvgDefinitionContent.Symbol(viewBox, children) -> beginTag definitions "symbol"; appendAttribute definitions "id" (id definition.Id); viewBox |> Option.iter (fun rect -> appendAttribute definitions "viewBox" $"{svgNumber rect.X} {svgNumber rect.Y} {svgNumber rect.Width} {svgNumber rect.Height}"); endOpen definitions; children |> List.iter (appendElement definitions); closeTag definitions "symbol"
                    | SvgDefinitionContent.Clip(coordinateUnits, shapes) ->
                        beginTag definitions "clipPath"; appendAttribute definitions "id" (id definition.Id); appendAttribute definitions "clipPathUnits" (units coordinateUnits); endOpen definitions
                        shapes |> List.iter (function SvgClipShape.Rectangle rect -> beginTag definitions "rect"; appendAttribute definitions "x" (svgNumber rect.X); appendAttribute definitions "y" (svgNumber rect.Y); appendAttribute definitions "width" (svgNumber rect.Width); appendAttribute definitions "height" (svgNumber rect.Height); emptyTag definitions | SvgClipShape.Path path -> beginTag definitions "path"; appendAttribute definitions "d" (pathData path); appendAttribute definitions "fill-rule" (match path.FillType with PathFillType.Winding -> "nonzero" | PathFillType.EvenOdd -> "evenodd"); emptyTag definitions | SvgClipShape.Intersection references -> let mutable opened = 0 in references |> List.iter (fun reference -> beginTag definitions "g"; appendAttribute definitions "clip-path" (url reference); endOpen definitions; opened <- opened + 1); beginTag definitions "rect"; appendAttribute definitions "x" (svgNumber document.ViewBox.X); appendAttribute definitions "y" (svgNumber document.ViewBox.Y); appendAttribute definitions "width" (svgNumber document.ViewBox.Width); appendAttribute definitions "height" (svgNumber document.ViewBox.Height); emptyTag definitions; for _ in 1 .. opened do closeTag definitions "g")
                        closeTag definitions "clipPath"
                    | SvgDefinitionContent.Mask(coordinateUnits, region, kind, children) -> beginTag definitions "mask"; appendAttribute definitions "id" (id definition.Id); appendAttribute definitions "maskUnits" (units coordinateUnits); appendAttribute definitions "x" (svgNumber region.X); appendAttribute definitions "y" (svgNumber region.Y); appendAttribute definitions "width" (svgNumber region.Width); appendAttribute definitions "height" (svgNumber region.Height); appendAttribute definitions "style" (match kind with SvgMaskKind.Alpha -> "mask-type:alpha" | SvgMaskKind.Luminance -> "mask-type:luminance"); endOpen definitions; children |> List.iter (appendElement definitions); closeTag definitions "mask"
                    | SvgDefinitionContent.Gradient gradient ->
                        let name = match gradient.Geometry with SvgGradientGeometry.Linear _ -> "linearGradient" | SvgGradientGeometry.Radial _ -> "radialGradient"
                        beginTag definitions name; appendAttribute definitions "id" (id definition.Id); appendAttribute definitions "gradientUnits" (units gradient.Units); appendAttribute definitions "gradientTransform" (svgMatrix gradient.Transform); appendAttribute definitions "spreadMethod" (spread gradient.Spread); appendAttribute definitions "color-interpolation" "sRGB"; gradient.InheritFrom |> Option.iter (fun reference -> appendAttribute definitions "href" $"#{id reference}")
                        match gradient.Geometry with SvgGradientGeometry.Linear(first, second) -> appendAttribute definitions "x1" (svgNumber first.X); appendAttribute definitions "y1" (svgNumber first.Y); appendAttribute definitions "x2" (svgNumber second.X); appendAttribute definitions "y2" (svgNumber second.Y) | SvgGradientGeometry.Radial(center, radius, focal) -> appendAttribute definitions "cx" (svgNumber center.X); appendAttribute definitions "cy" (svgNumber center.Y); appendAttribute definitions "r" (svgNumber radius); focal |> Option.iter (fun point -> appendAttribute definitions "fx" (svgNumber point.X); appendAttribute definitions "fy" (svgNumber point.Y))
                        endOpen definitions; appendStops definitions gradient.Stops; closeTag definitions name
                    | SvgDefinitionContent.Font font ->
                        beginTag definitions "style"; appendAttribute definitions "data-fsgg-font-id" definition.Id; endOpen definitions
                        definitions.Append("@font-face{font-family:&quot;").Append(xmlEscape font.Family).Append("&quot;;src:url(&quot;").Append(xmlEscape font.Source).Append("&quot;) format(&quot;woff2&quot;)}") |> ignore
                        closeTag definitions "style"

                document.Definitions |> List.iter appendExplicitDefinition
                document.Children |> List.iter (appendElement body)
                let output = StringBuilder()
                beginTag output "svg"; appendAttribute output "xmlns" "http://www.w3.org/2000/svg"; appendAttribute output "id" (id "root"); appendAttribute output "data-fsgg-document-id" document.Id; appendAttribute output "data-fsgg-mount-namespace" mountNamespace; appendAttribute output "viewBox" $"{svgNumber document.ViewBox.X} {svgNumber document.ViewBox.Y} {svgNumber document.ViewBox.Width} {svgNumber document.ViewBox.Height}"; endOpen output
                beginTag output "defs"; endOpen output; output.Append(definitions) |> ignore; closeTag output "defs"; output.Append(body) |> ignore; closeTag output "svg"
                Ok(output.ToString())
            with error -> Error [ issue "unsupported-export" "/" error.Message ]
