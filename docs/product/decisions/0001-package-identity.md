# 0001. Package identity

**Status**: deferred
**Date**: 2026-06-14

## Decision

Keep the existing `FS.Skia.UI.*` package identity (package IDs, root namespaces, template
package ID) for now. **Defer** any rebrand to `FS.GG.UI.*` to a separate, explicit release
decision at migration **Stage R8**. Ordinary migration and product work does not change
package identity.

## Rationale

- The constitution states package identity stays `FS.Skia.UI.*` initially and a rebrand is a
  separate, explicit release decision — this record confirms that for the migration.
- Rebranding now would multiply churn across source, namespaces, the template, and docs before
  the product even compiles in this repository.
- Recording the decision removes ambiguity for the Stage R4 source import (imported code keeps
  its identifiers) without committing to or blocking a future rename.

## Revisit trigger

Migration **Stage R8 — Decide rebrand separately**, or any earlier explicit release decision by
the maintainer. A rebrand, if chosen, publishes replacement packages before deprecating the old
IDs and updates namespace, template, and docs identity as one coherent matrix.

## Options considered

- **Defer, keep `FS.Skia.UI.*` (chosen)** — lowest churn; unblocks import; preserves a clean
  future rename decision.
- **Rebrand to `FS.GG.UI.*` now** — rejected: premature; forces a coordinated identity change
  across code/template/docs before the product is even building here.
- **Adopt a new identity only for net-new modules** — rejected: produces a mixed, confusing
  identity surface for consumers.
