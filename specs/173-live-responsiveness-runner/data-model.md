# Data Model: Live Responsiveness Runner

## Live Runner Request

Represents a maintainer request to collect responsiveness evidence.

**Fields**

- `script`: stable script id; initial accepted value is `representative`.
- `scope`: `all-interactive` or a specific page id.
- `theme`: `light` or `dark`.
- `outputRoot`: directory where the run-id child directory is created.
- `requireLive`: true when accepted live evidence is required.
- `printJson`: true when the command prints a machine-readable summary pointer.
- `timeout`: maximum allowed run duration when live automation is active.

**Validation Rules**

- `--page` and `--all-interactive` are mutually exclusive.
- Unknown theme, page, or script values fail before the run starts.
- Accepted readiness requires `requireLive = true` and measured live output.
- The output root must be writable before claiming the run has started.

## Live Responsiveness Review

Represents one visible desktop review run for a theme and interaction scope.

**Fields**

- `runId`: stable `resp-...` identifier.
- `scope`: e.g. `second-antshowcase/all-interactive/light`.
- `startedUtc`, `completedUtc`: run boundaries.
- `desktopSessionStatus`: `measured`, `no-visible-surface`, `hidden`, `minimized`, `not-focusable`, `missing-boundary`, `low-precision-timestamp`, `non-monotonic-timestamp`, `timeout`, `write-failed`, or `failed`.
- `presentationBoundaryStatus`: `measured`, `missing-boundary`, `unreliable`, or `not-run`.
- `overallReadiness`: `accepted`, `rejected`, `blocked`, `failed`, or `environment-limited`.
- `artifactsRoot`: run directory.
- `environmentLimitations`: blocking or caveat tokens.
- `diagnostics`: actionable messages.

**Validation Rules**

- `accepted` requires measured desktop session and measured presentation boundary.
- Any missing/unreliable live prerequisite keeps readiness non-accepted.
- `completedUtc` must be present for completed, blocked, rejected, failed, or environment-limited summaries.

**State Transitions**

`not-started -> initializing -> live-window-opening -> exercising-actions -> writing-artifacts -> accepted | rejected | blocked | environment-limited | failed`

## Representative Interaction

Represents one action derived from `InteractionContracts.all` or one display-only exclusion.

**Fields**

- `actionId`: unique id inside the run.
- `contractId`: source interaction contract id.
- `pageId`: page containing the representative control.
- `controlFamily`: family such as `button-click`, `slider-rating`, `navigation`, or `disclosure`.
- `controlIds`: covered catalog controls.
- `actionType`: `click`, `drag`, `select`, `navigate`, `open-close`, `value-change`, or `excluded`.
- `inputKind`: `pointer-discrete`, `pointer-move`, `key-down`, `wheel`, or `none`.
- `expectedVisibleResult`: reviewer-facing expected feedback.
- `displayOnlyReason`: reason for exclusions.

**Validation Rules**

- Timed interactions require `controlIds`, `pageId`, `inputKind`, and `expectedVisibleResult`.
- Display-only exclusions require `displayOnlyReason` and are not timed failures.
- Every `InteractionContracts.all` family must produce a measured record or missing-family diagnostic.

## Live Interaction Execution

Represents the runtime attempt to perform one representative interaction.

**Fields**

- `actionId`: representative interaction id.
- `targetResolution`: `resolved`, `missing-target`, `ambiguous-target`, or `not-run`.
- `inputSequenceIds`: viewer input sequence ids produced by the action.
- `startedAt`, `inputReceivedAt`, `visibleResponseAt`: timing boundaries when available.
- `presentedFrameId`: visible frame id when measured.
- `observedVisibleResult`: reviewer-facing observed result.
- `executionStatus`: `measured`, `no-visible-response`, `missing-boundary`, `target-missing`, `timeout`, `failed`, or `not-run`.

**Validation Rules**

- Accepted records require resolved target, measured input receipt, measured presentation boundary, and observed visible result matching the expected result.
- Target lookup or activation failures create non-accepted records with diagnostics.

## Drag Continuity Evidence

Represents continuity evidence for value-changing pointer movement.

**Fields**

- `actionId`: drag interaction id.
- `sampleCount`: number of pointer samples considered.
- `visibleFeedbackSamples`: number of samples with observed visible movement/value response.
- `maxSampleGapMs`: largest measured gap between visible feedback samples.
- `delayedCatchUp`: true when final value changes only after stalled feedback.
- `classification`: `continuous`, `delayed-catch-up`, `insufficient-samples`, `missing-boundary`, or `failed`.

**Validation Rules**

- Accepted drag records require `classification = continuous`.
- `delayed-catch-up`, insufficient samples, missing boundary, or failed classification rejects the drag evidence.

## Measured Interaction Record

Represents the persisted evidence for one representative interaction.

**Fields**

- Viewer latency fields: `recordId`, `runId`, `inputSequenceId`, `inputKind`, `inputName`, `receiptTimestamp`, queue depths, coalesced movement count, product/runtime state-change facts, `visibleResponse`, `presentedFrameId`, `environmentStatus`, `phaseTiming`, dirty-region summary, `longFrame`, and diagnostics.
- Sample fields: `page`, `controlFamily`, `controlIds`, `actionType`, `expectedVisibleResult`, `observedVisibleResult`, `acceptanceStatus`, and optional `dragContinuity`.

**Validation Rules**

- `acceptanceStatus = accepted` requires `environmentStatus = measured`, `visibleResponse = presented-frame`, `phaseTiming.totalInputToVisibleMs` present, budget pass, expected result observed, and no blocking diagnostics.
- Records over 150 ms are rejected.
- Records with missing live timing are blocked or environment-limited.
- A write-failed record cannot be accepted.

## Run Summary

Represents the aggregate readiness artifact.

**Fields**

- `runId`, `scope`, `startedUtc`, `completedUtc`.
- `overallReadiness`: `accepted`, `rejected`, `blocked`, `failed`, or `environment-limited`.
- `recordsPath`: relative path to `records.jsonl`.
- `budgets`: `inputReceiptP95Ms`, `inputReceiptMaxMs`, `inputToVisibleP95Ms`, `inputToVisibleMaxMs`, and `longFrameThresholdMs`.
- `coverage`: required families, accepted families, rejected families, blocked families, missing families, and display-only exclusions.
- `groups`: per page/input/family p50, p95, max, long-frame count, and readiness.
- `firstFailedBudget`.
- `slowestInteractions`: five slowest interactions when measured data exists.
- `dragContinuity`: per value-changing family classification.
- `environmentLimitations`, `diagnostics`, and `artifactWriteStatus`.

**Validation Rules**

- `summary.md` and `summary.json` must agree on readiness, record count, coverage counts, first failed budget, slowest interactions, limitations, and artifact-write status.
- Accepted readiness requires every required interactive family accepted or explicitly excluded as display-only.
- Invalid, partial, or inconsistent summary output is `failed`.

## Environment Limitation

Represents a non-acceptance reason caused by the host or evidence infrastructure.

**Fields**

- `code`: stable token.
- `stage`: `desktop-prerequisite`, `window`, `input`, `presentation`, `timing`, `artifact-write`, or `timeout`.
- `message`: actionable maintainer text.
- `blocking`: true when it prevents accepted readiness.
- `diagnosticArtifact`: optional relative path.

**Validation Rules**

- Blocking environment limitations always keep readiness non-accepted.
- Substitute or headless evidence must be represented by an environment limitation.

## Validation Package

Represents the final feature evidence bundle.

**Fields**

- `responsivenessRuns`: live light/dark run summary paths.
- `deterministicTests`: framework and sample test results.
- `coverageEvidence`: coverage command output.
- `visualEvidence`: preferred/minimum visual readiness output.
- `manualReviewStatus`: `accepted`, `blocked`, `manual-review-pending`, or `not-run`.
- `caveats`: timed-out, skipped, blocked, degraded, substitute, or environment-limited checks.

**Validation Rules**

- Final readiness cannot summarize timed-out, blocked, environment-limited, substitute, skipped, degraded, or manual-review-pending checks as green.
- Existing showcase regression checks must either pass or appear as visible caveats.
