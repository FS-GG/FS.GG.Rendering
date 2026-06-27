---
description: "Task list for feature 208 — Rename fs-skia-ui version machinery to fs-gg-ui"
---

# Tasks: Rename fs-skia-ui Version Machinery to fs-gg-ui

> **STATUS: COMPLETE (2026-06-27).** All 35 tasks done. US1 verified by live generate→restore→build of
> the bumped template `FS.GG.UI.Template@0.1.51-preview.1` (1 `FsGgUiVersion`, 0 `FsSkiaUiVersion`,
> restore+build+invariant green 30/30); US2 tags `fs-gg-ui/v*` at the same commits, `fs-skia-ui/v*`
> empty local+remote; US3 docs swept (SC-004 zero); cross-repo registry ids + ADR-0003 Accepted via
> FS-GG/.github#3 (merged). Evidence in `readiness/`.

**Input**: Design documents from `/specs/208-fs-gg-ui-version-rename/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: No new test projects are requested. The one test touched is the template's existing
single-source invariant (`GovernanceTests.fs`), re-pointed to `FsGgUiVersion` as part of the atomic
property rename (FR-003) and proven by a real generate→restore→build. No TDD scaffolding tasks.

**Organization**: Tasks are grouped by user story. Ordering honors the plan's standing assumption —
the breaking property rename is verified by a live generate→restore→build (US1) and the tags are
re-pointed (US2) **before** the doc sweep (US3) and **before** any cross-repo registry write.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the verification instruments (local feed + generate path + baseline) are sound
before relying on them to prove a breaking rename.

- [X] T001 Confirm the local feed at `~/.local/share/nuget-local/` carries the published coherent
  `FS.GG.UI.*` set (the 16 members + BOM) so a generated product can restore; record the feed contents.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/208-fs-gg-ui-version-rename/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge).
- [X] T003 [P] Capture the pre-rename git tag state for reproducibility: `git tag -l 'fs-skia-ui/v*'` and `git rev-list -n1 fs-skia-ui/v0.1.50-preview.1` / `fs-skia-ui/v0.1.51-preview.1`; record the two target commits (expected `57be86c`, `d9f4c81`) into `specs/208-fs-gg-ui-version-rename/readiness/pre-rename-tags.md` so the re-tag (US2) can be checked against them.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the exact rename-site inventory every story depends on, and prove the
generate→restore→build harness works end-to-end **before** it is used as the rename's verification.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** Before touching any rename site, drive the
> real generate path against the **current (unbumped, un-renamed)** template and confirm the
> generated product restores+builds green. This proves the harness itself is sound, so when US1's
> post-rename build is run, a failure is attributable to the rename — not a pre-broken pipeline. A
> green text-grep is **not** evidence (plan "Standing assumption"): only a real generate→restore→build
> trusts the renamed property.

- [X] T004 Build the rename-site inventory (the classification map every story depends on): enumerate the exact occurrences to change — `template/base/Directory.Packages.props` (1 `<FsSkiaUiVersion>` literal + 11 `$(FsSkiaUiVersion)` pins + 2 comments = 14 occurrences; the property pins 11 of the 16-member `FS.GG.UI.*` set — the others aren't consumed by generated products), `template/base/build.fsx` (the `<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>` regex line + ~5 other usages, 6 lines total), `template/base/tests/Product.Tests/GovernanceTests.fs` (assertion string + 2 comments), `.template.config/generated/README.md` (1), `.template.package/README.md` (1), and the US3 doc surfaces — and write it to `specs/208-fs-gg-ui-version-rename/readiness/rename-inventory.md`. Confirm counts with `grep -rn "FsSkiaUiVersion" template/base/ .template.config/ .template.package/`.
- [X] T005 **Early live smoke run**: generate a product from the **current** template via the repo's normal generate path (`dotnet new fs-gg-ui --name Acme` — PascalCase `--name`, per the known FS0053 dir-derived-lowercase trap), then `dotnet restore` + `dotnet build` green inside it. Record the run (or `environment-limited` with disclosed substitute) to `specs/208-fs-gg-ui-version-rename/readiness/smoke-pre-rename.md`. This confirms the harness before it judges the rename.

**Checkpoint**: Rename inventory confirmed and the generate→restore→build harness proven green on the current template — US1 can begin.

---

## Phase 3: User Story 1 - Coherent version property in generated products (Priority: P1) 🎯 MVP

**Goal**: Rename the single-source version property `FsSkiaUiVersion → FsGgUiVersion` across the
template base as an **atomic** change, bump the template version, and prove a generated product
restores+builds green driven solely by `FsGgUiVersion`.

**Independent Test**: Generate a product from the bumped template; confirm `Directory.Packages.props`
exposes exactly one `FsGgUiVersion` (and zero `FsSkiaUiVersion` anywhere in the tree), every
`FS.GG.UI.*` pin reads `$(FsGgUiVersion)`, and `dotnet restore`/`build`/the invariant test are green.

> **Atomic rename (research R2, plan Constraints).** T006–T010 rename the literal, all 11 pins, the
> `build.fsx` resolver regex, and the `GovernanceTests` invariant **together in one commit**. A
> half-renamed tree (literal renamed but a pin still `$(FsSkiaUiVersion)`) fails restore fast on an
> undefined property — so these tasks must land as one unit, then be verified by T012, before any
> commit is considered done.

### Implementation for User Story 1

- [X] T006 [US1] In `template/base/Directory.Packages.props`, rename the `<FsSkiaUiVersion>…</FsSkiaUiVersion>` declaration to `<FsGgUiVersion>…</FsGgUiVersion>`, rewrite all 11 `Version="$(FsSkiaUiVersion)"` pins (every `FS.GG.UI.*` consumed by the product) to `$(FsGgUiVersion)`, and update the 2 comments — preserving exactly one literal (FR-001/FR-002).
- [X] T007 [US1] In `template/base/build.fsx`, change the runtime resolver regex `<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>` to `<FsGgUiVersion>([^<]+)</FsGgUiVersion>` and update the other comment/usage references to `FsGgUiVersion` (6 lines total carry the name, regex line included; resolver still `failwithf`s loud if unresolved).
- [X] T008 [US1] In `template/base/tests/Product.Tests/GovernanceTests.fs`, flip the single-source invariant assertion string `"FsSkiaUiVersion"` → `"FsGgUiVersion"` and update the 2 comments (FR-003).
- [X] T009 [P] [US1] In `.template.config/generated/README.md`, rename the one `<FsSkiaUiVersion>` mention to `FsGgUiVersion`.
- [X] T010 [P] [US1] In `.template.package/README.md`, rename the one `<FsSkiaUiVersion>` mention to `FsGgUiVersion`.
- [X] T011 [US1] In `template/base/Directory.Build.props`, bump the template `<Version>` by a single preview increment to signal the breaking property rename (FR-006); record the chosen number.

### Verification for User Story 1

- [X] T012 [US1] Live generate→restore→build of the **bumped** template (plan's required first verification): `dotnet new fs-gg-ui --name Acme`, then inside the generated product run `grep -c "<FsGgUiVersion>" Directory.Packages.props` (→ 1), `! grep -rq "FsSkiaUiVersion" .` (→ no matches anywhere, SC-001), `dotnet restore`, `dotnet build` (→ green, SC-002), and `dotnet test tests/Product.Tests/Product.Tests.fsproj` (→ single-source invariant green on `FsGgUiVersion`, FR-003). Record evidence to `specs/208-fs-gg-ui-version-rename/readiness/smoke-post-rename.md`.

**Checkpoint**: Generated product has exactly one `FsGgUiVersion`, zero `FsSkiaUiVersion`, restores+builds green, invariant passes — US1 (MVP) is independently done. The breaking change is verified.

---

## Phase 4: User Story 2 - Reproducible snapshot lookups under the new tag namespace (Priority: P2)

**Goal**: Re-create the two coherent snapshot tags under `fs-gg-ui/v<V>` at the **same commits**, and
delete the legacy `fs-skia-ui/v*` tags (clean break).

**Independent Test**: `git tag -l 'fs-gg-ui/v*'` lists exactly the two; `git tag -l 'fs-skia-ui/v*'`
returns nothing; `git rev-list -n1 fs-gg-ui/v<V>` equals the pre-rename commit for each version.

### Implementation for User Story 2

- [X] T013 [US2] Create annotated tag `fs-gg-ui/v0.1.50-preview.1` at commit `57be86c` (the existing `fs-skia-ui/v0.1.50-preview.1` target from T003), carrying the same snapshot subject (FR-004).
- [X] T014 [US2] Create annotated tag `fs-gg-ui/v0.1.51-preview.1` at commit `d9f4c81` (the existing `fs-skia-ui/v0.1.51-preview.1` target from T003), carrying the same snapshot subject (FR-004).
- [X] T015 [US2] Delete the legacy tags `fs-skia-ui/v0.1.50-preview.1` and `fs-skia-ui/v0.1.51-preview.1` locally and push the deletions (FR-005); push the two new tags. (Leave the unrelated `fs-gg-ui-template/v*` namespace untouched.)
- [X] T016 [US2] Verify the namespace swap: `git tag -l 'fs-gg-ui/v*'` → exactly the two; `git tag -l 'fs-skia-ui/v*'` → empty; `git rev-list -n1 fs-gg-ui/v0.1.50-preview.1` → `57be86c`; `git rev-list -n1 fs-gg-ui/v0.1.51-preview.1` → `d9f4c81` (SC-003). Record to the readiness folder.

**Checkpoint**: Reproducibility lookups resolve under `fs-gg-ui/v<V>` to the identical commits; `fs-skia-ui/v*` is empty. US2 independently done.

---

## Phase 5: User Story 3 - No stale `fs-skia-ui` references in docs and provenance (Priority: P3)

**Goal**: Sweep `FsSkiaUiVersion`/`fs-skia-ui` from all currently-shipped guidance, add the
pre-rename migration note to `UPGRADING.md`, and leave matches only in immutable `specs/**` history
and the package-rebrand provenance docs.

**Independent Test**: `grep -rn "FsSkiaUiVersion" PROVENANCE.md template/base/README.md template/base/docs/UPGRADING.md .template.config/generated/README.md .template.package/README.md src/*/README.md` → no matches; `grep -rl "FsSkiaUiVersion" specs/` still finds the historical records (untouched).

> **Boundary (research R4, plan OUT OF SCOPE).** Do **not** edit `specs/**`,
> `docs/product/decisions/0001-package-identity.md`, `docs/audit/mechanism-inventory.md`, or
> `docs/bridge/package-identity-migration.md` — those `fs-skia-ui` strings document the prior package
> rebrand as history (FR-009) and are correct as provenance.

### Implementation for User Story 3

- [X] T017 [P] [US3] In `PROVENANCE.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T018 [P] [US3] In `template/base/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T019 [US3] In `template/base/docs/UPGRADING.md`, replace all 4 `FsSkiaUiVersion` references with `FsGgUiVersion` **and** add the FR-008 migration note: instruct authors of a pre-rename product to rename `FsSkiaUiVersion → FsGgUiVersion` when adopting the bumped template version.
- [X] T020 [P] [US3] In `src/Build/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T021 [P] [US3] In `src/Scene/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T022 [P] [US3] In `src/SkiaViewer/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T023 [P] [US3] In `src/Elmish/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T024 [P] [US3] In `src/KeyboardInput/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T025 [P] [US3] In `src/Layout/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T026 [P] [US3] In `src/Controls/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T027 [P] [US3] In `src/Controls.Elmish/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T028 [P] [US3] In `src/Testing/README.md`, replace the 1 `FsSkiaUiVersion` reference with `FsGgUiVersion`.
- [X] T029 [US3] Run the sweep verification: the shipped-doc grep (above) returns zero matches (SC-004); confirm `UPGRADING.md` instructs editing `FsGgUiVersion` and carries the migration note; confirm the immutable boundary docs were NOT edited.

**Checkpoint**: Zero `fs-skia-ui`/`FsSkiaUiVersion` in current guidance; only `specs/**` history remains. US3 independently done.

---

## Phase 6: Cross-Repo Contract & Final Validation (gated on US1 + US2)

**Purpose**: Re-root the cross-repo registry contract ids and accept ADR-0003 — **only after** the
in-repo property rename (US1) and tag re-point (US2) are verified (FR-010 gating, research R6). This
work lands in `FS-GG/.github` via `gh` + the `cross-repo-coordination` skill, never as files here.

- [X] T030 Confirm the gate: US1 (T012 green) and US2 (T016 green) are both verified before any cross-repo write.
- [X] T031 Via `gh` + `cross-repo-coordination`, rename the registry contract ids `fs-skia-ui-version` → `fs-gg-ui-version` and `fs-skia-ui-bom` → `fs-gg-ui-bom` in `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`) (FR-010).
- [X] T032 Via `cross-repo-coordination`, move ADR-0003 (`docs/adr/0003-rename-fs-skia-ui-version-machinery-to-fs-gg-ui.md` in `FS-GG/.github`) from Proposed → Accepted (FR-010 / SC-005).
- [X] T033 [P] Downstream check (FR-011): confirm Templates and SDD consumers carry no `FsSkiaUiVersion` / `fs-skia-ui/*` reference; if any is found, raise a cross-repo request via `cross-repo-coordination` (otherwise a verify-only no-op).
- [X] T034 Run the full `quickstart.md` validation end-to-end (Steps 1–4) and confirm SC-001…SC-005 all hold; record the final evidence to the readiness folder.
- [X] T035 Mark `tasks.md` complete and capture any per-phase feedback via the `fs-gg-feedback-capture` skill.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (the inventory + proven harness gate everything).
- **US1 (Phase 3, P1)**: Depends on Foundational. The MVP and the prerequisite verification for the whole feature.
- **US2 (Phase 4, P2)**: Depends on Foundational; independent of US1 (tags are decoupled from the property), but sequenced after US1 by priority. Uses T003's recorded commits.
- **US3 (Phase 5, P3)**: Depends on Foundational; independent of US1/US2. Sequenced last among the stories.
- **Cross-Repo (Phase 6)**: Hard-gated on US1 (T012) **and** US2 (T016) being verified (FR-010).

### Within User Story 1 (atomic)

- T006–T010 (the literal + pins + regex + invariant + doc echoes) form **one atomic commit** — a partial rename fails restore fast. T011 (version bump) lands with them. T012 verifies; do not consider US1 done until T012 is green.

### Parallel Opportunities

- T003 [P] runs alongside T001/T002 in Setup.
- US1: T009/T010 [P] (separate README files) can run alongside T006–T008, but all land in the single atomic commit before T012.
- US3: T017–T028 are all [P] — each edits a distinct README; T019 (UPGRADING) adds the migration note. T029 verifies after all land.
- Across stories: once Foundational completes, US1/US2/US3 are independently testable; US2 in particular shares no files with US1/US3.

---

## Parallel Example: User Story 3 (doc sweep)

```bash
# All per-library README edits touch distinct files — run together:
Task: "Replace FsSkiaUiVersion in src/Build/README.md"
Task: "Replace FsSkiaUiVersion in src/Scene/README.md"
Task: "Replace FsSkiaUiVersion in src/SkiaViewer/README.md"
Task: "Replace FsSkiaUiVersion in src/Elmish/README.md"
# … through src/Testing/README.md, plus PROVENANCE.md and template/base/README.md
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (confirm feed + baseline + record pre-rename tags).
2. Phase 2: Foundational (rename inventory + early live smoke run on the **current** template).
3. Phase 3: US1 — atomic property rename + version bump → live generate→restore→build green.
4. **STOP and VALIDATE**: the generated product is driven solely by `FsGgUiVersion`, zero `FsSkiaUiVersion`, green. This is the definition of done for the breaking change.

### Incremental Delivery

1. Setup + Foundational → harness proven.
2. US1 → verified breaking rename (MVP).
3. US2 → tags re-pointed, legacy deleted.
4. US3 → docs swept + migration note.
5. Cross-repo → registry ids + ADR-0003 Accepted (gated on US1 + US2).

---

## Notes

- [P] tasks = different files, no dependencies.
- US1's T006–T010 are atomic — land together; a partial rename fails restore fast (undefined property).
- A green text-grep is necessary but NOT sufficient — only a live generate→restore→build trusts the rename (plan Standing assumption).
- Out of scope (do not edit): `specs/**`, `docs/product/decisions/0001-package-identity.md`, `docs/audit/mechanism-inventory.md`, `docs/bridge/package-identity-migration.md`, `src/**/*.fs(i)`, and the `fs-gg-ui-template/v*` tag namespace.
- Use PascalCase `--name` when generating (`--name Acme`) to avoid the FS0053 dir-derived-lowercase trap.
- Cross-repo state lives in `FS-GG/.github`, mutated via `gh` + `cross-repo-coordination` — never as files in this repo.
