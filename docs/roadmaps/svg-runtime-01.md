# SVG-RUNTIME-01 — Rendering owner ledger

Status: SVG-RUNTIME-01.4 complete, qualified by its routine repository and browser-matrix gates. The Game owner revision is
`e72be2d50dcecfe50b73b55942e0d351e46e9ebf` (FS.GG.Game#624). Templates owns SVG-RUNTIME-01.5 after this
merge. Route: routine.

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
