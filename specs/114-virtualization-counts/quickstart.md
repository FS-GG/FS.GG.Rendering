# Quickstart — Validating Feature 114 (Virtualization Counts & Overscan)

Conformance backfill: code + tests exist. Validation = build green + the 114 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 114 suites (headless, no GL)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "114"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj   -c Release --filter "114"
```

Expected green:
- `Feature114OverscanTests` — US1: bounded, non-scaling realization; `V + 2N` mid-list; edge-clamped; fitting
  grid realizes all; negative overscan clamps (SC-001; FR-002/003/004/007).
- `Feature114OverscanParityTests` — US2: overscan 0 byte-identical slice; opt-in real adjacent rows; keyed
  stability across a scroll (SC-002/SC-003; FR-006/007/008).
- `Feature114OffscreenTests` — US3: offscreen select/toggle/focus/relocate without materializing; boundary
  nav; visible-row dispatch byte-identical (SC-004/SC-005; FR-009/010/011/016).
- `Feature114AccessibilityTests` — US3: logical `TotalItems`/`FocusedIndex`; `Collection = None` for
  non-collections (FR-012).
- `Feature114VirtualMetricsTests` — US4: `materialized ≤ visible`, `total = RowCount`, non-scaling, 0/0 when
  none, aggregation (SC-006; FR-013/014).

## 3. Author the readiness evidence

114 imported without `readiness/`. Author `specs/114-virtualization-counts/readiness/`:
`us1-bounded-materialization.md` (SC-001), `us2-overscan-parity.md` (SC-002/SC-003),
`us3-offscreen-a11y.md` (SC-004/SC-005), `us4-virtual-metrics.md` (SC-006). Gitignored — transient.

## 4. Confirm zero new public-surface delta (FR-017)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

Counting carrier internal; all public touches are field/param additions on already-baselined types.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips unrelated to 114
```

## Success = the C7 conformance bar

Build green; the five 114 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. No pixel/desktop claim — proofs are counts, byte-identity, and logical-model assertions.
</content>
