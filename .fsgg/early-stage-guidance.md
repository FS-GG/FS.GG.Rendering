# Early-Stage Authoring Guidance

This file is seeded by the SDD skeleton. It gives an author (or their coding agent)
self-contained guidance for the **pre-work-model** lifecycle stages — `charter`,
`specify`, `clarify`, and `checklist` — the stages that run **before**
`readiness/<id>/work-model.json` exists. Until that work model is built (by
`fsgg-sdd verify` or `fsgg-sdd ship`), `fsgg-sdd agents` and `fsgg-sdd refresh` cannot
generate per-work-item agent guidance, so this static guidance is what you author from.

It is an authoring surface, not a machine contract. The authoritative definitions live
in the SDD tool; this file restates them so you never have to read or decompile the CLI.

## Lifecycle order

The pre-work-model stages run in this order:

`fsgg-sdd charter` → `fsgg-sdd specify` → `fsgg-sdd clarify` → `fsgg-sdd checklist`

Each stage authors one Markdown artifact under `work/<id>/`. Author the required section
headings (verbatim, as `## <heading>`) and use the stable-id formats listed below.

## `fsgg-sdd charter`

Authors `work/<id>/charter.md`. Required section headings:

```text headings:charter
Identity
Principles
Scope Boundaries
Policy Pointers
Lifecycle Notes
```

Stable ids: the charter declares no scoped ids. It carries front-matter fields
(`workId`, `stage`, `changeTier`, `status`).

## `fsgg-sdd specify`

Authors `work/<id>/spec.md`. Required section headings:

```text headings:specify
User Value
Scope
Non-Goals
User Stories
Acceptance Scenarios
Functional Requirements
Ambiguities
Public Or Tool-Facing Impact
Lifecycle Notes
```

Stable-id formats (each is `PREFIX-` followed by three or more digits, e.g. `FR-001`):

```text ids:specify
US
AC
FR
SB
AMB
```

## `fsgg-sdd clarify`

Authors `work/<id>/clarifications.md`. Required section headings:

```text headings:clarify
Source Specification
Clarification Questions
Answers
Decisions
Accepted Deferrals
Remaining Ambiguity
Lifecycle Notes
```

Stable-id formats (each is `PREFIX-` followed by three or more digits):

```text ids:clarify
CQ
DEC
AMB
```

## `fsgg-sdd checklist`

Authors `work/<id>/checklist.md`. Required section headings:

```text headings:checklist
Source Specification
Source Clarifications
Source Snapshot
Checklist Items
Review Results
Accepted Deferrals
Blocking Findings
Advisory Notes
Lifecycle Notes
```

Stable-id formats (each is `PREFIX-` followed by three or more digits):

```text ids:checklist
CHK
CR
```

## §1.1 Acceptance coverage line

`fsgg-sdd checklist` marks a functional requirement **covered** only when a strict-scan
parser finds a list item that leads with `- FR-###:` and carries an acceptance reference
(`AC-###`) **on the same physical line**:

- the item starts with a literal `- `, then the id, then a literal `:`;
- the requirement id is `FR-` followed by three or more digits (case-insensitive);
- the acceptance reference(s) sit on that same physical line;
- there is prose after the colon.

A bold id (`**FR-001**`), a colon-less id (`- FR-001 — …`), or an acceptance reference on
a different line does **not** establish coverage. The scan reads a single physical line,
so a **soft-wrapped** requirement bullet whose `(covers AC-###)` marker wrapped onto the
next line is reported uncovered — keep the whole `- FR-###: … (covers AC-###)` on one
physical line. And fix a "missing acceptance coverage" verdict in `spec.md`, never in the
regenerated `checklist.md`.

The word `covers` is **decoration, not a magic token** — the scan looks for an `AC-###` on
the line and nothing else. How you word the reference is up to you. Both of these
establish coverage, and the second is the form `fsgg-sdd specify` scaffolds for you, so a
scaffolded line is already correct as written:

```text coverage:accepted
- FR-001: The system records one outcome per request. (covers AC-001)
- FR-002: The system records one outcome per request. (Stories: US-001; Acceptance: AC-002)
```

## §1.2 `evidence.yml` satisfaction

Each entry under `evidence:` declares a `kind` and a `result`. An obligation is
**satisfied** only by a matching declaration whose `result` is `pass` **and** whose
`synthetic` is `false`.

- `synthetic: true` with `result: pass` discloses a stand-in and does **not** satisfy.
- `result: deferred` (or `kind: deferral`) is an accepted deferral, not a satisfaction.
- `result: fail`, `missing`, `stale`, or `blocked` does not satisfy.

Copyable declaration that satisfies its obligation:

```yaml evidence:satisfied
schemaVersion: 1
evidence:
  - id: EV001
    kind: verification
    subject:
      type: task
      id: T001
    result: pass
    synthetic: false
```

## Once the work model exists

After `fsgg-sdd verify` or `fsgg-sdd ship` builds `readiness/<id>/work-model.json`, the
generated per-work-item views under `readiness/<id>/agent-commands/<target>/` become the
authoritative agent guidance. This static early-stage guidance covers only the
pre-work-model window; it does not shadow or duplicate those generated views.

For the full authoring contracts, see `docs/reference/authoring-contracts.md`.
