namespace FS.GG.UI.Scene.SvgBrowser

open System
open Browser.Types
open FS.GG.UI.Scene

/// Browser studio configuration. The worker factory must resolve `svg-geometry-worker.js` through the consumer bundler.
type SvgStudioOptions =
    { MountNamespace: string
      AccessibleLabel: string
      WorkerFactory: (unit -> obj) option }

type SvgStudioObservation =
    { Revision: int
      SelectionCount: int
      OwnedListenerCount: int
      ActiveGesture: string option
      WorkerInFlight: bool }

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
    member Start: prepared: SvgGeometryPrepared * onResult: (SvgGeometryResult -> unit) * onError: (string -> unit) -> Result<unit, string>
    member Cancel: unit -> unit

[<Sealed>]
type SvgStudioHost =
    interface IDisposable
    member Root: HTMLElement
    member State: SvgAuthoringState
    member ToolState: SvgArtState
    member SetSelection: elementIds: string list -> Result<unit, SvgArtError>
    member SetCamera: camera: SvgAffine -> Result<unit, SvgArtError>
    /// Repeated calls with one id replace the gesture preview without adding history.
    member Preview: transaction: SvgAuthoringTransaction -> Result<unit, SvgAuthoringError>
    member CommitGesture: transactionId: string -> Result<unit, SvgAuthoringError>
    member CancelGesture: transactionId: string -> Result<unit, SvgAuthoringError>
    member Undo: unit -> Result<unit, SvgAuthoringError>
    member Redo: unit -> Result<unit, SvgAuthoringError>
    member ApplyNumericTransform: transactionId: string * transform: SvgAffine -> Result<unit, SvgAuthoringError>
    member GeometryWorker: SvgGeometryWorkerHost option
    member Observe: unit -> SvgStudioObservation

[<RequireQualifiedAccess>]
module SvgStudio =
    /// Mount an explicit authoring entry with native controls and one retained document host.
    val mount: container: HTMLElement -> options: SvgStudioOptions -> initialState: SvgAuthoringState -> onChange: (SvgAuthoringState -> unit) -> Result<SvgStudioHost, SvgDocumentBrowserError>
    /// Verify exact bytes and rights before starting browser font activation; disposal removes the face and blob URL.
    val activateFont: resource: SvgFontResource -> Result<SvgFontResourceHost, SvgDocumentIssue list>
