# Quickstart & Validation Guide: God-Module Splits (Feature 182)

This is the runnable validation flow proving each split is **behavior- and surface-preserving**. It is
a refactor: there is no new feature to demo — the deliverable is *byte-stability against a baseline*.
All commands run from the repo root with `DISPLAY=:1` exported (GL needs a display).

## Prerequisites

- .NET SDK with `net10.0`; `DISPLAY=:1` available (Xvfb or a real display).
- A clean working tree (`git status` clean) before baseline capture.
- The 12 surface baselines committed at HEAD (`readiness/surface-baselines/*.txt`).

## Step 0 — Capture the pre-edit baseline (Setup, once)

```bash
export DISPLAY=:1
mkdir -p specs/182-god-module-splits/readiness/{baseline,post-change}

# (a) Surface snapshot — regenerate then confirm the tree is clean (no drift at HEAD)
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --exit-code readiness/surface-baselines/      # MUST be empty at HEAD
cp readiness/surface-baselines/*.txt specs/182-god-module-splits/readiness/baseline/

# (b) Full red/green snapshot across every *.Tests.fsproj (incl. release-only / sample lanes)
dotnet fsi scripts/baseline-tests.fsx --out specs/182-god-module-splits/readiness/baseline/

# (c) Artifact + render snapshot for the touched subsystems:
#     regenerate readiness/evidence (MD+JSON), viewer observations/screenshots,
#     scene hashes/fingerprints, damage regions — archive under baseline/
#     (use the same regeneration commands the touched suites/harness already expose)
```

Record the **allowed pre-existing non-green set** (known `Package.Tests` / `ControlsGallery`
stale-feed reds, per features 180/181) — these are baseline-not-regression, not failures to fix.

## Step 1 — Per-story validation loop (run after EACH of US1…US6)

After implementing a story, the story is "green" only when all three oracles pass:

```bash
export DISPLAY=:1

# Oracle 1 — SURFACE INVARIANCE (FR-002, SC-001): regenerate and diff. MUST be empty.
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --exit-code readiness/surface-baselines/

# Oracle 2 — BUILD + RED/GREEN PARITY (FR-008, SC-003): full sweep, same set as baseline.
dotnet build FS.GG.Rendering.slnx -c Release
dotnet fsi scripts/baseline-tests.fsx --out specs/182-god-module-splits/readiness/post-change/
#   compare post-change/ red/green vs baseline/ — identical set (no new reds, no flipped greens)

# Oracle 3 — ARTIFACT/RENDER BYTE-DIFF (FR-003, SC-002): regenerate the touched subsystem's
#   readiness/evidence + render artifacts, then diff byte-for-byte vs baseline/.
diff -r specs/182-god-module-splits/readiness/baseline/ specs/182-god-module-splits/readiness/post-change/
```

Per-story focus for Oracle 3 (which artifacts to regenerate & diff):

| Story | Surface file (Oracle 1) | Render/artifact diff (Oracle 3) |
|-------|-------------------------|----------------------------------|
| US1 SkiaViewer | `FS.GG.UI.SkiaViewer.txt` | viewer evidence/screenshots, window observations, diagnostics |
| US2 Control | `FS.GG.UI.Controls.txt` | chart scene + scene-hash + fingerprint for every chart control |
| US3 Scene | `FS.GG.UI.Scene.txt` | visual + retained inspection records (tokens, findings, serialized) |
| US4 Testing | `FS.GG.UI.Testing.txt` | every emitted readiness/evidence Markdown + JSON |
| US5 RetainedRender | `FS.GG.UI.Controls.txt` | rendered scene, damage regions, step metrics, promotion decisions |
| US6 FrameLoopState | `FS.GG.UI.Controls.Elmish.txt` | frame-loop transitions, emitted commands, render-lag traces |

> If any oracle fails, the split overshot. Narrow the seam, or retain the un-splittable unit / un-unified
> dedup explicitly per **FR-009** and record the rationale — never edit a surface baseline to "accept"
> drift, and never weaken an assertion to green a build (Constitution V).

## Step 2 — Phase-end verification (Polish)

```bash
export DISPLAY=:1
dotnet build FS.GG.Rendering.slnx -c Release
dotnet fsi scripts/baseline-tests.fsx --out specs/182-god-module-splits/readiness/post-change/
dotnet fsi scripts/refresh-surface-baselines.fsx && git diff --exit-code readiness/surface-baselines/

# Size targets (SC-005) — confirm no touched module > ~1,500 lines (goals, FR-009 exceptions recorded)
wc -l src/SkiaViewer/*.fs src/Controls/Control*.fs src/Controls/RetainedRender.fs \
      src/Scene/*.fs src/Testing/*.fs src/Controls.Elmish/ControlsElmish*.fs
```

## Done when (success criteria)

- [ ] **SC-001** — all 12 surface baselines byte-identical; `git diff readiness/surface-baselines/` empty after every story (zero baseline edits).
- [ ] **SC-002** — 100% of regenerated rendered/evidence/readiness artifacts, viewer observations, scene hashes/fingerprints, damage regions byte-identical to baseline across all six splits.
- [ ] **SC-003** — `dotnet build` + full sweep green at each story end and phase end; known pre-existing reds unchanged (same red/green set as baseline).
- [ ] **SC-004** — each of the six targets is an independently-shippable slice (builds, passes suite, holds byte-stability on its own).
- [ ] **SC-005** — no touched module > ~1,500 lines, no touched function > ~150 lines, except units explicitly retained per FR-009 with recorded rationale.
- [ ] **SC-006** — viewer window-lifecycle dedup (FR-004), the ×17 chart preamble (FR-005), and the Scene inspection dedup (FR-006) each unified OR explicitly retained with a recorded reason.
- [ ] **SC-007** — no new project, package dependency, or inter-project reference; dependency graph acyclic and unchanged (FR-010).
- [ ] Every FR-009 retention (un-split seam / un-unified dedup) recorded with its rationale.

See [contracts/](./contracts/) for the per-story split invariants and
[data-model.md](./data-model.md) for `StepMetrics` / `FrameLoopState` / the concern-module catalog.
