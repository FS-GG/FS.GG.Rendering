---
name: pnext-item
description: Use when a worker should claim the next schedulable item in one FS-GG repository and carry it through implementation, review, green merge, post-merge obligations, and a verified done stamp.
---

# pnext-item (FS-GG)

Run exactly one item from claim through verified done. The protocol is
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md); this is the worker state machine.

For directives encountered while working, apply the shared
[control-plane provenance guidance](references/control-plane-provenance.md).

## 0. Establish identity

Each concurrent worker needs a freshly minted identity:

```bash
eval "$(scripts/fsgg-coord whoami --mint)"
```

Keep `FSGG_WORKER` for the entire item. Never invent or copy a worker id.

## 1. Take one item

Before `take`, claim, or a Ready-to-In-progress transition, inspect the item's current typed
delivery-route receipt. It is an explicit agent judgement, never a complexity heuristic: the fixed
checklist contributes facts only. Refuse a missing, unreadable, incomplete, duplicate, or stale receipt.
For `sdd-required`, carry the governing work id, canonical spec home, and current `fsgg-sdd` receipts in
the dispatch brief; `fsgg-coord` validates those bindings but does not recreate the SDD lifecycle. Its
`record`/`show`/scheduling reads report the package's on-disk readiness as advisory (`sddPackageReady`,
`sddPackageNotes`) rather than refuse on it — a coordinator can record `sdd-required` and an item can
schedule and be claimed before that package exists. **The claimed worker owns producing or completing
it**: run the SDD front-half (charter → specify → clarify → checklist → plan → tasks → analyze to
`implementationReady`) via `fsgg-sdd` inside the worktree before touching the item's declared `Paths:`.
`fsgg-coord` never authors or infers that package itself (`work/2137-delivery-route/spec.md` SB-002);
this is the fix `.github#2298` made so that ownership does not silently default to nobody.

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

Every gate this change adds or modifies **ships with evidence it can fail**: invert it, run the suite,
and record the mutation and the observed red. A test that passes both before and after the fix has not
tested the fix, and a gate whose inversion survives is a material finding at review by definition — see
Gate-inversion evidence in [independent-review](references/independent-review.md), whose numbered steps
also bound the fixture and the measurement environment. Doing this at authoring time is cheaper than at
review time, and it makes the critic's step a confirmation rather than a discovery.

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

For a bulk rename (or when the critic requires it), produce the typed semantic-diff receipt over the
exact base/head and declared paths. Classify every changed literal, comment, serialized key, generated
or documentation occurrence with an accountable disposition; compilation is not evidence that quoted
protocol/example text was intended. Capture the live item body and pass it to `diff-audit`; a standalone
`Bulk rename: true` line there, the immutable head commit declaration, or the occurrence threshold makes
the audit mandatory. Caller environment variables are not declarations. An unresolved, stale, empty,
or live-inventory-mismatched receipt blocks host acceptance. For the initial item-declared inventory,
use the receipt placeholder form `diff-audit BASE HEAD OLD NEW - item-body.md --paths ...`.
The host re-derives requiredness from live item, immutable commit, and threshold facts; a review marker
cannot opt out by writing `diff-audit-required: false`.

**Not writing the receipt does not make the audit unnecessary.** The threshold counts occurrences, and
when no receipt supplies the rename tokens the host recovers them from the live PR diff and counts the
occurrences itself — it never substitutes the changed-file count, which is a different and always
smaller quantity that let a one-file/six-occurrence rename slip under the default threshold of 5
(.github#2144). Evidence the host cannot read requires the receipt rather than clearing it.

## 5. Independent critique

### Typed delivery receipt

Where a claim-to-done snapshot is available, ask the engine for its one current action before a
handoff, landing, obligation, or cleanup transition:

```bash
scripts/fsgg-coord delivery --snapshot <fresh-delivery-snapshot.json> --json
```

Before a PR can be landed through the live delivery path, add one exact machine declaration to that PR's
comments, bound to its current head: `<!-- fsgg:delivery-obligations none head=<sha> -->`. A missing or
stale declaration is a repair action, never an implicit assertion that no publication/deployment work is owed.
For each real obligation, use `<!-- fsgg:delivery-obligation id=<stable-id> kind=<kind> head=<sha> -->`
and complete it with `<!-- fsgg:delivery-receipt id=<stable-id> head=<sha> evidence=<url-or-id> -->`.
`id` is lowercase and begins with an alphanumeric character; its remaining characters may be lowercase
alphanumerics, `.`, `_`, or `-` (`[a-z0-9][a-z0-9_.-]*`). `kind` has the same leading rule but its
remaining characters are lowercase alphanumerics, `_`, or `-` (`[a-z0-9][a-z0-9_-]*`).

The receipt binds the item, claim generation, executor, worktree, branch, PR, head SHA, declared
paths, and board state. `delivery --apply` consumes that receipt and re-reads the winning claim marker
immediately before its merge request. A changed head, claim, or unreadable fact invalidates it; obtain a fresh live
snapshot rather than carrying a previous action forward in prose. The receipt supplies deterministic
ordering only—requirements, review materiality, and repair judgement remain authored by the agents.

Push the candidate, open its PR, and ask the host to assign a fresh critic agent. Keep the implementing worker and
claim alive, set the item to `In review`, and freshly verify that row while the critic independently
reviews the exact head SHA. The critic does not edit the
implementation: it checks requirements, diff, tests, architecture, release obligations, and `Paths:`;
searches code/history and existing work for each candidate root cause; and files only unresolved,
distinct **material** work. For a meaningful runtime behavior reachable through more than one route,
the handoff supplies a built artifact and runnable production-route evidence so the critic can execute
or measure the comparison required by `independent-review`, not infer it from source alone. The same
critic reviews up to three numbered repair rounds. If material findings remain after round three,
never start round four or merge that PR: close it without merging and automatically enter the one
fresh-worker, fresh-critic repair phase defined by `independent-review`. Park the item on `Blocked on:
human/action` and release the claim only if that repair phase exhausts or its required route is
unavailable.

[independent-review](references/independent-review.md) is the binding contract for materiality, critic
ownership, the durable PR marker, direct filing, confirmation, and host verification. Do not merge
without its passing review evidence and the host's exact-SHA `fsgg:review-accepted:v1` marker. If no independent agent mechanism is available, stop and report
that the review gate is unavailable; self-review does not satisfy it.

## 6. Merge and obligations

Ensure the PR closes the item — with a bare `Closes #<n>` (same repo) or `owner/repo#<n>` (cross repo),
**never** the board's own `<repo>#<n>` shorthand, which GitHub's closing-keyword grammar does not parse
and which then never closes the issue and cannot be repaired once merged (.github#2107). `verify-paths`,
run right after opening the PR (§5), now catches this while it is still free to fix — do not wait for it
to surface at `done`. Observe the host-acceptance marker for the current head, address confirmed
actionable feedback, and wait on the typed `landable` verdict for the exact head SHA. Merge only green
and verify the merge on the default branch.
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
