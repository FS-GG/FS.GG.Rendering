# Independent review and material filing

Review authority is the append-only structured ledger. Every decision is a digest-chained JSON
record posted with `scripts/fsgg-coord review record <ref> <draft.json> --pr <n> --json`.
Narrative prose, quoted JSON, and historical marker-shaped text carry no authority.

## Wire contract

<!-- BEGIN GENERATED: fsgg-protocol:review-policy -->
*Generated structured review contract. The digest validator and state machine consume these exact values.*

| fact | value |
|---|---|
| schema | `fsgg.coord.review-decision/v2` |
| kinds | `initial, confirmation, escalation, repair-phase, acceptance` |
| ordinary repair ceiling | 3 |
| repair-phase ceiling | 10 |

<!-- END GENERATED: fsgg-protocol:review-policy -->

- Marker: `<!-- fsgg:review-decision/v2 -->` followed immediately by one JSON object.
- Schema: `fsgg.coord.review-decision/v2`.
- Kinds: `initial`, `confirmation`, `escalation`, `repair-phase`, `acceptance`.
- Revisions start at one and are contiguous. `previousDigest` binds the prior canonical record.
- Every record binds the exact PR subject, 40-hex head SHA, minted critic identity, policy version,
  timestamp, kind-specific fields, and its canonical digest.
- Generic route identities such as `fsgg-critic-normal` are not minted critic identities.

Prepare a draft with the schema fields; the writer seals revision, predecessor, and digest from the
live ledger. It rejects malformed, gapped, stale, or concurrently advanced ledgers and never falls
back to prose.

## State transitions

<!-- BEGIN GENERATED: fsgg-protocol:lifecycle-policy -->
*Generated lifecycle boundary. These are machine-owned prerequisites; judgement about the work remains authored.*

Required housekeeping: `host-identity`, `stale-claim`, `engine-currency`, `pending-writes`, `reconcile`, `triage`.

Host acceptance fields: `accepted-head`, `initial-review`, `latest-confirmation`.

Terminal transition evidence: `merge` → `post-merge-obligations` → `done-stamp`.

<!-- END GENERATED: fsgg-protocol:lifecycle-policy -->

<!-- BEGIN GENERATED: fsgg-protocol:ledger-policy -->
*Generated ledger schema. The receipt id binds these fields; prose does not substitute for the ledger.*

Schema: `fsgg.coord.planning-receipt/3`.

Observation fields: `kind`, `observedAt`, `sourceSha`, `outcome`, `receiptId`.

Receipt fields: `schema`, `observedAt`, `sourceSha`, `complete`, `consolidationApproved`, `observations`, `contentIntakes`, `contentDispositions`.

<!-- END GENERATED: fsgg-protocol:ledger-policy -->

Mint a critic with `eval "$(scripts/fsgg-coord whoami --mint)"`. The `initial` record has verdict
`pass` or `changes-required`, round zero, and no review backlinks. A passing record carries either
four ordered meaningful-route evidence strings or exactly one not-meaningful reason. Set
`diffAuditRequired` when mechanically discovered semantic replacements exist.

After material repair, a freshly minted successor critic posts a `confirmation` for the new exact
head. Rounds are contiguous and one-based; `initialReview` names the initial comment and
`precedingReview` names the immediately prior structured comment. At most three ordinary
confirmations are allowed. The successor inherits the durable ledger and review packet, not the prior
critic's clearances, and performs a fresh full review of the current head.

If that ceiling is exhausted, append `escalation` then `repair-phase`. Escalation without the typed
repair-phase fact has no authority. Repair phase permits at most ten confirmations before human
escalation.

Fresh succession is the ordinary repair route, not an exceptional recovery. Five of five measured
repair chains on 2026-08-17 outlived their dispatched critic: `.github#2712` / PR #2745,
`.github#2724` / PR #2746 (twice), `.github#2730` / PR #2747, and the fifth chain recorded at
`.github#2691` comment `5311942674` (packet comments `5311928208` and `5311942674`). A successor
therefore records under **its own minted identity** — never as a record bearing the earlier critic's
id, and never as a second `initial`, which is allowed only after host acceptance. A `confirmation`
immediately following `changes-required` is the typed ordinary generation boundary. Historical
`succession` objects remain readable, but new ordinary successors do not manufacture a host grant.
The durable review-wait receipt below is the accountable transition; no host attests that an ephemeral
agent despawned. Outside that repaired-head boundary, an unrecorded critic change remains refused.

## Durable review waits and critic generations

A protocol-created queue is durable state, not an agent sleeping while its active lease expires. The
one receipt vocabulary is:

`WaitReceipt(item, claimGeneration, reviewGeneration, kind, enteredAt, expiresAt, evidenceRef)`.

`item` is the qualified issue ref; `claimGeneration` is the winning GitHub-issued claim marker id;
`reviewGeneration` is the structured-review generation token that anchors the current chain; `kind`
names the awaited event (`initial-review` or `repair-confirmation`); `enteredAt` and `expiresAt` bound the wait; and `evidenceRef` identifies
the durable comment or check whose change resumes it. These are authority-issued revisions, not values
an agent invents.

The canonical generation token is `<head>:initial-review:0` for an initial record and
`<head>:repair-confirmation:<round>` for confirmation, escalation, or repair-phase records. Exactly one
generation may be unconsumed. A new entry is refused until the preceding entry has a durable terminal
event; multiple distinct unconsumed entries are invalid authority, never a latest-wins queue.

Entering a review queue writes the receipt before the actor yields. A current receipt plus the open
item PR preserves the touch-set reservation, but it never extends or resurrects the worker's mutation
lease. Before changing the tree, posting a repair, or advancing review, the resumed actor revalidates
that `claimGeneration` is still current or explicitly reacquires the item and records the new
generation. `Unknown` or stale claim state authorizes nothing.

Review completion, cancellation, and bounded timeout each consume or expire the receipt idempotently.
They conditionally transition the same receipt revision, then read back the winner; completion racing
timeout therefore has one durable outcome rather than two inferred successes. Timeout returns the item
to an explicit recoverable review state and cannot reserve a lane forever. It does not revive a claim.

The receipt also carries critic-generation continuity. On an implementer-repair wait, the critic that
produced the finding may exit normally. Resumption dispatches a fresh successor, which receives the
item/spec, exact head, complete ledger, repair diff, verification evidence, and current wait receipt.
It inherits no prior clearance and performs a full independent review of that head. The successor's
structured record and the consumed receipt make the handoff re-derivable; ephemeral runtime liveness
and a host's testimony about despawn are not review evidence.

Write each entry/completion/cancellation/timeout event through the authoritative client boundary:

`scripts/fsgg-coord review wait <ref> <event.json> --pr <n> --json`

The writer rejects a non-current claim generation, a duplicate entry generation, or a transition with
no matching durable entry. A terminal event is authorized by both the matching entry and that entry's
still-current `claimGeneration`; replacing the claim cannot transfer authority to consume an older
entry. `review record` refuses every critic record until the matching canonical entry is waiting, and
host `acceptance` until the immediately preceding critic record is named by a completed entry. A live
`review <ref> --pr <n> --json` parses those PR markers and projects
`waiting`, `completed`, `cancelled`, `recoverable`, `invalid`, or `noReceipt` with the bound receipt.
Dispatch actions are available only from the matching `waiting` state. `noReceipt`, malformed or
invalid markers, stale-claim recovery, and an unconsumed generation all return no actionable verdict.

Only the host posts `acceptance`, after the latest critic record is `pass` and all checks are green.
It uses verdict `accepted`, binds the exact head, initial URL and latest critic URL, follows the
critic comment, and preserves generation critic identity. When diff audit is required it carries
base64 typed receipts; the engine recomputes live inventory and refuses missing, malformed, stale,
partial-coverage, mixed-head, or byte-drifted evidence.

Immediately before merge run:

`scripts/fsgg-coord landable <pr> --repo <repo> --wait --sha <head> --require fsgg:review-decision/v2`

A moved head retires the accepted older generation without rewriting it and requires a fresh initial
generation. Backlinks, head bindings, critic continuity, and digest continuity fail closed.

## Independent critic boundary

The critic is independent of implementation context: provide the roadmap/spec, diff, exact head and
verification evidence, but not hidden implementation reasoning. A fresh successor handles each
post-repair review from the durable wait packet. The structured ledger and consumed wait receipt carry
continuity; prose is never authority, and runtime liveness testimony is not evidence.

Report concrete findings first, ordered by severity and linked to files or commands. A pass means no
unresolved material finding remains at the reviewed head. The host validates the ledger and checks
itself and never translates a prose verdict into a structured pass.

## Root cause, dedupe, and materiality

For every candidate finding the critic searches the relevant code and history for the **cause**, then
searches open and closed issues, PRs, comments, and the board for that cause rather than the surface
symptom. Rows expressing one cause routinely share no symptom text at all. Reuse an existing item that
already carries the cause and add the new evidence there.

A finding is **material** only when the evidence shows at least one of:

- acceptance criteria are unmet, or observable correctness, compatibility, security, data integrity,
  performance intent, or releaseability is at risk;
- a test or gate can report green without checking its declared subject;
- a gate the change adds or modifies stays green when inverted, or a test named for a property does not
  itself provide that property — see **Gate-inversion evidence** below, which makes this a finding by
  definition rather than a judgement call;
- a verification artifact's proof of efficacy is drawn from its own author's model: a gate whose subject
  a production writer emits and which carries no producer-agreement leg, a mutation whose only witness
  is its sweep's own aggregate count, a "second reading" that consumes the first reading's output, or a
  control that cannot be shown producing a second answer — see **Author-independent efficacy** below,
  which is `.github#266`'s fifth admitted mechanism and is material on the same footing as a surviving
  inversion;
- an architecture or ownership violation creates a concrete defect or blocks safe evolution;
- bounded hardening prevents a measured recurring failure, retry, operational burden, or meaningful
  maintenance cost; or
- the item ships or claims reachable game functionality with no passing bot-driven headless player
  journey (`.github#2087`) — see **Game functionality** below.

Style, naming taste, speculative edge cases, optional refactors, "could be cleaner" observations, and
findings already repaired in the current PR are **not** material new work. Record them in the review
comment when useful, but never create an issue, board row, blocker edge, or follow-up queue entry for
them. Uncertainty is not materiality; measure or omit.

## Runtime-route evidence gate

Source review is required and is **not sufficient** for a runtime-behaviour claim. When the PR's
requirements, its claimed behaviour, or a candidate finding concern behaviour reachable through more
than one meaningful route, the critic **executes or measures** at least one comparison through the
production route against the built artifact. The comparison observes the behaviour that could diverge —
a player input route against its direct dispatch, say — rather than asserting that the two source
implementations look equivalent.

The ledger seals the *shape* of that evidence: a passing record carries four ordered meaningful-route
strings (built artifact, executed command, compared routes, observed result) or exactly one
not-meaningful reason. It cannot decide **whether** a comparison is meaningful, and it cannot tell
reading from executing. Both are the critic's, so a record that cites only source reading for such a
claim is incomplete and cannot be accepted as evidence that the routes agree. Where no meaningful
production-route comparison exists for the review subject, the critic states that boundary and why; the
exception waives none of the rest of the source review.

This is reusable guidance, not an audio-specific recipe. Rogue3 exposed the shape when a built product
route emitted `[]` while direct dispatch emitted `[PlaySfx (SoundId "floor-descend", 0.8)]`: the cue map
looked correct in isolation, and executing both routes revealed the defect.

## Game functionality — the bot-driven player journey gate

This gate is **blocking**, not advisory. When an item ships or claims reachable game functionality, the
critic verifies a passing bot-driven headless player journey exists and reviews the journey itself, not
only its result. Absence of that evidence is a material finding by itself — a green suite that never
boots the product cannot distinguish "works" from "unreachable" (`2026-08-02-Rogue3.md` §4.3: eleven
consecutive `shipReady` verdicts preceded the human launch that found an unreachable starting room).

A journey is evidence only when it is driven **through the product's real input surface** — the same
control messages a player emits — and **boots at the product's real entry point**. Direct `Msg`
injection, a test-only API, or any seam that exists solely for tests is not evidence, and a journey
using one is rejected by this gate rather than merely discouraged in prose. Seeding a mid-game model
and calling the functionality "reached" is a gate failure however correct the resulting state looks.
Functionality the item names that no journey reaches is reported as uncovered, never silently absent.

Where the product's entry point is not yet test-ownable, the critic returns `changes-required` and
records that the gate cannot run and why — fail closed, never pass by absence.

One advisory input is explicitly **not** consumed as blocking here: `FS.GG.Game#563`'s
`DegenerateVocabulary` check fires on declared-vocabulary cardinality alone, so it flags a legitimately
single-inhabitant slot with zero `Unbound` arms. A `DegenerateVocabulary`-only finding, with no
accompanying `Unbound`-arm evidence, is not by itself material under this gate.

## Gate-inversion evidence

A gate that has never been red is equally consistent with "nothing was ever wrong" and "it cannot
fire", and reading cannot separate those. `.github#2223` measured ten such gates in one run across six
items and four repositories, three months after `.github#1610` found the same class. So these are
numbered steps, not a virtue some critics happen to have. The bound is one mutation per touched gate,
plus the single non-vacuity leg step 2 names, plus whichever of steps 10–14 the gates in that same
inventory actually owe; this is never a suite-wide sweep.

1. **Inventory the gates the change adds or modifies, and show each one is REACHED.** A gate is
   anything whose purpose is to refuse: a test, an assertion, a fixture case, a checker script, a
   workflow step, a schema or parser rule. The inventory is bounded to what the diff touches — never
   the whole suite. For each, name the workflow, the job, and the invocation line that actually calls
   it. Every step below measures whether a gate *can* fire; not one of them asks whether anything
   ever *runs* it. A gate no workflow invokes is graded `NOT_MEASURED` at best and is material by the
   same logic step 3 applies to a surviving inversion: inverting it by hand reds exactly as step 2
   asks and certifies nothing, because the CI signal it exists to produce has never once been
   generated. `.github#2537` is the worked instance, cross-referenced here rather than restated, and
   its own repair belongs to it.

   **"Reached" includes the trigger's own `paths:` filter, evaluated against THIS change.** Where a
   gate runs under a `paths:`-filtered workflow, name the filter and the path in this diff that
   matches it. A gate that is wired, invertible, and simply never triggered for the diff in hand is
   indistinguishable from an absent one — and it is not merely accidentally silent but *selectively*
   silent: a path filter is quietest on the additive changes that leave a stale artifact in place, and
   loudest on the destructive ones that were easiest to notice anyway. On `.github#2230`,
   `.github#2510`'s coverage gate fired only because that change happened to DELETE files under a
   watched glob; the "keep both homes" variant — add the new home, leave the old one populated — would
   have matched no path in the filter at all, and two disagreeing copies would have landed with
   nothing watching. That variant is the one a reasonable implementer picks precisely to stay green.

2. **Invert each gate exactly once by breaking its SUBJECT, and show it examined something.** Break
   the thing the gate claims to protect — not the gate's own predicate — run the suite, and record the
   exact mutation and the exact observed result under `Verification:`. Where a subject mutation
   genuinely cannot be constructed, say so, record predicate inversion as the strictly weaker evidence
   it is, and grade the gate `NOT_MEASURED` — never `JUSTIFIED`.

   **Vacuous green: a gate can also pass because it examined nothing.** Breaking the subject presumes
   the gate had a subject in front of it. Where a gate's verdict is computed over a corpus, a fixture
   set, or any input collection, empty that collection and re-run: if the gate still passes, then
   "found nothing" and "looked at nothing" share an exit code, which is `#266`'s shape one layer in.
   `.github#2534` measured the need — of seven gate mutations the most load-bearing was the one that
   emptied the scanned corpus, and only a separate non-vacuity leg caught the vacuous pass;
   `.github#2510` measured its half-closed form, where a repair that closed "the declared root does
   not exist" left "the root exists but is EMPTY" producing the identical confident green.

   **A source-text gate has this failure mode and a behavioural gate does not.** An empty corpus
   satisfies a gate that greps for a name in a way it cannot satisfy a gate that executes the code. So
   a gate whose subject is source text carries a **non-vacuity leg** — the gate shown red on a
   non-empty corpus containing a genuine offender, so the two outcomes stop being indistinguishable.
   That leg is the one further mutation this section requires, it is owed only by source-text gates,
   and it is why a self-test for a scanner **calls** the scanner rather than grepping for its name.

3. **A surviving inversion is material by definition** — not a judgement call, not a style note, and
   not something a later round may absorb silently. So is a gate graded `NOT_MEASURED` because nothing
   invokes it.

4. **A test that claims a property must provide it.** Where the property is supplied by a test other
   than the one named for it, name that provider; a test named for an invariant it does not exercise
   is a decorative gate whichever way it happens to be passing.

5. **The fixture must reproduce production.** A fixture that omits the shape production has cannot go
   red on it, so the inversion has measured the fixture rather than the subject.

6. **The measurement environment must not supply what production lacks.** A run with a tool,
   credential, path, or file that CI does not have measures a different system; say which environment
   produced each observation.

7. **Invert the unreadable input, not only the happy path.** Mutating a gate's success path leaves one
   whole class untouched: **a non-answer reported as a confident answer.** Three of `.github#2223`'s
   instances are this and nothing else — a literal `always` matched no template and was graded *not
   selected*; a row with `scope` absent fell through to somebody else's row; an unreadable form was
   graded a confident negative. In each the gate received something it could not interpret and emitted
   a definite verdict about it, and inverting the happy path leaves all three green. So feed the gate a
   form it cannot parse and confirm it **refuses rather than decides**: "I could not evaluate this" is
   never "I evaluated it and it passed" (`#266`). A gate's stated limits are part of what is verified
   here too — grading workflow *files* is not evidence that the context executed anything, because
   `if: false` on every step and `continue-on-error` are each green and each invisible.

8. **A repair must catch the escape that was actually found.** When a mutation got through, the
   repaired gate must go red on *that* mutation — not merely pass, and not merely red on some newly
   added case. A repair that adds cases without re-running the original escape has not been shown to
   close it.

9. **When a repair strengthens what a gate ASSERTS, re-derive the evidence for the stronger claim.**
   Making a gate's claim broader, more specific, or more confident changes what must be proved, and
   evidence gathered for the weaker claim does not carry forward. On `FS.GG.Templates#349` a round-1
   repair strengthened a failure message to say the complete published set had been compared — *"so
   this is not a search-depth artifact"* — while the predicate still asked only whether a ceiling had
   been hit; a skipped download then produced a confident upstream accusation in better prose than
   before the repair. **A gate that gains eloquence faster than correctness is worse than one that
   stayed vague**, because the vague one did not invite reliance on a claim nothing checked. A
   strengthened assertion carrying unchanged evidence is material, exactly like a surviving inversion.

For each touched gate the review marker names the mutation applied, the observed result, and the
workflow and job that invoke it — with the trigger's path filter and the path in this diff that
matches it, wherever that trigger is filtered — and, where the gate's subject is source text, its
non-vacuity leg. For each gate whose inversion could not be obtained, the reason, which is
`NOT_MEASURED` and never a pass. `scripts/gate-mutate.py` is this org's harness for the sweep and its
verdict vocabulary is the one to use: `JUSTIFIED` fired, `DECORATIVE` could not fire, `NOT_MEASURED`
obtained no measurement. Where steps 10–14 below apply, the marker additionally names the
producer-agreement leg's declared relation and its mutation, the per-mutation witnesses, the ladder rung
each second reading reaches and the residual it does not escape, which of the two mutually blind methods
produced which observation, and each leg's negative/positive control pair — or, for any of these, the
declared boundary that closes it.

### Author-independent efficacy — `#266`'s fifth mechanism

Steps 1–9 measure whether a gate *can* fire. Not one of them asks whether the thing that graded that
measurement was independent of the person who wrote it, and that is what `.github#266` admitted on
2026-08-17 as its **fifth** mechanism: *the artifact's proof of efficacy is authored from the same model
of the world as the artifact, so a wrong model is invariant under the proof.* The other four mechanisms
describe what an artifact does wrong; this one describes why nobody caught it, which is why it is the
one that explains them.

It is not caught by care, and being right is not protection. One gate shipped with **ten**
authoring-time inversions; all ten fired correctly; not one could reach the blind spot, because no
mutation in the space asked the question the author's false premise had already foreclosed
(`.github#2691` comment `5311706988`). The same chain later measured **65 survivors where four expert
hand sweeps had found 5** (comment `5313644118`). Every one of those authors was reasoning correctly
from a premise that was false, and their inversion table was not merely unhelpful — it was affirmative
evidence pointing the wrong way, which a reader was entitled to rely on.

Steps 10–14 are bounded exactly as steps 1–9 are: per gate in step 1's inventory, per second reading the
change itself offers as evidence. Each states what it is owed on and what closes it, because a
requirement with no terminal state is this same defect one level up.

10. **Where a gate's subject is emitted by a production writer, it carries a producer-agreement leg.**
    A fixture that writes its own marker and then asserts the gate reads it proves only that the fixture
    and the gate agree; neither has been compared against what production emits. That is not an analogy
    for the mechanism — it is the same authorship relation with the fixture in the author's seat. The
    `§11.2` fencing sequence produced **four** artifacts of exactly this shape, audited at
    `.github#1858` comment `5316937299`: `OpLock.acquire` with zero production callers, an
    `fsgg:merge-election` reader with zero writers, a broker that refuses every real request, and a
    six-field gate whose only producer emitted four fields. Every one passed its own tests.

    So the gate carries one leg that

    - **parses the producer**, not a fixture's copy of it, and fails when no producer exists at all — a
      reader with no writer is the inert case above and is detectable in a single assertion;
    - asserts the relation **in both directions**, so a producer that stops writing a field the gate
      requires reds, and a gate that starts requiring a field no producer writes reds too;
    - **names which relation it asserts, and why. Equality is the wrong default.**
      `tests/receiver-validate/run.sh` asserted set equality until `.github#2395`, and equality *"would
      have reddened this leg the moment the producer became correct"* (`tests/receiver-validate/run.sh:975-996`):
      the gate it grades documents forward compatibility — it accepts additional pairs — so containment
      is the contract and equality was an over-statement that held only while both sets were the same
      four fields. Assert the relation the producer's contract actually states;
    - carries a **liveness term** refusing an empty set on either side, because `∅ ⊆ ∅` holds and "both
      sides forgot" would otherwise grade as agreement (comment `5314929995`);
    - and ships a **mutation** showing that assertion red — drop one field the gate requires from the
      parsed producer facts and confirm the leg fails.

    Reference implementation: `tests/receiver-validate/run.sh` — producer existence at `:971-973`, the
    declared relation and its recorded reasoning at `:975-1010`, the liveness refusal inside
    `required_subset` at `:1002-1003`, and the mutation leg at `:1012-1036`. Its header states the point
    in terms: *"That is the check neither slice 2 nor slice 3 had."* (`tests/receiver-validate/run.sh:20-25`).

    **Bound and terminal disposition.** Owed once per gate in step 1's inventory whose subject is
    producer-emitted, and only where that producer sits in a repository the reviewer can read. Where it
    does not — a third-party payload, a hosted API's response shape — the leg cannot be constructed:
    declare the boundary and the reason, grade it `NOT_MEASURED`, and that closes it. A gate whose
    subject is not producer-emitted at all — a source-text scanner, a pure function's algebra — does not
    owe this leg, and says so in one line rather than leaving the question open.

11. **Each mutation names the test that redded for it, and the witness case sits at the mutated
    predicate's boundary.** A sweep reporting only an aggregate — `N mutants, N killed, 0 survived` —
    has offered its own output as its evidence, which is rung 1 of step 12. The aggregate cannot
    distinguish a complete sweep from one whose enumeration silently shrank: **282 of 385 mutants — 73%
    — vanished with the sweep's own non-vacuity guard green and silent** (comment `5313910040`), and
    `0 survivors` over 385 and over 103 are the same bytes at exit 0. So the record is per mutation: the
    mutation applied, and the **name of the test that went red for it**, where that name describes the
    property the mutation broke. A mutation whose only witness is the sweep's own count is not
    witnessed, and a sweep containing one fails.

    **Reaching a branch is not covering its predicate**, and this clause was earned three times on one
    row. A leg named `green: /// inside strings and (nested) block comments is not a doc comment` could
    not detect the nesting it was named for: its nested `///` sat *before* the inner `*)` and was skipped
    at depth one either way, so removing nesting from the lexer entirely still left the fixture at **34
    passed, 0 failed** (comment `5311706988`). The discriminating case puts the marker *after* the inner
    `*)`, at the boundary the predicate actually decides. `.github#2395`'s critic did the affirmative
    version: a competing election **exactly one id lower**, not one at an arbitrary distance.

    **Bound and terminal disposition.** This governs the mutations *this change's own* evidence applies
    — the one per touched gate step 2 requires, plus whatever a sweep harness in the diff enumerates. It
    is never a demand to name tests for mutations nobody ran. Where a mutation legitimately reds no
    *test* — a compile error, a schema-load refusal — name that diagnostic exactly and the witness
    stands; a bare non-zero exit does not, because it cannot say which property broke. Where the suite
    genuinely has no named test for the property, that absence is the finding step 4 already owns, and
    naming it closes this step.

12. **Grade every "second reading" on the independence ladder, and name the rung it reaches.** Two
    readings compared against each other are an oracle only so far as they are independent, and
    independence has three rungs. Ask them in this order, because the cheapest question is also the one
    that voids the others.

    1. **Value — does the checker consume the checked value?** If the second reading is handed the first
       reading's output, there is one reading wearing two names and the comparison is an identity on
       itself. Measured: a sweep's per-operator accounting whose "second reading" was
       `counts["dir-drop"] = len(projects)` over the *same list object* passed in as a parameter, so the
       equality held for every input, including every wrong one (comment `5315392897`). The tell is
       mechanical and it is in the signature: **a "second reading" whose parameters include the thing it
       is supposed to be reading a second time.** Follow the data.
    2. **Key — do the two readings resolve the subject through the same identifier?** Two readings that
       call different libraries but match on the same name in the subject are one reading in two hats.
       Measured: four of ten operators resolved through the same identifier on both sides; one ordinary
       refactor of the *subject* — routing output through a `report()` helper — zeroed both terms of one
       operator at once, `0 + 0 == 0` held, and 27.5% of the sweep vanished at exit 0 (comment
       `5314929995`).
    3. **Library or runtime — do they share a front end?** The weakest rung, and the one most often
       disclosed. A disclosure closes nothing by itself; it names a residual.

    The review record states the **highest rung the reading reaches** and the residual it does not
    escape. A rung-3 disclosure presented as independence is the mechanism restated in good faith:
    honest, and not operative.

    Two corollaries, both cheap to apply. **An identity has a vacuous solution** — `a == b` is satisfied
    by `0 == 0`, so every dimension an identity ranges over needs a liveness term or "both sides forgot"
    grades as agreement. And **a hazard guarded once by hand is a hazard not generalised** — a bespoke
    guard written for exactly one case is evidence the author saw the hazard, and its absence for the
    siblings is the finding. Grep for the guard, then for its siblings.

    **Bound and terminal disposition.** Asked once per second reading the change offers as evidence,
    never of every comparison in the repository. A reading that reaches rung 3 with its residual stated
    is complete: the ladder never demands escaping rung 3, which is usually impossible, and that is
    precisely why rung 3 is named rather than required.

13. **A sweep-shaped remedy owes both mutually blind detection methods.** Where the remedy under review
    is itself a sweep or an enumerating harness, two methods are needed and **neither substitutes for
    the other**, because each is blind exactly where the other sees (comment `5313910040`).

    - **Execute it in a different environment.** A parallel-copy race reported `KILLED` at `--jobs 16`
      and `SURVIVED` at `--jobs 4`, and its wrong answer was also `0 survivors`. No number of re-runs in
      one environment finds that; only a different environment does.
    - **Break its own completeness predicate.** A non-vacuity floor guard could not detect the
      disappearance of 73% of its own mutation set. That is invisible in *every* environment, at every
      job count, however often re-run; only breaking the subject finds it.

    State which method produced which observation. One applied and the other not is an incomplete
    measurement, reported as such rather than as coverage.

    **Bound and terminal disposition.** Owed only by a sweep-shaped remedy — a harness that enumerates
    its own subjects. An ordinary one-mutation inversion under step 2 does not inherit a second
    environment. Where a second environment is genuinely unavailable, name the one used and what it
    cannot distinguish; that stated residual closes the step.

14. **Every control discriminates, and states what it assumed before it measured.** A control exists to
    show the harness is not hardwired to the answer it wants, and there are three ways for one to fail
    while looking fine.

    - **Always green.** A control that passes whatever it is given is a semantic no-op. Each leg above
      therefore carries **both** a negative control — an artifact deliberately given a wrong model,
      shown to red — and a positive control — a known-good artifact, shown to be admitted. A rule that
      refuses everything carries no more information than one that refuses nothing.
    - **The right answer for the wrong reason.** On `.github#2395` an `L0` control first parsed `HEAD~1`,
      which already carried the six-field template, and matched the expected diagnosis for a reason the
      control was not testing. The repair was to parse `origin/main` **and assert up front** that what
      it parsed really had the four-field shape the control assumed, and that no placeholder survived
      substitution. Every control asserts its input's shape before it measures.
    - **A harness that can only land where it was aimed.** `.github#2395`'s critic added an **L5 harness
      control** whose leg lands on a *different* check from the one under test — the body unchanged, the
      head changed, the observed verdict `[check2]` rather than the `[check4]` the other legs exercise.
      That leg is what makes the other five mean anything, and it is the concrete form this step
      requires: at least one control that demonstrates the harness producing an answer other than the
      one wanted.

    **Measure the artifact, not the tree.** Step 6 bounds the measurement environment; this is its
    instrument. A critic on this contract's own watch reverted a file, confirmed a clean `git status`,
    and then measured a built assembly still carrying its mutation. A clean tree is not a clean
    artifact: rebuild, and compare the artifact's digest against its baseline, before attributing an
    observation to the reverted source — as `.github#2395`'s own mutation evidence did (*"reverted, DLL
    hash restored to baseline"*).

    **A declared non-invertibility is rewarded, not penalised.** Where an assertion genuinely cannot be
    inverted, declaring it is the required disposition, and the critic's job is then to attempt the
    inversion the declaration says cannot be built. On `.github#2395` one assertion was declared not
    independently invertible on the ground that a data dependency forbids a *reordering* mutation; its
    critic then found one anyway — the test locates its indices with `List.findIndex`, so an
    **insertion** inverts it — and the gate got stronger. The declaration was a pessimistic self-grade,
    and it was declared rather than hidden. A found inversion after an honest declaration is a
    strengthening, never a caught lie.

**A predicted red is not a reached branch.** Guidance that tells operators which leg of a gate is
expected to fail is making a claim about reachability, and an operator-visible summary cannot tell a
predicted red from a branch that is never evaluated. Measured:
`.github/workflows/fsgg-claim-fence.yml:194` tells operators that *"`check4` is expected to fail on every
real pull request today"*, while `scripts/check-claim-fence.py` returns at **check 1** on any marker
missing one of the six auth fields it requires (`scripts/check-claim-fence.py:286` names the six,
`scripts/check-claim-fence.py:677` computes the absent set) — so while the production writer composed
four fields, check 4 was never evaluated on any real pull request at all. Recorded at `.github#2719`
comment `5319094213`; the producer half landed with `.github#2395`
(`src/FS.GG.Coord.Cli/Client.fs:1485` now composes all six), and the guidance half belongs to
`.github#2719`. Where guidance or a summary predicts which leg fails, one executed run must show that
leg actually reached.

**What closes steps 10–14, and the two `NOT_MEASURED` grades.** One word covers two dispositions and
they are not interchangeable, which is what `.github#2757` records as the missing stopping rule:

- **A measurement that could not be obtained** — the producer is unreadable, no second environment
  exists, the mutation cannot be constructed — declared up front with the reason and the attempt made.
  This is a **terminal** disposition. It closes the step, it is not itself a material finding, and it
  does not consume a repair round. It is not a pass either: it is recorded as the boundary it is.
- **A gate nothing invokes** (step 1) or **a surviving inversion** (step 3). Material by definition, and
  nothing here changes that.

Nothing in steps 10–14 authorises a suite-wide sweep, a repository-wide audit, or a re-derivation of
gates the diff does not touch.

**This subsection's own control set, and why its provenance is the load-bearing part.** A rule about
proving efficacy owes its own proof, and that proof may not be drawn from this subsection's author, or
it reproduces the mechanism one level up. Every entry below was measured and recorded by a different
agent, with its verdict fixed before this subsection existed:

| artifact | independently recorded verdict | disposition here |
|---|---|---|
| `Client.OpLock.acquire`, zero production callers | inert; passed its own tests (`.github#1858` c. `5316937299`) | **refused** by 10 — no producer exists |
| an `fsgg:merge-election` reader with zero writers | inert; passed its own tests (same audit) | **refused** by 10 — reader with no writer |
| a dispatch broker refusing every real request | inert; passed its own tests (same audit) | **refused** by 10 — the fixture supplied the only request shape |
| a six-field gate whose producer emitted four | check 4 unreachable (same audit; `.github#2719` c. `5319094213`) | **refused** by 10 — containment fails gate→producer |
| a ten-inversion sweep built on a wrong string model | 10/10 inversions fired; blind spot unreachable (`.github#2691` c. `5311706988`) | **refused** by 11 and 12 — no mutation named a test for the foreclosed property |
| a sweep whose second reading was `len(projects)` over its own input | held for every input, including every wrong one (c. `5315392897`) | **refused** by 12 rung 1 — the checker consumed the checked value |
| `tests/receiver-validate/run.sh` section F | *"the check neither slice 2 nor slice 3 had"* (its own header) | **admitted** by 10, 11 and 14 — producer parsed, relation declared and justified, liveness term present, mutation leg red |

The last row is not decoration. A rule that refused all seven would carry as little information as one
that refused none, and the admitted entry is the only thing separating those two cases.

## Handoff-assertion provenance

Every specific, checkable assertion in an implementation handoff, critic report, or host relay carries
`Verification:` — the command, `file:line`, API call, or URL actually used to establish the fact, or
exactly `Verification: unverified`. `unverified` is first-class and non-pejorative: it makes an
unchecked claim legible without requiring every claim to be checked. **A receiver must not infer
verification from prose**, and a missing field is a detectable incomplete handoff rather than evidence
that the assertion was checked. This binds the host relaying worker or critic claims onward exactly as
it binds the worker and critic who authored them.

### Issue and pull-request body evidence

Before accepting a newly filed or materially edited issue or PR body, independently re-derive every
checkable `path:line`, count, and suite-green claim it relies on. A local citation names the exact
tracked path; a cross-repository citation names `OWNER/REPOSITORY@REVISION:path:line` or a stable URL;
a count or suite verdict names the command or check URL that produced it. Record that basis in the
handoff's `Verification:` field, and where no reproducible basis exists write `unverified` and do not
treat the claim as acceptance evidence.

This review-time check **owns** remote bodies, and owns them by construction: a source-only CI checkout
cannot enumerate existing issue or PR text, so ADR-0074 places those bodies deliberately outside the
static citation gate and delegates them here. Deleting this section leaves that delegation pointing at
nothing.

### Body-edit provenance — the REST timeline does not surface body edits

When a check turns on whether an issue or PR **body** changed since some point — touch-set honesty,
delivery-obligation staleness, a superseded declaration — `gh api repos/<owner>/<repo>/issues/<n>/timeline`
is not evidence either way. That endpoint never emits an `edited` event for a body edit, so zero results
mean only that REST has nothing to say. Treating that absence as a confident "unedited" is precisely the
non-answer-graded-as-a-confident-negative shape step 7 above warns against: a REST-timeline-only "no
edits found" is `NOT_MEASURED` for this question, never a negative result.

Measured directly on `.github#2417`: its REST timeline returned zero `edited` events while GraphQL showed
five real body edits, three of them after the claim (`.github#2456`). The authoritative source is
GraphQL's `userContentEdits` connection — `totalCount` plus each edit's `editedAt` and `editor`; the
simpler `lastEditedAt` scalar answers only "has it ever been edited", with no count or history. Reach it
through `scripts/fsgg-coord body-edits`, which is metered and fails closed, and **not** through a
hand-built `gh api graphql` call: this repository's GraphQL budget is fleet-wide and worker-metered, and
an unmetered principal is the shape `graphql-monopoly` exists to catch.

## Disposition and repair bounds

The implementing worker repairs the material findings that belong in the current PR; a fresh successor
critic fully reviews each repaired head from the durable wait packet. Every round addresses material
findings only — never minor observations.
Where no repair is required, an initial `pass` whose reviewed head equals the candidate head **is** the
confirmation, and no second record is required. Before routing any repair the host validates the current
chain and permits it only while the latest round is below the ordinary ceiling; that count-before-routing
check is what stops a failed final confirmation racing into one more repair while the escalation writes
settle.

When the ordinary chain is exhausted, the host closes the exhausted PR **without merging** and enters the
repair phase below automatically. The human park is reached only if that phase also exhausts or its
required route is unavailable, and then the host:

1. adds `Blocked on: human/action` to the issue body without disturbing its `Paths:` declaration;
2. records who, when, and why in a comment linking the escalation record (and the repair-phase record
   too, where one ran);
3. sets `Status: Blocked` and releases the claim; and
4. stops — without merging, without filing a replacement review issue, and without starting another
   automated round.

Only a human, or the automatic repair-phase transition, may retire that sentinel; a human alone may move
the acceptance boundary. **An exhausted PR never resets its counter and never begins another automated
cycle.** An already-parked item whose evidence proves ordinary exhaustion becomes eligible for the
repair-phase transition on the next board-driver pass, without human interaction.

The critic — not the implementer — owns filing for review-discovered findings, and may file new work only
when all four hold:

1. the finding is material by the definition above;
2. it is a distinct root cause that cannot remain reviewably inside the current PR;
3. no existing issue already carries that cause; and
4. the evidence and acceptance boundary suffice for another worker to act.

It files directly in the root-cause repository — never through either agent's private follow-up queue —
with observed behaviour, root cause or explicitly measured unknown, impact, acceptance, verification, a
narrow `Paths:`, `Class:` and `Phase`, on the correct board at `Status: Backlog` unless it is a genuine
blocker. Class the cause from evidence: `defect` when observed behaviour violates a current contract or
acceptance boundary, `hardening` when no contract is broken but bounded preventative work addresses a
measured recurring risk or cost. A finding that still needs human judgement is not actionable enough for
critic filing; surface it to the host. If a filed issue blocks the current item, the critic reports it,
the worker sets the real `Blocked by` edge, parks the item and releases the claim.

### A head that moves after the chain was accepted

Succession above covers a chain that is *stuck* and the repair phase covers one that is *exhausted*.
Neither covers the third case, and it is not exotic: a chain that completed, passed, and was
host-accepted on a head that then **moved before the merge**. It needs only an acceptance, any delay
before merging — correctly refusing to merge over a red gate is delay — and `main` moving enough in that
window to conflict the branch. Measured live: `.github#2512` / PR #2514 was accepted at `f1d6218d`, held
while a red arm cleared, then conflicted by two sibling merges; the fresh review of the merged head
caught release notes still claiming that tree was byte-identical to `0.50.4` — true when first reviewed,
false afterwards, and bound for an irreversible dual-feed publish through a gate that deliberately does
not judge prose.

A chain is **retired** — excluded from the evidence the protocol classifies — when, and only when, a host
acceptance both names that chain's initial record and carries an accepted head that is **not** the
current head. Both facts are read from the acceptance record's own required fields rather than from a
grant a caller supplies, because the one-initial-record rule is what stops a stranger silently continuing
another critic's chain, and a second, less checkable channel for the same conclusion is how that
protection erodes. Two competing initial records with no intervening accepted-then-moved head still fail
closed.

**Retirement is a read-time exclusion, never an edit.** The retired critic's records stay exactly as
posted, and re-inspecting them at the head that chain reviewed classifies it exactly as it always did.
Demoting the superseded record in place was considered and rejected: it is cheaper and mechanically
sound, but it rewrites another critic's durable evidence, which is what these rules exist to protect.
Retirement is also a tie-breaker **between** chains and never a re-classification of one — a single
accepted chain whose head has moved is refused exactly as before.

Be precise about what is observed: the acceptance's **structure**, not the truth of its accepted head.
Nothing verifies that an acceptance was genuine, so a forged one will retire a live chain. That is a
smaller hole than it looks. A forgery bound to the *current* head already yields an accepted chain
outright; reached from the same forgery, retirement yields only a chain awaiting a fresh acceptance at
the current head — strictly less — and it publishes the bogus value where a reader can see it. The
guarantee is relative: retirement grants an attacker who can forge acceptances no authority they did not
already have, and leaves evidence they would rather not leave.

The fresh chain's critic performs a genuinely full, independent review of the current head, never a
confirmation of the retired critic's finding and never a reuse of its verdict. **When the evidence is
absent** — an older acceptance that does not name its initial record — retirement cannot apply, and the
fallback is the one used before this mechanism existed: close the PR without merging, reopen the same
branch as a fresh PR so the new chain starts alone, and leave both original chains intact on the closed
PR. It costs a PR number, a re-posted obligations declaration and a re-issued `delivery` call, and loses
review-thread continuity, which is why it is the fallback and not the mechanism.

### Reading the review state: a designed wait is not broken evidence

`scripts/fsgg-coord review` reports one closed state and one next action. Two of those words mislead in
opposite directions, and the cost of misreading them is measured, so this is what a host reads instead of
re-deriving the lifecycle by hand.

**Structurally invalid evidence means the durable record is wrong — and only that.** A broken or missing
record, a missing critic identity, an unordered round sequence, an exceeded ceiling, missing runtime-route
evidence, an unresolved diff audit, an acceptance bound to the wrong head. The established recovery —
close the PR without merging and start a fresh chain — is destructive and irreversible, which is why the
verdict must never be read over a healthy chain. It was: PR #2514 was closed and reopened as #2528 on that
reading, costing a full fresh review.

**The post-acceptance window is designed, and it is not an error.** The chain is complete, host-accepted,
one critic, bound to the current head. The state carries the PR's live check word and the action follows
from it:

| checks | what it means |
|---|---|
| `pending` | **Every ordinary landing passes through here.** Make the one live `delivery` call `pnext-item` §6 places directly after acceptance; the required `claim-generation` context cannot report until that call writes the authorization marker, so waiting for green first is a cycle the marker can never break. |
| `red`, `conflicted` | The change is failing CI. That is a defect in the change, not in the review evidence — do not restart the chain. |
| `unknown` | The check state could not be **read**. Deliberately not grouped with `pending`, because waiting cannot improve it. Establish the real check state first: it is a no-verdict on the checks, not a wait, and not a finding against the change. |
| `merged`, `closed` | The PR is no longer open; no routine review action remains. |

Never reach for close-and-reopen from the post-acceptance window. Nothing about the evidence is wrong.

**A repair whose subject is a PR comment rather than the tree.** A critic's findings include release
obligations, and the obligations declaration is a standing artefact of every item, so a
`changes-required` whose subject is a comment body recurs by construction. The repair is then a comment
edit and the head correctly does not move. Measured on `.github#2534` / PR #2541: the round-1 finding was
that a `none` obligations declaration did not parse, the repair was an edit to that comment, and the
round-1 `pass` names the same head as the initial review. The engine cannot observe this — a comment's
current body is readable, but "it changed in answer to this finding" is not — so, exactly like
succession, it is an accountable grant the caller supplies, naming the review it answers, the candidate
head, the granter, and why the subject was a comment. Absent a grant, an unmoved head after
`changes-required` still routes to the implementer. The grant is refused when it names a different head,
answers a different review, carries no granter, or is granted by the implementing worker or by the
round's own critic: an implementer can never unlock its own round, and a critic can never manufacture the
trigger it will then confirm.

**Do not manufacture a no-op commit to satisfy the old rule.** A moved head was only ever a proxy for
"the implementer did work", and an empty commit satisfies the proxy while proving nothing. The grant is
strictly stronger, because it names an accountable third party who is neither of the two parties the
round is between.

### Repair phase

One bounded escalated attempt runs between an exhausted ordinary chain and the human park — not a fourth
round of the same chain, and not a substitute for the park if it too exhausts.

Entry is **automatic, and only after validated ordinary exhaustion**. A passing check, a new commit, or
an agent's judgement that the item is "nearly there" is not an entry trigger; the host verifies the exact
round chain and the escalation record before entering. On entry:

1. The exhausted PR is closed without merging. **Its counter is never rewound and never reused.**
2. A **separately scoped** PR opens with a fresh implementing worker and a fresh critic, both dispatched
   at the escalated route the invoking driver skill names — never chosen ad hoc by the host. The
   `-best`/`-normal` variants use their explicit repair-phase tables; the bare canonical `drive-board`
   and `work-board` use the corresponding `-best` repair route. If the active runtime cannot request that
   exact model and effort, the host applies the park steps above and records the unsupported route as the
   concrete human action required. **Never downgrade, substitute, or fall back.**
3. The new PR's initial review record carries the `repair-phase` fact naming the exhausted PR and its
   escalation record, so a reader can tell "landed after repair-phase escalation" from "landed normally"
   without reconstructing history.
4. The repair-phase chain is a **fresh** chain: round numbering restarts at one under the identical
   confirmation discipline — one fresh successor per repaired head, one round per repair, no skipped or
   duplicate numbers — but under the repair-phase ceiling, a distinct literal that is never conflated
   with the ordinary one. The repair phase never changes the ordinary ceiling for any other item.
5. A clean repair-phase result merges under the same acceptance and `landable` gates as any other PR.
   **The repair phase grants no shortcut around either.**
6. If material findings remain after its final confirmation, automation is exhausted a second and final
   time: the critic posts the escalation record on the repair-phase PR and the park steps apply verbatim.
   **There is no second repair phase**, and the human park remains the only terminal outcome an exhausted
   chain can reach.

Every entry — the trigger evidence, the escalated route used, the fresh critic's identity, and the
outcome — is recorded on both PRs and on the item, so a completion report cannot describe a repair-phase
landing as an ordinary one. A new commit or a passing check alone never resets either chain, and never
creates another repair phase.
