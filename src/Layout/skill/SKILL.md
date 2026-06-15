---
name: fs-gg-layout
description: Work on Yoga-backed layout contracts and generated product layout usage.
---

# Layout Capability

## Scope

Owns `src/Layout/`, layout tests, and runtime layout engine guidance. Generated product layout-control and widget examples are owned by `fs-gg-ui-widgets`.

## Public Contract

The supported API lives in `src/Layout/*.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.Layout.txt`.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t DependencyReport`, and `./fake.sh build -t PackageSurfaceCheck`.

## Test Commands

Run `dotnet test tests/Layout.Tests/Layout.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Record package-surface and dependency evidence under the active feature
readiness reports. Stable public surface baselines live under
`readiness/surface-baselines/`.

## Package Boundary

Layout may depend on Scene and Yoga.Net. Do not introduce viewer, keyboard, controls, or chart dependencies.

## Generated Product

Products that select Controls receive `FS.GG.UI.Layout` as a runtime dependency but use `fs-gg-ui-widgets` for generated widget guidance. Use this skill only for lower-level layout engine work.

## Runnable example

Open the package namespace and evaluate a single layout node:

```fsharp
open FS.GG.UI.Layout

let root = Defaults.layoutNode "root"
let available = Defaults.availableSpace 320.0 240.0
let result = Layout.evaluate available root
printfn "revision=%d bounds=%d" result.Revision result.Bounds.Length
```

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] is the render target of `Layout.renderComputed`.
- [[fs-gg-ui-widgets]] owns generated layout-control and widget examples.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Yoga (the driven layout engine): https://www.yogalayout.dev/
