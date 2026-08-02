---
schemaVersion: 1
workId: 1120-retire-codex-skill-root
title: Retire Rendering .codex skill root
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1120-retire-codex-skill-root/spec.md
sourceClarifications: work/1120-retire-codex-skill-root/clarifications.md
sourceChecklist: work/1120-retire-codex-skill-root/checklist.md
publicOrToolFacingImpact: true
---

# Retire Rendering .codex skill root Plan

Prose status: planned

## Source Snapshot
- spec: work/1120-retire-codex-skill-root/spec.md sha256:1894509e372c93c3c7fb358bea3f2f8c4d91148d593168dc844761b3b97ac7c2 schemaVersion:1
- clarifications: work/1120-retire-codex-skill-root/clarifications.md sha256:4d245f20c326951f62ec4d08bb42e6f0427c0b5bd4cf70746e093ce8f6346673 schemaVersion:1
- checklist: work/1120-retire-codex-skill-root/checklist.md sha256:c1006ec2c1f71564d1b19eb657491fb73667035b4dd894ebb3e0cd65cc371612 schemaVersion:1

## Plan Scope
- Change only the materializer's declared roots and retired-root handling, the
  deterministic-gate explanation, the template-mirror terminology, and generated
  lifecycle evidence.
- The implementation must leave `.claude/skills` and `.agents/skills` as the
  active roots and must not alter kit-owned producer behavior.

## Plan Decisions
- PD-001 [FR-001] [AC-001]: Replace the three-root default with ADR-0065's two
  runtime roots and correct comments that still name ADR-0011's three roots.
- PD-002 [FR-002] [AC-002]: Keep producer-attribution, route and orphan checks;
  remove the vacuous cross-root projection comparison after documenting why
  `skill-view check` is the independent runtime-root verification.
- PD-003 [FR-003] [AC-003]: Add one `RETIRED_ROOTS` declaration and make check
  report each leftover while apply sweeps only declared retired directories.
- PD-004 [FR-004] [AC-004]: Rename the test oracle to historical template mirrors
  so it keeps protecting canonical-source inventory without claiming a stale
  runtime contract.

## Contract Impact
- PC-001 [PD-001] root-set contract: `DEFAULT_ROOTS` is the reviewed source of
  truth for Rendering-owned active roots; `RETIRED_ROOTS` is the reviewed source
  of truth for mechanical cleanup.
- PC-002 [PD-002] verification contract: producer attribution and orphan
  detection remain independently falsifiable after two roots become one view.

## Verification Obligations
- VO-001 [PD-001] [PD-003] [PC-001] scriptCheck: `scripts/materialize-skill-roots.sh --check` is green after apply and red when a retired-root directory remains.
- VO-002 [PD-002] [PC-002] negativeCheck: Plant an unattributed skill under a surviving root and confirm `[orphan]` is reported.
- VO-003 [PD-004] regressionTest: Run `Feature1081TemplateCanonicalRootsTests` to prove all historical template mirrors remain excluded.
- VO-004 [PD-001] integrationCheck: Run the deterministic-gate-relevant skill view/materializer checks and record their results in evidence.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] mechanicalRetirement: The root removal is backward-compatible for
  runtime discovery because Codex resolves `.agents/skills`; apply removes only
  declared `.codex/skills` directories and check diagnoses any survivor.

## Generated View Impact
- GV-001 [PD-001] workModel: The SDD work model and agent commands under
  `readiness/1120-retire-codex-skill-root/` are regenerated after authored plan,
  task, evidence, and verification changes; stale digests block readiness rather
  than silently describing a different implementation.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1120-retire-codex-skill-root`.
