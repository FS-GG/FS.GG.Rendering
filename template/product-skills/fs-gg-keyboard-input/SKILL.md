---
name: fs-gg-keyboard-input
description: Map keyboard input to product commands in a generated FS.GG.UI product.
---

# KeyboardInput Capability

## Scope

Use this skill for product keyboard handling in the `app` profile: mapping a
normalized `ViewerKey` (plus its down/up flag) to a product `Msg` at the host's
`MapKey` boundary. This is the boundary the generated host actually threads — there
is no separate keyboard reducer to seed.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/KeyboardInput/KeyboardInput.fsi` (the `ViewerKey` cases the host
delivers) and `docs/api-surface/SkiaViewer/SkiaViewer.fsi` (the `MapKey: ViewerKey
-> bool -> 'msg option` field on the generated host). The host normalizes raw key
strings to `ViewerKey` for you and calls `MapKey`; your only job is the pure
`ViewerKey -> bool -> Msg option` mapping.

## Usage

```fsharp
open FS.GG.UI.KeyboardInput

// The host calls this at its MapKey boundary: a normalized ViewerKey + down flag
// in, an optional product Msg out. This is the entire consumer keyboard contract.
let mapKey (key: ViewerKey) (isDown: bool) : Msg option =
    match key, isDown with
    | ArrowLeft, true -> Some MoveLeft
    | Space, true -> Some PrimaryAction
    | _ -> None

// Wire it into the generated host (app profile):
//   let generatedHost = { ... ; MapKey = mapKey ; ... }
```

The `Keyboard.init`/`Keyboard.update`/`KeyboardEffect` reducer in
`KeyboardInput.fsi` is an optional advanced surface for products that maintain
their own keyboard state machine; the `app` host does **not** use it, so do not
seed it as the consumer path. If you do adopt it, read
[Package Boundary](#package-boundary) first: **no host runner interprets a
`KeyboardEffect`**, so every effect it emits is yours to act on.

## Common pitfalls

- **Duplicate DU case names across co-opened modules.** `ViewerKey.Unknown of raw:
  string` (from `FS.GG.UI.KeyboardInput`) and `ViewerRunBlockedStage.Unknown`
  (from `FS.GG.UI.SkiaViewer`) are both in scope once you `open` both modules. A
  bare `Unknown` then binds to whichever module was opened **last**, producing a
  misleading type error far from the real site. Qualify the case at the use site:
  ```fsharp
  match key with
  | ViewerKey.Unknown raw -> handleUnknownKey raw   // not a bare `Unknown _`
  | _ -> ...
  ```
  The same trap fires across **your own** co-opened modules — it is not limited to
  framework-vs-framework collisions. A consumer that declares both
  `type GameMode = | Launch | Playing | …` and `type Msg = | Launch | Tick | …`
  and `open`s both has two `Launch` cases in scope; a bare `Launch` binds to the
  **last-declared** type (`Msg`), so a `GameMode`-typed match arm or constructor
  yields ten misleading "expected GameMode but has type Msg" errors far from the
  real site. Qualify the case — `GameMode.Launch` / `Msg.Launch` — at every use:
  ```fsharp
  let next = GameMode.Launch          // not a bare `Launch`
  match mode with
  | GameMode.Launch -> startGame ()
  | _ -> ...
  ```

## Capability boundary — the default host is keyboard-only

Know this **before** you design a control scheme. The game family's governed
default persistent host is **`Viewer.runApp`** over **`GeneratedAppHost`**, and its **only**
input seam is `MapKey: ViewerKey -> bool -> 'msg option` — **keyboard only**. `ViewerKey`
enumerates keyboard keys (`ArrowLeft`/…/`Letter`/`Digit`/…) and has **no mouse or pointer
case**; a key arrives at the host as `DispatchInput of ViewerKey * isDown`. There is therefore
**no way to read the mouse** on the default host — a mouse-aimed scheme (e.g. twin-stick WASD +
mouse aim) cannot be wired through `MapKey`.

Reading the mouse requires the **pointer-aware interactive host**: `InteractiveAppHost`, driven
by `Controls.Elmish.runInteractiveApp`, which adds a
`MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list` seam (this is the host the
`app`/controls family already uses). Switching a game onto it is a **durable, governance-scanned
host-wiring change in `Program.fs`** — not an edit at your `mapKey` / input-mapping site. Choose
keyboard-only controls, or plan for that host change, up front.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to assert binding resolution and command effects.

## Evidence

Record keyboard command and state evidence under this product's `readiness/`
paths. Do not copy framework readiness reports into the product.

## Package Boundary

Keep key reduction pure: the host delivers normalized keys, and `Keyboard.update` returns
`KeyboardEffect` values without performing I/O.

**No shipped host runner interprets a `KeyboardEffect`.** Every runner that interprets effects at
all — `Viewer.runApp`, `Viewer.runAppWithAudio`, `ControlsElmish.runInteractiveApp`, and the rest —
interprets `ViewerEffect`, and `ViewerEffect` has no keyboard case. (The scene-only runners,
`Viewer.run`/`runBounded`/`runForFrames`, take a `SceneNode` and interpret no effects at all.) The
only interpreter that exists,
`ControlsElmish.interpretKeyboardEffect`, is a pure lowering function that **the framework never
calls**. If you want it called, you call it, and you route what it returns.

### Which host interprets each `KeyboardEffect`

| `KeyboardEffect` | `interpretKeyboardEffect` lowers it to | Which host runner interprets that |
|---|---|---|
| `CommandResolved` | `DispatchProductMessage` | none — **you** route it into `update` |
| `ReportKeyboardDiagnostic` | `ReportAdapterDiagnostic` | none — **you** route it |
| `RequestHostKeyCapture` | `ReportAdapterDiagnostic` — `keyboard-input/HostKeyCaptureNotInterpreted` (issue 456) | **none — it is a complaint, not a capture** |
| `KeyStateChanged`, `LayoutChanged`, `ModeChanged`, `PendingSequenceChanged`, `StateDisplayChanged` | `[]` — dropped | n/a |

**`RequestHostKeyCapture` is inert in the framework, and the owner is you — the product.** Nothing
constructs it (`Keyboard.update` never emits it), no `ViewerEffect` carries it, and no runner is
listening for it. It used to lower to `DispatchHostCommand "capture-key:<key>"` — a string nothing
consumed — so wiring a rebind button to it did nothing, silently. Issue 456 removed that decoy: it now
lowers to a **diagnostic that says it is not interpreted**. Surface it with `AdapterCmd.diagnostics`
(`AdapterCmd.productMessages` keeps only product messages and would drop it). Do not build a rebind on
it; build it as below.

### Capturing a key for a rebind (the path that works)

Host key capture needs no host capability, and this is why: **`MapKey` is a closure fixed when you
build your host record, and it never sees your model.** So a `MapKey` that *resolves* a key — like
`ViewerKeyboard.mapKeyOfKeymap` — resolves against the keymap it closed over, and drops both key-up
and **every key that keymap does not bind**. A rebind capture needs exactly what it drops: the key the
user presses next is, by definition, not bound yet. Drive `MapKey` from a keymap and the key you are
waiting for is the one key that never arrives.

Forward the key instead of resolving it, and do the routing in `update`, where your model is:

```fsharp
type Msg = Key of KeyId * isDown: bool     // every key arrives raw; YOU decide what it means

// The seam: forwards key-down AND key-up, bound or not. Nothing is dropped.
MapKey = ViewerKeyboard.mapKeyRaw (fun key isDown -> Some(Key(key, isDown)))

let update (Key(key, isDown)) model =
    if not isDown then model, [] else
    match model.Rebinding with                       // the command awaiting a new key
    | Some _ when key = "Escape" -> { model with Rebinding = None }, []          // your cancel policy
    | Some command ->
        { model with
            Keymap = model.Keymap |> Keymap.rebind key command                   // the capture FIRES
            Rebinding = None }, []
    | None ->
        match Keymap.resolve model.Keymap key with                               // ordinary play
        | Some command -> { model with Dispatched = command :: model.Dispatched }, []
        | None -> model, []
```

A capture is then an ordinary model transition, and the rebound key routes on the very next press —
same host, no reconstruction, no mutable closure. Pair it with the `KeyRebind` config-screen control
(`KeyRebind.ofKeymap` / `KeyRebind.onRebind`) to arm `Rebinding` from the UI.

## Generated Product

The app profile threads `mapKey` into `generatedHost` so the viewer routes input
through your pure reducer.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). If your product uses Spec Kit, record the findings
and resolving links under the feature's `specs/<feature>/feedback/` folder; otherwise record
them in this skill's **Sources** / durable-lessons line (and any product-local `docs/`
location). Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-skiaviewer]] — the host that delivers raw key events to `mapKey`.
- [[fs-gg-elmish]] — thread keyboard `Msg` values through the pure adapter.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (host input/runtime): https://github.com/mono/SkiaSharp
