namespace FS.GG.UI.Scene

/// SVG affine matrix in the standard six-value form:
/// x' = A*x + C*y + E; y' = B*x + D*y + F.
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
    val identity: SvgAffine
    val translate: x: float -> y: float -> SvgAffine
    val scale: x: float -> y: float -> SvgAffine
    val rotateDegrees: angle: float -> SvgAffine
    val skewXDegrees: angle: float -> SvgAffine
    val skewYDegrees: angle: float -> SvgAffine
    /// `compose parent local` applies `local` first, then `parent`.
    val compose: parent: SvgAffine -> local: SvgAffine -> SvgAffine
    val transformPoint: transform: SvgAffine -> point: Point -> Point
    val tryInverse: transform: SvgAffine -> Result<SvgAffine, SvgAffineError>
    val isFinite: transform: SvgAffine -> bool

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
    val schema: string
    val defaultLimits: SvgDocumentLimits
    val defaultPresentation: SvgPresentation
    /// Validate identity, references, finite values and profile limits before browser mutation.
    val validate: serializedByteCount: int -> limits: SvgDocumentLimits -> document: SvgDocument -> SvgDocumentIssue list
    /// Checked compatibility adapter for the original retained foundation contract.
    val ofRetainedScene: viewBox: Rect -> scene: RetainedScene -> Result<SvgDocument, SvgDocumentIssue list>
