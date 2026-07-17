---
name: pnext-item
description: Claim the next schedulable item assigned to THIS FS-GG repo and take it all the way to merged and done-stamped, then keep going. Use when you are a worker (agent or person) picking up work in a repo, especially one of several running in parallel. Wraps the intra-repo-parallel-work protocol — worker id, comment-order claim lock, per-item git worktree, disjoint `Paths:` touch-set — then implements, opens a PR, reviews it, merges on green, and earns the done-stamp. A problem it finds along the way is FIXED, in the same PR when that keeps the change reviewable; what it files, it files at the ROOT CAUSE rather than the surface, and then TAKES — landing the current item first, then popping its own follow-up by number, recursively, until the queue drains. It files-and-leaves only when the finding is genuinely not fixable from here — another repo owns it, or it needs a decision a human has to make. Canonical protocol lives in FS-GG/.github. See ADR-0001, ADR-0021 and ADR-0027.
---

# pnext-item (FS-GG)

One command's worth of intent: **"give me the next thing to work on in this repo, and don't collide
with the other workers."** It is the driver for
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md), which owns the protocol — read it
if any step below surprises you. This skill is the *loop*, start to done-stamp — and then **round
again**, because a done-stamp is not the end: what you found while working the item is the best-sourced
work on the board, and §6 pops it rather than letting `take` re-roll for it.

**Two rules run through the whole loop, and they are the same rule.** When you find something: **fix the
CAUSE, not the surface** (§4) — the surface is where a defect showed up, not where it lives, and a fix to
the surface is a diff that regenerates. And when you cannot fix it here: **file the cause, then take it**
(§4 case 2 → §6) — you are the only worker who has the context, so hand the work to yourself rather than
to a scheduler that never saw the problem. The recursion is bounded by the done-stamp: **one item in
flight, ever**.

Safe to run N times concurrently in one repo. That is the whole point: the claim lock is a
server-side total order over comment ids, so exactly one worker wins each item, and `take`
re-schedules a loser rather than sending it home.

```sh
/pnext-item                     # claim + work the next schedulable item in this repo
/pnext-item --repo game         # ...in another repo (registry short-id)
/pnext-item 186                 # ...that specific item (uses `claim`, not `take`)
/pnext-item FS-GG/game#186      # ...qualified — takes whatever `claim` takes, and §6 pops this form
```

**The third form is how §6 takes your own follow-up, and it buys that with the scheduler's guarantee.**
`claim` does **not** check your touch-set against live claims — **only `take` does** — so on this form
`widen`'s exit code is the only collision check you get. Run it, and believe a non-zero.

## 0. Be someone before you take anything

N agents authenticate as the **same GitHub account**, so `@me` cannot tell two workers apart. The
lock, the channel, and attribution all key on a **worker id** instead.

```sh
scripts/fsgg-coord whoami       # your id, and which rule produced it
```

**If `whoami` warns, stop and fix it.** It warns when the id came from a shared checkout or from a
harness session id — and on Claude Code every subagent of a session shares one
`CLAUDE_CODE_SESSION_ID`, so a fan-out silently collapses onto **one id**. That is the same-account
bug one level down, and it defeats the lock you are about to take.

**Mint the id with the tool. Do not invent one, and do not copy one out of a document** — run this
verbatim, before anything else:

```sh
eval "$(scripts/fsgg-coord whoami --mint)"
```

This is the **one** mint idiom across the tool, the protocol doc, and both skill roots. It is the line
`whoami`'s own warning prints, so the thing the tool tells you to run is the thing written here.

**Why minted and not chosen** is the first of the four rules you are driving — stated below (the
*claim-lock* rule) with the lease you hold and the `Paths:` line you author, because all four are the
engine's, and this recipe restated them by hand for five repairs before it stopped (#1059). **No
literal id appears anywhere in these docs for you to copy** (#551) — the attractor is the word, not
the suffix, which is why the id is the tool's to mint and not yours to pick.

Until `claim` refuses a marker whose `worker=` duplicates a live one (the tool half of #419), **the id
scheme is advisory** — a mint you skip is a lock you do not have.

### The rules you are driving

<!-- BEGIN GENERATED: fsgg-protocol:driver-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  This recipe restated these rules by hand for its whole life, and #1059 counted the cost: five
  repairs to four sentences, every one of them the PROSE moving to where the engine already was.
  The two that were closed by generating a table have never drifted again. Two hand-written
  copies also disagreed OUT LOUD — §3 said an expired lease cannot be renewed and §6 said
  `heartbeat` renews it — and nothing could see it, because neither was the engine's answer.
  Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that takes your claim, holds your lease and refuses
your `Paths:` line is the engine that wrote this. The full rule set, with the incident behind
each one, is in [intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**The touch-set grammar — it is NOT a glob language**

supported: an exact path ('src/Foo.fs'), or a directory prefix ('src/Foo', 'src/Foo/*', 'src/Foo/**'). There is no glob matcher: a leading '**/' or an interior '*' matches nothing — spell the paths out.

**The claim lock is a comment-order CAS, and the ASSIGNEE cannot hold it**

A claim is an `fsgg:claim` marker COMMENT, and the lowest live marker id wins. GitHub issues comment ids from one server-side sequence, so "lowest live marker" is a total order every racer observes identically. The GitHub ASSIGNEE cannot be the lock, because N agents share one account. That total order is over MARKERS, and it separates WORKERS only while their ids are DISTINCT: an id two workers share is an id this lock cannot separate, and `release`, `heartbeat`, `say` and `inbox` then act on one another's claims. So a worker id is MINTED, never chosen — a worker asked to pick one is not a random source.

**The lease is a WINDOW, and an unknown age says so**

A claim's lease is 120 minutes by default (`FSGG_CLAIM_LEASE_MIN`), and `heartbeat` renews it only while it is LIVE. Past it the claim is REAPABLE — not free: only `reap` may break a lock, and an item's touch-set stays reserved until it does. An EXPIRED lease cannot be renewed in place; the holder must re-claim. Evidence that the work is alive — an open `item/<n>-*` PR — withholds the item from `take` and REFUSES a `reap`, but it does not revive the lease. A claim whose age cannot be read reports `lease unknown`, never a window.

<!-- END GENERATED: fsgg-protocol:driver-rules -->

These four are the protocol a driver *acts on*, and nothing above or below this block should restate
what they SAY — a hand-copy is what #921/#907/#1030 kept getting wrong while the engine was already
right. The procedure that USES them — the mint command above, the `widen` and `heartbeat` invocations
in §3 — stays here; the rules themselves are read off the engine.

Then read your mail — another worker may have left you a message on an item you are about to touch:

```sh
scripts/fsgg-coord inbox --repo <r>
```

## 1. Take the item

```sh
scripts/fsgg-coord take --repo <r>     # pick + claim the next SCHEDULABLE item, retrying a lost race
```

`take` asks `batch` what is startable *right now* — `Ready`, unblocked, and touch-set-disjoint from
every in-flight claim — then claims it. On a lost race it re-schedules automatically.

**CHECK THE EXIT CODE BEFORE YOU WORK — `take` exits `0` ONLY when it actually claimed you an item
([#585](https://github.com/FS-GG/.github/issues/585)).** It is the one command in your loop, and its
code tells "you hold it" from every way it can hand you nothing:

<!-- BEGIN GENERATED: fsgg-protocol:take-exit-codes -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy of this table was WRONG for as long as it existed (#889): it documented
  EX_PARTIAL — a write that half-landed — as `take` failing to READ the board, and its "≠0, ≠2"
  row swallowed every other row in the table. Edit Protocol.fs and regenerate.
-->

| exit | meaning | what to do |
|---|---|---|
| **0** | An item was CLAIMED. This is the ONLY code that means you hold one. | Go work it — and only here. |
| **5** (`EX_NONE`) | Looked, and nothing was startable — an empty or all-blocked queue. A LOOK THAT SUCCEEDED and found nothing, which is why it is not 0 and not a read failure. | Nothing to do: stop, or wait for the board to free up. Diagnose before you idle — `batch --include-backlog`, `who`, `next` each name a different reason a full board looks empty. |
| **6** (`EX_CONTENDED`) | The item was startable when it was picked and the claim CAS lost every race for it — somebody else got there first. | Back off briefly and retry. The board is busy, not empty. |
| **75** (`EX_RATE`) | A rate budget is exhausted. The message names WHICH one (#897): REST takes `claim`/`take`/`who` with it, because the lock lives there (ADR-0034 §3); GraphQL takes the board reads. When it is REST, the fleet STANDING DOWN is the designed behaviour, not an outage (#976): answering "is this item takeable?" costs the very budget that is gone, and a lock you cannot verify is not a lock. So this is a stop, and it is meant to be. | Back off until the reset it names — do not loop. Then `flush --dry-run`: a board write you made on an exhausted budget is QUEUED, and nothing replays it for you. AND IF YOU ARE HOLDING AN ITEM, `heartbeat` is REST too — an outage that outlives your lease cannot be renewed through, and the moment REST returns your item is startable again and the next `take` hands it to somebody else. Two things save you and neither is the timer: an OPEN `item/<n>-*` PR (#581 — the lease lapsed, the work did not), or a liveness probe that itself fails (which fails closed, #266). Push the branch and open the PR EARLY: it is the only proof of life that does not depend on the budget you just lost. |
| **3** | REFUSED — the batch cannot be scheduled at all. Some in-flight claim declares a touch-set that matches no file, so it reserves NOTHING, and scheduling against it would hand its files to a second worker. The message names the item and the offending tokens. | Do NOT retry — it will refuse identically until the declaration is fixed. Fix the claim it names (`widen <issue> --paths '<paths>'`), or talk to its holder. |
| **1** | No verdict was reached, for one of two reasons the message tells apart: the engine refused your INPUT before it looked (no worker id resolves; the board document does not parse), or the board READ failed. A read failure is never an empty queue and never EX_NONE (#266) — "I could not look" and "I looked, and it is empty" keep different codes on purpose. | Read the message. A refused input is not retryable — it names its own remedy. Retry only a read failure, and investigate one that persists. |
| **2** | The ENGINE broke — an unhandled defect, with a stack trace. Its own code, so a broken engine cannot hide behind a stream of what look like bad inputs. | Report it. Do not retry, and do not work an item you were not handed. |

<!-- END GENERATED: fsgg-protocol:take-exit-codes -->

So **never** write `take && work_it` — that fires on nothing (it did, live: a poller printed "CLAIMED"
over the words "nothing schedulable" and started editing with no claim and no touch-set reservation).
Gate on the code:

```sh
scripts/fsgg-coord take --repo <r> || { rc=$?; echo "no item (exit $rc)"; exit "$rc"; }
# only here do you hold a claim — read what it printed for the item id and worktree command.
```

**If `take` exits 75, a rate budget is exhausted — back off until the reset it names; do not loop.**
**Read WHICH budget it named** ([#897](https://github.com/FS-GG/.github/issues/897)): they fail
differently, and the remedy is not the same. GraphQL takes the **board reads** with it. REST takes the
**claim lock** — so `claim`/`take`/`who` stop while GraphQL-only work keeps running, and a session can
be locked out of taking an item on a board it can still read perfectly well.

You and every other worker authenticate as ONE account, so you share **both** of its budgets:
5,000 pt/hr of GraphQL, and REST's own 5,000 req/hr — which `budget` **cannot show you** (§5). This
loop drains both, and they are **not** interchangeable: GraphQL carries the **board reads** (#418:
five workers looping `take` drained it in ~15 minutes, which is why the scan cache exists), REST
carries the **claim lock** (ADR-0034 §3). Three rules follow, and they are the difference between a
fan-out that scales and one that takes the board down with it:

- **Let `take`/`next` use the shared 90s scan cache.** Never add `--fresh` in a loop; it exists for
  `take`'s own retry-after-a-lost-race, which already sets it.
- **Do NOT route reads onto REST to save GraphQL points.** This bullet used to say the opposite —
  *"read issues over REST, not GraphQL; `fsgg-coord issues` is free"* — and it was the doctrine that
  killed the lock ([#895](https://github.com/FS-GG/.github/issues/895)). `issues` is free in the
  currency that bullet counted, and **the cost lands somewhere else**: it spends a REST *request*,
  and REST is where the claim lock lives (ADR-0034 §3). Counting one budget while spending another
  is how the advice read as thrift for as long as it did.

  **And it bought nothing.** Measured in `.github` on 2026-07-17 — same 402 issues, both ways:

  | | metering | a full issue-list read (402 issues) | a full board read (383 items) | carries the lock? |
  |---|---|---|---|---|
  | **GraphQL** | **nodes** | `gh issue list --limit 402`: **7 pts** of 5,000 | `scan --fresh`: **31–41 pts**, 4 paged queries | no |
  | **REST** | **requests** | `fsgg-coord issues`: **6 reqs** of 5,000 | **no REST form exists** | **yes** (ADR-0034 §3) |

  **Seven GraphQL points, or six REST requests.** That is the whole trade the old advice was making:
  it spent the budget the lock lives on to save **7 points out of 5,000**. The thrift was not small —
  it was imaginary.

  The costs are near-identical because both reads are trivial. What is *not* symmetric is what each
  budget also has to carry, and whether you can do anything about it. REST's 5,000 carries **every
  claim, every heartbeat, every comment, for every worker on the account**, and per-request metering
  means there is **no lever to pull** under fan-out. GraphQL's 5,000 carries board reads that batch
  100 nodes to a query. Put the discretionary reads where the lever is, and leave REST for the thing
  that has no alternative.

  And it already inverted, live: [REST when the budget is gone](#rest-when-the-budget-is-gone) has
  the measured account — REST hit **0 / 5,000 twice on 2026-07-16**, taking the claim lock with it
  both times, while GraphQL stayed healthy throughout. That is the one condition four ADRs forbid —
  *a lock may never live on the budget that dies first* — and the recipe engineered it by steering
  the fleet onto the lock's own budget on a single shared account.

  **REST remains the fallback when GraphQL is genuinely exhausted**, and §5 leans on that. A fallback
  is not a default: reach for it when GraphQL is gone, not to keep it topped up.
- **A rate-limited board write is DEFERRED, not lost — but NOTHING replays it on its own.** *Every*
  board write — `set-field`, `claim`, `done --flip`, `release`, `reap` — queues itself on an
  exhausted budget and says so, and `scripts/fsgg-coord flush` replays the queue. Do not "fix" the
  board by hand; you will just duplicate the write.

  **`flush` is MANUAL.** There is no autoflush: no board write drains the queue as a side effect.
  Bash had one; the port does not. So `EX_RATE` (75) is a back-off-**and-come-back** instruction, and
  `flush` is the coming back — a deferral nobody flushes is a write that never lands. What you owe is
  on disk, so ask before you walk away:

  ```sh
  scripts/fsgg-coord flush --dry-run   # what is queued, and whose — replays NOTHING
  scripts/fsgg-coord flush             # replay it, once the budget is back
  ```

  `flush` replays by **default**; `--dry-run` is the read-only form. Both check the queue before they
  touch the board, so an empty queue and a dry run cost **zero GraphQL** — which is the point rather
  than an optimisation: an exhausted budget is the only reason a queue exists, so "what did I defer?"
  has to be answerable exactly when no board read is possible. `flush` reports what it **replayed**
  and, separately, what it **DROPPED** — an entry it can never land (an unparseable ref, or an item no
  longer on the board). A drop is a write that did *not* happen; if it names your item, re-read the
  board before you believe it.

  **Three repairs got this true, and an older kit has none of them.** `.github#510` made deferral
  universal — before it, only `claim` queued while the exhaustion message promised it to everyone, so
  a bare `set-field` printed the promise and dropped the write. `.github#878` then ported `flush`
  *itself*: the engine had named it in that promise for its whole life and never had it, so a
  deferred write was queued to `pending.jsonl` and **stranded** — real, on disk, and unreachable by
  any verb. `.github#862` is this text: the prose went on describing all three states at once.
- **A REFUSED write is not queued, and that is deliberate.** An unknown field, an unknown option, a
  `Blocked by` that is not a ref — the tool rejects these *before* spending any GraphQL, and replaying
  them could never succeed. You get the refusal and a non-zero exit, not a queue entry.

**If `take` finds nothing, that is a finding, not an empty queue.** Diagnose before you idle:

```sh
scripts/fsgg-coord batch --repo <r> --include-backlog   # `batch` is Ready-only by default
scripts/fsgg-coord who   --repo <r>                     # is everything already claimed?
scripts/fsgg-coord next  --repo <r>                     # prints WHY each candidate was skipped
```

Common causes, in order of frequency: every candidate is in `Backlog` (not `Ready`); every candidate
declares no `Paths:` and is therefore **unschedulable**; every candidate is blocked. If the board
itself looks wrong — items `Blocked` behind closed issues, claims past their lease — run
[`/check-board`](../check-board/SKILL.md) and try again.

### You hold it. Now read its COMMENTS — before the worktree, not after

`take` hands you a number, and the **body** tells you what somebody wanted three weeks ago. **The
comments are where a worker who already tried it says how it went** — and a prior worker's *"do not
do this"* is the highest-signal artifact on the board. Read them. REST, so it costs you nothing:

```sh
gh api repos/FS-GG/<repo>/issues/<n>/comments --paginate \
  --jq '.[] | "--- \(.user.login) @ \(.created_at)\n\(.body)"'
```

`--paginate` is not optional, for §4's reason exactly: a truncated read looks like an answer, and the
comment that says *"do not do this"* is the **last** one, not the first.

**Nothing else in this loop can tell you.** `take`/`batch` read the **board**, and a conclusion like
*"investigated — this must not be executed"* has no board field to live in, so it cannot reach the
scheduler. §0's `inbox` carries only messages addressed **to you**. §4's dedupe asks *"has this
finding been filed?"* — the right instinct, aimed one step late and at the wrong artifact. The
question nobody was asked is **"has this item already been worked?"**

On **2026-07-16**, #732 was claimed by **four** workers inside one hour — 16:16, 16:25, 17:08, 17:14.
Each built the engine, measured both gates, and reached the **same** conclusion (*do not delete*).
Every one of those conclusions was already sitting on the issue when `take` handed it to the next
worker: **four hour-long investigations, one answer**
([#888](https://github.com/FS-GG/.github/issues/888)).

Nobody was careless. [#867](https://github.com/FS-GG/.github/issues/867) kept re-arming the trap —
`release --status Blocked` **was** a silent no-op (the port parsed the flag and dropped it on the
floor), so every worker who correctly tried to park #732 put it back to `Ready` — and the recipe said
*read the body and start*. [#914](https://github.com/FS-GG/.github/issues/914) fixed the engine: an
explicit `--status` now beats both the recorded restore and the `Ready` fallback. **The body was the
thing that was wrong. The comments were where four people had already said so.** This is
[#464](https://github.com/FS-GG/.github/issues/464)'s shape one level up: *N workers file one finding
N times* became *N workers WORK one falsified item N times*, and it closes the same way — **look
before you start.**

**If the comments say the item is already answered, believe them.** Re-running the investigation to
be sure is the exact hour this step exists to save. Add what you know to the issue, park it
(*Abandoning an item*, below), and `take` again — then **verify the park landed.**

**`release` exits 0 even when the column did NOT land, and that is not a bug.** The lock really is
gone, so a failed column must not red the command. But it means a green exit is *not* the receipt you
want. Read **stdout**, which states only what is true (#914):

| `release` printed | the column |
|---|---|
| `released .github#732 → Blocked` | landed — `release` SET it, so the board holds it |
| `released .github#732 (column left at Blocked)` | landed — the board already held it, so `release` wrote nothing (#331) |
| `released .github#732 (no column to reset — …)` | nothing to set: the item is off the board, or has no `Status` |
| `released .github#732` (bare) | **no column was set** — stderr, immediately above, says why |

The middle row is a **success**, and it is the one to expect when you parked the item yourself during
the lease: a column you chose is preserved rather than reverted, and preserving it costs no write, so
there is no column for `release` to name as its own (#331/#911). The board holds `Blocked` either way.

The bare form is what you get when the board write was **deferred** on an exhausted budget (queued,
and nothing replays it — `fsgg-coord flush`), when the write failed, when the item is not on the
board at all, or when the item's **current column could not be read** — a column `release` cannot read
is one it will not overwrite, so it leaves it alone and says so. So a park can still silently not take,
and #732 is what that costs. Check:

```sh
# `ready` is the always-fresh TRUTH read: it reports the column the board actually holds. Do not
# check with `next` — it answers from the shared 90s scan cache, which can be older than your own
# write, and it reports only what is SCHEDULABLE. An item withheld because it overlaps somebody
# else in flight is not offered either, and that reads exactly like a park that landed.
p="$(scripts/fsgg-coord ready --repo <r> --all)" || { echo "read FAILED — no verdict"; exit 1; }
jq -r --argjson n <n> '.[] | select(.number == $n) | "status=\(.status)"' <<<"$p"
```

A comment is not a veto, though. It is **evidence** — it can be stale, or the earlier worker can be
the one who was wrong. Weigh it as you would any other finding. What you may not do is fail to
**look**.

### The item has no `Paths:` line

An item with no declared touch-set cannot be scheduled. Add one — but **claim the item first**.

The claim marker is the CAS; **the issue body is not**. Editing the body to declare `Paths:` before
you hold the lock races another worker doing the same thing, and the last write wins — silently
clobbering their declaration with yours. Take the lock, *then* declare:

```sh
scripts/fsgg-coord claim <issue>                              # 1. win the lock
scripts/fsgg-coord widen <issue> --paths "src/Scene/**, tests/Scene/**"   # 2. then declare
```

`widen` re-checks the touch-set against every live claim and notifies whoever it now collides with;
it exits non-zero on a collision. Despite the name it is a **re-declare, not an append**: it sets
`Paths:` to exactly what you pass, so it hands paths back as readily as it takes them (§3). Declare
**narrowly and honestly** — `Paths:` is not a glob language (exact paths, directory prefixes, and a
*trailing* `/**` or `/*`; a leading `**/` matches nothing and is refused).

**And do not reserve a generated artifact** (`.github#309`). If a checked-in generator produces the file
and a CI **regeneration gate** fails on any diff in it, nobody *authors* it — a collision there is a
rebase, not a decision — so declaring it reserves a file nobody owns and serialises every item that
regenerates it. Both conditions are required: if nothing in CI fails on a stale copy, keep declaring it,
because you would be trading a loud false `OVERLAP` for a silent staleness. Mind the **subtree**, too —
naming the artifact's parent directory reserves it just as effectively as naming the file. Declare
against **what the generator emits**, not what the issue's prose says it does. The full rule, with the
authorship test, is [intra-repo-parallel-work §1](../intra-repo-parallel-work/SKILL.md); expect
`verify-paths` to report the regenerated artifact as drift in §5, and say so there.

## 2. Isolate

Never work an item in the shared checkout — the other N workers are in it. **Fetch first, then branch
from `origin/main` by name.** Both halves, every time:

```sh
git fetch origin                                                # ...or the base you branch from is the PAST
git worktree add ../<repo>-<n> -b item/<n>-<slug> origin/main
cd ../<repo>-<n>
```

Construct that yourself: `claim` prints **only** the claim line. It names no branch and no worktree
command — older copies of this recipe said it did, and a worker told to paste what the tool prints
never thinks about the base ref, which is half of how this bug survives.

**Neither half is optional, and each fails silently without the other.**

- **`git worktree add` does not fetch.** It resolves `origin/main` against the *local* remote-tracking
  ref, which advances only when something in this checkout fetches. The shared checkout is long-lived
  and the other N workers are merging into `main` — that is the entire point of the fan-out — so **the
  better the protocol is working, the staler your base is when you start.** Measured: three commits
  behind after 15 minutes on a two-worker board; five during a single item on a busy one (#622).
- **Omit `origin/main` and `-b` branches from the shared checkout's `HEAD`** — routinely another
  worker's unmerged branch rather than `main`. Your PR then silently carries their commits, and
  `verify-paths` reports it only as an advisory, only for as long as that branch stays unmerged (#319).

**A stale base does not merely hide a merged fix — it manufactures fresh evidence for the bug.** You
build the engine from your stale tree, so it reproduces the bug faithfully. The item's body was written
before the fix landed, so it agrees with you. And every local gate goes green, because they check the
tree against *itself* and not one has an opinion about whether it is current. One worker re-ported
`flush` from scratch — an hour of work, deleted before merge — onto a `main` that had carried it for
two hours (#878). Another came one push from reverting a 76-line rewrite of **this very file**, merged
2h earlier, with a clean diff and no conflict (#892). Nothing inside the tree can tell you; by the time
any gate runs, the evidence is gone.

Agents: prefer the harness's built-in worktree isolation (`isolation: "worktree"`) — same
discipline, managed for you. Fetch anyway: whatever cuts the worktree can only cut it from the refs
**this checkout has already fetched**.

## 3. Work it

- **Stay inside the declared `Paths:`.** If the work grows into a file you did not declare, do not
  just edit it: `fsgg-coord widen <issue> --paths "<new set>"`. Non-zero exit means you have
  collided with a live claim — stop editing the shared paths and talk:
  `fsgg-coord say <issue> --to <worker> 'I need src/Audio for this; can you land first?'`
  **One exception, and it is the rule from §1: do NOT `widen` onto a generated artifact.** If the
  file you just touched is one a checked-in generator emits and a CI regeneration gate guards, you
  regenerated it — you did not author it. Declaring it now reserves a file nobody owns and
  serialises every other item that regenerates it, which is the exact failure §1 keeps you out of.
  Leave it undeclared: `verify-paths` asks the generators what they emit and reports it under
  `regenerated (expected):`, apart from the drift you are being asked to act on (ADR-0044, #498).
- **And the mirror of that rule: if the work turns out NARROWER than declared, re-declare it
  narrower — the moment you know.** `widen` sets the touch-set rather than extending it, so the same
  command gives paths back:

  ```sh
  scripts/fsgg-coord widen <issue> --paths "<what you are ACTUALLY touching>"
  ```

  **A narrowing can never collide** — a subset reserves nothing its superset did not — so it will
  never cost you an `OVERLAP`, and there is never a reason to sit on one. (It can still exit
  non-zero for a reason that is not yours: `widen` refuses to report `DISJOINT` when some *other*
  live claim declares unmatchable tokens, because it cannot see that claim's files to check against.
  That fires in either direction and means "fix theirs", not "your narrowing was rejected" — your
  re-declaration has already landed.) Two triggers fire in practice:

  - **A file you find you cannot edit.** A distributed or generated file whose CI gate fails on a
    consumer edit is not yours to author. You read it and moved on; give it back.
  - **A directory you turn out only to READ.** `src/` in the declaration, one file in the diff.

  Until you do, your declaration keeps reserving those files for the rest of the lease and `take`
  reports **"nothing schedulable"** over items that are startable but for a reservation nobody is
  using. Both triggers are one real item: FS.GG.Rendering#618 declared
  `Directory.Build.props src/ .github/workflows/`, merged a **one-file** diff, and never touched
  `Directory.Build.props` (a distributed file a consumer may not edit) or `src/` (the whole source
  tree). That unused `src/` alone held #619 off the board for the life of the claim — an entire
  source tree reserved, colliding on **one `.fsi` file** — and #618's holder had `widen` in hand the
  whole time with no reason to think of it
  ([#601](https://github.com/FS-GG/.github/issues/601)).
- **Heartbeat long work.** The lease is the *claim-lease* rule in the block above (§0) — a live claim
  goes stale after `FSGG_CLAIM_LEASE_MIN` without a heartbeat, and once it has EXPIRED it cannot be
  renewed in place:

  ```sh
  scripts/fsgg-coord heartbeat <issue>
  ```

  On a lapsed lease `heartbeat` refuses, names whoever holds the item now, and tells you to stop —
  believe it and re-`claim` (or walk away). Renewing a dead marker would put two workers on one item.
  An open `item/<n>-*` PR withholds your item from `take` and refuses a `reap` (§6), but it does **not**
  revive the lease; it buys you the chance to re-claim without racing, not a renewal.
- **Commit with the trailer, so attribution survives into history.** No id is written here to copy
  (#551) — expand the one §0 minted you:

  ```sh
  git commit --trailer "FSGG-Worker: $FSGG_WORKER"
  ```

  **`claim` does NOT print this line, and this step used to tell you to copy it from what `claim`
  printed** (#629). `claim` prints one line — `claimed <repo>#<n> by worker <id>` — and
  `grep -rn 'FSGG-Worker' src/` matches nothing. The **bash** client printed the trailer; ADR-0040's
  port dropped that output and this instruction outlived it. Worse, the same sentence forbade **both**
  ways to reconstruct it, so it left no legal move: copy a line that does not exist, or nothing.

  **`$FSGG_WORKER` is the honest read, and the reason it was forbidden is gone.** The old text said it
  "is empty if your id came from the **worktree name**" — there is no worktree-name derivation
  (`Identity.resolve` is `--worker` → `$FSGG_WORKER` → session id → *refuse*), so that is not a way it
  can be empty. §0 mandates the mint, and the mint sets it: for anyone who followed §0, this variable
  **is** the id holding the lock.

  If it is empty you skipped §0, and your id came from the session — the one every subagent of your
  session shares. Mint one rather than writing a trailer around it.

  **`$(git config fsgg.worker)` is still wrong**, for a reason nothing retired: it returns whoever
  claimed *most recently* (repo-shared unless `extensions.worktreeConfig` is set). A blank trailer
  loses the attribution; a borrowed one asserts a false one.

- Watch for stray build artifacts (`.pyc`, `bin/`, `obj/`) sneaking into the commit from a fresh
  worktree.

## 4. Findings you make along the way — **FIX them. File only what you cannot.**

Working an item is when you find the *other* things: a doc that lies, a gate that fails open, an
obvious improvement two files over, a contract whose version is incoherent.

**The default is to fix it, in the PR you already have open.** You are the one holding the context.
You have the files checked out, the tests running, and the problem in front of you. Nobody who reads
your issue three weeks from now will have any of that, and they will spend an hour rebuilding what you
already know.

> **This rule used to be the opposite**, and the opposite was wrong. It said *"don't fix it here — file
> it"*, and what that produced was **churn**: a worker fixes A, files B and C, and the worker who takes B
> files D and E. The board fills with findings faster than anyone can close them, the queue stops being
> a list of work and becomes a list of *observations*, and the same defect gets re-filed under a new
> number because nobody can find the first one. **A filed issue is not progress. A merged fix is.**

### The test, in order

**1. Can you fix it here, honestly?** Same repo; inside your declared `Paths:`, or a `widen` that
`widen` accepts; and the fix leaves your PR as **one story** a reviewer can hold in their head.
→ **Fix it.** Say so in the PR body, in a line that names what you found and why it belonged here.

**2. Can you fix it, but not in *this* PR?** Same repo, but it is its own change — a different subject,
a different touch-set, or a diff that would make this PR two stories.
→ **Take it next, and WRITE THE NUMBER DOWN.** Land this item, then claim that one and fix it. It is
still yours; you just do it in the right order. (If it needs an issue to be claimable, file one — but
you are filing it *to work it*, not to be rid of it.)

> **"It is still yours" is a promise the recipe could not keep, and the number is why**
> ([#1061](https://github.com/FS-GG/.github/issues/1061)). §6's last line was a bare `/pnext-item`,
> which re-scans the board and lets `take` pick — and **`take` is not bound by what you filed.** So the
> follow-up you promised to take was handed to a scheduler that never heard the promise.
>
> It was worse than a coin flip. §6's own text says it: the items most likely to overlap a just-finished
> item are **its own follow-up findings**, because §4 told you to file them *while you were standing in
> those files*. So your filing was the one `take` was **least** able to hand back, and the happy path
> guaranteed it. Meanwhile the number lived only in your head and your worktree, and §6 removes both.
>
> Hence the queue below. It is three lines of shell against the fact that a decision made with full
> context should not have to be re-derived by a scheduler that has none.

**Record it where it will outlive the worktree** — the disposition is the valuable part, and you are
about to delete the only place it exists:

```sh
# The follow-up QUEUE — your promise to yourself, one ref per line, outside the worktree §6 removes.
# The VERB owns the queue; the recipe only names it. Qualify with the TARGET repo, not `.github`.
scripts/fsgg-coord followup add FS-GG/<repo>#<new>
```

**The three things the hand-rolled `echo >>` used to get wrong are now the verb's refusals, not prose
you have to remember** ([#1063](https://github.com/FS-GG/.github/issues/1063)):

- **`add` refuses a bare `<new>`.** A queued ref must name its repo, because the queue outlives the
  checkout that wrote it — that is what it is *for* — so by the time §6 pops it you are standing
  somewhere else, and every target repo's numbering sits *entirely inside* `.github`'s, so a bare
  number would resolve onto a real, unrelated, usually-closed `.github` row (exit 0, wrong item).
- **`add` refuses an empty worker id.** The queue file is keyed on §0's *resolved* id, not on an env
  var you may have skipped — so it cannot become the shared file N workers race, where two pops read
  one head and the item is handed out twice. Skip §0 and `add` tells you to mint one, rather than
  keying every worker onto one file.
- **`add` stores the ref fully qualified**, owner and all, so the line means the same thing when a
  different checkout reads it back.

This stopped being ten lines of shell for the reason §5's merge gate did: nothing executes a recipe, so
nothing tested those ten lines, and #1061 shipped them wrong four ways before one review caught them
([#1063](https://github.com/FS-GG/.github/issues/1063)/[#724](https://github.com/FS-GG/.github/issues/724)).
The logic now lives in one tested place — `fsgg-coord followup`, with its own `.fsi` and legs — and the
recipe calls it.

**3. Can you not fix it?** Only these count:

- **Another repo owns it.** You cannot open a PR there from this worktree. This is a hard boundary, not
  a preference.
- **It needs a DECISION, not a fix** — an architectural call, a contract or ADR change, a trade-off with
  no obviously right answer, or anything that changes how somebody else's work has to be done. **This is
  the "needs a human" case, and it is the one worth filing.** A decision filed as an issue is the system
  working. A typo filed as an issue is the system clogging.
- **`widen` refuses it.** A live claim holds those files. Do not edit them — `say` to their holder
  instead, and if it still needs doing after they land, take it next.
- **You cannot verify the fix.** You would be guessing, and a guess merged is worse than a finding filed.

→ **File it — and do NOT queue it.**

**This is the case that must not auto-take, and the reason is every bullet above.** The queue is for
case 2 — *"I can fix this, just not here"*. Case 3 is the opposite finding: each of its four bullets is a
statement that **you are not the one who should work it next**, and taking it anyway does not route
around the obstacle, it walks into it.

| why you filed it | what auto-taking it would mean |
|---|---|
| **it needs a DECISION** | an agent making the architectural call the item was filed to escalate — the "needs a human" case, answered by the machine that noticed a human was needed |
| **another repo owns it** | a boundary the worktree cannot cross; you would take an item you cannot open a PR for |
| **`widen` refuses it** | a live claim holds those files. `claim` does **not** re-check disjointness (§6) — so the recursion would sail straight through the one refusal that was protecting somebody's in-flight work |
| **you cannot verify it** | a guess, merged. The bullet says it: a guess merged is worse than a finding filed |

A case-3 item is **still yours to have filed well** — the root-cause question above applies hardest here,
because a decision item is the one thing another worker genuinely cannot rebuild from a thin issue. Give
it the context, then leave it on the board for whoever should have it.

**4. Never drop it.** A finding that lives only in a PR description, a code comment, or your session
transcript is lost the moment the worktree is removed. That has not changed. The worker who *had* the
context is the only one who ever sees the problem, and they moved on.

### Before you file anything, four questions

- **Is this the thing, or the thing's SHADOW?** Ask it *first*, and ask it every time — not once the
  duplicates have piled up. A finding is where a defect *surfaced*, which is rarely where it *lives*: a
  doc that lies is often a premise that generates the lie into three docs; a gate that fails open is
  often a subject the gate cannot see. **File the cause. Fix the cause.** Filing the surface is how the
  same defect gets seven numbers, and the seventh is the first one anybody notices is a pattern.

  You are the only worker who can answer this, and you can answer it *now* — you have the files open and
  the failure in front of you. Whoever reads your issue in three weeks has neither, and will file the
  shadow again because the shadow is all your issue described.

  Two questions find the cause cheaply, and both are mechanical:
  - **"What would have had to be true for this never to happen?"** If the answer is *"someone would have
    had to remember"*, the thing to fix is whatever made remembering load-bearing.
  - **"If I fix only what I am looking at, what regenerates it?"** `grep` for the premise before you
    write the issue. If a generator, a projection, or a shared source emits your finding, **that** is the
    item — and fixing the emitted copy is a diff that reverts itself the next time anything regenerates.
- **Would fixing it take less time than writing the issue?** Then writing the issue is the *expensive*
  option, and you chose it to avoid the work. Fix it.
- **Is the issue you are about to write mostly a restatement of a rule that already exists?** Then the
  rule is not the problem — the code is. Fix the code. **And if the rule exists and keeps being broken,
  the rule is not the fix either** — a rule nothing enforces is a rule that has already failed once per
  worker. Fix what lets it be broken.
- **Has this, or its sibling, been filed before?** *Look* (below).

> **The first question used to be the LAST one, and it was conditional** — it fired only *"if a fix keeps
> regenerating the same finding"*, and told you not to *"file the symptom for the seventh time"*. Both
> halves were right and it was **addressed to the wrong worker**: the one about to file #1 has no way to
> know there will be a seventh, and the one who *can* see the pattern is the seventh — by which point the
> org has paid six times. That is [#266](https://github.com/FS-GG/.github/issues/266)'s signature in a
> recipe rather than a gate: a check whose subject is invisible to it at the moment it runs. Asking it
> first costs one question and needs no pattern to have formed yet
> ([#1061](https://github.com/FS-GG/.github/issues/1061)).

**Do not file an issue about a file that is already open in your diff.** That is the clearest case there
is: you are looking at it, you can change it, and you are choosing to write about it instead.

### Look before you file — you are not the only one filing

**Check whether it is already filed. Two REST reads — cheap, but not *free*: they spend the budget the claim lock lives on (#895). Spend them anyway; a duplicate costs the org more than two requests:**

```sh
# 1. Is there already an issue for this?  --state all IS NOT OPTIONAL: see below.
#    `issues` emits the raw JSON array and has NO --jq flag — it exits 1 on one. The --jq on the
#    `gh api` line below IS real; the two lines are not the same command (#874).
#    CAPTURE, don't pipe: `issues | jq` gives you jq's exit code, and jq is happy to read a FAILED
#    read as empty and print nothing, exit 0. "No hits" and "the read died" would be the same
#    answer — and the answer decides whether you file a duplicate. On an exhausted budget (§1 says
#    EXPECT one by now) that is not hypothetical. Believe an empty list only after a 0 here.
hits="$(scripts/fsgg-coord issues <target> --state all)" \
  || { echo "dedupe read FAILED ($?) — do NOT read this as 'nothing filed'"; exit 1; }
#    contains(), not test(): the keyword is a LITERAL, and test() would read it as a regex —
#    `test("C++")` matches every title containing "c" (oniguruma: `++` is possessive), so a
#    metachar keyword silently answers "already filed" and suppresses your finding.
jq -r --arg k "<keyword>" '.[]
  | select(.title | ascii_downcase | contains($k | ascii_downcase))
  | "#\(.number) [\(.state)] \(.title)"' <<<"$hits"

# 2. If you are filing a CHILD of an item you hold — look at what it ALREADY has. This is the
#    highest-signal place to look, and the one people skip.
#    --paginate is NOT optional: this endpoint pages at 30, and the parents worth checking are
#    exactly the big ones. Without it, #266 lists 30 of its 51 children and confidently omits
#    the rest (#547).
gh api repos/FS-GG/<repo>/issues/<parent>/sub_issues --paginate --jq '.[] | "#\(.number) \(.title)"'
```

**`--state all`, and this is the part that bites.** `issues` defaults to `state=open`, and this step
used to take that default — so it could not see a **closed** issue at all. But *"somebody already fixed
it"* is the **most likely reason a finding is a duplicate**, and it is the *only* reason available to a
worker whose recipe is stale: a recipe is copied into your context at session start and never
refreshed, so you can hit a bug the org repaired an hour ago, in the very file you are reading.

Those two compose into a trap with no exit. The mechanism that makes you rediscover a bug is the same
one that guarantees its issue is **closed** — so the dedupe step was structurally blind to exactly the
duplicates it exists to catch, and answered a confident *"no hit"*. That is [#266](https://github.com/FS-GG/.github/issues/266)'s
signature again: a check that runs, reports success, and cannot see its subject. It is how
[#719](https://github.com/FS-GG/.github/issues/719) was filed against a bug that had been fixed and
merged hours earlier.

**A closed hit is the BEST possible outcome.** It means the fix already exists: go read it, confirm
your case is covered, and if it is not, say so *on that issue* — do not open a rival.


**Every `gh api` read of a LIST needs `--paginate`.** A truncated read does not look truncated — it
looks like an answer, and here it is the answer to "has someone already filed this?", so the failure
mode is a confident *no* on a parent that already has the child you are about to duplicate. Worse, it
is a false negative on **linkage**: `done --flip` rolls up over the native sub-issue graph and
nothing else (#322), so a worker reading a truncated graph can conclude an epic's children are all
done when 19 of them are merely off-page. `fsgg-coord issues` pages for you; hand-written `gh api`
lines do not. `scripts/check-recipe-pagination.py` gates this recipe so the rule cannot rot.

This step exists because eager filing plus N workers **deterministically** produces duplicates, and
they are worst exactly where the protocol is working hardest — several workers splitting one parent
at the same time. [#459](https://github.com/FS-GG/.github/issues/459) and
[#460](https://github.com/FS-GG/.github/issues/460) were the same finding, filed **eleven minutes
apart**, by two workers who were each fixing a different half of the same issue and neither of whom
could see the other's in-flight filing. Nobody was careless; the recipe simply never said to look
([#464](https://github.com/FS-GG/.github/issues/464)).

**On a hit, do not open a rival.** The finding's value is its context, and a comment carries that
just as well as an issue does:

- **Comment on the existing issue** with what you know that it does not.
- If yours is the better-specified of the two, **transplant your detail into the existing one** and
  say so — do not leave two issues for one piece of work.
- **Prefer the child the parent's sub-issue graph already points at.** `done --flip` rolls up over
  that graph and *nothing else*, so of two duplicate children the linked one is the one that can
  actually complete its parent; an unlinked twin lets the parent stamp `Done` over open work (#322).

```sh
# 1. The message: an issue in the repo that OWNS the problem, not the one that found it.
#    The `Paths:` line is NOT optional — see below. Without it the item cannot be scheduled.
#    REST, because `gh issue create` is GraphQL and the budget is routinely gone by now (#587).
gh api -X POST repos/FS-GG/<target>/issues \
  -f title='[cross-repo] <short summary>' \
  -f 'labels[]=cross-repo' -f 'labels[]=cross-repo:request' \
  -f body="From: <this repo>, found while working <this repo>#<n>. Contract: <id>. <what and why>

Paths: src/Scene/ tests/Scene/" --jq .html_url

# 2. Put it on the board, so it is sequenced rather than merely filed.
#    QUALIFY EVERY REF HERE. A bare `<new>` resolves against the repo you are STANDING IN — which in
#    this block is never the target repo — so it would silently address `.github#<new>`: a real,
#    unrelated, usually-closed item. See "A bare ref is not a short ref" below.
scripts/fsgg-coord add FS-GG/<target>#<new>
scripts/fsgg-coord set-field FS-GG/<target>#<new> 'Repo Scope' <target-short-id>
scripts/fsgg-coord set-field FS-GG/<target>#<new> Phase '<the target repo's phase>'
scripts/fsgg-coord set-field FS-GG/<target>#<new> Status Backlog

# 3. If the finding belongs under an epic, LINK it as a sub-issue — NOW, not at close-out.
scripts/fsgg-coord child FS-GG/<repo>#<parent> FS-GG/<target>#<new>
```

**A bare ref is not a short ref — it resolves against the repo you are STANDING IN, and this block
is the one place that is always wrong.** `<n>`, `repo#n` and `owner/repo#n` are three different
refs, not three spellings of one. A bare `<n>` is resolved against `--repo` if you passed one, and
otherwise against **your checkout's own remote** — so in a cross-repo filing block, where every ref
names an issue in *another* repo, a bare `<new>` addresses `.github#<new>` instead. Measured:

```
$ scripts/fsgg-coord item-id 12                    # bare, from a .github checkout
PVTI_lADOEYAWY84Bb08WzgxC5-4                       # ...is .github#12 — a CLOSED P0 decision row
$ scripts/fsgg-coord item-id FS-GG/FS.GG.Audio#12
fsgg-coord-engine: GraphQL refused the query: Could not resolve to an Issue with the number of 12.
```

**This is not a corner case — it is every case.** Measured 2026-07-17, every target repo's numbering
sits *entirely inside* `.github`'s: the highest issue in any of them is FS.GG.Rendering#855, and
`.github` is past #1020. So the number you just filed in a target repo **always exists here too**,
and the write does not fail — it **succeeds, on the wrong item**: `Status: Backlog` stamped over a
closed row, exit 0, no diagnostic. Meanwhile the issue you actually filed is never sequenced, which
is [#442](https://github.com/FS-GG/.github/issues/442)'s invisible-work shape again.

**This used to fail LOUDLY, and the repair is what quieted it.** [#611](https://github.com/FS-GG/.github/issues/611)
named this very line and fixed the *error label*, correctly leaving a spelling that still errored.
Then [#548](https://github.com/FS-GG/.github/issues/548) taught the parser to resolve a bare `<n>`
against your checkout — right for the in-repo case it was about, and it turned this block's error
into a silent mis-write. Nothing re-read the cross-repo block in that light, and the
`documented-invocation` gate ([#919](https://github.com/FS-GG/.github/issues/919)) cannot: it
normalises `<new>` to `1` and asks whether `set-field 1 …` **parses**, which it does. That gate
checks the *shape* of an invocation — it has no referent to check, so a metavariable's repo is
outside its subject by construction.

**So the rule is not "always qualify".** It is: **qualify when the ref's repo is not your
checkout's.** The bare form below is correct precisely because it is not cross-repo —
`set-field <this-issue> …` names an item in the repo you are standing in, which is the case a bare
ref is *for*.

**A finding filed without `Paths:` is a finding nobody can pick up.** `take`/`batch` refuse an item
with no declared touch-set — correctly, since an undeclared one cannot be proven disjoint from
another worker's — so the item lands on the board *looking* like work and is invisible to every
worker who asks for work. This is not theoretical: `.github` reached **twelve** open items, all
filed by this very recipe, **none** of them schedulable, and `/pnext-item` reported a dead queue
over a full one ([#442](https://github.com/FS-GG/.github/issues/442)). You are the one holding the
context — you can name the files better than the eventual claimant can.

**But on a `[cross-repo]` item it is still a guess, and in the target repo it lands as a lock.** You
are naming files in a repo whose layout you are reading from the outside, and every worker there is
held out of those paths for the life of the claim. So declaring a wide touch-set "to be safe" is not
safe — it is a lock you took on somebody else's behalf. Declare what you can defend, and know that
**the claimant is expected to correct you**: when they find the declaration over-reserves, §3 tells
them to `widen` it narrower on the spot rather than inherit the guess
([#601](https://github.com/FS-GG/.github/issues/601)).

Declare it **narrowly and honestly**: exact paths, directory prefixes, and a *trailing* `/**` or
`/*` (a leading `**/` matches nothing and is refused; so is a backticked line, #435), and **no
generated artifact** — see §1: a file a generator emits and a CI regeneration gate guards is not
authored, and reserving it serialises every item that regenerates it.

If you truly cannot name a touch-set — a decision item, an epic, an investigation whose scope *is*
the question — say so **in the declaration itself**, not in prose:

```
Paths: none
```

`Paths: none` is a real sentinel, not a comment ([#496](https://github.com/FS-GG/.github/issues/496)).
It does not make the item schedulable — nothing does, without files — but it makes the *absence*
**deliberate and machine-readable**, and `fsgg-coord lint` now goes **red** on a `Ready`/`Backlog`
item that declares neither. This used to be a prose instruction, and **nothing read prose**: an epic
and an omission rendered identically (`no 'Paths:' declared`), so nine items of real work sat on the
board looking like work, invisible to every worker who asked for work, while `lint` reported
`0 error(s)`. Write the sentinel, or write the paths — those are the only two honest states.

`Repo Scope` decides the `Phase`, not the subject matter — a `game` item is `P6 Game` even when it
happens to do geometry. Always name the contract/registry id and cross-reference the item you were
working (`FS-GG/<repo>#<n>`), because the finding's value is mostly its context, and you are the
only one who has it.

If the finding is a child of an epic, the `child` step is not optional. `done --flip` rolls an epic
up from its **native sub-issue graph and nothing else** — a checklist line in the epic body or a
`child of #<parent>` mention is invisible to it, so an epic can stamp Done while an open child of it
is still in flight (this is exactly what #322 did). Only `scripts/fsgg-coord child` creates the edge
the roll-up reads, so run it the moment you file the child, not at close-out. It is idempotent:
re-running it during a close-out pass is free.

**If the finding blocks your item**, say so on the board rather than working around it silently:

```sh
scripts/fsgg-coord set-field <this-issue> 'Blocked by' 'FS-GG/<target>#<new>'
scripts/fsgg-coord set-field <this-issue> Status Blocked
scripts/fsgg-coord release <this-issue>     # don't hold a lease on work you cannot finish
```

**The third line no longer undoes the second, and for most of this protocol's life it did**
([#331](https://github.com/FS-GG/.github/issues/331)/[#911](https://github.com/FS-GG/.github/issues/911)).
`release` restored the column recorded in the claim marker *at claim time* and never read the item's live
one, so the `Blocked` you set on line 2 was reverted to `Ready` by line 3 — **this fence could not produce
its own documented end state**, and the board was left asserting `Ready` on a row whose `Blocked by` named
an open issue. `release` now reads the live column: `In progress` is the claim's own footprint and resets,
and **any other column was chosen deliberately and is preserved** — with no write at all, which is why a
preserving release reports `released <ref> (column left at Blocked)` rather than naming a column it set.
`reap` asks the same question, so a lease that lapses on a parked item no longer resets it either.

You may still write `release <this-issue> --status Blocked` — it is equivalent here, and it is the honest
form when you are parking an item whose column you have *not* already set.

Then `/pnext-item` again — take something startable while the other repo responds.

**If it doesn't block you, and it is genuinely not yours to fix (§4), file it and keep going.**
`Status: Backlog` is the honest resting place for a finding nobody has scheduled yet.

**But the constraint that makes this a real limit is REVIEWABILITY, not repo hygiene.** Do not let a
drive-by fix grow your diff into two stories: a PR that quietly fixes three unrelated things is
unreviewable, and its touch-set was a lie. That is the line — not "is this my item", but *"can one
reviewer hold this whole change in their head, and does the title still describe it?"* A small,
in-scope, verified fix belongs in your diff. A second subject does not, and it is not filed away
either — you take it next (§4, case 2).

Judgement, not a rule: fix the thing that would make *another worker's* next hour better; file only the
thing you cannot. A typo in a comment is noise **and it is also a one-character fix — so just fix it**. A
contract that cannot be satisfied, a doc that will mislead the next reader, a
gate that reports green on a missing subject — those are the findings this org keeps discovering
late, and they are cheap to file the moment you are standing in front of them.

## 5. PR, review, merge, stamp

```sh
# REST: every `gh pr` subcommand is GraphQL, and by now the budget is usually gone (#587, #528).
# `<n>` is the ITEM number, everywhere in this recipe. `<pr>` is the PULL number, and it is NOT the
# same number — which is why this prints it. Everything below that addresses the PR takes `<pr>`.
gh api -X POST repos/FS-GG/<repo>/pulls \
  -f title="$(git log -1 --format=%s)" \
  -f body="$(git log -1 --format=%b)" \
  -f head="item/<n>-<slug>" -f base=main --jq '"PR #\(.number)  \(.html_url)"'

scripts/fsgg-coord verify-paths --pr <pr>    # did the PR stay inside its declaration?
```

> **Put `Closes #<n>` in the commit BODY, never in the subject — and know why.**
>
> `--fill` maps the commit **subject → PR title** and the commit **body → PR body**. GitHub builds
> `closingIssuesReferences` — the field that says *"this PR closes that issue"* — from the **PR body
> only**, and **only while the PR is open**. So the near-universal convention
>
> ```
> gate: reconstruct the scaffold's scene edge (closes #165)
> ```
>
> puts the keyword in the **title**, where that field never looks. Everything still *works* — the
> squash commit closes the issue, because GitHub honours the keyword there too — so you get: PR
> merged ✓, issue closed ✓, CI green ✓, board Done ✓ … and the linkage GitHub records for the *PR* is
> **empty**. Before [#558](https://github.com/FS-GG/.github/issues/558) that meant a **permanently red
> stamp** on correct, merged, green work, unrepairable after the fact (editing the merged PR's body
> does **not** backfill the link — the window shuts at merge).
>
> `done` now also reads GitHub's own `CLOSED_EVENT` closer, so the stamp is earned either way. **The
> guidance stands anyway**: the body reference is the one that makes the link visible on the PR itself,
> and the two records agreeing is worth more than either alone.

> **And NEVER write a closing keyword next to an issue number you do not mean to close. GitHub does
> not read the word "not."**
>
> It scans the body for `close|closes|closed|fix|fixes|fixed|resolve|resolves|resolved` followed by an
> issue ref and links the two. **It does not parse the sentence.** A PR body that said, in as many
> words, `It does not close #422` **closed #422** on merge: the string contains `close #422`, and the
> negation is invisible to the parser. The board's auto-workflow then stamped the item **Done** — so
> an open, unfinished, *explicitly-not-done* item was closed and stamped with its acceptance criteria
> unmet ([#643](https://github.com/FS-GG/.github/issues/643)).
>
> **Nothing downstream catches this.** `done --flip` refuses to stamp work that is not *merged*, and
> this work **was** merged — it just did not *finish the item*. The only reason it surfaced at all is
> that the worker re-read the `release` output and disbelieved it.
>
> **And it needs no negation — only adjacency.** Narrative past tense (`On merge, GitHub closed
> #422`), an example you quoted, a `fixes #N` pasted from a log, a deferral (`a follow-up will
> resolve #N`): not one of them carries the word "not", and every one of them closes an issue. There
> is no such thing as a harmless closing keyword in a PR body. So the rule is **not** "avoid the word
> not":
>
> **Say what you close, on a line that says nothing else. Everywhere else, GitHub must not be able to
> bind a keyword to a number.**
>
> ```
> Closes #643.                  ← a declaration: the whole line, nothing else on it
> Closes #1, closes #2.         ← REPEAT the keyword. `Closes #1, #2` closes only #1; the
>                                 bare `#2` binds to nothing and is silently dropped.
> ```
>
> Everywhere else, deny GitHub the binding — **reword the verb** (`does NOT complete`, `addresses`,
> `supersedes`), **drop the verb** (`Refs #422.`), or **break the adjacency** (quote the number
> without its `#`).
>
> **CODE IS NOT A REMEDY — and this line used to say it was** ([#683](https://github.com/FS-GG/.github/issues/683)).
> It read *"write it as code (`closed #422`)"*, and that advice **closed an issue**. Two parsers read
> a PR and they disagree about code: the **markdown** parser builds the PR's `closingIssuesReferences`
> link and does skip code — but what actually **closes** the issue on a squash merge is the **commit
> message**, and a commit message is **plain text**. Backticks are ordinary characters in it.
> PR [#681](https://github.com/FS-GG/.github/pull/681) — the PR that shipped the gate against this
> very bug — dutifully wrote its examples in backticks and **re-closed #422**, which is how we found
> out. A markdown file in the repo is never parsed for keywords, so the docs may quote the bug in
> code; **a PR body may not.**
>
> The `closing-keywords` gate fails the PR on every undeclared closing reference, and it now scans the
> **raw** body — code included — because that is what the commit parser does. It was written against
> the body of the change that introduced it, a body that argued about this bug for forty lines and, in
> prose, would have re-closed 422. Then its own PR re-closed it anyway, in backticks.



> **Two independent reasons the landing steps below are `gh api`, not `gh pr …`.**
>
> 1. **The merge must be, always.** `gh pr merge` merges and *then* fails, because its local cleanup
>    cannot check out `main` from the worktree §2 puts you in — so a successful merge reports failure
>    and the branch survives ([#564](https://github.com/FS-GG/.github/issues/564)). This has nothing
>    to do with the budget; it is true on a fresh one.
> 2. **Every `gh pr …` command is GraphQL**, and so is `gh issue create`. On an exhausted budget — the
>    state §1 tells you to *expect*, and the one you are most likely to be in by the time you are
>    merging — they fail with `API rate limit already exceeded`, and you are left with finished,
>    green, reviewed work you cannot land ([#528](https://github.com/FS-GG/.github/issues/528)).
>
> Neither is a workaround for the *rules*, only for the transport: REST enforces branch protection
> exactly as GraphQL does. See [REST when the budget is gone](#rest-when-the-budget-is-gone).

`verify-paths` is **advisory** — the touch-set is a declaration, not an enforced boundary, and CI
reports drift rather than blocking it. It reports under two headings, and they ask different things
of you:

- **`undeclared (review):`** — the finding. Either you should have `widen`ed, or you edited a file
  that is not this item's work. Answer it before merge.
- **`regenerated (expected):`** — **not** a finding, and nothing to explain. A generated, CI-gated
  artifact §1 told you not to declare, which `verify-paths` subtracted by asking the generators
  themselves what they emit ([ADR-0044](../../../docs/adr/0044-generated-artifacts-are-derived-from-their-generators.md),
  [#498](https://github.com/FS-GG/.github/issues/498)).

> **This used to be one undifferentiated list, and the recipe's answer was to make the WORKER sort
> it** — *"name which one it is in the PR"*. That was the best available advice while the tool could
> not tell a regenerated artifact from a real overrun, and it aged into the thing it was warning
> about: the advisory fired on the behaviour §1 **mandates**, on every kit change, forever. A signal
> that fires on correct behaviour is one workers learn to skip past — and the one time it means a real
> overrun, nobody reads it. The sorting is the tool's job, because the generator already holds the
> fact; asking the worker to re-state it per PR was the hand-copied list ADR-0044 declined.

**The subtraction FAILS CLOSED, so a `regenerated` heading you do NOT see is not a claim that
nothing was regenerated.** An absent, failing, or silent `generated-paths` — and a
`verify-paths --pr N --repo <other>`, where the local generators say nothing about another repo's
artifacts — all subtract **nothing** and leave the file under `undeclared`. That is deliberate:
"I could not ask what is generated" and "nothing is generated" are opposite facts, and only one of
them is safe to act on (#266). So an artifact you expected to be subtracted showing up as
`undeclared` means **go look at the generator**, not "widen the touch-set".

Then review before you merge. Run `/code-review` on the diff and fix what it finds; if the change
has a runtime surface, drive it with `/verify` rather than trusting tests.
An agent reviewing its own PR is a weak check, so treat a finding you are inclined to dismiss as
the one worth a second look.

Merge once — and only once — **every required check is green**:

```sh
# ONE COMMAND. Do NOT hand-roll this gate — see the box below for why that instruction is the whole
# point. It polls until the verdict SETTLES and exits 0 ONLY on green.
scripts/fsgg-coord landable <pr> --wait || exit 1

# MERGE over REST. This is the DEFAULT here, not a rate-limit workaround (#564) — see below.
# `<pr>` is the PULL number; `<n>` is the ITEM/issue number. They are NOT the same, and this fence
# uses both — the branch is named for the item, the merge endpoint takes the pull.
gh api -X PUT repos/FS-GG/<repo>/pulls/<pr>/merge \
  -f merge_method=squash -f commit_title="<title> (#<pr>)" --jq '"merged=\(.merged)"'

gh api -X DELETE repos/FS-GG/<repo>/git/refs/heads/item/<n>-<slug>    # the branch, explicitly
```

`landable` prints one word on stdout and puts the decision in the **exit code**, so a poll loop reads
"keep waiting" from "stop" without parsing prose. Without `--wait` it answers once and returns; that is
the form `adopt`, `who` and `reap` use — and it is exactly when you must key on the code yourself:

<!-- BEGIN GENERATED: fsgg-protocol:landable-exit-codes -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy of this table documented BASH's codes (#900) — green 0, pending 3, red 1
  — where the engine returns 0/7/3/4 and keeps 3 == red across every verdict command. It was
  wrong in BOTH directions on the two codes a poll loop reads. Edit Protocol.fs and regenerate.
-->

| exit | meaning | what to do |
|---|---|---|
| **0** | GREEN — the PR is finished work: it merges cleanly, and every workflow run and check-run scored on its head SHA passed. The ONLY code that means merge it. | Merge it. This is the only code that says so. |
| **7** | PENDING — the verdict has not SETTLED: checks are still running, none have registered yet, the run set is still growing, GitHub has not finished computing the PR's mergeability (it does so in a BACKGROUND job, and `null` is the normal first answer for a PR you just opened — #950), or an assertion you added (`--require`, `--sha`) is not yet met. The ONE retryable verdict, which is why it has a code of its own rather than sharing one with a way to stop. | Keep waiting — this is the only code that says wait. Prefer `--wait`, which polls until the verdict settles rather than believing an early green. A `pending` that NEVER resolves is a finding: the job was RENAMED, its workflow's `paths:` filter no longer matches, `--sha` named the wrong commit, or GitHub never finished computing mergeability (rare, and not something waiting longer fixes — read the PR yourself). |
| **3** | RED or CONFLICTED — two words, one code, because both mean STOP and neither improves by waiting. Red: a run or check-run failed. Conflicted: the PR does not merge cleanly, so GitHub cannot build `refs/pull/N/merge` and gives it NO CI at all — which is why it is returned immediately rather than polled. | Stop. Do NOT wait — 3 is the code the recipe used to call `pending`, and a loop that waits on it never terminates. A red check is a finding; a conflicted PR needs a rebase, which is AUTHORING, not landing. |
| **4** | UNKNOWN — no verdict, and this is the FAIL-CLOSED one (#266). The read could not be made or its answer was not conclusive: a rate limit, a 404, a PR whose `mergeable` field is ABSENT entirely. Note what it is NOT. A `mergeable` GitHub has not computed YET is PENDING (7), not this — it is guaranteed to change, and calling it unknown made `--wait` settle at once and abandon a seconds-old PR (#950). And there is no EX_RATE (75) here, unlike `take`: an exhausted budget arrives as this code, because `landable` has no error channel to carry a budget on. | Do not merge, and do not treat it as a red. An unreachable answer is not a negative one. Look at why the read failed — check `budget` if you suspect a rate limit — and ask again. |
| **1** | REFUSED — the engine rejected your INPUT before it ever looked at the PR: no `--repo` (so which repo the PR is in is undefined), a ref that is not a PR number, or the wrong number of arguments. It is not a verdict about the PR, and no word is printed. | Read the message and fix the call. Not retryable — it will refuse identically. |
| **2** | The ENGINE broke — an unhandled defect, with a stack trace. Its own code, so a broken engine cannot hide behind a stream of what look like bad inputs. | Report it. Do not retry, and do not merge a PR you have no verdict on. |

<!-- END GENERATED: fsgg-protocol:landable-exit-codes -->

**`7` is the only code that means wait.** Every other non-zero means stop, which is why §5's own fence
(`landable <pr> --wait || exit 1`) is safe against this table being wrong — and why it stayed wrong so
long: the copy-pasteable command never read the numbers, and only a worker building their own loop got
hurt.

> **THIS USED TO BE FORTY LINES OF `jq` IN THIS FENCE, AND IT WAS WRONG FOUR TIMES**
> ([#724](https://github.com/FS-GG/.github/issues/724)). Each fix edited a **copy**:
>
> | | it merged a PR that | fixed in |
> |---|---|---|
> | [#547](https://github.com/FS-GG/.github/issues/547) | had a **failing check on page 2** — the read was unpaginated | the recipe |
> | [#606](https://github.com/FS-GG/.github/issues/606) | had **no checks at all** — "all passed" and "CI never started" are the same empty set | the recipe |
> | [#698](https://github.com/FS-GG/.github/issues/698) | was **green** — a `cancelled` SUPERSEDED run was read as a failure | the recipe |
> | [#710](https://github.com/FS-GG/.github/issues/710) | — the **autofix bot** read its OWN superseded runs as red and refused to merge the PR it had just pushed | the bot |
> | [#720](https://github.com/FS-GG/.github/issues/720) | — `adopt` refused to land **finished, green, force-pushed** work, the only kind it exists to land | the tool |
>
> **Nothing executes a recipe, so nothing tests one.** The result was backwards: the copy that *was*
> testable (`pr_landable`) was the one still carrying the bug, while the untested prose was right. So
> the logic now lives in **one tested place** — eleven legs in `tests/fsgg-coord/run.sh`, each a state
> a real PR reaches — and `scripts/check-recipe-landable.py` **fails any recipe that hand-rolls it
> again**. A fifth copy is not discouraged; it is unwritable.
>
> **And it is what makes a STALE recipe harmless.** This file is copied into an agent's context at
> session start and never refreshed, while N workers merge protocol fixes all day — so a worker can
> run a gate the org fixed hours ago. That is not hypothetical: [#718](https://github.com/FS-GG/.github/pull/718)
> was red-lit by a **nine-commit-stale** snapshot of this very section, and its worker then re-filed the
> already-fixed bug as [#719](https://github.com/FS-GG/.github/issues/719). A recipe that **calls** the
> tool reads the CURRENT one off disk at run time. The prose may drift; the behaviour cannot. That is
> [#609](https://github.com/FS-GG/.github/issues/609)'s *"an import cannot drift by construction"*,
> applied to the protocol itself.
>
> What `--wait` does, so you do not have to: it waits for the runs to **register** (for the first
> 20-60s after a push there are none, and zero runs scores as a #606 red — rejecting every PR for being
> new); it waits for the run set to **stop growing**, because GitHub schedules workflows over that same
> window and an early poll can see "2 runs, both green" while six more have not been *created* —
> merging a partial rollup; it drops a `cancelled` run **only** when a later run of its own concurrency
> group replaced it; it scores workflow runs **and** check-runs, because each sees a failure the other
> cannot (a `startup_failure` run makes no check-run; a job-level `continue-on-error` fails a check-run
> while its run succeeds); and it returns `conflicted` **immediately**, because a conflicted PR never
> gets CI at all and no amount of waiting fixes it.

> **ZERO check runs is a FINDING, not a pass** ([#606](https://github.com/FS-GG/.github/issues/606)).
> The loop this replaced waited `until [ -z "$(pending)" ]` — and `pending` is *also* empty when **no
> check has ever run**, so on a PR with no checks at all the wait exited **immediately**, the summary
> printed `checks=0 failed=0`, and the recipe merged an entirely untested PR. It could not tell *"every
> check passed"* from *"CI never started"*. That is epic [#266](https://github.com/FS-GG/.github/issues/266)'s
> signature exactly, and it is strictly worse than the #547 pagination hole above: pagination hides
> *some* checks; this hides *all* of them.
>
> **And the trigger is one this very recipe manufactures.** §2 branches you from `origin/main`, then
> you work the item for an hour while N other workers merge into main — so you are routinely
> **conflicted**. GitHub builds `pull_request` events against `refs/pull/N/merge` and **cannot create
> that ref when the merge conflicts**, so no workflow ever starts and the head commit has zero check
> runs *forever*. It is not a race you can outwait. `mergeable_state: "dirty"` is the tell, which is
> why step 0 looks: **rebase onto `main`, push, and the checks appear.** The REST merge would refuse a
> `dirty` PR anyway — but a PR whose checks are merely *late* is indistinguishable to the old loop, and
> **that one merges.**

> **A SUPERSEDED run is not a RED one — and the recipe's own happy path manufactures them**
> ([#698](https://github.com/FS-GG/.github/issues/698)).
>
> The org's workflows declare `cancel-in-progress: true`, and §5 tells you to do the two things that
> trip it: push the branch (`synchronize`) and keep the PR body in step (`edited`). Both fire the same
> workflows on the **same head SHA**, so the second run **cancels** the first. A `cancelled` conclusion
> is neither `success` nor `skipped` — so the gate this replaced called **correct, green work RED** on
> the happy path. It failed *closed*, so nothing unsafe merged; the damage was to the worker, because
> a gate that cries wolf on the happy path teaches exactly one lesson — *"FAILED is noise, merge
> anyway"* — and the next FAILED will be real. That is §5's own warning about `gh pr merge`, aimed
> back at §5.
>
> **The fix is keyed on the WORKFLOW, not the check name — and that distinction is the whole thing.**
> The obvious repair is *"take the latest check run per name"*, which is what branch protection does.
> **It fails open here**, because **check-run `.name` is the JOB name, and job names collide across
> workflows.** Measured on the very SHA that prompted this: **seven** check runs named `fixture`, from
> **six different workflows** (`pin-coherence`, `projection-selftest`, `permission-coherence`,
> `timeout-coherence`, …, all of which name a job `fixture`). Collapsing by name reduces those seven
> to **one** — so a genuinely failing `fixture` job, followed by *another workflow's* successful
> `fixture`, reports **`all 10 distinct checks green`** and merges. That is #606's signature again,
> and this time it hides a *red* check rather than a missing one.
>
> `cancel-in-progress` replaces a **workflow run**, so supersession is a fact about a workflow — and
> `.path` identifies a workflow uniquely, where `.name` identifies nothing. Hence the workflow-runs
> API, and hence a rule that drops **only** a cancelled run that a later run of *its own concurrency
> group* replaced. A cancelled run nobody re-ran is still a finding. A failed run is never dropped,
> whatever it is named.
>
> **Two traps in the shape of that fix, both of which fail OPEN — the direction the old bug did not.**
>
> - **`?head_sha=` is a FILTER, and an empty filter matches EVERYTHING.** The old gate named its
>   subject in the URL *path* (`commits/<ref>/check-runs`), where a bad ref 404s and yields an empty
>   array — it failed *closed* by construction. A query parameter does the opposite: if `$SHA` is
>   empty because the `.head.sha` read failed, GitHub **ignores the filter** and hands back the
>   repo's entire run history (3,709 runs, on this repo, when measured). The gate then cheerfully
>   asserts the greenness of *other commits* — and on a repo whose recent history happens to be green
>   it prints `all N workflows green` and merges a PR whose CI it never looked at. Hence
>   `: "${SHA:?…}"`, which is the whole reason that line is not decoration.
> - **Supersession must key on the CONCURRENCY GROUP, not the workflow.** `cancel-in-progress` only
>   cancels within `group: <workflow>-${{ github.ref }}` — same workflow *and same ref*. A
>   `workflow_dispatch` run on the item branch has the same head SHA, the same `.path`, and a **higher
>   `run_number`** (the counter is per-workflow, across every event), but a different `github.ref` — so
>   it supersedes nothing. Key on `.path` alone and it licenses the drop anyway. That is not academic:
>   `closing-keywords.yml` gates on `if: github.event_name == 'pull_request'`, so its dispatch run
>   **skips the gate job and still concludes `success`** — dropping the cancelled PR run in its favour
>   would count a vacuous green and merge a PR whose body was never checked. Re-triggering a cancelled
>   workflow by hand is the obvious thing to do about a cancelled run, which is what makes this
>   reachable.

> **A RE-RUN does not re-resolve `@main` — it re-runs the SAME workflow code, forever**
> ([#721](https://github.com/FS-GG/.github/issues/721)).
>
> The gate above tells you a check is RED. When that check is a **reusable workflow** (`uses:
> FS-GG/.github/.github/workflows/<w>.yml@main`) and you have just watched the upstream repair merge,
> the obvious next move is `gh run rerun --failed`. **It is a permanent no-op**, and this is the one
> warning in this family whose remedy is not "look harder" but "the button you are reaching for
> cannot work".
>
> GitHub resolves a mutable ref **once, when the run is CREATED** — and a re-run does not create a
> run. It adds an *attempt* to the existing one, keeping its id, its `created_at`, and therefore **its
> pinned SHA**. The repaired `@main` is never fetched. The log then looks like a legitimate fresh
> failure, because it is one: it is the **old code**, failing again.
>
> Measured on FS.GG.Governance#104, whose `lockfile-sync` first ran hours before the #671 repair:
>
> | run | attempt | started | resolved `lockfile-sync.yml@main` | result |
> |---|---|---|---|---|
> | `29236481212` | **2** — `gh run rerun` | 2026-07-14 09:30 | `e2867aab` = main @ **07-13 08:18** | ❌ |
> | `29322088160` | 1 — new event | 2026-07-14 09:32 | `21cac807` = main @ **07-14 09:28** | ✅ |
>
> The re-run executed a **full day** after the repair merged and still ran pre-repair code. The tell
> is visible in the API and nowhere in the UI: `created_at` is stuck at the *original* run's creation
> while `run_started_at` is a day later. **That gap IS the bug.**
>
> **So do not reason about which code ran — ASK.** The pin is a field, and the merge gate above has
> already fetched the runs that carry it (`$c`), so the red run's id is one `jq` away:
>
> ```sh
> jq -r '.[] | select(.conclusion == "failure") | .id' <<<"$c"     # the RED run(s)
>
> gh api repos/FS-GG/<repo>/actions/runs/<run-id> \
>   --jq '.referenced_workflows | map("\(.path) -> \(.sha)") | join("\n")'
> ```
>
> Compare that SHA against the repair you are expecting. If it predates the repair, **no number of
> re-runs will ever move it** — go to the remedy below. (The second call reads one run OBJECT, not a
> collection, so there is nothing to paginate; §4's rule — every `gh api` read of a LIST takes
> `--paginate` — is untouched.)
>
> **The remedy is a NEW `pull_request` event — the only thing that re-resolves the ref:**
>
> ```sh
> gh api -X PUT repos/FS-GG/<repo>/pulls/<pr>/update-branch   # ...when the branch is BEHIND base
> ```
>
> `update-branch` is the cheap one, and it is also the one that **refuses exactly when you most need
> it**. On a branch that is already current it does nothing and returns
>
> ```
> HTTP 422 — There are no new commits on the base branch.
> ```
>
> and "already current" is precisely where a stuck Renovate PR sits. That is the whole reason such a
> PR never self-heals: Renovate will not rebase a branch it considers up to date, a re-run is a no-op
> by the rule above, and so it sits there red — looking for all the world like a real dependency
> failure — until a human forces an event. When `update-branch` refuses, force one another way:
> **push a commit** (an empty one counts), or **close and reopen** the PR.
>
> **And do not generalise this into "re-runs are useless."** A re-run is the right tool for a flake or
> an outage — it re-executes the same code, which is precisely what you want when the code was never
> the problem. It is worthless for exactly one thing: picking up a change to code the run has already
> pinned. Knowing which of those you are looking at is the whole skill, and `referenced_workflows`
> is how you find out rather than guess.

**Why not `gh pr merge <pr> --squash --delete-branch`?** Because §2 mandates a worktree, and under
that layout `gh pr merge` **merges the PR and then exits 1**:

```
failed to run git: fatal: 'main' is already used by worktree at '/…/<repo>'
```

The API merge already succeeded. What failed is `gh`'s *local* post-merge cleanup — check out the
base branch, delete the local branch — and `git checkout main` cannot succeed in a worktree whose
repo has `main` checked out in the shared checkout. Which it always does: that is the checkout every
other worker is standing in. So this is not an edge case; it is **deterministic for every worker who
follows §2**, on every item. The remote branch is left undeleted (`--delete-branch` was part of the
aborted cleanup), and — the serious half — **a successful merge reports failure**. An agent loop
reads the exit code, concludes the merge failed, and then retries it, "fixes" something that is not
broken, or walks away without stamping, leaving merged work with `Status` un-flipped and its claim
still reserving the touch-set for the rest of the lease.

The REST form does no local checkout switching, so none of this arises. It is **not** a protection
bypass: REST enforces branch protection exactly as `gh pr merge` does, and a PR that needs a human
review is refused there identically. `gh pr merge` remains fine when you are merging from a plain
shared checkout — but that is not the layout this skill puts you in.

Do not "fix" this by reading past the `fatal:`. A recipe whose happy path depends on the worker
disbelieving an error is one that teaches them to skip errors, and the next one will be real.

**Hard rules on the merge.** Never `--admin`. Never bypass branch protection, and never disable a
required check to get past it. The repo's rules are authoritative over this skill: if protection
requires a human review, the merge is **refused** — `gh api` exits non-zero and prints GitHub's
reason — and you **stop there and report the PR for review** rather than looking for a way around it.
A red check is a finding, not an obstacle.

Then earn the stamp:

```sh
scripts/fsgg-coord done <issue> --flip      # green FSGG-DONE only if PR merged AND Status=Done
```

`--flip` sets `Status: Done` once it confirms the PR merged, and rolls the completion up to any
parent epic whose children are now all `Done`. A **red** stamp means a check failed — the item is
not done, whatever you believe. Do not hand-set `Status` to make the stamp green; the stamp is
earned, and faking it is how the board starts lying.

**`done --flip` is a board write, so an exhausted budget QUEUES it and exits 75 — and then it is on
YOU to replay it.** The stamp is not lost, and it has not landed either: it is a line in
`pending.jsonl`, and nothing drains that queue by itself (§1). Until you flush, your merged work sits
**unstamped** with your claim still reserving its touch-set. Check, don't trust — and the check is
free:

```sh
scripts/fsgg-coord flush --dry-run   # is your stamp still owed? Reads the queue, not the board
scripts/fsgg-coord flush             # replay it, after the budget resets
```

If `flush` reports your stamp **DROPPED** rather than replayed, it did not land and never will —
`--flip` again once you have fixed what made it undroppable.

**Do not hand-set `Status` to close the gap** — an unstamped item is a nuisance; a hand-stamped one is
a board that lies.

> **This block used to say the opposite, and the opposite was true when it was written.** It said the
> stamp was *"DROPPED — silently"* because *"only `claim` defers"*, and prescribed
> `budget | jq .pendingBoardWrites` to prove it. All three are dead, and two died honestly:
> [#510](https://github.com/FS-GG/.github/issues/510) made deferral universal, so every board write
> queues; [#878](https://github.com/FS-GG/.github/issues/878) ported the `flush` that replays it,
> which the engine had named all along and never had. The `jq` line is dead too — deleting it was
> right.
>
> **But the REASON given for killing it was a misdiagnosis — this block's own repair, wrong in the
> same shape as the thing it repaired** ([#1030](https://github.com/FS-GG/.github/issues/1030)). It
> said `pendingBoardWrites` was *"**never** a field the engine emitted"*, so the `jq` answered `null`
> and the check could not detect the drop it was for. **The engine emits it, and did when that
> sentence was written** — #878's port added it at 17:09:52Z, and #892's head commit was authored
> **42 minutes later** at 17:52:03Z on a branch that already contained it
> (`compare 8237c57...1ac545690` → `behind_by=0`; do NOT read this off the squash — its ancestry
> reflects `main` at landing time and says nothing about what the author's tree held). The base was
> fine. The command was simply never run. Measured on `main`:
>
> ```console
> $ scripts/fsgg-coord budget --json
> {"graphql":{"limit":5000,"remaining":2191},"pendingBoardWrites":0}
> ```
>
> **The old line WAS broken — just not that way.** It read `# 0 means your stamp was DROPPED, not
> queued`, which is the semantics **inverted**: `0` is an *empty queue*, the answer a healthy run
> gives, so the check called every landed stamp a drop. Deleting it was right; the reason given was
> not. And *"never"* is the word that did the damage — it forecloses **looking**, at a read that costs
> **nothing on either budget**: the depth is a local file, and the meter beside it is REST
> `/rate_limit`, which is billed to neither counter (`Reads.fs`, `Budget = Free`). That is the read you
> want precisely when a budget is gone — which is the state this whole section is about.
>
> **`null` is a third answer, and it means neither.** `Client.fs` holds `None` and `0` apart
> deliberately ([#266](https://github.com/FS-GG/.github/issues/266)): a queue that could not be READ is
> `null`, **never** `0` — *"I cannot tell you what is waiting"* is not *"nothing is waiting"*, and
> rendering the second as the first is how a worker concludes a queued stamp was dropped.
>
> So the two reads are **complementary, not one replacing a thing that never existed**: `budget --json`
> gives the queue's **depth**, free; `flush --dry-run` names **what** is queued and **whose**.

### REST when the budget is gone

The **merge** is already REST above — it has to be, under §2's worktree (#564). These are the *other*
`gh pr …` / `gh issue …` commands, which are GraphQL and die on an exhausted budget with
`API rate limit already exceeded`. Verified end-to-end at **0 remaining** GraphQL. `gh api repos/…`
spends the **REST** budget, which is a *separate* one — so GraphQL being gone does not stop it.

**That is the only thing REST being separate buys you. It is not a budget you can count on, and it is
UNMETERED** ([#907](https://github.com/FS-GG/.github/issues/907)). This line used to say REST was
"almost never exhausted" and that `fsgg-coord budget` "shows both". Both halves were false:

- **REST dies.** Twice on 2026-07-16 in `.github` — core hit **0 / 5,000** both times, with every real
  read 403'ing and the budget resetting at 17:46:14Z
  ([#894](https://github.com/FS-GG/.github/issues/894)) and 18:46:17Z (#907). It takes the **claim
  lock** with it, because the lock lives on REST (ADR-0034 §3), so `claim`/`take`/`who` stop while
  GraphQL-only work keeps running — a session can be locked out of taking an item on a board it can
  still read perfectly well. (Those are the **reset** instants, read from `X-RateLimit-Reset` on the
  403s; the exhaustion is earlier. #907 probed it dead at 18:29Z, ~17 minutes before its reset.)
- **`budget` cannot see REST.** It reports the GraphQL meter and the depth of the deferral queue, and
  nothing else — there is no REST line to read. So the worker who hits a REST limit and reaches for the
  free pre-flight read is shown a **healthy** number *for the budget that did not die*, and reads it as
  "everything is fine". That is [#266](https://github.com/FS-GG/.github/issues/266)'s signature — a
  check reporting green on a subject it cannot see — and this recipe was the thing asserting the check
  looks.

**`budget` is RIGHT to be silent, and the fix is not to teach it `/rate_limit`.** #894 ruled that out
explicitly, and two independent probes measured why: on this account `/rate_limit` **disagrees with
reality**. It reported `core: 2431/5000` (#894) and `core: 2320/5000` (#907, at 18:28Z) while real
requests 403'd with `x-ratelimit-remaining: 0`, `resource: core` — #907's pair in the *same second*,
#894's stably across repeated probes and naming a *different reset instant*. Metering REST from it
would replace silence with a confident wrong number, which is strictly worse.

**So the 403's own headers are the only honest reading of REST**, and the engine's `EX_RATE` message
is the one thing that names the budget that actually died — since
[#897](https://github.com/FS-GG/.github/issues/897) it tells three apart (`GraphQlBudget` /
`RestBudget` / `UnknownBudget`, `src/FS.GG.Coord.GitHub/Errors.fs`), reading them off the failing
response rather than guessing. **Believe that message over any pre-flight number**: it is emitted by
the call that actually died, at the moment it died, and `budget` is a *different* call reading a
*different* budget. When the two disagree, the message is the one that looked.

```sh
# CREATE the PR  (gh pr create is GraphQL)
jq -n --arg t "<title>" --rawfile b pr-body.md \
      '{title:$t, body:$b, head:"item/<n>-<slug>", base:"main"}' \
  | gh api -X POST repos/FS-GG/<repo>/pulls --input - --jq '"PR #\(.number)  \(.html_url)"'

# WATCH the checks  (gh pr checks is GraphQL)
# Nothing changes here on an exhausted budget: `landable` is REST all the way down, so it is the same
# one command as §5. This section used to carry a SECOND, hand-copied transcription of the gate — the
# structural reason it kept rotting (#724). There is now nothing to keep in step.
scripts/fsgg-coord landable <pr> --wait
```

`gh pr checks <pr> --watch` itself is fine in a worktree — it is GraphQL, but it reads the API and
never touches your local checkout, so only the budget can take it from you, not the layout.

Two more that bite in the same state:

- **`verify-paths` blames the wrong thing.** It reports *"not inside a GitHub checkout"* when the real
  cause is the rate limit, because it derives the repo via a GraphQL call and reads the empty result
  as "no checkout" ([#430](https://github.com/FS-GG/.github/issues/430)). Pass the repo explicitly:
  `scripts/fsgg-coord verify-paths --pr <pr> --repo FS-GG/<repo>`.
- **`gh issue create` is GraphQL too** — which strands you in §4, at the exact moment you are filing
  a finding after a long session, i.e. precisely when the budget is gone:

  ```sh
  jq -n --arg t "<title>" --rawfile b body.md \
        '{title:$t, body:$b, labels:["cross-repo","cross-repo:request"]}' \
    | gh api -X POST repos/FS-GG/<target>/issues --input - --jq '"#\(.number) \(.html_url)"'
  ```

  The **board** placement that follows it (`add`, `set-field`) is Projects v2 and has no REST form, so
  it cannot land on an exhausted budget. It is not lost: `set-field` QUEUES the write and exits 75, and
  `scripts/fsgg-coord flush` replays it after the reset (#510 made that true of every board write;
  #878 gave you the verb that replays them). So file the issue now, then either flush once the budget
  is back or say on the issue that the placement is still owed — the gap should be a decision somebody
  made rather than an omission nobody noticed.

## 6. Clean up, then go again

Before you remove the worktree, deal with what you found (§4) — and **"deal with" means fix, not file.**
Everything you noticed and did not act on is about to become unrecoverable: the branch is gone, the
context is gone, and the next worker rediscovers it from scratch.

But the answer to that is **not** to empty your head onto the board on the way out. A finding you dump
into an issue at the last minute, written by someone who has already stopped thinking about it, is the
lowest-value artefact this protocol produces — it costs the next worker an hour to rebuild what you knew
five minutes ago, and it costs everyone the noise. If it was worth noticing, it was worth either fixing
or leaving alone.

```sh
cd - && git worktree remove ../<repo>-<n>
git branch -D item/<n>-<slug>               # the LOCAL branch; §5's REST DELETE removed only the remote
scripts/fsgg-coord inbox --repo <r>         # anything arrive while you were heads-down?

# Your own follow-ups FIRST (§4 case 2) — the "take it next" you promised, and the ONLY thing that
# keeps it. `followup pop` returns the head ref on stdout and removes it ATOMICALLY; the queue is
# keyed on §0's worker id, so no other worker races you for it. GATE ON THE EXIT CODE, not on an
# empty string: 5 is "I looked, you owe yourself nothing" (go to the board); any OTHER non-zero is
# "I could not read the queue" — a promise may still be there, so STOP, do not read it as empty
# (#266/#585). Only 0 hands you a ref.
next="$(scripts/fsgg-coord followup pop)"; rc=$?
case "$rc" in
  0) echo "follow-up -> $next" ;;                 # then: /pnext-item $next (CLAIMS), then widen
  5) echo "queue empty -> the board" ;;           # then: /pnext-item
  *) echo "queue UNREADABLE (exit $rc) — NOT empty; fix it before you walk away"; exit "$rc" ;;
esac
```

**Then do EXACTLY ONE of these — the `case` is the whole point, and it is not decoration:**

| the queue had one (exit 0) | `/pnext-item <that ref>` — which CLAIMS it — **then** `scripts/fsgg-coord widen <that ref> --paths <your set>`, and believe a non-zero widen |
| the queue was empty (exit 5) | `/pnext-item` — back to the board |

**Claim FIRST, then `widen` — and that order is not interchangeable.** `/pnext-item <ref>` uses `claim`,
and **`widen` rewrites the touch-set of a lock you must be holding, so it refuses an item you do not
hold** ([#706](https://github.com/FS-GG/.github/issues/706)). This step told you to `widen` *before* the
claim for a day, and that cannot run: the refusal is a non-zero exit, and "believe a non-zero exit" then
reads it as a phantom `OVERLAP` and drops a follow-up nothing was colliding with
([#1094](https://github.com/FS-GG/.github/issues/1094)). §1 already prescribes the right order for the
same reason — *claim, then declare* — and this is that order.

**The collision check is still real; it just runs one step later.** `claim` does **not** check
disjointness — only `take` does — and your follow-up's paths are, by construction, the ones you *just*
released, so another worker's `take` may have been handed them the moment your claim dropped. So after
the claim, `widen` is the only collision check left. On a non-zero widen — an `OVERLAP`, or a `75` (§1
tells you to *expect* a budget by this point) — it is not yours to work right now: `release --status
Ready`, `say` the holder, and **put the promise back** —
`scripts/fsgg-coord followup add <that ref>` re-queues it at the BACK, so a blocked head does not stall
the ones behind it. A queue that drops a promise on a rate limit fails at the one job it has: requeue on
anything transient, and drop the ref only when it is genuinely spent — somebody else took it (guard 4),
or it is done.

**The local branch is not cleaned by anything else.** `--delete-branch` never deleted it either — `gh`
aborted at the `git checkout main` step *before* it got that far (#564) — so these have been quietly
accumulating for as long as the bug existed: the shared checkout of `.github` was holding **~40**
stale `item/*` branches from merged, done-stamped items when this was written. Harmless individually,
noise in every `git branch` you will ever run.

**The claim is dropped by `done --flip`, and you no longer have to think about it**
([#533](https://github.com/FS-GG/.github/issues/533)). It did not used to be. `done --flip` verified
the merge, set `Status: Done`, rolled up the epic — and **never touched the claim marker**. `release`
was the only path that dropped it, and `release` *rewrites* `Status`, so running it on an item you
have just stamped `Done` clobbers the stamp you just earned. This section removed the worktree and
never mentioned the claim; `release` appears only under *Abandoning an item*. **So on the success
path there was no action, in the tool or in this recipe, that dropped the lock.**

The marker stayed live for the rest of the lease (**120m**), and a live marker's `Paths:` keep
**reserving its touch-set**. And it bites hardest exactly where the protocol is working: the items
most likely to overlap a just-finished item are its own **follow-up findings** — the ones §4 tells
you to file *because* you were standing in those files. So the recipe reliably produced an item its
own author had locked out for two hours, and `take` reported a dead queue over it.

**The claim's lifetime is the work's lifetime.** The work is done, so the claim is done.

### The follow-up loop, and the four things that keep it honest

Popping your own filing here — rather than letting `take` re-scan — is what makes §4 case 2's *"it is
still yours"* true ([#1061](https://github.com/FS-GG/.github/issues/1061)). It is a **loop**, so it needs
the four guards that stop a loop from becoming the churn §4's own box warns about:

- **1. It is a TAIL call. Never recurse mid-item.** The pop is *here* — after the merge, after the
  done-stamp, after the claim is dropped — and never in §4 where you found the thing. A worker who
  descends into a follow-up while holding an item lands **nothing**: the outer item's claim sits live
  for the rest of its **120m** lease, reserving a touch-set nobody is editing, while you go three deep
  into work that has not been reviewed either. §4 case 2 has always said the right words — *land this
  item, **then** claim that one* — and this is the line that executes them. **One item in flight, ever.**
  Depth-first is how you turn a board full of observations into a worker who never merges, which is the
  same disease one level down.
- **2. `claim` does NOT check disjointness — and this loop aims straight at that.** `/pnext-item <n>`
  claims by number, and **only `take` checks a touch-set against live claims.** So the scheduler's
  overlap guarantee is simply absent here, and your `widen` exit code is the only collision check you
  get. That is not a general caution: it is pointed. Your follow-up's paths are, by construction, the
  paths you *just* released — the ones §6 above says are the likeliest in the repo to collide, because
  another worker's `take` may have been offered them the moment your claim dropped. **So `widen` right
  after the claim — never before it, because `widen` refuses an item you do not hold ([#706](https://github.com/FS-GG/.github/issues/706)/[#1094](https://github.com/FS-GG/.github/issues/1094))**
  — and believe a non-zero exit: it means somebody took the files while you were merging, and the
  answer is `say`, not a second claim.
- **3. It must terminate, and the done-stamp is the bound.** One landed item per hop. A hop that cannot
  land honestly does not spawn another hop — it goes back to the board. A queue that grows faster than it
  drains is not a loop, it is the churn §4 describes with extra steps: if every item you take files two
  more, you have built a machine for never finishing anything, and it will look productive the whole time.
- **4. The queue is a promise, not a claim.** A line in it reserves **nothing** — no lock, no lease, no
  touch-set. Another worker may take your follow-up before you get back to it, and **that is a success**:
  the work got done, which was the point. `claim` will exit non-zero and tell you who holds it. Drop the
  line and pop the next one; do not race them for an item you filed.

**And if the queue is empty, the bare `/pnext-item` is not a consolation prize.** The board is the
default and the loop is the exception: it exists only so that a decision you made **with** the context is
not thrown away and re-derived by a scheduler that has none. When you have no follow-up, you have no
decision to carry, and `take` is exactly right.

**And an expired lease is not proof that you stopped** ([#581](https://github.com/FS-GG/.github/issues/581)).
If a long build outruns the lease, an **open PR on `item/<n>-*` is proof of life**: `batch` will not
offer your item to anyone else, `reap` refuses to collect it, and `who` shows `STALE (#433 OPEN)`
rather than a bare `STALE`. What that proof of life buys you is that the item is **not being handed to
a stranger** while you finish — so you can re-`claim` it without racing a `take`. It does **not** revive
the lease: an EXPIRED lease cannot be renewed in place (the *claim-lease* rule in §0), so `heartbeat`
refuses it and re-`claim` is the path back. Heartbeat *before* the lease lapses so it never comes to
that; the open PR is the safety net for the one case where a build predictably outruns the window, not
a substitute for the heartbeat.

## Abandoning an item

Do not just walk away — the lease holds the item for two hours and blocks its touch-set.

```sh
scripts/fsgg-coord release <issue>          # marker deleted, unassigned, Status RESTORED
```

`release` undoes **the claim, and only the claim** — the marker is deleted, the assignee cleared, and
the board column is decided by the precedence below. It asks one question — *is the item still sitting in
the `In progress` that `claim` itself wrote?* — and everything else follows from the answer, plus the
explicit `--status` that overrides it. This table is emitted from the engine's own `unclaimColumn`, so
it cannot drift from what `release` actually does. The most-repaired behaviour in the org — seven issues
corrected a hand copy of it (#1099) — so read the **stdout — the tell** column and believe it:

<!-- BEGIN GENERATED: fsgg-protocol:release-columns -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  This precedence was hand-authored here, and it is the single most-repaired behaviour in the
  org: seven issues (#331/#354/#531/#867/#911/#914/#921) corrected a prose copy of it while the
  engine's `unclaimColumn` was, eventually, right. #889/#900 proved the cure — the two exit-code
  tables that were GENERATED have never drifted since. This is the third. Edit Protocol.fs and
  regenerate.
-->

| release sees | the column becomes | writes? | stdout — the tell |
|---|---|---|---|
| You pass an explicit `--status <col>`. It BEATS the recorded restore and the `Ready` fallback alike — the caller naming the deliberate end state (#867/#914), which is why parking an item into a column is `release <n> --status <col>`. | `<col>` — the column you named. | yes | `released <ref> → <col>` |
| No `--status`; the live column is still the `In progress` the claim wrote, and the marker recorded NO other column (or recorded `In progress` — the same footprint written twice). | `Ready` — the fallback for a claim with nothing to restore (#481). | yes | `released <ref> → Ready` |
| No `--status`; the live column is the claim's own `In progress`, and the marker recorded a DIFFERENT column at claim time — what the claim overwrote. | the recorded column, RESTORED — a `Backlog` item returns to `Backlog`, not `Ready` (#481). | yes | `released <ref> → <recorded>` |
| No `--status`; the live column is anything OTHER than the claim's `In progress` — it was chosen DURING the lease (you parked it `Blocked`, say). `reap` asks the same question, so a lapsed lease does not revert it either (#331/#911). | that column, PRESERVED — the write is skipped, and the absence of the write is what says the column was nobody's to change. | no | `released <ref> (column left at <col>)` |
| No `--status`; the item has no `Status` set, or is not on this board — so there is no column to reset. | nothing to set. | no | `released <ref> (no column to reset — not on this board, or no Status set)` |
| The live column could not be READ (unresolvable board, or a transient failure), OR a column `release` chose to write was DEFERRED on an exhausted budget or FAILED. A column it cannot read is one it will not overwrite (#266/#331). | UNCHANGED — left exactly as it is; the lock is dropped regardless. The BARE line — no `→`, no `(...)` — is the tell, and stderr immediately above it names the repair. | no | `released <ref>` |

<!-- END GENERATED: fsgg-protocol:release-columns -->

**Confirm it landed rather than assuming.** `release` exits 0 even when the column write does not take,
so the exit code cannot tell a park that landed from one that did not — the stdout line can, and it is
the last column above. A bare `released <ref>` with the reason on stderr is a park that did NOT land
(§1); read stderr, and set the column yourself if you meant to.

If you got far enough to be worth resuming, say so on the issue first (`fsgg-coord say`), and push
the branch so the next worker inherits the work rather than redoing it.

## Setup

- `claim`/`release`/`say`/`widen` need `issues: write`; board writes need
  `gh auth refresh -s project,read:project`. Merging needs `contents: write` and whatever the
  repo's branch protection demands.
- `--repo` takes a registry short-id (`sdd`, `rendering`, `governance`, `templates`, `game`,
  `audio`), `owner/repo`, or a bare repo name. Default: the repo you are standing in.
- Fan out with **one worktree per worker**, or set `FSGG_WORKER` per worker. Do not run two workers
  from one checkout with no explicit ids.
