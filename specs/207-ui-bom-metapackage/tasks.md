---
description: "Task list for Optional FS.GG.UI BOM / Metapackage"
---

# Tasks: Optional FS.GG.UI BOM / Metapackage Pinning the Coherent Package Set

**Input**: Design documents from `/specs/207-ui-bom-metapackage/`

**Prerequisites**: plan.md, spec.md, research.md (R1–R7), data-model.md (E1–E5, INV-1–6), contracts/ (bom-metapackage BM-A..D, consumer-pinning-behavior CP-A..E, cross-repo-record XR-A..C), quickstart.md (Scenarios 1–8)

**Tests**: This feature's whole deliverable is *verification of packaging behavior*, so the test
tasks are not optional add-ons — they are the artifact chain (Constitution Tier 1, Principle V). The
always-on membership-parity test and the env-gated live consumer test are first-class.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) so each can be implemented
and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (setup, foundational, and polish tasks carry no story label)
- Exact file paths are included in every task.

## Path / shape notes (from plan.md)

- The BOM ships **no assembly** (`IncludeBuildOutput=false`) — there is **no `.fs`/`.fsi` public
  surface** to draft. The metaproject + hand-authored `.nuspec` IS the artifact.
- Members and the `fs-gg-ui` template are **read-only** (members are re-packed at the new `V` via
  `-p:Version=V`, never edited). `src/ColorPolicy` stays `IsPackable=false`.
- `V` = the next coherent version the pack fixes (current published snapshot is `0.1.50-preview.1`,
  so the next preview, e.g. `0.1.51-preview.1`; the pack invocation fixes the exact value — never a
  hand-maintained second literal, INV-1 / FR-009).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the naming precondition before adding
the artifact.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project via the
> discovery-based runner so pre-existing reds (stale surface baselines, sample pins, missing reports)
> are known up front and not mistaken for regressions at merge — `dotnet test FS.GG.Rendering.slnx`
> deliberately omits `tests/Package.Tests` (this feature's home) and `samples/**/*.Tests`.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/207-ui-bom-metapackage/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T002 [P] Confirm the bare `FS.GG.UI` package ID is free (no collision precondition, plan Constraints / research R2): list the local feed `~/.local/share/nuget-local/` and confirm only `FS.GG.UI.<suffix>` members + the unrelated `FS.GG.UI.Template` exist and no producer already claims bare `FS.GG.UI`; record the finding in `specs/207-ui-bom-metapackage/readiness/baseline.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Produce the metapackage artifact and **prove the mechanism against a real feed before
anything is built on top of it.**

**⚠️ CRITICAL**: No user story (tests, report, registry) may begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit) — this feature's mandated FIRST live step.**
> The plan's standing assumption: *coherence is unverified until the BOM is packed and a real consumer
> restores against it.* A well-formed nuspec can still resolve wrong (NuGet edge cases) — exactly the
> failure this feature exists to prevent. So after the artifact exists and BEFORE writing the
> parity/consumer tests, the registry, or the report, **drive the real mechanism**: one-command pack
> the snapshot to a scratch feed, restore a hand-made clean consumer referencing only `FS.GG.UI@V`,
> observe every member resolves at `V`, then force a member to `Y≠V` and observe the real
> NU1605/NU1107 conflict. Treat the "exact `[X]` ⇒ loud in both directions" claim (research R1) as an
> **unverified hypothesis until this run is observed**. If no feed/SDK is available, record
> `environment-limited` with the disclosed substitute, do not fake a pass.

- [X] T003 Create the packable metaproject `src/Meta/FS.GG.UI.metaproj`: `PackageId=FS.GG.UI`, `IsPackable=true`, `IncludeBuildOutput=false`, `NuspecFile=FS.GG.UI.nuspec`, `NuspecProperties=version=$(Version)` (BM-1, BM-3; data-model E1)
- [X] T004 Author `src/Meta/FS.GG.UI.nuspec` with **one `<dependency id="FS.GG.UI.<member>" version="[$version$]" />` entry per packable `FS.GG.UI.*` member** — the single membership list — covering the current set (`Build, Scene, Canvas, Controls, Controls.Elmish, DesignSystem, Diagnostics, Elmish, KeyboardInput, Layout, SkiaViewer, Symbology, Symbology.Render, Testing, Themes.AntDesign, Themes.Default`, 16 today); the membership is the *discovered packable set*, not a hard-coded count (spec Assumption "16-package set": the BOM tracks the set rather than hard-coding a number — the count is asserted by the parity test in T007, never fixed as a literal). Every version is the single `[$version$]` token in exact-bracket form, no per-member literal (BM-1, BM-2; data-model E2; INV-1; FR-003)
- [X] T005 Add the `src/Meta` project under `/src/` in `FS.GG.Rendering.slnx` so the existing one-command `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=V` produces `FS.GG.UI@V` in the same snapshot as the 16 members (BM-4; quickstart Scenario 1)
- [X] T006 **Early live smoke run (mechanism hypothesis check)**: pick `V`, run `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=$V -o <scratch-feed>`; confirm `FS.GG.UI.$V.nupkg` has no `lib/` and 16 `[$V]` deps; restore a hand-made clean consumer (`<PackageReference Include="FS.GG.UI" Version="$V" />`), observe all `FS.GG.UI.*` at `$V`; force one member to `Y≠V` and observe a **real** NU1605/NU1107 conflict (both `Y<V` and `Y>V`). Capture live evidence (or `environment-limited` + disclosed substitute) into `specs/207-ui-bom-metapackage/readiness/` — do NOT proceed to the tests/registry on the unverified hypothesis

**Checkpoint**: Artifact exists, wired into the one-command pack, and the exact-`[V]` "loud in both
directions" mechanism is confirmed against a real feed (or limitation disclosed). User stories can now
proceed.

---

## Phase 3: User Story 1 - A consumer pins the whole coherent set with one reference (Priority: P1) 🎯 MVP

**Goal**: Referencing the single `FS.GG.UI` package at one version `V` brings every `FS.GG.UI.*`
member at `V` — no second version literal, no member omitted.

**Independent Test**: In a clean consumer whose only FS.GG.UI declaration is `FS.GG.UI@V`, restore and
build; every resolved `FS.GG.UI.*` is at `V` with zero NU1101/NU1605/NU1608 (quickstart Scenario 2; CP-A/CP-B; SC-001).

- [X] T007 [P] [US1] Create the always-on parity/shape test `tests/Package.Tests/Feature207BomMembershipTests.fs` (Expecto): assert the `FS.GG.UI.nuspec` dependency-ID set **equals** the discovered `IsPackable=true` `FS.GG.UI.*` projects under `src/**` (template excluded) — derive the expected count from that discovered set rather than asserting a fixed literal (it is 16 today, but the test must track the set per FR-003, not pin a magic number); every version is the single `[$version$]` token, every version uses the exact-bracket form, and no `lib/`/build output is declared (BM-A, CP-B, CP-C; data-model E2 parity invariant)
- [X] T008 [P] [US1] Create `tests/Package.Tests/Feature207BomConsumerTests.fs` (mirror `GeneratedConsumerValidationTests`/`Feature163PackageFeedValidationTests`): an always-on gate that asserts the committed `specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md` report (`bom-version:`, `resolved-members-at-version:`, `result: pass`), plus the env-gated (`FS_GG_RUN_BOM_CONSUMER_SMOKE=1`) US1 live arm: pack to a temp feed, restore a clean consumer whose **only** FS.GG.UI declaration is the single `FS.GG.UI@V` reference (N→1: no per-member pins), assert 100% of resolved `FS.GG.UI.*` at `V` with no member lost vs the full set and it builds, 0 NU1101/NU1605/NU1608; record `single-reference: true` / `members-resolved: <count>` so the report evidences the N→1 collapse (CP-A; SC-001; SC-002)
- [X] T009 [US1] Register `Feature207BomMembershipTests.fs` and `Feature207BomConsumerTests.fs` in `tests/Package.Tests/Package.Tests.fsproj` (compile-order correct); confirm both fail/`Pending` first (no report yet), per failing-first
- [X] T010 [US1] Run the env-gated regenerator (`FS_GG_RUN_BOM_CONSUMER_SMOKE=1 dotnet test tests/Package.Tests`) to write `specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md` (US1 tokens populated), then run the always-on gate (`dotnet test tests/Package.Tests`) green against the committed report (quickstart Scenarios 2, 5, 6)

**Checkpoint**: US1 independently verifiable — `Feature207BomMembershipTests` green deterministically;
the env-gated arm proves one-reference ⇒ coherent set at `V`. MVP deliverable complete.

---

## Phase 4: User Story 2 - A stale or mixed set becomes impossible (or loudly detected) (Priority: P2)

**Goal**: A consumer that adopted the BOM at `V` cannot end up with a mixed graph — forcing any member
to `Y≠V` fails restore/build loudly; membership stays in lockstep with the published set.

**Independent Test**: In a `FS.GG.UI@V` consumer, force a member to `Y≠V` (stale and newer); restore/build
surfaces a NU1605/NU1107 conflict and produces no mixed graph (quickstart Scenario 3; CP-D; SC-003).

- [X] T011 [US2] Extend `tests/Package.Tests/Feature207BomConsumerTests.fs` env-gated arm with the forced-mismatch case: add a member at `Y<V` then at `Y>V`, assert each restore/build fails with a real NU1605/NU1107 conflict and **no** mixed-version graph is produced; record `forced-mismatch:` (both directions) into the readiness report (CP-D; SC-003; US2 AS1/AS2)
- [X] T012 [US2] Strengthen `tests/Package.Tests/Feature207BomMembershipTests.fs` to prove lockstep loudly: assert that an added/removed packable `FS.GG.UI.*` project without a matching nuspec edit turns the parity test **red** (drift detection — protects adopters from silently missing a new member) (CP-C; US2 AS3; spec edge case "new member added")
- [X] T013 [US2] Re-run the regenerator + always-on gate so the committed `bom-consumer-validation.md` carries the `forced-mismatch: pass` evidence and the deterministic parity/drift test is green (quickstart Scenarios 3, 5, 6)

**Checkpoint**: US1 + US2 both independently verifiable — convenient pinning *and* loud deviation both
proven against a real feed. This is the gate the cross-repo record (US3) depends on.

---

## Phase 5: User Story 3 - The BOM version corresponds 1:1 to a recorded coherent snapshot (Priority: P3)

**Goal**: The BOM at `V` is published in the same snapshot/tag as the members at `V`, is
channel-matched and reproducible, and the cross-repo registry records it as part of the coherent set.

**Independent Test**: Confirm `FS.GG.UI@V` is in the same `fs-skia-ui/v<V>` snapshot as the members,
two clean restores yield an identical resolved set, the channel matches, and `FS-GG/.github` records
the BOM (quickstart Scenarios 1, 4, 7; BM-B/BM-C; SC-004; XR-A).

- [X] T014 [US3] Extend `tests/Package.Tests/Feature207BomConsumerTests.fs` env-gated arm with reproducibility + channel evidence: clear caches and restore the clean consumer **twice**, assert the two resolved member sets are identical, and assert `FS.GG.UI@V` channel matches the members (`-preview` ⇒ preview); record into `bom-consumer-validation.md` (BM-C; SC-004; quickstart Scenario 4)
- [X] T015 [US3] Cut/extend the annotated snapshot tag `fs-skia-ui/v<V>` (feature 204 mechanism) at the resolution commit so the tagged snapshot includes `FS.GG.UI@V` alongside the 16 members at `V` (BM-B; FR-006; data-model E3)
- [X] T016 [US3] **Gated on US1+US2 passing (T010 + T013 green)** — record the `FS.GG.UI` BOM in the `fs-skia-ui-version` compatibility registry in `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`) via the `cross-repo-coordination` skill / `gh` (NOT files in this repo), consistent with the verified US1/US2/SC-004 evidence; if a tracking issue exists, post a `## Response` linking the evidence. Do NOT record on a hypothesis (XR-A/XR-B/XR-C; FR-008)

**Checkpoint**: All three stories verified; the published BOM is a recorded, discoverable member of the
coherent set with no second version literal introduced anywhere (INV-1; XR-C).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Prove the additive/optional invariant and run the full quickstart end-to-end.

- [X] T017 [P] Optionality regression (FR-007 / CP-E): run `dotnet test tests/Package.Tests` and confirm `GovernanceTests` / the `FsSkiaUiVersion` single-source invariant is green and **unchanged** — `template/base/Directory.Packages.props` was not modified and the BOM is purely additive (quickstart Scenario 8). Note: the N→1 footprint-reduction outcome (SC-002) is proven by the US1 consumer test (T008), not here
- [X] T018 [P] Single-version-literal audit (FR-009 / INV-1 / BM-D / XR-C): grep the feature's added surface (`src/Meta/**`, the new tests, the registry record) to confirm no second FS.GG.UI version literal exists — every version derives from the one `-p:Version=V` / `$version$`
- [X] T019 Run the full `specs/207-ui-bom-metapackage/quickstart.md` (Scenarios 1–8) end-to-end against a clean feed and confirm every "Expected" holds; reconcile any drift with the readiness report
- [X] T020 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T001 to confirm no new reds were introduced by the feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the artifact (T003–T005) and the early live mechanism check (T006) must complete first.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 and US2 share the consumer test file, so they are best done in priority order (US1 → US2). US3's registry record (T016) is **hard-gated** on US1+US2 evidence (T010 + T013).
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational. No dependency on other stories — independently testable (MVP).
- **US2 (P2)**: Starts after Foundational. Extends US1's consumer test file (T011 follows T008/T010 — same file, sequential).
- **US3 (P3)**: Tag (T015) can follow Foundational; the **registry record (T016) MUST NOT precede** verified US1 (T010) + US2 (T013) — FR-008 gate.

### Within Each Story

- Foundational artifact (nuspec + slnx) before any test.
- Always-on deterministic tests fail-first, then the env-gated regenerator writes the report, then the always-on gate asserts it.
- Tests/evidence before the cross-repo record (gated).

### Parallel Opportunities

- T002 ∥ T001 (different outputs; T002 only records a finding).
- T007 ∥ T008 — different test files (membership vs consumer). T009 (fsproj registration) waits on both.
- Phase 6: T017 ∥ T018 (independent checks).
- US2 and US3 **cannot** be parallelized against US1 where they extend the same `Feature207BomConsumerTests.fs` (T011, T014 are sequential on T008).

---

## Parallel Example: User Story 1

```bash
# T007 and T008 touch different files — run together:
Task: "Create always-on parity test in tests/Package.Tests/Feature207BomMembershipTests.fs"
Task: "Create env-gated consumer test in tests/Package.Tests/Feature207BomConsumerTests.fs"
# Then T009 registers both in Package.Tests.fsproj (waits on both).
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup (baseline + ID-free check).
2. Phase 2 Foundational (CRITICAL) — author the metaproject + nuspec, wire the slnx, and run the **early live pack→restore→forced-mismatch smoke** that validates the exact-`[V]` mechanism before anything is built on it.
3. Phase 3 US1 — parity test + env-gated one-reference restore proof + committed report.
4. **STOP and VALIDATE**: clean consumer referencing only `FS.GG.UI@V` restores+builds with all members at `V`.

### Incremental Delivery

1. Setup + Foundational → artifact packs in the one-command snapshot, mechanism confirmed.
2. US1 → one-reference pinning proven (MVP).
3. US2 → loud deviation (forced mismatch both directions) + lockstep drift detection proven.
4. US3 → snapshot tag + reproducibility/channel evidence → **then** the gated cross-repo registry record.
5. Polish → optionality regression, single-literal audit, full quickstart, baseline diff.

### Critical gate (do not violate)

The cross-repo registry record (**T016**) is recorded **only after** US1 (T010) and US2 (T013) pass
against the packed snapshot. A partial/missing snapshot keeps the record unmade — no premature
coherence claim (FR-008; XR-B).

---

## Notes

- [P] = different files, no incomplete-task dependency.
- The BOM ships no F# surface — there is no `.fsi` to draft; the nuspec + metaproject is the artifact.
- Members and the `fs-gg-ui` template are read-only (re-packed at `V`, never edited).
- One version literal only: `-p:Version=V` ⇒ `$version$` ⇒ every member dep `[V]` and the BOM's own version (INV-1 / FR-009). Never introduce a second.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
