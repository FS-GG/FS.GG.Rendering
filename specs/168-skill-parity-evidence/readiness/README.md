# Feature 168 Readiness Evidence

This directory contains committed evidence for the skill parity and evidence
guidance feature.

## Evidence Map

- `fsi/README.md` and `fsi/skill-parity-authoring.fsx`: pre-implementation FSI
  authoring notes for `Rendering.Harness.SkillParity`.
- `surface-inventory.md`: canonical skill sources, wrapper surfaces, command
  surfaces, and external exclusions identified before guidance edits.
- `surface-baselines/Rendering.Harness.SkillParity.txt`: harness-only surface
  baseline used by the automated Feature 168 drift assertion.
- `fixtures/README.md` and `fixture-results.md`: controlled synthetic fixture
  cases for missing wrappers, wrapper-only entries, stale descriptions, broken
  targets, canonical drift, guidance gaps, and passing parity.
- `parity/README.md`, `skill-parity-report.md`, and
  `skill-parity-summary.json`: repository parity output and structured summary.
- `guidance-coverage.md`: required guidance-rule coverage matrix and list-rules
  evidence.
- `feature168-tests.md`: focused test output.
- `validation-log.md`: validation lane, package-feed reference, caveats, and
  `git check-ignore` proof.

Feature readiness directories are ignored by default through
`specs/*/readiness/`; `.gitignore` now allowlists this directory and nested
files so the evidence remains commit-visible.
