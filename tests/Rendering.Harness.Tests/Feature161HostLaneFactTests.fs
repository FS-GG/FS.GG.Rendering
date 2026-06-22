module Feature161HostLaneFactTests

open System
open Expecto
open Rendering.Harness

let private profile : Compositor.Types.HostProfile =
    { ProfileId = Compositor.Config.feature161AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "AMD Radeon RX 7900 XT (Mesa)"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private facts : Compositor.Types.Feature161HostFacts =
    { DisplayServer = "x11"
      DisplayIdentity = ":1"
      RendererIdentity = "AMD Radeon RX 7900 XT (Mesa)"
      DirectRendering = Some true
      RefreshRateHz = Some 119.93
      RefreshUnavailableReason = None
      DriverIdentity = "Mesa 25"
      PackageVersionSet = "Rendering.Harness=local;FS.GG.UI.Testing=local"
      CpuLoadNote = "representative"
      GpuLoadNote = "representative"
      EnvironmentLimits = []
      HostProfile = profile
      RunIdentity = "feature161-test"
      ScenarioIdentity = "timing/host-lane-ledger"
      TimingPolicyIdentity = Compositor.Config.feature161PolicyId
      CollectionTime = DateTimeOffset.UnixEpoch
      ArtifactLocations = [ "lane-ledger/entries/entry-feature161-test.md" ] }

let private entry status reason : Compositor.Types.Feature161LedgerEntry =
    { EntryId = "feature161-test"
      LaneId = Compositor.FeatureState.feature161LaneIdFromFacts facts
      HostFacts = facts
      PriorGates = Compositor.Config.feature161PriorGateLinks
      Status = status
      PrimaryExclusionReason = reason
      TimingStatus = "lane-scoped"
      AcceptedLaneScopedPerformanceArtifacts = if reason.IsNone then 1 else 0
      ArtifactPaths = [ "lane-ledger/entries/entry-feature161-test.md" ]
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature161 HostLaneFact" [
        test "declares host lane constants required scenarios and exclusion tokens" {
            Expect.equal Compositor.Config.feature161PolicyId "host-lane-ledger-v1" "policy"
            Expect.equal Compositor.Config.feature161HostLaneId "x11-:1-direct-opengl-amd-mesa" "lane"
            Expect.equal Compositor.Config.feature161RequiredScenarioIds Compositor.Config.feature160RequiredScenarioIds "inherits Feature 160 timing scenarios"
            Expect.equal Compositor.Config.feature161PriorGateLinks.Length 5 "prior gates"

            [ Perf.MissingDisplay, "missing-display"
              Perf.IndirectRendering, "indirect-rendering"
              Perf.SoftwareRaster, "software-raster"
              Perf.UnknownRenderer, "unknown-renderer"
              Perf.VirtualizedPresentation, "virtualized-presentation"
              Perf.AmbiguousGpu, "ambiguous-gpu"
              Perf.RefreshRateUnavailable, "refresh-rate-unavailable"
              Perf.LoadNonRepresentative, "load-non-representative"
              Perf.HostFactsMissing, "host-facts-missing"
              Perf.HostFactsContradictory, "host-facts-contradictory"
              Perf.CrossLaneEvidence, "cross-lane-evidence"
              Perf.NoisyTiming, "noisy-timing"
              Perf.PriorGateBlocked, "prior-gate-blocked" ]
            |> List.iter (fun (reason, token) -> Expect.equal (Perf.exclusionReasonToken reason) token token)
        }

        test "validates complete host facts and names primary missing or contradictory facts" {
            Expect.equal (Compositor.FeatureState.feature161LaneIdFromFacts facts) Compositor.Config.feature161HostLaneId "lane id"
            Expect.isNone (Compositor.FeatureState.feature161ValidateHostFacts facts) "complete facts"

            Expect.equal (Compositor.FeatureState.feature161ValidateHostFacts { facts with DisplayIdentity = "" }) (Some Perf.MissingDisplay) "missing display"
            Expect.equal (Compositor.FeatureState.feature161ValidateHostFacts { facts with RendererIdentity = "" }) (Some Perf.UnknownRenderer) "unknown renderer"
            Expect.equal (Compositor.FeatureState.feature161ValidateHostFacts { facts with DirectRendering = Some false }) (Some Perf.IndirectRendering) "indirect"
            Expect.equal (Compositor.FeatureState.feature161ValidateHostFacts { facts with RefreshRateHz = None; RefreshUnavailableReason = None }) (Some Perf.RefreshRateUnavailable) "refresh"
            Expect.equal (Compositor.FeatureState.feature161ValidateHostFacts { facts with TimingPolicyIdentity = "other-policy" }) (Some Perf.HostFactsContradictory) "policy"
        }

        test "MVU records host facts prior gates ledger entries and publication artifacts" {
            let model0, effects0 = Compositor.FeatureState.initFeature161 (Some "throughput")
            Expect.contains effects0 Compositor.Types.Feature161DetectHostProfile "host profile"
            Expect.contains effects0 (Compositor.Types.Feature161DeclarePolicy Compositor.Config.feature161PolicyId) "policy"
            Expect.contains effects0 Compositor.Types.Feature161CollectHostFacts "facts"
            Expect.contains effects0 (Compositor.Types.Feature161LoadThroughputPackage "throughput") "source throughput"

            let accepted = entry Compositor.Types.Feature161ReadinessStatus.Accepted None
            let model1, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161HostProfileDetected profile) model0
            let model2, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161PolicyDeclared Compositor.Config.feature161PolicyId) model1
            let model3, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161HostFactsCollected facts) model2
            let model4, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161PriorGateLinked Compositor.Config.feature161PriorGateLinks.Head) model3
            let model5, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161LedgerEntryRecorded accepted) model4
            let model6, _ = Compositor.FeatureState.updateFeature161 (Compositor.Types.Feature161ArtifactPublished "lane-ledger/summary.md") model5

            Expect.equal model6.ActiveProfile (Some profile) "profile"
            Expect.equal model6.PolicyId (Some Compositor.Config.feature161PolicyId) "policy"
            Expect.equal model6.HostFacts (Some facts) "facts"
            Expect.equal model6.PriorGates.Length 1 "prior gate"
            Expect.equal model6.Entries.Length 1 "entry"
            Expect.contains model6.PublishedArtifacts "lane-ledger/summary.md" "published"
        }

        test "renders historical P7 gate status in reviewer-visible ledger entry" {
            let rendered = Compositor.Render4.emitFeature161LedgerEntry (entry Compositor.Types.Feature161ReadinessStatus.Accepted None)
            [ Compositor.Config.feature155Id
              Compositor.Config.feature157Id
              Compositor.Config.feature158Id
              Compositor.Config.feature159Id
              Compositor.Config.feature160Id
              "confirmed"
              "Accepted lane-scoped performance artifacts: `1`" ]
            |> List.iter (fun required -> Expect.stringContains rendered required $"contains {required}")
        }
    ]
