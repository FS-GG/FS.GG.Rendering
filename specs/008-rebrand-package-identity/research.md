# Phase 0 — Research: Rebrand Package Identity (R8)

All NEEDS CLARIFICATION items are resolved. The two genuine release decisions were taken by the
maintainer; the remainder are mechanics established by reading the repository.

## R1 — New package lineage starting version

- **Decision**: The `FS.GG.UI.*` lineage starts at **`0.1.0-preview.1`**. Every renamed runtime
  module and the template package reset to this version; the old `FS.Skia.UI.*` IDs **freeze** at
  their last published `0.1.x-preview.1` version.
- **Rationale**: New package IDs are a new lineage with no shared history; resetting to a clean
  `0.1.0-preview.1` avoids implying continuity that doesn't exist and keeps the new brand honestly
  in *preview*. (Maintainer choice over "continue ~0.1.130" and "jump to 1.0.0".)
- **Alternatives considered**: Continue current per-module numbering (rejected — manufactures a
  false continuity across distinct IDs); jump to `1.0.0` (rejected — implies an API-stability
  commitment the preview line has not made).
- **Mechanics**: Per-module `<Version>` overrides in each `src/<M>/*.fsproj` and the template
  package fsproj are reset; the `Directory.Build.props` default `<Version>` is already
  `0.1.0-preview.1`. Record the starting version and the old-ID freeze in decision `0001` (FR-012).

## R2 — Brand-token scope (how far beyond the dotted prefix)

- **Decision**: **Full coherence including the `fs-skia-*` skill folders.** Rebrand (a) the
  `FS.Skia.UI.` dotted identity everywhere; (b) user-facing brand tokens — the `dotnet new`
  short name `fs-skia-ui` → `fs-gg-ui`, the template `packagePrefix` default, brand text in
  `Title`/`Description`/template `name`; and (c) the `fs-skia-*` skill-folder names → `fs-gg-*`
  with all their cross-references (template.json `source` paths, generated configs, docs).
- **Rationale**: Leaving `dotnet new fs-skia-ui` or `fs-skia-scene` skill folders after the
  libraries become `FS.GG.UI.*` is exactly the "mixed, confusing identity" the original decision
  rejected. The maintainer chose maximum coherence.
- **Preserved (descriptive, NOT brand)**: the `SkiaViewer` module name (a SkiaSharp-backed viewer),
  genuine `SkiaSharp`/`Skia` dependency references, and the descriptive `skia` **package tag**
  (denotes the SkiaSharp technology, not the brand). A blind global replace that mangles these is a
  defect (spec Edge Case; FR-006).
- **Alternatives considered**: dotted-prefix-only (rejected — leaves a mixed user-facing brand);
  prefix + user-facing tokens but keep skill folders (rejected by the maintainer in favor of full
  coherence).

## R3 — How identity is expressed in this repository (mechanics)

Read from the tree so the rename targets the right surfaces:

- **Package ID / assembly name / brand title** live **per-fsproj** in `<PackageId>`,
  `<AssemblyName>`, `<Title>`, and brand text in `<Description>`. There is no `<RootNamespace>`
  property — the root namespace is the `namespace FS.Skia.UI.<M>` line at the top of each `.fs`/
  `.fsi`. So renaming a module touches **both** its fsproj metadata and every source file's
  namespace line, plus every `open FS.Skia.UI.<M>` in consumers/tests.
- **Internal build graph is decoupled from package IDs.** Cross-project and test references are
  `ProjectReference` (e.g. `..\Scene\Scene.fsproj`), not `PackageReference Include="FS.Skia.UI.*"`.
  Renaming package IDs therefore does **not** break the internal compile; the namespace edits are
  what the compiler enforces and what must stay coherent.
- **Surface baselines** are `tests/surface-baselines/FS.Skia.UI.<M>.txt` — the namespace is in the
  **filename** and in **every fully-qualified type line** inside. The rename must move the file and
  rewrite its contents; `scripts/refresh-surface-baselines.fsx` regenerates them from the renamed
  assemblies, and the surface-drift check confirms only the prefix changed.
- **Template identity** is in `.template.config/template.json` (`identity`, `name`, `shortName`,
  `classifications`, `packagePrefix` default, and the skill `source` paths) and
  `.template.package/FS.Skia.UI.Template.fsproj` (the **file name itself** carries the brand, plus
  `<PackageId>`/`<Title>`/`<Description>`). The template also ships
  `template/base/docs/api-surface/**/*.fsi` **verbatim** (`copyOnly`, so `sourceName` substitution
  won't touch them) — these fully-qualified framework signatures reference the renamed packages and
  must be rebranded directly.
- **Ownership metadata already points to FS-GG.** `Directory.Build.props` `Authors`/`Company`/
  repository URLs were re-pointed at import (R4); only the package *identity* was retained. R8
  changes the identity, not the ownership metadata.

## R4 — In-scope vs. historical surfaces

- **In scope**: product source (`src/**`, the fsproj/props identity), tests/fixtures
  (`tests/**` namespaces + `Package.Tests` identity assertions + `surface-baselines`), the template
  (`.template.config`, `.template.package`, `template/**`), `.github/workflows/release.yml`
  (`dotnet new` short name), and **active docs** (bridge note, old-repo-redirect Block B,
  PROVENANCE, README, decisions `0001`/`0002`, per-module READMEs/SKILLs).
- **Out of scope (history)**: `specs/**` (every feature record — describes what was true at that
  stage), `docs/imported/**` (imported source snapshots), `docs/audit/**` (mechanism inventory).
  These keep the identity that was true when written, mirroring how PROVENANCE preserves the
  import-time mapping as history. SC-002 scopes its "zero `FS.Skia.UI.*`" search to **product
  source**, not these historical records.

## R5 — Publish-before-deprecate sequencing (and the no-overclaim boundary)

- **Sequence**: (1) rename + build + default-tier green under the new identity; (2) `dotnet pack`
  the ten `FS.GG.UI.*` modules and `FS.GG.UI.Template` to `~/.local/share/nuget-local/` — the
  replacement packages now **exist** (FR-007); (3) author the **copy-ready deprecation notice**
  (`contracts/deprecation-notice.md`) mapping each old ID → new ID as a **recorded action** for the
  public feed, and update old-repo-redirect Block B accordingly.
- **No-overclaim boundary** (Principle VI / FR-009): the actual nuget.org publish and old-ID
  deprecation are owned outside this tree. R8 produces content + a recorded action marked
  **NOT yet applied**; it MUST NOT report the feed as changed. Old IDs are **deprecated with a
  forward pointer, not deleted/unlisted**, so existing version pins keep resolving (FR-008).

## R6 — Surface-invariance guard (the rename's hidden risk)

Because every `.fsi` and every baseline line changes its namespace token, an accidental public
surface change could ride along unseen. **Guard**: after the rename, the only diff between the old
and new baselines (and between old/new `.fsi`) MUST be the `FS.Skia.UI` → `FS.GG.UI` prefix — zero
added/removed/retyped members (FR-005, SC-005). A normalized diff (substitute the prefix, then
compare) makes this checkable; the surface-drift check enforces it on every build. See
`contracts/surface-invariance.md`.
