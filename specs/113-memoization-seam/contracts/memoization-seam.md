# Contract — Memoization Seam (DataGrid) (Feature 113)

The **internal** seam the suites pin (reached via `InternalsVisibleTo`), plus the additive public
`FrameMetrics` fields and the advisory `Diagnostics.stabilityReport`. Signatures from `RetainedRender.fsi` /
`ControlsElmish.fsi`; behaviour clauses are what the four `Feature113*Tests` suites assert.

## C1 — `memoize` (the seam) [contract C1–C4]

```fsharp
val internal memoize:
    id: ControlId ->
    dependency: obj ->
    compute: (unit -> FS.GG.UI.Scene.Scene list) ->
    cache: MemoCache ->
        FS.GG.UI.Scene.Scene list * MemoCache * MemoOutcome
```

- **C1 (Hit)**: same id + structurally-equal dependency + resident ⇒ reuse the **same** stored `Subtree`
  instance; `compute` is **not** run.
- **C2 (Miss)**: cold or changed dependency ⇒ run `compute` once and store the result.
- **C3**: the store is keyed by `id` + dependency (per-`ControlId`).
- **FR-005**: equality is structural `=`, never object identity.
- Pure / total / deterministic.

*Pins*: FR-001, FR-003, FR-004, FR-005. *Used by*: US1.

## C2 — `MemoEnabled` (the always-miss / parity oracle) [C5/C6]

```fsharp
// field on the internal RetainedRender record:
MemoEnabled: bool
```

- `true` on the live path; `false` ⇒ memo-on ≡ memo-off byte-identical scene (C5/SC-002).
- *(E2 finding: the disabled path bypasses `memoize` — counters 0/0 — see spec; the doc-comment overstates.)*

*Pins*: FR-006, FR-007, FR-008. *Used by*: US2.

## C3 — `MemoHits` / `MemoMisses` → public `FrameMetrics.MemoHitCount` / `MemoMissCount` [C7/C8]

```fsharp
// internal counters on WorkReductionRecord, surfaced as additive public FrameMetrics fields:
MemoHitCount: int
MemoMissCount: int
```

- Steady-state unchanged data ⇒ `MemoHitCount > 0`, `MemoMissCount = 0` (SC-004).
- Perturbed inputs ⇒ `MemoMissCount > 0`; idle / no-memoizable-control ⇒ `0/0` (C8).

*Pins*: FR-009, FR-010. *Used by*: US3.

## C4 — `Diagnostics.stabilityReport` (advisory)

- Flags `UnstableReuseInput` for a per-frame event closure / always-new value / unstable key (naming the
  node); a structurally-equal rebuild reports **no** findings.

*Pins*: FR-011, FR-012. *Used by*: US4.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-013): the seam (`MemoOutcome`/`MemoEntry`/`MemoCache`/`memoize`/
  `MemoEnabled`/`MemoHits`/`MemoMisses`) is `internal`; `MemoHitCount`/`MemoMissCount` are additive on the
  already-baselined public `FrameMetrics`; `stabilityReport` rides the already-baselined `Diagnostics`.
  `FS.GG.UI.Controls.txt` / `FS.GG.UI.Controls.Elmish.txt` stay byte-unchanged.
</content>
