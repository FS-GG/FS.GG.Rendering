# Feature Specification: Surface the Keyboard-Only Host Input Boundary

**Feature Branch**: `251-keyboard-host-boundary`

**Created**: 2026-07-05

**Status**: Draft

**Input**: FS-GG/FS.GG.Rendering#139 (child of epic FS-GG/FS.GG.Rendering#137). Source: FS.GG framework
development-feedback report — *Hollow Depths* build (`001-hollow-depths`), 2026-07-05, §2.5. The persistent host that
is the governed default for the `game` family (`Viewer.runApp` over `GeneratedAppHost`) is **keyboard-only** — its
input seam is `MapKey: ViewerKey -> bool -> 'msg option`, `ViewerKey` has no mouse/pointer case, and input arrives as
`DispatchInput of ViewerKey * isDown`. A mouse-aimed control scheme (e.g. twin-stick WASD + mouse) cannot be
implemented on the default host without switching to a different, non-default host wiring. Surface this capability
boundary where a game author first meets input (a template comment at the input-wiring site in the starter `Model.fs`
plus a note in the keyboard-input product skill), so the constraint is known before a mouse-aimed scheme is designed.

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** product from the FS.GG.UI template gets a durable, governance-scanned host spine
that launches the persistent desktop window through `Viewer.runApp`. That path is backed by `GeneratedAppHost`, whose
only input seam is `MapKey: ViewerKey -> bool -> 'msg option` — a normalized **keyboard** key plus a down/up flag in,
an optional product `Msg` out. `ViewerKey` enumerates keyboard keys only (`ArrowLeft`/…/`Letter of char`/`Digit`/…) —
there is **no** mouse-button or pointer case — and at the host boundary input is delivered as
`DispatchInput of ViewerKey * isDown`. The starter `Model.fs` shows exactly this shape: a `paddleForKey` mapping from
`ViewerKey` to paddle moves, dispatched through a `ViewerInput of ViewerKey * isDown` message.

The framework **does** have a pointer-aware host — `InteractiveAppHost` (features 085/092), driven by
`Controls.Elmish.runInteractiveApp`, carries a `MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list` seam
alongside `MapKey`. But that is a **different host entry point** from the game family's governed default: the game
starter is wired to the keyboard-only `runApp`/`GeneratedAppHost` path, and its host wiring (`Program.fs` and the
governance-scanned spine) is durable — deliberately kept passing across a starter swap and therefore not something an
author is meant to freely rewrite. So a mouse-aimed control scheme is not available "for free" at the input-wiring
site an author actually edits; adopting one means moving off the default host onto the interactive/pointer-aware path,
a durable-wiring decision.

The *Hollow Depths* reporter (§2.5) hit this only **while wiring aim** — after choosing a mouse-aimed scheme and
reaching for a mouse case that does not exist on the default host. The boundary is real, and today it is discovered
late (at the moment you try to read the mouse) rather than surfaced up front where control schemes are chosen. Like
its sibling in this epic (#138, the `Scene`-field-label collision surfaced up front by a collision-safe `Vec2`), this
feature turns a trap discovered after work is done into a constraint stated at the point of authoring.

This feature makes the keyboard-only boundary **known before a mouse-aimed scheme is designed**, purely by surfacing
it where an author first meets input: a template comment at the input-wiring site in the replaceable starter
`Model.fs`, and a note in the keyboard-input product skill (and its fragment mirror). It ships **no** change to the
durable, governance-scanned host wiring and **no** new input capability — it documents the existing boundary and
points at the pointer-aware host path as the (non-default) way past it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - An author learns the keyboard-only boundary before choosing a control scheme (Priority: P1)

A developer scaffolds a game product and opens the replaceable `Model.fs` to wire input for their game. At the
input-wiring site — the key-to-command mapping and the `ViewerInput` handler they are about to edit — they read a
comment that states plainly: the default game host (`Viewer.runApp`) delivers **keyboard** input only, `ViewerKey`
has no mouse/pointer case, and a mouse-aimed scheme requires switching to the pointer-aware interactive host path
(which is a durable-wiring change, not an edit at this site). The author now chooses their control scheme knowing the
constraint, instead of discovering it after committing to a mouse-aimed design and reaching for a mouse case that
isn't there.

**Why this priority**: This is the whole point of the feature and of issue #139 — surface the hard host capability
boundary at the place an author first meets input, before a mouse-aimed scheme is designed. It is independently
valuable even if the skill note (US2) never ships, because the comment sits in the exact file the author edits.

**Independent Test**: Read the shipped starter `Model.fs`; confirm that at the input-wiring site it states the
default host is keyboard-only, that `ViewerKey` has no mouse/pointer case, and that mouse-aimed input requires the
pointer-aware interactive host path. Confirm the product still builds and its behavior tests pass unchanged.

**Acceptance Scenarios**:

1. **Given** a scaffolded game product, **When** an author opens `Model.fs` at the input-wiring site (the
   key-to-command mapping / `ViewerInput` handler), **Then** a comment states the default game host is keyboard-only,
   that `ViewerKey` carries no mouse/pointer case, and names the pointer-aware interactive host path as the way to a
   mouse-aimed scheme.
2. **Given** the surfaced comment, **When** the game product is scaffolded and built with no author edits, **Then**
   `dotnet build` succeeds and the behavior/governance tests pass — the comment is documentation only and changes no
   behavior.

---

### User Story 2 - The boundary is documented in the keyboard-input product skill (Priority: P2)

A developer consulting the keyboard-input product skill (the guidance they read when mapping input to product
commands) finds a "capability boundary" note that says: the game family's default persistent host is keyboard-only
(`MapKey` / `ViewerKey`, no `MapPointer`), and a mouse-aimed control scheme requires the pointer-aware interactive
host (`InteractiveAppHost` / `runInteractiveApp` with its `MapPointer` seam) rather than the default `runApp` path.
The author reading the skill before wiring input meets the boundary in the guidance, not only in the starter comment.

**Why this priority**: It reinforces the structural surfacing in the skill an author reads when they think about
input, but the in-file comment (US1) is what an author cannot miss while editing, so the skill note is secondary.

**Independent Test**: Read the keyboard-input product skill (and its fragment mirror); confirm both name the
keyboard-only default host, the absent mouse/pointer case, and the pointer-aware interactive host as the path to
mouse-aimed input.

**Acceptance Scenarios**:

1. **Given** the keyboard-input product skill, **When** an author reads it before wiring input, **Then** it states
   the default game host is keyboard-only and names the pointer-aware interactive host path as the way to mouse-aimed
   input.
2. **Given** the keyboard-input skill and its fragment mirror, **When** they are compared, **Then** both carry the
   same capability-boundary note (the surfaced constraint is consistent across the materialized skill and its
   fragment source).

---

### Edge Cases

- **Author still wants a mouse-aimed scheme.** The surfaced note must not merely say "not supported"; it must name the
  pointer-aware interactive host path (`InteractiveAppHost` / `runInteractiveApp`, `MapPointer`) as the actual way to
  read mouse input, so the boundary is a signpost, not a dead end.
- **Author edits/swaps the starter `Model.fs`.** The comment lives in the replaceable starter; a starter swap that
  removes it is the author's choice and must not break the durable spine, governance scans, or evidence tokens. The
  surfacing does not depend on the comment surviving a swap.
- **The claim must stay true to the shipped host contract.** The note must describe the *actual* seams
  (`GeneratedAppHost.MapKey` keyboard-only vs `InteractiveAppHost.MapPointer`), so it does not drift from the real
  `SkiaViewer`/`KeyboardInput` public surface as those evolve.
- **Non-game / `app` profiles.** The surfacing is scoped to where the game author meets input; it must not regress the
  byte-identical output or governance posture of the non-game profiles, nor imply the keyboard-only constraint applies
  to hosts that already expose a pointer seam.
- **No durable-wiring change.** Surfacing the boundary must not alter `Program.fs` or any governance-scanned host file,
  add an input capability, or change the emitted host — it is documentation at the authoring site only.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The replaceable starter `Model.fs` MUST carry a comment **at the input-wiring site** (the
  key-to-command mapping / `ViewerInput` handler an author edits) stating that the game family's default persistent
  host (`Viewer.runApp` over `GeneratedAppHost`) delivers **keyboard input only** and that `ViewerKey` has no
  mouse/pointer case.
- **FR-002**: That same comment MUST name the pointer-aware interactive host path (`InteractiveAppHost` /
  `Controls.Elmish.runInteractiveApp` with its `MapPointer` seam) as the way to a mouse-aimed control scheme, so the
  boundary is a signpost to the real alternative rather than a bare "unsupported".
- **FR-003**: The keyboard-input product skill MUST document the same capability boundary — default game host is
  keyboard-only (`MapKey` / `ViewerKey`, no `MapPointer`); mouse-aimed input requires the pointer-aware interactive
  host — where an author reads guidance before wiring input.
- **FR-004**: The keyboard-input skill's **fragment mirror** MUST carry the same note, so the surfaced boundary is
  consistent between the materialized product skill and its fragment source.
- **FR-005**: The surfacing MUST be **documentation only**: no change to the durable, governance-scanned host wiring
  (`Program.fs` and the durable spine), no new input capability, no change to the emitted host or its seams. The
  scaffolded game product MUST build clean and its behavior/governance tests MUST pass with no author edits.
- **FR-006**: The surfaced claims MUST be **accurate to the shipped host contract**: the keyboard-only seam is
  `GeneratedAppHost.MapKey: ViewerKey -> bool -> 'msg option`, `ViewerKey` has no mouse/pointer case, and the
  pointer-aware seam is `InteractiveAppHost.MapPointer` reached via `runInteractiveApp` — the note must not assert a
  capability the default host does not have or misname the alternative path.
- **FR-007**: The change MUST NOT alter the byte-identical output or governance posture of the non-game profiles
  (`app` / `governed` / `headless-scene`), and MUST NOT imply the keyboard-only constraint applies to host paths that
  already expose a pointer seam.

### Key Entities *(include if feature involves data)*

- **Keyboard-only default game host (`Viewer.runApp` / `GeneratedAppHost`)**: The governed default persistent host for
  the game family; its sole input seam is `MapKey: ViewerKey -> bool -> 'msg option`. The keyboard side of the
  boundary — unchanged by this feature, only described.
- **`ViewerKey`**: The normalized keyboard-key type the host delivers (`ArrowLeft`/…/`Letter`/`Digit`/…); it has **no**
  mouse-button or pointer case — the fact the surfacing states.
- **`DispatchInput of ViewerKey * isDown`**: The host-boundary shape by which keyboard input reaches the product — the
  concrete evidence that the default host input path is keyboard-shaped.
- **Pointer-aware interactive host (`InteractiveAppHost` / `runInteractiveApp`, `MapPointer`)**: The non-default host
  path that carries a mouse/pointer seam — the signpost the surfacing points at for mouse-aimed schemes.
- **Input-wiring site in the replaceable starter `Model.fs`**: The key-to-command mapping and `ViewerInput` handler an
  author first edits to wire input — the place the boundary comment lives.
- **Keyboard-input product skill (and fragment mirror)**: The guidance an author reads when mapping input to product
  commands — the second place the boundary is surfaced.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An author opening the starter `Model.fs` at the input-wiring site can determine, **without external
  docs**, that the default game host is keyboard-only, that `ViewerKey` has no mouse/pointer case, and that a
  mouse-aimed scheme requires the pointer-aware interactive host path.
- **SC-002**: An author reading the keyboard-input product skill before wiring input meets the same capability
  boundary and the same signpost to the pointer-aware host; the skill and its fragment mirror state it identically.
- **SC-003**: A fresh game scaffold builds clean and passes its behavior + governance tests with no author edits — the
  feature adds documentation only and changes no runtime behavior.
- **SC-004**: The surfaced note matches the shipped host contract: it names the actual keyboard-only seam
  (`GeneratedAppHost.MapKey` / `ViewerKey` with no mouse case) and the actual pointer-aware seam
  (`InteractiveAppHost.MapPointer` via `runInteractiveApp`), with no claim the default host cannot back.
- **SC-005**: Non-game profiles' output remains byte-identical and their governance posture unchanged; no durable,
  governance-scanned host file is modified.

## Assumptions

- The boundary to surface is that the game family's governed default host (`Viewer.runApp` / `GeneratedAppHost`) is
  keyboard-only (`MapKey` / `ViewerKey`, no `MapPointer`), and that mouse-aimed input requires the pointer-aware
  interactive host (`InteractiveAppHost` / `runInteractiveApp`) — as evidenced by the shipped `SkiaViewer` and
  `KeyboardInput` public surface and corroborated by the *Hollow Depths* §2.5 report; it remains provisional until
  confirmed against the current template during planning.
- This feature is **surfacing/documentation only** (issue #139: "documented/surfaced where a game author first wires
  input"). It does **not** add a mouse/pointer input capability to the default host, and it does not rewire the game
  starter onto the interactive host — either of those would be a separate, larger feature.
- The primary place an author "first meets input" is the replaceable starter `Model.fs` input-wiring site plus the
  keyboard-input product skill; planning confirms these are the right two surfaces and whether any additional
  authoring surface (e.g. a scaffold-map note) should also carry the boundary.
- The durable-vs-replaceable file taxonomy and the starter-swap contract (durable spine never modified by the author,
  kept passing across a swap) are the ones already documented in the scaffold map; this feature preserves that
  contract and touches only replaceable/authoring surfaces.
- Delivery follows the same template/skill/release path as sibling feature #138 (a `FS.GG.UI.Template` republish
  carrying the starter comment + the keyboard-input skill note, coordinated through the org Coordination board:
  #139 → epic #137). It is a Tier-2 template-content change — no `FS.GG.UI.*` library public surface changes.
