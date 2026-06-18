let command = "compositor-promotion --feature 159 --policy layer-promotion-v1"
let scenarios = [ "promotion/static-retained"; "promotion/placement-only-move"; "promotion/scroll-shifted"; "promotion/nested-retained"; "promotion/content-change"; "promotion/churn-demotion"; "promotion/fallback-safe" ]
printfn "%s %d" command scenarios.Length