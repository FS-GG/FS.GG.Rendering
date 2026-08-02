---
name: fs-gg-sdd-plan
description: Stage 5 of the FS.GG SDD lifecycle — fsgg-sdd plan authors the technical implementation plan (work/<id>/plan.md, plus work/<id>/contracts/ when contracts are authored) from the covered spec. Use after checklist, before tasks.
---

# Plan (stage 5)

`plan` is where WHAT becomes HOW. With a covered, clarified spec in hand, you
author the technical approach: the design, the public surface to declare, the
schema/migration posture, the structured contracts, and the tests that will cover
the change. Per the doctrine, signatures come before implementation — the plan
names the surface you will declare.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/plan.md` is a complete,
worked `plan.md` — the view the real `plan` gate produces from the corpus sources, regenerated and
checked clean on every build by the skill↔gate doctest. Where the prose below and the
example disagree, the example is the authority.

## Command

```text
fsgg-sdd plan --work <id>
fsgg-sdd plan --work <id> --accept-upstream   # re-baseline after an upstream edit
```

`plan` records the digests of `spec.md`/`clarifications.md`/`checklist.md` in its own
`## Source Snapshot`. Editing any of them afterwards makes the plan stale: a bare re-run
then **blocks** with `stalePlanSnapshot` and writes nothing (it never edits your prose).
Review the recorded `PD-###` decisions against the change, then re-run with
`--accept-upstream` to re-baseline the snapshot. See [[fs-gg-sdd-troubleshooting]].

## A clean `plan` run is not an authored plan

`plan` seeds a scaffold — one `PD`/`PC`/`VO`/`PM`/`GV` line per requirement — and its
success predicate is **structural**: it checks that the sections are present and the ids
line up, which the scaffold satisfies by construction. It does **not** check that you
replaced the seeded prose. So a run that reports `outcome: succeeded` / `coherent: true` /
`planBlockingFindings: 0` can still carry the generator's stub sentences verbatim —
`- PD-001 [FR-001] complete: Plan requirement FR-001 through the plan command contract.`,
`- VO-001 [PD-001] test: Run focused command tests before tasks.`, and the rest.

The un-authored-content gate lives **downstream at `analyze`** (`unauthoredScaffoldContent`),
which blocks until the stub prose is rewritten. So **rewrite every `PD`/`PC`/`VO`/`PM`/`GV`
line into the real plan before you run `analyze`** — leaving the scaffold prose in place does
not fail `plan`; it fails `analyze` two stages later and costs a full plan→tasks→analyze
re-run. The scan covers every scaffolded **requirement** decision plus the `PC`/`VO`/`PM`/`GV`
rows **and** the bottom `## Accepted Deferrals` section — a deferral left as the seeded
`Deferral remains visible to tasks and evidence.` blocks exactly like an un-rewritten decision.
It compares each entry's body verbatim against what the scaffold would have written, and
`analyze` names the offending ids in the diagnostic message, so you no longer have to grep
`plan.md` for the stub phrases.

**One exception (FS.GG.SDD#649):** the auto-generated **pure deferral-mirror** `PD-###` decision
— `- PD-### [DEC-###] acceptedDeferral: Accepted deferral DEC-### remains visible to task
generation.` — is left verbatim on purpose. It only restates an accepted deferral you already
authored upstream, so it is **exempt** from the un-authored-content scan, and `tasks` folds it
into that deferral's keep-visible obligation rather than deriving a second one. Change its prose
only if you mean it as a real design decision — then it is no longer a mirror, and it (correctly)
earns its own task and its own `analyze` authoring obligation again.
See [[fs-gg-sdd-analyze]] and [[fs-gg-sdd-troubleshooting]].

## Produces / consumes

- **Consumes:** `spec.md` + `clarifications.md` + `checklist.md` (the upstream
  cascade — a missing/malformed upstream starves this stage).
- **You author:** `work/<id>/plan.md`, and `work/<id>/contracts/` when you author
  interface/schema contracts.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `tasks` ([[fs-gg-sdd-tasks]]).

## What a plan covers

For a lifecycle/contracted (Tier 1) change, the plan identifies:

- the **public surface** to declare (signatures first — `.fsi` where the language
  supports it) and the surface baseline to maintain;
- the **structured contracts** (under `contracts/`) and their schema/migration
  posture — and, when the change builds against a framework package's API, cite it
  resolvably on a `## Contract Impact` line as `framework: <PackageId>[@<version>]#<symbol>`
  (or, when deferring because you believe the API is absent, as
  `blocked-on-framework:` on an `## Accepted Deferrals` line). `analyze` resolves the
  reference against the pinned package's committed captured surface and blocks a dangling
  use or a contradicted deferral at plan time — see [[fs-gg-sdd-authoring-contracts]] §6;
- the **generated views** the change touches and their currency/stale behavior;
- the **agent-facing behavior**, keeping Claude and Codex equivalent;
- any optional **Governance** integration;
- the **tests and fixtures** that cover stale or conflicting artifacts — prefer
  real fixtures over mocks.

A Tier 2 (internal) change needs a plan and tests, but signatures and baselines
stay unchanged.

## Example skeleton

<!-- fsgg-sdd:example corpus=plan.md mode=ref -->
```markdown
# Plan

## Technical Context
F# / Elmish-style command loop. Input mapping is a new public surface (Tier 1).

## Constitution Check
III Public Surface: declare `InputMap.fsi` before `.fs`. V MUE: key events are
messages; paddle motion is a pure transition; rendering is the edge effect.

## Design
- Pure `update : Msg -> Model -> Model * Effect list`.
- `contracts/input-map.md` defines the W/S → paddle-delta mapping.

## Plan Decisions
- PD-001 [FR-001] [AC-001] complete: Map W/S to paddle deltas through `InputMap`.
- PD-002 [DEC-003] complete: Key repeat is a held-key model flag, not an event
  stream — implements the resolved clarify decision.
- PD-003 [AC-007] complete: Dispose the acceptance scenario no requirement
  references.

## Tests
- Real-fixture transition tests (fail-before/pass-after) for FR-001, FR-002.
```

Every id you tag on a `## Plan Decisions` line becomes a `sourceIds` entry on the
task `tasks` derives for that line. That is how a lifecycle fact acquires a **task
disposition**, and the tag is id-class agnostic (`FR`/`AC`/`DEC`/…).

Most required facts already have a generator of their own — a resolved `DEC-###`
gets a disposing task straight from `clarifications.md`, so `PD-002`'s tag above is
**traceability**, not disposition. The tag is load-bearing exactly when a fact has
no generator: `PD-003` disposes `AC-007`, an acceptance scenario no requirement
references, which otherwise blocks `tasks` with `missingDisposition`.

## Structure is read from the declaration position, not from prose

Two things `tasks`/`analyze` read off a `## Plan Decisions` line are anchored to the
**declaration position**, so the descriptive prose you write after the colon is free text —
naming an id or a status word there no longer changes how the line is classified:

- **The status marker.** A decision's status is the `<status>:` keyword sitting between the
  id (with any `[…]` bracket tags) and the **first colon** — `- PD-001 [FR-001] complete: …`
  is `complete`. It is matched **exactly** against the known keywords
  (`complete`/`planned`, `stale`/`needs review`, `incomplete`, `advisory`), never as a
  contained word. A status word that merely appears in the description — "… so a **stale**
  prior frame is never re-fired." — no longer sets the status. Writing "stale" in a
  decision's prose therefore does **not** trip `stalePlanDecision` (which otherwise blocks
  `tasks` with `failedPlanPrerequisite: Plan contains stale decisions`); only a decision
  authored `stale:`/`needs review:` at the marker position counts as stale (FS.GG.SDD#653).
- **References.** Plan references resolve from the `[…]` **bracket-tag** citation grammar
  only (`[FR-002]`, `[DEC-006]`) — the same tags that carry a task disposition. An id merely
  named in a decision's prose — "… extending the SB-008 seam", "… inherited from M2 DEC-006"
  — is a **citation, not a reference**, so it no longer trips `Plan reference '<id>' does not
  resolve` or blocks `tasks`/`analyze`. Tag an id in `[…]` when you mean it to resolve and
  carry a disposition; leave it bare in prose when you only mean to cite an upstream id
  (FS.GG.SDD#648).

## Pitfalls

- Planning implementation without declaring the surface first (violates Principle
  III). Name the signatures in the plan.
- Authoring `contracts/` content that contradicts the spec — the work model
  surfaces prose-vs-structured conflicts as diagnostics rather than merging them.
- Answering `missingDisposition` by editing `tasks.yml` or re-running `tasks`.
  Neither works: `tasks` regenerates `tasks.yml` from the authored sources. Tag
  the stranded id on a `PD-###` line here instead (or, for an orphan `AC-###`,
  reference it from a `spec.md` requirement).
- Treating a clean `plan` run as a finished plan. Success is structural and does not
  detect un-edited scaffold prose; the `unauthoredScaffoldContent` gate is at `analyze`.
  Rewrite the `PD`/`PC`/`VO`/`PM`/`GV` lines before `analyze` (see "A clean `plan` run is
  not an authored plan" above).

## Next

- `tasks` — break the plan into a typed task graph: [[fs-gg-sdd-tasks]].

## Related

- [[fs-gg-sdd-lifecycle]], [[fs-gg-sdd-tasks]].

## Sources

- `docs/quickstart.md`; `.fsgg/constitution.md` (Principles III, V, VI).
