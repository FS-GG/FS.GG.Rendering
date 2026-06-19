# Runtime Diagnostics Summary

- run id: `feature169-synthetic-fixture`
- status: `blocked`
- severity counts: `informational=1 warning=3 error=1`
- category counts: `environment=1 backend-cost=1 rendering-limitation=1 readiness-blocker=1 developer-action=1`
- blockers: `1`
- review required: `1`
- unclassified: `0`
- accepted exceptions: `0`

## Artifacts
- `specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-fixture-summary.json`
- `specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-fixture-summary.md`

## Groups

| Source | Code | Severity | Category | Count | Message | Action |
|---|---|---|---|---:|---|---|
| `FS.GG.UI.Diagnostics.Tests/validation-lanes/diagnostics/feature169` | `PackageRestoreFailed` | `error` | `readiness-blocker` | 1 | Package proof did not restore the current local package. | Refresh the local feed and rerun package validation. |
| `FS.GG.UI.Diagnostics.Tests/package-feed/diagnostics/feature169` | `StalePackagePin` | `warning` | `developer-action` | 1 | Sample package pin does not match the current local feed package. | Run scripts/refresh-local-feed-and-samples.fsx before accepting readiness. |
| `FS.GG.UI.Diagnostics.Tests/renderer/diagnostics/feature169` | `FontFallback` | `warning` | `rendering-limitation` | 1 | Requested font family used a bundled substitute. | Review only if text evidence requires that exact platform font. |
| `FS.GG.UI.Diagnostics.Tests/stderr/diagnostics/feature169` | `HeadlessHost` | `warning` | `environment` | 1 | DISPLAY is unavailable; live screenshot evidence is environment limited. | Accept the environment limitation only for headless validation lanes. |
| `FS.GG.UI.Diagnostics.Tests/opengl-host/diagnostics/feature169` | `DamageScopedDecision` | `informational` | `backend-cost` | 1 | Damage-scoped redraw used an offscreen fallback. | No action required unless this appears in a performance-blocked lane. |
