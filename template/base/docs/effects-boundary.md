# Effects Boundary

This is the single, canonical page describing how your generated FS.GG.UI app
turns pure decisions into real window/render/screenshot work. You can follow it
without reading framework reports or framework source.

## Two effect categories

There are two distinct kinds of effect, on two sides of one boundary:

1. **Application commands at the MVU edge.** Your pure reducer
   `Product.Program.update : Msg -> Model -> Model * ViewerEffect list` returns a
   new model **and a list of requested effects as plain values**. This is the MVU
   edge: `update` decides *what should happen* but performs no I/O itself. It must
   not touch the filesystem, window system, process, clock, or randomness.

2. **Viewer effects at the host boundary.** The viewer interpreter inside
   `Viewer.runApp` executes those requested values against the real desktop host.
   These are the host-boundary effects, expressed as `ViewerEffect` cases:
   - `OpenWindow (title, size)` — create the native window.
   - `ApplyWindowOptions behavior` — resize/maximize/startup-state/backend.
   - `RenderScene scene` — draw the current `View model` scene.
   - `CaptureScreenshot path` / `CaptureImageEvidence path` — write visual evidence.
   - `EmitDiagnostic event` — record a diagnostic.
   - `CloseWindow`, `DispatchInput (key, isDown)`, `CheckDesktopSession`, …

## The boundary

Application commands are **not** viewer effects: a command is a value your pure
code returns; a viewer effect is the native action the interpreter performs at
the host boundary. Keep them separate — never perform window/render/screenshot
I/O inside `update`, and never append host-boundary work to the reducer by doing
the I/O directly. The interpreter at the edge (`Viewer.runApp`) is the only place
that turns requested effects into real host actions, then feeds results back as
`Msg` values.

## Canonical `update` → host wiring

Your app wires its pure reducer to the host through a `GeneratedAppHost` value and
`Viewer.runApp`:

```fsharp
// Product.EvidenceCommands
let generatedHost =
    { Init = fun () -> initialModel, []          // initial model + startup effects
      Update = update                            // pure Msg -> Model -> Model * ViewerEffect list
      View = view                                // Model -> SceneNode
      MapKey = mapKey                            // ViewerKey -> bool -> Msg option
      Tick = tick                                // TimeSpan -> Msg option
      Diagnostics = Viewer.defaultDiagnostics }

// Product.Program
match Viewer.runApp viewerOptions generatedHost with
| Ok outcome -> // window opened, scenes rendered, effects interpreted at the host boundary
| Error failure -> // classified host/launch/verification failure
```

`Viewer.runApp viewerOptions generatedHost` is the canonical entry point: it calls
`Init`, renders `View`, routes input through `MapKey`, advances time through
`Tick`, and interprets every `ViewerEffect` your `Update` returns at the host
boundary. For bounded evidence runs the same host is used with
`Viewer.runAppEvidence request viewerOptions generatedHost`.

## Summary

- Pure side (MVU edge): `Model`, `Msg`, `update`, `View` — values only.
- Host side (host boundary): `Viewer.runApp` interprets `ViewerEffect` values.
- One boundary between them; cross it only through the interpreter.
