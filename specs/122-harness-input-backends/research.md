# Phase 0 — Research: Harness Input Backends (pure + CLI) (Feature 122)

Net-new, contract-first work — "research" here records the design decisions for code that does **not** yet
exist, grounded in the harness modules it composes (`Domain`/`Probe`/`RunPlan`/`Evidence`/`Live`/`X11`/`Perf`/
`Cli`) and the pure product seam (`ControlsElmish.captureRespondsProof`). No open `NEEDS CLARIFICATION`.

## Decision 1 — One backend-agnostic input script, three interpreters

- **Decision**: Define `InputStep = Click of x*y | Key of string | Wait of ms` and `InputScript = { Name; Steps }`.
  A single `run` interprets a script under a selected `InputBackend` (`Pure | X11XTest | Uinput`); only the
  interpreter differs, the script does not.
- **Rationale**: The same declarative scenario must be provable pure (gate) and live (x11-xtest/uinput) so the
  pure replay's correspondence to a real desktop path is meaningful. A backend-agnostic script is the minimal
  shape that makes "same scenario, different fidelity" true (FR-001).
- **Alternatives considered**: A per-backend script type — rejected: duplicates scenarios and breaks the
  pure↔live correspondence; `Wait` as wall-clock sleep — rejected: non-deterministic (see Decision 3).

## Decision 2 — A new `InputBackend` type, distinct from the display `Backend`

- **Decision**: Add `InputBackend = Pure | X11XTest | Uinput` to `Domain.fs`, separate from the existing
  `Backend = X11 | Wayland | NoDisplay` (the display server).
- **Rationale**: Display detection and input interpretation are orthogonal axes; conflating them into one
  `Backend` would be wrong (a `pure` run on an X11 display is valid). Keeping them separate keeps each honest.
- **Alternatives considered**: Overloading `Backend` — rejected (semantic clash).

## Decision 3 — The pure backend replays against the MVU model deterministically

- **Decision**: The `pure` interpreter replays the script's clicks/keys against the MVU model via the
  production routing path (`ControlsElmish.captureRespondsProof` for the input→visible-change verdict, and/or
  `Perf.runScript`), with `Wait` as an **injected** duration (no wall-clock). It emits `Evidence` with
  `ProofLevel = Deterministic` and an authoritative claim like `input-msg-dispatch`.
- **Rationale**: `captureRespondsProof` already renders BEFORE/AFTER frames and yields an `Inert` verdict when
  nothing responds — exactly the input→repaint proof, reusing the production path so the pure proof is
  faithful, and headless/deterministic so it runs in the gate (FR-002/FR-008, SC-001/SC-002).
- **Alternatives considered**: A bespoke pure dispatch loop — rejected: would diverge from the production
  routing path and weaken the proof; reading a clock for `Wait` — rejected (non-deterministic).

## Decision 4 — The run/skip/fail decision stays in the pure planner; the executor only interprets

- **Decision**: Map each input backend to a planner `Tier` (`pure` → a `Deterministic`-proof tier;
  `x11-xtest` → `T2`/`LiveHost`; `uinput` → `TUinput`/`KernelInput`) and call the existing
  `RunPlan.plan tier facts` to decide `Run | Skip reason | FailClassified reason` and the claim lists. `Input.run`
  only *interprets* a `Run` decision; a `Skip`/`FailClassified` short-circuits to disclosed evidence.
- **Rationale**: The no-overclaim + safe-failure logic is already pure and unit-tested in `RunPlan`; reusing it
  keeps the decision in side-effect-free code and guarantees a non-empty `NotAuthoritativeFor` for every run
  (FR-004/FR-005, SC-005). The executor stays a thin, untested-logic-free interpreter.
- **Alternatives considered**: Re-deciding run/skip inside `Input.run` — rejected: duplicates the planner and
  risks divergence/overclaim.

## Decision 5 — Degrade-and-disclose, bounded, never hang or fake a pass

- **Decision**: `uinput` honest-skips when `/dev/uinput` (or the `ydotoold` socket) is absent — `Skip` with a
  reason, exit 0, **bounded** (detect absence up front; never block). `x11-xtest` honest-skips with no display
  and is `FailClassified` on a Wayland-only session (per the planner). A `pure` run never needs a display.
- **Rationale**: CI trust depends on a missing capability degrading rather than hanging or fabricating a pass
  (Principle VI). Detecting the device/socket/display before driving input keeps it bounded (FR-006/FR-007,
  SC-003/SC-004).
- **Alternatives considered**: Attempting the live drive and timing out — rejected: a timeout is a hang risk
  and a poor signal; up-front capability detection is cleaner.

## Decision 6 — Replace the CLI stub with a thin argument-parsing surface

- **Decision**: Replace the `input` stub in `Cli.fs` (~line 112) with `--backend pure|x11-xtest|uinput`,
  `--script <name>`, `--out <dir>`, `--json`; an unknown backend or unknown script name exits non-zero with a
  clear classified message. The command resolves the script from a small named catalog and calls `Input.run`.
- **Rationale**: The pure proof is only usable through a real CLI; keeping the CLI a thin parser over the
  tested `Input.run` keeps logic out of the I/O edge (FR-003).
- **Alternatives considered**: A config-file-driven script — rejected: heavier than needed; a named catalog of
  a few canonical scripts is enough for the harness's purpose.

## Decision 7 — Harness-only; no product surface

- **Decision**: All new code lives in `tests/Rendering.Harness` (+ its test project). No product package or
  `.fsi` changes; the surface-drift gate is untouched.
- **Rationale**: The input backends are a *harness capability*, not product behaviour; keeping them in the
  harness honours the scope and leaves the public baselines byte-unchanged (FR-009/SC-006).
- **Alternatives considered**: Exposing an input-script API from the product — rejected: out of scope and a
  needless public-surface expansion.

## Renderer-mode / evidence honesty

The `pure` proof is deterministic/headless (structural BEFORE/AFTER frames via `captureRespondsProof`, injected
waits). The live arms (`x11-xtest`/`uinput`) are env-gated and degrade-and-disclose; their evidence will carry
the appropriate `ProofLevel` (`LiveHost`/`KernelInput`) and a non-empty `NotAuthoritativeFor`, and honest-skip
when their capability is absent — consistent with the harness's existing tier discipline.
</content>
