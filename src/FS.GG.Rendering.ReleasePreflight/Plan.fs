namespace FS.GG.Rendering.ReleasePreflight

open System
open System.Collections.Generic
open System.Globalization
open System.Numerics
open System.Text.Json
open System.Text.RegularExpressions

/// Already enumerated packable project identity at an exact source commit.
type SourcePackage = { Id: string; Kind: string }

/// Pure plan checks. The caller owns git enumeration, feed status and receipt writes.
module Plan =
    let private stableVersion = Regex("^([0-9]+)\\.([0-9]+)\\.([0-9]+)$", RegexOptions.CultureInvariant)
    let private packageId = Regex("^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant)
    let private kinds = set [ "library"; "bom"; "template" ]

    let private versionParts value =
        if String.IsNullOrEmpty value then None
        else
            let matched = stableVersion.Match value
            if not matched.Success then None
            else
                Some [ for index in 1 .. 3 -> BigInteger.Parse(matched.Groups.[index].Value, CultureInfo.InvariantCulture) ]

    let private property (name: string) (item: JsonElement) =
        let mutable result = Unchecked.defaultof<JsonElement>
        if item.ValueKind = JsonValueKind.Object && item.TryGetProperty(name, &result) then Some result
        else None

    let private stringProperty name item =
        match property name item with
        | Some value when value.ValueKind = JsonValueKind.String -> Option.ofObj (value.GetString())
        | _ -> None

    let private duplicateProperties (root: JsonElement) =
        let findings = ResizeArray<string>()
        let rec inspect path (value: JsonElement) =
            match value.ValueKind with
            | JsonValueKind.Object ->
                let names = HashSet<string>(StringComparer.Ordinal)
                for item in value.EnumerateObject() do
                    let child = path + "." + item.Name
                    if not (names.Add item.Name) then findings.Add("duplicate-json:" + child)
                    inspect child item.Value
            | JsonValueKind.Array ->
                value.EnumerateArray() |> Seq.iteri (fun index item -> inspect ($"{path}[{index}]") item)
            | _ -> ()
        inspect "$" root
        List.ofSeq findings

    let private validateSource (source: SourcePackage list) =
        let findings = ResizeArray<string>()
        if source.Length <> 19 then findings.Add("source-count")
        let folded = HashSet<string>(StringComparer.OrdinalIgnoreCase)
        for item in source do
            if String.IsNullOrWhiteSpace item.Id || not (packageId.IsMatch item.Id) then findings.Add("source-id")
            elif not (folded.Add item.Id) then findings.Add("source-duplicate:" + item.Id)
            if not (kinds.Contains item.Kind) then findings.Add("source-kind:" + item.Id)
        if source |> List.filter (fun item -> item.Kind = "library") |> List.length <> 17 then findings.Add("source-library-count")
        if source |> List.filter (fun item -> item.Kind = "bom") |> List.map _.Id <> [ "FS.GG.UI" ] then findings.Add("source-bom")
        if source |> List.filter (fun item -> item.Kind = "template") |> List.map _.Id <> [ "FS.GG.UI.Template" ] then findings.Add("source-template")
        List.ofSeq findings

    /// Inspect raw plan JSON against independently enumerated source facts and a requested target.
    /// Findings are stable codes; no network, archive, filesystem or release mutation is performed.
    let inspect (source: SourcePackage list) (target: string) (raw: string) : string list =
        let findings = ResizeArray<string>()
        for issue in validateSource source do findings.Add issue
        try
            use document = JsonDocument.Parse raw
            let root = document.RootElement
            for issue in duplicateProperties root do findings.Add issue
            if root.ValueKind <> JsonValueKind.Object then findings.Add("plan-shape")
            else
                if stringProperty "schema" root <> Some "fsgg.rendering.release-plan/v1" then
                    findings.Add("plan-schema")
                let version = stringProperty "version" root
                let baseline = stringProperty "baselineVersion" root
                if version <> Some target then findings.Add("plan-version")
                match versionParts target, baseline |> Option.bind versionParts with
                | Some targetParts, Some baselineParts when baselineParts < targetParts -> ()
                | _ -> findings.Add("baseline-order")

                let expectedTags =
                    [ $"fs-gg-ui/v{target}"; $"fs-gg-ui-template/v{target}"; $"v{target}" ]
                    |> List.map Some
                let tags =
                    match property "tags" root with
                    | Some value when value.ValueKind = JsonValueKind.Array ->
                        value.EnumerateArray()
                        |> Seq.map (fun item ->
                            if item.ValueKind = JsonValueKind.String then Option.ofObj (item.GetString())
                            else None)
                        |> Seq.toList
                    | _ -> []
                if tags <> expectedTags then findings.Add("plan-tags")

                let rows =
                    match property "packages" root with
                    | Some value when value.ValueKind = JsonValueKind.Array ->
                        value.EnumerateArray()
                        |> Seq.mapi (fun index item ->
                            match stringProperty "id" item, stringProperty "kind" item with
                            | Some id, Some kind -> Some(id, kind)
                            | _ -> findings.Add($"package-shape:{index}"); None)
                        |> Seq.choose id
                        |> Seq.toList
                    | _ -> findings.Add("packages-shape"); []
                if rows.Length <> 19 then findings.Add("package-count")
                let folded = HashSet<string>(StringComparer.OrdinalIgnoreCase)
                for (id, kind) in rows do
                    if not (packageId.IsMatch id) then findings.Add("package-id:" + id)
                    elif not (folded.Add id) then findings.Add("package-duplicate:" + id)
                    if not (kinds.Contains kind) then findings.Add("package-kind:" + id)

                let expected = source |> List.map (fun item -> item.Id, item.Kind) |> Map.ofList
                let actual = rows |> Map.ofList
                let expectedIds = expected |> Map.keys |> Set.ofSeq
                let actualIds = actual |> Map.keys |> Set.ofSeq
                if expectedIds <> actualIds then findings.Add("package-roster")
                for id in Set.intersect expectedIds actualIds do
                    if expected.[id] <> actual.[id] then findings.Add("package-kind-mismatch:" + id)
        with :? JsonException -> findings.Add("json-invalid")
        findings |> Seq.distinct |> Seq.sortWith (fun left right -> StringComparer.Ordinal.Compare(left, right)) |> Seq.toList
