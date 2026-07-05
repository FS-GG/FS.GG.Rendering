# Feature Specification: Collision-Safe Vec2/Position in the Model Template

**Feature Branch**: `250-collision-safe-vec2`

**Created**: 2026-07-05

**Status**: Draft

**Input**: FS-GG/FS.GG.Rendering#138 (child of epic FS-GG/FS.GG.Rendering#137). Source: FS.GG framework development-feedback report — *Hollow Depths* build (`001-hollow-depths`), 2026-07-05, §2.3. Ship a collision-safe `Vec2`/`Position` value type in the game model template (or a documented naming lint), plus a minimal `Model.fs` showing the accumulator + `stepSim` pattern wired to the host `Tick`, so a new game model does not rediscover the `X`/`Y`-vs-`Scene.Rect` field collision only after a whole model is written.

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** product from the FS.GG.UI template gets a durable governance/layout spine
(`LayoutEvidence.fs`, `EvidenceCommands.fs`, `Program.fs`) that is deliberately kept passing across a starter
swap, plus a replaceable starter `Model.fs`/`View.fs` they are expected to edit into their own game. The durable
spine reuses the shared scene vocabulary from `FS.GG.UI.Scene` — in particular `Point` (`{ X; Y }`) and `Rect`
(`{ X; Y; Width; Height }`) — and constructs those records with **bare field labels** (`{ X = …; Y = …; Width = …; Height = … }`).

When an author swaps in their own game model, the natural first move is a position/velocity record with fields
named `X`/`Y` (and often `Width`/`Height`) on `Player`, `Enemy`, `Ball`, etc. Because `open FS.GG.UI.Scene` is in
scope, F#'s **record-field label inference** now has two record types offering `X`/`Y`/`Width`/`Height`, and it can
resolve the bare labels inside the **durable** `LayoutEvidence.fs` to the *game* record instead of `Rect`. Every
downstream `.Width`/`.Height`/`.X`/`.Y` access on what the author intended as a `Rect` then fails to compile with a
wall of `FS3566` (field not defined) / `FS0039` (undefined) errors — in a file the author was told not to touch,
far from the model record they actually wrote.

The trap is real, silent until the whole model is written, and has a known workaround: the *Hollow Depths* reporter
renamed their fields to `CenterX`/`CenterY` and the wall of errors disappeared. The shipped **Pong** starter already
side-steps the collision the same ad-hoc way — its `Ball` uses `CenterX`/`CenterY`/`VelocityX`/`VelocityY`, its
`Model` uses `PlayfieldWidth`/`PlayfieldHeight`, and a code comment ("Record-label note (fs-gg-scene pitfall)")
warns future editors. But that guidance is a **comment on one starter**, not a reusable, collision-safe *type* an
author can lean on. A developer who writes a fresh model from scratch — or who reads the comment, understands nothing
of *why* bare `X`/`Y` is dangerous, and uses them anyway — rediscovers the trap.

This feature makes the collision **impossible to fall into by default** at the point of authoring: the model template
ships a small, collision-safe position/velocity value type (a `Vec2`/`Position` whose field labels do **not** overlap
`Point`/`Rect`), the starter model is expressed in terms of it, and a minimal `Model.fs` demonstrates the
accumulator + `stepSim` (fixed-step simulation) pattern wired to the host `Tick` so the safe type is shown in the
exact place an author starts editing. The durable governance spine, the evidence tokens, and a starter swap all keep
passing. It is the sibling of the model/host ergonomics epic's second child (#139, surfacing the keyboard-only host
boundary) — both surface a template trap up front instead of after the author hits it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A fresh game model compiles without rediscovering the Rect collision (Priority: P1)

A developer scaffolds a game product and replaces the starter `Model.fs` with their own game: a `Player` and some
`Enemy` instances, each with a position and a velocity. They reach for the template's shipped position type instead
of hand-rolling bare `X`/`Y` floats. Because that type's field labels do not collide with `Scene.Point`/`Scene.Rect`,
the durable `LayoutEvidence.fs` continues to resolve its bare `{ X; Y; Width; Height }` record literals to `Rect`,
and the product still builds — no `FS3566`/`FS0039` wall, no editing of a file the author was told to leave alone.

**Why this priority**: This is the whole point of the feature and of issue #138 — remove the trap that only surfaces
after a whole model is written. It is independently valuable even if the demonstration `Model.fs` rewrite (US2) never
ships, because the safe type plus the durable spine is what prevents the collision.

**Independent Test**: Author a model record that uses the shipped position/velocity type for entity positions, place
it alongside the unmodified durable `LayoutEvidence.fs`, and confirm the product compiles clean. Separately, confirm
that a model record using **bare** `X`/`Y`/`Width`/`Height` fields (the trap) either fails to compile *or* is caught
by the shipped naming guard — i.e. the failure mode is demonstrably prevented or surfaced, not silent.

**Acceptance Scenarios**:

1. **Given** a scaffolded game product, **When** the author models entity positions with the shipped
   collision-safe position type and leaves `LayoutEvidence.fs` untouched, **Then** `dotnet build` succeeds with no
   `FS3566`/`FS0039` diagnostics originating from `LayoutEvidence.fs`.
2. **Given** the shipped collision-safe position type, **When** its field labels are compared against
   `FS.GG.UI.Scene.Point` and `FS.GG.UI.Scene.Rect`, **Then** they share **no** field-label name (so record-label
   inference can never confuse the two).
3. **Given** the durable governance/layout spine and evidence commands, **When** the starter is expressed in terms of
   the collision-safe type, **Then** the governance scans, evidence tokens (`hud-region` / `gameplay-region` /
   `measurement-mode` / `overlap`), and a starter swap all keep passing unchanged.

---

### User Story 2 - The starter shows the accumulator + stepSim pattern at the edit site (Priority: P2)

A developer opening the replaceable `Model.fs` to start their game sees a minimal, readable example that ties the
collision-safe position type to the game loop: positions/velocities expressed with the safe type, a pure fixed-step
`stepSim` (or equivalently named step) that advances the simulation, and the host `Tick` message wired to call it.
The author learns the intended pattern — safe position type + accumulator/step + `Tick` — from the code they are about
to edit, not from a comment or an external doc.

**Why this priority**: It converts the safe type from "available" into "demonstrated where you start editing," which
is what makes authors actually adopt it. It is valuable but secondary to US1: the collision prevention (US1) stands
on its own even if the demonstration is minimal.

**Independent Test**: Read the shipped `Model.fs`; confirm it expresses entity position/velocity with the
collision-safe type and contains a pure step function wired to the host `Tick`, and that the product builds and its
behavior tests pass.

**Acceptance Scenarios**:

1. **Given** the shipped game starter `Model.fs`, **When** an author reads it, **Then** entity positions and
   velocities are expressed with the collision-safe position type (not bare `X`/`Y` floats).
2. **Given** the host emitting `Tick`, **When** `update` handles `Tick`, **Then** it advances the simulation through a
   pure step function that operates on the collision-safe type, and the model stays inside its playfield/bounds after
   the step.

---

### User Story 3 - The pitfall is documented where an author first meets it (Priority: P3)

The product's authoring guidance (the game/model swap skill note and the template comment at the model-editing site)
explains, in one place an author actually reads, *why* bare `X`/`Y`/`Width`/`Height` on a game record is dangerous
next to `Scene`, and points at the shipped collision-safe type as the default. The existing "Record-label note"
comment is upgraded from "we renamed to avoid it" to "use this type; here is why."

**Why this priority**: Documentation reinforces the structural fix but does not, by itself, prevent the collision — so
it is the lowest priority of the three. It matters most for the author who chooses to hand-roll their own type anyway.

**Independent Test**: Confirm the swap/model authoring guidance and the `Model.fs` comment name the collision, name
the safe type, and state the rule (do not reuse `Scene` field labels on a game record) at the model-editing site.

**Acceptance Scenarios**:

1. **Given** the game/model authoring guidance, **When** an author reads it before writing their model, **Then** the
   `Scene`-field-label collision and the collision-safe type are both described at the model-editing site.

---

### Edge Cases

- **Author ignores the safe type and uses bare `X`/`Y` anyway.** The naming guard (if that route is chosen over/with
  the type) must surface the collision at build/authoring time with a message that names the offending field and the
  `Scene` collision — not leave the author to decode an `FS3566` wall in `LayoutEvidence.fs`.
- **Author needs `Width`/`Height` on an entity (size, hitbox).** The collision-safe vocabulary must cover the
  size-bearing case too (not just position), or the guidance must state how to name size fields safely — the collision
  is over `Rect`'s `Width`/`Height` as much as `Point`'s `X`/`Y`.
- **App / headless-scene / sample-pack profiles.** The change is scoped to where the collision actually bites (the
  game family and any profile whose durable spine constructs `Scene` records from bare labels); it must not regress the
  non-game profiles' byte-identical output or their governance posture.
- **Starter swap.** After an author swaps the starter for their own model expressed with the safe type, the durable
  spine, evidence tokens, and governance scans still pass (the swap contract is preserved).
- **Interop with the safe type and `Scene`.** Converting a collision-safe position into a `Scene.Point`/`Rect` for
  rendering/layout must be obvious and one small step, so the safe type does not become a wall between the model and
  the scene.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The model template MUST ship a collision-safe position/velocity value type (a `Vec2`/`Position`) whose
  field labels do **not** overlap `FS.GG.UI.Scene.Point` (`X`, `Y`) or `FS.GG.UI.Scene.Rect` (`X`, `Y`, `Width`,
  `Height`), so that record-label inference in a durable file with `open FS.GG.UI.Scene` in scope can never resolve a
  bare label to the game type.
- **FR-002**: The shipped collision-safe type MUST be usable for the size-bearing case as well as position (or the
  authoring guidance MUST state the safe naming for size/hitbox fields), because the collision spans `Rect`'s
  `Width`/`Height`, not only `Point`'s `X`/`Y`.
- **FR-003**: The game starter `Model.fs` MUST express entity positions/velocities in terms of the collision-safe
  type rather than bare `X`/`Y` floats.
- **FR-004**: The starter MUST demonstrate the accumulator + `stepSim` (fixed-step simulation) pattern wired to the
  host `Tick`: a pure step function over the collision-safe type, invoked from `update` on `Tick`.
- **FR-005**: The durable governance/layout spine (`LayoutEvidence.fs`, `EvidenceCommands.fs`, `Program.fs`,
  `WindowOptions.fs`) MUST remain untouched-and-passing when a model is expressed with the collision-safe type — the
  spine keeps resolving its bare `Scene` record literals to `Point`/`Rect`, and the evidence tokens are unchanged.
- **FR-006**: The scaffolded game product MUST build clean (`dotnet build`) and its behavior/governance tests MUST
  pass with the collision-safe type in place, on a fresh scaffold with no author edits.
- **FR-007**: The bare-`X`/`Y` trap MUST be **prevented or surfaced up front**: either the safe type is the obvious
  default an author reaches for, or a documented naming lint/guard flags a game record that reuses `Scene` field
  labels — the failure must not remain a silent post-hoc `FS3566`/`FS0039` wall in a durable file.
- **FR-008**: Authoring guidance — the game/model-swap skill note and the template comment at the model-editing site —
  MUST name the `Scene`-field-label collision, name the collision-safe type as the default, and state the rule, at the
  place an author first writes their model.
- **FR-009**: Converting a collision-safe position/size into `Scene.Point`/`Scene.Rect` for rendering and layout MUST
  be a single obvious step, so the safe type integrates with the existing scene/layout vocabulary rather than
  duplicating or obstructing it.
- **FR-010**: The change MUST NOT alter the byte-identical output or governance posture of the non-game profiles
  (`app` / `governed` / `headless-scene`) beyond what is required to host the shared safe type.

### Key Entities *(include if feature involves data)*

- **Collision-safe position/velocity type (`Vec2`/`Position`)**: A small value type carrying a 2D position (and/or
  velocity) whose field labels are chosen to *not* collide with `Scene.Point`/`Scene.Rect`. The unit the starter model
  and author models use for entity coordinates.
- **Scene `Point` / `Rect`**: The shared scene vocabulary (`{ X; Y }` and `{ X; Y; Width; Height }`) that the durable
  layout/evidence spine constructs with bare field labels — the *other* side of the collision, unchanged by this
  feature.
- **Durable layout/evidence spine**: `LayoutEvidence.fs` and the governance-scanned files that must keep compiling and
  passing across a model swap; the place where the mis-inference currently manifests.
- **Replaceable starter `Model.fs`**: The developer-owned game seam where the collision-safe type and the
  accumulator + `stepSim` + `Tick` pattern are demonstrated.
- **Naming guard / lint (if adopted)**: The build- or authoring-time check that flags a game record reusing `Scene`
  field labels, so the trap is surfaced instead of silent.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can write a fresh game model that stores entity positions and sizes and build the product
  clean on the first try, without editing any durable/governance-scanned file and without encountering an
  `FS3566`/`FS0039` field-resolution error originating from `LayoutEvidence.fs`.
- **SC-002**: The shipped collision-safe type shares **zero** field-label names with `Scene.Point` and `Scene.Rect`
  (verifiable by inspection/assertion), so the record-label mis-inference is structurally impossible for a model
  expressed with it.
- **SC-003**: A fresh game scaffold builds and passes its behavior + governance tests with no author edits, and a
  starter swap to an author model expressed with the collision-safe type keeps the durable spine, evidence tokens, and
  governance scans passing.
- **SC-004**: The shipped starter and every scaffolded model built on the collision-safe type declare **no**
  `Scene`-colliding labels, and the model-editing comment + swap/game-core guidance name the collision and the safe
  type **up front** — so an author meets the constraint before writing a colliding record, not after. (An author who
  bypasses the safe type and reuses `X`/`Y`/`Width`/`Height` anyway still receives the F# compiler's `FS3566`/`FS0039`
  as the ultimate signal; a friendlier build-time author-record naming lint is an accepted, tracked follow-up per
  FR-007's either/or, not part of this feature.)
- **SC-005**: An author opening `Model.fs` can identify, without external docs, both the collision-safe position type
  and the accumulator + `stepSim` + `Tick` pattern from the starter code itself.
- **SC-006**: Non-game profiles' output remains byte-identical and their governance posture unchanged.

## Assumptions

- The collision to prevent is `FS.GG.UI.Scene.Point`/`Rect` bare field-label inference leaking into the durable
  `LayoutEvidence.fs` (and any sibling durable file that constructs `Scene` records from bare labels) when a game model
  reuses `X`/`Y`/`Width`/`Height`; the *Hollow Depths* §2.3 report and the shipped Pong starter's `CenterX`/`CenterY`
  workaround are the ground truth for the failure and its fix.
- The primary blast radius is the **game** family (the governed default for the game profile). Non-game profiles are
  in scope only to the extent that they must not regress; a full audit of every profile's durable files for the same
  bare-label pattern is part of planning, not assumed away.
- "Collision-safe type" and "documented naming lint" from issue #138 are presented as *either/or* in the source; this
  spec treats the **type** as the primary mechanism (FR-001) and a lint/guard as the surfacing backstop for authors who
  hand-roll anyway (FR-007) — planning decides whether to ship one or both.
- The accumulator + `stepSim` pattern refers to the existing fixed-timestep simulation vocabulary already established
  for game/sim consumers (e.g. `FixedStep`); this feature demonstrates it in the starter, it does not introduce a new
  simulation engine.
- The durable-vs-replaceable file taxonomy and the starter-swap contract (durable spine never calls `update`/`view`)
  are the ones already documented in the scaffold map; this feature preserves that contract.
- Delivery follows the same template/skill/release path as sibling features (template fragment or base `Model.fs` +
  the `game`/`model-swap` authoring skill note + generated-product tests), coordinated through the standard release of
  the `FS.GG.UI.Template` package.
