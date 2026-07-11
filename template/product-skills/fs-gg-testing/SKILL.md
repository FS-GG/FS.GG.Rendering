---
name: fs-gg-testing
description: Test a generated FS.GG.UI product — assert generated-product expectations and evidence, and test that the UI actually responds (drive interaction headlessly through the real route, and guard clicks with BoundIds so a silent no-op cannot pass green).
---

# Testing Capability

## Scope

Use this skill for product test and evidence helpers: declaring
generated-product expectations, classifying local package drift, and building
evidence reports from pure inputs.

If your product has a UI, read **Test that your UI actually responds** first — it
is the test a product suite is most likely missing.

## Test that your UI actually responds

A UI suite that only exercises `update` proves the part that was never in doubt.
It never asks the question that actually breaks: **when the user clicks this
control, does anything happen?** A control can render perfectly and be wired to
nothing, and no amount of `update` testing will notice. This has shipped silently
more than once.

Two facts to test on, both of which your product can reach today:

- **Drive the real route, don't simulate it.** `ControlsElmish.Perf.runScriptToModel`
  folds an ordered script of clicks / keys / scrolls / ticks through the REAL
  retained pointer route and returns the **final model** — pure, headless, no GL,
  no window, deterministic. Assert on the model the interaction produced, not on a
  message you dispatched by hand.
- **An unbound click is silent, so guard it.** `ControlRenderResult.BoundIds` is
  the set of ids that actually carry an event binding. A click at an id that is
  *not* in it dispatches nothing and raises nothing. So a typo'd `ControlId` in a
  test drives *nothing* — and if the assertion is negative ("the screen did not
  change", "no error appeared") the test **passes**. An entire headless UI suite
  can be green and pressing nothing. Assert `Set.contains id result.BoundIds`
  before you drive a click, and build it into any click helper you write so it
  cannot be forgotten per-test.

**The runnable recipe — locate → guard → drive → assert, plus capturing the
post-interaction frame — is in [[fs-gg-elmish]].** It lives there because
`Perf` and `BoundIds` come from the Controls packages, which only the
control-bearing profiles (`app`, `sample-pack`, `game`) ship; a `headless-scene`
or `governed` product has no controls to click, and this skill must not tell it to
open a package it was never given.

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
    { Profile = "game" // your product's own profile
      // <YourProduct> = this product's name (its src/ project directory).
      RequiredFiles = [ "src/<YourProduct>/<YourProduct>.fsproj"; "docs/effects-boundary.md" ]
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

## Evidence Rules

- Compare your product's `FS.GG.UI.` package pins against the versions you intend
  to ship against; when you validate against a locally built package instead of a
  released one, record that as an explicit caveat so a stale pin is never mistaken
  for a passing check.
- Keep evidence under your product's own `readiness/` paths. Treat generated
  reports as transient: when a path is ignored by default, prove a committed file
  is actually tracked rather than silently dropped.
- Do not run `dotnet test` for the same project/configuration concurrently
  unless each run writes to its own isolated output path.
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

Every profile that ships a product test project (app, headless-scene, governed,
sample-pack, game) selects Testing alongside Scene so product tests can assert
their own generated structure and package pins.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). If your product uses Spec Kit, record the findings
and resolving links under the feature's `specs/<feature>/feedback/` folder; otherwise record
them in this skill's **Sources** / durable-lessons line (and any product-local `docs/`
location). Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-elmish]] — the runnable interaction-driver recipe (`Perf.runScriptToModel`,
  the `BoundIds` pre-click guard, post-interaction frame capture).
- [[fs-gg-scene]] — the capability whose generated output these tests assert.
- [[fs-gg-project]] — product-level wiring of expectations and readiness gates.

## Sources / links

- Expecto (driven test runner): https://github.com/haf/expecto
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
