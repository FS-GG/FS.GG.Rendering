---
schemaVersion: 1
workId: feedback-invalidation-base-audits
title: Index feedback audit invalidation from the base tree
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

# Index feedback audit invalidation from the base tree Charter

## Identity
- Work id: `feedback-invalidation-base-audits`
- Lifecycle stage: charter
- Status: chartered

Make the commit-time audit-invalidation check index the audits it calls *merged*
from the tree it was given as the merged state, so a candidate cannot be refused
by an audit the candidate itself introduced.

## Principles
- The subject a check indexes is part of its contract, and a check that leaves it
  implicit cannot be reasoned about. Name the indexed tree in the verdict itself.
- The audit index and the changed-path set must be derived from the *same* base,
  or the two halves of the answer describe different states of the world.
- Fail closed on every input the check could not evaluate: an unreadable ref, an
  unreadable blob, and a malformed audit each get their own explicit diagnostic.
  A subject that could not be evaluated is never a subject that passed.
- Preserve the protections `#1178` and `#1194` established. A genuinely merged
  audit must keep failing a candidate that touches its digest-bound evidence, and
  deleting or rewriting that audit in the candidate must not make the gate green.

## Scope Boundaries
- In: the `check-invalidation` command's audit-index subject, its `--base/--head`
  and `--changed` forms, the producer library behind them, the skill wording that
  documents them, and the generated skill manifest digest that binds that wording.
- Out: the report/audit JSON schema, the `validate` command's digest binding, the
  `audit-binding-exceptions.json` exception ledger's semantics (`FS-GG/.github#2659`
  covers a different root cause in the same producer file), and any rewrite of
  existing audit records.

## Policy Pointers
- SDD policy comes from `.fsgg/sdd.yml` and `.fsgg/agents.yml`.
- Constitution II, VI and VIII: keep the tool's structured contract, its automated
  proof, and its explicit diagnostics coherent with one another.
- Issue #1243's acceptance criteria are the product-specific delivery contract.
- Governance files are optional compatibility pointers and are not evaluated here.

## Lifecycle Notes
- Tier 1: `check-invalidation` is a durable, documented, tool-facing contract that
  the template distributes to every scaffolded product.
- The mandatory delivery-route decision is `sdd-required`; bind the completed work
  and readiness evidence to issue #1243 and its PR.
- Next lifecycle action: `fsgg-sdd specify --work feedback-invalidation-base-audits`.
