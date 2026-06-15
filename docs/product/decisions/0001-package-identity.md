# 0001. Package identity

**Status**: accepted
**Date**: 2026-06-15 (accepted; originally deferred 2026-06-14)

## Decision

**Rebrand** the package identity from `FS.Skia.UI.*` to `FS.GG.UI.*` at migration **Stage R8**.
This resolves the previously *deferred* decision: the rebrand is now the accepted, explicit release
decision the prior record reserved for R8. It moves as one coherent matrix — package IDs, root
namespaces, assembly names, and the `dotnet new` template identity (including the `fs-skia-ui` →
`fs-gg-ui` short name and the `fs-skia-*` skill folders).

Only the `FS.Skia.UI.` **brand prefix** changes. Descriptive Skia/SkiaSharp technology references
are preserved: the `SkiaViewer` module name, genuine `SkiaSharp`/`Skia` dependency references, and
the descriptive `skia` `<PackageTags>` tag.

### Old → new identity mapping

Runtime modules (×10) — for each, `PackageId == AssemblyName == Title == root namespace`:

| Old identity (`FS.Skia.UI.*`) | New identity (`FS.GG.UI.*`) | New version |
|---|---|---|
| `FS.Skia.UI.Color`           | `FS.GG.UI.Color`           | `0.1.0-preview.1` |
| `FS.Skia.UI.Scene`           | `FS.GG.UI.Scene`           | `0.1.0-preview.1` |
| `FS.Skia.UI.Layout`          | `FS.GG.UI.Layout`          | `0.1.0-preview.1` |
| `FS.Skia.UI.Input`           | `FS.GG.UI.Input`           | `0.1.0-preview.1` |
| `FS.Skia.UI.KeyboardInput`   | `FS.GG.UI.KeyboardInput`   | `0.1.0-preview.1` |
| `FS.Skia.UI.SkiaViewer`      | `FS.GG.UI.SkiaViewer`      | `0.1.0-preview.1` |
| `FS.Skia.UI.Elmish`          | `FS.GG.UI.Elmish`          | `0.1.0-preview.1` |
| `FS.Skia.UI.Controls`        | `FS.GG.UI.Controls`        | `0.1.0-preview.1` |
| `FS.Skia.UI.Controls.Elmish` | `FS.GG.UI.Controls.Elmish` | `0.1.0-preview.1` |
| `FS.Skia.UI.Testing`         | `FS.GG.UI.Testing`         | `0.1.0-preview.1` |

Template:

| Old | New |
|---|---|
| `FS.Skia.UI.Template` | `FS.GG.UI.Template` |
| `dotnet new fs-skia-ui` | `dotnet new fs-gg-ui` |

### Release sequencing and versioning

- **Publish-before-deprecate**: the replacement `FS.GG.UI.*` packages are packed/published **first**;
  only then are the old `FS.Skia.UI.*` IDs deprecated. The deprecation of the public-feed (nuget.org)
  listings is a **recorded action**, copy-ready but **not** applied from this repository
  (Constitution Principle VI — no overclaiming). See
  [`docs/bridge/package-deprecation-notice.md`](../../bridge/package-deprecation-notice.md).
- **Old IDs freeze**: each old `FS.Skia.UI.*` package freezes at its last published version and is
  **deprecated, not deleted/unlisted**, so existing version pins keep resolving.
- **New lineage starts at `0.1.0-preview.1`**: the per-fsproj version overrides from the
  `FS.Skia.UI.*` lineage are reset to `0.1.0-preview.1` for the new identity.

## Rationale (R8 acceptance)

- Migration Stage R8 is the explicit release decision the deferral reserved for this purpose; the
  product now builds and validates in this repository, so the coordinated identity change can be made
  coherently and verified end-to-end.
- The public API surface is unchanged apart from the namespace prefix (verified against `.fsi` and the
  surface-area baselines), so the rebrand is behavior-neutral by construction.
- Publishing replacements before deprecating, and freezing (not deleting) old IDs, protects existing
  consumers from being stranded.

## Revisit trigger

This decision is the realization of the R8 revisit trigger recorded below. No further revisit is
scheduled; a future identity change would itself be a new, explicit release decision following the
same publish-before-deprecate discipline.

---

## History — original deferral (2026-06-14)

> Retained verbatim as the record this decision resolves.

**Status**: deferred

### Decision (deferred)

Keep the existing `FS.Skia.UI.*` package identity (package IDs, root namespaces, template
package ID) for now. **Defer** any rebrand to `FS.GG.UI.*` to a separate, explicit release
decision at migration **Stage R8**. Ordinary migration and product work does not change
package identity.

### Rationale (deferral)

- The constitution states package identity stays `FS.Skia.UI.*` initially and a rebrand is a
  separate, explicit release decision — this record confirms that for the migration.
- Rebranding now would multiply churn across source, namespaces, the template, and docs before
  the product even compiles in this repository.
- Recording the decision removes ambiguity for the Stage R4 source import (imported code keeps
  its identifiers) without committing to or blocking a future rename.

### Revisit trigger (deferral)

Migration **Stage R8 — Decide rebrand separately**, or any earlier explicit release decision by
the maintainer. A rebrand, if chosen, publishes replacement packages before deprecating the old
IDs and updates namespace, template, and docs identity as one coherent matrix.

### Options considered (at deferral)

- **Defer, keep `FS.Skia.UI.*` (chosen at the time)** — lowest churn; unblocks import; preserves a
  clean future rename decision.
- **Rebrand to `FS.GG.UI.*` now** — rejected: premature; forces a coordinated identity change
  across code/template/docs before the product is even building here.
- **Adopt a new identity only for net-new modules** — rejected: produces a mixed, confusing
  identity surface for consumers.
