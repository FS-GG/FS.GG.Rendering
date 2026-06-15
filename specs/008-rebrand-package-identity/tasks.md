---
description: "Task list for Rebrand Package Identity (Migration Stage R8)"
---

# Tasks: Rebrand Package Identity (Migration Stage R8)

**Input**: Design documents from `/specs/008-rebrand-package-identity/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (rename-matrix.md, surface-invariance.md, deprecation-notice.md)

**Tests**: No new test code is written for R8 — it is an identity-only rebrand. The "tests" are the
existing default-tier validation suites, the CI surface-drift check, and the generated-consumer
contract, all run as verification tasks (not authored). No assertions are weakened.

**Organization**: Tasks are grouped by user story (P1 → P3) to enable independent implementation and
testing. The four identity facets (package ID, root namespace, assembly name, identity metadata)
MUST move **together** per module — a partial state is a failure, not an intermediate success.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US5)
- All paths are relative to repository root `/home/developer/projects/FS.GG.Rendering/`

## Canonical transformation rule (applies to every rename task)

- Replace the **brand prefix** token `FS.Skia.UI` (dotted) → `FS.GG.UI`, and the kebab brand
  `fs-skia-ui` / `fs-skia-<x>` → `fs-gg-ui` / `fs-gg-<x>`.
- **Preserve (NOT brand)**: the descriptive module name `SkiaViewer`, `SkiaSharp`/standalone `Skia`
  technology references and `open SkiaSharp`, and the descriptive `skia` `<PackageTags>` tag.
- **Out of scope (history — do not rewrite)**: `specs/**`, `docs/imported/**`, `docs/audit/**`,
  `**/bin/**`, `**/obj/**`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the environment and capture the pre-rebrand state needed to prove invariance.

- [X] T001 Verify prerequisites are available: .NET SDK (`net10.0`), a live X11/GL context for the
  GL-dependent default-tier suites, and the local pack feed dir `~/.local/share/nuget-local/`
  (create it if missing) — per `specs/008-rebrand-package-identity/quickstart.md` Prerequisites.
- [X] T002 Establish a clean working baseline: build `FS.GG.Rendering.slnx` and run the default-tier
  suites once **before** any rename so post-rename results have a known-green reference; record the
  pass/fail starting point.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-rebrand public surface so the surface-invariance guard (US2) has a
reference to diff against. This blocks the invariance verification in US2.

**⚠️ CRITICAL**: Complete before US2's surface verification so an accidental surface change cannot
ride along unnoticed under the prefix churn.

- [X] T003 Snapshot the pre-rebrand public surface for the normalized-diff invariance check: copy the
  current `tests/surface-baselines/FS.Skia.UI.<M>.txt` files (9 modules) to a temporary reference
  location outside the rename surface (e.g. `/tmp/r8-surface-before/`), to be compared after the
  rename per `specs/008-rebrand-package-identity/contracts/surface-invariance.md`. Note: **Color is
  intentionally not baseline-tracked** (it is omitted from `scripts/refresh-surface-baselines.fsx`), so
  there are 9 baselines for 10 modules; Color's prefix-only invariance is verified separately in T019
  by `.fsi` inspection.

**Checkpoint**: Pre-rebrand surface captured — rename work can begin.

---

## Phase 3: User Story 1 - The rebrand decision is made explicit and recorded (Priority: P1) 🎯 MVP

**Goal**: Resolve decision `0001` from *deferred* to an accepted **rebrand to `FS.GG.UI.*`**, with
the full old→new matrix, publish-before-deprecate rule, starting version, and old-ID freeze — the
governance keystone that authorizes every downstream change.

**Independent Test**: Read `docs/product/decisions/0001-package-identity.md` and confirm status is
`accepted` (no longer *deferred*), it names the complete old→new mapping for all 10 modules + the
template, states publish-before-deprecate, and records the starting version `0.1.0-preview.1` and the
old-ID freeze — with rationale and revisit conditions intact.

### Implementation for User Story 1

- [X] T004 [US1] Update `docs/product/decisions/0001-package-identity.md`: move status `deferred` →
  `accepted`, record the 2026-06-15 date and rationale, the accepted outcome **rebrand to
  `FS.GG.UI.*`**, the complete old→new identity mapping (10 modules + template, from
  `contracts/rename-matrix.md`), the **publish-before-deprecate** rule, the new starting version
  `0.1.0-preview.1`, and that old `FS.Skia.UI.*` IDs **freeze** at their last published version
  (FR-001, FR-012); keep the original rationale and revisit conditions as history.

**Checkpoint**: Decision `0001` is the single authoritative record authorizing R8 (SC-001).

---

## Phase 4: User Story 2 - Product code carries the new identity and still builds and validates (Priority: P1)

**Goal**: Rename all four identity facets for the ten runtime modules `FS.Skia.UI.<M>` →
`FS.GG.UI.<M>` as one coherent matrix; the product builds, the default tier passes, and the public
surface differs only by the namespace prefix.

**Independent Test**: Build the solution and run the default-tier suites; a product-source search
finds zero `FS.Skia.UI.*` brand-prefix identity tokens (except recorded descriptive usage); the
normalized old↔new surface diff is prefix-only.

### Implementation for User Story 2

> Each per-module task below renames **all four facets together** (package ID + assembly name +
> `<Title>` + brand `<Description>` in the fsproj, **and** the `namespace FS.Skia.UI.<M>` line in
> every `.fs`/`.fsi` in that module), plus resets `<Version>` to `0.1.0-preview.1`. Coherence is
> per-task. `SkiaViewer` and `SkiaSharp`/`Skia`/`skia`-tag usages are preserved.

- [X] T005 [P] [US2] Rename module **Color**: `src/Color/Color.fsproj` (`<PackageId>`,
  `<AssemblyName>`, `<Title>`, brand `<Description>`, `<Version>`→`0.1.0-preview.1`) and the
  `namespace FS.Skia.UI.Color` → `FS.GG.UI.Color` line in every `src/Color/*.fs`/`*.fsi`.
- [X] T006 [P] [US2] Rename module **Scene**: `src/Scene/Scene.fsproj` + every `namespace` line in
  `src/Scene/*.fs`/`*.fsi` (`FS.Skia.UI.Scene` → `FS.GG.UI.Scene`).
- [X] T007 [P] [US2] Rename module **Layout**: `src/Layout/Layout.fsproj` + every `namespace` line in
  `src/Layout/*.fs`/`*.fsi` (`FS.Skia.UI.Layout` → `FS.GG.UI.Layout`).
- [X] T008 [P] [US2] Rename module **Input**: `src/Input/Input.fsproj` + every `namespace` line in
  `src/Input/*.fs`/`*.fsi` (`FS.Skia.UI.Input` → `FS.GG.UI.Input`).
- [X] T009 [P] [US2] Rename module **KeyboardInput**: `src/KeyboardInput/KeyboardInput.fsproj` + every
  `namespace` line in `src/KeyboardInput/*.fs`/`*.fsi` (`FS.Skia.UI.KeyboardInput` →
  `FS.GG.UI.KeyboardInput`).
- [X] T010 [P] [US2] Rename module **SkiaViewer** (prefix only — keep the descriptive `SkiaViewer`
  name): `src/SkiaViewer/SkiaViewer.fsproj` + every `namespace` line in `src/SkiaViewer/*.fs`/`*.fsi`
  (`FS.Skia.UI.SkiaViewer` → `FS.GG.UI.SkiaViewer`); leave `SkiaSharp` references untouched. Also
  rebrand the kebab runtime literal in `src/SkiaViewer/SkiaViewer.fs:907`
  (`"fs-skia-ui-runtime"` → `"fs-gg-ui-runtime"`).
- [X] T011 [P] [US2] Rename module **Elmish**: `src/Elmish/Elmish.fsproj` + every `namespace` line in
  `src/Elmish/*.fs`/`*.fsi` (`FS.Skia.UI.Elmish` → `FS.GG.UI.Elmish`). Also rebrand the kebab runtime
  subscription-id literal in `src/Elmish/AnimationTick.fs:20` (`"fs-skia-ui"` → `"fs-gg-ui"`) **and**
  the mirrored assertion in `tests/Elmish.Tests/AnimationTickTests.fs:49`
  (`[ "fs-skia-ui"; "animation-tick" ]` → `[ "fs-gg-ui"; "animation-tick" ]`) — lockstep brand update,
  not a weakened assertion.
- [X] T012 [P] [US2] Rename module **Controls**: `src/Controls/Controls.fsproj` + every `namespace`
  line in `src/Controls/*.fs`/`*.fsi` (`FS.Skia.UI.Controls` → `FS.GG.UI.Controls`).
- [X] T013 [P] [US2] Rename module **Controls.Elmish**: `src/Controls.Elmish/Controls.Elmish.fsproj`
  + every `namespace` line in `src/Controls.Elmish/*.fs`/`*.fsi` (`FS.Skia.UI.Controls.Elmish` →
  `FS.GG.UI.Controls.Elmish`).
- [X] T014 [P] [US2] Rename module **Testing**: `src/Testing/Testing.fsproj` + every `namespace` line
  in `src/Testing/*.fs`/`*.fsi` (`FS.Skia.UI.Testing` → `FS.GG.UI.Testing`).
- [X] T015 [US2] Update every `open FS.Skia.UI.<M>` → `open FS.GG.UI.<M>` across all consumer/test
  sources in `tests/**/*.fs` (and any in-`src` cross-module `open`), so the compiler resolves the
  renamed namespaces (depends on T005–T014).
- [X] T016 [US2] Build `FS.GG.Rendering.slnx` and run the default-tier "Local inner loop" suites
  (`tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,Controls,Testing,SkiaViewer,Smoke}.Tests`
  + `tests/Lib.Tests`); confirm build succeeds and all suites pass with zero new failures
  attributable to the rename (FR-004, SC-003) (depends on T005–T015).
- [X] T017 [US2] Rename the 9 surface baselines `tests/surface-baselines/FS.Skia.UI.<M>.txt` →
  `FS.GG.UI.<M>.txt` (file rename **and** re-prefix every fully-qualified type/member line), then
  regenerate from the renamed assemblies via `dotnet fsi scripts/refresh-surface-baselines.fsx`
  (depends on T016).
- [X] T018 [US2] Update identity assertions in `tests/Package.Tests/` (`SurfaceAreaTests.fs`,
  `PackageApiReferenceTests.fs`, `NameCollisionSafetyTests.fs`,
  `GeneratedConsumerValidationTests.fs`) that literally contain `FS.Skia.UI` → `FS.GG.UI` so the
  drift/identity checks assert the new brand (depends on T017).
- [X] T019 [US2] Verify surface invariance per `contracts/surface-invariance.md`: (a) for the 9
  baseline-tracked modules, normalize-diff the T003 pre-rebrand snapshot against the regenerated
  baselines (substitute `FS.Skia.UI.`→`FS.GG.UI.`, then `diff`); confirm the diff is **empty** — zero
  added/removed/retyped members; (b) for the non-baseline-tracked **Color** module, confirm by
  inspection that each `src/Color/*.fsi` (`Contrast.fsi`, `Palettes.fsi`) differs from its pre-rebrand
  form only by the `namespace` prefix — no member added/removed/retyped (FR-005, SC-005)
  (depends on T017).

**Checkpoint**: Product builds and validates under `FS.GG.UI.*`; surface changed by prefix only.

---

## Phase 5: User Story 3 - The template instantiates a project on the new identity (Priority: P2)

**Goal**: Move the `dotnet new` template and its package to `FS.GG.UI.Template` / `fs-gg-ui`,
including skill folders and verbatim api-surface, so a generated project references only
`FS.GG.UI.*` and restores + builds.

**Independent Test**: `dotnet new fs-gg-ui` generates a project with template package ID
`FS.GG.UI.Template`, zero `FS.Skia.UI.*` references, and restore + build succeed against the new
packages.

### Implementation for User Story 3

- [X] T020 [US3] Rename the template package fsproj **file** `.template.package/FS.Skia.UI.Template.fsproj`
  → `.template.package/FS.GG.UI.Template.fsproj` and update `<PackageId>`/`<Title>`/`<Description>`
  to the `FS.GG.UI.Template` brand (depends on US2).
- [X] T021 [US3] Update `.template.config/template.json`: `identity` `FS.Skia.UI.Template` →
  `FS.GG.UI.Template`, `name`, `shortName` `fs-skia-ui` → `fs-gg-ui`, `packagePrefix` default
  `FS.Skia.UI` → `FS.GG.UI`, `classifications`, and every skill `source` path `fs-skia-*` →
  `fs-gg-*`.
- [X] T022 [P] [US3] Rename the 6 product-skill folders `template/product-skills/fs-skia-*` →
  `fs-gg-*` (`fs-skia-elmish`, `fs-skia-keyboard-input`, `fs-skia-scene`, `fs-skia-skiaviewer`,
  `fs-skia-testing`, `fs-skia-ui-widgets`) and update all cross-references inside them.
- [X] T023 [P] [US3] Rename the base project-skill folders `template/base/.agents/skills/fs-skia-project`
  and `template/base/.claude/skills/fs-skia-project` → `fs-gg-project` and update cross-references.
- [X] T024 [P] [US3] Rebrand the verbatim framework-surface signatures
  `template/base/docs/api-surface/**/*.fsi`: fully-qualified `FS.Skia.UI.*` → `FS.GG.UI.*` (these are
  `copyOnly`, so `sourceName` substitution won't touch them — edit directly).
- [X] T025 [P] [US3] Update generated configs `.template.config/generated/CLAUDE.md`,
  `.template.config/generated/AGENTS.md`, and `.template.config/generated/.claude/settings.json` for
  brand references (`FS.Skia.UI` / `fs-skia-*` → `FS.GG.UI` / `fs-gg-*`).
- [X] T026 [US3] Update remaining template-tree brand references in `template/base/**`,
  `.template.package/README.md`, and the `dotnet new fs-skia-ui` → `dotnet new fs-gg-ui` invocation
  in `.github/workflows/release.yml`.
- [X] T027 [US3] Verify the generated-consumer contract (mirrors release-only `Product.Tests`):
  `dotnet new fs-gg-ui --name SmokeProduct --output /tmp/r8-smoke`, confirm
  `grep -rn 'FS\.Skia\.UI' /tmp/r8-smoke` finds nothing, then `dotnet restore` + `dotnet build`
  succeed against the local-feed `FS.GG.UI.*` packages (FR-003, SC-004) (depends on T020–T026; the
  packs in T029 must exist on the feed for restore to resolve).

**Checkpoint**: Template identity is `FS.GG.UI.Template`/`fs-gg-ui`; generated project is brand-clean.

---

## Phase 6: User Story 4 - Existing consumers can move without being stranded (Priority: P2)

**Goal**: Produce the replacement `FS.GG.UI.*` packages on the local feed **first**, then deliver a
copy-ready deprecation notice as a recorded (not-yet-applied) action mapping each old ID to its
replacement.

**Independent Test**: The 11 `FS.GG.UI.*` `0.1.0-preview.1` packages exist on the local feed before
any deprecation is actioned; a copy-ready deprecation notice maps every old ID → new ID, deprecates
(not deletes) old IDs, and is explicitly marked **NOT yet applied**.

### Implementation for User Story 4

- [X] T028 [US4] Confirm publish-before-deprecate sequencing: the rename + build + default-tier green
  (US2) is complete **before** packing — this is the precondition that the replacements exist before
  any deprecation (FR-007).
- [X] T029 [US4] Pack the replacement packages: `dotnet pack FS.GG.Rendering.slnx -o ~/.local/share/nuget-local/`,
  then verify with `ls ~/.local/share/nuget-local/ | grep FS.GG.UI` that all 10 runtime modules +
  `FS.GG.UI.Template` exist at `0.1.0-preview.1` (FR-007, SC-006) (depends on T016; unblocks T027).
- [X] T030 [US4] Materialize the copy-ready deprecation notice as
  `docs/bridge/package-deprecation-notice.md` from `contracts/deprecation-notice.md`: the full
  old→new map (11 packages), the per-package deprecation message block, the apply checklist, and the
  status **NOT yet applied** (old IDs deprecated with a forward pointer, **not** deleted/unlisted)
  (FR-008).
- [X] T031 [US4] Verify no overclaiming (FR-009, SC-007): the notice and any out-of-tree deliverable
  are marked as a **recorded action** with copy-ready content and explicitly **not** described as
  already applied to nuget.org.

**Checkpoint**: Replacements exist on the feed; deprecation is recorded, honest, and outstanding.

---

## Phase 7: User Story 5 - Provenance, bridge, and docs reflect the rebrand (Priority: P3)

**Goal**: Update every doc that asserted identity was "retained/unchanged" to the rebranded reality,
record the old→new mapping once authoritatively, scope the import-time mapping as history, and keep
all in-repo cross-references resolving.

**Independent Test**: A search for "retained"/"unchanged"/"no rename" finds no stale claim presented
as current truth; the old→new mapping is recorded; every in-repo cross-reference still resolves.

### Implementation for User Story 5

- [X] T032 [P] [US5] Update `docs/bridge/package-identity-migration.md`: "retained / unchanged by the
  move" → "rebranded `FS.Skia.UI.*` → `FS.GG.UI.*` at R8", with the old→new mapping and the
  import-time retained mapping correctly scoped as history (FR-010).
- [X] T033 [P] [US5] Update `docs/bridge/old-repo-redirect.md` **Block B**: "no rename" →
  deprecation/redirect to the new `FS.GG.UI.*` IDs (aligned with the deprecation notice from T030)
  (FR-010).
- [X] T034 [P] [US5] Update `PROVENANCE.md`: record that the rebrand occurred at R8, map imported
  `FS.Skia.UI.*` identifiers → `FS.GG.UI.*`, while still tracing imported files to their source
  paths/commit (import-time mapping preserved as history) (FR-010).
- [X] T035 [P] [US5] Update `docs/product/decisions/0002-template-ownership.md`: template-ID
  references → `FS.GG.UI.Template`.
- [X] T036 [P] [US5] Update brand references in `README.md`, the 9 `src/*/README.md`, and the 7
  `src/*/skill/SKILL.md` files (prefix + kebab tokens; preserve descriptive `SkiaViewer`/`SkiaSharp`).
- [X] T037 [US5] Verify in-repo link integrity (FR-011, SC-008): every cross-reference touched by the
  rebrand (bridge, decisions, `PROVENANCE.md`, `README.md`) still resolves — no dead in-repo links;
  no document presents "identity retained/unchanged" as current truth (depends on T032–T036).

**Checkpoint**: The record is honest end-to-end; no stale identity claims; no dead links.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final coherence and quickstart validation across all stories.

- [X] T038 Run the product-source brand sweep (SC-002): (a) dotted token —
  `grep -rn 'FS\.Skia\.UI' src tests template .template.config .template.package --include='*.fsproj'
  --include='*.fs' --include='*.fsi' --include='*.json' --include='*.props' --include='*.yml'`
  returns nothing; (b) kebab token —
  `grep -rn 'fs-skia' src tests template .template.config .template.package` returns only intended
  results (every `fs-skia-ui` / `fs-skia-<x>` brand token rebranded to `fs-gg-*`), **including
  `.fs`/test bodies** (`src/SkiaViewer/SkiaViewer.fs`, `src/Elmish/AnimationTick.fs`,
  `tests/Elmish.Tests/AnimationTickTests.fs`) that the per-module namespace tasks did not touch;
  confirm `specs/**`, `docs/imported/**`, `docs/audit/**` were left untouched (history).
- [X] T039 Confirm descriptive-usage preservation (FR-006): `grep -rln 'SkiaViewer\|SkiaSharp' src`
  still returns matches and the descriptive `skia` `<PackageTags>` tag is intact — no blind replace
  mangled them.
- [X] T040 Run the full `specs/008-rebrand-package-identity/quickstart.md` validation guide (sections
  1–7) end-to-end and confirm every "Expect" holds.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2 / T003)**: Depends on Setup; BLOCKS the US2 invariance check (T019).
- **US1 (Phase 3)**: Independent — the decision record can be written first (it authorizes the rest).
- **US2 (Phase 4)**: The substantive rename; depends on Setup/Foundational. Authorized by US1.
- **US3 (Phase 5)**: Depends on US2 (libraries renamed) and on US4's pack (T029) for the generated
  restore in T027.
- **US4 (Phase 6)**: Depends on US2 (pack requires green build). Pack (T029) precedes US3's T027.
- **US5 (Phase 7)**: Depends on US4's deprecation notice (T030) for Block B alignment; otherwise docs.
- **Polish (Phase 8)**: Depends on all stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent — deliverable on its own (the "decide" half of "decide rebrand separately").
- **US2 (P1)**: Authorized by US1; the functional core. Independently testable (build + default tier).
- **US3 (P2)**: Needs US2's renamed libraries and US4's packed feed (T029) to validate the generated consumer.
- **US4 (P2)**: Needs US2's green build to pack. Sequencing guarantee: replacements before deprecation.
- **US5 (P3)**: Follows the substantive changes; aligns docs with US2/US3/US4 reality.

### Within User Story 2 (coherence-critical)

- T005–T014 (per-module, all four facets together) can run in parallel — different modules/files.
- T015 (`open` updates) depends on the namespaces existing (T005–T014).
- T016 (build + default tier) depends on T005–T015.
- T017 (baselines) → T018 (Package.Tests assertions) → T019 (invariance vs. T003 snapshot).

### Parallel Opportunities

- T005–T014: all ten module renames in parallel (distinct files).
- T022, T023, T024, T025: template skill/api-surface/config groups in parallel (distinct trees).
- T032–T036: all five doc-group updates in parallel (distinct files).

---

## Parallel Example: User Story 2 module renames

```bash
# Launch all ten coherent per-module renames together (each does fsproj + namespaces + version):
Task: "T005 Rename module Color (src/Color/*)"
Task: "T006 Rename module Scene (src/Scene/*)"
Task: "T007 Rename module Layout (src/Layout/*)"
Task: "T008 Rename module Input (src/Input/*)"
Task: "T009 Rename module KeyboardInput (src/KeyboardInput/*)"
Task: "T010 Rename module SkiaViewer (src/SkiaViewer/* — keep descriptive SkiaViewer name)"
Task: "T011 Rename module Elmish (src/Elmish/*)"
Task: "T012 Rename module Controls (src/Controls/*)"
Task: "T013 Rename module Controls.Elmish (src/Controls.Elmish/*)"
Task: "T014 Rename module Testing (src/Testing/*)"
# Then T015 (open updates) → T016 (build + default tier) → T017–T019 (baselines + invariance).
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Phase 1 Setup + Phase 2 Foundational (capture pre-rebrand surface).
2. US1 — record the decision (the governance keystone).
3. US2 — rename the ten modules coherently; build + default tier green; surface prefix-only.
4. **STOP and VALIDATE**: product carries `FS.GG.UI.*`, builds, validates, surface invariant.

### Incremental Delivery

1. Setup + Foundational → reference captured.
2. US1 → decision recorded → reviewable on its own.
3. US2 → coherent rename + green validation (functional core).
4. US4 → pack replacements to the local feed (publish-before-deprecate) + recorded deprecation notice.
5. US3 → template + generated-consumer contract (restores against the packed feed).
6. US5 → provenance/bridge/docs honest; links resolve.
7. Polish → product-source brand sweep + full quickstart.

> Sequencing note: US4's pack (T029) is done **before** US3's generated-consumer build (T027) so the
> generated project can restore the `FS.GG.UI.*` packages from the local feed, and **before** any
> deprecation — honoring publish-before-deprecate.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks.
- The four identity facets per module move **together** (T005–T014) — partial renames are failures.
- Brand prefix only: preserve `SkiaViewer`, `SkiaSharp`/`Skia` refs, and the `skia` package tag.
- History dirs (`specs/**`, `docs/imported/**`, `docs/audit/**`) are never rewritten.
- No overclaiming: the nuget.org publish/deprecation is a recorded action, never reported as applied.
- Commit after each logical group; stop at any checkpoint to validate the story independently.
