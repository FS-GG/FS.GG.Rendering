---
description: "Task list for Version-Staleness Guard implementation"
---

# Tasks: Make the FS.GG.UI Version-Staleness Bug Class Structurally Impossible

**Input**: Design documents from `/specs/209-version-staleness-guard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/version-coherence-guard.md, quickstart.md

**Tests**: Test tasks ARE included — the Constitution's Principle V (Test Evidence Is Mandatory) and the
spec/plan require forced-drift fixtures (204 lag, half-bump, unwired member, phantom) that go red before
the guard exists and green after. These are the honest audience for this machinery feature (CLI/exit-code,
not an `.fsi`).

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) for independent implementation
and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup / Foundational / Polish carry no story label)
- Exact file paths are included in each task.

## Path Conventions

F# UI-framework monorepo. Guard ships as a `dotnet fsi` script at repo-root `scripts/`, wired into
`.github/workflows/gate.yml`, with an optional xUnit wrapper under `tests/Package.Tests/`. Read-only
inputs: `template/base/Directory.Packages.props`, `src/Meta/FS.GG.UI.nuspec`, `src/**/*.fsproj`,
`template/base/build.fsx`, and `git tag --list 'fs-gg-ui/v*'`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project scaffolding and a comprehensive no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project
> via the discovery-based runner so pre-existing reds (stale surface baselines, stale sample pins,
> missing-report failures) are known up front and not mistaken for regressions at merge.

- [X] T001 Create the feature readiness directory `specs/209-version-staleness-guard/readiness/` (will hold `version-coherence.md` and `baseline.md`)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/209-version-staleness-guard/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T003 [P] Confirm tooling: `dotnet --version` (net10.0 SDK present) and `git fetch --tags` so `fs-gg-ui/v*` are visible locally, then `git tag --list 'fs-gg-ui/v*'` shows `fs-gg-ui/v0.1.50-preview.1` and `fs-gg-ui/v0.1.51-preview.1` (the guard's tag authority, D1)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Map the coherence surface, verify the plan's unverified hypotheses against a real
pack/restore, and lay the pure verdict seams every user story builds on.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's two root-cause hypotheses —
> "drift is currently silent" and "derivation already collapses to one literal `<FsGgUiVersion>`" — are
> **unverified assumptions until a real pack→restore→build has been driven**. T005/T006 pull that
> verification forward, BEFORE any rule is built. A stray hardcoded pin would invalidate the verify-only
> design (D3); confirm it first.

- [X] T004 Build the coherence-surface map in `specs/209-version-staleness-guard/readiness/surface-map.md`: enumerate the live inputs from data-model.md — the single `<FsGgUiVersion>` literal location (`template/base/Directory.Packages.props:9`), the 16 packable `FS.GG.UI.*` members under `src/**` (P), the 16 BOM dependencies in `src/Meta/FS.GG.UI.nuspec` (B), the 11 consumed pins + their `$(FsGgUiVersion)` derivation in `template/base/Directory.Packages.props` (T), the `build.fsx:60` runtime regex, and the `fs-gg-ui/v*` tags — recording the actual current values as the expected manifest the verdict will check
- [X] T005 **Early live smoke run — confirm today's tree is coherent & restorable**: pack `FS.GG.UI.*` + BOM from source at the current pinned `V` (0.1.50-preview.1) to a throwaway feed and restore `FS.GG.UI@V` in a clean consumer (reuse the clean-consumer layer in `scripts/validate-bom-consumer.fsx`); record in `specs/209-version-staleness-guard/readiness/surface-map.md` whether all 16 members resolve to exactly `V` (live evidence; `environment-limited` with disclosed substitute only if pack/restore is unavailable)
- [X] T006 **Early live smoke run — confirm the 204 drift is currently silent**: deliberately set `<FsGgUiVersion>` to a stale value, run the existing gate/build path (NOT the new guard), and record in `surface-map.md` that nothing in the repo goes red today (proving the "drift is silent" premise); also grep `template/base/Directory.Packages.props` to confirm every `FS.GG.UI.*` pin resolves through `$(FsGgUiVersion)` (no hardcoded literal — confirms D3's single-literal hypothesis). Restore the file afterward via `git checkout --`
- [X] T007 Create the guard script skeleton `scripts/validate-version-coherence.fsx` with the pure `Verdict` data shape from data-model.md §8 (`{ ok; failures: { location; expected; actual; rule } list; provenance }`) and the two-layer env-gate (`FS_GG_RUN_VERSION_COHERENCE_SMOKE`) structure mirroring `scripts/validate-bom-consumer.fsx`; structural layer prints `DRIFT [<rule-id>] <location> / expected / actual / fix` per the contract §2 and exit-codes 0/1/2 (coherent / drift / guard-error-fails-closed) — no rules implemented yet, just the seam + a stub that exits 0
- [X] T008 [P] Implement the preview-aware SemVer comparator (D7) as a pure function inside `scripts/validate-version-coherence.fsx` (parse `major.minor.patch` numerically, then dotted prerelease identifiers per SemVer §11; prefer `NuGet.Versioning.NuGetVersion` if the SDK assemblies are reachable from `fsi`, else the hand-rolled comparator), with inline assertions for the exact spec edge pairs `0.1.9-preview.1 < 0.1.10-preview.1` and `…-preview.1 < …-preview.2`
- [X] T009 [P] Implement the pure input readers in `scripts/validate-version-coherence.fsx`: `SingleVersionSource` (literal + occurrence count from `Directory.Packages.props`), `CoherentSnapshotTag` set (parse `git tag --list 'fs-gg-ui/v*'`), `PublishedMemberSet P` (packable `FS.GG.UI.*` `.fsproj` under `src/**` — reuse `validate-bom-consumer.fsx`'s `discoveredMembers`), `BomDependencySet B` (`src/Meta/FS.GG.UI.nuspec` dependency ids + version tokens), `TemplateConsumedPinSet T` (`FS.GG.UI.*` `PackageVersion` entries + their `Version` attribute), and `RuntimeResolution` (`build.fsx:60` regex match) — each reader fails closed (exit 2) on unreadable input

**Checkpoint**: Coherence surface mapped & confirmed against a real restore; today's silence reproduced;
comparator and input readers in place. User-story rule logic can now proceed.

---

## Phase 3: User Story 1 - Drift fails this repo's own validation, not a downstream consumer's build (Priority: P1) 🎯 MVP

**Goal**: A PR whose `FsGgUiVersion` lags, or points at a version with no published snapshot tag, makes
this repo's own gate go red with a message naming the mismatch — before merge to `main`.

**Independent Test**: Reintroduce the Feature-204 condition (stale `FsGgUiVersion`) and run
`dotnet fsi scripts/validate-version-coherence.fsx` → exit 1 naming the mismatch; restore → exit 0.

### Tests for User Story 1 ⚠️ (write FIRST, must FAIL before implementation)

- [X] T010 [P] [US1] Forced-drift fixture for the 204 lag (quickstart Scenario B): author it as the **canonical** documented shell scenario recorded in `specs/209-version-staleness-guard/readiness/version-coherence.md` (the optional xUnit wrapper T033 mirrors it, never replaces it — see A1 authority note) — set `<FsGgUiVersion>` below the latest tag, run the guard, assert exit 1 + `DRIFT [pin-lags-tag] template/base/Directory.Packages.props:9` with `expected: >= 0.1.51-preview.1` / `actual: <stale>` — MUST fail (no such rule yet)
- [X] T011 [P] [US1] Forced-drift fixture for the phantom version (quickstart Scenario E): set `<FsGgUiVersion>` to `0.1.99-preview.1` (no tag), run the guard, assert exit 1 + `DRIFT [pin-no-tag] … expected a tag fs-gg-ui/v0.1.99-preview.1; actual none` — MUST fail before implementation

### Implementation for User Story 1

- [X] T012 [US1] Implement the `pin-no-tag` rule (FR-002, FR-009) in `scripts/validate-version-coherence.fsx`: `SingleVersionSource.version` MUST equal some `CoherentSnapshotTag.version`; on no match emit `DRIFT [pin-no-tag]` per contract §2 (expected a `fs-gg-ui/v<V>` tag; actual none). Empty tag set ⇒ fail closed (exit 2), never green-by-absence (data-model §2)
- [X] T013 [US1] Implement the `pin-lags-tag` rule (FR-001, FR-002 — the 204 case) using the T008 comparator: `SingleVersionSource.version` MUST NOT be strictly less than `latest(tags).version`; on lag emit `DRIFT [pin-lags-tag]` with `expected: >= <latest> (latest fs-gg-ui/v* tag)` / `actual: <pin>` (preview-aware, not string compare)
- [X] T014 [US1] Wire the structural verdict-core summary + report write: aggregate `pin-no-tag`/`pin-lags-tag` failures into `Verdict`, print all `DRIFT […]` lines, exit 1 if any failure else 0, and (re)write `specs/209-version-staleness-guard/readiness/version-coherence.md` with `result: pass|fail`, `provenance: verdict-core` (contract §1 Output)
- [X] T015 [US1] Add the merge-blocking "Version coherence guard" step to `.github/workflows/gate.yml` running the **structural verdict-core** (`dotnet fsi scripts/validate-version-coherence.fsx`, env-free — always runs), placed alongside the existing surface-baseline-drift step, AND set the gate job's `actions/checkout@v4` to `fetch-depth: 0` (or `fetch-tags: true`) so `git tag` sees `fs-gg-ui/v*` (D2/contract §3) — non-zero exit fails the gate ⇒ PR blocked from merging to `main` (FR-006, US1 #4). NOTE: the scoped restore-grounded proof is added to this same gate step later by T029 (it depends on the live layer T027); US1 ships the structural gate as the MVP, US3 completes the FR-008 gate grounding
- [X] T016 [US1] Run T010/T011 fixtures + quickstart Scenarios A/B/E end-to-end; confirm they now go red on the named location and the coherent tree (Scenario A) exits 0; record results in `specs/209-version-staleness-guard/readiness/version-coherence.md`

**Checkpoint**: US1 fully functional — the 204 lag and phantom-version drift fail this repo's gate with a
named location, before merge. MVP deliverable.

---

## Phase 4: User Story 2 - A version bump is one coherent operation; a partial bump cannot ship (Priority: P2)

**Goal**: A half-bump — one BOM exact pin off the single token, a template pin hardcoded, a dropped/extra
consumed pin, an unwired new `src/**` member, a duplicated literal, or a broken runtime regex — is
detected and named, independently of any `warnings-as-errors` consumer policy.

**Independent Test**: Bump only the single source → passes. Perturb exactly one derived location (a BOM
pin, a member pin) → fails naming the lagging location (quickstart Scenarios C/D).

### Tests for User Story 2 ⚠️ (write FIRST, must FAIL before implementation)

- [X] T017 [P] [US2] Forced-drift fixture for the BOM half-bump (quickstart Scenario C): flip one `src/Meta/FS.GG.UI.nuspec` dependency from `[$version$]` to `[0.1.50-preview.1]`, run the guard with NO `WarningsAsErrors` set, assert exit 1 + `DRIFT [bom-pin-not-token] … FS.GG.UI.Scene` — MUST fail before implementation (proves FR-004 policy independence)
- [X] T018 [P] [US2] Forced-drift fixture for the unwired member (quickstart Scenario D): remove one `<dependency/>` line from `src/Meta/FS.GG.UI.nuspec`, run the guard, assert exit 1 + `DRIFT [bom-member-skew] … missing FS.GG.UI.<X>` — MUST fail before implementation (SC-004)
- [X] T019 [P] [US2] Forced-drift fixture for a hardcoded template pin: change one `FS.GG.UI.*` `PackageVersion` in `template/base/Directory.Packages.props` from `$(FsGgUiVersion)` to a literal, assert exit 1 + `DRIFT [template-pin-hardcoded]` — MUST fail before implementation

### Implementation for User Story 2

- [X] T020 [P] [US2] Implement `bom-pin-not-token` + `bom-exact-bracket` rules (FR-004) in `scripts/validate-version-coherence.fsx`: every `B` dependency `version` MUST be the single `[$version$]` token and an exact `[…]` bracket with no comma; compare **structurally / directly** — explicitly NOT relying on NU1605/NU1608 loudness (policy-independent per contract §3)
- [X] T021 [P] [US2] Implement `bom-member-skew` rule (FR-003): `B.ids == P.members` (full 16-set parity); on mismatch name the missing/extra member expected-vs-actual (reuse `validate-bom-consumer.fsx`'s exact check)
- [X] T022 [P] [US2] Implement `template-pin-hardcoded` + `template-consumed-skew` rules (FR-003, FR-005, D6): `T.pins ⊆ P.members`, every `T` pin's `Version` is `$(FsGgUiVersion)` (no hardcoded literal), and `T.pins == T.expected` (the 11-member consumed manifest from T004) — the 16-vs-11 gap is intentional, do not require `T == P`
- [X] T023 [P] [US2] Implement `single-source-not-unique` + `runtime-regex-broken` rules (FR-005): `<FsGgUiVersion>` `occurrences == 1` in `Directory.Packages.props`, and the `build.fsx:60` regex still matches the literal in the current tree (a renamed/half-renamed property breaks runtime resolution — the 208 half-bump class)
- [X] T024 [US2] Fold the new US2 rules into the `Verdict` aggregation in `scripts/validate-version-coherence.fsx` so all failures across US1+US2 are collected and reported together (no early-exit hiding a second drift), and the readiness report lists every named location
- [X] T025 [US2] Run T017/T018/T019 fixtures + quickstart Scenarios C/D end-to-end; confirm each goes red on the named location with NO `WarningsAsErrors` policy active, and the coherent tree still exits 0; record in `specs/209-version-staleness-guard/readiness/version-coherence.md`

**Checkpoint**: US1 AND US2 both work — every structural half-bump class (BOM token/bracket/member,
template hardcode/skew, duplicate literal, broken regex) is caught at the gate, policy-independent.

---

## Phase 5: User Story 3 - The pinned version must resolve to the complete real coherent set (Priority: P3)

**Goal**: A real pack→restore grounds the structural facts (FR-008, anti-text-grep): the pinned `V` must
resolve to the **complete** 16-member set in a clean consumer, or fail loudly — never a silent partial graph.

**Independent Test**: `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx`
packs from source, restores `FS.GG.UI@V`, asserts 16/16 resolve to `V` (quickstart Scenario F).

### Tests for User Story 3 ⚠️ (write FIRST, must FAIL before implementation)

- [X] T026 [P] [US3] Forced-drift fixture for `restore-partial` (data-model §7, contract acceptance US3 #1/#2): simulate a member not resolving to `V` (e.g. pack 15 of 16 members, or point at a half-published version), run the live layer, assert it fails loudly with `DRIFT [restore-partial] … expected all members @<V>; actual FS.GG.UI.<X> @<other>` and never reports success on a partial graph — MUST fail before the live layer exists

### Implementation for User Story 3

- [X] T027 [US3] Implement the env-gated restore-grounded proof layer (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`) in `scripts/validate-version-coherence.fsx` (D4): pack `FS.GG.UI.*` framework + BOM from source at the pinned `V` to a throwaway feed under the system temp dir, restore `FS.GG.UI@V` in a clean consumer (reuse `scripts/validate-bom-consumer.fsx`'s clean-consumer layer), assert every resolved member == `V` over the **complete** set, and emit `DRIFT [restore-partial]` on any member off `V`; an undefined version property fails fast at exit 2 (fails closed)
- [X] T028 [US3] Extend the readiness report write to add `provenance: live` and `resolved-members-at-version: 16/16` when the live layer ran (contract §1 Output, quickstart Scenario F)
- [X] T029 [US3] **Wire the scoped restore-grounded proof into the gate (fixes the FR-008 gate-grounding gap)**: add a second invocation `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx` to the "Version coherence guard" step in `.github/workflows/gate.yml` (the step T015 created), so every PR runs the structural verdict-core AND the scoped restore (one Release pack + one clean restore) merge-blocking — without this the gate is structural-only and a green check never restores (contract §3, research D4). Depends on T027 (the live layer must exist first)
- [X] T030 [US3] Run T026 fixture + quickstart Scenario F end-to-end; confirm 16/16 members resolve to `V` on the coherent tree and the partial-set simulation fails with `restore-partial`; record in `specs/209-version-staleness-guard/readiness/version-coherence.md`

**Checkpoint**: All three user stories independently functional — structural drift AND restore-grounded
incompleteness are both caught, and the gate runs both layers merge-blocking.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Gate reviewer ergonomics, optional release-lane wrapper, cross-repo contract upkeep, docs.

- [X] T031 [P] Add the gate step summary (SC-006): on failure echo the `DRIFT […]` lines to `$GITHUB_STEP_SUMMARY` in the "Version coherence guard" step of `.github/workflows/gate.yml` so the reviewer sees the named location without opening logs
- [X] T032 **Confirm FR-008's full clause is covered by the release lane (fixes the FR-008 full-build coverage gap)**: verify `.github/workflows/release.yml`'s Package.Tests / product-from-template job still performs a real **generate→restore→build of a product from the template** at the pinned `V` across profiles (the gate's scoped restore in T029 grounds the *pin resolves to the complete set* claim, but the full product build stays on release — contract §4, research D4). If the release job exists and runs, record it as the unchanged pre-existing guarantee in `specs/209-version-staleness-guard/readiness/version-coherence.md`; if it has regressed or does not cover product-from-template at `V`, file the gap (do not silently rely on it)
- [X] T033 [P] (OPTIONAL release-lane wrapper — mirrors, never replaces, the canonical shell scenarios of T010/T011/T017–T019/T026; **A1 authority**) Add `tests/Package.Tests/Feature209VersionCoherenceTests.fs` — an xUnit wrapper that re-derives the structural verdict env-free and asserts the coherent baseline passes + the same forced-drift fixtures go red, for the release lane and local dev; it MUST stay in parity with the documented scenarios in the readiness report (which remain the source of truth). The full generate→restore→build of a product from the template stays in `release.yml` (T032), not duplicated here — contract §4
- [X] T034 Wire the cross-repo contract note (FR-010, D8) via the `cross-repo-coordination` skill: record an ADR/note in `FS-GG/.github` that the `fs-gg-ui-version` / `fs-gg-ui-bom` registry `coherent` row is now enforced structurally by this repo's gate before merge — **after** in-repo verification passes (the 208 ordering: in-repo verify → cross-repo flip). No registry schema change (Tier 2, upheld not modified)
- [X] T035 [P] Run the full quickstart.md validation (Scenarios A–G, "Done when") and confirm A passes clean after each restore, B–E go red on the named location, F resolves 16/16, G blocks a drifting PR at the gate
- [X] T036 [P] Update the cross-repo / versioning docs as needed (e.g. `docs/ci/cadence-map.md` for the new gate step; note the decoupled repo-root `<Version>` per D5) and confirm the readiness report `specs/209-version-staleness-guard/readiness/version-coherence.md` is regenerated and committed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** T004 (surface map) →
  T005/T006 (early live smoke run — MUST precede any rule) → T007 (skeleton) → T008/T009 (comparator +
  readers, parallel).
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 → US2 → US3 in priority order; US2 and
  US3 each reuse the verdict skeleton/readers from Phase 2 but layer independent rules, so they are
  testable independently. US3's live layer is independent of US1/US2 rules.
- **Polish (Phase 6)**: Depends on the user stories it touches (T031 needs T015's gate step; T034 needs
  US1–US3 verified in-repo). NOTE: T029 (gate live-layer wiring) lives in US3, not Polish, because it
  depends on the live layer T027.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3. Delivers the MVP (204 + phantom drift).
- **US2 (P2)**: After Foundational. Adds independent rule families into the same script; the T024
  aggregation merely collects them. Independently testable.
- **US3 (P3)**: After Foundational. Independent live layer; does not need US1/US2 rules to run. Its gate
  live-layer wiring (T029) depends on T015's gate step (created in US1) — so US3's gate step completes
  the gate that US1 started, but US3's *rule logic* remains independently testable via the local run (T030).

### Within Each User Story

- Forced-drift fixture tests written FIRST and FAIL before the rule exists, then pass after.
- Readers (Phase 2) before rules; rules before aggregation/report; report before gate end-to-end run.

### Parallel Opportunities

- Setup: T003 ∥ (T001/T002 sequential — T002 needs the dir).
- Foundational: T008 ∥ T009 (comparator vs readers — same file but independent functions; if authored by
  one developer, sequential is fine — the [P] marks logical independence).
- US1 tests T010 ∥ T011; US2 tests T017 ∥ T018 ∥ T019; US2 rules T020 ∥ T021 ∥ T022 ∥ T023 (independent
  rule functions).
- Polish: T031 ∥ T033 ∥ T035 ∥ T036 (T032 release-lane check and T034 cross-repo note are sequential —
  T034 runs only after in-repo verification passes).
- Across stories: once Foundational is done, US1/US2/US3 can be staffed in parallel (different rule
  families; coordinate the single `Verdict` aggregation point T024/T014). Caveat: T029 (US3) edits the
  same `gate.yml` step as T015 (US1), so serialize those two workflow edits.

---

## Parallel Example: User Story 2

```bash
# Write all US2 forced-drift fixtures first (must fail):
Task: "BOM half-bump fixture (Scenario C) — assert DRIFT [bom-pin-not-token]"
Task: "Unwired member fixture (Scenario D) — assert DRIFT [bom-member-skew]"
Task: "Hardcoded template pin fixture — assert DRIFT [template-pin-hardcoded]"

# Then implement the independent rule families together:
Task: "bom-pin-not-token + bom-exact-bracket rules (FR-004, policy-independent)"
Task: "bom-member-skew rule (B.ids == P.members)"
Task: "template-pin-hardcoded + template-consumed-skew rules (T ⊆ P, allDerive)"
Task: "single-source-not-unique + runtime-regex-broken rules"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup.
2. Phase 2 Foundational — **including the early live smoke run (T005/T006)** that confirms today's tree
   is coherent/restorable and that the 204 drift is currently silent, BEFORE building any rule.
3. Phase 3 US1 — the 204-lag + phantom-version guard wired merge-blocking into `gate.yml`.
4. **STOP and VALIDATE**: reintroduce the 204 drift → gate red on the named location; restore → green.
5. This alone closes the headline bug class (SC-001, SC-005).

### Incremental Delivery

1. Setup + Foundational → foundation ready (comparator, readers, verdict seam, live-restore confirmed).
2. US1 → 204 + phantom drift blocked at the gate → MVP.
3. US2 → every structural half-bump class (BOM/template/regex/duplicate) blocked, policy-independent.
4. US3 → restore-grounded proof (anti-grep) — the pin resolves to the complete real set, and the gate
   step is upgraded to run that scoped restore merge-blocking (T029).
5. Polish → step summary, release-lane FR-008 coverage check, optional release wrapper, cross-repo
   contract note, docs.

---

## Notes

- [P] = independent function/file, no dependency on an incomplete task. The guard is one script, so
  several [P] rule tasks touch `scripts/validate-version-coherence.fsx` — they are independent *logic*;
  serialize the edits if one developer authors them, and converge at the single `Verdict` aggregation.
- The guard is **verify-centric** (D3) — no propagation/bump-rewrite script; if T006 finds a stray
  hardcoded pin, the fix is to re-route it through `$(FsGgUiVersion)`, not to add propagation.
- The verdict **fails closed** (exit 2) on unreadable inputs or unfetched tags — never green-by-absence.
- Repo-root `<Version>` (`0.1.0-preview.1`) is **decoupled by default** (D5) — the verdict does NOT
  compare it; the reversal trigger is documented in data-model.md.
- **Fixture authority (A1)**: the forced-drift fixtures (T010/T011/T017–T019/T026) are authoritatively
  the **documented shell scenarios** recorded in `readiness/version-coherence.md` (mirroring quickstart.md).
  The optional `tests/Package.Tests` xUnit wrapper (T033) re-derives the same verdict for the release lane
  and MUST stay in parity — it never replaces the documented scenarios as the source of truth.
- **Gate runs both layers (G1)**: T015 wires the structural verdict-core (US1 MVP); T029 adds the scoped
  restore-grounded proof to the same gate step (US3) so every PR is both structurally checked and
  restore-grounded merge-blocking. The full product-from-template build stays on the release lane (T032).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.

---

## Implementation evidence (2026-06-28 — all tasks complete)

- **Guard shipped**: `scripts/validate-version-coherence.fsx` — env-free structural verdict-core +
  `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1` restore-grounded proof; exit 0/1/2 (coherent/drift/fail-closed).
- **Real drift caught (fail-before, stronger than synthetic)**: the guard flagged a genuine
  Feature-204 staleness in the tree — pin `0.1.50-preview.1` lagging the latest snapshot tag
  `fs-gg-ui/v0.1.51-preview.1` (`DRIFT [pin-lags-tag]`, exit 1). **Coherent remediation applied**
  (D2/D3): `<FsGgUiVersion>` bumped to `0.1.51-preview.1` (tag + all 16 members + BOM present in feed).
- **Pass-after**: structural verdict-core exit 0; live layer `resolved-members-at-version: 16/16 at
  0.1.51-preview.1`, clean-consumer-build pass (`readiness/version-coherence.md`, provenance: live).
- **Fixtures (A1 canonical shell scenarios)** recorded in `readiness/version-coherence-scenarios.md`:
  B `pin-lags-tag`, E `pin-no-tag`, C `bom-pin-not-token` (no warnings-as-errors), D `bom-member-skew`,
  hardcoded-pin `template-pin-hardcoded`, F live 16/16, restore-partial negative (disclosed Synthetic).
  Aggregation verified (multiple DRIFT lines, no early-exit).
- **Gate wired** (`.github/workflows/gate.yml`): new merge-blocking "Version coherence guard" step runs
  both layers; `actions/checkout` set `fetch-depth: 0` so `git tag` sees `fs-gg-ui/v*`.
- **Release-lane (T032)**: `release.yml` `template-product-tests` still does the full
  generate→restore→build of a product from the template at `V` — pre-existing FR-008 guarantee, unchanged.
- **Optional wrapper (T033)**: `tests/Package.Tests/Feature209VersionCoherenceTests.fs` — 7 Expecto
  tests mirror the scenarios env-free; Package.Tests green at **139** (132 + 7).
- **Baseline (T002)**: `readiness/baseline.md` — 21/21 green, 0 pre-existing reds.
- **Cross-repo (T034)**: `FS-GG/.github#4` PR adds the `enforcement:` note to the `fs-gg-ui-version` /
  `fs-gg-ui-bom` coherence rows (no schema change, no coherent flip); Coordination board P5 epic →
  *In review*.
- **Readiness allowlist**: `.gitignore` allowlists `specs/209-.../readiness/` (nuget-cache still ignored);
  `git check-ignore` confirms the report is staged while caches are not.
