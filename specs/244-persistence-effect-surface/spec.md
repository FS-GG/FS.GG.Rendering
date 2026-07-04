# Feature Specification: Persistence (save/load) effect surface + fs-gg-persistence product skill

**Feature Branch**: `244-persistence-effect-surface`

**Created**: 2026-07-04

**Status**: Draft

**Input**: User description: "Minimal save/load persistence capability + fs-gg-persistence product skill for the game default profile (closes cross-repo issue FS-GG/FS.GG.Rendering#93). Add a pure, dependency-light persistence effect surface following the existing effects-as-values, interpreted-at-the-host-boundary pattern (mirroring the Feature 243 audio surface): a versioned save envelope, Save/Load/DeleteSlot expressed as pure PersistenceEffect values returned from product update, and a record-only host interpreter that is headless-safe (records requested save/load effects as ordered evidence, never does real file I/O, never blocks, never throws). The pure model owns the payload/serialization choice; the framework does not own the on-disk format. A real file-backed host backend (SkiaViewer host) is deferred and will consume the same PersistenceEffect values without changing the surface. Add an fs-gg-persistence product skill gated to profile in [game, sample-pack], wired into the skill-manifest and template. Scope is the pure surface + record-only host seam + skill + template wiring, NOT a real file backend and NOT CI that reads/writes save files."

## Context

The `--provider rendering` scaffold defaults to `profile=game`. The vendored product skills
cover scene, symbology, layout, keyboard-input, styling, ui-widgets, elmish, skiaviewer,
game-core (deterministic loop / seeded RNG / AABB / culling), and — as of Feature 243 — audio.
There is **no save/load (persistence) surface or skill**: a game default that can render,
simulate, and make sound but cannot snapshot and restore its own state across runs is a
conspicuous hole for the flagship profile. Every non-trivial game needs save slots. This
feature closes cross-repo issue FS-GG/FS.GG.Rendering#93 with the triage decision "in scope —
minimal capability + skill" (the same treatment #92 audio received).

The repository already establishes a consistent discipline of modelling side effects as *pure
values requested by the model, interpreted at the host boundary* (`ViewerEffect`,
`KeyboardEffect`, `LayoutWorkflowEffect`, `TextInputEffect`, and now `AudioEffect`, each with a
host `interpret*` function). Persistence is an especially natural fit: `fs-gg-game-core` already
gives a deterministic, seeded, fixed-step simulation — exactly the pure state you would want to
snapshot and restore. The pure `update` never touches the filesystem; it only emits requests to
save/load/delete a slot, which the host interprets. This respects the constitution (Principle
IV: Elmish/MVU is the boundary for stateful/I-O; Principle VI: observability and safe failure).

Crucially, the framework does **not** own the on-disk format: the pure model serializes its own
`Model` to a payload of the product author's choosing and stamps a version; the persistence
surface carries that already-serialized payload as opaque data, exactly as symbology keeps the
per-game stat mapping out of the library.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Request a save/load from pure product code (Priority: P1)

A product author building a game-profile product wants "save to slot 1", "load slot 1 on the
title screen", and "delete a save slot" to be expressible from their pure `update`. They
serialize their pure `Model` into a versioned save envelope and return a persistence *request*
value — exactly as they already express scene/render, input, and audio effects — without
importing a filesystem API, blocking, or performing I/O inside `update`.

**Why this priority**: This is the core capability the issue asks for and the smallest slice
that delivers value. Without a pure request surface there is nothing for a skill to document or
a host to interpret. It is independently valuable even while the host backend is still a stub,
because the requested-effect values (with their carried version + slot + serialized payload) are
observable evidence that the game "asked to persist state."

**Independent Test**: Author a tiny pure model whose `update` returns persistence requests for a
set of game events (save on checkpoint, load on start, delete on "erase save"); assert the exact
sequence of requested persistence effect values — slot, version, and payload — produced by
driving the model through those events. No filesystem required.

**Acceptance Scenarios**:

1. **Given** a pure product model, **When** `update` handles a "checkpoint reached" event,
   **Then** it emits a `Save` request carrying a versioned envelope (version, slot, and the
   product-serialized payload), and performs no I/O.
2. **Given** a pure product model, **When** `update` handles a "continue game" event, **Then** it
   emits a `Load` request carrying the slot to read, and performs no I/O.
3. **Given** a pure product model, **When** `update` handles an "erase save" event, **Then** it
   emits a `DeleteSlot` request carrying the slot, and the model itself never references a
   filesystem path or stream.

### User Story 2 - Interpret persistence requests at the host boundary, safely headless (Priority: P1)

The host runtime receives the requested persistence effects and interprets them. In a headless
environment (CI, sandbox, no writable save location) interpretation MUST NOT fail or block — it
records the requested effects as ordered evidence instead of touching the filesystem. A real
file-backed host backend (SkiaViewer host) that actually writes/reads save files — and dispatches
a "loaded" result back to the model — is explicitly deferred; the seam must be shaped so it can
be added later without changing the pure surface.

**Why this priority**: The interpreter seam is what makes the requested values "real" and keeps
the design honest (Principle VI: safe failure). It is the second half of the MVP and, like the
existing no-GL/visual-evidence and headless-audio stories, must degrade gracefully when the
environment can't actually persist.

**Independent Test**: Feed a known sequence of persistence requests to the headless interpreter
and assert it records each request faithfully (order, slot, version, payload) and returns without
error and without attempting any filesystem access.

**Acceptance Scenarios**:

1. **Given** the headless interpreter, **When** it receives a batch of persistence requests,
   **Then** it records them in order and completes successfully without any writable save
   location present.
2. **Given** the headless interpreter, **When** a save location is unavailable, **Then** no
   exception escapes and the product continues to run.
3. **Given** the interpreter seam, **When** a real file-backed backend is added later, **Then**
   the pure persistence request surface (US1) does not have to change.

### User Story 3 - Discover and apply persistence via the fs-gg-persistence product skill (Priority: P2)

A product author (or the agent assisting them) scaffolds a game-profile product and finds an
`fs-gg-persistence` product skill materialized alongside the other product skills. The skill
teaches the persistence-request → host-interpret pattern, the recipe for versioning a save
envelope, and the discipline of keeping serialization in the product and I/O at the host; it
points at the shipped persistence request surface and is present only for the profiles where it
applies (`game`, `sample-pack`), matching how `fs-gg-game-core` and `fs-gg-audio` are delivered.

**Why this priority**: The skill is the consumer-facing deliverable that makes the capability
discoverable; it depends on US1 existing (so it has a concrete surface to cite) but is a
separable slice.

**Independent Test**: Scaffold a `profile=game` product and confirm an `fs-gg-persistence` skill
is materialized at the expected skill root; scaffold a `profile=app` product and confirm it is
NOT materialized. Confirm the skill references the actually-shipped persistence request surface.

**Acceptance Scenarios**:

1. **Given** `profile=game` (or `sample-pack`), **When** a product is scaffolded, **Then** an
   `fs-gg-persistence` skill is present at the product skill root and the manifest/template agree
   it should be.
2. **Given** `profile=app` (or `headless-scene`/`governed`), **When** a product is scaffolded,
   **Then** no `fs-gg-persistence` skill is materialized.
3. **Given** the materialized skill, **When** its references are followed, **Then** they resolve
   to the shipped persistence request surface (no dangling/aspirational API references) and use
   consumer-appropriate vocabulary (no framework-process leakage).

### Edge Cases

- **Load a slot that was never saved**: `Load` of an unknown slot is a request value, not a
  crash — the headless interpreter records the requested load; how a real backend reports "no
  such save" (a loaded-result message carrying "absent") is a deferred-backend concern, and the
  surface must not force the pure model to handle a filesystem error.
- **Save-format version mismatch on a future load**: The envelope carries a version so a future
  backend/product can migrate or reject an old save; the surface stamps the version, and the pure
  model owns the migration policy — the framework never interprets the payload.
- **Delete a slot with nothing in it**: `DeleteSlot` for an empty/absent slot is a well-defined
  no-op at the interpreter, not an error.
- **Opaque payload**: The serialized payload is carried as opaque data; the interpreter records
  it faithfully and never parses, validates, or re-encodes it (the product owns the format).
- **Manifest/template divergence**: If the skill is registered in one gate (manifest) but not the
  other (template condition), the parity/currency checks must catch it — the two must carry the
  same `profile in [game, sample-pack]` predicate.
- **Skill referenced by a non-persistence profile**: The skill must not leak into profiles that
  exclude it, and must not be counted as materialized there.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide a pure, dependency-light persistence *request* surface
  whose values represent requested save/load actions — at minimum: save a versioned envelope to
  a slot, load a slot, and delete a slot.
- **FR-002**: The persistence request surface MUST be usable from pure product `update` code
  without performing any I/O, filesystem access, or blocking — persistence is *requested* as a
  value only (Constitution Principle IV).
- **FR-003**: The save envelope MUST carry a save-format version, a target slot, and a payload
  that is **opaque to the framework** (the product author serializes their own `Model`); the
  framework MUST NOT define, parse, or validate the payload's format.
- **FR-004**: Public visibility of the persistence surface MUST be expressed through its
  signature (`.fsi`) file, consistent with the repository's visibility discipline (Constitution
  Principle II), and the surface MUST be documented as an honest, non-stub API for the parts that
  ship.
- **FR-005**: The framework MUST provide a host-boundary interpreter seam that consumes the
  requested persistence effects, following the existing `interpret*`-at-the-host pattern.
- **FR-006**: In a headless / no-writable-location environment the interpreter MUST record the
  requested effects as observable, ordered evidence (preserving slot, version, and payload),
  complete without error, and never block or require a filesystem (Constitution Principle VI:
  safe failure). Evidence derives from the *requested* values, not from actual file contents.
- **FR-007**: The interpreter seam MUST be shaped so a real file-backed backend — including
  returning a load *result* back to the model as a message — can be added later without changing
  the pure request surface (FR-001).
- **FR-008**: An `fs-gg-persistence` product skill MUST be authored, mirroring the structure of
  the `fs-gg-game-core` / `fs-gg-audio` product skills, teaching the persistence-request →
  host-interpret pattern, the versioned-envelope recipe, and citing the shipped surface.
- **FR-009**: The `fs-gg-persistence` skill MUST be gated to `profile in [game, sample-pack]` and
  MUST be registered consistently in **both** the skill-manifest (`materializes-when` predicate
  with a content hash) and the template copy condition — the two MUST carry the same predicate.
- **FR-010**: The persistence request surface MUST be made available to the generated product
  only for the `game`/`sample-pack` profiles (a profile-gated reference), and MUST NOT be pulled
  into profiles that exclude it.
- **FR-011**: Consumer-active wrapper aliases for the skill (the `fs-gg-product-persistence`
  variant) MUST be provided consistent with how the other product skills expose their
  consumer-active wrappers, so the skill is discoverable in a scaffolded product.
- **FR-012**: The skill's references MUST resolve to the shipped surface (no dangling API
  references) and MUST use consumer-appropriate vocabulary (no framework-process leakage),
  consistent with the skill de-leak / currency checks already applied to the other product
  skills.
- **FR-013**: The feature MUST decide and record whether persistence warrants a capability
  catalog row (the `template/capabilities.yml` catalog), following whichever precedent audio /
  game-core set; the chosen treatment MUST be internally consistent with that precedent.
- **FR-014**: Existing scaffold profiles that do NOT include persistence (e.g. `app`,
  `headless-scene`, `governed`) MUST remain byte-unchanged with respect to persistence — no
  persistence surface, skill, or reference appears in them.
- **FR-015**: Semantic tests MUST cover the pure request surface (US1) and the headless
  interpreter (US2), and the skill materialization gating (US3) MUST be verifiable by the
  existing manifest/template parity and skill-currency checks (Constitution Principles I & V).

### Out of Scope

- A real file-backed backend that actually reads/writes save files, and the load-result message
  it would dispatch back to the model. Deferred behind the interpreter seam.
- Any CI that reads or writes actual save files.
- Persistence for profiles other than `game`/`sample-pack`.
- Serialization format ownership: encoding/decoding the `Model`, schema migration engines,
  compression, encryption, cloud/remote save sync, or a save-file browser UI.

### Key Entities *(include if feature involves data)*

- **Persistence request (persistence effect)**: A pure value describing a requested save/load
  action. Variants at minimum: save (a versioned envelope), load (a slot), delete (a slot).
  Carries only data — never a filesystem handle, stream, or callback.
- **Save slot**: An opaque identifier the product uses to name a save location (e.g. "slot-1",
  "autosave"); the framework does not own the slot→path mapping (kept out of the library,
  mirroring how per-game stat mapping is kept out of symbology).
- **Save envelope**: The versioned wrapper the product author fills in — a save-format version, a
  target slot, and an opaque, product-serialized payload. The framework carries it but never
  parses the payload.
- **Recorded persistence evidence**: The ordered list of requested persistence effects captured
  by the headless interpreter, used as proof that the product requested to persist/restore state.
- **fs-gg-persistence skill**: The product skill document delivered to `game`/`sample-pack`
  scaffolds, plus its consumer-active wrapper alias and its manifest/template registration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product author can make a game event request a save, a load, and a delete
  entirely from pure code, demonstrated by a semantic test that asserts the exact sequence of
  requested persistence effects (slot, version, payload) with zero I/O performed in `update`.
- **SC-002**: The headless interpreter processes a batch of persistence requests and records 100%
  of them, in order, with slot/version/payload preserved, no errors, and no filesystem
  dependency — verifiable in CI with no writable save location.
- **SC-003**: Scaffolding a `game` (or `sample-pack`) profile yields exactly one materialized
  `fs-gg-persistence` skill at the expected root; scaffolding any non-persistence profile yields
  zero — with manifest and template in agreement (parity check green).
- **SC-004**: All persistence surface members referenced by the skill resolve to shipped API (no
  dangling references), and the skill passes the existing skill-currency / de-leak checks.
- **SC-005**: Non-persistence profiles are unchanged with respect to persistence (no new files,
  skill, or references in `app`/`headless-scene`/`governed`).
- **SC-006**: The catalog-row decision (FR-013) is recorded and the repository is internally
  consistent with it (either a persistence capability row exists and is exercised, or the
  skill-only treatment is documented mirroring the audio/game-core precedent).

## Assumptions

- "Users" of this feature are product authors scaffolding a `game`/`sample-pack` FS.GG.UI product
  and the coding agents assisting them; the primary consumer surface is the pure persistence
  request API plus the delivered skill.
- The minimal request vocabulary (save-envelope / load-slot / delete-slot) is sufficient for the
  game-default gap; richer persistence (enumerate slots, save metadata/thumbnails, migration
  helpers) is deferred and can extend the request DU additively later without breaking the seam.
- The save payload is opaque, already-serialized data owned by the product author; the framework
  carries it and stamps/reads only the version and slot, never the payload's internal format.
  This keeps the surface non-generic and format-agnostic.
- The persistence request surface is dependency-light and can live in an existing shipped project
  (mirroring how game-core and audio reuse Canvas/Scene) or a small new project; the placement
  decision is a plan-phase concern, constrained only by "no filesystem dependency ships."
- The headless interpreter's "record the requests" evidence model mirrors the established
  no-GL / visual-evidence and headless-audio approaches, so evidence tooling and CI posture are
  consistent with existing lanes.
- The natural snapshot target is the `fs-gg-game-core` deterministic/seeded model; the skill will
  reference game-core as the state to serialize, but persistence does not depend on game-core
  being present in a product.
- No real file-backed backend, filesystem dependency, or save-file-touching CI is introduced by
  this feature; those are explicitly deferred behind the interpreter seam.
