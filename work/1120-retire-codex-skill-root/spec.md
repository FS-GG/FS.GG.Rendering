---
schemaVersion: 1
workId: 1120-retire-codex-skill-root
title: Retire Rendering .codex skill root
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Retire Rendering .codex skill root Specification

Prose status: specified

## User Value
Codex users see each Rendering skill once through the supported runtime discovery
paths, and contributors receive a gate failure if an unattributed skill or a retired
root projection reappears.

## Scope
- SB-001: Narrow Rendering-owned skill-root projection from three roots to
  `.claude/skills` and `.agents/skills`, remove the projected `.codex` tree, and
  preserve non-vacuous verification.

## Non-Goals
- SB-002: Do not change the two surviving runtime roots, kit-owned skills, or the
  template canonical-source algorithm.

## User Stories
- US-001 (P1): As a Codex user, I see each Rendering skill once through the
  supported runtime discovery roots.
- US-002 (P1): As a maintainer, I get a failing check when a retired-root
  projection or an unattributed skill is present.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given the materializer is applied, when its check is
  run, then only `.claude/skills` and `.agents/skills` are active roots and
  `.codex/skills` has no projected skill directory.
- AC-002 [US-002] [FR-002]: Given an unproduced skill is planted under a surviving
  root, when the materializer check runs, then it fails with `[orphan]`.
- AC-003 [US-002] [FR-003]: Given a skill directory remains in `.codex/skills`,
  when the materializer check runs, then it fails with `[retired-leftover]` and
  apply removes that directory through the retired-root declaration.
- AC-004 [US-002] [FR-004]: Given the template contains any historical mirror root,
  when canonical sources are inventoried, then it contributes no canonical source.

## Functional Requirements
- FR-001: The materializer MUST declare exactly `.claude/skills` and `.agents/skills` as its default roots, citing ADR-0065 and ADR-0067 §5; the deterministic-gate explanation MUST not claim an independent cross-root comparison remains. (Stories: US-001; Acceptance: AC-001)
- FR-002: The materializer MUST retain producer-attribution and orphan detection across surviving roots; an orphan negative case MUST fail. (Stories: US-002; Acceptance: AC-002)
- FR-003: The materializer MUST declare the retired `.codex/skills` root once; check mode MUST report a surviving directory and apply mode MUST sweep only that declared retired root. (Stories: US-002; Acceptance: AC-003)
- FR-004: The template-canonical regression test MUST identify its three paths as historical mirrors, not as the current runtime-root contract. (Stories: US-002; Acceptance: AC-004)

## Ambiguities
- AMB-001: The two surviving roots are intentionally not byte-compared: on the
  current view layout they resolve to the same object. Resolved by retaining
  independent producer-attribution and the kit `skill-view` contract check.

## Public Or Tool-Facing Impact
- This changes the repository's agent-skill root and CI verification contracts; it
  is a Tier 1 tool-facing change.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1120-retire-codex-skill-root`.
