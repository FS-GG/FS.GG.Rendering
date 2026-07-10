# Feature Specification: The audio host seam — a generated game can play what it requests

**Feature Branch**: `item/245-audio-host-seam`

**Created**: 2026-07-10

**Status**: Implemented

**Input**: GitHub issue [FS.GG.Rendering#245](https://github.com/FS-GG/FS.GG.Rendering/issues/245) —
"[cross-repo] ViewerEffect has no audio case — a game-family product can request audio but never play it",
raised by Breakout1 (external consumer) via `FS-GG/.github` root-cause analysis.

## Context

A game-family product could **request** audio. It could never **play** it.

Spec `243-audio-effect-surface` shipped with its scope statement written plainly: *"the pure surface
+ host seam + skill + template wiring, **NOT a real audio backend**."* Separately, the standalone
`FS.GG.Audio` repo shipped a real `IAudioBackend`, an OpenAL device backend, a mixing engine, and an
Elmish bridge, at `fs-gg-audio` 0.1.0. Both repos were green. Both delivered exactly what they
specified.

**The connecting case was in neither spec.** `ViewerEffect` carried fourteen cases and none of them
was audio, so a generated product's pure `update` had no value it could return that would reach
`FS.GG.Audio.Host`. The game family launches through `Viewer.runApp`, not an Elmish `Program`, so
`Audio.Cmd` — an `Elmish.Cmd<'msg>` — had no loop to run in either. No generated template `.fs` source
referenced Audio at all; only `Product.fsproj` did, as four package references to code nothing called.

Meanwhile the shipped `fs-gg-audio` skill told authors: *"at runtime the host interprets the same
values into actual playback through `FS.GG.Audio.Host`, with no change to your `update`."* That was
not a lie about any line of code. It was a description of the **intended end state**, shipped
alongside the **partial delivery**, across a repo boundary where nobody's definition of done included
the join. Each side's gates were satisfied locally, so nothing failed.

Breakout1 worked around it by putting the frame's `AudioEffect list` on the model and asserting with
`Audio.interpret`. All eleven of its spec's SFX cues were requested correctly, and none of them played.

This feature builds the join. It is a coordination fix expressed as a contract change, not a bug fix.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A scaffolded game plays a sound without editing the host (Priority: P1)

A product author scaffolds `--profile game`, adds a case to the cue map saying "a save makes a click",
drops a WAV next to it, and runs. They hear the click. They never open `Program.fs`, never name a
device, never write `unit -> unit` into `update`.

**Why this priority**: this is the capability the issue says does not exist. Everything else in this
feature is machinery in service of it.

**Acceptance**:
- `ViewerEffect` carries an audio case, so a pure `update` has a value that reaches the host.
- The generated game template wires a real backend to that case, in the durable file, shipped.
- A product's own audio decisions live in a *replaceable* product file, not in framework code.

### User Story 2 - The headless and evidence paths stay hardware-free and deterministic (Priority: P1)

CI, the offscreen evidence surface, and a developer on a box with no sound card all run the same
product and observe the same requests, deterministically, with no device and no window.

**Why this priority**: the repo's entire test discipline depends on it. An audio seam that makes CI
depend on an audio device would be a worse defect than the one it fixes.

**Acceptance**:
- The launch path that opens no window plays nothing, and never throws for want of a device.
- Requested audio is assertable as an ordered value, with no window and no device.
- `Viewer.runApp` — the silent, pre-existing path — behaves exactly as before.

### User Story 3 - The skill tells the truth about what ships (Priority: P2)

An author reading `fs-gg-audio` finds a runtime-playback claim that the code honours, and a named path
from `update` to the device.

**Why this priority**: the skill's claim is the artifact that misled the consumer. Leaving it while
fixing the code would fix the symptom and keep the cause.

**Acceptance**:
- The skill names the two files that carry the seam and the sink that drives it.
- Every `Audio.<member>` the skill cites resolves in a surface the template actually bundles.

## Requirements *(mandatory)*

- **FR-001**: `ViewerEffect` MUST gain a case carrying a batch of `AudioEffect` values, in dispatch
  order, as pure data — no device handle, no stream, no effectful closure.
- **FR-002**: A generated product's `update` MUST be able to reach `FS.GG.Audio.Host` without editing
  the durable `Program.fs`.
- **FR-003**: The rendering core MUST NOT acquire a device dependency. `FS.GG.UI.SkiaViewer` may
  depend on the data-only `FS.GG.Audio.Core`; it MUST NOT depend on `FS.GG.Audio.Host`/`.Engine`.
- **FR-004**: The backend's lifetime MUST belong to the caller, not to the viewer.
- **FR-005**: `runApp` and `runAppWithWindowBehavior` MUST stay intact and keep their behaviour, and
  every existing `GeneratedAppHost` literal MUST keep compiling unchanged (FR-006 of spec 085).
- **FR-006**: A frame's requested audio MUST be observable as an ordered pure value, hardware-free.
- **FR-007**: The `fs-gg-audio` skill's runtime-playback claim MUST be made true, and the surface it
  cites MUST be bundled with the template.
- **FR-008**: Spec `244-persistence-effect-surface` MUST NOT land its record-only host seam until this
  seam exists, or it reproduces this defect on a second capability.

## Out of scope

- Mixing/bus/ducking/3D semantics — owned by `FS.GG.Audio.Engine`, already shipped.
- The `Audio.Cmd` Elmish bridge — the game family does not run an Elmish `Program`.
- An audio case on the pointer-aware `InteractiveViewerHost`. Audio is a game-family seam; the
  interactive host discards `PlayAudio`, as it discards the other effects it does not interpret.
- Bundling audio assets with the template. `SoundId -> bytes` is product-owned by contract.
- CI that plays sound. The record-only backend is the CI backend, as it was before.

## Success Criteria *(mandatory)*

- **SC-001**: A `PlayAudio` batch emitted from `update` arrives at an `IAudioBackend`, asserted in a
  test that opens no window and no device.
- **SC-002**: The public surface grows additively — no member removed, none resigned.
- **SC-003**: The full solution test suite and the release-only `Package.Tests` gates stay green.
- **SC-004**: `Viewer.runApp`'s behaviour is unchanged: it discards audio and opens the same window.
