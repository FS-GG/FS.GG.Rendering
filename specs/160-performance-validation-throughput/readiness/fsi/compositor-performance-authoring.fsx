let command = "compositor-performance --feature 160 --lane focused --policy focused-throughput-v1"
let requiredScenarios =
    [ "timing/localized-update"
      "timing/no-change"
      "timing/movement-old-new"
      "timing/overlap"
      "timing/edge-clipping" ]
let bounds = {| maxIterationMinutes = 10; attempts = 3; unsupportedHostMinutes = 2 |}
let exclusions =
    [ "timed-out"; "canceled"; "partial-evidence"; "cross-profile-evidence"
      "stale-evidence"; "mixed-policy"; "missing-metadata"; "unsupported-host"
      "environment-limited"; "scenario-coverage-missing"; "sample-policy-mismatch"
      "run-identity-mismatch"; "artifact-unreadable"; "readback-contaminated" ]
printfn "%s %i %A %A" command bounds.maxIterationMinutes requiredScenarios exclusions