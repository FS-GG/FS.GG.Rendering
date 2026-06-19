# Feature 169 Sample Output

## JSON Mode

Command:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample --json
```

Result: exit 0. The command wrote:

- `specs/169-runtime-diagnostics-taxonomy/readiness/sample/diagnostics-summary.json`
- `specs/169-runtime-diagnostics-taxonomy/readiness/sample/diagnostics-summary.md`
- `specs/169-runtime-diagnostics-taxonomy/readiness/sample/diagnostics-records.jsonl`

Summary status: `accepted`; severity counts `informational=1 warning=1`; category counts `environment=1 backend-cost=1`; blockers `0`.

## Verbose Console Mode

Command:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose --verbose
```

Result: exit 0.

```text
Diagnostics: accepted
Severity: informational=1 warning=1
Category: environment=1 backend-cost=1
Blockers: 0 (first: none)
Review required: 0
Artifacts: specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-summary.json specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-summary.md specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose/diagnostics-records.jsonl
- environment/warning HeadlessEnvironment x1: The diagnostics command can run without opening a live viewer window. (Use live visual-readiness commands when screenshot proof is required.)
- backend-cost/informational DamageScopedDecision x1: Damage-scoped redraw can use an offscreen fallback for deterministic evidence. (No action required unless a performance lane marks this scenario blocked.)
```
