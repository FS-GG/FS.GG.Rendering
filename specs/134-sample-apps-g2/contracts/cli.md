# Contract — CLI (`SampleApps.App`)

The thin executable dispatches four subcommands. The headless `evidence` + `coverage` + `list` paths are
the CI-facing surface and **never depend on a display or GL** (FR-014). Mirrors G1's `Program.fs` dispatch.

```
SampleApps list
SampleApps interactive <sample-id> [--theme light|dark] [--accent indigo|teal]
SampleApps evidence --seed <int> [--sample <sample-id>] [--out <dir>]
SampleApps coverage [--out <file>]
```

`<sample-id>` ∈ `tetris | snake | pong | kanban | todo | calendar`.

## `list`

Prints each registered sample: `id`, family, title, controls/inputs summary. Always exit `0`. No GL.

## `interactive <sample-id>`

Looks up the `SampleEntry`, calls its `Interactive` closure → `runInteractiveApp host`. GL-gated and
advisory. Unknown id → message + exit `1`. On a no-display/no-GL host it degrades-and-discloses (it does not
hang) consistent with the viewer's capability reporting.

## `evidence --seed <int>`

For each sample (or the one named by `--sample`):
1. replay the seeded script via `Perf.runScript` → golden `state.txt` (no GL needed),
2. compute the sample `Outcome` from the final model and check it equals the authored `ExpectedOutcome`,
3. capture an offscreen screenshot via `captureScreenshotEvidence` — **degrade-and-disclose** on any GL
   failure (no fabricated frame, no stale `frame.png`),
4. write `run.json` / `summary.md` / `state.txt` (+ `frame.png` only if proven) under
   `<out>/<seed>/<sample-id>/`.

`--out` defaults to `artifacts/sample-apps`. Exit codes:

| Code | Meaning |
|---|---|
| `0` | all requested samples produced a record (including **disclosed degraded** screenshot runs) |
| `2` | bad usage — missing/non-integer `--seed`, or `--sample` matched no sample |

A run is **success (0)** even with no GL: the deterministic state + outcome + disclosure are the
authoritative product; the screenshot is best-effort (FR-008/FR-014).

## `coverage [--out <file>]`

Emits / validates the coverage + backlog report (see `coverage-backlog.md`). Prints per-sample
control/input coverage and the 22-spec adopted/deferred table. Exit `0` when the honesty check passes,
`1` when it fails (missing sample, unaccounted spec, dangling control id, missing input modality). No GL.

## Determinism & no-overclaim invariants (all headless paths)

- No wall-clock, no `System.Random` — seeded PRNG + injected `Tick` deltas only (FR-006).
- Every evidence record carries a **non-empty** `NotAuthoritativeFor` (FR-007).
- A no-GL/no-display host yields a clean, **non-hanging** exit with a disclosed reason — never a fabricated
  pass (FR-008).
