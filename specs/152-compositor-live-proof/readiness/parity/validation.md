# Feature 152 Damage Parity Validation

Command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 152 --out specs/152-compositor-live-proof/readiness/parity
```

Current result: `environment-limited`.

The report records same-profile parity requirements and fallback categories. It does not accept damage-scoped live output because the current environment does not have an accepted three-run capable-host proof set.

