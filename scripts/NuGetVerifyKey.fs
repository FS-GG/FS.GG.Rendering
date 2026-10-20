module Rendering.NuGetVerifyKey

open System
open System.Net.Http
open System.Threading
open System.Text.Json
open System.Text.RegularExpressions

/// Stateless bounded observations; HTTP200 is the only successful authorization observation.
type Observation = { PackageId: string; HttpStatus: int option; Outcome: string }
type Transport = string -> string -> Observation

let inspect (transport: Transport) (ids: string list) (baseline: string) =
    if ids.Length <> 19 || (ids |> List.map (fun id -> id.ToLowerInvariant()) |> Set.ofList |> Set.count) <> 19 || (baseline <> "0.31.0" && baseline <> "0.32.0" && baseline <> "0.32.1") then
        Error "verify-key-roster-or-baseline-refused"
    else
        let observations = ids |> List.map (fun id ->
            try transport id baseline
            with _ -> {PackageId=id; HttpStatus=None; Outcome="transport-unavailable"})
        let passed = List.forall2 (fun id item -> item.PackageId=id && item.HttpStatus=Some 200 && item.Outcome="authorized") ids observations
        Ok (observations, passed)

/// Same-run source provenance for successor scope-only observations, never publication authority.
type ScopeContext = {
    SourceSha: string; WorkflowSha: string; WorkflowRef: string; Repository: string
    RunId: string; PlanHash: string; PackageIds: string list
}

/// Closed raw-plan identities for nonmutating same-run scope observations.
let scopePlanVersion = function
    | "ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2" -> Some "0.32.1"
    | "81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a" -> Some "0.32.2"
    | "89369de0c0944c66a52c6ed914cef78d6bb7670866baedf85cd8a198c5c46239" -> Some "0.32.3"
    | _ -> None

/// This new subject grants scope-only GET observations, never writer eligibility.
let fontDisposalScopePlan scopeOnly planHash =
    scopeOnly && scopePlanVersion planHash = Some "0.32.3"

let scopeSourceFromReceipt (context: ScopeContext) (receipt: JsonElement) =
    try
        let text (element: JsonElement) (name: string) = element.GetProperty(name).GetString()
        let selectedVersion = scopePlanVersion context.PlanHash |> Option.defaultValue null
        let workflow = receipt.GetProperty("workflow")
        let roster = receipt.GetProperty("roster").EnumerateArray() |> Seq.map(fun p -> p.GetString()) |> Seq.toList
        if isNull context.SourceSha || not(Regex.IsMatch(context.SourceSha, "^[0-9a-f]{40}$")) ||
           isNull context.WorkflowSha || not(Regex.IsMatch(context.WorkflowSha, "^[0-9a-f]{40}$")) ||
           context.Repository<>"FS-GG/FS.GG.Rendering" ||
           context.WorkflowRef<>"FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main" ||
           String.IsNullOrWhiteSpace(context.RunId) ||
           isNull selectedVersion ||
           text receipt "schema"<>"fsgg.rendering.release-preflight/v1" ||
           text receipt "result"<>"pass" || text receipt "mutation"<>"none" ||
           text receipt "sourceSha"<>context.SourceSha || text receipt "workflowSha"<>context.WorkflowSha ||
           text receipt "version"<>selectedVersion || text receipt "frameworkVersion"<>selectedVersion ||
           text receipt "templateVersion"<>selectedVersion || text receipt "planSha256"<>context.PlanHash ||
           text workflow "repository"<>context.Repository || text workflow "ref"<>context.WorkflowRef ||
           text workflow "runId"<>context.RunId || receipt.GetProperty("rosterCount").GetInt32()<>19 ||
           context.PackageIds.Length<>19 || roster<>context.PackageIds then
            Error "verify-key-scope-source-refused"
        else Ok context.SourceSha
    with _ -> Error "verify-key-scope-source-refused"

let httpTransportWithHandler (key: string) (handler: HttpMessageHandler) : Transport =
    let client = new HttpClient(handler, true)
    client.Timeout <- TimeSpan.FromSeconds 10.
    fun id baseline ->
        try
            use request = new HttpRequestMessage(HttpMethod.Get, "https://www.nuget.org/api/v2/verifykey/" + Uri.EscapeDataString(id) + "/" + baseline)
            if String.IsNullOrWhiteSpace key || not (request.Headers.TryAddWithoutValidation("X-NuGet-ApiKey", key)) then
                { PackageId=id; HttpStatus=None; Outcome="credential-unavailable" }
            else
                use response = client.Send(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
                let status = int response.StatusCode
                { PackageId=id; HttpStatus=Some status; Outcome=if status=200 then "authorized" else "authorization-unproven" }
        with _ -> { PackageId=id; HttpStatus=None; Outcome="transport-unavailable" }

let httpTransport (key: string) : Transport =
    // Header remains within original host; no redirects or response bodies are consumed.
    httpTransportWithHandler key (new HttpClientHandler(AllowAutoRedirect = false))
