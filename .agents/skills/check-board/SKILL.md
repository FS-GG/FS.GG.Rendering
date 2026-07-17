---
name: check-board
description: Reconcile the org-level FS-GG "Coordination" Projects v2 board against the repos' real issue state, re-verify that every recorded blocker still holds, and find the epics that are sitting open with all their work finished. Use when the board looks wrong or stale, before a planning pass, before fanning workers out with intra-repo-parallel-work, or when `next`/`take` says "nothing to do" and you doubt it. Reports every discrepancy; with --apply it fixes the board-side ones. Gathers the judgements it cannot make — should this epic close, was this flip premature — puts them to a human in one batch, and writes the answers back. Never edits an issue unprompted. Canonical protocol lives in FS-GG/.github.
---

# check-board (FS-GG)

The Coordination board is a **projection**. GitHub issues, their `state`, and the `fsgg:claim`
markers are the **ground truth**; the board's `Status`, `Phase`, and `Blocked by` are a cached
view of it that humans and `fsgg-coord next`/`take` read to decide what happens next. The board
drifts, and the ADR-0034/ADR-0040 engine drifts it in three ways this pass hunts. **A REFUSED write
is not replayed by anything** — an unknown field, a `Blocked by` that is not a ref: the engine rejects
these outright, because replaying them could never succeed (#510). An **exhausted** write is the
opposite case and must not be confused with it: it is *queued*, and `fsgg-coord flush` replays it
(#878). Nothing flushes automatically, though, so a deferral nobody flushes is still a write that
never landed. Second, `done --flip` flips `Status` only once it sees a merged PR. Third, and the
one that rots quietly: **nothing re-checks a `Blocked by` edge when its blocker closes.** So a
drifted board hands out work that is already done, hides work that is startable, and keeps items
"blocked" behind issues that closed weeks ago.

This skill is the **reconcile pass** over that drift. It answers three questions:

1. **Is the board in sync with the issues?** (and if not, fix the board — never the issue)
2. **Do the recorded blockers still hold?** (a `Blocked by` edge is a claim about the *present*)
3. **Are there epics whose work is all finished, still sitting open?** (§4 — same rot, one graph over:
   nothing re-asks an epic whether it can close, exactly as nothing re-checks a cleared blocker)

Related: [cross-repo-coordination](../cross-repo-coordination/SKILL.md) owns the protocol the board
implements; [intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md) owns claims and
touch-sets. This skill only *reconciles*. Decisions: ADR-0001 (the board), ADR-0027 (the claim).

## The one rule: the issue is the truth, the board is the copy

**Fixes only ever write to the board.** Auto-remediation may set `Status`, add a missing item to
the board, or release an expired claim. It may **never** close or reopen an issue, delete a
`Blocked by` edge, merge a PR, or touch a working tree. When ground truth and the projection
disagree, the projection is what changes.

Corollary: an issue is not "done" because the board says `Done`. `fsgg-coord done` exists precisely
to make the stamp *earned* (PR merged **and** `Status: Done`), and this skill never fakes it.

### The one exception, and its exact shape: an ANSWER licenses a write

The rule above is about what this pass may decide **on its own**, and that is what it still says: this
pass never writes to an issue **unprompted**. What §5 adds is a human in the loop — a specific
question, carrying its evidence, answered in the session by a person who can be wrong and knows it.
**That answer, and nothing else, licenses the write it names.** A `yes` on "close FS.GG.Game#211?"
closes FS.GG.Game#211 and authorises nothing else.

Four constraints keep that from becoming a hole big enough to drive #561 through:

- **The pass may only ask for the judgement the tooling genuinely cannot reach.** Where a rule exists,
  the rule decides. A question that re-opens a settled rule is a way of voting the rule down, and the
  answer is not a licence — it is a bypass with extra steps. #1003 states this in its own terms: the
  remedy for un-checkable acceptance "is a declaration, never an escape sentinel — one that let an
  author assert *it is all delegated* would be a loophole that PAYS its user, and #889 would have
  taken it." A chat `yes` is that sentinel unless it is fenced this narrowly.
- **Every question carries its evidence** — the children and how each one closed, the acceptance
  lines, the offending token. A question a human answers by trusting the asker is a rubber stamp, and
  the guards here exist because people rubber-stamp.
- **Silence is never consent.** Unanswered, skipped, ambiguous, or answered with a question back → the
  finding stays report-only and the pass says so in the summary. "I could not look" is not "I looked
  and it is fine" (#266), and that applies to looking at a human.
- **The answer gets recorded on the issue**, not just acted on (§5). An unrecorded decision is how the
  roll-up's own refusals evaporate.

## Run it

```sh
scripts/fsgg-coord budget                      # free; are we near the GraphQL cap?
/check-board                                   # DRY RUN — report every finding, ask nothing, write nothing
/check-board --apply                           # ...fix the board-side ones, and ASK the §5 questions
/check-board --repo rendering                  # scope to one repo (registry short-id)
/check-board --apply --no-ask                  # apply the mechanical fixes; leave every judgement report-only
```

Dry run is the default, as it is for `reap` and `coordination-sync --check`. Read the findings
before you let anything write.

**A dry run never asks.** The questions in §5 exist to license writes, so asking them in a mode that
cannot write would train a human to answer questions that do nothing — and the next batch, the one
that *does* close issues, gets the same reflex. If you want to see what would be asked without
answering it, a dry run lists the questions; it just does not put them.

## The rules this pass enforces

**These are the engine's, and they are GENERATED. So are five of the §2 finding codes below —
`STALE-CLAIM`, `CLAIM-STATUS-LAG`, `CLOSED-ISSUE-NOT-DONE`, `BLOCKER-CLEARED` and `STATUS-NOT-BLOCKED`
are `Chore.fsi`'s, each carrying its remedy AND its deference rules as a type. Read them there before
you re-spell one here.**

This line used to say the §2 codes were *"this skill's own"*, and that premise is what kept them
hand-rolled. It was true until Phase 4.3 and false after it: `Chore` owns those five outright. The
skill re-derived what the engine already knew, and got `CLAIM-STATUS-LAG` **backwards** — the engine's
own docstring named the bug, with the remedy, before anyone found it (#1035). `Chore` is dead code
with a live test suite pending #733's wiring, so the correct rule ships, is tested, and nothing reads
it, while the hand-rolled `jq` below is what actually runs. That is #889's thesis exactly.

What is still **this skill's own**: the ordering of the passes, the apply/dry-run policy, and the
report-only refusals — decisions about what to *do* with a finding, not about what is *true*. A
reconciler that gets a truth wrong enforces it wrong across the whole board.

If you are re-spelling one of these in `jq`, spell it once — and spell it the way `Chore.fsi` states it.

<!-- BEGIN GENERATED: fsgg-protocol:reconcile-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  These rules were restated by hand here, and a RECONCILER restating them is the sharpest form
  of the problem: this pass exists to correct the projection, so a rule it gets wrong is a rule
  it enforces WRONG across the whole board. The hand-written copies agreed with each other for
  as long as they existed (#916). Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that resolves your blockers is the engine that wrote
this. The full rule set, with the incident behind each one, is in
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**A MERGED blocker is RESOLVED; an unreadable one BLOCKS**

`Blocked by` clears on CLOSED **or MERGED**. It does not clear on OPEN, on a blocker whose state could not be read (unverifiable), or on prose that is not an issue ref at all (unparseable) — all three BLOCK.

**A read that did not happen may never render as a confident answer**

An error, an empty result, and a legitimate "no" are three different facts. A failed board scan is not an empty board; a failed marker read is not an unheld item; an unread issue body is not an undeclared touch-set. Every one of them fails CLOSED and says which it was.

<!-- END GENERATED: fsgg-protocol:reconcile-rules -->

## 1. Snapshot (three reads, whole board)

Take the snapshot **once** and classify from it. Do not re-query per item — a scan is a full-board
read, and the whole point of `fsgg-coord` is that it costs ~3 points instead of ~2,500.

```sh
scripts/fsgg-coord scan --fresh --include-backlog > /tmp/scan.json  # EVERY item, incl. Done — AND its blockers
scripts/fsgg-coord lint --json            > /tmp/lint.json   # touch-sets, epic invariants, the Done/open
                                                            #   note, and the §4 roll-up candidates
scripts/fsgg-coord who  --repo <r> --json > /tmp/who.json    # live claims, per repo
```

**`--fresh` is not optional, and it is the one flag `scan` needs that `ready` never did.** `scan` is
the **scheduler's** read, so it is served from a 90s shared cache (`FSGG_COORD_SCAN_TTL_SEC`) — that
cache is why N looping workers cost one board scan instead of N. But this pass is a **truth** read,
and the kit is explicit about the difference: the cached reads are `next`/`take`, while
`ready`/`lint`/`who`/`reap` and *the `/check-board` snapshot* scan fresh, **"because a reconciler
serving a cached 'truth' reports drift that was already fixed."** Omit `--fresh` and you can
reconcile against a board up to 90 seconds stale — inventing findings a co-worker just repaired, and
missing an item added moments ago. That is not theoretical: a cached scan taken seconds after an
issue was boarded did not contain it, and the same scan with `--fresh` did.

**Take the snapshot from `scan`, NOT from `ready` — this is the trap that silently disarms the
whole pass.** `ready --all --json` looks like the right read and is not: it emits **only**
`number, repo, state, status, title`. It carries **no `blockers`**, no `blockedBy`, no `blocked`,
no `phase`. So `select(.blockers | length > 0)` over a `ready` snapshot matches **nothing**, §3
finds zero blocker findings, and the pass reports a *clean board* precisely when blockers have gone
stale. That is a **false clean** — the worst output this skill has, because it buys confidence in
the projection instead of correcting it. It is not hypothetical: it hid three cleared blockers on a
1,059-item board while `ready` reported everything in order.

`scan --include-backlog` is a strict superset for this pass — same item count, plus blockers, plus
each issue's `body` (which §2's touch-set note needs). It emits an **envelope**, not a bare array,
so every jq starts at `.items`:

```json
{ "schema": "fsgg.coord.snapshot/1", "allowBacklog": true, "leaseMinutes": 90, "limit": null,
  "inFlight": [ ... ],
  "items": [
    { "owner": "FS-GG", "repo": "FS.GG.Rendering", "number": 752, "status": "Blocked",
      "state": "OPEN", "body": "…Paths: …",
      "blockers": [ { "owner": "FS-GG", "repo": "FS.GG.Game",  "number": 321,
                      "raw": "FS.GG.Game#321",  "state": "closed" },
                    { "owner": "FS-GG", "repo": "FS.GG.Audio", "number": 106,
                      "raw": "FS.GG.Audio#106", "state": "closed" } ] } ] }
```

Two shape details that will bite you if you skim:

- **`blockers[].state` is lower case** — its values are the table below, generated from the
  engine that writes them. Compare against `"CLOSED"` and it never matches, so every blocker
  classifies as still-holding and every finding vanishes. (`ascii_upcase` in §3 is for **REST**,
  which is a different read with the opposite convention. Do not "unify" them.)
- **The ref field is `raw`, not `ref`** — `.blockers[].raw` is `"FS.GG.Game#321"`. `.ref` yields
  `null`, which prints as a finding you cannot act on.

<!-- BEGIN GENERATED: fsgg-protocol:blocker-states -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy NAMED its own source — "the five cases of the engine's `BlockerState`"
  — and was still a copy. Naming a source is not reading it. Generatable only since #1012 gave
  the vocabulary an owner in Core; before that it was two private INVERSE copies outside it, and
  typing the cases into Protocol.fs would have been a THIRD (#865).

  TWO sources, because this region states two different facts. The `.state` strings and the case
  list come from Types.fs (`BlockerState`, `blockerStateWireName`) — that is where a NEW state or
  a renamed one belongs. The `holds?` bit and the prose are Protocol.fs. Regenerate either way.
-->

*Generated from the typed core: `Types.blockerStateWireName` writes these strings, so the engine
that emits `.state` is the engine that wrote this table. Lower case, deliberately — an issue's own
`state` is UPPER case on the wire and a blocker's is not, and the two conventions are not to be
"unified" (§3).*

| `.state` | holds? | what it means |
|---|---|---|
| `open` | **YES** | The blocker is open. It HOLDS. |
| `closed` | no | The blocker issue is closed. It does not hold — the work it named is finished or abandoned. |
| `merged` | no | The blocker is a MERGED pull request. It does not hold. A rule that cleared only on CLOSED would unblock when the PR was ABANDONED and block forever once it was FINISHED — the gate opening precisely when the work is thrown away (#476). |
| `unknown` | **YES** | The ref parsed and its state could not be read. It HOLDS: "I could not look" is not "I looked and it is fine" (#266). Usually an off-board ref the scan could not resolve — board it, and it becomes `open` or clears. |
| `unparseable` | **YES** | The `Blocked by` text is not an issue ref at all. It HOLDS: prose in a dependency field is a question nobody answered, and a field this pass cannot read is not a field it may declare empty. |
<!-- END GENERATED: fsgg-protocol:blocker-states -->

`scan` resolves an off-board ref **over REST itself**, in the scan, and says how many on stderr
(`scan: 1059 candidate(s); 0 off-board blocker(s) resolved`) — you no longer have to. What it
cannot resolve stays `unknown`, which **holds** — see the table.

## 2. The findings

Each finding has a code, a ground truth, and a fix — or an explicit refusal to fix.

| Code | Condition | Fix (`--apply`) |
|---|---|---|
| `CLOSED-ISSUE-NOT-DONE` | **no live claim**, `state == CLOSED`, and `status != Done` | `set-field --batch <i> Status=Done` |
| `DONE-STATUS-OPEN-ISSUE` | `status == Done` and `state == OPEN` | **ask** (§5) — is the work done, or was the flip premature? |
| `OFF-BOARD-ISSUE` | open `roadmap` issue in a rostered repo with no board item | `fsgg-coord add <i>` — idempotent; see the note below |
| `BLOCKER-CLEARED` | **no live claim**, every blocker `closed` **or `merged`**, but `status == Blocked` | `set-field --batch <i> Status=Ready` |
| `BLOCKER-UNKNOWN` | a blocker ref `scan` could not resolve | resolve over REST (§3), then board the blocker if it is open |
| `BLOCKER-UNPARSEABLE` | a `Blocked by` token is not an issue ref | **ask** (§5) — what did the prose mean? `Blocked by` is text, so the answer can be written |
| `STATUS-NOT-BLOCKED` | **no live claim**, an open blocker, but `status` is `Ready`/`Backlog` | `set-field --batch <i> Status=Blocked` |
| `STALE-CLAIM` | `who` says `state == "stale"` | `reap --repo <r> --apply` |
| `UNCLAIMED-IN-PROGRESS` | `who` says `state == "unclaimed"` | **ask** (§5) — someone is working outside the protocol; only a human knows who, and whether to park it |
| `CLAIM-STATUS-LAG` | held claim, and board `status` is one of `Ready`/`Backlog`/*(no status)* — the columns a claim SHOULD have overwritten. A held `Blocked`/`In review` is the holder's own decision and is **deferred, not reconciled** (#331) | `set-field --batch <i> "Status=In progress"` |
| `UNDECLARED-PATHS` | open, unclaimed, not `Done`, and the issue body declares no `Paths:` | **ask** (§5) — the fix is an *issue* edit, so it takes an answer |
| `EPIC-*` (`error`) | from `lint --json` — an epic that **cannot** roll up | **report only** — a mechanical remedy, and no answer substitutes for it (§4) |
| `EPIC-ROLLUP-READY` | from `lint --json` (`note`) — every precondition to roll up holds, epic still open | **ask** (§4/§5) — the close needs a `Discharge` judgement, and only a human has it |

**AN ITEM YIELDS AT MOST ONE FINDING THAT WRITES ITS COLUMN, and the claim is what splits them.**
That is `Chore.fs`'s structure, not a style note: it matches on the claim first, and the two halves
never overlap.

- **A live claim reserves the item**, and only the two rules that act on the MARKER may fire:
  `STALE-CLAIM` (lapsed lease) and `CLAIM-STATUS-LAG` (live lease). They are mutually exclusive *on
  the lease state*, so at most one. Neither writes a column the reserver did not already own.
- **No claim** — then, and only then, the three that write the column directly: `CLOSED-ISSUE-NOT-DONE`
  (needs `Closed`), `BLOCKER-CLEARED` (needs `Blocked`, every blocker resolved), `STATUS-NOT-BLOCKED`
  (needs `Ready`/`Backlog`, one blocker open). Pairwise disjoint on facts you can see, so at most one.

**Drop the claim check and this table contradicts itself.** An item that was `Ready`, claimed, and
blocked yields BOTH `CLAIM-STATUS-LAG` (*"set In progress"*) and `STATUS-NOT-BLOCKED` (*"set
Blocked"*) — two writes of opposite columns to one item, with the winner decided by whichever ran
last. `ChoreTests` asserts the invariant over every (status × claim × blocker × issue-state)
combination with no kind excluded; this table has no such test, which is exactly why the claim check
is written into each row above rather than left to be remembered.

The remaining classes (`UNCLAIMED-IN-PROGRESS`, `DONE-STATUS-OPEN-ISSUE`, `BLOCKER-UNKNOWN`,
`BLOCKER-UNPARSEABLE`, `UNDECLARED-PATHS`, `EPIC-*`, `EPIC-ROLLUP-READY`, `OFF-BOARD-ISSUE`) are
outside this rule, and they are this skill's own rather than `Chore`'s. **They used to be outside it
because they wrote no column. That is no longer the reason, and the difference matters:** an *answered*
question writes (§5) — `DONE-STATUS-OPEN-ISSUE` judged premature puts `Status` back, and
`EPIC-ROLLUP-READY` answered `yes` writes `Status=Done`. So they must earn their place in the
invariant rather than sit outside it by definition.

Most earn it **on status**, which is the same fact the rows above already turn on: the three
column-writers need `Closed` / `Blocked` / `Ready`-`Backlog`, while `DONE-STATUS-OPEN-ISSUE` needs
`Done` and `UNCLAIMED-IN-PROGRESS` needs `In progress`. Pairwise disjoint, so at most one.
`UNDECLARED-PATHS` and `BLOCKER-UNPARSEABLE` write an issue body and a text field; neither touches a
column.

**`EPIC-ROLLUP-READY` is the exception, and it is a real overlap.** It is gated on the sub-issue graph,
not on status — so a `Blocked` epic whose blocker has cleared and whose children are all resolved
yields **both** `BLOCKER-CLEARED` (*"set Ready"*) and `EPIC-ROLLUP-READY` (*"set Done, and close"*).
Two writes of one column, which is precisely what this rule forbids.

It is resolved **by order, not by disjointness**: §6 applies the answers last (step 4), so `Status=Done`
and the close land over the `Status=Ready` from step 3, and the item settles in the right state either
way. This is the one place in this skill where *"whichever ran last"* is the design rather than the bug
above — because here the last writer is a **human's adjudication** and the first is a mechanical
inference, and which of those two outranks the other is the whole of §4. Do not "fix" it by gating
`EPIC-ROLLUP-READY` on status: an epic's board column is a projection, and its children being finished
is a fact about the world.

**Always write with `set-field --batch`, never the single-field form.** On the engine the fleet is
**pinned** to, `set-field <ref> <field> <value>` **cannot write any single-select**: it declares its
GraphQL variable `$optionId: ID!` while `ProjectV2FieldValue.singleSelectOptionId` is a `String`, so
the server refuses the query on a type mismatch (`.github#848`). `Status`, `Phase`, `Repo Scope`,
`Workstream` and `Effort` are **all** single-selects, which is every field this table writes. The
form still works for text fields (`Blocked by`, `Contract`) — which is why the break is easy to
miss, and why this table prescribed the broken spelling for every flip in it until `#848` was found.

`--batch` builds a different document and is **immune either way**, which is why it is the standing
instruction rather than a workaround to retire. `.github#857` **has** repaired the single-field form
at the source — and that repaired it for **nobody but `.github`**. This repo builds the engine from
source (ADR-0034 §4.3), so it picked the fix up on merge; every other repo restores the **pinned**
`FS.GG.Coord.Cli` from `dist/dotnet/.config/dotnet-tools.json` and keeps the bug until a release
carries `#857` and the pin flips (as `#852`/`#853` did for 0.2.0). So **"is it fixed?" has two
answers at once**, and which one is true depends only on where you are standing — a merged fix is
not a distributed one (`#846`). `--batch` is correct in both, and is the cheaper spend regardless:
it writes N fields in **one aliased mutation** (`#448`) against a budget every worker shares.

**Board an off-board issue with `fsgg-coord add <i>`, and never with the raw `gh project` call.**
The client is metered and cached; a recipe that reaches past it is an unmetered principal on the
5,000/hr budget the whole fleet shares, which is why `check-graphql-monopoly` **fails the merge** of
any skill or doc carrying `gh project item-add` as a runnable line (`#418`, `#586`). `add` was
missing from the ported engine for a while and that rule had no compliant path — the gate's own
remediation named a verb that exited 1 — but `#870` has restored it, so the honest spelling and the
enforceable one are the same again.

Same two-answers caveat as `--batch` above, for the same reason: `#870` is *merged*, not
*distributed*. `.github` builds from source and has `add` now; a receiver restores the **pinned**
engine and will get `unknown command: add` until a release carries it and the pin flips (`#846`). If
that is where you are standing, this class is report-only until then — say so rather than reaching
for the raw call, which the gate will refuse anyway and which is unmetered for a reason.

**`add` is idempotent, and it is safe to `--apply` — but not because adding twice is harmless.**
Adding an already-boarded issue is a **no-op**: it prints the existing item id and exits 0, with no
twin created (`addProjectV2ItemById` is idempotent server-side — measured on the live board in
`#870`, not inferred). If you have read this skill before, note the correction: it used to say a
second add **creates a duplicate**. That was `#421`'s *counterfactual* — "a duplicate would have been
created had I followed that remediation" — hardening into an assertion as it was copied inward, and
it does not reproduce (`#871`).

What `#421` is actually about survives intact, and it is the part that matters: **the only thing
licensing the mutation is a successful read that found nothing.** An error is a read that did not
happen, and unreachable is not absent (`#266`). Adding on a *failed* read spends a mutation against a
budget that just refused a query and reports success for an issue whose absence was never
established — a definite answer built on no information. `add` now enforces that itself, so
`--apply` may board what your snapshot genuinely shows off-board; if the scan failed, you have no
finding to act on in the first place.

`CLAIM-STATUS-LAG` reads off **one command, at one instant** — `scan --fresh --include-backlog`, which
§1 already mandates. Every claimed item carries a `claim` object *atomically with its `status`*:

```json
{ "number": 931, "status": "In progress",
  "claim": { "worker": "godwit-5b49", "ageSeconds": 1933, "prevStatus": "Backlog",
             "liveness": { "kind": "lease-held" } } }
```

**Do not join `scan` against a separately-timed `who`.** This section used to, and the join is both
unnecessary and wrong: two reads are two *instants*, so a claim landing between them reports as lag
that never existed. Measured on a busy board: **4 raw hits, 0 real** — two items were claimed between
the `scan` and the `who` (#1035).

**`liveness.kind == "lease-held"` is the live-claim predicate** — the same condition `who` calls
`held`. A lapsed lease is `STALE-CLAIM`'s, not this one's, and `lease-expired-pr-open` is a worker
demonstrably still working (#581); neither is a lag.

**Only the columns a claim SHOULD have overwritten qualify: `Ready`, `Backlog`, and `""` (no status).**
A holder who set `Blocked` or `In review` *during* the lease made a decision, and #331 is the rule
that a column set deliberately during a lease still wins — so those are **deferred, not reconciled**.
Reconciling them would overwrite the holder's own judgement with a default: the drift this pass
closes, running backwards. This is not hypothetical — the rule here fired on `.github#1004`, a live
item whose holder had parked it `Blocked` with *"this is a decision, so I am parking it rather than
working it"*, and `--apply` would have overwritten that park with `In progress` (#1035). `Chore.fsi`
states this rule, with its reasoning; it is the owner, and this is its restatement.

Before you repair a real one by hand, run `scripts/fsgg-coord flush --dry-run`: if that write is
sitting in the deferred queue, an exhausted budget merely *paused* it, `flush` is the repair, and
reconciling it here would duplicate the write rather than fix it (#878). What this pass repairs is the
lag that is **not** queued — a `Status` write that was refused outright, or one whose worker walked
away without flushing. Write it:

```sh
# `""` IS the wire form of "no status" (Types.fs: statusWireName NoStatus = ""), for `status` and for
# `prevStatus` alike — so render it, don't `//` it. jq's `//` only catches null, and an empty string
# is truthy: `.prevStatus // "—"` prints nothing at all for the very column it exists to name.
jq -r 'def col: if . == null then "—" elif . == "" then "(no status)" else . end;
  .items[]
  | select(.claim.liveness.kind == "lease-held")
  | select(.status == "Ready" or .status == "Backlog" or .status == "")
  | "CLAIM-STATUS-LAG  \(.owner)/\(.repo)#\(.number)  held by \(.claim.worker)"
    + ", board says \(.status | col)"
    + " (claim overwrote \(.claim.prevStatus | col))"
' /tmp/scan.json
```

`UNDECLARED-PATHS` is the class that makes a **full queue look like an empty one**. An item with no
`Paths:` cannot be scheduled — `take`/`batch` refuse it, because an undeclared touch-set cannot be
proven disjoint from another worker's — so it sits on the board *looking* startable while every
worker who asks for work is told there is none. `.github` reached **twelve** such items at once, all
filed through the org's own recipe, and `/pnext-item` reported a dead queue over a full one
([#442](https://github.com/FS-GG/.github/issues/442)). The board is not wrong here — the *issue* is —
so the remedy is an issue-body edit, which is why this class **asks** (§5) rather than fixing itself:
what the touch-set should be is a fact about work nobody has done yet, and the pass has no way to
derive it. Ask for the paths; an answer writes them. Unanswered, report it and let the claimant
`claim` then `widen`, which is the ordering ADR-0021 requires anyway (the marker is the CAS; the body
is not) — and that ordering is why an unanswered question here is cheap: the claimant fixes it for
free on the way in.

The touch-set lives in the issue body, which the REST issue list already carries — so this costs
**no extra call**:

**Do not re-grep for `Paths:` — ask the tool.** A hand-rolled `test("(?m)^Paths:")` is a **fourth**
parser of a grammar that already has three, and it is the loosest: it gets the fence rule and the
`Paths: none` sentinel wrong — both of which `touch-set-declaration` states above — and it is
case-sensitive where the tool is not. `lint` applies the real grammar, and since `#520` it also
reports a touch-set that is *declared but unusable*:

```sh
scripts/fsgg-coord lint --repo <r> --json \
  | jq -r '.[] | select(.code == "NO-TOUCH-SET" or .code == "BAD-TOUCH-SET")
           | "\(.code)  \(.id)  \(.detail)"'
```

`NO-TOUCH-SET` = nothing was declared. `BAD-TOUCH-SET` = something was, and **every token of it is
unmatchable** — a token that matches no file conflicts with nothing, so `batch` refuses the item and
no worker can ever pick it up. Both are the same death; only the diagnosis differs. Both **ask** (§5),
and they are the clearest case for asking: the pass can prove the item is unschedulable and cannot
guess the paths that would fix it, because that is a fact about work nobody has started.

`DONE-STATUS-OPEN-ISSUE` is never auto-fixed in either direction, and `lint` calls it a *note* rather
than an error for the reason: `Done` over an open issue is how a premature flip looks, **and** how
"merged, issue left open for the release note" looks. Closing the issue and reverting the status are
both destructive, and only a human knows which happened — so this is a **question**, and one of the
best-shaped in the pass. Both branches are one write, the evidence fits on a line, and the answer is
something the person who flipped it knows instantly.

## 3. Re-verify the blockers (this is the half people skip)

A `Blocked by` edge is a claim about *now*, and nothing re-checks it when the blocker closes. Items
rot in `Blocked` behind issues that shipped. This is the class a **migration** manufactures in bulk:
a release that closes a batch of issues silently clears the edges that named them, and every item
those edges gated stays `Blocked` — advertised as unstartable, skipped by `next`, invisible as work.
For every item with `blockers`:

```sh
jq -r '.items[] | select(.blockers | length > 0)
  | "\(.repo)#\(.number)  [\(.status)]  " + ([.blockers[] | "\(.raw)=\(.state)"] | join(" "))' /tmp/scan.json
```

Then, per blocker state (lower case — see §1):

- **`closed`** / **`merged`** — does not hold. If *every* blocker is resolved, the item is startable:
  flip `Blocked` → `Ready`. **Leave the `Blocked by` field alone.** It is provenance, and a resolved
  ref costs nothing — the scan and `next` both ignore it. Deleting the edge destroys history to fix
  a `Status` that was the actual problem.
- **`open`** — holds. If the item's `status` is not `Blocked`, that is `STATUS-NOT-BLOCKED`; the
  board is lying to a human reading the column (`next` skips it correctly either way).
- **`unknown`** — *the scan could not resolve the blocker.* Do not trust it. Resolve it over
  **REST** — `gh issue view` is GraphQL, and spending the budget here is spending the budget this
  pass needs to write its own fixes in §6:

  A blocker ref reads `owner/repo#n`; REST wants the parts, so split it. Keep `html_url` — it is the
  field that tells an issue from a PR, which is exactly what `blocker-resolution` above turns on.
  The scan hands you the parts already (`.blockers[]` carries `owner`, `repo`, `number` beside
  `raw`), so you rarely have to parse the ref yourself:

  ```sh
  # FS-GG/.github#449  ->  owner=FS-GG  repo=.github  n=449
  gh api repos/FS-GG/.github/issues/449 \
    --jq '"\(.state | ascii_upcase)  is_pr=\(.pull_request != null)  \(.html_url)  \(.title)"'
  # OPEN  is_pr=true  https://github.com/FS-GG/.github/pull/449  [adr] …
  ```

  **Mind the two case conventions — they are opposite, and that is not a bug to "fix".** REST emits
  `open`/`closed` lower case for an *issue's* state, while an item's own `state` in the snapshot is
  `OPEN`/`CLOSED` upper case. `ascii_upcase` above normalises the REST answer to the item convention
  so it can be compared with `.state`. A **blocker's** state is the third convention — lower case
  (§1) — so compare it against `"closed"`, never `"CLOSED"`. Get any of these backwards and the
  comparison silently never matches: every blocker classifies as still-holding, and the pass reports
  a clean board.

  `is_pr` is not a curiosity either: an **off-board blocker is very often a PR** (an ADR PR gating
  an issue). `gh issue view` renders a PR as an issue without saying so — only the URL (`/pull/<n>`)
  gives it away — so it hides the one fact you need to read the blocker correctly. REST states it
  outright, and costs no GraphQL.

  `CLOSED` (or a merged PR) → treat as `BLOCKER-CLEARED`. `OPEN` → the blocker is genuine but
  **off-board**, so `next` will refuse this item forever and never say why in a way you can act on.
  Fix the *cause*: board the blocker (`fsgg-coord add <i>` — see §2, and mind its note on where that
  verb exists yet), turning `unknown` into `open`. The remedy is the same whether the blocker is an
  issue or a PR; only the diagnosis gets clearer.
- **`unparseable`** — prose or a placeholder leaked into the field before it was validated. Report
  it with the offending token. A human re-writes the field with refs, or clears it — and `Blocked
  by` is a **text** field, so the single-field form is the one that works here:
  `fsgg-coord set-field <i> 'Blocked by' ''`.

## 4. The epics: is this one finished?

An epic closes when its children discharge it. `done --flip` does that climb — and **it only climbs
when a worker stamps a child.** So the epic whose last child was closed by hand, or whose stamping
worker walked away, or whose child was closed as a duplicate, is never asked the question at all. It
sits open with every child resolved, advertising work that does not exist, and `next` offers it to
nobody. This is §3's rot one graph over: nothing re-asks an epic whether it can close, exactly as
nothing re-checks a `Blocked by` when its blocker closes.

**You cannot read why an epic is open.** The roll-up's refusals are `ParentLeftOpen` values — they
render to the terminal of the worker who ran `done --flip`, and are **never recorded on the issue**.
So an epic sitting open with all children closed is telling you nothing: it may have been refused for
a named reason five weeks ago, or never asked. Do not go looking for that reason in the comments;
recompute it.

### Ask the tool, not the graph

The verdict is `lint`'s, because that is where the sub-issue graph already is. No verb hands you an
epic's children — `scan` rows carry a title and a status, not a graph — so there is nothing to write
the jq against, and that is the good outcome. A hand-rolled version would be a second spelling of a
rule carrying a guard per incident (#614, #613, #325, #965, #1003), and a copy drifts toward
optimism, which is the direction that closes things (#485, #864).

```sh
scripts/fsgg-coord lint --repo <r> --json \
  | jq -r '.[] | select(.code | startswith("EPIC-"))
           | "\(.severity)  \(.code)  \(.id)  \(.detail)"'
```

Two kinds come back, and the split is the whole section:

- **`error` — the epic CANNOT roll up.** `EPIC-NO-CHILDREN`, `EPIC-CHILDREN-TRUNCATED`,
  `EPIC-UNLINKED-CHILD`, `EPIC-UNDELEGATED-ACCEPTANCE`, `EPIC-NO-STATED-ACCEPTANCE`. Each names a
  mechanical defect with a mechanical remedy — link the child, state the acceptance, re-read the
  truncated page. **These are report-only, and they are not close questions.** No answer changes them:
  an unlinked child is still unlinked after a human says "close it", and the acceptance nobody stated
  is still unread. Asking a human to close over one is asking them to be the escape sentinel #1003
  refused to build.
- **`note` — `EPIC-ROLLUP-READY`.** Every mechanical precondition holds: the graph is whole, every
  child is resolved, the acceptance is stated and fully delegated, no declared child is missing. This
  is a **candidate**, and it goes to §5 as a question.

### Why a clean epic is a question and not a close

This is the one place the instinct to just close it has to be resisted, and the reason is specific.

`EPIC-ROLLUP-READY` says every *mechanical* precondition holds. It does not say the epic is done,
because "do these children discharge this parent?" is not a mechanical question — the engine's own
types say so. `Discharge` (#614) is an **argument**, with no default, that a caller must supply:

> It is an ARGUMENT because it is a fact only the child's author knows, and no amount of reading the
> board can recover it.

FS.GG.SDD#350 needed an ADR *and* a code change. A worker split the disclosure-only half out as #398,
whose body said **in bold** that it did *not* complete #350, and linked it as a sub-issue exactly as
the recipe instructs. When #398 merged, the roll-up saw "all children complete", closed #350, and
climbed a hop further to stamp an epic `Done` over it. **None of the parent's actual work existed.**
Children do not partition their parent.

Nothing in `EPIC-ROLLUP-READY` can see what #398's body said in bold. The finding reads the graph and
the acceptance lines; #398 was a linked, closed child like any other, and the one fact that mattered
was prose in a *child*. So:

**A `ROLLUP-READY` epic and an epic the roll-up deliberately refused are indistinguishable from
here.** The guard against #614 is `done --flip --partial "<why>"`, where the *worker* declares that
their child does not complete its parent — and that refusal is a `ParentLeftOpen` that was never
written down. An auto-close on `ROLLUP-READY` would therefore close, in silence, precisely the epics a
worker took the trouble to protect. It re-creates #614 while reading green.

So every candidate becomes a question. **Bias the recommendation toward closing** — an epic with every
child resolved and its acceptance delegated is usually finished, that is what the finding means, and a
human who has to argue *for* closing will leave the board rotting. Recommend the close, make `yes` the
cheap answer, and put the evidence next to it. But take the answer, and take a `no` at face value: the
`no` is the only thing standing between this pass and #350.

### The climb is not yours to make

`done --flip` climbs after it closes: the parent it just closed is a resolved child of *its* parent,
so it asks the next hop up. **A close made here does not climb** — it goes through REST, and nothing
is watching.

Do not simulate the climb. The grandparent will surface as `EPIC-ROLLUP-READY` on the next pass, with
its own evidence and its own question, and a human will adjudicate that hop too. One hop per pass, one
judgement per hop, is the correct rate: #614's damage was done by a climb that took *two* hops on one
inference. A pass that closes a chain of four epics from a single `yes` has re-invented exactly that.

## 5. The decisions: gather, ask once, write the answers down

The classes marked **ask** are the ones where the pass can prove something is wrong and cannot know
what to do about it. Left as report-only they accumulate: a summary naming five of them is a summary
nobody acts on, which is how they survive to the next pass, and the pass after that.

### Gather first. Ask nothing mid-scan.

Collect every question during §§1–4 and **put none of them until the read is finished**. Two reasons,
and both have teeth:

- **A pass that blocks mid-scan strands the board between two consistent states.** §6 exists because
  order matters; a human who wanders off at question 3 of 11 leaves half of it applied. Gather, ask,
  then apply as one movement.
- **The batch is itself evidence.** "Close these six epics?" is a different question from six separate
  "close this epic?" — the shape of the batch is what tells a human that a release closed a wave of
  children, or that one worker's items all rotted at once. Asking serially hides the pattern that
  makes the answers obvious.

### What a question has to carry

Never ask a question the tooling can answer — that is a rule being voted on, not a judgement being
made (§*The one exception*). Every question states the finding, **the evidence**, the recommendation,
and exactly what gets written on each answer:

| Class | Ask | Evidence it must carry | `yes` writes |
|---|---|---|---|
| `EPIC-ROLLUP-READY` | "Close this epic?" | every child, and **how each closed** — merged PR, duplicate, `ResolvedWithoutPr` — plus the acceptance lines | board `Status=Done`, then close the issue, then record (§5, “Write the decision down”) |
| `DONE-STATUS-OPEN-ISSUE` | "Done, or premature flip?" | the closing PR if any; who flipped it | *done* → close the issue; *premature* → `Status` back to the real one |
| `NO-TOUCH-SET` / `BAD-TOUCH-SET` / `UNDECLARED-PATHS` | "What does this item touch?" | the title, and for `BAD-TOUCH-SET` the tokens that match nothing | the `Paths:` line into the issue body |
| `BLOCKER-UNPARSEABLE` | "What did this prose mean?" | the offending token, verbatim | refs into `Blocked by`, or clear it |
| `UNCLAIMED-IN-PROGRESS` | "Who is on this?" | the item, and that no `fsgg:claim` marker exists | park it, or leave it and note the worker |

A child that closed as a **duplicate** or with `ResolvedWithoutPr` is the evidence that most changes
an answer, so it is never summarised away. "All four children merged PRs" and "two merged, one was a
duplicate, one was closed by nobody" are different epics, and only the second is a real question.
`ClosedByNobody` — closed, with nothing recording why — is the strongest reason to answer `no` that
this pass can hand anybody.

### Write the decision down, or it evaporates

An answer that is only *acted on* leaves the next reader exactly where §4 left you: an issue whose
state has no recorded reason. **The roll-up's refusals evaporate for precisely this reason** — do not
reproduce the defect in the fix for it.

So every applied answer gets a comment on the issue, naming what was decided, by whom, on what
evidence, and that `/check-board` was the instrument:

```sh
# 1. board first, then the issue — #613: a parent stamped Done on the board with the issue left OPEN
#    makes the next hop up read an OPEN child, and the board and the issue disagree about one thing.
scripts/fsgg-coord set-field --batch FS.GG.Game#211 Status=Done

# 2. record the judgement BEFORE the close — a comment on a closed issue is easy to miss, and if the
#    close fails you have still written down what was decided.
gh api -X POST repos/FS-GG/FS.GG.Game/issues/211/comments -f body='…'

# 3. close over REST. `gh issue close` is GraphQL against the budget the fleet shares; this is 0 pts,
#    and it is the spelling `check-graphql-monopoly` prescribes for every issue write.
gh api -X PATCH repos/FS-GG/FS.GG.Game/issues/211 -f state=closed
```

The comment says what was judged, not that a tool ran:

> **Closed by `/check-board`** — epic roll-up adjudicated by @ehotwagner on 2026-07-17.
> All 3 children resolved: #204 (merged PR #219), #205 (merged PR #221), #206 (closed as duplicate of
> #205). Acceptance in the body is fully delegated to those three; no un-delegated criterion.
> Judged: these children **discharge** this epic (#614). Roll-up never ran because #206 was closed by
> hand, so nothing climbed.

**An unanswered question is report-only, and the summary says so** (§7). Silence is not a `yes`; a
human who skipped question 4 did not decide question 4.

## 6. Apply, in this order

Order matters — later steps read state the earlier ones changed.

```sh
scripts/fsgg-coord budget                             # 0. is there budget to finish? a pass that dies
                                                      #    half-applied leaves the board worse than found
scripts/fsgg-coord reap --repo <r>                    # 1. dry run: whose lease expired?
scripts/fsgg-coord reap --repo <r> --apply            #    release them (tells the reaped worker)
scripts/fsgg-coord add <i>                            # 2. off-board issues + off-board blockers
scripts/fsgg-coord set-field --batch <i> Status=<V>   # 3. the status flips (--batch, always — #848)
                                                      # 4. the §5 answers — board write, comment, then
                                                      #    the REST close (§5 has the spelling)
scripts/fsgg-coord scan --fresh --include-backlog     # 5. RE-READ — --fresh, or you re-read the
                                                      #    90s cache you already have (§1)
```

Step 0 is a budget check, and it does **not** replace the `flush` that used to lead this list —
`flush` is back (#878), and a queue this pass does not drain is drift it will "find" and then
duplicate. Run `scripts/fsgg-coord flush --dry-run` alongside step 0: it reads the queue rather than
the board, and a non-empty queue means some of the drift below is already owed rather than lost. What
can still bite you is running *out* of budget partway through, which strands the board between two
consistent states — so look before you start, and if `budget` is thin, do step 1 and stop.

Step 4 is where the answers land, and it is **last among the writes** for the reason step 0 exists: the
mechanical fixes are replayable and an issue close is not. If the budget dies during step 3, you re-run
and the board converges; if it dies halfway through a batch of closes, some epics are closed, some are
not, and nothing records which were adjudicated. Get the cheap, reversible, board-side writes done
first, then spend the answers.

Step 5 is not optional, and `--fresh` is what makes it a re-read rather than a replay of the
snapshot you already hold (§1). Boarding a blocker in step 2 re-resolves it from `unknown` to
`open`/`closed`, which can create or clear a `BLOCKER-CLEARED` finding that did not exist in your
snapshot. Reclassify from the fresh scan, then apply any newly-earned status flips.

Do **not** re-run `lint` after step 4 hunting for epics that became `ROLLUP-READY` because you just
closed their children. That is the climb, and §4 says why it is not yours to make: the next hop is a
new judgement, on evidence a human has not seen, and taking it inside the same pass is how one `yes`
becomes four closes. Those epics surface on the next `/check-board`, which is the correct rate.

## 7. Confirm, and report what you did not touch

```sh
scripts/fsgg-coord lint --repo <r>       # NOT necessarily exit 0 — see below
scripts/fsgg-coord ready --repo <r>      # the board a human will now read
scripts/fsgg-coord budget                # what the pass cost
```

**A red `lint` here is not a failed pass, and this line used to imply it was.** `lint` exits non-zero
on any `error`-severity finding, and the `EPIC-*` errors are **report-only by design** (§4) — their
remedy is an issue-body edit this skill will not make. So a pass that ran perfectly, over a board with
one epic whose acceptance is prose, ends with `lint` exiting 1. Measured, not hypothetical:
`lint --repo .github` exits 1 today on `.github#729`, whose body states no task-line acceptance while
two of its six children are still open.

Read the findings, not the exit code. What must be true at the end is that **every error still standing
is one you named in the summary** — a red you can account for, line by line. A red you cannot is the
thing to chase.

Finish with a summary in **four** parts, and the fourth is the one that rots if you drop it:

1. **Fixed** — the board-side writes, by class.
2. **Decided** — each §5 question, the answer, and the write it licensed. A human who answered eleven
   questions gets to see what their answers did, in one place, without reading eleven issues.
3. **Left alone** — the report-only classes (`EPIC-*` errors, and any `UNCLAIMED-IN-PROGRESS` parked).
   A pass that quietly "succeeded" while leaving five of them standing is how they survive to the next
   pass. Name them, with their issue refs.
4. **Asked and unanswered** — every question that got no decision, named as such. This is not the same
   list as *left alone*, and collapsing the two is what makes the pass dishonest: "nobody has ruled on
   this yet" and "this needs a mechanical fix nobody has done" are different facts, and only the first
   one is waiting on the person reading your summary. An unanswered close question means **the epic is
   still open** — say it in those words, because "asked about 6 epics" reads as though something
   happened to them.

**Never report a silent cap.** If you scoped to one repo, or stopped after N fixes, say so — a
partial reconcile that reads as a full one is worse than no reconcile, because it buys false
confidence in the projection. The same applies to the questions: if you asked about four epics because
the batch was getting long, the other two are **not** findings you reported, and a summary implying
otherwise is the false clean of §1 wearing a different hat.

## Setup

- Board writes need `gh auth refresh -s project,read:project`; `reap`/`release` need `issues: write`.
- `--repo` takes a registry short-id (`sdd`, `rendering`, `governance`, `templates`, `game`,
  `audio`, `.github`), `owner/repo`, or a bare repo name. The roster is `registry/repos.yml`;
  `scripts/repos.sh list --all` enumerates it (bare `list` is a usage error — it wants `--all` or
  `--receives <cap>`, so that a capability sweep cannot silently widen to the whole roster).
- Unscoped, this is a whole-org pass over every board item — one full scan, plus one `who` per repo.
- **In `.github` the engine is built from source, never restored from the feed** (ADR-0034 §4.3), and
  this repo keeps no `.config/dotnet-tools.json` of its own — the manifest it *distributes* lives at
  `dist/dotnet/.config/`. So the shim finds no engine here until you build one:
  `dotnet build src/FS.GG.Coord.Cli -c Release`, then point
  `FSGG_COORD_ENGINE_BIN` at the **apphost** (`…/net10.0/fsgg-coord-engine`, not the `.dll` — the
  shim requires an executable). Receivers restore the pinned engine instead and need none of this.
