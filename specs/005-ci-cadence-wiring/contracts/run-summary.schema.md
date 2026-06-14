# Contract: Per-Run Proof-Scope Summary (Stage R6)

Every CI run emits a single human-readable summary that states **what it proved and what it could
not prove on that runner** — so a reviewer judges the result without opening raw logs (FR-006,
SC-005). The content is sourced from the R5 harness evidence (`run.json`/`summary.md`); R6 presents
it, it does not recompute proof.

## Required content

| Field | Source | Required |
|---|---|---|
| `cadence` | which workflow ran (`gate`/`release`/`capability`) | yes |
| `runnerCapability` | harness `probe`: display? GL? `/dev/uinput`? | yes |
| `proved` | list of checks that ran and passed (with `authoritativeFor`) | yes |
| `notProvedHere` | checks skipped for absent capability, with `notAuthoritativeFor` + `rationale` | yes |
| `failed` | checks that failed (empty on green) | yes |
| `overall` | `pass` / `fail` (gate) or `evidence-only` (release/capability) | yes |

## Rules

1. A `skipped` check appears under **`notProvedHere`**, never under `proved`. A skip is never rendered
   as a pass. *(FR-005, SC-004)*
2. `notProvedHere` MUST carry a written, machine-readable `rationale` per entry (e.g.
   `"no hardware GL on hosted runner"`). *(FR-005)*
3. The summary MUST let a reader answer "was live/visual behavior verified here?" from the summary
   alone. On a headless gate run the answer is an explicit *no, not proved here*. *(SC-005)*
4. Misconfiguration (vs. absence) surfaces as a `failed` entry with probe facts, not a silent skip. *(FR-010)*

## Example (headless gate run — illustrative)

```text
cadence: gate            runnerCapability: { display: none, gl: none, uinput: absent }
overall: pass
proved:
  - Color/Scene/Layout/Input/KeyboardInput/Elmish/Controls/Testing/Lib.Tests  (deterministic)
  - surface-baselines  (no .fsi drift)
  - docs build (fsdocs)
  - harness T0 (offscreen deterministic)
notProvedHere:
  - SkiaViewer.Tests, Smoke.Tests, harness T1  — rationale: "no hardware GL on hosted runner"
  - harness T2 (live), T3 (perf), T-uinput      — rationale: "capability cadence; not run in gate"
failed: []
```

## Acceptance (maps to spec)

- [ ] Every run produces this summary; a reviewer determines proved-vs-not from it alone. *(FR-006, SC-005)*
- [ ] No skipped check ever appears as proved/passed. *(FR-005, SC-004)*
