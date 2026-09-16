// See skill: fs-gg-scene
namespace FS.GG.UI.Scene

[<RequireQualifiedAccess>]
type SvgArtAlignment =
    | Left
    | HorizontalCenter
    | Right
    | Top
    | VerticalCenter
    | Bottom

[<RequireQualifiedAccess>]
type SvgArtError =
    | InvalidInput of SvgDocumentIssue list
    | MissingElement of string
    | InvalidSelection of string

type SvgArtGuide = { Axis: string; Position: float }

[<RequireQualifiedAccess>]
type SvgArtPrimitive =
    | Rectangle of Rect
    | Ellipse of Rect
    | Polygon of Point list
    | Path of PathSpec

[<RequireQualifiedAccess>]
type SvgArtSiblingOrder =
    | Back
    | Backward
    | Forward
    | Front

/// Portable state owned by an SVG art tool. Accepted content and history remain in `SvgAuthoringState`.
type SvgArtState =
    {
        Camera: SvgAffine
        Selection: string list
        GridSize: float
        SnapToGrid: bool
    }

type SvgGeometryPrepared =
    {
        Request: SvgGeometryRequest
        EncodedRequest: string
        InputVertexCount: int
    }

/// Bounded request sent to the optional polygon-clipping worker.
type SvgGeometryRequest =
    {
        OperationId: string
        AcceptedRevision: int
        InputContentHash: string
        Operation: PathOperation
        MaximumDeviation: float
        Subjects: Point list list
        Clips: Point list list
    }

type SvgGeometryResult =
    {
        OperationId: string
        AcceptedRevision: int
        InputContentHash: string
        Contours: Point list list
    }

[<RequireQualifiedAccess>]
module SvgArt =
    val align:
        alignment: SvgArtAlignment -> ids: string list -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

    val create:
        elementId: string ->
        primitive: SvgArtPrimitive ->
        presentation: SvgPresentation ->
        document: SvgDocument ->
            Result<SvgDocument, SvgArtError>

    val group: groupId: string -> ids: string list -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val guides: state: SvgArtState -> points: Point list -> Result<SvgArtGuide list, SvgArtError>
    val initialState: SvgArtState

    val insertPathPoint:
        elementId: string -> index: int -> point: Point -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

    val putGradient:
        id: string -> gradient: SvgGradientDefinition -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

    val removePathPoint: elementId: string -> index: int -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val reorder: id: string -> order: SvgArtSiblingOrder -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val replacePath: elementId: string -> path: PathSpec -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val rotate: ids: string list -> degrees: float -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val scale: ids: string list -> x: float -> y: float -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

    val setPresentation:
        ids: string list -> presentation: SvgPresentation -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

    /// Snap in document space with deterministic midpoint-away-from-zero rounding.
    val snapPoint: state: SvgArtState -> point: Point -> Result<Point, SvgArtError>
    val transform: ids: string list -> affine: SvgAffine -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val translate: ids: string list -> x: float -> y: float -> document: SvgDocument -> Result<SvgDocument, SvgArtError>
    val ungroup: groupId: string -> document: SvgDocument -> Result<SvgDocument, SvgArtError>

[<RequireQualifiedAccess>]
module SvgGeometry =
    val defaultMaximumDeviation: float
    val maximumSubdivisionDepth: int

    val prepare:
        operationId: string ->
        revision: int ->
        operation: PathOperation ->
        subjects: PathSpec list ->
        clips: PathSpec list ->
        deviation: float ->
            Result<SvgGeometryPrepared, SvgArtError>

    /// Validate identity and result budgets before producing one atomic replacement transaction.
    val transaction:
        result: SvgGeometryResult ->
        prepared: SvgGeometryPrepared ->
        document: SvgDocument ->
            Result<SvgAuthoringTransaction, SvgArtError>
