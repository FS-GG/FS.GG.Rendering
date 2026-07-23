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
- `Config = { Title; DefaultKeymap; DisplayModes; Resolutions; InitialDisplay }` —
  what YOUR game supplies (its name, its rebindable key→command `Keymap`, the
  offered resolutions/modes).
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
open FS.GG.UI.Scene

let config : GameShell.Config =
    { Title = "My Game"
      DefaultKeymap =
        Keymap.ofBindings
            [ { Key = "ArrowLeft"; Command = "move-left" }
              { Key = "Space"; Command = "fire" } ]
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
the mode picks windowed / borderless / exclusive fullscreen.

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
