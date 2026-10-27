# Skipped tests

Per constitution Principles V & VI and FR-011, a test that cannot pass for an **out-of-scope**
or **host-capability** reason is recorded as an explicit skip with written rationale — **never
marked passing, never weakened, never a vacuous green**. This ledger is the single index of every
such skip. Three kinds live here:

- **Unconditional skips** — always skipped in this repo until an import/artifact lands. Fixed count.
- **Scheduled-only skips** — excluded from the PR tier but active in a named evidence cadence.
- **Host-capability skips** — skipped only when the host lacks a capability (offscreen raster, a
  live window/GL context, a Linux desktop session); they run and **fail loudly** on a capable host.
  Conditional, so no fixed count.

## 1. Performance corpus / baselines → scheduled evidence cadence

- `tests/Elmish.Tests/Feature109CorpusTests.fs` — "performance-scenario corpus, deterministic
  metrics goldens" (`ptestList` in the default tier, active on the scheduled tier).
- `tests/Elmish.Tests/Feature109BaselineReportTests.fs` — "non-golden timing/allocation
  baselines" (`ptestList` in the default tier, active on the scheduled tier).

**Owner/review**: `FS-GG/FS.GG.Rendering#1047`, review by **2026-11-02**. The
`pending-tests.yml` Monday cadence sets `FSGG_SCHEDULED_PENDING_TESTS=1`, regenerates the
environment-dependent evidence in an isolated job, asserts both suites, and uploads the result.
The default PR tier keeps them pending so wall-clock/allocation evidence cannot become a merge gate.
`PendingTestOwnershipTests` fails once the review date expires.

**Reviewed 2026-10-27**: the [26 October scheduled run](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/40312471519)
at `775505f82d4391360c2b5ee610be0ffeaa4f385a` passed all 308 tests with no failures or skips,
using the scheduled flag and both regeneration flags, and uploaded its evidence. The
[owner review](https://github.com/FS-GG/FS.GG.Rendering/issues/1047#issuecomment-6436025407)
retains this cadence and the unchanged assertions. Rendering maintenance and the active programme
coordinator own the next Monday review on 2 November, or earlier review if the cadence fails,
misses a run, its flags/evidence drift, or a supported tier change is proposed. The original
27 October expired-review failure remains retained; this review does not promote timing or
allocation evidence into PR acceptance.

**Review when**: the scheduled evidence should be promoted, split, or retired. A capable
faithful-vsync runner may justify a separate host cadence, but is not required for this
deterministic offscreen/count evidence.

## 2. Documentation-fence whole-corpus drive — active

`tests/DocFences.Tests/DriveTests.fs` uses an active `testList` under the compilation model
established by Rendering#1050 and refreshed by Rendering#1256. The complete inventory classifies
fences as `SelfContained` or `Contextual` with a reason. Every self-contained fence compiles
against the published pins; contextual snippets retain their symbol oracle. This is no longer
an unconditional pending suite. Its existing unpublished-pin release-window waiver remains
conditional on the actual restore result.

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
