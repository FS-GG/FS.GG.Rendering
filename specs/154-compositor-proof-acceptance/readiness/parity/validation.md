# Feature 154 Parity Validation

Status: `fallback-gated`

The ten required same-profile parity scenarios are listed in `README.md`. No accepted proof host profile exists in this checkout, so parity cannot unlock partial redraw.

Command evidence:

- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-parity --feature 154 --out specs/154-compositor-proof-acceptance/readiness/parity` completed with exit code `0`.
