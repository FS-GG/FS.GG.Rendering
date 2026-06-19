# Research: Runtime Diagnostics Taxonomy

## Decision: Add a dependency-light `FS.GG.UI.Diagnostics` package

**Rationale**: The taxonomy must be shared by SkiaViewer, Controls,
Controls.Elmish, Testing, the validation harness, and package-consuming samples.
`FS.GG.UI.Testing` cannot be the producer dependency because runtime packages
should not reference test helpers. A small package containing records, unions,
pure aggregation, readiness evaluation, and artifact rendering lets each
producer map its existing diagnostics without creating package cycles.

**Alternatives considered**:

- Put the taxonomy in `FS.GG.UI.Testing`: rejected because SkiaViewer and
  Controls would need a runtime dependency on a testing package.
- Put the taxonomy in `FS.GG.UI.Scene`: rejected because diagnostics/readiness
  are not scene primitives and would pollute the base rendering model.
- Duplicate types in each package: rejected because readiness and samples would
  still need cross-package conversion logic and tests would keep parsing prose.

## Decision: Preserve existing diagnostics and add explicit adapters

**Rationale**: Existing `RenderDiagnostic`, `ControlDiagnostic`, and
`AdapterDiagnostic` surfaces already carry useful source messages and severities.
The first implementation should keep those constructors source-compatible and
add functions that map them into `RuntimeDiagnostic` records. This honors the
spec's migration rule while enabling readiness to consume one typed contract.

**Alternatives considered**:

- Replace all existing diagnostic record types: rejected as unnecessarily
  source-breaking for a Tier 1 migration.
- Add category/severity fields directly to every existing record immediately:
  rejected because `AdapterDiagnostic` lacks severity today and host diagnostics
  include `Fatal`, so an adapter can normalize behavior more safely.

## Decision: Model readiness impact separately from severity

**Rationale**: Informational environment and backend-cost diagnostics must remain
visible without blocking readiness, while unclassified diagnostics require
review and true blockers fail accepted readiness. Severity alone cannot encode
that distinction. The taxonomy therefore records severity/category and derives
readiness impact as `non-blocking`, `blocks-readiness`, `requires-review`, or
`environment-limited`.

**Alternatives considered**:

- Treat every `Error` as a blocker: rejected because some artifact or
  environment failures should be developer-action or environment-limited rather
  than runtime-readiness blockers.
- Treat only the `readiness-blocker` category as meaningful: rejected because
  missing severity/category must be review-required and accepted exceptions must
  remain visible.

## Decision: Aggregate by stable fingerprint and retain first/last context

**Rationale**: The spec requires repeated diagnostics to collapse in summaries
without losing occurrence count. A fingerprint derived from source, code,
normalized message, category, severity, and action keeps identical diagnostics
grouped. First and last contexts preserve useful investigation detail without
printing every repeated line by default.

**Alternatives considered**:

- Aggregate by message text only: rejected because identical text from different
  sources or categories can have different readiness meaning.
- Store every occurrence in the summary: rejected because default sample output
  must stay compact and repeated backend-cost diagnostics should not dominate
  readiness summaries.

## Decision: Write JSON as the machine contract and Markdown as the reviewer view

**Rationale**: Readiness lanes and tests need stable machine-checkable records.
Reviewers also need a quick view with counts, blocker status, exceptions, and
artifact links. JSON is the authoritative artifact; Markdown is deterministic
and generated from the same summary.

**Alternatives considered**:

- Markdown-only artifacts: rejected because tests would keep parsing prose.
- JSON-only artifacts: rejected because reviewers would lose the existing
  repository convention of opening a concise Markdown readiness summary.

## Decision: Default console output is grouped, verbose output preserves detail

**Rationale**: The retrospective problem was noisy output that made benign
diagnostics look like failures. Default output should show status and grouped
counts; verbose output or artifacts should expose every record when a maintainer
is investigating.

**Alternatives considered**:

- Hide non-blocking diagnostics by default: rejected because environment and
  backend-cost facts must remain visible.
- Print every diagnostic by default: rejected because repeated non-blocking
  diagnostics would keep sample output alarming.

## Decision: Artifact write failures become developer-action warnings

**Rationale**: A run can still classify diagnostics in memory even when its
artifact path is missing or unwritable. The failure must be visible and
machine-checkable, but it should not erase the diagnostic evidence already
captured. The warning may require review depending on readiness policy.

**Alternatives considered**:

- Throw and abandon the summary: rejected because it loses the in-memory
  diagnostic classification.
- Silently continue: rejected because missing artifacts are readiness-relevant
  developer-action diagnostics.

## Decision: Avoid a repository-wide logging provider in this feature

**Rationale**: The constitution already records structured logging provider
selection as unresolved future work. This feature needs classification,
aggregation, artifact contracts, and readiness interpretation, not a logging
framework migration.

**Alternatives considered**:

- Select and integrate a logging library now: rejected as explicitly out of
  scope and too broad for the retrospective item.
- Route diagnostics through ad hoc console strings: rejected because the spec
  requires machine-checkable artifact and readiness behavior.
