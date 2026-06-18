#r "nuget: FS.GG.UI.Testing"

open FS.GG.UI.Testing

let evidence name status =
    { EvidenceName = name
      EvidencePath = Some($"specs/154-compositor-proof-acceptance/readiness/{name}")
      EvidenceStatus = status
      EvidenceRequired = true
      EvidenceDiagnostics = [] }

let report =
    { Feature = "154-compositor-proof-acceptance"
      ProofStatus = CompositorReadinessEnvironmentLimited
      ParityStatus = CompositorReadinessFallbackGated
      TimingStatus = CompositorReadinessEnvironmentLimited
      CompatibilityStatus = CompositorReadinessAccepted
      RegressionStatus = CompositorReadinessAccepted
      Evidence = [ evidence "validation-summary.md" CompositorReadinessEnvironmentLimited ]
      Limitations = [ "zero accepted partial-redraw artifacts" ] }

let validation = CompositorReadiness.validate report

printfn "Feature154 readiness status = %s" (CompositorReadiness.statusText validation.Status)
