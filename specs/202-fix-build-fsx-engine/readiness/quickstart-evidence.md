# T024 / T025 — Acceptance pass + Success-Criteria mapping (live)

Environment: dotnet 10.0.301; local-feed `~/.local/share/nuget-local`; coherent feed packed at
`V=0.1.49-preview.1` (and `V2=0.1.50-preview.1` for the lock-step proof).

## Success Criteria

| SC | Outcome | Evidence |
|----|---------|----------|
| **SC-001** | ✅ | govgate + headlessgate (and app/sample) Verify exit 0; EvidenceGraph + EvidenceAudit execute against the resolved engine, writing real `evidence-graph.md` + `evidence-audit.md` (`verdict=PASS`), not log-only stubs. See `verify-evidence.md`, `smoke-evidence.md`. |
| **SC-002** | ✅ | After `dotnet restore`, `dotnet fsi build.fsx target Verify` runs end-to-end with zero extra manual engine-setup (consumer path). The maintainer-side coherent-feed pack is the framework workflow, not a consumer step. |
| **SC-003** | ✅ | Exactly one `FsSkiaUiVersion` literal; re-pin `0.1.49 → 0.1.50` + restore moved the engine (cache `fs.gg.ui.build/0.1.50-preview.1/` created). See `verify-evidence.md` T017. |
| **SC-004** | ✅ | `grep -Eri "fs\.skia\.ui\.build\|FS\.Skia\.UI" build.fsx` → none, in the generated product. Guarded by the new `GovernanceTests` scan (runs in all profiles). |
| **SC-005** | ✅ | Engine-absent run exits 1 with a message naming `FS.GG.UI.Build <version>` + cache path/feed. See `honest-failure-evidence.md`. |

## FR coverage

FR-001..FR-008 all satisfied; FR-006 (in-process; only `dotnet test` external) is held by the generated
`GovernanceTests` in-process/no-decommissioned-scripts assertions (green in T018) and by the engine
itself using zero external processes / FSharp.Core only.

## T024 — Comprehensive baseline (no net-new reds from this change)

`scripts/baseline-tests.fsx` over every test project (solution + Package.Tests + samples):

- **New `tests/Build.Tests` → 🟢 PASS (6/6).**
- Stable pre-existing reds, **identical** between the Phase-1 (`baseline.md`) and final
  (`baseline-final.md`) runs, in subsystems this feature does not touch:
  - `tests/Package.Tests` — 8 failures: 7× *Feature128 design-system template validation* (GV-1..7)
    + 1× *Feature163 AntShowcase pin coherence*. (SurfaceArea tests — incl. the new
    `FS.GG.UI.Build` baseline — are **green**, 12/12.)
  - `samples/AntShowcase.Tests` (2), `samples/ControlsGallery.Tests` (2),
    `samples/SecondAntShowcase.Tests` (6) — sample package-pin coherence vs source versions.
- **Disclosed flaky (environment-limited, NOT a regression):** `tests/SkiaViewer.Tests` shows a
  nondeterministic GL/window failure count across runs (observed 0, 1, then 2 of ~190–207). `git`
  confirms `src/SkiaViewer` and `tests/SkiaViewer.Tests` are **untouched** by this changeset; the
  flakiness is the known headless GL/display sensitivity, not caused by feature 202. Not summarized as
  green — flagged here per the Feature-168 evidence rules.

**Conclusion:** zero net-new reds attributable to this change; the feature's own new tests are green and
all four generated profiles pass `Verify` with the evidence gates executing against the real engine.
