---
name: fs-gg-audio
description: Make a generated FS.GG.UI product request sound — audio as pure values (sfx/music/stop/volume) recorded at the host boundary, no device calls in update.
---

# Audio (Requested Sound) Capability

## Scope

Use this skill to give a game/sim product **sound**: firing a sound effect, starting or stopping
music, and setting the master volume. Audio here is **requested as pure values** — your `update`
returns `AudioEffect` values, it never touches an audio device. A record-only interpreter folds the
requests into ordered evidence, so the whole thing is deterministic and testable with no sound
hardware. Real device playback is the host's job (a deferred backend); this skill covers requesting
sound and proving what was requested. This skill materializes for the `game` and `sample-pack`
profiles.

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Canvas/Audio.fsi` — the `AudioEffect` request DU, the `SoundId`/`TrackId`
  identifiers, the `AudioEvidence` record, and the `Audio` module (smart constructors + the
  record-only `interpret`/`record`). Shipped in `FS.GG.UI.Canvas`, referenced on the `game` and
  `sample-pack` profiles (the same package that carries the simulation primitives).

All helpers are **total**: volumes are clamped into `[0.0, 1.0]` at the boundary (even `nan`), and
no helper throws or performs I/O.

## Requesting sound from `update`

`AudioEffect` is a plain value you return from your pure `update` alongside your model change — the
same discipline as scene and input effects. Build effects with the smart constructors so carried
volumes are clamped:

```fsharp
open FS.GG.UI.Canvas

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
them to real assets in your host layer, exactly where the real playback backend will live.

## Recording what was requested (headless-safe evidence)

`Audio.interpret` folds a batch of requests into `AudioEvidence` — the requested effects in dispatch
order, with volumes normalized. This is the record-only interpreter: it never blocks, never touches a
device, and is the evidence you assert on in tests. `Audio.record` appends a single effect if you
accumulate frame by frame.

```fsharp
open FS.GG.UI.Canvas

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
  value and let the host (or, today, the record-only interpreter) act on it. I/O in `update` breaks
  determinism and testability.
- **Building `PlaySfx`/`SetMasterVolume` by hand with an unclamped volume.** Prefer `Audio.playSfx`
  / `Audio.setMasterVolume`; even if you don't, `interpret`/`record` normalize the carried volume, so
  recorded evidence is always in range.
- **Expecting `StopMusic` to be conditional.** The interpreter records exactly what you request; it
  does not dedupe or check "is anything playing." That policy is your product's — keep it in `update`.
- **Expecting actual sound in CI.** There is no real backend yet; the evidence is the *requested*
  values. Assert on `AudioEvidence.Requested`, not on audio output.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned audio-request examples (assert the
`AudioEvidence.Requested` sequence your `update` produces for a set of events).

## Evidence

Record audio evidence (the requested-effect sequences for representative events) under this
product's `readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`AudioEffect`/`Audio` are in `FS.GG.UI.Canvas` (referenced only on the `game`/`sample-pack`
profiles). Canvas depends only on Scene — the audio request surface pulls in no viewer, layout, or
widget machinery. Real device playback belongs in the host (`fs-gg-skiaviewer`), not in `update`.

## Generated Product

Map each `Msg` that should make a sound to an `AudioEffect` in your `update`, collect the frame's
requests, and `Audio.interpret` them for evidence today; when a real backend lands, the host will
interpret the same values into actual playback with no change to your `update`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game-core]] — the simulation half of a game product; audio requests come from the same `update`.
- [[fs-gg-skiaviewer]] — the host window where a real audio backend will interpret these requests.
- [[fs-gg-keyboard-input]] — map input to the `Msg` values whose `update` requests sound.
- [[fs-gg-scene]] — the visual half; audio and scene are both effects requested from `update`.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Effects-as-values / MVU background: https://guide.elm-lang.org/effects/
