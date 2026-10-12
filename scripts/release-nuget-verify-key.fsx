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
    let selected = Environment.GetEnvironmentVariable("FSGG_RELEASE_ATTEMPT")
    let currentAttempt = selected="0322"
    if not(String.IsNullOrEmpty selected || selected="0321" || currentAttempt) || (currentAttempt && scopeOnly) then
        invalidArg "input" "verify-key-attempt-selection-refused"
    let producer = if currentAttempt then "9b73395658d4683a067d150ca9441b04e802c4af" else "6c9f766fdd91483c2de6f061e75589e94852a265"
    let outer = if currentAttempt then "7b0532caf431cfa41acaf35555016cf126bc495c5a6e13fe7db6b8fbe7478cca" else "486182db3efd8442c007f66322a7bfb27caf0cb8e3c87bc2521e66dd90a7adee"
    let custody = if currentAttempt then "85bb1dcdf0378d05ca536d5878b88dedc301ff1524d39d98966e23888aae0120" else "ecd0c5d742fd8d19d5659991e413e92971c1d2cb9aea913faa2bb249f53f577d"
    let version = if currentAttempt then "0.32.2" else "0.32.1"
    // The new plan permits scope-only reads; the historical effect binding remains closed.
    let successor = planHash="ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2" ||
                    ((scopeOnly || currentAttempt) && planHash="81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a")
    if currentAttempt && planHash<>"81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a" then invalidArg "plan" "verify-key-current-plan-refused"
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
        if inputs.RootElement.GetProperty("producerSha").GetString()<>producer ||
           inputs.RootElement.GetProperty("originalPlanSha256").GetString()<>planHash ||
           inputs.RootElement.GetProperty("outerSha256").GetString()<>outer ||
           inputs.RootElement.GetProperty("custodySha256").GetString()<>custody then
            invalidArg "input" "verify-key-input-binding-refused"
        if currentAttempt && inputs.RootElement.GetProperty("effectId").GetString()<>"rendering-0322-original38200411661-a1" then
            invalidArg "input" "verify-key-current-effect-refused"
    let currentJourney =
        if currentAttempt then
            let proof = executorFacts.RootElement
            let flag key = proof.GetProperty(key:string).GetBoolean()
            let number key = proof.GetProperty(key:string).GetInt32()
            let journey : CurrentJourney = {
                OriginalCustodyVerified=flag "originalCustodyVerified"; NativeTemplatesQualified=flag "nativeTemplatesQualified"
                ProviderPassed=number "providerPassed"; BrowserPassed=proof.GetProperty("browserPassed").EnumerateArray() |> Seq.map(fun n -> n.GetInt32()) |> Seq.toList
                BrowserUnexpected=number "browserUnexpected"; BrowserSkipped=number "browserSkipped"; BrowserFlaky=number "browserFlaky"
                AtPassed=number "atPassed"; AtCustodyQualified=flag "atCustodyQualified" }
            if not(currentJourneyQualified journey) then invalidArg "input" "verify-key-current-journey-refused"
            Some journey
        else None
    if currentAttempt then
        use toolchain = JsonDocument.Parse(File.ReadAllBytes("artifacts/toolchain/publish-packages.json"))
        for (field, expectedValue) in ["sdkVersion","10.0.401"; "runtimeVersion","10.0.12";
                                      "compilerSha256","2bd6190eafa94f47739538467f5a7cd52de233a627789c69f07959d7578e4d6b";
                                      "fsharpCoreSha256","2232698f623c28b8bcca32fa3cc8943cdd8529b20b487b5938106aae1c2ab256"] do
            if toolchain.RootElement.GetProperty(field).GetString()<>expectedValue then
                invalidArg "input" "verify-key-current-toolchain-refused"
    let started = DateTimeOffset.UtcNow.ToString("O")
    let key = Environment.GetEnvironmentVariable("NUGET_API_KEY")
    if String.IsNullOrWhiteSpace key then invalidArg "credential" "verify-key-credential-unavailable"
    match inspect (httpTransport key) ids baseline with
    | Error _ -> invalidArg "roster" "verify-key-roster-refused"
    | Ok(observations,passed) ->
        let receipt = {| schema="fsgg.rendering.nuget-verify-key/v1"; producerSha=(if scopeOnly then null else producer); sourceSha=scopeSource; expectedExecutorSha=expected; workflowSha=current "GITHUB_WORKFLOW_SHA"; executorSha=Environment.GetEnvironmentVariable("GITHUB_SHA"); executorRef=Environment.GetEnvironmentVariable("GITHUB_WORKFLOW_REF"); runId=Environment.GetEnvironmentVariable("GITHUB_RUN_ID"); attempt=Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"); planSha256=planHash; rosterSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(String.Join("\n",ids)))).ToLowerInvariant(); baseline=baseline; startedUtc=started; endedUtc=DateTimeOffset.UtcNow.ToString("O"); observations=(observations |> List.map(fun row -> {|packageId=row.PackageId; httpStatus=(match row.HttpStatus with Some n -> Nullable n | None -> Nullable()); outcome=row.Outcome|})); passed=passed; noPackageVersionMutation=true |}
        let output = value "--receipt"
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
        File.WriteAllText(output,JsonSerializer.Serialize(receipt,JsonSerializerOptions(WriteIndented=true))+"\n")
        printfn "NuGet existing-ID verify-key: %d/19 HTTP200; no package mutation" (observations |> List.filter(fun row -> row.HttpStatus=Some 200) |> List.length)
        if not passed then exit 3
        if scopeOnly then exit 0
        let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
        if String.IsNullOrWhiteSpace token then invalidArg "credential" "census-credential-unavailable"
        match Rendering.ReleasePackageCensus.inspectWith Rendering.ReleasePackageCensus.DefaultLimits (Rendering.ReleasePackageCensus.httpTransport token) ids version with
        | Error _ -> invalidArg "census" "census-unavailable"
        | Ok census ->
            let censusOutput = value "--census"
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath censusOutput)) |> ignore
            File.WriteAllText(censusOutput,Rendering.ReleasePackageCensus.receiptJson producer (Environment.GetEnvironmentVariable("GITHUB_SHA")) (Environment.GetEnvironmentVariable("GITHUB_RUN_ID")) version planHash census)
            use qualified = JsonDocument.Parse(File.ReadAllBytes(value "--qualification"))
            let proof = qualified.RootElement
            let flag (key: string) = proof.GetProperty(key).GetBoolean()
            let number (key: string) = proof.GetProperty(key).GetInt32()
            let facts : Facts = { OriginalCustodyVerified=flag "originalCustodyVerified"; NativeTemplatesQualified=flag "nativeTemplatesQualified"; ProviderPassed=number "providerPassed"; BrowserPassed=proof.GetProperty("browserPassed").EnumerateArray() |> Seq.map(fun n -> n.GetInt32()) |> Seq.toList; BrowserUnexpected=number "browserUnexpected"; BrowserSkipped=number "browserSkipped"; BrowserFlaky=number "browserFlaky"; CompleteReadCensus=census.Organization.Complete && census.Packages.Length=19 && (census.Packages |> List.forall(fun (_,row) -> row.Complete)); NuGetScopeStatuses=observations |> List.map(fun row -> row.HttpStatus); GitHubEffectiveWrite=proof.GetProperty("githubEffectiveWrite").GetString() }
            let eligible =
                match currentJourney with
                | Some journey -> admitCurrent journey facts.CompleteReadCensus facts.NuGetScopeStatuses facts.GitHubEffectiveWrite
                | None -> admit facts
            if not eligible then invalidArg "eligibility" "publication-attempt-ineligible"
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
