---
name: fs-gg-sdd-analyze
description: Stage 7 of the FS.GG SDD lifecycle — fsgg-sdd analyze runs a read-only cross-artifact consistency/readiness check across spec/clarify/checklist/plan/tasks and emits readiness/<id>/analysis.json. It authors no source. Implementation happens right after analyze, before evidence.
---

# Analyze (stage 7)

`analyze` is the cross-artifact consistency check before you write code. It reads
the whole authored cascade (spec → clarify → checklist → plan → tasks), reports
inconsistencies and readiness, and emits a generated view. It authors **no**
`work/<id>/` source — it aggregates.

**Read the worked example first.** `analyze` authors nothing, but it reads the whole
authored cascade — and `docs/examples/lifecycle-artifacts/` is a complete, worked instance of
it. The skill↔gate doctest drives that corpus through the real gates (`charter` →
`analyze`) on every build, so it is both the shape `analyze` expects to find and a
known-clean run.

After `analyze` reports implementation-ready, you **implement** (write the code —
there is no `fsgg-sdd implement` command), then record `evidence`.

## Command

```text
fsgg-sdd analyze --work <id>
```

## Produces / consumes

- **Consumes:** the tasks cascade and all upstream authored sources.
- **Authors:** nothing under `work/<id>/`.
- **Tool generates:** `readiness/<id>/analysis.json` (analysis readiness, missing
  dispositions, findings) and refreshes the work model.
- **Next:** **implement, then `evidence`** ([[fs-gg-sdd-evidence]]).

## What it checks

`analyze` surfaces the kinds of problems that should block coding: requirements
with no covering task, tasks referencing ids that do not exist, prose-vs-structured
conflicts, missing dispositions. It reports them as readiness findings rather than
silently merging anything.

## The implement step

`implement` is a lifecycle **stage value**, not a command. Between `analyze` and
`evidence` you:

1. write the code for the tasks (respecting the plan's surface-first ordering);
2. write the tests that fail-before / pass-after;
3. then declare what proves each obligation in `evidence.yml`.

Keep the authored sources and generated views in sync as you go — if you edit a
`work/<id>/` source while implementing, re-run [[fs-gg-sdd-refresh-agents]] so the
views are current (presence is not currency).

## Pitfalls

- Treating a green `analysis.json` on disk as current after you changed sources —
  re-run `analyze`/`refresh`; a stale view is not readiness.
- Skipping the implement step and jumping to `evidence` with nothing to evidence.

## Next

- Implement the code, then `evidence` — declare proof: [[fs-gg-sdd-evidence]].

## Related

- [[fs-gg-sdd-lifecycle]], [[fs-gg-sdd-verify]].

## Sources

- `docs/quickstart.md` (analyze row + "implement, then evidence").
