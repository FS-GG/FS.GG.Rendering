/// The MVU host bridge: assembles the pure `init`/`update`/`Shell.view` into the
/// framework's `InteractiveAppHost`, used by both the App edge (interactive + evidence)
/// and the test project (deterministic `Perf.runScript`). Effects are always empty — all
/// I/O happens at the App edge (Principle IV).
module AntShowcase.Core.Host

open FS.GG.UI.Scene
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.KeyboardInput
open AntShowcase.Core.Model

/// The seeded starting model: first family page, antLight, seeded demo state.
let initModel: AntShowcaseModel =
    { CurrentPage = (List.head PageRegistry.all).Id
      Mode = Light
      PageState = DemoState.seed
      FeedbackDraft = ""
      Feedback = [] }

/// Map a key press to a showcase message (interaction-contract.md). Activation keys
/// (Enter/Space) exercise the focused command (FR-014) — enough to make a seeded keyboard
/// script produce a visible state change. Key-up is ignored (only pressed transitions are
/// mapped), keeping seeded scripts minimal and deterministic. Pointer activation needs no
/// `MapPointer`: a hit control's authored `onClick`/`onChanged` bindings are dispatched
/// directly by `runInteractiveApp` (feature 090), so `MapPointer` stays inert.
let mapKey (key: ViewerKey) (pressed: bool): AntShowcaseMsg option =
    if not pressed then
        None
    else
        match key with
        | Enter
        | Space -> Some(PageMsg ButtonClicked)
        | _ -> None

/// Build the host for a given initial mode. `Theme` is resolved from the mode (R3); the
/// runtime app-bar toggle flips `model.Mode` (reflected in the status strip + re-themed
/// tree where the host re-resolves), and `Theme` is the antLight/antDark variant for the
/// launch mode.
let create (mode: ThemeMode): InteractiveAppHost<AntShowcaseModel, AntShowcaseMsg> =
    { Init = fun () -> { initModel with Mode = mode }, []
      Update = fun msg model -> Model.update msg model, []
      View = fun size model -> Shell.view size model
      Theme = AntTheme.resolve mode
      MapKey = mapKey
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// The default host (antLight).
let defaultHost: InteractiveAppHost<AntShowcaseModel, AntShowcaseMsg> = create Light
