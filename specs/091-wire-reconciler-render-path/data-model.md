# Phase 1 Data Model: Wire the Keyed Reconciler onto the Render Path (Feature 091)

The **091-in-scope** entities of `module internal RetainedRender` and the `module internal Reconcile`
it wires. All types are `internal` (zero public-surface delta, FR-010). Fields introduced by later
features (092–120) are listed under each entity only as *out-of-scope markers* so the 091 boundary is
explicit; their semantics belong to their owning features.

## RetainedId

`RetainedId of uint64`

- The stable, path-independent identity the diff confers on a matched node.
- Minted from `RetainedRender.NextId` (monotonic per-host counter). Deterministic — no clock/random.
- **Not** the path-derived `ControlId` (which is unstable across a positional shift).
- **Validation/invariants**: identical frame sequences mint identical ids (D2/SC-005); a `Kind` change
  yields a *new* id (D3/FR-002); an unchanged matched node carries its id across frames (FR-001).

## RenderFragment

`{ OwnScene; SubtreeScene; Box; Fingerprint }`

- The cached, reusable unit of measure + paint for one node.
- `OwnScene` = the node's own painted contribution; `SubtreeScene` = pre-order painted scene of the
  node + descendants (reused verbatim when the subtree is unchanged **and** unshifted); `Box` = the
  node's evaluated absolute box (the reuse key).
- *Out of scope (120)*: `Fingerprint` — structural digest of `SubtreeScene`; not part of 091's contract.
- **Invariants**: reuse requires structural equality of inputs **and** an unchanged `Box` (D4).

## RetainedNode\<'msg\>

`{ Identity; Control; Fragment; Children }`

- One retained control node: its `RetainedId`, the lowered `Control<'msg>` it was built from, its
  cached `RenderFragment`, and its retained children (mirroring `Control.Children` order).
- **Relationships**: forms the retained tree rooted at `RetainedRender.Root`; children order mirrors
  the lowered control's children.

## RetainedUiState

`{ Animation: AnimationClock option; Text: TextInputModel option }`

- Per-control UI state keyed by the **stable** `RetainedId` so it survives a positional shift (FR-003).
- In 091: **carried** across the diff; nothing writes/advances it (091 only remaps the lookup; feature
  099 drives the clock live).
- Focus is **not** stored here — it stays in the consumer model's `ControlRuntime.FocusedControl`; 091
  only remaps its lookup key to `RetainedId`.

## AnimationClock  *(091 carries; 099/103 write)*

`{ Anim; Elapsed; Target; From }`

- The per-identity animation clock. In 091's scope it is the **carried** value whose survival and
  advanceability are proven (US2): `advance` continues from `Elapsed`, never resets.
- *Out of scope here*: live ticking (099), `Target` retarget logic, the `From` cross-fade snapshot
  (103). 091 only asserts the carried clock persists across an unrelated shift and advances.

## RetainedRender\<'msg\>  *(the durable Model)*

`{ Root; NextId; StateByIdentity; Theme; … }`

- **091 fields**: `Root` (retained tree), `NextId` (identity counter), `StateByIdentity`
  (`Map<RetainedId, RetainedUiState>`), `Theme` (the theme the structure was painted under).
- Lives in the host loop's existing mutable-ref state (the interpreter edge, D6).
- *Out of scope (092–120)*: `Memo`/`MemoEnabled` (113), `Layout` (097), `PictureCache`/`…Enabled`
  (116), `TextCache`/`…Enabled` (117). Not asserted by 091.

## WorkReductionRecord  *(SC-003 measurement)*

`{ BaselineNodeCount; RecomputedNodeCount; ChangedSubtreeBound; … }`

- **091 fields**: `BaselineNodeCount` (== N, what a full rebuild recomputes), `RecomputedNodeCount`
  (what the wired path actually recomputed), `ChangedSubtreeBound` (the genuinely-changed work).
- **091 invariant** (US3/SC-003): `RecomputedNodeCount ≤ ChangedSubtreeBound < BaselineNodeCount` for a
  localized change with no geometry shift.
- *Out of scope*: `ShiftedNodeCount`/`RemeasuredNodeCount`/memo/virtual/damage/cache counts (092, 097,
  113, 114, 116, 117, 120). Note 092 later **revises** the 091 inequality to
  `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount` to account for sibling-shift work —
  that refinement is feature 092's, not 091's.

## RetainedRenderStep\<'msg\>  *(the step result)*

`{ Retained; Render; Diagnostics; WorkReduction }`

- The result of one wired frame: the next `RetainedRender`, the `ControlRenderResult` (byte-identical
  to a full rebuild of `next`, FR-006), the `ControlDiagnostic` list surfaced from the diff (e.g.
  `KeyCollision`), and the `WorkReductionRecord`.

## RetainedInit\<'msg\>  *(seam; 092 shapes it)*

`{ Retained; Render; Diagnostics }`

- The first-frame result of `init`. 091 conceptually only needs the seeded `Retained` structure; the
  test helper `rinit` projects `.Retained`. The `Render`/`Diagnostics` fields are feature 092's
  first-frame-paint-once refinement, kept here only to note the seam.

## Reconcile types  *(feature 067, wired by 091)*

- `ReconcileResult<'msg> = { Patch: NodePatch<'msg>; Diagnostics: ControlDiagnostic list }`
- `NodePatch<'msg> = Keep | Replace of Control | Update of UpdatePatch`
- `ChildOp<'msg> = ChildKeep | ChildMove | ChildInsert | ChildRemove`
- `diff : prev → next → ReconcileResult` (pure, total, deterministic); `apply : prev → patch → Control`
  (round-trip oracle).
- **Invariants used by 091**: `diff` matches by `Key` then positionally; a `Kind` mismatch yields
  `Replace`; duplicate keys emit a `KeyCollision` `Warning`; the function never throws.

## ControlDiagnostic  *(observability channel)*

- `Code = KeyCollision` with `Severity = Warning` is the 091-relevant diagnostic (Types.fs).
- Surfaced on `RetainedRenderStep.Diagnostics` (and `RetainedInit.Diagnostics` for a first-frame
  collision) — the safe-failure contract (FR-009, Principle VI).
