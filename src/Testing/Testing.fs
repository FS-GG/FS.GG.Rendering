namespace FS.GG.UI.Testing

open System
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
