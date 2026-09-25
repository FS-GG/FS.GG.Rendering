// Provisional, source-only plan over #1335's descriptor-captured bytes.
// This owns byte copies but performs no staging, packing, or receiver write.
#load "SkillStagingPhysical.fsx"

open System
open System.Collections.Generic
open System.Text.Json
open SkillStagingPolicy
open SkillStagingPhysical

type CopyCandidate = { Destination: string; SourceMode: uint32; Bytes: byte[] }

type PlannedFile internal (destination: string, sourceMode: uint32, digest: string, bytes: byte[]) =
    let snapshot = Array.copy bytes
    member _.Destination = destination
    member _.SourceMode = sourceMode
    member _.CanonicalDigest = digest
    member _.Bytes = Array.copy snapshot

type StagePlan internal (manifestBytes: byte[], files: PlannedFile list, productCount: int) =
    let manifestSnapshot = Array.copy manifestBytes
    member _.ManifestBytes = Array.copy manifestSnapshot
    member _.Files = files
    member _.ProductCount = productCount

type private ProductRow = {
    Id: string
    SuppliedBy: string
    BodyDigest: string
    Files: DeclaredFile list
}

let private parseRows (raw: byte[]) : Result<ProductRow list, string> =
    try
        use document = JsonDocument.Parse(ReadOnlyMemory<byte>(raw))
        let rec unique (item: JsonElement) =
            match item.ValueKind with
            | JsonValueKind.Object ->
                let seen = HashSet<string>(StringComparer.Ordinal)
                for property in item.EnumerateObject() do
                    if not (seen.Add property.Name) then
                        raise (JsonException("duplicate JSON property: " + property.Name))
                    unique property.Value
            | JsonValueKind.Array ->
                for child in item.EnumerateArray() do unique child
            | _ -> ()
        unique document.RootElement
        let root = document.RootElement
        if root.GetProperty("schemaVersion").GetInt32() <> 2 then Error "manifest-invalid"
        else
            let rows =
                root.GetProperty("skills").EnumerateArray()
                |> Seq.filter (fun row -> row.GetProperty("scope").GetString() = "product")
                |> Seq.map (fun row ->
                    { Id = row.GetProperty("id").GetString()
                      SuppliedBy = row.GetProperty("supplied-by").GetString()
                      BodyDigest = row.GetProperty("sha256").GetString()
                      Files =
                        row.GetProperty("files").EnumerateArray()
                        |> Seq.map (fun file ->
                            { Path = file.GetProperty("path").GetString()
                              Sha256 = file.GetProperty("sha256").GetString() })
                        |> Seq.toList })
                |> Seq.toList
            if List.isEmpty rows then Error "manifest-empty-product-set" else Ok rows
    with
    | :? JsonException
    | :? InvalidOperationException
    | :? KeyNotFoundException
    | :? ArgumentException
    | :? NullReferenceException -> Error "manifest-invalid"

let private validId (id: string) =
    let letter c = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
    let digit c = c >= '0' && c <= '9'
    not (String.IsNullOrEmpty id)
    && (letter id.[0] || digit id.[0])
    && id |> Seq.forall (fun c -> letter c || digit c || c = '-' || c = '_' || c = '.')

let private sameContent (left: PlannedFile) (right: PlannedFile) =
    left.Destination = right.Destination
    && left.SourceMode = right.SourceMode
    && left.CanonicalDigest = right.CanonicalDigest
    && left.Bytes = right.Bytes

/// Capture every product row, validate its closed bytes against the manifest,
/// then own the exact bytes and source modes a later copier would need.
let prepareCurrent repositoryRoot (manifestBytes: byte[]) : Result<StagePlan, string> =
    let manifestSnapshot = if isNull manifestBytes then null else Array.copy manifestBytes
    match parseRows manifestSnapshot with
    | Error issue -> Error issue
    | Ok rows ->
        let seenIds = HashSet<string>(StringComparer.OrdinalIgnoreCase)
        let seenDestinations = HashSet<string>(StringComparer.OrdinalIgnoreCase)
        let rec collect (accumulated: PlannedFile list) remaining =
            match remaining with
            | [] ->
                accumulated
                |> List.sortWith (fun left right ->
                    StringComparer.Ordinal.Compare(left.Destination, right.Destination))
                |> fun files -> Ok(StagePlan(manifestSnapshot, files, rows.Length))
            | row :: tail ->
                if not (validId row.Id) then Error "skill-id-invalid"
                elif not (seenIds.Add row.Id) then Error "skill-id-alias"
                else
                    match captureSnapshot repositoryRoot row.SuppliedBy row.BodyDigest row.Files with
                    | Error issue -> Error issue
                    | Ok captured ->
                        let declaredPaths = row.Files |> List.map _.Path |> Set.ofList
                        let capturedPaths = captured |> List.map _.Path |> Set.ofList
                        if declaredPaths <> capturedPaths then Error "declared-path-noncanonical"
                        else
                            let byPath = row.Files |> List.map (fun file -> file.Path, file.Sha256) |> Map.ofList
                            let projected =
                                captured |> List.map (fun file ->
                                    let destination = $"skills/{row.Id}/{file.Path}"
                                    PlannedFile(destination, file.Mode, byPath.[file.Path], file.Bytes))
                            if projected |> List.exists (fun file -> not (seenDestinations.Add file.Destination)) then
                                Error "copy-path-alias"
                            else collect (projected @ accumulated) tail
        collect [] rows

/// Check that an independently proposed copy projection uses these paths,
/// captured modes, and exact raw bytes. It does not inspect a staged receiver.
let verifyProjection (plan: StagePlan) (proposed: CopyCandidate list) : Result<unit, string> =
    let sort rows = rows |> List.sortWith (fun left right ->
        StringComparer.Ordinal.Compare(left.Destination, right.Destination))
    let expected = plan.Files
    let actual = sort proposed
    if expected.Length <> actual.Length then Error "copy-path-mismatch"
    else
        List.zip expected actual
        |> List.tryPick (fun (want, got) ->
            if got.Destination <> want.Destination then Some "copy-path-mismatch"
            elif got.SourceMode <> want.SourceMode then Some "copy-mode-mismatch"
            elif got.Bytes <> want.Bytes then Some "copy-bytes-mismatch"
            else None)
        |> function Some issue -> Error issue | None -> Ok ()

/// A later planner can refuse a stale manifest or changed source before use.
/// This is a fresh observation, not a reservation or atomic transaction.
let verifyCurrent (plan: StagePlan) repositoryRoot (currentManifestBytes: byte[]) : Result<unit, string> =
    let currentSnapshot = if isNull currentManifestBytes then null else Array.copy currentManifestBytes
    if plan.ManifestBytes <> currentSnapshot then Error "stale-plan"
    else
        match prepareCurrent repositoryRoot currentSnapshot with
        | Error issue -> Error issue
        | Ok current ->
            if plan.ProductCount <> current.ProductCount
               || plan.Files.Length <> current.Files.Length then Error "stale-plan"
            elif List.forall2 sameContent plan.Files current.Files then Ok ()
            else Error "source-changed"
