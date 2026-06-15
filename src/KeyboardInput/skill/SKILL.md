---
name: fs-gg-keyboard-input
description: Work on keyboard input contracts and generated product keyboard guidance.
---

# KeyboardInput Capability

## Scope

Owns `src/KeyboardInput/`, keyboard input tests, `template/fragments/keyboard-input/`, and generated product keyboard reducer usage.

## Public Contract

The supported API lives in `src/KeyboardInput/KeyboardInput.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.KeyboardInput.txt`.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t DependencyReport`, and `./fake.sh build -t PackLocal`.

## Test Commands

Run `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Capture reducer transition and emitted effect evidence under the active feature
readiness package-surface reports. Stable public surface baselines live under
`readiness/surface-baselines/`.

## Package Boundary

Keyboard input may depend on Scene and YamlDotNet only. Keep viewer hosting, controls, charting, graphing, and layout concerns out of this package; use `fs-gg-ui-widgets` for widget authoring.

## Generated Product

Products that select keyboard input receive the keyboard skill only when selected directly or as a prerequisite.

## Runnable example

Open the package namespace and drive the pure keyboard reducer:

```fsharp
open FS.GG.UI.KeyboardInput

let model, _ = Keyboard.init [ { Key = "Space"; Command = "jump" } ]
let key, isDown = ViewerKeyboard.normalizeEvent { RawKey = "Space"; Direction = KeyDown }
let next, effects = Keyboard.update (KeyDown(ViewerKeyboard.toKeyId key)) model
printfn "down=%b last=%A effects=%A" isDown next.LastCommand effects
```

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-skiaviewer]] dispatches host key events into this reducer.
- [[fs-gg-scene]] is the only allowed package dependency for keyboard state visuals.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- YamlDotNet (the binding-config dependency): https://github.com/aaubry/YamlDotNet
