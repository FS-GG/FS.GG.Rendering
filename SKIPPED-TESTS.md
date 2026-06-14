# Skipped tests (Stage R4 import)

Per constitution Principle V and FR-011, tests that cannot pass for an **out-of-scope** reason
are skipped with written rationale — never marked passing, never weakened. This is the
per-file triage R3 deferred to R4. Total: **18 skipped**, all in two categories.

## 1. Performance corpus / baselines → Stage R5 (harness) — 17

- `tests/Elmish.Tests/Feature109CorpusTests.fs` — "performance-scenario corpus, deterministic
  metrics goldens" (`ptestList`).
- `tests/Elmish.Tests/Feature109BaselineReportTests.fs` — "non-golden timing/allocation
  baselines" (`ptestList`).

**Why**: these depend on committed perf-golden fixtures, `docs/reports/_baselines/**`, and
byte-identical perf determinism — exactly the performance-evidence tier that R3 routed to the
**Stage R5 rendering/perf harness**. They are not product-behavior unit tests and were not
part of the import-now behavior surface. The honest-FrameMetrics fidelity tests in the same
feature (not golden-dependent) remain **active and passing**.

**Un-skip when**: the R5 harness lands with its committed perf goldens and a deterministic
perf-capture path.

**R5 status (2026-06-14)**: the harness T3 perf tier now provides a **headless offscreen
render-throughput** mode (`harness perf --mode throughput`) with real per-frame timing +
percentiles, honestly scoped (`offscreen-render-throughput`, **not** vsync-faithful). The
**faithful vsync/present-timing** perf path these Feature109 tests want still depends on the
live present loop (blocked headlessly in this container — see
`docs/harness/capability-baseline.md`), so they remain `ptest`/`ptestList` until that tier
lands.

## 2. FSI transcript fixture → excluded old-repo artifact — 1

- `tests/Controls.Tests/TypedControlContractTests.fs` — "FSI transcript expectations …"
  (`ptest`).

**Why**: it asserts on `specs/028-agent-validation-framework/readiness/fsi-session.txt`, an
old-repo feature-workflow/readiness artifact deliberately **not imported** (FR-009). The rest
of that test file (typed front-door contract checks) is active and passing.

**Un-skip when**: a current FSI transcript fixture is added under this repo.

## Note — test-generated artifacts

Some Controls/Elmish suites regenerate goldens/readiness files into repo-relative paths
(`specs/<n>/readiness/**`) when a committed golden is absent. Those generated directories are
**not source** and were removed after the import run; controlling their output location is a
Stage R6 (CI wiring) concern.
