# Worktrees and overlap

Start from fetched `origin/main`; never share a mutable checkout between workers. A worktree is
disposable isolation, not ownership—the claim marker owns the item.

There is one authoritative Coordination-board orchestrator at a time. Before an orchestrator in an
external repository starts or routes a Coordination fix, use the production orchestrator route against
the checked-in board authority `FS-GG/.github#2801`: `overlap orchestrate FS-GG/.github#2801 <requesting-repo> <idempotence-key>
<coordination-ref> <holder>`. Every board driver uses the same configured authority ref. The route
refuses any caller-supplied alternative before reading or writing a lease, so callers cannot split the
singleton into per-request authority domains. The route
binds the repository to the request ref and the holder to the current
minted worker; copied public lease strings cannot nominate another actor. The active holder projects
the highest external block to board `Severity: Critical` before ordinary work—it does not merely print it. If a live
generation exists, the external orchestrator must durably route one
generation-bound, idempotent blocking request to that holder and must not start a competing
Coordination implementation or orchestration lane. The board orchestrator preserves in-flight safety,
then promotes that request ahead of ordinary board work to minimize the external repository's wait. If
the complete authority census proves no live generation exists, the external orchestrator acquires the
next immutable generation and runs the standard board protocol itself. Unreadable/conflicting leases,
stale-generation requests, and acquisition races fail closed; takeover is permitted only after absence
or expiry, and the lowest GitHub comment id wins a same-generation race while losers withdraw only
their own candidates.

Treat touch-set tokens as the scheduler does. `none` is an intentional file-less item; missing or
unmatchable declarations are not equivalent. Before adding a path, run `widen` or `set-paths`; the
engine performs the live overlap check and notifies affected holders. A transient collision should be
resolved through the engine's automatic mutual-wait route before manual negotiation. Record a typed
wait only against the other holder's exact live claim generation and current shared reservation tokens.
Before the second reciprocal wait is persisted, generation-bound freeze receipts are written for both
holders; production `widen` and `set-paths` then refuse removing the shared tokens. Two authoritative
reciprocal waits idempotently reuse one ADR-0051
room. The host records one revisioned precedence receipt; the loser keeps its claim while the shared
tokens are narrowed, and resumes only after the winner lands by fetching/rebasing, re-running overlap,
explicitly re-widening, and refreshing any review invalidated by the new head. The precedence
application leaves a generation-bound freeze on the losing item; production `widen`
and `set-paths` consume it and refuse a shared-token PATCH until the winner is closed, the current head
has fetched the server's current `main` into `origin/main`, that exact ref is an ancestor of `HEAD`,
overlap is clear, the call is the explicit re-widen, and any open loser PR carries a passing structured
review for its exact current head. Stale or locally manufactured remote-tracking refs, unreviewed heads,
stale generations,
conflicting precedence at one revision, unreadable state, and incomplete writes refuse rather than
guess; retry the same operation, which reconciles from live state.

When there is no authoritative mutual cycle, coordinate manually in the existing room or by `say`:
narrow or sequence the work. Add `Blocked by:` only when one implementation must be authored against
the other's landed result. Deadlock occurrences with this mechanism are evidence on `.github#2801`,
not new work-item children; create another row only after adjudication establishes a different root cause.

Poll `inbox` before widening, before push, and before merge. Keep the claim until merge, publishing,
registry reconciliation, and done-stamp verification are complete.
