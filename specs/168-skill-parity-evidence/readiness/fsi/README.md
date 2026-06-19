# FSI Evidence Guide

`Rendering.Harness.SkillParity.fsi` is authored before the implementation body.
The intended surface covers:

- supported skill surfaces and discovered skill entries
- wrapper target resolution and normalized repository-relative paths
- required guidance rules and coverage status
- intentional exceptions, findings, reports, request/model/message/effect
  values, and renderer functions
- CLI/report functions used by `scripts/check-agent-skill-parity.fsx`

The transcript in `skill-parity-authoring.fsx` is a lightweight prelude that
documents the public shapes before `.fs` implementation. The automated surface
drift assertion compares `tests/Rendering.Harness/SkillParity.fsi` against
`../surface-baselines/Rendering.Harness.SkillParity.txt`.
