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

    /// A repository-owned artifact that a skill's process guidance must point at. Both cases are
    /// adjudicated by a closed world — the filesystem, or the harness dispatch table — never by the
    /// skill's vocabulary.
    type ArtifactRef =
        /// Resolves iff the path exists beneath the repository root.
        | RepoPath of path: string
        /// Resolves iff `tools/Rendering.Harness/Cli.fs` still dispatches the verb.
        | HarnessCommand of verb: string

    /// Verdict for one `GuardedTheme` against one skill in its scope.
    type ArtifactStatus =
        /// The skill names an artifact of the theme, and that artifact exists.
        | ArtifactResolved
        /// The skill names an artifact of the theme, and it no longer exists — the guidance is stale.
        | ArtifactDangling
        /// The skill is in the theme's scope but names none of its artifacts — the guidance is gone.
        | ArtifactUnnamed

    /// A process-guidance theme retained from the deleted substring-matched guidance layer, narrowed to
    /// the artifacts it can actually resolve. Themes whose prose named no repository artifact were not
    /// retained; see `specs/235-gate-cadence-from-slnx/guidance-rule-disposition.md`.
    type GuardedTheme =
        { ThemeId: string
          Intent: string
          Artifacts: ArtifactRef list
          ApplicablePatterns: string list }

    /// One `GuardedTheme` resolved against one in-scope skill.
    type ArtifactReference =
        { ThemeId: string
          /// The theme's `Intent`, carried so a finding can say what the missing guidance was for.
          Intent: string
          SkillName: string
          SurfaceId: string
          Path: string
          /// The artifact the skill named — `None` exactly when `Status` is `ArtifactUnnamed`.
          Reference: ArtifactRef option
          Expected: ArtifactRef list
          Status: ArtifactStatus }

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
        | UnresolvedArtifactReference
        | MissingRequiredArtifact
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

    type ThemeArtifactSummary =
        { ThemeId: string
          Scoped: int
          Resolved: int
          Dangling: int
          Unnamed: int }

    type ParityReport =
        { CheckedAtUtc: DateTime
          RepositoryRoot: string
          OverallStatus: OverallStatus
          SupportedSurfaces: SkillSurface list
          CanonicalSourceCount: int
          WrapperCount: int
          FindingCountsBySeverity: SeverityCounts
          ApiSymbolCoverage: SkillSymbolSummary list
          GuardedThemeCoverage: ThemeArtifactSummary list
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
          Artifacts: ArtifactReference list
          Report: ParityReport option
          Diagnostics: string list }

    type Msg =
        | InventoryRequested
        | InventoryLoaded of SkillSurface list * SkillEntry list
        | SymbolsResolved of ApiSymbol list
        | ArtifactsResolved of ArtifactReference list
        | FindingsClassified of ParityFinding list
        | ReportGenerated of ParityReport
        | WorkflowFailed of string

    type Effect =
        | ReadSkillSurfaces
        | ResolveApiSymbols
        | ResolveArtifactReferences
        | ClassifyFindings
        | WriteMarkdownReport
        | WriteSummaryJson

    val surfaceKindToken: kind: SurfaceKind -> string

    val agentToken: agent: AgentSurface -> string

    val entryKindToken: kind: EntryKind -> string

    val symbolStatusToken: status: SymbolStatus -> string

    val artifactStatusToken: status: ArtifactStatus -> string

    /// The text a skill must name to point at this artifact.
    val artifactRefToken: reference: ArtifactRef -> string

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

    /// The process-guidance themes that survived the deleted substring layer, each narrowed to the
    /// repository artifacts it can resolve.
    val defaultGuardedThemes: unit -> GuardedTheme list

    /// Every verb `tools/Rendering.Harness/Cli.fs` dispatches. This is the closed world for
    /// `HarnessCommand`. `None` when the dispatch table is absent, so callers degrade instead of
    /// reporting a false green.
    val loadHarnessCommands: repositoryRoot: string -> Set<string> option

    /// Resolve each guarded theme against the skills in its scope. A skill satisfies a theme only by
    /// naming an artifact that resolves — naming the right words satisfies nothing.
    val evaluateArtifactReferences:
        repositoryRoot: string ->
        harnessCommands: Set<string> ->
        themes: GuardedTheme list ->
        entries: SkillEntry list ->
            ArtifactReference list

    val runCheck: request: ParityCheckRequest -> ParityReport

    val renderMarkdown: report: ParityReport -> string

    val renderSummaryJson: report: ParityReport -> string

    val writeReport: request: ParityCheckRequest -> report: ParityReport -> string list

    val createFixture: root: string -> fixtureName: string -> unit

    val init: request: ParityCheckRequest -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    val runCli: argv: string list -> int
