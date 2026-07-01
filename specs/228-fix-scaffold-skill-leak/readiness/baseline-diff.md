# T018 — Full-baseline diff (before T002 vs after)

| Run | Projects | Green | Red |
|---|---|---|---|
| [baseline.md](./baseline.md) (T002, before) | 21 | 4 | 17 |
| [baseline-after.md](./baseline-after.md) (T018, after) | 21 | **21** | **0** |

## Interpretation (Constitution V: no assertion weakened to green a build)

The 17 "before" reds were **all** the pre-existing `NU1403` FSharp.Core.10.1.301 lock-file hash mismatch
(an environment/cache condition, not a test-logic failure) — see the env caveat in
[non-goals-held.md](./non-goals-held.md). Every one reported "build/restore failure", none reported a
failing assertion. After `dotnet restore --force-evaluate` cleared the hash mismatch (environment-only
lock churn, reverted before commit), all 21 projects — including `Package.Tests` with the **corrected**
Feature 204/219 gates and every sample — are green.

**No new reds** were introduced. The only test-logic change (Feature 204/219 surface-specific gating)
passes in the full suite. The 4 sample projects that were green in both runs stayed green (88 + 34 + 25 +
171 tests). The fix is a strict improvement: the gates are made more precise, not looser.
