# Implementation Plan: Cross-Cutting Dedup + State Records (Pattern C)

**Branch**: `186-cross-cutting-dedup-state-records` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/186-cross-cutting-dedup-state-records/spec.md`

## Summary

Phase 2 of the god-module decomposition (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §6).
A **byte-identical-by-construction** Pattern-C refactor: collapse the duplicated per-frame metrics
tuple into one internal builder, gather the scattered `let mutable` accumulators in
`RetainedRender.step`/`init` and `ControlsElmish.runScriptCore` into named state records, and unify
two copy-pasted blocks in the Testing layer (inspection validation; markdown managed-section update).
No behavior change: rendered frames, per-frame metrics, and emitted evidence artifacts stay
byte-identical (semantically equivalent where wording legitimately varies). This is the prerequisite
that gives `RetainedRender.step` an explicit named state record before the Phase 6 pipeline split.

**Central design constraint (discovered, not in the spec's estimates):** the existing
`FrameMetrics` type, both `validateCheck` functions, and all three `updateManagedSection` functions
are **public** (declared in the curated `.fsi` files). FR-009 forbids changing the public surface.
Therefore every dedup in this phase **introduces a NEW internal helper and rewrites the existing
public function/site as a thin delegator** — the `.fsi` files stay byte-identical, all new
records/builders are private-by-absence-from-`.fsi`, and no surface baseline regenerates. This is
fully consistent with the spec's "all new types MUST be internal" (FR-009) and is the spine of the
design below.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.** Not
> applicable in the usual sense: this is a pure structural refactor, not a defect fix. The analogous
> obligation here is the **baseline-first** discipline (Assumptions §"Verification spine"): capture a
> pre-refactor baseline of frames/metrics/artifacts and the red/green test set in the Foundational
> phase *before any production edit*, then diff every user story against it. `/speckit-tasks` MUST
> schedule that baseline capture as the first Foundational task.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: SkiaSharp over OpenGL (GL); Expecto test framework
(`Microsoft.NET.Test.Sdk` host). No new dependency is added (FR-010).

**Storage**: N/A (in-process render loop + filesystem evidence/readiness artifacts under `specs/…`).

**Testing**: Expecto, run via `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` (GL via X11).
Affected projects: `tests/Controls.Tests`, `tests/Elmish.Tests`, `tests/Testing.Tests`,
`tests/Rendering.Harness.Tests` (FR-008).

**Target Platform**: Linux desktop (SkiaSharp/GL viewer host); CI deterministic tier.

**Project Type**: F# UI framework library (`FS.GG.UI.*` packages) — single-repo, multi-project.

**Performance Goals**: No regression and no improvement target — byte-identical frame output and
metrics are the contract (FR-007). The §7 golden-image/perf gates are **explicitly out of scope**
for this byte-identical phase (spec Assumptions; parent report §7 scopes them to render-altering
Phases 5–6).

**Constraints**: Byte-identical rendered frames + per-frame metrics (FR-007); preserved float
accumulation order in the `step` accumulators (Edge Cases); preserved fail-loud behavior at every
deduplicated site (FR-011); unchanged public surface and no version bump (FR-009); no new
project/dependency/inter-project reference (FR-010).

**Scale/Scope**: 4 user stories, 2 source projects touched (`src/Controls`, `src/Controls.Elmish`,
`src/Testing`), ~5 dedup targets. Re-confirmed current-tree counts (2026-06-22), which **revise the
spec's estimates**:

| Target | Spec estimate | Confirmed current tree | Location |
|---|---|---|---|
| `FrameMetrics` fields | ~36 | **32** | `src/Controls.Elmish/ControlsElmish.fs:63–97` (public, `.fsi:69`) |
| Full metrics construction sites | ~5 | **2** full spell-outs (+4 `{ zero with … }` partials) | `ControlsElmish.fs:1423–1460`, `1957–1990` (partials at 2026/2092/2132/2171) |
| `step` loose mutables | ~19 | **19** | `src/Controls/RetainedRender.fs:1455+` (init seeding parallels at `1289–1341`) |
| `runScriptCore` mutables | ~10 | **10** total; **7** are metric carriers | `ControlsElmish.fs:1835` (private); carriers `1849–1865`, 3 state (`model`/`retained`/`lastRender`) at `1840–1845` |
| Inspection-validator overlap | ~95 lines | **~65–71 lines** each | `TestingVisual.fs:995–1065` (`VisualInspectionValidation.validateCheck`, public) vs `TestingRetainedInspection.fs:373–438` (`RetainedInspectionValidation.validateCheck`, public) |
| `updateManagedSection` impls | 3 (~129 lines) | **3** (~41–47 lines each) | `TestingVisual.fs:642–688` + `1271–1311`; `TestingRetainedInspection.fs:654–694` (all public) |

Per spec Assumptions, the requirement is "defined/built once," not a specific count — these revised
numbers update the success-criteria targets (SC-001 → 1 builder for the 2 full sites; SC-002 → 0
loose mutables; SC-003 → 1 validation + 1 section algorithm).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Assessment |
|---|---|
| **I. Spec → FSI → Tests → Impl** | Tier-2 refactor; public `.fsi` is **unchanged** (no new public surface). New helpers are internal, so no new `.fsi` drafting. Existing tests are the FSI-surface evidence. ✅ |
| **II. Visibility lives in `.fsi`** | All new records/builders/shared routines are **omitted from the `.fsi`** → private by compiler enforcement. No `private`/`internal`/`public` modifiers added in `.fs`. The public `validateCheck`/`updateManagedSection`/`FrameMetrics` signatures stay byte-identical. ✅ |
| **III. Idiomatic simplicity** | State-record-with-mutable-fields is the plainest shape for the hot path and is **repo-proven** (feature 182 `FrameLoopState`). Constitution explicitly allows mutation on a measured hot path; each mutable field carries a `// mutable: hot path` disclosure. No SRTP/reflection/custom operators/CEs introduced. ✅ |
| **IV. Elmish/MVU boundary** | No new stateful/I-O workflow; the existing Elmish boundary is untouched. N/A. ✅ |
| **V. Test evidence** | No behavior change ⇒ the obligation is "same tests fail/pass as before" + byte-identical output, captured against a pre-refactor baseline. No assertion weakened (FR-008). No synthetic evidence introduced. ✅ |
| **VI. Observability & safe failure** | Fail-loud preserved at every deduplicated site (imbalanced markers, invalid declared exceptions) — FR-011. ✅ |
| **Change Classification** | **Tier 2 (internal change)** — refactor, no behavioral change, no public API surface change. `.fsi` and surface baselines remain untouched; no version bump (FR-009). ✅ |
| **Engineering Constraints** | No new project, dependency, or inter-project reference (FR-010). Stays within `src/Controls`, `src/Controls.Elmish`, `src/Testing`. ✅ |

**Gate result: PASS.** No violations → Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/186-cross-cutting-dedup-state-records/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal-helper contracts)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/
├── RetainedRender.fs     # US2: introduce FrameState record; collapse step's 19 mutables;
│                         #      converge init's 1289–1341 seeding onto the shared record
└── RetainedRender.fsi    # UNCHANGED (FrameState/seeding are internal, absent here)

src/Controls.Elmish/
├── ControlsElmish.fs     # US1: FrameMetricsBuilder (internal) → 1 build site, 2 spell-outs delegate
│                         # US2: FrameScriptState record for runScriptCore's metric carriers
└── ControlsElmish.fsi    # UNCHANGED (public FrameMetrics type stays; builder/state are internal)

src/Testing/
├── TestingVisual.fs                # US3: shared internal validation routine (visual delegates);
│                                   # US4: shared internal ManagedSection (2 writers delegate)
├── TestingVisual.fsi               # UNCHANGED (public validateCheck/updateManagedSection stay)
├── TestingRetainedInspection.fs    # US3: retained delegates (admits Warning + ReviewRequired);
│                                   # US4: third writer delegates to shared ManagedSection
└── TestingRetainedInspection.fsi   # UNCHANGED

tests/Controls.Tests/              # US1/US2 frame + metrics byte-identity (RetainedRender, init)
tests/Elmish.Tests/                # US1/US2 runScriptCore metrics byte-identity
tests/Testing.Tests/               # US3/US4 inspection-validation + managed-section behavior
tests/Rendering.Harness.Tests/     # readiness/evidence artifact equivalence
```

**Structure Decision**: Single-repo multi-project F# library; no new projects or files are created —
each dedup lands **inside the existing `.fs` module** that already owns the duplicated code, behind a
new internal helper. The four user stories map cleanly: US1 + US2 to the two Controls projects, US3 +
US4 to `src/Testing`. The shared internal helper for US3/US4 lives in whichever Testing file the two
call sites can both reach without introducing a back-edge (confirmed in research.md).

## Complexity Tracking

> No constitution violations — section intentionally empty.

## Implementation Status (2026-06-22)

**All four user stories implemented, verified byte-identical, and committed** on
`186-cross-cutting-dedup-state-records`. Four source files changed; **only** 37 internal-only lines
added to one `.fsi` (`TestingVisual.fsi` `module internal SharedTesting`); no `.fsproj`/`.slnx`
change (FR-010); public surface baseline byte-identical (SC-006).

| Phase | Tasks | Status | Evidence |
|---|---|---|---|
| Setup + Foundational | T001–T005 | ✅ | Clean build at HEAD; full 16-project baseline captured (14 green, **2 pre-existing reds**: `Package.Tests` ×8, `ControlsGallery.Tests` ×2 — package-feed/sample pins, unrelated); byte baseline + internal-seam confirmed. |
| US1 — one `FrameMetrics` builder | T006–T009 | ✅ | `buildFrameMetrics` names all 32 fields once; 2 full sites delegate; 4 `{ zero with … }` partials unchanged. Elmish 209 + Controls 931 green; `ControlsElmish.fsi` diff empty (SC-001). |
| US2 — named frame state records | T010–T015 | ✅ | `step`'s 19 loose mutables → one `FrameState` (mutable fields, exact accumulation order); `init` converged; `runScriptCore`'s 7 carriers → `FrameScriptState` feeding the US1 builder. 0 loose mutables in migrated regions (SC-002); Controls 931 + Elmish 209 green; both `.fsi` empty (FR-009). |
| US3 — one inspection-validation routine | T016–T019 | ✅ | `SharedTesting.validateCheck` (generic over finding/exn/status/result via a knobs record); both validators delegate; visual accepts `Blocking` only, retained accepts `Blocking‖Warning` + derives `ReviewRequired` (severity asymmetry preserved, FR-005). Testing.Tests 104 green. |
| US4 — one managed-section updater | T020–T023 | ✅ | `SharedTesting.updateManagedSection` (append/replace/**fail-loud**, FR-011); all 3 writers delegate; per-writer result type + wording injected; dead `countOccurrences` copies removed. Testing 104 + Harness 209 green. |
| Polish | T024–T028 | ✅ | Surface byte-identical (SC-006); no new dep (FR-010); SC-007 walkthrough + per-phase feedback captured; final full-solution sweep red/green identical to baseline (no assertion weakened, SC-004/FR-008). |

**Verification spine:** no new tests authored; each story re-ran the affected suites and confirmed
identical red/green + byte-identical frames/metrics + empty public `.fsi`/surface diffs against the
pre-refactor baseline. The two pre-existing reds remained exactly those two (no regression).
