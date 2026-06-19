# Feature 168 Focused Test Evidence

## Focused Feature Tests

Command:

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168"
```

Result on 2026-06-19:

```text
Passed!  - Failed:     0, Passed:    13, Skipped:     0, Total:    13
```

Covered focused areas:

- required guidance rule catalog
- implementation/testing/visual/responsiveness coverage classification
- `Rendering.Harness.SkillParity.fsi` surface-baseline drift assertion
- fixture wrapper target resolution
- synthetic missing-wrapper, wrapper-only, stale-description, broken-target,
  canonical-drift, guidance-gap, and passing cases
- Markdown/JSON report agreement
- generated-section preservation for manual caveats
- non-destructive repository skill-file checking
- current repository parity has no unresolved findings

## Repository Parity Report Test

Command:

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high --json
```

Result:

```json
{"summaryJson":"specs/168-skill-parity-evidence/readiness/skill-parity-summary.json","report":"docs/reports/skills-parity.md","overallStatus":"passed","critical":0,"high":0,"warning":0,"info":0}
```

The current repository parity report is green. Wrapper-description drift,
generated-product wrapper aliases, and the `speckit-merge` visual-readiness
coverage gap are resolved in the active report.
