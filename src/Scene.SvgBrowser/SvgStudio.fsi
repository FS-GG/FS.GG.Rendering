namespace FS.GG.UI.Scene.SvgBrowser

open System
open Browser.Types
open FS.GG.UI.Scene

/// Browser studio configuration. The worker factory must resolve `svg-geometry-worker.js` through the consumer bundler.
type SvgStudioOptions =
    {
        MountNamespace: string
        AccessibleLabel: string
        WorkerFactory: (unit -> obj) option
    }

type SvgStudioObservation =
    {
        Revision: int
        SelectionCount: int
        OwnedListenerCount: int
        ActiveGesture: string option
        WorkerInFlight: bool
    }

[<Sealed>]
type SvgFontResourceHost =
    interface IDisposable
    member Ready: bool
    member Diagnostic: string option

[<Sealed>]
type SvgGeometryWorkerHost =
    interface IDisposable
    member InFlight: bool

    /// Start the sole allowed request. The worker is terminated on every terminal path.
    member Start:
        prepared: SvgGeometryPrepared * onResult: (SvgGeometryResult -> unit) * onError: (string -> unit) ->
            Result<unit, string>

    member Cancel: unit -> unit

[<Sealed>]
type SvgStudioHost =
    interface IDisposable
    member Root: HTMLElement
    member State: SvgAuthoringState
    member ToolState: SvgArtState
    /// Portable mode, panel, focus and overlay state owned alongside authoring state.
    member WorkspaceState: SvgWorkspaceState
    /// Apply one pure workspace transition and return host effects for persistence/focus/input composition.
    member UpdateWorkspace: message: SvgWorkspaceMessage -> SvgWorkspaceEffect list
    member SetSelection: elementIds: string list -> Result<unit, SvgArtError>
    member SetCamera: camera: SvgAffine -> Result<unit, SvgArtError>
    /// Pick through the inverse current camera while retaining semantic element identity.
    member Pick: screenPoint: Point -> Result<string option, SvgArtError>
    /// Repeated calls with one id replace the gesture preview without adding history.
    member Preview: transaction: SvgAuthoringTransaction -> Result<unit, SvgAuthoringError>
    member CommitGesture: transactionId: string -> Result<unit, SvgAuthoringError>
    member CancelGesture: transactionId: string -> Result<unit, SvgAuthoringError>
    member Undo: unit -> Result<unit, SvgAuthoringError>
    member Redo: unit -> Result<unit, SvgAuthoringError>
    /// Commit a document produced by any portable `SvgArt` operation as one history entry.
    member CommitDocument: transactionId: string * candidate: SvgDocument -> Result<unit, SvgAuthoringError>
    /// Change grid/freeform placement in the same history as document, catalog and entity edits.
    member SetGrid: transactionId: string * grid: SvgSceneGrid option -> Result<unit, SvgAuthoringError>
    member ApplyNumericTransform: transactionId: string * transform: SvgAffine -> Result<unit, SvgAuthoringError>
    /// Validate and atomically commit a current packaged-worker Boolean result once.
    member CommitGeometry: prepared: SvgGeometryPrepared * result: SvgGeometryResult -> Result<unit, SvgArtError>
    member GeometryWorker: SvgGeometryWorkerHost option
    member Observe: unit -> SvgStudioObservation

[<RequireQualifiedAccess>]
module SvgStudio =
    /// Mount an explicit authoring entry with native controls and one retained document host. Portable `SvgArt` operations can be committed through the returned host.
    val mount:
        container: HTMLElement ->
        options: SvgStudioOptions ->
        initialState: SvgAuthoringState ->
        onChange: (SvgAuthoringState -> unit) ->
            Result<SvgStudioHost, SvgDocumentBrowserError>

    /// Verify exact bytes and rights before starting browser font activation; disposal removes the face and blob URL.
    val activateFont: resource: SvgFontResource -> Result<SvgFontResourceHost, SvgDocumentIssue list>
