# Implementation Plan: The audio host seam

**Branch**: `item/245-audio-host-seam` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/253-audio-host-seam/spec.md`

## Summary

Give `ViewerEffect` an audio case and give the generated game host a way to realize it, without
letting a device dependency into the rendering core.

`FS.GG.UI.SkiaViewer` takes a package reference on **`FS.GG.Audio.Core` only** — the data-only
vocabulary (`AudioEffect`, `SoundId`, `TrackId`, `Bus`), no device, no OpenAL. `ViewerEffect` gains
`PlayAudio of AudioEffect list`. A new launch overload, `Viewer.runAppWithAudio`, hands each batch to
a caller-supplied `AudioEffect list -> unit` sink in dispatch order. The generated template supplies
the sink from `FS.GG.Audio.Host` — which it already references — so the backend, and its lifetime, sit
on the product's side of the boundary.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**New dependency**: `FS.GG.Audio.Core` 0.1.0, pinned in `Directory.Packages.local.props` (the
repo-owned file; the root `Directory.Packages.props` is org-synced from `FS-GG/.github` and must not
be edited). `FS.GG.Audio.Host` 0.1.0 is added **test-only**, so `SkiaViewer.Tests` can drive the real
record-only backend.

**Version axis**: `FS.GG.Audio` releases on its own cadence (ADR-0024). This edge consumes 0.1.0, an
already-published stable; no release of another repo is required, which is why this lands as an
intra-repo item rather than a cross-repo sequence.

## Key decision: the sink is a launch parameter, not a `GeneratedAppHost` field

The issue offered two shapes: an audio case on `ViewerEffect`, **or** an `Effects: 'model ->
AudioEffect list` seam on `GeneratedAppHost`. We took the first, and then had to choose where the
*backend* attaches.

Adding a field — even an optional one — to `GeneratedAppHost` breaks **every record literal that
constructs it**, because F# record construction requires all fields. That is roughly fifteen sites
across `samples/`, `tests/`, the template, and the docs, most of which never make a sound. It would
also contradict an invariant this codebase asserts in three places: *"`runApp`/`GeneratedAppHost` are
untouched (FR-006)"*.

A launch overload breaks nothing, and the codebase already has the exact precedent:
`runAppWithWindowBehavior` is `runApp` plus one parameter, with `runApp` delegating to it. So:

```
runGeneratedApp options behavior audioSink host      // private, the one body
  ├── runAppWithWindowBehavior options behavior host           = ... ignore   (unchanged)
  ├── runApp options host                                      = ... ignore   (unchanged)
  ├── runAppWithAudio options audioSink host                   = ... audioSink
  └── runAppWithWindowBehaviorAndAudio options behavior sink host
```

`runApp` passing `ignore` is what makes FR-005 true by construction rather than by promise.

The rejected `Effects: 'model -> AudioEffect list` shape has a second problem beyond the churn: it is
a *projection of the model*, drained per frame, so a one-shot sound effect replays on every frame
until the model is manually cleared. Dispatch order lives nowhere. `PlayAudio` on the effect list
inherits ordering from the effect list, which the host already interprets in order.

## The template's three files

| File | Durability | Role |
|---|---|---|
| `src/Product/AudioCues.fs` | **replaceable** | `forTransition : Msg -> Model -> Model -> AudioEffect list` — the one place the product decides what to play. Names your `Msg` cases, so a model swap rewrites it. Also carries `resolver`, the product-owned `id -> WAV bytes` mapping. |
| `src/Product/EvidenceCommands.fs` | durable | lifts each frame's cues onto `ViewerEffect.PlayAudio`. |
| `src/Product/Program.fs` | durable | creates the backend once, passes `Audio.play backend` as the sink. |

`OpenAlBackend.create` degrades to the record-only `NullBackend` when OpenAL or the device is absent
and never throws into game code, so the durable line is safe headless and in CI (FR-004 of the
FS.GG.Audio spec). An unresolved `SoundId` resolves to `None`, which the backend records as a no-op —
a game with no assets yet still runs and still requests the right sounds.

Wiring is gated to `profile == "game" || profile == "sample-pack"`, matching the existing gate on the
four `FS.GG.Audio.*` package references (asserted by `AudioProfileWiringTests`, G-GATE).

## Blast radius outside the seam

- **`src/Testing/TestingEvidence.fs`** required the generated product's default branch to contain the
  literal substring `Viewer.runApp viewerOptions generatedHost`. Its *intent* is "the default branch
  opens the interactive window through `generatedHost`", not one spelling, so it now accepts the
  audio-carrying overloads. A test pins that it still rejects a branch that launches nothing.
- **`AudioSkillSurfaceTests` (A-MEMBERS)** resolved every `Audio.<member>` the skill cites against
  `Audio.Core/Audio.fsi` alone — and its own comment named `Audio.play` as a member that *should*
  fail. Now that the skill honestly cites the host drive, `Audio.Host/Host.fsi` is bundled and
  A-MEMBERS resolves against both. This is a partial, load-bearing slice of issue #247.
- **Lockfiles**: a `PackageReference` on a core library restages `packages.lock.json` for all
  fourteen downstream projects. Additive only — one `FS.GG.Audio.Core` entry each.
- **Surface baselines** (`readiness/surface-baselines/`) gain one DU case and three members.

## Verification

Hardware-free, because the seam must be:

1. `GeneratedAppHost.audioRequests` flattens `PlayAudio` batches in dispatch order — unit-tested
   directly, and composed with `Audio.interpret` to yield the same `AudioEvidence` the record-only
   path always produced. This is the assertion Breakout1 could only make against its own model.
2. The **end-to-end** test drives the exact composition `Program.fs` installs — `Audio.play backend` —
   over `NullBackend`, and asserts the backend received the opening music then the keypress sfx, in
   order. This is the step that did not exist before this feature.
3. `runAppWithAudio` on a host that cannot open a window must fail exactly as `runApp` does and must
   not touch the sink. (Skips on a box with a display; runs on headless CI.)
