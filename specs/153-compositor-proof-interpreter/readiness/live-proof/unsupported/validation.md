# Feature 153 Unsupported Host Validation

Status: `environment-limited`

Command:

```text
env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 153 --out specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported
```

Recorded result:

- Verdict: `environment-limited`
- Accepted partial-redraw artifacts: `0`
- Under-2-minute target: `pass`
- Elapsed time: `1s`
- Exit code: `0`
- Output: `specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported/proof.md`
- Reason: current validation environment has no display and records no accepted live sentinel/damage readback proof.

This is a safe, non-accepting result.
