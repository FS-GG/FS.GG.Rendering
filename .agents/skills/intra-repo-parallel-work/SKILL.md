---
name: intra-repo-parallel-work
description: Coordinate multiple workers (agents or people) running in parallel on different items INSIDE one FS-GG repo. Use when you want to fan work out across concurrent workers without them grabbing the same item, stomping a shared working tree, or colliding on the same files. Gives each worker an id, locks items with a comment-order CAS (the assignee CANNOT lock, because N agents share one GitHub account), isolates work in per-item git worktrees, schedules disjoint `Paths:` touch-sets, and gives workers a channel to talk when their work overlaps. Canonical protocol lives in FS-GG/.github. See ADR-0021 and ADR-0027.
---

# Intra-repo parallel work (FS-GG)

The [cross-repo-coordination](../cross-repo-coordination/SKILL.md) skill coordinates work
*between* the four product repos. This skill is its **inner-repo sibling**: running several
workers **in parallel on different items inside one repo**. It does **not** replace that
fabric — it reuses the Coordination board, the `Blocked by` sequencing, the earned
done-stamp, and the `fsgg-coord` client verbatim. Canonical protocol:
`FS-GG/.github` → `docs/coordination/parallel-work.md`; decisions: `docs/adr/0021-*`
(worktrees, touch-sets) and `docs/adr/0027-*` (worker identity, the lock, the channel).

## Read this first: the assignee cannot lock anything

N agents in a fan-out authenticate as the **same GitHub account**. `@me` is therefore one
principal for all of them, and anything keyed on the account cannot tell two workers apart.
ADR-0021 keyed its claim lock on the issue assignee; under exactly the conditions the lock
existed for, a second worker's claim on a held item **succeeded**, and both worked it.

So: **every worker has an id**, and the lock, the channel, and attribution all key on it.

```sh
scripts/fsgg-coord whoami          # your id, and which rule produced it
```

Resolution order: `--worker <id>` → `$FSGG_WORKER` → **the git worktree's name** (the normal
case: one worktree per item, so the worktree *is* an identity) → the agent harness's
**session id** → a generated name persisted per checkout.

The last two rules **warn**, because each can hand one id to several workers. A shared checkout
gives them one id; and a session id is unique per *session*, not per *worker* — on Claude Code
every subagent of a session shares one `CLAUDE_CODE_SESSION_ID`, so a fan-out collapses onto a
single id, which is the same-account bug one level down. (On OpenCode subagents are child
sessions with their own ids, so there it is per-worker and does not warn.) **Fan out with
worktrees, or set `FSGG_WORKER` per worker.** See
`docs/coordination/agent-session-identifiers.md` in FS-GG/.github for the harness survey.

Whatever named you, the claim marker records `harness=<name> session=<id>` as provenance, so
"which agent transcript took this lock?" is a lookup rather than mtime forensics.

## What this adds (and only this)

| Free across repos | Not free inside one repo | This skill's answer |
|---|---|---|
| A repo has **one owner** | N workers can grab the **same item** | **Claim** = an `fsgg:claim` marker comment, won by **lowest live comment id** (a server-side total order → exactly one winner) |
| Separate repos = separate **checkouts** | Workers **stomp one working tree** | **Isolate** = one branch + one **git worktree** per item |
| Shared surface is a versioned **contract** (registry) | Shared surface is **files** | **Touch-set** = a declared `Paths:` line, scheduled by `batch` |
| A repo owner is a **person you can ask** | Workers cannot see or talk to each other | **`who`** (what is running) + **`say`/`inbox`** (a channel) |

Everything else is inherited: the board is still the source of *order*, the registry of
*contracts*, ADRs of *decisions*; `fsgg-coord done <issue> --flip` still earns the done-stamp
and rolls epics up.

## When to use this skill

1. You want to **fan out** several agents/people onto different items in one repo at once.
2. You need to know whether **two items can run in parallel** (or must be sequenced).
3. You're about to start an item and must **claim it** so no one else picks it up.
4. Your work has started to **overlap someone else's** and you need to coordinate.

If the work is cross-repo (needs a change/release from *another* FS-GG repo), use
[cross-repo-coordination](../cross-repo-coordination/SKILL.md) instead.

## The loop

```sh
export FSGG_WORKER=finch-a3f                   # or let the worktree name you
scripts/fsgg-coord take --repo <this-repo>     # pick + claim the next SCHEDULABLE item, retrying a lost race
git worktree add ../<repo>-<n> -b item/<n>-<slug>   # `take` prints this
# ...implement, commit with the printed FSGG-Worker trailer, PR into main...
scripts/fsgg-coord done <issue> --flip         # earn the stamp
```

`take` is the entry point. It asks `batch` what is schedulable *right now* (disjoint from
everything in flight), claims it, and — on a lost race — **re-schedules** rather than going
home. Use `claim <issue>` only when you must have a *specific* item.

## 1. Declare the touch-set (on every parallelizable item)

Each item's issue body carries a **`Paths:`** line naming the file subtrees it will touch —
comma- or space-separated globs. This is the intra-repo analogue of a contract: it makes the
shared surface explicit and checkable *before* work starts.

```
Paths: src/Scene/**, tests/Scene/**
```

Declare narrowly and honestly. An item with no `Paths:` is **unschedulable** — `batch` will
report it and refuse to hand it out.

## 2. Let the scheduler decide what can run at once

Do **not** run `overlap` over every candidate pair by hand. Ask:

```sh
scripts/fsgg-coord batch --repo <r> -n 4    # a maximal disjoint set, at most 4
scripts/fsgg-coord overlap <a> --active     # one candidate vs every live claim
scripts/fsgg-coord overlap <a> <b>          # the pairwise check (0=disjoint, 1=overlap, 2=undeclared)
```

`batch` picks items disjoint **from each other and from every claimed item**, and it reads the
**claim marker, not the board column** (the `Status` flip is best-effort and can lag). A claimed
item's touch-set is **reserved**, not merely skipped.

- **DISJOINT** → own worktree each, run concurrently.
- **OVERLAP** → **sequence** with `Blocked by`, or **talk** (§4) and split the touch-set.
  `Blocked by` takes **issue refs only** — `fsgg-coord set-field <later> 'Blocked by' '<earlier>'`.
  Put "overlaps X on Program.fs, sequence" in an issue comment; the field is what
  `fsgg-coord next` reads to refuse to hand out the later item.

## 3. Claim, isolate, heartbeat

```sh
scripts/fsgg-coord claim <issue>            # marker + assignee + Status=In progress; prints the worktree
scripts/fsgg-coord claim <issue> --force    # steal an item another worker holds
scripts/fsgg-coord heartbeat <issue>        # renew the lease on a long-running claim
scripts/fsgg-coord release <issue>          # drop it back into the pool
```

The **marker is the lock**. The assignee is set only so a human sees it on the board.
On a simultaneous claim there is exactly **one winner** (lowest live marker id); a loser deletes
its own marker and tells you to pick another item.

**Leases.** Past `FSGG_CLAIM_LEASE_MIN` (default 120m) without a heartbeat, a claim is *stale*: the
next claimant collects it (and tells you), and `reap` releases it.

**An expired lease cannot be renewed.** `heartbeat` renews only the current holder. If yours lapsed it
**refuses and tells you to stop working the item**, naming whoever holds it now. Re-take it with
`claim`, or walk away — renewing a dead marker would put two workers on one item.

```sh
scripts/fsgg-coord reap --repo <r>            # dry run: whose claims outlived their lease?
scripts/fsgg-coord reap --repo <r> --apply    # release them, and tell the reaped worker
```

Work on `item/<n>-<slug>` in its **own git worktree**. Agents: prefer the harness's built-in
worktree isolation (`isolation: "worktree"`) — the same discipline, managed for you. Commit with the
trailer `claim` prints (`FSGG-Worker: <id>`) so attribution survives into history.

Keep `main` green. **Disjoint** items merge in any order; **overlapping** items (which you
sequenced) merge in `Blocked by` order and rebase.

## 4. See what is running; talk when work touches

```sh
scripts/fsgg-coord who --repo <r>           # worker, item, age, lease, paths, title
scripts/fsgg-coord who --repo <r> --local   # ...joined to the local git worktrees
```

Never go spelunking through worktrees to work out what is running. `who` flags:

- **`STALE`** — the holder stopped heartbeating; probably dead. `reap` it.
- **`UNCLAIMED`** — `In progress` with **no marker**: someone is working outside the protocol and
  nothing records who. Detection, not prevention — but loud instead of invisible.

When your work touches someone else's:

```sh
scripts/fsgg-coord say <issue> --to <worker> 'I own src/Audio until this lands.'
scripts/fsgg-coord inbox --repo <r>          # what is new for me, across every live claim
```

Widening a touch-set mid-flight re-checks it **and notifies whoever it now collides with**:

```sh
scripts/fsgg-coord widen <issue> --paths "src/Scene/**, src/Audio/**"   # non-zero on a collision
```

Stop editing the shared paths until the collision is resolved.

## 5. Finish — the earned done-stamp (unchanged)

```sh
scripts/fsgg-coord done <issue> --flip     # green FSGG-DONE only after PR merged AND Status=Done
```

Same stamp and epic roll-up as cross-repo. Check your PR stayed inside its declaration:

```sh
scripts/fsgg-coord verify-paths --pr <n>   # files changed outside the issue's `Paths:`
```

The touch-set is a **declaration, not an enforced boundary** — CI reports drift, it does not block.

## Setup

- The board `Status` already has `In progress`; `Blocked by` already sequences — **no board
  schema change is required**. A repo that wants touch-sets filterable MAY add an optional
  `Paths` text field, but the protocol reads the `Paths:` line from the issue body.
- `claim`/`release`/`say` need `issues: write`; board writes need
  `gh auth refresh -s project,read:project`, as the rest of `fsgg-coord` does.
- To activate this skill in a product repo, copy it into that repo's
  `.claude/skills/intra-repo-parallel-work/` (same as the cross-repo skill), and
  `.github/workflows/touch-set-drift.yml` for the advisory drift check.
