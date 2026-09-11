namespace FS.GG.UI.Scene.SvgBrowser

open System
open Browser.Types
open FS.GG.UI.Scene

/// Browser host dimensions and accessible name for one retained SVG root.
type SvgBrowserOptions =
    { Width: float
      Height: float
      AccessibleLabel: string
      WheelZoomFactor: float }

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
    { DefinitionId: string
      Family: string
      Ready: bool
      Diagnostic: string option }

/// A mounted identified SVG document. Validation and export finish before any DOM mutation.
[<Sealed>]
type SvgDocumentBrowserHost =
    interface IDisposable

    member Root: Element
    member MountNamespace: string
    member Document: SvgDocument
    member ExportedSvg: string
    /// Observe declared fonts without treating browser fallback as font-fidelity success.
    member ObserveFonts: unit -> SvgBrowserFontObservation list
    /// Validate and export the replacement fully before replacing the mounted root.
    member Replace: document: SvgDocument -> Result<unit, SvgDocumentBrowserError>

/// Observable resource and scene state for lifecycle and early-cost evidence.
type SvgBrowserObservation =
    { RootId: string
      Revision: int
      LayerCount: int
      ObjectCount: int
      SvgNodeCount: int
      OwnedListenerCount: int
      ScheduledFrameCount: int }

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
