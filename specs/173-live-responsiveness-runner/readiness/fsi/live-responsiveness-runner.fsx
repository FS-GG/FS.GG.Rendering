// Feature 173 FSI authoring notes for live responsiveness runner public surfaces.
// Run after building or refreshing the local package feed so the sample Core assembly is available.

#r "../../../../samples/SecondAntShowcase/SecondAntShowcase.Core/bin/Release/net10.0/SecondAntShowcase.Core.dll"

open SecondAntShowcase.Core

let budgetP95 = Evidence.responsivenessTargetP95Ms
let budgetMax = Evidence.responsivenessTargetMaxMs
let drag = Evidence.responsivenessDragContinuity "slider-rating" 4 4 (Some 16.0) false
let status = ResponsivenessWorkflow.statusToken ResponsivenessWorkflow.EnvironmentLimited

printfn "p95=%i max=%i drag=%s status=%s" budgetP95 budgetMax drag.Classification status
