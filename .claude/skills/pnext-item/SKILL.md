---
name: pnext-item
description: Claim the next schedulable item assigned to THIS FS-GG repo and take it all the way to merged and done-stamped. Use when you are a worker (agent or person) picking up work in a repo, especially one of several running in parallel. Wraps the intra-repo-parallel-work protocol — worker id, comment-order claim lock, per-item git worktree, disjoint `Paths:` touch-set — then implements, opens a PR, reviews it, merges on green, and earns the done-stamp. A problem it finds along the way is FIXED, in the same PR when that keeps the change reviewable; it is filed only when it is genuinely not fixable from here — another repo owns it, or it needs a decision a human has to make. Canonical protocol lives in FS-GG/.github. See ADR-0001, ADR-0021 and ADR-0027.
---

# pnext-item (FS-GG)

One command's worth of intent: **"give me the next thing to work on in this repo, and don't collide
with the other workers."** It is the driver for
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md), which owns the protocol — read it
if any step below surprises you. This skill is the *loop*, start to done-stamp.

Safe to run N times concurrently in one repo. That is the whole point: the claim lock is a
server-side total order over comment ids, so exactly one worker wins each item, and `take`
re-schedules a loser rather than sending it home.

```sh
/pnext-item                     # claim + work the next schedulable item in this repo
/pnext-item --repo game         # ...in another repo (registry short-id)
/pnext-item 186                 # ...that specific item (uses `claim`, not `take`)
```

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

Inventing one *feels* safe and is not. Agents asked to pick an id converge on the same corner of the
name space, and an id two workers share is an id the lock cannot separate — `release` would drop the
other's claim mid-flight, `heartbeat` would renew a marker that is not yours, and `say`/`inbox` would
cross-deliver. This board has carried **four `finch-*` workers at once**, all of them pattern-matched
off the single example id that used to sit on this line, while `whoami`'s own minted ids spread
cleanly across the word list (#419). The attractor is the *word*, not the suffix: re-rolling the hex
does not help if you still reach for the bird you just read — which is why **no literal id appears
anywhere in these docs for you to copy**, and why minting is the tool's job, not yours (#551).

Until `claim` refuses a marker whose `worker=` duplicates a live one (the tool half of #419), **the id
scheme is advisory** — a mint you skip is a lock you do not have.

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
  Leave it undeclared, and name it as expected drift in the PR (§5).
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
- **Heartbeat long work.** A claim goes stale after `FSGG_CLAIM_LEASE_MIN` (default 120m) without
  one, and the next claimant collects it.

  ```sh
  scripts/fsgg-coord heartbeat <issue>
  ```

  **An expired lease cannot be renewed.** `heartbeat` refuses, names whoever holds the item now, and
  tells you to stop working it. Believe it. Re-take with `claim`, or walk away — renewing a dead
  marker would put two workers on one item, which is the entire failure this protocol exists to
  prevent.
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
→ **Take it next.** Land this item, then claim that one and fix it. It is still yours; you just do it
in the right order. (If it needs an issue to be claimable, file one — but you are filing it *to work
it*, not to be rid of it.)

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

→ **File it.**

**4. Never drop it.** A finding that lives only in a PR description, a code comment, or your session
transcript is lost the moment the worktree is removed. That has not changed. The worker who *had* the
context is the only one who ever sees the problem, and they moved on.

### Before you file anything, three questions

- **Would fixing it take less time than writing the issue?** Then writing the issue is the *expensive*
  option, and you chose it to avoid the work. Fix it.
- **Is the issue you are about to write mostly a restatement of a rule that already exists?** Then the
  rule is not the problem — the code is. Fix the code.
- **Has this, or its sibling, been filed before?** *Look* (below). And if a fix keeps regenerating the
  same finding, **the finding is not the bug — the thing that regenerates it is.** File *that*, once,
  and fix it. Do not file the symptom for the seventh time.

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
scripts/fsgg-coord add FS-GG/<target>#<new>
scripts/fsgg-coord set-field <new> 'Repo Scope' <target-short-id>
scripts/fsgg-coord set-field <new> Phase '<the target repo's phase>'
scripts/fsgg-coord set-field <new> Status Backlog

# 3. If the finding belongs under an epic, LINK it as a sub-issue — NOW, not at close-out.
scripts/fsgg-coord child FS-GG/<repo>#<parent> FS-GG/<target>#<new>
```

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
reports drift rather than blocking it. Drift means one of three things, and each needs an answer
before merge: you should have `widen`ed; you edited a file that is not this item's work; or you
**regenerated an artifact §1 told you not to declare** — which is correct behaviour, and the one
case where the right answer is to say so and merge. `verify-paths` cannot yet tell the third from
the first ([#498](https://github.com/FS-GG/.github/issues/498)), so **name which one it is in the
PR**: an advisory that fires on correct behaviour, unexplained, is one the next worker skips past.

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
> `budget | jq .pendingBoardWrites` to prove it. Every part of that is now dead:
> [#510](https://github.com/FS-GG/.github/issues/510) made deferral universal, so every board write
> queues; [#878](https://github.com/FS-GG/.github/issues/878) ported the `flush` that replays it,
> which the engine had named all along and never had. And `pendingBoardWrites` was **never** a field
> the engine emitted — that `jq` answered `null`, not `0`, so the check prescribed for detecting a
> dropped write could not detect one, and read as "0 = dropped" on every healthy run. `flush
> --dry-run` is the read that actually looks at the queue.

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
/pnext-item                                 # next
```

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

**And an expired lease is not proof that you stopped** ([#581](https://github.com/FS-GG/.github/issues/581)).
If a long build outruns the lease, an **open PR on `item/<n>-*` is proof of life**: `batch` will not
offer your item to anyone else, `reap` refuses to collect it, `who` shows `STALE (#433 OPEN)` rather
than a bare `STALE`, and `heartbeat` will let you **renew your own lapsed lease** rather than making
you re-`claim` and race a `take` that is already offering your work to a stranger. You still should
heartbeat. You are no longer punished for the one case where you predictably won't.

## Abandoning an item

Do not just walk away — the lease holds the item for two hours and blocks its touch-set.

```sh
scripts/fsgg-coord release <issue>          # marker deleted, unassigned, Status RESTORED
```

`release` undoes **the claim, and only the claim**. It asks one question: *is the item still sitting in
the `In progress` that `claim` itself wrote?*

- **Yes** — that column is the claim's own footprint, so it goes back to **the column the claim
  overwrote**: a `Backlog` item returns to `Backlog`, not `Ready` (#481). `Ready` is only the fallback
  for a claim that recorded nothing to restore, and for a recorded `In progress`, which is that same
  footprint written twice and still nobody's choice.
- **No** — then somebody moved it **during the lease**, deliberately, and `release` **preserves it**
  (#331/#911). A `Blocked` you set because you hit a blocker is yours, not the claim's, and dropping a
  lease is not a reason to undo it. Preserving costs **no write at all**, which is why stdout reads
  `released <ref> (column left at Blocked)` rather than naming a column `release` set.

`reap` asks the same question, so a lapsed lease on an item you parked does not reset it either — a
reaper collects a *lease*, and knows nothing about whether the item became startable.

**A column the tool cannot READ is one it will not overwrite.** On a failed read `release` leaves the
column exactly as it is and says so on stderr, naming the repair, rather than guessing in either
direction — the lease is dropped first, so a board it cannot read never strands a lock.

**To land somewhere specific, say so:** `release <issue> --status Blocked`. An explicit `--status` beats
the preserve, the recorded restore, and the `Ready` fallback alike — the caller stating the end state
instead of `release` inferring it (#331/#481's precedence, restored by
[#914](https://github.com/FS-GG/.github/issues/914) after the port parsed the flag and ignored it — that
no-op is how #732 came back four times, §1). It also spends no read, having left no default to derive.

**Confirm it landed rather than assuming**: `release` exits 0 even when the column write does not
take, and the tell is on stdout — `released <ref> → Blocked` names a column it SET, `released <ref>
(column left at Blocked)` is a preserve (the board holds it either way), and a bare `released <ref>`
means no column was set, with the reason on stderr (§1).

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
