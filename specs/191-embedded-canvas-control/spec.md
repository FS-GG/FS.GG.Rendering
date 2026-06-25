# Feature Specification: Embedded Canvas Control

**Feature Branch**: `191-embedded-canvas-control`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "Embedded Canvas Control — a first-class, model-driven `canvas` control for games and arbitrary rendering, embedded inside the existing Ant-themed UI (a viewport in the control tree alongside themed chrome, not a full-window takeover), plus a reusable element library authored as pure `'props -> Scene` functions and a fixed-timestep game-loop helper. Base the spec on docs/reports/2026-06-17-13-42-embedded-canvas-control-analysis-and-plan.md."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Embed an application-drawn canvas in the UI (Priority: P1)

A developer building an FS.GG application wants to display their own arbitrary 2D drawing — a chart, a diagram, a game board, a custom visualization — inside a region of the otherwise themed UI. They author the drawing as an application-produced scene derived from their model, place a `canvas` control in the control tree (inside a stack/panel alongside buttons, labels, and other themed chrome), and the drawing paints into the control's laid-out box, clipped to that box.

**Why this priority**: This is the foundational capability and the minimum viable product. Today no control lets an application's own drawing reach the painter (the existing `CustomControl` declares the right shape but its drawing is silently discarded). Without this, none of the higher stories are reachable. A static, embedded, paintable canvas already unblocks charts, diagrams, and custom visualizations on its own.

**Independent Test**: Author a fixed scene (e.g., a red rectangle and a circle), place a `canvas` carrying it inside a stack with sibling themed controls, render headlessly, and assert the drawn content appears, is translated to the control's box origin, and is clipped to the box — while the surrounding chrome lays out and paints normally.

**Acceptance Scenarios**:

1. **Given** a `canvas` control carrying an application-supplied scene placed inside a stack, **When** the frame is rendered, **Then** the supplied drawing appears inside the control's laid-out box, positioned relative to the box origin and clipped to the box bounds.
2. **Given** a `canvas` with an explicit width and height, **When** layout runs, **Then** the control occupies exactly that size and its siblings lay out around it as for any other leaf control.
3. **Given** a `canvas` with no scene supplied (design-time/empty state), **When** rendered, **Then** a clear placeholder is shown rather than a crash or blank gap.
4. **Given** the same model state rendered twice, **When** both frames are produced, **Then** the emitted scene and its fingerprint are identical (deterministic output).

---

### User Story 2 - Animate and interact with the canvas without disturbing the surrounding UI (Priority: P2)

A developer wants the canvas to update continuously (e.g., ~60 fps animation) and respond to raw pointer and keyboard input, while the static themed chrome around it stays cheap to render. They feed input edge-events into their model, redraw the canvas scene each frame, and the surrounding UI is not forced to repaint just because the canvas changed.

**Why this priority**: A static canvas (US1) covers visualization, but games and live simulations need per-frame redraw plus input. The key risk this story addresses is isolation: a canvas that changes every frame must not invalidate the cached, unchanging chrome around it. Delivering this turns the canvas from a picture into an interactive surface.

**Independent Test**: Run a canvas whose scene changes every frame next to static chrome; assert (a) raw pointer move/press/release/wheel samples and keyboard events reach the bound model when the canvas is the target/focus, and (b) the surrounding chrome registers as render-work-skipped (cache-stable) across frames while the canvas repaints.

**Acceptance Scenarios**:

1. **Given** a focusable `canvas` and a pointer event inside its box, **When** the event is routed, **Then** the raw pointer sample (position, button, wheel) is dispatched to the application's bound handler with coordinates resolvable to the canvas's local space.
2. **Given** a focused `canvas`, **When** a key is pressed or released, **Then** the raw key event is delivered to the application's bound handler.
3. **Given** a `canvas` marked as continuously-changing surrounded by unchanged themed chrome, **When** the canvas redraws every frame, **Then** the surrounding chrome is not repainted (its render work is reported as reused/skipped) while the canvas content updates.
4. **Given** a canvas whose scene is unchanged between two frames, **When** rendered, **Then** the system recognizes the unchanged content and avoids redundant repaint of that canvas.

---

### User Story 3 - Build games with a reusable element kit and a deterministic game loop (Priority: P3)

A developer wants to build a small game or simulation embedded in the UI without re-deriving the plumbing every time. They compose the canvas scene from a library of reusable drawing elements (sprites, shapes, primitives, helpers) authored as pure functions of their inputs, and advance their simulation on a fixed-timestep loop fed by the host's animation tick, with held-key and pointer state reconstructed from input edge-events. A seeded run reproduces byte-for-byte.

**Why this priority**: This is the ergonomics-and-proof layer. US1+US2 make the canvas usable; this story makes it pleasant for game/simulation authors and proves the whole stack end-to-end with a runnable sample. It depends on the prior two and is therefore lowest priority, but it is what turns "possible" into "productive."

**Independent Test**: Use the element library and the fixed-timestep loop helper to drive a small sample (e.g., bouncing sprites or Pong) from a seed and a scripted input sequence; assert that the same seed + inputs produce an identical world state, identical scene, and identical fingerprint each run, and that the loop clamps runaway frame times (no spiral of death).

**Acceptance Scenarios**:

1. **Given** a reusable element function and a set of inputs, **When** it is evaluated, **Then** it returns a drawing that is a pure function of those inputs (same inputs → identical drawing), composable into a larger canvas scene.
2. **Given** the fixed-timestep loop helper fed a sequence of tick durations, **When** simulation advances, **Then** the number of fixed update steps is a deterministic function of accumulated time, and an abnormally large frame time is clamped so the simulation cannot enter an unbounded catch-up loop.
3. **Given** a sample game run with a fixed seed and a scripted input sequence, **When** it is run twice headlessly, **Then** both runs yield identical world state, identical emitted scenes, and identical fingerprints (repeatable seeded evidence).

---

### Edge Cases

- **Empty / no scene**: a `canvas` with no supplied drawing shows a clear design-time placeholder, never a crash or silent blank.
- **Oversized drawing**: drawn content larger than the control's box is clipped to the box; it must not bleed over sibling controls or chrome.
- **Zero-size or unmeasured box**: a canvas laid out to zero area paints nothing and does not error.
- **Input outside the box**: pointer events outside the canvas box are not delivered to it; hit-testing honors the laid-out box.
- **Unfocused keyboard**: key events are only delivered to a canvas that holds focus.
- **Runaway frame time** (e.g., debugger pause, GC stall): the loop helper clamps the elapsed time so the simulation does not attempt an unbounded number of catch-up steps.
- **Rapid scene churn**: a canvas redrawing every frame must not exhaust or thrash the shared render cache, nor leak its per-frame invalidation into neighboring cached content.
- **Coordinate convention**: authored content uses a single documented local origin/orientation; the control is responsible for placing it at the box.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a `canvas` control kind that paints an application-supplied drawing into its laid-out box, clipped to that box, and integrated with the existing layout, hit-testing, and focus model. This is a new, separate control kind; the existing `CustomControl` placeholder behavior MUST NOT be regressed by this work.
- **FR-002**: The `canvas` control MUST carry its drawn content as the project's existing immutable scene representation, produced by the application from its own model (no new drawing primitives are required for v1).
- **FR-003**: The system MUST derive canvas invalidation from the existing content fingerprint so that any render-affecting change to the supplied drawing is detected automatically, with no application-authored "should-repaint" predicate required.
- **FR-004**: The system MUST allow a canvas to be designated as continuously-changing ("volatile"), in which case its content bypasses the reuse/replay cache (record-then-immediately-invalidate is avoided) and is treated as always-dirty.
- **FR-005**: The system MUST isolate a volatile canvas behind a repaint boundary so that its per-frame redraw does NOT force repaint of unchanged surrounding content; surrounding chrome MUST remain cache-stable across frames in which only the canvas changes.
- **FR-006**: The system MUST forward raw pointer input (position, button state, wheel) targeting a canvas to the application's bound handler, with coordinates resolvable to the canvas's local space, honoring the laid-out box for hit-testing.
- **FR-007**: The system MUST allow a canvas to receive focus and MUST forward raw keyboard events (key press/release) to a focused canvas's bound handler.
- **FR-008**: The system MUST provide a reusable library of drawing elements (e.g., rectangles, sprites, circles, polylines, positioning/layering/caching helpers) authored as pure functions of their inputs, composable into a canvas scene and testable in isolation.
- **FR-009**: The system MUST provide a fixed-timestep game-loop helper that advances simulation by a deterministic number of fixed steps from accumulated tick time, supports interpolation between steps, and clamps abnormally large elapsed times to prevent unbounded catch-up.
- **FR-010**: The system MUST provide a documented pattern for reconstructing held-input state (set of held keys, current pointer state) from input edge-events, since the host exposes events rather than pollable input state.
- **FR-011**: The system MUST preserve determinism end-to-end: identical model state MUST produce an identical drawing, an identical fingerprint, and reproducible rendered frames; simulation MUST depend only on injected inputs (tick durations, input events, seed) and never on wall-clock time.
- **FR-012**: The canvas and its supporting library MUST be embeddable within the existing themed control tree (a viewport alongside chrome); a full-window / no-chrome host mode is explicitly out of scope for this feature.
- **FR-013**: The canvas MUST show a clear design-time placeholder when no drawing has been supplied, and MUST render safely (no crash) for empty, zero-size, or unmeasured boxes.
- **FR-014**: The feature MUST ship with a runnable embedded sample (e.g., a minimal game or animated demo) that exercises the canvas, the element library, input forwarding, and the fixed-timestep loop, and produces repeatable seeded evidence.
- **FR-015**: All new application-facing capability MUST be reachable through the project's existing public package surface; sample applications MUST build against that public surface only.
- **FR-016**: The canvas MUST support an optional viewport transform (pan/zoom) applied to the drawn content only and NOT to the control's laid-out size; absent a viewport, content is painted at the box origin with no additional transform. The viewport transform composes with the box-origin translate/clip of FR-001 and does not alter hit-testing of the laid-out box.

### Key Entities *(include if feature involves data)*

- **Canvas control**: a leaf control kind in the control tree that carries an application-supplied immutable drawing, has a laid-out box, can hold focus, can be flagged volatile (continuously-changing), applies an optional viewport (pan/zoom) transform to its content, and clips its content to its box.
- **Drawing / Scene**: the immutable display-list content an application produces from its model and hands to a canvas; the unit over which the content fingerprint is computed and determinism is asserted.
- **Drawing element**: a reusable pure function from inputs (props) to drawing content; the building block of the element library, composable and independently testable.
- **Game-loop state**: the accumulator/timestep bookkeeping (accumulated time, fixed step size, interpolation factor, clamp bound) that turns a stream of tick durations into a deterministic sequence of simulation steps.
- **Input state**: the reconstructed held-key set and pointer state derived from raw input edge-events, consumed by application simulation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can place a canvas carrying an application-authored drawing inside the themed UI and see that drawing painted, correctly positioned and clipped, with no changes to the existing public package surface beyond the new canvas capability.
- **SC-002**: Rendering the same model state any number of times produces byte-identical drawings and identical content fingerprints — 100% reproducibility across repeated and headless runs.
- **SC-003**: When only the canvas changes between two frames, 0 surrounding chrome nodes are repainted (the surrounding UI is fully cache-stable while the canvas updates every frame), demonstrated by the project's render-work accounting.
- **SC-004**: An animated canvas sustains continuous redraw (target ~60 fps cadence on the existing animation tick) without degrading the responsiveness/throughput of the surrounding application beyond the project's existing per-frame budgets, verified against the existing perf/responsiveness lanes (features 160/161/167/173) with an animating canvas in the tree.
- **SC-005**: Raw pointer and keyboard events reach a targeted/focused canvas's handler in 100% of in-box / focused cases and in 0% of out-of-box / unfocused cases.
- **SC-006**: A seeded sample run with a scripted input sequence reproduces identical world state, scenes, and fingerprints on every run, and demonstrably clamps a injected oversized frame time (no unbounded catch-up).
- **SC-007**: A developer can compose a canvas scene entirely from the reusable element library and drive it with the fixed-timestep loop helper without writing any custom invalidation or cache logic.

## Assumptions

- **Embedded placement only**: the selected model is an embedded viewport inside the existing control tree; a full-window/no-chrome game host (the deselected "Option C") is out of scope for this feature.
- **Volatile by default for games**: a canvas intended for animation/games defaults to volatile (no-cache) behavior, with the option to treat a static canvas as cacheable; static-dashboard canvases that never change may opt into caching. (Open question 2 in the source report; defaulted here, revisitable in planning.)
- **Raw input is sufficient for v1**: forwarding raw pointer samples and raw key events is sufficient for the first iteration; higher-level gesture/drag interpretation surfaced to the canvas is not required for v1. (Open question 5 in the source report.)
- **CustomControl disposition**: the existing `CustomControl` is left functionally intact for this feature; converging it onto the new canvas machinery is a possible follow-up, not in scope here. (Open question 3 in the source report.)
- **No new rendering primitives**: the existing immutable scene IR already expresses arbitrary 2D drawing; v1 introduces no new low-level drawing primitives or GPU shader/compute pipeline, and no entity-component-system runtime (start functional, revisit only if entity counts prove hot).
- **Determinism via injected time**: the loop consumes a nominal fixed tick interval as a parameter; no component reads wall-clock time directly, preserving seeded reproducibility and golden-frame tests.
- **Validation environment**: live/interactive validation runs on the existing Linux desktop GL-gated lanes; pure logic (element functions, loop accumulator, fingerprints) is testable headlessly without GL.
- **Scene-carrier mechanism and library packaging** (where the drawing is stored on the control, and whether the element/loop helpers live in a new module or an existing one) are implementation decisions deferred to `/speckit-plan`. (Open questions 1 and 4 in the source report.)
