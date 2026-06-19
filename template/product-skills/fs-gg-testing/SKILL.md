---
name: fs-gg-testing
description: Assert generated-product expectations and evidence in a governed FS.GG.UI product.
---

# Testing Capability

## Scope

Use this skill for product test and evidence helpers: declaring
generated-product expectations, classifying local package drift, and building
evidence reports from pure inputs.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/Testing/Testing.fsi`. The helper modules
(`GeneratedProductAssertions`, `LocalConsumerPackages`, `EvidenceReports`) are
pure functions over value records.

## Usage

```fsharp
open FS.GG.UI.Testing

// Declare what this product expects of its own generated output.
let expectation =
    { Profile = "governed"
      RequiredFiles = [ "src/Product/Product.fsproj"; "docs/effects-boundary.md" ]
      ForbiddenPrefixes = [ "samples/" ]
      PackageReferences =
        [ { PackageId = "FS.GG.UI.Scene"; Required = true }
          { PackageId = "FS.GG.UI.Testing"; Required = true } ] }

let summary = GeneratedProductAssertions.summarize expectation
```

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to evaluate product expectations and evidence
reports.

## Evidence

Build and write evidence with `EvidenceReports.build` / `write` into this
product's `readiness/` paths. Do not copy framework readiness reports into the
product.

## Feature 168 Evidence Rules

- Package-consuming products must compare `FS.GG.UI.` package pins with the
  current framework packages and use `scripts/refresh-local-feed-and-samples.fsx`
  or the `package-feed` proof workflow when local feed validation is required;
  stale package pins need an explicit caveat.
- Framework readiness output under `specs/*/readiness/` is ignored until
  `.gitignore` allowlists it; product evidence should still record
  `git check-ignore` proof when committed.
- Do not run `dotnet test` for the same project/configuration concurrently
  unless each run uses isolated output or a distinct `BaseOutputPath`.
- Prefer real screenshot evidence, disclose degraded capture, require reviewer
  accepted readiness, and keep manual caveats outside generated summary or
  managed section rewrites.
- Responsiveness evidence must validate pointer and keyboard activation
  separately from screenshot readiness and separate routing from update, render,
  and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks remain visibly caveated.

## Package Boundary

Keep assertion and evidence logic pure over value records; let your test runner
and `Verify` target perform the actual file and process I/O.

## Generated Product

The governed profile selects Testing alongside Scene so product tests can assert
their own generated structure and package pins.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] — the capability whose generated output these tests assert.
- [[fs-gg-project]] — product-level wiring of expectations and readiness gates.

## Sources / links

- Expecto (driven test runner): https://github.com/haf/expecto
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
