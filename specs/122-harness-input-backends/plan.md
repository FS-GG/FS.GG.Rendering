# Implementation Plan: Harness Input Backends (pure + CLI) (Feature 122)

**Branch**: `122-harness-input-backends` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/122-harness-input-backends/spec.md`

## Summary

The rendering harness proves offscreen (T0/T1), live-X11 (T2), and perf (T3) tiers but has **no input layer**.
Feature 122 adds a single backend-agnostic **declarative input-script model** (click / key / injected-wait)
interpreted by a selectable backend: **`pure`** (deterministic MVU replay, gate-runnable — the MVP),
**`x11-xtest`** (live window via the existing X11 primitives, env-gated), and **`uinput`** (kernel
evdev/libinput, env-gated). Every run emits the harness's no-overclaim `Evidence` (non-empty
`NotAuthoritativeFor`), and the run/skip/fail decision is made by the existing pure `RunPlan.plan` from probed
facts — so unavailable capabilities **degrade and disclose**, never hang or fake a pass.

**This is net-new, contract-first work** — Workstream A (A1/A2/A5/A6 in-gate; A3/A4/A7 env-gated). It follows
the canonical `Spec → .fsi → semantic tests → implementation` order; `/speckit-plan` → `/speckit-tasks` →
`/speckit-implement` will **build** `tests/Rendering.Harness/Input.fsi`/`Input.fs`, the backends, the CLI
wiring, and the harness unit tests — not a conformance pass. It is **harness-only** (`tests/Rendering.Harness`),
so it adds no product API and leaves the public-surface-drift gate untouched.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto (harness unit tests). Existing harness modules it composes (does **not**
rebuild): `Domain` (`Tier` incl. `TUinput`, `ProofLevel`, `ProbeFacts`, `RunStatus`, `Degradation`),
`Probe.probe`, `RunPlan.plan` (the pure run/skip/fail planner, always-non-empty `NotAuthoritativeFor`),
`Evidence` (`Evidence` record + `toJson`/`write`), `Live` (Xvfb/EGL window discovery), `X11.clickAt`/`sendKey`,
`Perf.runScript`, `Cli`. The pure product seam the `pure` backend replays against:
`ControlsElmish.captureRespondsProof` (feature 090 — BEFORE/AFTER frames + `Inert` verdict) and
`Perf.runScript`. External tools for the live arms: `xdotool`/`maim` (x11-xtest, via `X11`); `ydotool`
(uinput). No new product/runtime dependency.

**Storage**: Evidence artifacts (`run.json` / `metrics.csv` / `summary.md`) written under `--out <dir>` by the
existing `Evidence.write`. Nothing else persisted.

**Testing**: Default-tier "Local inner loop" in `tests/Rendering.Harness.Tests` (Expecto): the `pure` backend
runs **in the gate**; unit tests cover (a) the planner decision per input backend, (b) `NotAuthoritativeFor`
never empty, (c) the clean-skip exit code/status for `uinput`/`x11-xtest` when their capability is absent, and
(d) a **deterministic `pure`-replay golden** (byte-identical evidence over two runs). The `x11-xtest` and
`uinput` live proofs are exercised only on a capable runner; headless they honest-skip.

**Target Platform**: Linux/dev. `pure` is fully headless (no display, no GL); `x11-xtest` needs a display/GL
host (Xvfb + EGL); `uinput` needs `/dev/uinput` (+ a running `ydotoold` socket).

**Project Type**: F# UI framework — **harness tooling** (a test/tooling project), not product code. The
harness's evidence files + unit tests are the observable surface (vertical-slice rule for tooling).

**Performance Goals**: No wall-clock target. Goals are correctness/determinism/safe-failure: the `pure` run is
byte-reproducible (injected waits, no wall-clock — SC-002); every run discloses a non-empty
`NotAuthoritativeFor` (SC-005); unavailable capabilities skip promptly within a bounded time (SC-003, no hang).

**Constraints**:
- **No product API** and **zero public-surface-baseline change** (harness-only — FR-009/SC-006).
- The `pure` backend MUST be **deterministic** — injected `Wait`, no wall-clock, no randomness (FR-008,
  Principle VI).
- The run/skip/fail decision MUST stay in the pure `RunPlan.plan`; the executor only **interprets** (FR-005).
- Every unavailable capability MUST **degrade and disclose** — a classified `Skip`/`FailClassified` with a
  reason, exit-0-on-skip, **bounded** (no hang; detect a missing `ydotoold` socket / no display rather than
  blocking) (FR-006/FR-007).
- Every run's evidence MUST carry a non-empty `NotAuthoritativeFor` (FR-004).

**Scale/Scope**: One new harness module (`Input.fsi`/`Input.fs`) + the `Cli` `input` subcommand wiring + unit
tests. **In-gate now**: the input-script model, the `pure` backend, the CLI, the tests (A1/A2/A5/A6).
**Env-gated** (specified, honest-skip headless): `x11-xtest` (A3), `uinput` + the T-uinput executor (A4/A7).
Capable-runner provisioning is Workstream B (out of scope). **Naming note**: the input backend is a **new**
type `InputBackend` (`Pure | X11XTest | Uinput`) — distinct from the existing display `Backend`
(`X11 | Wayland | NoDisplay`) in `Domain.fs`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — new observable harness behaviour (a real `input`
subcommand + the input-script proof). But **harness-only**: no product `.fsi`, no public package surface, so
the public-surface-drift gate is unaffected (FR-009). The harness's own `Input.fsi` is the contract; its unit
tests + evidence files are the user-reachable surface (vertical-slice rule).

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ **Pass (clean)** | Unlike the C backfills, this is genuinely contract-first: this spec is step one; `/speckit-tasks`/`implement` will author `Input.fsi` **before** `Input.fs`, then the semantic tests, then the implementation. No import-before-spec deviation. |
| II. Visibility lives in `.fsi` | ✅ Pass | `Input.fsi` will declare the input-script model + the single `run` entry; consistent with the harness's existing `.fsi`-per-module convention (`Evidence.fsi`/`RunPlan.fsi`/…). |
| III. Idiomatic simplicity | ✅ Pass | A small step union + a record + a backend interpreter; reuses `RunPlan`/`Evidence`/`X11`/`captureRespondsProof`. No SRTP/reflection/type-providers/custom operators. |
| IV. Elmish/MVU boundary | ✅ Pass | The `pure` backend replays the input script against the MVU model through `captureRespondsProof` / `Perf.runScript` (the production routing path) — input as data, no wall-clock in the transition; the live arms interpret at the edge. |
| V. Test evidence mandatory | ✅ Pass | Harness unit tests cover the planner per backend, non-empty `NotAuthoritativeFor`, clean-skip status/exit, and a deterministic `pure` golden; the `pure` arm runs in the gate. The live arms degrade-and-disclose (honest skip), never a fake pass. |
| VI. Observability & safe failure | ✅ Pass | The whole feature *is* safe-failure: `RunPlan.plan` classifies run/skip/fail; missing capability ⇒ disclosed `Skip`/`FailClassified`, exit-0-on-skip, bounded (no hang). The `pure` path consults no wall-clock (deterministic). |

**Gate result**: PASS (no deviations — clean contract-first). Re-checked post-Phase-1 design below: unchanged;
the design adds no product surface, no new runtime dependency, and keeps the run/skip/fail decision in the
already-tested pure planner.

## Project Structure

### Documentation (this feature)

```text
specs/122-harness-input-backends/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/
│   ├── input-fsi.md       # the Input.fsi module contract (script model + run)
│   └── input-cli.md       # the `harness input` CLI command schema + exit/status contract
└── checklists/requirements.md
```

### Source Code (repository root)

```text
tests/Rendering.Harness/
├── Domain.fs                  # ADD: InputBackend (Pure | X11XTest | Uinput); (maybe) a uinput probe fact if absent
├── Input.fsi / Input.fs       # NEW: InputStep / InputScript / InputBackend; the named script catalog; `run` interpreter
├── Probe.fs(i)                # (read) ProbeFacts — uinput availability fact for the planner
├── RunPlan.fs(i)              # (reuse) plan: Tier -> ProbeFacts -> RunPlan (per-backend run/skip/fail)
├── Evidence.fs(i)             # (reuse) Evidence + write (run.json/metrics.csv/summary.md)
├── X11.fs(i) / Live.fs(i)     # (reuse) clickAt/sendKey + window discovery (x11-xtest arm)
├── Perf.fs(i)                 # (reuse) runScript (pure replay)
└── Cli.fs                     # EDIT: replace the `input` stub (line ~112) with --backend/--script/--out/--json

tests/Rendering.Harness.Tests/
└── Tests.fs                   # ADD: planner-per-backend, non-empty NotAuthoritativeFor, clean-skip, deterministic pure golden

src/Controls.Elmish/ControlsElmish.fsi   # (reuse, no change) captureRespondsProof — the pure input→visible-change seam
```

**Structure Decision**: Single F# solution (`FS.GG.Rendering.slnx`). 122 adds one harness module + CLI wiring +
tests; **no new project, no product change**. The pure backend is the gate-runnable MVP; the live arms
degrade-and-disclose. Surface baselines under `tests/surface-baselines/` are **untouched** (harness is not part
of the public-package surface).

## Complexity Tracking

> No constitution violations to justify (clean contract-first). The notes below record deliberate scope/design
> calls, kept visible.

| Decision | Why | Note |
|---|---|---|
| New `InputBackend` type, separate from the existing display `Backend` | `Domain.Backend` already means the display server (`X11 / Wayland / NoDisplay`); the input backend (`Pure / X11XTest / Uinput`) is an orthogonal axis. | Reusing `Backend` would conflate display detection with input interpretation. |
| `x11-xtest` (A3) + `uinput` (A4/A7) specified but env-gated | They need a display/GL host and `/dev/uinput` respectively; the headless gate cannot prove them. | They honest-skip in the gate; proven on a capable runner (provisioning = Workstream B). The contract + planner decision are still authored + unit-tested now. |
| Each input backend maps to a planner `Tier` for the run/skip/fail decision | `RunPlan.plan` is keyed by `Tier`; reusing it keeps the decision in tested pure code (FR-005). | `pure` → `Deterministic` proof; `x11-xtest` → `T2`/`LiveHost`; `uinput` → `TUinput`/`KernelInput`. |
</content>
