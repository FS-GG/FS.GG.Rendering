let command = "compositor-performance --feature 161 --lane host-ledger --policy host-lane-ledger-v1"
let requiredFacts =
    [ "display-server"; "display-identity"; "renderer-identity"; "direct-rendering"
      "refresh"; "driver-identity"; "package-version-set"; "cpu-load-note"
      "gpu-load-note"; "environment-limits"; "host-profile"; "run-identity"
      "scenario-identity"; "timing-policy-identity"; "collection-time"; "artifact-locations" ]
let nonGeneralized = [ "Wayland"; "indirect GL"; "missing display"; "software raster"; "virtualized presentation"; "unknown renderer" ]
printfn "%s %A %A" command requiredFacts nonGeneralized