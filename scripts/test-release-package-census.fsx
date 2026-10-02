#load "ReleasePackageCensus.fs"
open System
open System.IO
open System.Text
open System.Text.Json
open Rendering.ReleasePackageCensus
let plan = JsonDocument.Parse(File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__, "../eng/release/svg-external-authority-0.32.0.json")))
let packages = plan.RootElement.GetProperty("packages").EnumerateArray() |> Seq.map (fun p -> p.GetProperty("id").GetString()) |> Seq.toList
let response status headers body : Response = { Status=status; Headers=headers; Body=Encoding.UTF8.GetBytes(body:string) }
let metadata package = $"{{\"id\":1,\"name\":\"{package}\",\"package_type\":\"nuget\",\"owner\":{{\"login\":\"FS-GG\"}},\"repository\":{{\"full_name\":\"FS-GG/FS.GG.Rendering\"}},\"url\":\"https://api.github.com/orgs/FS-GG/packages/nuget/{package}\",\"version_count\":1,\"updated_at\":\"2026-10-02T00:00:00Z\"}}"
let org = "["+(packages |> List.mapi (fun i p -> $"{{\"id\":{i+1},\"name\":\"{p}\",\"package_type\":\"nuget\"}}") |> String.concat ",")+"]"
let baseline (request: Request) _ =
    if request.Method <> "GET" || not request.Authenticated || request.Uri.Host <> "api.github.com" then failwith "unsafe request"
    if request.Uri.AbsolutePath = "/orgs/FS-GG/packages" then response 200 Map.empty org
    elif request.Uri.AbsolutePath.EndsWith("/versions") then response 200 Map.empty "[]"
    else response 200 Map.empty (metadata(request.Uri.Segments |> Array.last))
let mutable checks = 0
let check name expected transport limits =
    match inspectWith limits transport packages "0.32.0" with
    | Ok actual when actual.Overall = expected -> checks <- checks+1
    | result -> failwithf "%s failed: %A" name result
check "complete19" Absent baseline DefaultLimits
for status in [403;404;429;500] do check (string status) Unknown (fun _ _ -> response status Map.empty "{}") DefaultLimits
let mutate body = fun r t -> if r.Uri.AbsolutePath.EndsWith("/versions") then response 200 Map.empty body else baseline r t
for body in ["{}";"not-json";"[{\"id\":1,\"id\":2,\"name\":\"0.32.0\"}]";"[{\"id\":0,\"name\":\"0.1.0\",\"url\":\"x\"}]"] do check "malformed" Unknown (mutate body) DefaultLimits
let first = packages.Head
let version = $"{{\"id\":1,\"name\":\"0.32.0\",\"url\":\"https://api.github.com/orgs/FS-GG/packages/nuget/{first}/versions/1\"}}"
for state in ["active";"deleted"] do
 check ("occupied-"+state) Occupied (fun r t -> if r.Uri.AbsolutePath.Contains(first+"/versions") && r.Uri.Query.Contains("state="+state) then response 200 Map.empty ("["+version+"]") else baseline r t) DefaultLimits
check "duplicate-version" Unknown (mutate("["+version+","+version+"]")) DefaultLimits
for link in ["<https://evil.example/versions>; rel=\"next\"";"<https://api.github.com/orgs/FS-GG/packages/nuget/FS.GG.UI.Build/versions?per_page=100&page=1&state=active>; rel=\"next\"";"broken";"<https://api.github.com/orgs/FS-GG/packages/nuget/FS.GG.UI.Build/versions?per_page=100&page=2&state=deleted>; rel=\"next\""] do
 check "pagination" Unknown (fun r t -> if r.Uri.AbsolutePath.EndsWith("/versions") then response 200 (Map.ofList [("link",link)]) "[]" else baseline r t) DefaultLimits
let mutable counts = Map.empty
check "metadata-drift" Unknown (fun r t -> let result=baseline r t in if r.Uri.AbsolutePath.EndsWith("/versions") || r.Uri.AbsolutePath = "/orgs/FS-GG/packages" then result else let n=Map.tryFind r.Uri.AbsoluteUri counts |> Option.defaultValue 0 in counts<-Map.add r.Uri.AbsoluteUri (n+1) counts; if n>0 then response 200 Map.empty ((Encoding.UTF8.GetString result.Body).Replace("00:00:00","00:00:01")) else result) DefaultLimits
check "page-budget" Unknown baseline { DefaultLimits with MaxGitHubPages=1 }
check "byte-budget" Unknown baseline { DefaultLimits with MaxTotalBytes=1L }
check "expired" Unknown baseline { DefaultLimits with AcquisitionTimeout=TimeSpan.Zero }
let transport = httpTransport "fixture-secret"
try transport {Uri=Uri("https://evil.example/");Method="GET";Authenticated=true} (TimeSpan.FromSeconds 1.0) |> ignore; failwith "host accepted" with :? InvalidOperationException -> checks<-checks+1
printfn "%d finite census controls PASS; no network requests" checks

let completed = inspectWith DefaultLimits baseline packages "0.32.0" |> Result.defaultWith failwith
let serialized = receiptJson (String.replicate 40 "a") (String.replicate 40 "b") "1" "0.32.0" "hash" completed
use receipt = JsonDocument.Parse serialized
if receipt.RootElement.GetProperty("selectedRoster").GetArrayLength() <> 19 || serialized.Contains("fixture-secret") then failwith "receipt refused"
printfn "sanitized complete19 receipt PASS"
