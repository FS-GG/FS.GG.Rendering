# T012 — Coherent set served at the same `V` (FR-001, SC-002)

The template version equals the sibling `FS.GG.UI.*` package versions on the org feed — coherence is
**live-verified**, not inferred from packing.

```
FS.GG.UI.Template      0.1.54-preview.1 ✓
FS.GG.UI.Scene         0.1.54-preview.1 ✓
FS.GG.UI.Build         0.1.54-preview.1 ✓
FS.GG.UI.Controls      0.1.54-preview.1 ✓
FS.GG.UI               0.1.54-preview.1 ✓   (full-set BOM/metapackage)
```

(`gh api orgs/FS-GG/packages/nuget/<pkg>/versions` per package.) All members + template pack at the
**same** `V` — the "incoherent set" edge case is excluded. ✅

> Note: there is no `FS.GG.UI.Core` package (the tasks.md example name); the real coherent-set members
> are `FS.GG.UI.{Scene,Build,Controls,Elmish,Layout,KeyboardInput,SkiaViewer,DesignSystem,…}` + the
> `FS.GG.UI` BOM — all confirmed at `0.1.54-preview.1`.
