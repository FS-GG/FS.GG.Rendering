---
name: fs-gg-sdd-lifecycle
description: The map of the FS.GG spec-driven development (SDD) process — what it is, the canonical command order from charter to ship, the authored-source vs generated-view model, the doctrine, and how agents drive it with the fsgg-sdd CLI. Start here, then use the per-stage fs-gg-sdd-* skills.
---

# FS.GG SDD Lifecycle

This is the orientation skill for **spec-driven development in FS.GG**. Read it
first; it is the map. Each stage has its own skill (`fs-gg-sdd-charter`,
`fs-gg-sdd-specify`, …) with the exact command, inputs, outputs, and copyable
examples — this skill is how they fit together.

> **Driven by the `fsgg-sdd` CLI.** SDD is a CLI-driven lifecycle, not a set of
> prompts. Every stage is one `fsgg-sdd <command> --work <id>` invocation. If you
> have not created the project yet, start with [[fs-gg-sdd-getting-started]]
> (`fsgg-sdd init` / `fsgg-sdd scaffold`).

## What SDD is

A consumer uses `fsgg-sdd` to take a work item from intent to a shippable,
evidence-backed change, keeping **humans, the CLI, CI, and coding agents on one
machine contract**. Project intent becomes a *typed lifecycle model* that tools
and agents can trust: requirements have stable ids, plans and tasks reference
those ids, evidence is declared explicitly, generated views are currency-checked,
and an optional Governance layer can inspect the same artifacts without taking
over authoring.

The payoff is not ceremony. It is that "ship readiness" for a change is a
deterministic, inspectable fact rather than a judgement call.

## The canonical order (memorize this)

```
init → charter → specify → clarify → checklist → plan → tasks → analyze → [implement] → evidence → verify → ship
```

- `init` (or `scaffold`) seeds the skeleton once per project — see
  [[fs-gg-sdd-getting-started]].
- `[implement]` is the human/agent **coding step**, between `analyze` and
  `evidence`. It is a lifecycle *stage value* but **not** a `fsgg-sdd` command —
  there is no `fsgg-sdd implement`. You write the code, then declare evidence for
  it.
- Each command tells you the next action in its report, so you can always ask the
  tool "what's next" rather than memorizing.

## The stage table

For each stage: the command, the **hybrid source** under `work/<id>/` — yours to
author, but carrying regions the stage re-derives on every run — the **generated
readiness view** the *tool* writes under `readiness/<id>/`, and the next action.

| Stage | Command | Hybrid source (`work/<id>/`) | Tool refreshes/reports (`readiness/<id>/`) | Next |
|---|---|---|---|---|
| charter | `fsgg-sdd charter --work <id> --title "<title>"` | `charter.md` | `work-model.json` | specify |
| specify | `fsgg-sdd specify --work <id> --input "<intent>"` | `spec.md` | `work-model.json` | clarify |
| clarify | `fsgg-sdd clarify --work <id>` | `clarifications.md` | `work-model.json` | checklist |
| checklist | `fsgg-sdd checklist --work <id>` | `checklist.md` | `work-model.json` | plan |
| plan | `fsgg-sdd plan --work <id>` | `plan.md` (+ `contracts/`) | `work-model.json` | tasks |
| tasks | `fsgg-sdd tasks --work <id>` | `tasks.yml` | `work-model.json` | analyze |
| analyze | `fsgg-sdd analyze --work <id>` | — | `analysis.json` | implement, then evidence |
| evidence | `fsgg-sdd evidence --work <id>` | `evidence.yml` | `work-model.json` | verify |
| verify | `fsgg-sdd verify --work <id>` | — | `verify.json` | ship |
| ship | `fsgg-sdd ship --work <id>` | — | `ship.json` | Governance handoff (optional) |

`analyze`, `verify`, and `ship` write no `work/<id>/` source — they aggregate the
hybrid sources into a generated readiness view.

**None of the seven is yours alone.** Each stage re-derives the regions the tool
owns and preserves the regions you own; that merge is what makes the file safe to
rewrite. Text you put in a re-derived region does not survive the next run of that
stage. `docs/reference/authoring-contracts.md` names the regions per stage, and
`docs/reference/artifact-taxonomy.md` classifies them. The one strictly authored
thing here is `work/<id>/contracts/…`, which the tool never writes.

> **When the work model is built.** Each stage *refreshes or reports* the
> `work-model.json` status, but the **normalized `work-model.json` is only built
> once enough sources exist — at `verify`/`ship`**. During the early window
> (`charter`/`specify`/`clarify`/`checklist`) it does not exist yet, which is why
> `agents`/`refresh` fall back to `.fsgg/early-stage-guidance.md` there (see "Two
> windows" below and [[fs-gg-sdd-refresh-agents]]).

## The one distinction everything hangs on: authored source vs generated view

- **Authored sources** live under `work/<id>/` and are **yours**. Markdown
  (`charter.md`, `spec.md`, …) is the human authoring surface; `tasks.yml` and
  `evidence.yml` are typed.
- **Generated views** live under `readiness/<id>/` (`work-model.json`,
  `analysis.json`, `verify.json`, `ship.json`, `summary.md`,
  `agent-commands/<target>/`) and are **outputs** the tool produces.

**Presence is not currency.** A generated view that exists on disk but has not
been refreshed against its current sources is *stale*. Currency comes from
re-running the generators ([[fs-gg-sdd-refresh-agents]]), not from a file being
present. Each generated view records its sources, their sha256 digests, schema
version, and generator identity, so staleness is detectable.

## Two gating grammars will block you if you get them subtly wrong

These are the load-bearing authoring contracts. The full reference is
[[fs-gg-sdd-authoring-contracts]]; in short:

- **Checklist coverage:** a requirement is *covered* only when a list item leads
  with `- FR-###:` and carries its acceptance reference **on the same physical
  line**: `- FR-001: W/S move the left paddle. (covers AC-002)`. A bold
  `**FR-001**`, a missing colon, or a separate-line `(covers AC-###)` — including a
  **soft-wrapped** bullet whose marker wrapped to the next line — is **counted but
  uncovered**. See [[fs-gg-sdd-checklist]].
- **Evidence satisfaction:** an obligation is satisfied **only** by a matching
  `evidence.yml` declaration with `result: pass` **and** `synthetic: false`. A
  synthetic pass discloses a stand-in and does **not** satisfy; a deferral does
  not satisfy. See [[fs-gg-sdd-evidence]].

> **Deferral closure — a second sense of "deferral" the lifecycle does NOT gate
> for you.** The evidence rule above is *within* a work item. A `DEC` /
> clarification that defers a seam to a **later milestone** ("input routing
> deferred to M4", "persistence launch deferred to M6") is different: it hands
> real work forward, and nothing automatically re-surfaces it when that milestone
> arrives. So close the loop by hand:
> - **When you defer to milestone Mx, that milestone's `charter` must list picking
>   it up.** A deferral with no obligation recorded on its target is a dropped
>   thread — the deferring stage is honest, but the receiving stage never hears of
>   it.
> - **When Mx ships, verify every deferral aimed at Mx was discharged**, not just
>   that Mx's own new ACs are green.
> - **A deferral whose target milestone is complete but whose work is not is a
>   smell** — it is the exact shape by which a seam that was *stood up but never
>   wired* reaches ship AC-green: each milestone's checklist passes while the
>   handed-forward wiring quietly never lands. Treat an open deferral pointing at
>   an already-shipped target as a blocking finding, not a note.

## How gates work

A single prerequisite cascade builds facts in strict order
(`specification → clarification → checklist → plan → tasks`): each downstream
stage is computed only if every upstream stage produced facts, so a
missing/malformed upstream artifact starves the later stages. Inside each
command, any **error-severity diagnostic suppresses all writes** — the stage
produces nothing and reports `Blocked`. Stage outcomes are
`Succeeded · SucceededWithWarnings · Blocked · NoChange`; exit code is `0` for
the first three of those, `1` when blocked.

When you edit an upstream artifact after a downstream stage has run, that stage's
recorded source digest goes **stale**, and you must re-run the affected stages to
reconcile it. You no longer have to guess the order: a stale-digest `nextAction`
(`plan.acceptUpstream`, `tasks.correctStaleTasks`) now **names the ordered re-run
set** in its `reason` — the stale stage plus each downstream stage that has already
run, e.g. `re-run the recorded downstream stages in order: tasks, then evidence`.
Stages you never ran carry no digest to stale and are not named.

## Output formats (every command)

Every command projects the same `CommandReport` three ways, precedence
`--rich` > `--text` > `--json` > default:

- default / `--json` — the deterministic JSON automation contract. **Agents and
  CI should use this.**
- `--text` — portable plain-text summary. **This is your best diagnostic:** when a
  stage blocks or reports `noChange` for a reason the JSON `outcome`/`diagnostics`
  don't spell out, re-run it with `--text` and read the summary counters
  (`blockingAmbiguities`, `checklistFailedBlocking`, …). See [[fs-gg-sdd-troubleshooting]].
- `--rich` — human Spectre.Console panels/tables/color; a pure projection that
  changes no JSON byte, stream, or exit code, and degrades to zero-ANSI plain
  text when non-interactive or color is disabled (`NO_COLOR`, `TERM=dumb`).

## How agents fit (read this if you are an agent)

- **Agents author; they do not become a second source of truth** (Principle VII).
  You may write the authoring surfaces (`spec.md`, `tasks.yml`, …), but the typed
  work model and generated views are the contract. If you write a surface, refresh
  the views or they report stale.
- **Generated guidance is not authority.** The per-work guidance under
  `readiness/<id>/agent-commands/<target>/` (`commands.md`, `skills.md`) is a
  *projection* of the work model, marked generated — never a source of truth
  (`agents.yml` policy `generatedGuidanceIsAuthority: false`).
- **Claude and Codex stay equivalent.** `requireEquivalentClaudeAndCodexBehavior:
  true` — when you change workflow behavior, keep both agents' guidance aligned.
- **Before the work model exists** (the `charter`/`specify`/`clarify`/`checklist`
  window), there is no `work-model.json` yet, so author from the seeded
  `.fsgg/early-stage-guidance.md`. After `verify`/`ship` build the work model, the
  generated `agent-commands/<target>/` views become the live guidance.

## Doctrine (the principles the process enforces)

The scaffolded `.fsgg/constitution.md` is the highest-precedence engineering
authority in a product. Core principles:

1. **Specify before implementing** — every non-trivial change starts from a
   written spec (outcome, scope, tier, surface impact, how it's verified).
2. **Structured artifacts are the machine contract** — Markdown authors; typed,
   schema-versioned artifacts are what tools rely on; when prose and structured
   data disagree, the plan declares which wins and which view records it.
3. **Public surface is declared, not incidental** — declare signatures; keep a
   surface baseline; a contracted change updates signatures, baselines, tests, and
   docs together.
4. **Idiomatic simplicity is the default.**
5. **Model–Update–Effect is the boundary for state and I/O** — pure transitions,
   I/O at the edge.
6. **Test evidence is mandatory** — behavior-changing code ships with tests that
   fail before and pass after; prefer real fixtures over mocks; disclose synthetic
   stand-ins.
7. **Agents and humans share one contract** — agent guidance is generated from the
   same lifecycle contract, never a second source of truth.
8. **Observability and safe failure** — actionable diagnostics; distinguish
   malformed input from tool defects; critical paths fail fast, optional
   integrations degrade explicitly.

> **Regression scope: a behavior-changing feature can invalidate *existing*
> tests, not only demand new ones (principle 6).** Watch especially for a change
> that adds a **terminal transition** — death, timeout, defeat, victory, any first
> way for a long-running process to *end*. Earlier tests written when the process
> never terminated may assume it stays alive (asserting on state that is only
> populated while it runs, driving a passive actor over many steps), and the new
> terminal path can make that assumption false — surfacing as a crash or a
> null-reference rather than a clear failure. When your change introduces a first
> terminal transition, re-run the **whole** suite and expect to update the
> long-running/integration tests that assumed non-termination, not just to add
> tests for the new behavior.

Every change also declares a **tier**: Tier 1 (contracted — public surface,
schema, command, artifact layout, integration; needs spec+plan+tasks+signatures+
tests+docs) or Tier 2 (internal cleanup — needs spec+tests, baselines unchanged).

## SDD is useful without Governance

You can run the entire lifecycle — `init` through `ship` — with **no Governance
runtime installed**. `ship` aggregates SDD-owned merge-boundary readiness and
*points* ship-ready work at the optional, Governance-owned protected-boundary
handoff; SDD never evaluates or enforces it. Adopting Governance is additive. See
[[fs-gg-sdd-ship]].

## Migrating from legacy Spec Kit

If you have an existing Spec Kit project (`specs/<feature>/` + `.specify/`), SDD
adoption is additive and non-destructive — there is no migration command, you
just run `fsgg-sdd init` and author the native `work/<id>/` sources through the
ordinary lifecycle commands. Legacy Spec Kit keeps working. Details:
`docs/migration-from-spec-kit.md`. New work should use the `fsgg-sdd` lifecycle
below; legacy `speckit-*` commands are not the active process.

## The per-stage skills

| Stage | Skill |
|---|---|
| getting started | [[fs-gg-sdd-getting-started]] (`init`, `scaffold`) |
| charter | [[fs-gg-sdd-charter]] |
| specify | [[fs-gg-sdd-specify]] |
| clarify | [[fs-gg-sdd-clarify]] |
| checklist | [[fs-gg-sdd-checklist]] |
| plan | [[fs-gg-sdd-plan]] |
| tasks | [[fs-gg-sdd-tasks]] |
| analyze | [[fs-gg-sdd-analyze]] |
| evidence | [[fs-gg-sdd-evidence]] |
| verify | [[fs-gg-sdd-verify]] |
| ship | [[fs-gg-sdd-ship]] |
| generators | [[fs-gg-sdd-refresh-agents]] (`refresh`, `agents`) |
| validation | [[fs-gg-sdd-validate]] |
| grammar reference | [[fs-gg-sdd-authoring-contracts]] |
| troubleshooting | [[fs-gg-sdd-troubleshooting]] (a stage blocks and you can't tell why) |

## Sources

- `docs/quickstart.md` — the canonical end-to-end walkthrough.
- `docs/reference/authoring-contracts.md` — the load-bearing grammars.
- `.fsgg/constitution.md` (seeded by `init`) — the product doctrine.
- `docs/migration-from-spec-kit.md` — legacy Spec Kit coexistence.
