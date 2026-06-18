# Contract: Lane Workflow Effects

## Scope

Feature 161 evidence collection is stateful and I/O-bearing. Decision logic must remain pure, and
process, display, GL, package, load, and filesystem work must run through edge interpreters.

## Model

The workflow model owns:

- Run id.
- Source throughput path.
- Expected host profile.
- Active host profile.
- Lane id.
- Policy id.
- Host fact set.
- Prior gate links.
- Ledger entries.
- Excluded evidence.
- Unsupported-host record.
- Published artifacts.
- Full validation record.
- Final lane status.
- Performance claim status.
- Diagnostics.

## Messages

Workflow messages include:

- `SourceTimingDiscovered`
- `HostProfileDetected`
- `HostFactsCollected`
- `HostFactsRejected`
- `LaneDeclared`
- `PolicyDeclared`
- `PriorGateLinked`
- `LedgerEntryAccepted`
- `LedgerEntryExcluded`
- `UnsupportedHostRecorded`
- `ClaimScopeRendered`
- `FullValidationRecorded`
- `ArtifactPublished`
- `SummaryPublished`
- `DiagnosticRecorded`

## Effects

Workflow effects include:

- Discover source timing evidence.
- Detect host profile.
- Collect display facts.
- Collect renderer and direct-rendering facts.
- Collect refresh facts or unavailable reason.
- Collect driver facts.
- Collect package version set.
- Collect CPU/GPU load notes.
- Link prior P7 gate evidence.
- Classify ledger entry.
- Write host fact artifact.
- Write ledger entry artifact.
- Write excluded evidence artifact.
- Write unsupported-host artifact.
- Write claim-scope summary.
- Write full-validation record.
- Write compatibility, package, regression, and readiness summaries.

## Edge Interpreter Rules

- `update` remains pure: it transforms model and message into next model plus requested effects.
- Native window, GL, display probing, process execution, load sampling, package inspection, timers,
  and filesystem writes happen only in interpreters.
- Interpreters return messages with enough detail to fail closed on missing display, unknown
  renderer, indirect rendering, software rasterization, ambiguous GPU, missing load notes, stale
  package versions, cross-lane evidence, artifact write failure, or full-validation failure.
- No effect may silently convert incomplete host facts into accepted lane-scoped evidence.
- Synthetic fixtures may be used only for rejection tests and must carry `Synthetic` in the test
  name and source comment.

## Terminal States

- `accepted`: host-lane facts are complete and scoped for the claimed lane.
- `rejected`: invalid, partial, stale, mixed, or unverifiable evidence prevents lane acceptance.
- `fallback-only`: safe fallback is verified but no accepted lane-scoped performance evidence is
  available.
- `environment-limited`: host or presentation environment prevented comparable evidence and zero
  accepted lane-scoped performance artifacts were recorded.
- `blocked`: lane facts are complete but prior gates, full validation, compatibility, package, or
  noisy timing blocks claim or release readiness.

## Acceptance Tests

- Complete current-lane facts transition to accepted lane status while preserving final claim
  boundary.
- Missing renderer facts transition to excluded evidence with zero accepted artifacts.
- Cross-lane timing records transition to rejected or split evidence, never accepted aggregation.
- Unsupported host transitions to `environment-limited` and emits write effects for limitation
  artifacts.
- Noisy timing with complete lane facts keeps `performance-not-accepted`.
