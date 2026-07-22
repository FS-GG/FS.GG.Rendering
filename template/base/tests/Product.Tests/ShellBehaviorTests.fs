module AppRootShellBehaviorTests

//#if (profile == "app" || profile == "game")
// ================================================================================================
// Release-lane BEHAVIOUR coverage for the generic game shell (FS.GG.Rendering#1002, child of #991).
//
// PR-time coverage of `GameShell.fs` is compile-only (the #366 app-profile probe proves the module
// builds). This file adds the missing thing: it DRIVES the shell's pure `update` and its host-free
// seams and asserts what they do. The shell `update` is a total, deterministic Elmish reducer with
// no clock, no IO and no randomness, so a scripted `Msg` sequence replays identically — exactly the
// shape the release/generated-product lane can run headless on every profile that ships the shell.
//
// PROFILE GATE. The shell module (`AppRoot.GameShell`) is emitted on the `app` and `game` profiles
// only (see Product.fsproj's `//#if (profile == "app" || profile == "game")` around GameShell.fs),
// so this whole file is gated the same way — on every other profile it compiles to an empty module
// and adds nothing, the pattern CoverageGateTests.fs uses.
//
// RELEASE-SAFE. Everything asserted is reached through the shell's OWN public surface plus the
// `Keymap`/`KeymapCodec` API the pinned `FsGgUiVersion` already ships (GameShell.fs itself calls
// them), so this compiles against the pin with no unreleased dotted API — the doc-vs-pin gate has
// nothing to reject.
//
// ADDITIVE ONLY. This does NOT touch the durable model-agnostic pins in GovernanceTests.fs or the
// replaceable scaffold-model pins in BehaviorTests.fs (the keyboard-only generatedHost + the Pong
// model). It only ADDS shell-behaviour tests beside them.
// ================================================================================================

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open AppRoot.GameShell

// ---- fixtures -------------------------------------------------------------------------------

let private jump: CommandId = "jump"
let private fire: CommandId = "fire"

// KeyIds are built through the same `ViewerKeyboard.toKeyId` seam the shell resolves keys through,
// so a test hands the shell the SAME KeyId the live `routeKeyDown` would derive from a raw key.
let private wKey: KeyId = ViewerKeyboard.toKeyId (Letter 'W')
let private fKey: KeyId = ViewerKeyboard.toKeyId (Letter 'F')
let private qKey: KeyId = ViewerKeyboard.toKeyId (Letter 'Q') // an unbound key a rebind can capture

let private res720: FS.GG.UI.Scene.Size = { Width = 1280; Height = 720 }
let private res1080: FS.GG.UI.Scene.Size = { Width = 1920; Height = 1080 }

let private testConfig: Config =
    { Title = "Test Game"
      Actions =
        [ { Command = jump; Label = "Jump"; Order = 10; Binding = None; DefaultBinding = Some wKey }
          { Command = fire; Label = "Fire"; Order = 20; Binding = None; DefaultBinding = Some fKey } ]
      DisplayModes = [ Windowed; Borderless; Fullscreen ]
      Resolutions = [ res720; res1080 ]
      InitialDisplay = { Resolution = res720; Mode = Windowed } }

// Effect list is inspected by projection, never by structural equality: `Effect` carries a `Keymap`
// (an abstract type) in `KeymapChanged`, so an `Expect.equal` over an `Effect list` need not even
// compile. Project to the payloads and assert on those.
let private displayEffects effects =
    effects |> List.choose (function DisplayChanged d -> Some d | _ -> None)

let private keymapEffects effects =
    effects |> List.choose (function KeymapChanged k -> Some k | _ -> None)

let private hasExit effects =
    effects |> List.exists (function ExitRequested -> true | _ -> false)

/// A model parked on a given screen, reached only through the shell's own transitions (never by
/// hand-mutating `Screen`) so the fixture itself exercises the router.
let private atPlaying () = fst (update Start (init testConfig))
let private atPaused () = fst (update PauseGame (atPlaying ()))
let private atSettingsFromMenu () = fst (update OpenSettings (init testConfig))

// ---- router + menu activation ---------------------------------------------------------------

[<Tests>]
let shellRouterTests =
    testList "game-shell router + menu (#991/#1002)" [
        test "init parks on the main menu with the game's keymap and display" {
            let m = init testConfig
            Expect.equal m.Screen MainMenu "the shell launches on the main menu (the launch screen IS the menu)"
            Expect.equal m.Rebinding None "no rebind is in flight at launch"
            Expect.equal m.SettingsReturn MainMenu "settings returns to the menu until opened from a pause"
            Expect.equal m.Display testConfig.InitialDisplay "the shell starts in the game's initial display"
            Expect.equal (Keymap.resolve m.Keymap wKey) (Some jump) "the shell starts with the game's default bindings"
        }

        test "main menu: Start enters play, Config opens settings, Exit requests host exit" {
            let m0 = init testConfig

            let started, startFx = update Start m0
            Expect.equal started.Screen Playing "Start routes the main menu into play"
            Expect.isEmpty startFx "entering play is a pure screen transition, no host effect"

            let configured, _ = update OpenSettings m0
            Expect.equal configured.Screen Settings "Config opens the settings screen"
            Expect.equal configured.SettingsReturn MainMenu "settings opened from the menu returns to the menu"

            let quit, quitFx = update Quit m0
            Expect.equal quit.Screen MainMenu "Exit does not itself change the screen — the host closes the window"
            Expect.isTrue (hasExit quitFx) "Exit asks the host to close the app (ExitRequested)"
        }

        test "Start only fires from the main menu (it is a no-op elsewhere)" {
            let paused = atPaused ()
            let m, fx = update Start paused
            Expect.equal m.Screen Paused "Start on the pause overlay does nothing — you resume, not restart"
            Expect.isEmpty fx "a guarded no-op emits no effect"
        }

        test "the pause overlay is the same menu: play pauses and resumes" {
            let playing = atPlaying ()
            let paused, pauseFx = update PauseGame playing
            Expect.equal paused.Screen Paused "PauseGame overlays the menu on live play"
            Expect.isEmpty pauseFx "pausing is pure"

            let resumed, _ = update ResumeGame paused
            Expect.equal resumed.Screen Playing "ResumeGame returns to live play"

            // ResumeGame is guarded to the paused screen.
            let noop, _ = update ResumeGame playing
            Expect.equal noop.Screen Playing "ResumeGame from live play is a no-op"
        }

        test "settings opened from a pause returns to the pause, not the main menu (SettingsReturn)" {
            let paused = atPaused ()
            let settings, _ = update OpenSettings paused
            Expect.equal settings.Screen Settings "Config opens settings from a pause"
            Expect.equal settings.SettingsReturn Paused "the return target is the screen settings was opened from"

            let back, _ = update LeaveSettings settings
            Expect.equal back.Screen Paused "Back from settings returns to the pause overlay it was opened from"
        }
    ]

// ---- Esc routing ----------------------------------------------------------------------------

[<Tests>]
let shellEscapeTests =
    testList "game-shell Esc routing (#991/#1002)" [
        test "Esc pauses live play, resumes a pause, backs out of settings, and no-ops at the menu" {
            let toPaused, _ = update EscapePressed (atPlaying ())
            Expect.equal toPaused.Screen Paused "Esc pauses live play"

            let toPlaying, _ = update EscapePressed toPaused
            Expect.equal toPlaying.Screen Playing "Esc on a pause resumes play"

            let outOfSettings, _ = update EscapePressed (atSettingsFromMenu ())
            Expect.equal outOfSettings.Screen MainMenu "Esc backs out of settings to where it was opened from"

            let atMenu, menuFx = update EscapePressed (init testConfig)
            Expect.equal atMenu.Screen MainMenu "Esc at the main menu is a no-op (the menu already shows)"
            Expect.isEmpty menuFx "the menu-level Esc no-op emits no effect"
        }

        test "Esc cancels an in-flight rebind before it routes any screen" {
            let armed, _ = update (ArmRebind jump) (atSettingsFromMenu ())
            Expect.equal armed.Rebinding (Some jump) "a rebind is in flight"

            let cancelled, fx = update EscapePressed armed
            Expect.equal cancelled.Rebinding None "Esc cancels the capture first"
            Expect.equal cancelled.Screen Settings "and does NOT also back out of settings — the capture ate the Esc"
            Expect.isEmpty (keymapEffects fx) "a cancelled capture never rebinds, so it emits no KeymapChanged"
        }
    ]

// ---- rebind capture ---------------------------------------------------------------------------

[<Tests>]
let shellRebindTests =
    testList "game-shell rebind capture (#991/#1002)" [
        test "rebind round-trip: ArmRebind then CaptureKey rebinds the command and emits KeymapChanged" {
            let armed, armFx = update (ArmRebind jump) (atSettingsFromMenu ())
            Expect.equal armed.Rebinding (Some jump) "ArmRebind puts the command in flight"
            Expect.isEmpty armFx "arming a capture is pure — nothing persists until a key lands"
            Expect.equal (Keymap.resolve armed.Keymap qKey) None "the key to be captured is unbound before the capture (so the rebind has teeth)"

            let captured, capFx = update (CaptureKey qKey) armed
            Expect.equal captured.Rebinding None "the capture completes and clears the in-flight command"
            Expect.equal (Keymap.resolve captured.Keymap qKey) (Some jump) "the captured key now resolves to the rebound command (an upsert-by-key onto the keymap)"
            Expect.equal (Keymap.resolve captured.Keymap wKey) None "command replacement removes the selected command's old binding"
            Expect.equal (Keymap.resolve captured.Keymap fKey) (Some fire) "an unrelated binding is left untouched by the rebind"

            match keymapEffects capFx with
            | [ k ] ->
                Expect.equal (Keymap.resolve k qKey) (Some jump) "the emitted KeymapChanged carries the rebound keymap for the host to persist"
            | other -> failtestf "a completed rebind emits exactly one KeymapChanged; got %d" (List.length other)
        }

        test "Esc during a capture cancels it — a command never binds to the menu key" {
            let armed, _ = update (ArmRebind jump) (atSettingsFromMenu ())
            let cancelled, fx = update (CaptureKey menuKey) armed

            Expect.equal cancelled.Rebinding None "capturing the menu key cancels rather than rebinds"
            Expect.equal (Keymap.resolve cancelled.Keymap menuKey) None "Escape is never bound to a command"
            Expect.equal (Keymap.resolve cancelled.Keymap wKey) (Some jump) "the original binding is left untouched"
            Expect.isEmpty (keymapEffects fx) "a cancelled capture emits no KeymapChanged"
        }

        test "CancelRebind abandons an in-flight capture without rebinding" {
            let armed, _ = update (ArmRebind fire) (atSettingsFromMenu ())
            let cancelled, fx = update CancelRebind armed
            Expect.equal cancelled.Rebinding None "CancelRebind clears the in-flight command"
            Expect.equal (Keymap.resolve cancelled.Keymap fKey) (Some fire) "the binding under edit is unchanged"
            Expect.isEmpty (keymapEffects fx) "cancelling emits no KeymapChanged"
        }

        test "ArmRebind only arms on the settings screen (the rebind UI lives there)" {
            let m, fx = update (ArmRebind jump) (atPlaying ())
            Expect.equal m.Rebinding None "arming a rebind off the settings screen is a no-op"
            Expect.isEmpty fx "and emits nothing"
        }

        test "a captured key with no armed command is ignored (no stray rebind)" {
            let settings = atSettingsFromMenu ()
            let m, fx = update (CaptureKey qKey) settings
            Expect.equal (Keymap.resolve m.Keymap qKey) None "a key press with nothing armed does not bind anything"
            Expect.isEmpty (keymapEffects fx) "and emits no KeymapChanged"
        }

        test "capturing another action's key displaces it but keeps its catalog row, and reset restores defaults" {
            let armed, _ = update (ArmRebind jump) (atSettingsFromMenu ())
            let displaced, _ = update (CaptureKey fKey) armed
            Expect.equal (Keymap.resolve displaced.Keymap fKey) (Some jump) "the intended command owns the captured key"
            Expect.equal (Keymap.resolve displaced.Keymap wKey) None "its previous key is removed"

            let projected = KeyRebind.withBindings displaced.Keymap displaced.Actions
            let fireRow = projected |> List.find (fun action -> action.Command = fire)
            Expect.equal fireRow.Label "Fire" "the displaced action keeps its player-facing label"
            Expect.equal fireRow.Binding None "the displaced action remains explicitly present and unbound"

            let restored, fx = update ResetBindings displaced
            Expect.equal (Keymap.resolve restored.Keymap wKey) (Some jump) "reset restores Jump"
            Expect.equal (Keymap.resolve restored.Keymap fKey) (Some fire) "reset restores Fire"
            Expect.equal (keymapEffects fx).Length 1 "reset emits persistence"
        }
    ]

// ---- raw-key routing (routeKeyDown) ---------------------------------------------------------

[<Tests>]
let shellKeyRoutingTests =
    // `toGame` lifts a resolved live-play command into the game's own value; here the game value IS
    // the CommandId, so a resolved key round-trips visibly.
    let toGame (command: CommandId) : CommandId option = Some command

    testList "game-shell routeKeyDown (#991/#1002)" [
        test "the menu key routes to EscapePressed" {
            let outcome = routeKeyDown toGame menuKey (atPlaying ())
            Expect.equal outcome (ShellMsg EscapePressed) "a raw Esc down becomes the shell's universal Esc route"
        }

        test "while playing, a bound key resolves through the keymap to a game value" {
            match routeKeyDown toGame wKey (atPlaying ()) with
            | Game command -> Expect.equal command jump "W resolves to its bound command and reaches gameplay"
            | other -> failtestf "expected a Game outcome for a bound key while playing; got %A" other
        }

        test "chrome screens never feed the keymap — a bound key is inert off the play screen" {
            Expect.equal (routeKeyDown toGame wKey (init testConfig)) NoInput "a bound key at the main menu resolves to nothing"
            Expect.equal (routeKeyDown toGame wKey (atPaused ())) NoInput "a bound key on the pause overlay resolves to nothing"
        }

        test "a capture in flight swallows the very next key as CaptureKey (even a live-play binding)" {
            let armed, _ = update (ArmRebind jump) (atSettingsFromMenu ())
            Expect.equal (routeKeyDown toGame fKey armed) (ShellMsg(CaptureKey fKey)) "the next key is handed to the capture, not resolved as a command"
            Expect.equal (routeKeyDown toGame menuKey armed) (ShellMsg(CaptureKey menuKey)) "even the menu key is captured (the reducer turns that into a cancel)"
        }

        test "routeKeyEvent preserves both gameplay edges on one normalized seam" {
            let playing = atPlaying ()
            Expect.equal (routeKeyEvent toGame wKey true playing) (GameEdge(jump, true)) "key-down begins the resolved gameplay control"
            Expect.equal (routeKeyEvent toGame wKey false playing) (GameEdge(jump, false)) "key-up ends the same resolved gameplay control"
            Expect.equal (routeKeyEvent toGame menuKey false playing) NoKeyEvent "shell chrome never reacts to key-up"
        }
    ]

// ---- display seams --------------------------------------------------------------------------

[<Tests>]
let shellDisplayTests =
    testList "game-shell display mapping (#991/#1002)" [
        test "SetResolution / SetDisplayMode update the display and emit DisplayChanged; logicalSize follows the resolution" {
            let m0 = init testConfig

            let resized, resFx = update (SetResolution res1080) m0
            Expect.equal resized.Display.Resolution res1080 "the chosen resolution is applied to the display"
            Expect.equal (displayEffects resFx) [ resized.Display ] "the change emits a DisplayChanged carrying the new display for the host to re-apply"
            Expect.equal (logicalSize resized.Display) res1080 "logicalSize is the chosen resolution (#246 letterbox source)"

            let fs, modeFx = update (SetDisplayMode Fullscreen) resized
            Expect.equal fs.Display.Mode Fullscreen "the chosen mode is applied"
            Expect.equal (displayEffects modeFx) [ fs.Display ] "a mode change also emits DisplayChanged"
        }

        test "windowBehavior maps each display mode to its window startup state and resize policy" {
            let behaviourOf mode = windowBehavior { Resolution = res720; Mode = mode }

            let windowed = behaviourOf Windowed
            Expect.equal windowed.StartupState ViewerWindowStartupState.Normal "Windowed starts as a normal, resizable window"
            Expect.equal windowed.ResizePolicy Resizable "a windowed game may be resized"

            let borderless = behaviourOf Borderless
            Expect.equal borderless.StartupState ViewerWindowStartupState.WindowedFullscreen "Borderless is a work-area windowed-fullscreen"
            Expect.equal borderless.ResizePolicy FixedSize "borderless fills the work area at a fixed size"

            let fullscreen = behaviourOf Fullscreen
            Expect.equal fullscreen.StartupState ViewerWindowStartupState.Fullscreen "Fullscreen is exclusive fullscreen"
            Expect.equal fullscreen.ResizePolicy FixedSize "exclusive fullscreen is a fixed size"
        }
    ]

// ---- persistence ----------------------------------------------------------------------------

[<Tests>]
let shellPersistenceTests =
    testList "game-shell keymap persistence (#991/#1002)" [
        test "encodeKeymap / decodeKeymap round-trips a rebound keymap onto a fresh model" {
            // Rebind, then persist and restore into a freshly-initialised model (the default bindings),
            // proving the player's rebinding survives the encode/decode the host writes at shutdown.
            let armed, _ = update (ArmRebind jump) (atSettingsFromMenu ())
            let rebound, _ = update (CaptureKey qKey) armed

            let blob = encodeKeymap rebound
            let restored = decodeKeymap blob (init testConfig)

            Expect.equal (Keymap.resolve restored.Keymap qKey) (Some jump) "the rebound key survives the encode/decode round-trip"
            Expect.equal (Keymap.resolve restored.Keymap fKey) (Some fire) "and the other bindings come back intact after a restore"
        }

        test "decodeKeymap degrades to the current keymap on a corrupt blob (total, never throws)" {
            let m0 = init testConfig
            let kept = decodeKeymap [| 0uy; 1uy; 2uy; 3uy |] m0
            Expect.equal (Keymap.resolve kept.Keymap wKey) (Some jump) "a corrupt save is dropped and the current bindings are kept, not thrown away"
        }

        test "the pre-catalog v1 codec payload migrates into catalog-backed settings without losing actions" {
            let legacy =
                sprintf
                    "{\"format\":\"fsgg.keymap\",\"version\":1,\"bindings\":[{\"key\":\"%s\",\"command\":\"jump\"}]}"
                    qKey
                |> System.Text.Encoding.UTF8.GetBytes

            let restored = decodeKeymap legacy (init testConfig)
            Expect.equal (Keymap.resolve restored.Keymap qKey) (Some jump) "the legacy runtime binding is retained"
            let projected = KeyRebind.withBindings restored.Keymap restored.Actions
            let jumpRow = projected |> List.find (fun action -> action.Command = jump)
            let fireRow = projected |> List.find (fun action -> action.Command = fire)
            Expect.equal jumpRow.Label "Jump" "catalog metadata supplies the player-facing label after migration"
            Expect.equal fireRow.Binding None "an action absent from the legacy keymap remains visible as unbound"
        }
    ]
//#endif
