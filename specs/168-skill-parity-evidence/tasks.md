# Tasks: Skill Parity and Evidence Guidance

**Input**: Design documents from `/specs/168-skill-parity-evidence/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification and constitution. Write story-specific Expecto tests before implementation, exercise the draft `SkillParity.fsi` surface through F# Interactive or an equivalent transcript before `.fs` implementation, and keep fixture/synthetic evidence visibly caveated.

**Organization**: Tasks are grouped by user story so repository trap guidance, wrapper drift detection, visual/responsiveness evidence honesty, and parity report review can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks
- **[Story]**: User story label from spec.md
- Every task names the exact target file or evidence path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the Feature 168 evidence scaffold and commit-visible readiness path before implementation.

- [X] T001 Create the Feature 168 readiness overview in `specs/168-skill-parity-evidence/readiness/README.md` with report, fixtures, FSI, surface-baseline, coverage, tests, validation, and caveat evidence locations
- [X] T002 [P] Create the FSI evidence guide in `specs/168-skill-parity-evidence/readiness/fsi/README.md` with the pre-implementation `SkillParity.fsi` authoring transcript and surface-baseline requirements
- [X] T003 [P] Create the repository parity output guide in `specs/168-skill-parity-evidence/readiness/parity/README.md` with durable report, summary JSON, and guidance coverage expectations
- [X] T004 [P] Create the controlled fixture evidence guide in `specs/168-skill-parity-evidence/readiness/fixtures/README.md` with missing-wrapper, wrapper-only, stale-description, broken-target, canonical-drift, duplicate canonical-source conflict, guidance-gap, and passing fixture expectations
- [X] T005 Add Feature 168 readiness allowlist entries for `specs/168-skill-parity-evidence/readiness/` and nested files in `.gitignore`
- [X] T006 [P] Create the implementation validation ledger in `specs/168-skill-parity-evidence/readiness/validation-log.md` with sections for focused tests, surface-baseline drift proof, fixture mode, repository parity, validation lane, package-feed reference, and `git check-ignore` proof

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the harness-visible contract, compile-order hooks, fixture scaffolding, and source inventory before any story implementation.

**Critical**: No user story implementation should begin until the surface inventory, `.fsi` contract, compile entries, FSI transcript placeholders, and `SkillParity` surface-baseline evidence exist.

- [X] T007 Identify canonical skill sources, wrapper surfaces, command-only skills, and intentional external exclusions in `specs/168-skill-parity-evidence/readiness/surface-inventory.md` before editing any skill guidance
- [X] T008 Draft `Rendering.Harness.SkillParity` signatures for surfaces, entries, wrapper targets, guidance rules, coverage, exceptions, findings, reports, request/model/messages/effects, and renderer functions in `tests/Rendering.Harness/SkillParity.fsi`
- [X] T009 Create a compile-safe placeholder implementation and add compile entries for `tests/Rendering.Harness/SkillParity.fsi` and `tests/Rendering.Harness/SkillParity.fs` before `Cli.fs` in `tests/Rendering.Harness/Rendering.Harness.fsproj`
- [X] T010 Record the pre-implementation FSI authoring transcript for the T008 signatures in `specs/168-skill-parity-evidence/readiness/fsi/skill-parity-authoring.fsx`
- [X] T011 Create the `Rendering.Harness.SkillParity` surface-area baseline evidence in `specs/168-skill-parity-evidence/readiness/surface-baselines/Rendering.Harness.SkillParity.txt`, including the package-surface zero-delta note and the automated drift assertion that will compare `tests/Rendering.Harness/SkillParity.fsi` against this baseline
- [X] T012 [P] Create shared Feature 168 temporary filesystem, synthetic fixture, Markdown, and JSON assertion helpers in `tests/Rendering.Harness.Tests/Feature168SkillParityFixtures.fs`
- [X] T013 Add `tests/Rendering.Harness.Tests/Feature168SkillParityFixtures.fs` to the compile list before other Feature 168 test files in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T014 [P] Create the thin maintainer script skeleton in `scripts/check-agent-skill-parity.fsx` that will forward to `tests/Rendering.Harness/Rendering.Harness.fsproj`

**Checkpoint**: Feature 168 shared contracts are drafted, exercised through FSI authoring and surface-baseline evidence, and story tests can target stable module names before `.fs` bodies are filled in.

---

## Phase 3: User Story 1 - Follow Repository Trap Guidance (Priority: P1) MVP

**Goal**: A coding agent or contributor touching samples, readiness artifacts, or validation workflows can load the relevant skill and see the repository-specific checks required before claiming readiness.

**Independent Test**: Inspect every relevant updated skill and verify that it names guidance for package-pin drift, readiness evidence allowlisting, validation output isolation, visual readiness, responsiveness diagnostics, post-merge package bump validation, and evidence honesty.

### Tests for User Story 1

Write these tests first and confirm they fail before implementation.

- [X] T015 [P] [US1] Create failing guidance-rule coverage tests for package-pin drift, readiness allowlisting, validation output isolation, post-merge package bump validation, and evidence honesty in `tests/Rendering.Harness.Tests/Feature168GuidanceCoverageTests.fs`
- [X] T016 [US1] Add `tests/Rendering.Harness.Tests/Feature168GuidanceCoverageTests.fs` to the compile list after `Feature168SkillParityFixtures.fs` in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 1

- [X] T017 [US1] Implement required guidance-rule definitions, required references, applicability patterns, and rule tokens in `tests/Rendering.Harness/SkillParity.fs`
- [X] T018 [US1] Update implementation guidance for package pins, readiness allowlisting, validation output isolation, and evidence honesty in `.agents/skills/speckit-implement/SKILL.md` and `.claude/skills/speckit-implement/SKILL.md`
- [X] T019 [P] [US1] Update package-owned testing guidance with readiness allowlisting, validation output isolation, and evidence caveats in `src/Testing/skill/SKILL.md`
- [X] T020 [P] [US1] Update generated-product testing guidance with package-feed proof, readiness allowlisting, validation isolation, and evidence caveats in `template/product-skills/fs-gg-testing/SKILL.md`
- [X] T021 [P] [US1] Update sample-pack guidance with current `FS.GG.UI.*` package pin checks, `scripts/refresh-local-feed-and-samples.fsx` proof workflow references, and `scripts/run-validation-lanes.fsx` validation-lane expectations in `template/fragments/samples/skill/SKILL.md`
- [X] T022 [P] [US1] Update generated-project guidance with readiness ignore checks and evidence honesty rules in `template/base/.agents/skills/fs-gg-project/SKILL.md` and `template/base/.claude/skills/fs-gg-project/SKILL.md`
- [X] T023 [P] [US1] Update repository package guidance for package-consuming samples, package-feed proof, validation-lane workflow references, responsiveness evidence, and validation caveats in `src/Controls/skill/SKILL.md` and `src/SkiaViewer/skill/SKILL.md`
- [X] T024 [US1] Add repo-local merge and post-merge guidance requiring package bump evidence, local-feed pack, sample pin alignment, restore or validation, and readiness ledger updates in `.agents/skills/speckit-merge/SKILL.md` and `.claude/skills/speckit-merge/SKILL.md`
- [X] T025 [US1] Implement guidance coverage scanning with covered, partial, missing, not-applicable, and excepted statuses in `tests/Rendering.Harness/SkillParity.fs`
- [X] T026 [US1] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168&Guidance"` and record output in `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`

**Checkpoint**: User Story 1 is independently testable by reviewing updated skills and running the focused guidance coverage tests. This is the MVP slice.

---

## Phase 4: User Story 2 - Detect Skill Wrapper Drift (Priority: P1)

**Goal**: A maintainer can run a parity check and identify missing, stale, wrapper-only, or broken skill surfaces across supported agent directories.

**Independent Test**: Run the parity check against controlled fixture or dry-run cases with a missing wrapper, stale description, broken target path, wrapper-only entry, canonical drift, duplicate canonical-source conflict, and non-destructive behavior proof. Each case must be detected with the affected skill name, agent surface, finding type, and remediation hint.

### Tests for User Story 2

Write these tests first and confirm they fail before implementation.

- [X] T027 [P] [US2] Create failing surface inventory, `Rendering.Harness.SkillParity` surface-baseline drift, and wrapper target resolution tests for `.agents/skills`, `.claude/skills`, `src/*/skill`, `template/**/skill`, `template/product-skills`, Ant canonical, and Spec Kit command surfaces in `tests/Rendering.Harness.Tests/Feature168SkillInventoryTests.fs`
- [X] T028 [P] [US2] Create failing parity finding and non-destructive behavior tests for missing-wrapper, wrapper-only, stale-description, broken-target, canonical-drift, duplicate canonical-source conflict, guidance-rule-gap, and passing fixture cases in `tests/Rendering.Harness.Tests/Feature168ParityFindingTests.fs`
- [X] T029 [US2] Add `tests/Rendering.Harness.Tests/Feature168SkillInventoryTests.fs` and `tests/Rendering.Harness.Tests/Feature168ParityFindingTests.fs` to the compile list after `Feature168GuidanceCoverageTests.fs` in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 2

- [X] T030 [US2] Implement front-matter parsing, normalized repository-relative path handling, and skill entry discovery in `tests/Rendering.Harness/SkillParity.fs`
- [X] T031 [US2] Implement default surface discovery for `.agents/skills`, `.claude/skills`, `src/*/skill`, `template/**/skill`, `template/product-skills`, `.claude/skills/fs-gg-ant-design/SKILL.md`, and Spec Kit command skills in `tests/Rendering.Harness/SkillParity.fs`
- [X] T032 [US2] Implement wrapper target parsing and relative target resolution from the "Before acting, read" route text in `tests/Rendering.Harness/SkillParity.fs`
- [X] T033 [US2] Implement metadata comparison, description normalization, wrapper-only classification, command-surface exceptions, and stale-description findings in `tests/Rendering.Harness/SkillParity.fs`
- [X] T034 [US2] Implement missing-wrapper, broken-target, canonical-drift, guidance-rule-gap, unreadable-surface, and remediation-hint finding generation in `tests/Rendering.Harness/SkillParity.fs`
- [X] T035 [US2] Implement controlled fixture builders for missing-wrapper, wrapper-only, stale-description, broken-target, canonical-drift, duplicate canonical-source conflict, guidance-gap, and passing cases in `tests/Rendering.Harness.Tests/Feature168SkillParityFixtures.fs`
- [X] T036 [US2] Wire the `skill-parity` CLI options `--repo`, `--out`, `--report`, `--summary-json`, `--fixture`, `--surface`, `--allow-exception`, `--fail-on`, `--list-rules`, and `--json` in `tests/Rendering.Harness/Cli.fs`
- [X] T037 [US2] Complete `scripts/check-agent-skill-parity.fsx` so it forwards all arguments to `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- skill-parity` in `scripts/check-agent-skill-parity.fsx`
- [X] T038 [US2] Run fixture mode with `dotnet fsi scripts/check-agent-skill-parity.fsx --fixture all --out specs/168-skill-parity-evidence/readiness/fixtures --summary-json specs/168-skill-parity-evidence/readiness/fixture-summary.json --json` and record the expected findings plus non-destructive file-hash proof in `specs/168-skill-parity-evidence/readiness/fixture-results.md`

**Checkpoint**: User Story 2 is independently testable through fixture mode and the focused inventory/finding test files.

---

## Phase 5: User Story 3 - Preserve Visual and Responsiveness Evidence Honesty (Priority: P2)

**Goal**: A maintainer or agent working on screenshots, contact sheets, visual inspections, or live interactivity sees consistent guidance that real evidence is preferred and limitations must be disclosed.

**Independent Test**: Review updated visual-readiness, product-testing, implementation, and merge guidance. Each relevant skill must distinguish accepted evidence from degraded, synthetic, environment-limited, pending-review, and substitute evidence.

### Tests for User Story 3

Write these tests first and confirm they fail before implementation.

- [X] T039 [US3] Add failing visual-readiness and responsiveness evidence-honesty coverage cases to `tests/Rendering.Harness.Tests/Feature168GuidanceCoverageTests.fs`

### Implementation for User Story 3

- [X] T040 [US3] Update visual readiness guidance to prefer real screenshots, disclose degraded captures, require reviewer classification, and preserve manual caveats in `src/Testing/skill/SKILL.md` and `template/product-skills/fs-gg-testing/SKILL.md`
- [X] T041 [P] [US3] Update responsiveness guidance to separate screenshot readiness from pointer activation, keyboard activation, input routing latency, update latency, render latency, and presentation latency in `src/SkiaViewer/skill/SKILL.md` and `template/product-skills/fs-gg-skiaviewer/SKILL.md`
- [X] T042 [P] [US3] Update controls and widget guidance to require visual evidence caveats and live interaction caveats for generated controls in `src/Controls/skill/SKILL.md`, `template/fragments/controls/skill/SKILL.md`, and `template/product-skills/fs-gg-ui-widgets/SKILL.md`
- [X] T043 [US3] Update implementation and merge guidance so canceled, timed-out, skipped, synthetic, substitute, degraded, pending-review, or environment-limited checks remain visibly caveated in `.agents/skills/speckit-implement/SKILL.md`, `.claude/skills/speckit-implement/SKILL.md`, `.agents/skills/speckit-merge/SKILL.md`, and `.claude/skills/speckit-merge/SKILL.md`
- [X] T044 [US3] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168&Guidance"` and append US3 evidence-honesty results to `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`

**Checkpoint**: User Story 3 is independently testable by reviewing the relevant guidance and running the focused coverage tests.

---

## Phase 6: User Story 4 - Review a Skill Parity Report (Priority: P3)

**Goal**: A reviewer can open a generated parity report and quickly understand which skill surfaces are current, which guidance rules are covered, and what remains incomplete.

**Independent Test**: Generate the parity report after updating skills. A reviewer must be able to identify overall parity status, required guidance coverage, affected skill surfaces, and any remaining findings from the report alone.

### Tests for User Story 4

Write these tests first and confirm they fail before implementation.

- [X] T045 [P] [US4] Create failing Markdown report, summary JSON, severity counts, coverage matrix, remediation, command echo, and first-high-finding visibility tests in `tests/Rendering.Harness.Tests/Feature168ParityReportTests.fs`
- [X] T046 [US4] Add `tests/Rendering.Harness.Tests/Feature168ParityReportTests.fs` to the compile list after `Feature168ParityFindingTests.fs` in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 4

- [X] T047 [US4] Implement parity report aggregation for checked date, overall status, surface counts, canonical source count, wrapper count, severity counts, guidance coverage, findings, intentional exceptions, and caveats in `tests/Rendering.Harness/SkillParity.fs`
- [X] T048 [US4] Implement Markdown and summary JSON rendering that agree on status, counts, findings, coverage, and caveats in `tests/Rendering.Harness/SkillParity.fs`
- [X] T049 [US4] Implement `<!-- SKILL-PARITY:START -->` and `<!-- SKILL-PARITY:END -->` generated-section preservation for manual caveats in `tests/Rendering.Harness/SkillParity.fs`
- [X] T050 [US4] Wire CLI report writing, summary JSON writing, exit codes, and `--json` stdout behavior in `tests/Rendering.Harness/Cli.fs`
- [X] T051 [US4] Generate the durable parity report at `docs/reports/skills-parity.md` and the structured readiness summary at `specs/168-skill-parity-evidence/readiness/skill-parity-summary.json`
- [X] T052 [US4] Generate the feature readiness parity report and guidance coverage matrix at `specs/168-skill-parity-evidence/readiness/skill-parity-report.md` and `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`
- [X] T053 [US4] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168&ParityReport"` and record output in `specs/168-skill-parity-evidence/readiness/feature168-tests.md`

**Checkpoint**: User Story 4 is independently testable by report-focused tests and the generated report artifacts.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete end-to-end validation, readiness proof, and final review artifacts.

- [X] T054 Run all focused Feature 168 tests, including the `SkillParity` surface-baseline drift assertion, with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter "Feature168"` and record output in `specs/168-skill-parity-evidence/readiness/feature168-tests.md`
- [X] T055 Run repository parity report generation with `dotnet fsi scripts/check-agent-skill-parity.fsx --out specs/168-skill-parity-evidence/readiness/parity --report docs/reports/skills-parity.md --summary-json specs/168-skill-parity-evidence/readiness/skill-parity-summary.json --fail-on high --json` and record caveats in `specs/168-skill-parity-evidence/readiness/skill-parity-report.md`
- [X] T056 Run the rendering-harness validation lane with `dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out specs/168-skill-parity-evidence/readiness/lanes` and record accepted, blocked, canceled, timed-out, skipped, substitute, synthetic, or environment-limited status in `specs/168-skill-parity-evidence/readiness/validation-log.md`
- [X] T057 Run `dotnet fsi scripts/check-agent-skill-parity.fsx --list-rules` and record the seven required rule themes in `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`
- [X] T058 Run `git check-ignore -v specs/168-skill-parity-evidence/readiness/skill-parity-report.md || true` and `git status --short specs/168-skill-parity-evidence/readiness docs/reports/skills-parity.md` and record commit-visibility proof in `specs/168-skill-parity-evidence/readiness/validation-log.md`
- [X] T059 Update final compatibility and scope notes confirming no public `FS.GG.UI.*` runtime package behavior changed in `specs/168-skill-parity-evidence/readiness/validation-log.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup completion and blocks user story implementation
- **User Stories (Phase 3+)**: Depend on Foundational completion
- **Polish (Phase 7)**: Depends on the desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1, MVP)**: Can start after Foundational. It is the minimum useful guidance update slice.
- **User Story 2 (P1)**: Can start after Foundational. The structural drift checks are independent; the guidance-gap fixture uses the rule coverage helpers from US1 when both P1 stories are implemented together.
- **User Story 3 (P2)**: Can start after US1 because it deepens the visual and responsiveness guidance themes added there.
- **User Story 4 (P3)**: Can start after US2 and benefits from US1/US3 coverage data so the report can include real guidance-rule coverage.

### Within Each User Story

- Tests must be written and observed failing before implementation tasks in that story.
- `.fsi` signatures, FSI authoring evidence, and surface-baseline evidence precede `.fs` implementations.
- Skill guidance edits happen only after `specs/168-skill-parity-evidence/readiness/surface-inventory.md` identifies canonical sources and wrapper surfaces.
- Do not run two `dotnet test` invocations for the same project and configuration concurrently unless their output paths are explicitly isolated.

---

## Parallel Opportunities

- Setup guides T002, T003, T004, and T006 can run in parallel after T001 is understood.
- Foundational fixture/script work T012 and T014 can run in parallel with T008/T009 if compile-order edits are coordinated.
- US1 skill updates T019, T020, T021, T022, and T023 touch different skill files and can run in parallel after T017.
- US2 test files T027 and T028 can be authored in parallel before the shared fsproj registration T029.
- US3 guidance updates T041 and T042 can run in parallel after T039.
- US4 report tests T045 can be authored independently before T046 registers the file.

## Parallel Example: User Story 1

```text
Task: "Update package-owned testing guidance with readiness allowlisting, validation output isolation, and evidence caveats in src/Testing/skill/SKILL.md"
Task: "Update generated-product testing guidance with package-feed proof, readiness allowlisting, validation isolation, and evidence caveats in template/product-skills/fs-gg-testing/SKILL.md"
Task: "Update sample-pack guidance with current FS.GG.UI.* package pin checks, scripts/refresh-local-feed-and-samples.fsx proof workflow references, and scripts/run-validation-lanes.fsx validation-lane expectations in template/fragments/samples/skill/SKILL.md"
Task: "Update repository package guidance for package-consuming samples, package-feed proof, validation-lane workflow references, responsiveness evidence, and validation caveats in src/Controls/skill/SKILL.md and src/SkiaViewer/skill/SKILL.md"
```

## Parallel Example: User Story 2

```text
Task: "Create failing surface inventory, SkillParity surface-baseline drift, and wrapper target resolution tests in tests/Rendering.Harness.Tests/Feature168SkillInventoryTests.fs"
Task: "Create failing parity finding and non-destructive behavior tests in tests/Rendering.Harness.Tests/Feature168ParityFindingTests.fs"
```

## Parallel Example: User Story 3

```text
Task: "Update responsiveness guidance in src/SkiaViewer/skill/SKILL.md and template/product-skills/fs-gg-skiaviewer/SKILL.md"
Task: "Update controls and widget guidance in src/Controls/skill/SKILL.md, template/fragments/controls/skill/SKILL.md, and template/product-skills/fs-gg-ui-widgets/SKILL.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Stop and validate updated guidance through `Feature168&Guidance` tests and manual skill inspection.
4. Record guidance coverage in `specs/168-skill-parity-evidence/readiness/guidance-coverage.md`.

### Incremental Delivery

1. Add US1 guidance updates and coverage tests.
2. Add US2 non-destructive parity checker and fixture detection.
3. Add US3 visual/responsiveness evidence honesty refinements.
4. Add US4 report generation and reviewer-readable summaries.
5. Run Phase 7 validation and preserve all caveats visibly.

### Parallel Team Strategy

1. One contributor owns `tests/Rendering.Harness/SkillParity.fsi` and `tests/Rendering.Harness/SkillParity.fs`.
2. One contributor owns guidance updates under `src/*/skill` and `template/**/SKILL.md`.
3. One contributor owns wrapper/command skill parity under `.agents/skills` and `.claude/skills`.
4. One contributor owns report output and readiness evidence under `docs/reports/skills-parity.md` and `specs/168-skill-parity-evidence/readiness/`.

## Notes

- Fixture and dry-run data are synthetic evidence and must be marked as such in test names, comments, and readiness records.
- The checker is non-destructive by default; no automatic wrapper repair belongs in this task list.
- Global machine-local Codex skill installs may be listed as excluded external surfaces, but repository parity must be reproducible from this checkout.
- Wrapper files should remain short route pointers unless the task explicitly updates command-skill guidance.
