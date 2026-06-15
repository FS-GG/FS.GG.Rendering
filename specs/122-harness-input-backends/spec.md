# Feature Specification: Harness Input Backends (pure + CLI) (Feature 122)

**Feature Branch**: `122-harness-input-backends`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is **net-new, contract-first** work — **Workstream A** (tasks A1/A2/A5/A6, with A3/A4/A7 specified but
env-gated) in the 2026-06-15 missing-features plan. Unlike the Workstream-C items (091–121), the code does
**not** exist yet: `tests/Rendering.Harness/Input.fsi`/`Input.fs` are absent and the CLI `input` subcommand is
a stub that prints "input backends pending…" and exits 2. This is the first item after Workstream C in the
plan's near-term sequence, and it completes the unfinished harness tasks the rendering-harness spec (feature
004) already named (T010 pure, T014 x11-xtest, T019/T020 uinput, T022 integration). It follows the canonical
`Spec → .fsi → semantic tests → implementation` order; this document is step one.

The rendering harness can already prove offscreen (T0/T1), live-X11 (T2), and perf (T3) tiers, but it has **no
input layer**: nothing drives a declarative *input → MVU → repaint* sequence and records honest evidence of
it. This feature adds a single **declarative input-script model** interpreted by a **selectable backend**:

- **`pure`** — replays the script against the MVU model deterministically (no live desktop), so it runs in the
  default CI gate. The in-scope MVP.
- **`x11-xtest`** — drives a live viewer window via the existing X11 primitives; proves real input → repaint on
  a display/GL host; degrades cleanly (honest skip) when there is no display.
- **`uinput`** — drives the kernel evdev/libinput path; requires `/dev/uinput`; honest-skips when absent.

The script is **backend-agnostic** — only the interpreter differs. Every run emits no-overclaim evidence (a
non-empty "not authoritative for" list), and every unavailable capability **degrades and discloses** (a
classified skip/fail, never a hang or a fake pass), reusing the harness's existing pure planner.

This is **harness-only** tooling (`tests/Rendering.Harness`); it adds **no product API** and therefore leaves
the public-surface-drift gate unaffected. "Users" are harness operators and CI; the harness's own evidence
files and unit tests are the observable surface.

**Scope boundary.** In scope to **run now** (gate): the input-script model, the `pure` backend, the CLI
wiring, and harness unit tests (A1/A2/A5/A6). Specified but **env-gated** (degrade-and-disclose; proven only
on a capable runner): `x11-xtest` (A3, needs display/GL) and `uinput` (A4/A7, needs `/dev/uinput`) — these
honest-skip in the headless gate. The kernel/vsync capable-runner provisioning itself is Workstream B (out of
scope here).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A pure input script proves input → MVU → repaint headlessly (Priority: P1)

A harness operator (or CI) runs `harness input --backend pure --script <name>` with **no** display and gets a
deterministic proof that a declarative sequence of clicks/keys/waits drives the MVU model and produces the
expected repaint — emitting a `run.json` whose evidence records what it proved **and**, honestly, what it does
**not** prove.

**Why this priority**: A gate-runnable input proof is the MVP and the whole point of the feature — it closes
the harness's missing input layer without any environment dependency.

**Independent Test**: Run `harness input --backend pure --script <name>` headless; confirm exit 0, a `run.json`
with `status` = passed, a **non-empty** `NotAuthoritativeFor` list, and that replaying the same script twice
produces byte-identical evidence (deterministic — injected waits, no wall-clock).

**Acceptance Scenarios**:

1. **Given** no display, **When** `harness input --backend pure --script <name>` runs, **Then** it exits 0 and
   writes a `run.json` with `status` = passed and a non-empty `NotAuthoritativeFor` list.
2. **Given** the same script, **When** it is replayed twice, **Then** the two evidence outputs are
   byte-identical (deterministic; injected `Wait`, no wall-clock).

---

### User Story 2 - The input subcommand is wired with backend + script selection (Priority: P1)

The stubbed `input` subcommand is replaced by a real command that selects a backend
(`--backend pure|x11-xtest|uinput`) and a named script (`--script <name>`), writes evidence to `--out <dir>`,
and supports `--json`. An unknown backend or missing script fails with a clear, classified message — never a
silent or confusing exit.

**Why this priority**: Co-critical with US1 — the pure proof is only usable through a real CLI surface that
selects backend and script and disposes of bad input cleanly.

**Independent Test**: `harness input --backend pure --script <name> --out <dir> --json` runs and writes
evidence under `<dir>`; an unknown `--backend` or unknown `--script` exits non-zero with a clear message; the
old "input backends pending…" stub no longer appears.

**Acceptance Scenarios**:

1. **Given** `--backend pure --script <name> --out <dir>`, **When** run, **Then** evidence is written under
   `<dir>` and (with `--json`) machine-readable output is emitted.
2. **Given** an unknown backend or an unknown script name, **When** run, **Then** the command exits non-zero
   with a clear, classified message (not the removed stub).

---

### User Story 3 - The uinput backend honest-skips when the kernel device is absent (Priority: P2)

`harness input --backend uinput` on a machine **without** `/dev/uinput` does not hang and does not fake a
pass: it exits 0 with a `status` of skipped and a disclosed reason, decided by the harness's existing pure
planner from the probed facts.

**Why this priority**: P2 — the no-overclaim / safe-failure guard for the kernel tier. A tier that hung or
fabricated a pass when its device is missing would corrupt CI trust.

**Independent Test**: With `/dev/uinput` absent, run `harness input --backend uinput`; confirm exit 0,
`status` = skipped, a non-empty disclosed `SkipReason`, and that the run terminates promptly (no hang).

**Acceptance Scenarios**:

1. **Given** `/dev/uinput` is absent, **When** `harness input --backend uinput` runs, **Then** it exits 0 with
   `status` = skipped and a disclosed reason — promptly, with no hang and no fabricated pass.

---

### User Story 4 - The x11-xtest backend proves input → repaint on a display, and skips cleanly otherwise (Priority: P2)

On a display/GL host, `harness input --backend x11-xtest` drives a live viewer window with real pointer/key
input and proves a visible repaint resulted (a before/after change). Headless (no display), it **skips
cleanly** with a disclosed reason rather than failing.

**Why this priority**: P2 — the live-host proof that the pure backend's deterministic replay corresponds to a
real desktop input path; it is env-gated (needs a display/GL runner) so it must degrade, not fail, in the gate.

**Independent Test**: On a display/GL host, run `harness input --backend x11-xtest --script <name>`; confirm a
before/after repaint change is recorded with `status` = passed and a non-empty `NotAuthoritativeFor`. Headless,
confirm a clean `status` = skipped with a disclosed reason (not a failure).

**Acceptance Scenarios**:

1. **Given** a display/GL host, **When** `harness input --backend x11-xtest --script <name>` runs, **Then** it
   records a real input → repaint change with `status` = passed.
2. **Given** no display, **When** the same command runs, **Then** it `status` = skipped with a disclosed reason
   (degrade-and-disclose, not a failure).

---

### User Story 5 - Every input run discloses what it does not prove (Priority: P2)

Every backend run — pure, x11-xtest, or uinput — populates a **non-empty** `NotAuthoritativeFor` list and is
classified by the harness's existing pure planner as run / skip / fail from the probed facts. No run overclaims
its proof scope.

**Why this priority**: P2 — the cross-cutting no-overclaim contract that makes all the above evidence
trustworthy. It reuses the already-tested planner so the run/skip/fail decision stays in pure, tested code.

**Independent Test**: For each backend, inspect the emitted evidence and confirm `NotAuthoritativeFor` is
non-empty and the run/skip/fail classification matches the planner's decision for the probed facts.

**Acceptance Scenarios**:

1. **Given** any backend run, **When** the evidence is inspected, **Then** `NotAuthoritativeFor` is non-empty
   and the run/skip/fail status matches the planner's decision for the current probed facts.

---

### Edge Cases

- **No display + `pure`**: runs fully (deterministic; the pure backend never needs a display).
- **No display + `x11-xtest`**: clean skip with a disclosed reason (not a failure).
- **Absent `/dev/uinput` + `uinput`**: clean skip, exit 0, prompt (no hang).
- **Unknown `--backend` / unknown `--script`**: non-zero exit with a clear classified message.
- **Missing input device daemon** (e.g. the uinput tool's socket absent): detected and skipped, never a hang
  (bounded, no indefinite wait).
- **Wayland-only session for `x11-xtest`**: classified as a fail-or-skip per the planner, disclosed — never a
  silent pass.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The harness MUST define a **backend-agnostic declarative input script** — an ordered sequence of
  steps covering at least *click at a coordinate*, *key press*, and *wait an injected duration* — with a name.
- **FR-002**: The harness MUST provide a **`pure` backend** that replays an input script against the MVU model
  **deterministically and headlessly** (no display, no wall-clock; waits are injected), runnable in the
  default CI gate.
- **FR-003**: The harness MUST expose a real `input` CLI subcommand — `--backend pure|x11-xtest|uinput`,
  `--script <name>`, `--out <dir>`, `--json` — replacing the current stub; an unknown backend or script MUST
  exit non-zero with a clear, classified message.
- **FR-004**: Every input run MUST emit evidence (a `run.json`) carrying a run **status**, a disclosed skip
  reason when skipped, and a **non-empty** `NotAuthoritativeFor` list (no overclaim).
- **FR-005**: The run/skip/fail decision for each backend MUST be made by the harness's existing **pure
  planner** from the probed environment facts (the executor only interprets) — keeping the decision in tested,
  side-effect-free code.
- **FR-006**: The **`uinput`** backend MUST honest-skip when `/dev/uinput` (or its required daemon/socket) is
  absent — exit 0, `status` = skipped, disclosed reason, **promptly** (a bounded wait; never a hang or a
  fabricated pass).
- **FR-007**: The **`x11-xtest`** backend MUST, on a display/GL host, drive a live viewer window with real
  input and record a before/after repaint change (`status` = passed); headless it MUST skip cleanly with a
  disclosed reason (degrade-and-disclose, not a failure).
- **FR-008**: The `pure` backend MUST be **deterministic**: replaying the same script MUST produce
  byte-identical evidence (injected waits; no wall-clock; no randomness).
- **FR-009**: This feature MUST add **no product API** and MUST NOT change the public-surface baselines (it is
  harness-only, in `tests/Rendering.Harness`).
- **FR-010**: Harness unit tests MUST cover the planner's decision per backend, that `NotAuthoritativeFor` is
  never empty, the clean-skip exit code/status, and a deterministic `pure`-replay golden.

### Key Entities *(include if feature involves data)*

- **Input step**: one declarative action — *click(x, y)*, *key(name)*, or *wait(injected ms)*.
- **Input script**: a named, ordered list of input steps; backend-agnostic.
- **Backend**: the selectable interpreter — `pure` (MVU replay), `x11-xtest` (live X11 window), `uinput`
  (kernel evdev/libinput).
- **Run evidence**: the emitted `run.json` — run status, optional skip reason, `AuthoritativeFor` /
  **non-empty** `NotAuthoritativeFor` claim lists.
- **Probe facts**: the environment capabilities (display / GL / `/dev/uinput` presence) the planner reads.
- **Run plan**: the pure planner's per-backend run / skip / fail decision + the claimable proof scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `harness input --backend pure --script <name>` runs in the default CI gate **headlessly**, exits
  0, and emits a `run.json` with a non-empty `NotAuthoritativeFor`, in 100% of runs.
- **SC-002**: Replaying the same `pure` script twice produces **byte-identical** evidence, in 100% of runs.
- **SC-003**: `harness input --backend uinput` with `/dev/uinput` absent exits 0 with `status` = skipped and a
  disclosed reason, terminating within a bounded time (no hang), in 100% of runs.
- **SC-004**: `harness input --backend x11-xtest` proves a before/after repaint on a display/GL host and skips
  cleanly (disclosed, not failed) headless, in 100% of runs on each respective environment.
- **SC-005**: Every input run (any backend) emits a **non-empty** `NotAuthoritativeFor`, in 100% of runs.
- **SC-006**: The public-surface-drift gate is unaffected (zero baseline change), in 100% of builds.

## Assumptions

- The harness already provides: the live X11 input primitives (`X11.clickAt` / `X11.sendKey`), live window
  discovery (`Live`), the environment probe (`Probe`), the pure run/skip/fail planner with a non-empty
  not-authoritative schema (`RunPlan`), the no-overclaim evidence type (`Evidence`), and the pure product
  seams the `pure` backend replays against (`ControlsElmish.captureRespondsProof` / `Perf.runScript`). This
  feature wires them behind one declarative input model — it does not re-build them.
- This is **harness-only** — no product package changes, so the surface-drift gate is unaffected (FR-009).
  "Users" are harness operators + CI; the harness's evidence files and unit tests are the observable surface
  (per the constitution's vertical-slice rule for internal/tooling surfaces).
- In scope to **run in the gate now**: the input-script model, the `pure` backend, the CLI, and unit tests
  (A1/A2/A5/A6). The `x11-xtest` (A3) and `uinput` (A4/A7) arms are specified but **env-gated** — they
  degrade-and-disclose in the headless gate and are proven only on a capable runner (capable-runner
  provisioning is Workstream B, out of scope here).
- Determinism is the constitution's hard live-path constraint: the `pure` backend injects waits and consults
  no wall-clock, so its evidence is byte-reproducible (FR-008).
- This is the **Workstream A** near-term cut; being net-new, `/speckit-plan` and the chain will **build** the
  `Input.fsi` contract, the backends, the CLI wiring, and the tests (not a conformance pass).
</content>
