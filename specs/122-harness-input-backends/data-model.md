# Phase 1 — Data Model: Harness Input Backends (pure + CLI) (Feature 122)

The 122-in-scope entities (all in `tests/Rendering.Harness`; harness-only, no product surface). New types in
**bold**; the rest are existing harness/domain types this feature composes.

## InputStep (new) — `Domain.fs` or `Input.fsi`

A single declarative action; backend-agnostic.

| Case | Payload | Meaning |
|---|---|---|
| `Click` | `x: int * y: int` | a pointer click at a coordinate |
| `Key` | `string` | a key press (e.g. `"space"`, `"Right"`, `"a"`) |
| `Wait` | `ms: int` | an **injected** delay (deterministic; never wall-clock) |

## InputScript (new) — `Input.fsi`

`{ Name: string; Steps: InputStep list }` — a named, ordered, backend-agnostic scenario. A small **named
catalog** of canonical scripts is resolved by `--script <name>`.

## InputBackend (new) — `Domain.fs`

`Pure | X11XTest | Uinput` — the selectable interpreter. **Distinct** from the existing display `Backend`
(`X11 | Wayland | NoDisplay`). Maps to a planner `Tier` + `ProofLevel`:

| InputBackend | Tier (for `RunPlan.plan`) | ProofLevel | Authoritative claim (example) | Capability gate |
|---|---|---|---|---|
| `Pure` | a `Deterministic`-proof tier | `Deterministic` | `input-msg-dispatch` | none (always runs) |
| `X11XTest` | `T2` | `LiveHost` | `real-input`, `input-to-repaint` | display/GL (Xvfb+EGL); Wayland ⇒ FailClassified |
| `Uinput` | `TUinput` | `KernelInput` | `evdev-libinput-input-path` | `/dev/uinput` (+ `ydotoold` socket) |

## Input.run (new) — `Input.fsi` (the single entry point)

`run : InputBackend -> InputScript -> facts: ProbeFacts -> selfDll: string -> outDir: string -> Evidence.Evidence`.
Computes the run plan via `RunPlan.plan`, then: `Run` ⇒ interpret the script under the backend and emit
`Passed`/`Failed` evidence; `Skip reason` ⇒ emit `Skipped` evidence (exit 0, disclosed reason); `FailClassified
reason` ⇒ emit classified-fail evidence. Pure planning; interpretation confined to the executor edge.

## Evidence (existing — reused) — `Evidence.fsi`

`Evidence.Evidence` (`RunId`, `Tier`, `Subcommand`, `Status: RunStatus`, `SkipReason: string option`,
`ProofLevel`, `AuthoritativeFor`, **non-empty** `NotAuthoritativeFor`, `Facts`, frames, percentiles,
`Artifacts`). `Evidence.write dir evidence frameMs : string` persists `run.json`/`metrics.csv`/`summary.md`.
122 adds no field; it populates this for input runs.

## RunPlan (existing — reused) — `RunPlan.fsi`

`plan : Tier -> ProbeFacts -> RunPlan` → `{ Tier; ClaimableProof; AuthoritativeFor; NotAuthoritativeFor (non-empty);
Degradation = Run | Skip reason | FailClassified reason; VsyncFaithfulAllowed }`. The pure run/skip/fail
authority for every backend (FR-005).

## ProbeFacts (existing — reused; may gain a uinput fact) — `Domain.fs` / `Probe.fs`

The probed environment (effective display `Backend`, display/GL/refresh/extension facts). The `uinput` arm
reads a `/dev/uinput`-availability fact from probe; if not already present, adding that fact is part of the
build (a `None`-degrading probe, never a crash).

## Reused product seam (no change) — `ControlsElmish.fsi`

`captureRespondsProof host state size model input : RespondsProof` (feature 090) — renders BEFORE/AFTER frames
through the production routing path and yields an `Inert` verdict when nothing responds. The `pure` backend's
input→repaint proof. (`Perf.runScript` for scripted multi-step replay.)

## Relationships

```text
--script <name> ─▶ named catalog ─▶ InputScript { Name; Steps: [Click|Key|Wait] }
--backend <b>  ─▶ InputBackend (Pure | X11XTest | Uinput) ─▶ Tier
                                                              │
                            RunPlan.plan(Tier, ProbeFacts) ──┼─ Run             ─▶ interpret script under backend:
                                                              │                     Pure  : captureRespondsProof / Perf.runScript (deterministic, injected Wait)
                                                              │                     X11XTest: X11.clickAt/sendKey on a Live window; before/after repaint
                                                              │                     Uinput : ydotool via /dev/uinput
                                                              ├─ Skip reason     ─▶ Skipped evidence (exit 0, disclosed)
                                                              └─ FailClassified  ─▶ classified-fail evidence
                                                              ▼
                            Evidence { Status; ProofLevel; AuthoritativeFor; NotAuthoritativeFor (non-empty); ... } ─▶ Evidence.write ─▶ run.json
```
</content>
