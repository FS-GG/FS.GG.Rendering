#load "ReleasePackageCensus.fs"
open System
open System.IO
open System.Diagnostics
open System.Text.Json
open System.Security.Cryptography
open System.Text.RegularExpressions
open Rendering.ReleasePackageCensus

let fail reason = raise (InvalidDataException reason)
let args = fsi.CommandLineArgs |> Array.skip 1
let value name =
    let indexes = args |> Array.indexed |> Array.choose (fun (i,s) -> if s=name then Some i else None)
    if indexes.Length <> 1 || indexes.[0]+1 >= args.Length then fail "census-arguments-refused"
    args.[indexes.[0]+1]
let git arguments =
    let start = ProcessStartInfo("git", RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false)
    arguments |> List.iter start.ArgumentList.Add
    use child = Process.Start start
    let output = child.StandardOutput.ReadToEnd()
    child.WaitForExit()
    if child.ExitCode <> 0 then fail "census-source-refused"
    output
let parse bytes = match parseJson bytes with Ok doc -> doc | Error reason -> fail reason
let field (doc: JsonElement) (key:string) = doc.GetProperty(key).GetString()
let strings (doc: JsonElement) (key:string) = doc.GetProperty(key).EnumerateArray() |> Seq.map (fun x -> x.GetString()) |> Seq.toList
try
    if args.Length <> 6 || (args |> Array.mapi (fun i x -> if i%2=0 then x else "") |> Array.filter ((<>) "") |> Set.ofArray) <> set ["--preflight-receipt";"--plan";"--receipt"] then fail "census-arguments-refused"
    let priorBytes = File.ReadAllBytes(value "--preflight-receipt")
    use prior = parse priorBytes
    let root = prior.RootElement
    let source, workflow, version = field root "sourceSha", field root "workflowSha", field root "version"
    let env name = Environment.GetEnvironmentVariable name
    if env "GITHUB_REPOSITORY" <> Repository || workflow <> env "GITHUB_SHA" || workflow <> (git ["rev-parse";"HEAD"]).Trim() || not (Regex.IsMatch(source,"^[0-9a-f]{40}$")) || field root "result" <> "pass" then fail "census-context-refused"
    let workflowRef = env "GITHUB_WORKFLOW_REF"
    if workflowRef <> Repository+"/.github/workflows/release.yml@refs/heads/main" && not (Regex.IsMatch(workflowRef, "^FS-GG/FS\\.GG\\.Rendering/\\.github/workflows/release\\.yml@refs/tags/v[^/]+$")) then fail "census-workflow-refused"
    let previousWorkflow = root.GetProperty("workflow")
    if field previousWorkflow "repository" <> Repository || field previousWorkflow "ref" <> workflowRef || field previousWorkflow "runId" <> env "GITHUB_RUN_ID" then fail "census-prior-context-refused"
    let planPath = value "--plan"
    if planPath <> "eng/release/svg-external-authority-0.32.0.json" then fail "census-plan-path-refused"
    let planBytes = System.Text.Encoding.UTF8.GetBytes(git ["show"; source+":"+planPath])
    use plan = parse planBytes
    if field plan.RootElement "version" <> version then fail "census-version-refused"
    let packages = plan.RootElement.GetProperty("packages").EnumerateArray() |> Seq.map (fun p -> field p "id") |> Seq.toList
    if packages <> strings root "roster" || root.GetProperty("rosterCount").GetInt32() <> 19 then fail "census-plan-roster-refused"
    let token = env "GITHUB_TOKEN"
    if String.IsNullOrWhiteSpace token then fail "census-credential-unavailable"
    match inspectWith DefaultLimits (httpTransport token) packages version with
    | Error reason -> fail reason
    | Ok census ->
        let output = value "--receipt"
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath output)) |> ignore
        File.WriteAllText(output, receiptJson source workflow (env "GITHUB_RUN_ID") version (Convert.ToHexString(SHA256.HashData planBytes).ToLowerInvariant()) census)
        printfn "Rendering read census: %A; no mutation; writer grants UNKNOWN" census.Overall
        if census.Overall <> Absent then exit 3
with _ ->
    // Never print exception payloads from credential-bearing HTTP or environment operations.
    eprintfn "Rendering read census: UNKNOWN; input/context/transport refused"
    exit 3
