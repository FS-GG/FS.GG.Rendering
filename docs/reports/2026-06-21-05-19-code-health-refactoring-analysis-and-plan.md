# Code Health Analysis & Refactoring Plan — FS.GG.Rendering

**Date:** 2026-06-21 05:19 CEST
**Scope:** Whole repository (`src/`, `tests/`, harness tooling) — ~36k LOC of `src` across 15 projects plus a large test/harness tree.
**Method:** Five parallel review passes (Controls, SkiaViewer+Testing, Scene/Layout/Input, Tests+Harness, cross-cutting), cross-validated. Quantitative signals via `wc`/`grep`; targeted reads of the largest files.
**Status:** Analysis + plan only. No code changed by this report.

---

## 1. Executive summary

**Overall verdict: structurally heavy, but remarkably clean of rot.**

The codebase has **no decay-type debt**: zero `TODO`/`FIXME`/`HACK`/`XXX` markers, no commented-out code, documented skip hygiene (`SKIPPED-TESTS.md`), an acyclic dependency graph, no util/grab-bag modules, and near-complete `.fsi` signature discipline. This is a well-maintained codebase.

Its debt is almost entirely **size and duplication**, concentrated in a small number of locations rather than spread thin:

- A **5,667-line production CLI ("Rendering.Harness") mis-filed under `tests/`**.
- A **per-feature copy-forward pattern** that has metastasized into 97 near-identical render functions, 262 per-feature constants, and hundreds of `Feature*.fs` files.
- **No shared "readiness/status" type** — the same 6-value vocabulary is re-encoded as ~66 distinct DU cases across 8+ files.
- **Two competing keyboard-input stacks** (one of them a ~1,400-line near-orphan with no production consumer), and a similarly orphaned `Color` library.
- A handful of **god-files and god-functions** (several modules > 2,000 lines; individual functions of 300–600 lines).

This is a refactoring problem, not a cleanup one — a much more favorable position. The work below is sequenced so that the **lowest-risk, highest-volume duplication removal happens first**, before any structural module splits.

### Severity snapshot

| Theme | Severity | Effort | Risk |
|---|---|---|---|
| Harness mis-filed under `tests/` | High (clarity) | Low | Low |
| Per-feature copy-forward duplication | High | High | Medium |
| `findRepositoryRoot` copied ~54× | Medium | Low | Very low |
| No shared `ReadinessStatus` (~66 DU cases) | High | Medium | Medium |
| Duplicate keyboard-input / Color stacks | Medium | Medium | Low–Medium |
| God-files / god-functions | Medium | High | Medium |
| Scattered hash/clamp/JSON/markdown helpers | Low | Low | Very low |
| Stringly-typed control `Kind` dispatch | Medium | High | Medium |
| Codec write/read hand-symmetry drift risk | Medium | Medium | Medium |

---

## 2. What is genuinely healthy (do not "fix")

These are strengths to preserve; they are listed so future work does not disturb them:

- **No rot.** No `TODO`/`FIXME`/`HACK` markers anywhere in `src/` or `tests/`. No dead or commented-out code. Skipped tests are few (8 `ptest`/`ptestList` across 3 files) and all documented in `SKIPPED-TESTS.md` with rationale and un-skip conditions.
- **Clean architecture.** Acyclic, upward-fanning dependency graph. `Scene` is the dependency-free root (referenced by 17), `Diagnostics` is also dependency-free (referenced by 12). No surprising back-edges, no cycles, no grab-bag utility modules.
- **Strong `.fsi` discipline.** Every public `src` module has a companion signature file. The only 4 `.fs` without one are legitimately `module internal` (exempt under the repo's FS0078 convention): `SkiaViewer/SceneRenderer.fs`, `Color/ColorPolicy.fs`, `Controls/Internal/AttrKeys.fs`, `Controls/Widgets/WidgetLowering.fs`. The headline "157 `.fsi` / 774 `.fs`" ratio is a measurement artifact diluted by `tests/` (correctly no `.fsi`) and generated `obj/` files.

---

## 3. Size landscape

### Largest source files

| File | Lines | Notes |
|---|---|---|
| `tests/Rendering.Harness/Compositor.fs` | 5,667 | Largest file in repo; 97 `renderFeatureNNN…` fns, 262 feature constants |
| `src/Testing/Testing.fs` | 4,550 | 100 types, 29 modules in one file (functions individually OK) |
| `src/SkiaViewer/SkiaViewer.fs` | 4,063 | `module Viewer` spans ~3,237 lines |
| `tests/Rendering.Harness/Cli.fs` | 4,004 | One giant command dispatcher |
| `src/Controls/Control.fs` | 3,570 | `ControlInternals` god-module ~2,990 lines |
| `src/Controls.Elmish/ControlsElmish.fs` | 2,227 | Frame-loop god-function ~500 lines |
| `src/Controls/RetainedRender.fs` | 2,086 | `step` ~600 lines |
| `src/Scene/Scene.fs` | 2,077 | ~767-line type wall + 8 unrelated modules |
| `tests/SkiaViewer.Tests/Tests.fs` | 1,626 | One `testList` with 56 inline tests |
| `src/Scene/SceneCodec.fs` | 1,503 | Hand-written binary codec, ~99 write/read fns |
| `tests/Rendering.Harness/SkillParity.fs` | 1,493 | Harness production module |
| `src/SkiaViewer/Host/OpenGl.fs` | 1,443 | |
| `src/Input/KeyboardInput.fs` | 1,400 | Near-orphaned parallel keyboard stack |
| `tests/Rendering.Harness/ValidationLanes.fs` | 1,376 | Harness production module |
| `src/Layout/Layout.fs` | 1,241 | Flex algorithm |

### LOC per `src` project

```
12,473  src/Controls/
 7,767  src/SkiaViewer/
 4,550  src/Testing/
 3,855  src/Scene/
 2,227  src/Controls.Elmish/
 1,927  src/Layout/
 1,400  src/Input/
   631  src/Diagnostics/
   468  src/Color/
   413  src/DesignSystem/
   323  src/KeyboardInput/
   109  src/Themes.AntDesign/
    95  src/Themes.Default/
    77  src/Elmish/
```

### Largest individual functions

| Function | Location | ~Lines |
|---|---|---|
| `RetainedRender.step` | `src/Controls/RetainedRender.fs:1423` | 600 |
| `runInteractiveAppWithLauncher` | `src/Controls.Elmish/ControlsElmish.fs:1186` | 500 |
| `runFeature159ReadinessCmd` | `tests/Rendering.Harness/Cli.fs:3145` | 399 |
| `hashScene` | `src/Controls/Control.fs:2453` | 377 |
| `runFeature158PerformanceCmd` | `tests/Rendering.Harness/Cli.fs:1691` | 356 |
| `runPresentedPersistentWindow` | `src/SkiaViewer/SkiaViewer.fs:2114` | 323 (11 params) |
| `runPersistentWindow` | `src/SkiaViewer/SkiaViewer.fs:2437` | 235 (near-dup of above) |
| `renderKeyboardStateDisplayAt` | `src/Input/KeyboardInput.fs:1150` | 166 |
| `layoutNode` (rec) | `src/Layout/Layout.fs:195` | 148 |
| `keyboardStateDisplay` | `src/Input/KeyboardInput.fs:1001` | 148 |
| `evaluateIncremental` | `src/Layout/Layout.fs:586` | 134 |
| `update` (InputMsg) | `src/Input/KeyboardInput.fs:755` | 123 |

---

## 4. Detailed findings

### 4.1 Harness mis-filed under `tests/` (High / Low effort)

`tests/Rendering.Harness/` is **not a test project**. It is a production CLI executable:

- `<OutputType>Exe</OutputType>`, `[<EntryPoint>] let main` at `Cli.fs:3942`, 24 subcommands.
- Links `SkiaSharp` and `Silk.NET`; references 6 `src` projects.
- No Expecto / test-SDK reference.
- It is *itself* tested by a **separate, real** test project: `tests/Rendering.Harness.Tests/`.

Roughly 16k LOC of shippable tooling lives under `tests/` (vs ~45k of actual tests), which distorts any read of "test health." **Recommendation:** relocate `Rendering.Harness` (and its sibling production modules `SkillParity.fs`, `ValidationLanes.fs`, `PackageFeed.fs`, `Evidence.fs`) to `tools/` or `src/`, keeping `Rendering.Harness.Tests` under `tests/`.

### 4.2 Per-feature copy-forward duplication (High / High effort)

The convention of "one new function / constant / file per Feature-NNN" has produced large-scale structural repetition:

- **`Compositor.fs`** — 97 `renderFeatureNNN…` functions, 59 type defs, 262 `feature1NN…` constants including 12 separate `…ReadinessDirectory` path constants (e.g. `Compositor.fs:30`, `:42`). Estimated ~80% structural repetition. Each compositor feature gets its own `renderFeatureNNNValidationSummary` + a `…ReadinessDirectory`/`…ParityDirectory`/`…TimingDirectory` constant quintet.
- **`Cli.fs`** — per-feature command handlers up to ~400 lines (`runFeature159ReadinessCmd:3145`, `runFeature158PerformanceCmd:1691`, `runFeature160PerformanceCmd:2047`, …).
- **`tests/Package.Tests/`** — 17 `Feature*CompatibilityLedgerTests.fs` files, each ~50 lines re-defining the repo-root finder and asserting the same markdown header set (`## Public Surface Changes`, `## Behavior Changes`, `## Migration Guidance`, `## Limitations`). `Feature150CompatibilityLedgerTests.fs` is the de-facto template.
- **387 `Feature*.fs` files** under `tests/` overall. The 147→161 compositor series is structurally identical (`diff Feature148ReadinessPackageTests.fs Feature149ReadinessPackageTests.fs` differs only in the feature number and a few strings).

**Fix shape:** replace per-feature *code* with per-feature *data* — a `FeatureDescriptor` record (`{ id; slug; directories; requiredHeaders; … }`), one generic renderer, and a command/data table. Convert the copy-forward test families into data-driven `testList`s parameterized over `(featureId, slug, requiredHeaders)`.

### 4.3 `findRepositoryRoot` copied ~54× (Medium / Low effort)

The same repo-root walker (`walk (DirectoryInfo(AppContext.BaseDirectory))`) is re-inlined in ~54 files: `Package.Tests/Tests.fs:8`, `Lib.Tests/Tests.fs:173`, `Controls.Tests/Feature139AssemblyExtractionTests.fs:140`, `Controls.Tests/Feature141RetainedRendererUnificationTests.fs:84`, plus ~50 more. Minor variants (some check `*.sln`, some `build.fsx`) make drift worse than a clean copy. The pattern for a shared module already exists (`Rendering.Harness/Domain.fs`, `TestAssertions.fs`). **One shared `RepoRoot`/`TestSupport` module deletes ~54 copies.** This is the single easiest high-volume win.

### 4.4 No shared `ReadinessStatus` type — ~66 parallel DU cases (High / Medium effort)

A common vocabulary (`accepted` / `rejected` / `blocked` / `missing` / `unsupported` / `environment-limited`, plus `degraded` / `pending` / `unknown`) is re-encoded as **~66 distinct DU cases across 8+ files**, each with its own `toString`/render and often a reverse parse. There is no shared `ReadinessStatus` type. Parallel DUs all rendering `"accepted"` include: `VisualInspectionStatus`, `RetainedInspectionStatus`, `ProofSetReadiness`, `PackageAccepted`, `VisualReadinessAccepted`, `LayoutReadinessAccepted`, `CompositorReadinessAccepted`, `Feature159/160/161Accepted`, `ViewerResponsivenessReadiness.Accepted`, `Diagnostics.Accepted`.

`Testing.fs` is the worst offender with ~11 status-conversion functions (`844, 975, 1104, 3554, 3850, 3929, 4101, 4195, 4301, 4419`). The three `Feature159/160/161Readiness` modules (`Testing.fs:4194, 4300, 4418`) are near-identical ~100-line validators differing only by `Feature15x`/`Feature16x` prefix: each defines its own status DU, an identical `statusText`, a `statusBlocksAcceptance`, and a structurally identical `validate` (missingScenarios → requiredResults → failures → fallbackOnly/environmentLimited classification).

**Fix shape:** one shared `ReadinessStatus` DU + `statusText` + `blocksAcceptance`, and one parameterized `validate` over a config record. Collapses the three feature-readiness modules and ~10 status mappers, and removes the matching magic-string sprawl.

### 4.5 Competing / orphaned abstractions (Medium / Medium effort)

- **Two keyboard-input stacks.** `src/Input/` (`FS.GG.UI.Input`, 1,400 lines: `CommandRegistry`, `ModeDefinition`, `KeyChord`, `BindingDefinition`, `LayoutProfile`, bigram engine) vs `src/KeyboardInput/` (`FS.GG.UI.KeyboardInput`, 323 lines: `ViewerKey`, `KeyboardModel/Msg`, simple Elmish). Both define `type CommandId = string`; both primary files are literally named `KeyboardInput.fs`. The small one is consumed by SkiaViewer, Controls, Controls.Elmish, all samples, the template, and dozens of tests. **The large one is referenced by no `src` project — only `Input.Tests` and `Lib.Tests`.** Decide: retire the orphan (−1,400 lines, removes the naming collision in one move), or factor the shared vocabulary into a common base both build on.
- **Orphaned `Color` library.** `src/Scene/Scene.fs` owns the runtime `Color` type; `src/Color/` owns `ColorPolicy`/`Contrast`/`Palettes` **but is referenced by no `src` project** (only `Color.Tests`, `Controls.Tests`); `src/DesignSystem/DesignTokens.fs` owns color tokens. The production render path does not go through the `Color` library. Confirm intended (leaf NuGet package for generated products) vs accidental drift.
- **Two JSON conventions.** SkiaViewer and Diagnostics use `System.Text.Json`; `Testing.fs` hand-builds JSON via interpolated strings + manual escaping (51 sites). The hand-rolled path is also the source of the triplicated helpers below.

> Note: four `src` projects are consumed only by tests, never by other `src` projects: `Color`, `Input`, `Elmish`, `Themes.AntDesign`. Some is intentional (leaf packages for downstream generated products), but it means these abstractions ship unvalidated against an in-tree consumer.

### 4.6 Scattered small helpers (Low / Low effort)

- **FNV-1a hashing reimplemented 4×** with inline magic constants: `Control.fs:2454-2455` (`hashScene`), `Control.fs:2830-2831` (`fnvOffset`/`fnvPrime`), `Composition.fs:157-158`, `RetainedRender.fs:851` (`feature159Hash`). **⚠ `feature159Hash` uses `1469598103934665603UL`, which is NOT the standard FNV offset basis `0xcbf29ce484222325UL` used everywhere else — verify this is intentional and not a latent bug.**
- **`clamp` redefined 4×**: `OpenGl.fs:461` and `RetainedRender.fs:714` are byte-identical (`min hi (max lo value)`); `TextInput.fs:45` and `Layout.fs:26` are variants.
- **JSON/markdown helpers triplicated inside `Testing.fs`**: `esc` (`1235, 1883, 2583`), `q` (`1238, 1886, 2586`), `jsonStringArray` (`1240, 1888`), `countsText` (`1896, 2596`), `line` shim (`1258, 1904, 2604`) — character-for-character identical across `VisualReadinessMarkdown`, `VisualInspectionMarkdown`, `RetainedInspectionMarkdown`. `Diagnostics.fs:372` has its own `jsonStringArray`.
- **Trace modules duplicated across projects**: `RetainedRenderTrace` (`RetainedRender.fs:6-37`) and `RenderLagTrace` (`ControlsElmish.fs:11-25`) — identical `enabled`/`emit`; the Elmish copy is missing the `time` helper.
- **19 inline `ViewerDiagnosticEvent` record literals** in `SkiaViewer.fs` (~6 lines each) with no smart constructor.
- **Duplicated `cleanToken`/`duplicateIds`/`finding`** between `Scene.fs` `VisualInspection` (`1704, 1829, 1817`) and `RetainedInspection` (`1874, 2036, 1926`) — a dedup that was started (`unsupportedFact:1911` already delegates) but not finished.
- **Version drift**: `Directory.Build.props` sets `0.1.0-preview.1`, but `.fsproj` files override inline and have drifted into 3 clusters — `0.1.36-preview.1`, `0.1.44-preview.1` (DesignSystem), `0.1.45-preview.1` (SkiaViewer/Controls/Controls.Elmish). The `"rev=150"` layout-cache version is hand-duplicated at `Layout.fs:839` and `:964`.

### 4.7 God-functions & boolean traps (Medium / Medium–High effort)

- **`RetainedRender.step`** (`:1423`, ~600 lines) — ~30 `let mutable` accumulators + 8 nested recursive walks (`buildFresh`, `carry`, `build`, `countVirtual`, `walkPictures`, `collectOffscreen`, `indexPriorOwn`, `collect`, `assemble`). `init` (`:1253`) duplicates much of its build/paint scaffolding. Extract a `StepMetrics` record and pull each pass into a named function.
- **`runInteractiveAppWithLauncher`** (`ControlsElmish.fs:1186`, ~500 lines) — ~20 `ref` cells as ad-hoc frame state with ~15 nested closures (effectively an untyped mutable object). Promote to a `FrameLoopState` record + module functions.
- **`runPresentedPersistentWindow`/`runPersistentWindow`** (`SkiaViewer.fs:2114`/`:2437`) — duplicated window-lifecycle scaffolding (handler add/remove lists, `WindowOptions.Default`, init/failure block). Unify behind one lifecycle scaffold; the older path looks legacy.
- **Boolean-trap / long positional param lists**: `validateDamage` (5 consecutive bools, `OpenGl.fs:523`), `classifyWindowObservation` (4 bools, `SkiaViewer.fs:1632`), `promotionDecision` (6 params incl. bool, `RetainedRender.fs:768`), `popoverGeom … withActions` (`Control.fs:1755`, called `false`/`true` at 2009–2011), `damageRegion` (10 positional args, `Scene.fs:2000`). Replace tails with small named flag records.

### 4.8 Stringly-typed control `Kind` (Medium / High effort)

~101 distinct kind string literals in `Control.fs`, dispatched by ~12 parallel `match …Kind` sites across `Control.fs` (`112, 259, 354, 502, 1930, 2157, 2174`), `Inspection.fs` (`48, 68, 89`), `Accessibility.fs:29`, `Catalog.fs:501`. Adding a control kind means editing ~10 disjoint switches with no compiler exhaustiveness help. The same kind strings (`"data-grid"`, `"line-chart"`, `"bar-chart"`, …) are re-typed as literals in ~11 files. **Fix:** a single kind registry table (`kind -> { painter; requiredAttrs; prettyName; a11yRole; layoutTraits }`) collapses the parallel switches and restores exhaustiveness — but touches `Control.fs`, `Inspection.fs`, `Accessibility.fs`, `Catalog.fs`, so it is a genuine structural project.

### 4.9 Codec write/read hand-symmetry (Medium / Medium effort)

`SceneCodec.fs` is a hand-written binary serializer with ~99 `writeX`/`readX` functions. `writeSceneNode`/`readSceneNode` (`:761`/`:877`, ~224 lines combined) mirror-match the 24-case `SceneNode` DU. Every new case requires **3 coordinated hand-edits** (DU + writer + reader) with no compiler enforcement — the highest-risk drift point in the codebase. `SceneNode` itself (`Scene.fs:391`) mixes styles (named-field cases like `Circle of center: Point...` alongside bare-tuple cases like `Rectangle of (float*float*float*float)*Color`), inviting confusion at every match site. `writeStringOption`/`writeIntOption`/`writeInt64Option` (`:249-266`) are three near-clones over the generic `writeOption`. **Fix:** a per-case codec table or tag-table so the symmetry is enforceable.

### 4.10 Test smells (Low / Low effort)

- **Two tautological assertions** test nothing: `Expect.isTrue true` at `Controls.Tests/Feature093ParityTests.fs:77` and `Controls.Tests/TypedMigrationTests.fs:555`.
- **Brittle doc-coupled paths**: tests assert on `specs/**/readiness/**` markdown contents, e.g. `Feature141RetainedRendererUnificationTests.fs:271` reads `specs/141-…/tasks.md`; `TypedControlContractTests.fs:80` reads `specs/028-…/readiness/fsi-session.txt`. Couples unit tests to doc layout.
- **Oversized single tests**: `SkiaViewer.Tests/Tests.fs:757` "MVU lifecycle transitions…" (~82 lines) in a 56-block monolithic `testList`.
- **Module-level mutable side-channels**: `Scene.fs:1299-1300` (`realTextMeasurer`/`measurementVersionBucket`) in an otherwise pure scene module; `SceneRenderer.fs:22,179` global caches; `RenderLagTrace` module-level `mutable capturing`.

---

## 5. Implementation plan

The plan is sequenced so the **lowest-risk, highest-volume duplication removal lands first**, the orphan/placement decisions are made early (they clarify everything downstream), and the structural module splits come last. Each phase is independently shippable and independently verifiable (`dotnet build` + `dotnet test`).

### Phase 0 — Verify & quick safety fixes (½ day, very low risk)
Goal: bank the safest wins and resolve one possible latent bug before larger work.

1. **Confirm the `feature159Hash` constant** (`RetainedRender.fs:851`, `1469598103934665603UL`). If it should be the standard FNV basis `0xcbf29ce484222325UL`, fix it (note: changing a hash basis may invalidate persisted/golden hashes — check first). If intentional, add a comment explaining why.
2. **Replace the two `Expect.isTrue true` placeholders** with real assertions or delete them.
3. **Centralize the `"rev=150"` layout-cache version** into one constant referenced by `Layout.fs:839` and `:964`.

*Exit:* build + full test suite green; no behavior change (except the hash fix, if applied, gated on golden review).

### Phase 1 — Shared test/util helpers (1–2 days, very low risk)
Goal: delete the highest-volume mechanical duplication.

1. **`RepoRoot`/`TestSupport` module** — extract one repo-root finder; delete the ~54 inline copies. Reconcile the `*.sln` vs `build.fsx` variants into one correct implementation.
2. **`Fnv` helper module** — single offset/prime + `mix`/`mixString`; route `hashScene`, the `Control.fs` fingerprints, `Composition` hash, and `feature159Hash` through it.
3. **`MathHelpers.clamp`** (or fold into an existing small shared module) — replace the 4 local definitions.

*Exit:* build + tests green; net line reduction in the thousands.

### Phase 2 — Placement & orphan decisions (1–2 days, low risk; needs owner sign-off)
Goal: resolve ambiguous ownership before building on it.

1. **Relocate `Rendering.Harness`** out of `tests/` to `tools/` (recommended) or `src/`. Move `Compositor.fs`, `Cli.fs`, `SkillParity.fs`, `ValidationLanes.fs`, `PackageFeed.fs`, `Evidence.fs`, `Domain.fs`, `Live.fs` with it; keep `Rendering.Harness.Tests` under `tests/`. Update the `.slnx` and any path references.
2. **Decide the keyboard-input duplication.** Recommended default: **retire `src/Input/`** (no production consumer; −1,400 lines; removes the `KeyboardInput.fs` naming collision) after confirming nothing downstream/generated depends on `FS.GG.UI.Input`. If it is a planned future direction, instead factor shared vocabulary (`CommandId`, viewer-key normalization, `KeyboardStateDisplay`) into a common base.
3. **Decide the `Color` library.** Confirm `src/Color/` is an intentional leaf package for generated products (keep, document) vs accidental drift (wire the production render path through it, or fold into `DesignSystem`).

*Exit:* `tests/` contains only tests; one keyboard-input story; documented `Color` ownership. Build + tests green.

> **Decision gate:** Phase 2 items change project layout / public surface. Surface these to the maintainer before executing (especially the `src/Input/` retirement and any package-version implications).

### Phase 3 — Shared `ReadinessStatus` (3–5 days, medium risk)
Goal: collapse the ~66-case status vocabulary and the per-feature readiness clones.

1. Introduce a shared `ReadinessStatus` DU (`Accepted | Rejected | Blocked | Missing | Unsupported | EnvironmentLimited | Degraded | Pending | Unknown`) with `statusText`, `blocksAcceptance`, and a parse, in a low-level project (e.g. `Diagnostics` or a new `Readiness` module reachable by `Testing`/`SkiaViewer`/`Scene`).
2. Migrate the per-domain status DUs to wrap or alias it; remove the ~10 duplicate `statusText` mappers.
3. Generalize `Feature159/160/161Readiness` into **one parameterized validator** over a config record; delete the three near-identical modules.
4. Extract a shared `Markdown`/`Json` helper module (`esc`, `q`, `jsonStringArray`, `countsText`, `line`); delete the 3 copies in `Testing.fs` and the `Diagnostics.fs` copy. Standardize on `System.Text.Json` where practical.

*Exit:* one status type; one readiness validator; one JSON/markdown helper. Build + tests green (golden/report outputs reviewed for byte-stability).

### Phase 4 — Per-feature data-table refactor (5–8 days, medium risk)
Goal: convert copy-forward *code* into *data*.

1. Define a `FeatureDescriptor` record (`id`, `slug`, directory set, required headers, timing/parity config).
2. **`Compositor.fs`** — replace the 97 `renderFeatureNNN…` functions + 262 constants with a descriptor list + generic renderer.
3. **`Cli.fs`** — replace per-feature command handlers with a command table keyed by descriptor; extract the shared performance/readiness runner bodies.
4. **`tests/Package.Tests/`** — collapse the 17 `Feature*CompatibilityLedgerTests.fs` into one data-driven `testList`; do the same for the 147→161 compositor test families.

*Exit:* per-feature additions become a single data entry. Largest expected line reduction in the repo. Build + tests green.

### Phase 5 — God-module splits (ongoing, medium risk, do incrementally)
Goal: bring the largest modules under control along existing seams. Each split is its own PR.

1. **`SkiaViewer.fs`** — move the ~77-type header to `Viewer.Types`; split `Viewer` by concern (responsiveness summarization, window-behavior/validation, native run-loops, evidence/screenshot, app/interactive runners). Unify `runPresentedPersistentWindow`/`runPersistentWindow`.
2. **`Control.fs`** — split `ControlInternals` into `ChartGeometry` (the `*Geom` family), `WidgetGeometry`, `SceneHash`/`Fingerprint`, `LayoutEval`, `NodeAssembly`. Hoist the `match pts with | [] -> emptyState …` chart preamble (×17) into a `withPoints` combinator and a shared bar-layout helper.
3. **`Scene.fs`** — move `VisualInspection`, `RetainedInspection`, `LayoutEvidence`, `SceneEvidence` into their own files; separate the ~767-line type block; finish the started `cleanToken`/`duplicateIds`/`finding` dedup; isolate the `realTextMeasurer` mutable.
4. **`Testing.fs`** — split into per-domain files (Visual, RetainedInspection, Evidence, Compositor, Feature-readiness).
5. **`RetainedRender.step`** — extract `StepMetrics` + named passes; unify with `init`.
6. **`ControlsElmish.runInteractiveAppWithLauncher`** — `FrameLoopState` record + module functions.

*Exit:* no single module > ~1,500 lines; no single function > ~150 lines (targets, not hard rules).

### Phase 6 — Type-safety hardening (structural, medium risk, optional/last)
Goal: remove the remaining stringly-typed and hand-symmetry hazards.

1. **Control `Kind` registry** — one table replacing the ~12 parallel `match …Kind` switches across `Control.fs`/`Inspection.fs`/`Accessibility.fs`/`Catalog.fs`.
2. **Codec symmetry** — per-case codec table so `SceneNode` additions are compiler-enforced; normalize `SceneNode` case styling (named fields throughout).
3. **Boolean-trap cleanup** — replace bool-flag tails (`validateDamage`, `classifyWindowObservation`, `promotionDecision`, `popoverGeom`) with named flag records.

*Exit:* adding a control kind or scene node is a single-site, compiler-checked change.

---

## 6. Sequencing rationale

- **Phases 0–1** are nearly free and remove the most lines for the least risk — do them regardless of appetite for the rest.
- **Phase 2** front-loads the decisions (harness placement, orphan stacks) that otherwise make every later phase ambiguous; it needs maintainer sign-off because it moves projects/public surface.
- **Phases 3–4** are the bulk-duplication payoff and depend on the Phase 2 placement being settled.
- **Phases 5–6** are the genuinely structural work; they are valuable but higher-touch, best done incrementally as individual PRs once the duplication noise is gone.

Each phase ends green on `dotnet build` + `dotnet test`; report/golden outputs should be diffed for byte-stability where the change touches rendering or evidence generation.

---

*Generated from a five-pass parallel review on 2026-06-21. File/line references reflect the tree at that time and should be re-confirmed before editing.*
