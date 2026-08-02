---
schemaVersion: 1
workId: 1207-clean-checkout-evidence
title: Clean-checkout-safe feedback evidence locators
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1207-clean-checkout-evidence/spec.md
sourceClarifications: work/1207-clean-checkout-evidence/clarifications.md
sourceChecklist: work/1207-clean-checkout-evidence/checklist.md
publicOrToolFacingImpact: true
---

# Clean-checkout-safe feedback evidence locators Plan

Prose status: planned

## Source Snapshot
- spec: work/1207-clean-checkout-evidence/spec.md sha256:c96d3e1e7921888042c4b153c0836d687b85436d01ddbdc719b10e67f94ecbb8 schemaVersion:1
- clarifications: work/1207-clean-checkout-evidence/clarifications.md sha256:0f21ee62a28e89bfd0b9156b680a85a762f1a3ec8b4cdc09e1b1da5d1572d334 schemaVersion:1
- checklist: work/1207-clean-checkout-evidence/checklist.md sha256:aaa52d56619ed9b8deb3dbf4b2b338b95c12747387baec93f2756c7d77012881 schemaVersion:1

## Plan Scope
- Add a command-facing validation path for `feedback-tool validate`; preserve the
  existing reusable in-memory validation helper for synthetic callers.
- Update the shipped skill and test its copied payload from clean Git clones.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Resolve the frontmatter commit once per
  validation and prove `file:` evidence with `git ls-tree` plus `git show`.
- PD-002 [AC-001] [FR-002] complete: When the tree lacks the path, use Git's
  ignore/index state to classify ignored, untracked, and absent paths and attach
  bounded remediation to every diagnostic.
- PD-003 [AC-001] [FR-003] complete: Treat process startup, repository, commit,
  tree, and object-read failures as explicit validation errors.
- PD-004 [AC-001] [FR-004] complete: Do not execute command locators; document a
  generated render/performance command and verify its retained locator contract.
- PD-005 [AC-001] [FR-005] complete: Copy the package payload into a committed
  fixture repository, clone it cleanly, and exercise all availability states.

## Contract Impact
- PC-001 [PD-001] command report: `feedback-tool validate` now validates every
  `file:` locator against the report's `commit:` tree. Its existing nonzero exit
  behavior remains, with diagnostics that classify untracked, ignored, absent,
  non-regular, and unknown-Git evidence and state a bounded replacement route.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: Run the Package.Tests clean-clone
  fixture against the copied `.agents/skills/fs-gg-feedback-report` payload,
  covering committed, untracked, ignored, absent, unknown-Git, and command
  locator cases; build the package test project without restore afterward.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] compatible: Existing report and audit JSON schemas are
  unchanged. Historical `file:` citations whose frontmatter cannot resolve now
  fail closed and must be repaired using the printed bounded routes.

## Generated View Impact
- GV-001 [PD-001] workModel: Refresh the SDD work model and generated Codex and
  Claude guidance after authored artifacts and the verification receipt settle.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1207-clean-checkout-evidence`.
