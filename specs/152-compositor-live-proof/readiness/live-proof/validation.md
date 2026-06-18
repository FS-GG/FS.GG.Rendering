# Feature 152 Live Proof Validation

Command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 152 --out specs/152-compositor-live-proof/readiness/live-proof
```

Current result: `environment-limited`.

The deterministic harness can write the Feature 152 proof summary and limitation record, but the current environment has not produced three fresh matching capable-host sentinel/damage readback attempts. No partial-redraw acceptance is recorded.

