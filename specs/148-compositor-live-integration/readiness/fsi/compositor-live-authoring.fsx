#r "nuget: FS.GG.UI.SkiaViewer"
#r "nuget: FS.GG.UI.Controls.Elmish"

open System
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls.Elmish

let proofStatus = CompositorProof.ProofReadiness.EnvironmentLimited "missing display"
let proofToken = CompositorProof.readinessToken proofStatus

let metrics =
    Unchecked.defaultof<FrameMetrics>

let _diagnosticsTypeName = typeof<CompositorFrameDiagnostics>.FullName

printfn "FSI transcript PASS: %s" proofToken
