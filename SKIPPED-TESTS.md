# Skipped tests

Per constitution Principles V & VI and FR-011, a test that cannot pass for an **out-of-scope**
or **host-capability** reason is recorded as an explicit skip with written rationale — **never
marked passing, never weakened, never a vacuous green**. This ledger is the single index of every
such skip. Two kinds live here:

- **Unconditional skips** — always skipped in this repo until an import/artifact lands. Fixed count.
- **Host-capability skips** — skipped only when the host lacks a capability (offscreen raster, a
  live window/GL context, a Linux desktop session); they run and **fail loudly** on a capable host.
  Conditional, so no fixed count.

## 1. Performance corpus / baselines → Stage R5 (harness) — unconditional

- `tests/Elmish.Tests/Feature109CorpusTests.fs` — "performance-scenario corpus, deterministic
  metrics goldens" (`ptestList`).
- `tests/Elmish.Tests/Feature109BaselineReportTests.fs` — "non-golden timing/allocation
  baselines" (`ptestList`).

**Why**: these depend on committed perf-golden fixtures, `docs/reports/_baselines/**`, and
byte-identical perf determinism — exactly the performance-evidence tier that R3 routed to the
**Stage R5 rendering/perf harness**. They are not product-behavior unit tests and were not
part of the import-now behavior surface. The honest-FrameMetrics fidelity tests in the same
feature (not golden-dependent) remain **active and passing**.

**Un-skip when**: the R5 harness lands with its committed perf goldens and a deterministic
perf-capture path.

**R5 status (2026-06-14)**: the harness T3 perf tier now provides a **headless offscreen
render-throughput** mode (`harness perf --mode throughput`) with real per-frame timing +
percentiles, honestly scoped (`offscreen-render-throughput`, **not** vsync-faithful). The
**faithful vsync/present-timing** perf path these Feature109 tests want still depends on the
live present loop (blocked headlessly in this container — see
`docs/harness/capability-baseline.md`), so they remain `ptest`/`ptestList` until that tier
lands.

## 2. FSI transcript fixture → excluded old-repo artifact — unconditional — 1

- `tests/Controls.Tests/TypedControlContractTests.fs` — "FSI transcript expectations …"
  (`ptest`).

**Why**: it asserts on `specs/028-agent-validation-framework/readiness/fsi-session.txt`, an
old-repo feature-workflow/readiness artifact deliberately **not imported** (FR-009). The rest
of that test file (typed front-door contract checks) is active and passing.

**Un-skip when**: a current FSI transcript fixture is added under this repo.

## 3. Parity samples not imported → Stage R4 — unconditional — 4

- `tests/Lib.Tests/Tests.fs` — the three US4 sample contract smokes `BasicViewer`,
  `InteractiveViewer`, `ScreenshotGallery` (each `if File.Exists project then <full smoke> else
  skiptest`).
- `tests/Smoke.Tests/Tests.fs` — the `skipIfNoSamples ()` gate (`skiptest`), guarding the
  project-reference parity contract checks; keyed on the `samples/BasicViewer` parity marker, not
  the mere existence of `samples/` (feature 123 populates `samples/ControlsGallery`, a *different*,
  package-consuming sample with its own GL-free contract test that stays active).

**Why**: the project-reference parity samples tree (`samples/BasicViewer`,
`samples/InteractiveViewer`, `samples/ScreenshotGallery`) was **not imported** at migration Stage
R4. Until it lands, the smokes have no sample project to run, so they record an explicit skip —
**not** the vacuous `Expect.isFalse (File.Exists project)` self-passing assertion they carried
before (repo review 2026-07-02, finding P8/T1). Each smoke self-restores to its full assertion the
moment its sample project exists.

**Un-skip when**: the parity samples are imported under `samples/` (a `--contract-smoke` entry
point emitting the `status=ok` / `contains-*` / `screenshot-format=*` lines each smoke asserts).

## 4. Host-capability render tiers — conditional (headless-only)

These run and **fail loudly** on a capable host; they skip-with-tier only where the capability is
absent, recorded via a shared helper so the skip carries its tier (never an intermittent red, never
a faked pass — Constitution VI).

**T1 — offscreen raster / pixel (SkiaSharp `SKSurface` unavailable headless)** — `withRaster` /
`tierSkip`:

- `tests/SkiaViewer.Tests/Feature063RendererTests.fs`
- `tests/SkiaViewer.Tests/Feature086SceneTranslateTests.fs`
- `tests/SkiaViewer.Tests/Feature136TextRenderingTests.fs`
- `tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs`
- `tests/SkiaViewer.Tests/Audit_ReplayCache.fs`

**T2 — live native window / GL context (no desktop session,
`desktopSessionDiagnostic=unsupported-host`)** — `liveWindowSkip`:

- `tests/SkiaViewer.Tests/Tests.fs` — the window/GL-context-requiring cases.

**Linux desktop-session diagnostics (not applicable off Linux)**:

- `tests/SkiaViewer.Tests/Tests.fs` — the two `desktop diagnostics …` cases skip on non-Linux
  hosts (`skiptest "Linux desktop-session diagnostics are not applicable on this host"`).

**Why**: the container/headless host in this environment exposes neither an offscreen raster
surface nor a live window/GL context (`docs/harness/capability-baseline.md`). The pure
measurement/structural/font-resolution siblings of these tests need no surface and stay active.

**Un-skip when**: run on a raster-capable / windowed / Linux-desktop host — the guarded body then
executes and asserts for real.

## Note — test-generated artifacts

Some Controls/Elmish suites regenerate goldens/readiness files into repo-relative paths
(`specs/<n>/readiness/**`) when a committed golden is absent. Those generated directories are
**not source** and were removed after the import run; controlling their output location is a
Stage R6 (CI wiring) concern. The **exception** is the Feature093 migration-parity oracle
(`specs/093-visual-state-style-layer/readiness/parity/*.scene.txt`): those are **committed
goldens** (`.gitignore`-allowlisted), regenerated only under `PARITY_REGEN=1` and compared against
on every run — not rewritten each run (repo review 2026-07-02, finding P8/T2).
