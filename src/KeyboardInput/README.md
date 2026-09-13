# FS.GG.UI.KeyboardInput

`CommandInput.compile` validates one product catalog and ordered input profile
before any dispatch state is constructed. Profiles preserve logical key values,
physical codes, complete modifiers including AltGraph, key sequences, pointer,
touch and gamepad controls. Overrides distinguish replacement, aliases and an
explicitly unbound command.

`InputProfileCodec` round-trips the ordered `fsgg.input-profile/1` envelope in
.NET and Fable. `CommandInput.ofKeymap` migrates the existing `fsgg.keymap` v1
surface as logical-key defaults; the old reader and last-wins behavior remain
unchanged. Products still own command meanings and availability projections.

`CommandResolver.update` consumes ordered input observations as a pure reducer.
It resolves exclusive modal contexts, opt-in key-sequence prefixes and injected
deadlines, suppresses destructive repeats, and tracks held actions by source.
Focus loss, composition, modal takeover, context/profile replacement, source
disconnect and disposal cancel transient work and neutralize owned holds. Hosts
interpret the returned invocation, deadline, focus, capture and prevent-default
effects; the package reads no clock or browser state.

Package-owned keyboard input runtime, reducer, effect, diagnostics, and state display contracts for FS.GG.UI products.

`FS.GG.UI.KeyboardInput` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.KeyboardInput
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

```fsharp
open FS.GG.UI.KeyboardInput

// Bind host keys to product commands.
let bindings =
    [ { Key = "ArrowUp"; Command = "move-up" }
      { Key = "Enter"; Command = "confirm" } ]

// Initialise the keyboard runtime model + startup effects.
let model, _initEffects = Keyboard.init bindings

// Normalise a raw host key event, then feed it to the reducer.
let viewerKey, isDown = ViewerKeyboard.normalizeEvent { RawKey = "ArrowUp"; Direction = KeyDown }
let keyId = ViewerKeyboard.toKeyId viewerKey
let model, effects = Keyboard.update (KeyboardMsg.KeyDown keyId) model

// React to resolved commands and surface diagnostics.
for effect in effects do
    match effect with
    | CommandResolved cmd -> printfn "command resolved: %s" cmd
    | ReportKeyboardDiagnostic d -> printfn "[%s] %s" d.Severity d.Message
    | _ -> ()

// Project a snapshot for HUD / state display rendering.
let display = Keyboard.stateDisplay model
printfn "pressed: %A active layout: %s" display.PressedKeys display.ActiveLayout
```

## API at a glance

- `Keyboard.init` — builds the initial `KeyboardModel` from a `KeyboardBinding list` and returns startup `KeyboardEffect`s.
- `Keyboard.update` — the Elmish reducer; applies a `KeyboardMsg` (key down/up, focus lost, layout/mode changes, sequence resolution) and returns the new model plus emitted effects.
- `Keyboard.stateDisplay` — projects a `KeyboardStateDisplay` snapshot (pressed keys, active layout, mode stack, pending sequence, last command) for rendering.
- `ViewerKeyboard.normalize` / `normalizeEvent` — turn a raw host key string or `ViewerKeyEvent` into a typed `ViewerKey` (plus direction flag).
- `ViewerKeyboard.toKeyId` — converts a `ViewerKey` into the `KeyId` used by bindings and the reducer.
- `KeyboardEffect` — the effects the runtime emits: `CommandResolved`, `KeyStateChanged`, `LayoutChanged`, `ModeChanged`, `StateDisplayChanged`, `ReportKeyboardDiagnostic`, `RequestHostKeyCapture`, and more.
- `KeyboardModel` / `KeyboardMsg` — the runtime state record and the message set driving the reducer.
- `KeyboardDiagnostic` — a structured diagnostic (`Code`, `Severity`, `Message`, optional `Key`) reported via effects.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsGgUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT
