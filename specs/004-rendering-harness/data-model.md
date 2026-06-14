# Phase 1 Data Model: Rendering Test Harness (Stage R5)

The harness's domain types. Pure types (`RunPlan`, `Evidence`, `Tier`, `ProbeFacts`) live behind
`.fsi`; the interpreter (`X11`, `Tiers`) consumes them. Field formats: [`contracts/`](./contracts/).

## Entity: Tier

`T0 | T1 | T2 | T3 | TUinput` — each maps to a display dependency, a driving seam, and an
authoritative/not-authoritative scope (see `contracts/tier-matrix.md`). Note: the tier written
`T-uinput` in prose/CLI is the F# DU case `TUinput` (a case label cannot contain `-`).

## Entity: ProbeFacts (pure record from the environment)

| Field | Description |
|---|---|
| EffectiveBackend | `X11 | Wayland | None` (after `WAYLAND_DISPLAY` unset for the viewer) |
| Display | `:N` or none |
| Gl | renderer / version / direct-rendering |
| RefreshHz | real output refresh (e.g. 119.93) or none |
| Extensions | XTEST / Present / RANDR / DRI3 / XInput presence |
| Present | swap-control + vblank source (or none) |
| UinputAvailable | `/dev/uinput` + `/dev/input` present? |

## Entity: RunPlan (PURE — the testable core)

| Field | Rule |
|---|---|
| Tier | requested tier |
| Subcommand | probe/offscreen/live-x11/perf/input |
| RequiredCapability | what the tier needs (e.g. live X11, vblank facts, uinput) |
| Assertions | the checks the tier will make |
| ClaimableProof | the `proofLevel` + `authoritativeFor` this plan MAY claim **given** ProbeFacts |
| NotAuthoritativeFor | always populated for non-probe tiers |
| Degradation | `Run | Skip of reason | FailClassified of reason` computed from ProbeFacts |

**Validation** (FR-007/008/012/014, SC-003/004/005/007):
- `vsync-faithful` ∈ ClaimableProof **iff** Present facts complete.
- RequiredCapability absent → `Degradation = Skip`/`FailClassified` (never crash, never false pass).
- NotAuthoritativeFor non-empty for every non-probe tier.

## Entity: Evidence (run.json + metrics.csv + summary.md)

Per `contracts/run-json.schema.md`. Built/validated via `Testing.parseScreenshotEvidenceRecord`
/ `validateScreenshotEvidence`. `status ∈ passed|failed|skipped`; `skipReason` when skipped.

## Entity: PerfMode

`throughput | paced-60 | paced-native | stress-resize | input-latency`; each declares evidence
kind (`deterministic | live-host | timing`).

## Entity: InputScript + Backend

A declarative sequence (pointer move / click / key) with backend `Pure | X11XTest | Uinput`.
`Pure` → `Perf.runScript`/`captureRespondsProof` (deterministic); `X11XTest` → `xdotool` (default
live); `Uinput` → `ydotool` (opt-in).

## Cross-cutting invariants

- **No overclaim** (SC-004): no Evidence omits `proofLevel`/`notAuthoritativeFor`.
- **Capability not gate** (FR-012): no tier is required for a routine change; T0/T1 default, rest opt-in.
- **No governance** (FR-013): the harness references only product + Testing projects + installed tools.
- **Pure/edge split** (Principle IV): `RunPlan` decisions are unit-tested without a desktop.
