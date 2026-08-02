---
name: fs-gg-sdd-specify
description: Stage 2 of the FS.GG SDD lifecycle — fsgg-sdd specify drafts work/<id>/spec.md from three labeled --input facts (value, scope, requirement) and establishes the stable FR/AC/US ids the rest of the lifecycle references. Use to write the specification.
---

# Specify (stage 2)

`specify` drafts the specification — the source of truth for the plan, tasks,
evidence, and ship audit. It establishes the **stable ids** (`FR-###`, `AC-###`,
`US-###`) that every later stage references, so getting the ids right here matters
more than anywhere else.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/spec.md` is a complete,
worked `spec.md`, and it is machine-checked rather than illustrative: on every build the
skill↔gate doctest seeds it and drives the real gates (`charter` → `analyze`) over it,
failing if it does not come out clean. Where the prose below and the example disagree, the
example is the authority.

## Command

```text
fsgg-sdd specify --work <id> --input "<intent>"
```

## The `--input` intent grammar (gating)

`specify` drafts a spec **only** when `--input` supplies three labeled facts on
their own lines:

- `value:` — the user value (what the user can now do).
- `scope:` — the scope boundary.
- `requirement:` — a single measurable requirement.

An **unlabeled** line is read as the user value only; free prose therefore reports
`scope` and `measurable requirement` as **missing** and blocks. Supply all three:

```text
value: A player can rally against a second human at one keyboard.
scope: Two-player local volley; no AI opponent, no online play.
requirement: A rally of 20 consecutive volleys completes without a dropped frame.
```

## Produces / consumes

- **Consumes:** `work/<id>/charter.md`.
- **You author:** `work/<id>/spec.md`.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `clarify` ([[fs-gg-sdd-clarify]]).

## Required headings and id prefixes in `spec.md`

Sections: **User Value, Scope, Non-Goals, User Stories, Acceptance Scenarios,
Functional Requirements, Ambiguities, Public Or Tool-Facing Impact, Lifecycle
Notes.**

Id prefixes: `US-###` (user stories), `AC-###` (acceptance), `FR-###` (functional
requirements), `SB-###` (scope boundaries), `AMB-###` (ambiguities).

## Example: a functional-requirements block

Write requirements with MUST language and stable ids — these are the ids
`checklist`, `tasks`, and `evidence` will reference. Each `FR-###` is a **non-bold**
`- FR-###:` list item that carries its acceptance reference on the **same physical
line**.

`covers` is **decoration, not a magic token.** The gate scans the line for an `AC-###`
reference and nothing else — the word `covers` appears in no regex. So both of these
establish coverage, and you may write either:

```text
- FR-001: The system MUST serve toward the prior loser. (covers AC-001)
- FR-001: The system MUST serve toward the prior loser. (Stories: US-001; Acceptance: AC-001)
```

The second is the form `fsgg-sdd specify` scaffolds for you, so leaving a scaffolded line
as-is is correct. What actually breaks coverage is the *shape*: a bold `**FR-###**`, a
colon-less line, or an acceptance ref on its own line is *counted but not covered* —
`checklist` will report it as uncovered. The block below is the same coverage grammar the
`checklist` gate accepts (run verbatim through the gate by the skill↔gate doctest against
`docs/examples/lifecycle-artifacts/spec.md`):

<!-- fsgg-sdd:example corpus=spec.md mode=contains -->
```markdown
## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given a rally has just ended, when the next serve occurs, then the ball serves toward the player who lost the prior rally.
- AC-002 [US-001] [FR-002]: Given an ongoing match, when a point is scored, then the rally score updates and play continues without requiring a match-end condition.

## Functional Requirements
- FR-001: The system MUST serve the ball toward the player who lost the prior rally. (covers AC-001)
- FR-002: The system MUST record the rally score for each point without requiring a match-end condition. (covers AC-002)
```

> The `FR-###` ids here are referenced by the **checklist coverage line** later.
> Keep the numbering stable from here on — see [[fs-gg-sdd-checklist]] and
> [[fs-gg-sdd-authoring-contracts]].

## Pitfalls

- Free-prose `--input` with no `value:`/`scope:`/`requirement:` labels → blocked
  with scope/requirement missing.
- Renumbering FRs after `checklist`/`tasks` exist — the cross-references go stale.
- Putting HOW (implementation) in the spec. Spec is WHAT and WHY; HOW is `plan`.

## Next

- `clarify` — resolve the ambiguities you listed: [[fs-gg-sdd-clarify]].

## Related

- [[fs-gg-sdd-lifecycle]], [[fs-gg-sdd-authoring-contracts]].

## Sources

- `docs/reference/authoring-contracts.md` (`specify --input` grammar);
  `docs/quickstart.md`; the seeded `.fsgg/early-stage-guidance.md`.
