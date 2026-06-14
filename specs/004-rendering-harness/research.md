# Phase 0 Research: Rendering Test Harness (Stage R5)

Seams and environment inspected against the R4 import. No `NEEDS CLARIFICATION` — the seams are
present and the dev-env capability is measured.

## Decision 1 — Orchestrate the viewer + shell to installed X11 tools (no native interop)

**Decision**: The harness drives the product viewer via its seams and **shells out** to the
installed X11 toolchain for the live/probe parts: `xdpyinfo`/`xrandr`/`xinput` (probe),
`xdotool` (window discovery + XTEST mouse/keyboard injection), `maim`/`xwd` (screenshots),
`ydotool` (opt-in uinput).

**Rationale**: These tools are installed (dev baseline) and cover XTEST injection, window search,
and capture without P/Invoking `libX11`/`libXtst` — far simpler (Principle III) and avoids new
native dependencies. SkiaSharp (already pinned) handles PNG non-blank/diff checks.

**Alternatives considered**: P/Invoke `libXtst`/`libX11` (rejected — native-interop burden, no
benefit over `xdotool`); a managed X11 NuGet (rejected — new dependency, less battle-tested than
the CLI tools).

## Decision 2 — Pure `RunPlan` → edge interpreter (Principle IV)

**Decision**: Each invocation computes a pure `RunPlan` (tier, the assertions it will make, the
proof level it may claim, and the degradation rule for missing capability) and then an interpreter
executes the I/O (launch viewer, shell, capture) and folds results into evidence.

**Rationale**: Makes proof-level and degradation decisions unit-testable with **no desktop** —
the riskiest logic (overclaim prevention, clean degradation) is pure. Satisfies the constitution's
MVU/effect-separation rule for CLIs.

## Decision 3 — Evidence schema with explicit proof scope

**Decision**: Every run emits `run.json` (`proofLevel`, `authoritativeFor`, `notAuthoritativeFor`,
display/renderer/present facts, timing percentiles) + `metrics.csv` (per-frame) + `summary.md`
(human). Built on `Testing.parseScreenshotEvidenceRecord` / `validateScreenshotEvidence`.

**Rationale**: The plan's defining rule — artifacts must state what they prove and what they do
not. `notAuthoritativeFor` is mandatory so a T1 offscreen PNG can never be read as desktop-visible.

## Decision 4 — Plain `argv` CLI

**Decision**: `Cli.fs` with `[<EntryPoint>]` pattern-matches `argv` into the five subcommands
(`probe|offscreen|live-x11|perf|input`) and flags. No `System.CommandLine`.

**Rationale**: Five subcommands with a handful of flags; a match expression is plainer (Principle
III) and adds no dependency. Mirrors the source test projects' `Program.fs` entrypoint idiom.

## Decision 5 — Re-home the R4-skipped perf tests under T3

**Decision**: The 17 `Feature109` perf-corpus/baseline tests skipped in R4 (`SKIPPED-TESTS.md`)
are re-expressed as harness **perf modes** (T3) with committed goldens managed by the harness,
not as ad-hoc Elmish unit tests.

**Rationale**: They are performance evidence — exactly the tier R3/R5 designate for the harness.
Centralizing perf goldens in the harness also fixes the R4 repo-pollution (tests writing goldens
to repo-relative paths): the harness writes to a gitignored `artifacts/` run dir.

## Decision 6 — Degradation matrix (safe failure, Principle VI)

| Missing capability | Tier behavior |
|---|---|
| No `DISPLAY` (headless) | T0/T1 run; T2/T3 → `skipped: no live desktop` with probe facts (not a crash) |
| Effective backend = Wayland | viewer process has `WAYLAND_DISPLAY` unset; if still Wayland → run **classified/failed** as Wayland |
| Missing vblank/swap facts | T3 **refuses** the `vsync-faithful` proof level |
| `/dev/uinput` absent | `uinput` input backend → `opt-in unavailable (needs host device pass-through)`, harness continues |
| Window not discoverable | T2 fails with probe facts, no hang/timeout-forever |

## Capability baseline (measured 2026-06-14; re-probed per run)

`DISPLAY=:1` live; `XTEST/Present/RANDR/DRI3/XInput` available; real output `HDMI-A-1`
1920x1080 @ 119.93 Hz (T3 vsync feasible); hardware GL via AMD/Mesa (GL 4.6, direct);
`xrandr/xdpyinfo/xinput/xdotool/maim/xwd/ImageMagick/ffmpeg/perf` installed; `/dev/uinput` +
`/dev/input` **absent** (T-uinput opt-in); `WAYLAND_DISPLAY` set (harness unsets it for the viewer).

## Out of scope (deferred)

- Wiring tiers into CI at chosen frequencies → Stage R6.
- Host device pass-through for `/dev/uinput` → environment/ops, not this feature.
