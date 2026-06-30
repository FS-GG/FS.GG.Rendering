# Quickstart: Validate Headless Image Evidence

A runnable validation guide proving the feature end-to-end. Implementation details live in [tasks.md](./tasks.md); contracts in [contracts/scene-evidence-png.md](./contracts/scene-evidence-png.md).

## Prerequisites

- .NET SDK (`net10.0`), repo restored/buildable (see the FSharp.Core lockfile note in CLAUDE memory if restore is blocked).
- **No GPU, GL, X server, or virtual display required** for the P1 scenario — that is the point.
- A GL/virtual-display host (e.g. `Xvfb` + EGL) is required **only** for the US2 live-window scenario.

## Scenario 1 — Deterministic headless PNG (US1 / P1, the MVP)

**Goal**: a real, decodable, non-blank, byte-identical PNG with no GPU/display.

1. Build the viewer (owns the CPU rasterizer + injection):
   ```bash
   dotnet build src/SkiaViewer/SkiaViewer.fsproj
   ```
2. From the public surface, inject the rasterizer and render a representative game scene to PNG twice at a fixed size (e.g. 640×360). The supported entry is `SceneEvidence.renderPng size scene` after SkiaViewer wiring has called `Scene.setRealPngRasterizer` (see contract C1/C2).
3. **Expected**:
   - Both calls return `Ok bytes`.
   - `bytes` **decodes as a PNG** of exactly the requested W×H (contract C1.1).
   - Pixel content is **non-blank** and shows the scene's shapes/colors/text (FR-002).
   - The two byte arrays are **identical** (FR-003 / contract C1.2).
4. Run the semantic tests directly:
   ```bash
   dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter "HeadlessImageEvidence"
   ```
   Expect determinism, dimensions, non-blank, and **concurrency** (contract C1.7) tests green.

## Scenario 2 — Honest failure, no silent stub (US3 / P3)

1. Force an unproducible request (e.g. invoke `renderPng` with **no** rasterizer injected, or an unsupported renderer mode).
2. **Expected**: `Error` with a typed `SceneEvidenceFailure` carrying a `BlockedStage`, a `Classification` (`UnsupportedEnvironment` vs `ProductDefect`), and a message (contract C1.3/C1.4) — and **no `byte[]` smaller than a valid image** is returned as success (SC-005 / contract C1.5).
3. Edge checks: zero/negative size → `ProductDefect`; very large size → success-within-bounds or a clear resource diagnostic, never a stub.

## Scenario 3 — Live-window pixel proof (US2 / P2, GL-required)

1. In a `Xvfb` + EGL virtual display, run the viewer with `PresentMode = ViewerPresentMode.OffscreenReadback`.
2. Follow the documented capture path in `docs/usage.md` (the steps added by FR-006).
3. **Expected**: a **non-black** image of the current frame, with **zero** undocumented steps (SC-003 / contract C4). Note this path requires GL/virtual-display and is distinct from Scenario 1.

## Scenario 4 — Existing surfaces unbroken (FR-007) & no regressions

1. Confirm `Hash`/metadata/evidence-file consumers are unchanged (record in `readiness/fr007-diff.md`).
2. Re-run the full baseline and diff against the pre-change baseline:
   ```bash
   dotnet fsi scripts/baseline-tests.fsx --out specs/221-headless-image-evidence/readiness/baseline.md
   ```
   Expect no new reds versus the Phase-1 baseline (T002 vs T026).
3. Confirm the surface gate passes with the new seam in the baseline:
   ```bash
   dotnet test tests/Package.Tests/Package.Tests.fsproj
   ```

## Success = all of

- Scenario 1 green (decodable, non-blank, identical, concurrency-safe) — SC-001, SC-002.
- Scenario 2 green (typed classified failure, no stub) — SC-005.
- Scenario 3 green (documented non-black live capture) — SC-003.
- Scenario 4 green (FR-007 intact, baseline clean, surface gate passes).
- A representative scene renders in < 5 s (SC-004), recorded under `evidence/`.
