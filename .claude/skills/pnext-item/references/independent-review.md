# Independent review and material filing

Every item gets one independent critique cycle before merge. The implementer and critic are different
agents. The critic receives the issue, acceptance criteria, declared `Paths:`, exact PR head SHA,
complete diff, and test evidence; it does not receive the implementer's conclusions. The critic may
read code, history, issues, PRs, and the board, but **must not edit the implementation or push commits**.

The host reserves a slot for the critic and keeps the implementing worker alive until confirmation.
The critic reviews requirements coverage, correctness, regressions, tests/evidence, architecture and
ownership boundaries, release obligations, and touch-set honesty.

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
- `round-four-action: human-escalation`
- `human-escalation-sentinel: Blocked on: human/action`
- `repair-phase-entry: operator-authorized`
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

If the third confirmation still reports any unresolved material finding, automation is exhausted. The
critic posts one durable comment beginning with
`<!-- fsgg:independent-review-escalation:v1 -->` that names the current head SHA, all three ordered
confirmation URLs, the unresolved material findings and attempted repairs, and the concrete decision or
action required from a human. This is a fork, not always a park: without operator authorization for the
repair phase below, the worker or host proceeds directly through steps 1-4; with it, the repair phase
below runs first and this park is reached only if that phase also exhausts.

1. adds `Blocked on: human/action` to the issue body without disturbing its `Paths:` declaration;
2. records who, when, and why in an issue comment that links the escalation marker (and, if a repair
   phase ran first, the repair-phase escalation marker too);
3. sets `Status: Blocked` and releases the claim; and
4. stops without merging, filing a replacement review issue, or starting another automated round.

Only a human may retire that sentinel and decide whether to take ownership or change the acceptance
boundary. The exhausted PR cannot reset its counter or begin another automated cycle.

### Repair phase

One bounded escalated attempt is available between an exhausted three-round chain and the human park —
not a fourth round of the same chain, and not a substitute for the park if it too exhausts.

Entry is **operator-authorized, never inferred**: a declared parameter of the invocation, present
*before* the chain exhausts. An agent must not infer authorization from a passing check, a new commit,
or its own judgement that the item is "nearly there" — the same prohibition the escalation steps above
already state, binding here without exception. Absent explicit authorization, an exhausted chain goes
straight to the human park exactly as steps 1-4 describe; the repair phase never runs by default and
never runs retroactively on an already-parked item without a fresh authorized invocation.

When authorized:

1. The exhausted PR is closed without merging, exactly as the unauthorized path would leave it; its
   counter is never rewound and never reused.
2. A separately scoped PR opens with a **fresh implementing worker and a fresh critic**, both dispatched
   at the escalated route the invoking driver skill's own routing table names (the `-best`/`-normal`
   variants are the precedent for where that table lives) — never chosen ad hoc by the host. A driver
   invocation with no routing table of its own (the bare canonical `drive-board`/`work-board`, run
   without a `-best`/`-normal` override) has no escalated route to supply, so it cannot authorize entry
   into this phase; its exhaustion goes straight to the human park. When a route is available but the
   active runtime cannot request that exact model and effort, report the unsupported route and stop —
   never downgrade, substitute, or fall back — the same rule the routing tables already enforce for the
   ordinary chain.
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

Every entry — authorization, the escalated route used, the fresh critic's identity, and the outcome — is
recorded on both PRs and the item, so a completion report cannot describe a repair-phase landing as an
ordinary one. If a human later directs renewed automation on an item already parked with the sentinel
(rather than authorizing the repair phase before exhaustion), close the exhausted PR without merging and
start a separately scoped PR with a fresh critic and initial marker that links the escalation; absent
that explicit instruction, automation remains stopped. An agent must not infer permission from a new
commit or a passing check.

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
issue against GitHub before merge or terminal acceptance. An exhausted three-round chain is a human
escalation — via the repair phase above when authorized, otherwise straight to the park — never a
passing terminal acceptance either way. After verification of a passing chain (ordinary or
repair-phase), the host posts `<!-- fsgg:review-accepted:v1 -->` with the accepted head SHA, initial
review URL, and confirmation URL when a repair occurred, and — for a repair-phase landing — the
`fsgg:independent-review-repair-phase:v1` marker URL so acceptance evidence itself shows which path
the item took.
The worker must observe that exact-SHA host marker before calling `landable` or merging.
