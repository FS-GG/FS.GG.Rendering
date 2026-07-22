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
module — `src/Product/GameShell.fs` (`module AppRoot.GameShell`) — that you
PARAMETERIZE with your game; you do not re-author a menu per game.

The shell **composes** framework mechanisms and rebuilds none of them:

- **Key rebinding** — the `KeyRebind` config-screen control over the immutable
  `Keymap` (rebind mechanism) with `KeymapCodec` for persistence, captured through
  the `ViewerKeyboard.mapKeyRaw` seam. See [[fs-gg-keyboard-input]] for the
  raw-key capture recipe the shell is built on.
- **Resolution / fullscreen** — a `DisplaySettings` mapped onto a
  `ViewerWindowBehaviorRequest` (window startup state) and `LogicalCanvas` (the
  fixed-logical-resolution letterbox). See [[fs-gg-skiaviewer]].
- **UI** — the typed `Controls` front door (Button / Stack / TextBlock) plus the
  `KeyRebind` control, over the pointer-aware interactive host. See [[fs-gg-ui-widgets]].

## Public Contract

The shell module lives at `src/Product/GameShell.fs` and is yours to adapt. Its
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
- `routeKeyDown`, `encodeKeymap`, `decodeKeymap` — the raw-key + persistence seams.

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
`GameShell.windowBehavior` and `GameShell.logicalSize`.

### The raw-key seam (rebind capture + Esc routing)

A rebind capture MUST forward the raw key — a `MapKey` that RESOLVES a key drops
exactly the unbound key a capture waits for ([[fs-gg-keyboard-input]]). Wire the
host's `mapKeyRaw` to `routeKeyDown`, which decides — from the shell state — whether
the key completes a capture, routes menu chrome (Esc), or resolves to a live-play
command:

```fsharp
// toGame lifts a resolved live-play CommandId into your game's own Msg value.
let outcome = GameShell.routeKeyDown (fun command -> commandToMsg command) key model.Shell
match outcome with
| GameShell.ShellMsg m -> dispatch (Shell m)      // capture completion or Esc route
| GameShell.Game gameMsg -> dispatch gameMsg      // live gameplay
| GameShell.NoInput -> ()
```

Arm a rebind from the settings UI: the `KeyRebind` control's row activation
dispatches `GameShell.ArmRebind command`; the next key press then fires the capture
(`GameShell.Keymap.rebind` via `update`) and emits `KeymapChanged` for you to persist.

### Display settings

`GameShell.windowBehavior settings` yields a `ViewerWindowBehaviorRequest` and
`GameShell.logicalSize settings` the `ViewerOptions.LogicalSize`, so the chosen
resolution letterboxes onto any surface and the mode picks windowed / borderless /
exclusive fullscreen. Re-apply them when `DisplayChanged` fires.

## Capability boundary — the shell needs the pointer-aware host

A menu needs a mouse, and the game family's DEFAULT host is keyboard-only
([[fs-gg-keyboard-input]]). Driving the shell's buttons with a pointer means the
`InteractiveAppHost` (`Controls.Elmish.runInteractiveApp`) — the same host the
`app`/controls family uses — a durable host-wiring change in `src/Product/Program.fs`,
not an edit at your model. Plan for it up front.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this game.

## Test Commands

Run `./fake.sh build -t Test`. The shell `update` is pure, so assert its routing,
the rebind capture, and the display → window-behavior mapping with no window.

## Evidence

Record menu/settings/rebind evidence under this game's `readiness/` paths.

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
