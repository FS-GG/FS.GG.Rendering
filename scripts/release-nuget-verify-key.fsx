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
    if not successor then invalidArg "plan" "verify-key-plan-refused"
    use plan = JsonDocument.Parse(planBytes)
    let baseline = plan.RootElement.GetProperty("baselineVersion").GetString()
    let ids = plan.RootElement.GetProperty("packages").EnumerateArray() |> Seq.map(fun p -> p.GetProperty("id").GetString()) |> Seq.toList
    if ids |> List.exists(fun id -> isNull id || not(Regex.IsMatch(id,"^FS\\.GG\\.UI(?:\\.[A-Za-z][A-Za-z0-9]*)*$"))) then invalidArg "roster" "verify-key-roster-refused"
    // Reuse the genuine same-run preflight receipt; do not invent a producer grant
    // or start another source-reader process merely to label read-only observations.
    let scopeSource =
        if successor && scopeOnly then
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
    // Join the selected executor and same-run facts before credential lookup or transport.
    let expected = Environment.GetEnvironmentVariable("FSGG_EXPECTED_EXECUTOR_SHA")
    let current name = Environment.GetEnvironmentVariable(name)
    if isNull expected || not(Regex.IsMatch(expected,"^[0-9a-f]{40}$")) ||
       expected<>current "GITHUB_SHA" || expected<>current "GITHUB_WORKFLOW_SHA" then
        invalidArg "executor" "verify-key-executor-refused"
    let joinExecutor (proof: JsonElement) =
        for (field, actual) in ["expectedExecutorSha",expected; "executorSha",expected;
                              "workflowContextSha",expected; "workflowSha",expected;
                              "workflowRef",current "GITHUB_WORKFLOW_REF";
                              "run",current "GITHUB_RUN_ID"; "attempt",current "GITHUB_RUN_ATTEMPT"] do
            if proof.GetProperty(field).GetString()<>actual then
                invalidArg "executor" "verify-key-executor-facts-refused"
    use executorFacts = JsonDocument.Parse(File.ReadAllBytes(
        if scopeOnly then "artifacts/preflight/executor-binding.json" else value "--qualification"))
    joinExecutor executorFacts.RootElement
    if not scopeOnly then
        use inputs = JsonDocument.Parse(File.ReadAllBytes("artifacts/attempt/input-binding.json"))
        joinExecutor inputs.RootElement
        if inputs.RootElement.GetProperty("producerSha").GetString()<>"6c9f766fdd91483c2de6f061e75589e94852a265" ||
           inputs.RootElement.GetProperty("originalPlanSha256").GetString()<>planHash ||
           inputs.RootElement.GetProperty("outerSha256").GetString()<>"486182db3efd8442c007f66322a7bfb27caf0cb8e3c87bc2521e66dd90a7adee" ||
           inputs.RootElement.GetProperty("custodySha256").GetString()<>"ecd0c5d742fd8d19d5659991e413e92971c1d2cb9aea913faa2bb249f53f577d" then
            invalidArg "input" "verify-key-input-binding-refused"
    let started = DateTimeOffset.UtcNow.ToString("O")
    let key = Environment.GetEnvironmentVariable("NUGET_API_KEY")
    if String.IsNullOrWhiteSpace key then invalidArg "credential" "verify-key-credential-unavailable"
    match inspect (httpTransport key) ids baseline with
    | Error _ -> invalidArg "roster" "verify-key-roster-refused"
    | Ok(observations,passed) ->
        let receipt = {| schema="fsgg.rendering.nuget-verify-key/v1"; producerSha=(if scopeOnly then null else "6c9f766fdd91483c2de6f061e75589e94852a265"); sourceSha=scopeSource; expectedExecutorSha=expected; workflowSha=current "GITHUB_WORKFLOW_SHA"; executorSha=Environment.GetEnvironmentVariable("GITHUB_SHA"); executorRef=Environment.GetEnvironmentVariable("GITHUB_WORKFLOW_REF"); runId=Environment.GetEnvironmentVariable("GITHUB_RUN_ID"); attempt=Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"); planSha256=planHash; rosterSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(String.Join("\n",ids)))).ToLowerInvariant(); baseline=baseline; startedUtc=started; endedUtc=DateTimeOffset.UtcNow.ToString("O"); observations=(observations |> List.map(fun row -> {|packageId=row.PackageId; httpStatus=(match row.HttpStatus with Some n -> Nullable n | None -> Nullable()); outcome=row.Outcome|})); passed=passed; noPackageVersionMutation=true |}
        let output = value "--receipt"
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
        File.WriteAllText(output,JsonSerializer.Serialize(receipt,JsonSerializerOptions(WriteIndented=true))+"\n")
        printfn "NuGet existing-ID verify-key: %d/19 HTTP200; no package mutation" (observations |> List.filter(fun row -> row.HttpStatus=Some 200) |> List.length)
        if not passed then exit 3
        if scopeOnly then exit 0
        let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
        if String.IsNullOrWhiteSpace token then invalidArg "credential" "census-credential-unavailable"
        match Rendering.ReleasePackageCensus.inspectWith Rendering.ReleasePackageCensus.DefaultLimits (Rendering.ReleasePackageCensus.httpTransport token) ids "0.32.1" with
        | Error _ -> invalidArg "census" "census-unavailable"
        | Ok census ->
            let censusOutput = value "--census"
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath censusOutput)) |> ignore
            File.WriteAllText(censusOutput,Rendering.ReleasePackageCensus.receiptJson "6c9f766fdd91483c2de6f061e75589e94852a265" (Environment.GetEnvironmentVariable("GITHUB_SHA")) (Environment.GetEnvironmentVariable("GITHUB_RUN_ID")) "0.32.1" planHash census)
            use qualified = JsonDocument.Parse(File.ReadAllBytes(value "--qualification"))
            let proof = qualified.RootElement
            let flag (key: string) = proof.GetProperty(key).GetBoolean()
            let number (key: string) = proof.GetProperty(key).GetInt32()
            let facts = { OriginalCustodyVerified=flag "originalCustodyVerified"; NativeTemplatesQualified=flag "nativeTemplatesQualified"; ProviderPassed=number "providerPassed"; BrowserPassed=proof.GetProperty("browserPassed").EnumerateArray() |> Seq.map(fun n -> n.GetInt32()) |> Seq.toList; BrowserUnexpected=number "browserUnexpected"; BrowserSkipped=number "browserSkipped"; BrowserFlaky=number "browserFlaky"; CompleteReadCensus=census.Organization.Complete && census.Packages.Length=19 && (census.Packages |> List.forall(fun (_,row) -> row.Complete)); NuGetScopeStatuses=observations |> List.map(fun row -> row.HttpStatus); GitHubEffectiveWrite=proof.GetProperty("githubEffectiveWrite").GetString() }
            if not(admit facts) then invalidArg "eligibility" "publication-attempt-ineligible"
            printfn "Attempt prerequisites joined; GitHub effective write UNKNOWN before actual acknowledgement/readback"
with
| :? ArgumentException as error when List.contains error.ParamName ["executor";"input";"credential"] ->
    // Only a closed stage name is observable; never print exception/header payloads.
    eprintfn "NuGet verify-key: refused stage=%s; no publication eligibility" error.ParamName
    exit 3
| _ ->
    // Header-bearing exception payloads are deliberately never printed.
    eprintfn "NuGet verify-key: unavailable/refused; no publication eligibility"
    exit 3
