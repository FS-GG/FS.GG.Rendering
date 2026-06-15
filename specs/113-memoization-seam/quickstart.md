# Quickstart — Validating Feature 113 (Memoization Seam / DataGrid)

Conformance backfill: code + tests exist. Validation = build green + the 113 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build (Release, zero warnings)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 113 suites (headless)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "113"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj   -c Release --filter "113"
```

Expected green:
- `Feature113MemoSeamTests` — US1: cold Miss (thunk once), stable Hit (same instance, no thunk), changed Miss,
  structural-equality Hit, per-`ControlId` cold miss (FR-001/004/005; C1–C3).
- `Feature113MemoParityTests` — US2: memo-on ≡ memo-off byte-identical (C5/SC-002); real Hit on unchanged data;
  no staleness on changed inputs (C6/FR-007).
- `Feature113MemoMetricsTests` — US3: steady-state `MemoHitCount > 0`/`MemoMissCount = 0` (SC-004); perturbed
  misses; idle/no-memoizable 0/0 (C7/C8).
- `Feature113StabilityDiagTests` — US4: stable rebuild → no findings (FR-012); per-frame closure / always-new
  value / unstable key → `UnstableReuseInput` (FR-011).

## 3. Author the readiness evidence

113 imported without `readiness/`. Author `specs/113-memoization-seam/readiness/`: `us1-hit-miss.md`,
`us2-parity-no-staleness.md` (SC-002), `us3-metrics.md` (SC-004), `us4-stability-diagnostic.md`. Gitignored
(`specs/*/readiness/`) — transient.

## 4. Confirm zero new public-surface delta (FR-013)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

Seam internal; `MemoHitCount`/`MemoMissCount` additive on the already-baselined public `FrameMetrics`.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips unrelated to 113
```

## Note — recorded finding (E2)

The `MemoEnabled` doc-comment overstates the disabled path (it is a 0/0 bypass, not "every node a miss"). This
is recorded and routed to Workstream E2; it is **not** fixed in this doc-only backfill.

## Success = the C6 conformance bar

Build green; the four 113 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. No pixel/desktop claim — proofs are Hit/Miss + scene byte-equality + metric/diagnostic assertions.
</content>
