# 0002. Template ownership

**Status**: accepted
**Date**: 2026-06-14

## Decision

The rendering repository **owns the templates** — the `dotnet new` template
(`.template.config/`) and its template package (`.template.package/`). They are imported and
maintained here alongside the rendering product, not split into a separate repository.

## Rationale

- The rendering implementation plan's default is to keep templates with rendering unless their
  release cadence later justifies a separate repository.
- Templates validate the product's **generated-consumer contract** (restore/build/instantiate a
  real generated app), which is a rendering concern and a useful product check.
- Keeping templates co-located avoids cross-repo coordination cost, consistent with the
  project's lightweight working style.

## Revisit trigger

Reopen if template release cadence diverges enough from the rendering product to justify
independent versioning/release (for example, templates needing frequent releases decoupled from
library releases), or if template scope grows beyond the rendering product.

## Options considered

- **Rendering repo owns templates (chosen)** — co-located, low coordination cost, templates act
  as a generated-consumer check.
- **Separate template repository now** — rejected: no cadence pressure yet; adds coordination
  overhead without current benefit.
