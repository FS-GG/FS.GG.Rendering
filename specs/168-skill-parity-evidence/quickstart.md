# Quickstart: Skill Parity and Evidence Guidance

## Prerequisites

- .NET SDK for `net10.0`
- Repository root as the working directory
- Packages restored for the solution

```sh
dotnet restore FS.GG.Rendering.slnx
```

## 1. Run Focused Feature 168 Tests

Expected outcome: all Feature 168 checker tests pass.

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168"
```

The focused tests must cover:

- missing wrapper fixture
- wrapper-only fixture
- stale description fixture
- broken target fixture
- canonical drift fixture
- guidance-rule gap fixture
- passing fixture
- report Markdown/JSON agreement

## 2. Run Fixture Mode

Expected outcome: the fixture report detects each deliberate finding type and
the command exits `1` when `--fail-on high` sees required high-severity fixture
findings.

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx \
  --fixture all \
  --out specs/168-skill-parity-evidence/readiness/fixtures \
  --summary-json specs/168-skill-parity-evidence/readiness/fixture-summary.json \
  --json
```

Record the generated fixture result in
`specs/168-skill-parity-evidence/readiness/fixture-results.md`.

## 3. Generate Repository Parity Report

Expected outcome: the command writes the durable report and structured readiness
summary. A passing implementation has zero unresolved high-severity findings for
supported repository surfaces.

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx \
  --out specs/168-skill-parity-evidence/readiness/parity \
  --report docs/reports/skills-parity.md \
  --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json \
  --json
```

Review:

- `docs/reports/skills-parity.md`
- `specs/168-skill-parity-evidence/readiness/skill-parity-summary.json`
- `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`

## 4. Check Required Guidance Coverage

Expected outcome: every required guidance theme is covered, not applicable, or
explicitly excepted for relevant updated skills.

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx --list-rules
```

Required themes:

- package-pin drift and local-feed proof
- readiness evidence allowlisting
- validation output isolation
- visual readiness
- responsiveness diagnostics
- post-merge package bump validation
- evidence honesty

## 5. Validate Readiness Evidence Is Commit-Visible

Feature readiness evidence is ignored by default by `specs/*/readiness/`. If
Feature 168 readiness artifacts are committed, implementation must add an
allowlist entry for `specs/168-skill-parity-evidence/readiness/` and prove the
files are no longer ignored.

Expected outcome after allowlisting: `git check-ignore -v` prints no matching
ignore rule for committed readiness files.

```sh
git check-ignore -v specs/168-skill-parity-evidence/readiness/skill-parity-report.md || true
git status --short specs/168-skill-parity-evidence/readiness docs/reports/skills-parity.md
```

If the first command prints an ignore rule, readiness evidence is not committed
evidence yet.

## 6. Run Harness Validation Lane

Expected outcome: the rendering-harness lane passes or reports a visible caveat;
it must not be summarized as accepted if canceled, timed out, skipped,
substitute, synthetic, or environment-limited.

```sh
dotnet fsi scripts/run-validation-lanes.fsx \
  --lane rendering-harness \
  --out specs/168-skill-parity-evidence/readiness/lanes
```

## 7. Package-Consuming Sample Proof Reference

This feature does not change package-feed functionality, but updated skills must
point package-consuming sample work at the existing proof workflow.

Expected outcome when run during implementation: package pins and source proof
are checked through the existing harness.

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx \
  --sample samples/AntShowcase \
  --mode proof \
  --out specs/168-skill-parity-evidence/readiness/package-feed
```

## Completion Evidence

The final readiness package should contain:

- parity checker fixture output
- repository parity report
- guidance-rule coverage summary
- focused Feature 168 test results
- validation-lane output or visible caveat
- `git check-ignore` proof when readiness evidence is committed
