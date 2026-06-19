<!-- SKILL-PARITY:START -->
# Skill Parity Report

Checked at UTC: `2026-06-19T13:15:00.5413515Z`
Overall status: `warning`
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
| 0 | 0 | 35 | 0 |

## Guidance Coverage
| Rule | Covered | Partial | Missing | Excepted | Not applicable |
| --- | --- | --- | --- | --- | --- |
| package-pin-drift | 9 | 0 | 0 | 0 | 40 |
| readiness-allowlisting | 6 | 0 | 0 | 0 | 43 |
| validation-output-isolation | 4 | 0 | 0 | 0 | 45 |
| visual-readiness | 9 | 2 | 0 | 0 | 38 |
| responsiveness-diagnostics | 7 | 0 | 0 | 0 | 42 |
| post-merge-package-bump | 2 | 0 | 0 | 0 | 47 |
| evidence-honesty | 12 | 0 | 0 | 0 | 37 |

## Findings
| Skill | Surface | Category | Severity | Path | Message | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| fs-gg-elmish | codex-local | stale-description | warning | .agents/skills/fs-gg-elmish/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-ui-widgets | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-ui-widgets/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-samples | codex-local | stale-description | warning | .agents/skills/fs-gg-samples/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-keyboard-input | codex-local | stale-description | warning | .agents/skills/fs-gg-keyboard-input/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-ant-design | codex-local | stale-description | warning | .agents/skills/fs-gg-ant-design/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-scene | codex-local | stale-description | warning | .agents/skills/fs-gg-scene/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-scene | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-scene/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-product-skiaviewer | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-skiaviewer/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-project | codex-local | stale-description | warning | .agents/skills/fs-gg-project/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-testing | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-testing/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-testing | codex-local | stale-description | warning | .agents/skills/fs-gg-testing/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-layout | codex-local | stale-description | warning | .agents/skills/fs-gg-layout/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-keyboard-input | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-keyboard-input/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-ui-widgets | codex-local | stale-description | warning | .agents/skills/fs-gg-ui-widgets/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-skiaviewer | codex-local | stale-description | warning | .agents/skills/fs-gg-skiaviewer/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-elmish | codex-local | metadata-drift | warning | .agents/skills/fs-gg-product-elmish/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-feedback-capture | codex-local | stale-description | warning | .agents/skills/fs-gg-feedback-capture/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-elmish | claude | stale-description | warning | .claude/skills/fs-gg-elmish/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-ui-widgets | claude | metadata-drift | warning | .claude/skills/fs-gg-product-ui-widgets/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-samples | claude | stale-description | warning | .claude/skills/fs-gg-samples/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-keyboard-input | claude | stale-description | warning | .claude/skills/fs-gg-keyboard-input/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-scene | claude | stale-description | warning | .claude/skills/fs-gg-scene/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-scene | claude | metadata-drift | warning | .claude/skills/fs-gg-product-scene/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-product-skiaviewer | claude | metadata-drift | warning | .claude/skills/fs-gg-product-skiaviewer/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-project | claude | stale-description | warning | .claude/skills/fs-gg-project/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-testing | claude | metadata-drift | warning | .claude/skills/fs-gg-product-testing/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-testing | claude | stale-description | warning | .claude/skills/fs-gg-testing/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-layout | claude | stale-description | warning | .claude/skills/fs-gg-layout/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-keyboard-input | claude | metadata-drift | warning | .claude/skills/fs-gg-product-keyboard-input/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-ui-widgets | claude | stale-description | warning | .claude/skills/fs-gg-ui-widgets/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-skiaviewer | claude | stale-description | warning | .claude/skills/fs-gg-skiaviewer/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-product-elmish | claude | metadata-drift | warning | .claude/skills/fs-gg-product-elmish/SKILL.md | Wrapper skill name differs from the routed canonical skill. | Align wrapper metadata or document an intentional command exception. |
| fs-gg-feedback-capture | claude | stale-description | warning | .claude/skills/fs-gg-feedback-capture/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-ant-design | claude | missing-wrapper | warning | .claude/skills/fs-gg-ant-design/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| speckit-merge | spec-kit-command | guidance-rule-gap | warning | .agents/skills/speckit-merge/SKILL.md | Guidance rule visual-readiness is partial. | Add the missing concrete command/path/status caveat or record an explicit exception. |

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high
```
<!-- SKILL-PARITY:END -->
