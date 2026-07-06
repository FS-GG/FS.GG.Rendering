---
name: intra-repo-parallel-work
description: Coordinate multiple workers (agents or people) running in parallel on different items INSIDE one FS-GG repo. Use when you want to fan work out across concurrent workers without them grabbing the same item, stomping a shared working tree, or colliding on the same files. Adds three things to the existing Coordination board — a claim lock (assignee), per-item git worktrees, and a declared `Paths:` touch-set with an overlap check — reusing the cross-repo board, done-stamp, and `fsgg-coord` client. Canonical protocol lives in FS-GG/.github. See ADR-0021.
---

# Intra-repo parallel work (FS-GG)

The [cross-repo-coordination](../cross-repo-coordination/SKILL.md) skill coordinates work
*between* the four product repos. This skill is its **inner-repo sibling**: running several
workers **in parallel on different items inside one repo**. It does **not** replace that
fabric — it reuses the Coordination board, the `Blocked by` sequencing, the earned
done-stamp, and the `fsgg-coord` client verbatim. It only adds the three things parallelism
*inside* one repo needs that cross-repo coordination gets for free. Canonical protocol:
`FS-GG/.github` → `docs/coordination/parallel-work.md`; decision: `docs/adr/0021-*`.

## What this adds (and only this)

Cross-repo, three things are free that intra-repo parallelism must earn:

| Free across repos | Not free inside one repo | This skill's answer |
|---|---|---|
| A repo has **one owner** | N workers can grab the **same item** | **Claim** = assign `@me` (advisory lock) + `Status: In progress` |
| Separate repos = separate **checkouts** | Workers **stomp one working tree** | **Isolate** = one branch + one **git worktree** per item |
| Shared surface is a versioned **contract** (registry) | Shared surface is **files** | **Touch-set** = a declared `Paths:` line + an **overlap** check |

Everything else is inherited: the board is still the source of *order*, the registry of
*contracts*, ADRs of *decisions*; `fsgg-coord done <issue> --flip` still earns the done-stamp
and rolls epics up.

## When to use this skill

1. You want to **fan out** several agents/people onto different items in one repo at once.
2. You need to know whether **two items can run in parallel** (or must be sequenced).
3. You're about to start an item and must **claim it** so no one else picks it up.

If the work is cross-repo (needs a change/release from *another* FS-GG repo), use
[cross-repo-coordination](../cross-repo-coordination/SKILL.md) instead.

## 1. Declare the touch-set (on every parallelizable item)

Each item's issue body carries a **`Paths:`** line naming the file subtrees it will touch —
comma- or space-separated globs. This is the intra-repo analogue of a contract: it makes the
shared surface explicit and checkable *before* work starts.

```
Paths: src/Scene/**, tests/Scene/**
```

Declare narrowly and honestly. If you must widen mid-flight, edit the line and **re-check
overlap** before touching the new paths.

## 2. Check overlap before parallelizing

Two items may run in parallel **iff their touch-sets are disjoint**. Let the tool decide:

```sh
scripts/fsgg-coord overlap <a> <b>     # DISJOINT (exit 0) -> parallel; OVERLAP (exit 1) -> sequence
```

- **DISJOINT** → give each its own worktree and run them concurrently.
- **OVERLAP** → **sequence** them with the board's `Blocked by` field (or a sub-issue chain),
  same as any dependency. Do **not** run overlapping items in parallel — that's the merge
  conflict you're trying to avoid.
- `overlap` compares declared globs as **subtrees** (conservative, file-existence-independent);
  exit 2 means an item hasn't declared `Paths:` yet — add it.

## 3. Claim the item (the lock)

```sh
scripts/fsgg-coord next  --repo <this-repo>     # pick the next startable item
scripts/fsgg-coord claim <issue>                # assign @me + Status=In progress; prints the branch
scripts/fsgg-coord claim <issue> --force        # steal an item already assigned to someone else
```

The **assignee is the advisory lock** (first assignee wins). `claim` refuses an item already
held by someone else; on a true simultaneous claim **both racers back off and retry** — never
both proceed. It prints the isolation branch to use next.

## 4. Isolate in a git worktree, integrate by PR

One branch + one **git worktree** per claimed item, so parallel workers never share a working
tree (`claim` prints the exact command):

```sh
git worktree add ../<repo>-<n> -b item/<n>-<slug>   # isolated checkout for this item
# ...work, commit, push, open a PR into main...
git worktree remove ../<repo>-<n>                    # when merged
```

Keep `main` green. **Disjoint** items merge in any order; **overlapping** items (which you
sequenced in step 2) merge in `Blocked by` order and rebase.

## 5. Finish — the earned done-stamp (unchanged)

```sh
scripts/fsgg-coord done <issue> --flip     # green FSGG-DONE only after PR merged AND Status=Done
```

Same stamp and epic roll-up as cross-repo. If you abandon an item, return it to the pool:

```sh
scripts/fsgg-coord release <issue>                 # unassign @me + Status=Ready
scripts/fsgg-coord release <issue> --status Blocked # or park it Blocked
```

## The scheduling loop (fanning out N workers)

```sh
# scheduler: partition Ready items into disjoint-touch-set batches
scripts/fsgg-coord ready --repo <this-repo> --json        # actionable items
# for each candidate pair, `overlap` -> parallel batch (disjoint) or Blocked-by chain (overlap)

# each worker, independently:
i=$(scripts/fsgg-coord next --repo <this-repo>)           # pick
scripts/fsgg-coord claim "$i" || exit 0                   # lock (skip if lost the race)
git worktree add ../wt-$i -b item/$i                      # isolate
# ...implement, PR, merge...
scripts/fsgg-coord done "$i" --flip                       # earn the stamp
```

Agents: prefer the harness's built-in worktree isolation (`isolation: "worktree"`) — it is
the same one-worktree-per-item discipline, managed for you.

## Setup

- The board `Status` already has `In progress`; `Blocked by` already sequences — **no board
  schema change is required**. A repo that wants touch-sets filterable MAY add an optional
  `Paths` text field, but the protocol reads the `Paths:` line from the issue body.
- `claim`/`release`/`overlap` need the same auth as the rest of `fsgg-coord`
  (`gh auth refresh -s project,read:project` for the board writes).
- To activate this skill in a product repo, copy it into that repo's
  `.claude/skills/intra-repo-parallel-work/` (same as the cross-repo skill).
