# Feature 160 Focused Throughput Summary

Run identity: `feature160-20260618180244`
Final throughput status: `blocked`
Focused throughput status: `accepted`
Full validation status: `missing`
Release-ready status: `blocked`
Shipped compositor performance claim: `performance-not-accepted`
Lane id: `focused`
Policy id: `focused-throughput-v1`
Declared per-iteration bound minutes: `10`
Required accepted iterations: `3`
Accepted iterations: `3`
Excluded iterations: `0`
Unsupported-host reason: `none`
Accepted same-profile performance artifacts from unsupported-host validation: `0`
Host profile: `probe-08a47c01`
Warmup count: `3`
Measured repetitions: `5`

## Required Scenarios

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

## Iterations

| Iteration | Status | Duration minutes | Primary reason | Restricted scenario | Artifact |
|-----------|--------|------------------|----------------|---------------------|----------|
| `feature160-20260618180244-001` | `accepted` | `0.083` | `none` | `none` | `throughput/iterations/feature160-20260618180244-001.md` |
| `feature160-20260618180244-002` | `accepted` | `0.083` | `none` | `none` | `throughput/iterations/feature160-20260618180244-002.md` |
| `feature160-20260618180244-003` | `accepted` | `0.083` | `none` | `none` | `throughput/iterations/feature160-20260618180244-003.md` |

## Release Gate Separation

- Focused throughput collection does not run `dotnet test FS.GG.Rendering.slnx --no-restore`.
- Full validation is recorded separately under `full-validation/` and blocks release-ready status when missing, failing, interrupted, stale, or undocumented.
- Noisy same-profile timing remains a performance-claim gate; it is not a focused-throughput exclusion reason by itself.

## Artifact Links

- Iterations: `throughput/iterations/`
- Raw samples: `throughput/raw/`
- Excluded evidence: `throughput/excluded/`
- Unsupported-host evidence: `throughput/unsupported/README.md`
- Full validation: `full-validation/validation.md`

## Diagnostics

- focused throughput package assembled
- full validation remains separate
- performance-not-accepted preserved
