# Implementation Plan: Clear the disclosed pre-existing test reds and baseline flakiness

**Branch**: `203-fix-disclosed-test-reds` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/203-fix-disclosed-test-reds/spec.md`

## Summary

Feature 202 disclosed — rather than fixed — four standing test-debt conditions that keep the
comprehensive baseline (`scripts/baseline-tests.fsx`) non-green and noisy: (1) stale `FS.GG.UI.*`
sample package pins, (2) a missing design-system validation report the *Feature128* gate audits,
(3) drifted sample count/bijection assertions (catalog grew 96→97), and (4) nondeterministic
`SkiaViewer.Tests` GL/window-system flakiness. This feature makes the baseline **genuinely green and
deterministic** so the disclosures become obsolete, fixing each condition with the repo's existing,
constitution-aligned mechanisms and **without weakening any assertion** (Constitution V) and **without
masking a real GL defect as "unsupported environment"** (Constitution VI).

Technical approach by user story:

- **US1 (P1) — pins:** Use the canonical `tools/Rendering.Harness package-feed --mode refresh --pack`
  workflow (wrapped by `scripts/refresh-local-feed-and-samples.fsx`) to pack the source packages to the
  local feed and rewrite every sample pin to the source-controlled version coherently, then assert with
  the *Feature163* gate and per-sample pin checks. ~64 stale pins across AntShowcase, SampleApps,
  SecondAntShowcase, ControlsGallery are corrected in one coherent pass.
- **US2 (P1) — design-system gate:** Make the *Feature128* gate self-provisioning. The validator's
  **verdict-core** phase is pure, local-file, env-free; have the gate (fixture / setup) produce the
  report from verdict-core when absent so it is **not red by default in a fresh checkout** (FR-002,
  spec assumption #4), instead of relying on a contributor hand-running an env-gated generator.
- **US3 (P2) — drifted assertions:** Correct the sample count/bijection assertions to their **true
  current values** and assign/classify the new 97th control so the bijection and contract-coverage
  assertions pass *as written* — never loosen `=` to `>` or delete a check (FR-003, Constitution V).
- **US4 (P3) — GL determinism:** Apply the repo's established `rasterAvailable` capability-probe +
  `skiptest`/`tierSkip` idiom (already canonical in `Audit_ReplayCache.fs`) to the flaky raster/live
  GL tests, so an unavailable GL/window-system yields a **deterministic skip-with-rationale**
  (Constitution VI) rather than an intermittent red.

This is a **Tier 2 (internal/corrective)** change: no public `.fs`/`.fsi` surface changes, no new
dependencies, no inter-package contract changes (FR-007). The non-test edits are sample `.fsproj`
pin versions (mechanically rewritten) and the sample `*.Core` page-assignment / classification source
that owns the 97th control (US3 — samples carry no `.fsi` baseline, so this stays Tier 2); the only
test-side edits are the Feature128 self-provisioning fixture, the extended Feature163 gate, the
corrected sample assertions, and the SkiaViewer GL guards.

> **Standing assumption — root-cause hypotheses are unverified until run.**
> The four root-cause hypotheses above are provisional. The Foundational phase MUST schedule an
> **early live baseline run** (drive `scripts/baseline-tests.fsx` and the four affected projects, capture
> real red/green + the actual catalog count and the actual flaky test names) that confirms or replaces
> these hypotheses **before** any fix work begins. In particular, ControlsGallery's `52`-vs-`97`
> question and the precise identity of the unreferenced/unclassified 97th control are confirmed by that
> run, not assumed here.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (SDK observed: dotnet 10.0.301).

**Primary Dependencies**: SkiaSharp over OpenGL (GL); Expecto (test framework, skip API `skiptest`);
Yoga (layout); Elmish; local NuGet feed at `~/.local/share/nuget-local/`. Pin/feed orchestration via
`tools/Rendering.Harness` `package-feed` command.

**Storage**: Local file system only — source `.fsproj` versions are the single source of truth for
package versions; the local NuGet feed holds packed `.nupkg`s; readiness artifacts under
`specs/*/readiness/` (gitignored, transient).

**Testing**: Expecto per-project (`dotnet test`); comprehensive sweep via `scripts/baseline-tests.fsx`
(solution + `tests/Package.Tests` + `samples/**/*.Tests` + `tests/SkiaViewer.Tests`). Determinism
measured by repeated runs.

**Target Platform**: Linux (headless-capable). GL/display capability is **optional** — tests must
degrade to explicit skip when it is absent, never fail intermittently.

**Project Type**: Multi-package F# UI framework product + samples + tests (single repository).

**Performance Goals**: N/A. The goal is **determinism and a 0-red baseline**, not throughput. SC-004:
5 consecutive `SkiaViewer.Tests` runs yield an identical pass set.

**Constraints**:
- No assertion may be weakened, broadened, or deleted to green a build (Constitution V; FR-003).
- GL smoke failures MUST distinguish an implementation defect from a missing window-system; a genuine
  defect MUST NOT be masked as "unsupported environment" (Constitution VI; edge case).
- No public-surface, governance, or evidence gate may regress; no previously-passing test may regress
  (FR-007). No `.fsi` / surface-baseline changes (Tier 2).
- Every sample `FS.GG.UI.*` pin MUST equal the source-controlled version **and** resolve from the local
  feed (the feed must contain those exact versions — guaranteed by `--pack`).
- Any irreducible environment residue MUST be an explicit, deterministic, *written-rationale* skip,
  with the skip count stated (FR-005/FR-006; SC-005).

**Scale/Scope**: ~64 stale pins across 4 samples (AntShowcase, SampleApps, SecondAntShowcase,
ControlsGallery); 8 `Package.Tests` reds (7× *Feature128* GV-1..GV-7 + 1× *Feature163*); 3 sample
suites with count/bijection drift (catalog 96→97); ~7–10 GL-sensitive `SkiaViewer.Tests` cases.
Bounded to the four conditions feature 202 disclosed (spec Assumptions); incidental finds are recorded,
not necessarily fixed.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.1.0. Re-checked after Phase 1 design.*

| Principle / Constraint | Assessment | Status |
|---|---|---|
| **V — Test Evidence Is Mandatory; never weaken an assertion to green; skip-with-rationale for out-of-scope** | Core constraint. US3 corrects assertions to true values (no loosening); US4 uses the framework skip mechanism (`skiptest`) with written rationale. FR-003 restates this. | ✅ Aligned |
| **VI — Observability & Safe Failure; GL smoke must distinguish defect vs missing window-system** | US4 probes capability and skips deterministically; the early baseline run must confirm flakiness is environment-bound, not a hidden defect (edge case + spec assumption). | ✅ Aligned |
| **II — Visibility in `.fsi`; surface-drift check** | No public surface change intended (Tier 2). Feature128 fix stays test-side; pin bumps touch only `.fsproj` versions. Surface-drift check must stay green (FR-007). | ✅ No `.fsi` edits |
| **I — Spec → FSI → Semantic Tests → Implementation** | Tests already exist and already fail (the reds); this feature makes them pass honestly. Failing-first is satisfied by the pre-existing reds. | ✅ Aligned |
| **III — Idiomatic Simplicity** | Reuses existing mechanisms (`package-feed` refresh, verdict-core generation, `rasterAvailable`+`skiptest`). No new abstractions, operators, or SRTP/reflection. | ✅ Aligned |
| **IV — Elmish/MVU boundary** | No new stateful/I/O workflow introduced; not triggered. | ✅ N/A |
| **Change Classification** | **Tier 2 (internal/corrective)**: no public API, no new deps, no inter-package contract change (FR-007). `.fsi`/baselines untouched. | ✅ Tier 2 |

**Result: PASS** — no violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/203-fix-disclosed-test-reds/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — root-cause decisions + the four fix mechanisms
├── data-model.md        # Phase 1 — the entities (baseline, pin, report, drifted assertion, flaky test)
├── quickstart.md        # Phase 1 — runnable validation: regenerate, refresh pins, run baseline x5
├── contracts/
│   └── baseline-green.contract.md   # Observable contract: 0 red + explicit-skip residue
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root) — files this feature touches

```text
samples/
├── AntShowcase/{AntShowcase.Core,AntShowcase.App,AntShowcase.Tests}/*.fsproj   # pin bumps (US1)
├── SampleApps/{SampleApps.Core,SampleApps.App,SampleApps.Tests}/*.fsproj        # pin bumps (US1)
├── SecondAntShowcase/{*.Core,*.App,*.Tests}/*.fsproj                            # pin bumps (US1)
├── ControlsGallery/{*.Core,*.App,*.Tests}/*.fsproj                              # pin bumps (US1)
├── AntShowcase/AntShowcase.Tests/CoverageTests.fs                               # count/bijection (US3)
├── ControlsGallery/ControlsGallery.Tests/CoverageTests.fs                       # count/bijection (US3)
└── SecondAntShowcase/SecondAntShowcase.Tests/{CoverageTests,Feature172CoverageRegressionTests,
        Feature173LiveResponsivenessRegressionTests,InteractionTests}.fs         # count + contract (US3)
        # + the *.Core page-assignment / classification source that owns the 97th control (US3)

tests/Package.Tests/
├── Feature163PackageFeedValidationTests.fs        # extend coverage to all 4 samples (US1)
└── Feature128DesignSystemTemplateTests.fs         # self-provision verdict-core report (US2)

tests/SkiaViewer.Tests/
├── Tests.fs                                       # live runApp/run skip-with-rationale (US4)
├── Feature063RendererTests.fs, Feature086SceneTranslateTests.fs,
    Feature136TextRenderingTests.fs, Feature140GlyphRunRenderingTests.fs   # rasterAvailable probe (US4)
└── Audit_ReplayCache.fs                           # CANONICAL idiom to mirror (reference only)

scripts/
├── baseline-tests.fsx                             # comprehensive sweep (source of truth; not modified)
├── refresh-local-feed-and-samples.fsx → tools/Rendering.Harness package-feed   # pin/feed mechanism (US1)
└── validate-design-system-template.fsx            # verdict-core generator the Feature128 gate uses (US2)
```

**Structure Decision**: Single-repository, multi-package F# product. This feature edits only sample
`.fsproj` pins, sample test fixtures, two `Package.Tests` gates, and `SkiaViewer.Tests` GL guards —
all corrective. No new projects, modules, or public surface.

## Complexity Tracking

> No Constitution violations — table intentionally empty.
