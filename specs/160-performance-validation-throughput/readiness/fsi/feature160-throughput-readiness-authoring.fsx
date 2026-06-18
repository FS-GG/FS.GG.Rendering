open FS.GG.UI.Testing

let scenario id =
    { ScenarioId = id
      Covered = true
      WarmupCount = 3
      MeasuredRepetitions = 5
      SamplePolicy = "readback-free"
      ArtifactPaths = [ $"throughput/iterations/{id}.md" ]
      PrimaryReason = None }

let required = [ "timing/localized-update"; "timing/no-change"; "timing/movement-old-new"; "timing/overlap"; "timing/edge-clipping" ]
let check =
    { Feature = "160-performance-validation-throughput"
      RequiredScenarioIds = required
      Scenarios = required |> List.map scenario
      AcceptedIterationCount = 3
      RequiredIterationCount = 3
      UnsupportedHostStatus = Feature160EnvironmentLimited
      AcceptedUnsupportedHostArtifacts = 0
      FullValidationStatus = "passed"
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      PerformanceClaim = "performance-not-accepted"
      Limitations = [] }

let result = Feature160ThroughputReadiness.validate check
printfn "%s %b" (Feature160ThroughputReadiness.statusText result.Status) result.Accepted