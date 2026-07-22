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

## Capability boundary — choose the host the generated profile actually launches

Know this **before** you design a control scheme. A generated game with the turnkey shell
launches through **`InteractiveAppHost`**, because its menu and rebind rows need retained pointer
routing. `MapKey: ViewerKey -> bool -> 'msg option` is still the keyboard seam, and the `bool`
is load-bearing: forward both down and up through one normalized raw-key message. A down-only
adapter can pass capture/reducer tests while turning held movement into a one-shot tap.

Profiles without the shell may still use the keyboard-only `GeneratedAppHost`. Mouse input requires
the pointer-aware host driven by `Controls.Elmish.runInteractiveApp*`; preserve its `MapPointer`
and retained routing when adapting a generated shell rather than silently switching back to the
scene host.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to assert binding resolution and command effects. For an interactive
game, add a host-level down → fixed tick → fixed tick → up → fixed tick script and assert the
control advances on both held ticks and not after release. Direct reducer injection does not count.

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
lowers to a **diagnostic that says it is not interpreted**. Surface it by picking the
`ReportAdapterDiagnostic` effects out of the command — an `AdapterCommand<'msg>` **is** an
`AdapterEffect<'msg> list`, so this is a `List.choose` and needs no helper:

```fsharp
let diagnosticsOf (command: AdapterCommand<'msg>) : AdapterDiagnostic list =
    command |> List.choose (function ReportAdapterDiagnostic d -> Some d | _ -> None)
```

`AdapterCmd.productMessages` keeps only product messages and would drop it. Do not build a rebind on
it; build it as below.

### Capturing a key for a rebind (the path that works)

Host key capture needs no host capability, and this is why: **`MapKey` is a closure fixed when you
build your host record, and it never sees your model.** So a `MapKey` that *resolves* a key — like
`ViewerKeyboard.mapKeyOfKeymap` — resolves against the keymap it closed over, and drops both key-up
and **every key that keymap does not bind**. A rebind capture needs exactly what it drops: the key the
user presses next is, by definition, not bound yet. Drive `MapKey` from a keymap and the key you are
waiting for is the one key that never arrives.

Forward the key instead of resolving it, and do the routing in `update`, where your model is. `MapKey`
is just a function — `ViewerKey -> bool -> 'msg option` — so the forwarding seam is one you write, and
`ViewerKeyboard.toKeyId` is the only piece of the framework it needs:

```fsharp
type Msg = Key of KeyId * isDown: bool     // every key arrives raw; YOU decide what it means

// The seam: forwards key-down AND key-up, bound or not. It resolves nothing, so it drops nothing.
MapKey = fun key isDown -> Some(Key(ViewerKeyboard.toKeyId key, isDown))

let update (Key(key, isDown)) model =
    if not isDown then model, [] else
    match model.Rebinding with                       // the command awaiting a new key
    | Some _ when key = "Escape" -> { model with Rebinding = None }, []          // your cancel policy
    | Some command ->
        { model with
            // Player-facing rebinding is COMMAND replacement: remove the old key(s), displace
            // this key's previous action, and leave exactly one binding for the chosen command.
            Keymap = model.Keymap |> Keymap.replaceCommandBinding command key
            Rebinding = None }, []
    | None ->
        match Keymap.resolve model.Keymap key with                               // ordinary play
        | Some command -> { model with Dispatched = command :: model.Dispatched }, []
        | None -> model, []
```

A capture is then an ordinary model transition, and the rebound key routes on the very next press —
same host, no reconstruction, no mutable closure. `Keymap.assignKey` (and its compatibility name
`rebind`) are KEY-indexed upserts and deliberately preserve another key for the same command; do not
use either for a "change this action's key" UI. Pair command replacement with a stable
`KeyRebindAction` catalog and `KeyRebind.ofActions` / `KeyRebind.onActionRebind`: the catalog keeps
unbound actions visible, owns player labels/order/defaults, and exposes `onReset` plus
`restoreDefaults` rather than trying to recover UI state from `Keymap` lookup state. Use
`KeyRebind.actions` when composing the catalog attribute directly, and
`KeyRebind.withBindings` to project a live keymap onto the stable rows. `KeyRebind.ofKeymap`
remains a compatibility projection for binding-only screens; it cannot show an action that is
currently unbound or supply its player label, order, or default. Its matching compatibility
event mapper is `KeyRebind.onRebind`, for rows whose payload already is a command id.

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
