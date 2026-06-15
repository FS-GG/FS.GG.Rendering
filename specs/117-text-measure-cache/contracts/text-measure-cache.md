# Contract — Text-Measure Cache (LRU) (Feature 117)

The **internal** cache seam (pinned via `InternalsVisibleTo`) + the three additive public `FrameMetrics`
fields. Signatures from `RetainedRender.fsi`; behaviour clauses are what the five suites (incl. `Audit_TextCache`)
assert.

## C1 — `measureTextCached` (the cache lookup, internal)

```fsharp
val internal measureTextCached:
    cache: TextMeasureCache -> enabled: bool -> text: string -> font: FS.GG.UI.Scene.FontSpec ->
        FS.GG.UI.Scene.TextMetrics * TextMeasureCache * bool
```

- Resident key ⇒ Hit (reuse metrics; no `Scene.measureText`); cold/changed/evicted ⇒ Miss (measure + store).
- A Hit's metrics are byte-identical to the un-cached measure (FR-001); any single differing keyed field
  Misses (FR-002).
- `enabled = false` ⇒ always re-measure (`wasHit` never true), proving cache-on ≡ cache-off (FR-004).
- Total / deterministic.

*Pins*: FR-001, FR-002, FR-004. *Used by*: US1.

## C2 — `TextMeasureKey` + `TextMeasureCache` + `TextMeasureCacheCap` (internal)

```fsharp
type internal TextMeasureKey   = { Text: string; Family: string option; Size: float; Weight: int option }
type internal TextMeasureCache = { Entries: Map<TextMeasureKey, int * FS.GG.UI.Scene.TextMetrics>; Clock: int }
val internal TextMeasureCacheCap: int   // = 256
```

- Bounded LRU: `Entries.Count ≤ cap` always; deterministic eviction; evicted key re-misses correctly.

*Pins*: FR-003. *Used by*: US1.

## C3 — Public metrics (`FrameMetrics`, additive)

```fsharp
TextMeasureCacheHitCount: int
TextMeasureCacheMissCount: int
LayoutInvalidatedNodeCount: int   // pre-pinning dirty-set size; <= RemeasuredNodeCount; 0 on style-only/idle
```

- Cold misses → warm hits + zero misses; style-only / idle ⇒ zero text misses + `LayoutInvalidatedNodeCount =
  0`; geometry ⇒ `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`; deterministic re-run.

*Pins*: FR-005, FR-006, FR-007, FR-010. *Used by*: US2, US3.

## C4 — feature-101 drift-guard (consumed)

- The geometry-driving name set `{ width; height; orientation }` MUST be unchanged (117 introduces no new
  geometry-driving attribute). Owned by feature 101; consumed here.

*Pins*: FR-008. *Used by*: US2.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-011): `TextMeasureKey`/`TextMeasureCache`/`measureTextCached`/
  `TextMeasureCacheCap`/`TextCache`/`TextCacheEnabled` + the `WorkReductionRecord` counters are `internal`; the
  three metric fields are additive on the already-baselined public `FrameMetrics` (type-granular baseline).
  `FS.GG.UI.Controls.txt` / `FS.GG.UI.Controls.Elmish.txt` stay byte-unchanged.
</content>
