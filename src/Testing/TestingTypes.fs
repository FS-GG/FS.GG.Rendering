namespace FS.GG.UI.Testing

open System
open System.IO
open System.Security.Cryptography
open System.Text
open FS.GG.UI.Scene
open SkiaSharp

type PackageReferenceExpectation =
    { PackageId: string
      Required: bool }

type GeneratedProductExpectation =
    { Profile: string
      RequiredFiles: string list
      ForbiddenPrefixes: string list
      PackageReferences: PackageReferenceExpectation list }

type LocalConsumerPackage =
    { PackageId: string
      Version: string
      FeedPath: string }

type LocalConsumerPackageDrift =
    { PackageId: string
      ExpectedVersion: string
      ActualVersion: string option
      FeedPath: string
      RemediationCommand: string }

type LocalConsumerPackageReport =
    { FeedPath: string
      Packages: LocalConsumerPackage list
      ConsumerConfigSnippet: string
      NuGetConfigSnippet: string option
      RestoreCommand: string
      DriftDiagnostics: LocalConsumerPackageDrift list }

type GeneratedValidationCategory =
    | PackageDrift
    | RestoreFailure
    | SemanticTestFailure
    | ViewerStartupFailure
    | UnsupportedHost
    | SceneEvidenceFailure
    | Completed

type GeneratedValidationResult =
    { Category: GeneratedValidationCategory
      Elapsed: TimeSpan
      CommandContext: string
      EvidencePath: string option
      Diagnostics: string list }

type GeneratedProductLaunchValidationResult =
    { InteractiveLaunchRequired: bool
      Diagnostics: string list }

type GeneratedWindowDiagnosticCheck =
    { Output: string
      RequiredFailureClasses: string list
      RequiredNativeFacts: string list }

type GeneratedWindowDiagnosticValidationResult =
    { DiagnosticsComplete: bool
      Diagnostics: string list }

type PackageResolutionCheck =
    { RequestedPackages: LocalConsumerPackage list
      ResolvedPackages: LocalConsumerPackage list
      PackageSources: string list
      RestoreWarnings: string list }

type PackageResolutionCheckResult =
    { ExactMatch: bool
      FailureReason: string option
      Diagnostics: string list }

type GeneratedTestExecutionCheck =
    { TestsExist: bool
      TestsRan: bool
      VerifyRan: bool }

type GeneratedTestExecutionResult =
    { Authoritative: bool
      NonAuthoritativeReason: string option
      Diagnostics: string list }

type VisualEvidenceKind =
    | Screenshot
    | PixelReadback
    | UnsupportedHost

type VisualEvidenceRequest =
    { ScreenshotAvailable: bool
      PixelReadbackAvailable: bool
      BoardReadable: bool option
      InputOrProgressObserved: bool option
      UnsupportedReason: string option }

type VisualEvidenceResult =
    { EvidenceKind: VisualEvidenceKind
      BoardReadable: bool option
      InputOrProgressObserved: bool option
      FallbackReason: string option
      UnsupportedReason: string option
      Diagnostics: string list }

type VisualSize =
    { Role: string
      Width: int
      Height: int
      Order: int }

type VisualTheme =
    { ThemeId: string
      Title: string
      Order: int }

type VisualPage =
    { PageId: string
      Title: string
      Order: int
      Required: bool }

type VisualCaptureTarget =
    { TargetId: string
      Page: VisualPage
      Theme: VisualTheme
      Size: VisualSize
      RelativePath: string
      Required: bool }

type VisualCaptureStatus =
    | VisualCaptureComplete
    | VisualCaptureMissing
    | VisualCaptureWrongSize
    | VisualCaptureUndecodable
    | VisualCaptureDegraded
    | VisualCaptureBlocked

type VisualCaptureArtifact =
    { RelativePath: string
      Exists: bool
      ByteCount: int64 option
      DecodedWidth: int option
      DecodedHeight: int option
      ContentHash: string option
      DecodeError: string option }

type VisualCaptureRecord =
    { Target: VisualCaptureTarget
      Status: VisualCaptureStatus
      Artifact: VisualCaptureArtifact option
      ExpectedWidth: int
      ExpectedHeight: int
      ObservedWidth: int option
      ObservedHeight: int option
      Reason: string option
      Diagnostics: string list }

type VisualReviewerSeverity =
    | VisualReviewerPending
    | VisualReviewerNone
    | VisualReviewerMinor
    | VisualReviewerMajor
    | VisualReviewerBlocking

type VisualReviewerClassification =
    { TargetId: string
      Severity: VisualReviewerSeverity
      DefectClass: string
      ReadinessImpact: string
      Reviewer: string
      ReviewedAt: string
      Notes: string }

type VisualReviewerValidationResult =
    { Classifications: VisualReviewerClassification list
      MissingTargetIds: string list
      DuplicateTargetIds: string list
      UnknownTargetIds: string list
      MalformedRows: string list
      PendingTargetIds: string list
      Diagnostics: string list }

type VisualContactSheet =
    { SheetId: string
      RelativePath: string
      SizeRole: string option
      ThemeId: string option
      TargetIds: string list
      MissingTargetIds: string list
      Diagnostics: string list }

type VisualReadinessStatus =
    | VisualReadinessAccepted
    | VisualReadinessPendingReview
    | VisualReadinessBlocked
    | VisualReadinessEnvironmentLimited
    | VisualReadinessIncomplete

type VisualReadinessReport =
    { RunId: string
      EvidenceRoot: string
      Targets: VisualCaptureTarget list
      Captures: VisualCaptureRecord list
      ReviewerClassifications: VisualReviewerClassification list
      ContactSheets: VisualContactSheet list
      CaptureStatusCounts: (string * int) list
      ReviewerStatusCounts: (string * int) list
      ReadinessStatus: VisualReadinessStatus
      Caveats: string list
      Diagnostics: string list }

type VisualSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
      Diagnostics: string list }

type GeneratedVisualEvidenceCommandCheck =
    { Output: string
      RequestedImageEvidence: bool }

type GeneratedVisualEvidenceCommandResult =
    { Accepted: bool
      EvidenceKind: string option
      FailureReason: string option
      Diagnostics: string list }

type GeneratedValidationContractCheck =
    { PackageResolution: PackageResolutionCheckResult
      GeneratedTests: GeneratedTestExecutionResult
      DefaultInteractiveLaunch: GeneratedProductLaunchValidationResult
      BoundedEvidenceValidated: bool
      CloseReasonValidated: bool
      WindowDiagnostics: GeneratedWindowDiagnosticValidationResult
      WindowOptionsValidated: bool
      ImageEvidence: GeneratedVisualEvidenceCommandResult }

type GeneratedValidationContractResult =
    { Output: string
      Authoritative: bool
      FailureClass: string
      Diagnostics: string list }

type GeneratedLayoutValidationFailureClass =
    | MissingLayoutFacts
    | UnsupportedLayoutFacts
    | OverlappingLayoutBounds
    | DeterministicRenderOnlyClaim

type GeneratedLayoutValidationCheck =
    { Report: LayoutEvidenceReport
      RequireReadableLayout: bool }

type GeneratedLayoutValidationResult =
    { Accepted: bool
      FailureClass: GeneratedLayoutValidationFailureClass option
      Diagnostics: string list }

type VisualInspectionRule =
    { RuleId: string
      Required: bool }

type VisualInspectionException =
    { ExceptionId: string
      RuleId: string
      OwnerId: string
      AffectedIds: string list
      Reason: string
      ExpiresWith: string option }

type VisualInspectionValidationCheck =
    { Artifact: VisualInspectionArtifact
      Rules: VisualInspectionRule list
      Exceptions: VisualInspectionException list
      RequiredRegionIds: string list
      PreviousArtifact: VisualInspectionArtifact option
      EnvironmentLimitations: string list }

type VisualInspectionValidationResult =
    { ArtifactId: string
      ReadinessStatus: VisualInspectionStatus
      Findings: VisualInspectionFinding list
      AppliedExceptions: string list
      InvalidExceptions: string list
      UnusedExceptions: string list
      Diagnostics: string list }

type VisualInspectionSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
      Diagnostics: string list }

type RetainedInspectionRule =
    { RuleId: string
      Required: bool }

type RetainedInspectionValidationCheck =
    { Artifact: RetainedInspectionArtifact
      Rules: RetainedInspectionRule list
      Exceptions: IntentionalDamageException list
      ExpectedAffectedRegionIds: string list
      PreviousArtifact: RetainedInspectionArtifact option
      EnvironmentLimitations: string list }

type RetainedInspectionValidationResult =
    { ArtifactId: string
      ReadinessStatus: RetainedInspectionStatus
      Findings: DamageLocalityFinding list
      AppliedExceptions: string list
      InvalidExceptions: string list
      UnusedExceptions: string list
      Diagnostics: string list }

type RetainedInspectionSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
      Diagnostics: string list }

type HostWarningClass =
    | BenignEnvironmentWarning
    | LaunchFailure
    | RenderingFailure
    | LayoutFailure
    | PackageFailure
    | UnknownWarning

type HostWarningClassificationCheck =
    { RawMessage: string
      KnownBenignMarkers: string list
      LaunchSucceeded: bool
      RenderingSucceeded: bool
      LayoutReadable: bool option
      ExplicitlyUnsupportedWithoutReadabilityClaim: bool
      PackageSucceeded: bool
      EvidencePath: string option }

type HostWarningClassificationResult =
    { WarningClass: HostWarningClass
      RawMessage: string
      Fatal: bool
      EvidencePath: string option
      SupportingFacts: string list
      Diagnostics: string list }

type PersistentLaunchArtifactCheck =
    { ArtifactPath: string
      Lines: string list
      SyntheticFixture: bool
      SupportedHostPassClaimed: bool }

type PersistentLaunchArtifactValidationResult =
    { Accepted: bool
      MissingFields: string list
      Contradictions: string list
      Diagnostics: string list }

type ReadinessFileDiscoveryCheck =
    { ReadinessDirectory: string
      RequiredFiles: string list
      ExistingFiles: string list }

type ReadinessFileDiscoveryResult =
    { Complete: bool
      MissingFiles: string list
      Diagnostics: string list }

type RuntimeDiagnosticReadinessCheck =
    { Summary: FS.GG.UI.Diagnostics.DiagnosticSummary
      RequiredStatus: FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus option
      RequireAccepted: bool }

type RuntimeDiagnosticReadinessResult =
    { Accepted: bool
      Status: string
      Diagnostics: string list }

type EvidenceReportStatus =
    | EvidenceOk
    | EvidenceUnsupported
    | EvidenceFailed

type EvidenceReportField =
    { Name: string
      Value: string }

type EvidenceReport =
    { Status: EvidenceReportStatus
      Command: string
      OutputPath: string option
      Fields: EvidenceReportField list
      Lines: string list
      ExitCode: int }

type EvidenceReportRequest =
    { Status: EvidenceReportStatus
      Command: string
      OutputPath: string option
      Fields: EvidenceReportField list }

type EvidenceReportValidationResult =
    { Accepted: bool
      MissingFields: string list
      Diagnostics: string list }

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

type ScreenshotEvidenceReportValidationResult =
    { Accepted: bool
      MissingFields: string list
      FailureClass: string option
      Diagnostics: string list }

type ScreenshotArtifactValidationCheck =
    { ReadinessDirectory: string
      ArtifactPath: string
      ExpectedWidth: int option
      ExpectedHeight: int option
      RequireNonBlank: bool }

type ScreenshotArtifactValidationResult =
    { Accepted: bool
      DecodedWidth: int option
      DecodedHeight: int option
      PixelContentValidation: string
      FailureClass: string option
      Diagnostics: string list }

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

type DefaultTextGlyphEvidenceValidationResult =
    { Accepted: bool
      GlyphCoverageMetric: float
      SolidBlockMetric: float
      PlaceholderMetric: float
      FailureClass: string option
      Diagnostics: string list }

type ScreenshotEvidenceRecord =
    { Fields: EvidenceReportField list
      ArtifactPath: string option
      Diagnostics: string list }

type PackageInspectionAssertionCheck =
    { Report: PackageInspectionReport
      ExpectedStatus: PackageInspectionStatus
      RequiredDiagnosticFragments: string list }

type PackageInspectionAssertionResult =
    { Accepted: bool
      Diagnostics: string list }

type LayoutReadinessStatus =
    | LayoutReadinessAccepted
    | LayoutReadinessIncomplete
    | LayoutReadinessFailed
    | LayoutReadinessSkipped
    | LayoutReadinessEnvironmentLimited
    | LayoutReadinessSyntheticOnly
    | LayoutReadinessCompatibilityBlocked
    | LayoutReadinessMissingEvidence

type LayoutReadinessEvidence =
    { Name: string
      Path: string option
      Status: LayoutReadinessStatus
      Required: bool
      Diagnostics: string list }

type LayoutCompatibilityDelta =
    { Surface: string
      Change: string
      Migration: string option
      Intentional: bool }

type LayoutReadinessReport =
    { Feature: string
      ContractStatus: LayoutReadinessStatus
      ScrollViewerStatus: LayoutReadinessStatus
      IntrinsicStatus: LayoutReadinessStatus
      ParityStatus: LayoutReadinessStatus
      CompatibilityStatus: LayoutReadinessStatus
      DiagnosticsStatus: LayoutReadinessStatus
      Evidence: LayoutReadinessEvidence list
      CompatibilityDeltas: LayoutCompatibilityDelta list
      Limitations: string list }

type LayoutReadinessValidationResult =
    { Accepted: bool
      Status: LayoutReadinessStatus
      MissingEvidence: string list
      BlockingLimitations: string list
      Diagnostics: string list }

type CompositorReadinessStatus =
    | CompositorReadinessAccepted
    | CompositorReadinessFallbackGated
    | CompositorReadinessFailed
    | CompositorReadinessEnvironmentLimited
    | CompositorReadinessMissingEvidence
    | CompositorReadinessCompatibilityBlocked

type CompositorReadinessEvidence =
    { EvidenceName: string
      EvidencePath: string option
      EvidenceStatus: CompositorReadinessStatus
      EvidenceRequired: bool
      EvidenceDiagnostics: string list }

type CompositorReadinessReport =
    { Feature: string
      ProofStatus: CompositorReadinessStatus
      ParityStatus: CompositorReadinessStatus
      TimingStatus: CompositorReadinessStatus
      CompatibilityStatus: CompositorReadinessStatus
      RegressionStatus: CompositorReadinessStatus
      Evidence: CompositorReadinessEvidence list
      Limitations: string list }

type CompositorReadinessValidationResult =
    { Accepted: bool
      Status: CompositorReadinessStatus
      MissingEvidence: string list
      BlockingLimitations: string list
      Diagnostics: string list }

type CompositorTimingVerdict =
    | CompositorTimingPositive
    | CompositorTimingNoisy
    | CompositorTimingNonBeneficial
    | CompositorTimingIncomplete
    | CompositorTimingRejected
    | CompositorTimingEnvironmentLimited
    | CompositorTimingLimited

type CompositorTimingScenario =
    { ScenarioId: string
      FullRedrawSampleCount: int
      DamageScopedSampleCount: int
      Verdict: CompositorTimingVerdict
      ArtifactPaths: string list
      RejectionReasons: string list }

type CompositorTimingSummaryCheck =
    { Feature: string
      ExpectedProfileId: string
      ActualProfileId: string
      PolicyId: string
      WarmupCount: int
      MeasuredRepetitions: int
      RequiredScenarioIds: string list
      Scenarios: CompositorTimingScenario list
      ShippedPerformanceClaim: string }

type CompositorTimingSummaryValidationResult =
    { Accepted: bool
      Verdict: CompositorTimingVerdict
      MissingScenarios: string list
      RejectedScenarios: string list
      Diagnostics: string list }

type CompositorDamageReadinessStatus =
    | CompositorDamageAccepted
    | CompositorDamageFallbackOnly
    | CompositorDamageRejected
    | CompositorDamageEnvironmentLimited

type CompositorDamageScenarioEvidence =
    { ScenarioId: string
      Status: CompositorDamageReadinessStatus
      AcceptedAttemptCount: int
      ArtifactPaths: string list
      FallbackReason: string option }

type CompositorDamageReadinessCheck =
    { Feature: string
      RequiredScenarioIds: string list
      Scenarios: CompositorDamageScenarioEvidence list
      AcceptedAttemptCount: int
      UnsupportedHostStatus: CompositorDamageReadinessStatus
      AcceptedPartialRedrawArtifacts: int
      CompatibilityAccepted: bool
      PackageAccepted: bool
      RegressionAccepted: bool
      PerformanceClaim: string
      Limitations: string list }

type CompositorDamageReadinessValidationResult =
    { Accepted: bool
      Status: CompositorDamageReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

type Feature159ReadinessStatus =
    | Feature159Accepted
    | Feature159NonBeneficial
    | Feature159FallbackOnly
    | Feature159Rejected
    | Feature159EnvironmentLimited

type Feature159ScenarioEvidence =
    { ScenarioId: string
      Status: Feature159ReadinessStatus
      PromotionDecision: string
      ReuseDecision: string
      AcceptedAttemptCount: int
      CounterNetSavedWork: int
      ParityPassed: bool
      ArtifactPaths: string list
      PrimaryReason: string option }

type Feature159ReadinessCheck =
    { Feature: string
      RequiredScenarioIds: string list
      Scenarios: Feature159ScenarioEvidence list
      AcceptedAttemptCount: int
      UnsupportedHostStatus: Feature159ReadinessStatus
      AcceptedReuseArtifacts: int
      AcceptedPromotionArtifacts: int
      CompatibilityAccepted: bool
      PackageAccepted: bool
      RegressionAccepted: bool
      PerformanceClaim: string
      Limitations: string list }

type Feature159ReadinessValidationResult =
    { Accepted: bool
      Status: Feature159ReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

type Feature160ThroughputReadinessStatus =
    | Feature160Accepted
    | Feature160Blocked
    | Feature160Rejected
    | Feature160FallbackOnly
    | Feature160EnvironmentLimited

type Feature160ScenarioEvidence =
    { ScenarioId: string
      Covered: bool
      WarmupCount: int
      MeasuredRepetitions: int
      SamplePolicy: string
      ArtifactPaths: string list
      PrimaryReason: string option }

type Feature160ThroughputReadinessCheck =
    { Feature: string
      RequiredScenarioIds: string list
      Scenarios: Feature160ScenarioEvidence list
      AcceptedIterationCount: int
      RequiredIterationCount: int
      UnsupportedHostStatus: Feature160ThroughputReadinessStatus
      AcceptedUnsupportedHostArtifacts: int
      FullValidationStatus: string
      CompatibilityAccepted: bool
      PackageAccepted: bool
      RegressionAccepted: bool
      PerformanceClaim: string
      Limitations: string list }

type Feature160ThroughputReadinessValidationResult =
    { Accepted: bool
      Status: Feature160ThroughputReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

type Feature161HostLaneReadinessStatus =
    | Feature161Accepted
    | Feature161Blocked
    | Feature161Rejected
    | Feature161FallbackOnly
    | Feature161EnvironmentLimited
    | Feature161MissingEvidence

type Feature161HostFactEvidence =
    { LaneId: string
      DisplayServer: string
      DisplayIdentity: string
      RendererIdentity: string
      DirectRendering: bool option
      RefreshStatus: string
      DriverIdentity: string
      PackageVersionSet: string
      CpuLoadNote: string
      GpuLoadNote: string
      EnvironmentLimits: string list
      HostProfile: string
      RunIdentity: string
      ScenarioIdentity: string
      TimingPolicyIdentity: string
      ArtifactPaths: string list }

type Feature161ClaimScopeEvidence =
    { AcceptedLaneId: string option
      NonGeneralizedLanes: string list
      RemainingBlockers: string list
      PerformanceClaim: string }

type Feature161HostLaneReadinessCheck =
    { Feature: string
      RequiredScenarioIds: string list
      CoveredScenarioIds: string list
      HostFacts: Feature161HostFactEvidence option
      AcceptedLaneScopedPerformanceArtifacts: int
      UnsupportedHostStatus: Feature161HostLaneReadinessStatus
      PriorGateStatuses: string list
      ClaimScope: Feature161ClaimScopeEvidence
      FullValidationStatus: string
      CompatibilityAccepted: bool
      PackageAccepted: bool
      RegressionAccepted: bool
      Limitations: string list }

type Feature161HostLaneReadinessValidationResult =
    { Accepted: bool
      Status: Feature161HostLaneReadinessStatus
      MissingFacts: string list
      MissingScenarios: string list
      Diagnostics: string list }

