# Feature 168 Validation Log

## Focused Tests

Command:

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168"
```

Result:

```text
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```

## Surface Baseline Drift Proof

The focused test `SkillParity FSI surface matches the readiness baseline`
compares `tests/Rendering.Harness/SkillParity.fsi` against
`specs/168-skill-parity-evidence/readiness/surface-baselines/Rendering.Harness.SkillParity.txt`.
It passed in the Feature 168 focused test run.

## Fixture Mode

Command:

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --fixture all --out specs/168-skill-parity-evidence/readiness/fixtures --summary-json specs/168-skill-parity-evidence/readiness/fixture-summary.json --json
```

Result:

```json
{"summaryJson":"specs/168-skill-parity-evidence/readiness/fixture-summary.json","report":"specs/168-skill-parity-evidence/readiness/fixtures/fixture-results.md","overallStatus":"failed","critical":0,"high":9,"warning":9,"info":0}
```

Exit code `1` is expected because fixture mode deliberately creates synthetic
broken-target, canonical-drift, and guidance-gap cases. This is synthetic
negative-case evidence, not real repository parity.

## Repository Parity

Command:

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high --json
```

Result:

```json
{"summaryJson":"specs/168-skill-parity-evidence/readiness/skill-parity-summary.json","report":"docs/reports/skills-parity.md","overallStatus":"warning","critical":0,"high":0,"warning":35,"info":0}
```

The warning status is accepted for this feature because warnings are visible
wrapper metadata drift and one partial `speckit-merge` visual-readiness warning;
no unresolved high or critical finding remains.

## Validation Lane

Command:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out specs/168-skill-parity-evidence/readiness/lanes
```

Result:

```text
specs/168-skill-parity-evidence/readiness/lanes/validation-20260619-131049-941eb2/summary.md
```

Lane summary: overall readiness `ready`; required `rendering-harness` lane
`passed` in `00:00:07.2169014`. Caveat preserved: `aggregate-solution` was not
selected and `rendering-harness` is a targeted substitute for it.

## Package-Feed Reference

This feature changes guidance and harness tooling only. It references the
existing package-feed proof workflow; it does not add package-feed behavior.

Referenced workflow:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode proof --out specs/168-skill-parity-evidence/readiness/package-feed
```

Not run for this feature because no package-consuming sample package pins were
changed.

## Commit Visibility

Command:

```sh
git check-ignore -v specs/168-skill-parity-evidence/readiness/skill-parity-report.md
```

Result:

```text
.gitignore:75:!specs/168-skill-parity-evidence/readiness/** specs/168-skill-parity-evidence/readiness/skill-parity-report.md
```

Command:

```sh
git status --short specs/168-skill-parity-evidence/readiness docs/reports/skills-parity.md
```

Result:

```text
?? docs/reports/skills-parity.md
?? specs/168-skill-parity-evidence/readiness/
```

## Compatibility And Scope

No public `FS.GG.UI.*` runtime package behavior is changed by this feature.
