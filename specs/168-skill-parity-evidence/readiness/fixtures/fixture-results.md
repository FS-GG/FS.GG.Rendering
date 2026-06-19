<!-- SKILL-PARITY:START -->
# Skill Parity Report

Checked at UTC: `2026-06-19T13:12:14.2685444Z`
Overall status: `failed`
Canonical sources: `6`
Wrappers: `4`

## Supported Surfaces
| Surface | Kind | Agent | Root | Required |
| --- | --- | --- | --- | --- |
| fixture-canonical | canonical | repository | canonical | True |
| fixture-codex | wrapper | codex | codex | True |
| fixture-claude | wrapper | claude | claude | True |

## Severity Counts
| Critical | High | Warning | Info |
| --- | --- | --- | --- |
| 0 | 9 | 9 | 0 |

## Guidance Coverage
| Rule | Covered | Partial | Missing | Excepted | Not applicable |
| --- | --- | --- | --- | --- | --- |
| package-pin-drift | 0 | 0 | 1 | 0 | 5 |
| readiness-allowlisting | 0 | 0 | 1 | 0 | 5 |
| validation-output-isolation | 0 | 0 | 1 | 0 | 5 |
| visual-readiness | 0 | 0 | 1 | 0 | 5 |
| responsiveness-diagnostics | 0 | 0 | 1 | 0 | 5 |
| post-merge-package-bump | 0 | 0 | 0 | 0 | 6 |
| evidence-honesty | 0 | 0 | 1 | 0 | 5 |

## Findings
| Skill | Surface | Category | Severity | Path | Message | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| fs-gg-fixture-stale | fixture-codex | stale-description | warning | codex/stale-description/SKILL.md | Wrapper description differs from the canonical skill description. | Refresh the wrapper description or add an explicit exception. |
| fs-gg-fixture-broken | fixture-codex | broken-target | high | codex/broken-target/SKILL.md | Wrapper target does not resolve. | Update the wrapper target path or restore the canonical skill source. |
| fs-gg-fixture-wrapper-only | fixture-codex | wrapper-only | warning | codex/wrapper-only/SKILL.md | Wrapper entry has no canonical target. | Add a canonical source route or classify the entry as an intentional command skill. |
| fs-gg-fixture-stale | claude | missing-wrapper | warning | canonical/stale-description/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-fixture-missing | codex-local | missing-wrapper | warning | canonical/missing-wrapper/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-fixture-missing | claude | missing-wrapper | warning | canonical/missing-wrapper/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-fixture-drift | codex-local | missing-wrapper | warning | canonical/drift-a/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-fixture-drift | claude | missing-wrapper | warning | canonical/drift-a/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-testing | codex-local | missing-wrapper | warning | canonical/guidance-gap/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-testing | claude | missing-wrapper | warning | canonical/guidance-gap/SKILL.md | Canonical skill is not exposed on this supported wrapper surface. | Add a short wrapper that routes to the canonical SKILL.md, or record an explicit exception. |
| fs-gg-fixture-drift | fixture-canonical | canonical-drift | high | canonical/drift-a/SKILL.md | Duplicate canonical sources with the same skill name diverge. | Choose one canonical source or document a specific variant exception. |
| fs-gg-fixture-drift | fixture-canonical | canonical-drift | high | canonical/drift-b/SKILL.md | Duplicate canonical sources with the same skill name diverge. | Choose one canonical source or document a specific variant exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule package-pin-drift is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule readiness-allowlisting is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule validation-output-isolation is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule visual-readiness is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule responsiveness-diagnostics is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |
| fs-gg-testing | fixture-canonical | guidance-rule-gap | high | canonical/guidance-gap/SKILL.md | Guidance rule evidence-honesty is missing. | Add the missing concrete command/path/status caveat or record an explicit exception. |

## Intentional Exceptions
No intentional exceptions were applied.

## Caveats
- Global Codex skill installation paths are excluded from required repository parity.
- Fixture mode uses synthetic skill files and is not real repository parity evidence.

## Regenerate

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/fixtures --report specs/168-skill-parity-evidence/readiness/fixtures/fixture-results.md --summary-json specs/168-skill-parity-evidence/readiness/fixture-summary.json --fixture all --fail-on high
```
<!-- SKILL-PARITY:END -->
