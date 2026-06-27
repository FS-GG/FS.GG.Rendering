---
description: "Task list for Refresh fs-gg-ui Template to Current Scene API"
---

# Tasks: Refresh fs-gg-ui Template to Current Scene API

**Input**: Design documents from `/specs/201-refresh-template-scene-api/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No NEW test tasks are generated. The spec does not request TDD; verification is the template's
**existing** governance tests (`tests/Product.Tests/GovernanceTests.fs`) plus the per-profile
generate→restore→build→evidence gate and `build.fsx target Verify`. Those are the authoritative "the
template still works" checks (spec Assumptions; FR-008/SC-005).

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) so each can be implemented and
verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (setup, foundational, and polish tasks carry no story label)
- Exact file paths are included in each task. Paths are relative to the repo root
  `/home/developer/projects/FS.GG.Rendering/`.

> **⚠️ STANDING ASSUMPTION (plan.md, do not weaken).** Drift hypotheses are **unverified** until a
> product is generated and built. The raw diff between
> `template/base/docs/api-surface/Scene/Scene.fsi` (flattened snapshot) and the split live
> `src/Scene/*.fsi` is **not** a reliable drift signal — they are organized differently by design. The
> ONLY trustworthy drift evidence is a real per-profile generate → restore → build → evidence run
> against a freshly packed feed (the "early live smoke run" below). Every seed/doc edit MUST be
> justified by an observed compiler/evidence failure, never by the `.fsi` diff alone.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the packed feed, the verification scaffolding, and the no-regression baseline the
whole refresh is measured against.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project (solution + Package.Tests + samples) so pre-existing reds are known up front and not mistaken
> for refresh regressions at merge. Use the discovery-based runner; do not hand-pick a subset.

- [X] T001 Create the readiness output directory `specs/201-refresh-template-scene-api/readiness/` and a per-profile evidence sub-tree (`readiness/<profile>/`) to collect drift items, build logs, and evidence reports referenced by every later phase.
- [X] T002 Establish the no-regression baseline: run `dotnet fsi scripts/baseline-tests.fsx --out specs/201-refresh-template-scene-api/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge).
- [X] T003 Pack the local feed and capture the produced version `V`: run `dotnet fsi scripts/refresh-local-feed-and-samples.fsx package-feed`, then read `V` from the `~/.local/share/nuget-local/FS.GG.UI.Scene.*.nupkg` filename and record it in `specs/201-refresh-template-scene-api/readiness/feed-version.txt` (this `V` is the re-pin target for FR-003/C3 and the restore target for C1/C3). Also record the pre-refresh `<FsSkiaUiVersion>` literal from `template/base/Directory.Packages.props` (the candidate "superseded" literal for C4).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Produce the authoritative drift map by actually generating, restoring, building, and
exercising every profile against the freshly packed feed — BEFORE any seed-code or doc edit.

**⚠️ CRITICAL**: No user-story edit (Phase 3+) may begin until the drift map exists and is grounded in a
real per-profile build/evidence run. The plan's drift hypotheses are unverified assumptions until then.

> **⚠️ Early live smoke run (STANDING, do not omit).** For this feature, the "real running app" is each
> **generated** product. `template/base` is NOT buildable in place (each seed `.fs` carries both the
> profile branch and its `//#else` sibling, which collide for the compiler) — drift can only surface in a
> generated copy. T005 IS that smoke run: generate → restore → build → evidence for all four profiles.
> Treat its compiler/evidence output as the only trustworthy drift signal (research Decision 1/2).

- [X] T004 Map the seed surface the refresh must conform: enumerate, per profile and per `//#if`/`//#else` branch, every Scene/Controls/Viewer/Elmish/Layout/KeyboardInput/DesignSystem construct referenced across `template/base/src/Product/Model.fs`, `View.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `Program.fs`, `WindowOptions.fs`. Record the construct inventory in `specs/201-refresh-template-scene-api/readiness/seed-construct-inventory.md` (this is the static cross-check basis for US2/SC-002; it is NOT yet a drift signal).
- [X] T004a **Restore-precondition guard** (contracts/generated-product-evidence.md "Preconditions": `FsSkiaUiVersion == V`): compare the produced feed version `V` (`readiness/feed-version.txt`, T003) against the current `<FsSkiaUiVersion>` in `template/base/Directory.Packages.props`. If they already match (the expected state per research Decision 3), record a no-op and proceed. If `V ≠ current pin`, re-pin to `V` **now** (this is the FR-003 edit pulled forward) so the smoke run's `dotnet restore` can resolve `FS.GG.UI.*` from the feed — otherwise restore fails on version resolution (NU16xx), not API drift, and detection cannot run. Record which branch was taken in `readiness/feed-version.txt`; if the re-pin happens here, T017 becomes a verification-only no-op.
- [X] T005 **Early live smoke run (drift detection)**: for each profile `p ∈ {headless-scene, governed, app, sample-pack}` run `dotnet new fs-gg-ui --name Product --profile p -o <tmp>/Product-p` then `dotnet restore` + `dotnet build -c Release` + the per-branch evidence commands (`--scene-evidence`, `--layout-evidence`; interactive adds `--launch-evidence`, `--image-evidence`) per `quickstart.md` Steps 2–3. Capture full build/evidence output to `specs/201-refresh-template-scene-api/readiness/<profile>/smoke.log`. Record interactive host `unsupported` as a pass, never `failed`. Requires T004a (pin must equal `V` for restore to resolve).
- [X] T006 Build the **Drift Item** map (data-model.md "Drift Item" shape) from T005 output: for every compiler error and evidence failure, record `{profile, file/location, symptom, surface, current equivalent (or "none — no drift"), resolution}` in `specs/201-refresh-template-scene-api/readiness/drift-map.md`. Group items by seed file and by profile branch so US1 tasks below can be scoped precisely. If T005 was fully green, record the map as empty (verified no-drift) — the version/doc/verify stories still proceed.

**Checkpoint**: Drift map exists and is grounded in a real per-profile build/evidence run. Each
compiler/evidence failure is now a concrete, file-scoped edit. User-story work can begin.

---

## Phase 3: User Story 1 - Generated product compiles and runs against the current engine (Priority: P1) 🎯 MVP

**Goal**: The seed product source (`template/base/src/Product/*.fs`) and the version pin reference the
current FS.GG.UI Scene/Controls/Viewer API, so a freshly generated product restores, builds, and emits
its scene/evidence for **every** profile with zero API-drift errors.

**Independent Test**: Scaffold a product at each profile, restore against the freshly packed local feed,
build, and run its evidence commands — build succeeds with no Scene-API-related compile error/warning and
each profile emits its expected scene/evidence (re-run `quickstart.md` Steps 2–3; contracts C1, C3,
generated-product-evidence per-profile + per-branch tables).

> **NOTE**: Every edit below MUST be justified by a specific entry in `readiness/drift-map.md` (T006).
> Edits stay inside the correct `//#if`/`//#else` branch and MUST NOT break the sibling branch (spec edge
> case: profile-guarded source). If the drift map is empty for a file, that file's task is a verified
> no-op recorded as such — do NOT edit it speculatively (FR-009: no unrelated changes).

### Implementation for User Story 1

- [X] T007 [P] [US1] Conform `template/base/src/Product/Model.fs` to the current surface per its `readiness/drift-map.md` entries (Scene types in both branches; Controls/Controls.Elmish/DesignSystem/Themes.Default/KeyboardInput in the interactive branch). Keep both `//#if`/`//#else` branches intact.
- [X] T008 [P] [US1] Conform `template/base/src/Product/View.fs` per its drift-map entries (Scene `Group/Text/Rectangle/Size/SceneNode` in the headless branch; typed `Controls.Typed.*`, `Widget.toControl`, `Control.renderTree` in the interactive branch). Keep both branches intact.
- [X] T009 [P] [US1] Conform `template/base/src/Product/LayoutEvidence.fs` per its drift-map entries (`LayoutEvidenceReport`, `LayoutRegionEvidence`, `Scene`, overlap diagnostics). Keep both branches intact.
- [X] T010 [P] [US1] Conform `template/base/src/Product/WindowOptions.fs` (interactive branch only) per its drift-map entries (window-behavior parsing → Viewer launch request).
- [X] T011 [US1] Conform `template/base/src/Product/EvidenceCommands.fs` per its drift-map entries (`SceneEvidence.render`; `Viewer.*` evidence/host APIs in the interactive branch). Sequenced after T007–T010 because it consumes Model/View/LayoutEvidence/WindowOptions types whose shapes those tasks may change.
- [X] T012 [US1] Conform `template/base/src/Product/Program.fs` per its drift-map entries (entry point; per-profile host selection — `Viewer.runApp` sample-pack / `ControlsElmish.runInteractiveApp` app / headless CLI). Sequenced last (it wires every other module).
- [X] T013 [US1] Re-run the per-profile generate → restore → build for all four profiles (`quickstart.md` Step 2; pin already equals `V` via T004a, so restore resolves cleanly). Confirm zero errors and zero API-drift warnings (contract C1 / SC-001). Append the post-edit build logs to `specs/201-refresh-template-scene-api/readiness/<profile>/build.log`. Loop back to T007–T012 for any residual drift item.
- [X] T014 [US1] Re-run per-branch evidence for all four profiles (`quickstart.md` Step 3): headless `--scene-evidence` (status=ok, deterministic) + `--layout-evidence` (status=ok, ReadableLayout); interactive `--scene-evidence`/`--layout-evidence` (accepted=true) + `--launch-evidence`/`--image-evidence` (ok **or** host-`unsupported`, never `failed`). Record reports under `specs/201-refresh-template-scene-api/readiness/<profile>/` (SC-006, generated-product-evidence per-branch table).

**Checkpoint**: All four profiles generate, restore, build clean, and emit expected scene/evidence — the
MVP (a template that produces a working product) is met (SC-001, SC-006).

---

## Phase 4: User Story 2 - Bundled API-surface docs and seed code agree with the engine (Priority: P2)

**Goal**: The template's bundled Scene reference (`template/base/docs/api-surface/Scene`) and the seed
`Product/*.fs` worked example reflect the current public Scene surface, so what a developer copies is the
API they actually have.

**Independent Test**: Every type/member presented as current in the bundled Scene reference resolves in
`src/Scene/*.fsi`; every Scene construct used by the seed exists in the current surface with the assumed
shape (`quickstart.md` Step 6; contract C5; SC-002/SC-004).

### Implementation for User Story 2

- [X] T015 [US2] Cross-check the seed Scene-construct inventory (`readiness/seed-construct-inventory.md`, T004) against the live `src/Scene/*.fsi` (`Types.fsi`, `Scene.fsi`, `Evidence.fsi`, `Inspection.fsi`, `TextShaping.fsi`, `SceneCodec.fsi`, `Animation.fsi`). Confirm 100% of referenced Scene constructs exist with the assumed arity/shape (SC-002). Any mismatch surfaced here that the build did not already catch becomes a new drift item routed back to the relevant US1 file task.
- [X] T016 [US2] Bring `template/base/docs/api-surface/Scene/Scene.fsi` into agreement with the current public Scene surface (`src/Scene/*.fsi`): refresh it so no type/member is presented as current that is absent from the live surface (FR-005/SC-004/C5). Preserve framework identifiers **verbatim** — this path is `copyOnly` in `.template.config/template.json`, so no `sourceName`/`Product` substitution may be introduced (spot-confirm after editing).

**Checkpoint**: Bundled Scene reference and seed example both agree with the live surface; a developer
copying from either gets a current, resolvable construct (SC-002, SC-004).

---

## Phase 5: User Story 3 - The refresh is verifiable and the template's own checks stay green (Priority: P3)

**Goal**: A maintainer can confirm the refresh is complete by running the template's existing validation
against the re-pinned version, with no stale version literal remaining anywhere.

**Independent Test**: Governance tests + generated-product checks pass for every profile against the
re-pinned `FsSkiaUiVersion`; a repo search finds no superseded literal presented as the current pin
(`quickstart.md` Steps 4–5; contracts C2/C3/C4; SC-003/SC-005).

### Implementation for User Story 3

- [X] T017 [US3] Confirm/finalize the `FsSkiaUiVersion` re-pin in `template/base/Directory.Packages.props` to the produced feed version `V` (from `readiness/feed-version.txt`, T003) — FR-003/C3. The actual edit may already have been performed by T004a (when `V ≠ current pin`); in that case verify the pin equals `V` and record this task as a verification-only no-op. If `V` equals the original pin, the re-pin was a verified no-op throughout. Either way, the remaining US3 tasks still run.
- [X] T018 [US3] Assert the single-version invariant on `template/base/Directory.Packages.props` (FR-004/C2): `grep -c '<FsSkiaUiVersion>' …` → `1`, and `grep -E 'Include="FS.GG.UI[^"]*" Version="[0-9]' …` → no matches (every `FS.GG.UI.*` pin resolves to `$(FsSkiaUiVersion)`; no second literal introduced).
- [X] T019 [US3] Assert no stale literal remains (FR-006/C4/SC-003): `grep -rn '<superseded-version>' template/base/` (the pre-refresh literal recorded in T003) returns only intentionally illustrative/historical occurrences (e.g. a non-live `docs/UPGRADING.md` example), none representing the live pin. **Scope note**: the scan is intentionally bounded to `template/base/` because FR-006 governs *template artifacts* and the Assumptions exclude illustrative/historical literals; repo-level docs outside `template/base/` (e.g. a root `README`) are out of scope unless they state the template's current pin. If the re-pin was a no-op (`V` unchanged), this is vacuously true — record that.
- [X] T020 [US3] Run the governance + verify gate for every profile (`quickstart.md` Step 4): in each generated product run `dotnet fsi build.fsx target Verify` (Dev + GeneratedGuidanceCheck + TemplateDrift + EvidenceGraph + EvidenceAudit + Test) and confirm exit 0 — including the GovernanceTests single-source-version assertion that `build.fsx` resolves the engine from `FsSkiaUiVersion` (FR-008/SC-005; generated-product-evidence governance gate).

**Checkpoint**: All template validation is green against the re-pinned version; exactly one
`FsSkiaUiVersion` literal equals `V` and no stale literal remains (SC-003, SC-005).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final whole-feature verification and regression confirmation.

- [X] T021 Re-run the full `quickstart.md` (Steps 1–6) end-to-end as a single pass across all four profiles and confirm the "Success = all of" block holds; archive the consolidated result in `specs/201-refresh-template-scene-api/readiness/quickstart-result.md`.
- [X] T022 Re-run `dotnet fsi scripts/baseline-tests.fsx` and diff against `readiness/baseline.md` (T002) to confirm the refresh introduced **zero** new test regressions (any red must match a pre-existing baseline red).
- [X] T023 [P] Confirm scope discipline (FR-009): review the working diff (`git diff`) and confirm every change is confined to `template/base/` (seed source, the single version pin, the bundled Scene doc) and is justified by a drift-map entry, the re-pin, or the doc-conformance task — no new product features, no profile changes, no unrelated refactors.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T003 (pack feed) blocks T005 and T017.
- **Foundational (Phase 2)**: Depends on Setup (needs the packed feed from T003). T004a (restore-precondition guard) MUST run before T005, because the smoke run's `dotnet restore` can only resolve `FS.GG.UI.*` when `FsSkiaUiVersion == V`. BLOCKS all user stories — the drift map (T006) is the input to every US1 edit.
- **User Story 1 (Phase 3)**: Depends on Foundational. The MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational (uses the T004 inventory). Largely independent of US1, but T015 may emit drift items routed back to US1 file tasks; T016 (doc) is independent of US1.
- **User Story 3 (Phase 5)**: Depends on Foundational (needs `V` from T003). The pin already equals `V` after T004a, so T017 is a confirmation step; the verify gate (T020) should run after US1 so builds are clean.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Independent MVP — delivers a buildable, evidence-emitting template.
- **US2 (P2)**: Independently testable (doc/inventory conformance); may feed drift items back to US1.
- **US3 (P3)**: Independently testable (version invariants + green gate); re-pin is independent, verify gate reads cleaner after US1.

### Within User Story 1

- T007–T010 are parallel (different files, independent branches).
- T011 (EvidenceCommands) after T007–T010 (consumes their types).
- T012 (Program) last (wires every module).
- T013 (build) then T014 (evidence) gate the story; loop back on residual drift.

### Parallel Opportunities

- **Setup**: T002 (baseline) and T003 (pack feed) can run in parallel; T001 is trivial and independent.
- **US1**: T007, T008, T009, T010 in parallel (different seed files / branches).
- **Across stories** (after Foundational): US3's re-pin (T017) and US2's doc refresh (T016) can proceed in parallel with US1 edits, since they touch different files (`Directory.Packages.props` / `docs/api-surface/Scene` vs `src/Product/*.fs`). Run the build/verify/evidence gates (T013/T014/T020/T021) only after the edits they validate land.

---

## Parallel Example: User Story 1

```bash
# After the drift map (T006) exists, conform the independent seed files together:
Task: "Conform template/base/src/Product/Model.fs per drift-map (T007)"
Task: "Conform template/base/src/Product/View.fs per drift-map (T008)"
Task: "Conform template/base/src/Product/LayoutEvidence.fs per drift-map (T009)"
Task: "Conform template/base/src/Product/WindowOptions.fs per drift-map (T010)"
# Then T011 (EvidenceCommands) → T012 (Program) → T013 (build) → T014 (evidence)
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — pack the feed, capture `V`, baseline the test suite.
2. Phase 2 Foundational — **early live smoke run** (T005) produces the real drift map (T006) before any edit.
3. Phase 3 US1 — conform the seed files, re-build and re-emit evidence for all four profiles.
4. **STOP and VALIDATE**: all four profiles generate, restore, build clean, emit scene/evidence.
5. This alone restores the template's core purpose (a working starting product).

### Incremental Delivery

1. Setup + Foundational → drift map ready.
2. US1 → all profiles build + emit evidence → **MVP** (SC-001, SC-006).
3. US2 → bundled Scene doc + seed example agree with live surface (SC-002, SC-004).
4. US3 → re-pin verified, single-version invariant + green governance gate (SC-003, SC-005).
5. Polish → end-to-end quickstart, no-regression diff, scope-discipline check.

---

## Notes

- [P] tasks = different files / branches, no dependencies on incomplete tasks.
- Every US1 seed edit MUST trace to a `readiness/drift-map.md` entry (T006) — never edit from the `.fsi`
  diff (plan standing assumption; research Decision 1).
- `template/base` is not buildable in place; always validate through `dotnet new` generation.
- Keep edits inside the correct `//#if`/`//#else` branch; never break the sibling branch.
- Interactive host `unsupported` is a pass for the evidence contract; only `failed` is a failure.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
