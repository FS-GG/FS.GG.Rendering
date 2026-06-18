let command = "compositor-performance --feature 158 --policy readback-free-timing-v1"
let probe = "compositor-performance --feature 158 --probe-readback"
let acceptedPolicies = [ "readback-free"; "readback-outside-measurement" ]
let excludedProbeReason = "probe-run-excluded"
printfn "%s %s %A" command probe acceptedPolicies