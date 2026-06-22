namespace FS.GG.UI.Scene

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open SceneWire

type ProtocolVersion =
    { Major: int
      Minor: int }

type RequirementLevel =
    | Required
    | Optional

type DegradationPolicy =
    | Reject
    | Degrade
    | Ignore

type CapabilityRequirement =
    { CapabilityId: string
      RequirementLevel: RequirementLevel
      DegradationPolicy: DegradationPolicy
      AffectedScenePaths: string list }

type ResourceKind =
    | ImageResource
    | FontResource
    | BinaryResource of string

type ResourceEntry =
    { ResourceId: string
      Kind: ResourceKind
      ContentHash: string
      ByteLength: int64 option
      Required: bool
      MediaType: string option
      SourceLabel: string option }

type ResourceAvailabilityStatus =
    | ResourceAvailable
    | ResourceMissing
    | ResourceCorrupted
    | ResourceHashMismatch
    | ResourceDuplicated

type ResourceAvailability =
    { ResourceId: string
      Kind: ResourceKind option
      ContentHash: string option
      ByteLength: int64 option
      Status: ResourceAvailabilityStatus }

type TargetCapabilityProfile =
    { ProfileId: string
      SupportedCapabilities: string list }

type PackageExportOptions =
    { ProfileId: string
      Resources: ResourceEntry list
      OptionalCapabilities: string list }

type PackageInspectionOptions =
    { TargetProfile: TargetCapabilityProfile option
      Resources: ResourceAvailability list }

type PackageDiagnosticStage =
    | Parse
    | Version
    | Capability
    | Resource
    | ScenePayload
    | TextPayload

type PackageDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: PackageDiagnosticStage
      Message: string
      ScenePath: string option
      CapabilityId: string option
      ResourceId: string option }

type PackageInspectionStatus =
    | PackageAccepted
    | PackageAcceptedWithDegradation
    | PackageRejected

type CapabilityVerdict =
    { Requirement: CapabilityRequirement
      Supported: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

type ResourceVerdict =
    { Entry: ResourceEntry
      Availability: ResourceAvailability option
      Accepted: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

type PortableScenePackage =
    { Version: ProtocolVersion
      ProfileId: string
      Capabilities: CapabilityRequirement list
      Resources: ResourceEntry list
      Scene: Scene
      CanonicalBytes: byte[]
      PackageIdentity: string
      Diagnostics: PackageDiagnostic list }

type PackageInspectionReport =
    { Status: PackageInspectionStatus
      PackageIdentity: string option
      Version: ProtocolVersion option
      ProfileId: string option
      CapabilityVerdicts: CapabilityVerdict list
      ResourceVerdicts: ResourceVerdict list
      Diagnostics: PackageDiagnostic list }

type SemanticComparison =
    { Equivalent: bool
      ExpectedCapabilities: SceneElementKind list
      ActualCapabilities: SceneElementKind list
      Diagnostics: PackageDiagnostic list }

module SceneCodec =
    let magicHeader = "FSGGSCENE"
    let supportedVersion = { Major = 1; Minor = 0 }

    let defaultExportOptions =
        { ProfileId = "scene-portable/v1"
          Resources = []
          OptionalCapabilities = [] }

    let defaultInspectionOptions =
        { TargetProfile = None
          Resources = [] }

    let private requiredProfileTag = 1
    let private requiredCapabilitiesTag = 2
    let private requiredResourcesTag = 3
    let private requiredSceneTag = 4

    // Tag table:
    // 1 required profile id, 2 required capability profile, 3 required resource manifest,
    // 4 required scene payload. Tags 1..999 are required; tags >=1000 are length-skippable optional.

    let private diagnostic
        (severity: DiagnosticSeverity)
        (stage: PackageDiagnosticStage)
        (message: string)
        (scenePath: string option)
        (capabilityId: string option)
        (resourceId: string option)
        : PackageDiagnostic =
        { Severity = severity
          Stage = stage
          Message = message
          ScenePath = scenePath
          CapabilityId = capabilityId
          ResourceId = resourceId }

    let private error (stage: PackageDiagnosticStage) (message: string) =
        diagnostic Error stage message None None None

    let private warning (stage: PackageDiagnosticStage) (message: string) =
        diagnostic Warning stage message None None None

    let private sha256Hex (bytes: byte[]) =
        SHA256.HashData bytes |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let packageIdentity (bytes: byte[]) =
        "sha256:" + sha256Hex bytes

    let private stringHash (value: string) =
        value |> Encoding.UTF8.GetBytes |> sha256Hex


    let private writeRequirementLevel (writer: BinaryWriter) (level: RequirementLevel) =
        writer.Write(enumTag level [ Required; Optional ])

    let private readRequirementLevel (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Required; Optional ] "requirement-level"

    let private writeDegradationPolicy (writer: BinaryWriter) (policy: DegradationPolicy) =
        writer.Write(enumTag policy [ Reject; Degrade; Ignore ])

    let private readDegradationPolicy (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Reject; Degrade; Ignore ] "degradation-policy"

    let private writeCapabilityRequirement (writer: BinaryWriter) (requirement: CapabilityRequirement) =
        writeString writer requirement.CapabilityId
        writeRequirementLevel writer requirement.RequirementLevel
        writeDegradationPolicy writer requirement.DegradationPolicy
        writeList writer writeString requirement.AffectedScenePaths

    let private readCapabilityRequirement (reader: BinaryReader) : CapabilityRequirement =
        { CapabilityId = readString reader
          RequirementLevel = readRequirementLevel reader
          DegradationPolicy = readDegradationPolicy reader
          AffectedScenePaths = readList reader readString }

    let private writeResourceKind (writer: BinaryWriter) (kind: ResourceKind) =
        match kind with
        | ImageResource -> writer.Write(0)
        | FontResource -> writer.Write(1)
        | BinaryResource label ->
            writer.Write(2)
            writeString writer label

    let private readResourceKind (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> ImageResource
        | 1 -> FontResource
        | 2 -> BinaryResource(readString reader)
        | tag -> failwithf "Unknown resource-kind tag %d" tag

    let private writeResourceEntry (writer: BinaryWriter) (entry: ResourceEntry) =
        writeString writer entry.ResourceId
        writeResourceKind writer entry.Kind
        writeString writer entry.ContentHash
        writeInt64Option writer entry.ByteLength
        writer.Write(entry.Required)
        writeStringOption writer entry.MediaType
        writeStringOption writer entry.SourceLabel

    let private readResourceEntry (reader: BinaryReader) : ResourceEntry =
        { ResourceId = readString reader
          Kind = readResourceKind reader
          ContentHash = readString reader
          ByteLength = readInt64Option reader
          Required = reader.ReadBoolean()
          MediaType = readStringOption reader
          SourceLabel = readStringOption reader }

    let private writeSection (writer: BinaryWriter) (tag: int) (writePayload: BinaryWriter -> unit) =
        use stream = new MemoryStream()
        use sectionWriter = new BinaryWriter(stream, Encoding.UTF8, true)
        writePayload sectionWriter
        sectionWriter.Flush()
        let bytes = stream.ToArray()
        writer.Write(tag)
        writer.Write(bytes.Length)
        writer.Write(bytes)

    let private writeEnvelope (profile: string) (capabilities: CapabilityRequirement list) (resources: ResourceEntry list) (scene: Scene) =
        use stream = new MemoryStream()
        use writer = new BinaryWriter(stream, Encoding.UTF8, true)
        writer.Write(Encoding.ASCII.GetBytes magicHeader)
        writer.Write(supportedVersion.Major)
        writer.Write(supportedVersion.Minor)
        writeSection writer requiredProfileTag (fun w -> writeString w profile)
        writeSection writer requiredCapabilitiesTag (fun w -> writeList w writeCapabilityRequirement capabilities)
        writeSection writer requiredResourcesTag (fun w -> writeList w writeResourceEntry resources)
        writeSection writer requiredSceneTag (fun w -> writeScene w scene)
        writer.Flush()
        stream.ToArray()

    let private capabilityId kind =
        match kind with
        | EmptyElement -> "scene.empty"
        | GroupElement -> "scene.group"
        | RectangleElement -> "scene.rectangle"
        | CircleElement -> "scene.circle"
        | EllipseElement -> "scene.ellipse"
        | LineElement -> "scene.line"
        | PathElement -> "scene.path"
        | PointsElement -> "scene.points"
        | VerticesElement -> "scene.vertices"
        | ArcElement -> "scene.arc"
        | TextElement -> "scene.text"
        | TextRunElement -> "scene.text-run"
        | ImageElement -> "scene.image"
        | ClipElement -> "scene.clip"
        | RegionElement -> "scene.region"
        | ColorSpaceElement -> "scene.color-space"
        | PerspectiveElement -> "scene.perspective"
        | PictureElement -> "scene.picture"
        | ChartElement -> "scene.chart"
        | TranslateElement -> "scene.translate"
        | SizedTextElement -> "scene.sized-text"
        | GlyphRunElement -> "scene.glyph-run"

    let private sourceMediaType (source: string) =
        let lower = source.ToLowerInvariant()
        if lower.EndsWith(".png", StringComparison.Ordinal) then Some "image/png"
        elif lower.EndsWith(".jpg", StringComparison.Ordinal) || lower.EndsWith(".jpeg", StringComparison.Ordinal) then Some "image/jpeg"
        elif lower.EndsWith(".webp", StringComparison.Ordinal) then Some "image/webp"
        else Some "application/octet-stream"

    let private autoResource (source: string) (index: int) : ResourceEntry =
        let resourceId = sprintf "image-%04d" index
        { ResourceId = resourceId
          Kind = ImageResource
          ContentHash = "unresolved:" + stringHash source
          ByteLength = None
          Required = true
          MediaType = sourceMediaType source
          SourceLabel = Some source }

    let private normalizeSceneResources (provided: ResourceEntry list) (scene: Scene) : Scene * ResourceEntry list =
        let bySource =
            provided
            |> List.choose (fun entry -> entry.SourceLabel |> Option.map (fun label -> label, entry))
            |> Map.ofList

        // mutable: stable package-local image ids are allocated during a single authored-order walk.
        let allocated = Dictionary<string, ResourceEntry>(StringComparer.Ordinal)
        let mutable nextIndex = 1

        let resourceFor (source: string) : ResourceEntry =
            match Map.tryFind source bySource with
            | Some entry -> entry
            | None ->
                match allocated.TryGetValue source with
                | true, entry -> entry
                | false, _ ->
                    let entry = autoResource source nextIndex
                    nextIndex <- nextIndex + 1
                    allocated[source] <- entry
                    entry

        let rec sceneWithResources (scene: Scene) : Scene =
            { Nodes = scene.Nodes |> List.map nodeWithResources }

        and nodeWithResources (node: SceneNode) : SceneNode =
            match node with
            | Image(bounds, source) ->
                let entry = resourceFor source
                Image(bounds, entry.ResourceId)
            | Group scenes -> Group(scenes |> List.map sceneWithResources)
            | ClipNode(clip, scene) -> ClipNode(clip, sceneWithResources scene)
            | ColorSpaceNode(colorSpace, scene) -> ColorSpaceNode(colorSpace, sceneWithResources scene)
            | PerspectiveNode(transform, scene) -> PerspectiveNode(transform, sceneWithResources scene)
            | PictureNode picture -> PictureNode { picture with Scene = sceneWithResources picture.Scene }
            | Translate(offset, scene) -> Translate(offset, sceneWithResources scene)
            | CachedSubtree boundary -> CachedSubtree { boundary with Scene = sceneWithResources boundary.Scene }
            | other -> other

        let transformed = sceneWithResources scene

        let resources =
            provided @ (allocated.Values |> Seq.toList)
            |> List.distinctBy _.ResourceId
            |> List.sortBy _.ResourceId

        transformed, resources

    let private capabilityPaths (scene: Scene) =
        let add (kind: SceneElementKind) (path: string) (acc: Map<string, string list>) =
            let id = capabilityId kind
            let paths = Map.tryFind id acc |> Option.defaultValue []
            Map.add id (path :: paths) acc

        let rec walkScene (path: string) (acc: Map<string, string list>) (scene: Scene) =
            scene.Nodes
            |> List.mapi (fun index node -> index, node)
            |> List.fold (fun state (index, node) -> walkNode (path + $"/nodes/{index}") state node) acc

        and walkNode (path: string) (acc: Map<string, string list>) (node: SceneNode) =
            match node with
            | Empty -> add EmptyElement path acc
            | Group scenes ->
                scenes
                |> List.mapi (fun index scene -> index, scene)
                |> List.fold (fun state (index, scene) -> walkScene (path + $"/group/{index}") state scene) (add GroupElement path acc)
            | Rectangle _ -> add RectangleElement path acc
            | PaintedRectangle _ -> add RectangleElement path acc
            | Circle _ -> add CircleElement path acc
            | FilledEllipse _ -> add EllipseElement path acc
            | Ellipse _ -> add EllipseElement path acc
            | Line _ -> add LineElement path acc
            | SceneNode.Path _ -> add PathElement path acc
            | Points _ -> add PointsElement path acc
            | Vertices _ -> add VerticesElement path acc
            | Arc _ -> add ArcElement path acc
            | Text _ -> add TextElement path acc
            | TextRun _ -> add TextRunElement path acc
            | Image _ -> add ImageElement path acc
            | ClipNode(_, scene) -> walkScene (path + "/clip") (add ClipElement path acc) scene
            | RegionNode _ -> add RegionElement path acc
            | ColorSpaceNode(_, scene) -> walkScene (path + "/color-space") (add ColorSpaceElement path acc) scene
            | PerspectiveNode(_, scene) -> walkScene (path + "/perspective") (add PerspectiveElement path acc) scene
            | PictureNode picture -> walkScene (path + "/picture") (add PictureElement path acc) picture.Scene
            | Chart _ -> add ChartElement path acc
            | Translate(_, scene) -> walkScene (path + "/translate") (add TranslateElement path acc) scene
            | SizedText _ -> add SizedTextElement path acc
            | GlyphRun _ -> add GlyphRunElement path acc
            | CachedSubtree boundary -> walkScene (path + "/cached") acc boundary.Scene

        walkScene "" Map.empty scene
        |> Map.toList
        |> List.map (fun (id, paths) -> id, List.rev paths)

    let private requirementsFor (optionalCapabilityIds: string list) (scene: Scene) : CapabilityRequirement list =
        let optional = optionalCapabilityIds |> Set.ofList
        let required =
            capabilityPaths scene
            |> List.map (fun (id, paths) ->
                { CapabilityId = id
                  RequirementLevel = if Set.contains id optional then Optional else Required
                  DegradationPolicy = if Set.contains id optional then Degrade else Reject
                  AffectedScenePaths = paths })

        let extras =
            optionalCapabilityIds
            |> List.filter (fun id -> required |> List.exists (fun item -> item.CapabilityId = id) |> not)
            |> List.map (fun id ->
                { CapabilityId = id
                  RequirementLevel = Optional
                  DegradationPolicy = Degrade
                  AffectedScenePaths = [] })

        (required @ extras) |> List.sortBy _.CapabilityId

    let exportScene (options: PackageExportOptions) (scene: Scene) =
        let normalizedScene, resources = normalizeSceneResources options.Resources scene
        let capabilities = requirementsFor options.OptionalCapabilities normalizedScene
        let canonicalBytes = writeEnvelope options.ProfileId capabilities resources normalizedScene

        { Version = supportedVersion
          ProfileId = options.ProfileId
          Capabilities = capabilities
          Resources = resources
          Scene = normalizedScene
          CanonicalBytes = canonicalBytes
          PackageIdentity = packageIdentity canonicalBytes
          Diagnostics = [] }

    let export scene = exportScene defaultExportOptions scene

    type private ParsedSections =
        { ProfileId: string option
          Capabilities: CapabilityRequirement list option
          Resources: ResourceEntry list option
          Scene: Scene option
          Diagnostics: PackageDiagnostic list }

    let private emptySections : ParsedSections =
        { ProfileId = None
          Capabilities = None
          Resources = None
          Scene = None
          Diagnostics = [] }

    let private parseSection (tag: int) (payload: byte[]) (sections: ParsedSections) : ParsedSections =
        use stream = new MemoryStream(payload)
        use reader = new BinaryReader(stream, Encoding.UTF8, true)

        match tag with
        | tag when tag = requiredProfileTag -> { sections with ProfileId = Some(readString reader) }
        | tag when tag = requiredCapabilitiesTag -> { sections with Capabilities = Some(readList reader readCapabilityRequirement) }
        | tag when tag = requiredResourcesTag -> { sections with Resources = Some(readList reader readResourceEntry) }
        | tag when tag = requiredSceneTag -> { sections with Scene = Some(readScene reader) }
        | tag when tag >= 1000 ->
            { sections with
                Diagnostics = warning Parse $"Skipped unknown optional package tag {tag}." :: sections.Diagnostics }
        | tag ->
            failwithf "Unknown required package tag %d" tag

    let importPackage (bytes: byte[]) =
        try
            if bytes.Length < magicHeader.Length + 8 then
                Result.Error [ error Parse "Package is too short to contain a portable scene header." ]
            else
                use stream = new MemoryStream(bytes)
                use reader = new BinaryReader(stream, Encoding.UTF8, true)
                let magic = Encoding.ASCII.GetString(reader.ReadBytes(magicHeader.Length))

                if magic <> magicHeader then
                    Result.Error [ error Parse $"Invalid package magic header '{magic}'." ]
                else
                    let version =
                        { Major = reader.ReadInt32()
                          Minor = reader.ReadInt32() }

                    let mutable sections = emptySections

                    while stream.Position < stream.Length do
                        let tag = reader.ReadInt32()
                        let length = reader.ReadInt32()
                        if length < 0 || stream.Position + int64 length > stream.Length then
                            failwithf "Invalid length %d for package tag %d" length tag
                        let payload = reader.ReadBytes(length)
                        sections <- parseSection tag payload sections

                    let missing =
                        [ if sections.ProfileId.IsNone then "profile"
                          if sections.Capabilities.IsNone then "capabilities"
                          if sections.Resources.IsNone then "resources"
                          if sections.Scene.IsNone then "scene" ]

                    if not missing.IsEmpty then
                        Result.Error [ error Parse ("Package is missing required sections: " + String.concat ", " missing) ]
                    else
                        let diagnostics =
                            [ yield! List.rev sections.Diagnostics
                              if version.Major <> supportedVersion.Major then
                                  yield error Version $"Producer protocol {version.Major}.{version.Minor} is outside supported major {supportedVersion.Major}.x."
                              elif version.Minor > supportedVersion.Minor then
                                  yield warning Version $"Producer protocol minor {version.Minor} is newer than supported minor {supportedVersion.Minor}; required tags and capabilities will decide acceptance." ]

                        Result.Ok
                            { Version = version
                              ProfileId = sections.ProfileId.Value
                              Capabilities = sections.Capabilities.Value
                              Resources = sections.Resources.Value
                              Scene = sections.Scene.Value
                              CanonicalBytes = Array.copy bytes
                              PackageIdentity = packageIdentity bytes
                              Diagnostics = diagnostics }
        with ex ->
            Result.Error [ error Parse ex.Message ]

    let private allKnownCapabilities =
        [ EmptyElement
          GroupElement
          RectangleElement
          CircleElement
          EllipseElement
          LineElement
          PathElement
          PointsElement
          VerticesElement
          ArcElement
          TextElement
          TextRunElement
          ImageElement
          ClipElement
          RegionElement
          ColorSpaceElement
          PerspectiveElement
          PictureElement
          ChartElement
          TranslateElement
          SizedTextElement
          GlyphRunElement ]
        |> List.map capabilityId

    let private duplicateResourceIds (resources: ResourceAvailability list) =
        resources
        |> List.groupBy _.ResourceId
        |> List.choose (fun (id, items) -> if List.length items > 1 then Some id else None)
        |> Set.ofList

    let private capabilityVerdicts (options: PackageInspectionOptions) (package: PortableScenePackage) : CapabilityVerdict list =
        let supported =
            options.TargetProfile
            |> Option.map _.SupportedCapabilities
            |> Option.defaultValue allKnownCapabilities
            |> Set.ofList

        package.Capabilities
        |> List.map (fun (requirement: CapabilityRequirement) ->
            let isSupported = Set.contains requirement.CapabilityId supported
            let degraded = (not isSupported) && requirement.RequirementLevel = Optional && requirement.DegradationPolicy <> Reject
            let diagnostics =
                [ if not isSupported then
                      let severity = if requirement.RequirementLevel = Required || requirement.DegradationPolicy = Reject then Error else Warning
                      yield
                          diagnostic
                              severity
                              Capability
                              $"Capability '{requirement.CapabilityId}' is not supported by the target profile."
                              (requirement.AffectedScenePaths |> List.tryHead)
                              (Some requirement.CapabilityId)
                              None ]

            { Requirement = requirement
              Supported = isSupported
              Degraded = degraded
              Diagnostics = diagnostics })

    let private resourceVerdicts (options: PackageInspectionOptions) (package: PortableScenePackage) : ResourceVerdict list =
        let availabilityById =
            options.Resources
            |> List.groupBy (fun (availability: ResourceAvailability) -> availability.ResourceId)
            |> Map.ofList

        package.Resources
        |> List.map (fun (entry: ResourceEntry) ->
            let observed : ResourceAvailability option =
                Map.tryFind entry.ResourceId availabilityById |> Option.bind List.tryHead
            let duplicated = duplicateResourceIds options.Resources |> Set.contains entry.ResourceId

            let status : ResourceAvailabilityStatus option =
                if duplicated then Some ResourceDuplicated
                else observed |> Option.map _.Status

            let accepted, degraded, diagnostics : bool * bool * PackageDiagnostic list =
                match status, observed with
                | Some ResourceAvailable, Some availability ->
                    let kindMismatch =
                        availability.Kind
                        |> Option.exists ((<>) entry.Kind)

                    let hashMismatch =
                        availability.ContentHash
                        |> Option.exists (fun hash -> not (String.Equals(hash, entry.ContentHash, StringComparison.Ordinal)))

                    let lengthMismatch =
                        match availability.ByteLength, entry.ByteLength with
                        | Some actual, Some expected -> actual <> expected
                        | _ -> false

                    if kindMismatch || hashMismatch || lengthMismatch then
                        false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' metadata does not match the manifest." None None (Some entry.ResourceId) ]
                    else
                        true, false, []
                | Some ResourceAvailable, None ->
                    false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' was marked available without metadata." None None (Some entry.ResourceId) ]
                | Some ResourceHashMismatch, _
                | Some ResourceCorrupted, _
                | Some ResourceDuplicated, _ ->
                    false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' is not usable: {status.Value}." None None (Some entry.ResourceId) ]
                | Some ResourceMissing, _
                | None, _ ->
                    if entry.Required then
                        false, false, [ diagnostic Error Resource $"Required resource '{entry.ResourceId}' is unavailable." None None (Some entry.ResourceId) ]
                    else
                        true, true, [ diagnostic Warning Resource $"Optional resource '{entry.ResourceId}' is unavailable and will degrade." None None (Some entry.ResourceId) ]

            { Entry = entry
              Availability = observed
              Accepted = accepted
              Degraded = degraded
              Diagnostics = diagnostics })

    let inspectWith (options: PackageInspectionOptions) (bytes: byte[]) =
        match importPackage bytes with
        | Result.Error diagnostics ->
            { Status = PackageRejected
              PackageIdentity = Some(packageIdentity bytes)
              Version = None
              ProfileId = None
              CapabilityVerdicts = []
              ResourceVerdicts = []
              Diagnostics = diagnostics }
        | Result.Ok package ->
            let capabilityVerdicts : CapabilityVerdict list = capabilityVerdicts options package
            let resourceVerdicts : ResourceVerdict list = resourceVerdicts options package

            let diagnostics =
                [ yield! package.Diagnostics
                  yield! capabilityVerdicts |> List.collect _.Diagnostics
                  yield! resourceVerdicts |> List.collect _.Diagnostics ]

            let rejected =
                diagnostics |> List.exists (fun d -> d.Severity = Error || d.Severity = Fatal)

            let degraded =
                capabilityVerdicts |> List.exists _.Degraded
                || resourceVerdicts |> List.exists _.Degraded
                || diagnostics |> List.exists (fun d -> d.Severity = Warning)

            let status =
                if rejected then PackageRejected
                elif degraded then PackageAcceptedWithDegradation
                else PackageAccepted

            { Status = status
              PackageIdentity = Some package.PackageIdentity
              Version = Some package.Version
              ProfileId = Some package.ProfileId
              CapabilityVerdicts = capabilityVerdicts
              ResourceVerdicts = resourceVerdicts
              Diagnostics = diagnostics }

    let inspect (bytes: byte[]) =
        inspectWith defaultInspectionOptions bytes

    let compareScenes (expected: Scene) (actual: Scene) =
        let expectedCapabilities = Scene.describe expected
        let actualCapabilities = Scene.describe actual

        let diagnostics =
            [ if expectedCapabilities <> actualCapabilities then
                  yield diagnostic Error ScenePayload "Scene capability sequence differs." None None None
              if expected <> actual then
                  yield diagnostic Error ScenePayload "Scene payload differs after import." None None None ]

        { Equivalent = diagnostics.IsEmpty
          ExpectedCapabilities = expectedCapabilities
          ActualCapabilities = actualCapabilities
          Diagnostics = diagnostics }

    let formatDiagnostics diagnostics =
        diagnostics
        |> List.map (fun diagnostic ->
            let severity = string diagnostic.Severity
            let stage = string diagnostic.Stage
            let path = diagnostic.ScenePath |> Option.map (sprintf " scene=%s") |> Option.defaultValue ""
            let cap = diagnostic.CapabilityId |> Option.map (sprintf " capability=%s") |> Option.defaultValue ""
            let res = diagnostic.ResourceId |> Option.map (sprintf " resource=%s") |> Option.defaultValue ""
            $"{severity} {stage}: {diagnostic.Message}{path}{cap}{res}")
