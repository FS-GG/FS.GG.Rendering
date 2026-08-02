---
schemaVersion: 1
workId: 1120-retire-codex-skill-root
title: Retire Rendering .codex skill root
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/1120-retire-codex-skill-root/spec.md
publicOrToolFacingImpact: true
---

# Retire Rendering .codex skill root Clarifications

## Source Specification
- work/1120-retire-codex-skill-root/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001] blocking open: Resolve source ambiguity AMB-001 before checklist.

## Answers
- CQ-001 [AMB:AMB-001] decision: Decision: `.agents/skills` is a generated view of `.claude/skills`, so old cross-root comparison is vacuous. The independent invariant is producer attribution plus orphan detection; `skill-view check` asserts the two runtime roots.

## Decisions
- DEC-001 [CQ-001] [AMB:AMB-001]: Decision: `.agents/skills` is a generated view of `.claude/skills`, so old cross-root comparison is vacuous. The independent invariant is producer attribution plus orphan detection; `skill-view check` asserts the two runtime roots.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
No blocking ambiguity remains.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1120-retire-codex-skill-root`.
