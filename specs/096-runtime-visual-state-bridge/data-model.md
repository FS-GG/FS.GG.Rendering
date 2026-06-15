# Phase 1 Data Model: Runtime Visual-State Bridge (Feature 096)

The "data" here is the runtime-state vocabulary and the projection/bridge semantics — all already
declared in the source. This document records the entities, their fields/relationships, validation
rules, and the projection's and bridge's transitions, cross-referenced to the contract in
`contracts/runtime-bridge.md`.

## Entities

### `VisualState` (the per-control state, consumed)

- **Where**: `src/Controls/Types.fsi:256-264` (eight cases):
  `Normal | Disabled | Hover | Pressed | Focused | Selected | Loading | Validation of ValidationState`.
- **Role**: the state feature 093's resolver paints. 096 derives only the runtime-derivable **tail**
  (`Pressed`/`Selected`/`Focused`/`Hover`/`Normal`); the head states (`Disabled`/`Loading`/
  `Validation`) are author intent only and are never produced by the projection.
- **Full order**: `Disabled > Validation > Loading > Pressed > Selected > Focused > Hover > Normal`.
  096's **closed runtime precedence** is the tail `Pressed > Selected > Focused > Hover > Normal`.

### `ControlRuntimeModel` (the live interaction state, read)

- **Where**: `src/Controls/ControlRuntime.fsi:41-50` (public).
- **Fields the projection reads**: `FocusedControl: ControlId option`,
  `HoveredControl: ControlId option`, `PressedControls: Set<ControlId>`,
  `Selection: ControlSelection option` (the projection checks `Selection.ControlId`). Other fields
  (`Caret`, `Composition`, `ActiveDrag`, `Diagnostics`, `RecentEffects`) are not read by 096.
- **Maintained by**: the `ControlRuntime` MVU (`ControlRuntimeMsg` `.fsi:53`, `ControlRuntimeEffect`
  `.fsi:28`) as it receives input messages. 096 projects **from** it; it does not mutate it.
- **Validation rule**: an id named by no field resolves to `Normal` (the projection is total).

### `deriveVisualState` (the public projection)

- **Where**: `ControlRuntime.fsi:96` (public); impl `ControlRuntime.fs:208-222`.
- **Signature**: `deriveVisualState : model: ControlRuntimeModel -> controlId: ControlId -> VisualState`.
- **Transition** (the closed cascade, `.fs:208-222`):
  ```
  if    PressedControls.Contains id                       -> Pressed
  elif  Selection |> Option.exists (s -> s.ControlId = id) -> Selected
  elif  FocusedControl = Some id                          -> Focused
  elif  HoveredControl = Some id                          -> Hover
  else                                                       Normal
  ```
- **Validation rules**: pure / total / deterministic; no per-kind branching; never throws; unknown or
  unreferenced id ⇒ `Normal`; the head states are never returned.

### `visualState` attribute (the single carrier)

- **Where**: builder `src/Controls/Attributes.fs:72` —
  `let visualState state = create "visualState" State (VisualStateValue state)`.
- **Role**: the lone attribute channel feature 093's `visualStateOf` reads and the resolver consumes.
  The bridge replace-or-appends it (last writer); both author intent and derived runtime state ride
  this one channel, indistinguishable at paint time.
- **Reader**: `visualStateOf : attrs: Attr<'msg> list -> VisualState` (`Control.fsi:100` /
  `Control.fs:96`); absent attribute ≡ `Normal`.

### `applyRuntimeVisualState` (the internal host bridge)

- **Where**: `ControlRuntime.fsi:103` (internal); a pure recursive tree walk.
- **Signature**: `internal applyRuntimeVisualState : model: ControlRuntimeModel -> control:
  Control<'msg> -> Control<'msg>`.
- **Behavior**: walks the lowered tree keyed by `ControlId` (= `Key` if present, else `Kind`),
  pre-reconcile. For each control: if the consumer set a non-`Normal` `visualState`, preserve it;
  else if the derived state is non-`Normal`, replace-or-append it onto the carrier; else (derived
  `Normal`, consumer unset) emit nothing — return the control verbatim.
- **Validation rules**: byte-identical at rest (derived `Normal` ⇒ no attribute); consumer non-`Normal`
  preserved 100%; pure / total; rides E2 retained identity because the stamp is pre-reconcile.

### Feature-112 targeted-stamp variants (internal)

- **`RuntimeStampResult<'msg>`**: `ControlRuntime.fsi:76-78` (internal) —
  `{ Stamped: Control<'msg>; RuntimeStateTouchedNodeCount: int }`.
- **`applyRuntimeVisualStateTargeted`**: `ControlRuntime.fsi:116-121` (internal) —
  `prev -> cur -> prevStamped -> fresh -> RuntimeStampResult<'msg>`; re-stamps only the controls whose
  interaction state changed between `prev` and `cur`, reporting the touched-node count.
- **`runtimeStampFor`**: `ControlRuntime.fsi:129-133` (internal) —
  `prior: (ControlRuntimeModel * Control<'msg>) option -> cur -> fresh -> RuntimeStampResult<'msg>`;
  the entry the host calls per frame (no prior ⇒ full stamp; prior ⇒ targeted delta).
- **Role**: realize the **bounded repaint** (SC-005) — a localized interaction touches fewer than all
  nodes. All three are `internal`; none widen the public surface.

## State transition: the bridge per control

```
applyRuntimeVisualState model control, at each node id = Key ?? Kind:
  let consumer = visualStateOf control.Attributes
  if consumer <> Normal then control                       // author intent wins (FR-003)
  else
    match deriveVisualState model id with
    | Normal  -> control                                   // at-rest identity (FR-005 / SC-003)
    | derived -> { control with Attributes =               // replace-or-append the single carrier
                     control.Attributes |> upsert (visualState derived) }
  // recurse into Children (pre-reconcile walk; rides E2 via keyed diff)
```

**Properties** (pinned by the suites):
- **Closed precedence** — `deriveVisualState` returns the highest-ranked tail state; peeling reveals
  the next; unknown id ⇒ `Normal` (SC-001).
- **Consumer-wins** — a consumer non-`Normal` state is preserved 100%; a consumer-`Normal`/absent slot
  takes the derived state (SC-004).
- **At-rest identity** — derived `Normal` emits nothing; tree unchanged, `Scene` byte-identical, zero
  recompute (SC-003).
- **Scoped restyle** — widened kinds show a paint delta; unmigrated kinds show none; a `Normal`
  attribute is byte-identical to the unset render for every kind (SC-006).
- **Totality / purity / determinism** — never throws; no clock/randomness/I/O; identical inputs →
  identical output (SC-004).

## Relationships to other features

- **E2 (retained identity, feature 092)**: the derived state rides the control's attributes through the
  keyed reconciler / live `RetainedRender` path, consumed not re-derived — a focus indicator survives a
  sibling-shifting re-render (FR-007, SC-007).
- **E3 (visual-state resolver, feature 093)**: paints the derived state read from the `visualState`
  carrier; 096 adds no draw of its own (FR-004).
- **Feature 095 (slot composition)**: 096 stamps the lowered tree *after* 095's slot lowering; it
  duplicates neither the resolver (093) nor slot composition (095).
- **`ControlRuntime` MVU (Principle IV)**: maintains the `ControlRuntimeModel` 096 reads; 096 is the
  pure read side and adds no stateful workflow.

See `contracts/runtime-bridge.md` for the authoritative projection + bridge contract the suites pin.
