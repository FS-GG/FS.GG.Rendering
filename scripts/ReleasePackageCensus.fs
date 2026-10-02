// Bounded read contract adapted from Templates FeedOccupancy; no publisher authority.
module Rendering.ReleasePackageCensus

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading

let Repository = "FS-GG/FS.GG.Rendering"

type Verdict = Absent | Occupied | Unknown

type PermissionTerm = { Name: string; Level: string }

type AcceptedPermissionsObservation = {
    SyntaxState: string
    UnrecognizedComponents: string list
    Alternatives: PermissionTerm list list
}

type FailureDiagnostic = {
    Phase: string
    Page: int option
    HttpStatus: int
    ErrorClass: string
    AcceptedPermissionsState: string
    AcceptedPermissionSets: PermissionTerm list list
    AcceptedPermissionsObservation: AcceptedPermissionsObservation option
    RequiredScopes: string list
}

type Request = { Uri: Uri; Method: string; Authenticated: bool }
type Response = { Status: int; Headers: Map<string, string>; Body: byte[] }
type Transport = Request -> TimeSpan -> Response

type Limits = {
    MaxGitHubPages: int
    MaxItemsPerPage: int
    MaxResponseBytes: int
    MaxTotalBytes: int64
    RequestTimeout: TimeSpan
    AcquisitionTimeout: TimeSpan
}

let DefaultLimits = {
    MaxGitHubPages = 20
    MaxItemsPerPage = 100
    MaxResponseBytes = 2 * 1024 * 1024
    MaxTotalBytes = 50L * 1024L * 1024L
    RequestTimeout = TimeSpan.FromSeconds 15.0
    AcquisitionTimeout = TimeSpan.FromSeconds 120.0
}

type FeedEvidence = {
    Feed: string
    Verdict: Verdict
    Reason: string
    Endpoint: string
    Statuses: int list
    Pages: int
    Records: int
    ResponseSha256: string list
    Complete: bool
    FailureDiagnostic: FailureDiagnostic option
}

let private stableVersion = Regex("\\A(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\z", RegexOptions.CultureInvariant)
let private packageVersion = Regex("\\A(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:\\.(0|[1-9][0-9]*))?(?:-([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?\\z", RegexOptions.CultureInvariant)

let validateCandidate (value: string) =
    if stableVersion.IsMatch value then Ok value
    else Error "candidate-version-refused"

let private normalized value =
    let matched = packageVersion.Match value
    if not matched.Success then Error "malformed-version-identity"
    else
        let fourth = matched.Groups.[4].Value
        let components = [ matched.Groups.[1].Value; matched.Groups.[2].Value; matched.Groups.[3].Value ]
        let core = if fourth = "" || fourth = "0" then String.Join(".", components) else String.Join(".", components @ [ fourth ])
        let prerelease = matched.Groups.[5].Value
        Ok(if prerelease = "" then core else core + "-" + prerelease.ToLowerInvariant())

let private verdictText = function Absent -> "ABSENT" | Occupied -> "OCCUPIED" | Unknown -> "UNKNOWN"
let private sha256 (bytes: byte[]) = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
let private diagnosticBodyLimit = 16 * 1024
let private acceptedPermissionsLimit = 1024
let private permissionName = Regex("\\A(packages|contents|metadata)\\z", RegexOptions.CultureInvariant)
let private permissionLevel = Regex("\\A(read|write|admin)\\z", RegexOptions.CultureInvariant)
let private scopeMessage = Regex("\\AYour token has not been granted the required scopes to execute this request\\. The '(read:packages|write:packages|delete:packages)' scope is required\\.\\z", RegexOptions.CultureInvariant)

let private validateNoDuplicateKeys (body: byte[]) =
    try
        let mutable reader = Utf8JsonReader(ReadOnlySpan<byte>(body), JsonReaderOptions(CommentHandling = JsonCommentHandling.Disallow))
        let stack = Stack<HashSet<string>>()
        while reader.Read() do
            match reader.TokenType with
            | JsonTokenType.StartObject -> stack.Push(HashSet<string>(StringComparer.Ordinal))
            | JsonTokenType.EndObject -> stack.Pop() |> ignore
            | JsonTokenType.PropertyName ->
                if stack.Count = 0 || not (stack.Peek().Add(reader.GetString())) then
                    raise (InvalidDataException "duplicate-json-key")
            | _ -> ()
        Ok ()
    with
    | :? JsonException -> Error "malformed-json"
    | :? InvalidDataException as ex -> Error ex.Message

let parseJson body =
    match validateNoDuplicateKeys body with
    | Error reason -> Error reason
    | Ok () ->
        try Ok(JsonDocument.Parse(ReadOnlyMemory<byte>(body)))
        with :? JsonException -> Error "malformed-json"

let private property (element: JsonElement) (name: string) kind =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.ValueKind <> JsonValueKind.Object || not (element.TryGetProperty(name, &value)) || value.ValueKind <> kind then
        Error($"missing-or-invalid-{name}")
    else Ok value

let private stringProperty element name =
    match property element name JsonValueKind.String with
    | Ok value -> Ok(value.GetString())
    | Error reason -> Error reason

let private int64Property element name =
    match property element name JsonValueKind.Number with
    | Ok value ->
        let mutable number = 0L
        if value.TryGetInt64(&number) then Ok number else Error($"missing-or-invalid-{name}")
    | Error reason -> Error reason

let private header (headers: Map<string, string>) name =
    headers |> Map.toSeq |> Seq.tryPick (fun (key, value) ->
        if String.Equals(key, name, StringComparison.OrdinalIgnoreCase) then Some value else None)

let parseAcceptedPermissions (raw: string option) =
    match raw with
    | None -> "absent", []
    | Some value when value.Length > acceptedPermissionsLimit -> "oversized", []
    | Some value when value = "" || value |> Seq.exists Char.IsControl -> "malformed", []
    | Some value ->
        let alternatives = value.Split(';')
        if alternatives.Length = 0 || alternatives.Length > 4 || alternatives |> Array.exists (fun item -> item.Trim() = "") then
            "malformed", []
        else
            let mutable state = "valid"
            let parsed = ResizeArray<PermissionTerm list>()
            let normalizedAlternatives = HashSet<string>(StringComparer.Ordinal)
            for alternative in alternatives do
                let terms = alternative.Split(',')
                if terms.Length = 0 || terms.Length > 8 || terms |> Array.exists (fun item -> item.Trim() = "") then
                    state <- "malformed"
                else
                    let seen = Dictionary<string, string>(StringComparer.Ordinal)
                    let parsedTerms = ResizeArray<PermissionTerm>()
                    for rawTerm in terms do
                        let pieces = rawTerm.Trim().Split('=')
                        if pieces.Length <> 2 || pieces.[0] <> pieces.[0].Trim() || pieces.[1] <> pieces.[1].Trim() then
                            state <- "malformed"
                        else
                            let name, level = pieces.[0], pieces.[1]
                            if not (permissionName.IsMatch name) || not (permissionLevel.IsMatch level) then
                                if state = "valid" then state <- "unrecognized"
                            elif seen.ContainsKey name then
                                state <- "malformed"
                            else
                                seen.Add(name, level)
                                parsedTerms.Add { Name = name; Level = level }
                    if state = "valid" then
                        let identity = parsedTerms |> Seq.map (fun term -> term.Name + "=" + term.Level) |> Seq.sort |> String.concat ","
                        if not (normalizedAlternatives.Add identity) then state <- "malformed"
                        else parsed.Add(parsedTerms |> Seq.toList)
            if state = "valid" then state, (parsed |> Seq.toList) else state, []

let private observedPermissionName = function
    | "packages" | "contents" | "metadata" | "organization_packages" | "administration" as value -> value
    | _ -> "other"

let private observedPermissionLevel = function
    | "read" | "write" | "admin" | "delete" | "none" as value -> value
    | _ -> "other"

let private observeUnrecognizedAcceptedPermissions (value: string) =
    let malformed () = {
        SyntaxState = "malformed"
        UnrecognizedComponents = []
        Alternatives = []
    }
    if value.Length > acceptedPermissionsLimit || value = "" || value |> Seq.exists Char.IsControl then malformed ()
    else
        let rawAlternatives = value.Split(';')
        if rawAlternatives.Length = 0 || rawAlternatives.Length > 4 || rawAlternatives |> Array.exists (fun item -> item.Trim() = "") then malformed ()
        else
            let alternatives = ResizeArray<PermissionTerm list>()
            let alternativeIdentities = HashSet<string>(StringComparer.Ordinal)
            let unrecognizedComponents = ResizeArray<string>()
            let seenComponents = HashSet<string>(StringComparer.Ordinal)
            let mutable valid = true
            let note kind = if seenComponents.Add kind then unrecognizedComponents.Add kind
            for rawAlternative in rawAlternatives do
                let rawTerms = rawAlternative.Split(',')
                if rawTerms.Length = 0 || rawTerms.Length > 8 || rawTerms |> Array.exists (fun item -> item.Trim() = "") then valid <- false
                else
                    let seenNames = Dictionary<string, string>(StringComparer.Ordinal)
                    let identityTerms = ResizeArray<string>()
                    let projectedTerms = ResizeArray<PermissionTerm>()
                    for rawTerm in rawTerms do
                        let term = rawTerm.Trim()
                        let pieces = term.Split('=')
                        if pieces.Length <> 2 || pieces.[0] = "" || pieces.[1] = "" ||
                           pieces.[0] <> pieces.[0].Trim() || pieces.[1] <> pieces.[1].Trim() ||
                           pieces.[0] |> Seq.exists Char.IsWhiteSpace || pieces.[1] |> Seq.exists Char.IsWhiteSpace then
                            valid <- false
                        else
                            let name, level = pieces.[0], pieces.[1]
                            if seenNames.ContainsKey name then valid <- false
                            else
                                seenNames.Add(name, level)
                                identityTerms.Add(name + "=" + level)
                                if not (permissionName.IsMatch name) then note "name"
                                if not (permissionLevel.IsMatch level) then note "level"
                                projectedTerms.Add { Name = observedPermissionName name; Level = observedPermissionLevel level }
                    if valid then
                        let identity = identityTerms |> Seq.sort |> String.concat ","
                        if not (alternativeIdentities.Add identity) then valid <- false
                        else alternatives.Add(projectedTerms |> Seq.toList)
            if not valid then malformed ()
            else {
                SyntaxState = "valid"
                UnrecognizedComponents = unrecognizedComponents |> Seq.toList
                Alternatives = alternatives |> Seq.toList
            }

let private diagnosticMessage (body: byte[]) =
    if body.Length > diagnosticBodyLimit then Error "oversized"
    else
        match parseJson body with
        | Error _ -> Error "malformed"
        | Ok document ->
            use document = document
            match stringProperty document.RootElement "message" with
            | Ok message -> Ok message
            | Error _ -> Error "malformed"

let private failureDiagnostic phase page (response: Response) =
    let acceptedPermissions = header response.Headers "x-accepted-github-permissions"
    let permissionsState, permissionSets = parseAcceptedPermissions acceptedPermissions
    let permissionsObservation =
        match permissionsState, acceptedPermissions with
        | "unrecognized", Some value -> Some(observeUnrecognizedAcceptedPermissions value)
        | _ -> None
    let rateLimited =
        response.Status = 429 ||
        (response.Status = 403 && header response.Headers "x-ratelimit-remaining" = Some "0")
    let message = diagnosticMessage response.Body
    let scopes =
        match message with
        | Ok value ->
            let matched = scopeMessage.Match value
            if matched.Success then [ matched.Groups.[1].Value ] else []
        | Error _ -> []
    let hasPackagePermission =
        permissionSets |> List.exists (List.exists (fun term -> term.Name = "packages"))
    let everyAlternativeRequiresPackageAdmin =
        not permissionSets.IsEmpty &&
        (permissionSets
         |> List.forall (List.exists (fun term -> term.Name = "packages" && term.Level = "admin")))
    let errorClass =
        if rateLimited then "rate-limited"
        elif permissionsState = "malformed" || permissionsState = "oversized" then "malformed-error"
        elif Result.isError message then "malformed-error"
        elif message = Ok "You must have admin access to this package." || everyAlternativeRequiresPackageAdmin then
            "package-admin-required"
        elif not scopes.IsEmpty then "package-scope-required"
        elif message = Ok "Resource not accessible by integration" || hasPackagePermission then
            "integration-permission-denied"
        elif response.Status = 403 then
            match message with Error _ -> "malformed-error" | Ok _ -> "unclassified-forbidden"
        else "other-http-error"
    {
        Phase = phase
        Page = page
        HttpStatus = response.Status
        ErrorClass = errorClass
        AcceptedPermissionsState = permissionsState
        AcceptedPermissionSets = permissionSets
        AcceptedPermissionsObservation = permissionsObservation
        RequiredScopes = scopes
    }

type private FetchState = {
    Stopwatch: Stopwatch
    TotalBytes: int64 ref
    mutable Statuses: int list
    mutable Hashes: string list
    mutable FailureDiagnostic: FailureDiagnostic option
}

let private unknown feed endpoint state pages records reason = {
    Feed = feed; Verdict = Unknown; Reason = reason; Endpoint = endpoint
    Statuses = List.rev state.Statuses; Pages = pages; Records = records
    ResponseSha256 = List.rev state.Hashes; Complete = false
    FailureDiagnostic = state.FailureDiagnostic
}

let private acquisitionExpired limits (state: FetchState) =
    state.Stopwatch.Elapsed >= limits.AcquisitionTimeout

let private fetch limits transport (state: FetchState) authenticated diagnosticSite (uri: Uri) =
    let remaining = limits.AcquisitionTimeout - state.Stopwatch.Elapsed
    if remaining <= TimeSpan.Zero then Error "acquisition-time-limit"
    elif uri.Scheme <> Uri.UriSchemeHttps then Error "non-https-endpoint-refused"
    else
        let timeout = if remaining < limits.RequestTimeout then remaining else limits.RequestTimeout
        let requestClock = Stopwatch.StartNew()
        try
            let response = transport { Uri = uri; Method = "GET"; Authenticated = authenticated } timeout
            if acquisitionExpired limits state then Error "acquisition-time-limit"
            elif requestClock.Elapsed >= timeout then Error "request-time-limit"
            else
                state.Statuses <- response.Status :: state.Statuses
                if response.Body.Length > limits.MaxResponseBytes then Error "response-size-limit"
                else
                    state.TotalBytes.Value <- state.TotalBytes.Value + int64 response.Body.Length
                    if state.TotalBytes.Value > limits.MaxTotalBytes then Error "total-size-limit"
                    else
                        state.Hashes <- sha256 response.Body :: state.Hashes
                        if acquisitionExpired limits state then Error "acquisition-time-limit"
                        elif response.Status <> 200 then
                            match diagnosticSite with
                            | Some(phase, page) -> state.FailureDiagnostic <- Some(failureDiagnostic phase page response)
                            | None -> ()
                            Error($"http-{response.Status}")
                        else Ok response
        with
        | :? OperationCanceledException -> Error "request-timeout"
        | :? TimeoutException -> Error "request-timeout"
        | :? InvalidDataException as ex -> Error ex.Message
        | _ -> Error "transport-failure"

type private Metadata = { Name: string; PackageType: string; Owner: string; Repository: string; Url: string; VersionCount: int64; UpdatedAt: string }

let private githubMetadata packageId (response: Response) =
    match parseJson response.Body with
    | Error reason -> Error reason
    | Ok document ->
        use document = document
        let root = document.RootElement
        match stringProperty root "name", stringProperty root "package_type", property root "owner" JsonValueKind.Object,
              property root "repository" JsonValueKind.Object, stringProperty root "url", int64Property root "version_count", stringProperty root "updated_at" with
        | Ok name, Ok packageType, Ok owner, Ok repository, Ok url, Ok count, Ok updated ->
            match stringProperty owner "login", stringProperty repository "full_name" with
            | Ok ownerLogin, Ok repositoryName when
                String.Equals(name, packageId, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(packageType, "nuget", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(ownerLogin, "FS-GG", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(repositoryName, Repository, StringComparison.OrdinalIgnoreCase) &&
                url = $"https://api.github.com/orgs/FS-GG/packages/nuget/{packageId}" && count >= 0L ->
                    Ok { Name = name; PackageType = packageType; Owner = ownerLogin; Repository = repositoryName; Url = url; VersionCount = count; UpdatedAt = updated }
            | Ok _, Ok _ -> Error "github-package-identity-refused"
            | Error reason, _ | _, Error reason -> Error reason
        | Error reason, _, _, _, _, _, _ | _, Error reason, _, _, _, _, _ | _, _, Error reason, _, _, _, _
        | _, _, _, Error reason, _, _, _ | _, _, _, _, Error reason, _, _ | _, _, _, _, _, Error reason, _
        | _, _, _, _, _, _, Error reason -> Error reason

let private sameMetadata left right =
    String.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
    String.Equals(left.PackageType, right.PackageType, StringComparison.OrdinalIgnoreCase) &&
    String.Equals(left.Owner, right.Owner, StringComparison.OrdinalIgnoreCase) &&
    String.Equals(left.Repository, right.Repository, StringComparison.OrdinalIgnoreCase) &&
    left.Url = right.Url &&
    left.VersionCount = right.VersionCount && left.UpdatedAt = right.UpdatedAt

let private linkNext (response: Response) =
    match response.Headers |> Map.tryFind "link" with
    | None -> Ok None
    | Some raw ->
        let entries = raw.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
        let parsed = entries |> Array.map (fun entry -> Regex.Match(entry, "^<([^>]+)>;\\s*rel=\"([^\"]+)\"$", RegexOptions.CultureInvariant))
        if parsed |> Array.exists (fun matched -> not matched.Success) then Error "malformed-link-header"
        else
            let next = parsed |> Array.choose (fun matched ->
                if matched.Groups.[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.contains "next"
                then Some matched.Groups.[1].Value else None)
            if next.Length > 1 then Error "duplicate-next-link"
            elif next.Length = 0 then Ok None
            else
                match Uri.TryCreate(next.[0], UriKind.Absolute) with
                | true, uri -> Ok(Some uri)
                | _ -> Error "malformed-next-link"

let private expectedPageUri packageId state page =
    Uri($"https://api.github.com/orgs/FS-GG/packages/nuget/{packageId}/versions?per_page=100&page={page}&state={state}")

let private validPageUri packageId packageState page (uri: Uri) =
    let expectedPath = $"/orgs/FS-GG/packages/nuget/{packageId}/versions"
    let pairs =
        uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun pair -> pair.Split('=', 2))
    uri.Scheme = Uri.UriSchemeHttps && uri.Host = "api.github.com" && uri.IsDefaultPort && uri.UserInfo = "" && uri.Fragment = "" && uri.AbsolutePath = expectedPath &&
    pairs.Length = 3 && pairs |> Array.forall (fun pair -> pair.Length = 2) &&
    (pairs |> Array.map (fun pair -> pair.[0]) |> Set.ofArray).Count = 3 &&
    pairs |> Array.exists (fun pair -> pair.[0] = "per_page" && pair.[1] = "100") &&
    pairs |> Array.exists (fun pair -> pair.[0] = "page" && pair.[1] = string page) &&
    pairs |> Array.exists (fun pair -> pair.[0] = "state" && pair.[1] = packageState)

let private githubVersions packageId limits transport state candidate =
    let ids = HashSet<int64>()
    let versions = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let mutable pages = 0
    let mutable records = 0
    let mutable occupied = false
    let mutable failure: string option = None
    for packageState in [ "active"; "deleted" ] do
        let mutable page = 1
        let mutable next = Some(expectedPageUri packageId packageState page)
        let seenPages = HashSet<string>(StringComparer.Ordinal)
        while next.IsSome && failure.IsNone do
            if pages >= limits.MaxGitHubPages then failure <- Some "github-page-limit"
            else
                let uri = next.Value
                if not (validPageUri packageId packageState page uri) || not (seenPages.Add(uri.AbsoluteUri)) then failure <- Some "github-pagination-refused"
                else
                    match fetch limits transport state true (Some(packageState, Some page)) uri with
                    | Error reason -> failure <- Some reason
                    | Ok response ->
                        pages <- pages + 1
                        match parseJson response.Body with
                        | Error reason -> failure <- Some reason
                        | Ok document ->
                            use document = document
                            if document.RootElement.ValueKind <> JsonValueKind.Array then failure <- Some "github-versions-not-array"
                            else
                                let entries = document.RootElement.EnumerateArray() |> Seq.toArray
                                if entries.Length > limits.MaxItemsPerPage then failure <- Some "github-page-item-limit"
                                else
                                    for entry in entries do
                                        if failure.IsNone then
                                            match int64Property entry "id", stringProperty entry "name", stringProperty entry "url" with
                                            | Ok id, Ok version, Ok url when id > 0L ->
                                                let expectedUrl = $"https://api.github.com/orgs/FS-GG/packages/nuget/{packageId}/versions/{id}"
                                                if not (ids.Add id) then failure <- Some "github-version-id-repeated"
                                                elif url <> expectedUrl then failure <- Some "github-version-binding-refused"
                                                else
                                                    records <- records + 1
                                                    match normalized version with
                                                    | Error reason -> failure <- Some reason
                                                    | Ok value when not (versions.Add value) -> failure <- Some "github-version-identity-repeated"
                                                    | Ok value when value = candidate -> occupied <- true
                                                    | Ok _ -> ()
                                            | Ok _, Ok _, Ok _ -> failure <- Some "github-version-id-refused"
                                            | Error reason, _, _ | _, Error reason, _ | _, _, Error reason -> failure <- Some reason
                                    if failure.IsNone then
                                        match linkNext response with
                                        | Error reason -> failure <- Some reason
                                        | Ok(Some target) ->
                                            page <- page + 1
                                            if not (validPageUri packageId packageState page target) then failure <- Some "github-next-link-refused"
                                            else next <- Some target
                                        | Ok None -> next <- None
    occupied, pages, records, failure

let private githubEvidence packageId limits transport state candidate =
    let endpoint = $"https://api.github.com/orgs/FS-GG/packages/nuget/{packageId}"
    match fetch limits transport state true (Some("metadata-before", None)) (Uri endpoint) with
    | Error reason -> unknown "github-packages" endpoint state 0 0 reason
    | Ok beforeResponse ->
        match githubMetadata packageId beforeResponse with
        | Error reason -> unknown "github-packages" endpoint state 0 0 reason
        | Ok before ->
            let occupied, pages, records, failure = githubVersions packageId limits transport state candidate
            match failure with
            | Some reason -> unknown "github-packages" endpoint state pages records reason
            | None ->
                match fetch limits transport state true (Some("metadata-after", None)) (Uri endpoint) with
                | Error reason -> unknown "github-packages" endpoint state pages records reason
                | Ok afterResponse ->
                    match githubMetadata packageId afterResponse with
                    | Error reason -> unknown "github-packages" endpoint state pages records reason
                    | Ok after when not (sameMetadata before after) -> unknown "github-packages" endpoint state pages records "github-metadata-changed"
                    | Ok _ when acquisitionExpired limits state -> unknown "github-packages" endpoint state pages records "acquisition-time-limit"
                    | Ok _ ->
                        {
                            Feed = "github-packages"; Verdict = if occupied then Occupied else Absent
                            Reason = if occupied then "candidate-version-present" else "complete-active-deleted-census"
                            Endpoint = endpoint; Statuses = List.rev state.Statuses; Pages = pages; Records = records
                            ResponseSha256 = List.rev state.Hashes; Complete = true; FailureDiagnostic = None
                        }


let private orgEvidence limits transport state (packages: string list) =
    let found = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let ids = HashSet<int64>()
    let names = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let endpoint = "https://api.github.com/orgs/FS-GG/packages"
    let valid page (uri: Uri) =
        let expected = Uri($"{endpoint}?package_type=nuget&per_page=100&page={page}")
        let pairs (u: Uri) = u.Query.TrimStart('?').Split('&') |> Array.sort
        uri.Scheme = "https" && uri.Host = "api.github.com" && uri.IsDefaultPort && uri.UserInfo = "" && uri.Fragment = "" && uri.AbsolutePath = expected.AbsolutePath && pairs uri = pairs expected
    let mutable page, records = 1, 0
    let mutable next = Some(Uri($"{endpoint}?package_type=nuget&per_page=100&page=1"))
    let mutable failure = None
    while next.IsSome && failure.IsNone do
        if page > 20 || not (valid page next.Value) then failure <- Some "org-pagination-refused"
        else
            match fetch limits transport state true (Some("org-list", Some page)) next.Value with
            | Error reason -> failure <- Some reason
            | Ok response ->
                match parseJson response.Body with
                | Error reason -> failure <- Some reason
                | Ok doc ->
                    use doc = doc
                    if doc.RootElement.ValueKind <> JsonValueKind.Array then failure <- Some "org-list-not-array"
                    else
                        let entries = doc.RootElement.EnumerateArray() |> Seq.toArray
                        if entries.Length > 100 then failure <- Some "org-page-item-limit"
                        for entry in entries do
                            if failure.IsNone then
                                match int64Property entry "id", stringProperty entry "name", stringProperty entry "package_type" with
                                | Ok id, Ok name, Ok kind when id > 0L && kind = "nuget" && ids.Add id && names.Add name ->
                                    records <- records + 1
                                    if packages |> List.exists (fun p -> String.Equals(p,name,StringComparison.OrdinalIgnoreCase)) then found.Add name |> ignore
                                | _ -> failure <- Some "org-package-identity-refused"
                        if failure.IsNone then
                            match linkNext response with
                            | Error reason -> failure <- Some reason
                            | Ok value -> next <- value; page <- page + 1
    match failure with
    | Some reason -> unknown "github-readable-organization" endpoint state (page-1) records reason
    | None when found.Count <> packages.Length -> unknown "github-readable-organization" endpoint state (page-1) records "selected-package-membership-incomplete"
    | None -> { Feed = "github-readable-organization"; Verdict = Absent; Reason = "readable-list-selected-membership-only"; Endpoint = endpoint; Statuses = List.rev state.Statuses; Pages = page-1; Records = records; ResponseSha256 = List.rev state.Hashes; Complete = true; FailureDiagnostic = None }

type Census = { Selected: string list; StartedUtc: DateTimeOffset; EndedUtc: DateTimeOffset; Organization: FeedEvidence; Packages: (string * FeedEvidence) list; Overall: Verdict }
let inspectWith limits transport (packages: string list) candidate =
    match validateCandidate candidate with
    | Error reason -> Error reason
    | Ok candidate when packages.Length <> 19 || (packages |> Set.ofList).Count <> 19 || packages |> List.exists (fun p -> not (Regex.IsMatch(p,"^FS\.GG\.UI(?:\.[A-Za-z]+)*$"))) -> Error "selected-roster-refused"
    | Ok candidate ->
        let started = DateTimeOffset.UtcNow
        let clock, total = Stopwatch.StartNew(), ref 0L
        let newState () = { Stopwatch = clock; TotalBytes = total; Statuses = []; Hashes = []; FailureDiagnostic = None }
        let org = orgEvidence limits transport (newState()) packages
        let mutable evidence = []
        if org.Complete then
            for package in packages do
                if evidence |> List.forall (fun (_,e) -> e.Complete) then
                    evidence <- evidence @ [(package, githubEvidence package limits transport (newState()) candidate)]
        let overall = if not org.Complete || evidence.Length <> 19 || evidence |> List.exists (fun (_,e) -> not e.Complete) then Unknown elif evidence |> List.exists (fun (_,e) -> e.Verdict = Occupied) then Occupied else Absent
        Ok { Selected = packages; StartedUtc = started; EndedUtc = DateTimeOffset.UtcNow; Organization = org; Packages = evidence; Overall = overall }
let httpTransportWithHandler token (handler: HttpMessageHandler) : Transport =
    let client = new HttpClient(handler, disposeHandler = false)
    fun request timeout ->
        if request.Method <> "GET" then raise (InvalidOperationException "http-method-refused")
        if request.Authenticated && request.Uri.Host <> "api.github.com" then
            raise (InvalidOperationException "authenticated-host-refused")
        use message = new HttpRequestMessage(HttpMethod.Get, request.Uri)
        message.Headers.UserAgent.ParseAdd("FS-GG-Rendering-package-census/1")
        if request.Authenticated then
            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/vnd.github+json"))
            message.Headers.Add("X-GitHub-Api-Version", "2022-11-28")
            message.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
        use cancellation = new CancellationTokenSource(timeout)
        use response = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).GetAwaiter().GetResult()
        use stream = response.Content.ReadAsStreamAsync(cancellation.Token).GetAwaiter().GetResult()
        use memory = new MemoryStream()
        let buffer = Array.zeroCreate<byte> 81920
        let mutable read = stream.ReadAsync(buffer.AsMemory(), cancellation.Token).AsTask().GetAwaiter().GetResult()
        while read > 0 do
            memory.Write(buffer, 0, read)
            if memory.Length > int64 DefaultLimits.MaxResponseBytes then raise (InvalidDataException "response-size-limit")
            read <- stream.ReadAsync(buffer.AsMemory(), cancellation.Token).AsTask().GetAwaiter().GetResult()
        let headers =
            Seq.append response.Headers response.Content.Headers
            |> Seq.map (fun pair -> pair.Key.ToLowerInvariant(), String.Join(",", pair.Value))
            |> Map.ofSeq
        { Status = int response.StatusCode; Headers = headers; Body = memory.ToArray() }

let httpTransport token : Transport =
    let handler = new HttpClientHandler(AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None)
    httpTransportWithHandler token handler

let private failureDiagnosticJson (writer: Utf8JsonWriter) diagnostic =
    writer.WriteStartObject()
    writer.WriteString("phase", diagnostic.Phase)
    diagnostic.Page |> Option.iter (fun page -> writer.WriteNumber("page", page))
    writer.WriteNumber("httpStatus", diagnostic.HttpStatus)
    writer.WriteString("errorClass", diagnostic.ErrorClass)
    writer.WriteString("acceptedPermissionsState", diagnostic.AcceptedPermissionsState)
    writer.WriteStartArray("acceptedPermissionSets")
    for alternative in diagnostic.AcceptedPermissionSets do
        writer.WriteStartArray()
        for term in alternative do
            writer.WriteStartObject()
            writer.WriteString("name", term.Name)
            writer.WriteString("level", term.Level)
            writer.WriteEndObject()
        writer.WriteEndArray()
    writer.WriteEndArray()
    match diagnostic.AcceptedPermissionsObservation with
    | Some observation ->
        writer.WriteStartObject("acceptedPermissionsObservation")
        writer.WriteString("syntaxState", observation.SyntaxState)
        writer.WriteStartArray("unrecognizedComponents")
        observation.UnrecognizedComponents |> List.iter writer.WriteStringValue
        writer.WriteEndArray()
        writer.WriteStartArray("alternatives")
        for alternative in observation.Alternatives do
            writer.WriteStartArray()
            for term in alternative do
                writer.WriteStartObject()
                writer.WriteString("name", term.Name)
                writer.WriteString("level", term.Level)
                writer.WriteEndObject()
            writer.WriteEndArray()
        writer.WriteEndArray()
        writer.WriteEndObject()
    | None -> ()
    if not diagnostic.RequiredScopes.IsEmpty then
        writer.WriteStartArray("requiredScopes")
        diagnostic.RequiredScopes |> List.iter writer.WriteStringValue
        writer.WriteEndArray()
    writer.WriteEndObject()

let private evidenceJson (writer: Utf8JsonWriter) evidence =
    writer.WriteStartObject()
    writer.WriteString("verdict", verdictText evidence.Verdict)
    writer.WriteString("reason", evidence.Reason)
    writer.WriteString("endpoint", evidence.Endpoint)
    writer.WriteNumber("pages", evidence.Pages)
    writer.WriteNumber("records", evidence.Records)
    writer.WriteBoolean("complete", evidence.Complete)
    writer.WriteStartArray("statuses"); evidence.Statuses |> List.iter writer.WriteNumberValue; writer.WriteEndArray()
    writer.WriteStartArray("responseSha256"); evidence.ResponseSha256 |> List.iter writer.WriteStringValue; writer.WriteEndArray()
    match evidence.FailureDiagnostic with
    | Some diagnostic -> writer.WritePropertyName("failureDiagnostic"); failureDiagnosticJson writer diagnostic
    | None -> ()
    writer.WriteEndObject()


let receiptJson (sourceSha:string) (workflowSha:string) (runId:string) (version:string) (planHash:string) (census: Census) =
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    writer.WriteStartObject()
    writer.WriteString("schema", "fsgg.rendering.package-census/v1")
    writer.WriteString("sourceSha", sourceSha); writer.WriteString("workflowSha", workflowSha)
    writer.WriteString("runId", runId); writer.WriteString("version", version); writer.WriteString("planSha256", planHash)
    writer.WriteString("startedUtc", census.StartedUtc); writer.WriteString("endedUtc", census.EndedUtc)
    writer.WriteString("overall", verdictText census.Overall)
    writer.WriteString("principal", "FS-GG/FS.GG.Rendering Actions GITHUB_TOKEN")
    writer.WriteString("scope", "read census only; effective writer grants and NuGet policy scopes UNKNOWN")
    writer.WriteString("mutation", "none")
    writer.WriteStartArray("selectedRoster"); census.Selected |> List.iter writer.WriteStringValue; writer.WriteEndArray()
    writer.WritePropertyName("organizationReadableMembership"); evidenceJson writer census.Organization
    writer.WriteStartArray("packages")
    for id, evidence in census.Packages do
        writer.WriteStartObject(); writer.WriteString("id",id); writer.WritePropertyName("githubPackages"); evidenceJson writer evidence; writer.WriteEndObject()
    writer.WriteEndArray(); writer.WriteEndObject(); writer.Flush()
    Encoding.UTF8.GetString(stream.ToArray()) + "\n"
