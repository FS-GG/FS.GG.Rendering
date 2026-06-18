open FS.GG.UI.Testing

let check =
    { Feature = "157-no-clear-damage-scissor"
      RequiredScenarioIds = [ "damage/static-preserved" ]
      Scenarios =
        [ { ScenarioId = "damage/static-preserved"
            Status = CompositorDamageAccepted
            AcceptedAttemptCount = 3
            ArtifactPaths = [ "damage/attempts/example.md" ]
            FallbackReason = None } ]
      AcceptedAttemptCount = 3
      UnsupportedHostStatus = CompositorDamageEnvironmentLimited
      AcceptedPartialRedrawArtifacts = 0
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      PerformanceClaim = "performance-not-accepted"
      Limitations = [] }

let result = CompositorDamageReadiness.validate check
printfn "%b" result.Accepted