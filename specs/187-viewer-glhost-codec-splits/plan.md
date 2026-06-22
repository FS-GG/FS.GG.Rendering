# Implementation Plan: Viewer + GlHost + SceneCodec Module Splits (Pattern E + A)

**Branch**: `187-viewer-glhost-codec-splits` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/187-viewer-glhost-codec-splits/spec.md`

## Summary

Phase 3 of the god-module decomposition (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §6).
A **behavior-preserving** module-by-responsibility decomposition (Pattern E) of the three
mid-sized god-modules in the viewer/host/codec layer, plus a per-case codec-table conversion
(Pattern A) for the scene-node serializer. Three independently-shippable user stories:

- **US1** — `SkiaViewer.fs` (3,370 lines): carve the `Viewer` module's internal bodies into named
  responsibility groups (input queue, responsiveness, evidence, window lifecycle) and unify the two
  near-duplicate persistent-window run loops behind one lifecycle scaffold.
- **US2** — `Host/OpenGl.fs` (1,454 lines): decompose `GlHost.run`'s ~295-line body (rendering /
  input / damage / effects+screenshot) into named internal units, `run` becoming a thin orchestrator.
- **US3** — `SceneCodec.fs` (1,571 lines): split the internal wire codec by node family and convert
  the hand-aligned `writeSceneNode`/`readSceneNode` pair (25 node cases) into a per-case codec table
  so encode/decode symmetry is structural.

**Central design constraint (the spine of this plan).** F# binds **one `.fsi` to one `.fs`**; you
cannot relocate a public function out of `module Viewer`/`module GlHost`/`module SceneCodec` without
changing that module's public path and therefore its `.fsi` + surface baseline. The campaign's
relaxed constraints *permit* surface change, but this phase deliberately does **not** take it (spec
Assumptions). Therefore every split is implemented as **new internal helper files (no `.fsi`, fully
internal — exactly like the existing `SceneRenderer.fs`/`Numeric.fs`) plus thin public delegators
left in place**. The three `.fsi` files and both surface baselines stay byte-identical; this is the
feature-186 precedent (internal seam + public delegators) applied to a larger surface. The
~1,500-line target is met by moving *bodies*, not *contracts*.

> **Standing assumption — adapted for a pure structural refactor.** Not a defect fix, so there is no
> "live smoke run to confirm a root-cause hypothesis." The analogous obligation is the
> **baseline-first** discipline: capture a pre-refactor baseline (affected-suite red/green +
> reference frames/traces/screenshots + the serialized-byte corpus + public `.fsi`/surface snapshot)
> in the Foundational phase *before any production edit*, then diff every user story against it.
> `/speckit-tasks` MUST schedule that baseline capture as the first Foundational task. Because the
> viewer/GL paths are GL/timing-bound, the baseline also records which suites legitimately skip
> when no GL surface is present (CI deterministic tier), so a skip is never mistaken for a regression.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: SkiaSharp over OpenGL (GL); Silk.NET windowing for the GL host; Expecto
test framework (`Microsoft.NET.Test.Sdk` host). No new dependency is added (FR-010).

**Storage**: N/A (in-process render loop + filesystem evidence/readiness artifacts and serialized
scene-package bytes).

**Testing**: Expecto, run via `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` (GL via X11).
Affected projects: `tests/SkiaViewer.Tests` (host / present / input-queue / responsiveness / OpenGl
host / live-proof / damage), `tests/Scene.Tests` (Feature146 portable-scene round-trip / resource /
capability / compatibility + Feature183 codec symmetry), `tests/Smoke.Tests` (GL smoke),
`tests/Elmish.Tests` (Feature167/174 responsiveness regressions consuming viewer output), and
`tests/Rendering.Harness.Tests` (evidence artifacts that read viewer screenshots / scene packages).
Surface drift: `tests/Package.Tests/SurfaceAreaTests.fs`.

**Target Platform**: Linux desktop (SkiaSharp/GL viewer host); CI deterministic tier.

**Project Type**: F# UI framework library (`FS.GG.UI.*` packages) — single-repo, multi-project.

**Performance Goals**: No regression and no improvement target — equivalent frame output, traces,
evidence artifacts, and serialized bytes are the contract (FR-006). The §7 golden-image/perf gates
are **out of scope** for this behavior-preserving phase (spec Assumptions; the parent report scopes
the gates to render-altering Phases 5–6, and states Phases 1–2 don't need them — this phase inherits
that reasoning because it is likewise behavior-preserving by construction).

**Constraints**: Behavior-preserving (FR-006); preserved frame-composition / metric-accumulation
order on the render path (Edge Cases); preserved byte-exact wire format — endianness, tag width,
field order (Edge Cases); preserved fail-loud at every refactored site (FR-009); unchanged public
`.fsi`/surface baselines and no version bump (FR-007); no new project/dependency/inter-project
reference (FR-010); no back-edge (new internal files compile *before* the public `.fs`).

**Scale/Scope**: 3 user stories, 2 source projects (`src/SkiaViewer` incl. `Host/`, `src/Scene`),
3 god-module targets. Re-confirmed current-tree counts (2026-06-22):

| Target | File | Size | Worst symptom | Public surface |
|---|---|---|---|---|
| `Viewer` module | `src/SkiaViewer/SkiaViewer.fs` | **3,370** | `runPresentedPersistentWindow` (~L1421) + `runPersistentWindow` (~L1744) near-duplicate loops; input-queue + responsiveness + evidence all in one module | `SkiaViewer.fsi` (191) — `Viewer`/`GeneratedAppHost`/`Text` |
| `GlHost.run` | `src/SkiaViewer/Host/OpenGl.fs` | **1,454** | `run` (~L1153, ~295-line fn) inlines `interpretEffect` (~L1227), screenshot/readback (~L788+), event loop, damage | `OpenGl.fsi` (324) — `GlResources`/`GlStartup`/`GlHost` (run + pure decisions) |
| node codec | `src/Scene/SceneCodec.fs` | **1,571** | `writeSceneNode` (~L772, 25 arms) ‖ `readSceneNode` (~L1046) hand-aligned; symmetry by hand | `SceneCodec.fsi` (177) — package types + `SceneCodec` fns |

Per spec Assumptions, the binding outcomes are structural (one scaffold; one codec entry per node)
and equivalence — not an exact line count; the ~1,500 target is a guideline (plan §6 "≈").

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Assessment |
|---|---|
| **I. Spec → FSI → Tests → Impl** | Tier-2 refactor; public `.fsi` is **unchanged** (no new public surface). New helpers are internal-only files (no `.fsi`). Existing tests are the FSI-surface evidence. ✅ |
| **II. Visibility lives in `.fsi`** | New responsibility-group files carry **no `.fsi`** → private by compiler enforcement (the `SceneRenderer.fs`/`Numeric.fs` precedent). No `private`/`internal`/`public` modifiers added to `.fs` top-level bindings. The public `Viewer`/`GlHost`/`SceneCodec` signatures stay byte-identical. ✅ |
| **III. Idiomatic simplicity** | Pattern E (plain extraction) + Pattern A (a per-case record table — plainest way to make codec symmetry structural). No SRTP/reflection/custom operators/CEs. Any retained `mutable` on the window/GL hot path keeps its `// mutable: hot path` disclosure. ✅ |
| **IV. Elmish/MVU boundary** | The viewer already exposes `init`/`update`/`Msg`/`Effect` (ViewerModel, ViewerRunModel, EvidenceWorkflow); this phase moves bodies behind those same boundaries without adding or altering a workflow. N/A change. ✅ |
| **V. Test evidence** | No behavior change ⇒ obligation is "same tests fail/pass as before" + equivalent frames/traces/screenshots/bytes, captured against a pre-refactor baseline. No assertion weakened (FR-008). No synthetic evidence introduced (existing `Synthetic`-tagged tests untouched). ✅ |
| **VI. Observability & safe failure** | Fail-loud preserved at every refactored site — GL/context-creation failures still distinguish defect vs. missing window-system; screenshot-before-first-frame, malformed/truncated package, unknown node tag all keep their diagnostics (FR-009). ✅ |
| **Change Classification** | **Tier 2 (internal change)** — refactor, no behavioral change, no public API surface change. `.fsi` and surface baselines remain untouched; no version bump (FR-007). ✅ |
| **Engineering Constraints** | No new project, dependency, or inter-project reference (FR-010). Stays within `src/SkiaViewer` (+ `Host/`) and `src/Scene`. SkiaSharp/GL pins unchanged. ✅ |

**Gate result: PASS.** No violations → Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/187-viewer-glhost-codec-splits/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (internal record/table shapes)
├── quickstart.md        # Phase 1 output (baseline-capture + per-story verification recipe)
├── contracts/           # Phase 1 output (internal-helper contracts)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/SkiaViewer/
├── SkiaViewer.fs              # US1: Viewer module — public delegators stay; bodies move out
├── SkiaViewer.fsi             # UNCHANGED (Viewer/GeneratedAppHost/Text surface byte-identical)
├── (new) ViewerInputQueue.fs  # US1 internal: enqueue/drain/dirty-state bodies (no .fsi)
├── (new) ViewerResponsiveness.fs # US1 internal: latency/summary/json/markdown bodies (no .fsi)
├── (new) ViewerEvidence.fs    # US1 internal: screenshot/evidence-workflow bodies (no .fsi)
├── (new) ViewerWindow.fs      # US1 internal: ONE persistent-window lifecycle scaffold (no .fsi)
└── Host/
    ├── OpenGl.fs              # US2: GlHost.run becomes orchestrator; pure decisions stay public
    ├── OpenGl.fsi            # UNCHANGED (GlResources/GlStartup/GlHost surface byte-identical)
    └── (new) GlHostRun.fs    # US2 internal: rendering/input/damage/effects+screenshot units (no .fsi)

src/Scene/
├── SceneCodec.fs              # US3: public package types + SceneCodec fns stay; node wire delegates
├── SceneCodec.fsi            # UNCHANGED (package types + SceneCodec fns byte-identical)
└── (new) SceneWire.fs        # US3 internal: per-node codec table grouped Primitives/Paint/Path/Text/Scene (no .fsi)

tests/SkiaViewer.Tests/        # US1/US2 host/present/input-queue/responsiveness/OpenGl/live-proof/damage
tests/Scene.Tests/             # US3 Feature146 round-trip/resource/capability + Feature183 codec symmetry
tests/Smoke.Tests/             # US2 GL smoke
tests/Elmish.Tests/            # US1 Feature167/174 responsiveness regressions
tests/Rendering.Harness.Tests/ # evidence artifacts consuming viewer screenshots / scene packages
tests/Package.Tests/           # SurfaceAreaTests — proves SkiaViewer/Scene baselines unchanged
```

**Structure Decision**: Single-repo multi-project F#. No new project/file with a `.fsi`; each split
introduces **internal-only `.fs` files (no `.fsi`)** inserted in `.fsproj` compile order *before*
the public `.fs` that delegates to them (so no back-edge): new SkiaViewer files before
`SkiaViewer.fsi` (L39, immediately before `SkiaViewer.fs`); `GlHostRun.fs` before `Host/OpenGl.fsi`
(L33 — it must precede `Host/OpenGl.fs` (L34), which delegates to it, so anchoring it merely before
`Host/Viewer.fsi` (L35) would permit a back-edge); `SceneWire.fs` between `Scene.fs` and
`SceneCodec.fsi`. The exact new-file names/count are confirmed in research.md
— the binding rule is "bodies out, contracts stay." US1→SkiaViewer, US2→OpenGl, US3→Scene; the three
are independent and independently testable.

## Complexity Tracking

> No constitution violations — section intentionally empty.

## Implementation Progress (2026-06-22, `/speckit-implement`)

Behavior-preserving by construction; every change verified against the pre-refactor baseline
(`readiness/baseline.md`), the codec byte-corpus (`readiness/codec-corpus.*`), and the surface gate.
Full per-criterion sign-off: `readiness/success-criteria.md`.

| Story | Status | Outcome |
|---|---|---|
| **US3 — SceneCodec split** | ✅ **Complete & verified** | `src/Scene/SceneWire.fs` (internal wire codec, 887 L) carved out; `SceneCodec.fs` **1,571 → 641**. Wire bytes + `FS.GG.UI.Scene` surface byte-identical; `Scene.Tests` 75/75; codec corpus hashes identical; `FS0025` exhaustiveness gate intact (SC-001/003/004/006). |
| **US1 — Viewer split** | ⚠️ **Partial (verified)** | `ViewerInputQueue.fs` (`module ViewerInputQueueOps`) + `ViewerResponsiveness.fs` carved out with public delegators; `SkiaViewer.fs` **3,370 → 2,841**. Surface byte-identical; `SkiaViewer.Tests` 207/207, `Elmish` 209/17-skip, `Harness` 209/209 — no regression. **Evidence runners + window scaffold (T011/T012) deferred** — forward-dependent live-path. |
| **US2 — GlHost.run** | ⏸️ **Deferred (documented)** | `run` is a closure over ~15 run-local mutables; extraction requires a state-record rewrite that risks the protected float-accumulation / present-sequencing order (Edge Cases, FR-006). `OpenGl.fs` already 1,454 ≤ ~1,500, so SC-001 is met without it. |

### Key implementation findings (vs. plan assumptions)

- **`Tag: byte` in data-model.md is wrong** — node tags are `Int32` on the wire (`writer.Write(0)` /
  `reader.ReadInt32()`); the implementation keeps `int` to preserve SC-004.
- **The read-side codec table already existed** (`sceneNodeCodec`/`readerByTag`, deliberate prior
  design with FS0025 + Feature183) — US3's real deliverable was the *file split*, not a Pattern-A
  conversion. The deliberate write-match + read-table design was preserved, not churned.
- **The file-split premise only generalizes to *self-contained* bodies.** The live-path window/run-loop
  bulk closes over forward-defined mutable state and cannot move before the public `.fs` (back-edge)
  without a rewrite the behavior-preserving mandate forbids — hence the documented US2 / US1-window
  deferral (research R2 permits the scaffold to degrade; SC-001 is a guideline).

This increment is independently shippable (spec Assumptions / Incremental Delivery): it is strictly
additive structure with zero behavior or public-surface change. The deferred legibility passes are
tracked for a dedicated follow-up.
