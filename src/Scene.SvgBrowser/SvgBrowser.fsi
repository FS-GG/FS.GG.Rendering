namespace FS.GG.UI.Scene.SvgBrowser

open System
open Browser.Types
open FS.GG.UI.Scene

/// Browser host dimensions and accessible name for one retained SVG root.
type SvgBrowserOptions =
    {
        Width: float
        Height: float
        AccessibleLabel: string
        WheelZoomFactor: float
    }

/// Why a retained browser root could not be mounted.
[<RequireQualifiedAccess>]
type SvgBrowserMountError =
    | InvalidScene of RetainedInteractionError
    | InvalidOptions of string
    | InvalidDocument of SvgDocumentIssue list

/// Why an identified SVG document could not be mounted or replaced.
[<RequireQualifiedAccess>]
type SvgDocumentBrowserError =
    | InvalidDocument of SvgDocumentIssue list
    | DuplicateMountNamespace of string

/// Browser-observed state for one declared local font.
type SvgBrowserFontObservation =
    {
        DefinitionId: string
        Family: string
        Ready: bool
        Diagnostic: string option
    }

/// A mounted identified SVG document. Validation and export finish before any DOM mutation.
[<Sealed>]
type SvgDocumentBrowserHost =
    interface IDisposable

    member Root: Element
    member MountNamespace: string
    member Document: SvgDocument
    member ExportedSvg: string
    /// Resolve the topmost semantic element using the browser's SVG paint, transform, clip and mask semantics.
    member HitTest: screenPoint: Point -> string option
    member internal HitTestBounds: screenPoint: Point -> string option
    /// Observe declared fonts without treating browser fallback as font-fidelity success.
    member ObserveFonts: unit -> SvgBrowserFontObservation list
    /// Validate and export the replacement fully before replacing the mounted root.
    member Replace: document: SvgDocument -> Result<unit, SvgDocumentBrowserError>

/// Observable resource and scene state for lifecycle and early-cost evidence.
type SvgBrowserObservation =
    {
        RootId: string
        Revision: int
        LayerCount: int
        ObjectCount: int
        SvgNodeCount: int
        OwnedListenerCount: int
        ScheduledFrameCount: int
    }

/// The scene-derived half of retained interaction synchronization, computed in one pass per synchronization.
module internal InteractionSync =
    type Projection =
        {
            /// Selectable objects of every layer by identity; the first selectable object wins an identity.
            Selectable: System.Collections.Generic.Dictionary<string, SemanticSceneObject>
            /// Selectable objects of visible layers, in layer then object order: the HTML control order.
            Controls: SemanticSceneObject list
            /// The accessible label of the selected selectable object, or "No object selected".
            SelectedLabel: string
            /// Scene objects examined to build this projection (an operation count, not a time).
            ObjectsVisited: int
        }

    val project: state: RetainedInteractionState -> Projection
/// What a retained-host transition invalidates, so the host reconciles only the DOM it can have changed.
module internal Invalidation =
    type Dependencies =
        {
            /// Root identity, scene revision or the layer/object structure changed.
            Scene: bool
            /// Only the camera differs.
            Camera: bool
            /// Selection or focus differs.
            Interaction: bool
        }

    val classify: before: RetainedInteractionState -> after: RetainedInteractionState -> Dependencies

/// Mapping between browser client coordinates and local SVG screen coordinates for one measured frame.
module internal PointerMapping =
    type Frame =
        {
            Left: float
            Top: float
            Width: float
            Height: float
        }

    val toLocal: frame: Frame -> options: SvgBrowserOptions -> clientX: float -> clientY: float -> Point
    val toClientX: frame: Frame -> options: SvgBrowserOptions -> screenX: float -> float
    val toClientY: frame: Frame -> options: SvgBrowserOptions -> screenY: float -> float

/// A mounted retained SVG root. All state changes pass through the portable reducer.
[<Sealed>]
type SvgBrowserHost =
    interface IDisposable

    /// The persistent SVG root owned by this host.
    member Root: Element

    /// Current portable interaction state.
    member State: RetainedInteractionState

    /// Apply one portable reducer message and reconcile accepted state into the retained DOM.
    member Dispatch: RetainedInteractionMessage -> RetainedInteractionResult

    /// Resolve an object from a point in local SVG screen coordinates using the inverse camera transform.
    member HitTest: screenPoint: Point -> string option

    /// Pan the current camera by local SVG screen-coordinate deltas.
    member PanBy: delta: Point -> RetainedInteractionResult

    /// Set zoom while keeping the scene point under the supplied local SVG screen coordinate anchored.
    member ZoomAt: anchorScreen: Point * zoom: float -> RetainedInteractionResult

    /// Read bounded scene and owned-resource counts without scheduling work.
    member Observe: unit -> SvgBrowserObservation

[<RequireQualifiedAccess>]
module SvgBrowser =
    /// Validate and mount one retained SVG root into the supplied container.
    val mount:
        container: HTMLElement ->
        options: SvgBrowserOptions ->
        scene: RetainedScene ->
        onTransition: (RetainedInteractionResult -> unit) ->
            Result<SvgBrowserHost, SvgBrowserMountError>

    /// Mount an identified document under a unique namespace using its complete exported SVG.
    val mountDocument:
        container: HTMLElement ->
        mountNamespace: string ->
        document: SvgDocument ->
            Result<SvgDocumentBrowserHost, SvgDocumentBrowserError>
