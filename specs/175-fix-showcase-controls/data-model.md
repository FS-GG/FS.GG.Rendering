# Phase 1 Data Model: Fix Non-Functional Controls in the Second Ant Showcase

Entities derive from the spec's Key Entities. This feature owns transient interaction state and
evidence records; it adds no persistent storage. Where an entity maps to existing shared types,
the type is named.

## Entity: Control

A catalog item in the showcase.

| Field | Type | Notes |
|-------|------|-------|
| ControlId | stable id | unique within the catalog (validated by existing CoverageTests) |
| PageId | string | the page it appears on (`PageRegistry`) |
| Classification | `Interactive` \| `DisplayOnly` | exactly one; no control unclassified (FR-012) |
| Contract | InteractionContract option | present iff `Interactive` |
| DisplayOnlyReason | string option | present iff `DisplayOnly` (`InteractionContracts.fs`) |

**Validation**: every catalog control resolves to exactly one classification; an `Interactive`
control MUST have a contract; a `DisplayOnly` control MUST have a recorded reason. No control may be
both or neither (FR-008, FR-012, SC-007).

## Entity: Interaction contract

The recorded promise for an interactive control (existing `InteractionContract`,
`samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fsi:5-17`).

| Field | Type | Notes |
|-------|------|-------|
| ContractId | string | family/contract identity |
| ControlIds | string list | controls the contract governs |
| InputKind | string | `pointer-discrete` \| `pointer-move` \| `key-down` |
| Action | string | the documented primary interaction |
| ExpectedStateChange | string | the promised transition |
| VisibleEvidence | string | the acceptance bar for live behavior |
| ScriptStep | Msg option | the scripted-coverage step |
| ThemeInvariant | bool | tree shape identical across appearances |

**Validation**: a contract's live behavior MUST match its scripted behavior (FR-007); the contract
is changed only if found to misstate intended behavior (spec assumption).

## Entity: Interaction state

The transient pointer/keyboard state a control exposes (maps to shared `VisualState` and
`PointerState`).

| Field | Type | Notes |
|-------|------|-------|
| Hover | ControlId option | `PointerState.Hover` (`Pointer.fs`) |
| Focus | ControlId option | runtime focused control |
| Active | bool | press-in-progress |
| Resolved | VisualState | `Normal` \| `Hovered` \| `Focused` \| combined, stamped by `applyRuntimeVisualState` |

**Transitions** (per appearance, FR-003/FR-004/FR-005):

- `Normal --pointer enter--> Hovered`; `Hovered --pointer leave--> Normal`.
- `Normal --focus gained--> Focused`; `Focused --focus moved/lost--> Normal` (affordance moves with
  focus).
- `Hovered + focus gained --> Hovered∧Focused` (combined; neither suppresses the other);
  `Hovered∧Focused --pointer leave--> Focused` (focus persists when pointer leaves).
- Display-only controls never leave `Normal` under input (FR-008).

## Entity: Scroll state

The content region's scroll model, keyed by the `scroll-viewer` `ControlId`.

| Field | Type | Notes |
|-------|------|-------|
| Offset | float | current scroll offset (px) |
| ContentHeight | float | intrinsic content extent |
| ViewportHeight | float | visible region height |
| Scrollable | bool | derived: `ContentHeight > ViewportHeight` |
| ThumbHeight | float | derived: `max(minThumb, ViewportHeight * ViewportHeight/ContentHeight)` |
| ThumbPosition | float | derived from `Offset / (ContentHeight - ViewportHeight)` |

**Transitions** (FR-001/FR-002/FR-009):

- `applyScrollDelta d`: `Offset' = clamp(Offset + d, 0, max(0, ContentHeight - ViewportHeight))`.
  Drag, wheel, and keyboard scroll all reduce to this transition.
- `Scrollable = false ⇒` no draggable thumb is presented (and a one-pixel overflow is treated as
  non-scrollable to avoid flicker).
- Hit-testing inside the region subtracts `Offset` before resolving the control under the pointer.

**Invariants**: `0 <= Offset <= max(0, ContentHeight - ViewportHeight)`; content translated by
`-Offset` and clipped to the viewport; thumb position monotonically tracks offset.

## Entity: Finding

A recorded interaction defect discovered during the pass (evidence record, not runtime state).

| Field | Type | Notes |
|-------|------|-------|
| FindingId | string | stable id |
| ControlIds | string list | affected controls |
| PageId | string | page where observed |
| Symptom | string | observed live failure |
| RootCause | string | confirmed cause |
| FixTier | `Tier1` \| `Tier2` | shared-surface vs sample-local |
| Status | `Open` \| `Fixed` \| `ReVerified` | lifecycle |
| Verification | string | how re-verified (live path / test name / environment-limited) |

**Transitions**: `Open --fix applied--> Fixed --re-run verification--> ReVerified`. The feature is
accepted only when zero findings remain not-`ReVerified` (SC-005), and every control is classified
(SC-007).

## Cross-entity rules

- Every `Interactive` Control's live evidence (Interaction state transitions and/or Scroll state)
  MUST satisfy its Contract's VisibleEvidence under real input (FR-006).
- No Control, page, or existing passing behavior is removed (FR-014).
- Affordance colors use Ant palette roles, valid in both appearances (FR-011).
