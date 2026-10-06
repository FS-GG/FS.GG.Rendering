#load "NuGetVerifyKey.fs"
#load "AttemptEligibility.fs"
#load "ReleasePackageCensus.fs"
open System
open System.IO
open System.Text.Json
open System.Security.Cryptography
open System.Text.RegularExpressions
open Rendering.NuGetVerifyKey
open Rendering.AttemptEligibility

let args = fsi.CommandLineArgs |> Array.skip 1
let value name =
    let matches = args |> Array.indexed |> Array.filter (fun (_,v) -> v=name)
    if matches.Length<>1 || fst matches.[0]+1>=args.Length then invalidArg "arguments" "verify-key-arguments-refused"
    args.[fst matches.[0]+1]
try
    if args.Length<>8 || Set.ofList [args.[0];args.[2];args.[4];args.[6]]<>set ["--plan";"--receipt";"--qualification";"--census"] then invalidArg "arguments" "verify-key-arguments-refused"
    if Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")<>"FS-GG/FS.GG.Rendering" || Environment.GetEnvironmentVariable("GITHUB_WORKFLOW_REF")<>"FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main" then invalidArg "context" "verify-key-context-refused"
    let planBytes = File.ReadAllBytes(value "--plan")
    let planHash = Convert.ToHexString(SHA256.HashData planBytes).ToLowerInvariant()
    let scopeOnly = Environment.GetEnvironmentVariable("FSGG_NUGET_SCOPE_ONLY")="true"
    let successor = planHash="ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2"
    if planHash<>"6accaeb58e4cf8f4f5fea49dffbdcdbd8fa4bae4cd6387949cd7fe0df4f31b69" &&
       not(successor && scopeOnly) then invalidArg "plan" "verify-key-plan-refused"
    use plan = JsonDocument.Parse(planBytes)
    let baseline = plan.RootElement.GetProperty("baselineVersion").GetString()
    let ids = plan.RootElement.GetProperty("packages").EnumerateArray() |> Seq.map(fun p -> p.GetProperty("id").GetString()) |> Seq.toList
    if ids |> List.exists(fun id -> isNull id || not(Regex.IsMatch(id,"^FS\\.GG\\.UI(?:\\.[A-Za-z][A-Za-z0-9]*)*$"))) then invalidArg "roster" "verify-key-roster-refused"
    // Reuse the genuine same-run preflight receipt; do not invent a producer grant
    // or start another source-reader process merely to label read-only observations.
    let scopeSource =
        if successor then
            use prior = JsonDocument.Parse(File.ReadAllBytes(value "--qualification"))
            let context = { SourceSha=Environment.GetEnvironmentVariable("FSGG_NUGET_SCOPE_SOURCE");
                            WorkflowSha=Environment.GetEnvironmentVariable("GITHUB_SHA");
                            WorkflowRef=Environment.GetEnvironmentVariable("GITHUB_WORKFLOW_REF");
                            Repository=Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
                            RunId=Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
                            PlanHash=planHash; PackageIds=ids }
            match scopeSourceFromReceipt context prior.RootElement with
            | Ok source -> source
            | Error _ -> invalidArg "source" "verify-key-source-plan-refused"
        else null
    let started = DateTimeOffset.UtcNow.ToString("O")
    let key = Environment.GetEnvironmentVariable("NUGET_API_KEY")
    if String.IsNullOrWhiteSpace key then invalidArg "credential" "verify-key-credential-unavailable"
    match inspect (httpTransport key) ids baseline with
    | Error _ -> invalidArg "roster" "verify-key-roster-refused"
    | Ok(observations,passed) ->
        let receipt = {| schema="fsgg.rendering.nuget-verify-key/v1"; producerSha=(if successor then null else "730923fe9d27174e879566f21dab14a1b03d761a"); sourceSha=scopeSource; executorSha=Environment.GetEnvironmentVariable("GITHUB_SHA"); executorRef=Environment.GetEnvironmentVariable("GITHUB_WORKFLOW_REF"); runId=Environment.GetEnvironmentVariable("GITHUB_RUN_ID"); attempt=Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"); planSha256=planHash; rosterSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(String.Join("\n",ids)))).ToLowerInvariant(); baseline=baseline; startedUtc=started; endedUtc=DateTimeOffset.UtcNow.ToString("O"); observations=(observations |> List.map(fun row -> {|packageId=row.PackageId; httpStatus=(match row.HttpStatus with Some n -> Nullable n | None -> Nullable()); outcome=row.Outcome|})); passed=passed; noPackageVersionMutation=true |}
        let output = value "--receipt"
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
        File.WriteAllText(output,JsonSerializer.Serialize(receipt,JsonSerializerOptions(WriteIndented=true))+"\n")
        printfn "NuGet existing-ID verify-key: %d/19 HTTP200; no package mutation" (observations |> List.filter(fun row -> row.HttpStatus=Some 200) |> List.length)
        if not passed then exit 3
        if scopeOnly then exit 0
        let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
        if String.IsNullOrWhiteSpace token then invalidArg "credential" "census-credential-unavailable"
        match Rendering.ReleasePackageCensus.inspectWith Rendering.ReleasePackageCensus.DefaultLimits (Rendering.ReleasePackageCensus.httpTransport token) ids "0.32.0" with
        | Error _ -> invalidArg "census" "census-unavailable"
        | Ok census ->
            let censusOutput = value "--census"
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath censusOutput)) |> ignore
            File.WriteAllText(censusOutput,Rendering.ReleasePackageCensus.receiptJson "730923fe9d27174e879566f21dab14a1b03d761a" (Environment.GetEnvironmentVariable("GITHUB_SHA")) (Environment.GetEnvironmentVariable("GITHUB_RUN_ID")) "0.32.0" planHash census)
            use qualified = JsonDocument.Parse(File.ReadAllBytes(value "--qualification"))
            let proof = qualified.RootElement
            let flag (key: string) = proof.GetProperty(key).GetBoolean()
            let number (key: string) = proof.GetProperty(key).GetInt32()
            let facts = { OriginalCustodyVerified=flag "originalCustodyVerified"; NativeTemplatesQualified=flag "nativeTemplatesQualified"; ProviderPassed=number "providerPassed"; BrowserPassed=proof.GetProperty("browserPassed").EnumerateArray() |> Seq.map(fun n -> n.GetInt32()) |> Seq.toList; BrowserUnexpected=number "browserUnexpected"; BrowserSkipped=number "browserSkipped"; BrowserFlaky=number "browserFlaky"; CompleteReadCensus=census.Organization.Complete && census.Packages.Length=19 && (census.Packages |> List.forall(fun (_,row) -> row.Complete)); NuGetScopeStatuses=observations |> List.map(fun row -> row.HttpStatus); GitHubEffectiveWrite=proof.GetProperty("githubEffectiveWrite").GetString() }
            if not(admit facts) then invalidArg "eligibility" "publication-attempt-ineligible"
            printfn "Attempt prerequisites joined; GitHub effective write UNKNOWN before actual acknowledgement/readback"
with _ ->
    // Header-bearing exception payloads are deliberately never printed.
    eprintfn "NuGet verify-key: unavailable/refused; no publication eligibility"
    exit 3
