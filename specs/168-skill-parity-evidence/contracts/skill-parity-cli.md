# Contract: Skill Parity CLI

## Entry Point

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx [options]
```

The script forwards to:

```sh
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- skill-parity [options]
```

## Options

| Option | Meaning |
|--------|---------|
| `--repo <path>` | Repository root. Defaults to the current working directory. |
| `--out <dir>` | Output directory for generated readiness evidence. |
| `--report <path>` | Markdown report path. Defaults to `docs/reports/skills-parity.md`. |
| `--summary-json <path>` | Structured summary path. Defaults to `<out>/skill-parity-summary.json`. |
| `--fixture <name>` | Run a controlled fixture case instead of the repository inventory. Use `all` to run every required fixture. |
| `--surface <id=path>` | Add or override a skill surface for fixture or advanced local checks. Repeatable. |
| `--allow-exception <id>` | Allow an intentional exception id while keeping it visible in the report. Repeatable. |
| `--fail-on <severity>` | Lowest unresolved severity that returns exit code `1`. Defaults to `high`. |
| `--list-rules` | Print required guidance rules and exit without writing reports. |
| `--json` | Print final structured summary path and status as JSON. |

No `--fix` or auto-update mode is part of the MVP.

## Default Repository Surfaces

When no `--surface` is supplied, the checker reads:

- canonical package skills under `src/*/skill/SKILL.md`
- canonical template and generated-product skills under `template/**/SKILL.md`
- the canonical Ant Design skill at `.claude/skills/fs-gg-ant-design/SKILL.md`
- Codex/local-agent wrappers under `.agents/skills/*/SKILL.md`
- Claude wrappers under `.claude/skills/*/SKILL.md`

Spec Kit command skills that exist as wrappers without package/template
canonical sources are reported as command-surface entries, not hidden.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Report generated and no unresolved finding meets `--fail-on`. |
| `1` | Report generated and at least one unresolved finding meets `--fail-on`. |
| `2` | Request, fixture, or surface configuration error. |
| `3` | Infrastructure error reading required surfaces or writing reports. |

## Operator Output

The CLI prints:

- checked repository root
- checked surfaces
- report path and summary JSON path
- overall status
- unresolved finding counts by severity
- guidance coverage counts by rule

With `--json`, stdout is a single JSON object:

```json
{
  "summaryJson": "specs/168-skill-parity-evidence/readiness/skill-parity-summary.json",
  "report": "docs/reports/skills-parity.md",
  "overallStatus": "failed",
  "critical": 0,
  "high": 1,
  "warning": 3,
  "info": 2
}
```

Diagnostics go to stderr.

## Fixture Cases

The required fixture set includes:

- `missing-wrapper`
- `wrapper-only`
- `stale-description`
- `broken-target`
- `canonical-drift`
- `guidance-gap`
- `passing`

Each fixture result names the expected finding category and the actual finding
ids produced by the checker.
