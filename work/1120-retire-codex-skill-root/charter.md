---
schemaVersion: 1
workId: 1120-retire-codex-skill-root
title: Retire Rendering .codex skill root
stage: charter
changeTier: tier1
status: chartered
policyPointers:
  - .fsgg/sdd.yml
  - .fsgg/agents.yml
  - .fsgg/policy.yml
  - .fsgg/capabilities.yml
  - .fsgg/tooling.yml
---

# Retire Rendering .codex skill root Charter

## Identity
- Work id: `1120-retire-codex-skill-root`
- Lifecycle stage: charter
- Status: chartered

## Principles
- Retire roots only through the declared root-set constants; never hide a projection
  change by deleting a mirror by hand.
- Preserve a verification subject that can genuinely fail. After `.agents/skills`
  became a generated view of `.claude/skills`, cross-root byte comparison is
  tautological and producer attribution is the non-vacuous replacement.
- Keep the runtime contract explicit: ADR-0065 / ADR-0067 §5 retain `.claude/skills`
  and `.agents/skills`, while the template test continues to exclude historical
  mirror locations rather than asserting the runtime-root set.

## Scope Boundaries
- In scope: `scripts/materialize-skill-roots.sh`, the retired `.codex/skills`
  projections, deterministic-gate wording, and the template-mirror regression test.
- Out of scope: changing the two surviving runtime roots, the kit root set, or
  changing how template canonical sources are discovered.

## Policy Pointers
- SDD policy comes from `.fsgg/sdd.yml` and `.fsgg/agents.yml`.
- Governance files are optional compatibility pointers and are not evaluated by this command.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd specify --work 1120-retire-codex-skill-root`.
