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

When you set it, **mint it with the tool — never invent it, and never copy one from a document**:

```sh
eval "$(scripts/fsgg-coord whoami --mint)"
```

This is the **one** mint idiom across the tool, the protocol doc, and both skill roots — the line
`whoami`'s own warning prints (#551).

A hand-picked id is the same-account bug a *third* level down. Agents asked to name themselves
converge — this board has carried **four `finch-*` workers at once**, every one of them copied from
the worked example that used to sit on this line, while the ids `whoami` *mints* spread cleanly
across the word list. Two workers holding one id defeats every operation that keys on it: `release`
drops the other's claim mid-flight, `heartbeat` renews a marker that is not yours, `say`/`inbox`
cross-deliver, and `claim`'s CAS cannot tell its own marker from its twin's. Re-rolling the suffix
is not enough — the attractor is the **word**, and any literal printed here becomes one (#419).
Which is why there is none: not on this line, not in the loop below, not in the protocol doc.

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

**Once per machine, first:** `dotnet tool install -g FS.GG.Coord.Cli`
**And keep it current:** `dotnet tool update -g FS.GG.Coord.Cli` — a global tool does NOT self-update, and
**a stale engine is worse than no engine** (#655). `fsgg-coord` carries a floor and REFUSES to shadow
below it (a recorded skip, never an error), because engines before `0.1.1` mis-parse every dotfile path
and will call a HELD item startable (#649).

Optional, and safe to skip — `fsgg-coord` works exactly as before without it. What it buys is the
**shadow** (ADR-0034): with an engine present, every `take`/`next`/`batch` is decided by *both* the
bash client and the typed F# engine, **bash's answer is still the one you get**, and any disagreement
is logged (`fsgg-coord divergence`). Your run does not change — not the answer, not the exit code.

It is worth the one command because the shadow is how the port earns its cutover: bash stays
authoritative until that log has been clean across the live fleet for three consecutive days, and the
log only fills where an engine exists. A worker without one contributes no evidence, and the clock
does not move.

**Your evidence is published for you.** `done --flip` sends it to the fleet ledger as part of finishing
the item — no extra step, nothing to remember (#656). This used to be a request, and across 28 workers and
597 compared verdicts it was run **zero times**. Asking is not a mechanism.

Run it by hand only if you stop without finishing an item:

```sh
fsgg-coord divergence --publish                # your local log is not evidence until it is published
fsgg-coord divergence --fleet                  # where the FLEET stands: 0 green · 1 red · 3 no verdict
```

The log lives in a *cache dir* that dies with your container. The criterion is about the **live fleet**,
and a worker who shadows but never publishes has moved the clock exactly as far as one who never
shadowed at all (#634).

```sh
eval "$(scripts/fsgg-coord whoami --mint)"     # MINT one; never invent or copy one (#419, #551)
scripts/fsgg-coord take --repo <this-repo>     # pick + claim the next SCHEDULABLE item, retrying a lost race
git worktree add ../<repo>-<n> -b item/<n>-<slug> origin/main   # `take` prints this; name the base
# ...implement, commit with the printed FSGG-Worker trailer, PR into main...
scripts/fsgg-coord done <issue> --flip         # earn the stamp
```

`take` is the entry point. It asks `batch` what is schedulable *right now* (disjoint from
everything in flight), claims it, and — on a lost race — **re-schedules** rather than going
home. Use `claim <issue>` only when you must have a *specific* item.

## 1. Declare the touch-set (on every parallelizable item)

Each item's issue body carries a **`Paths:`** line naming the file subtrees it will touch —
comma- or space-separated **exact paths and directory prefixes**. This is the intra-repo analogue
of a contract: it makes the shared surface explicit and checkable *before* work starts.

```
Paths: src/Scene/**, tests/Scene/**, Directory.Packages.props
```

**An item with no touch-set says `Paths: none`** (`.github#496`) — an epic, a decision item, an
investigation whose scope *is* the question. It stays unschedulable either way; the sentinel makes
the absence **deliberate and machine-readable**. `fsgg-coord lint` errors (`NO-TOUCH-SET`) on a
`Ready`/`Backlog` item declaring neither, because the alternative is what actually happened: an epic
and an omission rendered identically, nine items of real work went invisible to every worker who
asked for work, and the one surface whose job is board health reported `0 error(s)` over a dead queue.

**Not globs** (ADR-0021, `.github#273`). A token matches by exact equality or subtree containment;
the only wildcard is a **trailing** `/**` or `/*`. A leading `**/` — or a `*` in the middle —
matches nothing, and a token that matches nothing would conflict with nothing, i.e. read as
`DISJOINT` against everything. So the tool **refuses** it: `claim`, `widen`, `batch`, `overlap`,
and `verify-paths` all reject an unmatchable token and name it. Want every lockfile? List them.

**Editing a kit source obliges `registry/repos.lock` — and you must NOT reserve it** (`.github#469`,
`#527`, ADR-0019). The coordination kit is **content-addressed**: `registry/repos.lock` pins a `sha256`
of every kit source — `scripts/fsgg-coord` and each `.claude/skills/<kit>/` directory. Any edit to one
invalidates its digest, and `repos-registry-selftest` reds `main` until it is regenerated:

```sh
scripts/repos.sh relock          # regenerates registry/repos.lock
```

**The touch-set for a kit change is the kit source ONLY.** `registry/repos.lock` is a **generated,
CI-gated artifact**, so `#309`'s rule applies to it: *do not reserve a generated artifact*. Regenerate
it, commit it, and name it as **expected drift** in the PR body. A collision in it is a **rebase, not a
decision**.

> This paragraph used to say the opposite — reserve `registry/repos.yml`, run `repos.sh digest`. Both
> were true before `#527`, which moved the digest out of the authored roster and into the generated
> lock *precisely to end the deadlock that reserving it caused*: three workers serialised on one file
> in a single afternoon (`#428`). `#527` touched seven files and **none of them was a skill**, so this
> recipe went on telling every worker to re-create the deadlock the fix had just removed, and
> `repos.sh digest` — which still exists, and now writes nothing — went on reporting success and doing
> nothing (`#588`, `#563`; epic `#416`). The rule reached the registry and the tool, and not the one
> artifact a worker actually loads. That is the projection defect, and it is why ADR-0034 proposes
> generating this file rather than copying it.

`widen` and `verify-paths` now **look**: they recompute each kit source's digest and compare it against
`registry/repos.lock`, so the warning fires exactly when the lock is genuinely stale and goes quiet
exactly when you have relocked. It used to ask whether you had *declared* a file, and call the
obligation met if you had — which was silent in precisely the case where it was wrong (`#563`).
Advisory, because `repos-registry-selftest` is the authority.

**A declaration is a line you wrote as one** (`.github#277`). A `Paths:` line inside a fenced
(``` or `~~~`) or indented code block is a **quotation**, not a declaration — quote freely in
reproductions and suggested `widen` commands. So an issue whose only `Paths:` line is fenced declares
**nothing** and is refused as undeclared, rather than silently reserving the files it quoted. Indent a
real declaration by **at most 3 spaces, and never a tab** — markdown reads 4 spaces (or a tab) as a
code block, and so does the reader.

Quote in a **fence**, not bare. A bare `Paths:` line at column 0 *is* a declaration, and if you leave
two of them the reader **unions** both — it over-reserves rather than guess which one you meant, so
you get a loud false `OVERLAP` instead of a silent collision. `widen` collapses them back to one.

**Do not reserve generated artifacts** (`.github#309`). A `Paths:` token names a file two workers might
*author* into conflict. A file produced by a checked-in generator and guarded by a **regeneration gate**
— a CI check that re-runs the generator and fails on any diff — is neither authored nor semantically
conflicting: **a collision in it is a rebase, not a decision.** Declaring it reserves a file nobody owns,
and serialises every item that regenerates it. `FS.GG.Game` is the instance: one generated
`readiness/surface-baselines/<pkg>.txt` per package, and every `[core]` item appends a line to it — so
declared honestly, every `[core]` item overlapped every other and the whole `P6 Game` phase collapsed to
**one worker**, in the phase the protocol exists to fan out.

Two conditions, and the second is not optional:

1. **Nobody authors it.** Ask whether a human makes a *merge decision* in the file. If two workers' edits
   can only be reconciled by re-running a script, neither has an intent to preserve. The test is
   **authorship, not `.gitignore`** — a generated file that is committed and reviewed is still not
   authored.
2. **A regeneration gate guards it.** Excluding the file moves the guarantee from the *scheduler* to
   *CI*, so CI must actually have it. **If nothing fails on a stale copy, do not exclude it** — you would
   be trading a loud false `OVERLAP` for a silent unguarded staleness, which is strictly worse. Add the
   gate first, or keep declaring the file.

**Beware the subtree.** `overlap` matches directory prefixes, so declaring the artifact's *parent*
reserves it exactly as effectively as naming it. Declare your sources, not the directory the generated
file happens to sit in — `src/Core/**, readiness/**` locks the baseline against the whole board just as
`readiness/surface-baselines/**` does, while `src/Core/Pathfinding.fs, tests/Core/PathfindingTests.fs`
locks nothing it does not own.

**Declare against what the generator emits, not against the issue's prose.** `FS.GG.Game#31`'s acceptance
said "surface baseline"; it adds a *function* to an existing module, and the generator emits one exported
**type** per line — so it never touched the baseline at all. A `Paths:` line asserted from an issue body
rather than from the generator's output is how a *false global lock* gets created.

**Then expect the drift, and name it.** Excluding an artifact you go on to regenerate is exactly what
`verify-paths` reports as touch-set drift, and it cannot today tell that from a real undeclared edit
(`.github#498`). **Say which one it is in the PR** — an advisory that fires on correct behaviour is one
workers learn to skip past.

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
scripts/fsgg-coord release <issue>          # drop the lease; Status RESTORED to what the claim overwrote
scripts/fsgg-coord release <issue> --status Blocked   # ...drop it, but say where it lands
scripts/fsgg-coord child <parent> <issue>   # link <issue> as a sub-issue — see §5
```

`release` drops the **lease**, which is not the same claim as *"this item is startable"*. It undoes
the `In progress` that `claim` set, and only that — but note *how*, because the two cases are
different mechanisms (#481):

- **Restored.** `claim` **overwrites** the column, so it *records* what it overwrote. `release` puts
  that back: a `Backlog` item returns to `Backlog`. It is not preserved — it is remembered. `Ready`
  is only the fallback for a claim that recorded nothing (a pre-#481 marker, or a column that could
  not be read). Guessing `Ready` was the bug: since #440 made `claim` reachable from `Backlog`,
  every undo path quietly **promoted** triaged work into the queue humans read as ready.
- **Kept.** A `Status` you set *deliberately during* the lease — `Blocked`, `Done` — is left alone
  (#331). A column it cannot read is left alone too, rather than guessed.

`reap` collects an expired lease under the same rule, so a claim that dies on a `Blocked` item does
not resurrect it as `Ready`. So handing back an item you cannot finish keeps its column honest:

```sh
scripts/fsgg-coord set-field <issue> 'Blocked by' 'FS-GG/<repo>#<n>'
scripts/fsgg-coord release   <issue> --status Blocked
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

**Always name the base ref** — `git worktree add … -b item/<n>-<slug> origin/main`. With no
commit-ish, `-b` branches from the *shared checkout's* `HEAD`, and this protocol's whole premise is
that N workers pass through that checkout: its `HEAD` is routinely another worker's unmerged branch,
not `main`. Omit the base and the item's PR silently carries that branch's commits too. Nothing warns
you: `verify-paths` reports the resulting drift only as an advisory, and only for as long as that
sibling branch stays unmerged (.github#319).

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

`widen` **re-declares** a touch-set mid-flight. It sets `Paths:` to exactly what you pass — it does
not union with what was there — then re-checks the result against every live claim **and notifies
whoever it now collides with**:

```sh
scripts/fsgg-coord widen <issue> --paths "src/Scene/**, src/Audio/**"   # non-zero on a collision
```

Stop editing the shared paths until the collision is resolved.

**So it narrows, too — and narrowing is the direction nobody ever uses.** Pass a smaller set and the
reservation genuinely shrinks. A narrowing can never be refused for collision, because a subset
collides with nothing its superset did not; the capability is already there and it is safe. The name
says "widen" and only the *growth* direction is ever taught, so an over-reservation is **never handed
back** — it holds for the full lease, against files nobody is touching, and the workers it locks out
see only a dead queue ([#601](https://github.com/FS-GG/.github/issues/601)).

**When you learn your declaration over-reserves, re-declare it smaller — at once, not at merge.**
[pnext-item §3](../pnext-item/SKILL.md) names the two triggers that actually fire.

## 5. Finish — the earned done-stamp (unchanged)

```sh
scripts/fsgg-coord done <issue> --flip     # green FSGG-DONE only after PR merged AND Status=Done
```

Same stamp and epic roll-up as cross-repo. Check your PR stayed inside its declaration:

### Never write a closing keyword next to an issue you do not mean to close

GitHub scans the PR body for `close|closes|closed|fix|fixes|fixed|resolve|resolves|resolved` followed
by an issue ref, and links the two. **It does not parse the sentence.** A body that said, in as many
words, `It does not close #422` **closed #422** on merge — the string contains `close #422`, and the
negation is invisible to the parser. The board then stamped the item **Done**, so an open, unfinished,
explicitly-not-done item was closed and stamped with its acceptance criteria unmet
([#643](https://github.com/FS-GG/.github/issues/643)).

`done --flip` cannot save you here: it refuses to stamp work that is not *merged*, and this work
**was** merged — it just did not *finish the item*.

It needs no negation, either — only adjacency. Narrative past tense (`On merge, GitHub closed #422`),
a quoted example, a deferral (`a follow-up will resolve #N`): none carries the word "not", and every
one closes an issue. So the rule is:

> **Say what you close, on a line that says nothing else. Everywhere else in the body, GitHub must
> not be able to bind a keyword to a number.**

```
Closes #643.                  ← a declaration: the whole line, nothing else on it
Closes #1, closes #2.         ← REPEAT the keyword. `Closes #1, #2` closes only #1; the bare
                                `#2` binds to nothing and is silently dropped.
```

Everywhere else, deny the binding: **reword the verb** (`does NOT complete`, `addresses`,
`supersedes`), **drop the verb** (`Refs #422.`), or **break the adjacency** (quote the number without
its `#`).

**Writing it as code does NOT work, and this line used to say it did**
([#683](https://github.com/FS-GG/.github/issues/683)). Two parsers read a PR and they disagree about
code. The **markdown** parser builds the PR's `closingIssuesReferences` link and skips code — but what
**closes** the issue on a squash merge is the **commit message**, and that is **plain text**, in which
backticks and fences are ordinary characters. PR
[#681](https://github.com/FS-GG/.github/pull/681) — the PR that shipped the gate against this bug —
took its own advice, wrote its examples in backticks, and **re-closed #422**. The docs may quote the
bug in code because a file in the tree is never parsed for keywords; **a PR body may not.**

The `closing-keywords` gate fails the PR on every undeclared closing reference, and it now scans the
**raw** body — code included — because that is the parse that closes the issue.

### If you filed an issue, LINK it — a mention is not a link

`done --flip` rolls an epic up from its **native sub-issue graph**. A `(j) child of #266` title, a
`Child of #266` comment, a line in the epic's body — each *looks* like a link, and none of them is
one. File a child of an epic without linking it and the roll-up cannot see it, so the epic can be
stamped `Done` over your still-open issue. That happened: an epic completed thirty minutes after an
open child of it was filed (#325).

```sh
scripts/fsgg-coord child <parent> <new>    # the ONLY thing that creates the edge rollup reads
```

Run it the moment you file, not at close-out — the whole failure is a worker who moved on. `child`
is idempotent, so re-running it is free. It also puts the REST API's two traps in one place: the
endpoint keys on the child's **id**, not its number, and `gh api -f sub_issue_id=…` sends that id as
a string and gets a 422 — it needs `-F`.

`done --flip` now **refuses to roll up** an epic whose body declares a child that is not linked, and
names it; `fsgg-coord lint` reports the same as `EPIC-UNLINKED-CHILD`. Both read the epic's body
task-list (`- [ ]` / `- [x]` lines naming an issue) as its second, human-legible record of what its
children are, and hold the two records to agreement.

So write the body's task-list the way the matcher reads it, or it counts a child you did not mean.
Only a `- [ ]` / `- [x]` line (also `*`/`+` bullets, `[X]`) indented **at most three spaces**
declares a child, and only its **first** issue ref does:

- **The first ref wins**, positionally in the raw line — a `#n` in a code span or a link's text
  still counts. Put the child ref first and let an aside (`(cf. #100)`, `ties to #163`) trail, or
  the aside becomes the declared child.
- **A bare `#n` resolves against the epic's own repo.** `SDD#109` has no slash, so it is read as
  `.github#109`. Write cross-repo children fully qualified: `FS-GG/FS.GG.SDD#109`.
- **A PR is not a child** — the sub-issue graph holds issues, never PRs. Cite a line a PR delivered
  as a **bare** `/pull/` URL (`https://github.com/FS-GG/.github/pull/239`), which carries no `#n`
  and so declares nothing. `(PR #239)` or `[PR #239](…)` reads as an ordinary `#n`; since #346 the
  gate re-resolves it and drops the ones GitHub confirms are PRs, so a genuine same-repo PR ref no
  longer lingers — it survives only if the number is an *issue* or will not resolve (kept
  fail-closed, #266). The `/pull/` URL stays cleanest: it declares nothing and needs no REST probe.

Canonical, with the live incidents: `docs/coordination/parallel-work.md` §"What the body counts as a declaration".

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
