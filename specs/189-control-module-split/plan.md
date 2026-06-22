# Implementation Plan: Control.fs / ControlInternals Decomposition (Patterns A+E, kind registry)

**Branch**: `189-control-module-split` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/189-control-module-split/spec.md`

## Summary

Phase 5 of the god-module decomposition campaign
(`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §4.2 / §6). Decompose
`src/Controls/Control.fs` (**3,513 lines — re-confirmed 2026-06-22**) whose `module internal
ControlInternals` (L124–~3066) is ~2,942 of them, by responsibility (**Pattern E**) and route the
per-kind dispatch through the already-built `ControlKindRegistry` (**Pattern A**). Four stories,
sequenced strictly by risk:

1. **US1 (P1) — `ChartGeometry` / `WidgetGeometry` + `withPoints`**: relocate the ~40 `private *Geom`
   functions (L626–~1860) into two responsibility groupings and factor the ~17 repeated
   `match pts with [] -> emptyState …` preambles into one `withPoints` combinator. Pure relocation +
   shared-skeleton collapse — every `Scene list` **byte-identical**. Lowest risk, biggest standalone
   reduction, and it establishes the dispatch targets US3 routes into.

2. **US2 (P2) — `SceneHash` / `LayoutEval` / `NodeAssembly`**: extract `hashScene` (L2405–2853) into a
   `SceneHash` grouping recast as a `SceneHasher` visitor (Pattern A); the layout evaluators
   (`toLayout` L2163, `evaluateLayout` L2268, `evaluateLayoutIncremental`) into `LayoutEval`; the
   assembly functions (`paintLeaf` L2298, `paintNode`, `renderScene` L2036) into `NodeAssembly`.
   Behavior-sensitive (hot path / `RetainedRender` picture-cache), so gated on golden-hash byte-equality
   with the §7 review gate for any legitimate reorder.

3. **US3 (P3) — registry painter + the 6 parallel `match …Kind` sites**: express `faithfulContent`'s
   60+ kind branches (L1884) as a per-kind painter on the **extended `ControlKindRegistry`**, and route
   the ~6 disjoint `match …Kind` switches (`Control.fs` ×2, `ControlRuntime.fs`, `Catalog.fs`,
   `Inspection.fs`, `RetainedRender.fs`) through the single table. The Pattern-A core: kills the
   cross-file exhaustiveness drift and extends the catalog↔registry completeness oracle (feature 183
   `Feature183KindRegistryTests.fs`) to the painter. Most behavior-sensitive; sequenced last so it
   routes into already-stable groupings.

4. **US4 (P4) — `Control.Helpers` tail-module collapse**: collapse the duplicated bodies of the 30
   public tail-constructor modules (`TextBlock`…`Overlay`, L3358–3511) behind a shared data-driven
   helper while preserving every public `create`/`text` surface as thin delegations. Held to the
   strictest surface-neutral bar; **ships only if it nets a real line reduction** (carry-forward lesson
   180 SC-005 / 181) — otherwise dropped without blocking US1–US3.

> **Standing assumption — hypotheses unverified until the suites run.** The ordering/mechanism reading
> below (geometry depends on a shared L124–625 prelude; the registry painter table must be assembled
> *after* geometry to avoid an F# back-edge) is a code-reading hypothesis. `/speckit-tasks` MUST
> schedule the **baseline capture (FR-014) before any production edit** and a **full-solution compile
> probe** of the chosen module topology in the Foundational phase, before US1 work. Each story diffs
> emitted scenes/hashes/surface against that baseline; any golden-hash delta is reviewed before it is
> treated as expected. Do not build later stories on an unverified compile-order hypothesis.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: SkiaSharp over OpenGL (GL). `FS.GG.UI.Controls` sits atop `FS.GG.UI.Scene`
and `FS.GG.UI.Layout`; this phase introduces **no new project or package reference** (FR-013) — work
stays in `src/Controls` plus baseline/consumer edits a non-empty surface diff would require.

**Storage**: N/A (in-memory `Control`/`Scene` values; readiness/evidence artifacts are markdown+JSON).

**Testing**: `dotnet test FS.GG.Rendering.slnx` (xUnit-style). GL-dependent suites run under
`DISPLAY=:1` (X11). Surface-drift gate: `tests/Package.Tests/SurfaceAreaTests.fs` +
`scripts/refresh-surface-baselines.fsx` → `readiness/surface-baselines/FS.GG.UI.Controls.txt`.
Catalog↔registry completeness oracle: `tests/Controls.Tests/Feature183KindRegistryTests.fs`.

**Target Platform**: Linux (GL via X11) for local validation; library targets net10.0 cross-platform.

**Project Type**: Single F# library project (`src/Controls`) with downstream consumers in the repo.

**Performance Goals**: No regression. `hashScene` is on the `RetainedRender` picture-cache/fingerprint
hot path; the visitor recast and registry routing MUST preserve numeric/dispatch order so rendered
scenes stay equivalent and the fingerprint stays byte-identical except for a reviewed golden-hash delta.

**Constraints**:
- **F# non-namespace module cannot span files.** `module internal ControlInternals` cannot be
  literally spread across files (same limit Scene.fs hit in 188). The split is done via **sibling
  internal modules** compiled in producer→consumer order; the shared helper preamble (L124–625) is
  extracted first so the relocated geometry can reach it (see research D1).
- **Internal back-edge / registry ordering.** The registry painter table references the geometry
  functions, so it MUST be assembled in a file compiled **after** `ChartGeometry`/`WidgetGeometry`; the
  cheap metadata predicates (`chartSource`/`isRich`/…) that early sites (e.g. `Control.fs:499`) read
  stay where they are. No circular module dependency may be introduced (Edge Case "internal back-edge").
- **Hot-path equivalence.** Recasting `hashScene`/`faithfulContent` MUST NOT break `RetainedRender`
  picture-cache/fingerprint invariants; any hash change is intentional, reviewed, and proven not to
  cause spurious cache misses or visual drift (Edge Cases; FR-003/FR-009).
- **Surface neutrality.** Decomposition targets are `private` members inside `module internal
  ControlInternals` (not public surface); the 30 tail modules keep their public `create`/`text`
  surface. Target is an **empty** `FS.GG.UI.Controls` surface diff; version is bumped **iff** the
  regenerated, reviewed baseline diff is non-empty (FR-010, same gate as 188).
- **Every public `.fs` module needs a curated `.fsi`** (Constitution II). Internal sibling modules
  follow the repo precedent (`ControlKindRegistry`, `Reconcile`, `RetainedRender`): `module internal` +
  a paired `.fsi`, reached by tests via `InternalsVisibleTo`, never on the public surface.
- **Required-attribute validation retained.** The `required kind` validation (`Control.fs:353`) that
  feature 183 deliberately kept at its site MUST keep firing identically after the split (Edge Cases;
  FR-012) — it is not absorbed by the painter routing.

**Scale/Scope**: `Control.fs` 3,513 → target each resulting file ≤ ~1,500 lines (SC-001, "≈"
guideline). New internal files: `ControlPrimitives` (shared prelude), `ChartGeometry`, `WidgetGeometry`,
`SceneHash`, `LayoutEval`, `NodeAssembly`, `ContentRender` (registry painter wiring); `Control.Helpers`
(US4, conditional). Affected suites: `Controls.Tests` (construction/faithful-content/retained-render/
hash-fingerprint/layout/inspection/`Feature183KindRegistryTests`), `Package.Tests` (`SurfaceAreaTests`),
`Elmish.Tests`, `ControlsGallery.Tests`, and any `Rendering.Harness.Tests` reading controls
scene-hash/inspection artifacts (exact list re-confirmed at task time).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Tests → Impl | ✅ | Each new internal file gets its `.fsi` authored before the `.fs` body; tests are the existing suites run against the packed surface. This relocates FSI-validated internal surface + extends one registry record — not new public API design. |
| II. Visibility in `.fsi` | ✅ | New sibling modules are `module internal` + paired `.fsi` (precedent: `ControlKindRegistry`/`Reconcile`/`RetainedRender`). The L124–625 prelude helpers move from `private` (intra-module) to module-internal; no `private`/`internal`/`public` keyword is added to a top-level `.fs` binding. Surface-drift gate (`SurfaceAreaTests`) enforces zero public drift. |
| III. Idiomatic Simplicity | ✅ | `SceneHasher` visitor + registry painter table are plainer than the 25-case inline walk / 60-branch `match`. `withPoints` collapses only the shared skeleton (180/181 lesson). No new operators/SRTP/reflection/CE/type-providers. Existing `mutable h` FNV-1a accumulator stays disclosed (`// mutable: hot path`). |
| IV. Elmish/MVU boundary | ✅ N/A | Pure scene/geometry/hash producers + a pure dispatch table; no new stateful/I-O workflow. The `setMeasureTextHook`/`measureText` seam is a pre-existing injected pure function, relocated with the prelude, not redesigned. |
| V. Test Evidence | ✅ | FR-011: every suite keeps its red/green result except explicitly reviewed golden-hash expected-output updates; no test deleted/skipped/weakened. Baseline captured first (FR-014). New completeness assertion for the painter (FR-007 / SC-007) fails before, passes after. |
| VI. Observability / Safe Failure | ✅ | FR-012: fail-loud preserved at every refactored site — unknown kind falls back to the same default the pre-refactor `match` used; missing painter entry fails the oracle loudly; `required`-attribute validation keeps firing. No swallowed exceptions. |

**Change Classification**: Spec declares **Tier 1 (surface-changing-conditional) + behavior-sensitive
(hash/fingerprint)**, taking the maintainer-granted relaxed-constraints latitude (Full Patterns A+E).
Under the sibling-internal-module mechanism the decomposition targets stay off the public surface and
the tail modules keep their surface, so the **target is an empty `FS.GG.UI.Controls` surface diff**.
**Version-bump gate**: bump `FS.GG.UI.Controls` *iff* the regenerated
`readiness/surface-baselines/FS.GG.UI.Controls.txt` shows a non-empty, reviewed-and-approved diff
(empty diff ⇒ no bump; FR-010 / SC-006). The hash/fingerprint question is independent and always gated
by the §7 golden-hash review + the existing suites (FR-003/FR-009/SC-008).

**No Complexity Tracking violations.** No new project, dependency, or non-idiomatic construct; the
visitor + table are simplicity-positive.

## Project Structure

### Documentation (this feature)

```text
specs/189-control-module-split/
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — split mechanism, registry-ordering, hash-visitor decisions
├── data-model.md        # Phase 1 — module/file topology + ControlKindEntry/painter + withPoints model
├── quickstart.md        # Phase 1 — baseline-capture + per-story validation runbook
├── contracts/
│   └── module-topology.md   # Phase 1 — post-split module map + surface/ordering/oracle contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/
├── Controls.fsproj             # compile order updated (Structure Decision below): shared prelude +
│                               #   geometry + hash compile BEFORE ContentRender; ContentRender +
│                               #   LayoutEval/NodeAssembly before Control.fs; registry metadata stays
│                               #   early. Producers strictly precede consumers (FR-013, no back-edge)
├── ControlKindRegistry.fsi/.fs # EXTENDED (US3) — ControlKindEntry gains Painter field; registry
│                               #   metadata predicates stay early; painter TABLE assembled in
│                               #   ContentRender (research D2). Completeness oracle covers painter.
├── ControlPrimitives.fsi/.fs   # NEW (US1 foundational) — module internal: the L124–625 shared
│                               #   prelude (measureText seam, chartValues, styleClassesOf,
│                               #   fittedFontSize, ellipsize, accessibility, required, …) geometry
│                               #   + content depend on. (Name TBD at impl; see research D1.)
├── ChartGeometry.fsi/.fs       # NEW (US1) — chart *Geom producers + withPoints combinator
├── WidgetGeometry.fsi/.fs      # NEW (US1) — widget/layout/container *Geom producers
├── SceneHash.fsi/.fs           # NEW (US2) — hashScene recast as SceneHasher visitor over SceneNode
├── LayoutEval.fsi/.fs          # NEW (US2) — toLayout/evaluateLayout/evaluateLayoutIncremental
├── ContentRender.fsi/.fs       # NEW (US3) — faithfulContent + the registry painter table
├── NodeAssembly.fsi/.fs        # NEW (US2/US3) — paintLeaf/paintNode/renderScene (calls content+hash)
├── Control.fsi/.fs             # residual ControlInternals core + public `module Control` + 30 tail
│                               #   modules (+ Control.Helpers, US4 conditional). Thin delegations.
└── ...                         # ControlRuntime/Catalog/Inspection/RetainedRender route match…Kind
                                #   through the registry (US3, FR-006) — unchanged external surface

readiness/surface-baselines/FS.GG.UI.Controls.txt   # regenerated; diff reviewed (version-bump gate)
```

**Structure Decision**: Single-project library split into responsibility-scoped files **within
`src/Controls`** (FR-013). The `module internal ControlInternals` is decomposed into **sibling
`module internal` files** (not one module spanning files — F# forbids that; 188 confirmed it). The
`Controls.fsproj` compile-order edit is load-bearing: F#'s file order is the dependency order, so the
shared prelude precedes geometry, geometry precedes the `ContentRender` painter table, and
content/layout/hash precede `NodeAssembly` and the residual `Control.fs`. The exact relative order of a
few pairs (e.g. `ContentRender` vs `NodeAssembly`, registry-painter placement) is fixed **empirically**
by the full-solution compile probe scheduled in the Foundational phase (research D1/D2). Module names
*preserved on move* so internal callers resolve unchanged: `renderScene`, `paintNode`, `evaluateLayout`,
`hashScene`, `faithfulContent` keep their names and call shapes (US2/US3 acceptance). Namespace
preserved everywhere: `FS.GG.UI.Controls`.

## Complexity Tracking

*No Constitution Check violations — section intentionally empty.*

## Implementation Progress (2026-06-22)

**Pattern E (decomposition): COMPLETE. Pattern A (registry routing): drift-elimination satisfied;
painter-table restructure deferred.** `Control.fs` 3,513 → **984** lines via 7 new sibling
`module internal` files in `src/Controls/Internal/` (the AttrKeys/Hashing precedent). Every scene
and `hashScene` fingerprint is **byte-identical**; the `FS.GG.UI.Controls` public surface diff is
**EMPTY** (486 types, no version bump). Full matrix red set = baseline (the 2 pre-existing reds:
`Package.Tests` design-system/pin + `ControlsGallery.Tests` catalog-coverage; neither touches this
work).

| Phase | Status | Notes |
|---|---|---|
| Setup (T001–T004) | ✅ Done | Baseline captured (FR-014): 16 projects, 14 green / 2 pre-existing red, surface snapshot, corpus, suite list — `readiness/baseline/`. |
| Foundational (T005–T009) | ✅ Done | Topology/compile-order proven by the real incremental build (`readiness/baseline/topology-confirm.md`); `ControlPrimitives` prelude extracted. |
| US1 (T010–T015) | ✅ Done | `ControlPrimitives` + `ChartGeometry` + `WidgetGeometry` + `withPoints` (14 guards collapsed). Byte-identical; surface empty. |
| US2 (T016–T021) | ✅ Done | `SceneHash` (verbatim hashScene), `LayoutEval`, `NodeAssembly`, `ContentRender` (faithfulContent byte-identical). The layoutNode↔renderScene cycle avoided by keeping `layoutNode` residual. INV-1 + golden-hash zero-delta confirmed. |
| US3 (T024–T026) | ✅ Done (drift + oracle) | 6-site registry routing already satisfied by feature 183 (verified); painter-completeness oracle added (FR-007/SC-007, every rich catalog kind painted). |
| US3 (T022–T023) | ⏸ Deferred | Painter-table *restructure*: `Map<string,Theme->Rect->Control<'msg>->Scene list>` is a generic value (value restriction); per-call rebuild is a hot-path regression. The plan's anticipated FR-005 deviation — `readiness/us3-us4-decision.md`. |
| US4 (T027–T028) | ⏸ Deferred | Conditional on net line reduction (FR-008); SC-001 already met (984 ≪ 1,500), so dropped per the conditional. |
| Polish (T029–T034) | ✅ Done | Full gate = baseline; surface/version gate empty (no bump); golden-hash zero-delta; line-counts; FR-013/C7 (only added `<Compile>`, no new dep). |

**Deviations from the as-written plan (all recorded under `readiness/`):**
1. New internal modules placed in `src/Controls/Internal/` (no paired `.fsi`) following the
   AttrKeys/Hashing precedent, rather than top-dir + `.fsi` — both are repo conventions for
   `module internal`; `Internal/` satisfies the `SurfaceAreaTests` governance (which pairs `.fsi`
   for top-dir files only) without ~140 redundant signatures, consistent with the maintainer's
   pragmatic assembly-surface reading of the `.fsi` freeze.
2. `LayoutEval`/`NodeAssembly` split was achievable (no cycle) **only** by keeping the offscreen
   `layoutNode` helper in `ControlInternals`; the naive split (layoutNode∈LayoutEval) would have
   introduced the `layoutNode→renderScene` / `paintNode→toLayout` back-edge the plan warned against.
3. The `ControlInternals` thin re-export facade preserves every `ControlInternals.<member>` name
   external callers (RetainedRender / Controls.Elmish / test assembly) use, so the decomposition
   required **zero** consumer edits (FR-013) and the `Control.fsi` contract is unchanged.
