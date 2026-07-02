# Feature Specification: unify control-id schemes onto `Key ?? path`

**Feature Branch**: `232-unify-control-id-schemes`

**Created**: 2026-07-02

**Status**: Draft

**Input**: Finding P1 / C1–C2 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#44**.

## Context (non-normative)

A lowered `Control<'msg>` tree is addressed by an *id* in several host seams — layout/hit-test,
event-binding routing, focus ordering, focus stamping, runtime visual-state stamping, scroll-offset
stamping, and transient-widget overlay/focus metadata. Feature 098 unified most of these onto a
single **structural-path** scheme: **`Key ?? path`**, where `path` is the positional path from the
root (`"0"`, child *i* → `parent + "." + i`). Layout (`Control.collectBoundsWith`), event bindings
(`Control.eventBindingsOf` / `boundIdsOf`), and focus **stamping** (`Focus.markFocused`) already emit
`Key ?? path`.

**The gap — three schemes still coexist:**

1. **`Key ?? path`** (the unified scheme): layout, hit-test, dispatch/binding routing, `Focus.markFocused`.
2. **`Key ?? Kind`** (the legacy scheme): `Focus.order` (`src/Controls/Focus.fs:52-53`) mints focus-stop
   ids by `Kind`; the runtime visual-state bridge `ControlRuntime.applyRuntimeVisualState` /
   `finalVisualState` / `applyRuntimeVisualStateTargeted` (`src/Controls/ControlRuntime.fs:271,292,369`)
   and `applyScrollOffsets` (`:369`) compute the stamp id by `Kind`; the Elmish hover/press/text bridge
   looks nodes up by `Kind` (`src/Controls.Elmish/ControlsElmish.fs:969,1220,1394,1537`).
3. **`RetainedId`**: a separate stable-identity domain for retained-tree reconciliation (out of scope
   here — it is internally coherent; this feature does not touch it).

**Why it breaks (unkeyed controls only — keyed controls already agree, since `Key` wins in every
scheme):** for any control with no authored `Key`, scheme 1 yields its `path` while scheme 2 yields
its `Kind`, and the two never match. Concretely:

- **Hover / press disagree with the visual stamp.** Hit-test reports `HoveredControl = "0.3"` (path),
  but the runtime visual-state bridge stamps the node whose `Key ?? Kind` = `"button"` — so the derived
  interaction state lands on the wrong node (or no node), and unkeyed same-kind siblings all receive
  the same stamp.
- **Keyboard activation of an unkeyed focused control dispatches nothing.** `Focus.order` records the
  focus stop under `Kind`, so the focus model holds `"button"`, but `routeFocusedKey` filters event
  bindings keyed by `path` — the focused id never matches a binding, so the keypress is dropped.
- **Unkeyed same-kind siblings collapse onto one focus stop.** Two unkeyed `button`s both get focus id
  `"button"`, so tabbing cannot distinguish them (admitted in the comment at `ControlsElmish.fs:903-906`).

**Phantom widget ids (structurally guaranteed `MissingOverlayAnchor`):** transient widgets declare ids
no control actually carries:

- `WidgetLowering.focusScope` fabricates focus stops `surfaceId + "-item-1"` / `"-item-2"`
  (`src/Controls/Widgets/WidgetLowering.fs:45-50`) that no lowered control keys.
- `DatePicker` (`Widgets/Pickers.fs:57-60`) and `SplitButton` (`Widgets/Buttons.fs:72-75`) declare
  `triggerId = rootId + "-trigger"` without applying that `Key` to the trigger `Button`, so the overlay
  anchor never resolves.

**The fix:** collapse schemes 1 and 2 into **one shared `Key ?? path` id function** used by focus
ordering, runtime visual-state stamping, scroll-offset stamping, Elmish binding routing / node lookup,
and widget metadata; thread the structural `path` through the runtime visual-state bridge and the
Elmish adapter so those seams have a `path` to key by; give transient widgets **real** `Key`s for their
fabricated trigger/item ids; and update the `Diagnostics` unkeyed-collapse rule and any `.fsi`
contracts to describe the unified scheme. This is the root cause behind most unkeyed-control behavior
loss identified in the review.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unkeyed focused control activates by keyboard (Priority: P1)

A developer builds a scene with an **unkeyed** interactive control (e.g. a `Button` with no
`withKey`). The user tabs to it and presses Enter/Space. The control's activation binding fires and the
message is dispatched — exactly as it would for a keyed control.

**Why this priority**: This is the primary correctness loss. Today a focused unkeyed control's keypress
is filtered out because the focus id (`Key ?? Kind`) and the binding id (`Key ?? path`) are in different
schemes, so keyboard activation silently does nothing.

**Independent Test**: Lower a tree with one unkeyed focusable control, focus it, route an activation
key through the focus/dispatch seam, and assert the expected message is produced (non-empty dispatch).

**Acceptance Scenarios**:

1. **Given** a lowered tree with a single unkeyed focusable control, **When** it is focused and an
   activation key is routed, **Then** the control's binding fires and a message is dispatched.
2. **Given** the same tree, **When** `Focus.order` is computed, **Then** the focus stop's id equals the
   id `Control.eventBindingsOf`/`boundIdsOf` emit for that node (`Key ?? path`), not its `Kind`.

---

### User Story 2 - Hover/press state lands on the control under the pointer (Priority: P1)

A developer hovers/presses an **unkeyed** control. The runtime-derived visual state (Hover/Press) is
stamped onto that exact node — the same node hit-test reported — and no other same-kind sibling is
affected.

**Why this priority**: Visual feedback is a core interaction guarantee. Today the pointer resolves to a
`path` id while the visual bridge stamps a `Kind` id, so hover/press marks the wrong node or every
same-kind sibling.

**Independent Test**: Build a tree with two unkeyed same-kind controls, set the runtime hovered id to
the path of the second one, run the visual-state bridge, and assert only the second node carries the
Hover stamp.

**Acceptance Scenarios**:

1. **Given** two unkeyed same-kind controls and a runtime `HoveredControl` = the second node's `path`,
   **When** the visual-state bridge runs, **Then** only the second node is stamped Hover and the first
   is byte-identical to its un-bridged form.
2. **Given** a runtime scroll offset keyed by a `scroll-viewer`'s `path`, **When** `applyScrollOffsets`
   runs, **Then** the offset is applied to that node (path-keyed), consistent with layout.

---

### User Story 3 - Transient widgets carry the ids they declare (Priority: P2)

A developer opens a `DatePicker` / `SplitButton` (overlay-anchored) or a widget that declares a focus
scope. The declared trigger/item ids correspond to real lowered controls, so the overlay anchor
resolves and the declared focus stops are reachable — no `MissingOverlayAnchor`, no phantom stops.

**Why this priority**: Distinct, self-contained defect class (phantom ids) that is structurally
guaranteed today; lower frequency than the pervasive unkeyed-control loss but still a correctness bug.

**Independent Test**: Lower a `DatePicker`/`SplitButton`, collect the lowered control ids, and assert
each declared `triggerId`/focus-scope item id is present in the lowered id set; run the overlay-anchor
diagnostic and assert no `MissingOverlayAnchor`.

**Acceptance Scenarios**:

1. **Given** a lowered `DatePicker`/`SplitButton`, **When** its declared `triggerId` is looked up in the
   lowered control ids, **Then** a control carries that id (the trigger `Button` is keyed with it).
2. **Given** a widget declaring a focus scope with item stops, **When** the tree is lowered, **Then**
   every declared item stop id is carried by a real lowered control (no fabricated `-item-N` stops).
3. **Given** any of the above, **When** the overlay-anchor / focus diagnostics run, **Then** no
   `MissingOverlayAnchor` and no phantom-stop diagnostic is emitted.

---

### User Story 4 - Diagnostics describe the unified scheme (Priority: P3)

The unkeyed same-kind collapse diagnostic (`Diagnostics.fs`) and the `.fsi`/doc contracts describe the
**single** `Key ?? path` scheme, so guidance the framework surfaces to a product author matches actual
behavior.

**Why this priority**: Documentation/diagnostic coherence — prevents the finding from re-appearing as a
"docs contradict behavior" defect and keeps authoring guidance truthful.

**Independent Test**: Read the unkeyed-collapse diagnostic message and the focus/runtime `.fsi` doc
comments; assert they reference `Key ?? path` (not `Key ?? Kind`) and that the collapse warning fires
only for genuinely ambiguous cases under the unified scheme.

**Acceptance Scenarios**:

1. **Given** the updated `Diagnostics` rule, **When** two unkeyed same-kind siblings exist, **Then**
   the emitted guidance references the unified `Key ?? path` scheme and the correct remediation
   (`Control.withKey`).
2. **Given** the `.fsi` contracts for `Focus`, `ControlRuntime`, and widget lowering, **When** read,
   **Then** they describe `Key ?? path` as the single id scheme for those seams.

---

### Edge Cases

- **Keyed control unchanged**: a control with an authored `Key` keeps the identical id in every seam
  (`Key` wins in both old and new schemes) — no behavior change; existing keyed tests stay green.
- **Root control**: the root's `path` is `"0"`; an unkeyed root stamps/focuses under `"0"` coherently.
- **Consumer-set visual state**: a consumer-set non-Normal `visualState` attribute still wins over the
  derived state (precedence unchanged); only the *id* the derived state keys by changes.
- **At-rest byte-identity**: with nothing hovered/focused/pressed and no scroll offset, the bridged tree
  is byte-identical to the un-bridged build (the id-scheme change must not perturb the at-rest tree).
- **Structural mismatch self-heal**: the targeted visual-state walk's child-count-mismatch fallback
  stays total under path threading.
- **Deeply nested / reordered children**: `path` is positional, so re-keying by `path` must match the
  exact path layout/hit-test computed for the same tree (same `"0.1.2"` derivation).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST address every host seam that today keys by `Key ?? Kind` — focus
  ordering (`Focus.order`), runtime visual-state stamping (`applyRuntimeVisualState`,
  `finalVisualState`, `applyRuntimeVisualStateTargeted`), scroll-offset stamping
  (`applyScrollOffsets`), and Elmish node lookup/binding routing — by the unified **`Key ?? path`** id
  instead.
- **FR-002**: The system MUST provide a single shared id derivation (`Key ?? path`) reused by focus,
  runtime stamping, binding routing, and widget metadata — no seam MAY re-derive the id by `Kind`.
- **FR-003**: The runtime visual-state bridge and scroll-offset bridge MUST thread the structural
  `path` (root `"0"`, child *i* → `parent + "." + i`) through their tree walks so each node's id is
  `Key ?? path`, matching the path layout/hit-test/bindings compute for the same tree.
- **FR-004**: The Elmish hover/press/text bridge MUST resolve a node's id/state by `Key ?? path` so
  the id it looks up matches the id hit-test/dispatch produced.
- **FR-005**: For any unkeyed focusable control, `Focus.order` MUST emit a focus stop whose id equals
  the id `Control.eventBindingsOf`/`boundIdsOf`/`markFocused` use for that node, so `routeFocusedKey`
  finds the focused control's bindings and keyboard activation dispatches.
- **FR-006**: Unkeyed same-kind siblings MUST receive distinct focus-stop ids (their distinct paths),
  so tab order can address each independently.
- **FR-007**: Transient widgets (`DatePicker`, `SplitButton`, and any `focusScope`-declaring widget)
  MUST apply the `Key`s they declare to the real lowered controls, so a declared `triggerId`/item id
  is carried by an actual control and the overlay anchor resolves — eliminating structurally
  guaranteed `MissingOverlayAnchor`/phantom-stop diagnostics.
- **FR-008**: Keyed controls MUST keep byte-identical ids and behavior in every seam (no regression for
  the keyed case).
- **FR-009**: With nothing hovered/focused/pressed and no scroll offset, the bridged tree MUST remain
  byte-identical to the un-bridged build (at-rest byte-identity preserved).
- **FR-010**: The `Diagnostics` unkeyed same-kind collapse rule and the affected `.fsi`/doc contracts
  (`Focus`, `ControlRuntime`, widget lowering) MUST describe the unified `Key ?? path` scheme and
  correct remediation.
- **FR-011**: The public API surface (`.fsi`) MUST remain coherent with the change; any signature or
  doc drift MUST be updated in the same feature, and the public-surface/ApiCompat gate MUST pass.

### Key Entities

- **Control id (`ControlId`)**: the string identity of a lowered control at a host seam. Unified
  derivation: `Key ?? path`, where `Key` is the authored `Control.Key` and `path` is the positional
  structural path from the root (`"0"`, child *i* → `parent + "." + i`).
- **Structural path**: the positional address of a node in the lowered tree, computed identically by
  layout, hit-test, event bindings, focus, and (after this feature) runtime stamping.
- **Focus stop**: an entry in `TabOrder` addressing a focusable control by its `ControlId`.
- **Widget-declared id**: a `triggerId` / focus-scope item id a transient widget declares for its
  overlay anchor or focus scope; after this feature it is a real `Key` on a lowered control.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An unkeyed focusable control, when focused, dispatches its activation message on keypress
  in 100% of cases (today: 0% for unkeyed controls).
- **SC-002**: Hover/press-derived visual state is stamped on exactly the node the pointer resolves to;
  0 same-kind sibling controls are incorrectly stamped for the unkeyed case.
- **SC-003**: 0 focus-stop id collisions among unkeyed same-kind siblings (each addressable
  independently in tab order).
- **SC-004**: 0 structurally-guaranteed `MissingOverlayAnchor` / phantom-stop diagnostics from
  `DatePicker`, `SplitButton`, or `focusScope`-declaring widgets on a well-formed tree.
- **SC-005**: 100% of previously-green keyed-control tests remain green (no keyed-case regression); the
  at-rest bridged tree is byte-identical to the un-bridged build.
- **SC-006**: The full solution builds and the complete test suite (all Controls / Elmish / diagnostics
  projects) passes, and the public-surface / ApiCompat gate reports no unaccounted drift.

## Assumptions

- The `RetainedId` domain is internally coherent and out of scope; this feature unifies only schemes 1
  and 2 (`Key ?? path` vs `Key ?? Kind`). Retained hit-test (`resolveFocus`) already uses `RetainedId`
  and is not re-pointed here.
- The positional `path` derivation is the canonical one already used by `Control.collectBoundsWith` /
  `eventBindingsOf` (root `"0"`, child *i* → `parent + "." + i`); all newly path-threaded seams adopt
  that exact derivation so ids match across seams for the same tree.
- Visual-state precedence (consumer-set non-Normal wins; derived Normal emits nothing) is unchanged;
  only the id the derived state keys by changes.
- Existing tests that encode the old `Key ?? Kind` behavior for **unkeyed** controls are asserting the
  bug and MUST be updated to the unified scheme; keyed-control tests are expected to stay green
  unchanged.
- This is a `src/` + `.fsi` change to `FS.GG.UI` (Controls / Controls.Elmish / widget lowering /
  diagnostics); it ships through the normal `fs-gg-ui` release/coherent-set flow when merged.
- **Scoped deferral (FR-007, item-stops portion)**: the transient-widget **trigger→anchor** fix (the
  issue's explicitly named "structurally guaranteed `MissingOverlayAnchor`") is delivered by keying the
  trigger `Button`. The separate `focusScope` fabricated `-item-N` **focus-scope stops** are produced by
  the SHARED `WidgetLowering.transientMetadata` (7 widget callers + a `DataEntry2` copy) and feed
  overlay focus-trap traversal; unifying them onto real content ids is a distinct 8-site change to
  overlay-trap semantics and is **deferred to a scoped follow-up** rather than folded into this
  id-unification. US3 acceptance scenario 2 (item-stops) is deferred; scenarios 1 and 3 (trigger anchor,
  no `MissingOverlayAnchor`) are delivered.
