---
description: "Task list for feature 230 — standalone spec-kit three-root skill mirror"
---

# Tasks: standalone spec-kit product mirrors the skill union into all three agent roots

**Input**: design docs in `/specs/230-standalone-skill-root-mirror/`. **Tier 2** (content/config + gate rework; template version bump). Gate corrections co-commit with the template change (the pre-230 gates assert the opposite invariant).

## Phase 1: Setup
- [X] T001 Confirm branch `230-standalone-skill-root-mirror`, net10.0 SDK, template installed from working tree (pack + `dotnet new install`; ensure the dev version sorts HIGHER than any feed-published `0.1.59-preview.1` or it is shadowed).
- [X] T002 Baseline: `dotnet fsi scripts/baseline-tests.fsx --out .../readiness/baseline.md` (recorded via baseline-after; 21/21 green pre/post).

## Phase 2: Foundational
- [X] T003 Verify the mechanism constraint: repo-root `.agents/skills/fs-gg-*` differs byte-wise from `template/product-skills/fs-gg-*` (so per-skill twins are required, not a blanket copy). Record in `readiness/mechanism.md`.
- [X] T004 Confirm `.codex/skills/` is SDD-owned/guard-watched (installed `fsgg-fixture-skills-intrusion-codex`) → mirror only under spec-kit.

## Phase 3: US1/US2 — three-root mirror (spec-kit) + clean orchestrated lanes (sdd/none)
- [X] T005 Add 24 mirror source rows to `.template.config/template.json`: for each of the 12 `.agents/skills/`-targeting sources, a `.claude/skills/` twin and a `.codex/skills/` twin, spec-kit-gated (product twins `(profile...) && lifecycle == "spec-kit"`).
- [X] T006 Live verify (pack+install): `spec-kit` → `.agents==.claude==.codex` (25 each, styling byte-identical); `sdd`/`none` → `.claude=.codex=0`, `.agents=8`. Record in `readiness/three-root-mirror.md`.

## Phase 4: US3 — gates
- [X] T007 Feature 219 G-EMIT surface test: `sources.Length >=27`; each product skill emits to `.agents/` (not spec-kit-gated) + `.claude/`+`.codex/` (spec-kit-gated); each id under all three roots.
- [X] T008 Feature 204: `gatedSourceAudit` — product-skill→`.claude/.codex` is spec-kit-gated workspace (add `.codex` to gated targets; drop the 229 forbidden-`.claude` guards); GV-2 workspace floor `>=6`→`>=30`; `gated-condition` string; GV-4/GV-5 assert `claude-product-skills=0 codex-product-skills=0` under sdd/none; new **GV-4b** asserts `spec-kit/<p>: three-root-mirror=ok`.
- [X] T009 `validate-lifecycle-template.fsx`: classifier + `.codex` gated target + workspace floor + `gated-condition`; `isGatedPath` adds `.codex/`; `claudeProductSkillCount`/`codexProductSkillCount` (exclude base `fs-gg-project`); `validateProfileLive` asserts the spec-kit three-root mirror + sdd/none claude+codex=0; report lines.
- [X] T010 Regenerate the shared report (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`, provenance: live); prove red-before (new gates fail on the 229 template) / green-after (15 pass). Record in `readiness/gate-transcripts.md`.

## Phase 5: Polish
- [X] T011 Bump template `0.1.59-preview.1` → `0.1.60-preview.1` (re-release; framework `FsGgUiVersion` unchanged); pack to local feed. `readiness/rerelease.md`.
- [X] T012 Full baseline-after (21/21 green); confirm no `src/`/`.fsi` touched; `.gitignore` readiness allowlist. `readiness/non-goals-held.md`.
- [X] T013 Update issue #42 with the corrected behavior (three-root standalone mirror; sdd/none clean).

## Notes
- Supersedes Feature 229's full confinement in the standalone lane only; `sdd`/`none` behavior unchanged (Templates#47 stays unblocked).
- Gate corrections co-commit with the template change.
