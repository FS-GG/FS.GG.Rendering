// #752 — DO THE PACKED PACKAGES CARRY THEIR SIGNATURE FILES?
//
// `template/base/docs/api-surface/**` is to be GENERATED from the pinned package (#694's epic), so the
// pinned package has to contain the `.fsi`. `Directory.Build.local.props` packs them under `api-surface/`;
// this script is what proves it, and it is called from BOTH lanes that pack:
//
//   * `packaged-consumer.yml` — on every PR that touches a root build file or `src/`, so a regression is
//     caught at review time;
//   * `release.yml` — immediately before `dotnet nuget push`, because that is the only moment that matters.
//     A published package cannot be un-published, and the generator's input is whatever the release shipped.
//
// ONE definition, two callers, no drift — the #661 rule. The first cut of this check was forty lines of
// shell inlined in `packaged-consumer.yml`, which meant `release.yml` — the workflow that actually PUBLISHES
// — had no check at all. The guard would have been green on every PR and absent at the one gate that could
// still prevent the damage.
//
// WHY IT COMPARES SETS, AND NOT COUNTS. The obvious check is "does the nupkg carry at least one .fsi". It
// fails open twice over, and both were live in the first cut:
//
//   * a PARTIAL pack passes. Widen the `Exclude`, or grow a subdirectory the glob misses, and Controls packs
//     30 of its 44 `.fsi` — non-zero, green, and the generator later emits a silently incomplete mirror.
//   * a CORRUPTED PATH passes, unless you happen to guess its shape. `PackagePath="api-surface/%(RecursiveDir)"`
//     — a trailing slash — reads to NuGet as a DIRECTORY, so it appends the file's own recursive path a second
//     time and `Widgets/Buttons.fsi` lands at `api-surface/Widgets/Widgets/Buttons.fsi`. A hand-written
//     "no segment repeats itself" assertion catches THAT shape at one level of nesting and misses it at two
//     (`Host/Gl/Host/Gl/…` repeats no adjacent segment).
//
// Exact set equality has no shape to guess. The expected set is "the `.fsi` this project actually has, at
// the paths it has them", so ANY difference — a file missing, a file added, a path mangled at any depth — is
// a difference. There is nothing left to be clever about.
//
// AND IT NEEDS NO HARDCODED UMBRELLA. The obvious special case is "except FS.GG.UI, the BOM, which has no
// .fsi" — a fourth hand-mirrored copy of `PackageFeed.UmbrellaPackageId` (see the header of
// tests/Package.Tests/PackablePackages.fs, and #674, for what the previous three cost). It is unnecessary:
// `src/Meta/FS.GG.UI.fsproj` HAS no `.fsi`, so its expected set is empty, so an empty `api-surface/` is
// exactly what this script demands of it. The rule states itself.
//
// FAILS CLOSED ON AN ABSENT SUBJECT (#266/#606). No feed, no nupkgs, or a discovered package with no nupkg
// in the feed is a FINDING — not a vacuous pass. A guard whose subject can silently vanish is the failure
// this repo keeps re-learning.

open System
open System.IO
open System.IO.Compression
open System.Text.RegularExpressions

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))

let feedDir =
    let argv = fsi.CommandLineArgs |> Array.toList

    let rec find args =
        match args with
        | "--feed" :: value :: _ -> Some value
        | _ :: rest -> find rest
        | [] -> None

    match find argv with
    | Some value -> Path.GetFullPath value
    | None ->
        eprintfn "usage: dotnet fsi scripts/check-packed-api-surface.fsx --feed <dir-of-nupkgs>"
        exit 2

/// The packable set, read as TEXT from the project files: declares a `<PackageId>`, and is not explicitly
/// `<IsPackable>false</IsPackable>`.
///
/// THE SAME RULE THE ItemGroup IN `Directory.Build.local.props` USES, and it has to be — this script's whole
/// job is to check that ItemGroup's output, so a project the ItemGroup skips and this script also skips is a
/// project nobody checks. `IsPackable` DEFAULTS to true (the SDK applies the default in its .targets, after
/// evaluation), so keying either half on an explicit `true` would silently drop a project that is packable by
/// default: it would pack with no `.fsi`, and be excused here for the same reason it was skipped there. Two
/// copies of a rule agreeing *including when they are both wrong* is the #661 hazard; keying both on the SDK's
/// real default is what stops them being able to agree wrongly.
///
/// `src/ColorPolicy` falls out by the rule itself rather than by a special case: it declares no `<PackageId>`
/// and is `<IsPackable>false</IsPackable>`, so it ships no package — and a package that does not exist cannot
/// be missing its `.fsi`.
let discovered =
    Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun project ->
        let text = File.ReadAllText project
        let packageId = Regex.Match(text, @"<PackageId>\s*([^<\s]+)\s*</PackageId>")

        let notPackable =
            Regex.IsMatch(text, @"<IsPackable>\s*false\s*</IsPackable>", RegexOptions.IgnoreCase)

        if packageId.Success && not notPackable then
            Some(packageId.Groups.[1].Value, Path.GetDirectoryName project)
        else
            None)
    |> Array.sortBy fst
    |> Array.toList

if List.isEmpty discovered then
    eprintfn
        "::error::no packable project found under src/ — this check's SUBJECT does not exist, so nothing was \
         proved. That is a discovery regression (#674), not a repository without packages. NOT a pass (#266)."

    exit 1

/// The `.fsi` a project has, as forward-slashed paths relative to the project directory — which is exactly
/// what `PackagePath="api-surface/%(RecursiveDir)%(Filename)%(Extension)"` is supposed to produce.
let expectedFsi (projectDir: string) =
    Directory.GetFiles(projectDir, "*.fsi", SearchOption.AllDirectories)
    |> Array.filter (fun f ->
        let rel = Path.GetRelativePath(projectDir, f).Replace('\\', '/')
        not (rel.StartsWith("bin/", StringComparison.Ordinal) || rel.StartsWith("obj/", StringComparison.Ordinal)))
    |> Array.map (fun f -> Path.GetRelativePath(projectDir, f).Replace('\\', '/'))
    |> Set.ofArray

/// What the nupkg actually carries under `api-surface/`.
let packedFsi (nupkg: string) =
    use archive = ZipFile.OpenRead nupkg

    let prefix = "api-surface/"

    archive.Entries
    |> Seq.map (fun entry -> entry.FullName)
    |> Seq.filter (fun name -> name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".fsi", StringComparison.Ordinal))
    |> Seq.map (fun name -> name.Substring prefix.Length)
    |> Set.ofSeq

if not (Directory.Exists feedDir) then
    eprintfn $"::error::feed directory does not exist: {feedDir} — nothing to check, so nothing was proved. NOT a pass (#266)."
    exit 1

let nupkgs = Directory.GetFiles(feedDir, "*.nupkg") |> Array.toList

if List.isEmpty nupkgs then
    eprintfn $"::error::no .nupkg in {feedDir} — this check's subject does not exist, so nothing was proved. NOT a pass (#266)."
    exit 1

/// nupkg -> its package id, read from the NUSPEC (authoritative: it is what actually shipped). The filename
/// is `<id>.<version>` with no separator to split on, so parsing it means guessing where the id ends.
let idOf (nupkg: string) =
    use archive = ZipFile.OpenRead nupkg

    archive.Entries
    |> Seq.tryFind (fun e -> e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && not (e.FullName.Contains '/'))
    |> Option.bind (fun entry ->
        use reader = new StreamReader(entry.Open())
        let m = Regex.Match(reader.ReadToEnd(), @"<id>\s*([^<\s]+)\s*</id>")
        if m.Success then Some m.Groups.[1].Value else None)

let byId =
    nupkgs
    |> List.choose (fun nupkg -> idOf nupkg |> Option.map (fun id -> id, nupkg))
    |> Map.ofList

let mutable failures = 0

for (packageId, projectDir) in discovered do
    let expected = expectedFsi projectDir

    match Map.tryFind packageId byId with
    | None ->
        // A discovered package with no .nupkg in the feed. In `release.yml` this is the difference between
        // "we checked what we are about to publish" and "we checked whatever happened to be lying around".
        eprintfn
            $"::error::{packageId} is packable but has NO .nupkg in the feed — this check cannot have proved \
              anything about it. A package that leaves the subject set does not fail, it stops being EXAMINED (#674)."

        failures <- failures + 1

    | Some nupkg ->
        let packed = packedFsi nupkg
        let missing = Set.difference expected packed
        let unexpected = Set.difference packed expected

        if Set.isEmpty missing && Set.isEmpty unexpected then
            if Set.isEmpty expected then
                printfn $"  ok    {packageId} — no .fsi in source, none packed"
            else
                printfn $"  ok    {packageId} — {Set.count expected} .fsi"
        else
            eprintfn $"::error::{packageId} — api-surface/ in the nupkg does not match the project's .fsi."

            for f in missing do
                eprintfn $"           MISSING from the package: api-surface/{f}"

            for f in unexpected do
                eprintfn
                    $"           PACKED but not in the project at that path: api-surface/{f}  \
                      (a mangled PackagePath lands here — e.g. the %%(RecursiveDir) doubling that put \
                      Widgets/Buttons.fsi at api-surface/Widgets/Widgets/Buttons.fsi)"

            failures <- failures + 1

// A nupkg in the feed that maps to no packable src project is REPORTED, never silently skipped. In the
// release lane the feed also carries FS.GG.UI.Template (packed from .template.package/, no .fsi of its own),
// and "unrecognised, therefore fine" is precisely how a package slips out of a guard's subject set.
let unmatched =
    byId
    |> Map.toList
    |> List.map fst
    |> List.filter (fun id -> discovered |> List.forall (fun (packageId, _) -> packageId <> id))

for id in unmatched do
    printfn $"  --    {id} — in the feed, not a packable src/ project (no .fsi expected of it); not judged"

if failures > 0 then
    eprintfn
        $"\n{failures} package(s) do not carry their signature files as packed. `template/base/docs/api-surface/**` \
          is generated FROM THE PIN (#694/#752), so a package without its .fsi is a package the mirror cannot be \
          generated from. Check the `api-surface` ItemGroup in Directory.Build.local.props."

    exit 1

printfn $"\napi-surface: all {List.length discovered} packable packages carry their .fsi, exactly as the source has them."
