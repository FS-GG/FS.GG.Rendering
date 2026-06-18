module Feature161LaneLedgerTests

open System
open Expecto
open Rendering.Harness

let private profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature161AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "AMD Radeon RX 7900 XT (Mesa)"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private facts : Compositor.Feature161HostFacts =
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
      RunIdentity = "feature161-ledger"
      ScenarioIdentity = "timing/host-lane-ledger"
      TimingPolicyIdentity = Compositor.feature161PolicyId
      CollectionTime = DateTimeOffset.UnixEpoch
      ArtifactLocations = [ "lane-ledger/entries/entry-feature161-ledger.md" ] }

let private entry id lane status reason acceptedArtifacts : Compositor.Feature161LedgerEntry =
    { EntryId = id
      LaneId = lane
      HostFacts = facts
      PriorGates = Compositor.feature161PriorGateLinks
      Status = status
      PrimaryExclusionReason = reason
      TimingStatus = if acceptedArtifacts > 0 then "lane-scoped" else "not-accepted"
      AcceptedLaneScopedPerformanceArtifacts = acceptedArtifacts
      ArtifactPaths = [ $"lane-ledger/entries/entry-{id}.md" ]
      Diagnostics = [] }

let private summary entries : Compositor.Feature161Summary =
    let scope = Compositor.feature161ScopeFromEntries entries
    let provisional : Compositor.Feature161Summary =
        { RunId = "feature161-ledger"
          HostProfile = profile
          PolicyId = Compositor.feature161PolicyId
          Entries = entries
          UnsupportedHostReason = None
          ClaimScope = scope
          FullValidationStatus = "passed"
          CompatibilityImpact = "test"
          PackageValidationStatus = "test"
          RegressionValidationStatus = "test"
          Status = Compositor.Feature161ReadinessStatus.FallbackOnly
          ReleaseReadyStatus = "pending"
          PerformanceClaim = "performance-not-accepted"
          Diagnostics = [] }

    { provisional with Status = Compositor.feature161OverallStatus provisional }

[<Tests>]
let tests =
    testList "Feature161 LaneLedger Unsupported" [
        test "accepted claim scope names exact lane and non-generalized lanes" {
            let accepted = entry "accepted" Compositor.feature161HostLaneId Compositor.Feature161ReadinessStatus.Accepted None 1
            let scope = Compositor.feature161ScopeFromEntries [ accepted ]
            Expect.equal scope.AcceptedLaneId (Some Compositor.feature161HostLaneId) "accepted lane"
            Expect.stringContains scope.AppliesTo "X11 `:1`" "scope"
            Expect.contains scope.NonGeneralizedLanes "Wayland" "Wayland not generalized"
            Expect.contains scope.NonGeneralizedLanes "software raster" "software raster not generalized"
            Expect.equal scope.PerformanceClaim "performance-not-accepted" "claim boundary"
        }

        test "cross-lane aggregation is rejected and not counted as accepted evidence" {
            let wrongLane = entry "wayland" "wayland-wayland-0-direct-opengl-amd-mesa" Compositor.Feature161ReadinessStatus.Rejected (Some Perf.CrossLaneEvidence) 0
            let scope = Compositor.feature161ScopeFromEntries [ wrongLane ]
            Expect.equal scope.AcceptedLaneId None "no accepted lane"
            Expect.contains scope.RemainingBlockers "no accepted lane-scoped performance artifacts" "blocker"
            Expect.isFalse (Compositor.feature161LedgerEntryAccepted wrongLane) "not accepted"
            let rendered = Compositor.renderFeature161ExcludedEvidenceReport Perf.CrossLaneEvidence [ wrongLane ]
            Expect.stringContains rendered "cross-lane-evidence" "reason"
            Expect.stringContains rendered "Accepted lane-scoped performance contribution: `0`" "zero contribution"
        }

        test "fail-closed host fact classifications preserve zero accepted artifacts" {
            let cases =
                [ { facts with DisplayServer = "missing-display"; DisplayIdentity = "missing-display" }, Perf.MissingDisplay
                  { facts with DirectRendering = Some false }, Perf.IndirectRendering
                  { facts with RendererIdentity = "llvmpipe software rasterizer" }, Perf.SoftwareRaster
                  { facts with RendererIdentity = "" }, Perf.UnknownRenderer
                  { facts with EnvironmentLimits = [ "virtualized-presentation" ] }, Perf.VirtualizedPresentation
                  { facts with PackageVersionSet = "stale-package" }, Perf.PackageVersionMismatch
                  { facts with CpuLoadNote = "non-representative load" }, Perf.LoadNonRepresentative ]

            cases
            |> List.iter (fun (caseFacts, expected) ->
                Expect.equal (Compositor.feature161ValidateHostFacts caseFacts) (Some expected) (Perf.exclusionReasonToken expected))
        }

        test "summary renders lane facts excluded evidence unsupported result and final claim status" {
            let accepted = entry "accepted" Compositor.feature161HostLaneId Compositor.Feature161ReadinessStatus.Accepted None 1
            let rejected = entry "noisy" Compositor.feature161HostLaneId Compositor.Feature161ReadinessStatus.Rejected (Some Perf.NoisyTiming) 0
            let rendered = Compositor.renderFeature161LaneLedgerSummary (summary [ accepted; rejected ])
            [ "lane-ledger/host-facts/"
              "lane-ledger/entries/"
              "lane-ledger/excluded/"
              "lane-ledger/unsupported/README.md"
              "performance-not-accepted"
              "Wayland"
              "noisy-timing" ]
            |> List.iter (fun required -> Expect.stringContains rendered required $"contains {required}")
        }
    ]
