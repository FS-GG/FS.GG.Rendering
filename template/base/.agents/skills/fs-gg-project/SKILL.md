---
name: fs-gg-project
description: Work on a generated FS.GG.UI product.
---

# Generated Product

## Scope

Owns product application code, product tests, product docs, readiness evidence,
and selected capability skills copied into this product.

## Public Contract

The product references FS.GG.UI capability packages. Product API contracts
belong in product `.fsi` files when public surfaces are introduced.

## Usage

Compose selected capability packages at the product entry point:

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Product entry point wiring selected capabilities into the host.
let view model : SceneNode =
    Scene.group [ Scene.textAt { X = 12.0; Y = 24.0 } "product"
                    { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy } ]

[<EntryPoint>]
let main _ =
    match Viewer.runApp viewerOptions generatedHost with
    | Ok _ -> 0
    | Error _ -> 1
```

## Build Commands

Generated FAKE-backed commands (`./fake.sh`, `fake.cmd`, or `dotnet fake`)
share `.fake` state and are not safe to run concurrently. Run multiple
FAKE-backed commands sequentially:

1. `./fake.sh build -t Dev`
2. `./fake.sh build -t Test`
3. `./fake.sh build -t Verify`

Non-FAKE checks may run in parallel when they do not invoke FAKE or depend on
`.fake`.

## Test Commands

Run `./fake.sh build -t Test` for product tests and selected capability usage checks.

## Evidence

Store product evidence under product readiness paths. Do not copy framework
readiness evidence into the product.

## Feature 168 Evidence Rules

- Package-consuming generated products must compare current `FS.GG.UI.` package
  pins and use `package-feed` proof for stale package pins and feed evidence.
- Framework readiness output under `specs/*/readiness/` is ignored until
  `.gitignore` allowlists it; record `git check-ignore` proof before treating it
  as committed evidence.
- Do not run `dotnet test` for the same project/configuration concurrently
  unless each run uses isolated output or a distinct `BaseOutputPath`.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited evidence must keep a visible caveat.

## Package Boundary

Reference selected capability packages. Do not copy framework implementation
projects into consumer-mode products.

## Generated Product

Keep product governance focused on product behavior, generated guidance, drift,
and evidence gates.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] — the base capability every product profile selects.
- [[fs-gg-testing]] — assert generated structure, drift, and readiness gates.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
