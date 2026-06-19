# Repository Parity Output Guide

Run repository parity with:

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx \
  --out specs/168-skill-parity-evidence/readiness/parity \
  --report docs/reports/skills-parity.md \
  --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json \
  --fail-on high \
  --json
```

Expected durable outputs:

- `docs/reports/skills-parity.md`: reviewer-facing generated report.
- `specs/168-skill-parity-evidence/readiness/skill-parity-summary.json`:
  structured status, counts, coverage, findings, and caveats.
- `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`: guidance
  matrix and list-rules proof.

The checker is report-only. It does not repair wrappers or rewrite canonical
skills.
