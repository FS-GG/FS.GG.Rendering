---
schemaVersion: 1
workId: 1207-clean-checkout-evidence
title: Clean-checkout-safe feedback evidence locators
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/1207-clean-checkout-evidence/spec.md
publicOrToolFacingImpact: true
---

# Clean-checkout-safe feedback evidence locators Clarifications

## Source Specification
- work/1207-clean-checkout-evidence/spec.md

## Clarification Questions
- CQ-001 [FR-001]: Which revision is authoritative for `file:` evidence?
- CQ-002 [FR-004]: Must validation execute a `command:` locator?
- CQ-003 [FR-003]: Can unavailable Git state be treated as an ordinary missing file?

## Answers
- CQ-001: The report frontmatter's `commit:` value is authoritative. The tool
  verifies it as a commit and reads the file from that tree.
- CQ-002: No. The critic runs and inspects the bounded command before recording
  it; validation preserves the existing non-file-locator contract.
- CQ-003: No. It is an explicit fail-closed diagnostic because no reproducible
  availability decision can be made.

## Decisions
- DEC-001 [CQ-001] [FR-001] [FR-002]: Use Git plumbing (`rev-parse`, `ls-tree`,
  and `show`) rather than local file existence; classify untracked, ignored, and
  absent paths only after a tree lookup misses.
- DEC-002 [CQ-002] [FR-004]: Keep `command:` locators executable evidence
  descriptions, not arbitrary commands the validator launches.
- DEC-003 [CQ-003] [FR-003]: Any failed Git probe is an actionable diagnostic
  that gives the same bounded replacement routes.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
No blocking ambiguity remains.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1207-clean-checkout-evidence`.
