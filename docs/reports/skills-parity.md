<!-- SKILL-PARITY:START -->
# Skill Parity Report

Checked at UTC: `2026-07-05T12:42:56.3629936Z`
Overall status: `passed`
Canonical sources: `31`
Wrappers: `63`

## Supported Surfaces
| Surface | Kind | Agent | Root | Required |
| --- | --- | --- | --- | --- |
| codex-local | wrapper | codex | .agents/skills | True |
| claude | wrapper | claude | .claude/skills | True |
| package-canonical | canonical | package | src | True |
| template-canonical | canonical | generated-product | template | True |
| ant-canonical | canonical | repository | .claude/skills/fs-gg-ant-design/SKILL.md | True |
| spec-kit-command | command | spec-kit | .agents/skills/speckit-* and .claude/skills/speckit-* | True |

## Severity Counts
| Critical | High | Warning | Info |
| --- | --- | --- | --- |
| 0 | 0 | 0 | 0 |

## Guidance Coverage
| Rule | Covered | Partial | Missing | Excepted | Not applicable |
| --- | --- | --- | --- | --- | --- |
| package-pin-drift | 8 | 0 | 0 | 0 | 55 |
| readiness-allowlisting | 5 | 0 | 0 | 0 | 58 |
| validation-output-isolation | 4 | 0 | 0 | 0 | 59 |
| visual-readiness | 11 | 0 | 0 | 0 | 52 |
| responsiveness-diagnostics | 7 | 0 | 0 | 0 | 56 |
| post-merge-package-bump | 2 | 0 | 0 | 0 | 61 |
| evidence-honesty | 12 | 0 | 0 | 0 | 51 |

## Findings
No unresolved parity findings.

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out /home/developer/projects/fsgg-rendering-248/artifacts/skill-parity --report /home/developer/projects/fsgg-rendering-248/docs/reports/skills-parity.md --summary-json /home/developer/projects/fsgg-rendering-248/artifacts/skill-parity/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
