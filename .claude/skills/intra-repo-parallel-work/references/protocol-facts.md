# Generated parallel-work protocol

<!-- BEGIN GENERATED: fsgg-protocol -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  This region exists because a rule stated in six documents is a rule that will disagree with
  itself — #485 (startability computed in five places, agreeing in none) and the #502/#531/#551
  family. Edit Protocol.fs and regenerate; a collision here is a rebase, not a decision (#309).
-->

### The rules the scheduler actually enforces

*Generated from the typed core. The engine that decides your item is the engine that wrote this.*

#### `Paths:` is a declaration, and a fenced one is a QUOTATION

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

> **Why:** #277 (a fenced line read as a declaration would let a doc reserve files) and #496 (an epic and a forgotten touch-set rendered identically, so no gate could be written at all — nine items of real work went invisible, and the surface whose job is board health reported `0 error(s)` over a dead queue).

#### The touch-set grammar — it is NOT a glob language

supported: an exact path ('src/Foo.fs'), or a directory prefix ('src/Foo', 'src/Foo/*', 'src/Foo/**'). There is no glob matcher: a leading '**/' or an interior '*' matches nothing — spell the paths out.

> **Why:** #273. Four hand-copied forms of the unmatchable-token predicate existed across two engines. A token that matches no file conflicts with nothing — so an item declaring only such tokens reserves NOTHING, clears every overlap check, and the lock succeeds under exactly the conditions it exists to prevent.

#### Blockers are checked BEFORE the touch-set

The scheduler asks, in order: is the issue closed? is its Status one we hand out? is it BLOCKED? is its touch-set usable? is it HELD? does it overlap work in flight? The first answer that is not "no" is the verdict, and it is the one sentence the worker reads.

> **Why:** ADR-0038. A blocked item cannot be started whatever its touch-set says, so reporting "no `Paths:` declared" sends a worker to fix something that leaves them exactly where they were. And blockers are FREE — they are board facts already in the scan — where a touch-set costs a body READ per item, on the budget that dies first (#418). That is why bash never fetched a blocked item's body, and how an unreadable one could silently cease to exist.

#### A MERGED blocker is RESOLVED; an unreadable one BLOCKS

`Blocked by` is a Projects v2 board FIELD, not a body line — the same medium split as `Paths:` and its own fence rule, in reverse: `Paths:` lives in the body and a `Blocked by` FIELD is the only place this dependency is recorded. A `Blocked by:` line written into the issue BODY is inert: nothing that clears a blocker reads the body, so it looks like a declaration and does nothing. Write the edge with `set-field <ref> "Blocked by" <ref>`. Once the edge is on the field: `Blocked by` clears on CLOSED **or MERGED**. It does not clear on OPEN, on a blocker whose state could not be read (unverifiable), or on prose that is not an issue ref at all (unparseable) — all three BLOCK.

> **Why:** #476: `Blocked by` may name a PULL REQUEST, whose state is OPEN | CLOSED | MERGED. A rule clearing only on CLOSED unblocks when the blocking work is ABANDONED and blocks forever once it is FINISHED — the gate opened precisely when the work was thrown away and shut precisely when it was done. And #266/#421: "I could not look" is not "I looked and it is fine"; prose in a dependency field is not permission. And .github#1933: two agents independently read a `Blocked by:` BODY line, found no FIELD edge, and concluded there was none — one filed a false defect (.github#1931) and withheld from promoting a row only because a third worker caught the contradiction by hand. Nothing in the operator-facing docs said which medium held the fact; only the `.fsi` comments did, and filers do not open those.

#### The claim lock is a comment-order CAS, and the ASSIGNEE cannot hold it

A claim is an `fsgg:claim` marker COMMENT, and the lowest live marker id wins. GitHub issues comment ids from one server-side sequence, so "lowest live marker" is a total order every racer observes identically. The GitHub ASSIGNEE cannot be the lock, because N agents share one account. That total order is over MARKERS, and it separates WORKERS only while their ids are DISTINCT: an id two workers share is an id this lock cannot separate, and `release`, `heartbeat`, `say` and `inbox` then act on one another's claims. So a worker id is MINTED, never chosen — a worker asked to pick one is not a random source.

> **Why:** ADR-0027, and #419 for the distinctness half: agents asked to invent an id converge on the same corner of the name space, and this board carried FOUR `finch-*` workers at once — every one of them lifted from the single example id that then sat in the recipe. The attractor is the WORD, not the suffix, which is why the remedy is a mint rather than a reminder to be careful, and why #532/#551/#570 had to remove the pasteable id from the docs twice by hand before a gate asserted it. The lock lives on REST, and the invariant it serves — a lock may never live on the budget that dies first — is unamended. What inverted is WHICH budget that is, so this rule no longer asserts a standing answer. #418 measured GraphQL dying first (five workers looping `take` drained 5,000 pt/hr in ~15 minutes), and REST was chosen as the survivor. #895 measured the reverse, twice on 2026-07-16: REST core hit 0/5,000 and took `claim`/`take`/`who` down with it, while GraphQL stayed healthy through both — 3,639/5,000 at the first of them. This rule used to state "GraphQL is the first budget to die" as standing fact, and that premise is what kept regenerating the doctrine that caused the inversion — a recipe steering every worker's reads onto REST to save GraphQL points, on one shared account, spending the lock's own budget to save 7 points of 5,000. #895 decided (2026-07-17) that the lock STAYS and the DOCTRINE moves (#968): REST is metered per request and cannot be batched, so under fan-out it is structurally the scarcer budget with no lever to pull, where GraphQL batches 100 nodes to a query. Discretionary reads belong on GraphQL; REST carries the lock, which has no alternative.

#### The lease is a WINDOW, and an unknown age says so

A claim's lease is 120 minutes by default (`FSGG_CLAIM_LEASE_MIN`), and `heartbeat` renews it only while it is LIVE. Past it the claim is REAPABLE — not free: only `reap` may break a lock, and an item's touch-set stays reserved until it does. An EXPIRED lease cannot be renewed in place; the holder must re-claim. Evidence that the work is alive — an open `item/<n>-*` PR — withholds the item from `take` and REFUSES a `reap`, but it does not revive the lease. A claim whose age cannot be read reports `lease unknown`, never a window.

> **Why:** #428 ("nothing schedulable" and "queued behind a claim held by <w>, lease frees in ~96m" are the same fact and two completely different operator instructions — the first reads as an empty queue and sends a worker home) and #440/#488 (inventing "frees in ~120m" from a missing timestamp is a confident-but-unfounded sentence, which is the class both were closed for). And the lease is a TIMER, which is why it never decides alone: it cannot see a REST outage, and `heartbeat` is REST, so an outage on the lock's budget spends a lease nobody can renew and silently reads as abandonment (#976, ratifying that the fleet stops there rather than making the clock outage-aware). What answers instead is evidence — an open `item/<n>-*` PR (#581), or a liveness probe that failed and therefore fails closed (#266). Expiry is EVIDENCE of abandonment, never proof.

#### A read that did not happen may never render as a confident answer

An error, an empty result, and a legitimate "no" are three different facts. A failed board scan is not an empty board; a failed marker read is not an unheld item; an unread issue body is not an undeclared touch-set. Every one of them fails CLOSED and says which it was.

> **Why:** Epic #266, which has 51 children. #461: a failed claim scan read as "nothing is claimed", so `take` handed a held item to a second worker. #344: a rate-limited scan exited 0 with no verdict, and a worker read "nothing to do" off a board it never managed to read.

### What the scheduler can tell you, and nothing else

One total function returns one of these. There is no other answer, and there is no silent no —
an unreachable answer is not a negative one.

- **`startable`** — Nothing holds it. It can be claimed now.
- **`issue-closed`** — The issue is CLOSED while the board still shows it open. The issue's state is the WORK; the board column is a PROJECTION of it. When they disagree, the issue wins — run /check-board.
- **`wrong-status`** — Its board Status is not one a scheduler hands out (or it has none at all, which makes it invisible to every scheduler and is a bug, not a decision).
- **`blocked-by`** — A `Blocked by` entry is unresolved. CLOSED and MERGED resolve; OPEN, unverifiable and unparseable all BLOCK.
- **`awaiting-human`** — `Blocked on: human/...` — a HUMAN must act first, whatever the `Paths:` line records, so an agent cannot make the call the item exists to escalate (#918). `human/decision` is unschedulable until a human CHOOSES; `human/action` becomes startable the moment a human action (e.g. a scope grant) lands, but not before. Which one rides on the verdict's `humanBlock` detail.
- **`awaiting-delivery-route-decision`** — The mandatory agent-authored delivery-route receipt is missing, stale, malformed, or unreadable. Re-evaluate the item; the engine never infers a route from checklist facts.
- **`no-touch-set`** — No `Paths:` line at all — an OMISSION. The item is real work and it is invisible to every worker who asks for work. Declare one, or `Paths: none` if it truly has no touch-set.
- **`deliberately-no-touch-set`** — `Paths: none` — a decision somebody made. An epic, a decision item, an investigation whose scope IS the question. Unschedulable BY DESIGN, and correct.
- **`unusable-touch-set`** — The declaration contains token(s) that can match no file, so they reserve NOTHING — and files nobody reserved are invisible to every other worker's overlap check.
- **`held-by`** — A live claim marker holds it. Wait out the lease, or talk to the worker.
- **`held-by-live-work`** — The lease EXPIRED but the work did not: an open `item/<n>-*` PR is the worktree protocol's own artifact, and it outranks a timer. Not offered; its touch-set stays reserved.
- **`item-pr-open`** — No claim marker governs it, but an `item/<n>-*` PR is already OPEN on its branch — an implementation is in flight whether or not anyone claimed it. Not offered: claiming it would duplicate work that is already written (#651).
- **`overlaps-in-flight`** — Its files collide with work already in flight. The holder and its lease window are named, because "nothing schedulable" and "queued behind a claim that frees in ~96m" are the same fact and two completely different instructions.
- **`undetermined`** — WE COULD NOT DECIDE — and that is never a silent no. An unreachable answer is not a negative one. This is the case whose absence made every other case a lie waiting to happen.

<!-- END GENERATED: fsgg-protocol -->
