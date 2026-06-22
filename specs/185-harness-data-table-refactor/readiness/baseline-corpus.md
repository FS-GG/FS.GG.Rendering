# Baseline Corpus — Harness Data-Table Refactor (185)

The pre-refactor ground truth for every per-story semantic diff (T004, FR-008). Captured by a **live
harness run** before any production edit.

## Capture

```bash
dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release   # green (T001)
scripts/emit-harness-readiness.sh /tmp/185-baseline                         # 12 features
```

`scripts/emit-harness-readiness.sh` drives `compositor-readiness --feature <N> --out <dir>` for every
catalog feature (148,149,152,153,154,155,156,157,158,159,160,161 — 150/151 absent). The legacy
handler serves 148–155; dedicated handlers serve 156–161.

## Result

- **12 features** emitted, all `exit=0`.
- **160 artifact files** captured (markdown + JSON + fsi authoring logs + surface-baseline snapshots).
- Per-feature artifact spread ranges from 2 files (148/149: validation-summary + ledger) to 30+
  files (159 promotion attempts/fallbacks/parity, 161 lane-ledger entries).

## Semantic-equivalence tooling (T007)

`scripts/semantic-diff-artifacts.fsx <baseline> <candidate>` normalizes embedded timestamps/run-ids
(both in content and in timestamp-bearing **filenames** such as
`feature160-<TS>-001.md`, `entry-feature161-readiness-<TS>.md`) and the absolute `--out` root path,
then compares the normalized path-set and per-file content.

**Validation:** a second independent re-emit (`/tmp/185-check`) semantic-diffs **clean** against
`/tmp/185-baseline` (`problems=0`), proving the normalizer absorbs run-to-run variance so any future
non-zero result is a real divergence.

## Test red/green baseline (T002, SC-004)

`dotnet fsi scripts/baseline-tests.fsx --config Release` → `readiness/baseline.md`. Full red/green set:

- **Green:** Controls, Diagnostics, Elmish, KeyboardInput, Layout, Lib, **Rendering.Harness (209/209)**,
  Scene, SkiaViewer, Smoke, Testing, AntShowcase, SampleApps, SecondAntShowcase.
- **Known pre-existing reds (NOT regressions — carried from 182/183 stale local feed):**
  - `tests/Package.Tests` — 8 failed / 101 passed
  - `samples/ControlsGallery/ControlsGallery.Tests` — 2 failed / 32 passed

The refactor must keep this exact red/green set (no new reds; no pre-existing red masked).
