# SVG-PRESENT-01 — Animation, browser audio and persistence

Status: active; SVG-PRESENT-01.1 implementation complete in its routine change, with .2 next after
merge readback. Route: routine.

Owners: FS.GG.Rendering owns portable animation/effect sampling and browser presentation/storage hosts.
FS.GG.Audio owns portable browser-audio realization. FS.GG.Game owns save compatibility and migration
semantics. FS.GG.Templates owns the generated product composition and installed journeys. This is order 8
of the accepted SVG programme and covers C11–C13 plus M6.1–M6.4.

## Outcome and boundary

A generated game can play and seek deterministic vector clips, reduce or bound decorative motion, emit
meaningful audio cues through a gesture-unlocked browser host, and transactionally preserve saves, project
documents, asset manifests and workspace preferences. Presentation clocks, audio handles and browser storage
remain outside gameplay authority. The last accepted value survives malformed data, unsupported versions,
quota failure, interruption and disposal.

SVG-PRESENT-01 does not publish packages or change public template pins. Replay integration remains
SVG-REPLAY-01, server persistence remains SVG-NETWORK-01, integrated scale limits remain SVG-SCALE-01, and
public authoring/runtime installation remains SVG-PREVIEW-B. S.I.R. remains strictly off limits.

## Baseline and contract choices

Planning inspected Rendering `50bb064c8acb0251a469ca406d193551f8e209d1`, Game
`c6de5b83eaa3d3f14909b42c3f8c3c94558157c9`, Audio
`ce63e5850b72f96fc0628b2919a4064d859d3936`, and Templates
`4a392a18ada74a87a12dd0b31aebaa7039a84b86`.

- Rendering already has pure single-tween opacity/transform/color sampling and retained browser cadence, but
  Animation is excluded from its curated Fable view and there is no clip, event-cue, loop or reduced-motion
  contract.
- Audio already has product-owned cue identifiers, buses, volumes, loops, fades, spatial pan, bounded voices,
  record-only evidence and native OpenAL realization. It has no curated Fable package or Web Audio host.
- Game session snapshots carry compatibility identity, but there is no general save family, migration plan or
  transactional autosave state machine.
- The generated player has real authority state and a disposable frame host. Its public package pins remain
  unchanged until Preview B.

Clips contain ordered, finite keyframes for position, rotation, scale, opacity, color and named scalar
properties. Seek is a pure sample at explicit time. Live cue collection uses a half-open time interval and
stable cue identity so a repeated frame cannot emit twice; seek and pause emit no historical cues. Loop and
ping-pong iteration are bounded values. Path morphing is accepted only for already-compatible topology.
Animation never mutates authority state.

The browser audio package consumes the existing `AudioEffect` vocabulary. Construction is silent until an
explicit user gesture resumes one owned `AudioContext`. Asset decode/readiness is explicit. Master, music,
effects and UI buses, mute, bounded one-shots, one loop, fades and stereo pan are realized with owned Web Audio
nodes. Replay seek does not replay historical cues. Audibility is a disclosed listening observation beside
automated lifecycle and meaningful-dispatch evidence.

Persistence keeps four independent schema families: project documents, asset manifests, saves and workspace
preferences. Game owns portable version acceptance, migration ordering, hashes and refusal state. Rendering's
browser host owns IndexedDB transactions, debounced autosave, recovery and quota/error reporting. Imports bind
member paths and SHA-256 identities, refuse traversal/missing members, and validate the complete replacement
before commit. A newer schema remains preserved and is never rewritten as an older one.

## Executable milestones

- [x] **SVG-PRESENT-01.1 — Portable clips, tracks and cue policy — route: routine**

  Owner: Rendering. Extend the existing Animation surface with validated clips, ordered keyframes, named
  easing, once/repeat/ping-pong playback, deterministic seek, compatible-topology path morph descriptors,
  event cues and explicit live/seek/pause cue policy. Curate the exact implementation into the Scene Fable
  package.

  Acceptance: position, rotation, scale, opacity, color and custom-scalar tracks sample reproducibly at
  boundaries and intermediate times. Duplicate/empty IDs, nonfinite values, unordered or out-of-range frames,
  type-mismatched tracks, invalid loop bounds and incompatible paths refuse without a partial clip. Identical
  .NET and Fable corpora cover seek, wrap, reverse, completion and cue interval edges. Existing settled
  animation remains byte-identical to static output.

  Evidence: `AnimationClip.validate` atomically checks clip/track/cue identities, finite duration and values,
  typed tracks, strict endpoint ordering, path point-count compatibility and bounded iteration counts.
  `AnimationClip.sample` deterministically covers once/repeat/ping-pong time, forward/reverse sampling and
  half-open live cue occurrences while seek/pause remain silent. All 131 Scene tests pass, including focused
  scalar/color/path, wrap, reverse, completion and multi-defect refusal cases. Isolated packed .NET and Fable
  consumers emit the same animation corpus at SHA-256
  `7efe479b02a077cd71dcffe343ff8fdb92c7d489d7bb17797171e167b3ea9bff`; the curated package closure contains
  no browser, Skia or native dependency.

- [ ] **SVG-PRESENT-01.2 — Retained animation and bounded effects host — route: routine**

  Owner: Rendering. Add a disposable SvgBrowser presentation host over the portable clip sampler and existing
  retained scene. Keep presentation revision separate from authority revision; schedule frames only while
  active. Apply reduced-motion policy, bound decorative effect count, preserve semantic facts and interaction
  targets, and expose cue batches without executing product commands.

  Acceptance: Chromium, Firefox and WebKit cover play, pause, seek, loop, transition, reduced motion, hidden-tab
  recovery, replacement, stale revision refusal and disposal. Reduced motion settles or substitutes declared
  decoration while preserving accessible state. Excess effects degrade decoration deterministically. No frame,
  listener, timer, DOM node or cue remains owned after disposal.

- [ ] **SVG-PRESENT-01.3 — Gesture-unlocked Web Audio host — route: routine**

  Owner: Audio. Curate the portable Core/Engine closure for Fable and add a browser package that realizes the
  existing effect vocabulary through Web Audio. Own unlock state, asset decode/readiness/errors, master/music/
  SFX/UI buses, volume/mute, one loop, bounded one-shots, fades, pan, pause and disposal. Keep the native OpenAL
  package and current semantics intact.

  Acceptance: headless .NET/Fable request and mixing corpora agree. Real browser contexts demonstrate locked
  refusal, gesture unlock, meaningful one-shot/loop/bus/mute/fade/pan dispatch, pause/resume, asset failure,
  voice stealing and complete node/context disposal. A disclosed listening fixture distinguishes automated
  graph evidence from audibility. No native/OpenAL edge enters the Fable closure.

- [ ] **SVG-PRESENT-01.4 — Versioned save and migration authority — route: routine**

  Owner: Game. Add portable save-family identities, content/asset hashes, migration steps and a pure autosave
  reducer. Bind saves to engine/profile/schema compatibility, preserve last-valid bytes, reject skips/cycles/
  downgrades, and make interruption/retry/replacement/disposal explicit. Curate the implementation into the
  Game.Core Fable package.

  Acceptance: .NET and Fable produce byte-identical accepted/refused migration and autosave transition corpora.
  Wrong engine/profile/content identity, unknown newer schema, corrupt bytes, stale completion, quota failure,
  cancellation and post-disposal observations preserve the last accepted value. Recovery resumes from the last
  committed migration step and never applies one step twice.

- [ ] **SVG-PRESENT-01.5 — Transactional IndexedDB and archive host — route: routine**

  Owner: Rendering. Add a disposable browser persistence host that interprets Game's reducer effects for the
  four independent storage families. Use one IndexedDB transaction per accepted replacement, bounded debounce,
  recoverable autosave and explicit quota/error state. Export/import archives validate normalized relative
  paths, declared members and SHA-256 hashes before one commit.

  Acceptance: Chromium, Firefox and WebKit cover first save, update, reload, interrupted transaction, stale
  completion, quota failure, database failure, corrupt/newer data, retry, export/import, traversal/missing/hash
  refusal and disposal. The previous committed value stays readable after every negative case. Browser and
  headless reducer observations agree on operation ordering.

- [ ] **SVG-PRESENT-01.6 — Generated player journey and Preview-B handoff — route: routine**

  Owner: Templates; producer repositories retain defect and completion ledgers. Compose clips/effects, cue
  dispatch, browser audio and persistence into the opt-in generated arena. Qualify direct, SDD, wizard-adopter
  and retained upgrade/rollback routes without changing public pins.

  Acceptance: an isolated generated player animates movement and outcome effects, honors reduced motion, unlocks
  audio from a real gesture, dispatches meaningful live cues without replaying them on seek, saves and reloads
  authority state, recovers from failed autosave and exports/imports a valid archive. Player output excludes
  Studio and native audio modules. Evidence binds exact producer and Templates revisions, package/archive
  identities, .NET/Fable correspondence, all three browsers, public baselines and migration/rollback results.

## Completion and release impact

After all six milestones meet their authority boundaries, record SVG-PRESENT-01 complete with publication
pending Preview B, update the unified projection and select SVG-PREVIEW-B. Candidate versions remain private
and unique to their bytes. No provider, registry, lifecycle or default changes occur in this feature.
