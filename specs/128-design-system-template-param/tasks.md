---
description: "Task list for Design-System Template Parameter (--designSystem wcag|ant) — Workstream F3"
---

# Tasks: Design-System Template Parameter (`--designSystem wcag|ant`) — Workstream F3

**Input**: Design documents from `/specs/128-design-system-template-param/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓ (template-parameter + generated-product-validation), quickstart.md ✓

**Tests**: INCLUDED. The always-on gate test (`Feature128DesignSystemTemplateTests`) is a named feature deliverable (plan §Testing; validation contract Layer 1) and is authored failing-first (GV-8, Principle V). No other test scaffolding is added.

**Organization**: Tasks are grouped by user story (US1=P1, US2=P2, US3=P3) so each story is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`) + a dotnet project template. Key locations:

- Template contract: `.template.config/template.json`
- Template content: `template/` (base + overlays; `feedback/` is the no-diff precedent)
- New overlay: `template/design-system/ant/`
- Verdict oracle (reused, not duplicated): `docs/reports/color-policy-{wcag,ant}.md`
- Live regenerator script: `scripts/validate-design-system-template.fsx`
- Committed report: `specs/128-design-system-template-param/readiness/design-system-template-validation.md`
- Always-on gate test: `tests/Package.Tests/Feature128DesignSystemTemplateTests.fs` (registered in `tests/Package.Tests/Package.Tests.fsproj`)
- Surface baselines (must stay zero-delta): `tests/surface-baselines/*.txt`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the F1/F2 foundations this feature wires into the template are in place and green.

- [X] T001 Verify the solution builds clean: run `dotnet build FS.GG.Rendering.slnx` and confirm green (baseline before any change).
- [X] T002 Confirm the verdict oracle exists and is drift-gated: `docs/reports/color-policy-wcag.md` (overall **FAIL**) and `docs/reports/color-policy-ant.md` (overall **PASS**, no-overclaim note) are present and pass their existing drift gate; record current `overall`/`authority`/divergent-pairing tokens for reuse (no new color values introduced — assumption "reuse of F1/F2").
- [X] T003 Confirm baseline template scaffolds today: install the template (`dotnet new install .`) and run `dotnet new fs-gg-ui --name Demo -o /tmp/demo-baseline` to capture the current default output as the byte-identical reference for SC-001.

**Checkpoint**: Foundations confirmed; the oracle and the byte-identical reference scaffold are available.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared scaffolding-side and validation-side groundwork needed before any user story; introduces no behavior on the default path.

**⚠️ CRITICAL**: No user-story work proceeds until this phase is complete.

- [X] T004 Create the `readiness/` directory for the committed validation report at `specs/128-design-system-template-param/readiness/` (artifact location referenced by both the regenerator and the gate test).
- [X] T005 Confirm zero-public-surface posture upfront: capture current `tests/surface-baselines/*.txt` as the reference and confirm `ColorPolicy`/`DesignTokensExt` remain `internal` with no `.fsi` change planned (FR-011/SC-007 guardrail for every later task).

**Checkpoint**: Foundation ready — user-story implementation can begin.

---

## Phase 3: User Story 1 - Scaffold a product with a chosen design-system policy (Priority: P1) 🎯 MVP

**Goal**: Add the `designSystem` choice parameter (`wcag` default, `ant`) to the project template so a maintainer can declare the governing color policy at scaffold time; the default path stays byte-identical to today, `ant` records its policy + carries the imprint, and unknown values are rejected.

**Independent Test**: `dotnet new fs-gg-ui` (no value) and `--designSystem wcag` produce byte-identical trees equal to today's output (no new files); `--designSystem ant` produces a project containing `design-system.json` with `policy:"ant"`; `--designSystem material` is rejected listing the accepted set. (Quickstart Scenarios 1–3.)

### Implementation for User Story 1

- [X] T006 [US1] Add the `designSystem` `choice` parameter symbol to `.template.config/template.json` (`defaultValue: "wcag"`, choices `wcag`/`ant` with descriptions, per `contracts/template-parameter-contract.md`); model it on the existing `profile` choice. Edit **only** the `symbols` block — do not touch `template/base/`.
- [X] T007 [US1] Add the conditional `sources` entry to `.template.config/template.json` firing **only** for `ant`: `{ "condition": "(designSystem == \"ant\")", "source": "template/design-system/ant/", "target": "./" }` (the `feedback` no-diff precedent; no entry fires for `wcag`).
- [X] T008 [P] [US1] Create the self-describing record `template/design-system/ant/design-system.json` with `{ "policy": "ant", "authority": "AntExpectation" }` (FR-005/SC-002; data-model E2).
- [X] T009 [P] [US1] Create the Ant imprint as committed data `template/design-system/ant/docs/reports/color-policy-ant.md` by reusing F2's `docs/reports/color-policy-ant.md` verbatim (FR-004); add a PROVENANCE note that the imprint reuses F1/F2 (no new color values; identifiers `FS.GG.UI.*`).
- [X] T010 [US1] Verify TP-2 (default no-op): re-run `dotnet new install .`, scaffold `-o /tmp/demo-default` (no value) and `--designSystem wcag -o /tmp/demo-wcag`, and `diff -r /tmp/demo-default /tmp/demo-baseline` and `/tmp/demo-wcag` ⇒ **no differences** (SC-001). Fix the overlay/condition if any diff leaks onto the default path.
- [X] T011 [US1] Verify TP-3 (`ant` record): scaffold `--designSystem ant -o /tmp/demo-ant`; assert `/tmp/demo-ant/design-system.json` has `policy:"ant"` and `/tmp/demo-ant/docs/reports/color-policy-ant.md` exists (Quickstart Scenario 2).
- [X] T012 [US1] Verify TP-4 (rejection): scaffold `--designSystem material` ⇒ non-zero exit, accepted set (`wcag`,`ant`) surfaced, nothing generated (Quickstart Scenario 3 / FR-007/SC-005). Also verify TP-5 (casing) against the **observed** `profile` behavior — confirm a case-variant of a real value (e.g. `--designSystem Ant`) resolves the same way `profile` resolves its own case-variants (do **not** assume case-sensitivity; match the established convention), so the rejection assertion targets only genuinely-unknown tokens.

**Checkpoint**: The parameter is live; default is byte-identical to today, `ant` records its policy + imprint, unknown values are rejected. US1 is independently demoable (MVP).

---

## Phase 4: User Story 2 - A generated product validates its colors against the selected policy (Priority: P2)

**Goal**: Prove the recorded choice selects *policy*, not a palette — resolve a product's recorded policy and govern its colors with that policy framework-side (the generated product itself does not call the internal engine in F3): `wcag` reaches today's verdicts, `ant` passes the Ant pairings, and ≥1 pairing diverges by policy with the correct disclosed authority.

**Independent Test**: For each recorded policy string, `ColorPolicy.byName <recorded> |> evaluate catalog |> overall/renderReport` matches the committed `docs/reports/color-policy-<v>.md` oracle; the `primary-hover-fg-on-surface` pairing is `Fail` under `wcag` and `Aa` under `ant`; the `ant` certify-where-WCAG-fails verdict carries Ant authority (no-overclaim).

### Implementation for User Story 2

- [X] T013 [US2] Create `scripts/validate-design-system-template.fsx` and add the **policy-verdict** core: load the design-system `Pairing` catalog and, for a given recorded policy string, run `ColorPolicy.byName <recorded> |> evaluate catalog`, computing `overall`, `authority`, and `renderReport`. Reach the `internal` F2 engine by `#load`-ing the F2 source closure (`src/Color/ColorPolicy.fs` + the design-system `Pairing` catalog source) into the script so it compiles into the fsx's **own assembly** (same-assembly access — an `fsx` cannot use the `Controls.Tests` IVT grant), per data-model "Reused F2 entities".
- [X] T014 [US2] In `scripts/validate-design-system-template.fsx`, compare the computed verdicts against the committed oracle `docs/reports/color-policy-<v>.md` for each value (single source of truth — no re-derivation): assert `wcag` ⇒ `overall=FAIL`/`authority=WcagCertified`, `ant` ⇒ `overall=PASS`/`authority=AntExpectation`.
- [X] T015 [US2] In `scripts/validate-design-system-template.fsx`, assert the divergence + no-overclaim: the `primary-hover-fg-on-surface` pairing is `Fail` under `wcag` and `Aa` under `ant` (SC-004/FR-006), and `ant`'s certify-where-WCAG-fails verdict carries the Ant `AuthorityNote` "not WCAG-certified" (FR-010), reusing F2's note verbatim.

**Checkpoint**: The verdict logic proves policy-not-palette over the recorded choice; ready to be wrapped in the full per-value scaffold→build→report loop.

---

## Phase 5: User Story 3 - Validate that both generated variants build and govern correctly (Priority: P3)

**Goal**: A repeatable, coverage-exhaustive check that every accepted `designSystem` value scaffolds, really builds, and governs per its policy — surfaced as a committed report and guarded by an always-on test, with zero public-surface delta.

**Independent Test**: `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx` processes every choice (scaffold + real `dotnet build` + verdict) and writes the report; the gate test passes asserting coverage/build/no-diff/record/divergence/no-overclaim; surface baselines show zero delta (Quickstart Scenarios 4–5).

### Tests for User Story 3 (failing-first) ⚠️

> Author the gate test BEFORE the regenerator writes the report — with the report absent it MUST fail (GV-8).

- [X] T016 [US3] Create the always-on gate test `tests/Package.Tests/Feature128DesignSystemTemplateTests.fs` (Expecto; deterministic, GL-free, no `dotnet new`) asserting the committed report `specs/128-design-system-template-param/readiness/design-system-template-validation.md`: GV-1 `covered-values: wcag, ant`, GV-2 every value `build=pass`, GV-3 `wcag diff-vs-today=none overall=FAIL authority=WcagCertified`, GV-4 `ant record=ant overall=PASS authority=AntExpectation`, GV-5 `divergent-pairing: primary-hover-fg-on-surface wcag=Fail ant=Aa`, GV-6 `no-overclaim-note: ant: not WCAG-certified`, GV-7 `result: pass`, GV-8 report-missing ⇒ fail.
- [X] T017 [US3] Register the new test file in `tests/Package.Tests/Package.Tests.fsproj` (compile order) and run the suite to confirm `Feature128DesignSystemTemplateTests` **fails** (report not yet generated) — failing-first evidence (Principle V).

### Implementation for User Story 3

- [X] T018 [US3] Add the env-gate + per-value loop to `scripts/validate-design-system-template.fsx`: gated by `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1` (the `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE` precedent); enumerate the `designSystem` choice set from `.template.config/template.json` as the coverage source (FR-009/SC-006/TP-7) and fail loudly if any accepted value goes unprocessed.
- [X] T019 [US3] In the loop, for each value `v`: `dotnet new fs-gg-ui --name <Tmp> --designSystem <v> -o <tmpdir>` (must succeed); if `wcag` byte-compare the tree to a no-value scaffold ⇒ `diff-vs-today=none`; if `ant` assert `design-system.json` `policy=="ant"` (validation contract Layer 2 steps 1–2).
- [X] T020 [US3] In the loop, run a real `dotnet build` on each scaffold ⇒ success recorded as `build=pass` (FR-008, assumption "real build"); wire in the US2 verdict-comparison (T013–T015) for each value's recorded policy.
- [X] T021 [US3] Have the script write `specs/128-design-system-template-param/readiness/design-system-template-validation.md` deterministically (sorted value order, invariant culture, no wall-clock content) with the exact contract tokens from `generated-product-validation-contract.md` Layer 1.
- [X] T022 [US3] Run `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx`, commit the generated report, and re-run the suite to confirm `Feature128DesignSystemTemplateTests` now **passes** (green-after-regenerator).

**Checkpoint**: Every accepted value is scaffolded, built, and governed; the committed report is asserted by an always-on gate.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Neutrality proof, provenance, and full-runbook validation across all stories.

- [X] T023 Confirm zero public-surface delta: `dotnet fsi scripts/refresh-surface-baselines.fsx --check` ⇒ no delta; confirm no `.fsi` edited and no new public rows (FR-011/SC-007).
- [X] T024 Confirm behavior-neutrality: existing suite pass/skip counts unchanged and rendered/gallery output unchanged (SC-007); no React/DOM/web/icon-font dependency added by the overlay (FR-012/TP-8).
- [X] T025 [P] Record PROVENANCE for the F3 adoption (Ant rules trace to archived `EHotwagner/FS-Skia-UI`, identifiers rebranded `FS.Skia.UI.* → FS.GG.UI.*`) per the cross-cutting rules.
- [X] T026 Run the full quickstart runbook (`quickstart.md` Scenarios 1–5) end-to-end and confirm "Done when" holds: scenarios pass, `covered-values` equals the choice set, zero surface-baseline delta, unchanged counts.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3–5)**: All depend on Foundational. Here they are **sequential by priority** because US2 validates the choice US1 imprints, and US3 wraps US2's verdict logic + asserts the report — US1 → US2 → US3.
- **Polish (Phase 6)**: Depends on all user stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent — delivers the scaffolding parameter end-to-end (MVP).
- **US2 (P2)**: Builds on US1's recorded choice (resolves/governs the recorded policy). Independently testable via the recorded policy string against the oracle.
- **US3 (P3)**: Builds on US1 (scaffolds to validate) and US2 (verdict logic reused in the loop); adds coverage, build, report, and the gate test.

### Within Each User Story

- US1: parameter symbol (T006) + conditional source (T007) gate the overlay; overlay files (T008, T009) are [P]; verifications (T010–T012) follow.
- US2: T013 → T014 → T015 are sequential (same script file, `scripts/validate-design-system-template.fsx`).
- US3: gate test failing-first (T016, T017) **before** the regenerator (T018–T021) and the greening run (T022).

### Parallel Opportunities

- US1: T008 and T009 (different overlay files) can run in parallel after T007.
- Polish: T025 (provenance doc) is [P] vs the verification tasks.
- Note: all `scripts/validate-design-system-template.fsx` tasks (T013–T015, T018–T021) touch one file — keep sequential, not parallel.

---

## Parallel Example: User Story 1

```bash
# After T006 (symbol) + T007 (conditional source), create the two overlay files together:
Task: "Create template/design-system/ant/design-system.json with { policy: ant, authority: AntExpectation }"
Task: "Create template/design-system/ant/docs/reports/color-policy-ant.md from F2's report (with PROVENANCE)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup → 2. Phase 2: Foundational → 3. Phase 3: US1.
4. **STOP and VALIDATE**: default byte-identical (SC-001), `ant` records policy (SC-002), unknown rejected (SC-005).
5. Ship the scaffolding choice as the MVP.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → test independently → MVP (the parameter).
3. US2 → test independently → policy-not-palette proven over the recorded choice.
4. US3 → test independently → coverage-exhaustive build+govern validation, always-on gate.
5. Polish → neutrality + provenance + full runbook.

---

## Notes

- [P] = different files, no incomplete dependencies; [Story] maps each task to US1/US2/US3 for traceability.
- The default (`wcag`/no-value) path must stay byte-identical to today — edit **no** base file; record only on the `ant` path (the `feedback` precedent).
- Reuse F1/F2 verbatim (no new color values/rules) and the `docs/reports/` oracle (single source of truth).
- Keep `ColorPolicy`/`DesignTokensExt` `internal`; zero `.fsi` edits; zero surface-baseline delta.
- The gate test is authored failing-first; the env-gated regenerator does the heavy live `dotnet new`+`dotnet build` work.
