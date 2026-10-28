module Rendering.NuGetVerifyKey

open System
open System.Net.Http
open System.Threading
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

/// Stateless bounded observations; HTTP200 is the only successful authorization observation.
type Observation = { PackageId: string; HttpStatus: int option; Outcome: string }
type Transport = string -> string -> Observation

let inspect (transport: Transport) (ids: string list) (baseline: string) =
    if ids.Length <> 19 || (ids |> List.map (fun id -> id.ToLowerInvariant()) |> Set.ofList |> Set.count) <> 19 || (baseline <> "0.31.0" && baseline <> "0.32.0" && baseline <> "0.32.1" && baseline <> "0.32.3") then
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
    | "4bcbb1c756a799ab634e14d1fc9191ac51a8cd5d3537aec75eeab1c1d545deaa" -> Some "0.32.4"
    | _ -> None

/// This new subject grants scope-only GET observations, never writer eligibility.
let fontDisposalScopePlan scopeOnly planHash =
    scopeOnly && scopePlanVersion planHash = Some "0.32.3"

let scopeSourceFromReceipt (context: ScopeContext) (receipt: JsonElement) =
    try
        let asText (element: JsonElement) = element.GetString() |> Option.ofObj |> Option.defaultWith (fun () -> invalidArg "receipt" "null scope text")
        let text (element: JsonElement) (name: string) = asText (element.GetProperty(name))
        let selectedVersion = scopePlanVersion context.PlanHash |> Option.defaultWith (fun () -> invalidArg "plan" "unknown scope plan")
        let workflow = receipt.GetProperty("workflow")
        let roster = receipt.GetProperty("roster").EnumerateArray() |> Seq.map asText |> Seq.toList
        if String.IsNullOrEmpty(context.SourceSha) || not(Regex.IsMatch(context.SourceSha, "^[0-9a-f]{40}$")) ||
           String.IsNullOrEmpty(context.WorkflowSha) || not(Regex.IsMatch(context.WorkflowSha, "^[0-9a-f]{40}$")) ||
           context.Repository<>"FS-GG/FS.GG.Rendering" ||
           context.WorkflowRef<>"FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main" ||
           String.IsNullOrWhiteSpace(context.RunId) ||
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

// Exact English primary-source text at NuGetGalleryc25bab0, ServicesStrings.resx.
// This diagnostic never turns a rejected response into scope or writer eligibility.
// Only closed tags can leave the parser; arbitrary header/body text never does.
type private Media = Missing | Text | Html | Json | Other
type private EncodingKind = Unspecified | Utf8 | OtherEncoding | Encoded
type private Size = Unknown | Bounded | DeclaredOversize | StreamOversize
type private ReadDisposition = Empty | Unmatched | Reflection | NonUtf8 | Timeout | ReadFailure | NotRead | LimitReached

type private RefusalDiagnostic = VerificationScopePolicy | Unclassified of Media * EncodingKind * Size * ReadDisposition

let effectiveAccountAccepted account = String.Equals(account, "Paradigma11", StringComparison.Ordinal)

let private diagnosticOutcome = function
    | VerificationScopePolicy -> "security-policy-verification-scope-required"
    | Unclassified(media, encoding, size, disposition) ->
        let m = match media with Missing->"missing" | Text->"text" | Html->"html" | Json->"json" | Other->"other"
        let e = match encoding with Unspecified->"unspecified" | Utf8->"utf8" | OtherEncoding->"other" | Encoded->"encoded"
        let s = match size with Unknown->"unknown" | Bounded->"bounded" | DeclaredOversize->"declared-oversize" | StreamOversize->"stream-oversize"
        let r = match disposition with Empty->"empty" | Unmatched->"unmatched" | Reflection->"reflection" | NonUtf8->"non-utf8" | Timeout->"timeout" | ReadFailure->"read-failure" | NotRead->"not-read" | LimitReached->"limit-reached"
        "authorization-unproven:m-"+m+":e-"+e+":s-"+s+":r-"+r

let private refusalDiagnostic (key: string) (response: HttpResponseMessage) (cancellation: CancellationToken) =
    let mutable media = Missing
    let mutable encoding = Unspecified
    let mutable size = Unknown
    let unclassified disposition = Unclassified(media, encoding, size, disposition)
    try
        match response.Content.Headers.ContentType |> Option.ofObj with
        | None -> ()
        | Some contentType ->
            media <- match Option.ofObj contentType.MediaType |> Option.map(fun value -> value.ToLowerInvariant()) with
                     | Some "text/plain" -> Text | Some "text/html" -> Html | Some "application/json" -> Json | _ -> Other
            encoding <- match Option.ofObj contentType.CharSet |> Option.map(fun value -> value.Trim('"').ToLowerInvariant()) with
                        | None | Some "" -> Unspecified | Some "utf-8" -> Utf8 | _ -> OtherEncoding
        if response.Content.Headers.ContentEncoding.Count>0 then encoding <- Encoded
        let limit = 4096
        let length = response.Content.Headers.ContentLength
        if length.HasValue && (length.Value < 0L || length.Value > int64 limit) then
            size <- DeclaredOversize
            unclassified NotRead
        else
            use stream = response.Content.ReadAsStreamAsync(cancellation).WaitAsync(cancellation).GetAwaiter().GetResult()
            let bytes = Array.zeroCreate<byte> (limit + 1)
            let mutable count = 0
            let mutable ended = false
            while not ended && count < bytes.Length do
                let received = stream.ReadAsync(bytes.AsMemory(count, bytes.Length - count), cancellation).AsTask().WaitAsync(cancellation).GetAwaiter().GetResult()
                if received = 0 then ended <- true else count <- count + received
            if not ended || count > limit then
                size <- StreamOversize
                unclassified LimitReached
            else
                size <- Bounded
                if count=0 then unclassified Empty
                else
                    let body = UTF8Encoding(false, true).GetString(bytes, 0, count)
                    let policy = "A package verification key is required to push symbols. Please contact support@nuget.org to get more details."
                    if body.Contains(key, StringComparison.Ordinal) then unclassified Reflection
                    elif body.Trim([| ' '; '\t'; '\r'; '\n' |]) = policy then VerificationScopePolicy
                    else unclassified Unmatched
    with
    | :? DecoderFallbackException -> unclassified NonUtf8
    | :? OperationCanceledException -> unclassified Timeout
    | _ -> unclassified ReadFailure

let httpTransportWithHandler (key: string) (handler: HttpMessageHandler) : Transport =
    let client = new HttpClient(handler, true)
    client.Timeout <- TimeSpan.FromSeconds 10.
    fun id baseline ->
        try
            use request = new HttpRequestMessage(HttpMethod.Get, "https://www.nuget.org/api/v2/verifykey/" + Uri.EscapeDataString(id) + "/" + baseline)
            if String.IsNullOrWhiteSpace key || not (request.Headers.TryAddWithoutValidation("X-NuGet-ApiKey", key)) then
                { PackageId=id; HttpStatus=None; Outcome="credential-unavailable" }
            else
                use deadline = new CancellationTokenSource(TimeSpan.FromSeconds 10.)
                use response = client.Send(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token)
                let status = int response.StatusCode
                let outcome =
                    if status = 200 then "authorized"
                    elif status = 400 then
                        refusalDiagnostic key response deadline.Token |> diagnosticOutcome
                    else "authorization-unproven"
                { PackageId=id; HttpStatus=Some status; Outcome=outcome }
        with _ -> { PackageId=id; HttpStatus=None; Outcome="transport-unavailable" }

let httpTransport (key: string) : Transport =
    // Header remains within the original host; redirects are refused. Only400 gets a bounded diagnostic body read; no body is persisted.
    // Do not silently drain an oversized/unread response after the diagnostic cap.
    httpTransportWithHandler key (new SocketsHttpHandler(AllowAutoRedirect = false, MaxResponseDrainSize = 0))
