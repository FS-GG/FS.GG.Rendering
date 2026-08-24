---
name: fs-gg-sdd-charter
description: Stage 1 of the FS.GG SDD lifecycle — fsgg-sdd charter establishes a work item's identity (id, title, scope boundaries, policy pointers) in work/<id>/charter.md before any spec exists. Use to start a new work item.
---

# Charter (stage 1)

`charter` establishes the **identity** of a work item — the first thing you do for
any new piece of work, before specifying anything. It is part of the
pre-work-model window (no `work-model.json` exists yet), so author from this skill
and the seeded `.fsgg/early-stage-guidance.md`.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/charter.md` is a complete,
worked `charter.md`, and it is machine-checked rather than illustrative: on every build the
skill↔gate doctest seeds it and drives the real gates (`charter` → `analyze`) over it,
failing if it does not come out clean. Where the prose below and the example disagree, the
example is the authority.

## Command

```text
fsgg-sdd charter --work <id> --title "<title>"
```

- `--work <id>` — the work item id, e.g. `001-two-player-volley`.
- `--title "<text>"` — the human-readable title.

## Produces / consumes

- **You author:** `work/<id>/charter.md`.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `specify` ([[fs-gg-sdd-specify]]).

## Required headings in `charter.md`

Author these sections (the early-stage guidance pins them):

- **Identity** — what this work item is, in one or two sentences.
- **Principles** — the governing constraints specific to this work.
- **Scope Boundaries** — what is in and explicitly out.
- **Policy Pointers** — references to constitution principles or governance this
  work must honor.
- **Lifecycle Notes** — anything downstream stages need to know.

Charter carries **no scoped ids** (those start at `specify`). It uses front-matter
facts instead: `workId`, `stage`, `changeTier`, `status`.

## Example

<!-- fsgg-sdd:example corpus=charter.md mode=ref -->
```markdown
---
workId: 001-two-player-volley
stage: charter
changeTier: 1
status: active
---

# Charter

## Identity
A two-player local Pong volley mode: two humans rally at one keyboard.

## Principles
Deterministic frame stepping; no AI opponent; input latency is the headline metric.

## Scope Boundaries
In: local two-player volley, scoring, serve. Out: online play, AI, tournaments.

## Policy Pointers
Honors constitution I (specify-before-implement) and VI (test evidence mandatory).

## Lifecycle Notes
Tier 1 change: introduces a public input-mapping surface, so signatures + tests
land together.
```

## Pitfalls

- Picking a `--work` id you will not keep — the id threads through every later
  artifact and `readiness/<id>/` path. Choose `NNN-kebab-title` and keep it.
- Treating charter as the spec. Charter is *identity and boundaries*; the
  measurable requirements come in `specify`.

## Next

- `specify` — turn this identity into a specification: [[fs-gg-sdd-specify]].

## Related

- [[fs-gg-sdd-lifecycle]] — the full process map.

## Sources

- `docs/quickstart.md` (stage table); the seeded `.fsgg/early-stage-guidance.md`.
