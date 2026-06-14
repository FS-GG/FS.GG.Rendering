---
name: fs-skia-scene
description: Build pure scene descriptions in a generated FS.Skia.UI product.
---

# Scene Capability

## Scope

Use this skill for product code that builds pure `Scene` / `SceneNode`
descriptions: HUD regions, gameplay geometry, markers, and text. Scene values are
plain data — they perform no window, render, or screenshot I/O themselves.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/Scene/Scene.fsi`. Read them to confirm any union case's exact
field order locally — no DLL reflection needed. Prefer the self-describing
constructors (`Scene.filledRectangle`, `Scene.textAt`, `Scene.circle`) over the
positional tuple cases to avoid an arity slip.

## Usage

```fsharp
open FS.Skia.UI.Scene

let panel = { Red = 40uy; Green = 90uy; Blue = 200uy; Alpha = 255uy }
let ink = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

// A pure scene: a HUD bar plus a label. No I/O happens here.
let hud : Scene =
    Scene.group
        [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 320.0; Height = 48.0 } panel
          Scene.textAt { X = 12.0; Y = 30.0 } "tally: 0" ink ]
```

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** Scene exposes
  `Point = { X: float; Y: float }` and `Rect = { X: float; Y: float; Width: float;
  Height: float }`. If your product also defines a geometry record with the same field
  names (a common `type Vec2 = { X: float; Y: float }`), F# label resolution binds a
  bare `{ X = ...; Y = ... }` to whichever record type is in scope **last**, which
  produces a misleading error cascade at unrelated call sites. Disambiguate explicitly
  at the boundary — annotate the type or qualify the fields — and convert your record
  into the framework type when you call Scene:
  ```fsharp
  type Vec2 = { X: float; Y: float }                     // product geometry
  let toPoint (v: Vec2) : Point = { X = v.X; Y = v.Y }   // explicit conversion
  let p : Point = { Point.X = 0.0; Point.Y = 0.0 }       // or qualify fields inline
  ```

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned scene examples.

## Evidence

Record scene and bounds evidence under this product's `readiness/` paths. Do not
copy framework readiness reports into the product.

## Package Boundary

Scene must not reference Elmish, the viewer host, layout, or widgets. Keep host
wiring in `fs-skia-skiaviewer` and control authoring in `fs-skia-ui-widgets`.

## Generated Product

Scene is the base capability in every profile; build product geometry from these
primitives and feed the resulting `SceneNode` to your `View`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-skia-skiaviewer]] — render the `SceneNode` this skill builds at the host boundary.
- [[fs-skia-ui-widgets]] — compose higher-level controls that ultimately emit scenes.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
