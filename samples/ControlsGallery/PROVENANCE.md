# Provenance — Controls Gallery Showcase (feature 123, FR-015 / research R9)

This sample **adopts and rebrands** material from the archived `EHotwagner/FS-Skia-UI`
showcase specifications (`docs/testSpecs/Showcase/01`–`10`). All imported identifiers
are rebranded **`FS.Skia.UI.*` → `FS.GG.UI.*`**.

## Adopted material

| Item | Source | Adaptation |
|------|--------|------------|
| The 10-page structure & family grouping | FS-Skia-UI Showcase 01–10 | Mapped onto the live `src/Controls/Catalog.fs` 52-control set; see `contracts/page-registry.md`. |
| "Indigo & Teal on Slate" palette name | FS-Skia-UI Showcase | Accents defined as consumer-owned `Color` literals over the shipped `slate` ramp (research R5). |
| The pointer-interaction contract | FS-Skia-UI Showcase | Re-expressed against the current public control surface (see `README.md`). |
| Per-page evidence requirements | FS-Skia-UI Showcase + `tests/Rendering.Harness` schema | Re-implemented in the consumer as `ControlsGallery.Core/Evidence.fs` so the gallery stays package-only (research R3). |

## Authoritative sources where the archive is unavailable

The archive specs are **not present in this repository**. Where a detail was
unavailable, the authoritative sources are, in order:

1. `specs/123-controls-gallery-showcase/plan.md` §10.1 (family list) and the contracts.
2. The live `src/Controls/Catalog.fs` (`Catalog.supportedControls`) — the control count
   (52) and ids were verified against it, not taken from narrative.

## Rebrand note

No `FS.Skia.UI.*` identifier appears in this sample's source; the framework is consumed
exclusively as the packed `FS.GG.UI.*` packages from `~/.local/share/nuget-local/`.
