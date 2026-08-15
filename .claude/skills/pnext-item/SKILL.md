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

**Tier-2a is also a shadowing hazard, not only an escape hatch.** Once your OWN worktree has a source
build, `scripts/fsgg-coord` keeps preferring it over the shared checkout's for the rest of this item —
so a *later* staleness refusal from it names YOUR worktree's toplevel, never the shared checkout's, even
after you have independently confirmed the shared checkout is current (`.github#2471`: a worker verified
the shared checkout `Already up to date` and still hit exit 69, because an earlier `dotnet build` in
their OWN worktree, from evidence-gathering before this item's build, had gone stale in place). The
refusal's own message now says so explicitly and names both checkouts. When it fires: do NOT repair the
shared checkout — it did not cause this — and do NOT merge `origin/main` into a feature branch under
review to clear it, which moves a head an independent critic may already have confirmed. Rebuild (or
delete the stale `bin`/`obj` under, then rebuild) the checkout the refusal names, exactly as printed.

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

If the head changes, **edit that declaration in place** to bind it to the new head, or delete it before
posting a replacement. Declarations are not append-only: an old declaration remains parsed, and a second
one with the same `id` also collides. Adding a new declaration cannot supersede the old one.

The receipt binds the item, claim generation, executor, worktree, branch, PR, head SHA, declared
paths, and board state. `delivery --apply` consumes that receipt and re-reads the winning claim marker
immediately before its merge request. A changed head, claim, or unreadable fact invalidates it; obtain a fresh live
snapshot rather than carrying a previous action forward in prose. The receipt supplies deterministic
ordering only—requirements, review materiality, and repair judgement remain authored by the agents.

### Typed review/repair protocol

Where the alternating critic/implementer transitions inside review need one typed answer instead of a
manual re-read of PR comments and round counts, ask the engine for the current review-protocol state:

```bash
scripts/fsgg-coord review --snapshot <fresh-review-snapshot.json> --json
```

It returns exactly one closed state (awaiting initial review, changes requiring repair, awaiting
implementer repair, awaiting the same critic's confirmation, passed awaiting checks, awaiting host
acceptance, ordinary exhaustion, repair-phase setup, repair-phase active review, accepted, or terminal
human park) and the one typed next action that follows from it — dispatch critic, resume implementer,
resume the same critic, await checks, request host acceptance, enter the one permitted fresh repair
phase, accept, or park for human action — bound to a freshness token that a changed head invalidates.
This is a mechanical cross-check, not a substitute for the qualitative judgement below: materiality,
same-critic continuity, and repair-phase provenance are still read from the live PR by both the worker
and the critic.

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
ownership, the durable PR record, direct filing, confirmation, and host verification. Do not merge
without its passing review evidence and exact-SHA structured v2 acceptance record, authored through
`scripts/fsgg-coord review record <ref> <draft.json> --pr <n> --json`. If no independent agent mechanism is available, stop and report
that the review gate is unavailable; self-review does not satisfy it.

## 6. Merge and obligations

Ensure the PR closes the item — with a bare `Closes #<n>` (same repo) or `owner/repo#<n>` (cross repo),
**never** the board's own `<repo>#<n>` shorthand, which GitHub's closing-keyword grammar does not parse
and which then never closes the issue and cannot be repaired once merged (.github#2107). `verify-paths`,
run right after opening the PR (§5), now catches this while it is still free to fix — do not wait for it
to surface at `done`. Observe the host-acceptance marker for the current head and address confirmed
actionable feedback.

**Immediately after the host-acceptance marker is observed and every repair is complete — and BEFORE
checking `landable`, not after — make one LIVE call:**

```bash
scripts/fsgg-coord delivery <ref> --pr <pr> --json
```

No `--snapshot` — §5's own read uses a different, IO-free path that never reaches this write. This is what makes
`Client.ensureAuthorization` PATCH the PR's `fsgg:pr-authorization` marker onto the head about to be
merged (`.github#2488`); nothing else in this documented flow reaches that write, and a worker who skips
this step reproduces the five-for-five pattern `.github#2488` measured — a merged `item/<n>-*` PR with no
marker at all (`.github#2496`).

This step moved ahead of the `landable` check (`.github#2504`) because `.github#1858` made
`claim-generation` a required status context on `main`: GitHub now reports the PR `BLOCKED` — and
`landable` reads that mergeability verdict, not only its own advisory rollup — until this call's marker
is current, so waiting for `landable` green *before* making this call is a cycle the marker can never
break. No push should happen between this call and the merge below, and the ordinary flow does not
produce one — but the reorder does not merely hope that holds: if a push ever did land in that window,
`claim-generation`'s own check compares the marker's `head=` field against the PR's actual current head
and fails closed on any mismatch (`scripts/check-claim-generation.py`), and the workflow re-runs on
`synchronize` as well as on the marker's `edited` PATCH (`.github/workflows/coherence.yml`) — so a
stale marker cannot silently carry a merge on a new head; it forces `claim-generation` red again until
this call is re-issued, which is a zero-cost no-op PATCH-skip against an already-current marker and
therefore always safe to make again. Making the call here binds the marker to the identical head
`landable` is about to score and the one that actually merges — the same property §6 has always
required, reached one step earlier.

This call's JSON output may report `action: refreshReview, stage: reviewActive` at this point, because
`claim-generation` and any other still-running checks have not yet reported against the marker this call
just wrote. **That is not a refusal** — the write happens unconditionally (`.github#2488` removed the
`--apply` gate), and the reported action is only the engine's own next-step suggestion from a review
chain it re-inspected before the checks caught up. Do not call it a second time; proceed straight to
waiting on `landable` below.

- **Once per item, at this exact point — after the host-acceptance marker and all repairs, and before
  `landable`'s check — never after opening the PR, and never on every push.** A call made earlier binds
  the marker to whatever head existed then; every later push re-stales it, and nothing short of this step
  calls `delivery` again. Calling it here is also sufficient on its own — `rebindAuthorization` makes a
  call against an already-current marker a zero-cost no-op PATCH-skip, so this is never a routine
  per-push network write.
- **Only the worker currently holding this item's live claim marker may make this call, and only before
  releasing that claim.** The live form itself refuses otherwise ("no live claim marker can authorize
  delivery") — a fresh critic or a worker after `done --flip` released the claim cannot make it on your
  behalf.
- **It must run from your own credentialed shell — never CI.** The live path's first action is a
  Coordination Projects-v2 board bootstrap (`Board.bootstrapCached`), a GraphQL read no CI credential in
  this org's inventory carries (ADR-0019 §1, `.github#2332`); routing this call through CI is refuted by
  that boundary, not merely undesirable.
- **A failed call is reported, never silently swallowed — and never blocks the merge.** The write is the
  only thing this call does; `claim-generation`'s conclusion is no longer advisory anywhere —
  `.github#1858` armed it into `main`'s required contexts, and `.github#2517` replaced `landable`'s
  hand-written carve-out with the DERIVED COMPLEMENT of that required set (`Landable.advisoryFrom`), so
  the engine's own rollup scores it `Blocking` too, agreeing with branch protection rather than
  contradicting it. Note the failure (to whoever dispatched you, or in the item's own history) and
  proceed with the merge.

Now wait on the typed `landable` verdict for that exact same head SHA — the marker this call just wrote
is what lets `claim-generation`, and therefore `landable`, go green. Merge only once `landable` reports
green for that exact head, and verify the merge on the default branch.
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
