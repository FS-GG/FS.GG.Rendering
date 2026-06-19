# Data Model: Runtime Diagnostics Taxonomy

## Runtime Diagnostic

Represents one classified diagnostic event emitted by a runtime, sample, or
readiness workflow.

Fields:

- `id`: stable event identifier within a run.
- `source`: `DiagnosticSource` naming subsystem, command, package, lane, or
  sample area.
- `code`: stable producer code when available.
- `severity`: `DiagnosticSeverity option`; missing values require review.
- `category`: `DiagnosticCategory option`; missing values require review.
- `message`: human-readable diagnostic message.
- `action`: recommended human/developer action.
- `context`: first occurrence context for the event.
- `fingerprint`: normalized key used for repeated-message aggregation.

Validation rules:

- `source` and `message` are required.
- `severity` and `category` must be present before a diagnostic can be fully
  accepted.
- A `readiness-blocker` category derives blocker impact unless a valid scoped
  exception is attached.
- `backend-cost` and expected `environment` diagnostics stay visible and
  non-blocking unless an explicit classification rule elevates them.

## Diagnostic Severity

User-facing importance level.

Values:

- `informational`: expected fact or advisory context.
- `warning`: visible condition that may require developer action or review.
- `error`: runtime or readiness-significant failure.

Validation rules:

- Existing SkiaViewer `Fatal` maps to taxonomy `error` with blocker or
  environment-limited impact based on category/source.
- Existing Controls `Info`, `Warning`, and `Error` map directly to the taxonomy
  names.

## Diagnostic Category

Reason group used in summaries and readiness evaluation.

Values:

- `environment`
- `backend-cost`
- `rendering-limitation`
- `readiness-blocker`
- `developer-action`

Validation rules:

- Every diagnostic used by sample summaries or readiness decisions must have a
  category.
- Environment and backend-cost categories are not blockers by default.
- Developer-action diagnostics can be non-blocking warnings, review-required
  warnings, or blockers depending on the classification rule.

## Diagnostic Source

Names where the diagnostic originated.

Fields:

- `packageId`: optional package identity such as `FS.GG.UI.SkiaViewer`.
- `subsystem`: source subsystem such as `opengl-host`, `control-runtime`,
  `validation-lanes`, or `sample-cli`.
- `laneId`: optional validation lane.
- `sampleId`: optional sample command or app.

Validation rules:

- At least one source token is required.
- Source tokens are stable lowercase strings when written to JSON.

## Diagnostic Context

Carries occurrence-specific context.

Fields:

- `runId`: optional run identifier.
- `timestampUtc`: optional event time.
- `outputPath`: optional artifact/log/screenshot path.
- `details`: stable string key/value facts.

Validation rules:

- Paths are stored repository-relative when available.
- Context must not contain raw exception dumps when a concise cause/action is
  available.

## Aggregated Diagnostic

Represents one repeated diagnostic group in summaries.

Fields:

- `fingerprint`
- `source`
- `code`
- `severity`
- `category`
- `message`
- `action`
- `occurrenceCount`
- `firstOccurrence`
- `lastOccurrence`
- `exampleIds`

Validation rules:

- `occurrenceCount` is at least 1.
- The first and last occurrence contexts are preserved even when individual
  occurrences are omitted from default output.

## Diagnostic Exception

Scoped acceptance of a known diagnostic that would otherwise require review or
block readiness.

Fields:

- `exceptionId`
- `scope`: affected category, source, code, fingerprint, lane, or sample.
- `reason`: reviewer-readable justification.
- `expiresOn`: optional expiration date.
- `acceptedBy`: optional reviewer or automation name.

Validation rules:

- `scope` and `reason` are required.
- An exception never hides the diagnostic; it changes readiness interpretation
  and remains listed in summary/artifact output.
- Invalid or unmatched exceptions are developer-action diagnostics.

## Diagnostic Summary

Rollup for one run.

Fields:

- `runId`
- `status`: `ReadinessDiagnosticStatus`
- `countsBySeverity`
- `countsByCategory`
- `blockerCount`
- `unclassifiedCount`
- `reviewRequiredCount`
- `exceptionCount`
- `artifactPaths`
- `groups`: aggregated diagnostics
- `exceptions`
- `artifactWriteDiagnostics`

Validation rules:

- Counts are derived from groups and occurrences, not hand-authored.
- Summary status is derived through readiness evaluation rules.
- Artifact write failures are included as developer-action diagnostics.

## Readiness Diagnostic Status

Readiness interpretation derived from diagnostics.

Values:

- `accepted`: all diagnostics are classified and no blocker/review-required
  diagnostics remain.
- `blocked`: at least one unexcepted readiness blocker exists.
- `review-required`: at least one unclassified/partially classified diagnostic,
  invalid exception, or review-required developer-action warning exists.
- `environment-limited`: no blockers exist, but accepted environment
  limitations prevent full live readiness.

State transitions:

- `collecting` -> `summarized`: diagnostics are aggregated into a summary.
- `summarized` -> `review-required`: any missing category/severity or invalid
  exception is present.
- `summarized` -> `blocked`: any unexcepted blocker is present.
- `summarized` -> `environment-limited`: only accepted environment limitations
  prevent accepted readiness.
- `summarized` -> `accepted`: all diagnostics are classified and non-blocking.
- `artifact-write-failed` -> `summarized`: failure is added as a
  developer-action warning before final evaluation.
