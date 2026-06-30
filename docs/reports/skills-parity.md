<!-- SKILL-PARITY:START -->
# Skill Parity Report

Checked at UTC: `2026-06-30T22:21:20.0316820Z`
Overall status: `passed`
Canonical sources: `22`
Wrappers: `45`

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
| package-pin-drift | 8 | 0 | 0 | 0 | 46 |
| readiness-allowlisting | 5 | 0 | 0 | 0 | 49 |
| validation-output-isolation | 4 | 0 | 0 | 0 | 50 |
| visual-readiness | 11 | 0 | 0 | 0 | 43 |
| responsiveness-diagnostics | 7 | 0 | 0 | 0 | 47 |
| post-merge-package-bump | 2 | 0 | 0 | 0 | 52 |
| evidence-honesty | 12 | 0 | 0 | 0 | 42 |

## Findings
No unresolved parity findings.

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out /home/developer/projects/FS.GG.Rendering/artifacts/skill-parity --report /home/developer/projects/FS.GG.Rendering/docs/reports/skills-parity.md --summary-json /home/developer/projects/FS.GG.Rendering/artifacts/skill-parity/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
