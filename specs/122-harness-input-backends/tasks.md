# Tasks: Harness Input Backends (pure + CLI) (Feature 122)

**Input**: Design documents from `/specs/122-harness-input-backends/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/input-fsi.md, contracts/input-cli.md, quickstart.md

**Tests**: Test tasks ARE included — FR-010 explicitly requires harness unit tests. This is **net-new,
contract-first BUILD work** (not a conformance pass): author `Input.fsi` **before** `Input.fs`, write the
semantic tests, then implement. All work is harness-only (`tests/Rendering.Harness` + its test project); no
product code, so the public-surface-drift gate is untouched.

**Organization**: by user story (US1 pure + US2 CLI = the P1 MVP; US3 uinput, US4 x11-xtest, US5 no-overclaim = P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallelizable (different file, no dependency on an incomplete task)
- **[Story]**: the user story a task serves (US1–US5)
- Exact file paths included

## Path Conventions

Single F# solution (`FS.GG.Rendering.slnx`). New module `tests/Rendering.Harness/Input.fsi` + `Input.fs`; edits
to `Domain.fs`, `Probe.fs(i)`, `Cli.fs`, the harness `.fsproj`, and `tests/Rendering.Harness.Tests/Tests.fs`.
Reuses `RunPlan`/`Evidence`/`X11`/`Live`/`Perf` and the product seam `ControlsElmish.captureRespondsProof`.

---

## Phase 1: Setup

- [X] T001 Confirm the starting state: `dotnet build FS.GG.Rendering.slnx -c Release` green; `tests/Rendering.Harness/Input.fs*` ABSENT; the `input` subcommand in `tests/Rendering.Harness/Cli.fs` (~line 112) is the "input backends pending…" stub (exit 2)
- [X] T002 Add `Input.fsi` then `Input.fs` to `tests/Rendering.Harness/Rendering.Harness.fsproj` in compile order — AFTER `Domain`/`Probe`/`RunPlan`/`Evidence`/`X11`/`Live`/`Perf`, BEFORE `Cli.fs` (which will consume `Input.run`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: the type/contract/planner-mapping below block every user story.

- [X] T003 Add `type InputBackend = Pure | X11XTest | Uinput` to `tests/Rendering.Harness/Domain.fs` — DISTINCT from the existing display `Backend` (`X11 | Wayland | NoDisplay`) (data-model §InputBackend)
- [X] T004 Ensure a `/dev/uinput`-availability fact is on `ProbeFacts` (`Domain.fs`) and computed in `tests/Rendering.Harness/Probe.fs` (checks `/dev/uinput`; a missing device degrades to a `None`/false fact, never a crash) — needed for the `uinput` run/skip decision (FR-006)
- [X] T005 Author the **`Input.fsi`** contract (`.fsi` BEFORE `.fs`, Principle I) per `contracts/input-fsi.md`: `InputStep` (`Click`/`Key`/`Wait`), `InputScript { Name; Steps }`, `InputBackend`, `scripts: Map<string,InputScript>`, `tryScript`, and `run: InputBackend -> InputScript -> ProbeFacts -> selfDll:string -> outDir:string -> Evidence.Evidence`
- [X] T006 Define the per-backend → planner-`Tier` mapping used by `run` (data-model table): `Pure`→Deterministic-proof, `X11XTest`→`T2`/`LiveHost`, `Uinput`→`TUinput`/`KernelInput`; the run/skip/fail `Degradation` MUST come from `RunPlan.plan tier facts` (executor only interprets, FR-005), with input-specific `AuthoritativeFor`/non-empty `NotAuthoritativeFor` claim lists

**Checkpoint**: types, the `.fsi` contract, and the planner mapping exist — user stories can build.

---

## Phase 3: User Story 1 — pure backend proves input → MVU → repaint headlessly (Priority: P1) 🎯 MVP

**Goal**: `harness input --backend pure --script <name>` runs headless, deterministic, emitting `run.json`
with `status=passed` and a non-empty `NotAuthoritativeFor`.

**Independent Test**: run pure headless → exit 0 + `run.json` (passed, non-empty `NotAuthoritativeFor`);
replay twice → byte-identical evidence.

- [X] T007 [P] [US1] Write the **deterministic pure-replay golden** test in `tests/Rendering.Harness.Tests/Tests.fs`: `Input.run Pure <script> facts … ` twice yields byte-identical `Evidence` (and non-empty `NotAuthoritativeFor`) (FR-008, SC-002) — RED until T008/T009
- [X] T008 [US1] Implement the named script catalog (`scripts`/`tryScript`) + ≥1 canonical script (clicks/keys/injected `Wait`) in `tests/Rendering.Harness/Input.fs` (FR-001)
- [X] T009 [US1] Implement the **`Pure`** interpreter arm of `Input.run` in `Input.fs`: replay the script against the MVU model via `ControlsElmish.captureRespondsProof` (and/or `Perf.runScript`), `Wait` injected (no wall-clock), emit `Evidence` with `ProofLevel = Deterministic`, `Status = Passed` (or `Failed` if `Inert`), non-empty `NotAuthoritativeFor` (FR-002, FR-004, FR-008)
- [X] T010 [US1] Confirm headless: `Input.run Pure` exits/returns `Passed` evidence with a non-empty `NotAuthoritativeFor`; T007 golden green (SC-001, SC-002)

**Checkpoint**: the pure proof (the MVP core) is green in the gate.

---

## Phase 4: User Story 2 — the `input` subcommand is wired (Priority: P1)

**Goal**: replace the stub with `--backend pure|x11-xtest|uinput --script <name> [--out <dir>] [--json]`;
unknown backend/script exits non-zero with a clear classified message.

**Independent Test**: `harness input --backend pure --script <name> --out <dir> --json` writes evidence under
`<dir>`; unknown `--backend`/`--script` exits non-zero; the old stub is gone.

- [X] T011 [P] [US2] Write a CLI-surface test in `tests/Rendering.Harness.Tests/Tests.fs`: unknown `--backend` and unknown `--script` produce a non-zero/classified result; a valid `pure` invocation resolves a script and writes evidence (FR-003) — RED until T012
- [X] T012 [US2] Replace the `input` stub in `tests/Rendering.Harness/Cli.fs` (~line 112) with argument parsing (`--backend`/`--script`/`--out`/`--json`) that resolves the script via `Input.tryScript` and calls `Input.run`; unknown backend/script ⇒ non-zero exit + clear classified message (not the removed stub) (FR-003)
- [X] T013 [US2] Confirm `--out <dir>` writes `run.json`/`metrics.csv`/`summary.md` via `Evidence.write` and `--json` emits machine-readable output (FR-003, FR-004)

**Checkpoint**: US1 + US2 = the full P1 MVP (a real CLI driving the gate-runnable pure proof).

---

## Phase 5: User Story 3 — uinput honest-skips when the kernel device is absent (Priority: P2)

**Goal**: `--backend uinput` with `/dev/uinput` absent exits 0, `status=skipped`, disclosed reason, prompt
(no hang, no fake pass).

**Independent Test**: with the device absent → exit 0, skipped, non-empty `SkipReason`, terminates promptly.

- [X] T014 [P] [US3] Write a test in `tests/Rendering.Harness.Tests/Tests.fs`: with a uinput-absent `ProbeFacts`, `RunPlan.plan` yields `Skip` for the `TUinput` tier and `Input.run Uinput` returns `Status = Skipped` with a non-empty `SkipReason` (FR-006, SC-003) — RED until T015
- [X] T015 [US3] Implement the **`Uinput`** interpreter arm of `Input.run` (shell `ydotool`): gate on the uinput fact (+ detect a missing `ydotoold` socket) UP FRONT; absent ⇒ `Skipped` evidence (exit-0 path), disclosed reason, **bounded** (never block); present ⇒ drive the kernel path with `ProofLevel = KernelInput` (FR-006)
- [X] T016 [US3] Confirm headless honest-skip is prompt (bounded, no hang) and never a fabricated pass (SC-003)

**Checkpoint**: the kernel tier degrades-and-discloses safely.

---

## Phase 6: User Story 4 — x11-xtest proves input → repaint, skips cleanly otherwise (Priority: P2)

**Goal**: on a display/GL host, drive a live window and record a before/after repaint (`status=passed`);
headless ⇒ clean `skipped` with a disclosed reason (not a failure); Wayland ⇒ classified fail.

**Independent Test**: display/GL host → repaint change, passed; headless → skipped + disclosed reason.

- [X] T017 [P] [US4] Write a test in `tests/Rendering.Harness.Tests/Tests.fs`: with a no-display `ProbeFacts`, `RunPlan.plan` yields `Skip` for `T2` and `Input.run X11XTest` returns `Skipped` (disclosed); with a Wayland fact, `FailClassified` (FR-007, SC-004) — RED until T018
- [X] T018 [US4] Implement the **`X11XTest`** interpreter arm of `Input.run`: reuse `Live` window discovery + `X11.clickAt`/`sendKey`; capture before/after; assert a visible repaint change; emit `ProofLevel = LiveHost` evidence; headless ⇒ clean `Skipped`; Wayland ⇒ `FailClassified` (FR-007)
- [X] T019 [US4] Verify on a display/GL host the before/after repaint is recorded (`passed`); headless the skip is clean (disclosed, not failed). On the headless dev box, record the env-gated honest-skip (capable-runner proof is Workstream B) (SC-004)

**Checkpoint**: the live X11 tier proves input→repaint on a capable host, honest-skips otherwise.

---

## Phase 7: User Story 5 — every run discloses what it does not prove (Priority: P2)

**Goal**: every backend run emits a non-empty `NotAuthoritativeFor` and a status matching the planner's
`Degradation`.

**Independent Test**: for each backend, evidence has a non-empty `NotAuthoritativeFor` and status = planner decision.

- [X] T020 [US5] Write a cross-backend test in `tests/Rendering.Harness.Tests/Tests.fs`: for `Pure`/`X11XTest`/`Uinput` over representative `ProbeFacts`, assert `NotAuthoritativeFor` is non-empty and `Status` matches `RunPlan.plan`'s `Degradation` (run/skip/fail) (FR-004, FR-005, SC-005)

**Checkpoint**: the no-overclaim contract holds for every backend.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T021 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release` and the full suite (`dotnet test FS.GG.Rendering.slnx -c Release`) — 0 failures; the `pure` arm runs in the gate; the live arms honest-skip
- [X] T022 Confirm zero public-surface change: `git status -s tests/surface-baselines/` empty (harness-only — FR-009/SC-006)
- [X] T023 [P] Update `docs/harness/capability-baseline.md` (and the README harness status, if stale) with the new input-backend matrix (pure in-gate; x11-xtest/uinput env-gated) — the A7 docs slice
- [X] T024 Run `/speckit-analyze` for cross-artifact consistency (spec ↔ plan ↔ tasks)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** → **Foundational (P2, blocks all stories)** → **US1–US5** → **Polish**.
- Within Foundational: T003 (type) → T004 (probe fact) → T005 (`.fsi`) → T006 (planner mapping).
- US1 + US2 deliver the P1 MVP and should land first; US3/US4/US5 are independent P2 increments after them.

### User Story Dependencies

- **US1 (P1)**: needs Foundational. The MVP core.
- **US2 (P1)**: needs US1 (`Input.run` must exist to wire the CLI to it).
- **US3 / US4 (P2)**: need Foundational; independent of each other (different interpreter arms of `Input.run`).
- **US5 (P2)**: needs the three arms to assert the cross-backend invariant (lightest; can follow US3/US4).

### Parallel Opportunities

- The test-authoring tasks (T007, T011, T014, T017 — all in `Tests.fs`) are written before their
  implementation; they touch one file so run them sequentially within that file, but each is independent of the
  other stories' *implementation*.
- The three interpreter arms (T009 pure, T015 uinput, T018 x11-xtest) are distinct code paths in `Input.fs`;
  once the `.fsi` (T005) lands they can be implemented in parallel.
- Polish T023 (docs) is independent of T021/T022.

---

## Implementation Strategy

> Net-new BUILD (contract-first). `Input.fsi` is authored before `Input.fs`; tests precede each arm where the
> golden/skip behaviour is the spec (FR-008/FR-006/FR-007).

### MVP First (US1 + US2)

1. Setup + Foundational (types, `.fsi`, planner mapping).
2. US1 — the deterministic pure backend (golden test → catalog → interpreter), green in the gate.
3. US2 — wire the CLI to `Input.run`.
4. **STOP and VALIDATE**: `harness input --backend pure --script <name>` runs headless, deterministic,
   non-empty `NotAuthoritativeFor` — the harness's missing input layer is closed for the gate.

### Incremental Delivery

1. US3 (uinput honest-skip) → US4 (x11-xtest live + skip) → US5 (cross-backend no-overclaim).
2. Polish: full suite green, zero public-surface change, capability-baseline doc, analyze.

---

## Notes

- `[P]` = different file, no incomplete-task dependency. `[Story]` maps each task to its user story.
- Contract-first: `Input.fsi` (T005) precedes `Input.fs` (T008+). The run/skip/fail decision stays in the
  tested pure `RunPlan.plan` (T006); the interpreter only acts on a `Run` (FR-005).
- Harness-only: no product code; the surface-drift gate is the direct check of FR-009 (T022).
- The `x11-xtest`/`uinput` live proofs are env-gated; on a headless box they honest-skip (capable-runner
  provisioning is Workstream B). Their contract + planner decision are still authored and unit-tested now.
</content>
