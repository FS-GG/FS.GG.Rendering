# Feature 157 Unsupported Host

Status: `environment-limited`
Accepted partial-redraw artifacts: `0`
Reason: `missing display`

Validation command: `env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-damage --feature 157 --out specs/157-no-clear-damage-scissor/readiness/damage/unsupported-run`

Elapsed time: `10s`
Under-2-minute requirement: `passed`

Unsupported or unavailable presentation environments cannot accept damage-scoped no-clear artifacts.
