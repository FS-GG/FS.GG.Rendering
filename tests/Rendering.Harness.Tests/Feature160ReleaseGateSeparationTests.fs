module Feature160ReleaseGateSeparationTests

open System
open Expecto
open Rendering.Harness

let private profile : Compositor.Types.HostProfile =
    { ProfileId = Compositor.Config.feature160AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "test-renderer"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private sample scenario : Compositor.Types.Feature158TimingSample =
    { SampleId = $"feature160-release-{scenario}"
      SampleIndex = 1
      ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + scenario.Replace("/", "-")
      Path = Perf.FullRedraw
      RunId = "feature160-release"
      HostProfileId = Compositor.Config.feature160AcceptedProfileId
      PackageVersion = Compositor.Config.feature156PackageVersion
      DurationMs = 1.0
      MeasurementPolicy = Perf.ReadbackFree
      InclusionStatus = Perf.Included
      ExclusionReason = None
      ArtifactPath = $"throughput/raw/{Compositor.FeatureState.feature160ScenarioFileName scenario}.csv" }

let private scenarioReport scenario : Compositor.Types.Feature158ScenarioReport =
    { ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + scenario.Replace("/", "-")
      FullRedraw = None
      DamageScoped = None
      WarmupCount = 3
      MeasuredRepetitions = 5
      IncludedSamples = [ sample scenario ]
      ExcludedSamples = []
      ProofProbeArtifacts = []
      Status = Compositor.Types.Feature158ReadinessStatus.Accepted
      ArtifactPaths = [ $"throughput/iterations/{Compositor.FeatureState.feature160ScenarioFileName scenario}" ]
      Diagnostics = [] }

let private iteration index : Compositor.Types.Feature160Iteration =
    let iterationId = sprintf "feature160-release-%03i" index
    let reports = Compositor.Config.feature160RequiredScenarioIds |> List.map scenarioReport
    { IterationId = iterationId
      RunId = "feature160-release"
      HostProfile = profile
      LaneId = Compositor.Config.feature160FocusedLaneId
      PolicyId = Compositor.Config.feature160PolicyId
      DeclaredBoundMinutes = 10
      ActualDuration = TimeSpan.FromMinutes 1.0
      WarmupCount = 3
      MeasuredRepetitions = 5
      ScenarioReports = reports
      ScenarioCoverage = Compositor.Config.feature160RequiredScenarioIds
      IncludedSamples = reports |> List.collect _.IncludedSamples
      ExcludedSamples = []
      Status = Compositor.Types.Feature160ReadinessStatus.Accepted
      ExclusionReason = None
      ArtifactPaths = [ $"throughput/iterations/{Compositor.FeatureState.feature160IterationFileName iterationId}" ]
      RestrictedScenario = None
      Diagnostics = [] }

let private fullValidation status command : Compositor.Types.Feature160FullValidationRecord =
    { Command = command
      StartedAt = Some DateTimeOffset.UnixEpoch
      CompletedAt = Some(DateTimeOffset.UnixEpoch.AddMinutes 1.0)
      Status = status
      ImplementationCommit = "abc123"
      PackageSurfaceBaseline = "readiness/fsi"
      ReadinessArtifactSet = [ "validation-summary.md" ]
      ArtifactPaths = [ "full-validation/validation.md" ]
      Diagnostics = [] }

let private summary validation : Compositor.Types.Feature160ThroughputSummary =
    { RunId = "feature160-release"
      HostProfile = profile
      LaneId = Compositor.Config.feature160FocusedLaneId
      PolicyId = Compositor.Config.feature160PolicyId
      DeclaredBoundMinutes = 10
      RequiredAttempts = 3
      WarmupCount = 3
      MeasuredRepetitions = 5
      Iterations = [ iteration 1; iteration 2; iteration 3 ]
      UnsupportedHostReason = None
      FullValidation = validation
      CompatibilityImpact = "accepted"
      PackageValidationStatus = "accepted"
      RegressionValidationStatus = "accepted"
      Status = Compositor.Types.Feature160ReadinessStatus.Accepted
      ReleaseReadyStatus = "pending"
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature160 ReleaseGate separation" [
        test "focused throughput can pass while missing full validation blocks release ready" {
            let package = summary None
            Expect.equal (Compositor.FeatureState.feature160FocusedThroughputStatus package) Compositor.Types.Feature160ReadinessStatus.Accepted "focused accepted"
            Expect.equal (Compositor.FeatureState.feature160OverallStatus package) Compositor.Types.Feature160ReadinessStatus.Blocked "overall blocked"
            Expect.equal (Compositor.FeatureState.feature160FullValidationStatus package.FullValidation) "missing" "missing"
        }

        test "failing interrupted stale or wrong-command full validation blocks release ready" {
            [ "failed", "dotnet test FS.GG.Rendering.slnx --no-restore", "failed"
              "interrupted", "dotnet test FS.GG.Rendering.slnx --no-restore", "interrupted"
              "passed", "dotnet test other.sln --no-restore", "stale"
              "stale", "dotnet test FS.GG.Rendering.slnx --no-restore", "stale" ]
            |> List.iter (fun (status, command, expected) ->
                let package = summary (Some(fullValidation status command))
                Expect.equal (Compositor.FeatureState.feature160FullValidationStatus package.FullValidation) expected expected
                Expect.equal (Compositor.FeatureState.feature160OverallStatus package) Compositor.Types.Feature160ReadinessStatus.Blocked $"blocked {expected}")
        }

        test "current passing full validation unblocks release-ready status while claim remains separate" {
            let package = summary (Some(fullValidation "passed" "dotnet test FS.GG.Rendering.slnx --no-restore"))
            Expect.equal (Compositor.FeatureState.feature160OverallStatus package) Compositor.Types.Feature160ReadinessStatus.Accepted "accepted"
            let rendered = Compositor.Render4.emitFeature160ValidationSummary package
            Expect.stringContains rendered "Full validation status: `passed`" "full validation"
            Expect.stringContains rendered "Performance claim: `performance-not-accepted`" "claim boundary"
        }
    ]
