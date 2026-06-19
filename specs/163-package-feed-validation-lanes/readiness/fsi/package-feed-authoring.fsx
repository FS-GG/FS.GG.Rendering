open Rendering.Harness

let mode = PackageFeed.tryParseMode "proof"
let status = PackageFeed.statusToken PackageFeed.Current
printfn "%A %s" mode status
