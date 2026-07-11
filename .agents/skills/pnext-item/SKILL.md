---
name: pnext-item
description: Claim the next schedulable item assigned to THIS FS-GG repo and take it all the way to merged and done-stamped. Use when you are a worker (agent or person) picking up work in a repo, especially one of several running in parallel. Wraps the intra-repo-parallel-work protocol — worker id, comment-order claim lock, per-item git worktree, disjoint `Paths:` touch-set — then implements, opens a PR, reviews it, merges on green, and earns the done-stamp. Any problem or improvement it finds that another repo owns gets filed on the Coordination board rather than fixed in place or forgotten. Canonical protocol lives in FS-GG/.github. See ADR-0001, ADR-0021 and ADR-0027.
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

```sh
export FSGG_WORKER=finch-a3f    # per worker, before anything else
```

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

**If `take` exits 75, the GraphQL budget is exhausted — back off until the reset it names; do not
loop.** You and every other worker share ONE 5,000-pt/hr budget (one account), and this loop is what
drains it ([#418](https://github.com/FS-GG/.github/issues/418)): board reads are GraphQL-only, so N
workers polling cost N full scans a round. Three rules follow, and they are the difference between a
fan-out that scales and one that takes the board down with it:

- **Let `take`/`next` use the shared 90s scan cache.** Never add `--fresh` in a loop; it exists for
  `take`'s own retry-after-a-lost-race, which already sets it.
- **Read issues over REST, not GraphQL.** `fsgg-coord issues <repo>` is free; `gh issue list` /
  `gh issue view` cost 2 points each, and `gh issue edit` costs 4. When GraphQL is gone, REST is still
  up — `gh api repos/…` will still open your PR and post your comments.
- **A rate-limited board write is DEFERRED, not lost.** `claim` says so, queues it, and `fsgg-coord
  flush` (or the next board write) replays it. Do not "fix" the board by hand; you will just duplicate
  the write.

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
it exits non-zero on a collision. Declare **narrowly and honestly** — `Paths:` is not a glob
language (exact paths, directory prefixes, and a *trailing* `/**` or `/*`; a leading `**/` matches
nothing and is refused).

## 2. Isolate

`claim` prints the branch and the worktree command. Use them — never work an item in the shared
checkout, because the other N workers are in it.

```sh
git worktree add ../<repo>-<n> -b item/<n>-<slug>
cd ../<repo>-<n>
```

Agents: prefer the harness's built-in worktree isolation (`isolation: "worktree"`) — same
discipline, managed for you.

## 3. Work it

- **Stay inside the declared `Paths:`.** If the work grows into a file you did not declare, do not
  just edit it: `fsgg-coord widen <issue> --paths "<new set>"`. Non-zero exit means you have
  collided with a live claim — stop editing the shared paths and talk:
  `fsgg-coord say <issue> --to <worker> 'I need src/Audio for this; can you land first?'`
- **Heartbeat long work.** A claim goes stale after `FSGG_CLAIM_LEASE_MIN` (default 120m) without
  one, and the next claimant collects it.

  ```sh
  scripts/fsgg-coord heartbeat <issue>
  ```

  **An expired lease cannot be renewed.** `heartbeat` refuses, names whoever holds the item now, and
  tells you to stop working it. Believe it. Re-take with `claim`, or walk away — renewing a dead
  marker would put two workers on one item, which is the entire failure this protocol exists to
  prevent.
- **Commit with the trailer** `claim` printed, so attribution survives into history:

  ```
  FSGG-Worker: finch-a3f
  ```

- Watch for stray build artifacts (`.pyc`, `bin/`, `obj/`) sneaking into the commit from a fresh
  worktree.

## 4. Findings that aren't yours to fix — file them, don't carry them

Working an item is when you find the *other* things: an upstream API that forces a workaround, a
contract whose version is incoherent, a doc that lies, an obvious improvement two repos over. You
have three options and only one of them is right.

- **Don't fix it here.** It is outside your declared `Paths:`, and usually outside this repo
  entirely. Widening a touch-set across a repo boundary is not a thing you can do.
- **Don't drop it.** A finding that lives only in your PR description, a code comment, or your
  session transcript is lost the moment the worktree is removed. This is the failure mode: the
  worker who *had* the context is the only one who ever sees the problem, and they moved on.
- **File it**, then carry on with your item.

Anything owned by another repo — a bug, an incoherence, a missing capability, an improvement — goes
on the **Coordination board** as a real issue, per
[cross-repo-coordination](../cross-repo-coordination/SKILL.md). Issues are the mailbox; git is not
a queue, and neither is a TODO comment.

### Look before you file — you are not the only one filing

**Check whether it is already filed. Two REST reads, zero GraphQL, and they cost you nothing:**

```sh
# 1. Is there already an issue for this?
scripts/fsgg-coord issues <target> --jq '.[] | select(.title | test("<keyword>"; "i")) | "#\(.number) \(.title)"'

# 2. If you are filing a CHILD of an item you hold — look at what it ALREADY has. This is the
#    highest-signal place to look, and the one people skip.
gh api repos/FS-GG/<repo>/issues/<parent>/sub_issues --jq '.[] | "#\(.number) \(.title)"'
```

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
gh issue create --repo FS-GG/<target> \
  --title "[cross-repo] <short summary>" \
  --label cross-repo --label cross-repo:request \
  --body "From: <this repo>, found while working <this repo>#<n>. Contract: <id>. <what and why>

Paths: src/Scene/ tests/Scene/"

# 2. Put it on the board, so it is sequenced rather than merely filed.
gh project item-add <board> --owner FS-GG --url <issue-url>
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

Declare it **narrowly and honestly**: exact paths, directory prefixes, and a *trailing* `/**` or
`/*` (a leading `**/` matches nothing and is refused; so is a backticked line, #435). If you truly
cannot name a touch-set — a decision item, an epic, an investigation whose scope *is* the question —
**write that in the body** ("no touch-set: declare at claim time with `widen`"), so an undeclared
item is a decision somebody made rather than an omission nobody noticed.

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

Then `/pnext-item` again — take something startable while the other repo responds.

**If it doesn't block you, file it and keep going.** Do not let a drive-by improvement grow your
diff: an item that quietly fixes three other repos' problems is unreviewable, and its touch-set was
a lie. `Status: Backlog` is the honest resting place for a finding nobody has scheduled yet.

Judgement, not a rule: file the thing that would make *another worker's* next hour better. A typo in
a comment is noise. A contract that cannot be satisfied, a doc that will mislead the next reader, a
gate that reports green on a missing subject — those are the findings this org keeps discovering
late, and they are cheap to file the moment you are standing in front of them.

## 5. PR, review, merge, stamp

```sh
gh pr create --fill --base main
scripts/fsgg-coord verify-paths --pr <n>    # did the PR stay inside its declaration?
```

`verify-paths` is **advisory** — the touch-set is a declaration, not an enforced boundary, and CI
reports drift rather than blocking it. Drift still means one of two things, and both need an
answer before merge: you should have `widen`ed, or you edited a file that is not this item's work.

Then review before you merge. Run `/code-review` on the diff and fix what it finds; if the change
has a runtime surface, drive it with `/verify` rather than trusting tests.
An agent reviewing its own PR is a weak check, so treat a finding you are inclined to dismiss as
the one worth a second look.

Merge once — and only once — **every required check is green**:

```sh
gh pr checks <n> --watch                    # wait for CI, don't merge into a pending run
gh pr merge <n> --squash --delete-branch
```

**Hard rules on the merge.** Never `--admin`. Never bypass branch protection, and never disable a
required check to get past it. The repo's rules are authoritative over this skill: if protection
requires a human review, `gh pr merge` will refuse — **stop there and report the PR for review**
rather than looking for a way around it. A red check is a finding, not an obstacle.

Then earn the stamp:

```sh
scripts/fsgg-coord done <issue> --flip      # green FSGG-DONE only if PR merged AND Status=Done
```

`--flip` sets `Status: Done` once it confirms the PR merged, and rolls the completion up to any
parent epic whose children are now all `Done`. A **red** stamp means a check failed — the item is
not done, whatever you believe. Do not hand-set `Status` to make the stamp green; the stamp is
earned, and faking it is how the board starts lying.

## 6. Clean up, then go again

Before you remove the worktree, empty your head into the board (§4). Everything you noticed and did
not fix is about to become unrecoverable — the branch is gone, the context is gone, and the next
worker rediscovers it from scratch.

```sh
cd - && git worktree remove ../<repo>-<n>
scripts/fsgg-coord inbox --repo <r>         # anything arrive while you were heads-down?
/pnext-item                                 # next
```

## Abandoning an item

Do not just walk away — the lease holds the item for two hours and blocks its touch-set.

```sh
scripts/fsgg-coord release <issue>          # marker deleted, unassigned, Status -> Ready
```

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
