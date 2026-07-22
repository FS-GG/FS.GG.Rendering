<!-- SKILL-PARITY:START -->
# Skill Parity Report

Overall status: `passed`
Canonical sources: `28`
Wrappers: `57`

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

## API Symbol Coverage
| Skill | Documented | Exercised | Unexercised | Unresolved |
| --- | --- | --- | --- | --- |
| fs-gg-diagnostics | 2 | 2 | 0 | 0 |
| fs-gg-elmish | 23 | 23 | 0 | 0 |
| fs-gg-generated-controls-guidance | 25 | 25 | 0 | 0 |
| fs-gg-keyboard-input | 7 | 7 | 0 | 0 |
| fs-gg-layout | 3 | 3 | 0 | 0 |
| fs-gg-samples | 2 | 2 | 0 | 0 |
| fs-gg-scene | 27 | 27 | 0 | 0 |
| fs-gg-skiaviewer | 6 | 6 | 0 | 0 |
| fs-gg-styling | 17 | 17 | 0 | 0 |
| fs-gg-symbol-design | 9 | 9 | 0 | 0 |
| fs-gg-symbology | 20 | 20 | 0 | 0 |
| fs-gg-testing | 12 | 12 | 0 | 0 |
| fs-gg-ui-widgets | 44 | 44 | 0 | 0 |

## Guarded Theme Coverage
| Theme | Scoped | Resolved | Dangling | Unnamed |
| --- | --- | --- | --- | --- |
| package-pin-drift | 8 | 8 | 0 | 0 |
| post-merge-package-bump | 2 | 2 | 0 | 0 |
| readiness-allowlisting | 5 | 5 | 0 | 0 |

## Findings
No unresolved parity findings.

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.
- 4 skill(s) show F# examples that name no public API symbol, so none was judged: fs-gg-collision, fs-gg-grids, fs-gg-line-drawing, fs-gg-visibility.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out artifacts/skill-parity --report docs/reports/skills-parity.md --summary-json artifacts/skill-parity/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
