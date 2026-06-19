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
        | ReadRestoreAssets
        | WritePackageEvidence

    val defaultFeedPath: string

    val statusToken: status: PackagePinStatus -> string

    val proofStatusToken: status: ProofStatus -> string

    val modeToken: mode: PackageFeedMode -> string

    val tryParseMode: token: string -> PackageFeedMode option

    val init: options: PackageFeedOptions -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    val discoverPackablePackages: repositoryRoot: string -> feedPath: string -> PackablePackage list

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
