# Contracts: Code Health — Quick Safety Fixes

**No external interface contracts apply to this feature.**

This is a Tier 2 (internal) change. It does not add, remove, or modify any public `.fsi` surface,
package contract, CLI command, or inter-project boundary:

- `feature159Hash` is private and `feature159ContentIdentity` keeps its existing `val internal`
  signature — `src/Controls/RetainedRender.fsi` is unchanged.
- The new layout cache-revision constant is omitted from `src/Layout/Layout.fsi` (private) — that
  `.fsi` is unchanged.
- The two test edits touch only test bodies.

The only "contract" to protect is **byte-identical layout cache-key output** (FR-006) and **no
silent change to persisted goldens/evidence** (FR-002). Both are verified behaviorally in
[`../quickstart.md`](../quickstart.md) (cache-key byte-identity assertion + golden `git diff`
review), not via a published interface document.

The repo's existing **API surface-drift check** acts as the guard that this remains Tier 2: it MUST
report no surface change after the implementation.
