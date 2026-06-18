let readiness = "compositor-readiness --feature 158"
let statusTokens = [ "accepted"; "rejected"; "fallback-only"; "environment-limited" ]
let performanceClaim = "performance-not-accepted"
let noTestingHelperSurface = true
let noSkiaViewerHelperSurface = true
printfn "%s %s %b %b" readiness performanceClaim noTestingHelperSurface noSkiaViewerHelperSurface