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

