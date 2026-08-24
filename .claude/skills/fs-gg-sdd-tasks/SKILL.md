---
name: fs-gg-sdd-tasks
description: Stage 6 of the FS.GG SDD lifecycle — fsgg-sdd tasks generates the typed, dependency-ordered task graph in work/<id>/tasks.yml (NOT tasks.md), where each task back-references requirements, decisions, required skills, and required evidence. Use after plan, before analyze.
---

# Tasks (stage 6)

`tasks` turns the plan into a **typed task graph**. Unlike legacy Spec Kit's
Markdown `tasks.md`, the native SDD source is `work/<id>/tasks.yml` — a
schema-versioned file where each task carries stable cross-references that the work
model resolves into links. This is what makes "is this task done?" a checkable
fact later.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/tasks.yml` is a complete,
worked `tasks.yml`, and it is machine-checked rather than illustrative: on every build the
skill↔gate doctest seeds it and drives the real gates (`charter` → `analyze`) over it,
failing if it does not come out clean. Where the prose below and the example disagree, the
example is the authority.

## Command

```text
fsgg-sdd tasks --work <id>
```

## Produces / consumes

- **Consumes:** the plan cascade (`spec` → `clarify` → `checklist` → `plan`).
- **Hybrid source:** `work/<id>/tasks.yml` — the tool derives the graph; see
  "Who owns what" below.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `analyze` ([[fs-gg-sdd-analyze]]).

## `tasks.yml` schema

Each task has: a stable `id`, `status`, `owner` (the agent/role assumed),
back-references to `requirements` and `decisions`, `dependencies`, `requiredSkills`
(capability tags the implementer must have loaded), and `requiredEvidence` (the
obligation ids that close the task).

## Who owns what

`tasks.yml` is a **hybrid**, and for the derived graph the tool holds the pen. Every
run of `fsgg-sdd tasks` re-derives that graph from the plan cascade, so a derived
task's `title`, `requirements`, `decisions`, `dependencies`, `requiredSkills` and
`requiredEvidence` are overwritten from source. Change the plan, not the graph.

Two fields are carried across a regeneration, because they are the ones only you know:

<!-- fsgg-sdd:example corpus=tasks.yml mode=ref -->
```yaml
  - id: T001
    status: done        # yours: pending | inProgress | done | skipped
    owner: codex        # yours: the agent or role that took the task
```

The merge matches a derived task to a prior one by its **title**, and carries that
task's `status`, `owner`, its stable `T###`, and its still-live authored refs across.
A `status` of `stale` is never carried — it was a tool signal, not something you wrote.

Mark implemented tasks `done` when they are done. If this is the first point at which
`evidence.yml` is needed, do **not** move them backward to make the lifecycle advance:
run `fsgg-sdd evidence --work <id>` next. The evidence stage can scaffold the missing
`EV###` declarations from already-completed tasks without changing their authored status.
Those declarations begin `kind: missing` / `result: missing`, so verify remains blocked
until you author real evidence and, for verification passes, register an observed run.

A task you wrote yourself, matching nothing derived, is kept **verbatim** for as long
as it still references a requirement or decision that exists upstream. When that
disposition goes, the task is dropped as an orphan.

`docs/examples/lifecycle-artifacts/tasks.yml` shows a full generated graph. Read it
to learn the schema; there is nothing there to copy into your own file.

## Notes

- `requirements`/`decisions` must reference ids that exist in `spec.md` /
  `clarifications.md`; the work model resolves them into `LinkedTaskIds` and
  surfaces dangling references as diagnostics.
- `requiredEvidence` ids (`EV###`) are the obligations you will later satisfy in
  `evidence.yml` — see [[fs-gg-sdd-evidence]].
- `dependencies` define the order `analyze`/`verify` read as the task graph; keep
  them acyclic.

## Pitfalls

- Writing `tasks.md` instead of `tasks.yml` — the native SDD source is the typed
  YAML. (`tasks.md` is the legacy Spec Kit artifact.)
- Referencing an `FR-###` or `DEC-###` that was renumbered upstream — fix the spec
  ids first, then the tasks.
- Rolling a completed task back to `pending` only to create `evidence.yml` — keep the
  honest `done` status and run the evidence stage to bootstrap its declarations.

## Next

- `analyze` — check cross-artifact readiness, then implement: [[fs-gg-sdd-analyze]].

## Worked example

A complete, valid `tasks.yml` (with a matching `clarifications.md`, `checklist.md`,
and `evidence.yml` for one coherent work item) ships at
`docs/examples/lifecycle-artifacts/`. Every example there is validated against the
live parser on each build, so it never drifts from the tool.

## Related

- [[fs-gg-sdd-evidence]] (the `requiredEvidence` obligations), [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/quickstart.md`; the shipped worked example
  `docs/examples/lifecycle-artifacts/tasks.yml`.
