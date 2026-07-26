---
name: pnext-item
description: Use when a worker should claim the next schedulable item in one FS-GG repository and carry it through implementation, review, green merge, post-merge obligations, and a verified done stamp.
---

# pnext-item (FS-GG)

Run exactly one item from claim through verified done. The protocol is
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md); this is the worker state machine.

## 0. Establish identity

Each concurrent worker needs a freshly minted identity:

```bash
eval "$(scripts/fsgg-coord whoami --mint)"
```

Keep `FSGG_WORKER` for the entire item. Never invent or copy a worker id.

## 1. Take one item

```bash
scripts/fsgg-coord budget
scripts/fsgg-coord take --repo <repo> --json
```

Proceed only on a converged claim receipt. On no work, inspect `next`, inbox, and follow-ups; do not
substitute an unclaimed item. Load [command-contracts](references/command-contracts.md) for exit codes,
rate limits, and recovery.

## 2. Isolate and inspect

Create a fresh worktree from current `origin/main`, switch to `item/<number>-<slug>`, then read the full
issue, comments, declared `Paths:`, repository instructions, and relevant tests. Confirm the claim and
touch-set before editing. Heartbeat during long work.

## 3. Implement and verify

Change only the declared paths. If scope must grow, use `widen` before touching it; stop on overlap.
Before implementing interactive/game work, run the
[performance-first planning gate](references/performance-first.md). Then fix causes, add focused
regression coverage, and run proportionate build/test/format gates. Poll inbox at phase boundaries.

## 4. Route findings

Fix in-scope causes now. For a distinct cause, dedupe over REST, file one issue with acceptance criteria
and a narrow `Paths:` declaration, link dependencies only when authorship truly depends on landed work,
then add it to the follow-up queue. Load [findings-and-filing](references/findings-and-filing.md) for
the recipes and judgement boundaries.

## 5. Review, merge, and obligations

Push, open a PR that closes the item, obtain review, address actionable feedback, and wait on the typed
`landable` verdict for the exact head SHA. Merge only green and verify the merge on the default branch.
Then complete every package, deployment, generated-registry, and downstream obligation described in
[merge-and-release](references/merge-and-release.md).
For uncommon failure recovery, exact REST recipes, review-thread handling, and incident rationale,
load [deep detail](references/deep-detail.md).

## 6. Stamp and stop

```bash
scripts/fsgg-coord done <ref> --flip --pr <pr>
```

The exact green done stamp, closed issue, `Done` column, released claim, zero pending board writes, and
fresh board confirmation are completion. If any is missing, repair it before reporting. Clean the
worktree and branch only after verification. Report the exact stamp and stop; one invocation owns one item.
