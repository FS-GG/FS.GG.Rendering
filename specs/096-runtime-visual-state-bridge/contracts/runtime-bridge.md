# Contract: Runtime Visual-State Projection + Host Bridge (Feature 096)

The interface this feature exposes is split in two: a **public pure projection**
(`deriveVisualState`, the only consumer-/host-reachable entry) and an **internal host bridge**
(`applyRuntimeVisualState` and the feature-112 targeted variants, reachable from `Controls.Tests` via
`InternalsVisibleTo`). The **only public-surface entry** is `deriveVisualState` on the `ControlRuntime`
module, already committed in `tests/surface-baselines/FS.GG.UI.Controls.txt`. This contract is what the
`Feature096RuntimeBridgeTests` / `Feature096BridgePropertyTests` / `Feature096LiveBridgeTests` suites
pin.

## 1. Public surface (committed; zero new delta)

```fsharp
// ControlRuntime.fsi — public
val deriveVisualState : model: ControlRuntimeModel -> controlId: ControlId -> VisualState   // line 96
```

The supporting types — `ControlRuntimeModel` (`.fsi:41`), `ControlRuntimeMsg` (`.fsi:53`),
`ControlRuntimeEffect` (`.fsi:28`) — and the `VisualState` DU (`Types.fsi:256`) were all committed at
import.

**Contract**: `deriveVisualState` is the surface entry; everything else below is `internal`. The
surface-drift check (`tests/surface-baselines/FS.GG.UI.Controls.txt`, lines 61-89) MUST pass
**unchanged** (FR-008, SC-008).

### `deriveVisualState`
- **Contract**: a **pure, total, deterministic** projection from the live `ControlRuntimeModel` and a
  `ControlId` to a single `VisualState`, under the **closed runtime precedence**
  `Pressed > Selected > Focused > Hover > Normal`. No clock/randomness/I/O; **no per-kind branching**;
  identical inputs → identical result; an unknown or unreferenced id ⇒ `Normal`; **never throws**
  (FR-001, FR-002, SC-001). The head states `Disabled`/`Validation`/`Loading` are **never** produced —
  they are author intent only.

## 2. Internal host bridge (`ControlRuntime.fsi`; `InternalsVisibleTo("Controls.Tests")`)

```fsharp
// ControlRuntime.fsi — internal
val internal applyRuntimeVisualState :
    model: ControlRuntimeModel -> control: Control<'msg> -> Control<'msg>          // line 103

type internal RuntimeStampResult<'msg> =                                           // line 76
    { Stamped: Control<'msg>
      RuntimeStateTouchedNodeCount: int }

val internal applyRuntimeVisualStateTargeted :                                     // line 116
    prev: ControlRuntimeModel -> cur: ControlRuntimeModel ->
    prevStamped: Control<'msg> -> fresh: Control<'msg> -> RuntimeStampResult<'msg>

val internal runtimeStampFor :                                                     // line 129
    prior: (ControlRuntimeModel * Control<'msg>) option ->
    cur: ControlRuntimeModel -> fresh: Control<'msg> -> RuntimeStampResult<'msg>
```

### `applyRuntimeVisualState`
- **Contract**:
  1. **Pre-reconcile tree walk** — a pure recursive walk of the lowered `Control<'msg>` tree in the
     `ControlId` domain (id = `Key` if present, else `Kind`), applied before reconcile (FR-004).
  2. **Consumer intent wins** — a consumer-set non-`Normal` `visualState` is preserved unchanged; only
     a consumer-`Normal`/absent slot is filled by `deriveVisualState model id` (FR-003, SC-004).
  3. **One carrier, replace-or-append** — arbitration flows through the lone `visualState` attribute
     feature 093's `visualStateOf` reads; the bridge replace-or-appends (last writer), no stale
     accumulation (FR-003).
  4. **Byte-identical at rest** — a derived `Normal` emits **nothing**: the control/tree is returned
     verbatim, renders a `Scene` byte-identical to the un-bridged build, and recomputes **zero** nodes
     on the live retained path (FR-005, SC-003).
  5. **Realized by 093, not duplicated** — the look is painted by feature 093's resolver (E3); 096
     adds no draw and duplicates neither the resolver nor 095's slot composition (FR-004).
  6. **Rides E2 by construction** — because the stamp is pre-reconcile, the derived state rides feature
     092's stable retained identity through the keyed diff, surviving a position-shifting re-render
     (FR-007, SC-007).

### `applyRuntimeVisualStateTargeted` / `runtimeStampFor` / `RuntimeStampResult` (feature 112)
- **Contract**: re-stamp only the controls whose interaction state changed between `prev`/`prior` and
  `cur`, returning the `Stamped` tree and `RuntimeStateTouchedNodeCount`. This realizes the **bounded
  repaint** — a localized interaction (a single hover) produces `RecomputedNodeCount < BaselineNodeCount`
  via the live retained path, with the changed control counted as work (FR-007, SC-005). All `internal`.

## 3. Scope of the visible restyle (FR-006, SC-006)

- **Widened kinds** — `button` / `slider` / `text-box` / `radio-group` / `switch` visibly restyle
  under a runtime state (resolved paint differs from `Normal`).
- **Unmigrated kinds** — `progress-bar` / `numeric-input` (and others) are stamped but show **no
  render delta**.
- **Every kind** — a `Normal` attribute is byte-identical to the unset (at-rest) render.
- The bridge stamps uniformly; the *visible* delta is a 093-resolver/styling concern. Widening further
  kinds is bounded follow-up DF-3.

## 4. Conformance mapping (contract clause → success criterion → test)

| Contract clause | SC | Test (suite) |
|---|---|---|
| Closed precedence / peel + unknown-id ⇒ `Normal` + determinism | SC-001 | precedence + Normal + deterministic (`Feature096RuntimeBridgeTests`) |
| No-attribute control restyles; non-interacted sibling unchanged | SC-002 | no-attribute restyle + sibling-unchanged (`Feature096RuntimeBridgeTests`) |
| Focus indicator with no consumer attr; focus move | SC-002 | focus-indicator + focus-move (`Feature096RuntimeBridgeTests`) |
| At-rest unchanged + Scene-identity + zero recompute | SC-003 | at-rest unchanged + Scene-identity + 0-recompute (`Feature096RuntimeBridgeTests`) |
| Consumer non-`Normal` wins; consumer-`Normal` takes derived | SC-004 | arbitration tests + closed-order property (`…RuntimeBridgeTests` + `…BridgePropertyTests`) |
| Total / deterministic / consumer-vs-derived ≥1000 cases | SC-004 | three `Gen096` properties (`Feature096BridgePropertyTests`) |
| Single-hover bounded repaint | SC-005 | bounded-repaint (`Feature096RuntimeBridgeTests`, live retained step) |
| Widened-kind restyle; unmigrated-kind no-delta; `Normal`≡unset | SC-006 | per-kind restyle + unmigrated no-delta (`Feature096RuntimeBridgeTests`) |
| Focus survives a sibling shift on a stable retained id | SC-007 | focus-survives-reshuffle (`Feature096LiveBridgeTests`; regenerates `readiness/`) |
| Zero public-surface-baseline delta | SC-008 | surface-drift check (`tests/surface-baselines/FS.GG.UI.Controls.txt`) |

## 5. Non-goals (permanent, not deferrals)

- The projection never derives the head states `Disabled`/`Validation`/`Loading` — they are author
  intent set through feature 093's attribute (FR-002).
- 096 adds no draw styling — painting is feature 093's resolver (E3); identity is feature 092 (E2);
  slot composition is feature 095. 096 is state derivation + stamping only.
- No pixel-level / desktop-visibility proof — render equivalence is structural `Scene` (in)equality
  (disclosed in `readiness/responds-proof.md`).
