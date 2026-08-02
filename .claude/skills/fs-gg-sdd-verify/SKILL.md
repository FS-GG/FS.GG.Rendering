---
name: fs-gg-sdd-verify
description: Stage 9 of the FS.GG SDD lifecycle — fsgg-sdd verify evaluates verification readiness over task/evidence/test/skill obligations and emits readiness/<id>/verify.json. It authors no source and builds the work model. Use after evidence, before ship.
---

# Verify (stage 9)

`verify` evaluates whether the work item is actually verified — it reads the task
graph, the evidence dispositions, test and skill obligations, and lifecycle stage
state, and emits a readiness view. It authors **no** `work/<id>/` source; it (with
`ship`) is also the point at which the normalized `work-model.json` is built.

**Read the worked example first.** `verify` authors nothing; what it evaluates is the
`tasks.yml` and `evidence.yml` you wrote — and `docs/examples/lifecycle-artifacts/` holds a
complete, machine-checked instance of both. When `verify` reports an obligation unsatisfied,
that corpus is the fastest reference for what a satisfying declaration actually looks like.

## Command

```text
fsgg-sdd verify --work <id>
```

## Produces / consumes

- **Consumes:** task / evidence / test / skill obligations from the authored
  sources.
- **Authors:** nothing under `work/<id>/`.
- **Tool generates:** `readiness/<id>/verify.json` and builds/refreshes the work
  model.
- **Next:** `ship` ([[fs-gg-sdd-ship]]).

## What `verify.json` reports

A verification view with: evidence dispositions, test dispositions, skill
visibility, the task graph status validity, and lifecycle readiness, with a
top-level readiness verdict. Crucially, **synthetic and missing evidence never
count as satisfied** here — the disposition states include explicit
`EvidenceSyntheticDisposition` / `EvidenceMissingDisposition` (and the test
equivalents), so a synthetic stand-in shows up as disclosed-but-unsatisfied rather
than passing silently.

## The work-model boundary

`verify` (and `ship`) build the normalized `work-model.json`. Before this point —
during `charter`/`specify`/`clarify`/`checklist` — the work model does not exist,
which is why early-stage agent guidance comes from the static
`.fsgg/early-stage-guidance.md`. Once `verify` builds the work model, the
generated `agent-commands/<target>/` guidance becomes available (see
[[fs-gg-sdd-refresh-agents]]).

## Pitfalls

- Expecting `verify` to pass with synthetic-only or deferred evidence — it reports
  them as not-satisfied. Supply real `result: pass`, `synthetic: false` evidence
  for obligations that must close. See [[fs-gg-sdd-evidence]].
- Reading a stale `verify.json` — re-run after changing any source.

## Next

- `ship` — aggregate merge-boundary readiness: [[fs-gg-sdd-ship]].

## Related

- [[fs-gg-sdd-evidence]], [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/quickstart.md`; `src/FS.GG.SDD.Artifacts/LifecycleArtifacts/Verify.fsi`.
