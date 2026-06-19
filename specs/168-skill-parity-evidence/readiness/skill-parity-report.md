<!-- SKILL-PARITY:START -->
# Skill Parity Report

Checked at UTC: `2026-06-19T16:14:24.9538632Z`
Overall status: `passed`
Canonical sources: `17`
Wrappers: `35`

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
| package-pin-drift | 9 | 0 | 0 | 0 | 40 |
| readiness-allowlisting | 6 | 0 | 0 | 0 | 43 |
| validation-output-isolation | 4 | 0 | 0 | 0 | 45 |
| visual-readiness | 11 | 0 | 0 | 0 | 38 |
| responsiveness-diagnostics | 7 | 0 | 0 | 0 | 42 |
| post-merge-package-bump | 2 | 0 | 0 | 0 | 47 |
| evidence-honesty | 12 | 0 | 0 | 0 | 37 |

## Findings
No unresolved parity findings.

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
