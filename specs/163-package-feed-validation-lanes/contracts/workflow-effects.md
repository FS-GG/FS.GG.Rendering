# Contract: Workflow Effects

## Purpose

Package-feed refresh/proof and validation-lane execution are stateful I/O workflows. The
implementation must keep decisions in pure model/message/update logic and execute filesystem,
NuGet, process, timer, and diagnostic work at the edge.

## Package Feed Workflow

### Model

- Repository root.
- Selected samples.
- Local feed path.
- Isolated cache path.
- Cold-proof settings.
- Discovered packable packages.
- Sample package pins.
- Compatibility exceptions.
- Feed package status.
- Source rules.
- Proof status.
- Evidence paths.

### Messages

- `DiscoverPackagesRequested`
- `PackagesDiscovered`
- `SamplePinsRead`
- `LocalFeedChecked`
- `PinsRefreshRequested`
- `PinsRefreshed`
- `SourceProofRequested`
- `RestoreCompleted`
- `SourceProofClassified`
- `EvidenceWritten`
- `WorkflowFailed`

### Effects

- `ReadProjectFiles`
- `ReadSampleProjects`
- `PackLocalFeed`
- `WriteSamplePins`
- `CheckLocalFeed`
- `CreateGeneratedNuGetConfig`
- `RunRestore`
- `ReadRestoreAssets`
- `WritePackageEvidence`

### Rules

- `update` never reads files, starts processes, or writes evidence.
- Stale pins transition to failure unless refresh or accepted exception is requested.
- Source proof cannot start until package discovery, sample pin read, and feed check are complete.
- Destructive global cache clearing is an edge effect that requires explicit cold mode.

## Validation Lane Workflow

### Model

- Lane definitions.
- Running lanes.
- Completed lane results.
- Canceled lanes.
- Timeout and no-progress policies.
- Output roots.
- Summary status.
- Diagnostics paths.

### Messages

- `RunRequested`
- `LaneStarted`
- `LaneOutputReceived`
- `LaneCompleted`
- `LaneTimedOut`
- `LaneNoProgressDetected`
- `LaneCanceled`
- `LaneDiagnosticsCaptured`
- `SummaryRequested`
- `SummaryWritten`
- `RunnerFailed`

### Effects

- `StartProcess`
- `AppendLaneLog`
- `PollProcess`
- `StopProcess`
- `CaptureDiagnostics`
- `WriteLaneResult`
- `WriteLaneSummary`
- `CreateOutputRoot`

### Rules

- A lane can start only when its output root does not conflict with running lanes.
- Timeout and no-progress classification happens in pure transition logic from timestamps and lane
  state.
- Process termination and diagnostics are edge effects requested by `update`.
- A canceled/timed-out/hung lane never transitions to `passed`.
- Summary computation uses recorded lane results and package proof status only; it does not infer
  success from absent artifacts.

## Interpreter Requirements

- Record every external command before execution.
- Preserve stdout/stderr logs for failed, timed-out, hung, and canceled lanes.
- Use project-relative paths in evidence when possible.
- Convert OS/process/NuGet exceptions into explicit workflow failure messages.
- Never swallow critical failures; write an incomplete or blocked summary instead.
