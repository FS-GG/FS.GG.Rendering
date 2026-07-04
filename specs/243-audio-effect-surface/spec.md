# Feature Specification: Audio effect surface + fs-gg-audio product skill

**Feature Branch**: `243-audio-effect-surface`

**Created**: 2026-07-04

**Status**: Draft

**Input**: User description: "Minimal audio capability + fs-gg-audio product skill for the game default profile (closes cross-repo issue #92). Add a pure, dependency-light audio effect surface following the existing effects-as-values, interpreted-at-the-host-boundary pattern; a host interpreter seam that is a record-only stub in headless environments (no real audio backend yet); an fs-gg-audio product skill gated to profile in [game, sample-pack], wired into the skill-manifest and template; scope is the pure surface + host seam + skill + template wiring, NOT a real audio backend and NOT CI that plays sound."

## Context

The `--provider rendering` scaffold defaults to `profile=game`. The vendored product skills
cover scene, symbology, layout, keyboard-input, styling, ui-widgets, elmish, skiaviewer, and
game-core (deterministic loop / seeded RNG / AABB / culling). There is **no audio surface or
skill**: a game default that can render and simulate but cannot make a sound is a conspicuous
hole for the flagship profile. This feature closes cross-repo issue FS-GG/FS.GG.Rendering#92
with the triage decision "in scope — minimal capability + skill."

The repository already establishes a consistent discipline of modelling side effects as *pure
values requested by the model, interpreted at the host boundary* (`ViewerEffect`,
`KeyboardEffect`, `LayoutWorkflowEffect`, `TextInputEffect`, each with a host `interpret*`
function). Audio is a natural fit for exactly this pattern: the pure `update` never touches an
audio device; it only emits requests to play/stop sound, which the host interprets. This also
respects the constitution (Principle IV: Elmish/MVU is the boundary for stateful/I-O; Principle
VI: observability and safe failure).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Request a sound from pure product code (Priority: P1)

A product author building a game-profile product wants a unit's firing, a coin pickup, or a
menu confirmation to make a sound. They express the sound as a value returned from their pure
`update` (a requested audio effect), exactly as they already express scene/render and input
effects — without importing an audio device, blocking, or performing I/O inside `update`.

**Why this priority**: This is the core capability the issue asks for and the smallest slice
that delivers value. Without a pure request surface there is nothing for a skill to document or
a host to interpret. It is independently valuable even while the host backend is still a stub,
because the requested-effect values are observable evidence that the game "asked for sound."

**Independent Test**: Author a tiny pure model whose `update` returns audio requests for a set
of game events; assert the exact sequence of requested audio effect values produced by driving
the model through those events. No sound device required.

**Acceptance Scenarios**:

1. **Given** a pure product model, **When** `update` handles a "fire" event, **Then** it emits
   a `PlaySfx` request carrying the sound id and a volume, and performs no I/O.
2. **Given** a pure product model, **When** `update` handles a "enter level" event, **Then** it
   emits a `PlayMusic` request carrying a track id and a loop flag.
3. **Given** a pure product model, **When** `update` handles a "pause"/"mute" event, **Then** it
   emits `StopMusic` / `SetMasterVolume` requests, and the model itself never references an
   audio device.

### User Story 2 - Interpret audio requests at the host boundary, safely headless (Priority: P1)

The host runtime receives the requested audio effects and interprets them. In a headless
environment (CI, no audio device) interpretation MUST NOT fail or block — it records the
requested effects as evidence instead of producing sound. A real windowed-host backend that
actually plays audio is explicitly deferred; the seam must be shaped so it can be added later
without changing the pure surface.

**Why this priority**: The interpreter seam is what makes the requested values "real" and keeps
the design honest (Principle VI: safe failure). It is the second half of the MVP and, like the
existing no-GL/visual-evidence story, must degrade gracefully when the environment can't
actually render/play.

**Independent Test**: Feed a known sequence of audio requests to the headless interpreter and
assert it records each request faithfully (order, ids, volumes) and returns without error and
without attempting device access.

**Acceptance Scenarios**:

1. **Given** the headless interpreter, **When** it receives a batch of audio requests, **Then**
   it records them in order and completes successfully without a sound device present.
2. **Given** the headless interpreter, **When** an audio device is unavailable, **Then** no
   exception escapes and the product continues to run.
3. **Given** the interpreter seam, **When** a real backend is added later, **Then** the pure
   audio request surface (US1) does not have to change.

### User Story 3 - Discover and apply audio via the fs-gg-audio product skill (Priority: P2)

A product author (or the agent assisting them) scaffolds a game-profile product and finds an
`fs-gg-audio` product skill materialized alongside the other product skills. The skill teaches
the audio request → host-interpret pattern, points at the shipped audio request surface, and is
present only for the profiles where it applies (`game`, `sample-pack`), matching how
`fs-gg-game-core` is delivered.

**Why this priority**: The skill is the consumer-facing deliverable that makes the capability
discoverable; it depends on US1 existing (so it has a concrete surface to cite) but is a
separable slice.

**Independent Test**: Scaffold a `profile=game` product and confirm an `fs-gg-audio` skill is
materialized at the expected skill root; scaffold a `profile=app` product and confirm it is
NOT materialized. Confirm the skill references the actually-shipped audio request surface.

**Acceptance Scenarios**:

1. **Given** `profile=game` (or `sample-pack`), **When** a product is scaffolded, **Then** an
   `fs-gg-audio` skill is present at the product skill root and the manifest/template agree it
   should be.
2. **Given** `profile=app` (or `headless-scene`/`governed`), **When** a product is scaffolded,
   **Then** no `fs-gg-audio` skill is materialized.
3. **Given** the materialized skill, **When** its references are followed, **Then** they resolve
   to the shipped audio request surface (no dangling/aspirational API references) and use
   consumer-appropriate vocabulary (no framework-process leakage).

### Edge Cases

- **Volume out of range**: A requested volume outside the normal range is a value, not a
  crash — the surface documents/normalizes the accepted range; the interpreter never throws on
  it.
- **Music request while music already playing**: A new `PlayMusic` request supersedes the
  previous one; the recorded evidence reflects the sequence, and the pure model owns the
  policy (the surface does not silently dedupe).
- **Stop with nothing playing**: `StopMusic` when no track is active is a well-defined no-op at
  the interpreter, not an error.
- **Manifest/template divergence**: If the skill is registered in one gate (manifest) but not
  the other (template condition), the parity/currency checks must catch it — the two must carry
  the same `profile in [game, sample-pack]` predicate.
- **Skill referenced by a non-audio profile**: The skill must not leak into profiles that
  exclude it, and must not be counted as materialized there.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide a pure, dependency-light audio *request* surface whose
  values represent requested sounds — at minimum: play a sound effect (with a sound id and a
  volume), play music (with a track id and a loop flag), stop music, and set a master volume.
- **FR-002**: The audio request surface MUST be usable from pure product `update` code without
  performing any I/O, device access, or blocking — audio is *requested* as a value only
  (Constitution Principle IV).
- **FR-003**: Public visibility of the audio surface MUST be expressed through its signature
  (`.fsi`) file, consistent with the repository's visibility discipline (Constitution
  Principle II), and the surface MUST be documented as an honest, non-stub API for the parts
  that ship.
- **FR-004**: The framework MUST provide a host-boundary interpreter seam that consumes the
  requested audio effects, following the existing `interpret*`-at-the-host pattern.
- **FR-005**: In a headless / no-audio-device environment the interpreter MUST record the
  requested effects as observable evidence, complete without error, and never block or require a
  sound device (Constitution Principle VI: safe failure). Evidence derives from the *requested*
  values, not from actual sound output.
- **FR-006**: The interpreter seam MUST be shaped so a real audio backend can be added later
  without changing the pure request surface (FR-001).
- **FR-007**: An `fs-gg-audio` product skill MUST be authored, mirroring the structure of the
  `fs-gg-game-core` product skill, teaching the audio-request → host-interpret pattern and
  citing the shipped surface.
- **FR-008**: The `fs-gg-audio` skill MUST be gated to `profile in [game, sample-pack]` and MUST
  be registered consistently in **both** the skill-manifest (`materializes-when` predicate with
  a content hash) and the template copy condition — the two MUST carry the same predicate.
- **FR-009**: The audio request surface MUST be made available to the generated product only for
  the `game`/`sample-pack` profiles (a profile-gated reference), and MUST NOT be pulled into
  profiles that exclude it.
- **FR-010**: Consumer-active wrapper aliases for the skill (the `fs-gg-product-audio` variant)
  MUST be provided consistent with how the other product skills expose their consumer-active
  wrappers, so the skill is discoverable in a scaffolded product.
- **FR-011**: The skill's references MUST resolve to the shipped surface (no dangling API
  references) and MUST use consumer-appropriate vocabulary (no framework-process leakage),
  consistent with the skill de-leak / currency checks already applied to the other product
  skills.
- **FR-012**: The feature MUST decide and record whether audio warrants a capability catalog row
  (the `template/capabilities.yml` catalog), noting that game-core deliberately skips this and
  ships skill-only; the chosen treatment MUST be internally consistent with whichever precedent
  is followed.
- **FR-013**: Existing scaffold profiles that do NOT include audio (e.g. `app`,
  `headless-scene`, `governed`) MUST remain byte-unchanged with respect to audio — no audio
  surface, skill, or reference appears in them.
- **FR-014**: Semantic tests MUST cover the pure request surface (US1) and the headless
  interpreter (US2), and the skill materialization gating (US3) MUST be verifiable by the
  existing manifest/template parity and skill-currency checks (Constitution Principles I & V).

### Out of Scope

- A real audio backend that actually produces sound (device output, mixing, decoding, an audio
  library dependency). Deferred behind the interpreter seam.
- Any CI that plays or captures actual audio.
- Audio for profiles other than `game`/`sample-pack`.
- Asset pipeline / packaging of sound files, streaming, spatial/3D audio, DSP.

### Key Entities *(include if feature involves data)*

- **Audio request (audio effect)**: A pure value describing a requested sound action. Variants
  at minimum: play sound-effect (sound id + volume), play music (track id + loop), stop music,
  set master volume. Carries only data — never a device handle or callback.
- **Sound id / track id**: An opaque identifier the product uses to name a sound or music track;
  the framework does not own the id→asset mapping (kept out of the library, mirroring how
  per-game stat mapping is kept out of symbology).
- **Recorded audio evidence**: The ordered list of requested audio effects captured by the
  headless interpreter, used as proof that the product requested sound.
- **fs-gg-audio skill**: The product skill document delivered to `game`/`sample-pack` scaffolds,
  plus its consumer-active wrapper alias and its manifest/template registration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product author can make a game event request a sound entirely from pure code,
  demonstrated by a semantic test that asserts the exact sequence of requested audio effects
  with zero I/O performed in `update`.
- **SC-002**: The headless interpreter processes a batch of audio requests and records 100% of
  them, in order, with no errors and no sound-device dependency — verifiable in CI with no audio
  hardware.
- **SC-003**: Scaffolding a `game` (or `sample-pack`) profile yields exactly one materialized
  `fs-gg-audio` skill at the expected root; scaffolding any non-audio profile yields zero — with
  manifest and template in agreement (parity check green).
- **SC-004**: All audio surface members referenced by the skill resolve to shipped API (no
  dangling references), and the skill passes the existing skill-currency / de-leak checks.
- **SC-005**: Non-audio profiles are unchanged with respect to audio (no new audio files, skill,
  or references in `app`/`headless-scene`/`governed`).
- **SC-006**: The catalog-row decision (FR-012) is recorded and the repository is internally
  consistent with it (either an audio capability row exists and is exercised, or the skill-only
  treatment is documented mirroring game-core).

## Assumptions

- "Users" of this feature are product authors scaffolding a `game`/`sample-pack` FS.GG.UI
  product and the coding agents assisting them; the primary consumer surface is the pure audio
  request API plus the delivered skill.
- The minimal request vocabulary (play-sfx / play-music / stop-music / set-master-volume) is
  sufficient for the game-default gap; richer audio (spatial, DSP, ducking) is deferred and can
  extend the DU additively later without breaking the seam.
- The audio request surface is dependency-light and can live in an existing shipped project
  (mirroring how game-core reuses Canvas/Scene) or a small new project; the placement decision
  is a plan-phase concern, constrained only by "no audio-device dependency ships."
- The headless interpreter's "record the requests" evidence model mirrors the established
  no-GL / visual-evidence approach already used for rendering, so evidence tooling and CI
  posture are consistent with existing lanes.
- Sound/track id → asset resolution is the product author's responsibility and is intentionally
  kept out of the framework library.
- No real audio backend, audio library dependency, or sound-playing CI is introduced by this
  feature; those are explicitly deferred behind the interpreter seam.
