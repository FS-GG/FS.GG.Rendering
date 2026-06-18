open System
open System.IO

let readinessRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName

let readRequired relativePath =
    let path = Path.Combine(readinessRoot, relativePath)
    if not (File.Exists path) then
        failwithf "Required readiness artifact is missing: %s" relativePath
    File.ReadAllText path

let requireContains (label: string) (token: string) (text: string) =
    if not (text.Contains(token, StringComparison.Ordinal)) then
        failwithf "%s did not contain required token: %s" label token

let proofSet = readRequired "proof-set.md"
let validationSummary = readRequired "validation-summary.md"
let unsupported = readRequired (Path.Combine("live-proof", "unsupported", "README.md"))

requireContains "proof-set.md" "Status: `accepted`" proofSet
requireContains "proof-set.md" "Selected attempts: `3/3`" proofSet
requireContains "validation-summary.md" "Status: `accepted`" validationSummary
requireContains "validation-summary.md" "Proof set: `accepted`" validationSummary
requireContains "validation-summary.md" "Parity status: `accepted`" validationSummary
requireContains "validation-summary.md" "Performance claim: `not-accepted`" validationSummary
requireContains "unsupported README" "Accepted partial-redraw artifacts: `0`" unsupported

let attemptsRoot = Path.Combine(readinessRoot, "live-proof", "attempts")
let attempts = Directory.GetDirectories(attemptsRoot, "feature155-*") |> Array.sort

if attempts.Length <> 3 then
    failwithf "Expected exactly three Feature155 attempts, found %d" attempts.Length

for attempt in attempts do
    let proof = Path.Combine(attempt, "proof.md")
    let sentinel = Path.Combine(attempt, "sentinel-frame.png")
    let damage = Path.Combine(attempt, "damage-frame.png")

    if not (File.Exists proof && File.Exists sentinel && File.Exists damage) then
        failwithf "Attempt is missing proof or PNG artifacts: %s" attempt

    let proofText = File.ReadAllText proof
    requireContains proof "Verdict: `passed`" proofText
    requireContains proof "damaged-updated=true" proofText
    requireContains proof "undamaged-preserved=true" proofText

printfn "Feature155 native proof capture readiness accepted with %d attempts." attempts.Length
printfn "Performance claim remains not accepted."
