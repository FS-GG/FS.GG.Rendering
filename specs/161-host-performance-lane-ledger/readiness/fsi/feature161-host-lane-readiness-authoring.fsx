open FS.GG.UI.Testing

let facts =
    { LaneId = "x11-:1-direct-opengl-amd-mesa"
      DisplayServer = "x11"
      DisplayIdentity = ":1"
      RendererIdentity = "AMD Radeon Mesa"
      DirectRendering = Some true
      RefreshStatus = "119.93 Hz"
      DriverIdentity = "Mesa"
      PackageVersionSet = "local-harness"
      CpuLoadNote = "representative"
      GpuLoadNote = "representative"
      EnvironmentLimits = []
      HostProfile = "probe-08a47c01"
      RunIdentity = "feature161-authoring"
      ScenarioIdentity = "timing/host-lane-ledger"
      TimingPolicyIdentity = "host-lane-ledger-v1"
      ArtifactPaths = [ "lane-ledger/entries/entry-feature161-authoring.md" ] }

let check =
    { Feature = "161-host-performance-lane-ledger"
      RequiredScenarioIds = [ "timing/host-lane-ledger" ]
      CoveredScenarioIds = [ "timing/host-lane-ledger" ]
      HostFacts = Some facts
      AcceptedLaneScopedPerformanceArtifacts = 1
      UnsupportedHostStatus = Feature161FallbackOnly
      PriorGateStatuses = [ "confirmed"; "confirmed"; "confirmed"; "confirmed"; "confirmed" ]
      ClaimScope =
        { AcceptedLaneId = Some facts.LaneId
          NonGeneralizedLanes = [ "Wayland"; "indirect GL"; "missing display" ]
          RemainingBlockers = []
          PerformanceClaim = "performance-not-accepted" }
      FullValidationStatus = "passed"
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      Limitations = [] }

let result = Feature161HostLaneReadiness.validate check
printfn "%s %b" (Feature161HostLaneReadiness.statusText result.Status) result.Accepted