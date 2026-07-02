# Tasks: derive the gate test loop from the slnx

**Issue**: FS-GG/FS.GG.Rendering#47 · **Spec/Plan**: [`spec.md`](./spec.md) / [`plan.md`](./plan.md)

## Phase 1 — Investigation

- [X] T001 Enumerate slnx `*.Tests` members and each workflow reference; confirm the six orphans run
  in no cadence. → `research.md` R1.
- [X] T002 Classify the six orphans by capability (agent + headless run): all `none`. → `research.md` R2.
- [X] T003 Run the six headless; isolate `Rendering.Harness.Tests` red = `Feature168` parity
  `WarningStatus` (`fs-gg-samples` / `package-pin-drift` partial, missing "local feed"). → `research.md` R4.

## Phase 2 — Implementation

- [X] T004 `.github/workflows/gate.yml`: add job `env: GL_TEST_PROJECTS`; derive the deterministic
  tier from the slnx (skip `GL_TEST_PROJECTS`); GL step iterates `$GL_TEST_PROJECTS`. (FR-001/002/003)
- [X] T005 `template/fragments/samples/skill/SKILL.md`: add the "local feed" reference (the only
  missing `package-pin-drift` group — G2 was already covered by `package-feed`). Do NOT add the
  framework-only `scripts/refresh-local-feed-and-samples.fsx` path: this is a product-emitted skill
  and that path dangles in a generated product (Feature 225 de-leak; `Package.Tests` G-NODANGLE). (FR-006)
- [X] T006 Regenerate `template/skill-manifest/skill-manifest.json`
  (`dotnet fsi scripts/generate-skill-manifest.fsx`). (FR-006)

## Phase 3 — Meta-guard & docs

- [X] T007 Add `tests/Build.Tests/CadenceCoverageTests.fs` (+ fsproj `Compile` entry): assert
  `GL_TEST_PROJECTS ⊆ slnx`, gate deterministic tier is slnx-derived (no hardcoded name list),
  union == slnx test set, and both cadence docs are coherent. (FR-004/005)
- [X] T008 Refresh `docs/ci/cadence-map.md` §2/§3.1 and `docs/validation/validation-set.md` local
  inner loop to the true membership. (FR-005)

## Phase 4 — Verification & delivery

- [X] T009 Build the solution clean; run every gate-lane deterministic project headless incl. the new
  `CadenceCoverageTests` and `Rendering.Harness.Tests` (now green). Confirm `Feature168` parity
  `Passed`. (SC-001..004)
- [X] T010 Write spec / plan / research / tasks; squash-merge to `main`, update the Coordination board
  item to Done, push. Closes #47.
