# Data Model: unify control-id schemes onto `Key ?? path`

**Feature**: 232 · This is an id-scheme unification; the "data model" is the set of id entities and
their derivations, not new persisted state.

## Entities

### ControlId (existing type, unified derivation)

The string identity of a lowered control at a host seam.

- **Unified derivation**: `id = Control.Key |> Option.defaultValue path`.
- **Field**: `Key : ControlId option` (authored via `Control.withKey`), `path : string` (positional,
  see below).
- **Invariant (post-feature)**: for a given lowered tree, every seam (layout, hit-test, event bindings,
  focus ordering, focus stamping, runtime visual-state stamping, scroll stamping, widget metadata)
  yields the **same** `id` for the same node. Keyed nodes: `id = Key`. Unkeyed nodes: `id = path`.

### Structural path

The positional address of a node in the lowered `Control<'msg>` tree.

- **Root**: `"0"`.
- **Child rule**: child at index *i* of a node at `path` → `path + "." + string i`.
- **Canonical producer**: `Control.eventBindingsOf` / `collectBoundsWith` / `LayoutEval.toLayout`
  (`Key ?? path`). All newly path-threaded walks (Group A/B, scroll) MUST reproduce this derivation
  exactly, including index continuity across subtrees a walk does not descend (Focus.order).

### FocusStop (existing; id domain changes)

- **Field**: `Control : ControlId` — after this feature = `Key ?? path` (was `Key ?? Kind`).
- **Effect**: unkeyed same-kind siblings now carry distinct `Control` ids (distinct paths) →
  independently addressable in `TabOrder`; `Focus.traverse` steps between them.

### ControlRuntimeModel (existing; read consistently)

Fields and their key domains after the feature (all `Key ?? path`):

| Field | Producer domain (unchanged) | Bridge reads (after) |
|---|---|---|
| `HoveredControl : ControlId option` | `Key ?? path` (hit-test) | `Key ?? path` ✅ aligned |
| `PressedControls : Set<ControlId>` | `Key ?? path` (hit-test) | `Key ?? path` ✅ aligned |
| `ScrollOffsets : Map<ControlId, ScrollState>` | `Key ?? path` (`collectScrollViewerIds`) | `Key ?? path` ✅ aligned |
| `FocusedControl : ControlId option` | now `Key ?? path` (resolver at site 1394) | `Key ?? path` ✅ aligned |

Precedence rules unchanged: a consumer-set non-Normal `visualState` attribute wins; a derived `Normal`
emits no attribute (at-rest byte-identity).

### RetainedId (existing; OUT OF SCOPE)

Retained-tree stable identity (`RetainedNode.Identity`). Keys `StateByIdentity`, `loopState.Focused`,
`resolveFocus`/`retainedHitTest`. **Not changed.** A new `internal` resolver maps a `RetainedId` → its
`Key ?? path` (read-only bridge into the unified scheme).

### Widget-declared ids (existing; made real)

- `TransientWidget.TriggerId` / `AnchorId` = `rootId + "-trigger"` → the trigger `Button` is now keyed
  with that id (a real control carries it).
- `FocusScope.Stops` / `InitialFocus` / `RecoveryTarget` → point at real lowered ids (trigger id +
  real content ids), replacing fabricated `surfaceId + "-item-N"`.

## Derivation reference (must match across seams)

```
id(node)        = node.Key |> Option.defaultValue path(node)
path(root)      = "0"
path(child i of p) = path(p) + "." + string i
```

## State transitions

None. This feature changes how nodes are *addressed*, not any state machine. Focus/hover/press/scroll
transitions are unchanged; they now act on the correct node for unkeyed controls.
