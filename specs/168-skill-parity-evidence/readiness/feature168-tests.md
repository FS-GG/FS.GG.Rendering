# Feature 168 Focused Test Evidence

## Focused Feature Tests

Command:

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168"
```

Result on 2026-06-19:

```text
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12
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

## Repository Parity Report Test

Command:

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high --json
```

Result:

```json
{"summaryJson":"specs/168-skill-parity-evidence/readiness/skill-parity-summary.json","report":"docs/reports/skills-parity.md","overallStatus":"warning","critical":0,"high":0,"warning":35,"info":0}
```

The warning status is retained rather than hidden. It reflects existing
wrapper-description and wrapper-name drift plus one partial visual-readiness
warning for `speckit-merge`; no high or critical findings remain.
