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

**Under agent worktree isolation that line does not run, and the fallback is where identity is lost.**
Every `fsgg-worker-*` and `fsgg-critic-*` agent is dispatched worktree-isolated, and that harness
refuses `eval "$(…)"` as too complex to verify it stays inside the worktree. Do **not** improvise a
second way to conjure an id — there is no second way, and reaching for one is #419 from the other end.
Consume the **same** sanctioned command isolation-safely instead: run it as a plain single command,
read the id it prints, and carry that id by prefix.

```bash
scripts/fsgg-coord whoami --mint        # plain, single command; prints: export FSGG_WORKER=<id>
FSGG_WORKER=<id> scripts/fsgg-coord take --repo <repo> --json    # the FIRST board write — prefix it
```

Shell state does not survive between an agent's tool calls, so even where the `eval` is permitted it
sets `FSGG_WORKER` for exactly one command. The prefix is the only thing carrying your identity
forward, and it belongs on **every** later invocation.

**`take` is the invocation the prefix is missed on** (#2684). It is the first board write, it happens
before the prefix is a habit, and an unprefixed `take` does not fail: the engine falls back to the
agent harness's session id — which every subagent of one Claude Code session shares — warns that it
did, and then writes the claim under it anyway and returns **0**. A warning on a completed write is not a guard. Measured
in one wave: a converged claim marker on the right item under a session-derived id, and a sibling
worker that had done nothing wrong denied its item with exit 6.

So after `take` — and after any `claim` — **read the marker back and confirm it names the id you
minted.** If it names a session-derived id, stop and report it rather than working the item: you are
holding a lock that cannot separate you from any other agent in your session.

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
Apply the shared [measurement discipline](references/measurement-discipline.md) to every absence,
count, or unchanged assertion in implementation evidence and review handoffs.
Before implementing interactive/game work, run the
[performance-first planning gate](references/performance-first.md). Then fix causes, add focused
regression coverage, and run proportionate build/test/format gates. Poll inbox at phase boundaries.

Every gate this change adds or modifies **ships with evidence it can fail**: invert it, run the suite,
and record the mutation and the observed red. A test that passes both before and after the fix has not
tested the fix, and a gate whose inversion survives is a material finding at review by definition — see
[Gate-inversion evidence](references/independent-review.md#gate-inversion-evidence), whose numbered steps
also bound the fixture, the measurement environment, the unreadable input, and the repair that must catch
the escape actually found. Doing this at authoring time is cheaper than at
review time, and it makes the critic's step a confirmation rather than a discovery.

## 4. Route implementation findings

Fix in-scope causes now. For a distinct cause, **establish the root cause before you file** — a finding
is where a defect *surfaced*, which is rarely where it *lives*, and filing the surface is how one defect
gets seven numbers (#266). Then **dedupe over REST against that cause, not against the symptom**: reuse
an existing issue that expresses the same cause and transplant your evidence onto it instead of opening
a second row. File only when no row carries that cause. Use the complete `fsgg.coord.intake/v1` draft
shown in [deep detail](references/deep-detail.md): put observed behavior, **the root cause** — or the
measurement that remains unestablished — acceptance, verification, `paths`, `class`, `severity`, and
optional `blockedBy` in the draft, run `scripts/fsgg-coord intake validate`, then `intake apply` on
that same file. A hand-authored `Paths:` or `Class:` line in a created body is a defect, not a style
choice. Link dependencies only when authorship truly depends on landed work, then add it to the
follow-up queue.

[findings-and-filing](references/findings-and-filing.md) carries the rest of this rule and is **binding,
not elaboration** — load it for the dedupe reads and the judgement boundaries. This section owns
findings discovered before independent review begins. Once review starts, do not file the critic's
findings or add them to a private follow-up queue; the critic owns their disposition, under
[Root cause, dedupe, and materiality](references/independent-review.md#root-cause-dedupe-and-materiality),
which is also where the definition of a **material** finding lives.

Every checkable assertion you write into the handoff carries `Verification:` or exactly `unverified`, per
[Handoff-assertion provenance](references/independent-review.md#handoff-assertion-provenance); a claim
about whether an issue or PR **body** changed is answered by `body-edits` and never by the REST timeline,
per [Body-edit provenance](references/independent-review.md#body-edit-provenance--the-rest-timeline-does-not-surface-body-edits).
The body you file is itself evidence someone will act on, so re-derive every checkable `path:line`, count
and suite-green claim in it before relying on it, per
[Issue and pull-request body evidence](references/independent-review.md#issue-and-pull-request-body-evidence)
— the review-time check that owns remote bodies, because a source-only CI checkout cannot read them.

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
implementer repair, awaiting a fresh successor's full review, passed awaiting checks, awaiting host
acceptance, ordinary exhaustion, repair-phase setup, repair-phase active review, accepted, or terminal
human park) and the one typed next action that follows from it — dispatch critic, resume implementer,
dispatch a fresh successor critic, await checks, request host acceptance, enter the one permitted fresh repair
phase, accept, or park for human action — bound to a freshness token that a changed head invalidates.
This is a mechanical cross-check, not a substitute for the qualitative judgement below: materiality,
critic-generation continuity, durable wait receipts, and repair-phase provenance are read from the live PR by both the worker
and the critic.

Push the candidate, open its PR, and ask the host to assign a fresh critic agent. Keep the implementing worker and
claim alive, set the item to `In review`, and freshly verify that row while the critic independently
reviews the exact head SHA. The critic does not edit the
implementation: it checks requirements, diff, tests, architecture, release obligations, and `Paths:`;
searches code/history and existing work for each candidate root cause; and files only unresolved,
distinct **material** work. For a meaningful runtime behavior reachable through more than one route,
the handoff supplies a built artifact and runnable production-route evidence so the critic can execute
or measure the comparison required by `independent-review`, not infer it from source alone. A fresh
successor performs each numbered repair review. If material findings remain after round three,
never start round four or merge that PR: close it without merging and automatically enter the one
fresh-worker, fresh-critic
[repair phase](references/independent-review.md#repair-phase). Park the item on `Blocked on:
human/action` and release the claim only if that repair phase exhausts or its required route is
unavailable.

Before yielding at every protocol-created critic queue, write the bounded entry event with
`scripts/fsgg-coord review wait <ref> <event.json> --pr <n> --json`. After a critic record lands, write
the matching completion event; cancellation and bounded timeout use the same command and generation.
Never treat a sleeping process as the receipt. On resumption, run live `review` and revalidate or
reacquire the current claim generation before any mutation. Use the canonical generation token
`<head>:initial-review:0` or `<head>:repair-confirmation:<round>`; dispatch and `review record` fail
closed without the matching waiting entry, and acceptance requires its completed critic-record evidence.

[independent-review](references/independent-review.md) is the binding contract for materiality, critic
ownership, the durable PR record, direct filing, confirmation, and host verification. Its
[Runtime-route evidence gate](references/independent-review.md#runtime-route-evidence-gate) states when
that comparison must be executed rather than read;
[Game functionality](references/independent-review.md#game-functionality--the-bot-driven-player-journey-gate)
states when a bot-driven player journey is blocking;
[Disposition and repair bounds](references/independent-review.md#disposition-and-repair-bounds) states the
park procedure and the critic's filing preconditions;
[A head that moves after the chain was accepted](references/independent-review.md#a-head-that-moves-after-the-chain-was-accepted)
and
[Reading the review state](references/independent-review.md#reading-the-review-state-a-designed-wait-is-not-broken-evidence)
state what a moved head and a designed wait do and do not mean. Do not merge
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

**Two answers here mean carry on, and they are the only two.** `stage: landable, action:
awaitLandability` is the one to expect: it is what a PR gets when its handoff facts are in order *and*
its review chain is complete and host-accepted, with checks not yet green — including the
`claim-generation` window that by design cannot be green until this very call writes
`fsgg:pr-authorization` (`.github#2504`). It is precisely the next step below: wait on `landable` for
this exact head. `stage: accepted, action: guardedLand` is the other, and far from being a surprise it is
the engine's **own merge authorization** — `authorizeGuardedLanding` refuses every other action outright
(`DeliveryApplication.fs`, `transition.Action <> Delivery.GuardedLand -> MergeRefused`), so this pair is
what a head that is already fully green looks like at this call site. You meet it whenever the marker was
current and the checks had settled before this call — typically a second call, after a repair below that
left the head unchanged. Proceed to the merge.

**Every other answer is a stop**, including any `verdict: noVerdict`, which carries no stage/action pair
at all. None of it is noise to proceed through. The write happened either way — it is unconditional
(`.github#2488` removed the `--apply` gate) — so repeating the call cannot conjure a different answer by
itself; what changes the answer is repairing the fact the stop names. Calling again *after* that repair
is expected rather than forbidden, and costs nothing: against an already-current marker the call is a
no-op PATCH-skip (see the bullets below).

`stage: reviewReady` is a stop about the **handoff**, and it is the likeliest one to meet here, because
`Delivery.inspect` tests these facts *before* it ever reads the review chain. `action:
repairReviewHandoff` means the item branch is not canonical, the canonical closing linkage is missing,
the declared paths are not verified, or the post-merge obligations are undeclared; `action: moveToReview`
means the board row is not `In review`. Two of those reach past this call — closing linkage cannot be
fixed once merged (`.github#2107`), and an undeclared obligation blocks the item's own completion, because
after a merge `Delivery.inspect` refuses a verdict outright while obligations are undeclared and holds the
item at `MergedAwaitingObligations` while any declared one is unverified — so repair the named fact and
call again rather than merging past it. Like `refreshReview` below, `repairReviewHandoff` renders without
its problem text, so read the cause off the PR itself.

`action: refreshReview, stage: reviewActive` is a stop about the review **evidence**. `.github#2575`
stopped folding the liveness clause "review checks are not green" into the review problem list, so a
still-running check can no longer produce that pair here. At this call site it now means the durable
review evidence is structurally invalid (`Delivery.reviewProblem` ->
`Driver.validateReviewChainStructure`) — chiefly a missing or invalid marker, a missing critic identity,
a missing review head SHA, rounds not ordered from one, an exhausted round ceiling, missing runtime-route
evidence, an unresolved diff-audit receipt, a missing host acceptance, or a chain recorded against a
different head SHA; it also carries any error the live adapter hit parsing the review comments at all,
which is a longer list. **Do not merge on it**, and report the chain to whoever dispatched you.
`delivery` prints the pair but not which cause it found (`DeliveryApplication.fs` renders `RefreshReview`
without its problem text), so identify it from the review chain itself rather than from this output. A
chain carrying no review evidence at all reports the distinct `action: awaitIndependentReview` instead.

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
scripts/fsgg-coord delivery <ref> --pr <pr> --flip --apply --json
```

This completion call re-inspects the exact merge and declared obligations, appends the typed completion
receipt first, and only then projects the closed issue, `Done` column, released claim, and cleanup state.
The exact green done stamp, zero pending board writes, and fresh board confirmation are completion. If any
is missing, repair it and repeat this same idempotent delivery call before reporting. Clean the worktree
and branch only after verification. Report the exact receipt/stamp and stop; one invocation owns one item.
