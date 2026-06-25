# Implementation Plan: Symbology Live Board Sample (M6)

**Branch**: `193-symbology-live-board` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/193-symbology-live-board/spec.md`

**Source design**: [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) — roadmap milestone **M6 (live board sample)**, the first item deferred by spec 192 (which scoped M1–M5). M7 (legibility linter, Badge/Ring grammars, label text) stays backlog and out of scope here.

## Summary

Ship a runnable **live board sample** that puts the approved M5 symbol set in motion: a window opens showing the per-game roster of unit-symbols laid out on a board, each animating continuously per its motion channel, smooth between fixed simulation steps. The same board is **byte-for-byte reproducible** from a seed via a default headless evidence subcommand, so the milestone closes on captured seeded evidence with no wall clock and no GPU.

Technical approach (grounded against the tree on 2026-06-25):

- A new executable sample **`samples/SymbologyBoard/`** (`OutputType=Exe`, `IsPackable=false`) mirrors the accepted **`samples/CanvasDemo`** precedent exactly: a pure fixed-timestep simulation advanced by `FS.GG.UI.Canvas` `Loop.advance`/`Loop.alpha`; a `Canvas.volatile'` surface inside a `ControlsElmish.runInteractiveApp` host for the interactive path; a default `evidence` subcommand that folds a seeded scripted tick sequence to a single canonical scene fingerprint; and a graceful headless fallback via `Viewer.runtimeCapability()`.
- The **approved M5 mapping is reused unchanged**. The roster→`Token` logic from `specs/192-agent-unit-symbology/readiness/dry-run/FinalSymbolSet.fsx` is brought in-tree as a compiled `Roster.fs` (the `UnitStats` record + `factionOf`/`klassOf`/`sigilOf`/`mapUnit` and a fixed roster literal). It consumes the existing `FS.GG.UI.Symbology` public surface only — the fixed grammar is not re-opened.
- Motion is the per-symbol `Symbology.animate motion token phase` overlay plus a deterministic board drift, both driven solely by the simulation's accumulated step phase (no wall-clock, no render-time randomness, no IO). Symbols **bounce** off the board boundary (chosen, documented) so none drifts off-board (FR-011).
- The milestone closes on **captured seeded evidence** under `specs/193-symbology-live-board/readiness/`: the canonical board fingerprint plus a record that two runs from the same seed matched and that a different seed diverged.

Per the constitution this is a **Tier 2** (additive, internal) change: the sample is an `IsPackable=false` executable that consumes existing public API only and **does not touch any public package surface** (FR-012, SC-005) — so no `.fsi`/baseline changes. Principle I/V are honored with a small semantic-test project over the sample's deterministic core (reproducibility, seed-sensitivity, on-board invariant).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a *greenfield additive* sample, not a defect fix, so there is no root-cause map. The analogue
> of the live-smoke mandate is an **early evidence smoke run** (Foundational phase): before US1/US3 work,
> build the new sample and run its `evidence` subcommand twice in *this* checkout, confirming a non-empty
> board produces a stable fingerprint across two runs and a different fingerprint from a different seed.
> Deterministic unit tests can pass while the real run path is broken; treat that smoke evidence — not
> this plan's narrative — as the confirmation that the board renders and reproduces.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies** (all existing, consumed via public API only): `FS.GG.UI.Symbology` (`Token`, channel DUs, `Symbology.defaultToken`/`token`/`animate`/`gallery`); `FS.GG.UI.Canvas` (`Loop.init`/`advance`/`alpha`, `Elements`); `FS.GG.UI.Scene` (`Scene`, `SceneCodec.export`/`packageIdentity`); `FS.GG.UI.Controls` + `FS.GG.UI.Controls.Elmish` (`Canvas.volatile'`, `ControlsElmish.runInteractiveApp`, `InteractiveAppHost`); `FS.GG.UI.SkiaViewer` (`Viewer.runtimeCapability`, `ViewerOptions`); `FS.GG.UI.Themes.Default` (interactive theme). No new third-party dependency.

**Storage**: Filesystem only, and only outside the simulation/scene path — the captured readiness evidence artifact (fingerprint + reproducibility record) written by the documented command. The simulation and scene production perform no IO.

**Testing**: A new `tests/SymbologyBoard.Tests/` (xUnit-style, mirroring `tests/Canvas.Tests/`) exercising the sample's deterministic core through its public modules: same-seed reproducibility (SC-001), different-seed divergence (SC-002), the on-board/non-degenerate invariant across N steps (FR-011/SC-003), and a non-empty board for a degenerate roster (edge case). The sample's own `evidence` subcommand is the captured milestone evidence (FR-013/SC-006).

**Target Platform**: Linux/CI headless for the evidence path (CPU, no GL); a live-window/GL host for the interactive path, which degrades gracefully when absent (FR-007/SC-004).

**Project Type**: Multi-project F# solution (`FS.GG.Rendering.slnx`). One new `samples/` executable + one new `tests/` project. No new library, no skill changes.

**Performance Goals**: Visual smoothness only — render interpolates between fixed steps via `Loop.alpha`, as CanvasDemo does. No fixed fps/throughput guarantee is asserted (Assumptions, spec). The evidence path is design-time/headless, not a hot path.

**Constraints**: Determinism is the hard constraint — motion phase derives solely from accumulated fixed-timestep steps; no wall-clock, no render-time randomness, no IO in `update`/`renderScene`/`evidence`. Identical seed + script ⇒ identical fingerprint (SC-001); different seed ⇒ different fingerprint (SC-002). Every symbol stays legible and on-board across the run (bounce at boundary; zero-area token already degrades to a placeholder in the grammar — FR-011). Zero public surface drift on existing baselines (FR-012/SC-005). Optional raw input, if added, must be reconstructed deterministically and must not affect the evidence path (FR-014).

**Scale/Scope**: M6 only. One sample (≈3 source files: `Roster.fs`, `Board.fs`, `Program.fs`) + one test project. Roster = the single approved M5 set (6–10 units). Deferred: M7 (legibility linter, Badge/Ring grammars, label text), package-feed reference swap (publish step).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | The sample's deterministic core is exercised through its public modules by `tests/SymbologyBoard.Tests` (reproducibility, seed-sensitivity, on-board invariant), fail-before/pass-after. The consumed symbology/canvas surfaces are already FSI-validated (spec 191/192). A sample executable carries no packable public surface, so no new `.fsi` (the CanvasDemo precedent). |
| **II. Visibility in `.fsi`, not `.fs`** | PASS (scoped) | The sample is `IsPackable=false` and exposes no package surface; like `samples/CanvasDemo` it ships no `.fsi` and adds no surface baseline. It consumes existing curated `.fsi` surfaces only; no `private`/`internal`/`public` modifiers on `.fs` top-level bindings. |
| **III. Idiomatic Simplicity** | PASS | Pure functions + records + DUs; a fixed-timestep `Loop` fold; no SRTP/reflection/type providers/custom CE. Any local geometry mutation on a render inner loop is disclosed at the use site. |
| **IV. Elmish/MVU boundary** | PASS | Interactive state goes through the established MVU edge: `InteractiveAppHost` (`Init`/`Update`/`View`) driven by `ControlsElmish.runInteractiveApp`; `update` is a pure `Msg`→`Model` transition; the host `Tick` carries the fixed step; IO (window/present) happens only at the viewer edge. Same boundary as CanvasDemo. |
| **V. Test Evidence Mandatory** | PASS | Early evidence smoke run (real `evidence` subcommand) + semantic tests over the deterministic core + captured seeded readiness artifact (two-runs-matched record). Real evidence preferred; any synthetic disclosed per Principle V. |
| **VI. Observability & Safe Failure** | PASS | Interactive mode prints a clear "skipped — no live window" notice and exits zero on a headless host (never blocks/crashes); a non-reproducible evidence run fails loud with a diff-style message and non-zero exit; an unknown subcommand prints a usage hint and exits non-zero. |
| **Change Classification** | Tier 2 | Additive sample + test project; consumes existing public API only; **no public package surface change**, no new dependency, no inter-package contract change. `.fsi`/baselines untouched. |
| **Engineering Constraints** | PASS | `net10.0`; SkiaSharp-over-GL backend untouched (interactive uses the existing viewer; evidence is CPU/headless); no new dependency; in-tree `ProjectReference`s now with the package-feed swap deferred to publish (CanvasDemo convention, FR-009); `FS.GG.UI.*` identity untouched. The sample is a *consumer*, not a new control/theme layer. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. Classifying the sample as Tier 2 (no surface, no baseline) is consistent with the `samples/CanvasDemo` precedent, which ships as an `IsPackable=false` executable with no `.fsi` and no surface baseline.

## Project Structure

### Documentation (this feature)

```text
specs/193-symbology-live-board/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — decisions: motion mapping, boundary policy, evidence shape, test-reachability
├── data-model.md        # Phase 1 — World, Model, Msg, BoardUnit, roster/evidence entities
├── quickstart.md        # Phase 1 — build + run (evidence/interactive) + capture-evidence recipe + per-SC validation
├── contracts/           # Phase 1 — CLI subcommand contract + sample deterministic-core module sketch
│   ├── cli-contract.md
│   └── board-core.md
├── checklists/          # (pre-existing)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
samples/
├── CanvasDemo/                   # EXISTING — the precedent this sample mirrors exactly
│   ├── Game.fs                   #   pure fixed-timestep World + renderScene + evidence fingerprint
│   └── Program.fs                #   subcommand dispatch (evidence default / interactive) + graceful fallback
│
└── SymbologyBoard/               # NEW — the M6 live board sample (OutputType=Exe; IsPackable=false)
    ├── SymbologyBoard.fsproj     #   ProjectReferences: Scene, Symbology, Canvas, Controls, Controls.Elmish,
    │                             #     SkiaViewer, Themes.Default (package-feed swap deferred — FR-009)
    ├── Roster.fs                 #   APPROVED M5 mapping reused in-tree: UnitStats + factionOf/klassOf/
    │                             #     sigilOf/mapUnit (from readiness/dry-run/FinalSymbolSet.fsx) + roster literal
    ├── Board.fs                  #   deterministic World (per-unit phase + drift), init/update/Tick,
    │                             #     motionOf, renderScene (animate + bounce + interpolate), evidence fingerprint
    └── Program.fs                #   [<EntryPoint>] subcommand dispatch + host + scripted sequence + fallback

tests/
└── SymbologyBoard.Tests/         # NEW — semantic tests over the sample's deterministic core
                                  #   reproducibility (SC-001), seed divergence (SC-002), on-board invariant
                                  #   (FR-011/SC-003), degenerate-roster non-empty board (edge case)

specs/193-symbology-live-board/readiness/
└── board-evidence.md             # NEW — captured seeded evidence (fingerprint + two-runs-matched record) (FR-013/SC-006)

FS.GG.Rendering.slnx              # register samples/SymbologyBoard + tests/SymbologyBoard.Tests
```

**Structure Decision**: One new executable sample under `samples/` plus one matching `tests/` project, both registered in `FS.GG.Rendering.slnx` (the sample under the existing `/samples/` solution folder alongside `CanvasDemo`). This mirrors the accepted `samples/CanvasDemo` two-file core (`Game.fs` deterministic sim + `Program.fs` dispatch) and adds a third `Roster.fs` only to carry the approved M5 mapping in-tree. No new library, no new public surface, no skill changes; existing `Symbology`/`Canvas`/`Scene`/`SkiaViewer`/`Controls` surfaces are consumed unchanged (FR-012, SC-005). In-tree `ProjectReference`s match how `CanvasDemo` is wired today; the package-feed swap is deferred to the publish step (FR-009).

## Complexity Tracking

> No constitution violations. Section intentionally empty.

## Implementation Status (2026-06-25) — COMPLETE

All 23 tasks (T001–T023) are done and marked `[X]` in [tasks.md](./tasks.md). M6 ships.

**Delivered**

- `samples/SymbologyBoard/` (`OutputType=Exe`, `IsPackable=false`) — `Roster.fs` (approved M5 mapping +
  `motionOf` + an 8-unit fixed roster), `Board.fs` (deterministic fixed-timestep `World`/`Model`/`Msg`,
  seeded positions/velocities, bounce + on-board clamp, `Loop.alpha` interpolation, `renderScene`,
  `evidence` fingerprint), `Program.fs` (full subcommand dispatch + interactive host + headless fallback).
  Registered in `FS.GG.Rendering.slnx` under `/samples/`.
- `tests/SymbologyBoard.Tests/` — 5 Expecto tests (reproducibility, seed-divergence, on-board invariant,
  non-empty board, zero-area placeholder), green via an Exe `ProjectReference` (no FSI fallback needed).

**Verified evidence** (`specs/193-symbology-live-board/readiness/`, committed via `.gitignore` allowlist)

- **Build**: `dotnet build FS.GG.Rendering.slnx` clean — 0 warnings, 0 errors (FR-008/SC-005).
- **Reproducibility (SC-001)**: `evidence` prints `sha256:4786…6a95` and `reproducible (two runs
  byte-identical).`, exit 0, stable across processes (smoke.md, board-evidence.md).
- **Seed-sensitivity (SC-002)**: seed 2 → `sha256:3b43…1be4` (≠ seed 1).
- **Interactive (SC-003/SC-004)**: launched `Ok` on this live-window host (`status=ok`, exit 0); headless
  skip notice is by construction via `Viewer.runtimeCapability()`. Visual smoothness disclosed as
  environment-limited; on-board guarantee proven by the 600-step invariant test (us1-interactive.md).
- **Subcommands (SC-007/FR-010)**: no-arg → evidence; `frobnicate` → usage hint + exit 1 (cli-contract.md).
- **Zero surface drift / no regression (SC-005/T019/T022)**: `Package.Tests` unchanged at exactly 8
  pre-existing failures (no `.fsi`/baseline touched); `ControlsGallery.Tests` unchanged at 2; new
  `SymbologyBoard.Tests` 5 🟢; all other projects 🟢 (no-regression.md).

**Out of scope (unchanged)**: M7 (legibility linter, Badge/Ring grammars, label text) and the package-feed
reference swap (ProjectReferences retained per the CanvasDemo convention, FR-009) remain deferred.
