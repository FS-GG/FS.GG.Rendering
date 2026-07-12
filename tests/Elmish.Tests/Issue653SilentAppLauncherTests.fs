module Issue653SilentAppLauncherTests

// Issue #653 — `ControlsElmish.runInteractiveApp` is the app family's SILENT launcher: the twin of
// `runInteractiveAppWithAudio` with no sink threaded in. The shipped `fs-gg-audio` skill teaches it as
// one of the six audio-launch symbols (FS.GG.Game#240 added the silent twins to the canonical compiled
// block), and nothing in `tests/` called it — so `skill-parity` reported `unexercised-api-symbol`,
// "the seam may be dead". It was right for the reason that makes this class of gap hard to see: the
// symbol is named in a dozen test COMMENTS, describing the loop other seams stand in for, and called by
// none of them. `skill-parity` strips comments and string literals before it looks (`SkillParity.fs`,
// `loadExercisedSymbols`), which is exactly why it caught what a grep would have called covered.
//
// The live launch is GL-bound and not drivable headless (the limitation #365/#396 record), so the seam
// is pinned where it CAN be observed: the unsupported-host path, which is the same path its audio twin
// is pinned on (`Issue429ControlsAudioTests`). What is asserted is the invariant the skill actually
// claims — the two launchers "differ only in the discarded sink" — by running BOTH over the same host
// and the same options and requiring their outcomes to agree.
//
// That claim was previously made in a COMMENT and only half-tested: Issue429's own case asserts the
// audio twin "classifies an unsupported host exactly as runInteractiveApp does" while never calling
// `runInteractiveApp` to check. This file calls the silent side of it.
//
// The `PersistentWindow` guard is not ceremony. `runInteractiveApp` on a host that CAN open a window
// opens a persistent one and runs until the user closes it — an unguarded call here would hang the
// suite on any developer machine with a display.

open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = StartPressed

type private Model = { Started: bool }

let private music = TrackId "menu-theme"
let private click = SoundId "ui-click"

/// A host that WANTS sound — menu music at init, a click on press. That is the realistic subject: the
/// silent launcher exists precisely for a product built WITH audio that is then launched WITHOUT a sink.
///
/// Its `Init`/`Update`/`View` are NOT reached on the path below, and this file does not pretend they
/// are: the capability gate rejects an unsupported host before the loop ever calls `Init`
/// (`SkiaViewer.fs` — `runtimeCapability()` is checked first, `init` only in the `else`). So no audio is
/// ever requested here, and an "assert the sink stayed empty" would hold for ANY host, sound or silent —
/// it would be a fact that cannot fail dressed up as evidence. What IS under test is the launch outcome,
/// which is observable and which the two launchers must agree on.
let private menuHost: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Started = false }, [ PlayAudio [ Audio.playMusic music true ] ]
      Update = fun StartPressed model -> { model with Started = true }, [ PlayAudio [ Audio.playSfx click 1.0 ] ]
      View =
        fun _ _ ->
            Stack.create
                [ Stack.children
                      [ Button.create [ Button.text "Start"; Button.onClick StartPressed ]
                        |> Control.withKey "start" ] ]
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private options: ViewerOptions =
    { Title = "Menu"
      InitialSize = { Width = 640; Height = 480 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = None }

[<Tests>]
let tests =
    testList
        "issue-653 silent app launcher"
        [
          // The seam is not dead: the symbol the skill teaches resolves, launches, and reports a
          // coherent failure rather than throwing — the minimum a published launcher must do.
          test "runInteractiveApp reports an unsupported host instead of pretending to launch" {
            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the live launch would run until user close"
            else
                match ControlsElmish.runInteractiveApp options menuHost with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "the silent launcher classifies an unsupported host as an environment failure"

                    Expect.equal failure.BlockedStage Window "an unsupported host is blocked before window lifecycle"
          }

          // The invariant the skill states, and the one #429 asserted only in prose: the silent launcher
          // and the audio twin are the SAME code path (`runInteractiveAppWithLauncher`) up to the
          // terminal viewer launcher, so over one host and one set of options they cannot disagree.
          test "the silent launcher and its audio twin agree on the same host, sink aside" {
            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the live launch would run until user close"
            else
                // The sink is `ignore` deliberately: it is never called on this path (the launch is
                // refused before `Init`), so capturing into a list and asserting it empty would assert
                // nothing. The claim under test is that the OUTCOMES agree.
                let silent = ControlsElmish.runInteractiveApp options menuHost
                let sounded = ControlsElmish.runInteractiveAppWithAudio options ignore menuHost

                match silent, sounded with
                | Result.Error silentFailure, Result.Error soundedFailure ->
                    Expect.equal
                        silentFailure.Classification
                        soundedFailure.Classification
                        "the two launchers classify one unsupported host identically"

                    Expect.equal
                        silentFailure.BlockedStage
                        soundedFailure.BlockedStage
                        "the two launchers are blocked at the same stage"
                | _ -> failtest "an unsupported host cannot report a successful launch on either launcher"
          } ]
