namespace Rendering.Harness

open System

/// Package-feed validation and source-proof workflow for package-consuming samples.
module PackageFeed =

    type PackagePinStatus =
        | Current
        | Stale
        | MissingExpectedPackage
        | CompatibilityException
        | NotSelected

    type ProofStatus =
        | Passed
        | Failed
        | EnvironmentLimited

    type PackageFeedMode =
        | Check
        | Refresh
        | Proof

    type PackablePackage =
        { PackageId: string
          Version: string
          ProjectPath: string
          IsPackable: bool
          PackageFilePath: string }

    type CompatibilityException =
        { Id: string
          PackageId: string
          DeclaredVersion: string
          ExpectedVersion: string
          SamplePath: string
          Reason: string
          Owner: string
          Review: string }

    type PackagePin =
        { PackageId: string
          DeclaredVersion: string
          ExpectedVersion: string option
          ProjectFilePath: string
          Status: PackagePinStatus
          CompatibilityExceptionId: string option }

    type FeedPackageStatus =
        { PackageId: string
          Version: string
          PackageFilePath: string
          Present: bool }

    type SourceRule =
        { RuleId: string
          PackagePattern: string
          AllowedSources: string list }

    type SourceProof =
        { Status: ProofStatus
          FeedPath: string
          CachePath: string
          GlobalCacheCleared: bool
          SelectedSamples: string list
          SourceRules: SourceRule list
          RestoreCommand: string option
          RestoreLogPath: string option
          /// `None` when the proof short-circuited before compiling the consumers (a stale pin or a
          /// missing feed package), so "did not build" is never mistaken for "built clean".
          BuildLogPath: string option
          AssetsFiles: string list
          Violations: string list }

    type PackageFeedOptions =
        { RepositoryRoot: string
          SelectedSamples: string list
          FeedPath: string
          OutDir: string
          Mode: PackageFeedMode
          PackBeforeCheck: bool
          IsolatedCachePath: string option
          Cold: bool
          ClearGlobalCache: bool
          AllowedExceptionIds: Set<string>
          CompatibilityExceptions: CompatibilityException list }

    type PackageFeedResult =
        { Status: ProofStatus
          CurrentPackages: PackablePackage list
          PackagePins: PackagePin list
          FeedPackages: FeedPackageStatus list
          ChangedFiles: string list
          SourceProof: SourceProof option
          EvidenceFiles: string list
          Diagnostics: string list }

    type Model =
        { RepositoryRoot: string
          SelectedSamples: string list
          FeedPath: string
          CurrentPackages: PackablePackage list
          PackagePins: PackagePin list
          FeedPackages: FeedPackageStatus list
          Proof: SourceProof option
          Status: ProofStatus option
          Diagnostics: string list }

    type Msg =
        | DiscoverPackagesRequested
        | PackagesDiscovered of PackablePackage list
        | SamplePinsRead of PackagePin list
        | LocalFeedChecked of FeedPackageStatus list
        | PinsRefreshRequested
        | PinsRefreshed of changedFiles: string list
        | SourceProofRequested
        | SourceProofClassified of SourceProof
        | EvidenceWritten of paths: string list
        | WorkflowFailed of reason: string

    type Effect =
        | ReadProjectFiles
        | ReadSampleProjects
        | PackLocalFeed
        | WriteSamplePins
        | CheckLocalFeed
        | CreateGeneratedNuGetConfig
        | RunRestore
        | BuildSampleProjects
        | ReadRestoreAssets
        | WritePackageEvidence

    /// A project under `src/` that declares itself packable but cannot be read as one — an
    /// unparseable/unreadable `.fsproj`, a packable project whose `<Version>` resolves to nothing, or
    /// a repository root with no `src/` at all. Discovery RAISES rather than omitting: the discovered
    /// set IS the expected-feed set, so a project that silently leaves it is a package the feed check
    /// stops looking for (#677 — the fails-open class of FS-GG/.github#266).
    exception PackageDiscoveryError of string

    val defaultFeedPath: string

    /// Why a sample pin must equal `src/*/*.fsproj` `<Version>` — printed alongside the offending
    /// pins so the next reader fixes the pin rather than bumping `<Version>` to match it.
    val mirrorRuleHint: string

    /// The samples that consume the framework as packages — those carrying a `nuget.config` that maps
    /// `FS.GG.UI.*` to the local feed. Discovered, so a newly added consumer is gated by construction.
    val discoverPackageConsumingSamples: repositoryRoot: string -> string list

    /// Every way a selected sample's pin disagrees with the packages `src/` produces, with project
    /// paths relative to `repositoryRoot` (these lines are read in CI logs and quoted in evidence).
    val pinViolations: repositoryRoot: string -> pins: PackagePin list -> string list

    val statusToken: status: PackagePinStatus -> string

    val proofStatusToken: status: ProofStatus -> string

    val modeToken: mode: PackageFeedMode -> string

    val tryParseMode: token: string -> PackageFeedMode option

    val init: options: PackageFeedOptions -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    /// The packages the real feed must contain: every `src/**/*.fsproj` declaring
    /// `<PackageId>FS.GG.UI.*` + `<IsPackable>true`, at the version MSBuild would pack it at (inline
    /// `<Version>`, else the one inherited from the nearest `Directory.Build[.local].props`).
    ///
    /// FAILS CLOSED. Identity and version resolution are separate concerns: a project that declares
    /// itself packable and cannot be read as one raises `PackageDiscoveryError` naming the file — it
    /// is never quietly absent from the result, because absent means "not expected", and a package
    /// that is not expected is one `MissingExpectedPackage` can never fire for.
    val discoverPackablePackages: repositoryRoot: string -> feedPath: string -> PackablePackage list

    /// The `FS.GG.UI.*` pins declared by the selected samples, classified against `currentPackages`.
    /// Raises `PackageDiscoveryError` on a sample project it cannot parse — an unreadable sample
    /// contributes no pins, and a sample with no pins has nothing that can be stale (#677).
    val readSelectedPackagePins:
        repositoryRoot: string ->
        selectedSamples: string list ->
        currentPackages: PackablePackage list ->
        allowedExceptionIds: Set<string> ->
        compatibilityExceptions: CompatibilityException list ->
            PackagePin list

    val checkLocalFeed: currentPackages: PackablePackage list -> FeedPackageStatus list

    val classifyPackagePins:
        currentPackages: PackablePackage list ->
        allowedExceptionIds: Set<string> ->
        compatibilityExceptions: CompatibilityException list ->
        pins: PackagePin list ->
            PackagePin list

    val refreshSamplePins: pins: PackagePin list -> string list

    val generatedSourceRules: feedPath: string -> SourceRule list

    val writeGeneratedNuGetConfig: path: string -> feedPath: string -> SourceRule list

    val runWorkflow: options: PackageFeedOptions -> PackageFeedResult
