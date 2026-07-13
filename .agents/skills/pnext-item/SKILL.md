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
# budget is emptiest (#587). This loop must not do EITHER of the two things that make a merge gate
# lie: read only page 1 (a failing check on page 2 is invisible, and the aggregate then reads green
# — #547), or treat NO checks as GREEN checks (#606). Hence `--paginate`, and an assertion that the
# subject EXISTS before it asserts the subject is clean.
runs="repos/FS-GG/<repo>/commits/item/<n>-<slug>/check-runs"
checks() { gh api "$runs" --paginate --slurp | jq '[.[].check_runs[]]'; }

# 0. A CONFLICTED PR NEVER GETS CI. Look BEFORE you wait — no amount of waiting fixes this one.
gh api repos/FS-GG/<repo>/pulls/<pr> --jq '"mergeable=\(.mergeable) state=\(.mergeable_state)"'

# 1. Wait for the checks to REGISTER, and then to COMPLETE. A fresh push takes 20-60s to schedule,
#    so "no checks yet" early is normal; "no checks still" at the end is the finding.
for _ in $(seq 30); do          # ~10 min ceiling — poll, do not spin forever
  c=$(checks)
  counts=$(jq -r '"\(length) \([.[] | select(.status != "completed")] | length)"' <<<"$c")
  [ "${counts% *}" -gt 0 ] && [ "${counts#* }" -eq 0 ] && break
  sleep 20
done

# 2. ASSERT on the state the loop just read — exits NON-ZERO on no checks, on pending checks, and
#    on any check that is not green. NOTE the first branch is also where a FAILED API read lands
#    (empty `$c`), which is why it names that too: it fails CLOSED either way, but the worker needs
#    to know which one they are looking at before they go rebasing a PR that was never conflicted.
jq -e -r '
  if   length == 0 then error("NO CHECK RUNS — CI never started, so nothing has passed. Conflicted PR (rebase), or a failed API read. A missing subject is a FINDING, not a pass.")
  elif ([.[] | select(.status != "completed")]                                     | length) > 0 then error("checks still PENDING — do not merge")
  elif ([.[] | select(.conclusion != "success" and .conclusion != "skipped")]      | length) > 0 then error("checks FAILED — a red check is a finding, not an obstacle")
  else "all \(length) checks green" end' <<<"$c"

# MERGE over REST. This is the DEFAULT here, not a rate-limit workaround (#564) — see below.
# `<pr>` is the PULL number; `<n>` is the ITEM/issue number. They are NOT the same, and this fence
# uses both — the branch is named for the item, the merge endpoint takes the pull.
gh api -X PUT repos/FS-GG/<repo>/pulls/<pr>/merge \
  -f merge_method=squash -f commit_title="<title> (#<pr>)" --jq '"merged=\(.merged)"'

gh api -X DELETE repos/FS-GG/<repo>/git/refs/heads/item/<n>-<slug>    # the branch, explicitly
```

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
# And ASSERT rather than PRINT — the same three-way assertion as the merge gate above. A line that
# merely PRINTS `checks=0 failed=1` and exits 0 is a gate that fails open the moment an agent reads
# the exit code instead of the number (#606, #266). Same semantics in both places, deliberately.
SHA=$(gh api repos/FS-GG/<repo>/pulls/<pr> --jq .head.sha)
gh api "repos/FS-GG/<repo>/commits/$SHA/check-runs" --paginate --slurp \
  | jq -e -r '[.[].check_runs[]]
              | if   length == 0                                                    then error("NO CHECK RUNS — CI never started (conflicted PR? API error?). NOT a pass.")
                elif ([.[]|select(.status!="completed")]|length) > 0                then error("checks still PENDING — do not merge")
                elif ([.[]|select(.conclusion!="success" and .conclusion!="skipped")]|length) > 0 then error("checks FAILED — do not merge")
                else "all \(length) checks green" end'
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
