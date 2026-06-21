# Implementation Plan: Shared Test/Util Helpers (Code-Health Refactoring Phase 1)

**Branch**: `178-shared-test-util-helpers` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/178-shared-test-util-helpers/spec.md`

## Summary

Three small utilities are copy-pasted across the repository; this feature extracts each into a
single shared definition and routes all call sites through it, deleting the duplicates. It is a
**Tier-2, behavior-preserving refactor**: no public API surface change, byte-identical hashes and
clamped values, build + full test suite stay green.

The three consolidations, in priority order:

1. **Repo-root finder** (P1) — one shared finder for every test/harness project.
2. **FNV-1a hash primitive** (P2) — one shared primitive behind the four `src/Controls` folds.
3. **`clamp`** (P3) — one shared bounding function for the `src` call sites.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature changes no runtime behavior, so it carries **no defect/root-cause hypothesis to
> confirm against a running app**. The honest "real evidence" here is the existing regression
> machinery: a clean `dotnet build` of the slnx plus the full `dotnet test` run — in particular the
> Feature 159 identity/reuse/promotion suites and the composition/control fingerprint tests, which
> assert that hash-driven layer reuse/promotion outcomes are unchanged. `/speckit-tasks` MUST place a
> **baseline build + test capture** as the first Foundational task (recording the pre-refactor green
> state and the two documented package-feed reds) so every later consolidation is diffed against it.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (exclusive stack per constitution).

**Primary Dependencies**: SkiaSharp (GL backend) — not touched by this feature. Test stack: the
existing test framework used across `tests/*` (xUnit-style runners with `Program.fs` entry points).

**Storage**: N/A (no persisted artifacts change; golden/readiness files MUST be byte-identical).

**Testing**: Existing `tests/*.Tests` projects plus `tests/Rendering.Harness*`. Key regression
gates: Feature 159 relational identity/reuse/promotion suites, composition/control fingerprint
tests, and the path-dependent tests that consume the repo-root finder.

**Target Platform**: Linux (primary dev/CI); the refactor is platform-neutral.

**Project Type**: F# UI framework / rendering library — single multi-project solution
(`FS.GG.Rendering.slnx`) with `src/*` packages and `tests/*` test projects.

**Performance Goals**: No regression. The FNV folds sit on hashing paths flagged `// mutable: hot
path`; the shared primitive MUST stay allocation-free and inline-friendly so the hot folds keep
their current shape.

**Constraints**:
- No public API surface change (`.fsi`/surface-area baselines stay green) — FR-007.
- No new public **package** surface (nothing test-only ships in an `FS.GG.UI.*` package).
- No new module/project cycle (test projects don't reference each other; `src/Controls` internal
  order must be preserved).
- Byte-identical hashes and clamped values; byte-identical golden/readiness artifacts.

**Scale/Scope**:
- Repo-root finder: **~59 files** carry a finder (named `findRepositoryRoot` **or** an inline
  `FS.GG.Rendering.slnx` walk) — materially larger than the spec's "~26" estimate (see research).
- FNV: **4 fold sites** across 3 files in `src/Controls`, using **3 distinct mixing conventions**.
- clamp: **3 local `let clamp` definitions** in `src` (`TextInput.fs`, `RetainedRender.fs`,
  `SkiaViewer/Host/OpenGl.fs`); `Layout.clampNonNegative` is a *different* function and is
  out of scope.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Impl | ✅ | No new public surface, so no new `.fsi`/FSI surface to design. Internal helpers are exercised through the existing public folds/finders their call sites use; behavior is pinned by the existing semantic/regression suites (Feature 159, fingerprint, path-dependent tests). |
| II. Visibility Lives in `.fsi` | ✅ | New helpers add **no** public surface. FNV helper is `module internal` with **no** `.fsi` (the established `Internal/AttrKeys.fs` / `WidgetLowering` / `SceneRenderer` precedent). No `private`/`internal`/`public` keyword on any new top-level **binding** (the `module internal` modifier matches precedent). |
| III. Idiomatic Simplicity | ✅ | Pure functions, plain `min`/`max`, a single mutable FNV accumulator (already disclosed `// mutable: hot path`). No SRTP/reflection/CE/type-provider/custom-operator use introduced. |
| IV. Elmish/MVU boundary | ✅ | N/A — no stateful/I/O workflow added; pure helpers only. |
| V. Test Evidence | ✅ | Behavior is preserved, so evidence = existing suites stay green (fail-before/pass-after does not apply to a no-op refactor; the gate is *no new red*). Baseline capture task makes the green state explicit. No synthetic evidence introduced. |
| VI. Observability & Safe Failure | ✅ | The shared repo-root finder **preserves** fail-loud behavior with an actionable message when no marker is found up to the filesystem root (FR-002). |
| Change Classification | ✅ | **Tier 2 (internal)**: no public API surface added/removed/modified; `.fsi` and baselines remain untouched (FR-007, SC-005). |

**Result: PASS.** No violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/178-shared-test-util-helpers/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — divergence reconciliation + placement decisions
├── data-model.md        # Phase 1 output — the three helper entities
├── quickstart.md        # Phase 1 output — validation/run guide
├── contracts/           # Phase 1 output — internal helper signatures + invariants
│   ├── repo-root-finder.md
│   ├── fnv-hash-primitive.md
│   └── clamp.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Scene/                       # zero-dependency base; referenced by Controls + SkiaViewer
├── Controls/
│   ├── Internal/
│   │   ├── AttrKeys.fs           # existing internal precedent (module internal, no .fsi)
│   │   └── Hashing.fs            # NEW — shared FNV-1a primitive (module internal, no .fsi)
│   ├── Composition.fs            # migrate fnv1a → Hashing
│   ├── Control.fs                # migrate hashScene + fingerprintParts/String → Hashing
│   ├── RetainedRender.fs         # migrate feature159Hash → Hashing; drop local clamp
│   └── TextInput.fs              # drop local clamp
├── SkiaViewer/Host/OpenGl.fs     # drop local clamp
└── Shared/                       # NEW (linked-source convention; not a packaged project)
    └── Numeric.fs                # NEW — shared clamp (module internal, no .fsi), linked into
                                  #       Controls + SkiaViewer via <Compile Include link>

tests/
├── TestSupport/                  # NEW — non-packed shared test-support project
│   ├── TestSupport.fsproj        #       <IsPackable>false</IsPackable>
│   └── RepositoryRoot.fs         #       the single shared finder + resolved root value
├── Controls.Tests/ … Smoke.Tests # ~59 files: delete local finders, reference TestSupport
└── Rendering.Harness*/           # same migration for harness projects
```

**Structure Decision**: Three placements, each chosen to satisfy "one definition" **and**
"no public package surface" (research records full rationale and alternatives):

- **Repo-root finder → new non-packed `tests/TestSupport` project** (`IsPackable=false`),
  referenced by every consuming test/harness project. Test projects don't reference each other
  today, so a shared, explicitly non-packaged test assembly is the cleanest cycle-free home and
  ships in no package.
- **FNV primitive → `src/Controls/Internal/Hashing.fs`**, `module internal`, **no** `.fsi`,
  compiled before `Composition.fs`. All four folds are inside `src/Controls`, so an
  assembly-internal module reaches them with no new cross-project edge and no surface change —
  exactly the `Internal/AttrKeys.fs` precedent.
- **clamp → `src/Shared/Numeric.fs`**, `module internal`, **no** `.fsi`, **linked** (not
  project-referenced) into `src/Controls` and `src/SkiaViewer`. clamp's two consumers share only
  `Scene`/`Diagnostics`; a *public* clamp there would change a package surface (FR-007 violation),
  so a single linked internal source file is the only way to get one source definition with zero
  public surface and no new package.

## Complexity Tracking

> No constitution violations — section intentionally empty.

## Implementation Progress

**Status: COMPLETE — all 25 tasks (T001–T025) done; all three stories shipped.** Evidence:
`readiness/baseline.md` (pre) and `readiness/post-change.md` (post + refactor evidence section).

| Phase | Tasks | Outcome |
|-------|-------|---------|
| 1 Setup | T001–T003 | Baseline captured: 16 green / 2 red (documented package-feed reds). Duplicate counts snapshotted (59 finder files, 4 FNV sites, 3 clamp defs). |
| 2 Foundational | T004–T008 | Created `tests/TestSupport` (RepositoryRoot, `IsPackable=false`), `src/Controls/Internal/Hashing.fs` (`module internal`), `src/Shared/Numeric.fs` (linked into Controls + SkiaViewer). Smoke build/test matched baseline. |
| 3 US1 (P1) | T009–T012 | Migrated **55 finder files across 9 projects** to `RepositoryRoot.find`/`.value`. Reconciled **three** families (the spec's two + a third `repoRoot`/`Directory.Packages.props` variant). Zero local finders remain. |
| 4 US2 (P2) | T013–T016 | Four `src/Controls` folds now draw constants + core `step` from `Hashing`; byte-identical (Feature159 + Fingerprint green). `0xcbf29ce484222325UL` lives only in `Hashing.fs`. |
| 5 US3 (P3) | T017–T021 | Three local `clamp` copies removed; all route through `Numeric.clamp`. `Layout.clampNonNegative` left untouched (different function). |
| 6 Polish | T022–T025 | Post-change run **identical** to baseline (no regression). `.fsi` diff empty (no surface change). Net **−274 lines** of code. |

### Notable deltas from the plan as written
- **Finder families: three, not two.** Beyond Family A (`findRepositoryRoot`) and Family B (inline
  `FS.GG.Rendering.slnx` walk), a third variant `let rec private repoRoot (dir: DirectoryInfo)`
  testing the `Directory.Packages.props` marker exists in `Scene.Tests`, `SkiaViewer.Tests`, and
  `Rendering.Harness.Tests`. All resolve to the same repo root (it carries the slnx, build markers,
  and `Directory.Packages.props`), so routing them to the canonical finder preserves behavior.
- **`FS.GG.Rendering.slnx` is not always a finder.** The string also appears as `dotnet build/pack`
  command-argument literals in the harness (`Cli.fs`, `Compositor.fs`, `PackageFeed.fs`,
  `ValidationLanes.fs`) and a few `*.Tests` command-assertion lists. Those are **not** finders and
  were intentionally left in place; the SC-002 grep is therefore interpreted as "zero finder
  *definitions*", which holds.
- **Net reduction ≈ −274 lines**, not the four-figure drop the plan estimated — many finders were
  compact inline blocks (~5 lines), not the ~15–20-line named form.
