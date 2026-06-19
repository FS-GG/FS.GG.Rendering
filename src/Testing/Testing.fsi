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

/// Visual-readiness viewport or render size required by a sample.
type VisualSize =
    { Role: string
      Width: int
      Height: int
      Order: int }

/// Visual-readiness theme declared by a sample.
type VisualTheme =
    { ThemeId: string
      Title: string
      Order: int }

/// Visual-readiness page declared by a sample.
type VisualPage =
    { PageId: string
      Title: string
      Order: int
      Required: bool }

/// One required page/theme/size/output screenshot target.
type VisualCaptureTarget =
    { TargetId: string
      Page: VisualPage
      Theme: VisualTheme
      Size: VisualSize
      RelativePath: string
      Required: bool }

/// Screenshot completeness status for a required target.
type VisualCaptureStatus =
    | VisualCaptureComplete
    | VisualCaptureMissing
    | VisualCaptureWrongSize
    | VisualCaptureUndecodable
    | VisualCaptureDegraded
    | VisualCaptureBlocked

/// Observed filesystem artifact details for one target path.
type VisualCaptureArtifact =
    { RelativePath: string
      Exists: bool
      ByteCount: int64 option
      DecodedWidth: int option
      DecodedHeight: int option
      ContentHash: string option
      DecodeError: string option }

/// Completeness classification for one visual capture target.
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

/// Reviewer severity parsed from visual-readiness review records.
type VisualReviewerSeverity =
    | VisualReviewerPending
    | VisualReviewerNone
    | VisualReviewerMinor
    | VisualReviewerMajor
    | VisualReviewerBlocking

/// Human reviewer classification for one visual capture target.
type VisualReviewerClassification =
    { TargetId: string
      Severity: VisualReviewerSeverity
      DefectClass: string
      ReadinessImpact: string
      Reviewer: string
      ReviewedAt: string
      Notes: string }

/// Parsed reviewer classification result with actionable diagnostics.
type VisualReviewerValidationResult =
    { Classifications: VisualReviewerClassification list
      MissingTargetIds: string list
      DuplicateTargetIds: string list
      UnknownTargetIds: string list
      MalformedRows: string list
      PendingTargetIds: string list
      Diagnostics: string list }

/// Contact-sheet metadata recorded by the shared readiness report.
type VisualContactSheet =
    { SheetId: string
      RelativePath: string
      SizeRole: string option
      ThemeId: string option
      TargetIds: string list
      MissingTargetIds: string list
      Diagnostics: string list }

/// Overall visual-readiness status.
type VisualReadinessStatus =
    | VisualReadinessAccepted
    | VisualReadinessPendingReview
    | VisualReadinessBlocked
    | VisualReadinessEnvironmentLimited
    | VisualReadinessIncomplete

/// Machine-checkable visual-readiness report.
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

/// Safe managed-section update result for human readiness summaries.
type VisualSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
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

/// Stable validation rule descriptor for structured visual inspection.
type VisualInspectionRule =
    { RuleId: string
      Required: bool }

/// Reviewed allowance for a specific visual inspection finding.
type VisualInspectionException =
    { ExceptionId: string
      RuleId: string
      OwnerId: string
      AffectedIds: string list
      Reason: string
      ExpiresWith: string option }

/// Request for validating one visual inspection artifact.
type VisualInspectionValidationCheck =
    { Artifact: VisualInspectionArtifact
      Rules: VisualInspectionRule list
      Exceptions: VisualInspectionException list
      RequiredRegionIds: string list
      PreviousArtifact: VisualInspectionArtifact option
      EnvironmentLimitations: string list }

/// Rule-validation result for one inspected artifact.
type VisualInspectionValidationResult =
    { ArtifactId: string
      ReadinessStatus: VisualInspectionStatus
      Findings: VisualInspectionFinding list
      AppliedExceptions: string list
      InvalidExceptions: string list
      UnusedExceptions: string list
      Diagnostics: string list }

/// Safe managed-section update result for inspection summaries.
type VisualInspectionSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
      Diagnostics: string list }

/// Stable validation rule descriptor for retained/damage inspection.
///
/// Rule ids are persisted in readiness findings, so keep them stable once
/// they are emitted by committed evidence.
type RetainedInspectionRule =
    { RuleId: string
      Required: bool }

/// Request for validating one retained inspection artifact.
///
/// The check combines artifact facts, the active rule set, expected affected
/// regions, intentional damage exceptions, and known environment limitations.
type RetainedInspectionValidationCheck =
    { Artifact: RetainedInspectionArtifact
      Rules: RetainedInspectionRule list
      Exceptions: IntentionalDamageException list
      ExpectedAffectedRegionIds: string list
      PreviousArtifact: RetainedInspectionArtifact option
      EnvironmentLimitations: string list }

/// Rule-validation result for one retained inspection artifact.
///
/// Applied, invalid, and unused exceptions stay visible so broad damage is
/// reviewed explicitly instead of being accepted silently.
type RetainedInspectionValidationResult =
    { ArtifactId: string
      ReadinessStatus: RetainedInspectionStatus
      Findings: DamageLocalityFinding list
      AppliedExceptions: string list
      InvalidExceptions: string list
      UnusedExceptions: string list
      Diagnostics: string list }

/// Safe managed-section update result for retained inspection summaries.
type RetainedInspectionSummarySectionUpdate =
    { UpdatedText: string
      SafeToWrite: bool
      InsertedMarkers: bool
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

/// Runtime diagnostics readiness check consumed by generated-product and framework evidence.
type RuntimeDiagnosticReadinessCheck =
    { Summary: FS.GG.UI.Diagnostics.DiagnosticSummary
      RequiredStatus: FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus option
      RequireAccepted: bool }

/// Runtime diagnostics readiness wrapper result.
type RuntimeDiagnosticReadinessResult =
    { Accepted: bool
      Status: string
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

/// Public contract type exposed by this FS.GG.UI package.
type LayoutReadinessStatus =
    | LayoutReadinessAccepted
    | LayoutReadinessIncomplete
    | LayoutReadinessFailed
    | LayoutReadinessSkipped
    | LayoutReadinessEnvironmentLimited
    | LayoutReadinessSyntheticOnly
    | LayoutReadinessCompatibilityBlocked
    | LayoutReadinessMissingEvidence

/// Public contract type exposed by this FS.GG.UI package.
type LayoutReadinessEvidence =
    { Name: string
      Path: string option
      Status: LayoutReadinessStatus
      Required: bool
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutCompatibilityDelta =
    { Surface: string
      Change: string
      Migration: string option
      Intentional: bool }

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type LayoutReadinessValidationResult =
    { Accepted: bool
      Status: LayoutReadinessStatus
      MissingEvidence: string list
      BlockingLimitations: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type CompositorReadinessStatus =
    | CompositorReadinessAccepted
    | CompositorReadinessFallbackGated
    | CompositorReadinessFailed
    | CompositorReadinessEnvironmentLimited
    | CompositorReadinessMissingEvidence
    | CompositorReadinessCompatibilityBlocked

/// Public contract type exposed by this FS.GG.UI package.
type CompositorReadinessEvidence =
    { EvidenceName: string
      EvidencePath: string option
      EvidenceStatus: CompositorReadinessStatus
      EvidenceRequired: bool
      EvidenceDiagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type CompositorReadinessReport =
    { Feature: string
      ProofStatus: CompositorReadinessStatus
      ParityStatus: CompositorReadinessStatus
      TimingStatus: CompositorReadinessStatus
      CompatibilityStatus: CompositorReadinessStatus
      RegressionStatus: CompositorReadinessStatus
      Evidence: CompositorReadinessEvidence list
      Limitations: string list }

/// Public contract type exposed by this FS.GG.UI package.
type CompositorReadinessValidationResult =
    { Accepted: bool
      Status: CompositorReadinessStatus
      MissingEvidence: string list
      BlockingLimitations: string list
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type CompositorTimingVerdict =
    | CompositorTimingPositive
    | CompositorTimingNoisy
    | CompositorTimingNonBeneficial
    | CompositorTimingIncomplete
    | CompositorTimingRejected
    | CompositorTimingEnvironmentLimited
    | CompositorTimingLimited

/// Public contract type exposed by this FS.GG.UI package.
type CompositorTimingScenario =
    { ScenarioId: string
      FullRedrawSampleCount: int
      DamageScopedSampleCount: int
      Verdict: CompositorTimingVerdict
      ArtifactPaths: string list
      RejectionReasons: string list }

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type CompositorTimingSummaryValidationResult =
    { Accepted: bool
      Verdict: CompositorTimingVerdict
      MissingScenarios: string list
      RejectedScenarios: string list
      Diagnostics: string list }

/// Feature 157 damage-scoped correctness readiness status.
type CompositorDamageReadinessStatus =
    | CompositorDamageAccepted
    | CompositorDamageFallbackOnly
    | CompositorDamageRejected
    | CompositorDamageEnvironmentLimited

/// Feature 157 scenario-level damage evidence used by package checks.
type CompositorDamageScenarioEvidence =
    { ScenarioId: string
      Status: CompositorDamageReadinessStatus
      AcceptedAttemptCount: int
      ArtifactPaths: string list
      FallbackReason: string option }

/// Feature 157 package-visible readiness check.
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

/// Feature 157 readiness validation result.
type CompositorDamageReadinessValidationResult =
    { Accepted: bool
      Status: CompositorDamageReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

/// Feature 159 promotion/reuse readiness status.
type Feature159ReadinessStatus =
    | Feature159Accepted
    | Feature159NonBeneficial
    | Feature159FallbackOnly
    | Feature159Rejected
    | Feature159EnvironmentLimited

/// Feature 159 scenario-level promotion/reuse evidence used by package checks.
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

/// Feature 159 package-visible readiness check.
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

/// Feature 159 readiness validation result.
type Feature159ReadinessValidationResult =
    { Accepted: bool
      Status: Feature159ReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

/// Feature 160 throughput-readiness status.
type Feature160ThroughputReadinessStatus =
    | Feature160Accepted
    | Feature160Blocked
    | Feature160Rejected
    | Feature160FallbackOnly
    | Feature160EnvironmentLimited

/// Feature 160 scenario-level coverage and sample-policy evidence.
type Feature160ScenarioEvidence =
    { ScenarioId: string
      Covered: bool
      WarmupCount: int
      MeasuredRepetitions: int
      SamplePolicy: string
      ArtifactPaths: string list
      PrimaryReason: string option }

/// Feature 160 package-visible throughput readiness check.
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

/// Feature 160 throughput readiness validation result.
type Feature160ThroughputReadinessValidationResult =
    { Accepted: bool
      Status: Feature160ThroughputReadinessStatus
      MissingScenarios: string list
      Diagnostics: string list }

/// Feature 161 host-lane-readiness status.
type Feature161HostLaneReadinessStatus =
    | Feature161Accepted
    | Feature161Blocked
    | Feature161Rejected
    | Feature161FallbackOnly
    | Feature161EnvironmentLimited
    | Feature161MissingEvidence

/// Feature 161 host facts required to scope performance evidence to one lane.
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

/// Feature 161 claim scope evidence for package validation.
type Feature161ClaimScopeEvidence =
    { AcceptedLaneId: string option
      NonGeneralizedLanes: string list
      RemainingBlockers: string list
      PerformanceClaim: string }

/// Feature 161 package-visible host lane readiness check.
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

/// Feature 161 host lane readiness validation result.
type Feature161HostLaneReadinessValidationResult =
    { Accepted: bool
      Status: Feature161HostLaneReadinessStatus
      MissingFacts: string list
      MissingScenarios: string list
      Diagnostics: string list }

/// Shared visual-readiness target matrix helpers.
module VisualCaptureMatrix =
    /// Build a stable target id from page/theme/size/path facts.
    val targetId: page: VisualPage -> theme: VisualTheme -> size: VisualSize -> relativePath: string -> string
    /// Expand pages x themes x sizes into deterministic visual capture targets.
    val expand:
        pages: VisualPage list ->
        themes: VisualTheme list ->
        sizes: VisualSize list ->
        pathFor: (VisualPage -> VisualTheme -> VisualSize -> string) ->
            Result<VisualCaptureTarget list, string list>

/// Shared visual screenshot completeness helpers.
module VisualCompleteness =
    /// Stable status token for readiness summaries.
    val statusText: status: VisualCaptureStatus -> string
    /// Build a degraded capture record with safe-failure diagnostics.
    val degraded: target: VisualCaptureTarget -> reason: string -> VisualCaptureRecord
    /// Validate required PNG artifacts below the evidence root and report stale extras.
    val validate: evidenceRoot: string -> targets: VisualCaptureTarget list -> VisualCaptureRecord list * string list

/// Shared reviewer-classification Markdown helpers.
module VisualReviewerClassifications =
    /// Stable severity token for readiness summaries.
    val severityText: severity: VisualReviewerSeverity -> string
    /// Generate a Markdown review table with one row per target.
    val writeTemplate: targets: VisualCaptureTarget list -> string
    /// Parse reviewer Markdown against the current target matrix.
    val parse: markdown: string -> targets: VisualCaptureTarget list -> VisualReviewerValidationResult

/// Shared visual-readiness aggregation helpers.
module VisualReadiness =
    /// Stable status token for readiness summaries.
    val statusText: status: VisualReadinessStatus -> string
    /// Aggregate captures, reviewer records, contact sheets, and caveats into readiness.
    val evaluate:
        runId: string ->
        evidenceRoot: string ->
        targets: VisualCaptureTarget list ->
        captures: VisualCaptureRecord list ->
        reviewerClassifications: VisualReviewerClassification list ->
        contactSheets: VisualContactSheet list ->
        caveats: string list ->
        acceptedExceptions: string list ->
            VisualReadinessReport

/// Shared visual-readiness Markdown, JSON, and managed-section helpers.
module VisualReadinessMarkdown =
    /// Managed-section start marker used in human summaries.
    val startMarker: string
    /// Managed-section end marker used in human summaries.
    val endMarker: string
    /// Render a human-readable generated Markdown section.
    val renderSummary: report: VisualReadinessReport -> string
    /// Render a deterministic machine-readable JSON report.
    val renderJson: report: VisualReadinessReport -> string
    /// Update or insert exactly one generated section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> VisualSummarySectionUpdate

/// Structured visual inspection rule vocabulary and validators.
module VisualInspectionValidation =
    /// Create a required validation rule by stable rule id.
    val rule: ruleId: string -> VisualInspectionRule
    /// The initial deterministic visual inspection rule set.
    val defaultRules: VisualInspectionRule list
    /// Validate an artifact with explicit rules, exceptions, expected regions, and optional previous artifact.
    val validateCheck: check: VisualInspectionValidationCheck -> VisualInspectionValidationResult
    /// Validate an artifact with the default check shape.
    val validate:
        artifact: VisualInspectionArtifact ->
        rules: VisualInspectionRule list ->
        exceptions: VisualInspectionException list ->
            VisualInspectionValidationResult

/// Readiness aggregation for one or more visual inspection validation results.
module VisualInspectionReadiness =
    /// Aggregate artifacts and validation results into a reviewer- and machine-readable summary.
    val aggregate:
        runId: string ->
        artifacts: VisualInspectionArtifact list ->
        results: VisualInspectionValidationResult list ->
        relatedVisualEvidence: string list ->
        caveats: string list ->
            VisualInspectionSummary

/// Markdown, JSON, and managed-section helpers for visual inspection evidence.
module VisualInspectionMarkdown =
    /// Managed-section start marker used in human inspection summaries.
    val startMarker: string
    /// Managed-section end marker used in human inspection summaries.
    val endMarker: string
    /// Render a human-readable generated Markdown inspection section.
    val renderSummary: summary: VisualInspectionSummary -> string
    /// Render deterministic machine-readable JSON for an inspection summary.
    val renderJson: summary: VisualInspectionSummary -> string
    /// Update or insert exactly one generated inspection section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> VisualInspectionSummarySectionUpdate

/// Retained/damage inspection rule vocabulary and validators.
module RetainedInspectionValidation =
    /// Create a required validation rule by stable rule id.
    val rule: ruleId: string -> RetainedInspectionRule
    /// Deterministic retained/damage rule set.
    ///
    /// The default set checks required facts, dirty-region locality, broad or
    /// full-surface damage, expected affected regions, and exception hygiene.
    val defaultRules: RetainedInspectionRule list
    /// Validate an artifact with explicit rules, exceptions, expected regions, and optional previous artifact.
    val validateCheck: check: RetainedInspectionValidationCheck -> RetainedInspectionValidationResult
    /// Validate an artifact with the default check shape.
    ///
    /// This convenience entry point uses the artifact transition's expected
    /// affected regions and records exception diagnostics in the result.
    val validate:
        artifact: RetainedInspectionArtifact ->
        rules: RetainedInspectionRule list ->
        exceptions: IntentionalDamageException list ->
            RetainedInspectionValidationResult

/// Readiness aggregation for retained/damage inspection validation results.
module RetainedInspectionReadiness =
    /// Aggregate artifacts and validation results into a retained inspection summary.
    ///
    /// Command evidence and caveats are preserved in the summary so generated
    /// Markdown/JSON can be used directly in readiness reports.
    val aggregate:
        runId: string ->
        artifacts: RetainedInspectionArtifact list ->
        results: RetainedInspectionValidationResult list ->
        relatedVisualEvidence: string list ->
        commandEvidence: (string * string) list ->
        caveats: string list ->
            RetainedInspectionSummary

/// Markdown, JSON, and managed-section helpers for retained inspection evidence.
module RetainedInspectionMarkdown =
    /// Managed-section start marker used in human retained inspection summaries.
    val startMarker: string
    /// Managed-section end marker used in human retained inspection summaries.
    val endMarker: string
    /// Render a human-readable generated retained inspection section.
    val renderSummary: summary: RetainedInspectionSummary -> string
    /// Render deterministic machine-readable JSON for a retained inspection summary.
    val renderJson: summary: RetainedInspectionSummary -> string
    /// Update or insert exactly one generated retained inspection section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> RetainedInspectionSummarySectionUpdate

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
module RuntimeDiagnosticReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: RuntimeDiagnosticReadinessCheck -> RuntimeDiagnosticReadinessResult

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

/// Public contract module exposed by this FS.GG.UI package.
module LayoutReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: LayoutReadinessStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: LayoutReadinessReport -> LayoutReadinessValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module CompositorReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: CompositorReadinessStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: CompositorReadinessReport -> CompositorReadinessValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module CompositorTimingAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val verdictText: verdict: CompositorTimingVerdict -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validateSummary: check: CompositorTimingSummaryCheck -> CompositorTimingSummaryValidationResult

/// Feature 157 damage-scoped readiness helper.
module CompositorDamageReadiness =
    /// Feature 157: stable status token for readiness summaries.
    val statusText: status: CompositorDamageReadinessStatus -> string
    /// Feature 157: validate accepted, fallback-only, rejected, and environment-limited damage packages.
    val validate: check: CompositorDamageReadinessCheck -> CompositorDamageReadinessValidationResult

/// Feature 159 promotion/reuse readiness helper.
module Feature159Readiness =
    /// Feature 159: stable status token for readiness summaries.
    val statusText: status: Feature159ReadinessStatus -> string
    /// Feature 159: validate promotion/reuse readiness packages without accepting broader performance.
    val validate: check: Feature159ReadinessCheck -> Feature159ReadinessValidationResult

/// Feature 160 throughput-readiness helper.
module Feature160ThroughputReadiness =
    /// Feature 160: stable status token for readiness summaries.
    val statusText: status: Feature160ThroughputReadinessStatus -> string
    /// Feature 160: validate focused throughput packages while preserving the performance-claim boundary.
    val validate: check: Feature160ThroughputReadinessCheck -> Feature160ThroughputReadinessValidationResult

/// Feature 161 host-lane-readiness helper.
module Feature161HostLaneReadiness =
    /// Feature 161: stable status token for readiness summaries.
    val statusText: status: Feature161HostLaneReadinessStatus -> string
    /// Feature 161: validate host lane readiness packages without broadening performance claims across lanes.
    val validate: check: Feature161HostLaneReadinessCheck -> Feature161HostLaneReadinessValidationResult
