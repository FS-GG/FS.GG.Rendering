#r "nuget: FS.GG.UI.SkiaViewer"
#r "nuget: FS.GG.UI.Controls.Elmish"
#r "nuget: FS.GG.UI.Testing"

open System
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Testing

let proofStatus = CompositorProof.ProofReadiness.EnvironmentLimited "missing display"
let proofToken = CompositorProof.readinessToken proofStatus

let metrics =
    Unchecked.defaultof<FrameMetrics>

let _diagnosticsTypeName = typeof<CompositorFrameDiagnostics>.FullName

let discovery =
    ReadinessFileDiscovery.validate
        { ReadinessDirectory = "specs/149-complete-compositor-p7/readiness"
          RequiredFiles = [ "validation-summary.md"; "compatibility-ledger.md"; "live-proof/proof.md" ]
          ExistingFiles = [ "validation-summary.md"; "compatibility-ledger.md"; "live-proof/proof.md" ] }

printfn "FSI transcript PASS: %s %b" proofToken discovery.Complete
