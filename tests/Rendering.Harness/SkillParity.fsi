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

    type CoverageStatus =
        | Covered
        | Partial
        | Missing
        | NotApplicable
        | Excepted

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
        | GuidanceRuleGap
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

    type GuidanceRule =
        { RuleId: string
          Theme: string
          Description: string
          RequiredReferences: string list list
          ApplicablePatterns: string list
          MinimumCoverage: string }

    type GuidanceCoverage =
        { RuleId: string
          SkillName: string
          SurfaceId: string
          Path: string
          Status: CoverageStatus
          Evidence: string list
          MissingReferences: string list
          ExceptionId: string option }

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
          RuleId: string option
          Message: string
          Remediation: string
          ExceptionId: string option }

    type SeverityCounts =
        { Critical: int
          High: int
          Warning: int
          Info: int }

    type RuleCoverageSummary =
        { RuleId: string
          Covered: int
          Partial: int
          Missing: int
          Excepted: int
          NotApplicable: int }

    type ParityReport =
        { CheckedAtUtc: DateTime
          RepositoryRoot: string
          OverallStatus: OverallStatus
          SupportedSurfaces: SkillSurface list
          CanonicalSourceCount: int
          WrapperCount: int
          FindingCountsBySeverity: SeverityCounts
          GuidanceRuleCoverage: RuleCoverageSummary list
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
          ListRulesOnly: bool
          JsonOutput: bool }

    type Model =
        { Request: ParityCheckRequest
          Surfaces: SkillSurface list
          Entries: SkillEntry list
          Findings: ParityFinding list
          Coverage: GuidanceCoverage list
          Report: ParityReport option
          Diagnostics: string list }

    type Msg =
        | InventoryRequested
        | InventoryLoaded of SkillSurface list * SkillEntry list
        | CoverageEvaluated of GuidanceCoverage list
        | FindingsClassified of ParityFinding list
        | ReportGenerated of ParityReport
        | WorkflowFailed of string

    type Effect =
        | ReadSkillSurfaces
        | EvaluateGuidanceRules
        | ClassifyFindings
        | WriteMarkdownReport
        | WriteSummaryJson

    val surfaceKindToken: kind: SurfaceKind -> string

    val agentToken: agent: AgentSurface -> string

    val entryKindToken: kind: EntryKind -> string

    val coverageToken: status: CoverageStatus -> string

    val severityToken: severity: FindingSeverity -> string

    val categoryToken: category: FindingCategory -> string

    val overallStatusToken: status: OverallStatus -> string

    val defaultGuidanceRules: unit -> GuidanceRule list

    val defaultRequest: repositoryRoot: string -> ParityCheckRequest

    val parseFrontMatter: content: string -> Map<string, string> * string

    val discoverDefaultSurfaces: repositoryRoot: string -> SkillSurface list

    val inventorySkills: request: ParityCheckRequest -> surfaces: SkillSurface list -> SkillEntry list

    val evaluateGuidanceCoverage: rules: GuidanceRule list -> entries: SkillEntry list -> GuidanceCoverage list

    val runCheck: request: ParityCheckRequest -> ParityReport

    val renderMarkdown: report: ParityReport -> string

    val renderSummaryJson: report: ParityReport -> string

    val writeReport: request: ParityCheckRequest -> report: ParityReport -> string list

    val createFixture: root: string -> fixtureName: string -> unit

    val init: request: ParityCheckRequest -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    val runCli: argv: string list -> int
