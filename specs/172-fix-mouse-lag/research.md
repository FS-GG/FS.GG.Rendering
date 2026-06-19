# Research: Fix Mouse Interaction Lag

## Decision: Accept only visible-session input-to-visible evidence for responsiveness closeout

**Rationale**: The referenced post-interactive report explicitly says manual feel and
headless substitute data are insufficient to close the lag concern. Accepted evidence must
record a live presentation boundary, visible response classification, and measured
input-to-visible timing.

**Alternatives considered**: Reusing `ControlsElmish.Perf.runScript` alone was rejected
because it is deterministic and useful for regression shape, but it does not measure the
visible desktop presentation path. Manual-only review was rejected because it cannot prove
the 100 ms / 150 ms budgets.

## Decision: Use existing retained pointer routing and viewer input queue as the hot-path basis

**Rationale**: `ControlsElmish.runInteractiveApp` already routes native pointer samples
through retained frames where possible, coalesces pointer moves, drains discrete inputs in
order, and emits frame metrics. This is the correct place to fix lag because it is the live
path used by the sample and already has focused tests in `Elmish.Tests` and `SkiaViewer.Tests`.

**Alternatives considered**: Adding a sample-local pointer path was rejected because it would
not fix the framework behavior reviewers exercise. Adding a new scheduler dependency was
rejected because the repository already owns input queue and frame-drain contracts.

## Decision: Cover interaction families through `SecondAntShowcase.Core.InteractionContracts`

**Rationale**: The sample already classifies interactive controls and display-only controls.
Using `InteractionContracts.all` gives a bounded representative set that maps each action to
page, control family, expected state change, and visible evidence without requiring every
individual catalog control to be manually timed.

**Alternatives considered**: Timing only the `buttons` page was rejected because it misses
value-changing, overlay, navigation, selection, and dense-control paths. Timing every catalog
control individually was rejected as slower than needed; the requirement is representative
family evidence plus explicit display-only exclusions.

## Decision: Extend the sample responsiveness evidence contract additively

**Rationale**: `FS.GG.UI.SkiaViewer` already defines `ViewerLatencyRecord`,
`ViewerResponsivenessSummary`, tokens, budgets, and writers. The sample should build on that
shape and add review-specific fields such as action type, expected visible result, observed
visible result, control ids, and acceptance status. This avoids a breaking framework record
change while still satisfying the feature evidence requirements.

**Alternatives considered**: Changing `ViewerLatencyRecord` with new required F# record fields
was rejected as a likely breaking public API change. Writing ad hoc markdown-only evidence was
rejected because maintainers need machine-readable pass/fail timing.

## Decision: Fail closed when live evidence is unavailable or unreliable

**Rationale**: The spec requires blocked/rejected status instead of acceptance when the visible
desktop session is unavailable, hidden, throttled, or missing a timing boundary. `--require-live`
must therefore keep non-measured output non-accepting and return a non-success exit code.

**Alternatives considered**: Treating `environment-limited` substitute records as accepted was
rejected because it repeats the defect called out in the feedback report.

## Decision: Preserve prior showcase fixes as part of the validation package

**Rationale**: The lag fix can touch pointer routing, retained render, sample CLI evidence, and
the package-consuming sample. Validation must rerun coverage, sample tests, visual readiness,
and alpha/navigation checks so opaque backgrounds, ghost-style navigation, mapped-control
coverage, and slider behavior do not regress.

**Alternatives considered**: Running only pointer-focused tests was rejected because the
referenced reports identify recent visual and slider regressions that acceptance must preserve.

## Decision: Add no new runtime dependency

**Rationale**: The repository already has Silk.NET input/windowing, OpenGL presentation,
SkiaSharp rendering, existing diagnostics, and JSON artifact writers. The planned feature can
be implemented with these facilities and normal .NET timing APIs.

**Alternatives considered**: Pulling in an external UI automation or tracing library was
rejected for this plan because it would add maintenance and package-surface cost without first
exhausting the existing viewer/session diagnostics.
