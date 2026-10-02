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
assert(inspect (response(Some 200)) ids "0.32.0" |> Result.isError)
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
type RecordingHandler(status: HttpStatusCode) =
    inherit HttpMessageHandler()
    let handle (request: HttpRequestMessage) =
        assert(request.Method=HttpMethod.Get)
        assert(request.RequestUri.Host="www.nuget.org")
        assert(request.RequestUri.AbsolutePath.EndsWith("/0.31.0"))
        requests.Add(request.RequestUri.AbsolutePath,request.Headers.GetValues("X-NuGet-ApiKey") |> Seq.exactlyOne)
        new HttpResponseMessage(status)
    override _.Send(request: HttpRequestMessage, _: CancellationToken) = handle request
    override _.SendAsync(request: HttpRequestMessage, _: CancellationToken) = Task.FromResult(handle request)
let liveFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.OK))
assert(match inspect liveFixture ids "0.31.0" with Ok(_,true)->true|_->false)
assert(requests.Count=19 && (requests |> Seq.forall(fun (_,key) -> key="public-test-fixture-key")))
let redirectFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.Redirect))
assert(match inspect redirectFixture ids "0.31.0" with Ok(_,false)->true|_->false)
assert(requests.Count=38)
let wrongIdentity _ _ = {PackageId="other";HttpStatus=Some 200;Outcome="authorized"}
assert(match inspect wrongIdentity ids "0.31.0" with Ok(_,false)->true|_->false)
let unavailable _ _ = failwith "must-not-be-logged"
assert(match inspect unavailable ids "0.31.0" with Ok(_,false)->true|_->false)
printfn "PASS exact19 header-only GET transport fixture; redirects/identity/unavailability refuse"
