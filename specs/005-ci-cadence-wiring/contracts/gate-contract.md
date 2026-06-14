# Contract: Deterministic Gate (Stage R6)

Defines exactly what the required `gate` workflow runs, what makes it red, and what can never make it
red. Branch protection requires **this gate and only this gate**.

## Trigger

- `push` and `pull_request` targeting the default branch.
- Runs on a hosted headless Linux runner. Uses **no privileged secrets** (so fork PRs run). *(FR-001, FR-013)*

## Steps (in order)

1. **Build** the full solution on `net10.0` (`dotnet build FS.GG.Rendering.slnx`). Build failure ⇒ gate FAIL. *(FR-002)*
2. **Default local tier** — run the deterministic, capability-`none` test projects (the `local` rows in
   `cadence-matrix.md` minus the `gl` ones). Any failure ⇒ gate FAIL. *(FR-002, FR-003)*
3. **Surface-baselines** — public `.fsi` surface-drift check. Drift ⇒ gate FAIL. *(FR-003)*
4. **Docs build** — `fsdocs` builds the site from current sources (build only; no publish). Failure ⇒ gate FAIL. *(FR-003)*
5. **Harness T0/T1** — one `Rendering.Harness offscreen` invocation emits `T0/run.json` (deterministic,
   **required**) and `T1/run.json` (GL readback, **advisory**). The gate reads **per-tier `run.json.status`**,
   not the process exit code: `T0` `status:"failed"` ⇒ gate FAIL *(FR-004)*; `T1` `status:"skipped"` ⇒
   disclosed skip, never gating. ⚠ The aggregate `offscreen` exit code is `1` whenever T1 is cleanly
   *skipped* (R5 `Cli.fs` `runOffscreen` requires **both** tiers `passed` for exit `0`), so gating on the
   process exit code would falsely red the headless gate — the action MUST key off each tier's `run.json`.
   *(FR-004, FR-005, FR-011)*
6. **GL/display checks** (`SkiaViewer.Tests`, `Smoke.Tests`) — run via the harness/`probe`
   classification: if GL is absent, **skip with written rationale** (`status:"skipped"`);
   they do **not** affect pass/fail. If GL *should* be present but the toolchain is misconfigured, **fail
   fast** with probe facts. *(FR-005, FR-010, FR-011)*

## Exit-code mapping (reused from R5 harness)

| Harness exit | `run.json.status` | Gate effect |
|---|---|---|
| `0` | `passed` | step passes |
| `0` | `skipped` | step **skipped + disclosed** — never counted as pass, never fails gate |
| `1` | `failed` | gate FAIL |
| `2` | — | gate FAIL (bad usage = misconfiguration) |

> **Per-tier, not per-process.** A subcommand that emits several tiers (e.g. `offscreen` → T0+T1) writes
> one `run.json` per tier; this table is applied to **each tier's `run.json.status`**. The aggregate
> process exit code is *not* the gate signal — `offscreen` returns `1` whenever T1 is cleanly *skipped*,
> which must not fail the gate. Process exit `2` (bad usage) is the only exit-code-level gate failure.

## What can NEVER fail the gate

- A capability being **absent** (no GL / no X11 / no `/dev/uinput`). *(FR-005)*
- Any `release` or `capability` cadence job. *(FR-007, FR-011)*
- Flakiness in an advisory tier. *(FR-011)*

## Outputs

- A job summary carrying the proof-scope disclosure per `run-summary.schema.md`. *(FR-006)*
- Uploaded harness artifacts (`run.json`/`metrics.csv`/`summary.md`) for the run.

## Acceptance (maps to spec)

- [ ] Clean PR ⇒ gate green; deterministic-break PR ⇒ gate red + merge blocked. *(SC-006)*
- [ ] Deterministic gate completes < 10 min on a standard hosted runner. *(SC-002)*
- [ ] On a headless run, every `gl`/`x11`/`uinput` check is skipped-with-rationale, never passed. *(SC-004)*
- [ ] No release-only check executes in the gate. *(SC-007)*
