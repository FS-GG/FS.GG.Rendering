# Contract: Framework/Library Report

**Feature**: `176-test-antshowcase-controls`

Defines the consolidated `docs/reports/` deliverable (US4): structure, the sample-vs-framework
classification and severity taxonomy, evidence anchoring, and prioritization.

## Location & naming

```text
docs/reports/2026-06-20-feature-176-second-antshowcase-control-pass-report.md
```

Follows the established `YYYY-MM-DD-feature-NNN-<slug>.md` convention (research §D9) and links back
to this feature (`specs/176-test-antshowcase-controls/`).

## Required structure (mirrors the Feature 175 report)

```text
# <Title> — Control Pass of the Second AntShowcase

- Report date (UTC):
- Author:
- Source: feature 176 + merge SHA (filled at merge)
- Status:

## Executive summary            # 1–2 line impact statement

## Background                    # what the pass exercised, how

## Part 1 — Framework / library  # FrameworkShared findings + improvement opportunities
### F1. <problem>. Severity: <Critical|High|Medium|Low>
- Evidence:        <readiness artifact / file:line anchor>
- Root cause:
- Impact:
- Shipped mitigation:           # or "deferred — <follow-up ref>"
- Recommendation:
- Effort: <S|M|L>. Risk: <low|medium|high>.
[F2, F3, ...]

## Part 2 — Tooling & developer loop     # if any surfaced
## Part 3 — Spec Kit / process           # if any surfaced
## Part 4 — Agent tools & skills         # if any surfaced

## Part N — Sample-local fixes (separated)   # SampleLocal findings, clearly distinct from framework

## Prioritisation
| ID | Item | Severity | Effort | Leverage |
|----|------|----------|--------|----------|

## Suggested phased roadmap
- Phase A — ...
- Phase B — ...
- Phase C — ...

## Appendix — evidence anchors
- <code/readiness pointers, merge SHA>
```

## Rules

- **R-1 (location/convention)**: under `docs/reports/`, feature-scoped filename, links back to the
  feature (FR-013, SC-008).
- **R-2 (entry completeness)**: every framework/library entry carries **severity**, **sample-vs-
  framework classification**, **supporting evidence/reference**, and a **concrete recommendation**
  (FR-014, US4 AC2).
- **R-3 (separation)**: framework/library improvements are in their own Part(s), visually separated
  from sample-local fixes (FR-014, US4 AC3).
- **R-4 (ordering)**: findings are ordered by priority/impact; the prioritisation table ranks by
  severity × leverage (FR-014).
- **R-5 (coverage)**: the report covers every category the pass exercised — functional, visual,
  interaction-state, damage-locality, overlays, determinism, environment-limits, testing-helper
  gaps (SC-008).
- **R-6 (evidence-anchored)**: no recommendation without an anchor to a readiness artifact or a
  `file:line` code pointer (mirrors the Feature 175 appendix discipline).
- **R-7 (classification fidelity)**: a finding's `Classification`/`Tier` in the report matches the
  finding log (data-model Finding entity); a `FrameworkShared` fix is `Tier1`.

## Source

The report is authored **from the finding log and verdict records** the pass produces — it is not a
separate investigation. Every report entry traces to a Finding (id) and its before/after evidence.

## Test obligation

`DocumentationReviewTests` (existing pattern) is extended to assert the report exists, sits under
`docs/reports/`, contains the required Part headers, and that every framework entry row in the
prioritisation table has a non-empty severity and recommendation (lightweight structural check,
not prose review).
