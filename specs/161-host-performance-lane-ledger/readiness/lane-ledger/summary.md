# Feature 161 Host Performance Lane Ledger

Run identity: `feature161-readiness-20260618193407`
Status: `accepted`
Release-ready status: `ready`
Performance claim: `performance-not-accepted`
Policy id: `host-lane-ledger-v1`
Accepted lane id: `x11-:1-direct-opengl-amd-mesa`
Claim applies to: X11 `:1` with direct OpenGL on AMD Radeon/Mesa for profile `probe-08a47c01`
Accepted lane-scoped performance artifacts: `1`
Excluded lane entries: `0`
Unsupported-host reason: `none`
Full validation status: `passed`
Compatibility impact: `Feature161HostLaneReadiness helper added; runtime rendering behavior unchanged`
Package validation: `accepted-with-recorded-limitations`
Regression validation: `accepted-with-recorded-limitations`

## Non-Generalized Lanes

- Wayland
- indirect GL
- missing display
- software raster
- virtualized presentation
- unknown renderer
- stale package
- cross-profile timing

## Remaining Blockers

- none

## Ledger Entries

| Entry | Status | Primary reason | Accepted artifacts | Artifact |
|-------|--------|----------------|--------------------|----------|
| `feature161-readiness-20260618193407` | `accepted` | `none` | `1` | `lane-ledger/entries/entry-feature161-readiness-20260618193407.md` |

## Artifact Links

- Host facts: `lane-ledger/host-facts/`
- Entries: `lane-ledger/entries/`
- Excluded evidence: `lane-ledger/excluded/`
- Unsupported-host evidence: `lane-ledger/unsupported/README.md`
- Summary JSON: `lane-ledger/summary.json`

## Diagnostics

- readiness package assembled
- host lane facts preserve current-host scope
- performance-not-accepted preserved
- readiness-output=specs/161-host-performance-lane-ledger/readiness
