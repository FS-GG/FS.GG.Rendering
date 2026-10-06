#load "NuGetVerifyKey.fs"
#load "AttemptEligibility.fs"
open Rendering.NuGetVerifyKey
open Rendering.AttemptEligibility
let ids = [1..19] |> List.map(fun n -> "FS.GG.UI.Package"+string n)
let response status id _ = {PackageId=id; HttpStatus=status; Outcome=if status=Some 200 then "authorized" else "authorization-unproven"}
let good = inspect (response (Some 200)) ids "0.31.0"
assert(match good with Ok(rows,true) -> rows.Length=19 | _ -> false)
for status in [Some 401;Some 403;Some 404;Some 302;Some 500;None] do
    assert(match inspect (response status) ids "0.31.0" with Ok(_,false)->true|_->false)
assert(inspect (response(Some 200)) (List.tail ids) "0.31.0" |> Result.isError)
assert(match inspect (response(Some 200)) ids "0.32.0" with Ok(rows,true) -> rows.Length=19 | _ -> false)
for refused in ["0.30.0"; "0.32.1"; "0.32.x"; ""] do
    assert(inspect (response(Some 200)) ids refused |> Result.isError)
let facts = {OriginalCustodyVerified=true;NativeTemplatesQualified=true;ProviderPassed=180;BrowserPassed=[4;4;4];BrowserUnexpected=0;BrowserSkipped=0;BrowserFlaky=0;CompleteReadCensus=true;NuGetScopeStatuses=List.replicate 19 (Some 200);GitHubEffectiveWrite="unknown-before-attempt"}
assert(admit facts)
for bad in [{facts with OriginalCustodyVerified=false};{facts with NativeTemplatesQualified=false};{facts with ProviderPassed=179};{facts with BrowserPassed=[4;4]};{facts with BrowserSkipped=1};{facts with BrowserFlaky=1};{facts with CompleteReadCensus=false};{facts with NuGetScopeStatuses=List.replicate 19 (Some 403)};{facts with GitHubEffectiveWrite="proven"}] do assert(not(admit bad))
printfn "PASS bounded19 existing-ID scope and truthful attempt eligibility refusals"

open System
open System.Net
open System.Net.Http
open System.Threading
open System.Threading.Tasks
let requests = ResizeArray<string * string>()
type RecordingHandler(status: HttpStatusCode, baseline: string) =
    inherit HttpMessageHandler()
    let handle (request: HttpRequestMessage) =
        assert(request.Method=HttpMethod.Get)
        assert(request.RequestUri.Host="www.nuget.org")
        assert(request.RequestUri.AbsolutePath.EndsWith("/"+baseline))
        requests.Add(request.RequestUri.AbsolutePath,request.Headers.GetValues("X-NuGet-ApiKey") |> Seq.exactlyOne)
        new HttpResponseMessage(status)
    override _.Send(request: HttpRequestMessage, _: CancellationToken) = handle request
    override _.SendAsync(request: HttpRequestMessage, _: CancellationToken) = Task.FromResult(handle request)
let liveFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.OK, "0.31.0"))
assert(match inspect liveFixture ids "0.31.0" with Ok(_,true)->true|_->false)
assert(requests.Count=19 && (requests |> Seq.forall(fun (_,key) -> key="public-test-fixture-key")))
let redirectFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.Redirect, "0.31.0"))
assert(match inspect redirectFixture ids "0.31.0" with Ok(_,false)->true|_->false)
assert(requests.Count=38)
let wrongIdentity _ _ = {PackageId="other";HttpStatus=Some 200;Outcome="authorized"}
assert(match inspect wrongIdentity ids "0.31.0" with Ok(_,false)->true|_->false)
let unavailable _ _ = failwith "must-not-be-logged"
assert(match inspect unavailable ids "0.31.0" with Ok(_,false)->true|_->false)
printfn "PASS exact19 header-only GET transport fixture; redirects/identity/unavailability refuse"

let successorRequestsBefore = requests.Count
let successorFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.OK, "0.32.0"))
assert(match inspect successorFixture ids "0.32.0" with Ok(rows,true)->rows.Length=19|_->false)
assert(requests.Count=successorRequestsBefore+19)
printfn "PASS successor baseline existing-ID GETs; no publication admission"

// Pure successor provenance joins. These observations never establish package custody.
open System.Text.Json
open System.Text.Json.Nodes
let context = { SourceSha=String.replicate 40 "a"; WorkflowSha=String.replicate 40 "b";
                WorkflowRef="FS-GG/FS.GG.Rendering/.github/workflows/release.yml@refs/heads/main";
                Repository="FS-GG/FS.GG.Rendering"; RunId="123";
                PlanHash="ac433609d14d0a672ad42e5f3578afdd6ecf6ee1144c33d5e6aec951535d84f2"; PackageIds=ids }
let receiptText = JsonSerializer.Serialize {| schema="fsgg.rendering.release-preflight/v1"; result="pass"; mutation="none";
    sourceSha=context.SourceSha; workflowSha=context.WorkflowSha; version="0.32.1"; frameworkVersion="0.32.1";
    templateVersion="0.32.1"; planSha256=context.PlanHash; rosterCount=19; roster=ids;
    workflow={| repository=context.Repository; ref=context.WorkflowRef; runId=context.RunId |} |}
let observe text =
    use document = JsonDocument.Parse(text: string)
    scopeSourceFromReceipt context document.RootElement
assert(observe receiptText = Ok context.SourceSha)
for field, replacement in ["schema","other"; "result","unknown"; "mutation","write";
                           "sourceSha",String.replicate 40 "c"; "workflowSha",String.replicate 40 "c";
                           "version","0.32.0"; "frameworkVersion","0.32.0"; "templateVersion","0.32.0";
                           "planSha256",String.replicate 64 "c"] do
    let changed = JsonNode.Parse(receiptText)
    changed.[field] <- JsonValue.Create(replacement)
    assert(observe (changed.ToJsonString()) |> Result.isError)
for field in ["repository"; "ref"; "runId"] do
    let changed = JsonNode.Parse(receiptText)
    changed.["workflow"].[field] <- JsonValue.Create("wrong")
    assert(observe (changed.ToJsonString()) |> Result.isError)
let missingRoster = JsonNode.Parse(receiptText)
missingRoster.AsObject().Remove("roster") |> ignore
assert(observe (missingRoster.ToJsonString()) |> Result.isError)
for badContext in [{context with SourceSha="main"}; {context with WorkflowSha="main"};
                   {context with Repository="other/repo"}; {context with WorkflowRef="other"};
                   {context with RunId=""}; {context with PlanHash=String.replicate 64 "d"};
                   {context with PackageIds=List.rev ids}] do
    use document = JsonDocument.Parse(receiptText)
    assert(scopeSourceFromReceipt badContext document.RootElement |> Result.isError)
printfn "PASS successor same-run scope provenance and refusal controls; no native authority"
