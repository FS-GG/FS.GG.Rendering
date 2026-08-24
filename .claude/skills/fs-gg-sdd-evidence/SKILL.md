---
name: fs-gg-sdd-evidence
description: Stage 8 of the FS.GG SDD lifecycle — fsgg-sdd evidence authors work/<id>/evidence.yml declaring how each obligation is satisfied. Carries the load-bearing satisfaction rule (only result:pass AND synthetic:false satisfies) and the kind/result vocabularies. Use after implementing, before verify.
---

# Evidence (stage 8)

`evidence` is where you declare what **proves** each obligation your tasks created
— after the code and tests exist. It is one of the two load-bearing authoring
contracts: a subtly wrong declaration leaves an obligation unsatisfied even though
the work is done.

**Read the worked example first.** `docs/examples/lifecycle-artifacts/evidence.yml` is a
complete, worked `evidence.yml`, machine-checked rather than illustrative: it is
parser-validated on every build, and the deferral rule it demonstrates is pinned directly by
the evidence gate tests. (It sits outside the `charter` → `analyze` gate chain.) Where the
prose below and the example disagree, the example is the authority.

## Command

```text
fsgg-sdd evidence --work <id>
fsgg-sdd evidence --work <id> --from-tests <path>        # pre-map new obligations to a proving test file
fsgg-sdd evidence --work <id> --from-test-report <trx>   # register an observed-run receipt (verify needs it)
fsgg-sdd evidence --work <id> --sync-observed-run <trx>  # re-stamp existing receipts after the TRX is regenerated
```

`--from-tests` and `--from-test-report` are **different flags** — a source *pointer*
versus an observed-run *receipt* — and one does not do the other's job. See "Satisfied
here is not observed at verify" below.

You may run the first command after implementation tasks are already marked `done`, even
when `evidence.yml` does not exist. It scaffolds their stable `EV###` declarations without
rewriting task status. The scaffold is deliberately unsatisfied (`kind: missing`,
`result: missing`): author it, run the suite, then use `--from-test-report` for the receipt.

## Produces / consumes

- **Consumes:** `readiness/<id>/analysis.json` and the task obligations.
- **You author:** `work/<id>/evidence.yml`.
- **Tool refreshes:** `readiness/<id>/work-model.json`.
- **Next:** `verify` ([[fs-gg-sdd-verify]]).

## Auto-scaffolded obligations carry their origin refs

Each scaffolded obligation already records the `requirementRefs` and `planDecisionRefs` it
descends from — routed from the originating task's source lineage (so a `PD-###` plan-decision
obligation carries its `PD-###` **and** the `FR-###` it traces to). You can classify each
obligation honestly from its `evidence.yml` entry alone, with **no join back to `tasks.yml` by
task title**. (Other ref buckets stay empty on scaffolds; add them by hand if you want.)

Each ref bucket holds **one id class**, keyed by prefix — put an id in the list its prefix names:

| id prefix   | ref field                  |
|-------------|----------------------------|
| `T###`      | `taskRefs`                 |
| `FR-###`    | `requirementRefs`          |
| `AC-###`    | `acceptanceScenarioRefs`   |
| `DEC-###`   | `clarificationDecisionRefs`|
| `CR-###`    | `checklistResultRefs`      |
| `PD-###`    | `planDecisionRefs`         |

A generated `tasks.yml` lists a task's `sourceIds` together (e.g. `[CR-008, PD-010]`), so it is
easy to copy a `CR-###` checklist-result id into `clarificationDecisionRefs` by mistake.
`evidence` names that specifically — `Reference 'CR-008' is a checklist-result id; put it in
checklistResultRefs, not clarificationDecisionRefs` — rather than reporting a well-formed id as
malformed. Move the ref to the field its prefix names.

`--from-tests <path>` additionally seeds each **newly scaffolded** obligation with a
verification-kind source pointing at `<path>` (a declared pointer; its existence is checked at
`verify`). It is inert without the flag. Neither behavior overwrites an obligation you have
already authored (no-clobber).

## The satisfaction rule (load-bearing)

> An obligation is **satisfied** only by a matching declaration whose `result` is
> `pass` **and** whose `synthetic` is `false`.

- `synthetic: true` + `result: pass` → disposition **synthetic**: discloses a
  stand-in, does **not** satisfy.
- `result: deferred` (or `kind: deferral`) → an accepted **deferral**, not a
  satisfaction.
- `result: fail | missing | stale | blocked` → not satisfied.

That rule is what **`evidence`** checks — necessary and sufficient *here*, and clearing
it earns `evidenceReady`. It is **not** sufficient at **`verify`**: an obligation you
satisfy with a real pass must additionally be *observed*. Read the next section before
you author a run of `pass`/`false` and assume `verify` will be green.

## Satisfied here is not observed at verify (the observed-run receipt)

`evidenceReady` is not `verifyReady`. The satisfaction rule above (`result: pass ∧
synthetic: false`) is *necessary but not sufficient* at `verify`: an obligation
satisfied by a real pass must **also carry an `observedRun` receipt** — proof that a
suite actually ran — or `verify` blocks with **`verify.unobservedRequiredTest`**. This
gate is **on by default** (the ADR-0035 stage-3b flip, since 0.14.0); `--no-require-observed`
is the migration-window opt-out, not the normal path.

So the common failure is real: author every obligation to `pass`/`false`, get
`evidenceReady`, then watch `verify` block on obligations you thought were done. The
satisfaction rule is where that pass is *declared*; the observed-run receipt is what
makes it *count* at the merge boundary.

**Earn the receipt.** Run your suite to a machine-readable report (a `.trx`), then
register it — a *separate* `evidence` invocation:

```text
fsgg-sdd evidence --work <id> --from-test-report <path/to/report.trx>
```

It parses the report and stamps an `observedRun` onto every **`verification`-kind**
declaration that claims a real pass (idempotent — re-running rewrites the same bytes).
A report whose run **failed** blocks with `evidence.observedRunFailed` rather than
becoming a silent receipt; a report that is missing or unparseable blocks too. Nothing
degrades to "no receipt".

**It enriches typed obligations; it does not bootstrap the scaffold.** The receipt only
lands on an obligation **already typed** `kind: verification` that claims a pass. A
freshly-scaffolded obligation starts `kind: missing` / `result: missing`, so on the
initial scaffold `--from-test-report` attaches **nothing** — author each obligation's
`kind`/`result` first. Handed a passing report over an all-missing obligation set, the
report shows a high `evidenceBlocking` count with no receipt attached; that means "the
obligations aren't typed yet", **not** "the tool didn't see your tests". `evidence` says
so with a non-blocking `evidence.testReportUntypedObligations` advisory naming how many
obligations are still untyped.

**`--from-test-report` is not `--from-tests`.** They look alike and do opposite jobs:

| flag | what it does | when |
| --- | --- | --- |
| `--from-tests <path>` | seeds a verification-kind **source pointer** onto *newly scaffolded* obligations (a declared path, existence checked at `verify`) | pre-mapping a fresh obligation set |
| `--from-test-report <trx>` | registers an observed-run **receipt** from a report SDD reads, parses, and hashes | after the suite has run, so `verify` passes |

Pointing at where tests *live* is not the same as proving they *ran*. `--from-tests`
alone never makes an obligation observed.

**Keep receipts honest when the TRX is regenerated: `--sync-observed-run <trx>`.** A
receipt pins each obligation to a *specific* report — its `digest` and its
`passed`/`failed`/`skipped` counts. When you regenerate that report (a test added late in
the cycle, a re-run), every already-authored receipt goes **stale**: the digest no longer
matches the bytes, and the counts no longer match the run. `--sync-observed-run` reconciles
them in place — it recomputes the digest and re-reads the counts for **every obligation
already carrying a receipt sourced from `<trx>`**, and only those:

```text
fsgg-sdd evidence --work <id> --sync-observed-run <path/to/report.trx>
```

It is the **maintenance** complement to `--from-test-report`'s **bootstrap**: that flag
*stamps* a receipt onto a typed pass, this one *re-stamps* receipts the regeneration
staled. It re-authors nothing — no `kind`, no `result`, no obligation you did not already
receipt — so it is the alternative to `sed`-ing a new sha256 across every obligation by
hand and silently desyncing the one you miss.

- Idempotent: syncing an unchanged TRX rewrites the same bytes.
- **Source-scoped.** A receipt sourced from a *different* report is left byte-for-byte
  alone — syncing one TRX never rewrites another's receipt.
- **Nothing to sync is not an error.** Point it at a report no obligation references and
  you get a non-blocking `evidence.syncObservedRunNothingToSync` advisory, and nothing is
  rewritten.
- **A regenerated run that now FAILS blocks** with `evidence.observedRunFailed`, and the
  old receipts **stand** — a blocked sync never half-rewrites (a missing or unparseable
  report blocks the same way). Fix the suite, then sync.
- **Mutually exclusive with `--from-test-report`.** Both write the receipt; giving both in
  one run is refused with `evidence.receiptModeConflict`. Bootstrap with one, maintain with
  the other — one at a time.

**How each kind reaches a green `verify`.** Only a `verification`-kind pass can receive
a receipt, so the observed rule lands differently per kind:

- **`verification` (a test).** Real `pass` + `synthetic: false` **plus** an
  `observedRun` from `--from-test-report`. This is the only kind a suite run can
  discharge, and the only kind that satisfies `verify` on a pass.
- **`generated-view`.** Its currency is established by the **generators**, not by a test
  run — so it has *no honest observed run*, and a bare `result: pass` on it is
  `unobserved` forever. Declare it a first-class **deferral** (the four fields below),
  not a pass. That is the honest disposition, not a workaround.
- **Any other non-`verification` obligation you were about to mark with a bare `pass`**
  (e.g. `kind: implementation`). Same trap: there is no receipt a suite run can attach
  to it, so it stays `unobserved`. Either back the work with a `verification` obligation
  that a real run discharges, or **defer** the non-test obligation honestly.
- **`review`, disclosed `synthetic`, `deferral`.** These make no real-pass claim (or
  disclose a stand-in), so they never reach the `unobserved` arm and are never punished
  for a run they did not assert.

The rule of thumb: if an obligation's proof is a **test that runs**, it is a
`verification` pass with a receipt; if its proof is anything a run cannot observe
(a generated view's currency, an out-of-scope cut), it is a **deferral**. A bare
non-`verification` `pass` is the state that surprises authors at `verify`.

## Deferrals are first-class, not failures

A **deferral** (`result: deferred`, or `kind: deferral`) is an honest, accepted
disposition — *not* a failure and *not* something to hide. A work item can ship with
declared deferrals and still be coherent: it truthfully says "this obligation is not
satisfied yet, and here is why." That is strictly better than dressing an unfinished
obligation up as done.

The one thing never to do is convert a deferral into a **synthetic pass** to make a
count look greener. Only `result: pass ∧ synthetic: false` satisfies, so a synthetic
pass buys you nothing at `verify` and costs you the honesty the model exists to
protect. When an obligation genuinely can't be satisfied in this cut (blocked
upstream, out of scope for now, awaiting a dependency), **defer it and say why** —
that is the honest, first-class outcome. A run that ends *N real pass / M deferred /
0 synthetic* is a healthy run.

## Vocabularies

- **`kind`:** `implementation · verification · review · generated-view ·
  synthetic · deferral · note · missing` (`generatedview` also accepted). An
  **unrecognized `kind` silently becomes `verification`** — a typo does not fail
  the build, it just records a different kind than you wrote. Spell it exactly.
- **`result`:** `pass · fail · deferred · missing · stale · advisory · blocked`
  (trimmed and lowercased before matching).

## Example: a declaration that SATISFIES

<!-- fsgg-sdd:example corpus=evidence.yml mode=ref -->
```yaml
schemaVersion: 1
evidence:
  - id: EV001
    kind: verification
    subject:
      type: task
      id: T001
    artifacts: [tests/Product.Tests/InputMapTests.fs]
    result: pass
    synthetic: false
```

## Example: an accepted DEFERRAL (all four fields are REQUIRED)

A deferral (`result: deferred`, or `kind: deferral`) is a first-class, accepted
outcome — not a failure. The evidence gate **requires every deferral to carry all
four fields**, or it blocks with `evidence.missingDeferralRationale`:

- **`rationale`** — why this obligation is deferred rather than met now.
- **`owner`** — who owns the deferred work.
- **`scope`** — what, precisely, is deferred.
- **`laterLifecycleVisibility`** — how/when the deferral resurfaces downstream.

This is the canonical deferral shape (a verbatim fragment of
`docs/examples/lifecycle-artifacts/evidence.yml`, run through the evidence gate by
the skill↔gate doctest):

<!-- fsgg-sdd:example corpus=evidence.yml mode=contains -->
```yaml
  - id: EV008
    kind: deferral
    subject:
      type: task
      id: T008
    requirementRefs: [FR-002]
    clarificationDecisionRefs: [DEC-002]
    checklistResultRefs: [CR-003]
    planDecisionRefs: [PD-003, PD-004]
    result: deferred
    synthetic: false
    rationale: A match-end/win condition is out of scope for this work item; rally scoring ships without it.
    owner: codex
    scope: match-end condition and win detection
    laterLifecycleVisibility: Re-open as a follow-on work item when match play is specified.
```

One accepted deferral (`DEC-002`) yields exactly **one** keep-visible obligation. Everything the
scaffolds emit that merely echoes it folds into this entry rather than fanning out into three more:
`PD-003` (the plan's pure deferral mirror, #649), `CR-003` (the checklist's pure deferral echo,
#646), and `PD-004` (the plan's mirror of that echo). A plan decision or checklist review that
carries real content, by contrast, keeps its own task and obligation.

## Example: declarations that DO NOT satisfy

```yaml
schemaVersion: 1
evidence:
  - id: EV001
    kind: synthetic
    subject: { type: task, id: T001 }
    result: pass
    synthetic: true       # discloses a stand-in → disposition synthetic, NOT satisfied
  - id: EV002
    kind: verification
    subject: { type: task, id: T002 }
    result: fail
    synthetic: false      # not satisfied
```

A subject may be a `task` or a `requirement` (e.g.
`subject: { type: requirement, id: FR-002 }`); `artifacts`/`source` point at the
real test or proof, and `note` documents context.

## At scale: classifying an auto-expanded obligation graph

`tasks` fans your authored tasks out (per-FR, per-plan-decision, per-contract, and
keep-deferral-visible tasks), so a couple dozen authored tasks routinely become
**scores of obligations** — a real run went 18 tasks → 85 obligations. `evidence`
scaffolds one entry per obligation. Don't panic at the count and don't blanket-`pass`
it: classify the set **honestly, one obligation at a time**. Each scaffolded entry
already carries the `requirementRefs`/`planDecisionRefs` it descends from, so its own
`evidence.yml` line tells you what it's for — no join back to `tasks.yml` by task
title.

A repeatable sweep over the graph:

1. **Group by origin ref.** Read each entry's `requirementRefs`/`planDecisionRefs`;
   obligations for the same FR/PD classify together.
2. **Point real work at real proof.** For each obligation whose code + test exist, set
   `result: pass`, `synthetic: false`, and `artifacts`/`source` to the actual test or
   proof file. This is the only state that satisfies.
3. **Defer what isn't done — honestly.** For each obligation you can't satisfy in this
   cut, record a deferral (`result: deferred` / `kind: deferral`) with a `note` saying
   why. Deferrals are first-class (above); leaving some is expected, not a failure.
4. **Never synthesize to fill a gap.** If you're tempted to mark something
   `synthetic: true` just to clear it, defer it instead — a synthetic pass doesn't
   satisfy anyway.

The end state is every obligation in a truthful disposition — a mix of real passes and
declared deferrals, zero synthetic stand-ins.

## Bulk-authoring a large obligation set

Filling scores of entries by hand is error-prone, and you should **not** reach for a
throwaway script that stamps a blanket `result: pass`. Author in bulk with the
affordances the tool already gives you, then keep each classification honest:

- **Pre-map proofs at scaffold time.** `fsgg-sdd evidence --work <id> --from-tests
  <path>` seeds each *newly scaffolded* obligation with a verification-kind `source`
  pointing at `<path>` (a declared pointer, checked at `verify`). It never overwrites
  an obligation you've already authored (no-clobber), so it's a safe first pass over a
  large graph before you refine.
- **Drive edits from the origin refs.** Because every entry carries its
  `requirementRefs`/`planDecisionRefs`, a scripted or editor-macro pass can set the
  right `artifacts`/`result` per obligation *by the ref it descends from* — the same
  grouping the sweep above uses — instead of matching on task title.
- **Then classify honestly, per obligation.** Bulk authoring speeds the typing, not
  the judgement: every obligation still needs an individual, truthful
  `result`/`synthetic` (a real `pass` only where the proof exists; a `deferred`
  otherwise). A blanket `pass` across the set is exactly the dishonesty `verify` and
  the satisfaction rule are built to catch.

## The visual-inspection obligation

If `.fsgg/project.yml` declares

```yaml
project:
  visualSurface: true
```

then `tasks` derives one extra task — **`Inspect a rendered frame`** — carrying the
`visual-inspection` required skill, and therefore one extra obligation in your
`evidence.yml`. It exists because SDD's obligation graph is otherwise closed over
requirements: a defect that lives in the **conjunction** of two requirements — a
draw order that occludes a geometry the physics got exactly right — is in no
requirement, so no requirements-derived gate can reach it. The only thing that
reaches it is rendering one representative frame and **looking at it**.

Discharge it by doing exactly that:

1. Drive your product's own update loop to a representative state (the state where
   the requirements you are worried about interact).
2. Render **one frame** through whatever render-to-image entry point your product
   already has, and write the image somewhere durable.
3. **Look at the image.** This step is not automatable and is not optional.
4. Declare the produced image as the obligation's evidence.

```yaml
  - id: EV006
    kind: verification
    subject:
      type: task
      id: T006
    obligationRefs: [EV006]
    artifacts: [evidence/frame-ceiling-bounce.png]
    result: pass
    synthetic: false
```

Two rules are enforced, and both exist because a green suite over a
self-contradicting spec once shipped an invisible ball:

- **A `pass` must name a rendered artifact.** A non-synthetic `result: pass` with an
  empty `artifacts:` and no `sourceRefs[]` `path`/`uri` blocks with
  `evidence.missingVisualInspectionArtifact`. A pass that names no image asserts that
  someone looked at a frame that does not exist.
- **A `synthetic: true` pass never satisfies it**, exactly as for every other
  obligation. Disclosing a stand-in is honest; it is not proof.

If you cannot render and look in this cut, **defer it** with the four deferral fields.
A declared deferral is a first-class outcome (above); a synthetic pass is not a
shortcut to one.

SDD owns the obligation, never the renderer. It does not know what your visual
surface is, does not check that the named file is an image, and ships no `render`
command — the declaration is a boolean, and the recipe is your product's.

## Disambiguation

This lifecycle `evidence.yml` is the **SDD evidence contract**. A product
scaffolded by SDD may ship a *separate, unrelated* "evidence" document of its own
(e.g. a rendering product's visual-evidence report) — that is **not** this file.

## Real-vs-synthetic discipline

Prefer real filesystem/process/schema artifacts. When a synthetic stand-in is
unavoidable, set `synthetic: true` and disclose what real path it stands in for —
the tool will correctly record it as not-satisfying rather than letting a stand-in
masquerade as proof.

## Pitfalls

- A typo in `kind` (e.g. `test`) silently becomes `verification` — your declared
  kind is lost. Use the exact vocabulary.
- Marking work `synthetic: true` and expecting it to satisfy — it never does.
- **An unescaped quote in free-text prose blocks the stage as a YAML syntax error.**
  `evidence.yml` is machine-generated with `notes`/`rationale`, so an apostrophe in a
  single-quoted scalar (`'RM1's shell'`) closes it early. Double it (`'RM1''s shell'`), or
  backslash-escape an inner `"` in a double-quoted scalar. `evidence` names the quote as the
  likely cause when a parse fails on a quoted line — see [[fs-gg-sdd-authoring-contracts]] §2.

## Next

- `verify` — evaluate verification readiness over your evidence: [[fs-gg-sdd-verify]].

## Related

- [[fs-gg-sdd-authoring-contracts]] (full grammar + drift guard),
  [[fs-gg-sdd-tasks]] (the obligations), [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/reference/authoring-contracts.md` (§ evidence.yml declarations);
  `docs/quickstart.md`.
