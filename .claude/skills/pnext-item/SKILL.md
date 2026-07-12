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
- **A rate-limited board write is DEFERRED, not lost.** *Every* board write — `set-field`, `claim`,
  `done --flip`, `release`, `reap` — says so, queues it, and `fsgg-coord flush` (or the next board
  write) replays it. Do not "fix" the board by hand; you will just duplicate the write.
  Until `.github#510` this was true of `claim` **only**, while the exhaustion message promised it to
  everyone — so a `set-field` on an exhausted budget printed "Board WRITES are queued" and dropped
  the write, and `flush` then reported "nothing pending" and confirmed the lie. If you are running an
  older kit, check `fsgg-coord flush --dry-run` after any board write you did not see land.
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
  **One exception, and it is the rule from §1: do NOT `widen` onto a generated artifact.** If the
  file you just touched is one a checked-in generator emits and a CI regeneration gate guards, you
  regenerated it — you did not author it. Declaring it now reserves a file nobody owns and
  serialises every other item that regenerates it, which is the exact failure §1 keeps you out of.
  Leave it undeclared, and name it as expected drift in the PR (§5).
- **Heartbeat long work.** A claim goes stale after `FSGG_CLAIM_LEASE_MIN` (default 120m) without
  one, and the next claimant collects it.

  ```sh
  scripts/fsgg-coord heartbeat <issue>
  ```

  **An expired lease cannot be renewed.** `heartbeat` refuses, names whoever holds the item now, and
  tells you to stop working it. Believe it. Re-take with `claim`, or walk away — renewing a dead
  marker would put two workers on one item, which is the entire failure this protocol exists to
  prevent.
- **Commit with the trailer `claim` printed** — the literal line, with your id already in it — so
  attribution survives into history. No id is written here to copy (#551), and **do not derive one**:
  `$FSGG_WORKER` is empty if your id came from the worktree name, and `$(git config fsgg.worker)`
  returns whoever claimed most recently (it is repo-shared unless `extensions.worktreeConfig` is set).
  A blank trailer loses the attribution; a borrowed one asserts a false one.

  ```sh
  git commit --trailer "FSGG-Worker: <the id `claim` printed>"
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
#    --paginate is NOT optional: this endpoint pages at 30, and the parents worth checking are
#    exactly the big ones. Without it, #266 lists 30 of its 51 children and confidently omits
#    the rest (#547).
gh api repos/FS-GG/<repo>/issues/<parent>/sub_issues --paginate --jq '.[] | "#\(.number) \(.title)"'
```

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
# REST: every `gh pr` subcommand is GraphQL, and by now the budget is usually gone (#587, #528).
gh api -X POST repos/FS-GG/<repo>/pulls \
  -f title="$(git log -1 --format=%s)" \
  -f body="$(git log -1 --format=%b)" \
  -f head="item/<n>-<slug>" -f base=main --jq .html_url

scripts/fsgg-coord verify-paths --pr <n>    # did the PR stay inside its declaration?
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
# Wait for CI over REST — every `gh pr` subcommand is GraphQL, and this is the one you run when the
# budget is emptiest (#587). `--paginate` is not optional: a failing check on page 2 is invisible
# without it, and the aggregate then reads as green (#547).
runs=repos/FS-GG/<repo>/commits/item/<n>-<slug>/check-runs
pending() { gh api "$runs" --paginate --jq '.check_runs[] | select(.completed_at == null) | .name'; }
until [ -z "$(pending)" ]; do sleep 20; done
gh api "$runs" --paginate --jq '.check_runs[] | [.conclusion, .name] | @tsv' | sort

# MERGE over REST. This is the DEFAULT here, not a rate-limit workaround (#564) — see below.
gh api -X PUT repos/FS-GG/<repo>/pulls/<n>/merge \
  -f merge_method=squash -f commit_title="<title> (#<pr>)" --jq '"merged=\(.merged)"'

gh api -X DELETE repos/FS-GG/<repo>/git/refs/heads/item/<n>-<slug>    # the branch, explicitly
```

**Why not `gh pr merge <n> --squash --delete-branch`?** Because §2 mandates a worktree, and under
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

**`done --flip` is a board write, so an exhausted budget DROPS it — silently.** It exits 75 saying
*"Board WRITES are queued: see `fsgg-coord flush`"*, and that is **false**: only `claim` defers.
Nothing is queued, `flush` has an empty queue and will cheerfully report success, and your merged
work sits **unstamped** with your claim still reserving its touch-set
([#510](https://github.com/FS-GG/.github/issues/510)). Check, don't trust:

```sh
scripts/fsgg-coord budget | jq .pendingBoardWrites   # 0 means your stamp was DROPPED, not queued
```

**Wait for the reset and re-run `done --flip`.** Do not hand-set `Status` to close the gap — an
unstamped item is a nuisance; a hand-stamped one is a board that lies.

### REST when the budget is gone

The **merge** is already REST above — it has to be, under §2's worktree (#564). These are the *other*
`gh pr …` / `gh issue …` commands, which are GraphQL and die on an exhausted budget with
`API rate limit already exceeded`. Verified end-to-end at **0 remaining** GraphQL. `gh api repos/…`
spends the **REST** budget, which is separate and almost never exhausted — `fsgg-coord budget` shows
both.

```sh
# CREATE the PR  (gh pr create is GraphQL)
jq -n --arg t "<title>" --rawfile b pr-body.md \
      '{title:$t, body:$b, head:"item/<n>-<slug>", base:"main"}' \
  | gh api -X POST repos/FS-GG/<repo>/pulls --input - --jq '"PR #\(.number)  \(.html_url)"'

# WATCH the checks  (gh pr checks is GraphQL)
# --paginate, again, and it matters MOST here: check-runs pages at 30, this repo has ~30 workflows,
# and a truncated read reports `pending=0 failed=0` while the checks that would have stopped you sit
# on page 2. That is a merge gate that greenlights a red PR (#547).
# `--slurp` cannot be combined with `--jq`, so aggregate in a separate `jq`.
SHA=$(gh api repos/FS-GG/<repo>/pulls/<n> --jq .head.sha)
gh api "repos/FS-GG/<repo>/commits/$SHA/check-runs" --paginate --slurp \
  | jq -r '[.[].check_runs[]]
           | "checks=\(length) pending=\([.[]|select(.status!="completed")]|length) failed=\([.[]|select(.conclusion!=null and .conclusion!="success")]|length)"'
```

`gh pr checks <n> --watch` itself is fine in a worktree — it is GraphQL, but it reads the API and
never touches your local checkout, so only the budget can take it from you, not the layout.

Two more that bite in the same state:

- **`verify-paths` blames the wrong thing.** It reports *"not inside a GitHub checkout"* when the real
  cause is the rate limit, because it derives the repo via a GraphQL call and reads the empty result
  as "no checkout" ([#430](https://github.com/FS-GG/.github/issues/430)). Pass the repo explicitly:
  `scripts/fsgg-coord verify-paths --pr <n> --repo FS-GG/<repo>`.
- **`gh issue create` is GraphQL too** — which strands you in §4, at the exact moment you are filing
  a finding after a long session, i.e. precisely when the budget is gone:

  ```sh
  jq -n --arg t "<title>" --rawfile b body.md \
        '{title:$t, body:$b, labels:["cross-repo","cross-repo:request"]}' \
    | gh api -X POST repos/FS-GG/<target>/issues --input - --jq '"#\(.number) \(.html_url)"'
  ```

  The **board** placement that follows it (`gh project item-add`, `set-field`) is Projects v2 and has
  no REST form. It cannot be done on an exhausted budget, and `set-field` will *say* it queued the
  write and drop it (#510). File the issue now, place it after the reset, and say so on the issue so
  the gap is a decision somebody made rather than an omission nobody noticed.

## 6. Clean up, then go again

Before you remove the worktree, empty your head into the board (§4). Everything you noticed and did
not fix is about to become unrecoverable — the branch is gone, the context is gone, and the next
worker rediscovers it from scratch.

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

`release` puts back **the column the claim overwrote** — a `Backlog` item returns to `Backlog`, not
`Ready` (#481). `Ready` is only the fallback for a claim that recorded nothing to restore. A column
you set *deliberately* during the lease still wins, so `release` will not undo a `Blocked` you meant
(#331); to land somewhere specific, say so: `release <issue> --status Blocked`.

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
