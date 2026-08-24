---
name: fs-gg-sdd-troubleshooting
description: Diagnose an FS.GG SDD lifecycle stage that blocks, stalls, or reports noChange for a reason the default JSON output hides. The fastest tool is fsgg-sdd <stage> --text — its summary counters (blockingAmbiguities, checklistFailedBlocking, staleResultCount, …) name the cause the JSON outcome omits. Use when a stage won't advance and you can't tell why.
---

# Troubleshooting a blocked stage

When a lifecycle stage won't advance and the default JSON output doesn't say why,
the single most useful move is to **re-run the same command with `--text`** and
read its summary counters. The default JSON contract leads with `outcome` and
`diagnostics`; a stage can legitimately report `outcome: noChange` (or succeed)
while a non-zero counter is the thing actually holding you at that stage. `--text`
puts those counters front and center.

## The recipe

```text
fsgg-sdd <stage> --work <id> --text
```

Read the counters. A non-zero counter that maps to your current stage is the block.
Then fix the authored source (not the generated view) and re-run.

> **Worked example.** `clarify` reports `outcome: noChange` with
> `blockingAmbiguities: 1` — the JSON `diagnostics` array is empty, so the default
> output looks clean, but `plan`/`checklist` will refuse to advance. `--text` shows
> the `blockingAmbiguities: 1` line immediately.

## Counter → meaning → next action

| Counter (`--text`) | Stage | Means | Do |
|---|---|---|---|
| `blockingAmbiguities` | clarify → checklist/plan | An `AMB-###` under `## Remaining Ambiguity` is still unresolved (or a bullet there names an AMB id) | Resolve it with a decision/accepted deferral, or write the section as a `None.`/`No remaining ambiguities.` disclaimer. See [[fs-gg-sdd-clarify]] |
| `remainingAmbiguities` | clarify | Open questions/ambiguities still recorded | Record a `DEC-###` decision or an accepted deferral for each |
| `checklistFailedBlocking` / `failedBlockingCount` | checklist → plan | A requirement failed the quality/coverage check, or `## Blocking Findings` holds a real finding | Fix the coverage line or clear the finding, then re-run `checklist`. See [[fs-gg-sdd-checklist]] |
| `staleResultCount` | checklist | A review was recorded against an older source snapshot | Re-run `checklist` — it re-derives its results and rewrites its own `## Source Snapshot` |
| `stalePlanSnapshot` (diagnostic, not a counter) | plan → tasks/analyze | `plan.md`'s recorded `## Source Snapshot` digests no longer match `spec.md`/`clarifications.md`/`checklist.md`. `plan` does **not** self-heal: a bare re-run blocks and writes nothing | Review the recorded `PD-###` decisions against the changed sources (the diagnostic's `relatedIds` name them), then re-run **`fsgg-sdd plan --accept-upstream`** — the one gesture that re-baselines the snapshot |
| checklist `status` not `checklistReady` | plan | The checklist review isn't clean (writes `needsCorrection`) | Clear blocking findings and **re-run `fsgg-sdd checklist`** — it auto-writes `checklistReady`; don't hand-edit the status |

## When the counter looks fine but the stage still blocks

- **Pre-flight the artifact first: `fsgg-sdd lint <artifact>`** (or `<stage>
  --explain`). It statically reports the load-bearing grammar defects — a
  mis-formatted coverage line, a missing `[AMB:AMB-###]` decision tag, incomplete
  per-stage front matter, or a duplicate id — each with a fix hint and a pointer to
  the grammar of record, *before* the stage blocks. Read-only; exit `0` clean / `1`
  defects / `2` unusable input. See `docs/reference/lint.md`.
- The block is almost always a **silently mis-formatted authored input** — a
  load-bearing grammar accepted the file but read it differently than you meant.
  Check the exact accepted/rejected forms in [[fs-gg-sdd-authoring-contracts]]
  (coverage line, `evidence.yml` satisfaction rule, `specify --input` intent, and
  the clarify/checklist empty-section rules).
- Compare against a known-good artifact: the shipped worked examples at
  `docs/examples/lifecycle-artifacts/` (`clarifications.md`, `checklist.md`,
  `tasks.yml`, `evidence.yml`) parse clean and are copy-adaptable.
- You are editing a **generated view** instead of the authored source. Re-run the
  stage; if the view is stale, [[fs-gg-sdd-refresh-agents]] brings it current.

## `--text` vs `--json` vs `--rich`

Same report, three projections (precedence `--rich` > `--text` > `--json` >
default). `--json` (default) is the automation contract; `--text` is the portable
human summary — the best quick diagnostic; `--rich` adds color/panels and degrades
to plain text when redirected. None changes the outcome or exit code.

## Related

- [[fs-gg-sdd-authoring-contracts]] (the load-bearing grammars),
  [[fs-gg-sdd-lifecycle]] (the stage map), and the per-stage `fs-gg-sdd-*` skills.
