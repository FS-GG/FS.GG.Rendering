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

**Not globs** (ADR-0021, `.github#273`). A token matches by exact equality or subtree containment;
the only wildcard is a **trailing** `/**` or `/*`. A leading `**/` — or a `*` in the middle —
matches nothing, and a token that matches nothing would conflict with nothing, i.e. read as
`DISJOINT` against everything. So the tool **refuses** it: `claim`, `widen`, `batch`, `overlap`,
and `verify-paths` all reject an unmatchable token and name it. Want every lockfile? List them.

**Editing a kit source obliges `registry/repos.yml`** (`.github#469`, ADR-0019). The coordination kit
is **content-addressed**: `registry/repos.yml` pins a `sha256` of every kit source — `scripts/fsgg-coord`
and each `.claude/skills/<kit>/` directory. Any edit to one invalidates its digest, and `repos-registry`
reds `main` until it is regenerated. So the touch-set for a kit change is **three** files, not two:

```sh
scripts/repos.sh digest scripts/fsgg-coord    # then commit registry/repos.yml
```

`widen` and `verify-paths` now name this the moment a kit source appears in a touch-set — advisory,
because `repos-registry` is the authority. Note what bit before they did: `verify-paths` asks *"did the
PR stay **inside** what you declared"*, never *"was your declaration **sufficient** for what you
touched"* — so a touch-set of `scripts/fsgg-coord` alone reported **OK** and red `main` anyway. A gate
can be most reassuring exactly when it is least informed (epic `.github#266`).

**A declaration is a line you wrote as one** (`.github#277`). A `Paths:` line inside a fenced
(``` or `~~~`) or indented code block is a **quotation**, not a declaration — quote freely in
reproductions and suggested `widen` commands. So an issue whose only `Paths:` line is fenced declares
**nothing** and is refused as undeclared, rather than silently reserving the files it quoted. Indent a
real declaration by **at most 3 spaces, and never a tab** — markdown reads 4 spaces (or a tab) as a
code block, and so does the reader.

Quote in a **fence**, not bare. A bare `Paths:` line at column 0 *is* a declaration, and if you leave
two of them the reader **unions** both — it over-reserves rather than guess which one you meant, so
you get a loud false `OVERLAP` instead of a silent collision. `widen` collapses them back to one.

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
scripts/fsgg-coord release <issue>          # drop the lease; In progress -> Ready
scripts/fsgg-coord release <issue> --status Blocked   # ...drop it, but say where it lands
scripts/fsgg-coord child <parent> <issue>   # link <issue> as a sub-issue — see §5
```

`release` drops the **lease**, which is not the same claim as *"this item is startable"*. It resets
the `In progress` that `claim` set, and only that: a `Status` you chose deliberately — `Blocked`,
`Backlog`, `Done` — is preserved, and a `Status` it cannot read is left alone rather than guessed.
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
