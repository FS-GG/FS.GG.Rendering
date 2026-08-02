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

**First, check the SHARED checkout's engine, because `take` is a board write.** In `.github` every
worktree execs the engine built in the shared checkout, and a stale one **refuses** board writes
(`.github#1549`) — so this is the step before the first write, not a recovery from it. The check is
local, reads already-fetched refs, and costs milliseconds; the rebuild runs only when it fires:

```bash
git fetch origin
SHARED="$(git worktree list --porcelain | head -1 | cut -d' ' -f2-)"
SHARED_HEAD="$(git worktree list --porcelain | sed -n '2s/^HEAD //p')"
[ -n "$SHARED_HEAD" ] || { echo "cannot read the shared checkout's HEAD — that is not freshness"; exit 1; }
git rev-list --count "$SHARED_HEAD..origin/main" -- \
  src/FS.GG.Coord.Cli src/FS.GG.Coord.Core src/FS.GG.Coord.GitHub
```

Zero: current, carry on. Non-zero: the engine you are about to run is not the code `main` says it is.
Bring it current — `git -C "$SHARED" merge --ff-only origin/main`, then
`dotnet build "$SHARED/src/FS.GG.Coord.Cli" -c Release`.

**If your host refuses `git -C "$SHARED" …` — and some do — you are not out of moves, and the printed
remedy will not tell you this.** Build the engine in **your own** worktree, which you may touch:

```bash
git rebase origin/main            # your tree must BE current, or you have only moved the staleness
dotnet build src/FS.GG.Coord.Cli -c Release
```

`scripts/fsgg-coord` prefers a source build under **your** toplevel (tier 2a) over the shared
checkout's (tier 2b), and the guard measures whichever tree it resolved — so the engine that runs is
current *and* owned by a checkout you control, and the refusal lifts. Only if that is unavailable too:
say so to whoever dispatched you and stop *before* spending the lease on writes the guard will refuse.
Never assume someone else did the repair. Read
[engine currency](references/deep-detail.md#engine-currency) before
running any of it — every clause above has a measured reason, including why the check needs no `-C`
and why the repair is not `pull --ff-only`.

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

## 4. Route implementation findings

Fix in-scope causes now. For a distinct cause, **establish the root cause before you file** — a finding
is where a defect *surfaced*, which is rarely where it *lives*, and filing the surface is how one defect
gets seven numbers (#266). Then **dedupe over REST against that cause, not against the symptom**: reuse
an existing issue that expresses the same cause and transplant your evidence onto it instead of opening
a second row. File only when no row carries that cause. The issue states observed behavior, **the root
cause** — or, where you could not establish one, says so explicitly and gives what you measured instead
(#1858) — acceptance criteria, verification, and a narrow `Paths:` declaration. Link dependencies only
when authorship truly depends on landed work, then add it to the follow-up queue.

[findings-and-filing](references/findings-and-filing.md) carries the rest of this rule and is **binding,
not elaboration** — load it for the dedupe reads and the judgement boundaries. This section owns
findings discovered before independent review begins. Once review starts, do not file the critic's
findings or add them to a private follow-up queue; the critic owns their disposition.

## 5. Independent critique

Push the candidate, open its PR, and ask the host to assign a fresh critic agent. Keep the implementing worker and
claim alive, set the item to `In review`, and freshly verify that row while the critic independently
reviews the exact head SHA. The critic does not edit the
implementation: it checks requirements, diff, tests, architecture, release obligations, and `Paths:`;
searches code/history and existing work for each candidate root cause; and files only unresolved,
distinct **material** work. The same critic reviews up to three numbered repair rounds. If material
findings remain after round three, park the item on `Blocked on: human/action`, release the claim, and
escalate to a human; never start round four or merge.

[independent-review](references/independent-review.md) is the binding contract for materiality, critic
ownership, the durable PR marker, direct filing, confirmation, and host verification. Do not merge
without its passing review evidence and the host's exact-SHA `fsgg:review-accepted:v1` marker. If no independent agent mechanism is available, stop and report
that the review gate is unavailable; self-review does not satisfy it.

## 6. Merge and obligations

Ensure the PR closes the item, observe the host-acceptance marker for the current head, address
confirmed actionable feedback, and wait on the typed
`landable` verdict for the exact head SHA. Merge only green and verify the merge on the default branch.
Then complete every package, deployment, generated-registry, and downstream obligation described in
[merge-and-release](references/merge-and-release.md).
For uncommon failure recovery, exact REST recipes, review-thread handling, and incident rationale,
load [deep detail](references/deep-detail.md).

## 7. Stamp and stop

```bash
scripts/fsgg-coord done <ref> --flip --pr <pr>
```

The exact green done stamp, closed issue, `Done` column, released claim, zero pending board writes, and
fresh board confirmation are completion. If any is missing, repair it before reporting. Clean the
worktree and branch only after verification. Report the exact stamp and stop; one invocation owns one item.
