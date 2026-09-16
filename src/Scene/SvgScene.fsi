namespace FS.GG.UI.Scene

[<RequireQualifiedAccess>]
/// Portable kinds available to product-defined scene property descriptors.
type SvgScenePropertyKind =
    | Text
    | Number
    | Flag
    | Coordinate

/// Declares one allowed property without assigning product behavior to Rendering.
type SvgScenePropertyDescriptor =
    {
        Key: string
        Kind: SvgScenePropertyKind
        Required: bool
    }

/// Product-supplied descriptor for one entity kind.
type SvgSceneKindDescriptor =
    {
        KindId: string
        DisplayName: string
        Properties: SvgScenePropertyDescriptor list
    }

/// Complete versioned interchange value for an editable SVG scene.
type SvgSceneEnvelope =
    {
        Schema: string
        Metadata: SvgSceneMetadata
        Document: SvgDocument
        Catalog: SvgAssetCatalog
        Instances: SvgPrefabInstance list
        Fonts: SvgFontResource list
    }

[<RequireQualifiedAccess>]
module SvgScene =
    /// The only scene-envelope version accepted by this API.
    val schema: string

    /// Validate entity kinds and properties against product-supplied descriptors.
    val validateDescriptors:
        descriptors: SvgSceneKindDescriptor list -> metadata: SvgSceneMetadata -> SvgDocumentIssue list

    /// Serialize a validated envelope into a bounded deterministic wire value.
    val serialize: value: SvgSceneEnvelope -> Result<string, SvgDocumentIssue list>
    /// Deserialize and validate the entire envelope before returning any value.
    val deserialize: serialized: string -> Result<SvgSceneEnvelope, SvgDocumentIssue list>

    /// Add empty product-neutral metadata around a legacy document/catalog pair.
    val migrateLegacy:
        document: SvgDocument ->
        catalog: SvgAssetCatalog ->
        instances: SvgPrefabInstance list ->
        fonts: SvgFontResource list ->
            Result<SvgSceneEnvelope, SvgDocumentIssue list>

[<RequireQualifiedAccess>]
module SvgScenePlacement =
    /// Snap a finite document-space point against an explicit origin and step.
    val grid: grid: SvgSceneGrid -> point: Point -> Result<Point, SvgDocumentIssue list>
    /// Validate and preserve a freeform point without snapping.
    val freeform: point: Point -> Result<Point, SvgDocumentIssue list>
