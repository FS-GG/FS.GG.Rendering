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

module VisualCaptureMatrix =
    let private blank (value: string) = String.IsNullOrWhiteSpace value

    let private duplicateValues values =
        values
        |> List.countBy id
        |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

    let private normalRelativePath (relativePath: string) =
        relativePath.Replace('\\', '/').Trim()

    let private pathIsSafe (relativePath: string) =
        let normalized = normalRelativePath relativePath
        let hasParentTraversal =
            normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists (fun part -> part = "..")

        not (blank normalized)
        && not (Path.IsPathRooted normalized)
        && not hasParentTraversal

    let targetId (page: VisualPage) (theme: VisualTheme) (size: VisualSize) (relativePath: string) =
        let normalizedPath = normalRelativePath relativePath
        $"{size.Role}:{size.Width}x{size.Height}:{theme.ThemeId}:{page.PageId}:{normalizedPath}"

    let expand (pages: VisualPage list) (themes: VisualTheme list) (sizes: VisualSize list) (pathFor: VisualPage -> VisualTheme -> VisualSize -> string) =
        let pageDuplicates = pages |> List.map _.PageId |> duplicateValues
        let themeDuplicates = themes |> List.map _.ThemeId |> duplicateValues
        let sizeDuplicates = sizes |> List.map (fun size -> $"{size.Role}:{size.Width}x{size.Height}") |> duplicateValues

        let declarationDiagnostics =
            [ for page in pages do
                  if blank page.PageId then
                      "visual page id must be non-empty"
              for theme in themes do
                  if blank theme.ThemeId then
                      "visual theme id must be non-empty"
              for size in sizes do
                  if blank size.Role then
                      "visual size role must be non-empty"
                  if size.Width <= 0 || size.Height <= 0 then
                      $"visual size must be positive: {size.Role}"
              for duplicate in pageDuplicates do
                  $"duplicate page id: {duplicate}"
              for duplicate in themeDuplicates do
                  $"duplicate theme id: {duplicate}"
              for duplicate in sizeDuplicates do
                  $"duplicate size: {duplicate}" ]

        let orderedPages = pages |> List.sortBy (fun page -> page.Order, page.PageId)
        let orderedThemes = themes |> List.sortBy (fun theme -> theme.Order, theme.ThemeId)
        let orderedSizes = sizes |> List.sortBy (fun size -> size.Order, size.Role, size.Width, size.Height)

        let targets =
            [ for size in orderedSizes do
                  for theme in orderedThemes do
                      for page in orderedPages do
                          let relativePath = pathFor page theme size |> normalRelativePath
                          { TargetId = targetId page theme size relativePath
                            Page = page
                            Theme = theme
                            Size = size
                            RelativePath = relativePath
                            Required = page.Required } ]

        let targetDuplicates = targets |> List.map _.TargetId |> duplicateValues
        let pathDuplicates = targets |> List.map _.RelativePath |> duplicateValues

        let targetDiagnostics =
            [ for target in targets do
                  if not (pathIsSafe target.RelativePath) then
                      $"relative path escapes evidence root: {target.RelativePath}"
              for duplicate in targetDuplicates do
                  $"duplicate target id: {duplicate}"
              for duplicate in pathDuplicates do
                  $"duplicate relative path: {duplicate}" ]

        let diagnostics = declarationDiagnostics @ targetDiagnostics
        if diagnostics.IsEmpty then Ok targets else Result.Error diagnostics

module VisualCompleteness =
    let statusText status =
        match status with
        | VisualCaptureComplete -> "complete"
        | VisualCaptureMissing -> "missing"
        | VisualCaptureWrongSize -> "wrong-size"
        | VisualCaptureUndecodable -> "undecodable"
        | VisualCaptureDegraded -> "degraded"
        | VisualCaptureBlocked -> "blocked"

    let private normalizeRelativePath (relativePath: string) =
        relativePath.Replace('\\', '/').Trim()

    let private absolutePath evidenceRoot relativePath =
        Path.Combine(evidenceRoot, normalizeRelativePath relativePath).Replace('/', Path.DirectorySeparatorChar)

    let private hashFile path =
        use stream = File.OpenRead path
        use sha = SHA256.Create()
        sha.ComputeHash stream
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let private missingArtifact (target: VisualCaptureTarget) =
        { RelativePath = target.RelativePath
          Exists = false
          ByteCount = None
          DecodedWidth = None
          DecodedHeight = None
          ContentHash = None
          DecodeError = Some "missing" }

    let private record (target: VisualCaptureTarget) status (artifact: VisualCaptureArtifact option) reason diagnostics =
        { Target = target
          Status = status
          Artifact = artifact
          ExpectedWidth = target.Size.Width
          ExpectedHeight = target.Size.Height
          ObservedWidth = artifact |> Option.bind _.DecodedWidth
          ObservedHeight = artifact |> Option.bind _.DecodedHeight
          Reason = reason
          Diagnostics = diagnostics }

    let degraded (target: VisualCaptureTarget) reason =
        if String.IsNullOrWhiteSpace reason then
            record target VisualCaptureBlocked None (Some "missing degraded reason") [ "degraded capture requires a non-empty reason" ]
        else
            record target VisualCaptureDegraded None (Some reason) [ $"degraded capture: {reason}" ]

    let private validateOne evidenceRoot (target: VisualCaptureTarget) =
        let path = absolutePath evidenceRoot target.RelativePath

        if not (File.Exists path) then
            let artifact = missingArtifact target
            record target VisualCaptureMissing (Some artifact) (Some "missing artifact") [ $"missing screenshot: {target.RelativePath}" ]
        else
            let info = FileInfo path
            let byteCount = info.Length

            if byteCount = 0L then
                let artifact =
                    { RelativePath = target.RelativePath
                      Exists = true
                      ByteCount = Some byteCount
                      DecodedWidth = None
                      DecodedHeight = None
                      ContentHash = Some(hashFile path)
                      DecodeError = Some "zero-byte artifact" }

                record target VisualCaptureUndecodable (Some artifact) (Some "zero-byte artifact") [ $"zero-byte screenshot: {target.RelativePath}" ]
            else
                try
                    let contentHash = hashFile path
                    use bitmap = SKBitmap.Decode path

                    if isNull bitmap then
                        let artifact =
                            { RelativePath = target.RelativePath
                              Exists = true
                              ByteCount = Some byteCount
                              DecodedWidth = None
                              DecodedHeight = None
                              ContentHash = Some contentHash
                              DecodeError = Some "SKBitmap.Decode returned null" }

                        record target VisualCaptureUndecodable (Some artifact) (Some "undecodable PNG") [ $"undecodable screenshot: {target.RelativePath}" ]
                    else
                        let artifact =
                            { RelativePath = target.RelativePath
                              Exists = true
                              ByteCount = Some byteCount
                              DecodedWidth = Some bitmap.Width
                              DecodedHeight = Some bitmap.Height
                              ContentHash = Some contentHash
                              DecodeError = None }

                        if bitmap.Width = target.Size.Width && bitmap.Height = target.Size.Height then
                            record target VisualCaptureComplete (Some artifact) None []
                        else
                            let diagnostic =
                                $"wrong-size screenshot: {target.RelativePath} expected {target.Size.Width}x{target.Size.Height} observed {bitmap.Width}x{bitmap.Height}"

                            record target VisualCaptureWrongSize (Some artifact) (Some "wrong-size artifact") [ diagnostic ]
                with ex ->
                    let artifact =
                        { RelativePath = target.RelativePath
                          Exists = true
                          ByteCount = Some byteCount
                          DecodedWidth = None
                          DecodedHeight = None
                          ContentHash = None
                          DecodeError = Some ex.Message }

                    record target VisualCaptureUndecodable (Some artifact) (Some "artifact decode failed") [ $"undecodable screenshot: {target.RelativePath}: {ex.Message}" ]

    let private staleDiagnostics evidenceRoot (targets: VisualCaptureTarget list) =
        if Directory.Exists evidenceRoot then
            let targetPaths = targets |> List.map (fun target -> normalizeRelativePath target.RelativePath) |> Set.ofList

            Directory.EnumerateFiles(evidenceRoot, "*.png", SearchOption.AllDirectories)
            |> Seq.choose (fun path ->
                let relative = Path.GetRelativePath(evidenceRoot, path) |> normalizeRelativePath
                if targetPaths.Contains relative then None else Some $"stale artifact outside target matrix: {relative}")
            |> Seq.toList
        else
            []

    let validate evidenceRoot (targets: VisualCaptureTarget list) =
        let records = targets |> List.map (validateOne evidenceRoot)
        records, staleDiagnostics evidenceRoot targets

module VisualReviewerClassifications =
    let severityText severity =
        match severity with
        | VisualReviewerPending -> "pending"
        | VisualReviewerNone -> "none"
        | VisualReviewerMinor -> "minor"
        | VisualReviewerMajor -> "major"
        | VisualReviewerBlocking -> "blocking"

    let private parseSeverity (text: string) =
        match text.Trim().ToLowerInvariant() with
        | "pending"
        | "pending review" -> Ok VisualReviewerPending
        | "none" -> Ok VisualReviewerNone
        | "minor" -> Ok VisualReviewerMinor
        | "major" -> Ok VisualReviewerMajor
        | "blocking"
        | "critical" -> Ok VisualReviewerBlocking
        | other -> Result.Error $"malformed reviewer severity: {other}"

    let private sizeText (size: VisualSize) = $"{size.Role}:{size.Width}x{size.Height}"

    let writeTemplate (targets: VisualCaptureTarget list) =
        let header =
            [ "# Visual Readiness Reviewer Classifications"
              ""
              "| targetId | pageId | themeId | size | severity | defectClass | readinessImpact | reviewer | timestamp | notes |"
              "|---|---|---|---|---|---|---|---|---|---|" ]

        let rows =
            targets
            |> List.filter _.Required
            |> List.sortBy (fun target -> target.Size.Order, target.Theme.Order, target.Page.Order, target.TargetId)
            |> List.map (fun target ->
                $"| {target.TargetId} | {target.Page.PageId} | {target.Theme.ThemeId} | {sizeText target.Size} | pending | none | pending review | pending | pending | pending review |")

        String.concat Environment.NewLine (header @ rows) + Environment.NewLine

    let private splitRow (line: string) =
        line.Trim().Trim('|').Split('|', StringSplitOptions.None)
        |> Array.map (fun cell -> cell.Trim())
        |> Array.toList

    let private isTableRow (line: string) =
        let trimmed = line.Trim()
        trimmed.StartsWith("|") && trimmed.EndsWith("|") && not (trimmed.Contains("---"))

    let parse (markdown: string) (targets: VisualCaptureTarget list) =
        let targetIds = targets |> List.filter _.Required |> List.map _.TargetId
        let targetSet = targetIds |> Set.ofList

        let rows =
            markdown.Split([| "\r\n"; "\n" |], StringSplitOptions.None)
            |> Array.toList
            |> List.filter isTableRow
            |> List.map splitRow
            |> List.filter (fun cells ->
                match cells with
                | "targetId" :: _ -> false
                | _ -> true)

        let mutable seen: Set<string> = Set.empty
        let mutable duplicateIds: string list = []
        let mutable unknownIds: string list = []
        let mutable malformedRows: string list = []
        let mutable pendingIds: string list = []
        let mutable classifications: VisualReviewerClassification list = []

        for cells in rows do
            match cells with
            | targetId :: _pageId :: _themeId :: _size :: severityText :: defectClass :: impact :: reviewer :: timestamp :: notesParts ->
                if not (targetSet.Contains targetId) then
                    unknownIds <- targetId :: unknownIds
                elif seen.Contains targetId then
                    duplicateIds <- targetId :: duplicateIds
                else
                    seen <- seen.Add targetId

                match parseSeverity severityText with
                | Result.Error _diagnostic -> malformedRows <- String.concat " | " cells :: malformedRows
                | Ok severity ->
                    let notes = String.concat " | " notesParts

                    if severity = VisualReviewerPending
                       || impact.Equals("pending review", StringComparison.OrdinalIgnoreCase)
                       || notes.Contains("pending review", StringComparison.OrdinalIgnoreCase) then
                        pendingIds <- targetId :: pendingIds

                    classifications <-
                        { TargetId = targetId
                          Severity = severity
                          DefectClass = defectClass
                          ReadinessImpact = impact
                          Reviewer = reviewer
                          ReviewedAt = timestamp
                          Notes = notes }
                        :: classifications
            | _ -> malformedRows <- String.concat " | " cells :: malformedRows

        let missingIds =
            targetIds
            |> List.filter (fun targetId -> seen.Contains targetId |> not)

        let duplicateIds = duplicateIds |> List.rev
        let unknownIds = unknownIds |> List.rev
        let malformedRows = malformedRows |> List.rev
        let pendingIds = pendingIds |> List.rev
        let classifications = classifications |> List.rev

        let diagnostics =
            [ for targetId in missingIds do
                  $"missing reviewer row: {targetId}"
              for targetId in duplicateIds do
                  $"duplicate reviewer row: {targetId}"
              for targetId in unknownIds do
                  $"unknown reviewer target: {targetId}"
              for row in malformedRows do
                  $"malformed reviewer row: {row}"
              for targetId in pendingIds do
                  $"pending reviewer row: {targetId}" ]

        { Classifications = classifications
          MissingTargetIds = missingIds
          DuplicateTargetIds = duplicateIds
          UnknownTargetIds = unknownIds
          MalformedRows = malformedRows
          PendingTargetIds = pendingIds
          Diagnostics = diagnostics }

module VisualReadiness =
    let statusText status =
        match status with
        | VisualReadinessAccepted -> "accepted"
        | VisualReadinessPendingReview -> "pending-review"
        | VisualReadinessBlocked -> "blocked"
        | VisualReadinessEnvironmentLimited -> "environment-limited"
        | VisualReadinessIncomplete -> "incomplete"

    let private countByText (textOf: 'a -> string) (values: 'a list) =
        values
        |> List.countBy textOf
        |> List.sortBy fst

    let evaluate
        (runId: string)
        (evidenceRoot: string)
        (targets: VisualCaptureTarget list)
        (captures: VisualCaptureRecord list)
        (reviewerClassifications: VisualReviewerClassification list)
        (contactSheets: VisualContactSheet list)
        (caveats: string list)
        (acceptedExceptions: string list)
        =
        let requiredTargets = targets |> List.filter _.Required
        let requiredTargetIds = requiredTargets |> List.map _.TargetId |> Set.ofList
        let acceptedExceptionIds = acceptedExceptions |> Set.ofList
        let captureByTarget = captures |> List.map (fun capture -> capture.Target.TargetId, capture) |> Map.ofList

        let missingCaptureIds =
            requiredTargets
            |> List.choose (fun target ->
                if captureByTarget.ContainsKey target.TargetId then None else Some target.TargetId)

        let captureBlocks =
            requiredTargets
            |> List.choose (fun target ->
                captureByTarget
                |> Map.tryFind target.TargetId
                |> Option.bind (fun capture ->
                    if acceptedExceptionIds.Contains target.TargetId then
                        None
                    else
                        match capture.Status with
                        | VisualCaptureMissing
                        | VisualCaptureWrongSize
                        | VisualCaptureUndecodable
                        | VisualCaptureBlocked -> Some(target.TargetId, VisualCompleteness.statusText capture.Status)
                        | _ -> None))

        let degradedIds =
            requiredTargets
            |> List.choose (fun target ->
                captureByTarget
                |> Map.tryFind target.TargetId
                |> Option.bind (fun capture ->
                    if capture.Status = VisualCaptureDegraded && not (acceptedExceptionIds.Contains target.TargetId) then
                        Some target.TargetId
                    else
                        None))

        let reviewByTarget =
            reviewerClassifications
            |> List.filter (fun review -> requiredTargetIds.Contains review.TargetId)
            |> List.groupBy _.TargetId
            |> Map.ofList

        let missingReviewIds =
            requiredTargets
            |> List.choose (fun target ->
                match reviewByTarget |> Map.tryFind target.TargetId with
                | None -> Some target.TargetId
                | Some reviews when reviews |> List.exists (fun review -> review.Severity = VisualReviewerPending) -> Some target.TargetId
                | Some _ -> None)

        let duplicateReviewIds =
            reviewByTarget
            |> Map.toList
            |> List.choose (fun (targetId, reviews) -> if reviews.Length > 1 then Some targetId else None)

        let blockingReviewIds =
            reviewerClassifications
            |> List.choose (fun review ->
                if review.Severity = VisualReviewerBlocking && requiredTargetIds.Contains review.TargetId then
                    Some review.TargetId
                else
                    None)

        let diagnostics =
            [ for targetId in missingCaptureIds do
                  $"missing capture record: {targetId}"
              for targetId, status in captureBlocks do
                  $"blocking capture status: {targetId} {status}"
              for targetId in degradedIds do
                  $"degraded capture blocks accepted readiness: {targetId}"
              for targetId in missingReviewIds do
                  $"missing or pending reviewer classification: {targetId}"
              for targetId in duplicateReviewIds do
                  $"duplicate reviewer classification: {targetId}"
              for targetId in blockingReviewIds do
                  $"blocking reviewer defect: {targetId}" ]

        let status =
            if not missingCaptureIds.IsEmpty then
                VisualReadinessIncomplete
            elif not captureBlocks.IsEmpty || not duplicateReviewIds.IsEmpty || not blockingReviewIds.IsEmpty then
                VisualReadinessBlocked
            elif not degradedIds.IsEmpty then
                VisualReadinessEnvironmentLimited
            elif not missingReviewIds.IsEmpty then
                VisualReadinessPendingReview
            elif diagnostics.IsEmpty then
                VisualReadinessAccepted
            else
                VisualReadinessBlocked

        { RunId = runId
          EvidenceRoot = evidenceRoot
          Targets = targets
          Captures = captures
          ReviewerClassifications = reviewerClassifications
          ContactSheets = contactSheets
          CaptureStatusCounts = captures |> countByText (fun capture -> VisualCompleteness.statusText capture.Status)
          ReviewerStatusCounts = reviewerClassifications |> countByText (fun review -> VisualReviewerClassifications.severityText review.Severity)
          ReadinessStatus = status
          Caveats = caveats
          Diagnostics = diagnostics }

module VisualReadinessMarkdown =
    let startMarker = "<!-- FS.GG VISUAL READINESS START -->"
    let endMarker = "<!-- FS.GG VISUAL READINESS END -->"

    let private esc (text: string) =
        text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")

    let private q text = "\"" + esc text + "\""

    let private jsonStringArray values =
        "[" + (values |> List.map q |> String.concat ", ") + "]"

    let private jsonCounts values =
        values
        |> List.map (fun (name, count) -> $"    {q name}: {count}")
        |> String.concat ",\n"

    let private statusCountsText values =
        if List.isEmpty values then
            "none"
        else
            values
            |> List.map (fun (name, count) -> $"{name}={count}")
            |> String.concat ", "

    let renderSummary (report: VisualReadinessReport) =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore

        line "## Visual Readiness"
        line ""
        line $"- run: `{report.RunId}`"
        line $"- status: **{VisualReadiness.statusText report.ReadinessStatus}**"
        line $"- targets: `{report.Targets.Length}`"
        line $"- required targets: `{report.Targets |> List.filter _.Required |> List.length}`"
        line $"- capture status counts: `{statusCountsText report.CaptureStatusCounts}`"
        line $"- reviewer status counts: `{statusCountsText report.ReviewerStatusCounts}`"

        if not report.ContactSheets.IsEmpty then
            line ""
            line "### Contact Sheets"
            for sheet in report.ContactSheets do
                line $"- `{sheet.RelativePath}` ({sheet.SheetId})"

        let problemCaptures =
            report.Captures
            |> List.filter (fun capture -> capture.Status <> VisualCaptureComplete)

        if not problemCaptures.IsEmpty then
            line ""
            line "### Capture Diagnostics"
            for capture in problemCaptures do
                let reason = capture.Reason |> Option.defaultValue (VisualCompleteness.statusText capture.Status)
                line $"- `{capture.Target.TargetId}` {VisualCompleteness.statusText capture.Status}: {reason}"

        if not report.Caveats.IsEmpty then
            line ""
            line "### Caveats"
            for caveat in report.Caveats do
                line $"- {caveat}"

        if not report.Diagnostics.IsEmpty then
            line ""
            line "### Diagnostics"
            for diagnostic in report.Diagnostics do
                line $"- {diagnostic}"

        sb.ToString()

    let renderJson (report: VisualReadinessReport) =
        let targetJson =
            report.Targets
            |> List.map (fun target ->
                let size = $"{target.Size.Width}x{target.Size.Height}"
                let required = string target.Required |> fun value -> value.ToLowerInvariant()
                $"    {{ \"targetId\": {q target.TargetId}, \"pageId\": {q target.Page.PageId}, \"themeId\": {q target.Theme.ThemeId}, \"size\": {q size}, \"relativePath\": {q target.RelativePath}, \"required\": {required} }}")
            |> String.concat ",\n"

        let captureJson =
            report.Captures
            |> List.map (fun capture ->
                let observed =
                    match capture.ObservedWidth, capture.ObservedHeight with
                    | Some width, Some height -> q $"{width}x{height}"
                    | _ -> "null"

                $"    {{ \"targetId\": {q capture.Target.TargetId}, \"status\": {q (VisualCompleteness.statusText capture.Status)}, \"relativePath\": {q capture.Target.RelativePath}, \"observedSize\": {observed}, \"diagnostics\": {jsonStringArray capture.Diagnostics} }}")
            |> String.concat ",\n"

        let reviewerJson =
            report.ReviewerClassifications
            |> List.map (fun review ->
                $"    {{ \"targetId\": {q review.TargetId}, \"severity\": {q (VisualReviewerClassifications.severityText review.Severity)}, \"defectClass\": {q review.DefectClass}, \"readinessImpact\": {q review.ReadinessImpact}, \"reviewer\": {q review.Reviewer}, \"timestamp\": {q review.ReviewedAt}, \"notes\": {q review.Notes} }}")
            |> String.concat ",\n"

        let sheetJson =
            report.ContactSheets
            |> List.map (fun sheet ->
                $"    {{ \"sheetId\": {q sheet.SheetId}, \"relativePath\": {q sheet.RelativePath}, \"targetIds\": {jsonStringArray sheet.TargetIds}, \"missingTargetIds\": {jsonStringArray sheet.MissingTargetIds}, \"diagnostics\": {jsonStringArray sheet.Diagnostics} }}")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": {q report.RunId},"
              $"  \"evidenceRoot\": {q report.EvidenceRoot},"
              $"  \"targetCount\": {report.Targets.Length},"
              $"  \"requiredTargetCount\": {report.Targets |> List.filter _.Required |> List.length},"
              $"  \"readinessStatus\": {q (VisualReadiness.statusText report.ReadinessStatus)},"
              "  \"captureStatusCounts\": {"
              jsonCounts report.CaptureStatusCounts
              "  },"
              "  \"reviewerStatusCounts\": {"
              jsonCounts report.ReviewerStatusCounts
              "  },"
              "  \"targets\": ["
              targetJson
              "  ],"
              "  \"captures\": ["
              captureJson
              "  ],"
              "  \"reviewerClassifications\": ["
              reviewerJson
              "  ],"
              "  \"contactSheets\": ["
              sheetJson
              "  ],"
              $"  \"caveats\": {jsonStringArray report.Caveats},"
              $"  \"diagnostics\": {jsonStringArray report.Diagnostics}"
              "}" ]
        + "\n"

    let private countOccurrences (text: string) (pattern: string) =
        let mutable count = 0
        let mutable start = 0
        let mutable finished = false

        while not finished do
            let index = text.IndexOf(pattern, start, StringComparison.Ordinal)
            if index < 0 then
                finished <- true
            else
                count <- count + 1
                start <- index + pattern.Length

        count

    let updateManagedSection (existingText: string) (generatedMarkdown: string) : VisualSummarySectionUpdate =
        let startCount = countOccurrences existingText startMarker
        let endCount = countOccurrences existingText endMarker

        let sectionText =
            startMarker
            + Environment.NewLine
            + generatedMarkdown.TrimEnd()
            + Environment.NewLine
            + endMarker

        match startCount, endCount with
        | 0, 0 ->
            let separator =
                if String.IsNullOrEmpty existingText then
                    ""
                elif existingText.EndsWith(Environment.NewLine, StringComparison.Ordinal) then
                    Environment.NewLine
                else
                    Environment.NewLine + Environment.NewLine

            { UpdatedText = existingText + separator + sectionText + Environment.NewLine
              SafeToWrite = true
              InsertedMarkers = true
              Diagnostics = [] }
        | 1, 1 ->
            let startIndex = existingText.IndexOf(startMarker, StringComparison.Ordinal)
            let endIndex = existingText.IndexOf(endMarker, StringComparison.Ordinal)

            if startIndex > endIndex then
                { UpdatedText = existingText
                  SafeToWrite = false
                  InsertedMarkers = false
                  Diagnostics = [ "visual readiness managed markers are reversed" ] }
            else
                let prefix = existingText.Substring(0, startIndex)
                let suffix = existingText.Substring(endIndex + endMarker.Length)

                { UpdatedText = prefix + sectionText + suffix
                  SafeToWrite = true
                  InsertedMarkers = false
                  Diagnostics = [] }
        | _ ->
            { UpdatedText = existingText
              SafeToWrite = false
              InsertedMarkers = false
              Diagnostics = [ "visual readiness managed section must contain exactly one start marker and one end marker" ] }

module VisualInspectionValidation =
    let rule ruleId = { RuleId = ruleId; Required = true }

    let defaultRules =
        [ "required-region-present"
          "required-region-painted"
          "ordinary-regions-disjoint"
          "text-contained-in-owner"
          "clip-intent-classified"
          "overlay-overlap-classified"
          "visual-order-stable"
          "unsupported-required-fact"
          "identity-stable" ]
        |> List.map rule

    let private isBlank value = String.IsNullOrWhiteSpace value

    let private isFiniteRect (rect: Rect) =
        not (Double.IsNaN rect.X)
        && not (Double.IsNaN rect.Y)
        && not (Double.IsNaN rect.Width)
        && not (Double.IsNaN rect.Height)
        && rect.Width >= 0.0
        && rect.Height >= 0.0

    let private intersects (a: Rect) (b: Rect) =
        a.X < b.X + b.Width
        && a.X + a.Width > b.X
        && a.Y < b.Y + b.Height
        && a.Y + a.Height > b.Y

    let private contains (outer: Rect) (inner: Rect) =
        inner.X >= outer.X
        && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width
        && inner.Y + inner.Height <= outer.Y + outer.Height

    let private isOverlayRole role =
        match role with
        | VisualInspectionSurfaceRole.Overlay
        | VisualInspectionSurfaceRole.Popup
        | VisualInspectionSurfaceRole.Floating -> true
        | _ -> false

    let private finding ruleId severity nodeIds regionIds message expected actual =
        VisualInspection.finding ruleId severity nodeIds regionIds message expected actual

    let private affectedIds (finding: VisualInspectionFinding) =
        finding.AffectedNodeIds @ finding.AffectedRegionIds |> List.sort

    let private exceptionValid (ex: VisualInspectionException) =
        not (isBlank ex.ExceptionId)
        && not (isBlank ex.RuleId)
        && not (isBlank ex.OwnerId)
        && not ex.AffectedIds.IsEmpty
        && not (isBlank ex.Reason)

    let private exceptionMatches (finding: VisualInspectionFinding) (ex: VisualInspectionException) =
        if not (exceptionValid ex) || ex.RuleId <> finding.RuleId then
            false
        else
            Set.ofList ex.AffectedIds = Set.ofList (affectedIds finding)

    let private requiredRegionPresent (artifact: VisualInspectionArtifact) requiredRegionIds =
        let regionsById = artifact.Regions |> List.map (fun region -> region.RegionId, region) |> Map.ofList
        let requiredIds =
            (requiredRegionIds @ (artifact.Regions |> List.filter _.Required |> List.map _.RegionId))
            |> List.distinct

        [ for regionId in requiredIds do
              match regionsById |> Map.tryFind regionId with
              | None ->
                  finding
                      "required-region-present"
                      VisualInspectionSeverity.Blocking
                      []
                      [ regionId ]
                      $"required region `{regionId}` is missing"
                      "required region present with finite bounds"
                      "missing"
              | Some region ->
                  match region.Bounds with
                  | Some bounds when isFiniteRect bounds -> ()
                  | _ ->
                      finding
                          "required-region-present"
                          VisualInspectionSeverity.Blocking
                          region.OwnerNodeIds
                          [ region.RegionId ]
                          $"required region `{region.RegionId}` has missing or invalid bounds"
                          "finite non-negative bounds"
                          "missing or invalid bounds" ]

    let private requiredRegionPainted (artifact: VisualInspectionArtifact) =
        let coverageByTarget =
            artifact.PaintCoverage
            |> List.groupBy _.TargetId
            |> Map.ofList

        [ for region in artifact.Regions do
              if region.Required then
                  let coverage = coverageByTarget |> Map.tryFind region.RegionId |> Option.defaultValue []
                  if coverage.IsEmpty then
                      finding
                          "required-region-painted"
                          VisualInspectionSeverity.Blocking
                          region.OwnerNodeIds
                          [ region.RegionId ]
                          $"required region `{region.RegionId}` has no paint coverage fact"
                          "complete intentional paint coverage"
                          "missing coverage fact"
                  else
                      for fact in coverage do
                          match fact.CoverageStatus with
                          | VisualInspectionCoverageStatus.Complete -> ()
                          | VisualInspectionCoverageStatus.Unsupported
                          | VisualInspectionCoverageStatus.Unavailable ->
                              finding
                                  "required-region-painted"
                                  VisualInspectionSeverity.Unsupported
                                  region.OwnerNodeIds
                                  [ region.RegionId ]
                                  $"required region `{region.RegionId}` paint coverage is unsupported"
                                  "complete intentional paint coverage"
                                  (VisualInspection.coverageStatusText fact.CoverageStatus)
                          | VisualInspectionCoverageStatus.Partial
                          | VisualInspectionCoverageStatus.Missing ->
                              finding
                                  "required-region-painted"
                                  VisualInspectionSeverity.Blocking
                                  region.OwnerNodeIds
                                  [ region.RegionId ]
                                  $"required region `{region.RegionId}` is not fully painted"
                                  "complete intentional paint coverage"
                                  (VisualInspection.coverageStatusText fact.CoverageStatus) ]

    let private ordinaryRegionsDisjoint (artifact: VisualInspectionArtifact) =
        [ for firstIndex, first in artifact.Regions |> List.indexed do
              for second in artifact.Regions |> List.skip (firstIndex + 1) do
                  match first.Bounds, second.Bounds with
                  | Some a, Some b when not (isOverlayRole first.Role) && not (isOverlayRole second.Role) && intersects a b && not (contains a b) && not (contains b a) ->
                      finding
                          "ordinary-regions-disjoint"
                          VisualInspectionSeverity.Blocking
                          (first.OwnerNodeIds @ second.OwnerNodeIds)
                          [ first.RegionId; second.RegionId ]
                          $"ordinary regions `{first.RegionId}` and `{second.RegionId}` overlap"
                          "ordinary regions are disjoint unless explicitly classified"
                          "overlap"
                  | _ -> () ]

    let private textContainedInOwner (artifact: VisualInspectionArtifact) =
        [ for textRun in artifact.TextRuns do
              if textRun.Required then
                  match textRun.FitStatus with
                  | VisualInspectionFitStatus.Inside ->
                      match textRun.OwnerBounds, textRun.TextBounds with
                      | Some owner, Some textBounds when not (contains owner textBounds) ->
                          finding
                              "text-contained-in-owner"
                              VisualInspectionSeverity.Blocking
                              [ textRun.OwnerNodeId ]
                              []
                              $"text `{textRun.TextId}` is classified inside but exceeds its owner bounds"
                              "text bounds inside owner bounds"
                              "outside owner bounds"
                      | _ -> ()
                  | VisualInspectionFitStatus.Wrapped
                  | VisualInspectionFitStatus.Truncated -> ()
                  | VisualInspectionFitStatus.Overflow
                  | VisualInspectionFitStatus.Clipped ->
                      finding
                          "text-contained-in-owner"
                          VisualInspectionSeverity.Blocking
                          [ textRun.OwnerNodeId ]
                          []
                          $"text `{textRun.TextId}` does not fit inside owner `{textRun.OwnerNodeId}`"
                          "inside, wrapped, or intentionally truncated text"
                          (VisualInspection.fitStatusText textRun.FitStatus)
                  | VisualInspectionFitStatus.Unsupported
                  | VisualInspectionFitStatus.Unavailable ->
                      finding
                          "text-contained-in-owner"
                          VisualInspectionSeverity.Unsupported
                          [ textRun.OwnerNodeId ]
                          []
                          $"text `{textRun.TextId}` fit facts are unavailable"
                          "inspectable text fit facts"
                          (VisualInspection.fitStatusText textRun.FitStatus) ]

    let private clipIntentClassified (artifact: VisualInspectionArtifact) =
        [ for clip in artifact.ClipFacts do
              match clip.ClipStatus with
              | VisualInspectionClipStatus.None
              | VisualInspectionClipStatus.Intentional -> ()
              | VisualInspectionClipStatus.Accidental ->
                  finding
                      "clip-intent-classified"
                      VisualInspectionSeverity.Blocking
                      [ clip.NodeId ]
                      []
                      $"node `{clip.NodeId}` has accidental clipping"
                      "no clipping or intentional owned clipping"
                      "accidental clipping"
              | VisualInspectionClipStatus.Unsupported
              | VisualInspectionClipStatus.Unavailable ->
                  finding
                      "clip-intent-classified"
                      VisualInspectionSeverity.Unsupported
                      [ clip.NodeId ]
                      []
                      $"node `{clip.NodeId}` clipping facts are unavailable"
                      "inspectable clipping facts"
                      (VisualInspection.clipStatusText clip.ClipStatus) ]

    let private overlayOverlapClassified (artifact: VisualInspectionArtifact) =
        [ for firstIndex, first in artifact.Regions |> List.indexed do
              for second in artifact.Regions |> List.skip (firstIndex + 1) do
                  if isOverlayRole first.Role || isOverlayRole second.Role then
                      match first.Bounds, second.Bounds with
                      | Some a, Some b when intersects a b ->
                          finding
                              "overlay-overlap-classified"
                              VisualInspectionSeverity.Blocking
                              (first.OwnerNodeIds @ second.OwnerNodeIds)
                              [ first.RegionId; second.RegionId ]
                              $"overlay overlap between `{first.RegionId}` and `{second.RegionId}` needs classification"
                              "explicit owner and reason for overlay overlap"
                              "unclassified overlay overlap"
                      | _ -> () ]

    let private unsupportedRequiredFacts (artifact: VisualInspectionArtifact) (environmentLimitations: string list) =
        [ for fact in artifact.UnsupportedFacts do
              if fact.Required then
                  let severity =
                      if fact.EnvironmentLimited || not environmentLimitations.IsEmpty then
                          VisualInspectionSeverity.EnvironmentLimited
                      else
                          VisualInspectionSeverity.Unsupported

                  finding
                      "unsupported-required-fact"
                      severity
                      (fact.OwnerId |> Option.map List.singleton |> Option.defaultValue [])
                      []
                      $"required inspection fact `{fact.Fact}` is unsupported"
                      "required fact inspectable or explicitly environment-limited"
                      fact.Reason ]

    let private identityStable (artifact: VisualInspectionArtifact) (previous: VisualInspectionArtifact option) =
        match previous with
        | None -> []
        | Some previousArtifact ->
            let previousIds = previousArtifact.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.map _.NodeId |> Set.ofList
            let currentIds = artifact.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.map _.NodeId |> Set.ofList

            if previousIds = currentIds then
                []
            else
                [ finding
                      "identity-stable"
                      VisualInspectionSeverity.Blocking
                      (Set.union previousIds currentIds |> Set.toList)
                      []
                      "static node identities changed between inspection runs"
                      "same static node id set"
                      "identity set changed" ]

    let private visualOrderStable (artifact: VisualInspectionArtifact) (previous: VisualInspectionArtifact option) =
        match previous with
        | None -> []
        | Some previousArtifact ->
            let orderOf (a: VisualInspectionArtifact) =
                a.Nodes |> List.filter (fun n -> not n.Dynamic) |> List.sortBy (fun n -> n.ZOrder, n.NodeId) |> List.map _.NodeId

            let previousOrder = orderOf previousArtifact
            let currentOrder = orderOf artifact

            if previousOrder = currentOrder then
                []
            else
                [ finding
                      "visual-order-stable"
                      VisualInspectionSeverity.Blocking
                      (previousOrder @ currentOrder |> List.distinct)
                      []
                      "static node visual order changed between inspection runs"
                      "same static visual order"
                      "visual order changed" ]

    let private findingsForRule (check: VisualInspectionValidationCheck) (rule: VisualInspectionRule) =
        match rule.RuleId with
        | "required-region-present" -> requiredRegionPresent check.Artifact check.RequiredRegionIds
        | "required-region-painted" -> requiredRegionPainted check.Artifact
        | "ordinary-regions-disjoint" -> ordinaryRegionsDisjoint check.Artifact
        | "text-contained-in-owner" -> textContainedInOwner check.Artifact
        | "clip-intent-classified" -> clipIntentClassified check.Artifact
        | "overlay-overlap-classified" -> overlayOverlapClassified check.Artifact
        | "unsupported-required-fact" -> unsupportedRequiredFacts check.Artifact check.EnvironmentLimitations
        | "identity-stable" -> identityStable check.Artifact check.PreviousArtifact
        | "visual-order-stable" -> visualOrderStable check.Artifact check.PreviousArtifact
        | unknown when rule.Required ->
            [ finding unknown VisualInspectionSeverity.Unsupported [] [] $"rule `{unknown}` is not implemented" "implemented validation rule" "unknown rule" ]
        | _ -> []

    let validateCheck (check: VisualInspectionValidationCheck) =
        let invalidExceptions =
            check.Exceptions
            |> List.filter (exceptionValid >> not)
            |> List.map _.ExceptionId

        let initialFindings =
            check.Rules
            |> List.collect (findingsForRule check)
            |> List.append check.Artifact.Findings
            |> List.sortBy _.FindingId

        let validExceptions = check.Exceptions |> List.filter exceptionValid
        let applied = ResizeArray<string>()

        let findings =
            initialFindings
            |> List.map (fun f ->
                match validExceptions |> List.tryFind (exceptionMatches f) with
                | Some ex when f.Severity = VisualInspectionSeverity.Blocking ->
                    applied.Add ex.ExceptionId
                    { f with
                        Severity = VisualInspectionSeverity.Pass
                        ExceptionId = Some ex.ExceptionId
                        Diagnostics = f.Diagnostics @ [ $"accepted by visual inspection exception `{ex.ExceptionId}`: {ex.Reason}" ] }
                | _ -> f)
            |> List.distinctBy _.FindingId
            |> List.sortBy _.FindingId

        let appliedIds = applied |> Seq.distinct |> Seq.toList
        let unused =
            validExceptions
            |> List.map _.ExceptionId
            |> List.filter (fun id -> not (List.contains id appliedIds))

        let diagnostics =
            (VisualInspection.artifactDiagnostics check.Artifact)
            @ (invalidExceptions |> List.map (fun id -> $"invalid visual inspection exception: {id}"))
            @ (unused |> List.map (fun id -> $"unused visual inspection exception: {id}"))
            |> List.distinct

        let has severity =
            findings |> List.exists (fun f -> f.Severity = severity)

        let status =
            if not invalidExceptions.IsEmpty || has VisualInspectionSeverity.Blocking then
                VisualInspectionStatus.Blocked
            elif has VisualInspectionSeverity.EnvironmentLimited then
                VisualInspectionStatus.EnvironmentLimited
            elif has VisualInspectionSeverity.Unsupported then
                if check.EnvironmentLimitations.IsEmpty then
                    VisualInspectionStatus.Unsupported
                else
                    VisualInspectionStatus.EnvironmentLimited
            else
                match check.Artifact.ReadinessStatus with
                | VisualInspectionStatus.NotRun
                | VisualInspectionStatus.NotInspected
                | VisualInspectionStatus.Incomplete -> VisualInspectionStatus.Incomplete
                | VisualInspectionStatus.Blocked -> VisualInspectionStatus.Blocked
                | VisualInspectionStatus.Unsupported -> VisualInspectionStatus.Unsupported
                | VisualInspectionStatus.EnvironmentLimited -> VisualInspectionStatus.EnvironmentLimited
                | VisualInspectionStatus.Accepted -> VisualInspectionStatus.Accepted

        { ArtifactId = check.Artifact.ArtifactId
          ReadinessStatus = status
          Findings = findings
          AppliedExceptions = appliedIds
          InvalidExceptions = invalidExceptions
          UnusedExceptions = unused
          Diagnostics = diagnostics }

    let validate artifact rules exceptions =
        validateCheck
            { Artifact = artifact
              Rules = rules
              Exceptions = exceptions
              RequiredRegionIds = []
              PreviousArtifact = None
              EnvironmentLimitations = [] }

module VisualInspectionReadiness =
    let private statusRank status =
        match status with
        | VisualInspectionStatus.Blocked -> 0
        | VisualInspectionStatus.Unsupported -> 1
        | VisualInspectionStatus.EnvironmentLimited -> 2
        | VisualInspectionStatus.Incomplete -> 3
        | VisualInspectionStatus.NotRun -> 4
        | VisualInspectionStatus.NotInspected -> 5
        | VisualInspectionStatus.Accepted -> 6

    let private worstStatus statuses =
        statuses
        |> List.sortBy statusRank
        |> List.tryHead
        |> Option.defaultValue VisualInspectionStatus.Accepted

    let private countBy values =
        values |> List.countBy id |> List.sortBy fst

    let aggregate
        (runId: string)
        (artifacts: VisualInspectionArtifact list)
        (results: VisualInspectionValidationResult list)
        (relatedVisualEvidence: string list)
        (caveats: string list)
        =
        let resultByArtifact = results |> List.map (fun result -> result.ArtifactId, result) |> Map.ofList
        let statuses =
            artifacts
            |> List.map (fun artifact ->
                resultByArtifact
                |> Map.tryFind artifact.ArtifactId
                |> Option.map _.ReadinessStatus
                |> Option.defaultValue artifact.ReadinessStatus)

        let findings =
            results |> List.collect _.Findings

        { RunId = runId
          OverallStatus = worstStatus statuses
          ArtifactCount = artifacts.Length
          InspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus <> VisualInspectionStatus.NotInspected && a.ReadinessStatus <> VisualInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          NotInspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus = VisualInspectionStatus.NotInspected)
            |> List.map _.Scope.ScopeId
            |> List.sort
          NotRunScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus = VisualInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          StatusCounts = statuses |> List.map VisualInspection.statusText |> countBy
          FindingCounts = findings |> List.map (fun finding -> VisualInspection.severityText finding.Severity) |> countBy
          BlockingFindings = findings |> List.filter (fun finding -> finding.Severity = VisualInspectionSeverity.Blocking)
          UnsupportedFacts = artifacts |> List.collect _.UnsupportedFacts
          AcceptedExceptions = results |> List.collect _.AppliedExceptions |> List.distinct |> List.sort
          InvalidExceptions = results |> List.collect _.InvalidExceptions |> List.distinct |> List.sort
          RelatedVisualEvidence = relatedVisualEvidence |> List.distinct |> List.sort
          Caveats = caveats
          Diagnostics = results |> List.collect _.Diagnostics |> List.distinct }

module VisualInspectionMarkdown =
    let startMarker = "<!-- FS.GG VISUAL INSPECTION START -->"
    let endMarker = "<!-- FS.GG VISUAL INSPECTION END -->"

    let private esc (text: string) =
        text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")

    let private q text = "\"" + esc text + "\""

    let private jsonStringArray values =
        "[" + (values |> List.map q |> String.concat ", ") + "]"

    let private jsonCounts values =
        values
        |> List.map (fun (name, count) -> $"    {q name}: {count}")
        |> String.concat ",\n"

    let private countsText values =
        if List.isEmpty values then
            "none"
        else
            values |> List.map (fun (name, count) -> $"{name}={count}") |> String.concat ", "

    let renderSummary (summary: VisualInspectionSummary) =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore

        let inspectedScopesText = String.concat ", " summary.InspectedScopes

        line "## Visual Inspection"
        line ""
        line $"- run: `{summary.RunId}`"
        line $"- status: **{VisualInspection.statusText summary.OverallStatus}**"
        line $"- artifacts: `{summary.ArtifactCount}`"
        line $"- inspected scopes: `{inspectedScopesText}`"
        line $"- status counts: `{countsText summary.StatusCounts}`"
        line $"- finding counts: `{countsText summary.FindingCounts}`"

        if not summary.BlockingFindings.IsEmpty then
            line ""
            line "### Blocking Findings"
            line "| finding | rule | affected | message |"
            line "|---|---|---|---|"
            for finding in summary.BlockingFindings do
                let affected = String.concat ", " (finding.AffectedRegionIds @ finding.AffectedNodeIds)
                line $"| `{finding.FindingId}` | `{finding.RuleId}` | `{affected}` | {finding.Message} |"

        if not summary.UnsupportedFacts.IsEmpty then
            line ""
            line "### Unsupported Facts"
            for fact in summary.UnsupportedFacts do
                let owner = fact.OwnerId |> Option.defaultValue "scope"
                line $"- `{fact.Fact}` on `{owner}`: {fact.Reason}"

        if not summary.AcceptedExceptions.IsEmpty then
            line ""
            line "### Accepted Exceptions"
            for exceptionId in summary.AcceptedExceptions do
                line $"- `{exceptionId}`"

        if not summary.InvalidExceptions.IsEmpty then
            line ""
            line "### Invalid Exceptions"
            for exceptionId in summary.InvalidExceptions do
                line $"- `{exceptionId}`"

        if not summary.RelatedVisualEvidence.IsEmpty then
            line ""
            line "### Related Visual Evidence"
            for path in summary.RelatedVisualEvidence do
                line $"- `{path}`"

        if not summary.Caveats.IsEmpty then
            line ""
            line "### Caveats"
            for caveat in summary.Caveats do
                line $"- {caveat}"

        if not summary.Diagnostics.IsEmpty then
            line ""
            line "### Diagnostics"
            for diagnostic in summary.Diagnostics do
                line $"- {diagnostic}"

        sb.ToString()

    let renderJson (summary: VisualInspectionSummary) =
        let findingJson =
            summary.BlockingFindings
            |> List.map (fun finding ->
                let affected = finding.AffectedRegionIds @ finding.AffectedNodeIds
                $"    {{ \"findingId\": {q finding.FindingId}, \"ruleId\": {q finding.RuleId}, \"severity\": {q (VisualInspection.severityText finding.Severity)}, \"affectedIds\": {jsonStringArray affected}, \"message\": {q finding.Message} }}")
            |> String.concat ",\n"

        let unsupportedJson =
            summary.UnsupportedFacts
            |> List.map (fun fact ->
                let ownerJson = fact.OwnerId |> Option.map q |> Option.defaultValue "null"
                $"    {{ \"fact\": {q fact.Fact}, \"ownerId\": {ownerJson}, \"required\": {fact.Required.ToString().ToLowerInvariant()}, \"reason\": {q fact.Reason}, \"environmentLimited\": {fact.EnvironmentLimited.ToString().ToLowerInvariant()} }}")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": {q summary.RunId},"
              $"  \"overallStatus\": {q (VisualInspection.statusText summary.OverallStatus)},"
              $"  \"artifactCount\": {summary.ArtifactCount},"
              $"  \"inspectedScopes\": {jsonStringArray summary.InspectedScopes},"
              $"  \"notInspectedScopes\": {jsonStringArray summary.NotInspectedScopes},"
              $"  \"notRunScopes\": {jsonStringArray summary.NotRunScopes},"
              "  \"statusCounts\": {"
              jsonCounts summary.StatusCounts
              "  },"
              "  \"findingCounts\": {"
              jsonCounts summary.FindingCounts
              "  },"
              "  \"blockingFindings\": ["
              findingJson
              "  ],"
              "  \"unsupportedFacts\": ["
              unsupportedJson
              "  ],"
              $"  \"acceptedExceptions\": {jsonStringArray summary.AcceptedExceptions},"
              $"  \"invalidExceptions\": {jsonStringArray summary.InvalidExceptions},"
              $"  \"relatedVisualEvidence\": {jsonStringArray summary.RelatedVisualEvidence},"
              $"  \"caveats\": {jsonStringArray summary.Caveats},"
              $"  \"diagnostics\": {jsonStringArray summary.Diagnostics}"
              "}" ]
        + "\n"

    let private countOccurrences (text: string) (pattern: string) =
        let mutable count = 0
        let mutable start = 0
        let mutable finished = false

        while not finished do
            let index = text.IndexOf(pattern, start, StringComparison.Ordinal)
            if index < 0 then
                finished <- true
            else
                count <- count + 1
                start <- index + pattern.Length

        count

    let updateManagedSection (existingText: string) (generatedMarkdown: string) =
        let startCount = countOccurrences existingText startMarker
        let endCount = countOccurrences existingText endMarker
        let sectionText = startMarker + Environment.NewLine + generatedMarkdown.TrimEnd() + Environment.NewLine + endMarker

        match startCount, endCount with
        | 0, 0 ->
            let separator =
                if String.IsNullOrEmpty existingText then
                    ""
                elif existingText.EndsWith(Environment.NewLine, StringComparison.Ordinal) then
                    Environment.NewLine
                else
                    Environment.NewLine + Environment.NewLine

            { UpdatedText = existingText + separator + sectionText + Environment.NewLine
              SafeToWrite = true
              InsertedMarkers = true
              Diagnostics = [] }
        | 1, 1 ->
            let startIndex = existingText.IndexOf(startMarker, StringComparison.Ordinal)
            let endIndex = existingText.IndexOf(endMarker, StringComparison.Ordinal)

            if startIndex > endIndex then
                { UpdatedText = existingText
                  SafeToWrite = false
                  InsertedMarkers = false
                  Diagnostics = [ "visual inspection managed markers are reversed" ] }
            else
                let prefix = existingText.Substring(0, startIndex)
                let suffix = existingText.Substring(endIndex + endMarker.Length)

                { UpdatedText = prefix + sectionText + suffix
                  SafeToWrite = true
                  InsertedMarkers = false
                  Diagnostics = [] }
        | _ ->
            { UpdatedText = existingText
              SafeToWrite = false
              InsertedMarkers = false
              Diagnostics = [ "visual inspection managed section must contain exactly one start marker and one end marker" ] }

module GeneratedProductAssertions =
    let summarize expectation =
        let packages =
            expectation.PackageReferences
            |> List.map (fun package -> if package.Required then package.PackageId else $"!{package.PackageId}")
            |> String.concat ", "

        $"{expectation.Profile}: files={expectation.RequiredFiles.Length}; forbidden={expectation.ForbiddenPrefixes.Length}; packages={packages}"

    let validateDefaultInteractiveLaunch (source: string) =
        let defaultBranch =
            let marker = "| _ ->"
            let index = source.LastIndexOf(marker, StringComparison.Ordinal)

            if index >= 0 then
                source.Substring(index)
            else
                source

        let contains (value: string) =
            defaultBranch.Contains(value, StringComparison.Ordinal)

        let diagnostics =
            [ if not (contains "Viewer.runApp viewerOptions generatedHost") then
                  "default executable must call Viewer.runApp viewerOptions generatedHost"
              if not (contains "mode=interactive-window") then
                  "default executable must report mode=interactive-window"
              if not (contains "accessible-window=true" || contains "window-visible=observed:true") then
                  "default executable must claim an accessible desktop window"
              if contains "Viewer.runBounded" then
                  "default executable must not use Viewer.runBounded bounded evidence"
              if contains "first-frame-only=true" || contains "exit after first frame" then
                  "default executable must not exit after first frame"
              if contains "SceneEvidence.render" then
                  "default executable must not substitute scene-only metadata"
              if contains "self-closed-for-evidence=true" then
                  "default executable must not report evidence self-close"
              if contains "control-count" || contains "count controls" || contains "print metadata" then
                  "default executable must not be metadata-only"
              if contains "mode=persistent-evidence" then
                  "default executable must keep persistent-evidence behind explicit flags" ]

        { InteractiveLaunchRequired = List.isEmpty diagnostics
          Diagnostics = diagnostics }

    let validateWindowDiagnostics (check: GeneratedWindowDiagnosticCheck) =
        let contains (value: string) =
            check.Output.Contains(value, StringComparison.OrdinalIgnoreCase)

        let statusIsFailureClass =
            contains "status=degraded" || contains "status=unsupported" || contains "status=failed"

        let diagnostics =
            [ if not statusIsFailureClass then
                  "window diagnostics must report degraded unsupported or failed status"
              for failureClass in check.RequiredFailureClasses do
                  if not (contains $"diagnostic-class={failureClass}" || contains $"failure-class={failureClass}") then
                      $"missing generated diagnostic failure class: {failureClass}"
              for fact in check.RequiredNativeFacts do
                  if not (contains $"{fact}=observed:true"
                          || contains $"{fact}=observed:false"
                          || contains $"{fact}=unsupported"
                          || contains $"{fact}=unavailable") then
                      $"missing observable-vs-unsupported native fact: {fact}"
              if contains "private runtime fallback" && not (contains "fallback-full-desktop-session=false") then
                  "private runtime fallback must be disclosed as not a full desktop session"
              if contains "taskbar-only" && contains "status=ok" then
                  "taskbar-only launch must not be reported as status=ok" ]

        { DiagnosticsComplete = List.isEmpty diagnostics
          Diagnostics = diagnostics }

module LocalConsumerPackages =
    let report feedPath (packages: LocalConsumerPackage list) =
        let packageLines =
            packages
            |> List.map (fun package -> $"""<PackageReference Include="{package.PackageId}" Version="{package.Version}" />""")
            |> String.concat Environment.NewLine

        { FeedPath = feedPath
          Packages = packages
          ConsumerConfigSnippet = packageLines
          NuGetConfigSnippet = Some $"<add key=\"local\" value=\"{feedPath}\" />"
          RestoreCommand = "dotnet restore --source " + feedPath
          DriftDiagnostics = [] }

    let classifyDrift (expected: LocalConsumerPackage list) (actual: LocalConsumerPackage list) =
        expected
        |> List.choose (fun package ->
            let actualPackage =
                actual |> List.tryFind (fun candidate -> candidate.PackageId = package.PackageId)

            match actualPackage with
            | Some current when current.Version = package.Version -> None
            | Some current ->
                Some
                    { PackageId = package.PackageId
                      ExpectedVersion = package.Version
                      ActualVersion = Some current.Version
                      FeedPath = package.FeedPath
                      RemediationCommand = "dotnet fake run build.fsx --target PackLocal" }
            | None ->
                Some
                    { PackageId = package.PackageId
                      ExpectedVersion = package.Version
                      ActualVersion = None
                      FeedPath = package.FeedPath
                      RemediationCommand = "dotnet fake run build.fsx --target PackLocal" })

module GeneratedConsumerValidation =
    let summarize (result: GeneratedValidationResult) =
        let evidence = result.EvidencePath |> Option.defaultValue "none"
        let diagnostics = result.Diagnostics |> String.concat "; "
        $"{result.Category}: elapsed={result.Elapsed}; command={result.CommandContext}; evidence={evidence}; diagnostics={diagnostics}"

    let verifyPackageResolution check =
        let drift = LocalConsumerPackages.classifyDrift check.RequestedPackages check.ResolvedPackages

        let nu1603 =
            check.RestoreWarnings
            |> List.filter (fun warning -> warning.Contains("NU1603", StringComparison.OrdinalIgnoreCase))

        let missingSources =
            check.PackageSources |> List.isEmpty

        let diagnostics =
            [ if missingSources then
                  "missing package sources"
              for warning in nu1603 do
                  $"restore warning: {warning}"
              for item in drift do
                  let actual = item.ActualVersion |> Option.defaultValue "missing"
                  $"package mismatch: {item.PackageId} requested={item.ExpectedVersion} resolved={actual}" ]

        let failureReason =
            if not (List.isEmpty nu1603) then
                Some "NU1603"
            elif not (List.isEmpty drift) then
                Some "version-mismatch"
            elif missingSources then
                Some "missing-package-sources"
            else
                None

        { ExactMatch = failureReason.IsNone
          FailureReason = failureReason
          Diagnostics = diagnostics }

    let verifyGeneratedTests check =
        let diagnostics =
            [ if check.TestsExist && not check.TestsRan then
                  "generated tests exist but did not run"
              if check.TestsRan && not check.VerifyRan then
                  "generated tests ran outside generated Verify" ]

        let reason =
            if check.TestsExist && not check.TestsRan then
                Some "missing-generated-test-execution"
            elif check.TestsRan && not check.VerifyRan then
                Some "verify-target-not-authoritative"
            else
                None

        { Authoritative = reason.IsNone
          NonAuthoritativeReason = reason
          Diagnostics = diagnostics }

    let selectVisualEvidence request =
        if request.ScreenshotAvailable then
            { EvidenceKind = Screenshot
              BoardReadable = request.BoardReadable
              InputOrProgressObserved = request.InputOrProgressObserved
              FallbackReason = None
              UnsupportedReason = None
              Diagnostics = [ "screenshot preferred for supported generated game evidence" ] }
        elif request.PixelReadbackAvailable then
            { EvidenceKind = PixelReadback
              BoardReadable = request.BoardReadable
              InputOrProgressObserved = request.InputOrProgressObserved
              FallbackReason = Some "screenshot unavailable; pixel-readback selected"
              UnsupportedReason = None
              Diagnostics = [ "pixel-readback fallback selected"; "screenshot unavailable" ] }
        else
            let reason = request.UnsupportedReason |> Option.defaultValue "no screenshot or pixel-readback path available"

            { EvidenceKind = UnsupportedHost
              BoardReadable = None
              InputOrProgressObserved = None
              FallbackReason = None
              UnsupportedReason = Some reason
              Diagnostics = [ $"unsupported-host visual evidence: {reason}" ] }

    let private outputField name (output: string) =
        output.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryPick (fun line ->
            let prefix = name + "="

            if line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                Some(line.Substring(prefix.Length).Trim())
            else
                None)

    let private outputContains (value: string) (output: string) =
        output.Contains(value, StringComparison.OrdinalIgnoreCase)

    let validateVisualEvidenceCommandOutput (check: GeneratedVisualEvidenceCommandCheck) =
        let kind = outputField "evidence-kind" check.Output
        let imageDecodable = outputField "image-decodable" check.Output
        let provesScene = outputField "proves-scene-rendering" check.Output
        let provesDesktop = outputField "proves-desktop-visibility" check.Output
        let unsupportedReason = outputField "unsupported-reason" check.Output

        let diagnostics =
            [ match kind with
              | None -> "visual evidence command output must include evidence-kind"
              | Some "image" ->
                  if imageDecodable <> Some "true" then
                      "requested image evidence must be a decodable image, not metadata/hash text"
                  if outputContains "hash=" check.Output && imageDecodable <> Some "true" then
                      "metadata/hash output must be labeled metadata-hash instead of image"
                  if provesScene.IsNone then
                      "image evidence must state whether it proves scene rendering"
                  if provesDesktop.IsNone then
                      "image evidence must state whether it proves desktop visibility"
              | Some "pixel-readback" ->
                  if not (outputContains "fallback-reason=screenshot-unavailable" check.Output) then
                      "pixel-readback evidence must name the screenshot-unavailable fallback reason"
                  if provesScene <> Some "true" then
                      "pixel-readback evidence must prove scene rendering"
                  if provesDesktop <> Some "false" then
                      "pixel-readback evidence must not claim desktop visibility"
              | Some "metadata-hash" ->
                  if provesDesktop <> Some "false" then
                      "metadata/hash evidence must not claim desktop visibility"
              | Some "unsupported-host" ->
                  if unsupportedReason.IsNone then
                      "unsupported-host evidence must include unsupported-reason"
              | Some other -> $"unsupported visual evidence kind: {other}"

              if check.RequestedImageEvidence && kind = Some "metadata-hash" then
                  "requested image evidence cannot be satisfied by metadata/hash output" ]

        let failureReason =
            if diagnostics.IsEmpty then
                None
            elif kind = Some "image" && imageDecodable <> Some "true" then
                Some "metadata-only-image-evidence"
            elif check.RequestedImageEvidence && kind = Some "metadata-hash" then
                Some "metadata-only-image-evidence"
            elif kind = Some "unsupported-host" then
                Some "unsupported-host"
            else
                Some "visual-evidence-incomplete"

        { Accepted = diagnostics.IsEmpty
          EvidenceKind = kind
          FailureReason = failureReason
          Diagnostics = diagnostics }

    let buildValidationContractOutput check =
        let diagnostics =
            [ if not check.PackageResolution.ExactMatch then
                  yield! check.PackageResolution.Diagnostics
              if not check.GeneratedTests.Authoritative then
                  yield! check.GeneratedTests.Diagnostics
              if not check.DefaultInteractiveLaunch.InteractiveLaunchRequired then
                  yield! check.DefaultInteractiveLaunch.Diagnostics
              if not check.BoundedEvidenceValidated then
                  "bounded evidence validation did not run"
              if not check.CloseReasonValidated then
                  "close reason validation did not run"
              if not check.WindowDiagnostics.DiagnosticsComplete then
                  yield! check.WindowDiagnostics.Diagnostics
              if not check.WindowOptionsValidated then
                  "window options validation did not run"
              if not check.ImageEvidence.Accepted then
                  yield! check.ImageEvidence.Diagnostics ]

        let failureClass =
            if not check.PackageResolution.ExactMatch then
                check.PackageResolution.FailureReason |> Option.defaultValue "package-verification"
            elif not check.GeneratedTests.Authoritative then
                check.GeneratedTests.NonAuthoritativeReason |> Option.defaultValue "generated-test-execution"
            elif not check.DefaultInteractiveLaunch.InteractiveLaunchRequired then
                "interactive-launch-validation"
            elif not check.BoundedEvidenceValidated then
                "bounded-evidence-validation"
            elif not check.CloseReasonValidated then
                "close-reason-validation"
            elif not check.WindowDiagnostics.DiagnosticsComplete then
                "window-diagnostics-validation"
            elif not check.WindowOptionsValidated then
                "window-options-validation"
            elif not check.ImageEvidence.Accepted then
                check.ImageEvidence.FailureReason |> Option.defaultValue "visual-evidence-validation"
            else
                "none"

        let authoritative = List.isEmpty diagnostics

        let diagnosticText = String.concat "; " diagnostics

        let output =
            [ $"exact-package-match={check.PackageResolution.ExactMatch.ToString().ToLowerInvariant()}"
              "package-resolution=validated"
              $"generated-tests-ran={(check.GeneratedTests.Authoritative && check.GeneratedTests.NonAuthoritativeReason.IsNone).ToString().ToLowerInvariant()}"
              "generated-test-execution=validated"
              $"default-interactive-launch={check.DefaultInteractiveLaunch.InteractiveLaunchRequired.ToString().ToLowerInvariant()}"
              $"bounded-evidence-validation={check.BoundedEvidenceValidated.ToString().ToLowerInvariant()}"
              $"close-reason-validation={check.CloseReasonValidated.ToString().ToLowerInvariant()}"
              $"window-diagnostics-validation={check.WindowDiagnostics.DiagnosticsComplete.ToString().ToLowerInvariant()}"
              $"window-options-validation={check.WindowOptionsValidated.ToString().ToLowerInvariant()}"
              $"image-evidence-validation={check.ImageEvidence.Accepted.ToString().ToLowerInvariant()}"
              $"authoritative={authoritative.ToString().ToLowerInvariant()}"
              $"failure-class={failureClass}"
              if not diagnostics.IsEmpty then
                  $"diagnostics={diagnosticText}" ]
            |> String.concat Environment.NewLine

        { Output = output
          Authoritative = authoritative
          FailureClass = failureClass
          Diagnostics = diagnostics }

module GeneratedLayoutValidation =
    let validate (check: GeneratedLayoutValidationCheck) =
        let classified = LayoutEvidence.classify check.Report

        let diagnostics =
            [ if check.RequireReadableLayout && classified.ProofLevel <> ReadableLayout then
                  $"layout proof level is {classified.ProofLevel}, expected ReadableLayout"
              if classified.HudRegion.IsNone then
                  "missing HUD region"
              if classified.GameplayRegion.IsNone then
                  "missing gameplay region"
              if classified.TextBounds.IsEmpty then
                  "missing HUD text bounds"
              if classified.GameplayBounds.IsEmpty then
                  "missing gameplay bounds"
              if classified.ProofLevel = UnsupportedLayoutInspection && classified.UnsupportedReasons.IsEmpty then
                  "unsupported layout inspection requires an unsupported reason"
              match classified.OverlapStatus with
              | LayoutOverlaps overlaps -> yield! overlaps |> List.map _.Message
              | NoLayoutOverlap -> ()
              yield! classified.Diagnostics ]
            |> List.distinct

        let failureClass =
            if diagnostics.IsEmpty then
                None
            elif classified.ProofLevel = DeterministicRenderOnly && classified.RenderEvidence.IsSome then
                Some DeterministicRenderOnlyClaim
            elif classified.ProofLevel = UnsupportedLayoutInspection then
                Some UnsupportedLayoutFacts
            else
                match classified.OverlapStatus with
                | LayoutOverlaps _ -> Some OverlappingLayoutBounds
                | NoLayoutOverlap -> Some MissingLayoutFacts

        { Accepted = failureClass.IsNone
          FailureClass = failureClass
          Diagnostics = diagnostics }

module HostWarningClassification =
    let classify (check: HostWarningClassificationCheck) =
        let warningLines =
            check.RawMessage.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map _.Trim()
            |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace line))
            |> Array.toList

        let lineHasKnownMarker (line: string) =
            check.KnownBenignMarkers
            |> List.exists (fun (marker: string) -> line.Contains(marker, StringComparison.OrdinalIgnoreCase))

        let known =
            not warningLines.IsEmpty
            && warningLines |> List.forall lineHasKnownMarker

        let layoutAccepted =
            check.LayoutReadable = Some true || check.ExplicitlyUnsupportedWithoutReadabilityClaim

        let warningClass =
            if not check.PackageSucceeded then PackageFailure
            elif not check.LaunchSucceeded then LaunchFailure
            elif not check.RenderingSucceeded then RenderingFailure
            elif not layoutAccepted then LayoutFailure
            elif known then BenignEnvironmentWarning
            else UnknownWarning

        let fatal =
            match warningClass with
            | BenignEnvironmentWarning -> false
            | _ -> true

        let layoutReadable =
            check.LayoutReadable
            |> Option.map (fun value -> value.ToString().ToLowerInvariant())
            |> Option.defaultValue "none"

        let supportingFacts =
            [ $"launch-succeeded={check.LaunchSucceeded.ToString().ToLowerInvariant()}"
              $"rendering-succeeded={check.RenderingSucceeded.ToString().ToLowerInvariant()}"
              $"layout-readable={layoutReadable}"
              $"unsupported-without-readability-claim={check.ExplicitlyUnsupportedWithoutReadabilityClaim.ToString().ToLowerInvariant()}"
              $"package-succeeded={check.PackageSucceeded.ToString().ToLowerInvariant()}" ]

        let diagnostics =
            [ $"warning-class={warningClass}"
              $"fatal={fatal}"
              yield! supportingFacts
              if String.IsNullOrWhiteSpace check.RawMessage then
                  "raw-message=missing"
              if not known && warningClass = UnknownWarning then
                  "unknown warning marker"
              if warningLines |> List.exists (lineHasKnownMarker >> not) then
                  "unrelated warning or error text present"
              if not check.LaunchSucceeded then
                  "launch evidence failed"
              if not check.RenderingSucceeded then
                  "rendering evidence failed"
              if not layoutAccepted then
                  "layout readability failed or missing"
              if not check.PackageSucceeded then
                  "package evidence failed" ]

        { WarningClass = warningClass
          RawMessage = check.RawMessage
          Fatal = fatal
          EvidencePath = check.EvidencePath
          SupportingFacts = supportingFacts
          Diagnostics = diagnostics }

module PersistentLaunchArtifactValidation =
    let private requiredFields =
        [ "status"
          "mode"
          "command"
          "window-opened"
          "input-dispatch"
          "exit-path"
          "blocked-stage"
          "classification"
          "category"
          "message" ]

    let private parseFields (lines: string list) =
        lines
        |> List.collect (fun (line: string) ->
            line.Split([| ' '; '\t'; '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.choose (fun (token: string) ->
                let equals = token.IndexOf('=')

                if equals <= 0 then
                    None
                else
                    Some(token.Substring(0, equals).Trim().ToLowerInvariant(), token.Substring(equals + 1).Trim()))
            |> Array.toList)
        |> Map.ofList

    let private validValues =
        Map.ofList
            [ "status", Set.ofList [ "ok"; "failed"; "unsupported" ]
              "mode", Set.ofList [ "interactive-window"; "persistent-evidence" ]
              "input-dispatch", Set.ofList [ "verified"; "not-verified"; "not-required"; "failed"; "true"; "false" ]
              "exit-path", Set.ofList [ "true"; "false" ]
              "window-opened", Set.ofList [ "true"; "false" ]
              "first-frame-presented", Set.ofList [ "true"; "false" ]
              "blocked-stage",
              Set.ofList
                  [ "none"
                    "desktopprerequisite"
                    "processlaunch"
                    "windowcreation"
                    "firstframerender"
                    "observation"
                    "capture"
                    "inputverification"
                    "controlledexit"
                    "artifactwrite"
                    "window"
                    "surface"
                    "renderer"
                    "swapchain"
                    "scene"
                    "readback"
                    "app"
                    "timeout"
                    "unknown" ]
              "classification",
              Set.ofList
                  [ "none"
                    "ok"
                    "unsupportedenvironment"
                    "packageresolution"
                    "verificationdepth"
                    "applifecycle"
                    "productdefect" ]
              "category",
              Set.ofList
                  [ "none"
                    "startup"
                    "environmentsession"
                    "input"
                    "frame"
                    "renderer"
                    "vulkan"
                    "skia"
                    "swapchain"
                    "scene"
                    "screenshot" ] ]

    let validate (check: PersistentLaunchArtifactCheck) =
        let fields = parseFields check.Lines

        let missing =
            requiredFields
            |> List.filter (fun field -> not (fields.ContainsKey field))

        let field name = fields |> Map.tryFind name

        let invalidFields =
            validValues
            |> Map.toList
            |> List.choose (fun (name, allowed) ->
                field name
                |> Option.bind (fun value ->
                    let normalized = value.Trim().ToLowerInvariant()

                    if allowed.Contains normalized then
                        None
                    else
                        Some $"{name}={value}"))

        let passClaim =
            check.SupportedHostPassClaimed
            || field "status" = Some "ok"
            || field "classification" = Some "ok"

        let contradictions =
            [ if check.SyntheticFixture && passClaim then
                  "synthetic fixture cannot satisfy supported-host persistent launch"
              if passClaim && field "window-opened" <> Some "true" then
                  "status=ok requires window-opened=true"
              if passClaim && field "first-frame-presented" <> Some "true" then
                  "status=ok requires first-frame-presented=true"
              if passClaim && field "exit-path" <> Some "true" then
                  "status=ok requires exit-path=true"
              if passClaim && field "blocked-stage" <> Some "none" then
                  "status=ok requires blocked-stage=none" ]

        let diagnostics =
            [ $"artifact-path={check.ArtifactPath}"
              if check.SyntheticFixture then
                  "synthetic-fixture=true"
              for item in missing do
                  $"missing-field={item}"
              for item in invalidFields do
                  $"invalid-field={item}"
              yield! contradictions ]

        { Accepted = missing.IsEmpty && invalidFields.IsEmpty && contradictions.IsEmpty
          MissingFields = missing
          Contradictions = invalidFields @ contradictions
          Diagnostics = diagnostics }

module ReadinessFileDiscovery =
    let validate (check: ReadinessFileDiscoveryCheck) =
        let existing =
            check.ExistingFiles
            |> List.map (fun path -> path.Trim().Replace('\\', '/'))
            |> Set.ofList

        let missing =
            check.RequiredFiles
            |> List.filter (fun file ->
                let normalized = file.Trim().Replace('\\', '/')
                not (existing.Contains normalized))

        let diagnostics =
            [ $"readiness-directory={check.ReadinessDirectory}"
              for item in missing do
                  $"missing-readiness-file={item}" ]

        { Complete = missing.IsEmpty
          MissingFiles = missing
          Diagnostics = diagnostics }

module RuntimeDiagnosticReadiness =
    let validate (check: RuntimeDiagnosticReadinessCheck) : RuntimeDiagnosticReadinessResult =
        let status = FS.GG.UI.Diagnostics.RuntimeDiagnostics.readinessStatusToken check.Summary.Status

        let accepted =
            match check.RequiredStatus with
            | Some required -> check.Summary.Status = required
            | None when check.RequireAccepted -> check.Summary.Status = FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus.Accepted
            | None ->
                check.Summary.Status <> FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus.Blocked
                && check.Summary.Status <> FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus.ReviewRequired

        let diagnostics =
            [ if check.RequireAccepted && check.Summary.Status <> FS.GG.UI.Diagnostics.ReadinessDiagnosticStatus.Accepted then
                  $"runtime diagnostics status `{status}` is not accepted"
              match check.RequiredStatus with
              | Some required when check.Summary.Status <> required ->
                  let requiredToken = FS.GG.UI.Diagnostics.RuntimeDiagnostics.readinessStatusToken required
                  $"runtime diagnostics status `{status}` did not match required `{requiredToken}`"
              | _ -> ()
              if check.Summary.UnclassifiedCount > 0 then
                  $"runtime diagnostics include {check.Summary.UnclassifiedCount} unclassified occurrence(s)"
              if check.Summary.BlockerCount > 0 then
                  $"runtime diagnostics include {check.Summary.BlockerCount} blocker occurrence(s)"
              if check.Summary.ReviewRequiredCount > 0 then
                  $"runtime diagnostics include {check.Summary.ReviewRequiredCount} review-required occurrence(s)" ]

        { Accepted = accepted
          Status = status
          Diagnostics = diagnostics }

module DefaultTextGlyphEvidence =
    let pixelDistance (a: SKColor) (b: SKColor) =
        abs (int a.Red - int b.Red)
        + abs (int a.Green - int b.Green)
        + abs (int a.Blue - int b.Blue)
        + abs (int a.Alpha - int b.Alpha)

    let regionBounds (bitmap: SKBitmap) (region: Rect option) =
        match region with
        | Some bounds ->
            let x = Math.Clamp(int (Math.Floor bounds.X), 0, bitmap.Width - 1)
            let y = Math.Clamp(int (Math.Floor bounds.Y), 0, bitmap.Height - 1)
            let maxX = Math.Clamp(int (Math.Ceiling(bounds.X + bounds.Width)), x + 1, bitmap.Width)
            let maxY = Math.Clamp(int (Math.Ceiling(bounds.Y + bounds.Height)), y + 1, bitmap.Height)
            x, y, maxX, maxY
        | None -> 0, 0, bitmap.Width, bitmap.Height

    let validate (check: DefaultTextGlyphEvidenceCheck) =
        let status = check.Status.Trim().ToLowerInvariant()
        let normalizedReadiness = IO.Path.GetFullPath check.ReadinessDirectory
        let screenshotFullPath = IO.Path.GetFullPath check.ScreenshotPath
        let insideReadiness =
            screenshotFullPath.StartsWith(normalizedReadiness.TrimEnd(IO.Path.DirectorySeparatorChar) + string IO.Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || String.Equals(screenshotFullPath, normalizedReadiness, StringComparison.Ordinal)

        let mutable glyphCoverageMetric = 0.0
        let mutable solidBlockMetric = 1.0
        let mutable placeholderMetric = 1.0

        let artifactDiagnostics =
            try
                if not (IO.File.Exists screenshotFullPath) then
                    [ "screenshot artifact is missing" ]
                else
                    use bitmap = SKBitmap.Decode(screenshotFullPath)

                    if Object.ReferenceEquals(bitmap, null) then
                        [ "screenshot artifact is not decodable" ]
                    else
                        let expectedDiagnostics =
                            [ match check.ExpectedWidth with
                              | Some width when bitmap.Width <> width -> $"screenshot width {bitmap.Width} does not match expected {width}"
                              | _ -> ()
                              match check.ExpectedHeight with
                              | Some height when bitmap.Height <> height -> $"screenshot height {bitmap.Height} does not match expected {height}"
                              | _ -> () ]

                        let background = bitmap.GetPixel(0, 0)
                        let x0, y0, x1, y1 = regionBounds bitmap check.TextRegion
                        let mutable foreground = 0
                        let mutable transitions = 0
                        let mutable edgeForeground = 0
                        let mutable interiorForeground = 0
                        let mutable minForegroundX = Int32.MaxValue
                        let mutable minForegroundY = Int32.MaxValue
                        let mutable maxForegroundX = Int32.MinValue
                        let mutable maxForegroundY = Int32.MinValue
                        let mutable previousInRow = false
                        let mutable hasPrevious = false

                        for y in y0 .. y1 - 1 do
                            previousInRow <- false
                            hasPrevious <- false

                            for x in x0 .. x1 - 1 do
                                let isForeground = pixelDistance (bitmap.GetPixel(x, y)) background > 48

                                if isForeground then
                                    foreground <- foreground + 1
                                    minForegroundX <- min minForegroundX x
                                    minForegroundY <- min minForegroundY y
                                    maxForegroundX <- max maxForegroundX x
                                    maxForegroundY <- max maxForegroundY y

                                    if x = x0 || x = x1 - 1 || y = y0 || y = y1 - 1 then
                                        edgeForeground <- edgeForeground + 1
                                    else
                                        interiorForeground <- interiorForeground + 1

                                if hasPrevious && previousInRow <> isForeground then
                                    transitions <- transitions + 1

                                previousInRow <- isForeground
                                hasPrevious <- true

                        let area = max 1 ((x1 - x0) * (y1 - y0))
                        let foregroundRatio = float foreground / float area
                        let transitionRatio = float transitions / float area
                        let edgeRatio = float edgeForeground / float (max 1 foreground)
                        let interiorRatio = float interiorForeground / float (max 1 foreground)
                        let boundingBoxPlaceholder =
                            if foreground = 0 then
                                1.0
                            else
                                let mutable boundingEdgeForeground = 0
                                let mutable boundingInteriorForeground = 0

                                for y in minForegroundY .. maxForegroundY do
                                    for x in minForegroundX .. maxForegroundX do
                                        let isForeground = pixelDistance (bitmap.GetPixel(x, y)) background > 48

                                        if isForeground then
                                            if x = minForegroundX || x = maxForegroundX || y = minForegroundY || y = maxForegroundY then
                                                boundingEdgeForeground <- boundingEdgeForeground + 1
                                            else
                                                boundingInteriorForeground <- boundingInteriorForeground + 1

                                let boundingEdgeRatio = float boundingEdgeForeground / float foreground
                                let boundingInteriorRatio = float boundingInteriorForeground / float foreground
                                boundingEdgeRatio * (1.0 - boundingInteriorRatio)

                        glyphCoverageMetric <- transitionRatio
                        solidBlockMetric <- foregroundRatio
                        placeholderMetric <- max (if foreground = 0 then 1.0 else edgeRatio * (1.0 - interiorRatio)) boundingBoxPlaceholder

                        [ yield! expectedDiagnostics
                          if foreground = 0 then
                              "default text region has no foreground coverage"
                          if transitionRatio < 0.015 then
                              "default text region lacks glyph-shaped interior/background transitions"
                          if foregroundRatio > 0.25 && transitionRatio < 0.015 then
                              "default text region looks like a solid block"
                          if placeholderMetric > 0.55 || (foregroundRatio <= 0.25 && transitionRatio < 0.025 && foreground > 0) then
                              "default text region looks like placeholder/tofu box coverage" ]
            with ex ->
                [ $"screenshot glyph validation failed: {ex.Message}" ]

        let statusDiagnostics =
            [ if not insideReadiness then
                  "screenshot path must stay inside readiness directory"
              match status with
              | "ok" ->
                  if check.FontResolution |> Option.exists String.IsNullOrWhiteSpace then
                      "font-resolution must not be blank"
                  if check.FallbackUsed.IsNone then
                      "fallback-used must be recorded"
                  if check.UnsupportedHostReason.IsSome then
                      "successful glyph evidence must not carry unsupported-host-reason"
              | "unsupported" ->
                  if check.UnsupportedHostReason.IsNone then
                      "unsupported glyph evidence must include unsupported-host-reason"
              | "failed" -> ()
              | other -> $"unsupported default text glyph status: {other}" ]

        let diagnostics = statusDiagnostics @ artifactDiagnostics @ check.Diagnostics
        let accepted = status = "ok" && diagnostics.IsEmpty

        let failureClass =
            if accepted then
                None
            elif artifactDiagnostics |> List.exists (fun item -> item.Contains("missing") || item.Contains("decodable")) then
                Some "undecodable-screenshot"
            elif artifactDiagnostics |> List.exists (fun item -> item.Contains("solid block")) then
                Some "solid-block-default-text"
            elif artifactDiagnostics |> List.exists (fun item -> item.Contains("placeholder") || item.Contains("tofu")) then
                Some "placeholder-default-text"
            elif status = "unsupported" then
                Some "unsupported-host"
            else
                Some "glyph-coverage-incomplete"

        { Accepted = accepted
          GlyphCoverageMetric = glyphCoverageMetric
          SolidBlockMetric = solidBlockMetric
          PlaceholderMetric = placeholderMetric
          FailureClass = failureClass
          Diagnostics = diagnostics }

module EvidenceReports =
    let statusText status =
        match status with
        | EvidenceOk -> "ok"
        | EvidenceUnsupported -> "unsupported"
        | EvidenceFailed -> "failed"

    let field name value =
        { Name = name
          Value = value }

    let private statusExitCode status =
        match status with
        | EvidenceOk
        | EvidenceUnsupported -> 0
        | EvidenceFailed -> 1

    let private normalizeFields (fields: EvidenceReportField list) =
        fields
        |> List.filter (fun item -> not (String.IsNullOrWhiteSpace item.Name))
        |> List.map (fun item -> { item with Name = item.Name.Trim(); Value = item.Value.Trim() })

    let build (request: EvidenceReportRequest) =
        let standardFields =
            [ field "status" (statusText request.Status)
              field "command" request.Command ]
            @ (request.OutputPath |> Option.map (field "output") |> Option.toList)

        let merged =
            standardFields @ normalizeFields request.Fields
            |> List.distinctBy (fun item -> item.Name.ToLowerInvariant())

        let lines = merged |> List.map (fun item -> $"{item.Name}={item.Value}")

        { Status = request.Status
          Command = request.Command
          OutputPath = request.OutputPath
          Fields = merged
          Lines = lines
          ExitCode = statusExitCode request.Status }

    let write (request: EvidenceReportRequest) =
        let report = build request

        match report.OutputPath with
        | Some path ->
            match IO.Path.GetDirectoryName path with
            | null
            | "" -> ()
            | directory -> IO.Directory.CreateDirectory(directory) |> ignore

            IO.File.WriteAllLines(path, report.Lines)
        | None -> ()

        report.Lines |> List.iter Console.WriteLine
        report

    let validate (report: EvidenceReport) =
        let names =
            report.Fields
            |> List.map (fun field -> field.Name.ToLowerInvariant())
            |> Set.ofList

        let required =
            [ "status"; "command" ]
            @ (if report.OutputPath.IsSome then [ "output" ] else [])
            @ (if report.Status = EvidenceUnsupported then [ "unsupported-host-reason"; "fallback" ] else [])

        let missing = required |> List.filter (fun name -> not (names.Contains name))

        let diagnostics =
            [ if report.Lines <> (report.Fields |> List.map (fun item -> $"{item.Name}={item.Value}")) then
                  "stdout/file lines must match report field ordering"
              if report.Status = EvidenceFailed && report.ExitCode = 0 then
                  "failed reports must use non-zero exit code"
              for item in missing do
                  $"missing-field={item}" ]

        { Accepted = missing.IsEmpty && diagnostics.IsEmpty
          MissingFields = missing
          Diagnostics = diagnostics }

    let parseScreenshotEvidenceRecord (lines: string list) =
        let fields =
            lines
            |> List.choose (fun line ->
                let index = line.IndexOf('=', StringComparison.Ordinal)
                if index <= 0 then
                    None
                else
                    Some(field (line.Substring(0, index)) (line.Substring(index + 1))))

        let value name =
            fields
            |> List.tryFind (fun item -> String.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            |> Option.map _.Value

        let diagnostics =
            [ for line in lines do
                  if line.IndexOf('=', StringComparison.Ordinal) <= 0 then
                      $"malformed-line={line}" ]

        { Fields = fields
          ArtifactPath = value "artifact-path" |> Option.orElse (value "screenshot-path")
          Diagnostics = diagnostics }

    let private readPngArtifact path =
        try
            if not (IO.File.Exists path) then
                None, "missing"
            else
                use bitmap = SKBitmap.Decode(path)

                if Object.ReferenceEquals(bitmap, null) then
                    None, "unreadable"
                else
                    let mutable nonBlank = false
                    let mutable y = 0

                    while y < bitmap.Height && not nonBlank do
                        let mutable x = 0

                        while x < bitmap.Width && not nonBlank do
                            if bitmap.GetPixel(x, y).Alpha > 0uy then
                                nonBlank <- true
                            x <- x + 1

                        y <- y + 1

                    Some(bitmap.Width, bitmap.Height), if nonBlank then "non-blank" else "blank"
        with _ ->
            None, "unreadable"

    let validateScreenshotArtifact (check: ScreenshotArtifactValidationCheck) =
        let normalizedReadiness = IO.Path.GetFullPath check.ReadinessDirectory
        let artifactFullPath = IO.Path.GetFullPath check.ArtifactPath
        let insideReadiness =
            artifactFullPath.StartsWith(normalizedReadiness.TrimEnd(IO.Path.DirectorySeparatorChar) + string IO.Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || String.Equals(artifactFullPath, normalizedReadiness, StringComparison.Ordinal)

        let dimensions, pixelValidation = readPngArtifact artifactFullPath
        let expectedMatches =
            match dimensions, check.ExpectedWidth, check.ExpectedHeight with
            | Some(width, height), Some expectedWidth, Some expectedHeight -> width = expectedWidth && height = expectedHeight
            | Some _, _, _ -> true
            | None, _, _ -> false

        let diagnostics =
            [ if not insideReadiness then
                  "artifact path must stay inside readiness directory"
              if dimensions.IsNone then
                  "artifact is missing or not a readable PNG"
              if not expectedMatches then
                  "artifact dimensions do not match expected dimensions"
              if check.RequireNonBlank && pixelValidation <> "non-blank" then
                  "artifact pixel content is blank" ]

        { Accepted = diagnostics.IsEmpty
          DecodedWidth = dimensions |> Option.map fst
          DecodedHeight = dimensions |> Option.map snd
          PixelContentValidation = pixelValidation
          FailureClass = if diagnostics.IsEmpty then None else Some "invalid-screenshot-artifact"
          Diagnostics = diagnostics }

    let validateScreenshotEvidence (check: ScreenshotEvidenceReportCheck) =
        let normalizedStatus = check.Status.Trim().ToLowerInvariant()
        let normalizedKind = check.EvidenceKind |> Option.map (fun value -> value.Trim().ToLowerInvariant())
        let normalizedSource = check.CaptureSource |> Option.map (fun value -> value.Trim().ToLowerInvariant())
        let normalizedPixelValidation = check.PixelContentValidation |> Option.map (fun value -> value.Trim().ToLowerInvariant())
        let hostilePath =
            (check.ArtifactPath |> Option.orElse check.ScreenshotPath)
            |> Option.exists (fun path ->
                let normalized = path.Replace('\\', '/')
                IO.Path.IsPathRooted path
                || normalized.StartsWith("../", StringComparison.Ordinal)
                || normalized.Contains("/../", StringComparison.Ordinal))

        let hiddenWarning =
            check.Diagnostics
            |> List.exists (fun diagnostic ->
                diagnostic.Contains("warning", StringComparison.OrdinalIgnoreCase)
                || diagnostic.Contains("Gtk-Message", StringComparison.OrdinalIgnoreCase))

        let positiveDimensions =
            match check.Width, check.Height with
            | Some width, Some height -> width > 0 && height > 0
            | _ -> false

        let missing =
            [ if check.Command.IsNone then
                  "command"
              if check.AppOrSample.IsNone then
                  "app-or-sample"
              if check.HostFacts.IsEmpty then
                  "host-facts"
              if check.CaptureMode.IsNone then
                  "capture-mode"
              if normalizedKind.IsNone then
                  "evidence-kind"
              if check.ArtifactPath.IsNone then
                  "artifact-path"
              if check.PixelContentValidation.IsNone then
                  "pixel-content-validation"
              if check.ProvesScreenshot.IsNone then
                  "proves-screenshot"
              if check.BlockedStage.IsNone then
                  "blocked-stage"
              if check.Classification.IsNone then
                  "classification"
              if check.Category.IsNone then
                  "category"
              if check.Message.IsNone then
                  "message"
              if check.Timestamp.IsNone then
                  "timestamp"
              if check.ViewerOpenStatus.IsNone then
                  "viewer-open-status"
              if check.FirstFrameStatus.IsNone then
                  "first-frame-status"
              if check.CaptureAvailability.IsNone then
                  "capture-availability"
              if normalizedSource.IsNone then
                  "capture-source"
              if normalizedStatus = "ok" then
                  if check.ArtifactPath.IsNone && check.ScreenshotPath.IsNone then
                      "artifact-path"
                  if check.Width.IsNone then
                      "width"
                  if check.Height.IsNone then
                      "height"
              if normalizedStatus = "unsupported" then
                  if check.UnsupportedHostReason.IsNone then
                      "unsupported-host-reason"
                  if check.Fallback.IsNone then
                      "fallback" ]

        let diagnostics =
            [ if normalizedKind <> Some "screenshot" then
                  "screenshot evidence report must use evidence-kind=screenshot"
              match normalizedStatus with
              | "ok" ->
                  if not positiveDimensions then
                      "successful screenshot evidence requires positive dimensions"
                  if normalizedSource <> Some "live-viewer-window" then
                      "successful screenshot evidence requires capture-source=live-viewer-window"
                  if check.ProvesScreenshot <> Some true then
                      "successful screenshot evidence requires proves-screenshot=true"
                  if normalizedPixelValidation <> Some "non-blank" && normalizedPixelValidation <> Some "pixel-content-non-blank" then
                      "successful screenshot evidence requires non-blank pixel validation"
                  if check.Fallback.IsSome then
                      "successful screenshot evidence must not require deterministic fallback"
                  if hostilePath then
                      "screenshot artifact path must stay within the requested readiness artifact tree"
                  if hiddenWarning then
                      "successful screenshot evidence must not hide warning diagnostics"
              | "unsupported" ->
                  if check.ScreenshotPath.IsSome || (check.ArtifactPath |> Option.exists (fun value -> value <> "none")) then
                      "unsupported screenshot evidence must not claim screenshot-path"
                  if normalizedSource = Some "live-viewer-window" then
                      "unsupported screenshot evidence must not claim live viewer capture"
                  if check.ProvesScreenshot = Some true then
                      "unsupported screenshot evidence must not claim screenshot proof"
              | "failed" -> ()
              | other -> $"unsupported screenshot status: {other}"
              yield! check.Diagnostics ]

        let failureClass =
            if not missing.IsEmpty then
                Some "missing-screenshot-evidence-fields"
            elif not diagnostics.IsEmpty then
                Some "invalid-screenshot-evidence-fields"
            else
                None

        { Accepted = failureClass.IsNone
          MissingFields = missing
          FailureClass = failureClass
          Diagnostics = diagnostics }

module PackageInspectionAssertions =
    let validate (check: PackageInspectionAssertionCheck) =
        let diagnostics =
            [ if check.Report.Status <> check.ExpectedStatus then
                  $"expected package inspection status {check.ExpectedStatus}, got {check.Report.Status}"

              for fragment in check.RequiredDiagnosticFragments do
                  let found =
                      check.Report.Diagnostics
                      |> List.exists (fun diagnostic -> diagnostic.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase))

                  if not found then
                      $"missing package diagnostic containing '{fragment}'" ]

        { Accepted = diagnostics.IsEmpty
          Diagnostics = diagnostics }

module LayoutReadiness =
    let statusText status =
        match status with
        | LayoutReadinessAccepted -> "accepted"
        | LayoutReadinessIncomplete -> "incomplete"
        | LayoutReadinessFailed -> "failed"
        | LayoutReadinessSkipped -> "skipped"
        | LayoutReadinessEnvironmentLimited -> "environment-limited"
        | LayoutReadinessSyntheticOnly -> "synthetic-only"
        | LayoutReadinessCompatibilityBlocked -> "compatibility-blocked"
        | LayoutReadinessMissingEvidence -> "missing-evidence"

    let private blocksAcceptance status =
        match status with
        | LayoutReadinessAccepted -> false
        | LayoutReadinessEnvironmentLimited -> false
        | _ -> true

    let validate (report: LayoutReadinessReport) : LayoutReadinessValidationResult =
        let requiredStatuses =
            [ "contract", report.ContractStatus
              "scroll-viewer", report.ScrollViewerStatus
              "intrinsic-cache", report.IntrinsicStatus
              "full-incremental-parity", report.ParityStatus
              "compatibility", report.CompatibilityStatus
              "diagnostics", report.DiagnosticsStatus ]

        let missingEvidence =
            report.Evidence
            |> List.choose (fun evidence ->
                if evidence.Required && (evidence.Path.IsNone || evidence.Status = LayoutReadinessMissingEvidence) then
                    Some evidence.Name
                else
                    None)

        let blockedStatus =
            requiredStatuses
            |> List.filter (snd >> blocksAcceptance)
            |> List.map (fun (name, status) -> $"{name}:{statusText status}")

        let blockingLimitations =
            report.Limitations
            |> List.filter (fun item -> item.Contains("blocking", StringComparison.OrdinalIgnoreCase))

        let unintentionalDeltas =
            report.CompatibilityDeltas
            |> List.filter (fun delta -> not delta.Intentional)
            |> List.map (fun delta -> $"{delta.Surface}:{delta.Change}")

        let diagnostics =
            [ if String.IsNullOrWhiteSpace report.Feature then
                  "layout readiness report must name the feature"
              for missing in missingEvidence do
                  $"missing required layout readiness evidence: {missing}"
              for status in blockedStatus do
                  $"blocking layout readiness status: {status}"
              for limitation in blockingLimitations do
                  $"blocking layout readiness limitation: {limitation}"
              for delta in unintentionalDeltas do
                  $"unintentional layout compatibility delta: {delta}"
              for evidence in report.Evidence do
                  yield! evidence.Diagnostics ]

        let status =
            if not missingEvidence.IsEmpty then
                LayoutReadinessMissingEvidence
            elif not unintentionalDeltas.IsEmpty then
                LayoutReadinessCompatibilityBlocked
            elif not blockedStatus.IsEmpty || not blockingLimitations.IsEmpty then
                LayoutReadinessIncomplete
            else
                LayoutReadinessAccepted

        { Accepted = status = LayoutReadinessAccepted
          Status = status
          MissingEvidence = missingEvidence
          BlockingLimitations = blockingLimitations
          Diagnostics = diagnostics }

module CompositorReadiness =
    let statusText status =
        match status with
        | CompositorReadinessAccepted -> "accepted"
        | CompositorReadinessFallbackGated -> "fallback-gated"
        | CompositorReadinessFailed -> "failed"
        | CompositorReadinessEnvironmentLimited -> "environment-limited"
        | CompositorReadinessMissingEvidence -> "missing-evidence"
        | CompositorReadinessCompatibilityBlocked -> "compatibility-blocked"

    let private blocksCorrectness status =
        match status with
        | CompositorReadinessAccepted -> false
        | _ -> true

    let validate (report: CompositorReadinessReport) : CompositorReadinessValidationResult =
        let requiredStatuses =
            [ "proof", report.ProofStatus
              "parity", report.ParityStatus
              "compatibility", report.CompatibilityStatus
              "regression", report.RegressionStatus ]

        let missingEvidence =
            report.Evidence
            |> List.choose (fun evidence ->
                if evidence.EvidenceRequired && (evidence.EvidencePath.IsNone || evidence.EvidenceStatus = CompositorReadinessMissingEvidence) then
                    Some evidence.EvidenceName
                else
                    None)

        let blockedStatus =
            requiredStatuses
            |> List.filter (snd >> blocksCorrectness)
            |> List.map (fun (name, status) -> $"{name}:{statusText status}")

        let blockingLimitations =
            report.Limitations
            |> List.filter (fun item -> item.Contains("blocking", StringComparison.OrdinalIgnoreCase))

        let diagnostics =
            [ if String.IsNullOrWhiteSpace report.Feature then
                  "compositor readiness report must name the feature"
              for missing in missingEvidence do
                  $"missing required compositor readiness evidence: {missing}"
              for status in blockedStatus do
                  $"blocking compositor readiness status: {status}"
              if report.TimingStatus <> CompositorReadinessAccepted then
                  $"timing claim status: {statusText report.TimingStatus}"
              for limitation in blockingLimitations do
                  $"blocking compositor readiness limitation: {limitation}"
              for evidence in report.Evidence do
                  yield! evidence.EvidenceDiagnostics ]

        let status =
            if not missingEvidence.IsEmpty then
                CompositorReadinessMissingEvidence
            elif report.CompatibilityStatus = CompositorReadinessCompatibilityBlocked then
                CompositorReadinessCompatibilityBlocked
            elif report.ProofStatus = CompositorReadinessEnvironmentLimited then
                CompositorReadinessEnvironmentLimited
            elif requiredStatuses |> List.exists (snd >> (=) CompositorReadinessFailed) then
                CompositorReadinessFailed
            elif not blockedStatus.IsEmpty || not blockingLimitations.IsEmpty then
                CompositorReadinessFallbackGated
            else
                CompositorReadinessAccepted

        { Accepted = status = CompositorReadinessAccepted
          Status = status
          MissingEvidence = missingEvidence
          BlockingLimitations = blockingLimitations
          Diagnostics = diagnostics }

module CompositorTimingAssertions =
    let verdictText verdict =
        match verdict with
        | CompositorTimingPositive -> "positive"
        | CompositorTimingNoisy -> "noisy"
        | CompositorTimingNonBeneficial -> "non-beneficial"
        | CompositorTimingIncomplete -> "incomplete"
        | CompositorTimingRejected -> "rejected"
        | CompositorTimingEnvironmentLimited -> "environment-limited"
        | CompositorTimingLimited -> "limited"

    let private verdictBlocksPositive verdict =
        match verdict with
        | CompositorTimingPositive -> false
        | _ -> true

    let validateSummary (check: CompositorTimingSummaryCheck) : CompositorTimingSummaryValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let rejectedScenarios =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if verdictBlocksPositive scenario.Verdict then
                    Some $"{scenario.ScenarioId}:{verdictText scenario.Verdict}"
                else
                    None)

        let incompleteSamples =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if scenario.FullRedrawSampleCount < check.MeasuredRepetitions
                   || scenario.DamageScopedSampleCount < check.MeasuredRepetitions then
                    Some scenario.ScenarioId
                else
                    None)

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "timing summary must name the feature"
              if check.ExpectedProfileId <> check.ActualProfileId then
                  $"profile mismatch: expected={check.ExpectedProfileId} actual={check.ActualProfileId}"
              if check.PolicyId <> "same-profile-live-threshold-v2" then
                  $"unexpected timing policy: {check.PolicyId}"
              if check.WarmupCount < 0 then
                  "warmup count must not be negative"
              if check.MeasuredRepetitions < 5 then
                  "at least five measured repetitions are required"
              for scenario in missingScenarios do
                  $"missing required timing scenario: {scenario}"
              for scenario in rejectedScenarios do
                  $"non-positive timing scenario: {scenario}"
              for scenario in incompleteSamples do
                  $"incomplete timing samples: {scenario}"
              for scenario in missingArtifacts do
                  $"missing timing artifacts: {scenario}"
              if check.ShippedPerformanceClaim <> "performance-not-accepted" then
                  "Feature 156 cannot accept the shipped P7 performance claim by itself"
              for scenario in requiredScenarioResults do
                  yield! scenario.RejectionReasons |> List.map (fun reason -> $"{scenario.ScenarioId}: {reason}") ]

        let verdict =
            if not missingScenarios.IsEmpty || not incompleteSamples.IsEmpty || not missingArtifacts.IsEmpty then
                CompositorTimingIncomplete
            elif diagnostics |> List.exists (fun item -> item.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase)) then
                CompositorTimingRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingEnvironmentLimited) then
                CompositorTimingEnvironmentLimited
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingLimited) then
                CompositorTimingLimited
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingRejected) then
                CompositorTimingRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingNoisy) then
                CompositorTimingNoisy
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Verdict = CompositorTimingNonBeneficial) then
                CompositorTimingNonBeneficial
            elif requiredScenarioResults.Length = check.RequiredScenarioIds.Length then
                CompositorTimingPositive
            else
                CompositorTimingIncomplete

        { Accepted = verdict = CompositorTimingPositive && diagnostics.IsEmpty
          Verdict = verdict
          MissingScenarios = missingScenarios
          RejectedScenarios = rejectedScenarios
          Diagnostics = diagnostics }

module CompositorDamageReadiness =
    let statusText status =
        match status with
        | CompositorDamageAccepted -> "accepted"
        | CompositorDamageFallbackOnly -> "fallback-only"
        | CompositorDamageRejected -> "rejected"
        | CompositorDamageEnvironmentLimited -> "environment-limited"

    let private statusBlocksAcceptance status =
        match status with
        | CompositorDamageAccepted -> false
        | _ -> true

    let validate (check: CompositorDamageReadinessCheck) : CompositorDamageReadinessValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let scenarioFailures =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if statusBlocksAcceptance scenario.Status then
                    Some $"{scenario.ScenarioId}:{statusText scenario.Status}"
                else
                    None)

        let fallbackOnly =
            requiredScenarioResults.Length = check.RequiredScenarioIds.Length
            && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = CompositorDamageFallbackOnly)

        let environmentLimited =
            requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = CompositorDamageEnvironmentLimited)

        let unsupportedArtifactViolation =
            check.AcceptedPartialRedrawArtifacts <> 0
            && check.UnsupportedHostStatus = CompositorDamageEnvironmentLimited

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "damage readiness check must name the feature"
              for scenario in missingScenarios do
                  $"missing required damage scenario: {scenario}"
              for scenario in missingArtifacts do
                  $"missing damage artifact path: {scenario}"
              for failure in scenarioFailures do
                  $"non-accepted damage scenario: {failure}"
              if check.AcceptedAttemptCount < 3 && not fallbackOnly && not environmentLimited then
                  "accepted damage readiness requires at least three accepted attempts"
              if unsupportedArtifactViolation then
                  "unsupported-host evidence must contain zero accepted partial-redraw artifacts"
              if not check.CompatibilityAccepted then
                  "compatibility ledger is not accepted"
              if not check.PackageAccepted then
                  "package validation is not accepted"
              if not check.RegressionAccepted then
                  "regression validation is not accepted"
              if check.PerformanceClaim <> "performance-not-accepted" then
                  "Feature 157 cannot accept the shipped P7 performance claim by itself"
              for limitation in check.Limitations do
                  if limitation.Contains("blocking", StringComparison.OrdinalIgnoreCase) then
                      $"blocking damage readiness limitation: {limitation}" ]

        let status =
            if environmentLimited || unsupportedArtifactViolation then
                CompositorDamageEnvironmentLimited
            elif not missingScenarios.IsEmpty || not missingArtifacts.IsEmpty then
                CompositorDamageRejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = CompositorDamageRejected) then
                CompositorDamageRejected
            elif fallbackOnly then
                CompositorDamageFallbackOnly
            elif diagnostics.IsEmpty
                 && check.AcceptedAttemptCount >= 3
                 && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = CompositorDamageAccepted) then
                CompositorDamageAccepted
            else
                CompositorDamageFallbackOnly

        { Accepted = status = CompositorDamageAccepted && diagnostics.IsEmpty
          Status = status
          MissingScenarios = missingScenarios
          Diagnostics = diagnostics }

module Feature159Readiness =
    let statusText status =
        match status with
        | Feature159Accepted -> "accepted"
        | Feature159NonBeneficial -> "non-beneficial"
        | Feature159FallbackOnly -> "fallback-only"
        | Feature159Rejected -> "rejected"
        | Feature159EnvironmentLimited -> "environment-limited"

    let private statusBlocksAcceptance status =
        match status with
        | Feature159Accepted -> false
        | _ -> true

    let validate (check: Feature159ReadinessCheck) : Feature159ReadinessValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios |> List.exists (fun candidate -> candidate.ScenarioId = scenario) |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let scenarioFailures =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if statusBlocksAcceptance scenario.Status then
                    Some $"{scenario.ScenarioId}:{statusText scenario.Status}"
                else
                    None)

        let fallbackOnly =
            requiredScenarioResults.Length = check.RequiredScenarioIds.Length
            && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159FallbackOnly)

        let environmentLimited =
            requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159EnvironmentLimited)

        let nonBeneficial =
            requiredScenarioResults.Length = check.RequiredScenarioIds.Length
            && requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159NonBeneficial)
            && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159Accepted || scenario.Status = Feature159NonBeneficial)

        let unsupportedArtifactViolation =
            check.UnsupportedHostStatus = Feature159EnvironmentLimited
            && (check.AcceptedReuseArtifacts <> 0 || check.AcceptedPromotionArtifacts <> 0)

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "Feature 159 readiness check must name the feature"
              for scenario in missingScenarios do
                  $"missing required promotion scenario: {scenario}"
              for scenario in missingArtifacts do
                  $"missing promotion artifact path: {scenario}"
              for failure in scenarioFailures do
                  $"non-accepted promotion scenario: {failure}"
              if check.AcceptedAttemptCount < 3 && not fallbackOnly && not environmentLimited then
                  "accepted Feature 159 readiness requires at least three accepted attempts"
              if requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159Accepted && not scenario.ParityPassed) then
                  "accepted Feature 159 scenarios require passing parity"
              if check.Scenarios |> List.exists (fun scenario -> scenario.Status = Feature159Accepted && scenario.CounterNetSavedWork <= 0) then
                  "accepted Feature 159 scenarios require positive net saved work"
              if unsupportedArtifactViolation then
                  "unsupported-host evidence must contain zero accepted Feature 159 reuse or promotion artifacts"
              if not check.CompatibilityAccepted then
                  "compatibility ledger is not accepted"
              if not check.PackageAccepted then
                  "package validation is not accepted"
              if not check.RegressionAccepted then
                  "regression validation is not accepted"
              if check.PerformanceClaim <> "performance-not-accepted" then
                  "Feature 159 cannot accept the shipped P7 performance claim by itself"
              for limitation in check.Limitations do
                  if limitation.Contains("blocking", StringComparison.OrdinalIgnoreCase) then
                      $"blocking Feature 159 readiness limitation: {limitation}" ]

        let status =
            if environmentLimited || unsupportedArtifactViolation then
                Feature159EnvironmentLimited
            elif not missingScenarios.IsEmpty || not missingArtifacts.IsEmpty then
                Feature159Rejected
            elif requiredScenarioResults |> List.exists (fun scenario -> scenario.Status = Feature159Rejected) then
                Feature159Rejected
            elif fallbackOnly then
                Feature159FallbackOnly
            elif nonBeneficial then
                Feature159NonBeneficial
            elif diagnostics.IsEmpty
                 && check.AcceptedAttemptCount >= 3
                 && requiredScenarioResults |> List.forall (fun scenario -> scenario.Status = Feature159Accepted) then
                Feature159Accepted
            else
                Feature159FallbackOnly

        { Accepted = status = Feature159Accepted && diagnostics.IsEmpty
          Status = status
          MissingScenarios = missingScenarios
          Diagnostics = diagnostics }

module Feature160ThroughputReadiness =
    let statusText status =
        match status with
        | Feature160Accepted -> "accepted"
        | Feature160Blocked -> "blocked"
        | Feature160Rejected -> "rejected"
        | Feature160FallbackOnly -> "fallback-only"
        | Feature160EnvironmentLimited -> "environment-limited"

    let private fullValidationPassed (status: string) =
        String.Equals(status, "passed", StringComparison.OrdinalIgnoreCase)
        || String.Equals(status, "current-passed", StringComparison.OrdinalIgnoreCase)

    let private samplePolicyAccepted (policy: string) =
        String.Equals(policy, "readback-free", StringComparison.OrdinalIgnoreCase)
        || String.Equals(policy, "readback-outside-measurement", StringComparison.OrdinalIgnoreCase)

    let validate (check: Feature160ThroughputReadinessCheck) : Feature160ThroughputReadinessValidationResult =
        let requiredIterations = max 1 check.RequiredIterationCount

        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario ->
                check.Scenarios
                |> List.exists (fun candidate -> candidate.ScenarioId = scenario && candidate.Covered)
                |> not)

        let requiredScenarioResults =
            check.RequiredScenarioIds
            |> List.choose (fun scenario ->
                check.Scenarios |> List.tryFind (fun candidate -> candidate.ScenarioId = scenario))

        let missingArtifacts =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if List.isEmpty scenario.ArtifactPaths then Some scenario.ScenarioId else None)

        let invalidWarmup =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if scenario.WarmupCount <> 3 then Some scenario.ScenarioId else None)

        let invalidRepetitions =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if scenario.MeasuredRepetitions <> 5 then Some scenario.ScenarioId else None)

        let invalidSamplePolicy =
            requiredScenarioResults
            |> List.choose (fun scenario ->
                if samplePolicyAccepted scenario.SamplePolicy then None else Some scenario.ScenarioId)

        let unsupportedArtifactViolation =
            check.UnsupportedHostStatus = Feature160EnvironmentLimited
            && check.AcceptedUnsupportedHostArtifacts <> 0

        let fullValidationBlocked = not (fullValidationPassed check.FullValidationStatus)

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "Feature 160 throughput readiness check must name the feature"
              for scenario in missingScenarios do
                  $"missing required throughput scenario: {scenario}"
              for scenario in missingArtifacts do
                  $"missing throughput artifact path: {scenario}"
              for scenario in invalidWarmup do
                  $"scenario warmup must be 3: {scenario}"
              for scenario in invalidRepetitions do
                  $"scenario measured repetitions must be 5: {scenario}"
              for scenario in invalidSamplePolicy do
                  $"scenario sample policy is not accepted: {scenario}"
              if check.AcceptedIterationCount < requiredIterations
                 && check.UnsupportedHostStatus <> Feature160EnvironmentLimited then
                  $"accepted Feature 160 throughput requires at least {requiredIterations} accepted iterations"
              if unsupportedArtifactViolation then
                  "unsupported-host evidence must contain zero accepted Feature 160 performance artifacts"
              if fullValidationBlocked && check.AcceptedIterationCount >= requiredIterations && missingScenarios.IsEmpty then
                  $"full validation blocks release-ready status: {check.FullValidationStatus}"
              if not check.CompatibilityAccepted then
                  "compatibility ledger is not accepted"
              if not check.PackageAccepted then
                  "package validation is not accepted"
              if not check.RegressionAccepted then
                  "regression validation is not accepted"
              if check.PerformanceClaim <> "performance-not-accepted" then
                  "Feature 160 cannot accept the shipped P7 performance claim by itself"
              for limitation in check.Limitations do
                  if limitation.Contains("overclaim", StringComparison.OrdinalIgnoreCase) then
                      $"overclaimed Feature 160 throughput limitation: {limitation}" ]

        let status =
            if check.UnsupportedHostStatus = Feature160EnvironmentLimited && check.AcceptedIterationCount = 0 then
                Feature160EnvironmentLimited
            elif unsupportedArtifactViolation
                 || not missingScenarios.IsEmpty
                 || not missingArtifacts.IsEmpty
                 || not invalidWarmup.IsEmpty
                 || not invalidRepetitions.IsEmpty
                 || not invalidSamplePolicy.IsEmpty
                 || check.PerformanceClaim <> "performance-not-accepted" then
                Feature160Rejected
            elif check.AcceptedIterationCount >= requiredIterations
                 && fullValidationBlocked then
                Feature160Blocked
            elif diagnostics.IsEmpty
                 && check.AcceptedIterationCount >= requiredIterations
                 && fullValidationPassed check.FullValidationStatus then
                Feature160Accepted
            elif check.AcceptedIterationCount = 0 then
                Feature160FallbackOnly
            else
                Feature160Rejected

        { Accepted = status = Feature160Accepted && diagnostics.IsEmpty
          Status = status
          MissingScenarios = missingScenarios
          Diagnostics = diagnostics }

module Feature161HostLaneReadiness =
    let statusText status =
        match status with
        | Feature161Accepted -> "accepted"
        | Feature161Blocked -> "blocked"
        | Feature161Rejected -> "rejected"
        | Feature161FallbackOnly -> "fallback-only"
        | Feature161EnvironmentLimited -> "environment-limited"
        | Feature161MissingEvidence -> "missing-evidence"

    let fullValidationPassed (status: string) =
        String.Equals(status, "passed", StringComparison.OrdinalIgnoreCase)
        || String.Equals(status, "current-passed", StringComparison.OrdinalIgnoreCase)

    let blank (value: string) = String.IsNullOrWhiteSpace value

    let requiredFacts (facts: Feature161HostFactEvidence option) =
        match facts with
        | None ->
            [ "host-facts" ]
        | Some facts ->
            [ if blank facts.LaneId then "lane-id"
              if blank facts.DisplayServer then "display-server"
              if blank facts.DisplayIdentity then "display-identity"
              if blank facts.RendererIdentity then "renderer-identity"
              if facts.DirectRendering.IsNone then "direct-rendering"
              if blank facts.RefreshStatus then "refresh-status"
              if blank facts.DriverIdentity then "driver-identity"
              if blank facts.PackageVersionSet then "package-version-set"
              if blank facts.CpuLoadNote then "cpu-load-note"
              if blank facts.GpuLoadNote then "gpu-load-note"
              if blank facts.HostProfile then "host-profile"
              if blank facts.RunIdentity then "run-identity"
              if blank facts.ScenarioIdentity then "scenario-identity"
              if blank facts.TimingPolicyIdentity then "timing-policy-identity"
              if List.isEmpty facts.ArtifactPaths then "artifact-paths" ]

    let unsupportedFacts (facts: Feature161HostFactEvidence option) =
        match facts with
        | None -> []
        | Some facts ->
            [ if facts.DisplayServer = "missing-display" || blank facts.DisplayIdentity then "missing-display"
              if facts.DirectRendering = Some false then "indirect-rendering"
              if facts.RendererIdentity.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase)
                 || facts.RendererIdentity.Contains("software", StringComparison.OrdinalIgnoreCase) then
                  "software-raster"
              if facts.RendererIdentity = "unknown" then "unknown-renderer"
              if facts.EnvironmentLimits |> List.exists (fun item -> item.Contains("virtual", StringComparison.OrdinalIgnoreCase)) then
                  "virtualized-presentation"
              if facts.PackageVersionSet.Contains("stale", StringComparison.OrdinalIgnoreCase) then
                  "stale-package" ]

    let validate (check: Feature161HostLaneReadinessCheck) : Feature161HostLaneReadinessValidationResult =
        let missingScenarios =
            check.RequiredScenarioIds
            |> List.filter (fun scenario -> check.CoveredScenarioIds |> List.contains scenario |> not)

        let missingFacts = requiredFacts check.HostFacts
        let unsupported = unsupportedFacts check.HostFacts
        let priorGateBlocked =
            check.PriorGateStatuses
            |> List.exists (fun status ->
                not (String.Equals(status, "confirmed", StringComparison.OrdinalIgnoreCase))
                && not (String.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase)))

        let fullValidationBlocked = not (fullValidationPassed check.FullValidationStatus)
        let unsupportedArtifactViolation =
            check.UnsupportedHostStatus = Feature161EnvironmentLimited
            && check.AcceptedLaneScopedPerformanceArtifacts <> 0

        let performanceClaim = check.ClaimScope.PerformanceClaim

        let diagnostics =
            [ if String.IsNullOrWhiteSpace check.Feature then
                  "Feature 161 host lane readiness check must name the feature"
              for scenario in missingScenarios do
                  $"missing required host-lane scenario: {scenario}"
              for fact in missingFacts do
                  $"missing host lane fact: {fact}"
              for item in unsupported do
                  $"unsupported host lane fact: {item}"
              if check.AcceptedLaneScopedPerformanceArtifacts < 1
                 && check.UnsupportedHostStatus <> Feature161EnvironmentLimited then
                  "accepted Feature 161 host-lane readiness requires at least one accepted lane-scoped performance artifact"
              if unsupportedArtifactViolation then
                  "unsupported-host evidence must contain zero accepted Feature 161 performance artifacts"
              if check.ClaimScope.AcceptedLaneId.IsNone
                 && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                  "accepted artifacts must name an accepted lane id"
              if List.isEmpty check.ClaimScope.NonGeneralizedLanes then
                  "claim scope must list non-generalized lanes"
              if priorGateBlocked then
                  "prior P7 gate status blocks host-lane readiness"
              if fullValidationBlocked && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                  $"full validation blocks release-ready status: {check.FullValidationStatus}"
              if not check.CompatibilityAccepted then
                  "compatibility ledger is not accepted"
              if not check.PackageAccepted then
                  "package validation is not accepted"
              if not check.RegressionAccepted then
                  "regression validation is not accepted"
              if performanceClaim <> "performance-not-accepted" then
                  "Feature 161 cannot broaden or accept the shipped P7 performance claim by itself"
              for limitation in check.Limitations do
                  if limitation.Contains("overclaim", StringComparison.OrdinalIgnoreCase) then
                      $"overclaimed Feature 161 host-lane limitation: {limitation}" ]

        let status =
            if check.UnsupportedHostStatus = Feature161EnvironmentLimited
               && check.AcceptedLaneScopedPerformanceArtifacts = 0 then
                Feature161EnvironmentLimited
            elif not missingFacts.IsEmpty then
                Feature161MissingEvidence
            elif unsupportedArtifactViolation
                 || not missingScenarios.IsEmpty
                 || not unsupported.IsEmpty
                 || check.ClaimScope.AcceptedLaneId.IsNone
                 || performanceClaim <> "performance-not-accepted" then
                Feature161Rejected
            elif priorGateBlocked || fullValidationBlocked then
                Feature161Blocked
            elif diagnostics.IsEmpty && check.AcceptedLaneScopedPerformanceArtifacts > 0 then
                Feature161Accepted
            elif check.AcceptedLaneScopedPerformanceArtifacts = 0 then
                Feature161FallbackOnly
            else
                Feature161Rejected

        { Accepted = status = Feature161Accepted && diagnostics.IsEmpty
          Status = status
          MissingFacts = missingFacts
          MissingScenarios = missingScenarios
          Diagnostics = diagnostics }
