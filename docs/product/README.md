# Product shape

This folder defines **what the FS.GG.Rendering product owns** — the output of migration
**Stage R2 (Define product shape)**. These are decision/definition artifacts produced *before*
any source is copied (source import is Stage R4).

## Contents

- **[module-map.md](./module-map.md)** — the authoritative catalog of product areas, their
  responsibilities, and their import disposition; the answer to "what does rendering own?"
- **[layering.md](./layering.md)** — the four UI layers (semantic controls, design-system
  primitives, themes, design-specific kits) and the one-control-set rule.
- **[docs-to-import.md](./docs-to-import.md)** — triage of source docs for Stage R4
  (import-as-is / adapt / exclude).
- **decisions/** — recorded product-shape decisions:
  - [0001-package-identity.md](./decisions/0001-package-identity.md) — keep `FS.Skia.UI.*`;
    rebrand deferred to Stage R8.
  - [0002-template-ownership.md](./decisions/0002-template-ownership.md) — rendering repo owns
    the templates for now.

## See also

- Project rules: [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md).
- The migration roadmap (R1→R8) and the feature spec for this stage:
  `specs/001-define-product-shape/`.
