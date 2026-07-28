# Contract: Skill Parity CLI

## Entry Point

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx [options]
```

The script forwards to:

```sh
dotnet run --project tools/Rendering.Harness/Rendering.Harness.fsproj -- skill-parity [options]
```

## Options

| Option | Meaning |
|--------|---------|
| `--repo <path>` | Repository root. Defaults to the current working directory. |
| `--out <dir>` | Output directory for generated readiness evidence. |
| `--report <path>` | Markdown report path. Defaults to `docs/reports/skills-parity.md`. |
| `--summary-json <path>` | Structured summary path. Defaults to `<out>/skill-parity-summary.json`. |
| `--fixture <name>` | Run a controlled fixture case instead of the repository inventory. Use `all` to run every required fixture. |
| `--surface <id=path>` | **Replace** the checked surface set with the surfaces named here. Repeatable. The run inspects these and no others. For fixtures and advanced local checks. |
| `--allow-exception <id>` | Allow an intentional exception id while keeping it visible in the report. Repeatable. |
| `--fail-on <severity>` | Lowest unresolved severity that returns exit code `1`. Defaults to `high`. |
| `--list-symbols` | Print every API symbol the skills document, with its status and skill, and exit without writing reports. |
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

## `--surface` Replaces, and Says So

`--surface <id>=<path>` **replaces** the surface set for that run. Supplying one
override does not add a seventh surface to the six above: it makes the run check
that one surface and nothing else. An overridden surface is always treated as
required, and is inventoried with every `SKILL.md` beneath its declared root
(no id-specific narrowing is inherited, because an override is a fresh
declaration rather than a patch to an existing one).

Because a narrowed run can still print `passed` and exit `0`, every run states
how much of the declared world it covered, on three channels:

- the `surfaces: <checked> checked of <declared> declared` operator line, which
  additionally says `NARROWED by --surface` when the set was replaced;
- `surfacesChecked` and `surfacesDeclared` in the `--json` object;
- a caveat in the generated report and the JSON summary naming the count and
  every baseline surface id the run did **not** check.

The report's regenerate line repeats the `--surface` arguments, so the command
it publishes reproduces the run it describes rather than a wider one.

### A malformed `--surface` value is refused

A `--surface` argument must be `<id>=<path>` with a **non-empty id** and a
**non-empty path**. Anything else is a surface configuration error: the CLI
names the offending value on stderr and exits `2` **before** any surface is
checked, so nothing is regenerated — a run that refused its arguments never
rewrites `docs/reports/skills-parity.md`.

These are errors, not narrowings:

- `--surface totally-malformed` — no `=`, so no path was named;
- `--surface =path` — an empty id;
- `--surface id=` — an empty path. An empty root resolves to the repository
  root, so this would inventory the **whole tree** under one operator id while
  reporting itself as a narrowed run. Both empty halves are errors, and so is a
  whitespace-only one;
- `--surface` with no value at all.

Silently dropping such a value would run the **full** repository check and print
`passed` with exit `0`, with none of the narrowing notices above firing — the
run the operator asked for would not have happened, and nothing would say so.

### With `--fixture`

`--fixture` and `--surface` combine with one defined meaning: `--fixture`
materializes the synthetic tree and re-roots the run at it, then `--surface`
replaces the **fixture** surface set. Override roots are therefore resolved
beneath the fixture root, and the fixture set — not the repository's six
surfaces — is the baseline the caveat and the `declared` count report.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Report generated and no unresolved finding meets `--fail-on`. |
| `1` | Report generated and at least one unresolved finding meets `--fail-on`. |
| `2` | Request, fixture, or surface configuration error. |
| `3` | Infrastructure error reading required surfaces or writing reports. |

## Operator Output

The CLI prints, one line each:

- `skill-parity status:` — overall status
- `root:` — the repository root that was checked (the fixture tree under `--fixture`)
- `surfaces:` — how many surfaces were checked, of how many declared
- `report:` — report path
- `summary-json:` — summary JSON path
- `findings:` — unresolved finding counts by severity

With `--json`, stdout is a single JSON object:

```json
{
  "summaryJson": "specs/168-skill-parity-evidence/readiness/skill-parity-summary.json",
  "report": "docs/reports/skills-parity.md",
  "overallStatus": "failed",
  "critical": 0,
  "high": 1,
  "warning": 3,
  "info": 2,
  "surfacesChecked": 6,
  "surfacesDeclared": 6
}
```

Diagnostics go to stderr, including the narrowing notice for
`--list-symbols --surface`, whose stdout is a tab-separated table.

## Fixture Cases

The required fixture set includes:

- `missing-wrapper`
- `wrapper-only`
- `stale-description`
- `broken-target`
- `canonical-drift`
- `unresolved-api-symbol`
- `unexercised-api-symbol`
- `passing`

Each fixture writes a synthetic surface baseline and test corpus alongside the
synthetic skills, so the API-symbol layer has both of its inputs.

Each fixture result names the expected finding category and the actual finding
ids produced by the checker.
