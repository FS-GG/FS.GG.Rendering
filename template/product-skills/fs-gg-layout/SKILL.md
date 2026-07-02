---
name: fs-gg-layout
description: Generated product guidance for laying out an FS.GG.UI product by output size — compute a HUD and a gameplay/content region responsively, and keep an active item inside the gameplay region.
---

# Layout

## Scope

Use this skill for generated product screens that must split by output size into a
HUD band and a gameplay/content region, and keep the active item inside that region.
It covers the **consumer slice** of layout:

- compute a HUD region and a gameplay/content region from the current output size,
- keep an active item clamped inside the gameplay region,
- read the `LayoutEvidence` report the starter produces for a size.

It does **not** cover the Yoga layout **engine** — see [Boundary](#boundary).

## Consumer surface

Layout for a product is reached through the starter's `LayoutEvidence` module
(re-exported at the product's top-level namespace in the starter project's
`Program.fs`). The region and
evidence records come from `FS.GG.UI.Scene`.

- `hudRegionForSize : Size -> LayoutRegionEvidence`
- `gameplayRegionForSize : Size -> LayoutRegionEvidence`
- `activeGameplayBoundsForSize : Size -> Model -> LayoutGameplayBounds`
- `movementUsesGameplayRegion` / `spawnUsesGameplayRegion : Size -> Model -> bool`
- `layoutEvidenceForSize : Size -> Model -> LayoutEvidenceReport`
- `boundsInside : outer -> inner -> bool`

You compute regions from a `Size` and place content into them; you do not call the
layout engine.

## Compute HUD + gameplay/content regions responsively by output size

Split the output size into a fixed-height HUD band across the top and a
gameplay/content region filling the rest. The gameplay region reserves at least one
unit of height so it never collapses:

```fsharp
open FS.GG.UI.Scene

// HUD band across the top, full width, fixed height.
let hudRegionForSize (size: Size) : LayoutRegionEvidence =
    { Name = "summary"
      Bounds = { X = 0.0; Y = 0.0; Width = float size.Width; Height = 96.0 } }

// Everything below the HUD is the gameplay/content region.
let gameplayRegionForSize (size: Size) : LayoutRegionEvidence =
    let hud = hudRegionForSize size
    { Name = "content"
      Bounds =
        { X = 0.0
          Y = hud.Bounds.Height
          Width = float size.Width
          Height = max 1.0 (float size.Height - hud.Bounds.Height) } }
```

Because both regions are derived from the passed-in `Size`, re-computing them for a
new size re-lays-out the screen — that is the responsive path.

## Keep an active item inside the gameplay region

Map the active item's position into the gameplay region and clamp it so it can never
leave, then confirm containment with `boundsInside`:

```fsharp
// Clamp the active item into the gameplay region (never outside).
let activeBounds = activeGameplayBoundsForSize size model
let region = gameplayRegionForSize size

// The movement/spawn policies read exactly this containment fact:
movementUsesGameplayRegion size model   // : bool  — active bounds are inside the region
spawnUsesGameplayRegion size model      // : bool  — spawn position is inside the region
```

`boundsInside region.Bounds activeBounds.Bounds` is the containment check the clamp
guarantees; keep the active item's placement flowing through
`activeGameplayBoundsForSize` so this stays true across sizes.

## The `LayoutEvidence` shape

`layoutEvidenceForSize size model` folds the scene, the HUD and gameplay regions,
HUD text bounds, and the overlap status into one `LayoutEvidenceReport` for a size:

```fsharp
let report = layoutEvidenceForSize size model
// report.HudRegion       : Some (HUD region)
// report.GameplayRegion  : Some (gameplay/content region)
// report.TextBounds      : HUD text-bound list
// report.OverlapStatus   : NoLayoutOverlap | LayoutOverlaps _
```

Read the report to see the regions your screen resolved to at a given size; a
non-empty overlap status means HUD text or gameplay bounds collided and the report
downgrades its proof level. Change the region math (heights, insets) — not any engine
internals — to resolve an overlap.

## Boundary

This skill stays in the consumer slice. It does **not** document:

- the Yoga layout **engine** — `Layout.evaluate`, `Defaults.layoutNode` /
  `Defaults.availableSpace`, and the rest of the `FS.GG.UI.Layout` package surface.
  Compute regions from a `Size`; do not evaluate layout nodes.
- surface baselines — the framework-governed `.fsi` / baseline authoring for the
  layout package.

When you need behaviour beyond computing regions from a size and clamping the active
item, you have reached the framework's layout engine, which is owned upstream by the
framework-authoring `fs-gg-layout` skill in the FS.GG.Rendering framework repository —
not product-author surface. (That upstream skill shares this `name:`; it is the engine
owner, this is the consumer slice.)

## Build & Test Commands

Run `./fake.sh build -t Dev` and `./fake.sh build -t Verify` in the generated product;
`./fake.sh build -t Test` for product-owned layout examples.

## Generated Product

Keep examples small and product-owned. Lay out the screens your product actually
ships; do not copy framework layout-engine internals or region math beyond what the
starter exposes.

## Related

- [[fs-gg-scene]] — the primitive layer the regions you compute are painted into.
- [[fs-gg-ui-widgets]] — compose the controls placed inside these regions.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
