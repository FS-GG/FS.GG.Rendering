# Contract: Runtime Diagnostics API

## Package

New public package:

```text
src/Diagnostics/
|-- Diagnostics.fsproj
|-- Diagnostics.fsi
`-- Diagnostics.fs
```

Package identity: `FS.GG.UI.Diagnostics`.

The package is dependency-light and must not depend on SkiaViewer, Controls,
Controls.Elmish, Testing, or the validation harness.

## Public Surface Shape

The exact implementation may refine naming during `.fsi` work, but the public
contract must expose these concepts:

```fsharp
namespace FS.GG.UI.Diagnostics

type DiagnosticSeverity =
    | Informational
    | Warning
    | Error

type DiagnosticCategory =
    | Environment
    | BackendCost
    | RenderingLimitation
    | ReadinessBlocker
    | DeveloperAction

type DiagnosticReadinessImpact =
    | NonBlocking
    | BlocksReadiness
    | RequiresReview
    | EnvironmentLimited

type ReadinessDiagnosticStatus =
    | Accepted
    | Blocked
    | ReviewRequired
    | EnvironmentLimitedStatus

type DiagnosticSource =
    { PackageId: string option
      Subsystem: string
      LaneId: string option
      SampleId: string option }

type DiagnosticContext =
    { RunId: string option
      TimestampUtc: System.DateTime option
      OutputPath: string option
      Details: (string * string) list }

type RuntimeDiagnostic =
    { Id: string
      Source: DiagnosticSource
      Code: string option
      Severity: DiagnosticSeverity option
      Category: DiagnosticCategory option
      Message: string
      Action: string option
      Context: DiagnosticContext
      Fingerprint: string }

type DiagnosticException =
    { ExceptionId: string
      Scope: string
      Reason: string
      ExpiresOn: System.DateOnly option
      AcceptedBy: string option }

type AggregatedDiagnostic =
    { Fingerprint: string
      Source: DiagnosticSource
      Code: string option
      Severity: DiagnosticSeverity option
      Category: DiagnosticCategory option
      Message: string
      Action: string option
      OccurrenceCount: int
      FirstOccurrence: DiagnosticContext
      LastOccurrence: DiagnosticContext
      ExampleIds: string list }

type DiagnosticSummary =
    { RunId: string option
      Status: ReadinessDiagnosticStatus
      CountsBySeverity: (DiagnosticSeverity * int) list
      CountsByCategory: (DiagnosticCategory * int) list
      BlockerCount: int
      UnclassifiedCount: int
      ReviewRequiredCount: int
      ExceptionCount: int
      ArtifactPaths: string list
      Groups: AggregatedDiagnostic list
      Exceptions: DiagnosticException list
      ArtifactWriteDiagnostics: RuntimeDiagnostic list }

module RuntimeDiagnostics =
    val source:
        packageId: string option ->
        subsystem: string ->
        laneId: string option ->
        sampleId: string option ->
            DiagnosticSource

    val context:
        runId: string option ->
        timestampUtc: System.DateTime option ->
        outputPath: string option ->
        details: (string * string) list ->
            DiagnosticContext

    val create:
        source: DiagnosticSource ->
        code: string option ->
        severity: DiagnosticSeverity option ->
        category: DiagnosticCategory option ->
        message: string ->
        action: string option ->
        context: DiagnosticContext ->
            RuntimeDiagnostic

    val aggregate: diagnostics: RuntimeDiagnostic list -> AggregatedDiagnostic list

    val summarize:
        runId: string option ->
        exceptions: DiagnosticException list ->
        artifactPaths: string list ->
        diagnostics: RuntimeDiagnostic list ->
            DiagnosticSummary

    val renderMarkdown: summary: DiagnosticSummary -> string

    val renderJson: summary: DiagnosticSummary -> string

    val renderConsole:
        verbose: bool ->
        maxDefaultLines: int ->
        summary: DiagnosticSummary ->
            string list
```

## Producer Adapters

Adapters stay near existing producers and are additive:

```fsharp
namespace FS.GG.UI.Controls

module Diagnostics =
    val toRuntimeDiagnostic:
        context: FS.GG.UI.Diagnostics.DiagnosticContext ->
        diagnostic: ControlDiagnostic ->
            FS.GG.UI.Diagnostics.RuntimeDiagnostic
```

```fsharp
namespace FS.GG.UI.SkiaViewer.Host

module Diagnostics =
    val toRuntimeDiagnostic:
        context: FS.GG.UI.Diagnostics.DiagnosticContext ->
        diagnostic: RenderDiagnostic ->
            FS.GG.UI.Diagnostics.RuntimeDiagnostic
```

```fsharp
namespace FS.GG.UI.Controls.Elmish

module ControlsElmish =
    val adapterDiagnosticToRuntimeDiagnostic:
        context: FS.GG.UI.Diagnostics.DiagnosticContext ->
        diagnostic: AdapterDiagnostic ->
            FS.GG.UI.Diagnostics.RuntimeDiagnostic
```

## Compatibility Rules

- Existing diagnostic constructors and records remain source-compatible unless a
  task documents a necessary exception.
- Existing messages remain semantically equivalent; changed category/severity
  mapping must be listed in migration notes and tests.
- New public package and adapter functions require surface-baseline updates.
- No runtime package may depend on `FS.GG.UI.Testing` to emit diagnostics.

## Classification Rules

- `backend-cost` diagnostics default to `informational` and `non-blocking`.
- Expected environment diagnostics default to visible non-blocking or
  `environment-limited` when they prevent live evidence.
- `readiness-blocker` diagnostics derive `blocks-readiness`.
- Missing severity or category derives `requires-review`.
- Artifact write failures produce `developer-action` warnings.
