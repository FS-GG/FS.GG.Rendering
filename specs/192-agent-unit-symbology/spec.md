# Feature Specification: Agent-Driven Unit-Symbology Design System

**Feature Branch**: `192-agent-unit-symbology`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan" — the build-out (milestones M1–M5) of `docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`: a reusable unit-symbology library plus an agent workflow that turns a game's unit roster + stats into a legible visual control set (abstract vector symbols, not depictions), refined through a render→eyeball→tweak loop until the user is satisfied. The proof-of-concept (P0) is already complete; this feature delivers objectives O1–O5 (the minimum viable agent loop with provenance).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encode a unit roster as legible vector symbols (Priority: P1)

A designer (or an agent acting for them) has a roster of game units with stats — `name, faction, role, hp, dps, speed, armor, heading, …`. They want each unit rendered as a compact **symbol** that encodes as much state as the eye can read — affiliation, class, threat, health, heading, speed, status — through a *fixed grammar* of vector channels (shape, colour, stroke, fill, motion). They author a small per-game mapping from their unit's stats onto the symbol's channels, and the library turns each mapped symbol into pure drawing content composable into a review board.

**Why this priority**: This is the foundational capability and the minimum viable product. Without a pure, deterministic symbol vocabulary that visibly encodes each channel, none of the higher stories (seeing it, looping on it) are reachable. A legible, goldenable symbol library is valuable on its own: it lets a game express its unit roster as an at-a-glance visual control set, pinnable like any other contracted surface.

**Independent Test**: Author a fixed set of symbol descriptions spanning all channels (two factions, three classes, varying threat/health/speed/heading, a status flag), build a review board from them, render the board headlessly, and assert that (a) every channel observably alters the output, (b) identical inputs produce an identical drawing and identical fingerprint, and (c) the symbols are distinguishable at the target on-board size.

**Acceptance Scenarios**:

1. **Given** a symbol description carrying the full channel set, **When** it is evaluated, **Then** it returns drawing content that is a pure function of its inputs (same inputs → identical drawing), composable into a larger board.
2. **Given** two symbol descriptions that differ in exactly one channel (e.g., faction hue, class silhouette, stroke width for threat, interior charge gradient, health-arc length, tail-bead count, heading rotation, inspection-state dash, or a corner mount), **When** both are rendered, **Then** the rendered outputs differ observably in that channel and only that channel.
3. **Given** a list of symbol descriptions, **When** a review board (gallery) is built from them, **Then** the symbols are laid out in a reproducible grid composable into a single drawing.
4. **Given** the same symbol description rendered twice, **When** both drawings are produced, **Then** the emitted drawing and its fingerprint are identical (deterministic output), and the drawing survives an export→import round-trip with all paint channels (geometry, gradients, dashes, arcs, stroke attributes) preserved.

---

### User Story 2 - Animate symbols and see them as an image headlessly (Priority: P2)

The designer wants to (a) attach a **motion rhythm** to a symbol to encode activity/alert state (e.g., pulse when firing, spin when channeling, blink on alert, throb on damage, translate when moving) sampled deterministically over a phase, and (b) **see** any board or motion sequence as a concrete image without writing rendering plumbing or reaching into internal painter machinery. They request a render of a board or a motion filmstrip and receive an image file plus evidence that the render succeeded.

**Why this priority**: A static symbol library (US1) covers the vocabulary, but the design loop is driven by *seeing* the result and by the motion channel that encodes activity. This story adds the deterministic motion overlays and the public, scriptable image-render path that the feedback loop depends on. It turns the library from data into something a designer or agent can eyeball.

**Independent Test**: Build a motion filmstrip (a motion type applied to a symbol, sampled across N phase steps) and a multi-symbol board; render both to image files through the public render path; assert (a) each filmstrip frame is byte-reproducible from its phase schedule with no wall-clock read, (b) the produced images are non-blank and pass the render verdict, and (c) the render path raises a diagnostic-bearing failure rather than emitting a blank image, and never requires access to any internal-only rendering entry point.

**Acceptance Scenarios**:

1. **Given** a motion rhythm applied to a symbol at a given phase, **When** it is evaluated, **Then** it returns drawing content that is a pure function of (symbol, phase) — identical (symbol, phase) → identical drawing — with the motion overlaid on the base symbol.
2. **Given** a motion rhythm and a fixed number of phase samples, **When** a filmstrip is built, **Then** the sequence of frames is reproducible from the phase schedule alone (no wall-clock time is read), mirroring the project's existing fixed-timestep determinism.
3. **Given** a board or filmstrip drawing and an output directory, **When** the public render path is invoked, **Then** it produces a non-blank image file at a content-addressable path and reports a passing render verdict together with evidence.
4. **Given** a drawing that fails to render (e.g., empty or a verdict other than "passed"), **When** the public render path is invoked, **Then** it raises a failure carrying the underlying render diagnostics rather than silently producing a blank image.
5. **Given** a request to render from a script, **When** the render path is used, **Then** the whole path is reachable through the project's public package surface with no change to the existing core control / scene / viewer surfaces.

---

### User Story 3 - Run the agent design loop end-to-end with provenance (Priority: P3)

A designer wants an agent to take a real roster and, applying a fixed grammar and legibility rules, draft a stat→channel mapping, render a review board, critique it against legibility guardrails, present it for feedback, and **loop** — tweaking only the mapping (never the grammar) — until the design is approved. Each iteration leaves an audit trail (a timestamped board plus a snapshot of the mapping used), and on approval the agent emits a final symbol-set module plus a design rationale and pins a golden board.

**Why this priority**: This is the workflow-and-proof layer. US1+US2 make the symbols usable and visible; this story makes the *design process* repeatable and auditable, and proves the whole stack end-to-end on a real roster. It depends on the prior two and is therefore lowest priority, but it is what turns "a symbol library exists" into "a game's unit control set was designed, reviewed, and approved with a record of why."

**Independent Test**: Drive the loop on a sample roster (6–10 units) through at least two feedback rounds where only the stat→channel mapping changes between rounds; assert that (a) the loop follows the fixed intake→map→render→critique→review→tweak→approve protocol consistently, (b) every iteration writes a timestamped board image and a mapping snapshot under a working directory, and (c) on approval a final symbol-set module + rationale is emitted and a golden board is pinned. Confirm the orchestrating guidance is present and consistent across the project's skill trees.

**Acceptance Scenarios**:

1. **Given** a roster with per-unit stats, **When** the loop's mapping step runs, **Then** it produces a stat→channel mapping that assigns urgent state redundantly across multiple pre-attentive channels and keeps faction and state on different channels (no hue collision), following the documented legibility rules.
2. **Given** a drafted mapping, **When** an iteration runs, **Then** it renders a review board, self-critiques it against the legibility guardrails (faction separable? class distinct? health readable at target size? no channel overloaded?), and presents the board for human feedback.
3. **Given** human feedback on a board, **When** the loop tweaks the design, **Then** the unit of change is the per-game mapping (and occasionally a theme palette) — never the fixed grammar/library internals — keeping iterations small and reversible.
4. **Given** any iteration of the loop, **When** it completes, **Then** a timestamped board image and a snapshot of the mapping that produced it are written under a working directory, forming an auditable design history.
5. **Given** the user approves a board, **When** the loop finishes, **Then** it emits a final symbol-set module (pure drawing-producing functions) plus a design rationale (channel assignments + rejected alternatives + legibility notes) and pins a golden board with a stable identity.

---

### Edge Cases

- **Empty / zero-area symbol**: a symbol description with zero radius (or otherwise no drawable area) degrades to a visible placeholder rather than a crash or silent blank (inherits the canvas empty-state rule).
- **Channel overload**: a roster with many independent stats may exceed the legibility budget (the eye cannot track all channels at once). The grammar caps which channels carry critical state (one *active* motion at a time; never critical state on dash alone); a roster that demands more is surfaced to the designer rather than silently overloaded. (A scoring linter is a later, out-of-scope enhancement.)
- **Hue collision**: a mapping that would put both faction and state on the colour/hue channel is disallowed by the legibility rules — state semantics use the project's status palette, faction uses a separate saturated palette.
- **Render failure**: when the underlying render reports any non-passing verdict (or produces no image), the render path fails loudly with the diagnostics, never returning a blank image as if it had succeeded.
- **Rotating vs screen-aligned gauges**: when the body rotates by heading, gauges meant to stay readable (e.g., the health arc) remain screen-aligned rather than rotating with the body.
- **Non-reproducible inputs**: any attempt to drive a symbol or motion from wall-clock time breaks determinism and is excluded — symbols and motion frames depend only on their explicit inputs (description + phase).
- **Text/glyph legibility**: identity is carried by vector sigils only for this iteration; pure-CPU label text (which can render as tofu without a real text measurer) is out of scope.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a dependency-light first-party symbol library that depends only on the project's existing immutable scene representation (no IO, no GPU, no codec) and emits symbols as pure drawing content.
- **FR-002**: The library MUST define a symbol description (the props record) exposing the full fixed channel set as typed fields: centre/size, heading, faction, class, identity sigil, inspection state (confirmed/suspected), threat, charge/energy, speed, health, and a boolean mount (one corner slot in v1; additional mount slots deferred).
- **FR-003**: The library MUST provide a pure symbol function from a symbol description to drawing content such that identical descriptions yield an identical drawing (and identical content fingerprint), with no wall-clock or IO dependency.
- **FR-004**: The symbol function MUST render the fixed encoding grammar so that each channel observably alters the output: stroke hue → faction; body silhouette + centre sigil → class + identity; whole-body rotation → heading; stroke width → threat; interior gradient → charge/energy; belly arc length + colour → health; tail bead count → speed; stroke dash → confirmed vs suspected (inspection state); corner mount → boolean mount flag.
- **FR-005**: The library MUST provide a fixed, enumerable set of channel constructors/enums (faction, class→silhouette, sigil, status) so that a per-game theme picks from a known vocabulary rather than inventing geometry.
- **FR-006**: The library MUST keep the **fixed grammar** (the symbol vocabulary) and the **per-game mapping** (a `stats → symbol description` data function) as separate layers; per-game encoding choices MUST live in the editable mapping, not baked into the library element.
- **FR-007**: The library MUST provide deterministic motion overlays as pure `(symbol, phase) → drawing` functions for a fixed set of rhythms (at minimum: idle, pulse, spin, blink, damage, moving), overlaid on the base symbol, with phase owned by the caller (no wall-clock read).
- **FR-008**: The library MUST provide review-board layout helpers — a gallery (`symbols → drawing`, reproducible grid) and a filmstrip (`(motion, symbol) samples → drawing`, deterministic phase schedule) — for visual review.
- **FR-009**: The motion and layout helpers MUST be deterministic: a filmstrip's frames MUST be byte-reproducible from a phase schedule with no wall-clock read, mirroring the project's existing fixed-timestep loop determinism.
- **FR-010**: The system MUST provide a public, scriptable headless render path that turns a drawing into an image file without exposing or requiring any internal-only rendering entry point, returning the image path on success.
- **FR-011**: The render path MUST be a thin helper kept **out** of the pure library (in a separate component that may reference the viewer/raster machinery), so the symbol library stays drawing-only with no IO or raster dependency.
- **FR-012**: The render path MUST fail loudly: on any non-passing render verdict or missing image it MUST raise an error carrying the underlying render diagnostics, never returning a blank image as success.
- **FR-013**: The render path MUST emit a content-addressable image plus render evidence per invocation, giving each iteration a reusable regression identity.
- **FR-014**: The system MUST provide an orchestrating skill that teaches any agent to run the design loop identically: the fixed grammar + legibility rules (assign-by-urgency, encode urgent state redundantly across pre-attentive channels, one active motion at a time, never critical state on dash alone, avoid faction/state hue collision), the library API and the grammar-vs-mapping pattern, the scripting recipe (reference the library + render path → define roster → define mapping → render a board → read the image back → iterate), and the feedback protocol.
- **FR-015**: The skill MUST be present and consistent across the project's skill trees that mirror the existing first-party skills, and MUST pass the project's skill-parity check.
- **FR-016**: The agent loop MUST follow a fixed protocol — intake roster → draft mapping → render board → self-critique against legibility rules → present for review → tweak mapping on feedback → approve — where the unit of change each iteration is the per-game mapping (and occasionally a theme palette), never the fixed grammar.
- **FR-017**: Each loop iteration MUST write provenance under a working directory: a timestamped board image plus a snapshot of the mapping that produced it, forming an auditable design history.
- **FR-018**: On approval, the loop MUST emit a final symbol-set module (pure drawing-producing functions) plus a design rationale (channel assignments, rejected alternatives, legibility notes) and pin a golden board with a stable identity.
- **FR-019**: State-semantics colour MUST reuse the project's existing status tokens while faction hue uses a separate saturated palette, so *state* and *team* never share the hue channel.
- **FR-020**: A symbol with zero/empty drawable area MUST degrade to a visible placeholder rather than crashing or emitting a silent blank.
- **FR-021**: All new application/agent-facing capability MUST be reachable through the project's existing public package surface; the new symbol library and render-path components MUST land specification-first (interface before implementation) with new public-surface baselines, and MUST NOT change the existing core control / scene / viewer public surfaces.
- **FR-022**: For this iteration, identity MUST be carried by vector sigils only (no label text), honouring "symbol, not depiction" and avoiding CPU-raster text legibility risk; label text is deferred.

### Key Entities *(include if feature involves data)*

- **Symbol description (Token)**: the props record carrying the full fixed channel set (centre, size, heading, faction, class, sigil, inspection state, threat, charge, speed, health, boolean mount); the unit over which the symbol function is pure and the fingerprint/determinism are asserted.
- **Encoding channel grammar**: the fixed set of visual channels (stroke hue, motion rhythm, size, silhouette+sigil, rotation, stroke width, interior gradient, belly arc, tail beads, dash, corner mounts) and their reliable level counts and salience — the stable part of the system.
- **Channel map (per-game mapping)**: the editable `stats → symbol description` data function that assigns a specific game's unit stats onto channels; the artifact the agent tweaks each loop iteration.
- **Motion rhythm**: a named pure overlay `(symbol, phase) → drawing` (idle/pulse/spin/blink/damage/moving) encoding the activity/alert channel; phase is supplied by the caller.
- **Review board**: a composed drawing — gallery (grid of symbols) or filmstrip (motion sampled over phases) — used for headless eyeballing and as a goldenable identity.
- **Render evidence**: the image path + verdict + diagnostics produced per render-path invocation, giving each iteration a regression identity and a fail-loud signal.
- **Design provenance**: the per-iteration record (timestamped board image + mapping snapshot) plus the final emitted symbol-set module, rationale, and pinned golden board.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Rendering the same symbol description any number of times produces byte-identical drawings and identical content fingerprints, and a review board's package identity is stable across runs — 100% reproducibility across repeated and headless runs.
- **SC-002**: Every encoding channel observably changes the rendered image: for each of the channels in FR-004, two descriptions differing only in that channel produce visibly different output, verified by readback evidence at the target board size.
- **SC-003**: An export→import→raster round-trip of any symbol drawing preserves all paint channels (path geometry, radial/linear/sweep gradients, dash effects, arcs, stroke width/cap/join) with no loss.
- **SC-004**: Delivering this feature changes only the new symbol library and render-path public-surface baselines; the existing core control / scene / viewer public surfaces show zero drift, verified by the surface gate.
- **SC-005**: The orchestrating skill is present and consistent across all mirrored skill trees and passes the skill-parity check.
- **SC-006**: Every filmstrip frame is byte-reproducible from a phase schedule with no wall-clock read — repeating a filmstrip render yields identical frames every time.
- **SC-007**: Symbols are legible at the target on-board size: each rendered symbol is non-blank and faction and class are separable at that size, verified by readback evidence.
- **SC-008**: The public render path returns a non-blank, passing-verdict image from a script without reaching any internal-only entry point, and raises a diagnostic-bearing failure (never a blank success) when the render does not pass.
- **SC-009**: A dry-run of the loop on a real roster (6–10 units) across at least two feedback rounds produces, for every iteration, a timestamped board image and a mapping snapshot, and on approval a final symbol-set module + rationale + a pinned golden board — a complete render→tweak→approve audit trail.

## Assumptions

- **Scope = M1–M5 (the minimum viable agent loop, O1–O5)**: this feature delivers the core symbol library, motion/layout boards, the headless render bridge, the orchestrating skill, and an end-to-end loop dry-run with provenance. The **live runnable board sample** (M6) and the **governance/breadth backlog** — a legibility-scoring linter, the Badge/Ring alternative grammars, and label text — are explicitly out of scope (deferred to M6/M7 in the source plan).
- **Token grammar only (G3)**: only the Directional-Token grammar is built; the Badge and Ring alternative grammars named in the plan are deferred.
- **Render bridge via the existing public headless path (G2/D2)**: because the low-level scene renderer is internal, the render path backs onto the project's existing public headless Scene→image entry (a codec round-trip), which the PoC verified preserves all paint channels. A dedicated direct-raster public entry is deferred and promoted only if loop latency demands it.
- **Dedicated libraries, not folded into the canvas/controls surface (G1/D1)**: the pure symbol vocabulary and the render-path helper live in their own first-party components (mirroring the embedded-canvas library's rationale), keeping game-symbol types off the core control surface and independently testable/packable.
- **Skill mirrored across all skill trees (G4)**: the orchestrating skill is authored once and mirrored to every skill tree that carries the existing first-party skills, gated by the skill-parity check.
- **Human-in-the-loop**: the design loop is deliberately human-approved; auto-generating the mapping from stats without human review is out of scope.
- **Determinism via injected phase**: motion consumes an explicit phase parameter; no component reads wall-clock time, preserving seeded reproducibility and golden-board tests (mirrors the existing fixed-timestep loop).
- **Validation environment**: pure logic (symbol/motion functions, fingerprints, layout) is testable headlessly; the render bridge uses the existing CPU raster path (no GL required).
- **Project/module layout** (exact project names, file split, and how the symbol description is carried) is an implementation decision deferred to `/speckit-plan`; the source plan's sketch is a starting point, not a contract.
