---
name: fs-gg-samples
description: Work on optional generated product sample-pack content.
---

# Samples Capability

## Scope

Owns optional sample-pack template content under `template/fragments/samples/`.

## Public Contract

Samples are non-runtime generated content with **no backing library**: they have
`no-public-surface` in the capability catalog. A sample reuses the driven surfaces
of the capabilities it demonstrates (Scene, SkiaViewer, Controls) rather than owning
its own API.

## Usage

A sample-pack screen is ordinary product code reusing existing capability surfaces:

```fsharp
open FS.GG.UI.Scene

// A minimal sample scene copied into a sample-pack product. No sample-owned API.
let sampleScene : Scene =
    Scene.group
        [ Scene.textAt { X = 12.0; Y = 24.0 } "sample-pack"
            { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy } ]
```

## Build Commands

Run `./fake.sh build -t GeneratedProductCheck` and `./fake.sh build -t TemplateCheck`.

## Test Commands

Run generated product `./fake.sh build -t Verify` for the sample-pack profile.

## Evidence

Record sample-pack file lists under the active feature
`readiness/generated-file-lists/` report.

## Feature 168 Sample Evidence Rules

- Sample-pack and package-consuming samples must compare current `FS.GG.UI.`
  package pins before validation is claimed.
- Use `scripts/refresh-local-feed-and-samples.fsx` or the `package-feed` proof
  workflow to prove stale package pins are absent and the local feed is the
  source of package-consuming sample restores.
- When sample readiness uses screenshots, prefer real screenshot evidence,
  disclose degraded captures, require reviewer accepted readiness, and keep
  generated summary caveats visible.
- Canceled, timed-out, skipped, synthetic, substitute, pending-review, or
  environment-limited validation remains a caveat, not a green result.

## Package Boundary

Do not include samples in default consumer products.

## Generated Product

Samples are copied only when the sample-pack profile or sample capability is selected.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] — the primitive capability most samples demonstrate.
- [[fs-gg-skiaviewer]] — host wiring a runnable sample launches through.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
