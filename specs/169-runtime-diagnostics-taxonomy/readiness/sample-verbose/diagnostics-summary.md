# Runtime Diagnostics Summary

- run id: `antshowcase-diagnostics`
- status: `accepted`
- severity counts: `informational=1 warning=1`
- category counts: `environment=1 backend-cost=1`
- blockers: `0`
- review required: `0`
- unclassified: `0`
- accepted exceptions: `0`

## Artifacts
- `specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-summary.json`
- `specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-summary.md`
- `specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-records.jsonl`

## Groups

| Source | Code | Severity | Category | Count | Message | Action |
|---|---|---|---|---:|---|---|
| `AntShowcase/sample-cli/ant-showcase` | `HeadlessEnvironment` | `warning` | `environment` | 1 | The diagnostics command can run without opening a live viewer window. | Use live visual-readiness commands when screenshot proof is required. |
| `AntShowcase/opengl-host/ant-showcase` | `DamageScopedDecision` | `informational` | `backend-cost` | 1 | Damage-scoped redraw can use an offscreen fallback for deterministic evidence. | No action required unless a performance lane marks this scenario blocked. |
