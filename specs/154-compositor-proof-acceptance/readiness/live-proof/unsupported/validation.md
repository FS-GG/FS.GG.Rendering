# Feature 154 Unsupported Host Validation

Status: `environment-limited`

Elapsed time: `0.6s`

Under 2 minutes: `yes`

Accepted partial-redraw artifacts: `0`

Result: `env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-live-proof --feature 154 --out specs/154-compositor-proof-acceptance/readiness/live-proof/unsupported` completed with exit code `0`. Unsupported-host evidence remains non-accepting and preserves full-redraw fallback.
