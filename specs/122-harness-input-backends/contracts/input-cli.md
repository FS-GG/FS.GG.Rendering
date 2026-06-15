# Contract — `harness input` CLI command (Feature 122)

Replaces the current stub in `tests/Rendering.Harness/Cli.fs` (~line 112, which prints "input backends
pending…" and exits 2). The CLI is a thin parser over `Input.run`; all decision logic stays in `RunPlan`/`Input`.

## Command

```
harness input --backend <pure|x11-xtest|uinput> --script <name> [--out <dir>] [--json]
```

| Flag | Required | Meaning |
|---|---|---|
| `--backend` | yes | the interpreter: `pure` \| `x11-xtest` \| `uinput` |
| `--script` | yes | a name resolved from the `Input.scripts` catalog |
| `--out` | no | evidence output directory (default the harness's standard out dir) |
| `--json` | no | emit machine-readable output |

## Behaviour / exit + status contract

- **`pure`** (any environment, incl. headless): runs; writes `run.json` with `status = passed` (or `failed` if
  the script proves inert / no change) and a **non-empty** `NotAuthoritativeFor`. **Exit 0** on a clean run
  (SC-001). Deterministic: same `--script` twice ⇒ byte-identical evidence (SC-002).
- **`x11-xtest`**: on a display/GL host, drives a live window and records a before/after repaint change
  (`status = passed`); **headless** ⇒ `status = skipped` with a disclosed `SkipReason`, **exit 0** (not a
  failure) (SC-004); Wayland-only ⇒ classified fail (disclosed).
- **`uinput`**: with `/dev/uinput` (+ `ydotoold`) present, drives the kernel path; **absent** ⇒ `status =
  skipped`, disclosed reason, **exit 0**, **promptly** (bounded; no hang, no fake pass) (SC-003).
- **Unknown `--backend`** or **unknown `--script`**, or a missing required flag ⇒ **non-zero exit** with a
  clear, classified message (NOT the removed stub) (FR-003).
- Every run writes `run.json` (+ `metrics.csv` + `summary.md`) via `Evidence.write`; every `run.json` carries a
  non-empty `NotAuthoritativeFor` (FR-004/SC-005).

## Evidence (the `run.json` shape — existing `Evidence.Evidence`)

`{ RunId; Tier; Subcommand = "input"; Status; SkipReason; ProofLevel; AuthoritativeFor; NotAuthoritativeFor
(non-empty); Facts; Frames; P50/95/99Ms; Artifacts }`. 122 adds no field.

## Surface

- Harness-only: **no product API**, so the public-surface-drift gate is unaffected (FR-009/SC-006).

*Pins*: FR-003, FR-004, FR-006, FR-007, FR-009. *Used by*: US1, US2, US3, US4.
</content>
