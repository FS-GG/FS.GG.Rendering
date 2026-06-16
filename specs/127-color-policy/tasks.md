---
description: "Task list for Color Validation Policies (wcag / ant) — Workstream F2"
---

# Tasks: Color Validation Policies (wcag / ant)

**Input**: Design documents from `/specs/127-color-policy/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: INCLUDED — the spec's Success Criteria (SC-001…SC-006) and Constitution Principle V (Test evidence) make the semantic tests the primary deliverable surface for this internal-first slice. Tests are authored failing-first, then greened.

**Organization**: Tasks grouped by user story (P1 → P3) for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). Engine lands as one internal `.fs` in `src/Color/`; catalog + evaluation + tests live in `tests/Controls.Tests/`; committed reports under `docs/reports/`; optional regenerator under `scripts/`. No new project.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Wire the new internal module and test file into the build with zero public-surface delta.

- [X] T001 Add `<Compile Include="ColorPolicy.fs" />` (after `Contrast.fs` — the only load-bearing constraint, since `ColorPolicy` reuses `Contrast`; place it **last, after `Palettes.fs`**, matching the plan's source tree in `plan.md`) and `<InternalsVisibleTo Include="Controls.Tests" />` to `src/Color/Color.fsproj`. Confirm NO `.fsi` is added for `ColorPolicy`.
- [X] T002 Create skeleton `src/Color/ColorPolicy.fs`: `namespace FS.GG.UI.Color`, `module internal ColorPolicy`, `open`s for the in-assembly `Contrast`/`Role`/`Verdict`/`Color`/`ContrastResult`. No top-level access modifiers on bindings (avoids FS0078-as-error). File must compile empty.
- [X] T003 Register `Feature127ColorPolicyTests.fs` in `tests/Controls.Tests/Controls.Tests.fsproj` (in the correct compile order before the test entry point) and create `tests/Controls.Tests/Feature127ColorPolicyTests.fs` with an empty Expecto `testList "Feature127"` and module `open`s (`FS.GG.UI.Color`, `FS.GG.UI.DesignSystem`, Expecto). Confirm the suite discovers/runs the empty list.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Engine types and shared evaluation machinery that ALL user stories build on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Define the core types in `src/Color/ColorPolicy.fs` per `contracts/color-policy-contract.md`: `Authority` (`WcagCertified` | `AntExpectation`), `PolicyOutcome` (`Passed` | `Failed` | `OutOfScope` | `Indeterminate`), `ColorPolicy` record (`Name`/`Label`/`Authority`/`Threshold: Role -> float`/`Classify: Role -> float -> Verdict`), `Pairing` record (`Name`/`Foreground`/`Background`/`Role`), `PairingResult` record (`Pairing`/`Measured`/`Threshold: float option`/`Outcome`/`Verdict`/`AuthorityNote: string option`).
- [X] T005 Implement the shared evaluation functions in `src/Color/ColorPolicy.fs`: `inScope`, `evaluatePairing` (composite a semi-transparent foreground over its background via `Contrast.compositeOver` BEFORE measuring with `Contrast.ratio`; `nan` measured + `Indeterminate` for non-solid/unmeasurable; `OutOfScope` when `inScope = false`, never `Passed`), `evaluate` (catalog order preserved), and `overall` (pass = no `Failed` rows; `OutOfScope`/`Indeterminate` listed but not counted as fail).

**Checkpoint**: Engine types + evaluation compile; user stories can now proceed.

---

## Phase 3: User Story 1 - Validate colors against a named policy (Priority: P1) 🎯 MVP

**Goal**: A selectable rule set evaluated over the design system's pairings, with `wcag` reproducing today's contrast behavior byte-for-byte and applied by default; unknown names rejected explicitly.

**Independent Test**: Evaluate the default (`wcag`) policy over the current design-system pairings and assert every verdict matches `Contrast.check` byte-for-byte (roles, thresholds, pass/fail).

### Tests for User Story 1 (write FIRST, ensure they FAIL) ⚠️

- [X] T006 [P] [US1] Parity test (FR-002/SC-001) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: for every catalog pairing, `(ColorPolicy.evaluatePairing ColorPolicy.wcag p).Verdict` equals `(Contrast.check p.Role p.Background p.Foreground).Verdict` byte-for-byte; assert `wcag.Classify` delegates to `Contrast.verdict` (not a re-implemented copy).
- [X] T007 [P] [US1] Default-policy test (FR-003) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: assert `ColorPolicy.defaultPolicy = ColorPolicy.wcag`.
- [X] T008 [P] [US1] Unknown-name rejection test (FR-006/SC-005) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: `byName "material"`, `byName "Wcag"`, `byName ""` each return `Error _` (never `Ok` of another policy).
- [X] T028 [P] [US1] Overall-summary test (FR-007) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: assert `ColorPolicy.overall` returns `false` for a catalog containing ≥1 `Failed` row and `true` when no row is `Failed` (`OutOfScope`/`Indeterminate` present but not counted as fail); assert the rendered report's overall summary line reports the correct failing / out-of-scope / indeterminate counts. Closes the FR-007 "overall pass/fail summary" gate. Greened by T005's `overall` + T021's summary line.
- [X] T029 [P] [US1] Alpha + Indeterminate test (edge cases; `color-policy-contract.md` behavioral guarantee #8) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: (a) a pairing with a semi-transparent `Foreground` yields `Measured` equal to the ratio of the explicitly `Contrast.compositeOver`-composited color (alpha never ignored, computed before measurement); (b) a non-solid / unmeasurable pairing yields `Outcome = Indeterminate` with `Measured = nan`. Greened by T005's `evaluatePairing`.

### Implementation for User Story 1

- [X] T009 [US1] Implement the `wcag` policy and `defaultPolicy` in `src/Color/ColorPolicy.fs`: `Authority = WcagCertified`; `Classify` delegates directly to `Contrast.verdict` (Text 7.0/4.5/3.0, GraphicOrUi 3.0, Decorative exempt) so it stays byte-identical; `defaultPolicy = wcag`.
- [X] T010 [US1] Implement `byName : string -> Result<ColorPolicy,string>` in `src/Color/ColorPolicy.fs`: returns `wcag`/`ant` for exact lowercase names, explicit `Error` (clear message) otherwise — no silent fallback. (`ant` value wired in US2; reference it as the second known name.)
- [X] T011 [US1] Build the `wcag` pairing catalog in `tests/Controls.Tests/Feature127ColorPolicyTests.fs` from `DesignTokens` (public primitives). Enumerate the core pairings — e.g. `text-on-canvas` (`Text`), `text-on-surface` (`Text`), `muted-text-on-canvas` (`Text`), `ui-foreground-on-surface` (`GraphicOrUi`), `ui-border-on-surface` (`GraphicOrUi`) — each with a stable `Name`/`Foreground`/`Background`/`Role`. The exact set is the author's choice, constrained only by the FR-002 parity requirement: every entry's `wcag` verdict MUST match `Contrast.check`. Greens T006–T008, T028, T029.

**Checkpoint**: `wcag` is selectable, default, byte-identical to today; unknown names rejected. US1 independently testable.

---

## Phase 4: User Story 2 - Hold colors to Ant's rules with the `ant` policy (Priority: P2)

**Goal**: A second, genuinely different policy (`ant`) whose distinct thresholds reach a different verdict on ≥1 shared pairing, covering all Ant semantic families, with out-of-scope and no-overclaim disclosure.

**Independent Test**: Evaluate `ant` over the Ant-derived families; assert it applies Ant thresholds (not WCAG's) and ≥1 shared pairing differs from its `wcag` verdict with identical color inputs.

### Tests for User Story 2 (write FIRST, ensure they FAIL) ⚠️

- [X] T012 [P] [US2] Divergence test (FR-005/SC-002) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: ≥1 catalog pairing has a different `Outcome`/`Verdict` under `ant` vs `wcag` with identical colors; the test names the diverging pairing (difference attributable to policy, not color).
- [X] T013 [P] [US2] Ant-families-covered test (FR-004/SC-003) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: evaluating `ant` over the catalog yields a `PairingResult` (threshold + measured + verdict) for each of primary, success, warning, error, info, text-on-surface.
- [X] T014 [P] [US2] Out-of-scope test (FR-011) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: the named exemplar `decorative-hairline-on-surface` (added in T017, with `inScope ant p = false` — not in `ant`'s validated set) evaluates to `Outcome = OutOfScope` (never `Passed`).
- [X] T015 [P] [US2] No-overclaim test (FR-010) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: an `ant` pairing that WCAG would `Fail` carries `AuthorityNote = Some _`.

### Implementation for User Story 2

- [X] T016 [US2] Implement the `ant` policy in `src/Color/ColorPolicy.fs`: `Authority = AntExpectation`; an authored `Role -> float` threshold table (F# literals with provenance comments tracing to the FS-Skia-UI Ant adoption analysis; `FS.Skia.UI.*` → `FS.GG.UI.*`), values deliberately distinct from WCAG's 7.0/4.5/3.0 gates so ≥1 shared pairing changes verdict; `Classify` maps ratio vs threshold to a `Verdict`; set `AuthorityNote` when certifying a WCAG-failing pairing. Once the numbers are fixed, record the final `Role -> float` table AND the name of the expected divergent pairing back into `contracts/color-policy-contract.md` (replacing the illustrative table) so the unit test and the contract cannot drift (resolves U1).
- [X] T017 [US2] Extend the catalog in `tests/Controls.Tests/Feature127ColorPolicyTests.fs` with the Ant semantic families (primary/success/warning/error/info/text-on-surface) sourced from `DesignTokensExt` (F1 internal Ant families via IVT), plus the named out-of-scope exemplar `decorative-hairline-on-surface` (a pairing deliberately NOT in `ant`'s validated set, `inScope ant = false`) for T014. Greens T012–T015.

**Checkpoint**: Both `wcag` and `ant` selectable; `ant` proven a rule set (not a palette swap); disclosure honest. US1 + US2 independently testable.

---

## Phase 5: User Story 3 - Generate a policy report as documentation and evidence (Priority: P3)

**Goal**: A deterministic, drift-checked report per policy enumerating every validated pairing (rule, measured, verdict) + summary, regenerable on demand, never silently diverging from the rules.

**Independent Test**: Generate both reports; assert one row per validated pairing with rule/measured/verdict; run the drift check (passes on match, fails on tamper) and idempotency (two renders byte-identical).

### Tests for User Story 3 (write FIRST, ensure they FAIL) ⚠️

- [X] T018 [P] [US3] Idempotency test (SC-004) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: `renderReport` called twice on identical (policy, catalog) inputs returns byte-identical strings.
- [X] T019 [P] [US3] Completeness test (FR-008/SC-003) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: each rendered report has one row per validated pairing in fixed catalog order; the `ant` report includes primary/success/warning/error/info/text-on-surface rows each with rule + measured + verdict; out-of-scope rows disclosed (not shown as pass).
- [X] T020 [US3] Drift-gate + tamper test (FR-009/SC-004) in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: re-render both reports from current code/tokens and assert byte-equality with the committed `docs/reports/color-policy-{wcag,ant}.md`; on mismatch the test names the divergent file (tamper detection covered by this byte-equality assertion). Uses a repo-root locator (per `Package.Tests` precedent).

### Implementation for User Story 3

- [X] T021 [US3] Implement `renderReport : ColorPolicy -> Pairing list -> string` in `src/Color/ColorPolicy.fs` per `contracts/policy-report-contract.md`: static "GENERATED — do not edit; regenerate via …" + authority header, one row per pairing (name, fg/bg as lowercase `#rrggbb`/`#rrggbbaa`, role, `Measured`/`Threshold` at fixed `F2` invariant-culture precision, verdict/outcome, authority note), overall summary; `\n` line endings; no clock/random/culture-sensitive content. Single evaluator shared with the tests.
- [X] T022 [US3] Add env-gated regeneration in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`: when `UPDATE_POLICY_REPORTS=1`, write both `docs/reports/color-policy-{wcag,ant}.md` via the same `renderReport` evaluator (repo-relative path); unset run verifies via T020.
- [X] T023 [US3] Generate and commit the two report artifacts `docs/reports/color-policy-wcag.md` and `docs/reports/color-policy-ant.md` by running the suite with `UPDATE_POLICY_REPORTS=1`; verify a subsequent unset run passes the drift gate. Greens T018–T020.
- [X] T024 [P] [US3] (Optional) Add `scripts/generate-policy-report.fsx` that `#r`s built `Color.dll`/`DesignSystem.dll` and calls `renderReport`. VALIDATE its ability to reach `module internal ColorPolicy` from the `dotnet fsi` dynamic assembly on net10 (Research R3); if `InternalsVisibleTo` is fragile, the script documents/delegates to the `UPDATE_POLICY_REPORTS=1` test path instead of re-implementing evaluation.

**Checkpoint**: All three stories independently functional; reports committed and drift-gated.

---

## Phase 6: Polish & Cross-Cutting Concerns (Neutrality Gate)

**Purpose**: Prove the behaviour- and surface-neutral landing (FR-012/SC-006).

- [X] T025 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` and assert `git diff --quiet -- tests/surface-baselines` (zero public-surface delta; no new public rows; no `.fsi` touched).
- [X] T026 Run the full suite and confirm existing render/gallery output and pass/skip counts are unchanged (no new skips): `dotnet test FS.GG.Rendering.slnx -c Debug` (GL tier optional; F2 itself needs no GL). This run also exercises F1's forbidden-package guard, which transitively verifies FR-013 (no React/DOM/web/icon-font dependency was added).
- [X] T027 Execute the `quickstart.md` runbook end-to-end and confirm all five SC checks pass, the drift gate detects tampering, and surface baselines show zero delta.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001/T002/T003 are sequential-ish (T002 needs T001's compile entry; T003 is independent of both).
- **Foundational (Phase 2)**: Depends on Setup. T004 → T005. BLOCKS all user stories.
- **User Stories (Phase 3–5)**: All depend on Foundational. Proceed in priority order (P1 → P2 → P3) or in parallel if staffed — but note the shared-file couplings below.
- **Polish (Phase 6)**: Depends on all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent after Foundational. Delivers the MVP (selectable policy, wcag default, parity, rejection).
- **US2 (P2)**: Logically independent and independently testable, but T010 (`byName`) references the `ant` value created in T016, and T017 extends the same catalog T011 created — coordinate edits to `ColorPolicy.fs` and the test file.
- **US3 (P3)**: Depends on US1/US2 producing verdicts to report; `renderReport` (T021) consumes the catalog from US1/US2.

### Within Each User Story

- Tests (T006–T008, T028, T029, T012–T015, T018–T020) are written FIRST and must FAIL before implementation.
- Engine bindings in `ColorPolicy.fs` before catalog/test wiring.
- Reports rendered (T021) before committed (T023) before drift-gated (T020 greens).

### Parallel Opportunities

- US1 tests T006/T007/T008/T028/T029 are [P] (assertions only, same file — author together, but logically independent).
- US2 tests T012–T015 are [P]; US3 tests T018/T019 are [P] (T020 depends on committed reports).
- T024 (optional fsx) is [P] — separate file.
- Cross-story: US1 and US2 can be developed in parallel by different people IF edits to `src/Color/ColorPolicy.fs` and the single test file are coordinated (they share both files).

> **Note on [P] within one file**: T006–T008, T028, T029 (and T012–T015, T018–T019) all edit `Feature127ColorPolicyTests.fs`. They are marked [P] because they are independent *test additions*; if one developer owns the file, append them sequentially to avoid merge conflicts.

---

## Parallel Example: User Story 1

```bash
# Author the three US1 tests together (all in Feature127ColorPolicyTests.fs), failing-first:
Task: "Parity test: wcag verdict == Contrast.check verdict, byte-for-byte"
Task: "Default-policy test: defaultPolicy = wcag"
Task: "Unknown-name rejection test: byName material/Wcag/'' -> Error"

# Then implement the engine bindings (src/Color/ColorPolicy.fs) and catalog (test file) to green them.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (wire `ColorPolicy.fs` + IVT + test file).
2. Phase 2: Foundational (types + evaluation) — CRITICAL, blocks all stories.
3. Phase 3: US1 (wcag, default, byName, catalog, parity/rejection tests).
4. **STOP and VALIDATE**: `dotnet test … --filter "Feature127"` — wcag ≡ today, default = wcag, unknown rejected.
5. This alone is the backward-compatible policy mechanism (SC-001/SC-005).

### Incremental Delivery

1. Setup + Foundational → engine ready.
2. US1 → test → MVP (selectable wcag, byte-identical default).
3. US2 → test → `ant` proves rule-set divergence (SC-002/SC-003).
4. US3 → test → committed, drift-gated reports (SC-004).
5. Polish → neutrality gate green (SC-006).

---

## Notes

- [P] = independent work; within the single test file, append sequentially if one owner.
- Constitution: `ColorPolicy` is `module internal`, NO `.fsi`; no top-level access modifiers; no public surface added (deferred to F5).
- One evaluator: tests and the committed report both go through `renderReport` — no second recompute path.
- Verify tests fail before implementing; commit after each task or logical group.
- Hard gate: zero surface-baseline delta, unchanged pass/skip counts, behaviour-neutral under default `wcag`.
