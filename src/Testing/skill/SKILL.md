---
name: fs-skia-testing
description: Work on generated product and package validation helper contracts.
---

# Testing Capability

## Scope

Owns `src/Testing/`, testing helper contracts, `template/fragments/testing/`, and generated product validation helper guidance.

## Public Contract

The supported API lives in `src/Testing/Testing.fsi`. Surface changes require `readiness/surface-baselines/FS.Skia.UI.Testing.txt`.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`.

## Test Commands

Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Record helper surface evidence under the active feature readiness
package-surface reports. Stable public surface baselines live under
`readiness/surface-baselines/`.

## Package Boundary

Testing helpers must not pull broad framework implementation projects into generated products.

## Generated Product

Testing is available to governed products when selected and should stay product-validation focused.

## Runnable example

Open the package namespace and build a generated-product validation evidence report:

```fsharp
open FS.Skia.UI.Testing

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

- [[fs-skia-scene]] supplies the `LayoutEvidenceReport` types these helpers validate.
- [[fsharp-build-orchestration]] runs the governed targets these helpers back.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Expecto (the F# test framework used by generated products): https://github.com/haf/expecto
