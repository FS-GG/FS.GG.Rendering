# Feature 152 Timing Validation

Command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 152 --tier damage --out specs/152-compositor-live-proof/readiness/timing
```

Current result: `environment-limited`.

The generated report records the required timing policy. It does not accept a performance claim because the current environment lacks same-profile capable-host proof, parity, and repeated timing artifacts.

