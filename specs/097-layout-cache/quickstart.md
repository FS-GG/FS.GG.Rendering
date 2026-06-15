# Quickstart — Validating Feature 097 (Layout Cache / Incremental Re-Measure)

This is a **conformance backfill**: the code and tests already exist. Validation = build green + the three
097 suites green + zero new public-surface-baseline delta + readiness regenerated.

## Prerequisites

- .NET `net10.0` SDK; repo restored. No GL context required (097's proofs are deterministic/headless:
  bounds-map equality, structural scene equality, re-measure-count invariants).

## 1. Build (Release, zero warnings)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

Expected: 0 warnings, 0 errors.

## 2. Run the 097 suites

**Pure evaluator** (public `FS.GG.UI.Layout` package — equivalence, boundary subset, at-rest, honest
`Invalidated`):

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj -c Release --filter "097"
```

Expected: `Feature097IncrementalTests` green — incremental ≡ full over a fixed-size boundary (SC-001), the
≥1000-case FsCheck equivalence over generated `(tree, edit-sequence)` cases (SC-002), the empty-dirty-set
at-rest case (SC-006), and the partial-vs-full subset property (SC-001/SC-004). `Audit_IncrementalLayout`
green (audit cross-check).

> Note: the filter string is the feature number `"097"` (matched against test/list names). If a run reports
> 0 tests, the audit list may not carry the number in its name — fall back to running the whole project
> (`dotnet test tests/Layout.Tests/Layout.Tests.fsproj -c Release`) and confirm the Feature097 lists pass.

**Wired path** (internal wiring via `InternalsVisibleTo` — metric honesty, dirty-set precision, byte-identity
vs full rebuild):

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "097"
```

Expected: `Feature097WiringTests` green — a localized geometry edit re-measures a strict subset
(`0 < RemeasuredNodeCount < BaselineNodeCount`) and renders byte-identical to a full `Control.renderTree`
(SC-001/SC-003/SC-005); an at-rest frame re-measures nothing (SC-003); a content-only change re-measures
nothing yet stays byte-identical (SC-004); a whole-tree relayout re-measures the baseline (SC-003/FR-010); a
child insert dirties its container and stays byte-identical (FR-003).

## 3. Confirm the lock-step name-set guard still passes (097 depends on it)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "101"
```

Expected: `Feature101LayoutDriftGuardTests` green — `layoutAffectingAttrNames` (read by `layoutDirtySet`)
stays in lock-step with the names the layout lowering reads.

## 4. Confirm zero new public-surface-baseline delta (FR-011)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx   # regenerate, then confirm no diff
git diff --stat tests/surface-baselines/            # (when under git) MUST be empty
```

Expected: `tests/surface-baselines/FS.GG.UI.Layout.txt` and `FS.GG.UI.Controls.txt` unchanged — the
evaluator was already baselined; the wiring is internal.

> Conformance-pass note (carried from the 099 backfill): `refresh-surface-baselines.fsx` has no real
> `--check` mode (it always rewrites), so confirm "no delta" via the VCS diff after regenerating, not via a
> script exit code.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release
```

Expected: 0 failures (18 honest `ptest`/`ptestList` skips remain — perf-corpus + FSI fixture, unrelated to 097).

## What this validation does NOT prove

- No pixel-level or desktop-visibility claim — parity is **structural scene equality** + **bounds-map
  equality**, captured deterministically (consistent with the 091/092/099 backfills).
- 097 owns the **measure/bounds cache** only — not the paint-side partial repaint (091), the picture cache
  (116), the text-measure cache (117), or the name-set guard itself (101).

## Success = the C2 conformance bar

Build green; the three 097 suites green; the 101 guard green; zero new public-surface delta; readiness
evidence authored under `readiness/`; `/speckit-analyze` reports cross-artifact consistency.
</content>
