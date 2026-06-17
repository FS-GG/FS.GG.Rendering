namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Public contract type exposed by this FS.GG.UI package.
type PackageReferenceExpectation =
    { PackageId: string
      Required: bool }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedProductExpectation =
    { Profile: string
      RequiredFiles: string list
      ForbiddenPrefixes: string list
      PackageReferences: PackageReferenceExpectation list }

/// Public contract type exposed by this FS.GG.UI package.
type LocalConsumerPackage =
    { PackageId: string
      Version: string
      FeedPath: string }

/// Public contract type exposed by this FS.GG.UI package.
type LocalConsumerPackageDrift =
    { PackageId: string
      ExpectedVersion: string
      ActualVersion: string option
      FeedPath: string
      RemediationCommand: string }

/// Public contract type exposed by this FS.GG.UI package.
type LocalConsumerPackageReport =
    { FeedPath: string
      Packages: LocalConsumerPackage list
      ConsumerConfigSnippet: string
      NuGetConfigSnippet: string option
      RestoreCommand: string
      DriftDiagnostics: LocalConsumerPackageDrift list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedValidationCategory =
    | PackageDrift
    | RestoreFailure
    | SemanticTestFailure
    | ViewerStartupFailure
    | UnsupportedHost
    | SceneEvidenceFailure
    | Completed

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedValidationResult =
    { Category: GeneratedValidationCategory
      Elapsed: TimeSpan
      CommandContext: string
      EvidencePath: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedProductLaunchValidationResult =
    { InteractiveLaunchRequired: bool
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedWindowDiagnosticCheck =
    { Output: string
      RequiredFailureClasses: string list
      RequiredNativeFacts: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedWindowDiagnosticValidationResult =
    { DiagnosticsComplete: bool
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type PackageResolutionCheck =
    { RequestedPackages: LocalConsumerPackage list
      ResolvedPackages: LocalConsumerPackage list
      PackageSources: string list
      RestoreWarnings: string list }

/// Public contract type exposed by this FS.GG.UI package.
type PackageResolutionCheckResult =
    { ExactMatch: bool
      FailureReason: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedTestExecutionCheck =
    { TestsExist: bool
      TestsRan: bool
      VerifyRan: bool }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedTestExecutionResult =
    { Authoritative: bool
      NonAuthoritativeReason: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type VisualEvidenceKind =
    | Screenshot
    | PixelReadback
    | UnsupportedHost

/// Public contract type exposed by this FS.GG.UI package.
type VisualEvidenceRequest =
    { ScreenshotAvailable: bool
      PixelReadbackAvailable: bool
      BoardReadable: bool option
      InputOrProgressObserved: bool option
      UnsupportedReason: string option }

/// Public contract type exposed by this FS.GG.UI package.
type VisualEvidenceResult =
    { EvidenceKind: VisualEvidenceKind
      BoardReadable: bool option
      InputOrProgressObserved: bool option
      FallbackReason: string option
      UnsupportedReason: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedVisualEvidenceCommandCheck =
    { Output: string
      RequestedImageEvidence: bool }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedVisualEvidenceCommandResult =
    { Accepted: bool
      EvidenceKind: string option
      FailureReason: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedValidationContractCheck =
    { PackageResolution: PackageResolutionCheckResult
      GeneratedTests: GeneratedTestExecutionResult
      DefaultInteractiveLaunch: GeneratedProductLaunchValidationResult
      BoundedEvidenceValidated: bool
      CloseReasonValidated: bool
      WindowDiagnostics: GeneratedWindowDiagnosticValidationResult
      WindowOptionsValidated: bool
      ImageEvidence: GeneratedVisualEvidenceCommandResult }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedValidationContractResult =
    { Output: string
      Authoritative: bool
      FailureClass: string
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedLayoutValidationFailureClass =
    | MissingLayoutFacts
    | UnsupportedLayoutFacts
    | OverlappingLayoutBounds
    | DeterministicRenderOnlyClaim

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedLayoutValidationCheck =
    { Report: LayoutEvidenceReport
      RequireReadableLayout: bool }

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedLayoutValidationResult =
    { Accepted: bool
      FailureClass: GeneratedLayoutValidationFailureClass option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type HostWarningClass =
    | BenignEnvironmentWarning
    | LaunchFailure
    | RenderingFailure
    | LayoutFailure
    | PackageFailure
    | UnknownWarning

/// Public contract type exposed by this FS.GG.UI package.
type HostWarningClassificationCheck =
    { RawMessage: string
      KnownBenignMarkers: string list
      LaunchSucceeded: bool
      RenderingSucceeded: bool
      LayoutReadable: bool option
      ExplicitlyUnsupportedWithoutReadabilityClaim: bool
      PackageSucceeded: bool
      EvidencePath: string option }

/// Public contract type exposed by this FS.GG.UI package.
type HostWarningClassificationResult =
    { WarningClass: HostWarningClass
      RawMessage: string
      Fatal: bool
      EvidencePath: string option
      SupportingFacts: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type PersistentLaunchArtifactCheck =
    { ArtifactPath: string
      Lines: string list
      SyntheticFixture: bool
      SupportedHostPassClaimed: bool }

/// Public contract type exposed by this FS.GG.UI package.
type PersistentLaunchArtifactValidationResult =
    { Accepted: bool
      MissingFields: string list
      Contradictions: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ReadinessFileDiscoveryCheck =
    { ReadinessDirectory: string
      RequiredFiles: string list
      ExistingFiles: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ReadinessFileDiscoveryResult =
    { Complete: bool
      MissingFiles: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceReportStatus =
    | EvidenceOk
    | EvidenceUnsupported
    | EvidenceFailed

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceReportField =
    { Name: string
      Value: string }

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceReport =
    { Status: EvidenceReportStatus
      Command: string
      OutputPath: string option
      Fields: EvidenceReportField list
      Lines: string list
      ExitCode: int }

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceReportRequest =
    { Status: EvidenceReportStatus
      Command: string
      OutputPath: string option
      Fields: EvidenceReportField list }

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceReportValidationResult =
    { Accepted: bool
      MissingFields: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceReportCheck =
    { Status: string
      Command: string option
      AppOrSample: string option
      HostFacts: string list
      CaptureMode: string option
      EvidenceKind: string option
      ArtifactPath: string option
      ScreenshotPath: string option
      Width: int option
      Height: int option
      PixelContentValidation: string option
      CaptureSource: string option
      ProvesScreenshot: bool option
      BlockedStage: string option
      Classification: string option
      Category: string option
      Message: string option
      Timestamp: DateTimeOffset option
      ViewerOpenStatus: string option
      FirstFrameStatus: string option
      CaptureAvailability: string option
      UnsupportedHostReason: string option
      Fallback: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceReportValidationResult =
    { Accepted: bool
      MissingFields: string list
      FailureClass: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotArtifactValidationCheck =
    { ReadinessDirectory: string
      ArtifactPath: string
      ExpectedWidth: int option
      ExpectedHeight: int option
      RequireNonBlank: bool }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotArtifactValidationResult =
    { Accepted: bool
      DecodedWidth: int option
      DecodedHeight: int option
      PixelContentValidation: string
      FailureClass: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type DefaultTextGlyphEvidenceCheck =
    { ReadinessDirectory: string
      ScreenshotPath: string
      TextRegion: Rect option
      ExpectedWidth: int option
      ExpectedHeight: int option
      Status: string
      FontResolution: string option
      FallbackUsed: bool option
      UnsupportedHostReason: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type DefaultTextGlyphEvidenceValidationResult =
    { Accepted: bool
      GlyphCoverageMetric: float
      SolidBlockMetric: float
      PlaceholderMetric: float
      FailureClass: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceRecord =
    { Fields: EvidenceReportField list
      ArtifactPath: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type PackageInspectionAssertionCheck =
    { Report: PackageInspectionReport
      ExpectedStatus: PackageInspectionStatus
      RequiredDiagnosticFragments: string list }

/// Public contract type exposed by this FS.GG.UI package.
type PackageInspectionAssertionResult =
    { Accepted: bool
      Diagnostics: string list }

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedProductAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val summarize: expectation: GeneratedProductExpectation -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validateDefaultInteractiveLaunch: source: string -> GeneratedProductLaunchValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowDiagnostics: check: GeneratedWindowDiagnosticCheck -> GeneratedWindowDiagnosticValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module LocalConsumerPackages =
    /// Public contract function exposed by this FS.GG.UI package.
    val report: feedPath: string -> packages: LocalConsumerPackage list -> LocalConsumerPackageReport
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyDrift: expected: LocalConsumerPackage list -> actual: LocalConsumerPackage list -> LocalConsumerPackageDrift list

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedConsumerValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val summarize: result: GeneratedValidationResult -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val verifyPackageResolution: check: PackageResolutionCheck -> PackageResolutionCheckResult
    /// Public contract function exposed by this FS.GG.UI package.
    val verifyGeneratedTests: check: GeneratedTestExecutionCheck -> GeneratedTestExecutionResult
    /// Public contract function exposed by this FS.GG.UI package.
    val selectVisualEvidence: request: VisualEvidenceRequest -> VisualEvidenceResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateVisualEvidenceCommandOutput: check: GeneratedVisualEvidenceCommandCheck -> GeneratedVisualEvidenceCommandResult
    /// Public contract function exposed by this FS.GG.UI package.
    val buildValidationContractOutput: check: GeneratedValidationContractCheck -> GeneratedValidationContractResult

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedLayoutValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: GeneratedLayoutValidationCheck -> GeneratedLayoutValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module HostWarningClassification =
    /// Public contract function exposed by this FS.GG.UI package.
    val classify: check: HostWarningClassificationCheck -> HostWarningClassificationResult

/// Public contract module exposed by this FS.GG.UI package.
module PersistentLaunchArtifactValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: PersistentLaunchArtifactCheck -> PersistentLaunchArtifactValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module ReadinessFileDiscovery =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: ReadinessFileDiscoveryCheck -> ReadinessFileDiscoveryResult

/// Public contract module exposed by this FS.GG.UI package.
module DefaultTextGlyphEvidence =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: DefaultTextGlyphEvidenceCheck -> DefaultTextGlyphEvidenceValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module EvidenceReports =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: EvidenceReportStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val field: name: string -> value: string -> EvidenceReportField
    /// Public contract function exposed by this FS.GG.UI package.
    val build: request: EvidenceReportRequest -> EvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val write: request: EvidenceReportRequest -> EvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: EvidenceReport -> EvidenceReportValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val parseScreenshotEvidenceRecord: lines: string list -> ScreenshotEvidenceRecord
    /// Public contract function exposed by this FS.GG.UI package.
    val validateScreenshotArtifact: check: ScreenshotArtifactValidationCheck -> ScreenshotArtifactValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateScreenshotEvidence: check: ScreenshotEvidenceReportCheck -> ScreenshotEvidenceReportValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module PackageInspectionAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: PackageInspectionAssertionCheck -> PackageInspectionAssertionResult
