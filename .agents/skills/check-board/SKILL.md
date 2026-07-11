---
name: check-board
description: Reconcile the org-level FS-GG "Coordination" Projects v2 board against the repos' real issue state, and re-verify that every recorded blocker still holds. Use when the board looks wrong or stale, before a planning pass, before fanning workers out with intra-repo-parallel-work, or when `next`/`take` says "nothing to do" and you doubt it. Reports every discrepancy; with --apply it fixes the board-side ones. Never edits an issue. Canonical protocol lives in FS-GG/.github.
---

# check-board (FS-GG)

The Coordination board is a **projection**. GitHub issues, their `state`, and the `fsgg:claim`
markers are the **ground truth**; the board's `Status`, `Phase`, and `Blocked by` are a cached
view of it that humans and `fsgg-coord next`/`take` read to decide what happens next. Every write
to the board is best-effort — `claim` sets `Status: In progress` in a subshell that *swallows a
Projects v2 5xx*, and `done --flip` flips `Status` only after it sees a merged PR. So the
projection drifts, silently, and a drifted board hands out work that is already done, hides work
that is startable, and keeps items "blocked" behind issues that closed weeks ago.

This skill is the **reconcile pass** over that drift. It answers two questions:

1. **Is the board in sync with the issues?** (and if not, fix the board — never the issue)
2. **Do the recorded blockers still hold?** (a `Blocked by` edge is a claim about the *present*)

Related: [cross-repo-coordination](../cross-repo-coordination/SKILL.md) owns the protocol the board
implements; [intra-repo-parallel-work](../intra-repo-parallel-work/SKILL.md) owns claims and
touch-sets. This skill only *reconciles*. Decisions: ADR-0001 (the board), ADR-0027 (the claim).

## The one rule: the issue is the truth, the board is the copy

**Fixes only ever write to the board.** Auto-remediation may set `Status`, add a missing item to
the board, or release an expired claim. It may **never** close or reopen an issue, delete a
`Blocked by` edge, merge a PR, or touch a working tree. Those need judgement, so they are
**reported and left for a human**. When ground truth and the projection disagree, the projection
is what changes.

Corollary: an issue is not "done" because the board says `Done`. `fsgg-coord done` exists precisely
to make the stamp *earned* (PR merged **and** `Status: Done`), and this skill never fakes it.

## Run it

```sh
scripts/fsgg-coord budget                      # free; are we near the GraphQL cap?
/check-board                                   # DRY RUN — report every finding, write nothing
/check-board --apply                           # ...and fix the board-side ones
/check-board --repo rendering                  # scope to one repo (registry short-id)
```

Dry run is the default, as it is for `reap` and `coordination-sync --check`. Read the findings
before you let anything write.

## 1. Snapshot (three reads, whole board)

Take the snapshot **once** and classify from it. Do not re-query per item — `ready` is a full-board
GraphQL scan, and the whole point of `fsgg-coord` is that it costs ~3 points instead of ~2,500.

```sh
scripts/fsgg-coord ready --all --json > /tmp/board.json    # EVERY item, incl. Done (--all widens past 'not Done')
scripts/fsgg-coord lint --json          > /tmp/lint.json    # epic invariants + the Done/open note
scripts/fsgg-coord who  --repo <r> --json > /tmp/who.json   # live claims, per repo
```

`ready --all --json` yields one object per item, already annotated:

```json
{ "repo": "FS-GG/FS.GG.Rendering", "number": 186, "title": "...", "state": "OPEN",
  "type": "Issue", "status": "Blocked", "phase": "P1 Rendering",
  "blockedBy": "FS-GG/FS.GG.SDD#8, #33",
  "blockers": [ { "ref": "FS-GG/FS.GG.SDD#8", "state": "CLOSED" },
                { "ref": "FS-GG/FS.GG.Rendering#33", "state": "OPEN" } ],
  "blocked": true }
```

**Know where `blockers[].state` comes from, or you will misread every blocker finding.**
`board_annotate` resolves each ref against an index built from **the board's own items only**. A
blocker that was never added to the board is therefore `UNKNOWN` — *not* `CLOSED` — and
`blocked: any(.state != "CLOSED")` makes the item **blocked forever**. That is a real, common,
invisible failure: `next` skips the item citing a blocker nobody can see. Resolve every `UNKNOWN`
over REST before you believe it (§3).

## 2. The findings

Each finding has a code, a ground truth, and a fix — or an explicit refusal to fix.

| Code | Condition | Fix (`--apply`) |
|---|---|---|
| `CLOSED-ISSUE-NOT-DONE` | `state == CLOSED` and `status != Done` | `set-field <i> Status Done` |
| `DONE-STATUS-OPEN-ISSUE` | `status == Done` and `state == OPEN` | **report only** — is the work done, or was the flip premature? |
| `OFF-BOARD-ISSUE` | open `roadmap` issue in a rostered repo with no board item | `gh project item-add` |
| `BLOCKER-CLEARED` | every blocker `CLOSED`, but `status == Blocked` | `set-field <i> Status Ready` |
| `BLOCKER-UNKNOWN` | a blocker ref is not on the board | resolve over REST, then `item-add` the blocker if it is open |
| `BLOCKER-UNPARSEABLE` | a `Blocked by` token is not an issue ref | **report only** — hand-fix the field |
| `STATUS-NOT-BLOCKED` | an open blocker, but `status` is `Ready`/`Backlog` | `set-field <i> Status Blocked` |
| `STALE-CLAIM` | `who` says `state == "stale"` | `reap --repo <r> --apply` |
| `UNCLAIMED-IN-PROGRESS` | `who` says `state == "unclaimed"` | **report only** — someone is working outside the protocol |
| `CLAIM-STATUS-LAG` | held claim, but board `status != In progress` | `set-field <i> Status 'In progress'` |
| `UNDECLARED-PATHS` | open, unclaimed, not `Done`, and the issue body declares no `Paths:` | **report only** — the fix is an *issue* edit, and this skill never writes to an issue |
| `EPIC-*` | from `lint --json` (severity `error`) | **report only** — a broken epic needs a human |

`CLAIM-STATUS-LAG` is the one class you cannot read off a single command: `who --json` does not
emit `inProgress`. Join it yourself — an item `who` reports as `held` whose board `status` is not
`In progress` is a lock whose best-effort `Status` write was swallowed:

```sh
jq -r --slurpfile b /tmp/board.json '
  ($b[0] | map({ key: "\(.repo)#\(.number)", value: .status }) | from_entries) as $st
  | .[] | select(.state == "held")
  | select($st["\(.repo)#\(.number)"] != "In progress")
  | "CLAIM-STATUS-LAG  \(.repo)#\(.number)  held by \(.worker), board says \($st["\(.repo)#\(.number)"] // "—")"
' /tmp/who.json
```

`UNDECLARED-PATHS` is the class that makes a **full queue look like an empty one**. An item with no
`Paths:` cannot be scheduled — `take`/`batch` refuse it, because an undeclared touch-set cannot be
proven disjoint from another worker's — so it sits on the board *looking* startable while every
worker who asks for work is told there is none. `.github` reached **twelve** such items at once, all
filed through the org's own recipe, and `/pnext-item` reported a dead queue over a full one
([#442](https://github.com/FS-GG/.github/issues/442)). The board is not wrong here — the *issue* is —
which is exactly why this is report-only: the remedy is an issue-body edit, and **this skill never
writes to an issue** (§*The one rule*). Report it, and let the claimant `claim` then `widen`, which
is the ordering ADR-0021 requires anyway (the marker is the CAS; the body is not).

The touch-set lives in the issue body, which the REST issue list already carries — so this costs
**no extra call**:

```sh
scripts/fsgg-coord issues <r> --jq '.[] | select((.body // "") | test("(?m)^Paths:") | not)
  | "UNDECLARED-PATHS  #\(.number)  \(.title)"'
```

`DONE-STATUS-OPEN-ISSUE` is deliberately **not** auto-fixed, in either direction. `lint` calls it a
*note* rather than an error for the same reason: `Done` over an open issue is how a premature flip
looks, **and** how "merged, issue left open for the release note" looks. Closing the issue and
reverting the status are both destructive, and only a human knows which happened.

## 3. Re-verify the blockers (this is the half people skip)

A `Blocked by` edge is a claim about *now*, and nothing re-checks it when the blocker closes. Items
rot in `Blocked` behind issues that shipped. For every item with `blockers`:

```sh
jq -r '.[] | select(.blockers | length > 0)
  | "\(.repo)#\(.number)  [\(.status)]  " + ([.blockers[] | "\(.ref)=\(.state)"] | join(" "))' /tmp/board.json
```

Then, per blocker state:

- **`CLOSED`** — does not hold. If *every* blocker is closed, the item is startable: flip
  `Blocked` → `Ready`. **Leave the `Blocked by` field alone.** It is provenance, and a closed ref
  costs nothing — `board_annotate` and `next` both ignore it. Deleting the edge destroys history to
  fix a `Status` that was the actual problem.
- **`OPEN`** — holds. If the item's `status` is not `Blocked`, that is `STATUS-NOT-BLOCKED`; the
  board is lying to a human reading the column (`next` skips it correctly either way).
- **`UNKNOWN`** — *the board cannot see the blocker.* Do not trust it. Resolve it:

  ```sh
  gh issue view <owner/repo#n> --json state,title,url -q '.state'
  ```

  `CLOSED` → treat as `BLOCKER-CLEARED`. `OPEN` → the blocker is genuine but **off-board**, so
  `next` will refuse this item forever and never say why in a way you can act on. Fix the *cause*:
  add the blocker to the board (`gh project item-add`), which turns `UNKNOWN` into `OPEN`.
- **`UNPARSEABLE`** — prose or a placeholder leaked into the field before it was validated. Report
  it with the offending token. A human re-writes the field with refs, or clears it:
  `fsgg-coord set-field <i> 'Blocked by' ''`.

## 4. Apply, in this order

Order matters — later steps read state the earlier ones changed.

```sh
scripts/fsgg-coord reap --repo <r>              # 1. dry run: whose lease expired?
scripts/fsgg-coord reap --repo <r> --apply      #    release them (tells the reaped worker)
gh project item-add <n> --owner FS-GG --url <url>   # 2. off-board issues + off-board blockers
scripts/fsgg-coord set-field <i> Status <V>     # 3. the status flips
scripts/fsgg-coord ready --all --json           # 4. RE-READ: adding items changed the blocker index
```

Step 4 is not optional. Adding a blocker to the board in step 2 re-resolves it from `UNKNOWN` to
`OPEN`/`CLOSED`, which can create or clear a `BLOCKER-CLEARED` finding that did not exist in your
snapshot. Reclassify from the fresh scan, then apply any newly-earned status flips.

## 5. Confirm, and report what you did not touch

```sh
scripts/fsgg-coord lint --repo <r>       # exit 0
scripts/fsgg-coord ready --repo <r>      # the board a human will now read
scripts/fsgg-coord budget                # what the pass cost
```

Finish with a summary that separates **fixed** from **left alone**. The report-only classes
(`DONE-STATUS-OPEN-ISSUE`, `UNCLAIMED-IN-PROGRESS`, `BLOCKER-UNPARSEABLE`, `EPIC-*`) are the ones a
human must act on, and a pass that quietly "succeeded" while leaving four of them standing is how
they survive to the next pass. Name them, with their issue refs.

**Never report a silent cap.** If you scoped to one repo, or stopped after N fixes, say so — a
partial reconcile that reads as a full one is worse than no reconcile, because it buys false
confidence in the projection.

## Setup

- Board writes need `gh auth refresh -s project,read:project`; `reap`/`release` need `issues: write`.
- `--repo` takes a registry short-id (`sdd`, `rendering`, `governance`, `templates`, `game`,
  `audio`, `.github`), `owner/repo`, or a bare repo name. The roster is `registry/repos.yml`;
  `scripts/repos.sh list` enumerates it.
- Unscoped, this is a whole-org pass over every board item — one full scan, plus one `who` per repo.
