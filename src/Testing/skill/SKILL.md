---
name: fs-gg-testing
description: Work on generated product, package validation, visual inspection, and retained inspection readiness helper contracts.
---

# Testing Capability

## Scope

Owns `src/Testing/`, testing helper contracts, `template/fragments/testing/`, and generated product validation helper guidance.

## Public Contract

The supported API lives in `src/Testing/Testing.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.Testing.txt`.

## Retained Inspection Readiness

Retained inspection validation lives in Testing and consumes Scene artifacts.
Use `RetainedInspectionValidation`, `RetainedInspectionReadiness`, and
`RetainedInspectionMarkdown` for damage-locality rules, readiness aggregation,
Markdown, JSON, and managed-section updates.

When changing this area:

- keep missing required retained facts blocked or unsupported, never silently
  accepted;
- keep broad or full-surface damage blocking unless a matching
  `IntentionalDamageException` is recorded and remains visible;
- preserve invalid and unused exceptions in diagnostics;
- keep managed section markers
  `<!-- FS.GG RETAINED INSPECTION START -->` and
  `<!-- FS.GG RETAINED INSPECTION END -->` stable;
- run
  `dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes`
  when retained validation or readiness rendering changes.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`.

## Test Commands

Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Record helper surface evidence under the active feature readiness
package-surface reports. Stable public surface baselines live under
`readiness/surface-baselines/`.

## Feature 168 Evidence Rules

- Package-consuming samples must compare current `FS.GG.UI.` package pins and
  use `scripts/refresh-local-feed-and-samples.fsx` or `package-feed` proof;
  stale package pins need a local feed caveat.
- Committed feature evidence under `specs/*/readiness/` is ignored until
  `.gitignore` allowlists it; record `git check-ignore` proof before claiming
  committed evidence.
- Do not run `dotnet test` for the same project/configuration concurrently
  unless each run uses isolated output or a distinct `BaseOutputPath`.
- Prefer real screenshot evidence, disclose degraded captures, require reviewer
  accepted readiness, and keep manual caveats outside generated summary or
  managed section rewrites.
- Responsiveness evidence must validate pointer and keyboard activation
  separately from screenshot readiness and distinguish input routing from update,
  render, and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks keep a visible caveat.

## Package Boundary

Testing helpers must not pull broad framework implementation projects into generated products.

## Generated Product

Testing is available to governed products when selected and should stay product-validation focused.

## Runnable example

Open the package namespace and build a generated-product validation evidence report:

```fsharp
open FS.GG.UI.Testing

let report =
    EvidenceReports.build
        { Status = EvidenceOk
          Command = "dotnet test"
          OutputPath = Some "readiness/generated-product.md"
          Fields = [ EvidenceReports.field "profile" "app" ] }

let validation = EvidenceReports.validate report
printfn "accepted=%b status=%s" validation.Accepted (EvidenceReports.statusText report.Status)
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

- [[fs-gg-scene]] supplies the `LayoutEvidenceReport` types these helpers validate.
- [[fsharp-build-orchestration]] runs the governed targets these helpers back.
- [[fs-gg-ui-widgets]] produces retained inspection artifacts through
  `ControlInspection.inspectRetained`.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Expecto (the F# test framework used by generated products): https://github.com/haf/expecto
