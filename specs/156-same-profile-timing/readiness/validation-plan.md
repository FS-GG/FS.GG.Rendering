# Feature 156 Validation Plan

## Evidence Expectations

- Same-profile timing must bind to accepted Feature 155 host profile `probe-08a47c01`.
- The timing policy is `same-profile-live-threshold-v2` with noise band `max(0.25 ms, 5% of full-redraw p50)`.
- Required scenarios are `timing/localized-update`, `timing/no-change`, `timing/movement-old-new`, `timing/overlap`, and `timing/edge-clipping`.
- Each scenario must record warmup count, measured repetitions, p50, p95, p99, verdict, confidence decision, artifact paths, and rejection reasons when not positive.

## Fail-Closed Rules

- Cross-profile, incomplete, noisy, non-beneficial, limited, environment-limited, unreadable, stale, duplicated, or mixed-run evidence cannot support a positive timing decision.
- Proof readback or validation readback overhead is disclosed as limited and cannot support the shipped performance claim.
- Synthetic fixtures are allowed only for rejection policy coverage and must be named or commented with `SYNTHETIC`.

## Command Matrix

- `dotnet build FS.GG.Rendering.slnx --no-restore`
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature156 --no-build`
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature156 --no-build`
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature156 --no-build`
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature156 --no-build`
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --profile probe-08a47c01 --policy same-profile-live-threshold-v2 --warmup 3 --repetitions 5 --out specs/156-same-profile-timing/readiness/timing --json`
- `env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --out specs/156-same-profile-timing/readiness/timing/unsupported --warmup 1 --repetitions 1`
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 156 --out specs/156-same-profile-timing/readiness`
- `dotnet test FS.GG.Rendering.slnx --no-restore`
- `dotnet fsi scripts/refresh-surface-baselines.fsx`
- `git diff --check`

## Review Outcome

- Correctness acceptance remains Feature 155 based.
- Feature 156 records same-profile timing evidence only.
- Shipped P7 performance status remains `performance-not-accepted` until Features 157, 158, 159, and 161 pass.
- Feature 160 is a validation-throughput follow-up, not a shipped performance acceptance gate.
