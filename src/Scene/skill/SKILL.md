---
name: fs-gg-scene
description: Work on dependency-light scene primitives and generated product scene usage.
---

# Scene Capability

## Scope

Owns `src/Scene/`, Scene package tests, `template/fragments/scene/`, and generated product code that builds pure scene descriptions.

## Public Contract

The supported API lives in `src/Scene/Scene.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.Scene.txt` and package-surface evidence.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal` when this capability changes.

## Test Commands

Run `dotnet test tests/Scene.Tests/Scene.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Update the active feature readiness package-surface and capability-catalog
reports for contract or catalog changes. Stable public surface baselines live
under `readiness/surface-baselines/`.

## Package Boundary

Scene must not reference Elmish, Silk.NET, SkiaSharp, Yoga.Net, or YamlDotNet. Keep host, input, layout, and widget concerns outside this package; use `fs-gg-ui-widgets` for control, chart, and graph authoring.

## Generated Product

Scene is included in every app, governed, headless scene, and sample-pack product as the base capability.

## Runnable example

Open the package namespace and build a small pure scene description:

```fsharp
open FS.GG.UI.Scene

let scene =
    Scene.group
        [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 320.0; Height = 240.0 } (Colors.rgb 16uy 16uy 24uy)
          Scene.circle { X = 160.0; Y = 120.0 } 48.0 (Colors.rgb 220uy 60uy 60uy)
          Scene.textAt { X = 12.0; Y = 24.0 } "HUD" Colors.white ]

let kinds = Scene.describe scene
let evidence = Scene.renderReadbackEvidence { Width = 320; Height = 240 } scene
printfn "%A %s" kinds evidence.DeterministicHash
```

## One shared painter: node count is structural, the image is the visual proof

The interactive host renderer and the image-evidence (screenshot) renderer are the
**same** shared painter (`SceneRenderer.paintNode` inside `FS.GG.UI.SkiaViewer`,
feature 063). Its `match` over `SceneNode` is **exhaustive — there is no wildcard
fallback** — so every modeled primitive (`Line`, `Path`, `Arc`, `Points`, `Vertices`,
`Ellipse`/`FilledEllipse`, `Image`, `RegionNode`, `Chart`, and real-glyph
`Text`/`TextRun`) renders to actual pixels through both paths, and a new `SceneNode`
case is a compile error until both paths handle it. There is no longer an
evidence-mode placeholder rectangle that any primitive collapses onto.

Because of that, treat the two kinds of check as **distinct**:

- `Scene.describe` and node-count assertions are **structural** — they prove a node of
  a given kind is *present in the description*, not that it *painted visible pixels*.
- The decoded image (screenshot / render-readback pixels) is the **visual** proof.

Do not let a structural check stand in for visual proof: a scene that `describe`
reports as a `Line` is only *visibly* rendered when the image shows pixels along that
line. (Before feature 063 a `Line`-only scene drew a single placeholder block, so a
node-count "scene is visible" check passed on an effectively invisible image — that
false-positive is gone now that there is no placeholder.)

## Common pitfalls

- **Record-label collision (decide BEFORE you design your records).** Scene point
  and rect literals use the field labels `X`/`Y`/`Width`/`Height`. F# resolves a
  record literal `{ X = …; Y = … }` to the **most-recently-declared** record type
  carrying those labels, so if your own game model declares a record with the same
  labels (a common `{ X: float; Y: float }` position type), a bare literal can
  silently infer to the wrong type — or fail with a confusing inference error.
  **Remedy:** annotate the literal (`({ X = 0.0; Y = 0.0 }: Position)`), give your
  own positional record distinct labels, or qualify the field. Plan your record
  label names against the Scene labels before you start designing the model — see
  `docs/scaffold-map.md` for the durable-vs-replaceable map this feeds into.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-skiaviewer]] hosts and renders these scene descriptions.
- [[fs-gg-ui-widgets]] builds higher-level controls that compile down to Scene.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven rendering library): https://github.com/mono/SkiaSharp
