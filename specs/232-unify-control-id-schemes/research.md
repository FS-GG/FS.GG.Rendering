# Research: unify control-id schemes onto `Key ?? path`

**Feature**: 232 · **Date**: 2026-07-02 · Resolves FS-GG/FS.GG.Rendering#44 (Review P1)

This record resolves the one true unknown behind the fix: **at each site that keys by `Key ?? Kind`
today, is the backing data keyed by `Key ?? path` (so moving the site to `path` is a FIX) or by
`Kind`/`RetainedId` (so a naive move would BREAK)?** Getting the direction wrong would break
hover/focus rather than fix it. Determined by tracing every producer/consumer to `file:line`.

## Decision 1 — The unified id is full-tree `Key ?? path`

- **Decision**: One shared derivation `Key ?? path`, where `path` is the positional path from the root
  (`"0"`, child *i* → `parent + "." + string i`) — the exact scheme `Control.eventBindingsOf` /
  `collectBoundsWith` / `LayoutEval.toLayout` (`src/Controls/Internal/LayoutEval.fs:76-77`) already use.
- **Rationale**: Everything the pointer produces is already this scheme (hit-test runs over the layout
  whose ids are `Key ?? path`). Aligning the remaining seams to it makes the whole surface coherent
  without touching the pointer/layout producers.
- **Alternatives rejected**: (a) Move everything to `RetainedId` — that domain is retained-tree
  reconciliation identity, not a structural address usable by pure `Focus`/`Control` functions, and it
  is already internally coherent; widening it is a larger, riskier change. (b) Keep `Key ?? Kind` and
  make the pointer produce it — regresses layout/hit-test/bindings, the majority scheme.

## Decision 2 — The runtime model is **mixed-domain**; the bridge must read it per-field-correctly

`ControlRuntimeModel` (`src/Controls/ControlRuntime.fs:37-47`) is populated by `assembleRuntimeModel`
(`src/Controls.Elmish/ControlsElmish.fs:1389-1407`):

| Field | Key domain today | Producer |
|---|---|---|
| `HoveredControl` | **`Key ?? path`** (A) | `PointerState.Hover` ← `hitTest` over layout (`Pointer.fs:181,196`) |
| `PressedControls` | **`Key ?? path`** (A) | `PointerState.Presses[..].Control` ← same `hitTest` |
| `ScrollOffsets` | **`Key ?? path`** (A) | `loopState.ScrollOffsets` keyed by `collectScrollViewerIds "0"` (path) |
| `FocusedControl` | **`Key ?? Kind`** (B) | `focusedControlId` resolves `loopState.Focused: RetainedId` → node → `Key ?? Kind` (`:1394`) |

- **Decision**: `applyRuntimeVisualState` / `finalVisualState` / `applyScrollOffsets` read this model
  with **one** id per node = `Key ?? path`. That immediately fixes **hover / press / scroll** for
  unkeyed nodes (model already path-keyed; node id was `Kind` → never matched). For the **focus-ring**
  branch (`deriveVisualState`'s `FocusedControl` case), `FocusedControl` must ALSO become `Key ?? path`
  — i.e. site 1394 moves in lockstep, else the ring branch flips from self-consistent-B to broken.
- **Rationale**: hover/press/scroll producers are already scheme A; only the focus field is B, and it
  is self-consistent only because the bridge reads it with the same B id. Move the bridge → must move
  1394 too.

## Decision 3 — Coordinated move-groups (each is a self-cancelling island today)

Each scheme-B use only works today because its producer and consumer are BOTH scheme B, so the scheme
cancels within the island. Therefore members of an island must move **together**:

- **Group A — Focus traversal**: `Focus.order` (`Focus.fs:52,71`) → `nodeId` traverse use
  (`ControlsElmish.fs:1220`, feeds `Focus.traverse`) → `retainedIdOfControl` (`ControlsElmish.fs:1537`).
  Moving to `Key ?? path` **disambiguates unkeyed same-kind focusable siblings** (today they collapse
  to one `Kind` stop). Moving any one alone = BREAK.
- **Group B — Visual-state / focus-ring**: `applyRuntimeVisualState` + `finalVisualState`
  (+ `targetedWalk`) + `focusedControlId` (`:1394`). Fixes unkeyed hover/press immediately; keeps the
  ring branch correct only if 1394 moves too.
- **Independent — scroll**: `applyScrollOffsets`. Backing data (`ScrollOffsets`) is already scheme A,
  so this is a pure fix with **no partner** — unkeyed `scroll-viewer`s scroll after it.
- **Non-domain — text label**: `ControlsElmish.fs:969` (`routeFocusedText`). The `Key ?? Kind` value
  there is **not a map key** — it is a free-form label passed to `TextInput.init` and only echoed in
  effect payloads (no host looks it up against a tree scheme). Move it for label consistency; it is not
  a correctness site.

## Decision 4 — `routeFocusedKey` binding filter: route via the full-tree bindings

- **Problem**: `routeFocusedKey` (`ControlsElmish.fs:1219-1236`) computes `ownBindings =
  eventBindingsOf node.Control |> filter (b.ControlId = nodeId)`. `eventBindingsOf node.Control`
  **re-roots the node at `"0"`**, so a KEYED node's own binding id = `Key` (matches `nodeId=Key`), but
  an UNKEYED node's own binding id = `"0"` while `nodeId=Kind` → **no match → activation dispatches
  nothing** (the reported keyboard bug). The same `nodeId` is also fed to `Focus.traverse`, which needs
  the **full-tree** path id (to match `Focus.order`). One local variable, two rooting frames.
- **Decision**: Give the focused node a single **full-tree canonical id** (`Key ?? path`, recovered
  from the retained tree — see Decision 5) and filter the **full-tree** `eventBindingsOf r.Root.Control`
  (rooted at the real root `"0"`) by it — instead of the node-re-rooted `eventBindingsOf node.Control`.
  Then the one id serves both the traverse and the binding filter, and unkeyed focused controls
  dispatch. (Keyed nodes are unaffected either way, since `Key` wins in both framings.)
- **Alternative rejected**: keep the local re-rooted filter and compute a second, separate full-tree id
  for traverse — two ids per node, more surface, and the local-filter id (`Key ?? "0"`) is not
  meaningful outside the filter.

## Decision 5 — Recovering `Key ?? path` from a `RetainedId`

- **Fact**: `RetainedNode` (`RetainedRender.fs:95-100`) stores `Identity`, `Control`, `Fragment`,
  `Metadata`, `Children` — **no path / canonical id field**. `tryFindNode` matches by `Identity` and
  returns the node without a path.
- **Decision**: Add a small path-aware resolver (mirroring the existing walk in
  `RetainedRender.authoredControlIds`, `RetainedRender.fs:1596-1610`, which already computes
  `canonical = Key ?? path` per node): resolve a `RetainedId` → its full-tree `Key ?? path`. Use it at
  sites 1394 (focused-control id for the runtime model) and 1537 (match a traverse `next` id back to a
  `RetainedId`). This keeps the `RetainedId` domain untouched while producing the scheme-A id the
  unified seams need.
- **Rationale**: reuses an already-proven path walk; no new storage on the hot retained node.

## Decision 6 — Transient-widget real keys (FR-007)

- **Fact**: `Pickers.fs:57-60` (DatePicker) and `Buttons.fs:60-85` (SplitButton) declare
  `triggerId = rootId + "-trigger"` in `transientMetadata` but build `trigger = Button.create [...]`
  **without** applying that key → the overlay anchor (`AnchorId = triggerId`) resolves to no control →
  structurally guaranteed `MissingOverlayAnchor`. `WidgetLowering.focusScope` fabricates
  `Stops = [surfaceId+"-item-1"; surfaceId+"-item-2"]` and `InitialFocus = surfaceId+"-item-1"` that no
  lowered control carries.
- **Decision**: (a) **Key the trigger `Button`** with its declared `triggerId` (via `Control.withKey`)
  in both widgets, so `AnchorId`/`TriggerId` resolve to a real control. (b) Replace `focusScope`'s
  fabricated `-item-N` stops with the **real** lowered focusable ids of the surface's content (key the
  overlay's focusable items to the declared stop ids, or derive the stops from the actual content ids),
  and point `InitialFocus`/`RecoveryTarget` at real ids (`RecoveryTarget = triggerId` becomes valid once
  the trigger is keyed).
- **Rationale**: eliminates the entire phantom-id defect class; `AnchorId`/`RecoveryTarget` already
  reference `triggerId`, so keying the trigger is the minimal, high-value fix. The `-item-N` stops need
  the surface's content keyed to match — handled per-widget in tasks.

## Decision 7 — Diagnostics + `.fsi`

- **Decision**: Update the unkeyed same-kind collapse rule (`Diagnostics.fs:196-220`) and the affected
  doc/`.fsi` comments (`Focus.fsi`, `ControlRuntime.fsi`, widget-lowering docs, `Diagnostics.fsi`) to
  describe the unified `Key ?? path` scheme. The collapse warning now fires for genuinely ambiguous
  *authoring* cases (same-kind unkeyed interactive siblings the author should key), consistent with the
  unified behavior — not as a description of a bug the framework itself creates.
- **Rationale**: Constitution II (`.fsi` is the visibility/contract source) and the review's "docs
  contradict behavior" concern require the contract text to match the new single scheme.

## Standing-assumption note (unverified root cause → early smoke)

Per the plan template's standing assumption: the above is the *diagnosed* root cause. `tasks.md` MUST
schedule an **early live/behavioral smoke** (before the fixes) that reproduces at least one symptom —
an unkeyed focusable control failing to dispatch on keypress, and/or an unkeyed control not receiving
hover state — via the real focus/dispatch + runtime-bridge seams, so the fix is built on a confirmed,
not merely hypothesized, failure. Deterministic tests can pass while the running app stays broken
(Feature 175); the smoke exercises the seams end-to-end.
