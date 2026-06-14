# Implementation Plan: Build the Rendering Test Harness (Migration Stage R5)

**Branch**: `004-rendering-harness` *(git-extension before_specify hook)* | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-rendering-harness/spec.md`

## Summary

Build `tests/Rendering.Harness/` — a self-contained F# CLI that produces tiered, non-overclaiming
rendering evidence (T0 deterministic → T1 offscreen → T2 live X11 → T3 perf → T-uinput opt-in).
It orchestrates the R4-imported viewer/controls seams and shells to the installed X11 toolchain,
emitting a `run.json` / `metrics.csv` / `summary.md` evidence contract where **every artifact
declares what it proves and what it does not**. It is a **capability, not a gate** — T0/T1 are the
fast default inner loop; T2/T3/T-uinput are opt-in.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (consistent with the product).

**Primary Dependencies**: product projects `SkiaViewer`, `Controls.Elmish`, `Testing`, `Scene`,
`Controls` (ProjectReferences); `SkiaSharp` (already pinned) for PNG read/non-blank/diff checks.
**No new NuGet** for the live path — shell out to installed tools: `xdpyinfo`/`xrandr`/`xinput`
(probe), `xdotool` (window discovery + XTEST input), `maim`/`xwd` (desktop/window screenshot).
`ydotool` for the opt-in uinput backend (inert without `/dev/uinput`).

**Storage**: evidence artifacts under a run directory (e.g. `artifacts/harness/<run-id>/`) —
gitignored; `artifacts/` is already in `.gitignore`.

**Testing**: `tests/Rendering.Harness.Tests/` covers the harness's **pure** logic (evidence-schema
shape, run-planning, backend classification, degradation decisions) — these are deterministic and
join the default local tier. The tiers themselves are exercised via the CLI (quickstart V1–V5).

**Target Platform**: Linux desktop with X11 + hardware GL (dev baseline). Degrades on headless /
Wayland / no-uinput.

**Project Type**: CLI tool + companion unit-test project (infrastructure, not product API).

**Performance Goals**: T0/T1 complete in a few seconds (default inner loop). T3 measures, doesn't
gate.

**Constraints**: Constitution v1.0.0 — `.fsi` per public harness module; no `.fs` access modifiers
(FS0078-as-error inherited from `Directory.Build.props`); idiomatic-simple argv CLI (no heavy
arg framework); GL/`net10.0`; no governance dependency; **observability & safe failure are central**
(Principle VI) — clean degradation, never overclaim.

**Scale/Scope**: 5 CLI subcommands, 5 tiers, 5 perf modes, 3 input backends, 1 evidence schema,
1 capability-baseline doc.

### Imported seams the harness builds on (verified present)

| Seam | Signature (`.fsi`) | Used by |
|---|---|---|
| `SkiaViewer.run` | `ViewerOptions -> SceneNode -> Result<ViewerLaunchOutcome, ViewerRunFailure>` | T2 (live window) |
| `SkiaViewer.runBounded` | `ViewerRunRequest -> ViewerOptions -> SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>` | T1/T3 (bounded offscreen + metrics) |
| `SkiaViewer.captureScreenshotEvidence` | `ScreenshotEvidenceRequest -> ViewerOptions -> SceneNode -> ScreenshotEvidenceResult` | T1/T2 (readback) |
| `ControlsElmish.captureRespondsProof`, `Perf.runScript` | deterministic responds/perf corpus | T0 + `pure` input backend |
| `FrameMetrics.PaintDuration/ComposeDuration` | live per-phase present timings | T3 |
| `Testing.{parse,validate}ScreenshotEvidence*` | evidence record parse/validate | evidence schema |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This is real F# code — code-centric principles apply.

| Principle | Assessment |
|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | Harness public modules are designed FSI-first; tests in `Rendering.Harness.Tests` exercise the pure surface. **PASS** |
| II. Visibility in `.fsi` | Every public harness module carries a curated `.fsi`; no `.fs` access modifiers (build-enforced FS0078). **PASS** |
| III. Idiomatic Simplicity | Plain `argv` dispatch; shell to installed tools instead of P/Invoking libX11/XTEST; SkiaSharp for pixels. No clever frameworks. **PASS** |
| IV. Elmish/MVU boundary | Each run is **pure plan → edge interpreter**: a pure `RunPlan` (tier, assertions, expected proof) is computed, then an interpreter executes I/O (launch viewer, shell, capture) and folds results back into evidence. Satisfies the MVU/effect-separation rule for a CLI. **PASS** |
| V. Test Evidence Mandatory | The harness's own pure logic is unit-tested (schema, planning, degradation). The tiers produce real evidence; synthetic substitution is disclosed. **PASS** |
| VI. Observability & Safe Failure | **Central.** Clean degradation (no DISPLAY, Wayland, no `/dev/uinput`), fail-fast with probe facts, and the no-overclaim rule (proofLevel + authoritativeFor/notAuthoritativeFor). **PASS** |
| Engineering Constraints | `net10.0`, GL, SkiaSharp pinned, `.fsi`, no governance, separate from any governance path; new shell-tool usage documented, no heavy new NuGet. **PASS** |

**Change Classification**: **Infrastructure** — new harness project + its evidence contract; no
product public-API change. (`.fsi`/baseline rules still apply to the harness's own public modules.)

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/004-rendering-harness/
├── plan.md, spec.md, research.md, data-model.md, quickstart.md
├── contracts/
│   ├── cli.schema.md            # subcommands, args, exit codes
│   ├── run-json.schema.md       # evidence artifact (proofLevel/authoritativeFor/…)
│   └── tier-matrix.md           # T0–T-uinput: deps, authoritative-for, degradation
└── checklists/requirements.md
```

### Source Code (repository root)

```text
tests/Rendering.Harness/                 # the CLI (infrastructure, separate from governance)
├── Probe.fsi/.fs                 # env probe: display/GL/refresh/extensions + effective backend
├── Evidence.fsi/.fs              # run.json/metrics.csv/summary.md model + writers; proof levels
├── RunPlan.fsi/.fs               # PURE: tier → assertions + expected proof + degradation rule
├── X11.fsi/.fs                   # edge interpreter: shell to xdotool/maim/xrandr (Wayland unset)
├── Tiers.fsi/.fs                 # T0/T1/T2/T3/T-uinput executors over the seams
├── Perf.fsi/.fs                  # perf modes (throughput/paced-60/paced-native/stress-resize/input-latency)
├── Input.fsi/.fs                 # input scripts: pure / x11-xtest / uinput backends
├── Cli.fs                        # [<EntryPoint>] argv dispatch: probe|offscreen|live-x11|perf|input
└── Rendering.Harness.fsproj
tests/Rendering.Harness.Tests/           # unit tests for the PURE logic (joins local tier)
└── …
docs/harness/capability-baseline.md      # FR-015 recorded dev-env baseline
```

**Structure Decision**: A dedicated `tests/Rendering.Harness/` CLI plus a `Rendering.Harness.Tests`
unit project. Pure planning (`RunPlan`) is separated from the I/O interpreter (`X11`, `Tiers`) per
Principle IV, so degradation/proof-level decisions are unit-testable without a desktop. Live/input
work shells to the installed X11 toolchain (idiomatic simplicity, no native-interop burden). Both
projects join `FS.GG.Rendering.slnx`; only the pure unit tests run in the default local tier.

## Complexity Tracking

No constitution violations — section intentionally empty.
