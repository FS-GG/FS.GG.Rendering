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
for refused in ["0.30.0"; "0.32.2"; "0.32.x"; ""] do
    assert(inspect (response(Some 200)) ids refused |> Result.isError)
let facts : Facts = {OriginalCustodyVerified=true;NativeTemplatesQualified=true;ProviderPassed=180;BrowserPassed=[4;4;4];BrowserUnexpected=0;BrowserSkipped=0;BrowserFlaky=0;CompleteReadCensus=true;NuGetScopeStatuses=List.replicate 19 (Some 200);GitHubEffectiveWrite="unknown-before-attempt"}
assert(admit facts)
for bad in [{facts with OriginalCustodyVerified=false};{facts with NativeTemplatesQualified=false};{facts with ProviderPassed=179};{facts with BrowserPassed=[4;4]};{facts with BrowserSkipped=1};{facts with BrowserFlaky=1};{facts with CompleteReadCensus=false};{facts with NuGetScopeStatuses=List.replicate 18 (Some 200)};{facts with GitHubEffectiveWrite="proven"}] do assert(not(admit bad))
// verifykey is advisory (FS-GG/.github#4817): any fully observed nineteen statuses admit; length stays hard.
for statuses in [List.replicate 19 (Some 400); List.replicate 19 (Some 403); List.replicate 19 None; Some 200 :: List.replicate 18 (Some 400)] do
    assert(admit {facts with NuGetScopeStatuses=statuses})
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
printfn "PASS exact19 GET transport fixture; redirects/identity/unavailability refuse"

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

// The current recovery plan joins only its exact same-run, no-mutation source receipt.
let recoveryContext = { context with PlanHash="81571fc65ac117e90467428d2e50e103ec875aa33cb83b054d9056bf4129aa1a" }
let recoveryReceipt = JsonNode.Parse(receiptText)
for field in ["version"; "frameworkVersion"; "templateVersion"] do
    recoveryReceipt.[field] <- JsonValue.Create("0.32.2")
recoveryReceipt.["planSha256"] <- JsonValue.Create(recoveryContext.PlanHash)
let observeRecovery text =
    use document = JsonDocument.Parse(text: string)
    scopeSourceFromReceipt recoveryContext document.RootElement
assert(observeRecovery (recoveryReceipt.ToJsonString()) = Ok recoveryContext.SourceSha)
for field, replacement in ["version","0.32.1"; "frameworkVersion","0.32.1"; "templateVersion","0.32.1";
                           "mutation","write"; "sourceSha",String.replicate 40 "c";
                           "workflowSha",String.replicate 40 "c"; "planSha256",context.PlanHash] do
    let changed = JsonNode.Parse(recoveryReceipt.ToJsonString())
    changed.[field] <- JsonValue.Create(replacement)
    assert(observeRecovery (changed.ToJsonString()) |> Result.isError)
assert(observeRecovery receiptText |> Result.isError)
assert(observe (recoveryReceipt.ToJsonString()) |> Result.isError)
let recoveryRequestsBefore = requests.Count
let recoveryFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.OK, "0.32.1"))
assert(match inspect recoveryFixture ids "0.32.1" with Ok(rows,true)->rows.Length=19|_->false)
assert(requests.Count=recoveryRequestsBefore+19)
printfn "PASS recovery exact-plan version/source joins and published-baseline19 GETs; no publication authority"

let current : CurrentJourney = {OriginalCustodyVerified=true;NativeTemplatesQualified=true;ProviderPassed=186;BrowserPassed=[8;8;8];BrowserUnexpected=0;BrowserSkipped=0;BrowserFlaky=0;AtPassed=15;AtCustodyQualified=true}
assert(currentJourneyQualified current)
assert(admitCurrent current true (List.replicate 19 (Some 200)) "unknown-before-attempt")
for bad in [{current with OriginalCustodyVerified=false};{current with NativeTemplatesQualified=false};{current with ProviderPassed=180};{current with BrowserPassed=[4;4;4]};{current with BrowserUnexpected=1};{current with BrowserSkipped=1};{current with BrowserFlaky=1};{current with AtPassed=14};{current with AtCustodyQualified=false}] do
    assert(not(currentJourneyQualified bad))
    assert(not(admitCurrent bad true (List.replicate 19 (Some 200)) "unknown-before-attempt"))
assert(not(admitCurrent current false (List.replicate 19 (Some 200)) "unknown-before-attempt"))
assert(not(admitCurrent current true (List.replicate 18 (Some 200)) "unknown-before-attempt"))
assert(admitCurrent current true (List.replicate 19 (Some 403)) "unknown-before-attempt")
assert(admitCurrent current true (List.replicate 19 (Some 400)) "unknown-before-attempt")
assert(not(admitCurrent current true (List.replicate 20 (Some 400)) "unknown-before-attempt"))
assert(not(admitCurrent current false (List.replicate 19 (Some 400)) "unknown-before-attempt"))
assert(not(admitCurrent current true (List.replicate 19 (Some 200)) "proven"))
printfn "PASS separate current186/8/15 eligibility and historical180/4 contract"


// Genuine source-plan bytes are raw, including the final newline, not source_text.Trim().
let fontPlanBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../eng/release/font-disposal-0.32.3.json"))
let fontRawHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fontPlanBytes)).ToLowerInvariant()
let fontStrippedBytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(fontPlanBytes).Trim())
let fontStrippedHash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fontStrippedBytes)).ToLowerInvariant()
assert(fontPlanBytes.Length=3490 && fontRawHash="89369de0c0944c66a52c6ed914cef78d6bb7670866baedf85cd8a198c5c46239")
assert(fontStrippedHash="d0127bcd424480549f9de233eb2b2109768d00491d529a08e65a9c980bebe27d")
assert(scopePlanVersion fontRawHash=Some "0.32.3")
assert(scopePlanVersion fontStrippedHash=None)
assert(fontDisposalScopePlan true fontRawHash)
assert(not(fontDisposalScopePlan false fontRawHash))
for hash in [fontStrippedHash; context.PlanHash; recoveryContext.PlanHash; String.replicate 64 "c"; ""] do
    assert(not(fontDisposalScopePlan true hash))
let fontContext = { context with SourceSha="4ec6eafbbbf02264db9b0db4780c590abe0bf9cd"; PlanHash=fontRawHash }
let fontReceipt = JsonNode.Parse(receiptText)
for field in ["version"; "frameworkVersion"; "templateVersion"] do
    fontReceipt.[field] <- JsonValue.Create("0.32.3")
fontReceipt.["sourceSha"] <- JsonValue.Create(fontContext.SourceSha)
fontReceipt.["planSha256"] <- JsonValue.Create(fontRawHash)
let observeFont text =
    use document = JsonDocument.Parse(text: string)
    scopeSourceFromReceipt fontContext document.RootElement
let beforeFontJoins = requests.Count
assert(observeFont (fontReceipt.ToJsonString())=Ok fontContext.SourceSha)
for field, replacement in ["sourceSha",context.SourceSha; "workflowSha",String.replicate 40 "c";
                           "planSha256",fontStrippedHash; "planSha256",recoveryContext.PlanHash;
                           "version","0.32.2"; "frameworkVersion","0.32.2"; "templateVersion","0.32.2";
                           "result","unknown"; "mutation","write"] do
    let changed = JsonNode.Parse(fontReceipt.ToJsonString())
    changed.[field] <- JsonValue.Create(replacement)
    assert(observeFont (changed.ToJsonString()) |> Result.isError)
for badContext in [{fontContext with SourceSha=context.SourceSha};
                   {fontContext with WorkflowSha=String.replicate 40 "c"};
                   {fontContext with PlanHash=fontStrippedHash};
                   {fontContext with PackageIds=List.rev ids}] do
    use document = JsonDocument.Parse(fontReceipt.ToJsonString())
    assert(scopeSourceFromReceipt badContext document.RootElement |> Result.isError)
let wrongFontRoster = JsonNode.Parse(fontReceipt.ToJsonString())
wrongFontRoster.["roster"] <- JsonSerializer.SerializeToNode(List.rev ids)
assert(observeFont (wrongFontRoster.ToJsonString()) |> Result.isError)
assert(requests.Count=beforeFontJoins) // Provenance/readiness checks perform no transport.
let fontFixture = httpTransportWithHandler "public-test-fixture-key" (new RecordingHandler(HttpStatusCode.OK, "0.32.1"))
assert(match inspect fontFixture ids "0.32.1" with Ok(rows,true)->rows.Length=19|_->false)
assert(requests.Count=beforeFontJoins+19)
printfn "PASS font-disposal RAW-plan scope-only selection/source/executor/roster joins;19 GETs and no writer authority"

// New writer contract is distinct from both historical receiver populations.
let fontWriter : FontDisposalJourney = {
    OriginalCustodyVerified=true; NativeTemplatesQualified=true; ProviderPassed=186
    BrowserPassed=[15;15;15]; BrowserUnexpected=0; BrowserSkipped=0; BrowserFlaky=0
    AtPassed=15; AtSpeechWindows=4; AtEvidenceMembers=27; AtCustodyQualified=true }
assert(fontDisposalJourneyQualified fontWriter)
assert(admitFontDisposal fontWriter true (List.replicate 19 (Some 200)) "unknown-before-attempt")
for bad in [{fontWriter with OriginalCustodyVerified=false}; {fontWriter with NativeTemplatesQualified=false};
            {fontWriter with ProviderPassed=180}; {fontWriter with BrowserPassed=[8;8;8]};
            {fontWriter with BrowserPassed=[15;15;14]}; {fontWriter with BrowserUnexpected=1};
            {fontWriter with BrowserSkipped=1}; {fontWriter with BrowserFlaky=1};
            {fontWriter with AtPassed=14}; {fontWriter with AtSpeechWindows=3};
            {fontWriter with AtEvidenceMembers=26}; {fontWriter with AtCustodyQualified=false}] do
    assert(not(fontDisposalJourneyQualified bad))
    assert(not(admitFontDisposal bad true (List.replicate 19 (Some 200)) "unknown-before-attempt"))
assert(not(admitFontDisposal fontWriter false (List.replicate 19 (Some 200)) "unknown-before-attempt"))
assert(not(admitFontDisposal fontWriter true (List.replicate 18 (Some 200)) "unknown-before-attempt"))
assert(admitFontDisposal fontWriter true (List.replicate 19 (Some 403)) "unknown-before-attempt")
assert(admitFontDisposal fontWriter true (List.replicate 19 (Some 400)) "unknown-before-attempt")
assert(not(admitFontDisposal fontWriter true (List.replicate 18 (Some 400)) "unknown-before-attempt"))
assert(not(admitFontDisposal fontWriter true (List.replicate 19 (Some 400)) "proven"))
assert(not(admitFontDisposal fontWriter true (List.replicate 19 (Some 200)) "proven"))
printfn "PASS separate font-disposal186/45/15/4/27 writer eligibility; historical contracts unchanged"


// Same-response diagnostic controls. Public fixture values are never credentials.
open System.IO
open System.Text
let verificationPolicy = "A package verification key is required to push symbols. Please contact support@nuget.org to get more details."
let diagnosticFixtureKey = "public-diagnostic-fixture-key"
type DiagnosticHandler(status: HttpStatusCode, content: unit -> HttpContent, headerDelay: int) =
    inherit HttpMessageHandler()
    let mutable requestToken = CancellationToken.None
    let handle (request: HttpRequestMessage) cancellation =
        assert(request.Method=HttpMethod.Get && request.RequestUri.Host="www.nuget.org")
        assert(request.RequestUri.Query="" && request.Content=null)
        requestToken <- cancellation
        if headerDelay>0 then Thread.Sleep headerDelay
        new HttpResponseMessage(status, Content=content())
    member _.RequestToken = requestToken
    override _.Send(request, cancellation) = handle request cancellation
    override _.SendAsync(request, cancellation) = Task.FromResult(handle request cancellation)
let observeDiagnostic status content =
    httpTransportWithHandler diagnosticFixtureKey (new DiagnosticHandler(status,content,0)) "FS.GG.UI.Scene" "0.32.3"
// Independent fixture digests: exact received bytes, and the default HTTP/1.1 reason phrase.
let sha (bytes: byte[]) = Convert.ToHexString(Security.Cryptography.SHA256.HashData bytes).ToLowerInvariant()
let fp (bytes: byte[]) = ":bodyBytes="+string bytes.Length+":bodySha256="+sha bytes
let reasonFp (text: string) = let bytes=Encoding.UTF8.GetBytes text in ":reasonBytes="+string bytes.Length+":reasonSha256="+sha bytes
let defaultReason = reasonFp "Bad Request"
let checkRefused body suffix =
    let row=observeDiagnostic HttpStatusCode.BadRequest (fun () -> new ByteArrayContent(body))
    assert(row.HttpStatus=Some 400 && row.Outcome="authorization-unproven:m-missing:e-unspecified:"+suffix)
    let sanitized=JsonSerializer.Serialize(row)
    assert(not(sanitized.Contains(diagnosticFixtureKey)) && not(sanitized.Contains(verificationPolicy)))
    assert(match inspect (fun id _ -> {row with PackageId=id}) ids "0.32.3" with Ok(_,false)->true|_->false)
    assert(match inspect (fun id _ -> {row with PackageId=id;HttpStatus=Some 200}) ids "0.32.3" with Ok(_,false)->true|_->false)
let classified=observeDiagnostic HttpStatusCode.BadRequest (fun () -> new StringContent(verificationPolicy))
assert(classified.HttpStatus=Some 400 && classified.Outcome="security-policy-verification-scope-required")
assert(match inspect (fun id _ -> {classified with PackageId=id}) ids "0.32.3" with Ok(_,false)->true|_->false)
for body in ["unknown service refusal"; "{\"message\":\""+verificationPolicy+"\"}"; verificationPolicy+" extra"; " unknown with padding \r\n"] do
    let bytes=Encoding.UTF8.GetBytes body
    checkRefused bytes ("s-bounded:r-unmatched"+fp bytes+defaultReason)
// Key reflection, empty, malformed and oversize bodies carry no digest of any kind.
for body, suffix in [Encoding.UTF8.GetBytes(verificationPolicy+"\n"+diagnosticFixtureKey), "s-bounded:r-reflection"
                     Encoding.UTF8.GetBytes("ApiKeyNotAuthorized "+diagnosticFixtureKey), "s-bounded:r-reflection"
                     [||], "s-bounded:r-empty"
                     [|0xffuy|], "s-bounded:r-non-utf8"
                     Array.create 4097 65uy, "s-declared-oversize:r-not-read"] do
    checkRefused body suffix
    assert(not(suffix.Contains "Sha256") && not(suffix.Contains "Bytes="))

// Closed NuGetGallery allowlist: exact text yields only its resource name, never text or digest.
assert(knownGalleryMessages.Length=20)
assert((knownGalleryMessages |> List.map fst |> Set.ofList).Count=20)
for name in ["SecurityPolicy_RequireApiKeyWithPackageVerifyScope"; "SecurityPolicy_RequireMinProtocolVersionForPush"
             "SecurityPolicy_RequireMicrosoftPackageMetadataComplianceForPush"; "SecurityPolicy_RequirePackagePrefixReserved"
             "SecurityPolicy_CopyrightNotCompliant"; "SecurityPolicy_RequiredAuthorMissing"; "SecurityPolicy_RequiredLicenseUrlMissing"
             "SecurityPolicy_RequiredProjectUrlMissing"; "SecurityPolicy_PackageAuthorNotAllowed"; "SecurityPolicy_PackageAuthorDuplicatesNotAllowed"
             "ApiKeyNotAuthorized"; "ApiKeyOwnerUnconfirmed"; "ApiKeyOwnerLocked"; "ApiController.PackageIdFormatInvalid"
             "ApiController.VersionRequired"; "ApiController.VersionNotSemantic"] do
    assert(knownGalleryMessages |> List.exists(fun (known,_) -> known=name))
assert(File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__,"NuGetVerifyKey.fs")).Contains("NuGet/NuGetGallery@c25bab040e19d24593fc0c298a0cedb51bf57dcb"))
let observeText (text: string) =
    observeDiagnostic HttpStatusCode.BadRequest (fun () ->
        let content=new ByteArrayContent(Encoding.UTF8.GetBytes text)
        content.Headers.ContentType <- Headers.MediaTypeHeaderValue("text/html")
        content)
for name, text in knownGalleryMessages |> List.filter(fun (_,text) -> not(text.Contains "{")) do
    let row=observeText ("\r\n"+text+" \n")
    let expected=if text=verificationPolicy then "security-policy-verification-scope-required" else "authorization-unproven:m-html:e-unspecified:s-bounded:r-known:"+name
    assert(row.HttpStatus=Some 400 && row.Outcome=expected)
    assert(not(row.Outcome.Contains "Sha256") && not(JsonSerializer.Serialize(row).Contains(text)))
    assert(match inspect (fun id _ -> {row with PackageId=id}) ids "0.32.3" with Ok(_,false)->true|_->false)
// Templates: placeholder values are never emitted; the exact bytes are fingerprinted for offline comparison.
for name, text in ["SecurityPolicy_RequireMinProtocolVersionForPush", "A client version '4.1.0' or higher is required to be able to push packages. Please contact support@nuget.org to get more details."
                   "PackageWithIdAndVersionNotFound", "A package with ID 'FS.GG.UI.Scene' and version '0.32.3' does not exist."
                   "SecurityPolicy_RequiredAuthorMissing", "The package metadata is missing required author 'Microsoft'."] do
    let row=observeText text
    assert(row.Outcome="authorization-unproven:m-html:e-unspecified:s-bounded:r-known:"+name+fp (Encoding.UTF8.GetBytes text)+defaultReason)
    assert(not(row.Outcome.Contains "4.1.0") && not(row.Outcome.Contains "Microsoft'"))
for text in ["A client version '' or higher is required to be able to push packages. Please contact support@nuget.org to get more details."
             "A client version '4.1\n0' or higher is required to be able to push packages. Please contact support@nuget.org to get more details."
             "A client version '"+String.replicate 257 "9"+"' or higher is required to be able to push packages. Please contact support@nuget.org to get more details."
             "The specified API key is invalid, has expired, or does not have permission to access the specified package"] do
    let row=observeText text
    assert(row.Outcome="authorization-unproven:m-html:e-unspecified:s-bounded:r-unmatched"+fp (Encoding.UTF8.GetBytes text)+defaultReason)

// The reason phrase is key-checked before its own length and digest; its text never leaves.
type ReasonHandler(reason: string, body: string) =
    inherit HttpMessageHandler()
    let handle () = new HttpResponseMessage(HttpStatusCode.BadRequest, ReasonPhrase=reason, Content=new StringContent(body))
    override _.Send(_, _) = handle ()
    override _.SendAsync(_, _) = Task.FromResult(handle ())
let observeReason reason body = httpTransportWithHandler diagnosticFixtureKey (new ReasonHandler(reason,body)) "FS.GG.UI.Scene" "0.32.3"
let mystery="service refusal not in the allowlist"
let reasonRow=observeReason mystery mystery
assert(reasonRow.Outcome="authorization-unproven:m-text:e-utf8:s-bounded:r-unmatched"+fp (Encoding.UTF8.GetBytes mystery)+reasonFp mystery)
assert(not(JsonSerializer.Serialize(reasonRow).Contains mystery))
let reflectedReason=observeReason ("Bad Request "+diagnosticFixtureKey) mystery
assert(reflectedReason.Outcome="authorization-unproven:m-text:e-utf8:s-bounded:r-unmatched"+fp (Encoding.UTF8.GetBytes mystery)+":reason-reflection")
assert(not(reflectedReason.Outcome.Contains "reasonSha256"))
let reflectedBody=observeReason mystery (mystery+diagnosticFixtureKey)
assert(reflectedBody.Outcome="authorization-unproven:m-text:e-utf8:s-bounded:r-reflection")
let knownReason=observeReason "ignored for exact known bodies" "Version is required"
assert(knownReason.Outcome="authorization-unproven:m-text:e-utf8:s-bounded:r-known:ApiController.VersionRequired")
for media,charset,encoded,expected in ["text/plain","utf-8",false,"m-text:e-utf8"; "text/html","iso-8859-1",false,"m-html:e-other"; "application/json","",false,"m-json:e-unspecified"; "application/x-secret-header-"+diagnosticFixtureKey,"",true,"m-other:e-encoded"] do
    let row=observeDiagnostic HttpStatusCode.BadRequest (fun () ->
        let content=new ByteArrayContent(Encoding.UTF8.GetBytes "unmatched")
        content.Headers.ContentType <- Headers.MediaTypeHeaderValue(media)
        if charset<>"" then content.Headers.ContentType.CharSet <- charset
        if encoded then content.Headers.ContentEncoding.Add("fixture-secret-"+diagnosticFixtureKey)
        content)
    assert(row.Outcome="authorization-unproven:"+expected+":s-bounded:r-unmatched"+fp (Encoding.UTF8.GetBytes "unmatched")+defaultReason)
    assert(not(JsonSerializer.Serialize(row).Contains(diagnosticFixtureKey)))
for account in [null; ""; "other-owner"; "paradigma11"; "Paradigma11 "; "Paradigma11\n"] do
    assert(not(effectiveAccountAccepted account))
assert(effectiveAccountAccepted "Paradigma11")

type DiagnosticStream(bytes: byte[], throwRead: bool) =
    inherit MemoryStream(bytes)
    let mutable readBytes=0
    let mutable readCalls=0
    member _.ReadBytes=readBytes
    member _.ReadCalls=readCalls
    override _.CanSeek=false
    override this.ReadAsync(buffer: Memory<byte>, cancellation) =
        readCalls <- readCalls+1
        if throwRead then raise (IOException("must-not-escape-"+diagnosticFixtureKey))
        let count=min buffer.Length (bytes.Length-int this.Position)
        bytes.AsMemory(int this.Position,count).CopyTo(buffer)
        this.Position <- this.Position+int64 count
        readBytes <- readBytes+count
        ValueTask<int>(count)
let oversized=new DiagnosticStream(Array.create 9000 65uy,false)
let large=observeDiagnostic HttpStatusCode.BadRequest (fun () -> new StreamContent(oversized))
assert(large.HttpStatus=Some 400 && large.Outcome="authorization-unproven:m-missing:e-unspecified:s-stream-oversize:r-limit-reached" && oversized.ReadBytes=4097)
let declaredLarge=new DiagnosticStream(Array.create 9000 65uy,true)
let declared=observeDiagnostic HttpStatusCode.BadRequest (fun () ->
    let content=new StreamContent(declaredLarge)
    content.Headers.ContentLength <- Nullable 9000L
    content)
assert(declared.HttpStatus=Some 400 && declared.Outcome="authorization-unproven:m-missing:e-unspecified:s-declared-oversize:r-not-read" && declaredLarge.ReadCalls=0)
let failing=new DiagnosticStream([|1uy|],true)
let failed=observeDiagnostic HttpStatusCode.BadRequest (fun () -> new StreamContent(failing))
assert(failed.HttpStatus=Some 400 && failed.Outcome="authorization-unproven:m-missing:e-unspecified:s-unknown:r-read-failure")
for status in [HttpStatusCode.OK;HttpStatusCode.Forbidden;HttpStatusCode.Redirect] do
    let unread=new DiagnosticStream([|1uy|],true)
    let row=observeDiagnostic status (fun () -> new StreamContent(unread))
    assert(unread.ReadCalls=0 && row.HttpStatus=Some(int status))
    assert(row.Outcome=(if status=HttpStatusCode.OK then "authorized" else "authorization-unproven"))

type CancelledDiagnosticStream() =
    inherit MemoryStream()
    let mutable bodyToken=CancellationToken.None
    member _.BodyToken=bodyToken
    override _.CanSeek=false
    override _.ReadAsync(_: Memory<byte>, cancellation) =
        bodyToken <- cancellation
        let pending=TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)
        cancellation.Register(fun () -> pending.TrySetCanceled(cancellation) |> ignore) |> ignore
        ValueTask<int>(pending.Task)
let cancelledBody=new CancelledDiagnosticStream()
let delayedHandler=new DiagnosticHandler(HttpStatusCode.BadRequest,(fun () -> new StreamContent(cancelledBody)),8000)
// Shared10s should finish near10s; a reset after the8s header phase takes18s.
// Allow5s CI scheduling grace while still refusing the reset-timer mutant.
let deadlineClock=System.Diagnostics.Stopwatch.StartNew()
let cancelled=httpTransportWithHandler diagnosticFixtureKey delayedHandler "FS.GG.UI.Scene" "0.32.3"
deadlineClock.Stop()
assert(cancelled.HttpStatus=Some 400 && cancelled.Outcome="authorization-unproven:m-missing:e-unspecified:s-unknown:r-timeout")
assert(deadlineClock.Elapsed < TimeSpan.FromSeconds 15.)
// HttpClient links its request token internally; token identity is not the deadline contract.
assert(delayedHandler.RequestToken.CanBeCanceled && cancelledBody.BodyToken.IsCancellationRequested)
printfn "PASS bounded400 diagnostic: exact policy, closed Gallery allowlist tags, unknown-body/reason length+SHA-256 only, no reflected body/secret digest,4097-byte cap, same deadline and received status retained; no network requests"
