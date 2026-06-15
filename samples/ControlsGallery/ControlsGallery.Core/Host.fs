/// The MVU host bridge: assembles the pure `init`/`update`/`Shell.view` into the
/// framework's `InteractiveAppHost`, used by both the App edge (interactive + evidence)
/// and the test project (deterministic `Perf.runScript`). Effects are always empty —
/// all I/O happens at the App edge (Principle IV).
module ControlsGallery.Core.Host

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default.Theming
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.KeyboardInput
open ControlsGallery.Core.Model
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

/// The seeded starting model: first page, Light, indigo accent, seeded demo state.
let initModel: GalleryModel =
    { CurrentPage = (List.head Pages.all).Id
      Mode = Light
      Accent = GalleryTheme.indigo
      PageState = DemoState.seed }

/// Map a key press to a gallery message. Activation keys exercise the focused command
/// (FR-012) — enough to make a seeded keyboard script produce a visible state change.
let mapKey (key: ViewerKey) (pressed: bool): GalleryMsg option =
    if not pressed then
        None
    else
        match key with
        | Enter
        | Space -> Some(PageMsg ButtonClicked)
        | _ -> None

/// Build the host for a given initial mode + accent.
let create (mode: ThemeMode) (accent: Color): InteractiveAppHost<GalleryModel, GalleryMsg> =
    { Init = fun () -> { initModel with Mode = mode; Accent = accent }, []
      Update = fun msg model -> Model.update msg model, []
      View = fun size model -> Shell.view size model
      Theme = GalleryTheme.resolve mode accent
      MapKey = mapKey
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// The default host (Light + indigo).
let defaultHost: InteractiveAppHost<GalleryModel, GalleryMsg> =
    create Light GalleryTheme.indigo
