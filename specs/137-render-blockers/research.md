# Phase 0 Research — Render Blockers (Clipping, Overlay & Scroll)

Resolved decisions (R1–R4). Each: **Decision / Rationale / Alternatives considered**, anchored to concrete
code locations confirmed during investigation. The picture-cache root cause (R1) was confirmed by reading the
code, not inferred.

## Root-cause map (confirmed)

| Symptom (feature 136) | Confirmed cause (file:line) |
|---|---|
| `cache-on ≡ cache-off` byte-identity fails when container children are clipped | `RetainedRender.step` emits the painted scene two ways: the **fast path** `sceneList = newRoot.Fragment.SubtreeScene` (`RetainedRender.fs:1250`) which carries the clip built by `composeContainerScene`, and the **`assemble` walk** (`RetainedRender.fs:1252-1271`) used when `replayHitIds.Count > 0` or a clock is active, which rebuilds flat as `own @ (n.Children |> List.collect assemble)` (`:1269`) — **without the clip**. With the cache ON the rows hit, `assemble` runs, and the emitted scene is unclipped while the cache-OFF fast path is clipped → divergence. |
| Hit/effectiveness counters (`present-but-dead`, `effectiveness`) | The counters are computed by `walkPictures` over the **retained node tree** keyed by `pictureKeyOf`/`Fragment.Fingerprint` (`RetainedRender.fs:1127-1144`). A `data-grid-row` is a **leaf** (no children) so `composeContainerScene` leaves its `SubtreeScene`/fingerprint unchanged — the counters only regress as collateral of the same emit-walk inconsistency, not because clipping changes a leaf fingerprint. |
| `Audit_PictureCache` oracle | `tests/Controls.Tests/Audit_PictureCache.fs`: a `stack` of three cacheable `data-grid-row` controls; `flat` (`:43-55`) unwraps `Group`/`CachedSubtree` but **preserves `ClipNode`**; the parity assert is `Expect.equal (flat off.Render) (flat on.Render)` (`:88`), hits expected `=3`, misses `=0` (`:75-76`, `:108-110`). |

## R1 — Container clipping via ONE shared composition rule used at EVERY assembly site (DECIDED)

**Decision**: Re-introduce `ControlInternals.composeContainerScene (box) (own) (childScenes)` — own paint, then
the children wrapped in `Scene.clipped (RectClip box)` when there is a box and at least one child scene, else
flat `own @ childScenes`. Route **all six** assembly sites through it: `Control.renderTree`'s `paint`
recursion, the four `RetainedRender` build/carry sites (`build`, `buildFresh`, `carry`, and the
ChildInsert/Replace fallback), **and the `assemble` emit walk** (`RetainedRender.fs:1269`). The `assemble`
site is the one feature 136 missed; adding it makes the clipped scene identical whether emitted via the fast
path or the replay-emit walk.

**Rationale**:
- A single function reused everywhere makes full ≡ retained and `cache-on ≡ cache-off` true *by
  construction* — there is no second place for the rule to drift.
- The fingerprints are untouched (clipping only wraps a container's children; cacheable rows are leaves), so
  the cache hit rate / effectiveness margin is preserved — the `Audit_PictureCache` trio is the gate.
- It is the minimal, lowest-risk realization of FR-001/FR-002/FR-003: no change to `hashScene`,
  `pictureKeyOf`, `PictureReplayCache`, or the SKPicture record/replay path is required.

**Alternatives considered**:
- *Clip at paint time on the canvas instead of in the Scene IR* — rejected: the full-render path has no
  retained node tree at paint, and the clip must appear in the IR both paths share; canvas-only clipping would
  reintroduce a full/retained divergence.
- *Make `hashScene` clip-transparent / exclude container clips from the fingerprint* — rejected as
  unnecessary: leaf-row fingerprints don't change, so there is no fingerprint problem to solve once `assemble`
  is fixed. (Kept as a fallback only if a future cacheable *container* is introduced.)
- *Clip only in `renderTree`, not the retained path* — rejected: this is exactly the 136 regression.

**Open verification (probe-driven, per repo practice)**: before wiring all six sites, add the
container-clip and confirm the three `Audit_PictureCache` tests go green and a new full ≡ retained parity test
holds on the 3-row grid; treat any residual divergence as a finding to localize with a minimal probe.

## R2 — Deferred overlay pass, parity-preserving (DECIDED)

**Decision**: Add a deferred **overlay group** to `Control.renderTree`: walk the tree building the in-flow
scene; when a node is an overlay/transient surface (built on the existing `Overlay` container, `Control.fsi`),
emit its painted subtree into a separate ordered list instead of in-flow; the final scene is
`inFlow @ overlay` so overlays paint last (z-top) at their true coordinates, **outside** ancestor container
clips. Mirror the identical split in the retained `assemble`/`SubtreeScene` path so full ≡ retained holds, and
make `nearestAuthored`/`hitTest` consult the overlay group before in-flow. An empty overlay group yields a
scene byte-identical to the pre-overlay pass.

**Rationale**: Overprint (FR-004) needs paint-last-above-flow ordering; an overlay must escape the new
container clips (R1) or it would be clipped by its in-flow ancestor. Building on the existing `Overlay`
container keeps the surface minimal (one public entry). Mirroring in the retained path is required so the
overlay does not break the parity R1 establishes.

**Alternatives considered**:
- *Per-sample in-flow reservation* — rejected in 136 R4 (does not fix the framework; cannot represent true
  floating surfaces).
- *Hoist overlays by post-processing the flattened Scene* — rejected: the Scene IR has no overlay marker, so
  the hoist must happen during paint composition with Control-node knowledge (hence the renderTree/assemble
  split).

**Surface impact**: new/clarified public overlay-pass entry on `Control`; `.fsi` + surface baseline updated.

## R3 — ScrollViewer as a real clipping viewport (DECIDED)

**Decision**: `ScrollViewer` clips its content to its box (reusing the R1 container-clip model), exposes a
scroll offset, and renders a scroll affordance; content taller than the viewport is clipped (scrollable) not
spilled. Any viewport metric that must be read back is surfaced through `Layout`/`Control` `.fsi`. The sample
`Shell.fs` carries only compositional region sizing.

**Rationale**: Unbounded-content/no-scroll (FR-008) is a framework deficiency; once container clipping exists
(R1), the viewport is that clip plus an offset/affordance — a localized control/layout fix benefiting every
consumer.

**Alternatives considered**: clip only in the sample — rejected (leaves the framework defect for other
consumers).

## R4 — Re-baseline & verification (DECIDED)

**Decision**: Treat all renderer/control output changes as intended correctness fixes (FR-010). Re-establish
G1/G2 golden evidence, the rendered-output drift gate, and any surface-area baseline that gains surface
(overlay-pass entry; scroll metric). Commit one disclosed `rebaseline-ledger.md` row per changed baseline.
Re-capture all 19 showcase pages (both themes) via the feature-135 harness and confirm zero instances of the
seven defect classes; where no display exists, record a disclosed no-GL degrade — never a fabricated pass.

**Rationale**: A Tier-1 renderer change moves golden/drift baselines; the constitution requires the change be
intended, re-baselined, and disclosed. The 19-page re-capture is the verification vehicle for exactly the
layout/overlay/scroll classes this feature lands (deferred in 136 precisely because those classes were not yet
fixed).

**Re-capture command** (feature-135 harness):
```bash
cd samples/AntShowcase
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
```

**Alternatives considered**: a new evidence mechanism — rejected (reuse the established deterministic 135
harness, per the spec assumption).
