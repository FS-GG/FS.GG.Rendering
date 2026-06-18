# Feature 156 Unsupported-Host Validation

## Command

`env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --out specs/156-same-profile-timing/readiness/timing/unsupported --warmup 1 --repetitions 1`

## Result

- Output: `specs/156-same-profile-timing/readiness/timing/unsupported/summary.md`
- Timing verdict: `environment-limited`
- Reason: `missing display`
- Accepted performance artifacts: `0`
- Completion time: under 2 minutes in local run.

## Safety Boundary

- Unsupported hosts produce reviewer-visible evidence and fail closed.
- Unsupported-host evidence does not weaken Feature 155 correctness acceptance or full-redraw fallback.
- Shipped P7 performance claim remains `performance-not-accepted`.
