module AppRoot.Program

open System
open AppRoot.Model
open AppRoot.View
open AppRoot.LayoutEvidence
//#if (profile == "governed" || profile == "headless-scene")

type Model = AppRoot.Model.Model
type Msg = AppRoot.Model.Msg
let initialModel = AppRoot.Model.initialModel
let update = AppRoot.Model.update
let view = AppRoot.View.view
let layoutEvidenceForSize = AppRoot.LayoutEvidence.layoutEvidenceForSize

[<EntryPoint>]
let main args =
    match AppRoot.EvidenceCommands.tryRunEvidenceCommand (List.ofArray args) with
    | Some exitCode -> exitCode
    | None ->
        printfn "status=ok mode=headless-scene command=dotnet-run scene-nodes=1"
        0

//#else
open FS.GG.UI.SkiaViewer
open System.IO
open AppRoot.WindowOptions
//#if (profile == "app" || profile == "game")
open FS.GG.UI.Controls.Elmish
//#endif

type Model = AppRoot.Model.Model
//#if (profile == "app" || profile == "sample-pack")
type Page = AppRoot.Model.Page
type InputFlowDiagnostic = AppRoot.Model.InputFlowDiagnostic
//#endif
type Msg = AppRoot.Model.Msg
type GeneratedLayoutValidationFailureClass = AppRoot.Model.GeneratedLayoutValidationFailureClass
type GeneratedLayoutValidationResult = AppRoot.Model.GeneratedLayoutValidationResult
type WindowBehaviorSettings = AppRoot.WindowOptions.WindowBehaviorSettings

let initialModel = AppRoot.Model.initialModel
//#if (profile == "app" || profile == "sample-pack")
let pageName = AppRoot.Model.pageName
//#endif
let keyName = AppRoot.Model.keyName
//#if (profile == "app" || profile == "sample-pack")
let diagnostic = AppRoot.Model.diagnostic
let transitionViewerInput = AppRoot.Model.transitionViewerInput
let dispatchViewerKey = AppRoot.Model.dispatchViewerKey
let visibleRows = AppRoot.View.visibleRows
//#endif
let init = AppRoot.Model.init
let update = AppRoot.Model.update
let subscriptions = AppRoot.Model.subscriptions
//#if (profile == "app" || profile == "sample-pack")
let controlsExampleView = AppRoot.View.controlsExampleView
let adapterProgram = AppRoot.View.adapterProgram
//#endif
let hudRegionForSize = AppRoot.LayoutEvidence.hudRegionForSize
let gameplayRegionForSize = AppRoot.LayoutEvidence.gameplayRegionForSize
let boundsInside = AppRoot.LayoutEvidence.boundsInside
let activeGameplayBoundsForSize = AppRoot.LayoutEvidence.activeGameplayBoundsForSize
let movementUsesGameplayRegion = AppRoot.LayoutEvidence.movementUsesGameplayRegion
let spawnUsesGameplayRegion = AppRoot.LayoutEvidence.spawnUsesGameplayRegion
let collisionUsesGameplayRegion = AppRoot.LayoutEvidence.collisionUsesGameplayRegion
let layoutEvidenceForSize = AppRoot.LayoutEvidence.layoutEvidenceForSize
let validateGeneratedLayout = AppRoot.LayoutEvidence.validateGeneratedLayout
let view = AppRoot.View.view
let mapKey = AppRoot.EvidenceCommands.mapKey
let tick = AppRoot.EvidenceCommands.tick
let viewerOptions = AppRoot.EvidenceCommands.viewerOptions
let appCommandName = AppRoot.EvidenceCommands.appCommandName
let viewerEffectsForModel = AppRoot.EvidenceCommands.viewerEffectsForModel
let interpretAtHostBoundary = AppRoot.EvidenceCommands.interpretAtHostBoundary
let generatedHost = AppRoot.EvidenceCommands.generatedHost
//#if (profile == "app" || profile == "game")
let interactiveHost = AppRoot.EvidenceCommands.interactiveHost
//#endif
let defaultCommand = AppRoot.EvidenceCommands.defaultCommand
let windowBehaviorArgsFromFile = AppRoot.WindowOptions.windowBehaviorArgsFromFile
let parseWindowBehavior = AppRoot.WindowOptions.parseWindowBehavior
let toViewerWindowBehavior = AppRoot.WindowOptions.toViewerWindowBehavior
let windowOptionStatusText = AppRoot.WindowOptions.windowOptionStatusText
let manualWindowOptionResults = AppRoot.WindowOptions.manualWindowOptionResults
let windowOptionsReport = AppRoot.WindowOptions.windowOptionsReport

[<EntryPoint>]
let main args =
    match AppRoot.EvidenceCommands.tryRunEvidenceCommand (List.ofArray args) with
    | Some exitCode -> exitCode
    | None ->
        let args = List.ofArray args
        let windowBehavior = parseWindowBehavior args
        let windowBehaviorRequest = toViewerWindowBehavior windowBehavior
        let capability = Viewer.runtimeCapability()
        let desktopSessionDiagnosticApi = "Viewer.desktopSessionDiagnostic()"

        let optional value =
            value |> Option.defaultValue "none"

        let envOption name =
            match Environment.GetEnvironmentVariable name with
            | null -> None
            | value when String.IsNullOrWhiteSpace value -> None
            | value -> Some value

        let runtimeDirectory = envOption "XDG_RUNTIME_DIR"
        let runtimeDirectoryExists = runtimeDirectory |> Option.exists Directory.Exists
        let waylandDisplay = envOption "WAYLAND_DISPLAY"
        let x11Display = envOption "DISPLAY"

        let displayVariable =
            match waylandDisplay, x11Display with
            | Some value, _ -> Some $"WAYLAND_DISPLAY={value}"
            | None, Some value -> Some $"DISPLAY={value}"
            | None, None -> None

        let displaySocket =
            if runtimeDirectory.IsSome && waylandDisplay.IsSome then
                Some(Path.Combine(runtimeDirectory.Value, waylandDisplay.Value))
            elif x11Display.IsSome then
                let display = x11Display.Value
                let number = display.TrimStart(':').Split('.').[0]
                Some($"/tmp/.X11-unix/X{number}")
            else
                None

        let displaySocketExists = displaySocket |> Option.exists File.Exists
        let sessionBus = envOption "DBUS_SESSION_BUS_ADDRESS"

        let diagnosticClass, desktopMessage =
            if runtimeDirectory.IsNone || displayVariable.IsNone || (displaySocket.IsSome && not displaySocketExists) then
                "unsupported-host", "Desktop session prerequisites are missing before app lifecycle debugging."
            else
                "environment-session-ready", "Desktop session prerequisites are present."

        let missingPackageCapability =
            if List.isEmpty capability.MissingPackageCapabilities then
                "none"
            else
                String.concat "," capability.MissingPackageCapabilities

        let unsupportedHostReasons =
            if List.isEmpty capability.UnsupportedHostReasons then
                "none"
            else
                String.concat "|" capability.UnsupportedHostReasons

        let fallbackFullDesktopSession = "fallback-is-full-desktop-session=false"

        let windowOptionResults =
            manualWindowOptionResults windowBehaviorRequest

        let windowOptionSummary =
            windowOptionResults
            |> List.map (fun (option, _, _, status, _) -> $"{option}:{windowOptionStatusText status}")
            |> String.concat ","

        // Issue #245 (game) / #429 (the Controls seam) / #436 (this line reaching every profile that
        // opens a window). `OpenAlBackend.create` opens a real device and degrades to the record-only
        // Null backend when OpenAL or the device is unavailable, so this is safe headless and in CI:
        // it never throws into product code. `Audio.play backend` is the `AudioEffect list -> unit`
        // sink the viewer hands every `PlayAudio` batch, in dispatch order. Nothing else in the
        // product knows a device exists.
        //
        // It is hoisted ABOVE the family branch on purpose: the sink is identical for every family,
        // and the only thing that differs is which launch entry point takes it. Keeping one
        // construction site is what stops a profile from quietly acquiring the packages and a cue
        // seam while still launching through a sinkless overload — which is the exact shape of the
        // defect #436 fixed (`app` could not name `AudioEffect`; `sample-pack` referenced all four
        // FS.GG.Audio packages and then launched via `Viewer.runApp`, wiring none of them).
        use audioBackend = FS.GG.Audio.Host.OpenAlBackend.create AppRoot.AudioCues.resolver
        let audioSink = FS.GG.Audio.Host.Audio.play audioBackend

        // Per-family governed default launch (feature 086, FR-004/005/006, D6):
        //#if (profile == "app" || profile == "game")
        // POINTER-HOST family (CONTROLS + GAME): a pointer-aware persistent host — a mouse click on a
        // live control dispatches that control's bound message (via MapPointer over the renderTree
        // bounds). The GAME family joins it here (#991/#1000): its turnkey default boots the generic
        // game shell (main menu / Esc pause / settings + rebinding), and a clickable menu needs the
        // pointer host — the keyboard-only `generatedHost` cannot drive the menu buttons. A window flag
        // (e.g. --window-startup normal) threads the parsed behavior into the ACTUAL live launch
        // (feature 122, FR-005), so the scaffold-map remedy is effective instead of inert; with no flag
        // the default windowed-fullscreen path is preserved.
        //
        // #429/#436: both overloads carry the audio sink. `runInteractiveAppWithAudio` is the same
        // message->update->retained-step code path as the sinkless `runInteractiveApp` with the terminal
        // viewer launcher swapped, so sound cannot drift away from the live loop.
        let launchResult =
            if AppRoot.WindowOptions.windowFlagSupplied args then
                ControlsElmish.runInteractiveAppWithWindowBehaviorAndAudio viewerOptions (AppRoot.WindowOptions.toViewerLaunchRequest windowBehavior) audioSink interactiveHost
            else
                ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost
        //#else
        // SAMPLE-PACK family: the keyboard-only persistent host is preserved (FR-006). A window flag
        // routes through the window-behavior overload; otherwise the durable default path stays
        // reachable and inherits the framework windowed-fullscreen default.
        //
        // #436 folded sample-pack onto this sink-carrying expression rather than leaving it on the
        // sinkless `Viewer.runApp`. It had referenced all four FS.GG.Audio packages since ADR-0024 and
        // wired none of them — shipping the dependency and the silence together.
        let launchResult =
            if AppRoot.WindowOptions.windowFlagSupplied args then
                Viewer.runAppWithWindowBehaviorAndAudio viewerOptions (AppRoot.WindowOptions.toViewerLaunchRequest windowBehavior) audioSink generatedHost
            else
                Viewer.runAppWithAudio viewerOptions audioSink generatedHost
        //#endif

        match launchResult with
        | Result.Ok outcome ->
            let inputDispatchStatus =
                match $"%A{outcome.InputDispatch}" with
                | "Verified"
                | "true" -> "verified"
                | "NotVerified"
                | "false" -> "not-verified"
                | value -> value.ToLowerInvariant()

            printfn "status=%s mode=%s command=%s window-opened=%b window-visible=observed:true accessible-window=true first-frame-presented=%b user-close-observed=%b self-closed-for-evidence=%b input-dispatch=%s exit-path=%b renderer-mode=%s blocked-stage=none classification=none category=none window-options=%s missing-package-capability=%s unsupported-host-reasons=%s diagnostic-api=%s diagnostic-class=%s runtime-directory=%s runtime-directory-exists=%b display-variable=%s display-socket-exists=%b session-bus=%s %s message=%s desktop-message=%s" outcome.Status outcome.Mode defaultCommand outcome.WindowOpened outcome.FirstFramePresented outcome.UserCloseObserved outcome.SelfClosedForEvidence inputDispatchStatus outcome.ExitPath outcome.RendererMode windowOptionSummary missingPackageCapability unsupportedHostReasons desktopSessionDiagnosticApi diagnosticClass (optional runtimeDirectory) runtimeDirectoryExists (optional displayVariable) displaySocketExists (optional sessionBus) fallbackFullDesktopSession outcome.Message desktopMessage
            0
        | Result.Error (failure: ViewerRunFailure) ->
            printfn "status=%s mode=interactive-window command=%s window-visible=unsupported accessible-window=false blocked-stage=%A classification=%A category=%A window-options=%s missing-package-capability=%s unsupported-host-reasons=%s diagnostic-api=%s diagnostic-class=%s runtime-directory=%s runtime-directory-exists=%b display-variable=%s display-socket-exists=%b session-bus=%s %s message=%s desktop-message=%s" (if failure.Classification = UnsupportedEnvironment then "unsupported" else "failed") defaultCommand failure.BlockedStage failure.Classification failure.DiagnosticCategory windowOptionSummary missingPackageCapability unsupportedHostReasons desktopSessionDiagnosticApi diagnosticClass (optional runtimeDirectory) runtimeDirectoryExists (optional displayVariable) displaySocketExists (optional sessionBus) fallbackFullDesktopSession failure.Message desktopMessage
            if failure.Classification = UnsupportedEnvironment then 0 else 1
//#endif
