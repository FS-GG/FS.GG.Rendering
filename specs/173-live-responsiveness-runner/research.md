# Research: Live Responsiveness Runner

## Decision: Extend the existing `responsiveness` command as the live acceptance path

**Rationale**: `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs` already owns parsing, output roots, all-interactive scope, substitute records, and the Feature 172 evidence fields. Extending this command keeps maintainer workflow stable and makes `--require-live --all-interactive` the explicit accepted path.

**Alternatives considered**: Adding a separate command was rejected because it would duplicate parser and artifact contracts. Treating the current headless substitute command as accepted was rejected because the feature exists specifically to replace substitute acceptance with measured live evidence.

## Decision: Reuse `SkiaViewer` responsiveness records and writers

**Rationale**: `FS.GG.UI.SkiaViewer` already exposes stable input-kind, visible-response, environment-status, readiness tokens, `ViewerLatencyRecord`, `ViewerResponsivenessSummary`, budget aggregation, JSONL encoding, summary JSON/Markdown encoding, and run writers. The live runner should consume or adapt those records instead of inventing a second timing schema.

**Alternatives considered**: Writing sample-only ad hoc JSON was rejected because it would bypass existing package tests and summary rules. Adding a telemetry/tracing dependency was rejected because the repository already has the needed timing primitives and artifact writers.

## Decision: Layer sample review fields over viewer latency evidence

**Rationale**: Maintainers need page, control family, action type, covered control ids, expected visible result, observed visible result, and acceptance status. Feature 172 already added this sample evidence vocabulary in `SecondAntShowcase.Core.Evidence`; the live runner can keep those fields while replacing the headless timing status with measured live presentation facts.

**Alternatives considered**: Changing `ViewerLatencyRecord` to include sample-only fields was rejected as unnecessary public framework churn. Keeping the fields only in Markdown was rejected because budget checks and coverage need machine-readable data.

## Decision: Use `InteractionContracts.all` and `displayOnlyReasons` as the coverage source

**Rationale**: The sample already maps interactive families to controls, pages, input kinds, action types, expected state changes, and display-only reasons. Using these contracts satisfies all-interactive family coverage while keeping the run bounded and reviewable.

**Alternatives considered**: Timing only a few manually chosen controls was rejected because it would miss required families such as navigation, disclosure, selection, and value-changing controls. Timing every catalog control individually was rejected because display-only controls are intentionally excluded and representative family evidence is the accepted scope.

## Decision: Accepted readiness requires a visible presentation boundary

**Rationale**: The spec requires real mouse/keyboard interactions visibly responding within budget. A record can be accepted only when the environment status is measured, the visible response is a presented frame, timestamps are reliable, and `totalInputToVisibleMs` is present.

**Alternatives considered**: Using deterministic `ControlsElmish.Perf.runScript` timings was rejected because they are valuable regression shape evidence but do not measure desktop presentation. Treating missing presentation timing as zero was rejected as false evidence.

## Decision: Fail closed for missing prerequisites and artifact write failures

**Rationale**: Missing visible session, hidden/minimized/unfocusable window, no presentation boundary, low-precision or non-monotonic timestamps, timeout, target lookup failure, incomplete coverage, and write failure all undermine acceptance evidence. The command must still write diagnostics when possible, but exit non-zero and keep readiness non-accepted.

**Alternatives considered**: Skipping unavailable records and accepting the rest was rejected because FR-002 and SC-001 require complete family coverage or explicit missing-family diagnostics. Printing only console diagnostics was rejected because maintainers need durable artifacts.

## Decision: Use feature budgets of p95 <= 100 ms and max <= 150 ms

**Rationale**: Feature 173 keeps the user-facing review target from Feature 172 and the spec: at least 95% of representative interactions must visibly respond within 100 ms, and no accepted action may exceed 150 ms. Lower-level viewer receipt and long-frame budgets remain useful diagnostics but do not replace these acceptance thresholds.

**Alternatives considered**: Using the Feature 167 default 50 ms input-to-visible budget was rejected for this sample acceptance path because the current feature explicitly states 100 ms p95 and 150 ms max. Dropping max latency was rejected because SC-003 requires no accepted action above 150 ms.

## Decision: Drag continuity is an explicit acceptance fact

**Rationale**: Value-changing controls can appear to pass a first-response budget while still visibly catching up after the pointer moves. Drag actions must record movement samples or classifications that prove continuous visible feedback rather than delayed catch-up.

**Alternatives considered**: Treating a final value change as sufficient was rejected because FR-006 and SC-004 require continuous feedback, not just eventual state convergence.

## Decision: Preserve existing showcase validation as part of readiness

**Rationale**: Live runner work touches interaction orchestration, retained routing, sample evidence, and possibly framework timing hooks. The final package must rerun coverage, slider/rating behavior, navigation/overlay behavior, visual readiness, and prior deterministic interaction tests so the responsiveness fix does not hide regressions.

**Alternatives considered**: Running only the live responsiveness command was rejected because the feature requires preserving existing showcase behavior and visual readiness.
