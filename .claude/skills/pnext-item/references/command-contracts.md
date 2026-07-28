# Command contracts

<!-- BEGIN GENERATED: fsgg-protocol:driver-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  This recipe restated these rules by hand for its whole life, and #1059 counted the cost: five
  repairs to four sentences, every one of them the PROSE moving to where the engine already was.
  The two that were closed by generating a table have never drifted again. Two hand-written
  copies also disagreed OUT LOUD — §3 said an expired lease cannot be renewed and §6 said
  `heartbeat` renews it — and nothing could see it, because neither was the engine's answer.
  Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that takes your claim, holds your lease and refuses
your `Paths:` line is the engine that wrote this. The full rule set, with the incident behind
each one, is in [intra-repo-parallel-work](../../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**The touch-set grammar — it is NOT a glob language**

supported: an exact path ('src/Foo.fs'), or a directory prefix ('src/Foo', 'src/Foo/*', 'src/Foo/**'). There is no glob matcher: a leading '**/' or an interior '*' matches nothing — spell the paths out.

**The claim lock is a comment-order CAS, and the ASSIGNEE cannot hold it**

A claim is an `fsgg:claim` marker COMMENT, and the lowest live marker id wins. GitHub issues comment ids from one server-side sequence, so "lowest live marker" is a total order every racer observes identically. The GitHub ASSIGNEE cannot be the lock, because N agents share one account. That total order is over MARKERS, and it separates WORKERS only while their ids are DISTINCT: an id two workers share is an id this lock cannot separate, and `release`, `heartbeat`, `say` and `inbox` then act on one another's claims. So a worker id is MINTED, never chosen — a worker asked to pick one is not a random source.

**The lease is a WINDOW, and an unknown age says so**

A claim's lease is 120 minutes by default (`FSGG_CLAIM_LEASE_MIN`), and `heartbeat` renews it only while it is LIVE. Past it the claim is REAPABLE — not free: only `reap` may break a lock, and an item's touch-set stays reserved until it does. An EXPIRED lease cannot be renewed in place; the holder must re-claim. Evidence that the work is alive — an open `item/<n>-*` PR — withholds the item from `take` and REFUSES a `reap`, but it does not revive the lease. A claim whose age cannot be read reports `lease unknown`, never a window.

<!-- END GENERATED: fsgg-protocol:driver-rules -->

## `take`

<!-- BEGIN GENERATED: fsgg-protocol:take-exit-codes -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy of this table was WRONG for as long as it existed (#889): it documented
  EX_PARTIAL — a write that half-landed — as `take` failing to READ the board, and its "≠0, ≠2"
  row swallowed every other row in the table. Edit Protocol.fs and regenerate.
-->

| exit | meaning | what to do |
|---|---|---|
| **0** | An item was CLAIMED. This is the ONLY code that means you hold one. | Go work it — and only here. |
| **5** (`EX_NONE`) | Looked, and nothing was startable — an empty or all-blocked queue. A LOOK THAT SUCCEEDED and found nothing, which is why it is not 0 and not a read failure. | Nothing to do: stop, or wait for the board to free up. Diagnose before you idle — `batch --include-backlog`, `who`, `next` each name a different reason a full board looks empty. |
| **6** (`EX_CONTENDED`) | The item was startable when it was picked and the claim CAS lost every race for it — somebody else got there first. | Back off briefly and retry. The board is busy, not empty. |
| **75** (`EX_RATE`) | A rate budget is exhausted. The message names WHICH one (#897): REST takes `claim`/`take`/`who` with it, because the lock lives there (ADR-0034 §3); GraphQL takes the board reads. When it is REST, the fleet STANDING DOWN is the designed behaviour, not an outage (#976): answering "is this item takeable?" costs the very budget that is gone, and a lock you cannot verify is not a lock. So this is a stop, and it is meant to be. | Back off until the reset it names — do not loop. Then `flush --dry-run`: a board write you made on an exhausted budget is QUEUED, and nothing replays it for you. AND IF YOU ARE HOLDING AN ITEM, `heartbeat` is REST too — an outage that outlives your lease cannot be renewed through, and the moment REST returns your item is startable again and the next `take` hands it to somebody else. Two things save you and neither is the timer: an OPEN `item/<n>-*` PR (#581 — the lease lapsed, the work did not), or a liveness probe that itself fails (which fails closed, #266). Push the branch and open the PR EARLY: it is the only proof of life that does not depend on the budget you just lost. |
| **3** | REFUSED — the batch cannot be scheduled at all. Some in-flight claim declares a touch-set that matches no file, so it reserves NOTHING, and scheduling against it would hand its files to a second worker. The message names the item and the offending tokens. | Do NOT retry — it will refuse identically until the declaration is fixed. Fix the claim it names (`widen <issue> --paths '<paths>'`), or talk to its holder. |
| **1** | No verdict was reached, for one of two reasons the message tells apart: the engine refused your INPUT before it looked (no worker id resolves; the board document does not parse), or the board READ failed. A read failure is never an empty queue and never EX_NONE (#266) — "I could not look" and "I looked, and it is empty" keep different codes on purpose. | Read the message. A refused input is not retryable — it names its own remedy. Retry only a read failure, and investigate one that persists. |
| **2** | The ENGINE broke — an unhandled defect, with a stack trace. Its own code, so a broken engine cannot hide behind a stream of what look like bad inputs. | Report it. Do not retry, and do not work an item you were not handed. |

<!-- END GENERATED: fsgg-protocol:take-exit-codes -->

**`next` is a diagnostic that WRITES — reach for `batch` when you only want the answer**

The EX_NONE row above sends you to `next`, and `next` is not a read (.github#1535, DECIDED). After printing its answer it makes the #733 chore OFFER, which POSTs a claim marker TAKING this repo's chore lock (`.github#1033`, ADR-0041). That is deliberate rather than a bug: the tool has no thread of its own, so it conscripts an idle caller to reconcile the board, and `next` is the ONLY boundary that fires on a board with nothing left to schedule — `done --flip`, the other one, needs somebody to have finished an item, which on a wedged board nobody has. It is also why a STALE engine REFUSES `next` (#1528): the shim classifies it in `BOARD_WRITES_CONDITIONAL`, and a stale board write corrupts what the whole fleet shares.

So when you want the DECISION without the side effect, run **`batch --text -n 1`** (add `--include-backlog` to see past the column). `batch` is `next` uncapped: it makes no offer, takes no lock, and sits in the shim's `BOARD_READS`, so it still answers on a stale engine. `--text` is not optional in that spelling — `batch` defaults to **JSON**, where a drained board prints `[]` and the reasons go to stderr. With `--text` it prints exactly what `next` prints minus the offer: the same "nothing schedulable right now.", the same per-item passed-over reasons, the same starved-queue banner — the two verbs render them through one shared helper, so they cannot drift apart. `tests/coord-engine-e2e/writes.sh` pins both halves against one board and one chore: `batch` leaves the chore lock's comment thread empty, `next` posts a marker naming its worker to it.

## `landable`

Poll `scripts/fsgg-coord landable <pr> --repo <repo> --wait --sha <head>`; do not parse prose.

<!-- BEGIN GENERATED: fsgg-protocol:landable-exit-codes -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy of this table documented BASH's codes (#900) — green 0, pending 3, red 1
  — where the engine returns 0/7/3/4 and keeps 3 == red across every verdict command. It was
  wrong in BOTH directions on the two codes a poll loop reads. Edit Protocol.fs and regenerate.
-->

| exit | meaning | what to do |
|---|---|---|
| **0** | GREEN — the PR is finished work: it merges cleanly, and every workflow run and check-run scored on its head SHA passed. The ONLY code that means merge it. | Merge it. This is the only code that says so. |
| **7** | PENDING — the verdict has not SETTLED: checks are still running, none have registered yet, the run set is still growing, GitHub has not finished computing the PR's mergeability (it does so in a BACKGROUND job, and `null` is the normal first answer for a PR you just opened — #950), GITHUB ITSELF SAYS IT WILL REFUSE THIS MERGE — its `mergeable_state` for the gated head is `blocked` (the base branch policy is not satisfied: a required context that failed, or that has NO CHECK RUN AT ALL because the workflow producing it is not on this branch, or a required review, or an unresolved conversation), `behind` (a strict base moved) or `draft` (#1575) — or an assertion you added (`--require`, `--sha`) is not yet met. The ONE retryable verdict, which is why it has a code of its own rather than sharing one with a way to stop. | Keep waiting — this is the only code that says wait. Prefer `--wait`, which polls until the verdict settles rather than believing an early green. A `pending` that NEVER resolves is a finding, and the remedies are OPPOSITE, so read which one you were handed. GitHub refused the merge (`blocked`/`behind`/`draft`, and the line names the state and the base branch): REBASE onto the current base so the required workflow exists on this head and can fire; mark a DRAFT ready for review; or stop requiring a context nothing on this branch can produce. WAITING CANNOT CREATE A CHECK RUN FOR A WORKFLOW THAT DOES NOT EXIST ON THIS BRANCH — no amount of polling makes an absent producer report, which is why this arm names the context rather than telling you to wait harder. Otherwise: the job was RENAMED, its workflow's `paths:` filter no longer matches, `--sha` named the wrong commit, or GitHub never finished computing mergeability (rare, and not something waiting longer fixes — read the PR yourself). |
| **3** | RED or CONFLICTED — two words, one code, because both mean STOP and neither improves by waiting. Red: a run or check-run failed. Conflicted: the PR does not merge cleanly, so GitHub cannot build `refs/pull/N/merge` and gives it NO CI at all — which is why it is returned immediately rather than polled. | Stop. Do NOT wait — 3 is the code the recipe used to call `pending`, and a loop that waits on it never terminates. A red check is a finding; a conflicted PR needs a rebase, which is AUTHORING, not landing. |
| **4** | UNKNOWN — no verdict, and this is the FAIL-CLOSED one (#266). The read could not be made or its answer was not conclusive: a rate limit, a 404, a PR whose `mergeable` field is ABSENT entirely. Note what it is NOT. An UNREADABLE BRANCH POLICY is NOT one of these causes (#1575): when GitHub says it will refuse the merge, the refusal stands on GitHub's OWN word (`mergeable_state`, already in the PR body) and the policy read is DIAGNOSIS ONLY — a 403 or a rate limit on it costs you the SENTENCE naming which context is unmet, never the verdict, which stays PENDING (7). A gate that failed closed on a read the fleet's token cannot make would not fail closed; it would fail ALWAYS (#463). A `mergeable` GitHub has not computed YET is PENDING (7), not this — it is guaranteed to change, and calling it unknown made `--wait` settle at once and abandon a seconds-old PR (#950). And there is no EX_RATE (75) here, unlike `take`: an exhausted budget arrives as this code, because `landable` has no error channel to carry a budget on. | Do not merge, and do not treat it as a red. An unreachable answer is not a negative one. Look at why the read failed — check `budget` if you suspect a rate limit — and ask again. |
| **1** | REFUSED — the engine rejected your INPUT before it ever looked at the PR: no `--repo` (so which repo the PR is in is undefined), a ref that is not a PR number, or the wrong number of arguments. It is not a verdict about the PR, and no word is printed. | Read the message and fix the call. Not retryable — it will refuse identically. |
| **2** | The ENGINE broke — an unhandled defect, with a stack trace. Its own code, so a broken engine cannot hide behind a stream of what look like bad inputs. | Report it. Do not retry, and do not merge a PR you have no verdict on. |

<!-- END GENERATED: fsgg-protocol:landable-exit-codes -->

Exit 75 is budget exhaustion: back off until reset. Inspect `budget --json`; if
`pendingBoardWrites > 0`, run `flush` after budget returns and verify the writes.
