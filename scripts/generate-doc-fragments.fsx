// M4-Rendering / M6 (FS-GG/.github#1313) — render the count/version-bearing front-door doc
// fragments FROM SOURCE, so they cannot be hand-typed and drift (issue FS.GG.Rendering#966,
// decision #968 option 2).
//
// THE ANTI-PATTERN THIS CLOSES. The FS.GG.UI framework version and the "N libraries plus the BOM"
// count were hand-typed into README.md / docs/usage.md prose. Feature 242
// (tests/Build.Tests/Feature242DocsCurrencyTests.fs) is the gate that catches them drifting from
// what the repo ships — but a *human* still had to re-type the literal on every bump, and M2
// (the consumer-README standard, .github#1311) forbids hand-typed counts/versions in prose.
//
// This generator emits those two facts, from the SAME sources Feature 242 reads:
//   * framework version — <FsGgUiVersion> in template/base/Directory.Packages.props (the pin the
//     release publishes and the template hands a scaffolded product).
//   * library count      — the slnx's packable src projects, minus the `FS.GG.UI` BOM metapackage.
// into named <!-- BEGIN/END GENERATED: fsgg-doc:<name> --> regions in the front-door docs. A
// region is a BUILD OUTPUT: do not hand-edit it — edit the source and regenerate.
//
// Feature 242 (re-aimed by #966) now validates the *generated fragment* — the version/count live
// inside a generated region and equal source, and no version/count is stated OUTSIDE a region. This
// script is the other half of the M6 pattern: a generate step plus a `--check` gate.
//
//   dotnet fsi scripts/generate-doc-fragments.fsx           # rewrite every region in place
//   dotnet fsi scripts/generate-doc-fragments.fsx --check   # verify they are current; exit 1 on drift (the GATE)

open System.IO
open System.Text.RegularExpressions

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let repoPath (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let argv = fsi.CommandLineArgs |> Array.toList |> List.tail
let checkOnly = argv |> List.contains "--check"

// ---- the two facts, from their sources of truth --------------------------------------------------

// The FS.GG.UI framework version — the pin the release publishes (Feature 242's F-DOCS-1 source).
let fsGgUiVersion =
    let props = File.ReadAllText(repoPath "template/base/Directory.Packages.props")
    let m = Regex.Match(props, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")
    if m.Success then m.Groups.[1].Value.Trim()
    else failwith "<FsGgUiVersion> not found in template/base/Directory.Packages.props"

// The library count — every packable src project in the slnx EXCEPT the `FS.GG.UI` BOM metapackage,
// derived exactly as Feature 242's F-DOCS-2 derives it (not a frozen literal).
let private bomPackageId = "FS.GG.UI"

let libraryCount =
    let slnx = File.ReadAllText(repoPath "FS.GG.Rendering.slnx")
    let packableIds =
        Regex.Matches(slnx, "Path=\"([^\"]+\\.fsproj)\"")
        |> Seq.map (fun m -> m.Groups.[1].Value.Replace('\\', '/'))
        |> Seq.filter (fun p -> p.StartsWith("src/") && p.EndsWith(".fsproj"))
        |> Seq.choose (fun rel ->
            let text = File.ReadAllText(repoPath rel)
            if not (text.Contains "<IsPackable>true</IsPackable>") then None
            else
                let m = Regex.Match(text, "<PackageId>([^<]+)</PackageId>")
                if m.Success then Some(m.Groups.[1].Value.Trim())
                else failwithf "packable project %s declares no <PackageId>" rel)
        |> Set.ofSeq
    packableIds |> Set.remove bomPackageId |> Set.count

// ---- the regions -------------------------------------------------------------------------------
// Each entry: the doc, the region name, and the body rendered between its markers. The body is the
// text Feature 242's currency regexes read — keep the "framework version `X`" and
// "N libraries plus the `FS.GG.UI` BOM" phrasings so the re-aimed gate matches.

let targets : (string * (string * string) list) list =
    [ "README.md",
      [ ("consume-coordinates",
         sprintf "Published as `FS.GG.UI.*` packages on `net10.0` — %d libraries plus the `FS.GG.UI` BOM metapackage (current framework version `%s`)."
            libraryCount fsGgUiVersion) ]
      "docs/usage.md",
      [ ("package-coordinates",
         sprintf "The libraries are published as `FS.GG.UI.*` packages targeting `net10.0` — current framework version `%s`."
            fsGgUiVersion)
        ("library-count",
         sprintf "All %d libraries plus the `FS.GG.UI` BOM metapackage (see [module map](product/module-map.md) for the owning source module of each):"
            libraryCount) ] ]

/// Replace the interior of the named region, keeping the BEGIN/END marker lines exactly.
/// Fails loud if the region's markers are absent (a moved/renamed region must not silently no-op).
let applyRegion (text: string) (name: string) (body: string) =
    let pattern =
        sprintf "(<!-- BEGIN GENERATED: fsgg-doc:%s[^\\n]*-->)[\\s\\S]*?(<!-- END GENERATED: fsgg-doc:%s -->)"
            (Regex.Escape name) (Regex.Escape name)
    let rx = Regex(pattern)
    if not (rx.IsMatch text) then
        failwithf "region 'fsgg-doc:%s' markers not found in the target doc — cannot generate into it" name
    rx.Replace(text, MatchEvaluator(fun m -> m.Groups.[1].Value + "\n" + body + "\n" + m.Groups.[2].Value))

// ---- run ---------------------------------------------------------------------------------------

let mutable drift = 0
for (rel, regions) in targets do
    let path = repoPath rel
    let original = File.ReadAllText path
    let updated = regions |> List.fold (fun acc (name, body) -> applyRegion acc name body) original
    if updated <> original then
        if checkOnly then
            eprintfn "::error::doc-fragment drift in %s — it is a BUILD OUTPUT; run: dotnet fsi scripts/generate-doc-fragments.fsx and commit" rel
            drift <- drift + 1
        else
            File.WriteAllText(path, updated)
            printfn "regenerated %s" rel
    else
        printfn "%s — generated doc fragments up to date" rel

if checkOnly && drift > 0 then
    eprintfn "::error::%d doc(s) carry stale generated fragments (version=%s, libraries=%d)" drift fsGgUiVersion libraryCount
    exit 1
