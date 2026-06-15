# Contract — `Input.fsi` module (Feature 122)

The new harness module's signature (authored **before** `Input.fs`, per Principle I). Harness-internal (no
product surface). Types may live in `Domain.fs` (so `Evidence`/`RunPlan` can reference them) with `Input.fsi`
re-exposing the script model + the single `run` entry.

## C1 — the declarative input-script model

```fsharp
type InputStep =
    | Click of x: int * y: int
    | Key   of string            // e.g. "space", "Right", "a"
    | Wait  of ms: int           // injected duration — deterministic, never wall-clock

type InputScript = { Name: string; Steps: InputStep list }

type InputBackend =
    | Pure
    | X11XTest
    | Uinput
```

- `InputStep`/`InputScript` are **backend-agnostic** (FR-001).
- `InputBackend` is **distinct** from the display `Domain.Backend` (`X11|Wayland|NoDisplay`).

## C2 — the named script catalog

```fsharp
val scripts: Map<string, InputScript>        // resolved by --script <name>
val tryScript: name: string -> InputScript option
```

- An unknown `name` ⇒ `None` ⇒ the CLI exits non-zero with a clear message (FR-003).

## C3 — the single run entry

```fsharp
val run:
    backend: InputBackend ->
    script: InputScript ->
    facts: ProbeFacts ->
    selfDll: string ->
    outDir: string ->
        Evidence.Evidence
```

- MUST compute the run/skip/fail decision via `RunPlan.plan <tier-for-backend> facts` and **only interpret** a
  `Run` (FR-005). A `Skip reason` ⇒ `Status = Skipped`, `SkipReason = Some reason`, exit-0 path; a
  `FailClassified reason` ⇒ classified-fail evidence.
- The returned `Evidence` MUST carry a **non-empty** `NotAuthoritativeFor` (FR-004) and the right `ProofLevel`
  per backend (`Deterministic` / `LiveHost` / `KernelInput`).
- **`Pure`**: interpret the script against the MVU model via `ControlsElmish.captureRespondsProof` /
  `Perf.runScript`; deterministic, headless; `Wait` is injected (no wall-clock). Replaying the same script MUST
  yield byte-identical evidence (FR-008).
- **`X11XTest`**: reuse `Live` window discovery + `X11.clickAt`/`sendKey`; capture before/after; assert a
  visible repaint change; honest-skip with no display; `FailClassified` on Wayland (FR-007).
- **`Uinput`**: drive `ydotool` against the live window; **require** `/dev/uinput` (+ `ydotoold` socket);
  honest-skip promptly when absent — bounded, no hang, no fake pass (FR-006).

## C4 — totality / safe-failure

- `run` MUST be **total**: any backend on any environment yields valid `Evidence` (a disclosed `Skipped` /
  classified-fail rather than an exception or a hang). Capability absence is detected up front (bounded).

*Pins*: FR-001..FR-008. *Used by*: US1–US5 (via the CLI + harness unit tests).
</content>
