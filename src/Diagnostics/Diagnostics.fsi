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

    val severityToken: severity: DiagnosticSeverity -> string

    val categoryToken: category: DiagnosticCategory -> string

    val readinessStatusToken: status: ReadinessDiagnosticStatus -> string

    val tryParseReadinessStatus: token: string -> ReadinessDiagnosticStatus option

    val aggregate: diagnostics: RuntimeDiagnostic list -> AggregatedDiagnostic list

    val summarize:
        runId: string option ->
        exceptions: DiagnosticException list ->
        artifactPaths: string list ->
        diagnostics: RuntimeDiagnostic list ->
            DiagnosticSummary

    val renderMarkdown: summary: DiagnosticSummary -> string

    val renderJson: summary: DiagnosticSummary -> string

    val renderJsonLines: diagnostics: RuntimeDiagnostic list -> string

    val renderConsole:
        verbose: bool ->
        maxDefaultLines: int ->
        summary: DiagnosticSummary ->
            string list

    val writeArtifacts:
        outputDirectory: string ->
        runId: string option ->
        exceptions: DiagnosticException list ->
        diagnostics: RuntimeDiagnostic list ->
            DiagnosticSummary
