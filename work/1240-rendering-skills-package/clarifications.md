---
schemaVersion: 1
workId: 1240-rendering-skills-package
title: Publish FS.GG.Rendering.Skills
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/1240-rendering-skills-package/spec.md
publicOrToolFacingImpact: true
---

# Publish FS.GG.Rendering.Skills Clarifications

## Source Specification
- work/1240-rendering-skills-package/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001] blocking open: Resolve source ambiguity AMB-001 before checklist.

## Answers
- CQ-001 [AMB:AMB-001] decision: decision: Packed from where they are; nothing is relocated. The producer manifest already carries a per-row supplied-by path and the stager joins supplied-by with SKILL.md, so an out-of-tree source costs the packer nothing. Relocating would move bodies the fs-gg-ui template emits (violating SB-005) and would invalidate the source paths the org skill registry records for these rows. The count is three, not two: fs-gg-project is sourced from template/base/.agents/skills/fs-gg-project/ and is a third out-of-tree row alongside fs-gg-feedback-report and fs-gg-samples.

## Decisions
- DEC-001 [CQ-001] [AMB:AMB-001]: decision: Packed from where they are; nothing is relocated. The producer manifest already carries a per-row supplied-by path and the stager joins supplied-by with SKILL.md, so an out-of-tree source costs the packer nothing. Relocating would move bodies the fs-gg-ui template emits (violating SB-005) and would invalidate the source paths the org skill registry records for these rows. The count is three, not two: fs-gg-project is sourced from template/base/.agents/skills/fs-gg-project/ and is a third out-of-tree row alongside fs-gg-feedback-report and fs-gg-samples.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
No blocking ambiguity remains.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1240-rendering-skills-package`.
