# Quickstart: Verifying the Viewer + GlHost + SceneCodec Splits

**Feature**: 187-viewer-glhost-codec-splits | **Date**: 2026-06-22

This is the run/validation guide. It proves the refactor is behavior-preserving by diffing every
user story against a pre-refactor baseline. Implementation detail lives in `data-model.md` /
`contracts/internal-contracts.md`; task ordering lives in `tasks.md`.

## Prerequisites

- Linux desktop with an X11 display for GL suites: `export DISPLAY=:1` (Xvfb is fine).
- .NET `net10.0` SDK; SkiaSharp/GL pins as committed.
- Clean tree on branch `187-viewer-glhost-codec-splits`.

## Step 0 — Capture the pre-refactor baseline (Foundational; BEFORE any production edit)

```bash
# Full-solution red/green snapshot (records the 2 known pre-existing reds too: Package.Tests ×8,
# ControlsGallery.Tests ×2 — package-feed/sample pins, unrelated to this work).
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release 2>&1 | tee specs/187-viewer-glhost-codec-splits/readiness/baseline-tests.log

# Which GL/timing suites SKIP without a usable surface (so a skip is never read as a regression).
grep -iE "skip" specs/187-viewer-glhost-codec-splits/readiness/baseline-tests.log

# Public surface snapshot — must be reproducible byte-for-byte afterwards.
cp readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt /tmp/187-skiaviewer.surface.txt
cp readiness/surface-baselines/FS.GG.UI.Scene.txt       /tmp/187-scene.surface.txt

# Serialized-byte corpus for the codec (export each fixture scene to package bytes; hash them).
# (Reuses the Feature146 round-trip fixtures — see Scene.Tests.)
```

Also stash reference frames/screenshots/traces emitted by the proof suites for byte/semantic diff.

**Expected**: baseline log shows the known red set and the GL skip set; surface files copied.

## Step 1 — US3: SceneCodec split + node codec table (lowest risk first)

```bash
DISPLAY=:1 dotnet test tests/Scene.Tests/Scene.Tests.fsproj -c Release
```

**Expected**:
- `Feature146PortableSceneRoundTripTests` — all green; exported bytes identical to the Step-0 corpus
  hashes (SC-004).
- `Feature183CodecSymmetryTests` — green (symmetry now structural, SC-003).
- Adding a throwaway `SceneNode` case locally yields an `FS0025` compile error on the write match
  (proves one-site exhaustiveness) — revert it.
- `src/Scene/SceneCodec.fs` ≤ ~1,500 lines; `SceneWire.fs` holds the table (SC-001).

## Step 2 — US2: GlHost.run decomposition

```bash
DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
DISPLAY=:1 dotnet test tests/Smoke.Tests/Smoke.Tests.fsproj -c Release
```

**Expected**:
- `Feature119OpenGlHostTests`, present/damage/live-proof suites, smoke — red/green identical to
  baseline; GL skips identical to the Step-0 skip set.
- GL-context-failure and screenshot-before-first-frame paths still fail loud with the same
  diagnostics (FR-009).
- `src/SkiaViewer/Host/OpenGl.fs` ≤ ~1,500 lines (SC-001).

## Step 3 — US1: Viewer module split + window scaffold

```bash
DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
DISPLAY=:1 dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release      # Feature167/174 responsiveness
DISPLAY=:1 dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release
```

**Expected**:
- Input-queue / responsiveness / present-mode / interactive-host / trace-readback suites — red/green
  identical to baseline.
- Screenshot / evidence / harness-evidence artifacts equivalent to the Step-0 references (byte where
  guaranteed, else documented semantic equivalence — SC-007).
- `src/SkiaViewer/SkiaViewer.fs` ≤ ~1,500 lines (SC-001).
- **SC-002 note**: record the resulting shared-scaffold count. Target is the two persistent-window
  runners sharing one lifecycle scaffold; if R2's shared surface proved small, document the
  shared-helpers outcome instead — US1 still passes on the module-group split.

## Step 4 — Surface invariance (all stories)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --exit-code readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt readiness/surface-baselines/FS.GG.UI.Scene.txt
diff /tmp/187-skiaviewer.surface.txt readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt
diff /tmp/187-scene.surface.txt       readiness/surface-baselines/FS.GG.UI.Scene.txt
DISPLAY=:1 dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release   # SurfaceAreaTests
```

**Expected**: both `git diff` and `diff` are **empty** (SC-006); `SurfaceAreaTests` no worse than the
8 known pre-existing reds; no version bump in any `.fsproj`/`.nuspec`.

## Step 5 — Final full-solution sweep

```bash
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release 2>&1 | tee specs/187-viewer-glhost-codec-splits/readiness/final-tests.log
diff <(grep -E "Passed!|Failed!|skipped" specs/187-viewer-glhost-codec-splits/readiness/baseline-tests.log) \
     <(grep -E "Passed!|Failed!|skipped" specs/187-viewer-glhost-codec-splits/readiness/final-tests.log)
```

**Expected**: red/green/skip set identical to baseline (no assertion weakened — FR-008/SC-005); the
two pre-existing reds remain exactly those two (no regression, no new red).

## Done when

- [ ] SC-001 file sizes met (3 targets ≤ ~1,500 lines).
- [ ] SC-002 window run loops share one scaffold (or documented shared-helpers outcome).
- [ ] SC-003 one codec entry per node kind; `FS0025` enforces exhaustiveness.
- [ ] SC-004 100% round-trip byte-identity on the corpus.
- [ ] SC-005 affected suites red/green identical to baseline.
- [ ] SC-006 surface baselines diff empty; no version bump.
- [ ] SC-007 frame/evidence equivalence demonstrated per story.
