open Rendering.Harness

let status = ValidationLanes.statusToken ValidationLanes.TimedOut
let readiness = ValidationLanes.readinessToken ValidationLanes.Blocked
printfn "%s %s" status readiness
