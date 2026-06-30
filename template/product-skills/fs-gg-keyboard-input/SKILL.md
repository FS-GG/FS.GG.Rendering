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
seed it as the consumer path.

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

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to assert binding resolution and command effects.

## Evidence

Record keyboard command and state evidence under this product's `readiness/`
paths. Do not copy framework readiness reports into the product.

## Package Boundary

Keep key reduction pure; the host delivers raw key events and interprets
`RequestHostKeyCapture` through the viewer, not inside `Keyboard.update`.

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
