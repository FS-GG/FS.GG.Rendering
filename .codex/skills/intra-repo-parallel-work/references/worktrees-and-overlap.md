# Worktrees and overlap

Start from fetched `origin/main`; never share a mutable checkout between workers. A worktree is
disposable isolation, not ownership—the claim marker owns the item.

Treat touch-set tokens as the scheduler does. `none` is an intentional file-less item; missing or
unmatchable declarations are not equivalent. Before adding a path, run `widen` or `set-paths`; the
engine performs the live overlap check and notifies affected holders. A transient collision should be
resolved by narrowing, sequencing, or a room. Add `Blocked by:` only when one implementation must be
authored against the other's landed result.

Poll `inbox` before widening, before push, and before merge. Keep the claim until merge, publishing,
registry reconciliation, and done-stamp verification are complete.
