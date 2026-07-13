<!-- SKILL-PARITY:START -->
# Skill Parity Report

Overall status: `warning`
Canonical sources: `32`
Wrappers: `65`

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
| 0 | 0 | 2 | 0 |

## API Symbol Coverage
| Skill | Documented | Exercised | Unexercised | Unresolved |
| --- | --- | --- | --- | --- |
| fs-gg-audio | 6 | 6 | 0 | 0 |
| fs-gg-diagnostics | 2 | 2 | 0 | 0 |
| fs-gg-elmish | 18 | 17 | 1 | 0 |
| fs-gg-generated-controls-guidance | 25 | 25 | 0 | 0 |
| fs-gg-keyboard-input | 7 | 7 | 0 | 0 |
| fs-gg-layout | 3 | 3 | 0 | 0 |
| fs-gg-persistence | 5 | 5 | 0 | 0 |
| fs-gg-samples | 2 | 2 | 0 | 0 |
| fs-gg-scene | 14 | 14 | 0 | 0 |
| fs-gg-skiaviewer | 5 | 5 | 0 | 0 |
| fs-gg-styling | 17 | 17 | 0 | 0 |
| fs-gg-symbology | 20 | 20 | 0 | 0 |
| fs-gg-testing | 12 | 12 | 0 | 0 |
| fs-gg-ui-widgets | 44 | 43 | 1 | 0 |

## Guarded Theme Coverage
| Theme | Scoped | Resolved | Dangling | Unnamed |
| --- | --- | --- | --- | --- |
| package-pin-drift | 8 | 8 | 0 | 0 |
| post-merge-package-bump | 2 | 2 | 0 | 0 |
| readiness-allowlisting | 5 | 5 | 0 | 0 |

## Findings
| Skill | Surface | Category | Severity | Path | Message | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| fs-gg-elmish | template-canonical | unexercised-api-symbol | warning | template/product-skills/fs-gg-elmish/SKILL.md | Skill documents `ControlRuntime.diagnostics`, but no test calls it — the seam may be dead. | Add a test that calls the documented API, or stop documenting it. |
| fs-gg-ui-widgets | template-canonical | unexercised-api-symbol | warning | template/product-skills/fs-gg-ui-widgets/SKILL.md | Skill documents `Catalog.validate`, but no test calls it — the seam may be dead. | Add a test that calls the documented API, or stop documenting it. |

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.
- 6 skill(s) show F# examples that name no public API symbol, so none was judged: fs-gg-collision, fs-gg-game-core, fs-gg-grids, fs-gg-line-drawing, fs-gg-model-swap, fs-gg-visibility.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out artifacts/skill-parity --report docs/reports/skills-parity.md --summary-json artifacts/skill-parity/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
