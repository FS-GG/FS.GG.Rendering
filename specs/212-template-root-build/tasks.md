---
description: "Task list for Feature 212 — Root-buildable generated products"
---

# Tasks: Root-buildable generated products (template emits root solution + build wrapper)

**Input**: Design documents from `/specs/212-template-root-build/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/template-root-build.contract.md, quickstart.md

**Tests**: This feature has **no runtime entities and no TDD unit-test request**. Its executable evidence is
(a) the live instantiate-and-run smoke (Foundational) and (b) the release/instantiation regression gate
(US3). No separate per-story unit-test blocks are generated. Validation is via the `quickstart.md`
scenarios (A–E).

**Organization**: Tasks are grouped by user story (US1 P1 → US2 P2 → US3 P3) for independent
implementation and testing. The hypothesis-bearing root artifacts are created in Foundational because the
plan mandates a live smoke that confirms them **before** the verb wrapper / release wiring is built.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- Exact file paths are included in each task.

## Path Conventions

- Template content root: `template/base/` (the **ungated** product source — root artifacts ship for every
  profile × lifecycle × designSystem).
- Template emit/substitution wiring: `.template.config/template.json` (`sourceName = "Product"`).
- Release gate: `.github/workflows/release.yml` (`template-product-tests` job).
- Cross-repo registry: `FS-GG/.github` (separate repo, via the `cross-repo-coordination` skill).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the build environment.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project
> via the discovery-based runner so pre-existing reds are known up front and not mistaken for regressions
> at merge. The solution deliberately omits `tests/Package.Tests` (release-only) and `samples/**/*.Tests`;
> the globbing runner catches them.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/212-template-root-build/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set)
- [X] T002 [P] Confirm prerequisites in `specs/212-template-root-build/readiness/env.md`: `dotnet --version` shows a 10.0.x SDK, record that a differing default SDK (6.0.x) is also present (the `global.json` mismatch case for SC-006), and confirm the content root `template/base/` and `.template.config/template.json` exist

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the hypothesis-bearing root artifacts, wire their emission, and **prove the plan's
build hypotheses against a really-scaffolded product** before building the verb wrapper or release gate.

**⚠️ CRITICAL**: No user story (US1/US2/US3) work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's root-cause hypotheses are *behavioral
> about the build* and **unverified until the template is actually instantiated and the stock commands are
> run**: (a) a single root `.slnx` makes stock `dotnet build/test/run` resolve, and (b) headless
> `dotnet run` degrades to exit 0 via `UnsupportedEnvironment`. T006 pulls this live smoke forward — before
> US2/US3 — and must confirm or replace (a)/(b) against the real product.

- [X] T003 Build the emission/root-cause map in `specs/212-template-root-build/readiness/emission-map.md`: read the base `sources` block in `.template.config/template.json` (the `exclude`/`copyOnly` lists ~lines 131–157) and confirm the four new root files under `template/base/` will be copied with `sourceName=Product` substitution and are NOT excluded or `copyOnly`-frozen; record the verb→FAKE target table (Restore/Build/Run/Pack = new; Test/Verify = frozen) per contract C3
- [X] T004 [P] Create `template/base/Product.slnx` per contract C1/R1: a single `<Solution>` referencing `src/Product/Product.fsproj` and `tests/Product.Tests/Product.Tests.fsproj` (capitalized `Product` path segment so `sourceName` rewrites it to `<Name>`)
- [X] T005 [P] Create `template/base/global.json` per C1/R2: `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": false } }` — the **contract** is the 10.0.x band + `rollForward: latestFeature` + `allowPrerelease: false`; `10.0.100` is just the baseline value, not a hard requirement (content-neutral; no `product` token)
- [X] T006 Wire emission in `.template.config/template.json`: ensure `Product.slnx`, `global.json`, `build.sh`, `build.cmd` emit from the ungated base source with name substitution (add nothing to `exclude`; do not `copyOnly`-freeze `Product.slnx` since it needs the `Product→<Name>` rewrite) so they ship for every lifecycle (`spec-kit`/`sdd`/`none`) and designSystem (FR-008)
- [X] T007 **Early live smoke run**: `dotnet new install .`; `dotnet new fs-gg-ui --name Acme --output "$(mktemp -d)/Acme"`; at the product root run stock `dotnet restore && dotnet build && dotnet test && dotnet run --project src/Acme`; record live evidence in `specs/212-template-root-build/readiness/smoke.md` confirming (a) the single `Acme.slnx` resolves stock build/test/run and (b) headless `dotnet run` exits 0 via `UnsupportedEnvironment`. Use `--name Acme` (PascalCase — a lowercase/dir-derived name breaks the generated build). If hypothesis (a) fails, update plan.md/research.md before proceeding; if hypothesis (b) fails (headless `dotnet run` exits non-zero), switch the run assertion to the documented `tryRunEvidenceCommand` evidence-subcommand fallback (research R4) rather than treating it as a blocker, and record that decision in `smoke.md` so T017 mirrors it
- [X] T008 Confirm the seams in `specs/212-template-root-build/readiness/emission-map.md`: freeze that `Test`/`Verify` in `template/base/build.fsx` stay byte-for-byte unchanged (FR-007/SC-004) and record the FR-010 parity intent (stock root `.slnx` path builds the same project set FAKE builds — **intent only; actual parity is verified in T011**)

**Checkpoint**: Root artifacts emit, hypotheses (a)/(b) confirmed against a live scaffold, seams frozen — US1/US2/US3 can proceed.

---

## Phase 3: User Story 1 - Stock root build/test/run (Priority: P1) 🎯 MVP

**Goal**: A consumer scaffolds a product and, from the product root with only the stock .NET CLI (no FAKE),
restores, builds, tests, and runs the app profile successfully.

**Independent Test**: Scaffold into an empty dir, then from the product root run the stock restore → build →
test → run sequence (quickstart Scenario A); all succeed with no FAKE invocation.

- [X] T009 [US1] Reconcile the root `.slnx` with the pre-existing `template/base/Directory.Build.props` and `template/base/Directory.Packages.props`: confirm the root build inherits `net10.0` + lockfile policy and that nothing is duplicated or conflicting (FR-003; "existing root files" edge case)
- [X] T010 [US1] Verify name-rewrite (FR-001 / AS-5): scaffold `--name Acme` and assert `Acme.slnx` exists at the product root and references `src/Acme/Acme.fsproj` + `tests/Acme.Tests/Acme.Tests.fsproj` (and that `global.json` carries no placeholder token); record in `specs/212-template-root-build/readiness/us1.md`
- [X] T011 [US1] Verify FR-010 parity: confirm the project set built by stock `dotnet build` at the product root equals the set FAKE builds (no silent divergence); record the comparison in `specs/212-template-root-build/readiness/us1.md`
- [X] T012 [US1] Verify SDK-pin reproducibility (quickstart Scenario C / SC-006): the root build resolves the net10 band despite a differing default SDK, and a host lacking the band fails fast with an SDK-resolution error rather than a silent wrong-SDK build

**Checkpoint**: Quickstart Scenario A is green on a really-scaffolded product — US1 is independently deliverable (MVP).

---

## Phase 4: User Story 2 - Uniform verb wrapper delegating to FAKE (Priority: P2)

**Goal**: One predictable verb surface — `restore|build|test|run|verify|pack` — on both shell families,
each verb delegating to the governed FAKE path, with `verify`/`test` semantics unchanged.

**Independent Test**: In a scaffolded product, invoke each verb through the wrapper on POSIX and Windows
shells; each performs the corresponding action via FAKE, `verify` ≡ FAKE `Verify`, bogus verb is reported
(quickstart Scenario B).

- [X] T013 [P] [US2] Create `template/base/build.sh` (POSIX): map `restore|build|test|run|verify|pack` → `dotnet fsi build.fsx -t <Target>`; unknown/missing verb prints the supported-verb list and exits non-zero (mirror the existing `template/base/fake.sh` style)
- [X] T014 [P] [US2] Create `template/base/build.cmd` (Windows): parity with `build.sh` — same six verbs, same unknown/missing-verb behavior (mirror `template/base/fake.cmd`)
- [X] T015 [US2] Extend `template/base/build.fsx` `run` dispatch (~lines 210–237): add pass-through targets `Restore`, `Build`, `Run`, `Pack` that locate the single root `*.slnx` / `src` project name-agnostically and shell to stock `dotnet` (`restore`/`build`/`run --project src/<Name>`/`pack -c Release`); leave `Test` and `Verify` UNCHANGED (FR-007)
- [X] T016 [US2] Validate quickstart Scenario B on a scaffolded product: every verb routes through `dotnet fsi build.fsx -t <Target>`; `verify` ≡ FAKE `Verify` and `test` ≡ FAKE `Test` (SC-004); `./build.sh bogus` reports supported verbs + non-zero exit; both shells expose equivalent verbs (SC-003). Record in `specs/212-template-root-build/readiness/us2.md`

**Checkpoint**: Verb wrapper works on both shells and delegates to FAKE; US1 + US2 both independently functional.

---

## Phase 5: User Story 3 - Release gate proves root buildability (Priority: P3)

**Goal**: The template release process proves a freshly scaffolded product is root-buildable, so a future
change that breaks stock `dotnet build/test/run` is caught at release time.

**Independent Test**: Run the release/instantiation job; it scaffolds a product and asserts stock build +
test at the root succeed and the app profile runs, failing the release on regression (quickstart Scenario D).

- [X] T017 [US3] Extend (do NOT duplicate) the `template-product-tests` job in `.github/workflows/release.yml` (~lines 36–61): the "Test generated product" step already runs `dotnet test "$PRODUCT_DIR" -c Release` (line 60) — that line becomes root-resolvable via the new `.slnx`, so **keep it** and add the two missing stock assertions around it: `dotnet build "$PRODUCT_DIR"` and `dotnet run --project "$PRODUCT_DIR/src/GeneratedProduct"` exits 0 (per contract C4). Pick **one config for all three** so the gate proves what a consumer runs — match `quickstart.md` (stock default/Debug) unless US1's smoke shows Release is required, and apply that same config to the existing `dotnet test` line (today's `-c Release`); record the chosen config in `us3.md`. If the US1 smoke (T007) selected the headless-run fallback, mirror it here instead of `dotnet run`. Preserve the existing `if: github.repository == 'FS-GG/FS.GG.Rendering'` guard
- [X] T018 [US3] Demonstrate the gate both ways (SC-005) per quickstart Scenario D: one passing run with artifacts present, then a deliberately-broken run (remove the root `.slnx`) showing stock `dotnet build` fails and the gate would block release; record both in `specs/212-template-root-build/readiness/us3.md`

**Checkpoint**: Release gate is green on a passing product and red when root buildability is broken — all three stories functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cross-repo contract coherence, full coverage sweep, and regression confirmation.

- [X] T019 Cross-repo coherence (FR-011 / contract C5): via the `cross-repo-coordination` skill, record the `contract-change` to `fs-gg-ui-template` — update `FS-GG/.github` `registry/dependencies.yml` + `docs/registry/compatibility.md` to capture the root-buildable guarantee SDD's composition-acceptance probes consume, linking tracker FS-GG/FS.GG.Rendering#9
- [X] T020 Run quickstart Scenario E (FR-008 coverage): for `profile ∈ {app, headless-scene, governed, sample-pack}` × `lifecycle ∈ {spec-kit, sdd, none}`, confirm `<Name>.slnx`/`global.json`/`build.sh`/`build.cmd` emit and stock build/test succeed (`run` asserted only for the runnable `app` profile); confirm `wcag` vs `ant` designSystem is byte-neutral for these artifacts. Record in `specs/212-template-root-build/readiness/coverage.md`
- [X] T021 [P] Update `template/base/README.md` so a consumer with no FAKE knowledge can go from "just scaffolded" to "built and tested at root" with only stock commands (SC-007)
- [X] T022 Re-run the baseline (`dotnet fsi scripts/baseline-tests.fsx --out specs/212-template-root-build/readiness/baseline-after.md`) and diff against T001 to confirm no regression
- [X] T023 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/212-template-root-build/feedback/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** Creates the root artifacts
  (T004/T005), wires emission (T006), and runs the live smoke (T007) that confirms the plan's hypotheses.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 (P1) is the MVP. US2/US3 can then proceed
  in parallel with US1 hardening (if staffed), or sequentially P1 → P2 → P3.
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Acceptance/hardening over the Foundational artifacts — no dependency on US2/US3.
- **US2 (P2)**: Wrapper + new FAKE targets. Independent of US1; relies only on the Foundational `.slnx`.
- **US3 (P3)**: Release gate. Asserts the US1 stock surface; independently testable via quickstart D.

### Within Each User Story

- Foundational artifacts (T004–T006) exist before any story verifies them.
- US2: `build.sh`/`build.cmd` (T013/T014) and the new `build.fsx` targets (T015) before wrapper validation (T016).
- US3: gate wiring (T017) before the two-way demonstration (T018).

### Parallel Opportunities

- T002 runs parallel to T001.
- **Foundational**: T004 and T005 are different files — run in parallel; T006 follows (needs both); T007 (smoke) needs T004–T006.
- **US2**: T013 (`build.sh`) and T014 (`build.cmd`) are different files — run in parallel; T015 (`build.fsx`) is independent of them and can run alongside.
- **Polish**: T021 (README) and T023 (feedback) are parallel-safe.
- With staff, once Foundational completes, US1 / US2 / US3 can be worked concurrently.

---

## Parallel Example: Foundational artifacts

```bash
# Create the two hypothesis-bearing root artifacts together (different files):
Task: "Create template/base/Product.slnx referencing src + tests projects"
Task: "Create template/base/global.json pinning the 10.0.x SDK band"
# then wire emission (T006), then run the live smoke (T007).
```

## Parallel Example: User Story 2 wrapper pair

```bash
# Both shell wrappers are independent files:
Task: "Create template/base/build.sh POSIX verb wrapper"
Task: "Create template/base/build.cmd Windows verb wrapper"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup (baseline + env).
2. Phase 2: Foundational — create root artifacts, wire emission, and run the **early live smoke** that
   validates hypotheses (a)/(b) against a really-scaffolded product **before** building US2/US3.
3. Phase 3: US1 — reconcile, verify name-rewrite, FR-010 parity, SDK reproducibility.
4. **STOP and VALIDATE**: quickstart Scenario A green on a scaffolded product.
5. Ship MVP (stock root build/test/run).

### Incremental Delivery

1. Setup + Foundational → root-buildable foundation proven live.
2. US1 → stock root build/test/run (MVP).
3. US2 → uniform verb wrapper delegating to FAKE.
4. US3 → release gate so it cannot silently regress.
5. Polish → cross-repo coherence, coverage sweep, README, baseline re-check.

---

## Notes

- [P] = different files, no dependency on incomplete tasks.
- The root artifacts are created in **Foundational** (not US1) because the plan mandates a live
  instantiate-and-run smoke that confirms them before the wrapper/release wiring; US1 is the verified
  acceptance slice over those artifacts.
- `Test` and `Verify` in `build.fsx` are **frozen** (FR-007/SC-004) — never edit their bodies.
- Use `--name Acme` (PascalCase) for every live scaffold — a lowercase/dir-derived name breaks the
  generated build (FS0053).
- Commit after each task or logical group.
