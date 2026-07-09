---
name: cross-repo-coordination
description: Coordinate work across the FS-GG repos (FS.GG.SDD, FS.GG.Rendering, FS.GG.Governance, FS.GG.Templates). Use when you need something from another FS-GG repo, are changing a versioned cross-repo contract, hit a cross-repo version/API incoherence, or need to place/sequence work on the org-level "Coordination" Projects v2 roadmap. File and answer requests as GitHub issues, track sequencing on the Coordination board, keep the contract/compatibility registry coherent, and record cross-repo decisions as ADRs. Canonical protocol lives in FS-GG/.github.
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

```sh
gh issue create --repo FS-GG/<target> \
  --title "[cross-repo] <short summary>" \
  --label cross-repo --label cross-repo:request [--label blocked] \
  --body "From: <your repo>. Blocks: <ref>. Contract: <id>. <what you need and why>"
```

## Respond / resolve

- **Respond:** comment on the issue, starting with `## Response` (or link a PR/issue).
  The target repo's agent/maintainer owns the response.
- **Resolve:** close the issue (ideally via a linked PR). The requester confirms.

```sh
gh issue list    --repo FS-GG/<repo> --label cross-repo
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
| `Phase` | single-select | `P0 Decisions`, `P1 Rendering`, `P2 SDD`, `P3 Governance`, `P4 Templates`, `P5 Versioning` |
| `Repo` | single-select | `rendering`, `sdd`, `governance`, `templates`, `.github`, `cross-repo` |
| `Workstream` | single-select | `Composition`, `Lifecycle`, `Governance`, `Versioning`, `Docs`, `Coordination` |
| `Start` / `Target` | date | feed the Roadmap (timeline) view |
| `Effort` | single-select | `S`, `M`, `L`, `XL` |
| `Contract` | text | registry id the item touches (e.g. `fs-gg-ui-template`) |
| `Blocked by` | text | what blocks THIS item — comma-separated issue refs; Projects has no typed dependency field |

**Conventions** (also in the board README):

- **One roadmap item = one issue** in its owning repo. Cross-repo asks additionally carry
  `cross-repo`/`cross-repo:request` and live in the *target* repo.
- Set `Phase`, `Repo`, `Workstream`, `Target`, and (for cross-repo work) `Contract` on every
  item. `Blocked` status mirrors the `blocked` label.
- **`Blocked by` records the dependency edge, nothing else.** `fsgg-coord set-field` takes a
  comma-separated list of issue refs (`owner/repo#n`, `repo#n`, `#n`, or an issue URL) and
  canonicalizes each to `owner/repo#n`; anything else is refused before the write. It is not a
  delivery log and not the inverse (`blocks X`) edge — narrative goes in an issue comment, "this
  item is blocked" goes in `Status`. Clear it (`''`) when the blocker resolves.
  `fsgg-coord next` reads it: an item whose blockers are still open — or whose blocker it cannot
  see on the board — is skipped, with the reason on stderr (`--ignore-blocked` overrides).
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

**`gh` runbook** (the board, fields, and items are scriptable; view layout/grouping,
sub-issue links, issue-type assignment, and built-in workflows are UI/REST-driven):

```sh
gh auth refresh -s project,read:project                       # token needs project scope
P=$(gh project create --owner FS-GG --title "Coordination" --format json --jq '.number')
gh project field-create $P --owner FS-GG --name "Phase" --data-type SINGLE_SELECT \
  --single-select-options "P0 Decisions,P1 Rendering,P2 SDD,P3 Governance,P4 Templates,P5 Versioning"
# ...Repo / Workstream / Effort single-selects; Start / Target dates; Contract / Blocked by text
gh project item-add    $P --owner FS-GG --url https://github.com/FS-GG/<repo>/issues/<n>
gh project item-create $P --owner FS-GG --title "<draft item>" --body "<acceptance criteria>"
# set fields (prefer the thrifty client): scripts/fsgg-coord set-field <issue> <Field> <Value>
# raw form: gh project item-edit --id <itemId> --field-id <fid> --single-select-option-id <oid> ...
# pick the next item to work (thrifty; do NOT use raw `gh project item-list` for this):
scripts/fsgg-coord next  --repo .github            # the one most-startable item (Ready, else Backlog)
scripts/fsgg-coord ready --repo .github            # all actionable items (not Done) for a repo
```

> **Keep GraphQL cheap — route board work through `scripts/fsgg-coord`.** Projects v2 is
> GraphQL-only and the primary rate limit (5,000 pts/hr) is metered by *nodes requested*, not by
> request count — so batching buys nothing; the wins are not re-fetching static ids, narrow item
> lookups, and putting plain issue reads on REST. `fsgg-coord` does all three: `bootstrap` caches
> the project/field/option ids **once**; `set-field <issue> <Field> <Value>` resolves every id from
> cache and auto-routes by the field's dataType (one mutation, no introspection); `item-id` resolves
> via `issue → projectItems` (not a whole-board scan); `issues <repo> --label …` reads over REST with
> an ETag (304s cost nothing); and `next`/`ready` answer "**what do I pick up next?**" by scanning the
> board reading Status/Phase through the `fieldValueByName` **resolver** field (no node
> multiplication), ~1 point per 100-item page — whereas raw `gh project item-list` nests
> `fieldValues(first:100)` inside `items(first:N)` for **O(items × 100) ≈ 2,500 pts**, so a few calls
> exhaust the budget. **Use `next`/`ready` for that, never `item-list`.** Watch the meters with
> `fsgg-coord budget` (and `FSGG_COORD_DEBUG=1`
> logs each call's cost). Full cost model: `docs/coordination/graphql-budget.md`.

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
   relevant coherence row). Validate with the typed validator — the SAME one the gate runs:
   `fsgg-sdd registry validate registry/dependencies.yml` → `"valid": true`. Open the PR; the
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
