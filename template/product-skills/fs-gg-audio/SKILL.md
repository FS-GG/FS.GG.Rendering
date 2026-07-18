---
name: fs-gg-audio
description: Make a generated FS.GG.UI product request sound — audio as pure values (sfx/music/stop/volume/buses/3D) recorded at the host boundary, no device calls in update.
---

# Audio (Requested Sound) Capability

## Scope

Use this skill to give a product **sound**: firing a sound effect, starting or stopping music,
setting bus volumes, ducking, and positioning a sound in 3D. Audio here is **requested as pure
values** — your `update` returns `AudioEffect` values, it never touches an audio device. A
record-only interpreter folds the requests into ordered evidence, so the whole thing is
deterministic and testable with no sound hardware. Real device playback is the host's job, and it
now ships: `FS.GG.Audio.Host` carries the device seam and `FS.GG.Audio.Engine` the mixing. This
skill covers requesting sound and proving what was requested.

This skill materializes for the `app`, `game`, and `sample-pack` profiles — every profile that opens
a viewer window, and so every profile that can make a sound (FS.GG.Rendering#436). It is **not** a
game-only capability: a Controls app wants a click, a page turn, a save chime, and "every game with a
menu" was the motivating case for giving the Controls host family an audio sink at all
(FS.GG.Rendering#429). The `headless-scene` and `governed` profiles launch no viewer and get neither
the skill nor the packages.
<!-- skill-refs: closed-ok FS.GG.Rendering#436 — cited as the issue that WIDENED the profile set to `app`, not as somewhere to go. Closed is correct; it stays closed. -->
<!-- skill-refs: closed-ok FS.GG.Rendering#429 — cited as the issue that gave the Controls family an audio sink, not as somewhere to go. Closed is correct; it stays closed. -->

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Audio.Core/Audio.fsi` — the `AudioEffect` request DU, the `SoundId`/`TrackId`
  identifiers, the `Bus` DU, the `AudioEvidence` record, and the `Audio` module (smart constructors
  + the record-only `interpret`/`record`). Shipped in **`FS.GG.Audio.Core`**, referenced on the
  `app`, `game`, and `sample-pack` profiles.

`FS.GG.Audio` is its own component, released on its own `$(FsGgAudioVersion)` axis — independent of
`$(FsGgUiVersion)` and `$(FsGgGameVersion)`.

All helpers are **total**: volumes are clamped into `[0.0, 1.0]` at the boundary (even `nan`), and
no helper throws or performs I/O.

## Requesting sound from `update`

`AudioEffect` is a plain value you return from your pure `update` alongside your model change — the
same discipline as scene and input effects. Build effects with the smart constructors so carried
volumes are clamped:

```fsharp
open FS.GG.Audio.Core

type Msg = Fired | EnteredLevel | Paused | Muted

// update stays pure: it maps a Msg to a requested AudioEffect. No device, no IO.
let audioFor (msg: Msg) : AudioEffect =
    match msg with
    | Fired       -> Audio.playSfx (SoundId "fire") 0.8
    | EnteredLevel -> Audio.playMusic (TrackId "level1") true   // loop
    | Paused      -> Audio.stopMusic
    | Muted       -> Audio.setMasterVolume 0.0
```

`SoundId`/`TrackId` are opaque names **you** own — the framework does not map them to files. Resolve
them to real assets in your host layer, where the real playback backend runs.

### Buses, ducking, and 3D

Beyond the four basics, the vocabulary names a small set of mixer buses and three richer requests:

```fsharp
// Buses: Master | Music | Sfx | Ui | Ambient. Master scales every other bus.
Audio.setBusVolume Music 0.4                     // quieten the music bus
Audio.duck Music 0.3 250.0                       // duck it to 30% over 250 ms (e.g. under dialogue)
Audio.playSfx3D (SoundId "step") 3.0 0.0 0.0 0.9 // positioned; z = 0 is 2D stereo-pan-by-x
```

`Audio.setMasterVolume` targets the `Master` bus. `Audio.clampVolume` exposes the same clamping the
smart constructors apply, bounded by `Audio.minVolume` / `Audio.maxVolume`.

## Recording what was requested (headless-safe evidence)

`Audio.interpret` folds a batch of requests into `AudioEvidence` — the requested effects in dispatch
order, with volumes normalized. This is the record-only interpreter: it never blocks, never touches a
device, and is the evidence you assert on in tests. `Audio.record` appends a single effect to an
existing `AudioEvidence` (start from `Audio.emptyEvidence`) if you accumulate frame by frame.

```fsharp
open FS.GG.Audio.Core

let evidence =
    Audio.interpret
        [ Audio.playSfx (SoundId "fire") 0.8
          Audio.playMusic (TrackId "level1") true
          Audio.setMasterVolume 2.0 ]        // 2.0 clamps to 1.0

// evidence.Requested =
//   [ PlaySfx (SoundId "fire", 0.8); PlayMusic (TrackId "level1", true); SetMasterVolume 1.0 ]
```

## Common pitfalls

- **Calling an audio device inside `update`.** `update` must stay pure — return an `AudioEffect`
  value and let the host (or the record-only interpreter) act on it. I/O in `update` breaks
  determinism and testability.
- **Reaching for `FS.GG.UI.Canvas`.** The audio vocabulary used to live there. It was retired at the
  Canvas `0.3.0` major; `open FS.GG.Audio.Core` instead. Canvas now carries only the pure elements,
  the render loop, and the Persistence request surface.
- **Building `PlaySfx`/`SetMasterVolume` by hand with an unclamped volume.** Prefer `Audio.playSfx`
  / `Audio.setMasterVolume`; even if you don't, `interpret`/`record` normalize the carried volume, so
  recorded evidence is always in range.
- **Expecting `StopMusic` to be conditional.** The interpreter records exactly what you request; it
  does not dedupe or check "is anything playing." That policy is your product's — keep it in `update`.
- **Expecting actual sound in a headless test.** The record-only interpreter yields the *requested*
  values, and the device backend degrades to a null/record path with no hardware. Assert on
  `AudioEvidence.Requested`, not on audio output.
- **Expecting the initial model to make a sound.** It makes no *transition*, so `forTransition` is
  never called for it and anything the initial state implies — a restored volume, a menu track — is
  silently never requested. Handle it under `Started`; see
  [`Started` — the initial model makes no transition](#started--the-initial-model-makes-no-transition).

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned audio-request examples (assert the
`AudioEvidence.Requested` sequence your `update` produces for a set of events).

## Evidence

Record audio evidence (the requested-effect sequences for representative events) under this
product's `readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`AudioEffect`/`Audio`/`Bus` are in **`FS.GG.Audio.Core`** — a standalone component on its own
`$(FsGgAudioVersion)` axis. `Core` is BCL-only: the request vocabulary and the record-only
interpreter pull in no viewer, layout, widget, or rendering machinery. The rest of the component sits
behind that same edge. Keep device work out of `update` regardless of which piece you reach for.

The four packages are **not all on every profile** — check before you `open` one
(FS.GG.Rendering#436):

| Package | What it is | Profiles |
| --- | --- | --- |
| `FS.GG.Audio.Core` | the `AudioEffect` request vocabulary + record-only interpreter | `app`, `game`, `sample-pack` |
| `FS.GG.Audio.Host` | the `IAudioBackend` device seam (null/record + OpenAL) and `AssetResolver` | `app`, `game`, `sample-pack` |
| `FS.GG.Audio.Engine` | buses, fades, ducking, 3D | `game`, `sample-pack` |
| `FS.GG.Audio.Elmish` | the `Audio.Cmd` authoring bridge | `game`, `sample-pack` |

Core and Host are what *this skill's code* needs — the values you return from `forTransition`, and the
backend `Program.fs` hands the viewer — so they follow the skill onto every profile that opens a
window. Engine and Elmish are the simulation half; an `app` scaffold references neither, and nothing
above tells you to `open` them. To use named buses or 3D positioning on an `app` product, pin the
package in `Directory.Packages.props` on the `$(FsGgAudioVersion)` axis (as the `game` profile does)
and reference it from `Product.fsproj` — they are part of the same released component, not a
different one.

## Generated Product

Map each `Msg` that should make a sound to an `AudioEffect` in your `update`, collect the frame's
requests, and `Audio.interpret` them for evidence in tests; at runtime the host plays the same values
through `FS.GG.Audio.Host`, with no change to your `update`.

The seam is real and the scaffold ships it wired (FS.GG.Rendering#245). Two files carry it:
<!-- skill-refs: closed-ok FS.GG.Rendering#245 — cited as the issue that WIRED the seam, not as somewhere to go. Closed is correct; it stays closed. -->


- **`src/<ProductDir>/AudioCues.fs`** — *yours*. `forTransition : Msg -> Model -> Model -> AudioEffect list`
  is the one place the product decides what to play. Pure: a function of the message and the
  before/after model. Rewrite it when you swap the model; it names your `Msg` cases.
- **`src/<ProductDir>/Program.fs`** — *durable*. It creates a backend once and builds the sink the
  viewer's launcher takes:

```fsharp
open FS.GG.Audio.Host

// Opens a real device; degrades to the record-only Null backend when OpenAL or the
// device is missing, so this is safe headless and in CI. It never throws into product code.
use backend = OpenAlBackend.create AudioCues.resolver

// `Audio.play backend : AudioEffect list -> unit` is the sink. The viewer hands it every
// `ViewerEffect.PlayAudio` batch, in dispatch order.
let audioSink = Audio.play backend
```

The entry point that *accepts* that sink differs by profile — see
[the launch entry point is per family](#the-launch-entry-point-is-per-family--take-the-one-your-profile-launches-with)
below.

### Deriving cues from a model diff — most gameplay events have no `Msg`

The name `forTransition` invites you to `match` on the `Msg`, and for player-driven one-shots (a
fire button, a menu confirm) that is exactly right. But **most** gameplay cues — a shot leaving the
barrel, an enemy dying, a pickup consumed, a room sealing then clearing, a boss changing phase —
have **no `Msg` of their own**. They fall out of the simulation advancing under a single `Tick`, so
there is no message to match. You recover them instead by **diffing `previous` against `next`**:

```fsharp
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    | Started -> [ Audio.setMasterVolume next.Settings.Volume ]
    | Fired   -> [ Audio.playSfx (SoundId "fire") 0.8 ]        // a real Msg — match it directly
    | Tick _  ->
        // No Msg carries these. Read them out of the before/after model.
        [ if next.Score > previous.Score then Audio.playSfx (SoundId "score") 0.9
          if List.length next.Enemies < List.length previous.Enemies then
              Audio.playSfx (SoundId "enemy-death") 0.8 ]
```

This is why wiring event-driven audio into a scaffolded product needs **no new `Msg` case and no
`Model.fs` change**: the events are already latent in the model, and `forTransition`'s before/after
pair is where you read them off. The scaffold's `AudioCues.fs` ships the pattern — its Pong
`scored` / `bounced` helpers are exactly this previous-vs-next diff — so start from the shape it
gives you rather than inventing a message for every sound.

**The net-diff coverage boundary.** A single `Tick` does not advance the sim once — it drains a
whole *run* of fixed steps. The scaffold's `Model.advanceSim` folds the host's `dt` into an
accumulator and runs `stepSim` for **every whole fixed step** the accumulator has banked, so on a
slow frame that can be several steps at once. The `previous` and `next` handed to `forTransition`
therefore **straddle the entire drain**, not one step — you see where the sim *started* and where it
*ended up*, never the states in between.

So a cue works by model-diff **only if the state change it keys on survives to the end of the
drain**. That covers the real cues: a fired shot persists across frames, an enemy that died stays
dead, a score or a bounce is a durable state change — each still differs at `next`. The trap is an
event that both **appears and disappears inside one host frame**: a projectile spawned and consumed
within the same drain, a flag raised and cleared between two whole steps. It nets to **no diff**,
`forTransition` never sees it, and the cue silently never fires — no type is wrong and no test that
asserts on the model catches it, the same failure shape as the [`Started`
hole](#started--the-initial-model-makes-no-transition) below one level down. If you need to cue a
same-frame appear-and-vanish event, **give it its own `Msg`** and match that, rather than trying to
recover it from a diff that has already cancelled it out.

### `Started` — the initial model makes no transition

`forTransition` is a function of a **transition**, and the initial model does not make one: it comes
out of `initialModel`, and nothing is ever dispatched into it. So any sound the initial state
*implies* is never requested. That is a hole in the pattern rather than a bug in a function, and it
bites the moment you **load** state instead of transitioning into it.

Load the player's saved volume in `initialModel` and the model is correct — the setting genuinely
*is* loaded — and the mixer is never told, because no transition ever carried it there. Nothing
catches this: no type is wrong, and a test that asserts on the model **passes**, because from inside
the model a restored volume the mixer never heard is indistinguishable from one that was applied. It
reaches the player as *turn the music down, restart, and get full-volume music from a settings screen
that correctly reports it as quiet*. A save game, a restored window, a resumed session, a replayed
checkpoint — anything that enters the model through a door a transition-shaped seam is not watching
has this same shape.

`Started` is that door, and the scaffold ships it wired: the generated host dispatches it from `Init`
as `AudioCues.forTransition Started m m` — the **same** function `Update` calls, with the initial
model on both sides, so there is no separate startup-cue path to keep in sync. Put whatever the
initial state implies under it:

```fsharp
let forTransition (msg: Msg) (previous: Model) (next: Model) : AudioEffect list =
    match msg with
    // The initial model is LOADED, not transitioned into, so `Started` is the only chance state that
    // arrived that way has to reach the mixer: a restored volume, a menu track, a resumed session.
    | Started ->
        [ Audio.setMasterVolume next.Settings.Volume
          Audio.playMusic (TrackId "menu") true ]
    | Fired -> [ Audio.playSfx (SoundId "fire") 0.8 ]
```

Assert it **at the sink, not at the model**: the only test that catches this class asks what the
mixer was *told*, not what the model *holds* — which is exactly what `GeneratedAppHost.audioRequests`
(below) hands you. [[fs-gg-rendering:fs-gg-testing]] works the case end to end.

### The launch entry point is per FAMILY — take the one your profile launches with

The sink is the same value everywhere; only the entry point that accepts it differs. Reaching for the
game family's function on a Controls product (or vice versa) will not type-check, so this is the table
to read before you wire anything (FS.GG.Rendering#429, FS.GG.Rendering#436):

| Profile | Host record | Silent (discards audio) | **With sound** |
| --- | --- | --- | --- |
| `app` | `interactiveHost` | `ControlsElmish.runInteractiveApp` | **`ControlsElmish.runInteractiveAppWithAudio`** |
| `game`, `sample-pack` | `generatedHost` | `Viewer.runApp` | **`Viewer.runAppWithAudio`** |

Each takes the sink **between** `viewerOptions` and your host record, and each has a **silent** twin
that is the same call with that argument left out. Take one line: the family you scaffolded with, and —
all but always — the `*WithAudio` one.

```fsharp
// `app` — the Controls family. Host record: `interactiveHost`.
let appOutcome       = ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost
let appSilentOutcome = ControlsElmish.runInteractiveApp          viewerOptions           interactiveHost

// `game`, `sample-pack` — the viewer family. Host record: `generatedHost`.
let gameOutcome       = Viewer.runAppWithAudio viewerOptions audioSink generatedHost
let gameSilentOutcome = Viewer.runApp          viewerOptions           generatedHost
```

All four return `Result<ViewerLaunchOutcome, ViewerRunFailure>`, so a failed launch is an `Error`
**value** rather than an exception — `ViewerRunFailure` names the stage it blocked at, and that stage is
not always an early one (`WindowCreation`, but also `FirstFrameRender`, `ControlledExit`,
`ArtifactWrite`). Do not read `Ok` as *a window appeared*, either: `ViewerLaunchOutcome` is a **record
of what actually happened** — `WindowOpened`, `FirstFramePresented`, `CloseReason` — so the run that
reports itself is the thing to ask, not the `Result` tag. Neither type is audio-specific: a `*WithAudio`
launcher returns exactly what its silent twin does, which is why the block above can bind all four the
same way.

Each has a window-behavior sibling — `ControlsElmish.runInteractiveAppWithWindowBehaviorAndAudio` and
`Viewer.runAppWithWindowBehaviorAndAudio` — which slots the parsed `--window-*` request in ahead of
the sink, so the sink becomes the third argument rather than the second. The scaffold's `Program.fs`
already picks between the two by whether a window flag was supplied — you should not need to touch it.

These are not forks of the loop. Each `*WithAudio` entry point is the *same* message → update →
retained-step code path as its silent twin with the terminal viewer launcher swapped, so what you hear
cannot drift away from what the live loop actually did.

Between `AudioCues.fs` and `Program.fs`, `EvidenceCommands.fs` lifts each frame's cues onto
`ViewerEffect.PlayAudio`, which is the effect the viewer interprets. So a scaffolded product plays a
sound **without editing the host**: add a case to `AudioCues.forTransition` and drop a WAV at
`assets/audio/<id>.wav`.

`SoundId`/`TrackId` stay yours — `AudioCues.resolver` is the product-owned `id -> bytes` mapping, and
an id with no file resolves to `None`, which the backend records as a no-op rather than throwing. So a
product with no assets yet still runs, and still requests the right sounds.

Two escape hatches you rarely need. The silent entry points — the second line of each family in the
launcher block above — simply **discard** audio: use one when a product should make no sound at all.
And `GeneratedAppHost.audioRequests : ViewerEffect list -> AudioEffect list` flattens a frame's batches
in dispatch order, so a test can assert what was requested with no window and no device:

```fsharp
GeneratedAppHost.dispatchKey host keyEvent model
|> snd
|> GeneratedAppHost.audioRequests
|> Audio.interpret          // AudioEvidence — the same evidence the record-only path yields
```

The bundled surfaces: `docs/api-surface/Audio.Core/Audio.fsi` (the vocabulary) and
`docs/api-surface/Audio.Host/Host.fsi` (`IAudioBackend`, `Audio.play`, the backends).

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game:fs-gg-game-core]] — the simulation half of a game product; audio requests come from the same `update`.
- [[fs-gg-rendering:fs-gg-skiaviewer]] — the host window; audio's own device backend lives in `FS.GG.Audio.Host`.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map input to the `Msg` values whose `update` requests sound.
- [[fs-gg-rendering:fs-gg-scene]] — the visual half; audio and scene are both effects requested from `update`.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Effects-as-values / MVU background: https://guide.elm-lang.org/effects/
