# Implementation Plan: Scene.fs Module Split (Pattern E, finish FR-006 inspection dedup)

**Branch**: `188-scene-module-split` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/188-scene-module-split/spec.md`

## Summary

Phase 4 of the god-module decomposition campaign. Decompose `src/Scene/Scene.fs` (2,084 lines —
re-confirmed 2026-06-22) by responsibility (Pattern E), and finish the started FR-006 inspection-
finding dedup (the one behavior change). Three slices, sequenced by risk:

1. **US1 (P1) — `Types.fs`**: lift the ~770-line namespace-level type wall (lines 7–779,
   `Size`…`RetainedInspectionSummary`) into a new `Types.fs` that **stays in `namespace
   FS.GG.UI.Scene`** (namespace-level declarations, *not* a nested module). Because the CLR
   `FullName`s are unchanged (`FS.GG.UI.Scene.Size` stays `FS.GG.UI.Scene.Size`), this is
   **surface-neutral**: no surface-baseline drift, no consumer-reference churn, no version bump for
   the re-home. (Maintainer-confirmed mechanism, 2026-06-22 — chosen over the spec body's literal
   sub-module nesting precisely to avoid the 17-consumer blast radius.)

2. **US2 (P2) — `TextShaping.fs`**: unify the shaping trio (`buildGlyphRun`,
   `buildFallbackShapedText`, `glyphRunDataFromShapedText`, currently in `module Scene` at
   L1127/1201/1249) behind **one** private parameterized core, and relocate the `realTextMeasurer`
   mutable seam (L1306–1323) into the new `Text.Shaping` module as its single owner. Public
   `module Scene` entry points are preserved as thin delegations so glyph runs / fingerprints /
   measurement stay **byte-identical**. Net surface impact is whatever the regenerated baseline
   diff shows (see version-bump gate below).

3. **US3 (P3) — `Inspection.fs` + `Evidence.fs`, finish FR-006 dedup**: move the four namespace-
   level modules (`SceneEvidence` L1532, `LayoutEvidence` L1607, `VisualInspection` L1710,
   `RetainedInspection` L1880) into two new files **keeping their module names**
   (`module FS.GG.UI.Scene.VisualInspection`, etc.) → surface-neutral move. Then **finish** the
   FR-006 dedup: the `duplicateIds`/`stableFindingId`/`cleanToken` machinery currently *detects and
   reports* duplicate finding IDs as diagnostics but does not *collapse* duplicate findings —
   complete it so findings sharing a `stableFindingId` are collapsed consistently across both the
   visual and retained inspection paths. This is the only behavior change; it is gated by semantic-
   artifact diff + reviewed sign-off, not byte-equality.

> **Standing assumption — hypotheses unverified until the suites run.** The "dedup detects but does
> not collapse" reading of FR-006 (§ below) is a code-reading hypothesis. `/speckit-tasks` MUST
> schedule the **baseline capture (FR-011) before any production edit**, and the dedup task MUST
> diff actual emitted findings against that baseline before the delta is treated as expected. Do not
> build the dedup on the unverified hypothesis alone.

## Implementation Status — COMPLETE (2026-06-22)

All three slices shipped on branch `188-scene-module-split`; `Scene.fs` **2,084 → 412 lines**.

| Slice | Outcome | Surface | Tests |
|-------|---------|---------|-------|
| Phase 1/2 | Baselines + contracts captured (`readiness/`); pre-refactor parity = 14 green / 2 red (pre-existing `Package.Tests` 8, `ControlsGallery.Tests` 2) | snapshot saved | — |
| US1 `Types.fs` | Type wall (`Size`…`RetainedInspectionSummary`, 775 lines) re-homed namespace-level. Companion `module Paint`/`module Scene` got explicit `[<CompilationRepresentation(ModuleSuffix)>]` (FS0250 fix; same compiled name → surface-neutral) | **empty diff** (230 types), no bump | Scene.Tests 75/75 |
| US2 `TextShaping.fs` | Shaping trio + helpers + `realTextMeasurer` seam relocated into **`module internal FS.GG.UI.Scene.Text.Shaping`**; shared `shapedGlyphOfGlyphRun` factored (byte-neutral); `module Scene` keeps thin public delegations | **empty diff** (internal module), **no bump** | Scene.Tests 75/75, SkiaViewer.Tests 207/207 |
| US3 `Inspection.fs`+`Evidence.fs` | 4 modules moved keeping names (surface-neutral). FR-006 dedup finished: both `normalizeArtifact` paths `… \|> List.distinctBy _.FindingId` (collapse by `FindingId`, keep first, each path keeps its identity scope) | **empty diff** | Scene.Tests 77/77 (+2 dup-collapse cases) |

- **Version**: unchanged `0.1.37-preview.1` — every slice achieved a zero surface diff (`readiness/version-gate.md`).
- **Final compile order** (`Scene.fsproj`): `Types → TextShaping → Scene → Inspection → Evidence → SceneWire/SceneCodec/Animation`.
- **FR-006 dedup**: only behavior change; semantic-diff reviewed & approved (`readiness/dedup-review.md`); fail-loud duplicate diagnostics preserved (FR-009).
- **FR-010**: no new project/dependency/inter-project ref; `Scene.fsproj` adds only `<Compile>` entries.
- See `readiness/` for baselines, compile-order/version/dedup records, and `feedback/` for phase notes.

## Technical Context

**Language/Version**: F# on .NET `net10.0`.

**Primary Dependencies**: SkiaSharp over OpenGL (GL). `FS.GG.UI.Scene` is the dependency-free root
of the rendering stack; it takes no FS.GG project references.

**Storage**: N/A (in-memory scene values; readiness/evidence artifacts are markdown+JSON on disk).

**Testing**: `dotnet test FS.GG.Rendering.slnx` (xUnit-style suites). GL-dependent suites run under
`DISPLAY=:1` (X11). Surface-drift gate: `SurfaceAreaTests` + `scripts/refresh-surface-baselines.fsx`.

**Target Platform**: Linux (GL via X11) for local validation; library targets net10.0 cross-platform.

**Project Type**: Single F# library project (`src/Scene`) with downstream consumers in the same repo.

**Performance Goals**: No regression. Render-adjacent float/accumulation order MUST be preserved so
rendered frames and glyph fingerprints stay equivalent for unchanged scenes (Edge Cases).

**Constraints**:
- F# root-position ordering: `Types.fs` must compile **before** `Scene.fs`; both before consumers.
  No circular module dependency (Edge Case "F# root-position back-edge").
- `module Scene` (a non-namespace module) **cannot span files** — anything leaving it changes its
  module path unless a thin delegating shim stays behind in `Scene.fs`.
- No new project, NuGet dependency, or inter-project reference (FR-010). Work stays in `src/Scene`
  plus the consumer/template/baseline edits the surface diff (if any) requires.
- Every public `.fs` module needs a curated `.fsi` (Constitution II). New files (`Types.fs`,
  `TextShaping.fs`, `Inspection.fs`, `Evidence.fs`) each need a matching `.fsi`.

**Scale/Scope**: `Scene.fs` 2,084 → target ≤ ~1,500 lines (SC-001, "≈" guideline not a hard
assert). 17 consumers (14 ProjectReference + 3 PackageReference: `samples/AntShowcase`,
`samples/SecondAntShowcase`, `template/base`). Affected suites: `Scene.Tests` (round-trip/package/
shaping/inspection — `Feature140/142/146/165` files), `Package.Tests`, `Layout.Tests`, `Lib.Tests`,
`Smoke.Tests`, consumer suites, and any `Rendering.Harness`/`Testing` evidence suites reading scene
inspection/evidence artifacts.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Tests → Impl | ✅ | `.fsi` for each new file is authored/updated before the `.fs` body; tests are the existing suites run against the packed surface. This is a re-home of existing, FSI-validated surface, not new API design. |
| II. Visibility in `.fsi` | ✅ | New `Types.fsi`/`TextShaping.fsi`/`Inspection.fsi`/`Evidence.fsi` carry the moved declarations; no `private`/`internal`/`public` keywords added to `.fs`. Surface-drift gate (`SurfaceAreaTests`) is the enforcement. |
| III. Idiomatic Simplicity | ✅ | Drives the US1 mechanism choice (namespace-level file split over sub-module nesting). Existing `mutable realTextMeasurer` is a disclosed side-channel being quarantined, not new cleverness. No new operators/SRTP/reflection/CE/type-providers introduced. |
| IV. Elmish/MVU boundary | ✅ N/A | Pure scene vocabulary + pure inspection/evidence builders; no new stateful/I-O workflow. The `realTextMeasurer` seam is a pre-existing injected pure function, relocated not redesigned. |
| V. Test Evidence | ✅ | FR-008: every suite keeps its red/green result except the explicitly reviewed FR-006 dedup expected-output updates. No test deleted/skipped/weakened to go green. Baseline captured first (FR-011). |
| VI. Observability / Safe Failure | ✅ | FR-009: fail-loud preserved at every refactored site; dedup collapses duplicates only and MUST NOT silence a unique real finding (US3 acceptance #3). |

**Change Classification**: Spec declares **Tier 1 (surface-changing) + behavior-affecting**, taking
the maintainer-granted relaxed-constraints latitude. Under the confirmed surface-neutral mechanism
(US1) and surface-neutral module moves (US3), the *only* candidate public-surface drift is US2's
shaping/measurer relocation, and the *only* behavior change is the FR-006 dedup. **Version-bump
gate**: bump `FS.GG.UI.Scene` *iff* the regenerated `readiness/surface-baselines/FS.GG.UI.Scene.txt`
shows a non-empty, reviewed-and-approved diff. If the diff is empty (surface-neutral achieved across
all three slices), no bump is required and FR-007/SC-006 are satisfied by "zero incidental drift."
The dedup behavior change is independent of the surface question and is always gated by the §7
semantic-artifact-diff + reviewed-sign-off discipline.

**No Complexity Tracking violations.** No new project, dependency, or non-idiomatic construct.

## Project Structure

### Documentation (this feature)

```text
specs/188-scene-module-split/
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — mechanism decisions + FR-006 dedup reading
├── data-model.md        # Phase 1 — module/file topology + finding-identity model
├── quickstart.md        # Phase 1 — baseline-capture + per-story validation runbook
├── contracts/
│   └── module-topology.md   # Phase 1 — post-split module map + surface/ordering contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Scene/
├── Scene.fsproj          # compile order updated: Types.fsi/fs FIRST; Types.* before Scene.*;
│                         #   Inspection.*/Evidence.* after Scene.*; then SceneWire/Codec/Animation
│                         #   (unchanged tail). TextShaping.* vs Scene.* relative order is fixed
│                         #   EMPIRICALLY by US2's delegation direction (contract C1 / research D5)
├── Types.fsi  / Types.fs        # NEW (US1) — namespace-level type wall, FS.GG.UI.Scene.*
├── Scene.fsi  / Scene.fs        # builders-only root + thin shaping delegations (US1+US2)
├── TextShaping.fsi / TextShaping.fs  # NEW (US2) — module Text.Shaping: unified shaped-text core
│                                     #   + realTextMeasurer single owner
├── Inspection.fsi / Inspection.fs    # NEW (US3) — module VisualInspection + RetainedInspection
├── Evidence.fsi   / Evidence.fs      # NEW (US3) — module SceneEvidence + LayoutEvidence
├── SceneWire.fs / SceneCodec.* / Animation.*   # unchanged (downstream of Scene in same project)
└── ...

readiness/surface-baselines/FS.GG.UI.Scene.txt   # regenerated; diff reviewed (version-bump gate)
```

**Structure Decision**: Single-project library split into responsibility-scoped files **within
`src/Scene`** (FR-010). The compile-order edit in `Scene.fsproj` is load-bearing: F#'s file order is
the dependency order, so `Types.*` precedes `Scene.*`, which precedes the shaping/inspection/evidence
files that reference both the types and (for delegations) the `Scene` module surface. New module
names introduced: `Text.Shaping` (US2). Module names *preserved* on move: `VisualInspection`,
`RetainedInspection`, `SceneEvidence`, `LayoutEvidence` (US3). Namespace preserved everywhere:
`FS.GG.UI.Scene`.

## Complexity Tracking

*No Constitution Check violations — section intentionally empty.*
