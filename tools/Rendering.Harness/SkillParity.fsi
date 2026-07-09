namespace Rendering.Harness

open System

/// Non-destructive repository skill parity and evidence-guidance reporting.
module SkillParity =

    type SurfaceKind =
        | Canonical
        | Wrapper
        | Mixed
        | Command

    type AgentSurface =
        | Codex
        | Claude
        | GeneratedProduct
        | Package
        | SpecKit
        | Repository

    type EntryKind =
        | CanonicalEntry
        | WrapperEntry
        | CommandEntry
        | WrapperOnlyEntry

    /// Verdict for one `Module.member` an FS.GG skill documents inside an F# code fence.
    type SymbolStatus =
        /// Present in the public surface baseline, and called by at least one test source. This says
        /// a test names the API in code — not that the test asserts anything meaningful about it.
        | Exercised
        /// Present in the public surface baseline, but no test calls it — a seam that may be dead.
        | Unexercised
        /// Absent from the public surface baseline — the skill documents an API that does not exist.
        | Unresolved

    type FindingSeverity =
        | Info
        | Warning
        | High
        | Critical

    type FindingCategory =
        | MissingWrapper
        | WrapperOnly
        | StaleDescription
        | BrokenTarget
        | CanonicalDrift
        | UnresolvedApiSymbol
        | UnexercisedApiSymbol
        | MetadataDrift
        | IntentionalExceptionFinding
        | UnreadableSurface

    type OverallStatus =
        | Passed
        | WarningStatus
        | Failed

    type SkillSurface =
        { SurfaceId: string
          DisplayName: string
          RootPath: string
          Kind: SurfaceKind
          Agent: AgentSurface
          IsRequired: bool
          Notes: string list }

    type WrapperTarget =
        { RawTarget: string
          ResolvedPath: string
          Exists: bool
          CanonicalSkillName: string option
          CanonicalDescription: string option
          TargetHash: string option }

    type SkillEntry =
        { SkillName: string
          Description: string
          Path: string
          AbsolutePath: string
          SurfaceId: string
          EntryKind: EntryKind
          Metadata: Map<string, string>
          BodyHash: string
          Content: string
          WrapperTarget: WrapperTarget option }

    /// One `Module.member` a skill documents, resolved against the surface baseline and the test corpus.
    type ApiSymbol =
        { Symbol: string
          SkillName: string
          SurfaceId: string
          Path: string
          Status: SymbolStatus }

    type IntentionalException =
        { ExceptionId: string
          SkillName: string
          SurfaceId: string
          Category: string
          Reason: string
          Owner: string
          ReviewDate: string
          Scope: string }

    type ParityFinding =
        { FindingId: string
          SkillName: string
          SurfaceId: string
          Category: FindingCategory
          Severity: FindingSeverity
          CanonicalPath: string option
          WrapperPath: string option
          Symbol: string option
          Message: string
          Remediation: string
          ExceptionId: string option }

    type SeverityCounts =
        { Critical: int
          High: int
          Warning: int
          Info: int }

    type SkillSymbolSummary =
        { SkillName: string
          Documented: int
          Exercised: int
          Unexercised: int
          Unresolved: int }

    type ParityReport =
        { CheckedAtUtc: DateTime
          RepositoryRoot: string
          OverallStatus: OverallStatus
          SupportedSurfaces: SkillSurface list
          CanonicalSourceCount: int
          WrapperCount: int
          FindingCountsBySeverity: SeverityCounts
          ApiSymbolCoverage: SkillSymbolSummary list
          Findings: ParityFinding list
          IntentionalExceptions: IntentionalException list
          GeneratedReportPath: string
          StructuredSummaryPath: string
          Caveats: string list
          Command: string }

    type ParityCheckRequest =
        { RepositoryRoot: string
          OutDir: string
          ReportPath: string
          SummaryJsonPath: string
          FixtureMode: string option
          SurfaceOverrides: (string * string) list
          AllowedExceptionIds: Set<string>
          FailOnSeverity: FindingSeverity
          ListSymbolsOnly: bool
          JsonOutput: bool }

    type Model =
        { Request: ParityCheckRequest
          Surfaces: SkillSurface list
          Entries: SkillEntry list
          Findings: ParityFinding list
          Symbols: ApiSymbol list
          Report: ParityReport option
          Diagnostics: string list }

    type Msg =
        | InventoryRequested
        | InventoryLoaded of SkillSurface list * SkillEntry list
        | SymbolsResolved of ApiSymbol list
        | FindingsClassified of ParityFinding list
        | ReportGenerated of ParityReport
        | WorkflowFailed of string

    type Effect =
        | ReadSkillSurfaces
        | ResolveApiSymbols
        | ClassifyFindings
        | WriteMarkdownReport
        | WriteSummaryJson

    val surfaceKindToken: kind: SurfaceKind -> string

    val agentToken: agent: AgentSurface -> string

    val entryKindToken: kind: EntryKind -> string

    val symbolStatusToken: status: SymbolStatus -> string

    val severityToken: severity: FindingSeverity -> string

    val categoryToken: category: FindingCategory -> string

    val overallStatusToken: status: OverallStatus -> string

    val defaultRequest: repositoryRoot: string -> ParityCheckRequest

    val parseFrontMatter: content: string -> Map<string, string> * string

    val discoverDefaultSurfaces: repositoryRoot: string -> SkillSurface list

    val inventorySkills: request: ParityCheckRequest -> surfaces: SkillSurface list -> SkillEntry list

    /// Declaring module -> its public member names, from `readiness/surface-baselines/members`.
    /// `None` when the baseline is absent, so callers can degrade instead of reporting a false green.
    val loadSurfaceMembers: repositoryRoot: string -> Map<string, Set<string>> option

    /// Every qualified `Module.member` a test source under `tests/` calls. Comments and string
    /// literals are stripped first. `None` when the test corpus is absent.
    val loadExercisedSymbols: repositoryRoot: string -> Set<string> option

    val evaluateApiSymbols:
        surfaceMembers: Map<string, Set<string>> ->
        exercised: Set<string> ->
        entries: SkillEntry list ->
            ApiSymbol list

    val runCheck: request: ParityCheckRequest -> ParityReport

    val renderMarkdown: report: ParityReport -> string

    val renderSummaryJson: report: ParityReport -> string

    val writeReport: request: ParityCheckRequest -> report: ParityReport -> string list

    val createFixture: root: string -> fixtureName: string -> unit

    val init: request: ParityCheckRequest -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    val runCli: argv: string list -> int
