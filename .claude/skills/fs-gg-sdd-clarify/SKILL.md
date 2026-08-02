---
name: fs-gg-sdd-clarify
description: Stage 3 of the FS.GG SDD lifecycle — fsgg-sdd clarify records clarification questions, answers, and decisions (CQ/DEC/AMB ids) in work/<id>/clarifications.md, resolving spec ambiguities before checklist and plan. Use after specify.
---

# Clarify (stage 3)

`clarify` resolves the ambiguities the spec surfaced — turning open questions into
recorded **decisions** before they harden into plan and tasks. Skipping it leaves
ambiguity to be discovered (expensively) during implementation.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/clarifications.md` is a complete,
worked `clarifications.md`, and it is machine-checked rather than illustrative: on every build the
skill↔gate doctest seeds it and drives the real gates (`charter` → `analyze`) over it,
failing if it does not come out clean. Where the prose below and the example disagree, the
example is the authority.

## Command

```text
fsgg-sdd clarify --work <id>
```

## Produces / consumes

- **Consumes:** `work/<id>/spec.md`.
- **You author:** `work/<id>/clarifications.md`.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `checklist` ([[fs-gg-sdd-checklist]]).

## Required headings and id prefixes

Front matter (the four fields that **gate** the stage — `title`/`changeTier`/`status`
are defaulted if omitted): `schemaVersion`, `workId`, `stage: clarify`, `sourceSpec`.
See [[fs-gg-sdd-authoring-contracts]] for the full per-stage field table.

Sections: **Source Specification, Clarification Questions, Answers, Decisions,
Accepted Deferrals, Remaining Ambiguity, Lifecycle Notes.**

Id prefixes: `CQ-###` (clarification questions), `DEC-###` (decisions), `AMB-###`
(ambiguities, carried from the spec).

## How an ambiguity gets resolved (the load-bearing rule)

This is the grammar that blocks `clarify` most often, so read it before authoring.
A carried `AMB-###` is resolved only when **both** hold:

1. Its `AMB-###` id sits on a `DEC-###` line under `## Decisions` **or**
   `## Accepted Deferrals`. That decision/deferral line is what attaches the
   resolution. Author it as the canonical tagged form `[AMB:AMB-001]` (the tool
   writes this; the bracket is a convention — the parser only needs the bare
   `AMB-001` token on the line — but use the bracket form for clarity and parity).
2. That ambiguity is **not** left as a blocking bullet under `## Remaining
   Ambiguity`. Write a `None.`/disclaimer bullet there; a bullet that names an
   `AMB-###`/`CQ-###` id is read as still-unresolved (see the empty-section rule
   below).

**An answer alone does not resolve.** Keying an answer under `## Answers` by its
`CQ-###`/`AMB-###` id records the answer but does **not** clear the ambiguity —
resolution is carried by the decision **tag**, not the answer. If you only answer
and skip the tagged decision, `clarify` still reports *"missing answers for
blocking ambiguity."*

**Declare each `DEC-###` id exactly once.** A *declaration* is only a line under
`## Decisions` or `## Accepted Deferrals` whose **list-leading** id — the token right
after the `- ` bullet (bold `- **DEC-002**` is fine) — is that `DEC-###`. The two
sections are pooled, so declaring the same `DEC-002` at the leading position in **both**
raises `duplicateClarificationId`. Everything else is a safe *reference*: a `DEC-###`
mentioned in `## Answers`, `## Remaining Ambiguity`, or `## Lifecycle Notes`, **and** —
since FS.GG.SDD#647 — a `DEC-###` cited later in a decision's own prose, even inside
`## Decisions`/`## Accepted Deferrals`. So a hand-off note that cites a prior milestone's
decision — "… the deferral inherited from M2 DEC-004", exactly the citation the lifecycle
asks you to write — no longer counts as a second declaration of that id. Record an
accepted deferral as its own uniquely-id'd `DEC-###`, declared once at the leading
position, under `## Accepted Deferrals`.

## Example

<!-- fsgg-sdd:example corpus=clarifications.md mode=ref -->
```markdown
---
schemaVersion: 1
workId: 001-two-player-volley
stage: clarify
sourceSpec: work/001-two-player-volley/spec.md
---

# Clarifications

## Source Specification
- work/001-two-player-volley/spec.md

## Clarification Questions
- **CQ-001** (AMB-001): Does a serve after a point go to the loser or alternate?
- **CQ-002** (AMB-002): Is there a win condition, or is volley endless?

## Answers
- CQ-001 → serve goes to the player who lost the prior rally (resolves AMB-001).
- CQ-002 → endless volley for this work item; scoring tracked but no match end.

## Decisions
- **DEC-001** [CQ-001] [AMB:AMB-001] [FR-002] [AC-002]: The serve targets the prior-rally loser.

## Accepted Deferrals
- **DEC-002** [CQ-002] [AMB:AMB-002]: A match-end/win condition is deferred to a later work item — recorded, not dropped.

## Remaining Ambiguity
- None. AMB-001 and AMB-002 resolved above.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 001-two-player-volley`.
```

`DEC-001` carries `[AMB:AMB-001]` under `## Decisions`; the deferred `DEC-002`
carries `[AMB:AMB-002]` under `## Accepted Deferrals`; neither AMB is re-listed as a
blocking bullet under `## Remaining Ambiguity`. That is a complete, first-try-clean
`clarify`.

## Pitfalls

- Answering a question without recording a `DEC-###` — decisions are what later
  stages and the work model link to; an answer with no decision is not durable.
- Dropping an ambiguity instead of **deferring** it. Record the deferral; do not
  silently delete the open question.
- **`## Remaining Ambiguity` empty-section rule.** A bullet under this heading that
  names an `AMB-###`/`CQ-###` id is read as **still unresolved** and blocks
  `checklist`/`plan`. To say none remain, write a disclaimer bullet (`- None.`,
  `- No remaining ambiguities.`) — do **not** re-list the resolved ids
  (`- None. AMB-001…AMB-005 resolved.` is fine because it starts with `None`, but a
  bare `- AMB-001 resolved.` counts AMB-001 as blocking). Full grammar:
  `docs/reference/authoring-contracts.md`.

## Next

- `checklist` — verify requirements quality and coverage: [[fs-gg-sdd-checklist]].

## Related

- [[fs-gg-sdd-lifecycle]], [[fs-gg-sdd-specify]], [[fs-gg-sdd-authoring-contracts]].

## Sources

- `docs/reference/authoring-contracts.md` — the drift-guarded "Clarify decision-tag
  resolution" and "Per-stage front matter" grammars (every example there is run
  through the live parser on each build).
- `docs/quickstart.md`; the seeded `.fsgg/early-stage-guidance.md`.
