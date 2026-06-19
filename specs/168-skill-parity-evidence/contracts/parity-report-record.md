# Contract: Parity Report Record

## Files

Repository parity generation writes:

```text
docs/reports/skills-parity.md
specs/168-skill-parity-evidence/readiness/
|-- skill-parity-report.md
|-- skill-parity-summary.json
|-- guidance-coverage.md
|-- fixture-results.md
`-- validation-log.md
```

`docs/reports/skills-parity.md` is the durable reviewer entry point.
`skill-parity-summary.json` is the machine-readable record. The readiness report
may copy or link the durable report content, but it must not erase manual
caveats outside generated sections.

## Summary JSON Shape

```json
{
  "checkedAtUtc": "2026-06-19T12:34:56Z",
  "overallStatus": "failed",
  "repositoryRoot": "/home/developer/projects/FS.GG.Rendering",
  "surfaces": [
    {
      "surfaceId": "codex-local",
      "kind": "wrapper",
      "rootPath": ".agents/skills",
      "skillCount": 32,
      "required": true
    }
  ],
  "canonicalSourceCount": 18,
  "wrapperCount": 64,
  "findingCountsBySeverity": {
    "critical": 0,
    "high": 1,
    "warning": 3,
    "info": 2
  },
  "guidanceRuleCoverage": [
    {
      "ruleId": "package-pin-drift",
      "covered": 6,
      "partial": 0,
      "missing": 0,
      "excepted": 1,
      "notApplicable": 12
    }
  ],
  "findings": [
    {
      "findingId": "broken-target:codex-local:fs-gg-testing",
      "skillName": "fs-gg-testing",
      "surfaceId": "codex-local",
      "category": "broken-target",
      "severity": "high",
      "canonicalPath": null,
      "wrapperPath": ".agents/skills/fs-gg-testing/SKILL.md",
      "ruleId": null,
      "message": "Wrapper target does not resolve.",
      "remediation": "Update the wrapper target path or restore the canonical skill source.",
      "exceptionId": null
    }
  ],
  "caveats": [
    "Global Codex skill installation paths were excluded from required repository parity."
  ]
}
```

## Overall Status

| Status | Meaning |
|--------|---------|
| `passed` | No unresolved high or critical findings and all required guidance rules are covered or explicitly excepted. |
| `warning` | Only warning/info findings remain, or required guidance has partial but non-blocking coverage. |
| `failed` | At least one unresolved high/critical finding, unreadable required surface, broken target, or required guidance gap remains. |

## Markdown Requirements

The Markdown report includes:

- checked date
- overall status
- supported and excluded surfaces
- canonical source count
- wrapper count by agent surface
- finding counts by severity
- required guidance-rule coverage matrix
- unresolved findings table with skill name, surface, category, severity, path,
  and suggested next action
- intentional exceptions table
- caveats and evidence-honesty notes
- command used to regenerate the report

The first unresolved high-severity finding must be visible without reading
individual skill files.

## Non-Overwrite Rule

Generated sections use explicit markers when a file can contain manual notes:

```md
<!-- SKILL-PARITY:START -->
generated content
<!-- SKILL-PARITY:END -->
```

Manual caveats and reviewer notes outside the generated section are preserved.
