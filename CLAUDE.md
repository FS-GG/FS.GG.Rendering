<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
`specs/008-rebrand-package-identity/plan.md` (active feature: Rebrand Package
Identity — Migration Stage R8, the final planned stage and the first to change
product code. Resolves decision `0001` (deferred → accepted) and rebrands
`FS.Skia.UI.*` → `FS.GG.UI.*` as one coherent matrix: package IDs, root
namespaces, assembly names, and the `dotnet new` template identity (incl.
`fs-skia-ui` → `fs-gg-ui` and `fs-skia-*` skill folders). New lineage starts at
`0.1.0-preview.1`; old IDs freeze. Publish-before-deprecate: new packages packed
first, then a copy-ready deprecation notice as a recorded action (Principle VI,
no overclaiming). Only the `FS.Skia.UI.` brand prefix changes — descriptive
`SkiaViewer`/`SkiaSharp` usage is preserved; `specs/**`, `docs/imported/**`,
`docs/audit/**` stay as history. Surface differs from before only by the prefix).
<!-- SPECKIT END -->
