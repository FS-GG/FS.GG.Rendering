# Independent review and material filing

<!-- BEGIN GENERATED: fsgg-protocol:review-policy -->
*Generated review contract. The marker parser and receipt validator consume these exact values.*

| fact | value |
|---|---|
| initial marker | `fsgg:independent-review:v1` |
| confirmation marker | `fsgg:independent-review-confirmation:v1` |
| host acceptance marker | `fsgg:review-accepted:v1` |
| escalation marker | `fsgg:independent-review-escalation:v1` |
| repair-phase marker | `fsgg:independent-review-repair-phase:v1` |
| ordinary repair ceiling | 3 |
| repair-phase ceiling | 10 |

**Quoted vs. competing markers:** A marker counts only as a canonical whole line inside a comment's own leading marker block — the run of lines from byte 0 that are each exactly one known marker's text. A marker occurring elsewhere as a quotation (inside a fence, an indented code block, or prose that only mentions it) is inert: it carries no evidence and raises no error by itself. The same marker kind occurring more than once within one comment's leading block is a competing marker and is refused — a marker kind has exactly one meaning, so no comment may carry it twice.

<!-- END GENERATED: fsgg-protocol:review-policy -->

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

Every item gets one independent critique cycle before merge. The implementer and critic are different
agents. The critic receives the issue, acceptance criteria, declared `Paths:`, exact PR head SHA,
complete diff, and test evidence; it does not receive the implementer's conclusions. The critic may
read code, history, issues, PRs, and the board, but **must not edit the implementation or push commits**.

The host reserves a slot for the critic and keeps the implementing worker alive until confirmation.
The critic reviews requirements coverage, correctness, regressions, tests/evidence, architecture and
ownership boundaries, release obligations, and touch-set honesty.

## Runtime-route evidence gate

Source review remains required, but it is not sufficient for a runtime-route divergence claim. When
the PR's requirements, claimed behavior, or a candidate finding concern runtime behavior reachable
through more than one meaningful route, the critic **must execute or measure** at least one comparison
through the production route against the built artifact. The comparison must observe the behavior that
could diverge (for example, a player input route and its direct dispatch), rather than merely assert
that the source implementations look equivalent.

The critic records the built artifact, command or measurement, compared routes, and observed result in
the review report with `Verification:`. A report that cites only source reading for such a claim is
incomplete; it cannot be accepted as evidence that the routes agree. If no meaningful production-route
comparison exists for the review subject, the critic states that boundary and why under `Verification:`;
that exception does not waive the rest of the required source review.

Every **passing** initial or confirmation marker carries exactly one machine-readable applicability
shape; a `changes-required` marker may carry one, but cannot confer acceptance without a later passing
marker that does. The meaningful shape is:

```text
route-applicability: meaningful
built-artifact: <artifact exercised>
executed-command: <command or measurement performed>
compared-routes: <production route and comparison route>
observed-result: <observed equality or divergence>
```

The not-meaningful shape is:

```text
route-applicability: not-meaningful
route-not-meaningful-reason: <bounded reason tied to this review subject>
```

Missing, duplicate, empty, unknown, mixed-shape, or overlong reason fields fail the live review-marker
parser. A prose claim or `Verification:` line does not substitute for these fields; source-only review
therefore cannot produce a valid passing chain when the critic declares the comparison meaningful.

This is reusable guidance, not an audio-specific recipe. Rogue3 exposed the shape when a built product
route emitted `[]` while direct dispatch emitted `[PlaySfx (SoundId "floor-descend", 0.8)]`: the cue map
looked correct in isolation, but executing both routes revealed the defect. Apply the same comparison
discipline to any reachable behavior whose routes can diverge.

## Gate-inversion evidence — every gate the change touches must be shown it can fail

A gate that has never been red is equally consistent with "nothing was ever wrong" and "it cannot
fire". Reading cannot separate those; only breaking the subject can. `.github#2223` measured **ten
instances in one run, across six items and four repositories**, of a gate that passed green and could
not fail on the thing it claimed — and every one was found by a critic that inverted the gate rather
than read it. `.github#1610` found the same class three months earlier and nothing generalised from
it. So this is a numbered step, not a virtue some critics happen to have.

1. **Inventory the gates the change touches.** A gate is anything whose purpose is to refuse: a test,
   an assertion, a fixture case, a checker script, a workflow step, a schema or parser rule. The
   inventory is bounded to gates the diff **adds or modifies** — never the whole suite.
2. **Invert each one exactly once, by breaking its SUBJECT.** Break the thing the gate claims to
   protect — not the gate's own predicate — and run the suite. Record the exact mutation and the exact
   observed result under `Verification:`. One mutation per touched gate is the bound; this step is
   cheap by design and must not grow into a suite-wide sweep.

   **Predicate inversion is not an equal alternative, and on its own it proves nothing.** Negating a
   gate's own comparison reds necessarily, whatever the gate is pointed at. It demonstrates that the
   assertion is reachable and executed — never that it is connected to its subject, which is the only
   thing in question. `C4` in step 4 is the proof: it stayed 30/30 green under a subject mutation, so
   predicate inversion would have certified as `JUSTIFIED` the exact decorative gate this section
   exists to catch. `scripts/gate-mutate.py` breaks subjects for precisely this reason — it does not
   read gates. Where a subject mutation genuinely cannot be constructed, say so, record predicate
   inversion as the strictly weaker evidence it is, and grade the gate `NOT_MEASURED` — never
   `JUSTIFIED`.
3. **A surviving inversion is material by definition.** A gate that stays green on a mutant that broke
   its subject is a material finding — not a judgement call, not a style note, and not something a
   later round may absorb silently.
4. **The test that claims a property must be the test that provides it.** Where a test is named for a
   property, break *that test* and confirm the property is lost. Where the property is really provided
   elsewhere, record which test provides it. A property named by one test and provided by another is
   worse than an untested one: refactor or delete the real provider and the guarantee disappears while
   the named test still reports the coverage. `FS.GG.Governance#385` measured exactly that — `C4` is
   named for collision detection, but breaking `collisions()` outright left it 30/30 green, because
   the guarantee comes from `C3`'s ownership loop.
5. **A fixture must reproduce the production condition the gate will actually face.** A demonstration
   run against a world simpler than production is evidence about the fixture. Where a fixture cannot
   reproduce the condition, the demonstration records what it does not reproduce rather than omitting
   it.
6. **A measurement must not be taken in an environment that supplies what production would lack — and
   must prove it did not.** Where the change concerns a tool, binary, or ambient environment
   dependency, neutralise the confound and guard the measurement so that it **aborts** when the
   confound is still present. A prose claim that the `PATH` was stripped is not that proof; a run that
   self-aborts unless the tool is absent is.
7. **A repair must catch the escape that was actually found.** When a mutation got through, the
   repaired demonstration must red on *that* mutation — not merely pass, and not merely red on some
   newly added one. A repair that adds cases without re-running the original escape has not been shown
   to close it.
8. **When a repair strengthens what a gate ASSERTS, re-check the evidence for the strengthened
   assertion.** Making a gate's claim broader, more specific, or more confident changes what must be
   proved, and the evidence that supported the weaker claim does not carry forward to the stronger one.
   On `FS.GG.Templates#349` a round-1 repair strengthened a failure message to say the complete
   published set had been compared — *"so this is not a search-depth artifact"* — while the predicate
   still asked only whether a ceiling had been hit. A skipped download then produced a confident
   upstream accusation in better prose than before the repair. **A gate that gains eloquence faster
   than correctness is worse than one that stayed vague**, because the vague one did not invite
   reliance on a claim nothing checked. So the reviewer re-derives the evidence for what the gate now
   says, not for what it said when the evidence was gathered — and a strengthened assertion with
   unchanged evidence is a material finding, exactly like a surviving inversion.

### The sub-shape a happy-path mutation does not catch

Mutating a gate's success path leaves one whole class untouched: **a non-answer reported as a
confident answer.** Three of `.github#2223`'s instances are this and nothing else — the literal
`always` returned itself and so matched no template, and was graded *not selected*; a row with `scope`
**absent** fell through to *somebody else's row*; an unreadable form was graded a confident negative.
In each, the gate received something it could not interpret and emitted a definite verdict about it.
Inverting the happy path leaves all three green. So invert the **unreadable input** as well: feed the
gate a form it cannot parse and confirm it refuses rather than decides (`#266` — "I could not evaluate
this" is never "I evaluated it and it passed").

Two further shapes share one root — **the world under test is not the world shipped into** — and they
run in opposite directions, so no single rule catches both:

| sub-shape | the test world | how it deceives |
|---|---|---|
| fixture simpler than production (`FS.GG.Templates#379`) | **less** capable | the demonstration passes for a reason production does not supply |
| environment richer than production (`FS.GG.Templates#392`) | **more** capable | the subject passes for a reason production does not supply |

The second is not hypothetical: an ambient global `fable` on the default `PATH` made the **unfixed**
`run-cross-runtime.sh` exit 0 with 31 `cross-runtime: OK` — a green byte-identical to the fixed code's,
same count of the same assertions. No mutation of the code surfaces that, because the code is not what
differs. Steps 5 and 6 are the two remedies, and they are not interchangeable.

Finally, **a gate's stated limits are part of what the critic verifies.** An overclaimed limit is how a
real gap becomes invisible: grading workflow *files* is not evidence that the context executed
anything, because `if: false` on every step and `continue-on-error` are each green and each invisible.

### Worked example

`FS.GG.Templates#379`'s lane-coverage guard states at `tests/composition/lib/lane-coverage.sh:37-38`
that it exists *"so that deleting the step reds too"*. It was implemented as an **unanchored**
`grep -qF 'tests/composition/run.sh'`, and `composition.yml` carries that string twice: once at the
real invocation, and once inside a `#` comment. Replacing the real invocation with
`echo skipped   # was: tests/composition/run.sh` left the gate reporting `✓ lanes: 3 of 4`, `FAIL=0`.
Deleting the invocation did not red it.

It already had a self-demonstration — 22 offline outcomes, including a counter-inversion case that
genuinely works — so **step 2 was already satisfied and the defect still escaped**. It escaped because
the synthetic fixture workflow carried no comment mentioning the suite path while the real
`composition.yml` does: step 5's condition, not step 2's.

The repair is the worked form of steps 5 and 7. The obvious fix — "ignore comments" — is wrong, because
the repo names that path in prose three ways and the third is not a comment at all but prose inside a
`run: |` block scalar. **When a gate's false positive comes from prose, the fix is not to enumerate the
shapes prose takes**; prose has more shapes than anyone enumerates. The fix is to match the structure
of the thing being asserted, which here is position. Three fixture properties followed, and they
generalise:

1. every fixture carries the decoys the real artifacts carry;
2. detached and attached fixtures differ in exactly one line, so the pair is a controlled experiment
   rather than two separately-authored worlds — which is how the original fixture became simpler than
   production without anyone noticing; and
3. every detach case is paired with a reattached control that must go green, so a fixture that reds for
   the wrong reason is not read as a passing demonstration.

The demonstration went 22 → 25 outcomes, and the acceptance measurement for this whole class is the
first line of its result: **reverting only the anchor now gives `FAIL=1`, which the old demonstration
could not detect.**

### What the critic records

For each touched gate the review marker names the mutation applied and the observed result; for each
gate whose inversion could not be obtained, the reason — which is `NOT_MEASURED`, never a pass; and,
where a property is provided by a test other than the one named for it, that provider.
`scripts/gate-mutate.py` is this org's harness for the sweep, and its verdict vocabulary is the
vocabulary to use: `JUSTIFIED` fired, `DECORATIVE` could not fire, `NOT_MEASURED` obtained no
measurement. A critic report that records no inversion for a change that touches a gate is incomplete
in the same way a source-only runtime-route claim is.

## Handoff-assertion provenance

Every specific, checkable assertion in an implementation handoff, critic report, or host relay carries
`Verification:`. Give the command, `file:line`, API call, or URL actually used to establish the fact,
or write exactly `Verification: unverified`. `unverified` is first-class and non-pejorative: it makes
an unchecked claim legible without requiring every claim to be checked. A receiver must not infer
verification from prose.

Use this review checklist before forwarding or accepting a handoff: for every checkable assertion,
verify that the `Verification:` field is present and contains either a reproducible basis or
`unverified`. A missing field is a detectable incomplete handoff, never evidence that the assertion was
checked. This requirement binds the host when relaying worker or critic claims onward, as well as the
worker and critic who authored them.

## Root cause, dedupe, and materiality

For every candidate finding, the critic searches the relevant code and history for the cause, then
searches open and closed issues, PRs, comments, and the board for that cause rather than only the
surface symptom. Reuse an existing item when it already carries the cause and add the new evidence
there.

A finding is **material** only when the evidence shows at least one of:

- acceptance criteria are unmet, or observable correctness, compatibility, security, data integrity,
  performance intent, or releaseability is at risk;
- a test or gate can report green without checking its declared subject;
- a gate the change adds or modifies stays green when inverted, or a test named for a property does
  not itself provide that property (`.github#2223`) — see **Gate-inversion evidence** above, which
  makes this a finding by definition rather than a judgement call;
- an architecture or ownership violation creates a concrete defect or blocks safe evolution;
- bounded hardening prevents a measured recurring failure, retry, operational burden, or meaningful
  maintenance cost; or
- the item ships or claims reachable game functionality with no passing bot-driven headless player
  journey (`.github#2087`) — see **Game functionality** below.

## Game functionality — the bot-driven player journey gate

This gate is **blocking**, not advisory. When an item ships or claims reachable game functionality,
the critic verifies a passing bot-driven headless player journey exists and reviews the journey
itself, not only its result: whether the messages used are genuinely player-emittable and whether
the start point is genuinely the product's entry. Absence of that evidence is a material finding by
itself, never a style note — a green suite that never boots the product cannot distinguish "works"
from "unreachable" (`2026-08-02-Rogue3.md` §4.3: eleven consecutive `shipReady` verdicts preceded
the human launch that found an unreachable starting room).

A journey is evidence only when driven **through the product's real input surface** — the same
control messages a player emits. Direct `Msg` injection, a test-only API, or any seam that exists
solely for tests is **not evidence**; a journey using one is rejected by this gate, not merely
discouraged in review prose. A journey must **boot at the product's real entry point** and reach the
functionality by navigating as a player would — seeding a mid-game model, or claiming the
functionality "reached" from such a seed, is a gate failure regardless of whether the reducer state
afterward looks correct. The item states which functionality each journey covers; functionality
named by the item that no journey reaches is reported as uncovered, never silently absent.

Where the product's entry point is not yet test-ownable, the critic returns `changes-required` and
records that the gate cannot run and why, rather than treating the absence as a pass — fail closed,
not pass by absence.

One advisory input is explicitly **not** consumed as blocking here: `FS.GG.Game#563`'s
`DegenerateVocabulary` check fires unconditionally on declared-vocabulary cardinality alone, so it
flags a legitimately single-inhabitant slot with zero `Unbound` arms. A `DegenerateVocabulary`-only
finding, with no accompanying `Unbound`-arm evidence, is not by itself material under this gate.

Style, naming taste, speculative edge cases, optional refactors, “could be cleaner” observations, and
findings already repaired in the current PR are not material new work. Record them in the review
comment when useful, but **never create an issue, board row, blocker edge, or follow-up queue entry for
them**. Uncertainty is not materiality; measure or omit.

## Disposition and repair bounds

These machine-readable literals are part of the review contract:

- `max-automated-repair-rounds: 3`
- `round-numbering: 1-based`
- `round-four-action: automatic-repair-phase`
- `human-escalation-sentinel: Blocked on: human/action`
- `repair-phase-entry: automatic-after-ordinary-exhaustion`
- `repair-phase-max-rounds: 10`
- `repair-phase-round-numbering: 1-based`
- `repair-phase-exhausted-action: human-escalation`
- `repair-phase-marker: fsgg:independent-review-repair-phase:v1`

The critic posts one durable PR comment beginning with
`<!-- fsgg:independent-review:v1 -->`. It names the reviewed head SHA, critic identity, verdict, and
each finding's evidence, root cause (or explicitly bounded unknown plus measurements), duplicate-search
result, materiality, and disposition.

The implementing worker repairs material findings that belong in the current PR. The same critic
reviews each repaired head in a reply beginning with
`<!-- fsgg:independent-review-confirmation:v1 -->` and naming the initial review comment URL and
confirmation SHA, the 1-based `round` number, the preceding review or confirmation URL, and every
remaining material finding. There is exactly one initial marker and at most three ordered confirmation
markers. Each confirmation must advance the round by one and review the exact head produced by that
repair; duplicate round numbers, skipped rounds, competing markers, a changed critic, or a fourth
automated repair fail closed. When no repair is required, an initial `pass` whose reviewed SHA equals the
candidate head is itself the confirmation; no repair round or second marker is required. Allow at most
three repair-and-confirmation rounds. Every round addresses material findings only; do not iterate on
minor observations. Before routing any repair, the host validates the current chain and permits it only
when the latest round is less than three; this count-before-routing gate prevents a failed third
confirmation from racing into repair four while the escalation writes settle.

If the third confirmation still reports any unresolved material finding, the ordinary chain is exhausted. The
critic posts one durable comment beginning with
`<!-- fsgg:independent-review-escalation:v1 -->` that names the current head SHA, all three ordered
confirmation URLs, the unresolved material findings and attempted repairs, and the remaining repair
objective. The host closes the exhausted PR without merging and automatically enters the repair phase
below. Steps 1-4 are reached only if that phase also exhausts or its required route is unavailable.

1. adds `Blocked on: human/action` to the issue body without disturbing its `Paths:` declaration;
2. records who, when, and why in an issue comment that links the escalation marker (and, if a repair
   phase ran first, the repair-phase escalation marker too);
3. sets `Status: Blocked` and releases the claim; and
4. stops without merging, filing a replacement review issue, or starting another automated round.

Only a human or the automatic repair-phase transition may retire that sentinel; a human alone may
change the acceptance boundary. The exhausted PR cannot reset its counter or begin another automated
cycle. An already parked item whose evidence proves an ordinary three-round exhaustion is automatically
eligible for that transition on the next board-driver pass: the host removes the sentinel, sets
`Status: Ready`, records the transition, and dispatches the repair phase without human interaction.

### Repair phase

One bounded escalated attempt runs between an exhausted three-round chain and the human park —
not a fourth round of the same chain, and not a substitute for the park if it too exhausts.

Entry is **automatic only after validated ordinary exhaustion**. A passing check, a new commit, or an
agent's judgement that the item is "nearly there" is not an entry trigger. The host verifies the exact
three-round marker chain and escalation marker before entering. An already parked item with that valid
evidence enters automatically on the next board-driver pass; the transition records why it cleared the
human-action sentinel and resumed automation.

On automatic entry:

1. The exhausted PR is closed without merging; its
   counter is never rewound and never reused.
2. A separately scoped PR opens with a **fresh implementing worker and a fresh critic**, both dispatched
   at the escalated route the invoking driver skill names — never chosen ad hoc by the host. The
   `-best`/`-normal` variants use their explicit repair-phase tables; the bare canonical `drive-board`
   and `work-board` use the corresponding `-best` repair route. If the active runtime cannot request
   that exact model and effort, the host applies steps 1-4 and records the unsupported route as the
   concrete human action required; never downgrade, substitute, or fall back — the same rule the
   routing tables already enforce for the ordinary chain.
3. The new PR's initial review comment carries `<!-- fsgg:independent-review:v1 -->` as usual. The same
   comment, or an accompanying one, additionally carries `<!-- fsgg:independent-review-repair-phase:v1 -->`
   naming the exhausted PR and its `fsgg:independent-review-escalation:v1` marker URL, so a reader can
   tell "landed after repair-phase escalation" from "landed normally" without reconstructing history.
4. The repair-phase chain is a **fresh** chain: round numbering restarts at 1 and follows the identical
   confirmation-marker discipline as the ordinary chain (same critic across its own rounds, one round per
   repair, no skipped or duplicate round numbers) — but its ceiling is `repair-phase-max-rounds: 10`, a
   distinct machine-readable literal from `max-automated-repair-rounds: 3` (total automated attempts
   before a terminal park: 3 + 10 = 13). The two literals are never conflated, and the repair phase
   never changes the ordinary chain's limit for any other item.
5. A clean repair-phase result (an initial `pass`, or a confirmation with no remaining material finding)
   merges under the same `fsgg:review-accepted:v1` and `landable` gates as any other PR; the repair phase
   grants no shortcut around either.
6. If material findings remain after the repair phase's own tenth confirmation, automation is exhausted a
   **second and final** time. The critic posts `<!-- fsgg:independent-review-escalation:v1 -->` on the
   repair-phase PR, and steps 1-4 above apply verbatim to it. There is no second repair phase and no round
   beyond `repair-phase-max-rounds`: the human park is reached from at most one repair-phase attempt, and
   remains the only terminal outcome an exhausted chain can reach.

Every entry — the automatic trigger evidence, the escalated route used, the fresh critic's identity,
and the outcome — is
recorded on both PRs and the item, so a completion report cannot describe a repair-phase landing as an
ordinary one. A new commit or passing check alone never resets either chain or creates another repair
phase.

The critic may file new work only when all of these are true:

1. the finding is material by the definition above;
2. it is a distinct root cause that cannot remain reviewably inside the current PR;
3. no existing issue already carries that cause; and
4. the evidence and acceptance boundary are sufficient for another worker to act.

The critic—not the implementer—owns filing for review-discovered findings. It files directly in the
root-cause repository, adds observed behavior, root cause or measured unknown, impact, acceptance,
verification, a narrow `Paths:`, `Class:` and `Phase`, adds the item to the correct board, and sets
`Status: Backlog` unless it is a genuine blocker. Cross-repo work follows
[cross-repo-coordination](../../cross-repo-coordination/SKILL.md). Review findings never enter the
critic's or worker's private follow-up queue.

Class the filed cause from evidence: `defect` when observed behavior violates a current contract or
acceptance boundary; `hardening` when no current contract is broken but bounded preventative work
addresses a measured recurring risk or cost. A finding that still needs human judgement is not
actionable enough for critic filing; surface it to the host.

If a filed material issue blocks the current item, the critic reports it to the host; the worker sets
the real `Blocked by` edge, parks the item `Blocked`, releases the claim, and stops. Otherwise the
critic returns `pass` only after every material finding is repaired, deduplicated, or filed. The host
verifies the marker, ordered round/URL/SHA chain, critic independence, dispositions, and every filed
issue against GitHub before merge or terminal acceptance. An exhausted three-round chain automatically
enters the repair phase above; only unavailable routing or repair-phase exhaustion reaches the human
park, and neither exhaustion is a passing terminal acceptance. After verification of a passing chain (ordinary or
repair-phase), the host posts `<!-- fsgg:review-accepted:v1 -->` with the accepted head SHA, initial
review URL, and confirmation URL when a repair occurred, and — for a repair-phase landing — the
`fsgg:independent-review-repair-phase:v1` marker URL so acceptance evidence itself shows which path
the item took.
The ordinary marker's required machine fields are `accepted-head: <exact SHA>`,
`initial-review: <initial review comment URL>`, and
`latest-confirmation: <latest confirmation comment URL>`; when no repair occurred,
`latest-confirmation` equals `initial-review`. Missing, duplicated, stale, or differently linked fields
fail closed.
The worker must observe that exact-SHA host marker before calling `landable` or merging.
