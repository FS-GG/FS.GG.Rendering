# Implementation Plan: Headless Image Evidence Path

**Branch**: `221-headless-image-evidence` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/221-headless-image-evidence/spec.md`

## Summary

`SceneEvidence.renderPng` (`src/Scene/Evidence.fs:108-118`) is a stub: for `Format = Png` it returns the UTF-8 bytes of the deterministic **hash string**, not a decodable PNG. A consumer agent driving the TestSpec tutorial in a headless container could therefore obtain **no pixel proof** of the game — the live viewer presents direct-to-swapchain (no GPU→CPU readback, so X11 capture reads black) and the on-demand offscreen routine requires GL.

A working, **no-GL, no-display CPU rasterizer already exists** in the codebase — `ReferenceRendering.renderScenePng` (`src/SkiaViewer/ReferenceRendering.fs:119-137`) and the already-wired `writeSceneImageEvidence` (`src/SkiaViewer/SkiaViewer.fs:1824-1842`), both rasterizing via the shared exhaustive painter `SceneRenderer.paintNode` onto an `SKBitmap`/`SKSurface.Create(info)` with no `GRContext`. The technical approach is therefore **not** to write a new rasterizer but to **bridge** the existing CPU raster into the dependency-light `Scene.Evidence` surface that CI/consumers call directly.

Because `src/Scene` MUST stay SkiaSharp-free (zero package/project refs; `src/Scene/skill/SKILL.md:53`), the bridge is an **injectable seam** mirroring the existing `Scene.setRealTextMeasurer` (`src/Scene/Scene.fsi:131`, injected from `src/SkiaViewer/Fonts.fs:520`): Scene declares `setRealPngRasterizer`, SkiaViewer injects the CPU rasterizer at the same wiring point, and `renderPng` calls the injected function. When no rasterizer is injected (or a render genuinely cannot complete), `renderPng` returns the **already-existing** typed failure (`SceneEvidenceFailure` / `UnsupportedEnvironment | ProductDefect`, `Evidence.fs:11-49`) and writes nothing — never a success-shaped stub. The live-window route (US2) is documented atop the existing GL `OffscreenReadback` → `renderSceneToPixels` path. FR-009 doc claims (`no software-renderer fallback`) are flipped.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The hypotheses above are drawn from a static codebase map. `tasks.md` schedules an **early live
> smoke run** (T005) in the Foundational phase that drives the real headless viewer
> (`PresentMode = OffscreenReadback`) and confirms/replaces these hypotheses **before** any fix is
> built. Do not treat the donor-rasterizer assumption as proven until T005 records live evidence.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default)

**Primary Dependencies**: SkiaSharp over OpenGL (GL) for the viewer; the **headless path uses SkiaSharp CPU raster only** (`SKBitmap`/`SKSurface.Create(SKImageInfo)`, no `GRContext`). HarfBuzz + 9 bundled `.ttf` (embedded in SkiaViewer) for deterministic text. `src/Scene` itself stays **SkiaSharp-free**.

**Storage**: Filesystem — PNG/evidence artifacts under `specs/221-headless-image-evidence/evidence/` and the standard evidence writers; no database.

**Testing**: Semantic tests through the packed/public surface (constitution Principle I/V). New tests in `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs`; release-only surface gate in `tests/Package.Tests`; full-graph baseline via `scripts/baseline-tests.fsx`.

**Target Platform**: Linux/desktop CI container — the P1 path MUST run with **no GPU, no OpenGL context, no X server, no virtual display**. The US2 live-window path additionally needs a GL/virtual-display host.

**Project Type**: Single multi-project F# solution (rendering framework as a product). Relevant projects: `src/Scene` (dependency-light contract surface), `src/SkiaViewer` (SkiaSharp host + CPU raster donor), `tools/Rendering.Harness` (evidence harness), `tests/*`.

**Performance Goals**: A single representative game scene renders to PNG in **under 5 s** on a standard CI runner (SC-004). "Slow is acceptable" (FR-008) — portability over speed — but bounded.

**Constraints**: Deterministic (byte-identical) output across runs/machines (FR-003); `src/Scene` must not gain a SkiaSharp dependency; existing `Hash`/metadata/evidence-file surfaces unchanged (FR-007); no success-shaped non-image artifacts (FR-005/SC-005).

**Scale/Scope**: Small, surgical Tier 1 change — one new Scene seam, one SkiaViewer injection, one rewired `renderPng` branch, plus tests, docs (FR-009), and edge-case disclosures. No new packages, no new cross-repo contracts.

## Constitution Check

*GATE: evaluated against FS.GG.Rendering Constitution v1.1.0.*

| Principle / Constraint | Status | How this plan complies |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | ✅ | Spec done; `tasks.md` T007 drafts the `setRealPngRasterizer` `.fsi` seam and exercises it in FSI **before** `.fs`; tests T008-T010/T018-T019 precede implementation. |
| **II. Visibility lives in `.fsi`** | ✅ | New seam declared in `src/Scene/Scene.fsi`; no `private`/`internal`/`public` modifiers added to `.fs`; surface baseline updated (T024). |
| **III. Idiomatic simplicity** | ✅ | Reuses existing painter + CPU surface; no SRTP/reflection/type-providers/custom CEs. The injection seam copies an established, justified pattern (`setRealTextMeasurer`). Any `mutable` in a render hot path is disclosed at the use site. |
| **IV. Elmish/MVU boundary for stateful/I-O** | ✅ N/A | `renderPng` is a pure-ish request→`Result` function (scene + size → bytes-or-failure) with no multi-step state machine; no Elmish boundary required. Failure is data (`SceneEvidenceFailure`), not exceptions. |
| **V. Test evidence mandatory** | ✅ | Fail-before tests for determinism, non-blank, dims, concurrency, no-stub regression; prefer real evidence (real CPU raster, real PNG decode, real bundled fonts). Any synthetic substitute disclosed with the `Synthetic` token + use-site comment. |
| **VI. Observability & safe failure** | ✅ | `renderPng` fails fast with a **typed, classified** diagnostic (stage + `UnsupportedEnvironment`/`ProductDefect`), never silent/swallowed; GL-vs-environment distinction preserved (US2/US3). |
| **Change Classification** | ✅ | Declared **Tier 1** in spec.md → full chain (spec, plan, `.fsi`, baseline, tests, docs). T007 + T024 satisfy `.fsi`/baseline obligations. |
| **Engineering Constraints** | ✅ | net10.0; SkiaSharp pinned (no new/preview package added — reuses existing refs); `src/Scene` stays SkiaSharp-free via the seam; no new dependency introduced; package identity unchanged (`FS.GG.UI.*`). |

**Gate result: PASS** — no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/221-headless-image-evidence/
├── plan.md              # This file
├── spec.md              # Feature spec (Tier 1)
├── research.md          # Phase 0 — decisions (this command)
├── data-model.md        # Phase 1 — evidence entities (this command)
├── contracts/           # Phase 1 — Scene seam + renderPng contract (this command)
│   └── scene-evidence-png.md
├── quickstart.md        # Phase 1 — headless-PNG validation guide (this command)
├── checklists/          # spec quality checklist (existing)
├── readiness/           # baseline, smoke-run, root-cause map, degradation list, closeout
├── evidence/            # PNG proofs, timing, live-frame capture
└── tasks.md             # /speckit-tasks output (27 tasks)
```

### Source Code (repository root) — files touched

```text
src/Scene/                       # dependency-light contract surface — STAYS SkiaSharp-free
├── Scene.fsi                    # + setRealPngRasterizer seam (mirror setRealTextMeasurer:131)
├── Scene.fs                     # + default seam wiring (typed-failure default)
├── Evidence.fsi                 # renderPng signature unchanged; failure model already public
└── Evidence.fs                  # rewire Png branch (79-118) to call injected rasterizer; honest failure

src/SkiaViewer/                  # SkiaSharp host — owns the CPU raster donor + injection
├── ReferenceRendering.fs        # renderScenePng donor (119-137) → renderScenePngResult entry
├── SkiaViewer.fs                # writeSceneImageEvidence sibling donor (1824-1842)
├── SceneRenderer.fs             # shared painter paintNode (246-418) — reused, not changed
├── Fonts.fs                     # inject Scene.setRealPngRasterizer at the measurer wiring point (520)
└── Host/OpenGl.fs               # US2 doc reference: renderSceneToPixels (788-826), GL-required

tools/Rendering.Harness/         # existing T0 CPU determinism harness (Tiers.fs:49-54) — reference only

tests/
├── SkiaViewer.Tests/HeadlessImageEvidenceTests.fs   # NEW — US1/US3 semantic tests
└── Package.Tests/                                    # surface-drift gate (Tier 1 baseline update)

docs/
├── usage.md                                          # FR-009 headless/offscreen + US2 capture path
└── harness/capability-baseline.md                    # FR-009 T1 row

template/base/docs/evidence-formats.md                # FR-009 flip `no software-renderer fallback`
```

**Structure Decision**: Existing single-solution F# layout; no new projects. The one architectural decision is **where the rasterizer lives and how Scene reaches it** — resolved as: rasterizer stays in `src/SkiaViewer` (where SkiaSharp already is), reached from `src/Scene` through an injectable seam, preserving the dependency-light Scene contract. See [research.md](./research.md) for the alternatives weighed.

## Complexity Tracking

*No Constitution Check violations — section intentionally empty.*
