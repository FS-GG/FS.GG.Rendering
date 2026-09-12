namespace FS.GG.UI.Scene

open System
open System.Collections.Generic
open System.Globalization
open System.Text

type SvgImportRequest =
    { AssetNamespace: string
      DocumentId: string
      Limits: SvgDocumentLimits }

type SvgFontResource =
    { DefinitionId: string
      Family: string
      FileName: string
      Sha256: string
      License: string
      Base64: string }

type SvgResourceDocument = { Document: SvgDocument; Fonts: SvgFontResource list }

type SvgAssetRights =
    { License: string
      Attribution: string option
      Source: string option }

type SvgAssetReference =
    { AssetId: string
      Revision: int }

type SvgAssetEnvelope =
    { Schema: string
      AssetId: string
      Revision: int
      ContentHash: string
      Rights: SvgAssetRights
      Dependencies: SvgAssetReference list
      Document: SvgDocument }

type SvgAssetCatalog =
    { Schema: string
      Assets: SvgAssetEnvelope list }

[<RequireQualifiedAccess>]
type SvgPrefabProperty =
    | Transform
    | Visibility
    | Presentation

[<RequireQualifiedAccess>]
type SvgPrefabOverrideValue =
    | Transform of SvgAffine
    | Visibility of bool
    | Presentation of SvgPresentation option

type SvgPrefabOverride =
    { ElementId: string
      Property: SvgPrefabProperty
      Value: SvgPrefabOverrideValue }

type SvgPrefabInstance =
    { InstanceId: string
      AssetId: string
      AcceptedRevision: int
      Overrides: SvgPrefabOverride list }

type SvgPrefabConflict =
    { InstanceId: string
      ElementId: string
      Property: SvgPrefabProperty
      Message: string }

[<RequireQualifiedAccess>]
type SvgAuthoringOperation =
    | ReplaceDocument of SvgDocument
    | TransformElements of elementIds: string list * transform: SvgAffine
    | UpsertAsset of SvgAssetEnvelope
    | PutInstance of SvgPrefabInstance
    | UpdateInstances of assetId: string * fromRevision: int * toRevision: int

type SvgAuthoringTransaction =
    { Schema: string
      Id: string
      Operations: SvgAuthoringOperation list }

type SvgAuthoringSnapshot =
    { SourceRevision: int
      SerializedDocument: string
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list }

type SvgAuthoringCheckpoint =
    { Document: SvgDocument
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list
      Conflicts: SvgPrefabConflict list }

type SvgAuthoringPreview =
    { TransactionId: string
      BaseRevision: int
      Candidate: SvgAuthoringCheckpoint }

type SvgAuthoringState =
    { Revision: int
      Document: SvgDocument
      Catalog: SvgAssetCatalog
      Instances: SvgPrefabInstance list
      Conflicts: SvgPrefabConflict list
      Undo: SvgAuthoringCheckpoint list
      Redo: SvgAuthoringCheckpoint list
      Preview: SvgAuthoringPreview option
      PlaySnapshot: SvgAuthoringSnapshot option }

[<RequireQualifiedAccess>]
type SvgAuthoringError =
    | StaleRevision of expected: int * actual: int
    | InvalidTransaction of SvgDocumentIssue list
    | PreviewAlreadyActive of string
    | NoActivePreview
    | PreviewMismatch of string
    | NothingToUndo
    | NothingToRedo

module private AuthoringCommon =
    let issue code location message = { Code = code; Location = location; Message = message }

    let utf8Bytes (text: string) =
        let bytes = ResizeArray<byte>()
        let mutable index = 0
        while index < text.Length do
            let code = int text[index]
            if code <= 0x7f then bytes.Add(byte code)
            elif code <= 0x7ff then
                bytes.Add(byte (0xc0 ||| (code >>> 6)))
                bytes.Add(byte (0x80 ||| (code &&& 0x3f)))
            elif code >= 0xd800 && code <= 0xdbff && index + 1 < text.Length then
                let low = int text[index + 1]
                if low >= 0xdc00 && low <= 0xdfff then
                    let scalar = 0x10000 + ((code - 0xd800) <<< 10) + low - 0xdc00
                    bytes.Add(byte (0xf0 ||| (scalar >>> 18)))
                    bytes.Add(byte (0x80 ||| ((scalar >>> 12) &&& 0x3f)))
                    bytes.Add(byte (0x80 ||| ((scalar >>> 6) &&& 0x3f)))
                    bytes.Add(byte (0x80 ||| (scalar &&& 0x3f)))
                    index <- index + 1
                else
                    bytes.Add 0xefuy; bytes.Add 0xbfuy; bytes.Add 0xbduy
            else
                bytes.Add(byte (0xe0 ||| (code >>> 12)))
                bytes.Add(byte (0x80 ||| ((code >>> 6) &&& 0x3f)))
                bytes.Add(byte (0x80 ||| (code &&& 0x3f)))
            index <- index + 1
        bytes.ToArray()

    let sha256Bytes (source: byte array) =
        let constants =
            [| 0x428a2f98u; 0x71374491u; 0xb5c0fbcfu; 0xe9b5dba5u; 0x3956c25bu; 0x59f111f1u; 0x923f82a4u; 0xab1c5ed5u
               0xd807aa98u; 0x12835b01u; 0x243185beu; 0x550c7dc3u; 0x72be5d74u; 0x80deb1feu; 0x9bdc06a7u; 0xc19bf174u
               0xe49b69c1u; 0xefbe4786u; 0x0fc19dc6u; 0x240ca1ccu; 0x2de92c6fu; 0x4a7484aau; 0x5cb0a9dcu; 0x76f988dau
               0x983e5152u; 0xa831c66du; 0xb00327c8u; 0xbf597fc7u; 0xc6e00bf3u; 0xd5a79147u; 0x06ca6351u; 0x14292967u
               0x27b70a85u; 0x2e1b2138u; 0x4d2c6dfcu; 0x53380d13u; 0x650a7354u; 0x766a0abbu; 0x81c2c92eu; 0x92722c85u
               0xa2bfe8a1u; 0xa81a664bu; 0xc24b8b70u; 0xc76c51a3u; 0xd192e819u; 0xd6990624u; 0xf40e3585u; 0x106aa070u
               0x19a4c116u; 0x1e376c08u; 0x2748774cu; 0x34b0bcb5u; 0x391c0cb3u; 0x4ed8aa4au; 0x5b9cca4fu; 0x682e6ff3u
               0x748f82eeu; 0x78a5636fu; 0x84c87814u; 0x8cc70208u; 0x90befffau; 0xa4506cebu; 0xbef9a3f7u; 0xc67178f2u |]
        let rotate value count = (value >>> count) ||| (value <<< (32 - count))
        let normalizeWord value = value ||| 0u
        let bitLength = uint64 source.Length * 8UL
        let paddedLength = ((source.Length + 9 + 63) / 64) * 64
        let data = Array.zeroCreate<byte> paddedLength
        Array.blit source 0 data 0 source.Length
        data[source.Length] <- 0x80uy
        for index in 0 .. 7 do data[paddedLength - 1 - index] <- byte (bitLength >>> (index * 8))
        let mutable h0 = 0x6a09e667u
        let mutable h1 = 0xbb67ae85u
        let mutable h2 = 0x3c6ef372u
        let mutable h3 = 0xa54ff53au
        let mutable h4 = 0x510e527fu
        let mutable h5 = 0x9b05688cu
        let mutable h6 = 0x1f83d9abu
        let mutable h7 = 0x5be0cd19u
        for block in 0 .. 64 .. paddedLength - 1 do
            let words = Array.zeroCreate<uint32> 64
            for index in 0 .. 15 do
                let offset = block + index * 4
                words[index] <- (uint32 data[offset] <<< 24) ||| (uint32 data[offset + 1] <<< 16) ||| (uint32 data[offset + 2] <<< 8) ||| uint32 data[offset + 3]
            for index in 16 .. 63 do
                let s0 = rotate words[index - 15] 7 ^^^ rotate words[index - 15] 18 ^^^ (words[index - 15] >>> 3)
                let s1 = rotate words[index - 2] 17 ^^^ rotate words[index - 2] 19 ^^^ (words[index - 2] >>> 10)
                words[index] <- normalizeWord (words[index - 16] + s0 + words[index - 7] + s1)
            let mutable a = h0
            let mutable b = h1
            let mutable c = h2
            let mutable d = h3
            let mutable e = h4
            let mutable f = h5
            let mutable g = h6
            let mutable h = h7
            for index in 0 .. 63 do
                let s1 = rotate e 6 ^^^ rotate e 11 ^^^ rotate e 25
                let choice = (e &&& f) ^^^ ((~~~e) &&& g)
                let temp1 = normalizeWord (h + s1 + choice + constants[index] + words[index])
                let s0 = rotate a 2 ^^^ rotate a 13 ^^^ rotate a 22
                let majority = (a &&& b) ^^^ (a &&& c) ^^^ (b &&& c)
                let temp2 = normalizeWord (s0 + majority)
                h <- g; g <- f; f <- e; e <- normalizeWord (d + temp1)
                d <- c; c <- b; b <- a; a <- normalizeWord (temp1 + temp2)
            h0 <- normalizeWord (h0 + a); h1 <- normalizeWord (h1 + b); h2 <- normalizeWord (h2 + c); h3 <- normalizeWord (h3 + d)
            h4 <- normalizeWord (h4 + e); h5 <- normalizeWord (h5 + f); h6 <- normalizeWord (h6 + g); h7 <- normalizeWord (h7 + h)
        let hexWord value =
            let digits = "0123456789abcdef"
            let output = StringBuilder(8)
            for shift in [ 28; 24; 20; 16; 12; 8; 4; 0 ] do
                output.Append(digits[int ((value >>> shift) &&& 0xfu)]) |> ignore
            output.ToString()
        [ h0; h1; h2; h3; h4; h5; h6; h7 ] |> List.map hexWord |> String.concat ""

    let sha256 (text: string) = sha256Bytes (utf8Bytes text)

module SvgImport =
    open AuthoringCommon

    type private XmlNode =
        { Name: string
          Attributes: Map<string, string>
          Children: XmlNode list
          Text: string
          Location: string }

    let private byteCount text = AuthoringCommon.utf8Bytes text |> Array.length
    let private localName (name: string) = let parts = name.Split(':') in parts[parts.Length - 1]
    let private fail code location message = raise (InvalidOperationException(String.concat "|" [ code; location; message ]))
    let private located (xml: string) index =
        let mutable line = 1
        let mutable column = 1
        for offset in 0 .. index - 1 do
            if xml[offset] = '\n' then line <- line + 1; column <- 1 else column <- column + 1
        $"/line/{line}/column/{column}"

    let private decode location (value: string) =
        let mutable result = value
        for encoded, plain in [ "&quot;", "\""; "&apos;", "'"; "&lt;", "<"; "&gt;", ">"; "&amp;", "&" ] do
            result <- result.Replace(encoded, plain)
        if result.Contains("&") then fail "unsupported-entity" location "only the five predefined XML entities are supported"
        result

    let private parseXml (limits: SvgDocumentLimits) (xml: string) =
        if xml.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0 then fail "active-content" (located xml (xml.IndexOf('<'))) "DOCTYPE is forbidden"
        if xml.IndexOf("<!ENTITY", StringComparison.OrdinalIgnoreCase) >= 0 then fail "active-content" (located xml (xml.IndexOf('<'))) "entities are forbidden"
        if xml.IndexOf("<style", StringComparison.OrdinalIgnoreCase) >= 0 then fail "active-content" (located xml (xml.IndexOf("<style", StringComparison.OrdinalIgnoreCase))) "arbitrary CSS is forbidden"
        if xml.Length >= 2 && int xml[0] = 0x1f && int xml[1] = 0x8b then fail "compressed-input" "/" "compressed SVG input is unsupported"
        let mutable index = 0
        let mutable nodeCount = 0
        let skipSpace () = while index < xml.Length && Char.IsWhiteSpace xml[index] do index <- index + 1
        let readName () =
            let start = index
            while index < xml.Length && (Char.IsLetterOrDigit xml[index] || "_:-.".IndexOf(xml[index]) >= 0) do index <- index + 1
            if start = index then fail "malformed-xml" (located xml index) "expected an XML name"
            xml.Substring(start, index - start)
        let rec readNode depth =
            if depth > limits.MaxReferenceDepth then fail "xml-depth-limit" (located xml index) $"XML nesting exceeds {limits.MaxReferenceDepth}"
            skipSpace()
            while index + 1 < xml.Length && xml[index] = '<' && xml[index + 1] = '?' do
                let finish = xml.IndexOf("?>", index + 2, StringComparison.Ordinal)
                if finish < 0 then fail "malformed-xml" (located xml index) "unterminated processing instruction"
                index <- finish + 2; skipSpace()
            if index >= xml.Length || xml[index] <> '<' then fail "malformed-xml" (located xml index) "expected an element"
            let start = index
            nodeCount <- nodeCount + 1
            if nodeCount > limits.MaxNodes + limits.MaxDefinitions then fail "node-limit" (located xml start) $"SVG input exceeds the bounded parser node budget"
            index <- index + 1
            let name = readName()
            let attrs = ResizeArray<string * string>()
            let mutable selfClosing = false
            let mutable opened = false
            while not opened do
                skipSpace()
                if index >= xml.Length then fail "malformed-xml" (located xml start) "unterminated start tag"
                elif xml[index] = '>' then index <- index + 1; opened <- true
                elif xml[index] = '/' && index + 1 < xml.Length && xml[index + 1] = '>' then index <- index + 2; opened <- true; selfClosing <- true
                else
                    let attrName = readName() |> localName
                    skipSpace()
                    if index >= xml.Length || xml[index] <> '=' then fail "malformed-xml" (located xml index) "expected '=' after attribute"
                    index <- index + 1; skipSpace()
                    if index >= xml.Length || (xml[index] <> '\"' && xml[index] <> '\'') then fail "malformed-xml" (located xml index) "attribute values must be quoted"
                    let quote = xml[index]
                    index <- index + 1
                    let valueStart = index
                    while index < xml.Length && xml[index] <> quote do index <- index + 1
                    if index >= xml.Length then fail "malformed-xml" (located xml valueStart) "unterminated attribute"
                    let value = decode (located xml valueStart) (xml.Substring(valueStart, index - valueStart))
                    index <- index + 1
                    if attrs |> Seq.exists (fun (key, _) -> key = attrName) then fail "duplicate-attribute" (located xml valueStart) $"duplicate attribute {attrName}"
                    attrs.Add(attrName, value)
            let children = ResizeArray<XmlNode>()
            let text = StringBuilder()
            if not selfClosing then
                let mutable closed = false
                while not closed do
                    skipSpace()
                    if index >= xml.Length then fail "malformed-xml" (located xml start) $"unterminated {name}"
                    elif index + 1 < xml.Length && xml[index] = '<' && xml[index + 1] = '/' then
                        index <- index + 2
                        let closing = readName()
                        skipSpace()
                        if index >= xml.Length || xml[index] <> '>' || closing <> name then fail "malformed-xml" (located xml index) $"mismatched closing tag for {name}"
                        index <- index + 1; closed <- true
                    elif xml[index] = '<' then children.Add(readNode (depth + 1))
                    else
                        let textStart = index
                        while index < xml.Length && xml[index] <> '<' do index <- index + 1
                        let value = xml.Substring(textStart, index - textStart)
                        if not (String.IsNullOrWhiteSpace value) then text.Append(decode (located xml textStart) value) |> ignore
            { Name = localName name; Attributes = attrs |> Seq.toList |> Map.ofList; Children = children |> Seq.toList; Text = text.ToString(); Location = located xml start }
        let root = readNode 0
        skipSpace()
        if index <> xml.Length then fail "malformed-xml" (located xml index) "trailing content after root"
        root

    let private number location (value: string) =
        match Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture) with
        | true, parsed when not (Double.IsNaN parsed || Double.IsInfinity parsed) -> parsed
        | _ -> fail "invalid-number" location $"invalid finite number: {value}"

    let private numbers location (value: string) =
        value.Replace(",", " ").Split([| ' '; '\t'; '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (number location)
        |> Array.toList

    let private byte location value =
        let parsed = number location value
        if parsed < 0.0 || parsed > 255.0 || parsed <> floor parsed then fail "invalid-color" location $"invalid color channel: {value}"
        uint8 parsed

    let private color location (value: string) =
        let value = value.Trim()
        if value = "black" then { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }
        elif value = "white" then { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }
        elif value.StartsWith("#") && value.Length = 7 then
            try { Red = Convert.ToByte(value.Substring(1, 2), 16); Green = Convert.ToByte(value.Substring(3, 2), 16); Blue = Convert.ToByte(value.Substring(5, 2), 16); Alpha = 255uy }
            with _ -> fail "invalid-color" location $"unsupported color: {value}"
        elif value.StartsWith("rgb(") && value.EndsWith(")") then
            match value.Substring(4, value.Length - 5).Replace(",", " ").Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList with
            | [ red; green; blue ] -> { Red = byte location red; Green = byte location green; Blue = byte location blue; Alpha = 255uy }
            | _ -> fail "invalid-color" location $"unsupported color: {value}"
        else fail "invalid-color" location $"unsupported color: {value}"

    let private normalize (value: string) =
        let output = StringBuilder()
        for character in value.Trim() do
            if Char.IsLetterOrDigit character || character = '-' || character = '_' || character = '.' then output.Append(character) |> ignore
            else output.Append('-') |> ignore
        output.ToString()

    let private attr name (node: XmlNode) = node.Attributes |> Map.tryFind name
    let private required name node = attr name node |> Option.defaultWith (fun () -> fail "missing-attribute" node.Location $"{node.Name} requires {name}")
    let private rect node =
        { X = attr "x" node |> Option.map (number node.Location) |> Option.defaultValue 0.0
          Y = attr "y" node |> Option.map (number node.Location) |> Option.defaultValue 0.0
          Width = required "width" node |> number node.Location
          Height = required "height" node |> number node.Location }
    let private viewBox node =
        match required "viewBox" node |> numbers node.Location with
        | [ x; y; width; height ] -> { X = x; Y = y; Width = width; Height = height }
        | _ -> fail "invalid-view-box" node.Location "viewBox requires four numbers"

    let private transform node =
        match attr "transform" node with
        | None -> SvgAffine.identity
        | Some value when value.StartsWith("matrix(") && value.EndsWith(")") ->
            match numbers node.Location (value.Substring(7, value.Length - 8)) with
            | [ a; b; c; d; e; f ] -> { A = a; B = b; C = c; D = d; E = e; F = f }
            | _ -> fail "unsupported-transform" node.Location "matrix requires six numbers"
        | Some value when value.StartsWith("translate(") && value.EndsWith(")") ->
            match numbers node.Location (value.Substring(10, value.Length - 11)) with
            | [ x ] -> SvgAffine.translate x 0.0
            | [ x; y ] -> SvgAffine.translate x y
            | _ -> fail "unsupported-transform" node.Location "translate requires one or two numbers"
        | Some _ -> fail "unsupported-transform" node.Location "only matrix and translate transforms are supported"

    let private tokenizePath location (data: string) =
        let tokens = ResizeArray<string>()
        let mutable index = 0
        while index < data.Length do
            if Char.IsWhiteSpace data[index] || data[index] = ',' then index <- index + 1
            elif Char.IsLetter data[index] then tokens.Add(string data[index]); index <- index + 1
            else
                let start = index
                if data[index] = '+' || data[index] = '-' then index <- index + 1
                while index < data.Length && Char.IsDigit data[index] do index <- index + 1
                if index < data.Length && data[index] = '.' then index <- index + 1; while index < data.Length && Char.IsDigit data[index] do index <- index + 1
                if index < data.Length && (data[index] = 'e' || data[index] = 'E') then
                    index <- index + 1
                    if index < data.Length && (data[index] = '+' || data[index] = '-') then index <- index + 1
                    while index < data.Length && Char.IsDigit data[index] do index <- index + 1
                if start = index then fail "invalid-path" location "invalid path token"
                tokens.Add(data.Substring(start, index - start))
        tokens |> Seq.toList

    let private path limits node =
        let tokens = tokenizePath node.Location (required "d" node) |> List.toArray
        let commands = ResizeArray<PathCommand>()
        let mutable index = 0
        let take () = if index >= tokens.Length then fail "invalid-path" node.Location "missing path coordinate" else let value = number node.Location tokens[index] in index <- index + 1; value
        while index < tokens.Length do
            if commands.Count >= limits.MaxPathSegments then fail "path-segment-limit" node.Location $"path exceeds {limits.MaxPathSegments} segments"
            let command = tokens[index]
            index <- index + 1
            match command with
            | "M" -> commands.Add(PathCommand.MoveTo { X = take(); Y = take() })
            | "L" -> commands.Add(PathCommand.LineTo { X = take(); Y = take() })
            | "Q" -> commands.Add(PathCommand.QuadTo({ X = take(); Y = take() }, { X = take(); Y = take() }))
            | "C" -> commands.Add(PathCommand.CubicTo({ X = take(); Y = take() }, { X = take(); Y = take() }, { X = take(); Y = take() }))
            | "Z" | "z" -> commands.Add PathCommand.Close
            | _ -> fail "unsupported-path-command" node.Location $"unsupported absolute path command: {command}"
        { Commands = commands |> Seq.toList
          FillType = if attr "fill-rule" node = Some "evenodd" then PathFillType.EvenOdd else PathFillType.Winding }

    let private validateVocabulary (root: XmlNode) =
        let allowedElements = set [ "svg"; "defs"; "g"; "rect"; "circle"; "ellipse"; "line"; "polygon"; "polyline"; "path"; "text"; "linearGradient"; "radialGradient"; "stop"; "clipPath"; "mask"; "symbol"; "use" ]
        let globallyAllowed = set [ "id"; "data-semantic-id"; "data-fsgg-node"; "data-fsgg-element-id"; "data-fsgg-semantic-id"; "data-fsgg-document-id"; "data-fsgg-mount-namespace"; "transform"; "fill"; "fill-opacity"; "stroke"; "stroke-opacity"; "stroke-width"; "stroke-linecap"; "stroke-linejoin"; "stroke-miterlimit"; "stroke-dasharray"; "stroke-dashoffset"; "opacity"; "fill-rule"; "clip-path"; "mask"; "visibility"; "display"; "pointer-events"; "xmlns"; "version"; "viewBox"; "x"; "y"; "width"; "height"; "d"; "points"; "cx"; "cy"; "r"; "rx"; "ry"; "fx"; "fy"; "font-family"; "font-size"; "font-weight"; "direction"; "text-anchor"; "dominant-baseline"; "href"; "x1"; "y1"; "x2"; "y2"; "gradientUnits"; "gradientTransform"; "spreadMethod"; "offset"; "stop-color"; "stop-opacity"; "maskUnits"; "clipPathUnits"; "color-interpolation"; "style" ]
        let rec walk node =
            if node.Name = "script" || node.Name = "foreignObject" || node.Name = "style" then fail "active-content" node.Location $"active element is forbidden: {node.Name}"
            if not (allowedElements.Contains node.Name) then fail "unsupported-element" node.Location $"unsupported element: {node.Name}"
            if node.Text.Length > 0 && node.Name <> "text" then fail "unsupported-text" node.Location "text content is supported only by text elements"
            for KeyValue(name, value) in node.Attributes do
                if name.StartsWith("on", StringComparison.OrdinalIgnoreCase) then fail "active-content" node.Location $"event attribute is forbidden: {name}"
                if name = "filter" || (name = "style" && not (node.Name = "mask" && (value = "mask-type:alpha" || value = "mask-type:luminance"))) then fail "active-content" node.Location $"CSS/filter attribute is forbidden: {name}"
                if not (globallyAllowed.Contains name) then fail "unsupported-attribute" node.Location $"unsupported attribute: {name}"
                if (name = "href" && not (value.StartsWith("#"))) || value.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0 && not (value.StartsWith("url(#")) then
                    fail "external-reference" node.Location $"external reference is forbidden: {value}"
            node.Children |> List.iter walk
        walk root

    let importXml request xml =
        if String.IsNullOrWhiteSpace request.AssetNamespace then Error [ issue "blank-asset-namespace" "/assetNamespace" "asset namespace must not be blank" ]
        elif String.IsNullOrWhiteSpace request.DocumentId then Error [ issue "blank-document-id" "/documentId" "document id must not be blank" ]
        elif String.IsNullOrEmpty xml then Error [ issue "malformed-xml" "/" "SVG input must not be empty" ]
        elif byteCount xml > request.Limits.MaxSerializedBytes then Error [ issue "document-byte-limit" "/" $"SVG input exceeds {request.Limits.MaxSerializedBytes} bytes" ]
        else
            try
                let root = parseXml request.Limits xml
                validateVocabulary root
                if root.Name <> "svg" then fail "unsupported-root" root.Location "root element must be svg"
                let prefix = normalize request.AssetNamespace + "--"
                let idFor source = prefix + normalize source
                let mutable generated = 0
                let elementId node =
                    match attr "id" node with
                    | Some value -> idFor value
                    | None -> generated <- generated + 1; $"{prefix}generated-{generated}"
                let reference attribute node =
                    attr attribute node
                    |> Option.map (fun value ->
                        let local =
                            if value.StartsWith("url(#") && value.EndsWith(")") then value.Substring(5, value.Length - 6)
                            elif value.StartsWith("#") then value.Substring(1)
                            else fail "external-reference" node.Location $"reference must be local: {value}"
                        idFor local)
                let paintSource node name =
                    match attr name node with
                    | None | Some "none" -> None
                    | Some value when value.StartsWith("url(#") -> reference name node |> Option.map SvgPaintSource.Definition
                    | Some value -> Some(SvgPaintSource.Solid(color node.Location value))
                let presentation node =
                    let alpha name = attr name node |> Option.map (number node.Location) |> Option.defaultValue 1.0
                    let fill = paintSource node "fill"
                    let stroke =
                        paintSource node "stroke"
                        |> Option.map (fun source ->
                            { Source = source
                              Width = attr "stroke-width" node |> Option.map (number node.Location) |> Option.defaultValue 1.0
                              Cap = match attr "stroke-linecap" node with Some "round" -> StrokeCap.Round | Some "square" -> StrokeCap.Square | _ -> StrokeCap.Butt
                              Join = match attr "stroke-linejoin" node with Some "round" -> StrokeJoin.RoundJoin | Some "bevel" -> StrokeJoin.Bevel | _ -> StrokeJoin.Miter
                              Miter = attr "stroke-miterlimit" node |> Option.map (number node.Location) |> Option.defaultValue 4.0
                              Dash = attr "stroke-dasharray" node |> Option.map (numbers node.Location) |> Option.defaultValue []
                              DashOffset = attr "stroke-dashoffset" node |> Option.map (number node.Location) |> Option.defaultValue 0.0 })
                    { FillSource = fill
                      StrokeStyle = stroke
                      OverallOpacity = alpha "opacity"
                      FillRule = if attr "fill-rule" node = Some "evenodd" then PathFillType.EvenOdd else PathFillType.Winding }
                let basePaint =
                    { Fill = Some { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }; Stroke = None; Opacity = 1.0; Antialias = true; BlendMode = BlendMode.SrcOver
                      Shader = None; ColorFilter = ColorFilter.NoColorFilter; MaskFilter = MaskFilter.NoMaskFilter
                      ImageFilter = ImageFilter.NoImageFilter; PathEffect = PathEffect.NoPathEffect }
                let pointList node close =
                    let values = required "points" node |> numbers node.Location
                    if values.Length < 4 || values.Length % 2 <> 0 then fail "invalid-points" node.Location "points require at least two coordinate pairs"
                    let commands =
                        values
                        |> List.chunkBySize 2
                        |> List.mapi (fun index pair ->
                            let point = { X = pair[0]; Y = pair[1] }
                            if index = 0 then PathCommand.MoveTo point else PathCommand.LineTo point)
                    { Commands = if close then commands @ [ PathCommand.Close ] else commands
                      FillType = if attr "fill-rule" node = Some "evenodd" then PathFillType.EvenOdd else PathFillType.Winding }
                let rec toElement node =
                    let content =
                        match node.Name with
                        | "rect" ->
                            let value = rect node
                            SvgElementContent.SceneLeaf { Nodes = [ SceneNode.PaintedRectangle(value, basePaint) ] }
                        | "circle" ->
                            let center = { X = required "cx" node |> number node.Location; Y = required "cy" node |> number node.Location }
                            SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Circle(center, required "r" node |> number node.Location, basePaint.Fill.Value) ] }
                        | "ellipse" ->
                            let cx = required "cx" node |> number node.Location
                            let cy = required "cy" node |> number node.Location
                            let rx = required "rx" node |> number node.Location
                            let ry = required "ry" node |> number node.Location
                            SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Ellipse({ X = cx-rx; Y = cy-ry; Width = rx*2.0; Height = ry*2.0 }, basePaint) ] }
                        | "line" ->
                            let first = { X = required "x1" node |> number node.Location; Y = required "y1" node |> number node.Location }
                            let second = { X = required "x2" node |> number node.Location; Y = required "y2" node |> number node.Location }
                            SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Line(first, second, basePaint) ] }
                        | "polygon" -> SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(pointList node true, basePaint) ] }
                        | "polyline" -> SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(pointList node false, basePaint) ] }
                        | "text" ->
                            if node.Children.Length > 0 then fail "unsupported-element-position" node.Location "text cannot contain nested markup"
                            let position = { X = required "x" node |> number node.Location; Y = required "y" node |> number node.Location }
                            let font =
                                { Family = attr "font-family" node
                                  Size = attr "font-size" node |> Option.map (number node.Location) |> Option.defaultValue 16.0
                                  Weight = attr "font-weight" node |> Option.map (fun value -> int (number node.Location value)) }
                            SvgElementContent.SceneLeaf { Nodes = [ SceneNode.TextRun { Text = node.Text; Position = position; Font = font; Paint = basePaint } ] }
                        | "path" -> SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Path(path request.Limits node, basePaint) ] }
                        | "g" -> SvgElementContent.Group(node.Children |> List.map toElement)
                        | "use" ->
                            let viewport =
                                match attr "width" node, attr "height" node with
                                | Some width, Some height -> Some { X = attr "x" node |> Option.map (number node.Location) |> Option.defaultValue 0.0; Y = attr "y" node |> Option.map (number node.Location) |> Option.defaultValue 0.0; Width = number node.Location width; Height = number node.Location height }
                                | None, None -> None
                                | _ -> fail "invalid-symbol-viewport" node.Location "symbol use requires both width and height"
                            SvgElementContent.SymbolInstance(required "href" node |> fun value -> idFor (value.TrimStart('#')), viewport)
                        | _ -> fail "unsupported-element-position" node.Location $"{node.Name} cannot appear as drawable content"
                    { Id = elementId node
                      SemanticId = (attr "data-semantic-id" node |> Option.orElseWith (fun () -> attr "data-fsgg-semantic-id" node)) |> Option.map idFor
                      Visible = attr "visibility" node <> Some "hidden" && attr "display" node <> Some "none"
                      Transform = transform node
                      ClipId = reference "clip-path" node
                      MaskId = reference "mask" node
                      Presentation = Some(presentation node)
                      Content = content }
                let toStop node =
                    if node.Name <> "stop" then fail "unsupported-element-position" node.Location "gradient children must be stops"
                    { Offset = required "offset" node |> number node.Location
                      Color = required "stop-color" node |> color node.Location
                      StopOpacity = attr "stop-opacity" node |> Option.map (number node.Location) |> Option.defaultValue 1.0 }
                let toDefinition node =
                    let id = required "id" node |> idFor
                    let content =
                        match node.Name with
                        | "linearGradient" ->
                            let point x y = { X = attr x node |> Option.map (number node.Location) |> Option.defaultValue 0.0; Y = attr y node |> Option.map (number node.Location) |> Option.defaultValue 0.0 }
                            SvgDefinitionContent.Gradient
                                { Geometry = SvgGradientGeometry.Linear(point "x1" "y1", point "x2" "y2")
                                  Units = if attr "gradientUnits" node = Some "userSpaceOnUse" then SvgCoordinateUnits.UserSpaceOnUse else SvgCoordinateUnits.ObjectBoundingBox
                                  Transform = match attr "gradientTransform" node with None -> SvgAffine.identity | Some _ -> transform { node with Attributes = node.Attributes |> Map.add "transform" (required "gradientTransform" node) }
                                  Spread = match attr "spreadMethod" node with Some "repeat" -> SvgSpreadMethod.Repeat | Some "reflect" -> SvgSpreadMethod.Reflect | _ -> SvgSpreadMethod.Pad
                                  Stops = node.Children |> List.map toStop
                                  InheritFrom = reference "href" node }
                        | "radialGradient" ->
                            let center = { X = attr "cx" node |> Option.map (number node.Location) |> Option.defaultValue 0.5; Y = attr "cy" node |> Option.map (number node.Location) |> Option.defaultValue 0.5 }
                            let focal =
                                match attr "fx" node, attr "fy" node with
                                | None, None -> None
                                | fx, fy -> Some { X = fx |> Option.map (number node.Location) |> Option.defaultValue center.X; Y = fy |> Option.map (number node.Location) |> Option.defaultValue center.Y }
                            SvgDefinitionContent.Gradient
                                { Geometry = SvgGradientGeometry.Radial(center, attr "r" node |> Option.map (number node.Location) |> Option.defaultValue 0.5, focal)
                                  Units = if attr "gradientUnits" node = Some "userSpaceOnUse" then SvgCoordinateUnits.UserSpaceOnUse else SvgCoordinateUnits.ObjectBoundingBox
                                  Transform = match attr "gradientTransform" node with None -> SvgAffine.identity | Some _ -> transform { node with Attributes = node.Attributes |> Map.add "transform" (required "gradientTransform" node) }
                                  Spread = match attr "spreadMethod" node with Some "repeat" -> SvgSpreadMethod.Repeat | Some "reflect" -> SvgSpreadMethod.Reflect | _ -> SvgSpreadMethod.Pad
                                  Stops = node.Children |> List.map toStop
                                  InheritFrom = reference "href" node }
                        | "clipPath" ->
                            let shapes = node.Children |> List.map (fun child -> match child.Name with "rect" -> SvgClipShape.Rectangle(rect child) | "path" -> SvgClipShape.Path(path request.Limits child) | _ -> fail "unsupported-element-position" child.Location "clipPath supports rect and path")
                            SvgDefinitionContent.Clip((if attr "clipPathUnits" node = Some "objectBoundingBox" then SvgCoordinateUnits.ObjectBoundingBox else SvgCoordinateUnits.UserSpaceOnUse), shapes)
                        | "mask" ->
                            let region = { X = attr "x" node |> Option.map (number node.Location) |> Option.defaultValue 0.0; Y = attr "y" node |> Option.map (number node.Location) |> Option.defaultValue 0.0; Width = attr "width" node |> Option.map (number node.Location) |> Option.defaultValue 1.0; Height = attr "height" node |> Option.map (number node.Location) |> Option.defaultValue 1.0 }
                            let kind = if attr "style" node = Some "mask-type:luminance" then SvgMaskKind.Luminance else SvgMaskKind.Alpha
                            SvgDefinitionContent.Mask((if attr "maskUnits" node = Some "objectBoundingBox" then SvgCoordinateUnits.ObjectBoundingBox else SvgCoordinateUnits.UserSpaceOnUse), region, kind, node.Children |> List.map toElement)
                        | "symbol" -> SvgDefinitionContent.Symbol((attr "viewBox" node |> Option.map (fun _ -> viewBox node)), node.Children |> List.map toElement)
                        | _ -> fail "unsupported-definition" node.Location $"unsupported definition: {node.Name}"
                    { Id = id; Content = content }
                let definitions =
                    root.Children
                    |> List.filter (fun node -> node.Name = "defs")
                    |> List.collect _.Children
                    |> List.map toDefinition
                let children = root.Children |> List.filter (fun node -> node.Name <> "defs") |> List.map toElement
                let document = { Schema = SvgDocument.schema; Id = request.DocumentId; ViewBox = viewBox root; Definitions = definitions; Children = children }
                let issues = SvgDocument.validate (byteCount xml) request.Limits document
                if issues.IsEmpty then Ok document else Error issues
            with
            | :? InvalidOperationException as error ->
                match error.Message.Split('|') |> Array.toList with
                | code :: location :: rest -> Error [ issue code location (String.concat "|" rest) ]
                | _ -> Error [ issue "malformed-xml" "/" error.Message ]
            | error -> Error [ issue "malformed-xml" "/" error.Message ]

module SvgResourceInterchange =
    open AuthoringCommon

    let private expectedHash = "09aee8065d25508f23a4c3d92cd777ac869c52d93fd868a88f025d888a7937d6"
    let private family = "Noto Sans"
    let private fileName = "noto-sans-latin-400-normal.woff2"
    let private license = "OFL-1.1"
    let private maxFontBytes = 1024 * 1024
    let private maxTotalFontBytes = 2 * 1024 * 1024
    let private markerStart = "<style data-fsgg-resource-font=\"noto-sans-latin-400\">@font-face{font-family:\"Noto Sans\";font-weight:400;src:url(\"data:font/woff2;base64,"
    let private markerEnd = "\") format(\"woff2\")}</style>"

    let private decode location (base64: string) =
        // Reject from encoded length before allocating the decoded payload.
        let upperBound = (base64.Length / 4 + 1) * 3
        if upperBound > maxFontBytes then Error [ issue "font-byte-limit" location "decoded font exceeds 1 MiB" ]
        else
            try
                let bytes = Convert.FromBase64String base64
                if bytes.Length > maxFontBytes then Error [ issue "font-byte-limit" location "decoded font exceeds 1 MiB" ]
                else Ok bytes
            with _ -> Error [ issue "malformed-font-base64" location "font payload is not valid base64" ]

    let notoSansLatin400 base64 =
        match decode "/fonts/0/base64" base64 with
        | Error issues -> Error issues
        | Ok bytes when sha256Bytes bytes <> expectedHash -> Error [ issue "font-hash-mismatch" "/fonts/0/sha256" $"expected {expectedHash}" ]
        | Ok _ ->
            Ok { DefinitionId="noto-sans-latin-400"; Family=family; FileName=fileName; Sha256=expectedHash; License=license; Base64=base64 }

    let private validateResource index resource =
        if resource.DefinitionId <> "noto-sans-latin-400" || resource.Family <> family || resource.FileName <> fileName || resource.Sha256 <> expectedHash || resource.License <> license then
            Error [ issue "unapproved-font-manifest" $"/fonts/{index}" "font resource does not match the approved exact-byte Noto Sans manifest" ]
        else notoSansLatin400 resource.Base64

    let exportSvg mountNamespace value =
        if value.Fonts.Length > 2 then Error [ issue "font-total-limit" "/fonts" "font resources exceed the 2 MiB aggregate budget" ]
        else
            value.Fonts
            |> List.mapi validateResource
            |> List.fold (fun state result ->
                match state, result with
                | Ok resources, Ok resource -> Ok(resource :: resources)
                | Error issues, Error more -> Error(issues @ more)
                | Error issues, _ -> Error issues
                | _, Error issues -> Error issues) (Ok [])
            |> Result.bind (fun resources ->
                let total = resources |> List.sumBy (fun resource -> Convert.FromBase64String(resource.Base64).Length)
                if total > maxTotalFontBytes then Error [ issue "font-total-limit" "/fonts" "decoded fonts exceed 2 MiB" ]
                else
                    let withoutFonts = { value.Document with Definitions = value.Document.Definitions |> List.filter (fun definition -> match definition.Content with SvgDefinitionContent.Font _ -> false | _ -> true) }
                    SvgDocument.exportSvg mountNamespace withoutFonts
                    |> Result.map (fun xml ->
                        let generated = resources |> List.map (fun resource -> markerStart + resource.Base64 + markerEnd) |> String.concat ""
                        xml.Replace("<defs>", "<defs>" + generated)))

    let importXml request xml =
        if AuthoringCommon.utf8Bytes xml |> Array.length > request.Limits.MaxSerializedBytes then Error [ issue "document-byte-limit" "/" $"SVG input exceeds {request.Limits.MaxSerializedBytes} bytes" ]
        else
            let start = xml.IndexOf(markerStart, StringComparison.Ordinal)
            if start < 0 then SvgImport.importXml request xml |> Result.map (fun document -> {Document=document;Fonts=[]})
            else
                let payloadStart = start + markerStart.Length
                let finish = xml.IndexOf(markerEnd, payloadStart, StringComparison.Ordinal)
                if finish < 0 || xml.IndexOf("<style", finish + markerEnd.Length, StringComparison.OrdinalIgnoreCase) >= 0 then Error [ issue "unsupported-resource-style" "/fonts" "only one generated Noto Sans font-face is supported" ]
                else
                    let base64 = xml.Substring(payloadStart, finish-payloadStart)
                    notoSansLatin400 base64
                    |> Result.bind (fun resource ->
                        let stripped = xml.Remove(start, finish + markerEnd.Length - start)
                        SvgImport.importXml request stripped
                        |> Result.map (fun document ->
                            let font = {Id=resource.DefinitionId;Content=SvgDefinitionContent.Font {Family=resource.Family;Source=resource.FileName;Sha256=resource.Sha256;License=resource.License}}
                            { Document={document with Definitions=font::document.Definitions}; Fonts=[resource] }))

module SvgAsset =
    open AuthoringCommon

    let schema = "fsgg.svg-asset/1"
    let catalogSchema = "fsgg.svg-asset-catalog/1"
    let private wireHeader = "FSGGSVGASSET1"

    type private Writer() =
        let output = StringBuilder()
        member _.Token(value: string) = output.Append(value.Length).Append(':').Append(value) |> ignore
        member this.Int(value: int) = this.Token(string value)
        member this.Optional(value: string option) = match value with None -> this.Token("0") | Some item -> this.Token("1"); this.Token(item)
        member this.List(write, values: 'value list) = this.Int(values.Length); values |> List.iter write
        override _.ToString() = output.ToString()

    type private Reader(serialized: string) =
        let mutable index = 0
        member _.AtEnd = index = serialized.Length
        member _.Token() =
            let colon = serialized.IndexOf(':', index)
            if colon < index then failwith "missing asset token delimiter"
            let length = Int32.Parse(serialized.Substring(index, colon - index), CultureInfo.InvariantCulture)
            if length < 0 || colon + 1 + length > serialized.Length then failwith "asset token length exceeds input"
            let value = serialized.Substring(colon + 1, length)
            index <- colon + 1 + length
            value
        member this.Int() = Int32.Parse(this.Token(), CultureInfo.InvariantCulture)
        member this.Optional() = match this.Token() with "0" -> None | "1" -> Some(this.Token()) | _ -> failwith "invalid optional asset token"
        member this.List(maxCount, read) =
            let count = this.Int()
            if count < 0 || count > maxCount then failwith "asset list count exceeds limit"
            [ for _ in 1 .. count -> read() ]

    let contentHash document = SvgDocument.serialize document |> Result.map sha256

    let private key assetId revision = assetId + "@" + string revision

    let validateCatalog (catalog: SvgAssetCatalog) =
        let issues = ResizeArray<SvgDocumentIssue>()
        let add code location message = issues.Add(issue code location message)
        if catalog.Schema <> catalogSchema then add "unknown-catalog-schema" "/schema" $"expected {catalogSchema}"
        if catalog.Assets.Length > SvgDocument.defaultLimits.MaxDefinitions then
            add "asset-limit" "/assets" $"catalog exceeds {SvgDocument.defaultLimits.MaxDefinitions} asset revisions"
        catalog.Assets
        |> List.countBy (fun (asset: SvgAssetEnvelope) -> key asset.AssetId asset.Revision)
        |> List.iter (fun (identity, count) -> if count > 1 then add "duplicate-asset-revision" "/assets" $"asset revision is duplicated: {identity}")
        let assets: Map<string, SvgAssetEnvelope> = catalog.Assets |> List.map (fun asset -> key asset.AssetId asset.Revision, asset) |> Map.ofList
        catalog.Assets |> List.iteri (fun index (asset: SvgAssetEnvelope) ->
            let path = $"/assets/{index}"
            if asset.Schema <> schema then add "unknown-asset-schema" (path + "/schema") $"expected {schema}"
            if String.IsNullOrWhiteSpace asset.AssetId then add "blank-asset-id" (path + "/assetId") "asset id must not be blank"
            if asset.Revision < 1 then add "invalid-asset-revision" (path + "/revision") "asset revision must be positive"
            if String.IsNullOrWhiteSpace asset.Rights.License then add "missing-rights" (path + "/rights/license") "asset license must not be blank"
            match contentHash asset.Document with
            | Error nested -> nested |> List.iter issues.Add
            | Ok actual when actual <> asset.ContentHash -> add "content-hash-mismatch" (path + "/contentHash") $"expected canonical SHA-256 {actual}"
            | Ok _ -> ()
            asset.Dependencies |> List.iteri (fun depIndex (dependency: SvgAssetReference) ->
                if not (assets.ContainsKey(key dependency.AssetId dependency.Revision)) then add "missing-asset-reference" $"{path}/dependencies/{depIndex}" $"asset revision does not exist: {key dependency.AssetId dependency.Revision}"))
        let rec visit trail identity =
            if List.contains identity trail then
                let cycle = String.concat " -> " (List.rev (identity :: trail))
                add "cyclic-asset-reference" "/assets" $"asset dependency cycle: {cycle}"
            else
                match assets |> Map.tryFind identity with
                | None -> ()
                | Some asset -> asset.Dependencies |> List.iter (fun (dependency: SvgAssetReference) -> visit (identity :: trail) (key dependency.AssetId dependency.Revision))
        assets |> Map.toList |> List.iter (fun (identity, _) -> visit [] identity)
        issues |> Seq.distinct |> Seq.toList

    let serializeCatalog (catalog: SvgAssetCatalog) =
        let issues = validateCatalog catalog
        if not issues.IsEmpty then Error issues
        else
            try
                let writer = Writer()
                writer.Token wireHeader
                writer.Token catalog.Schema
                writer.List((fun (asset: SvgAssetEnvelope) ->
                    writer.Token asset.Schema; writer.Token asset.AssetId; writer.Int asset.Revision
                    writer.Token asset.ContentHash; writer.Token asset.Rights.License
                    writer.Optional asset.Rights.Attribution; writer.Optional asset.Rights.Source
                    writer.List((fun (dependency: SvgAssetReference) -> writer.Token dependency.AssetId; writer.Int dependency.Revision), asset.Dependencies)
                    writer.Token(SvgDocument.serialize asset.Document |> Result.defaultWith (fun _ -> failwith "validated document serialization failed"))), catalog.Assets)
                let serialized = writer.ToString()
                if AuthoringCommon.utf8Bytes serialized |> Array.length > SvgDocument.defaultLimits.MaxSerializedBytes then Error [ issue "catalog-byte-limit" "/" "serialized catalog exceeds 4 MiB" ] else Ok serialized
            with error -> Error [ issue "unsupported-catalog-serialization" "/" error.Message ]

    let deserializeCatalog serialized =
        if String.IsNullOrEmpty serialized then Error [ issue "invalid-catalog-serialization" "/" "serialized catalog must not be empty" ]
        elif AuthoringCommon.utf8Bytes serialized |> Array.length > SvgDocument.defaultLimits.MaxSerializedBytes then Error [ issue "catalog-byte-limit" "/" "serialized catalog exceeds 4 MiB" ]
        else
            try
                let reader = Reader(serialized)
                if reader.Token() <> wireHeader then failwith "unknown asset wire header"
                let catalog: SvgAssetCatalog =
                    { Schema = reader.Token()
                      Assets = reader.List(SvgDocument.defaultLimits.MaxDefinitions, fun () ->
                        { Schema = reader.Token(); AssetId = reader.Token(); Revision = reader.Int(); ContentHash = reader.Token()
                          Rights = { License = reader.Token(); Attribution = reader.Optional(); Source = reader.Optional() }
                          Dependencies = reader.List(SvgDocument.defaultLimits.MaxDefinitions, fun () -> { AssetId = reader.Token(); Revision = reader.Int() })
                          Document = SvgDocument.deserialize(reader.Token()) |> Result.defaultWith (fun _ -> failwith "invalid embedded document") }) }
                if not reader.AtEnd then failwith "trailing tokens after asset catalog"
                let issues = validateCatalog catalog
                if issues.IsEmpty then Ok catalog else Error issues
            with error -> Error [ issue "invalid-catalog-serialization" "/" error.Message ]

    let private mapElements (mapper: SvgElement -> SvgElement) (document: SvgDocument) =
        let rec map (element: SvgElement) : SvgElement =
            let nested = match element.Content with SvgElementContent.Group children -> SvgElementContent.Group(List.map map children) | other -> other
            mapper { element with Content = nested }
        { document with
            Children = document.Children |> List.map map
            Definitions = document.Definitions |> List.map (fun (definition: SvgDefinition) ->
                match definition.Content with
                | SvgDefinitionContent.Symbol(viewBox, children) -> { definition with Content = SvgDefinitionContent.Symbol(viewBox, List.map map children) }
                | SvgDefinitionContent.Mask(units, region, kind, children) -> { definition with Content = SvgDefinitionContent.Mask(units, region, kind, List.map map children) }
                | _ -> definition) }

    let resolveInstance (catalog: SvgAssetCatalog) (instance: SvgPrefabInstance) =
        let catalogIssues = validateCatalog catalog
        if not catalogIssues.IsEmpty then Error catalogIssues
        else
            match catalog.Assets |> List.tryFind (fun (asset: SvgAssetEnvelope) -> asset.AssetId = instance.AssetId && asset.Revision = instance.AcceptedRevision) with
            | None -> Error [ issue "missing-asset-reference" ("/instances/" + instance.InstanceId) $"asset revision does not exist: {key instance.AssetId instance.AcceptedRevision}" ]
            | Some asset ->
                let ids =
                    let values = ResizeArray<string>()
                    let rec collect (element: SvgElement) = values.Add element.Id; match element.Content with SvgElementContent.Group children -> children |> List.iter collect | _ -> ()
                    asset.Document.Children |> List.iter collect
                    asset.Document.Definitions |> List.iter (fun (definition: SvgDefinition) -> match definition.Content with SvgDefinitionContent.Symbol(_, children) | SvgDefinitionContent.Mask(_, _, _, children) -> children |> List.iter collect | _ -> ())
                    values |> Seq.toList |> Set.ofList
                let conflicts = ResizeArray<SvgPrefabConflict>()
                let overrides = instance.Overrides |> List.groupBy (fun value -> value.ElementId) |> Map.ofList
                let apply (element: SvgElement) : SvgElement =
                    match overrides |> Map.tryFind element.Id with
                    | None -> element
                    | Some values ->
                        values |> List.fold (fun current value ->
                            match value.Property, value.Value with
                            | SvgPrefabProperty.Transform, SvgPrefabOverrideValue.Transform transform -> { current with Transform = transform }
                            | SvgPrefabProperty.Visibility, SvgPrefabOverrideValue.Visibility visible -> { current with Visible = visible }
                            | SvgPrefabProperty.Presentation, SvgPrefabOverrideValue.Presentation presentation -> { current with Presentation = presentation }
                            | _ -> conflicts.Add { InstanceId = instance.InstanceId; ElementId = value.ElementId; Property = value.Property; Message = "override value does not match property" }; current) element
                instance.Overrides |> List.iter (fun value -> if not (ids.Contains value.ElementId) then conflicts.Add { InstanceId = instance.InstanceId; ElementId = value.ElementId; Property = value.Property; Message = "the accepted asset revision removed the overridden element/property" })
                Ok(mapElements apply asset.Document, conflicts |> Seq.toList)

module SvgAuthoring =
    open AuthoringCommon

    let transactionSchema = "fsgg.svg-authoring-transaction/1"

    let private checkpoint (state: SvgAuthoringState) : SvgAuthoringCheckpoint = { Document = state.Document; Catalog = state.Catalog; Instances = state.Instances; Conflicts = state.Conflicts }
    let private restore revision undo redo preview snapshot (value: SvgAuthoringCheckpoint) : SvgAuthoringState =
        { Revision = revision; Document = value.Document; Catalog = value.Catalog; Instances = value.Instances; Conflicts = value.Conflicts
          Undo = undo; Redo = redo; Preview = preview; PlaySnapshot = snapshot }
    let private validate (value: SvgAuthoringCheckpoint) : Result<SvgAuthoringCheckpoint, SvgDocumentIssue list> =
        let issues = ResizeArray<SvgDocumentIssue>()
        SvgDocument.validate 0 SvgDocument.defaultLimits value.Document |> List.iter issues.Add
        SvgAsset.validateCatalog value.Catalog |> List.iter issues.Add
        value.Instances
        |> List.countBy _.InstanceId
        |> List.iter (fun (identity, count) -> if String.IsNullOrWhiteSpace identity || count > 1 then issues.Add(issue "invalid-instance-id" "/instances" $"blank or duplicate instance id: {identity}"))
        let conflicts = ResizeArray<SvgPrefabConflict>()
        value.Instances |> List.iter (fun instance ->
            match SvgAsset.resolveInstance value.Catalog instance with
            | Error nested -> nested |> List.iter issues.Add
            | Ok(_, found) -> found |> List.iter conflicts.Add)
        if issues.Count = 0 then Ok { value with Conflicts = conflicts |> Seq.toList } else Error(issues |> Seq.toList)

    let private mapElements selected transform (document: SvgDocument) =
        let rec map (element: SvgElement) : SvgElement =
            let nested = match element.Content with SvgElementContent.Group children -> SvgElementContent.Group(List.map map children) | other -> other
            let value = { element with Content = nested }
            if selected |> Set.contains value.Id then { value with Transform = SvgAffine.compose transform value.Transform } else value
        { document with Children = document.Children |> List.map map }

    let private elementIds (document: SvgDocument) =
        let rec collect (element: SvgElement) =
            element.Id :: (match element.Content with SvgElementContent.Group children -> children |> List.collect collect | _ -> [])
        document.Children |> List.collect collect |> Set.ofList

    let private applyOperation (value: SvgAuthoringCheckpoint) operation : SvgAuthoringCheckpoint =
        match operation with
        | SvgAuthoringOperation.ReplaceDocument document -> { value with Document = document }
        | SvgAuthoringOperation.TransformElements(ids, transform) ->
            if not (SvgAffine.isFinite transform) || ids.IsEmpty || (ids |> List.exists (fun id -> not ((elementIds value.Document).Contains id))) then { value with Document = { value.Document with Schema = "invalid-transform" } }
            else { value with Document = mapElements (Set.ofList ids) transform value.Document }
        | SvgAuthoringOperation.UpsertAsset asset ->
            { value with Catalog = { value.Catalog with Assets = asset :: (value.Catalog.Assets |> List.filter (fun existing -> existing.AssetId <> asset.AssetId || existing.Revision <> asset.Revision)) } }
        | SvgAuthoringOperation.PutInstance instance ->
            { value with Instances = instance :: (value.Instances |> List.filter (fun existing -> existing.InstanceId <> instance.InstanceId)) }
        | SvgAuthoringOperation.UpdateInstances(assetId, fromRevision, toRevision) ->
            { value with Instances = value.Instances |> List.map (fun instance -> if instance.AssetId = assetId && instance.AcceptedRevision = fromRevision then { instance with AcceptedRevision = toRevision } else instance) }

    let private evaluate (transaction: SvgAuthoringTransaction) (state: SvgAuthoringState) =
        if transaction.Schema <> transactionSchema then Error [ issue "unknown-authoring-schema" "/transaction/schema" $"expected {transactionSchema}" ]
        elif String.IsNullOrWhiteSpace transaction.Id || transaction.Operations.IsEmpty then Error [ issue "invalid-transaction" "/transaction" "transaction id and at least one operation are required" ]
        elif
            transaction.Operations
            |> List.exists (function
                | SvgAuthoringOperation.UpsertAsset candidate ->
                    state.Catalog.Assets
                    |> List.exists (fun existing ->
                        existing.AssetId = candidate.AssetId
                        && existing.Revision = candidate.Revision
                        && existing <> candidate)
                | _ -> false)
        then
            Error [ issue "immutable-asset-revision" "/transaction/operations" "changed content, rights, or dependencies require a new asset revision" ]
        else
            transaction.Operations
            |> List.fold applyOperation (checkpoint state)
            |> validate

    let tryCreate revision document catalog instances =
        if revision < 0 then Error(SvgAuthoringError.InvalidTransaction [ issue "invalid-revision" "/revision" "revision must be non-negative" ])
        else
            let seed: SvgAuthoringCheckpoint = { Document = document; Catalog = catalog; Instances = instances; Conflicts = [] }
            match validate seed with
            | Error issues -> Error(SvgAuthoringError.InvalidTransaction issues)
            | Ok accepted -> Ok(restore revision [] [] None None accepted)

    let commit expectedRevision transaction state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        elif state.Preview.IsSome then Error(SvgAuthoringError.PreviewAlreadyActive state.Preview.Value.TransactionId)
        else
            match evaluate transaction state with
            | Error issues -> Error(SvgAuthoringError.InvalidTransaction issues)
            | Ok candidate -> Ok(restore (state.Revision + 1) (checkpoint state :: state.Undo) [] None state.PlaySnapshot candidate)

    let preview expectedRevision transaction state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        else
            match state.Preview with
            | Some active when active.TransactionId <> transaction.Id -> Error(SvgAuthoringError.PreviewAlreadyActive active.TransactionId)
            | _ ->
                match evaluate transaction { state with Preview = None } with
                | Error issues -> Error(SvgAuthoringError.InvalidTransaction issues)
                | Ok candidate -> Ok { state with Preview = Some { TransactionId = transaction.Id; BaseRevision = state.Revision; Candidate = candidate } }

    let commitPreview expectedRevision transactionId state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        else
            match state.Preview with
            | None -> Error SvgAuthoringError.NoActivePreview
            | Some preview when preview.TransactionId <> transactionId -> Error(SvgAuthoringError.PreviewMismatch transactionId)
            | Some preview when preview.BaseRevision <> state.Revision -> Error(SvgAuthoringError.StaleRevision(preview.BaseRevision, state.Revision))
            | Some preview -> Ok(restore (state.Revision + 1) (checkpoint state :: state.Undo) [] None state.PlaySnapshot preview.Candidate)

    let cancelPreview transactionId state =
        match state.Preview with
        | None -> Error SvgAuthoringError.NoActivePreview
        | Some preview when preview.TransactionId <> transactionId -> Error(SvgAuthoringError.PreviewMismatch transactionId)
        | Some _ -> Ok { state with Preview = None }

    let undo expectedRevision state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        else match state.Undo with [] -> Error SvgAuthoringError.NothingToUndo | previous :: rest -> Ok(restore (state.Revision + 1) rest (checkpoint state :: state.Redo) None state.PlaySnapshot previous)

    let redo expectedRevision state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        else match state.Redo with [] -> Error SvgAuthoringError.NothingToRedo | next :: rest -> Ok(restore (state.Revision + 1) (checkpoint state :: state.Undo) rest None state.PlaySnapshot next)

    let takePlaySnapshot expectedRevision state =
        if expectedRevision <> state.Revision then Error(SvgAuthoringError.StaleRevision(expectedRevision, state.Revision))
        else
            match SvgDocument.serialize state.Document with
            | Error issues -> Error(SvgAuthoringError.InvalidTransaction issues)
            | Ok serialized -> Ok { state with PlaySnapshot = Some { SourceRevision = state.Revision; SerializedDocument = serialized; Catalog = state.Catalog; Instances = state.Instances } }
