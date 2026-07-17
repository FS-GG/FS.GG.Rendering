---
name: cross-repo-coordination
description: Coordinate work across the FS-GG repos (FS.GG.SDD, FS.GG.Rendering, FS.GG.Governance, FS.GG.Templates, FS.GG.Game, FS.GG.Audio). Use when you need something from another FS-GG repo, are changing a versioned cross-repo contract, hit a cross-repo version/API incoherence, or need to place/sequence work on the org-level "Coordination" Projects v2 roadmap. File and answer requests as GitHub issues, track sequencing on the Coordination board, keep the contract/compatibility registry coherent, and record cross-repo decisions as ADRs. Canonical protocol lives in FS-GG/.github.
---

# Cross-repo coordination (FS-GG)

The FS-GG repos are deliberately decoupled but coupled at the edges through versioned
contracts. Coordinate through **GitHub-native primitives** — never a shared file
"mailbox" (git is not a queue). Canonical docs:

- Protocol: `FS-GG/.github` → `docs/coordination/README.md`
- Registry: `FS-GG/.github` → `registry/dependencies.yml` + `docs/registry/compatibility.md`
- Decisions: `FS-GG/.github` → `docs/adr/` (ADR-0001 mandates the Coordination board)
- GraphQL budget: `FS-GG/.github` → `docs/coordination/graphql-budget.md` (the `fsgg-coord` client)
- Board plan & architecture: `FS.GG.Templates` → `docs/reports/2026-06-27-github-projects-v2-coordination-roadmap-plan.md`
  and `docs/reports/2026-06-27-fsgg-packaging-composition-and-governance-architecture.md`

## The coordination layers (each owns one thing)

Coordination is layered; do not collapse them. Each layer answers a different question:

| Layer | Tool | Owns |
|---|---|---|
| Decisions — *why* | **ADRs** (`FS-GG/.github/docs/adr/`) | why a cross-repo choice was made |
| Contracts — *what* | **registry** (`registry/dependencies.yml`) | the versioned cross-repo surfaces |
| Messages — *requests* | **cross-repo issues** (this protocol) | requests/responses between repos (the "mailbox") |
| Feature detail — *the spec* | **`specs/<feature>/`** (per repo) | the spec for a unit of work |
| **Sequencing — *when / order / blocked / who*** | **Coordination Projects v2 board** | the roadmap / time layer |

The board **tracks**, it does not replace, the issue protocol: a cross-repo ask is still an
issue in the target repo; the board pulls that issue in and places it on the roadmap.

## When to use this skill

1. You need a change, decision, or release from **another** FS-GG repo.
2. You are about to change a **versioned cross-repo contract** (`scaffold-provider`,
   `scaffold-provenance`, `governance-handoff`, the governance config schemas,
   `fs-gg-ui-template`/`fs-gg-ui-version`).
3. You hit a **cross-repo incoherence** (a consumer can't build/run against an upstream).
4. You need to **place or sequence work on the Coordination roadmap** (set a phase/target,
   surface a blocker, add an epic + child issues).

## File a request (the "mailbox message")

A request is a **GitHub issue in the target repo**, using the org-wide
`Cross-repo request` template (labels `cross-repo` + `cross-repo:request`). Always name
the affected contract/registry id and the work it blocks; cross-reference with
`FS-GG/<repo>#<n>`, commit shas, and contract ids.

**First, check it is not already filed** — a REST read, **0 GraphQL**, and with N workers filing
eagerly the duplicates are not hypothetical (#459/#460 were the same ask, filed eleven minutes apart
by two workers who could not see each other's filing —
[#464](https://github.com/FS-GG/.github/issues/464)):

```sh
# --state all IS NOT OPTIONAL — a CLOSED hit is the one you most need to see (see below).
# `issues` emits raw JSON and has NO --jq flag (the one below is `gh api`'s). CAPTURE, don't pipe:
# piping hands you jq's exit code, so a FAILED read prints nothing and exits 0 — identical to
# "nothing filed", which is the answer that decides whether you file a duplicate (#874).
hits="$(scripts/fsgg-coord issues <target> --state all)" \
  || { echo "dedupe read FAILED ($?) — do NOT read this as 'nothing filed'"; exit 1; }
# contains(), not test(): the keyword is a literal; test("C++") matches any title with "c".
jq -r --arg k "<keyword>" '.[]
  | select(.title | ascii_downcase | contains($k | ascii_downcase))
  | "#\(.number) [\(.state)] \(.title)"' <<<"$hits"
gh api repos/FS-GG/<repo>/issues/<parent>/sub_issues --paginate --jq '.[] | "#\(.number) \(.title)"'   # filing a child? look here
```

**`--state all`, and this is the part that bites.** `issues` defaults to `state=open`, and this step
used to take that default — so it could not see a **closed** issue at all. But *"somebody already fixed
it"* is the **most likely reason a finding is a duplicate**. Worse, these skills are copied into an
agent's context at session start and never refreshed, so a worker can hit a bug the org repaired an
hour ago — and the mechanism that makes them rediscover it is the same one that guarantees its issue is
**closed**. The dedupe step was therefore blind to exactly the duplicates it exists to catch, and
answered a confident *"no hit"*: [#266](https://github.com/FS-GG/.github/issues/266)'s signature, and
how [#719](https://github.com/FS-GG/.github/issues/719) came to be filed against an already-merged fix.

**A closed hit is the BEST possible outcome** — the fix exists. Go read it, and if your case is not
covered, say so *on that issue* rather than opening a rival.

**`--paginate` is load-bearing, not tidiness.** `sub_issues` pages at 30, and the parents worth
checking are precisely the big, active epics where several workers are splitting one parent at once —
so the read truncates exactly where the duplicate it is meant to catch actually lives. Without it,
#266 returns 30 of its 51 children and says nothing about the other 21 (#547). Any `gh api` read of a
LIST needs it; `fsgg-coord issues` already pages for you.

On a hit, **comment on the existing issue** rather than opening a rival — the finding's value is its
context, and a comment carries that just as well.

**End the body with a `Paths:` line.** An issue with no declared touch-set **cannot be
scheduled**, and a request filed without one lands on the board looking like work while being
invisible to every worker who asks for work
([#442](https://github.com/FS-GG/.github/issues/442): twelve items, filed by the book, none
schedulable). You are the one holding the context — you can usually name the files better than
the eventual claimant can. The rules the engine will hold you to are below, generated from it.

```sh
# REST: `gh issue create` is GraphQL, and the budget is shared by the whole fleet (#587).
gh api -X POST repos/FS-GG/<target>/issues \
  -f title='[cross-repo] <short summary>' \
  -f 'labels[]=cross-repo' -f 'labels[]=cross-repo:request' \
  -f body="From: <your repo>. Blocks: <ref>. Contract: <id>. <what you need and why>

Paths: src/Scene/ tests/Scene/" --jq .html_url
```

### The rules your filing must satisfy

<!-- BEGIN GENERATED: fsgg-protocol:filing-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  These rules were restated by hand here, and #916 measured why that is not survivable: the
  hand-written copies AGREED with each other for as long as they existed and were wrong the
  whole time. Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that refuses your `Paths:` line is the engine that
wrote this. The full rule set, with the incident behind each one, is in
[intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**The touch-set grammar — it is NOT a glob language**

supported: an exact path ('src/Foo.fs'), or a directory prefix ('src/Foo', 'src/Foo/*', 'src/Foo/**'). There is no glob matcher: a leading '**/' or an interior '*' matches nothing — spell the paths out.

**A MERGED blocker is RESOLVED; an unreadable one BLOCKS**

`Blocked by` clears on CLOSED **or MERGED**. It does not clear on OPEN, on a blocker whose state could not be read (unverifiable), or on prose that is not an issue ref at all (unparseable) — all three BLOCK.

<!-- END GENERATED: fsgg-protocol:filing-rules -->

`fsgg-coord lint` goes **red** on a `Ready`/`Backlog` item that declares neither paths nor the
sentinel — so an omission is caught rather than sitting on the board looking like work.

## Respond / resolve

- **Respond:** comment on the issue, starting with `## Response` (or link a PR/issue).
  The target repo's agent/maintainer owns the response.
- **Resolve:** close the issue (ideally via a linked PR). The requester confirms.

```sh
scripts/fsgg-coord issues <repo> --label cross-repo   # REST + ETag: 0 GraphQL, free on repeat.
                                                      # `gh issue list` costs 2 pts to say the same thing.
gh issue comment <n> --repo FS-GG/<repo> --body "## Response ..."
```

## Track on the Coordination board (Projects v2)

ADR-0001 mandates a single **org-level Projects v2 board named `Coordination`** owned by
`FS-GG`, spanning all four product repos + `.github`. It is the **sequencing/time layer** —
ADRs answer *why*, the registry holds *contracts*, `specs/` hold *detail*; the board answers
*when / in what order / who is blocked*. Use it instead of per-repo milestones for the
cross-repo roadmap (milestones are repo-scoped; keep them for repo-local release cuts only).

**Custom fields** (Projects v2 fields, distinct from org issue-fields):

| Field | Type | Options / notes |
|---|---|---|
| `Status` | single-select | `Backlog`, `Ready`, `In progress`, `Blocked`, `In review`, `Done` |
| `Phase` | single-select | `P0 Decisions`, `P1 Rendering`, `P2 SDD`, `P3 Governance`, `P4 Templates`, `P5 Versioning`, `P6 Game`, `P7 Audio` |
| `Repo Scope` | single-select | `rendering`, `sdd`, `governance`, `templates`, `.github`, `cross-repo`, `game`, `audio` — **not** `Repository`, which is Projects' own built-in column |
| `Workstream` | single-select | `Composition`, `Lifecycle`, `Governance`, `Versioning`, `Docs`, `Coordination` |
| `Start` / `Target` | date | feed the Roadmap (timeline) view |
| `Effort` | single-select | `S`, `M`, `L`, `XL` |
| `Contract` | text | registry id the item touches (e.g. `fs-gg-ui-template`) |
| `Blocked by` | text | what blocks THIS item — comma-separated issue refs; Projects has no typed dependency field |

**Conventions** (also in the board README):

- **One roadmap item = one issue** in its owning repo. Cross-repo asks additionally carry
  `cross-repo`/`cross-repo:request` and live in the *target* repo.
- Set `Phase`, `Repo Scope`, `Workstream`, `Target`, and (for cross-repo work) `Contract` on every
  item. `Blocked` status mirrors the `blocked` label. One phase per product repo: a `game` item is
  `P6 Game`, an `audio` item `P7 Audio`. Do not reach for `P1 Rendering` because a `Game.Core` item
  happens to do geometry — `Repo Scope` decides the phase, not the subject matter.
- **`Blocked by` records the dependency edge, nothing else.** `fsgg-coord set-field` takes a
  comma-separated list of issue refs (`owner/repo#n`, `repo#n`, `#n`, or an issue URL) and
  canonicalizes each to `owner/repo#n`; anything else is refused before the write. It is not a
  delivery log and not the inverse (`blocks X`) edge — narrative goes in an issue comment, "this
  item is blocked" goes in `Status`. Clear it (`''`) when the blocker resolves. `fsgg-coord next`
  reads it and skips a blocked item with the reason on stderr (`--ignore-blocked` overrides);
  **what counts as blocked is the engine's call, and it is stated once** under
  [*The rules your filing must satisfy*](#the-rules-your-filing-must-satisfy) above.
- **Epics are the Phase parents**; use **sub-issues** for the children so progress rolls up.
  An epic is a card whose **title** carries `[epic]` (Projects v2 issue types are unset on this
  board). `fsgg-coord lint` enforces the invariants: an **open** `[epic]` must have at least one
  sub-issue — a childless one is an **orphan** that never rolls up and that `next` will hand out as
  work, neither of which can happen once the epic is closed — and an epic the board calls `Done`
  must have no open child (this one holds for closed epics too). Run it before you re-sequence the board;
  it exits non-zero on a violation. `done --flip` only rolls an epic up once every child is both
  board-`Done` **and** issue-closed, so a merged-but-unclosed child can no longer complete an epic.
- A `contract-change` item must link its registry PR (ADR-0001) — put the registry id in
  `Contract`.
- The board is the source of *order*; the registry is the source of *contracts*; ADRs are
  the source of *decisions*. Re-sequence → board; changed surface → registry; reversed
  choice → ADR.

## Spend the GraphQL budget like it is shared — because it is

Projects v2 is **GraphQL-only**, GitHub's primary limit is **5,000 points/hour**, and every worker you
fan out authenticates as the **same account** and draws on the **same 5,000**. Cost is metered by
*nodes requested*, not by request count, so batching buys nothing. Five workers looping the board
drained the whole budget in **15 minutes** ([#418](https://github.com/FS-GG/.github/issues/418)) — and
the first thing exhaustion kills is the **writes**, so the board starts lying about who holds what.

**Route every board interaction through `scripts/fsgg-coord`.** It is not a convenience wrapper; it is
the budget model, executable. Reach for raw `gh` on the board and you are opting out of it.

| What you want | Run this | Cost |
|---|---|---|
| What do I pick up next? | `fsgg-coord next --repo <r>` | ~7 pts cold, **0 warm** (90s shared cache) |
| Everything actionable | `fsgg-coord ready --repo <r>` | ~7 pts — a **truth** read, always fresh |
| Set a board field | `fsgg-coord set-field <issue> <Field> <Value>` | 1 mutation, ids from cache |
| Read issues / labels / PRs | `fsgg-coord issues <repo> --label …` | **0** — REST + ETag (304s are free) |
| Am I about to run out? | `fsgg-coord budget` | **0** — free, so *check it before a fan-out* |
| Claim → work → done | `/pnext-item`, `/check-board` | they already batch their reads — don't hand-roll |

**Four habits, in the order they save you the most:**

1. **Take ONE snapshot; answer many questions from it.** `fsgg-coord ready --all --json > /tmp/board.json`,
   then `jq` that file for every follow-up. A second scan to answer a second question about the same
   board is the most common way to waste the budget, and `jq` over a file costs nothing.
2. **Never `gh project item-list`.** Measured: **6 points to read FIVE items** — about what the thrifty
   scan costs for all **640**. It nests `fieldValues(first:100)` inside `items(first:N)`, so its cost
   grows as O(items × 100). `next`/`ready` read the same fields through the `fieldValueByName`
   **resolver** (no node multiplication). This is the single biggest own-goal available to you.
3. **Put anything that isn't the board on REST.** `gh issue list` and `gh issue view` cost **2 GraphQL
   points each**; `gh issue edit --add-assignee` costs **4**. REST does all of it for **0** — a separate
   5,000-*requests*/hr budget that ETags to free on repeat. `fsgg-coord issues` is the read; `gh api
   repos/…` is the escape hatch for comments, PRs, and edits (and it keeps working when GraphQL is gone).
4. **Don't poll, and don't defeat the cache.** `next`/`take` share a 90s scan across all workers — a
   loop that adds `--fresh`, or a coordinator re-scanning per item, puts the N-workers-N-scans cost
   right back. Re-scan when you have *changed* something, not on a timer.

**When it runs out** (`fsgg-coord budget` shows it, and every command exits **75** with the reset time):
**back off until the reset — do not retry in a loop.** The claim lock lives on REST, so work continues:
issues, comments, PRs, and pushes all still function. A board write refused by the budget is **queued,
not lost** — `fsgg-coord flush` replays it (#878), but you must RUN it: nothing flushes automatically,
so check `fsgg-coord flush --dry-run` before you assume a write landed.
`FSGG_COORD_DEBUG=1` logs every call's cost, so **verify the saving instead of assuming it**. Full cost
model, with the measured table: `docs/coordination/graphql-budget.md`.

**Adding an item to the board is `fsgg-coord add`** — not `gh project item-add`. It is idempotent, it
caches the item id (so the `set-field` that always follows pays nothing), and it refuses to add on a
*failed* lookup rather than creating a duplicate (#421). Reaching past the client puts an unmetered
principal on a budget the whole fleet shares (#587).

```sh
scripts/fsgg-coord add FS-GG/<repo>#<n>              # put it on the board
scripts/fsgg-coord set-field <n> Status Backlog      # ...then sequence it
# `ready` / `next` read the board. NEVER `gh project item-list` — 6 pts to read FIVE items.
```

**One-time board PROVISIONING** is the genuine exception: a human runs it once, with admin rights, and
no worker ever executes it. View layout/grouping, sub-issue links, issue-type assignment, and built-in
workflows are UI-driven.

<!-- graphql-monopoly: exempt — one-time board provisioning, run once by a human with admin rights; never on a worker path -->
```sh
gh auth refresh -s project,read:project                       # token needs project scope
P=$(gh project create --owner FS-GG --title "Coordination" --format json --jq '.number')
gh project field-create $P --owner FS-GG --name "Phase" --data-type SINGLE_SELECT \
  --single-select-options "P0 Decisions,P1 Rendering,P2 SDD,P3 Governance,P4 Templates,P5 Versioning,P6 Game,P7 Audio"
# ...Repo Scope / Workstream / Effort single-selects; Start / Target dates; Contract / Blocked by text
gh project item-create $P --owner FS-GG --title "<draft item>" --body "<acceptance criteria>"
```

> **Adding an option to an existing single-select is destructive — prefer the UI.** There is no
> additive API: `field-create` only makes new fields, so the only scriptable route is the GraphQL
> `updateProjectV2Field` mutation with a full `singleSelectOptions:[...]` list. Its option input
> carries **no id**, so resending the existing options verbatim does not preserve them — GitHub
> **recreates every option with a fresh id and clears the field on every item**. Adding `P6 Game` /
> `P7 Audio` this way ([#303](https://github.com/FS-GG/.github/issues/303)) blanked `Phase` on all
> **374** items that had it set. If you must script it: snapshot `itemId → option name` for every
> item first, mutate, then restore with one `updateProjectV2ItemFieldValue` per item against the
> **new** option ids, and diff the re-read against the snapshot before you trust the board. Any
> `fsgg-coord` cache still holds the dead ids — `bootstrap --refresh` before the next `set-field`.

**Manual steps (need org-admin in the UI, not the `project` scope):**

- **`Epic` issue type** — add it in org Settings → Planning → Issue types so each Phase is one
  epic with child `Task`/`Bug`/`Feature` issues and automatic rollup. Creating issue types
  needs `admin:org`; a read-only token cannot. (Alternatively model an epic as a `Feature`
  issue used as the sub-issue parent.)
- **Issue-fields pinned to `Epic`** — a custom issue type starts with **no** pinned fields
  (the default Priority/Effort/dates pin only to built-in types). Pin any desired fields to
  `Epic` in Settings → Planning → Issue fields. This is separate from the board's Projects v2
  fields above.
- **Roadmap view, per-view grouping** (e.g. group by `Phase`, zoom Quarter), **sub-issue
  links**, and **built-in workflows** (auto-add — prefer label-scoped, e.g. a `roadmap`
  label; item closed → `Status: Done`).

## Signal an item is finished (the done-stamp)

An item is **finished** only when **both** are true: its closing PR is **merged** *and* its board
`Status` is **`Done`**. Neither alone counts — a merged PR whose card never flipped is still open
work on the roadmap, and a card dragged to `Done` with no merged PR is a lie. Don't *assert* done
by printing a success line; **earn** it — have the tool confirm both facts first:

```sh
scripts/fsgg-coord done <issue>            # verify BOTH; green FSGG-DONE stamp (exit 0) or
                                           # red FSGG-NOT-DONE stamp naming the failing check (exit 1)
scripts/fsgg-coord done <issue> --flip     # once the PR is merged, also set Status=Done, then stamp
scripts/fsgg-coord done <issue> --pr <N>   # name the closing PR explicitly (else the first merged closer)
```

- The **green** two-line stamp prints **only** after the merge and `Status: Done` are both confirmed
  in one thrifty query (merge via `closedByPullRequestsReferences`, board state via the `Status`
  resolver). Any failing check prints a **red** counter-stamp saying *which* check failed and exits
  non-zero — so a half-finished item can't masquerade as "still working" by printing nothing.
- Both stamps carry a greppable sentinel — **`FSGG-DONE`** / **`FSGG-NOT-DONE`** — so scrollback,
  CI logs, and transcripts are searchable, not just eyeball-able by colour (which degrades to plain
  text when output isn't a TTY or `NO_COLOR` is set).
- **Close out an item with `done <issue> --flip`** — it merges the "flip the card" and "confirm it's
  really merged" steps into one earned signal. Treat a green stamp as the definition of finished; a
  red stamp is a to-do, not a done.
- **Epics roll up automatically.** When `--flip` takes the **last** child of an epic to `Done`, the
  command walks the parent chain **upward** and flips each parent epic whose children are now *all*
  board-`Done`, stamping each one that rolls up (`… (epic)`) — transitively, so a grandparent closes
  too. An epic that isn't complete yet prints a one-line `N/M children Done — holding` note instead
  of flipping. So you never hand-flip a Phase epic: finish its last child and the epic stamps itself.

## Keep the registry coherent (required for contract changes)

Before changing a versioned cross-repo contract, read
`FS-GG/.github` → `registry/dependencies.yml`. Any `contract-change` issue MUST update
that registry (and its `docs/registry/compatibility.md` projection) as part of its
resolution — including flipping a `coherence` entry and linking its `tracking` issue.
Record larger cross-repo decisions as an ADR under `docs/adr/`.

> **In flight (per the 2026-06-27 architecture report):** composition is moving from the
> vendored `fs-gg-fullstack` monolith to **CLI-orchestrated** scaffold (`fsgg-sdd scaffold
> --provider rendering`). Expected registry deltas: `fs-gg-ui-template` gains a `lifecycle`
> parameter (additive); the `templates → rendering` *"vendors"* edge is **retired** in favor
> of install-at-scaffold-time; the governance overlay moves empty-stub → **populated** gate
> set. These land as `contract-change`s + a new ADR, sequenced as P0–P5 on the board.

## Release a coherent set (publish-before-flip)

Delivering a versioned package to consumers is a **two-actor, ordered** dance — get the order
wrong and the registry advertises a version the feed can't serve. The universal rule is
**publish-before-flip (FR-007): the package is LIVE on the feed before the registry says so.**

1. **Publish.** The producing repo bumps its version-of-truth, cuts the release (its own
   `release.yml` / tag flow), and pushes the package(s) to the org feed
   (`nuget.pkg.github.com/FS-GG`). The producing repo's **release-only gates** (e.g. package
   consumption + generated-product tests) must be green — they fail-closed and SKIP the push,
   which is the safety net, not a nuisance. Confirm `"Your package was pushed"` before step 2.
2. **Flip the registry** (only now): in `FS-GG/.github` update `registry/dependencies.yml` — the
   contract entry's `version` / `package-version` / `package-tag`, the **consuming edge**
   (`{ from: <consumer>, to: <producer>, via: "<id>@<V> …" }`), and set the top-level
   `updated:` date. **Prepend one dated entry** (newest-first) to the registry changelog
   `registry/CHANGELOG.md` — `- **YYYY-MM-DD** — HEADER (owner; refs): body` — one entry
   per change, so diffs stay reviewable (the old single-line `updated:` comment is retired,
   .github#129). Then update the **hand-maintained** projection
   `docs/registry/compatibility.md` (dependency-graph line + versioned-contracts row + the
   relevant coherence row). Finally reconcile the **architecture map**
   `docs/architecture.md` — a registry flip touches `registry/dependencies.yml`, which is
   exactly the `architecture-map.yml` reconcile trigger. A routine version bump does not
   change the map's shape: take the opt-out, a one-line
   `architecture-map: unaffected` in the PR body (or the
   `architecture-map:unaffected` label). A flip that moves a coherent-set axis or the map's
   §5 contract picture updates the map instead.

   Validate with the typed validator — the SAME one the gate runs:
   `fsgg-sdd registry validate registry/dependencies.yml` → `"valid": true`. **A green
   validator is not a green PR:** neither it nor `check-feed-coherence` sees
   `docs/registry/compatibility.md` or `docs/architecture.md` — those are gated by
   `projection` and `architecture-map` respectively, and only in CI. Open the PR; the
   `contract-coherence` check must pass. The PR may `Closes <producer>#<n>`.
3. **Re-pin downstream.** Publishing makes the feed coherent, but a consumer only *receives* the
   new version when its pin moves. **Direct-pin** consumers self-bump (one version literal +
   restore). **Provider/composition-pinned** consumers need a re-pin PR in the consuming repo
   (e.g. `FS.GG.Templates` → `providers/<provider>.providers.yml` `source: <PkgId>::<V>`); its
   own CI (e.g. `composition`) must pass.
4. **Land + record.** Merge both PRs, confirm the producer issue closed, then close out each board
   item with `scripts/fsgg-coord done <issue> --flip` (see *Signal an item is finished*) — it flips
   `Status: Done` only after re-confirming the merge, the green stamp is your proof, and the parent
   epic rolls up to `Done` on its own once the last child stamps green.

> **Worked example — Rendering's `fs-gg-ui` coherent set.** Bump the two version-of-truth files
> (`template/base/Directory.Packages.props` `<FsGgUiVersion>` + `.template.package/FS.GG.UI.Template.fsproj`
> `<Version>`); cut the **tag triple** `v<V>` + `fs-gg-ui/v<V>` + `fs-gg-ui-template/v<V>` (only `v*`
> triggers `release.yml`; push the two snapshot tags first); the publish job `needs:` the two gate
> jobs. Then flip `fs-gg-ui-template` in the registry and re-pin `FS.GG.Templates`
> `providers/rendering.providers.yml`. The producing repo holds the full step-by-step — including
> the repo-local test-matrix rows a newly shipped item needs (a release-only gate, not the push
> gate, catches a missed row).

## Labels

`cross-repo`, `cross-repo:request`, `cross-repo:response`, `blocked`, `contract-change`.
Apply them to a repo with `FS-GG/.github` → `scripts/apply-labels.sh`.

## Setup notes

- Org Project tracking needs the `project` scope: `gh auth refresh -s project,read:project`.
  Adding the `Epic` issue type needs `admin:org` (or do it in the UI).
- To make this skill active in a product repo, copy it into that repo's
  `.claude/skills/cross-repo-coordination/`.
