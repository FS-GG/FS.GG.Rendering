# Implementation Plan: Skill Parity and Evidence Guidance

**Branch**: `168-skill-parity-evidence` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/168-skill-parity-evidence/spec.md`

## Summary

Encode the retrospective's recurring repository traps into canonical skill
guidance and add a non-destructive parity checker for supported agent skill
surfaces. The implementation will inventory canonical skill sources before
editing wrappers, update package/sample/readiness/visual/responsiveness/merge
guidance in canonical skills, keep `.agents/skills` and `.claude/skills` wrapper
surfaces synchronized, and emit reviewer-readable plus structured parity reports.
This is repository workflow tooling and guidance only; it does not change public
`FS.GG.UI.*` runtime package behavior.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`.
Harness-visible contracts are drafted in `.fsi` before `.fs` bodies.

**Primary Dependencies**: Existing .NET SDK, Expecto `10.2.2`, `System.IO`,
`System.Text.Json`, existing Spec Kit/agent skill files, and existing repository
scripts including `scripts/refresh-local-feed-and-samples.fsx` and
`scripts/run-validation-lanes.fsx`. No new package dependency is planned.

**Storage**: Filesystem artifacts only. The checker reads skill Markdown files,
wrapper target paths, and optional fixture directories. It writes generated
reports under `docs/reports/skills-parity.md` and feature readiness evidence
under `specs/168-skill-parity-evidence/readiness/`. No database or persistent
runtime storage.

**Testing**: Expecto through `dotnet test`. New focused tests in
`tests/Rendering.Harness.Tests` cover canonical/wrapper inventory, wrapper target
resolution, guidance-rule coverage, surface-baseline drift, finding
classification, fixture/dry-run cases including duplicate canonical-source
conflict, report rendering, and non-destructive behavior. CLI validation runs
through the `Rendering.Harness` executable and a thin script wrapper.

**Target Platform**: Maintainer and coding-agent workflow for the cross-platform
F#/.NET rendering repository. The checker uses repository-relative paths and
plain filesystem reads/writes.

**Project Type**: Multi-package F# rendering/UI library with repository-local
agent skills, generated-product skill templates, package-owned skill sources,
Spec Kit command skills, maintainer scripts, and package-consuming samples.

**Performance Goals**: Repository inventory and report generation complete within
10 seconds on this checkout. Fixture-mode validation completes within 5 seconds.
The checker names the first high-severity finding without requiring a reviewer to
open individual skill files.

**Constraints**: Non-destructive by default; no automatic wrapper repair in the
MVP. Public `FS.GG.UI.*` runtime package behavior and package surfaces must not
change. Existing skill metadata needed for discovery/invocation is preserved.
Wrapper guidance must point to canonical sources rather than fork them. Canceled,
timed-out, skipped, synthetic, substitute, degraded, pending-review, or
environment-limited evidence remains visibly caveated. Committed readiness
artifacts require `.gitignore` allowlisting proof.

**Scale/Scope**: Initial supported surfaces are `.agents/skills` (Codex/local
agent wrappers in this repo), `.claude/skills` (Claude wrappers), canonical
package skills under `src/*/skill`, generated-product and sample skill templates
under `template/**/skill` and `template/product-skills`, and the canonical Ant
Design skill under `.claude/skills/fs-gg-ant-design`. The implementation also
records wrapper-only Spec Kit command skills and intentional exceptions instead
of hiding them. Out of scope: automatic wrapper regeneration, CI enforcement,
marketplace publishing, and installed global Codex skill locations outside this
checkout.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies this as Tier 1 repository workflow and validation guidance. The plan keeps runtime rendering APIs out of scope. |
| Spec -> FSI -> semantic tests -> implementation | PASS | The checker model, statuses, reports, CLI, and effects are specified here and in contracts before implementation. Tasks must add `SkillParity.fsi`, failing semantic tests, a focused FSI/prelude transcript, and `SkillParity` surface-baseline evidence before `.fs` bodies. |
| Visibility lives in `.fsi` | PASS | Harness-visible types and functions belong in `tests/Rendering.Harness/SkillParity.fsi`; Markdown parsing, filesystem, and CLI plumbing stay implementation-owned. Feature readiness records the `Rendering.Harness.SkillParity` surface baseline plus the automated drift assertion. |
| Idiomatic simplicity | PASS | The design uses plain F# records, discriminated unions, path normalization, deterministic string/Markdown parsing, and JSON/Markdown rendering. No custom operators, SRTP, reflection-driven discovery, type providers, or broad framework abstractions are planned. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | The checker has request/inventory/report state and filesystem I/O, so it will expose a small `Model`/`Msg`/`Effect` boundary. Pure update logic classifies surfaces and findings; the edge interpreter reads files and writes reports. |
| Test evidence is mandatory | PASS | Focused tests prove missing-wrapper, wrapper-only, stale-description, broken-target, canonical-drift, duplicate canonical-source conflict, guidance-gap, non-destructive behavior, and passing findings. Any synthetic fixture content uses explicit fixture naming and stays separate from repository evidence. |
| Observability and safe failure | PASS | Every finding includes skill name, surface, category, severity, source path, wrapper path when applicable, and remediation hint. Broken paths and unreadable reports fail closed. |
| Tier 1 tooling boundaries | PASS | The contracted surface is maintainer tooling: `Rendering.Harness.SkillParity`, the `skill-parity` CLI, a script wrapper, skill Markdown guidance, and generated reports. No public `FS.GG.UI.*` runtime package behavior changes are planned. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/168-skill-parity-evidence/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- guidance-rule-coverage.md
|   |-- parity-report-record.md
|   |-- skill-parity-cli.md
|   `-- skill-surface-inventory.md
`-- readiness/
    |-- surface-baselines/
    |   `-- Rendering.Harness.SkillParity.txt
    |-- skill-parity-report.md
    |-- skill-parity-summary.json
    |-- guidance-coverage.md
    |-- fixture-results.md
    |-- feature168-tests.md
    `-- validation-log.md
```

### Source Code (repository root)

```text
scripts/
`-- check-agent-skill-parity.fsx

tests/
|-- Rendering.Harness/
|   |-- SkillParity.fsi
|   |-- SkillParity.fs
|   `-- Cli.fs
`-- Rendering.Harness.Tests/
    |-- Feature168SkillParityFixtures.fs
    |-- Feature168SkillInventoryTests.fs
    |-- Feature168GuidanceCoverageTests.fs
    |-- Feature168ParityFindingTests.fs
    `-- Feature168ParityReportTests.fs

docs/
`-- reports/
    `-- skills-parity.md

.agents/skills/
`-- */SKILL.md

.claude/skills/
`-- */SKILL.md

src/
`-- */skill/SKILL.md

template/
|-- base/.agents/skills/fs-gg-project/SKILL.md
|-- base/.claude/skills/fs-gg-project/SKILL.md
|-- feedback/skill/SKILL.md
|-- fragments/*/skill/SKILL.md
`-- product-skills/*/SKILL.md
```

**Structure Decision**: Keep the checker in `tests/Rendering.Harness` because
that executable already owns repository validation tooling (`PackageFeed`,
`ValidationLanes`, CLI subcommands, and reviewer evidence). The script stays a
thin maintainer-facing wrapper. Canonical skill text is updated first; wrappers
remain short target pointers with synchronized metadata. Reports are generated
to `docs/reports/skills-parity.md` for durable reviewer access and copied or
linked under feature readiness for implementation evidence. Feature readiness
also records the harness-only `SkillParity` surface baseline and package-surface
zero-delta note because this Tier 1 tooling change introduces a new
harness-visible `.fsi` module without changing public `FS.GG.UI.*` package
behavior.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Canonical skill sources are discovered before wrapper updates.
- `.agents/skills` is treated as the repo-local Codex/local-agent wrapper
  surface; `.claude/skills` is the Claude wrapper surface.
- Package-owned and template-owned skills remain canonical where wrappers point
  at them; `.claude/skills/fs-gg-ant-design/SKILL.md` is canonical for Ant
  guidance because both wrapper surfaces route through it.
- The checker extends `Rendering.Harness` instead of adding a shell-only parser.
- The first release is report-only and non-destructive by default.
- Guidance coverage is modeled as seven required rule themes matching the spec.
- Fixture/dry-run cases prove missing-wrapper, wrapper-only, stale-description,
  broken-target, canonical-drift, duplicate canonical-source conflict,
  guidance-gap, and non-destructive behavior findings.
- No new dependency is required.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state
transitions.

Observable contracts:

- [Skill Parity CLI](contracts/skill-parity-cli.md)
- [Skill Surface Inventory](contracts/skill-surface-inventory.md)
- [Guidance Rule Coverage](contracts/guidance-rule-coverage.md)
- [Parity Report Record](contracts/parity-report-record.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Design artifacts preserve the Tier 1 repository tooling/guidance boundary and explicitly forbid runtime `FS.GG.UI.*` behavior changes. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts define the intended `SkillParity.fsi` surface, CLI behavior, fixture semantics, report schema, MVU/effect transitions, and surface-baseline evidence before implementation. |
| Visibility lives in `.fsi` | PASS | Data model and contracts identify harness-visible records/unions/functions for `SkillParity.fsi`; filesystem interpreters and CLI parsing remain implementation-owned, and readiness carries an automated surface-drift assertion for the `.fsi`. |
| Idiomatic simplicity | PASS | Design uses plain records/unions, normalized paths, deterministic Markdown/front-matter parsing, and JSON/Markdown reports. |
| Elmish/MVU boundary | PASS | The data model maps inventory, classification, report writing, and failures through request/model/messages/effects. |
| Test evidence | PASS | `quickstart.md` lists focused tests, fixture runs, full repository parity report generation, readiness ignore checks, and validation-lane evidence commands. |
| Observability and safe failure | PASS | `parity-report-record.md` requires status, finding severity counts, affected surface paths, rule coverage, caveats, and remediation hints in both Markdown and JSON. |
| Tier 1 tooling boundaries | PASS | Tooling and guidance updates remain in harness/scripts/skills/docs. Public package surfaces and rendering behavior stay unchanged unless a later task explicitly reclassifies scope. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.
