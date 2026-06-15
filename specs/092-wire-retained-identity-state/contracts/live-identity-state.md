# Internal Contract: Live Retained-Identity State (Feature 092)

The internal seam the 092 suites pin. All operations are `internal` (zero public-surface delta,
FR-012), reached by the in-assembly tests via `InternalsVisibleTo`. Signatures are as declared in
`src/Controls/RetainedRender.fsi` and `src/Controls.Elmish/ControlsElmish.fsi`.

## Operations (092 slice)

### `RetainedRender.init : theme:Theme -> size:Size -> control:Control<'msg> -> RetainedInit<'msg>`
First frame. Returns the seeded retained structure, the **painted** `Render` the adapter reuses (no
second `Control.renderTree` — single first-frame paint), and any frame-0 `KeyCollision` in
`Diagnostics`. Total; never throws.

### `RetainedRender.step : theme:Theme -> size:Size -> r:RetainedRender<'msg> -> next:Control<'msg> -> RetainedRenderStep<'msg>`
One frame transition. Diffs `next` against `r`, carries/updates `StateByIdentity` by identity (Replace
drops, removal filters), uses `theme` in the reuse key, and yields the next `RetainedRender`, the
render (byte-identical to a full rebuild of `next`), the diagnostics, and the `WorkReductionRecord`.
Pure, total, deterministic.

### `RetainedRender.retainedHitTest : x:float -> y:float -> retained:RetainedRender<'msg> -> RetainedId option`
Resolves the point to the **deepest** retained node whose cached `Box` contains it; a **distinct**
`RetainedId` per node (incl. unkeyed same-kind siblings and keyed-container-wrapped fields); `None`
outside the root.

### `ControlsElmish.resolveFocus : retained:RetainedRender<'msg> -> x:float -> y:float -> RetainedId option`
The live focus seam. Routes a pointer to the focused node's stable identity via `retainedHitTest` over
the retained frame's cached boxes. Replaces the `ControlId` `hitTest`.

### `ControlsElmish.routeFocusedText : retained:RetainedRender<'msg> -> focused:RetainedId option -> msg:TextInputMsg -> RetainedRender<'msg> * 'msg list`
The live text seam. Reads/writes the focused identity's draft in `retained.StateByIdentity[id].Text`,
seeds from the control's current value (pre-filled append), honors `MultiLine`, dispatches **all**
matched `onChanged` product messages, and returns the next `RetainedRender` plus the message list.

## Contract invariants → requirements → tests

| Invariant | Requirement | Pinned by |
|---|---|---|
| Host reads/writes `StateByIdentity` on the real path | FR-001 | `Feature092LiveSurvivalTests` |
| Focus + draft survive a position shift (`hix`→`hixy`), no seeded state | FR-002, SC-001 | `Feature092LiveSurvivalTests` (survival) |
| Rebuild-every-frame baseline loses the state | FR-003, SC-001 | `Feature092LiveSurvivalTests` (baseline-fails) |
| Keyed/unkeyed/container fields → distinct ids; outside-root → `None` | FR-004, SC-002 | `Feature092RetainedRenderTests` (focus-resolution) |
| Pre-filled first keystroke appends; text-area MultiLine; zero loss | FR-005, SC-002 | `Feature092RetainedRenderTests` / live (prefilled-append) |
| Multiple change handlers all fire | FR-006 | `Feature092RetainedRenderTests` |
| Replace drops prior state; removal filters it out | FR-007 | `Feature092LiveSurvivalTests` |
| Theme in reuse key: change repaints byte-identically; no spurious repaint | FR-008, SC-006 | `Feature092RetainedRenderTests` (theme-reuse) |
| `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount < BaselineNodeCount` | FR-009, SC-003 | `Feature092RetainedRenderTests` (work-reduction) |
| `init` paints once; frame-0 `KeyCollision` surfaced; total | FR-010, SC-005 | `Feature092RetainedRenderTests` (first-frame) |
| Each chained frame byte-identical to rebuild; identity continuity; total/deterministic | FR-011, SC-004, SC-007 | `Feature092RetainedRenderTests` (multi-frame) |
| Zero public-surface-baseline delta | FR-012 | surface-drift check (`FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`) |

## Out of contract (092)

Animation-clock survival (owned by feature 099 — `Feature099AnimationClockTests/us2-survival`); layout
cache (097), cross-fade (103), perf driver (108/110), memo/virtualization/picture/text caches
(113/114/116/117), fingerprint/replay (120). Pixel-level / desktop-visibility parity (the readiness
evidence discloses it does not claim these).
