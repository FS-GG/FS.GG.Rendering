# Provenance — Ant Design Controls Showcase (feature 135 / G3, FR-017)

This sample **adopts and rebrands** material from two sources. All imported identifiers
are rebranded **`FS.Skia.UI.*` → `FS.GG.UI.*`**; no `FS.Skia.UI.*` identifier appears in
this sample's source.

## Adopted material

| Item | Source | Adaptation |
|------|--------|------------|
| The 3-project consumer split + harness scaffolding (Core / App / Tests, evidence record, seeded scripts) | `samples/ControlsGallery` (G1, feature 123) | Ported and renamed `ControlsGallery.*` → `AntShowcase.*`; the accent seam was dropped (R3). |
| The family-page structure & coverage bijection | G1 `contracts/page-registry.md` | Re-mapped onto the live **96-control** `src/Controls/Catalog.fs` (feature 132 widened it), grouped into 13 catalog pages. |
| The six enterprise template recipes | `docs/product/ant-design/templates/{workbench,list,detail,form,result,exception}.md` | Realized as compositions of **catalog controls only** in `AntShowcase.Core/Templates.fs` (R6 / SC-002); these "groundwork" docs explicitly name Workstream G3 as their consumer. |
| The Ant theme | `FS.GG.UI.Themes.AntDesign` (feature 132) | Consumed verbatim (`AntTheme.antLight` / `antDark`); the showcase never alters tokens (FR-016). |
| Per-page evidence record schema | G1 `ControlsGallery.Core/Evidence.fs` | Re-implemented as `AntShowcase.Core/Evidence.fs`, extended with the resolved Ant `Mode`, so the sample stays package-only. |

## Authoritative sources

1. `specs/135-antd-controls-showcase/` — `plan.md`, `research.md` (R1–R8), `data-model.md`,
   `contracts/`, `quickstart.md` (V0–V8).
2. The live `src/Controls/Catalog.fs` (`Catalog.supportedControls`) — the control count
   (96) and ids were verified against it, never taken from narrative.

## Rebrand note

The framework is consumed exclusively as the packed `FS.GG.UI.*` packages from
`~/.local/share/nuget-local/` (FR-015 / SC-006). Building and running this sample against
that feed *is* the proof the Ant-theme consumer path works end to end.
