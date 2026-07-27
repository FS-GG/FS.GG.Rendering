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

This umbrella is **lane-neutral**: it opens only what every profile pins. `FS.GG.UI.Scene` is the
base capability all five profiles select, so a product's view is always a pure `SceneNode`:

```fsharp
open FS.GG.UI.Scene

// A product's view is a pure function of its model. Every profile has this.
let view model : SceneNode =
    Scene.group [ Scene.textAt { X = 12.0; Y = 24.0 } "product"
                    { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy } ]
```

**How that scene reaches a host is profile-specific, and this skill deliberately does not say.**
`headless-scene` and `governed` have no host entry point at all and pin no viewer package. Consult
the capability skill your profile actually selected:

- `fs-gg-skiaviewer` — `Viewer.runApp` and the `[<EntryPoint>]` wiring. Vendored on **app**,
  **sample-pack** and **game** only; it is deliberately not linked here, because on the other two
  profiles it is not in your workspace and the link would dangle.
- [[fs-gg-testing]] — asserting the generated scene headlessly, with no host. Vendored everywhere.

Read the skills your scaffold vendored (`docs/skillist-reference.md` lists them); an example that
does not compile on your profile is worse than no example.

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

## Expected-workload performance

For a `game` profile, author `PerformanceEvidence.expectedWorkloads` **before feature implementation**,
as soon as normal play is defined. Every required row starts as `Placeholder`: replace its initial state
and messages with product-owned routes, run `./fake.sh build -t PerformanceEvidence`, review the emitted
`definitionDigest`, then copy that digest into `Authored`. A changed definition invalidates a stale
acknowledgement. Run `./fake.sh build -t PerformanceIntent` to emit the SDD-ready `performanceIntent`
block. It is the same published Contracts 7.x declaration embedded in performance evidence: edit its
target FPS, maximum scale, p95/p99/catch-up thresholds, structural budgets, required measurement
capability, and live-compositor posture at the generated declaration source, never in a second SDD
copy. `Test` and `Verify` fail closed on Placeholder/duplicate/stale rows and when a normal workload
exceeds p95 16.67 ms, p99 25 ms, sustained catch-up, or its scene-node budget. A linked blocking
performance-debt issue permits deliberate baseline capture, but the baseline remains failing evidence
and never satisfies acceptance. The artifact is bounded headless update + scene-route evidence; it is
never live compositor, swapchain, or vsync proof.

Representative readiness adds two gates after the machine gate and budget. Bind each normal-play
workload to an opaque FS.GG.Game runner-issued journey receipt (a canonical factory belongs at that
journey's boot seam; caller-authored labels/hashes are not provenance), keep `performanceCostDrivers` independent from the workloads,
cross-check every gameplay visual, compare declared stimulus with observed routing, and emit
unsupported host metrics as unsupported. Then run `PerformanceCriticRequest` and obtain a
fresh-context `supported` verdict in an attributable external review system at the exact landing
commit. A separated-pass fallback must disclose its lack of independence. In-repo JSON,
author-entered reviewer identity, and same-context mode strings are not independence evidence; no
critic verdict can waive provenance, coverage, capability, or budget.

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
