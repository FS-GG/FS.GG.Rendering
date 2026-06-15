# Phase 1 — Data Model: Memoization Seam (DataGrid) (Feature 113)

The 113-in-scope entities. The seam is **assembly-internal**; the public touch is the additive
`FrameMetrics.MemoHitCount`/`MemoMissCount` on the already-baselined public `FrameMetrics`. `memoize` is
pure/total/deterministic; equality is structural.

## MemoOutcome (internal)

`Hit | Miss` — what a single `memoize` call resolved to. Aggregated per frame into the two memo counts.

## MemoEntry (internal)

`{ Dependency: obj; Subtree: FS.GG.UI.Scene.Scene list }` — the stored dependency + reusable subtree. A `Hit`
returns the **same `Subtree` instance** stored last frame (FR-004). Comparison is structural `=` on
`Dependency`, never object identity (FR-005).

## MemoCache (internal)

`Map<ControlId, MemoEntry>` — the per-frame store keyed by stable `ControlId`; an absent key is a cold miss.
Carried frame-to-frame on the retained record (FR-003).

## memoize (internal)

`id: ControlId -> dependency: obj -> compute: (unit -> Scene list) -> cache: MemoCache -> Scene list * MemoCache * MemoOutcome`.
Hit ⇒ reuse instance, `compute` not run; cold/changed ⇒ Miss, run `compute` once, store. The sole memoized
site is a childless `data-grid` leaf; dependency = `(theme, evaluated box, dataGridCells)`.

## MemoEnabled (internal)

The always-miss / parity oracle switch on the retained record. `true` on the live path; a parity test flips it
`false` to prove memo-on ≡ memo-off byte-identical. *(E2 finding: the disabled path bypasses `memoize`, so the
counters stay 0/0 — see spec/plan.)*

## MemoHits / MemoMisses → FrameMetrics.MemoHitCount / MemoMissCount

Internal per-frame counters on `WorkReductionRecord`, surfaced as the **public** additive `FrameMetrics`
fields. Both 0 on a frame with no memoizable control (FR-009/FR-010).

## Diagnostics.stabilityReport (advisory)

Flags reuse-breaking inputs as `UnstableReuseInput` (naming the control id/kind/message): per-frame event
closures, always-new attribute values, unstable keys. A structurally-equal rebuild reports no findings
(FR-011/FR-012).

## Relationships

```text
DataGrid leaf (data-grid, no children) ──build──▶ dependency = (theme, box, dataGridCells)
                                                       │
        memoize(id, dependency, compute, MemoCache) ──┼─ equal dep + resident ─▶ Hit  (reuse same Subtree, no compute) ─▶ MemoHits++
                                                       └─ cold/changed dep      ─▶ Miss (compute once, store)          ─▶ MemoMisses++
                                                       │
                       MemoEnabled=false ─▶ bypass (scene identical; counters 0/0)   ⇒ memo-on ≡ memo-off (SC-002)
                                                       │
                       MemoHits/MemoMisses ─▶ public FrameMetrics.MemoHitCount / MemoMissCount
   Diagnostics.stabilityReport(tree) ─▶ UnstableReuseInput findings (advisory; none on a structurally-equal rebuild)
```
</content>
