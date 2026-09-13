# SVG-RUNTIME-01 — Rendering owner ledger

Status: complete at the source/generated-candidate boundary. SVG-RUNTIME-01.4 is qualified by its routine
repository and browser-matrix gates, and SVG-RUNTIME-01.5 is merged with exact installed-player evidence.
Publication remains owned by SVG-PREVIEW-B. Route: routine.

## SVG-RUNTIME-01.4 — Browser clock and retained projection host

`SvgSessionPolicy` is the portable boundary between browser time/resources and the authoritative Game
session. It converts increasing requestAnimationFrame timestamps to integer microseconds once, permits only
one pending projection plus one coalesced replacement, and accepts retained projections only when both their
generation and revision are current. Pause, reset, replacement, suspension, blur, visibility loss and
disposal invalidate the old generation before a late reply can mutate presentation.

`SvgSessionHost` owns the animation frame and lifecycle listeners. Its callbacks interpret policy effects
through `SessionRuntime` and `SessionOperations`; product state and fixed-step catch-up remain in Game. A
suspension gap pauses authority and asks the caller for explicit recovery rather than banking background tab
time. Presentation revisions never feed back into authoritative state.

Acceptance evidence:

- Four .NET reducer tests cover variable cadence, integer conversion, bounded projection ownership,
  monotonic retained revisions, stale generations, suspension recovery and pause/step/reset/replace/dispose.
- `tests/SvgSessionCorrespondence/run.sh` packs this repository with exact Game revision
  `e72be2d50dcecfe50b73b55942e0d351e46e9ebf`; .NET and Fable produce the same 299-byte transition corpus
  (`sha256:569bc5541e77ca2f67f6c531af201ad5072a596eb454f13ec7cdfe98844aefb5`).
- The packaged browser fixture exercises real RAF cadence, projection coalescing, pause, single-step, reset,
  replacement, a stale reply, blur recovery and disposal in Chromium, Firefox and WebKit. Disposal reports
  zero owned listeners, frames and requests in every family.

The host adds no Game package dependency to the portable Scene layer and no clock, DOM or renderer dependency
to Game.Core. Candidate package versions remain private pending Preview B.

## SVG-RUNTIME-01.5 — Generated continuous player and Preview-B handoff

Game producer repair PR #625 merged as `c6de5b83eaa3d3f14909b42c3f8c3c94558157c9`. Templates PR #471
merged as `4a392a18ada74a87a12dd0b31aebaa7039a84b86`, consuming Rendering revision
`50bb064c8acb0251a469ca406d193551f8e209d1` after the native gamepad bridge repair.

The exact candidate gates passed generated authoring, input and runtime players in Chromium, Firefox and
WebKit, Orca/AT-SPI, package consumers, installed typed receivers and repository composition. The continuous
player exercises real semantic keyboard, pointer, touch and gamepad movement, authoritative pause/step/reset
and lose/win/restart behavior while excluding Studio modules. Public pins remained unchanged.
