---
name: fs-gg-game-shell
description: Wire the reusable game shell — menu/start screen, settings (resolution/fullscreen), and key rebinding — into a generated FS.GG.UI game.
---

# Game Shell Capability

## Scope

Use this skill to give your game the turnkey shell that every scaffolded game
gets: a **main menu / start screen** (your game's name + Start / Config / Exit),
**Esc-reachable pause routing**, a **settings screen** for resolution + fullscreen,
and **in-game key rebinding** whose bindings persist. The shell is a game-agnostic
module — `GameShell.fs` (`module AppRoot.GameShell`) — that you
PARAMETERIZE with your game; you do not re-author a menu per game.

The shell **composes** framework mechanisms and rebuilds none of them:

- **Key rebinding** — explicitly keyed clickable rows from a stable `KeyRebindAction`
  catalog (player labels/order/defaults remain present when an action is unbound), with
  `KeymapCodec` for persistence, `Keymap.replaceCommandBinding` for capture, and the
  `ViewerKeyboard.mapKeyRaw` seam. See [[fs-gg-keyboard-input]].
- **Resolution / fullscreen** — a `DisplaySettings` mapped onto a
  `ViewerWindowBehaviorRequest` (window startup state) and `LogicalCanvas` (the
  fixed-logical-resolution letterbox). See [[fs-gg-skiaviewer]].
- **UI** — the typed `Controls` front door (Button / Stack / TextBlock) over the
  pointer-aware interactive host. See [[fs-gg-ui-widgets]].

## Public Contract

The shell module lives at `GameShell.fs` and is yours to adapt. Its
shape (a pure Elmish state machine plus view + host seams):

- `Screen` = `MainMenu | Playing | Paused | Settings` — the router; `Playing` is
  the only screen the shell does not draw over (your game owns it).
- `DisplayMode` = `Windowed | Borderless | Fullscreen`; `DisplaySettings =
  { Resolution: Size; Mode: DisplayMode }`.
- `Config = { Title; Actions; DisplayModes; Resolutions; InitialDisplay }` —
  what YOUR game supplies (its name, its rebindable `KeyRebindAction` catalog, the
  offered resolutions/modes). The `Keymap` itself is NOT supplied directly — `init`
  derives it from `Actions` via `KeyRebind.restoreDefaults`.
- `Model`, `Msg`, `Effect` — the shell state, its messages, and the intents the
  host interprets (`ExitRequested`, `DisplayChanged`, `KeymapChanged`).
- `init`, `update : Msg -> Model -> Model * Effect list` — deterministic, host-free.
- `windowBehavior`, `logicalSize`, `logicalFit` — the display → viewer seams.
- `routeKeyEvent` (`routeKeyDown` compatibility helper), `encodeKeymap`, `decodeKeymap` —
  the raw-key + persistence seams.

## Usage

Embed the shell in your model and thread its `Msg`:

```fsharp
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls
open FS.GG.UI.Scene

let config : GameShell.Config =
    { Title = "My Game"
      Actions =
        [ { Command = "move-left"; Label = "Move Left"; Order = 0
            Binding = None; DefaultBinding = Some "ArrowLeft" }
          { Command = "fire"; Label = "Fire"; Order = 1
            Binding = None; DefaultBinding = Some "Space" } ]
      DisplayModes = [ GameShell.Windowed; GameShell.Borderless; GameShell.Fullscreen ]
      Resolutions = [ { Width = 1280; Height = 720 }; { Width = 1920; Height = 1080 } ]
      InitialDisplay = { Resolution = { Width = 1280; Height = 720 }; Mode = GameShell.Windowed } }

type Model = { Shell: GameShell.Model ; (* your gameplay fields *) }
type Msg = Shell of GameShell.Msg | (* your gameplay messages *)

let update msg model =
    match msg with
    | Shell shellMsg ->
        let shell, effects = GameShell.update shellMsg model.Shell
        // route each effect to the host at your boundary (see the three below)
        { model with Shell = shell }, []
    | _ -> model, []
```

Route the three shell effects at the host boundary: an `ExitRequested` asks the
host to shut the window; a `KeymapChanged keymap` is persisted with
`GameShell.encodeKeymap`; a `DisplayChanged settings` re-applies
`GameShell.windowBehavior` as `ApplyWindowOptions` and `GameShell.logicalSize` as
`ApplyLogicalCanvas`.

### The raw-key seam (rebind capture + held gameplay)

A rebind capture MUST forward the raw key — a `MapKey` that RESOLVES a key drops
exactly the unbound key a capture waits for ([[fs-gg-keyboard-input]]). Wire the
host's `mapKeyRaw` to `routeKeyEvent` for **both down and up**. It decides — from
the shell state — whether a down completes capture/routes Esc, or whether either
edge resolves a live-play command. Retain a resolved command on `GameEdge(_, true)`,
apply that snapshot on fixed ticks, and clear it on `GameEdge(_, false)`:

```fsharp
// toGame lifts a resolved live-play CommandId into your game's own Msg value.
let outcome = GameShell.routeKeyEvent (fun command -> commandToMsg command) key isDown model.Shell
match outcome with
| GameShell.ShellEdge m -> dispatch (Shell m)       // down-only capture completion / Esc
| GameShell.GameEdge(gameMsg, true) -> hold key gameMsg
| GameShell.GameEdge(_, false) -> release key
| GameShell.NoKeyEvent -> ()
```

Arm a rebind from the settings UI: a keyed binding row dispatches
`GameShell.ArmRebind command`; the next key press then fires the capture
(`Keymap.replaceCommandBinding` via `update`) and emits `KeymapChanged` for you to
persist. The selected command has exactly one key; an action displaced from that key
remains visible as `Unbound`. `GameShell.ResetBindings` rebuilds the keymap from the
catalog's defaults and emits the same persistence effect.

### Display settings

`GameShell.windowBehavior settings` yields a `ViewerWindowBehaviorRequest` and
`GameShell.logicalSize settings` the logical canvas. Seed `ViewerOptions.LogicalSize`
with the initial value; when `DisplayChanged` fires, emit both `ApplyWindowOptions`
and `ApplyLogicalCanvas`. The chosen resolution then letterboxes onto any surface and
the mode picks windowed / borderless / exclusive fullscreen. Prefer `Fullscreen` as
the safe shipped default until the native host contract is verified on the target
desktop; reserve `Borderless` for an explicitly tested work-area transition.

The persistent viewer applies `ApplyWindowOptions` to the live native window on its
loop thread. Repeated identical requests are idempotent; returning to windowed mode
restores the remembered windowed geometry. Observe `ViewerDiagnosticCategory.Window`
to distinguish an applied transition from a rejected or failed request. In particular,
the initialized rendering backend cannot be switched live, so such a request is
diagnosed without partially mutating the window.

SkiaViewer is the sole transform owner. It fits and centers the logical canvas and maps
native pointer samples through the inverse fit before Controls lays out or hit-tests.
Do not scale the Controls tree or pointer coordinates again. The policy is identical for
windowed, borderless, and fullscreen requests.

At default launch, make `ViewerOptions.InitialSize` exactly
`config.InitialDisplay.Resolution` and use the same explicit window-behavior overload
for both flagged and unflagged launches. One native pointer coordinate must identify
the same point in the authored Controls layout; do not rely on a different overload's
implicit startup behavior.

When parsing launch/configuration options, overlay each supplied field on the shell's
existing `ViewerWindowBehaviorRequest`; do not replace the record. A single flag must
change only its named field, preserving `ResizePolicy`, `MaximizePolicy`, `StartupState`,
`StartupPosition`, and backend preference from the shell/default configuration. Test
the no-flag case and each single flag (including an omitted `startup`) through the real
parser before treating a worked configuration as safe.

### Pause-safe rebind and exact persistence (the production-host journey)

A settings screen that reports a successful rebind, and a `Playing` transition that
is truly zero-input-safe, are two different claims. Only a journey that runs BOTH
through the real seams — starting at `Screen = MainMenu` and actually REACHING
`Screen = Playing` before the held-key/pause sequence — tells you which one you
actually have. A test that never drives `Screen` to `Playing` at all proves nothing:
the retained command can never become `Some` in the first place, so a final `None`
assertion passes whether or not the pause-safety fix below is even present. A test
that constructs a resolved gameplay `Msg` by hand skips the raw key too, so it
cannot see a capture that silently fails to rearm.

The trap is exactly where the up edge lands. A shell that clears its retained
command only on the matching `GameEdge(_, false)` misses the one order that matters
here: the up edge can legally arrive one `Screen` transition later than the down
that started the hold, while `Screen = Paused`. So the retained-command snapshot
must ALSO clear the moment `Screen` leaves `Playing` — not only on release — or a
key released during a pause resumes play still moving.

Drive the whole journey through `GameShell.routeKeyEvent` and `GameShell.update` —
never a hand-built `Msg` — and assert the persistence side on the SAME list
`GameShell.update` actually returns: the `Effect list`. This template wires no
`ViewerEffect.Persist` sink (no host here calls `runAppWithPersistence`; see
[[fs-gg-testing]] for the requested-versus-durable split that applies once one is
wired), so `GameShell.Effect` is the observable point, one seam upstream of any host
sink. Thread it through every step — do not discard it as `_` — and count it per
preference: the one capture that actually changed the keymap must contribute exactly
one `KeymapChanged`, and `DisplaySettings`, untouched this run, must contribute zero
`DisplayChanged`. Menu navigation (`Start`, `OpenSettings`, `ArmRebind`,
`LeaveSettings`, `ResumeGame`) is dispatched directly, exactly as the shell's own
button rows do — only NATIVE KEY edges (the rebind capture, the pause, the resume)
go through `routeKeyEvent`:

```fsharp
// Drive every NATIVE KEY edge through routeKeyEvent + update — the raw-key mapping and the
// retained semantic-command boundary — never a hand-built game Msg. Menu buttons dispatch
// their Msg directly, same as the shell's own rows. Every returned Effect list is threaded
// through and accumulated below — none is discarded as `_` — because "unchanged preferences
// emit none" is an assertion on that list, not narration.
let step shell heldCommand key isDown =
    match GameShell.routeKeyEvent (fun command -> commandToMsg command) key isDown shell with
    | GameShell.ShellEdge m ->
        let next, effects = GameShell.update m shell
        // Leaving Playing must drop any retained command NOW: the matching up edge, if
        // one ever arrives, lands after Paused and must not find something to release.
        let stillHeld = if next.Screen = GameShell.Playing then heldCommand else None
        next, stillHeld, effects
    | GameShell.GameEdge(gameMsg, true) -> shell, Some gameMsg, []
    | GameShell.GameEdge(_, false) -> shell, None, []
    | GameShell.NoKeyEvent -> shell, heldCommand, []

// shell0 is explicit — GameShell.init's actual starting state — not left for the reader to
// guess: `Screen = MainMenu`, which is the only Screen every journey legally starts from.
let shell0 = GameShell.init config

// Reach Playing for real, through the shell's own button-row Msgs — MainMenu -> Playing ->
// Paused -> Settings, so ArmRebind actually arms (it only does while Screen = Settings).
let shell1, effects1 = GameShell.update GameShell.Start shell0                 // MainMenu -> Playing
let shell2, effects2 = GameShell.update GameShell.EscapePressed shell1         // Playing -> Paused
let shell3, effects3 = GameShell.update GameShell.OpenSettings shell2          // Paused -> Settings
let shell4, effects4 = GameShell.update (GameShell.ArmRebind "move-up") shell3 // arms; still Settings

// 1. Complete the capture with the next native key — the shipped raw-key seam, not a Msg.
let shell5, held5, effects5 = step shell4 None "KeyW" true

let shell6, effects6 = GameShell.update GameShell.LeaveSettings shell5         // Settings -> Paused
let shell7, effects7 = GameShell.update GameShell.ResumeGame shell6            // Paused -> Playing

// 2. Ordinary play: the SAME key now genuinely resolves through the retained semantic
//    boundary (Screen really is Playing here, so this actually becomes Some).
let shell8, held8, effects8 = step shell7 held5 "KeyW" true

// 3. Pause BEFORE the "KeyW" up edge ever arrives — through the same native-key seam.
let shell9, held9, effects9 = step shell8 held8 "Escape" true

// 4. The up edge lands while paused, then resume.
let shell10, held10, effects10 = step shell9 held9 "KeyW" false
let _shell11, held11, effects11 = step shell10 held10 "Escape" true

// The FULL accumulation, every step included — the list "unchanged preferences emit none"
// is actually asserted against, not a single call assumed representative of the rest.
let allEffects =
    effects1 @ effects2 @ effects3 @ effects4 @ effects5
    @ effects6 @ effects7 @ effects8 @ effects9 @ effects10 @ effects11

let keymapPersisted = allEffects |> List.filter (function GameShell.KeymapChanged _ -> true | _ -> false)
let displayPersisted = allEffects |> List.filter (function GameShell.DisplayChanged _ -> true | _ -> false)

Expect.equal shell7.Screen GameShell.Playing
    "the journey must actually reach Playing, or the assertion below is vacuous"
Expect.isSome held8
    "the rebound key must actually become held, or the assertion below is vacuous"
Expect.isNone held11
    "a rebound key whose up edge lands during a pause must not still be retained after resume"
Expect.equal (List.length keymapPersisted) 1
    "the one changed preference (the rebind) must appear in the Effect list exactly once"
Expect.isEmpty displayPersisted
    "a preference the run never touched (DisplaySettings) must contribute no Effect at all"
```

`keymapPersisted`/`displayPersisted` are filtered from `allEffects`, the FULL
accumulation across every step of the journey — not the capture step alone — so
"unchanged preferences emit none" is verified over the same nine other
`update`/`step` calls, not merely asserted in prose. Every one of those other calls
genuinely returns `[]` in the real shell (menu navigation, arming, pausing, and
resuming touch neither `Keymap` nor `DisplaySettings`), which is exactly what
`displayPersisted`'s emptiness proves.

Delete the `if next.Screen = GameShell.Playing then heldCommand else None` line above
— replace it with plain `heldCommand`, the pre-fix behavior — and `held11` becomes
`Some`, reddening `Expect.isNone held11`: the trace above is not vacuous, and this is
what it actually catches. The count assertions are equally falsifiable: fold in the
effects of an extra `GameShell.update (GameShell.SetResolution otherSize) shellN`
dispatched anywhere in the journey — `SetResolution` really does emit `DisplayChanged`
every time it fires — and `displayPersisted` stops being empty, reddening
`Expect.isEmpty displayPersisted`.

## Capability boundary — the shell needs the pointer-aware host

The generated game shell already launches through `InteractiveAppHost`
(`Controls.Elmish.runInteractiveApp*`). Preserve that host when replacing the starter:
the shell's authored buttons and key-rebind rows require its retained pointer route.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this game.

## Test Commands

Run `./fake.sh build -t Test`. Pure reducer assertions are necessary but do not prove
the live integration. Also drive the generated `interactiveHost` headlessly: require
`captureRespondsProof` to be `Responsive` for one menu button and one rebind row at
the exact default surface, complete capture with the next raw key, and prove one
movement key stays held over at least two fixed ticks and stops after key-up.
Also change 1280x720 to 1920x1080 and activate the same semantic control through the
corresponding physical point; this proves the visible fit and retained hit geometry use
one policy rather than merely proving that the resolution value persisted.

Extend that same rebound key one step further: pause BEFORE its key-up edge arrives,
release while paused, resume, and assert zero movement intent — see
[Pause-safe rebind and exact persistence](#pause-safe-rebind-and-exact-persistence-the-production-host-journey)
for the worked journey. Also assert the persistence side at the sink: the one capture
that actually changed a preference must reach the host boundary exactly once, and a
preference the run never touched must reach it zero times.

## Evidence

Record deterministic menu/settings/rebind **evidence** under this game's `readiness/`
paths. Store runtime preferences under the platform per-user application-data location,
not under `readiness/`; migrate any legacy readiness settings once and ignore/remove
the legacy file only after the platform write succeeds.

## Related

- [[fs-gg-keyboard-input]] — the `Keymap` + `mapKeyRaw` capture the rebind screen is built on.
- [[fs-gg-skiaviewer]] — the interactive host + `LogicalCanvas` the settings drive.
- [[fs-gg-ui-widgets]] — the typed `Controls` the menu is authored with.
- [[fs-gg-elmish]] — thread the shell `Msg` through the pure adapter.
- [[fs-gg-testing]] — the assert-at-the-sink pattern this journey's persistence count
  reuses, and the record-only-versus-durable distinction it depends on.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the
driven library's own documentation), then community sources. If your game uses
Spec Kit, record findings under the feature's `specs/<feature>/feedback/` folder;
otherwise record them in this skill's Sources line. Offline, the mandate degrades to
recording "research blocked — <why>" rather than hard-failing the phase.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (host input/runtime): https://github.com/mono/SkiaSharp
