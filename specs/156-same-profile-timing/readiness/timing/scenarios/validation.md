# Feature 156 Scenario Validation

## Same-Profile Run

- Command: `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --profile probe-08a47c01 --policy same-profile-live-threshold-v2 --warmup 3 --repetitions 5 --out specs/156-same-profile-timing/readiness/timing --json`
- Output: `specs/156-same-profile-timing/readiness/timing/summary.md`
- Run identity: `feature156-20260618130404`
- Measured host profile: `probe-08a47c01`
- Timing verdict: `noisy`
- Shipped performance claim: `performance-not-accepted`

## Required Scenario Evidence

- `timing/localized-update`: full-redraw and damage-scoped samples recorded; verdict `noisy`; rejection reason `p50 or p95 difference is inside the declared noise band`.
- `timing/no-change`: full-redraw and damage-scoped samples recorded; verdict `noisy`; rejection reason `p50 or p95 difference is inside the declared noise band`.
- `timing/movement-old-new`: full-redraw and damage-scoped samples recorded; verdict `noisy`; rejection reason `p50 or p95 difference is inside the declared noise band`.
- `timing/overlap`: full-redraw and damage-scoped samples recorded; verdict `noisy`; rejection reason `p50 or p95 difference is inside the declared noise band`.
- `timing/edge-clipping`: full-redraw and damage-scoped samples recorded; verdict `noisy`; rejection reason `p50 or p95 difference is inside the declared noise band`.

## Focused Test Status

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature156 --no-build`: passed, 7 tests.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature156 --no-build`: passed, 3 tests.
- Final regression status is recorded in `../../regression-validation.md`.
