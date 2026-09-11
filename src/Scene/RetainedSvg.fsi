namespace FS.GG.UI.Scene

/// Stable camera transform applied as screen = pan + zoom * scene.
type SvgCamera =
    { PanX: float
      PanY: float
      Zoom: float }

/// One semantic object backed by the existing Scene vocabulary.
type SemanticSceneObject =
    { Id: string
      Selectable: bool
      AccessibleLabel: string
      Content: Scene }

/// One ordered, identity-stable retained layer.
type RetainedSceneLayer =
    { Id: string
      Visible: bool
      Objects: SemanticSceneObject list }

/// Retained scene metadata layered over the existing Scene vocabulary.
type RetainedScene =
    { RootId: string
      Revision: int
      Camera: SvgCamera
      Layers: RetainedSceneLayer list }

/// Location and explanation for a scene value that cannot enter the first SVG subset.
type SvgAdapterIssue =
    { ObjectId: string option
      NodePath: int list
      Reason: string }

/// Explicit outcome from the portable half of the SVG adapter.
[<RequireQualifiedAccess>]
type SvgAdapterResult =
    | Rendered of RetainedScene
    | Unsupported of SvgAdapterIssue list
    | Invalid of SvgAdapterIssue list

/// Commands interpreted by the shared retained interaction reducer.
[<RequireQualifiedAccess>]
type RetainedInteractionMessage =
    | ReplaceScene of RetainedScene
    | Select of expectedRevision: int * objectId: string
    | ClearSelection of expectedRevision: int
    | FocusNext of expectedRevision: int
    | FocusPrevious of expectedRevision: int
    | SetCamera of expectedRevision: int * camera: SvgCamera
    | CapturePointer of expectedRevision: int * pointerId: int
    | ReleasePointer of expectedRevision: int * pointerId: int

/// Why an interaction command was rejected without changing state.
[<RequireQualifiedAccess>]
type RetainedInteractionError =
    | StaleRevision of expected: int * actual: int
    | NonIncreasingRevision of candidate: int * actual: int
    | UnknownObject of string
    | ObjectNotSelectable of string
    | InvalidCamera
    | InvalidScene of SvgAdapterIssue list
    | UnsupportedScene of SvgAdapterIssue list
    | PointerNotCaptured of int

/// Shared interaction state, independent of browser events and game commands.
type RetainedInteractionState =
    { Scene: RetainedScene
      SelectedObjectId: string option
      FocusedObjectId: string option
      CapturedPointerId: int option }

/// A reducer result always returns the resulting state; rejected commands leave it unchanged.
type RetainedInteractionResult =
    { State: RetainedInteractionState
      Error: RetainedInteractionError option }

[<RequireQualifiedAccess>]
module SvgRetained =
    /// Validate the selected SVG subset without changing identities or transforms.
    val project: RetainedScene -> SvgAdapterResult

    /// Apply the documented camera transform.
    val toScreenPoint: SvgCamera -> Point -> Point

    /// Invert the documented camera transform when the camera is valid.
    val tryToScenePoint: SvgCamera -> Point -> Point option

    /// Initialize reducer state from a supported retained scene.
    val tryCreateInteraction: RetainedScene -> Result<RetainedInteractionState, RetainedInteractionError>

    /// Apply one atomic interaction transition.
    val update: RetainedInteractionMessage -> RetainedInteractionState -> RetainedInteractionResult
