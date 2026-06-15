# Phase 1 Data Model: Wire Retained Identity State onto the Live Path (Feature 092)

The entities 092 reads/writes on the live path. These are **already declared** in
`src/Controls/RetainedRender.fsi` and `src/Controls.Elmish/ControlsElmish.fsi`; this records the
092-in-scope slice and the live-state semantics the suites pin. Out-of-scope accreted fields
(097/099/103/108/110/113/114/116/117/120) are owned by their features.

## RetainedId  *(091 mints; 092 keys live state on it)*

`RetainedId of uint64` — the stable, path-independent identity the diff confers on a matched node.
Minted deterministically from a per-host counter (no clock/randomness). 092 makes it the **key** the
live host's focus and in-progress text resolve through, replacing the path-derived `ControlId`.

## StateByIdentity  *(the durable per-identity Model state 092 wires)*

`Map<RetainedId, RetainedUiState>` carried on `RetainedRender<'msg>`. 091 carried it untouched; **092
reads and writes it on the live path**. Updated per frame: matched nodes keep their entry, a `Replace`
drops the prior entry, a removed node's entry is filtered out (no orphans).

## RetainedUiState  *(per-control state that must survive a shift)*

The value stored under an identity: focus flag, in-progress text **draft + line mode** (`MultiLine`),
and the carried animation clock (written by feature 099, not 092). 092's job: the draft **continues**
across a positional shift (`hix` → `hixy`) and seeds from the control's current value so a pre-filled
field's first keystroke **appends** rather than wipes.

## RetainedRender\<'msg\>  *(the per-frame retained root)*

Carries `Root`/`NextId`, `StateByIdentity` (above), and **`Theme`** — the theme the structure was
painted under, now part of the fragment **reuse key** (FR-008). A theme change between frames
invalidates all cached fragments; an unchanged theme reuses everything.

## RetainedInit\<'msg\>  *(the first-frame result — 092 shapes it)*

`{ Retained: RetainedRender<'msg>; Render: <painted scene>; Diagnostics: ControlDiagnostic list }`.
`init` returns the **painted** `Render` the adapter reuses (single first-frame paint, FR-010), and
`Diagnostics` surfaces any frame-0 duplicate-key `KeyCollision`.

## RetainedRenderStep\<'msg\>  *(the per-frame `step` result)*

`{ Retained: RetainedRender<'msg>; Render: ControlRenderResult<'msg>; Diagnostics: ControlDiagnostic list;
WorkReduction: WorkReductionRecord }`. The value `step` returns each frame: the next retained structure,
the rendered frame (byte-identical to a full rebuild of `next`), the frame diagnostics, and the
`WorkReductionRecord` (below). Pure/total/deterministic on the live path.

## WorkReductionRecord  *(SC-003 measurement; 092 splits it honestly)*

`{ BaselineNodeCount; RecomputedNodeCount; ChangedSubtreeBound; ShiftedNodeCount }`. Under a
sibling-inserted-above shift: `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount <
BaselineNodeCount`. The relaid-out leaf is counted as *shifted*, not free.

## The adapter seam  *(Controls.Elmish internals 092 wires)*

- **`resolveFocus retained x y : RetainedId option`** — resolves a pointer to the focused node's
  stable identity via `retainedHitTest` over the retained frame's cached boxes (deepest node;
  per-node distinct id; `None` outside root). Replaces the `ControlId` `hitTest`.
- **`routeFocusedText retained (focused: RetainedId option) (msg: TextInputMsg) : RetainedRender<'msg> * 'msg list`** —
  routes a focused TEXT control's printable keys against the **retained** tree, reading/writing the draft held in
  `retained.StateByIdentity[id].Text`, seeding from the control's current value, honoring `MultiLine`,
  and dispatching **all** matched `onChanged` product messages. Returns the next `RetainedRender` plus
  the message list.

## ControlDiagnostic  *(observability channel)*

`KeyCollision` (and peers) surfaced as a `ControlDiagnostic` with a `Severity`. 092 surfaces a
first-frame duplicate-key collision on **frame 0** through `RetainedInit.Diagnostics`; the path stays
total (never throws).
