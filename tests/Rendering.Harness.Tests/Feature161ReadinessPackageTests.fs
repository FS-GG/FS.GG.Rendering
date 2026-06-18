module Feature161ReadinessPackageTests

open System
open System.IO
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
      RunIdentity = "feature161-readiness"
      ScenarioIdentity = "timing/host-lane-ledger"
      TimingPolicyIdentity = Compositor.feature161PolicyId
      CollectionTime = DateTimeOffset.UnixEpoch
      ArtifactLocations = [ "lane-ledger/entries/entry-feature161-readiness.md" ] }

let private entry : Compositor.Feature161LedgerEntry =
    { EntryId = "feature161-readiness"
      LaneId = Compositor.feature161HostLaneId
      HostFacts = facts
      PriorGates = Compositor.feature161PriorGateLinks
      Status = Compositor.Feature161ReadinessStatus.Accepted
      PrimaryExclusionReason = None
      TimingStatus = "lane-scoped"
      AcceptedLaneScopedPerformanceArtifacts = 1
      ArtifactPaths = [ "lane-ledger/entries/entry-feature161-readiness.md" ]
      Diagnostics = [] }

let private summary : Compositor.Feature161Summary =
    let scope = Compositor.feature161ScopeFromEntries [ entry ]
    { RunId = "feature161-readiness"
      HostProfile = profile
      PolicyId = Compositor.feature161PolicyId
      Entries = [ entry ]
      UnsupportedHostReason = None
      ClaimScope = scope
      FullValidationStatus = "passed"
      CompatibilityImpact = "Feature161HostLaneReadiness helper added"
      PackageValidationStatus = "accepted-with-recorded-limitations"
      RegressionValidationStatus = "accepted-with-recorded-limitations"
      Status = Compositor.Feature161ReadinessStatus.Accepted
      ReleaseReadyStatus = "ready"
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature161 ReadinessPackage" [
        test "validation summary links reviewer entry point artifacts and performance claim boundary" {
            let rendered = Compositor.renderFeature161ValidationSummary summary
            [ "lane-ledger/summary.md"
              "lane-ledger/host-facts/"
              "lane-ledger/excluded/"
              "lane-ledger/unsupported/README.md"
              "full-validation/validation.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md"
              "Feature 155"
              "Feature 157"
              "Feature 158"
              "Feature 159"
              "Feature 160"
              "performance-not-accepted"
              "Under-5-minute reviewer decision target" ]
            |> List.iter (fun required -> Expect.stringContains rendered required $"contains {required}")
        }

        test "unsupported host and excluded evidence record zero accepted lane artifacts" {
            let unsupported = Compositor.renderFeature161UnsupportedHostReport "missing display"
            Expect.stringContains unsupported "Status: `environment-limited`" "status"
            Expect.stringContains unsupported "Accepted lane-scoped performance artifacts: `0`" "zero artifacts"

            let excluded = Compositor.renderFeature161ExcludedEvidenceReport Perf.HostFactsMissing [ { entry with PrimaryExclusionReason = Some Perf.HostFactsMissing; AcceptedLaneScopedPerformanceArtifacts = 0 } ]
            Expect.stringContains excluded "host-facts-missing" "reason"
            Expect.stringContains excluded "Accepted lane-scoped performance contribution: `0`" "zero contribution"
        }

        test "CLI writes Feature 161 readiness package and reviewer entry point" {
            let root = Path.Combine(Path.GetTempPath(), "feature161-readiness-" + Guid.NewGuid().ToString("N"))
            try
                let exitCode =
                    Cli.main
                        [| "compositor-readiness"
                           "--feature"
                           "161"
                           "--out"
                           root |]

                Expect.equal exitCode 0 "command exits cleanly"
                [ "validation-summary.md"
                  "compatibility-ledger.md"
                  "package-validation.md"
                  "regression-validation.md"
                  "lane-ledger/summary.md"
                  "lane-ledger/summary.json"
                  "lane-ledger/unsupported/README.md"
                  "full-validation/validation.md"
                  "fsi/compositor-host-lane-authoring.fsx"
                  "fsi/feature161-host-lane-readiness-authoring.fsx" ]
                |> List.iter (fun relative ->
                    let path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))
                    Expect.isTrue (File.Exists path) $"exists {relative}")

                let text = File.ReadAllText(Path.Combine(root, "validation-summary.md"))
                Expect.stringContains text "performance-not-accepted" "claim boundary"
                Expect.stringContains text "lane-ledger/summary.md" "ledger link"
            finally
                if Directory.Exists root then
                    Directory.Delete(root, true)
        }
    ]
