module Feature161HostLaneReadinessTests

open Expecto
open FS.GG.UI.Testing

let private required =
    [ "timing/localized-update"
      "timing/no-change"
      "timing/movement-old-new"
      "timing/overlap"
      "timing/edge-clipping" ]

let private facts =
    { LaneId = "x11-:1-direct-opengl-amd-mesa"
      DisplayServer = "x11"
      DisplayIdentity = ":1"
      RendererIdentity = "AMD Radeon RX 7900 XT Mesa"
      DirectRendering = Some true
      RefreshStatus = "119.93 Hz"
      DriverIdentity = "Mesa 25"
      PackageVersionSet = "Rendering.Harness=local;FS.GG.UI.Testing=local"
      CpuLoadNote = "representative"
      GpuLoadNote = "representative"
      EnvironmentLimits = []
      HostProfile = "probe-08a47c01"
      RunIdentity = "feature161-testing"
      ScenarioIdentity = "timing/host-lane-ledger"
      TimingPolicyIdentity = "host-lane-ledger-v1"
      ArtifactPaths = [ "lane-ledger/entries/entry-feature161-testing.md" ] }

let private scope =
    { AcceptedLaneId = Some facts.LaneId
      NonGeneralizedLanes = [ "Wayland"; "indirect GL"; "missing display"; "software raster" ]
      RemainingBlockers = []
      PerformanceClaim = "performance-not-accepted" }

let private check fullValidationStatus =
    { Feature = "161-host-performance-lane-ledger"
      RequiredScenarioIds = required
      CoveredScenarioIds = required
      HostFacts = Some facts
      AcceptedLaneScopedPerformanceArtifacts = 1
      UnsupportedHostStatus = Feature161FallbackOnly
      PriorGateStatuses = [ "confirmed"; "confirmed"; "confirmed"; "confirmed"; "confirmed" ]
      ClaimScope = scope
      FullValidationStatus = fullValidationStatus
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      Limitations = [] }

[<Tests>]
let tests =
    testList "Feature161 host lane readiness helper" [
        test "accepts complete host lane readiness without accepting shipped performance" {
            let result = Feature161HostLaneReadiness.validate (check "passed")
            Expect.isTrue result.Accepted "accepted"
            Expect.equal result.Status Feature161Accepted "status"
            Expect.equal (Feature161HostLaneReadiness.statusText result.Status) "accepted" "token"
        }

        test "blocks complete host lane readiness when full validation or prior gates are not current" {
            [ { check "missing" with FullValidationStatus = "missing" }
              { check "passed" with PriorGateStatuses = [ "confirmed"; "blocked" ] } ]
            |> List.iter (fun package ->
                let result = Feature161HostLaneReadiness.validate package
                Expect.isFalse result.Accepted "not accepted"
                Expect.equal result.Status Feature161Blocked "blocked")
        }

        test "Synthetic helper fixture: missing host facts scenarios and overclaim fail closed" {
            // SYNTHETIC: in-memory package rows exercise helper rejection policy without live GL artifacts.
            let invalid =
                { check "passed" with
                    RequiredScenarioIds = required @ [ "timing/missing" ]
                    HostFacts = Some { facts with RendererIdentity = ""; DirectRendering = None; ArtifactPaths = [] }
                    ClaimScope = { scope with AcceptedLaneId = None; PerformanceClaim = "performance-accepted" }
                    Limitations = [ "overclaim attempted" ] }

            let result = Feature161HostLaneReadiness.validate invalid
            Expect.isFalse result.Accepted "invalid rejected"
            Expect.equal result.Status Feature161MissingEvidence "missing facts"
            Expect.contains result.MissingFacts "renderer-identity" "renderer"
            Expect.contains result.MissingFacts "direct-rendering" "direct rendering"
            Expect.contains result.MissingScenarios "timing/missing" "missing scenario"
            Expect.exists result.Diagnostics (fun item -> item.Contains("Feature 161 cannot broaden")) "claim boundary"
        }

        test "environment-limited package preserves zero accepted unsupported-host artifacts" {
            let environmentLimited =
                { check "missing" with
                    HostFacts = Some { facts with DisplayServer = "missing-display"; DisplayIdentity = "missing-display" }
                    AcceptedLaneScopedPerformanceArtifacts = 0
                    UnsupportedHostStatus = Feature161EnvironmentLimited
                    ClaimScope = { scope with AcceptedLaneId = None } }

            let result = Feature161HostLaneReadiness.validate environmentLimited
            Expect.isFalse result.Accepted "environment-limited is not accepted"
            Expect.equal result.Status Feature161EnvironmentLimited "status"
        }
    ]
