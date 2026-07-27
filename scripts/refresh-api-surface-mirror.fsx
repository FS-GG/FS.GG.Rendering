// #752 / #694 — GENERATE `template/base/docs/api-surface/**` FROM THE PINNED PACKAGE.
//
// Every mirrored `.fsi` used to carry a header saying "regenerate when $(FsGgUiVersion) moves", and no
// such regenerator existed — it was asked for and never written (#694). This is it. After this, the
// mirror is not an authored file: it is emitted from the signature files the PINNED packages ship, plus
// the curation this repo owns, and `gate.yml` fails on any diff.
//
// ---------------------------------------------------------------------------------------------
// WHY THE PIN, AND NOT `src/`
// ---------------------------------------------------------------------------------------------
//
// The mirror teaches what a SCAFFOLDED PRODUCT restores, and a scaffolded product restores the pin. For
// the whole window between a feature landing on `main` and the release that publishes it, `src/` is
// AHEAD of the pin — which was the entire reason `tests/Package.Tests/mirror-pending-release-ledger.txt`
// existed. Generating from the pin closes that window by construction: the mirror cannot name a symbol
// the pin does not export. #753 has since deleted `M-MIR/TYPE` and that ledger with it.
//
// Be precise about WHY they went, because this comment used to claim the narrower thing: generating from
// the pin does NOT make `M-MIR/TYPE` stop contradicting #550's doc-vs-pin rule. It closes only ONE
// direction of it (the mirror can no longer teach a member the pin cannot bind). `M-MIR/TYPE` was an
// EQUALITY rule (mirror == src, member for member), so the OTHER direction got WORSE: the mirror now
// tracks the pin, so any type that grows a case on `main` makes the mirror legitimately OMIT it — and an
// equality rule reds on that, needing a ledger entry. Kept as-is, `M-MIR/TYPE` would have needed the
// ledger MORE than before, not less. Deleting the rule is what retired the ledger; the generator is what
// made the rule pointless (it compared this script's OUTPUT against `src/`, one link up its own input
// chain: `src/` -> nupkg -> mirror).
//
// ---------------------------------------------------------------------------------------------
// WHY IT READS `.fsi` AND NOT METADATA — the item's plan, and why it was not followed
// ---------------------------------------------------------------------------------------------
//
// #752 specified lifting the `MetadataReader` walk out of `TemplateConsumesPinnedApiTests.fs` and
// generating from IL. That is a category error, not a gap: a metadata walk yields NAMES, an `.fsi` needs
// SIGNATURES. F# type abbreviations (`type CommandId = string`) compile to literally nothing in IL and
// the mirror declares five, so no metadata reader can ever recover them. #782 landed the alternative —
// every packable project packs its own `.fsi` under `api-surface/` in the nupkg — and this reads those.
// Faithful by construction: it IS the signature file, as the compiler checked it.
//
// So the Build.Tests metadata walk STAYS. #752 asked for "exactly one PE/metadata walk in the tree" and
// that is already true; this adds none. The walk answers a different question (what does the assembly
// EXPORT — the doc-vs-pin oracle), and folding two questions into one reader is the #661 hazard rather
// than the cure for it.
//
// ---------------------------------------------------------------------------------------------
// WHY THERE IS A MANIFEST — "generated + curated inputs", and nothing less
// ---------------------------------------------------------------------------------------------
//
// The mirror is a CURATED TEACHING DOCUMENT, not a surface dump, and `ApiSurfaceMirrorTests.fs` says so
// in as many words: M-MIR is coherence (mirror => src) and "never completeness", because
// `docs/scaffold-map.md` sanctions the omissions. Measured against the 0.11.0/0.5.0/0.3.0 pins, the
// mirror declares 1,292 of the pin's 2,575 members — exactly half — and the curation is per-MEMBER at
// arbitrary depth (`module Paint`: 13 of the pin's 23 vals). A package can carry the signatures; it can
// never carry the decision about what to leave out. So the generator takes the pin PLUS
// `scripts/api-surface-manifest.txt`, which owns exactly the three things the pin cannot know:
//
//   * WHICH members are taught, and in what order, and merged into which of the 62 files (the pin ships
//     114 `.fsi`; the file layout is a source-tree fact a nupkg does not carry, and 79 things in this
//     repo reference the mirror's paths);
//   * the `// See skill:` pointer M-PTR requires — 0 of the packed `.fsi` carry one;
//   * the six doc blocks the pin is SILENT on (`KeyboardEffect`'s "Issue 456: NOT INTERPRETED BY ANY
//     HOST" warning is one: it teaches a union case the pin leaves bare).
//
// Everything else is copied verbatim from the pin, PROSE INCLUDED, so a signature and its documentation
// can no longer drift from the package a reader restores. That is #694's whole point: the manifest names
// symbols, the PIN provides their signatures — and `scaffold-map.md`'s "when they disagree, the `.fsi`
// wins" is what makes the pin, not the manifest, authoritative for the words as well.
//
// ---------------------------------------------------------------------------------------------
// FAILS CLOSED (#266/#606)
// ---------------------------------------------------------------------------------------------
//
// Every way this can fail to know something is an error, never an empty emit: a package that did not
// restore, a package that carries no `api-surface/` (it predates #782), a manifest line naming a member
// the pin does not export, a source `.fsi` with a line the parser cannot classify. A generator that
// silently emits less is indistinguishable from one whose subject shrank, and the gate would then
// enshrine the loss as the new expected output.

open System
open System.IO
open System.Diagnostics
open System.Text
open System.Text.RegularExpressions

#load "ApiSurfaceRestore.fs"
open FsGg

#load "lib/FsiSurface.fsx"
open FsiSurface

#load "ApiSurfaceCoverage.fs"
open FsGg.ApiSurface

#load "lib/ReleaseWindow.fsx"

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let manifestPath = Path.Combine(repoRoot, "scripts", "api-surface-manifest.txt")
let mirrorRoot = Path.Combine(repoRoot, "template", "base", "docs", "api-surface")
let pinsProps = Path.Combine(repoRoot, "template", "base", "Directory.Packages.props")

// #1101 — THE PRE-#782 BRIDGE IS GONE. There is no `legacyPre782Surfaces` map any more, and no
// `scripts/legacy-api-surfaces/`. Every package, FS.GG.Contracts included, is read from its own packed
// `api-surface/*.fsi` and from nowhere else. Do not reintroduce a per-version hand-copy escape hatch
// here; the paragraphs below are kept so the next reader knows what was removed and why it must not
// come back.
//
// What it was. Contracts 7.0.0 shipped the producer-owned performance-intent type before the package
// learned to carry `api-surface/*.fsi` (#782), so a narrow version-keyed map pointed
// ("FS.GG.Contracts", "7.0.0") at a hand-written copy of the producer's `Schemas.fsi`. The version key
// was a TRIPWIRE, not bookkeeping: the next Contracts bump would stop matching, so nobody could carry a
// hand-copy forward unnoticed.
//
// It fired, and it was stepped over. #1094 had to move `$(FsGgContractsVersion)` 7.0.0 -> 7.2.0 and
// added a second entry pointing at the same 7.0.0-era copy, on the measured basis that the one taught
// type (`Schemas.PerformanceIntentDeclaration`) had an identical fifteen-field shape in both. True at
// the time — and exactly the failure the tripwire existed to prevent, one step in.
//
// What it cost, measured. While an entry existed the generator never read that package's real surface,
// so it could not detect the taught type drifting: `--emit-waivers` at the 7.2.0 pin emitted 969 lines
// and not ONE of them was a Contracts member. The member-level COVERAGE rule had nothing to demand,
// because it only ever saw the hand-copy. Only the type/module-level completeness rule in
// tests/Build.Tests (which restores the real nupkg) still judged Contracts at all.
//
// Why it is safe to delete now. FS-GG/FS.GG.SDD#742 discharged #782's producer half: FS.GG.Contracts
// 7.4.0 is the first published version that packs `api-surface/` (six `.fsi`), on nuget.org — the feed
// this script actually restores from — and the packed files are the compiled signature files, not a
// copy that can drift. The pin above is 7.4.0. The normal package-owned surface path now has something
// to return to, so the bridge is dead code and is deleted with it.

let argv = fsi.CommandLineArgs |> Array.toList |> List.tail
let checkOnly = argv |> List.contains "--check"
// Print the pin's currently-untaught public members as `waive` lines and exit — the seed for, and the
// regeneration path of, the manifest's waiver block (#925). Emits nothing else so its output pastes clean.
let emitWaivers = argv |> List.contains "--emit-waivers"

let fail (msg: string) =
    eprintfn "refresh-api-surface-mirror: %s" msg
    exit 1

// ---------------------------------------------------------------------------------------------
// The pins — read from the props a scaffolded product actually restores.
// ---------------------------------------------------------------------------------------------

/// `PackageId -> version`, resolving the `$(FsGgUiVersion)` indirection the props deliberately uses.
/// Read as TEXT rather than by evaluating MSBuild: this is the file the template ships, the properties
/// are literals in it, and a `dotnet msbuild` round-trip here would need a project to evaluate against.
let pins =
    if not (File.Exists pinsProps) then
        fail $"no pins file at {pinsProps}"

    let text = File.ReadAllText pinsProps

    let props =
        Regex.Matches(text, @"<(?<k>FsGg[A-Za-z]*Version)>(?<v>[^<]+)</\k<k>>")
        |> Seq.map (fun m -> m.Groups.["k"].Value, m.Groups.["v"].Value)
        |> Map.ofSeq

    let resolve (v: string) =
        let m = Regex.Match(v, @"^\$\((?<p>[A-Za-z]+)\)$")

        if not m.Success then
            v
        else
            match props |> Map.tryFind m.Groups.["p"].Value with
            | Some x -> x
            | None -> fail $"pin property {v} is not defined in {pinsProps}"

    Regex.Matches(text, @"<PackageVersion\s+Include=""(?<id>FS\.GG\.[^""]+)""\s+Version=""(?<v>[^""]+)""")
    |> Seq.map (fun m -> m.Groups.["id"].Value, resolve m.Groups.["v"].Value)
    |> Seq.distinctBy fst
    |> Map.ofSeq

// ---------------------------------------------------------------------------------------------
// The release window — where the pin's `.fsi` come from `src/` because the feed cannot have them yet.
// ---------------------------------------------------------------------------------------------

/// The FS.GG.UI pins this commit BUMPS, mapped to the `src/` project that will publish them.
/// Empty on every commit that is not the release bump — which is the overwhelmingly common case, and
/// the one where `src/` is AHEAD of the pin and must NOT be read (that is the window #752 closed).
///
/// WHY `src/` IS THE RIGHT SOURCE HERE, AND ONLY HERE. `release.yml` packs this very commit —
/// `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version="$pin"` — so the `.fsi` in `src/` ARE the
/// `.fsi` that become `api-surface/` in the nupkg at the new pin. Inside the window the substitution
/// is not an approximation of the package: it is the same bytes, one step earlier in the chain
/// (`src/` -> nupkg -> mirror). So the mirror this commit checks in is byte-identical to the one the
/// published package generates the moment it lands, and the first post-release commit — which reads
/// the now-published pin — sees no drift. That is what keeps this from reintroducing the class where a
/// stale generated file reds the required gate and blocks the whole repo (#435, #477, #515).
///
/// FS.GG.Game.Core / FS.GG.Audio.* are NOT substituted at any time: they are released from their own
/// repos on their own axes ($(FsGgGameVersion)/$(FsGgAudioVersion)), this repo builds no project for
/// them, and a bump of those pins is a CONSUMER bump onto something already published. The feed is the
/// only honest source for them, and it can answer.
let srcBackedPins: Map<string, string> =
    let pinsRel = "template/base/Directory.Packages.props"

    let bumped =
        match ReleaseWindow.bumpedInCommitUnderTest repoRoot pinsRel "FsGgUiVersion" with
        | Ok b -> b
        | Error e -> fail e

    if not bumped then
        Map.empty
    else
        let projects = ReleaseWindow.packableProjects repoRoot

        pins
        |> Map.toList
        |> List.choose (fun (id, _) -> projects |> Map.tryFind id |> Option.map (fun dir -> id, dir))
        |> Map.ofList

// ---------------------------------------------------------------------------------------------
// Restore, and read the `api-surface/` each package carries.
// ---------------------------------------------------------------------------------------------

/// Probe-private packages folder, OUTSIDE any work dir, so a cold restore is paid once per machine.
/// It is deliberately NOT the machine's global packages folder: nothing this repo `pack`s locally may
/// ever satisfy this restore — only the feed can — and a published (id, version) is immutable, so
/// reuse is safe. This is the same isolation `TemplateConsumesPinnedApiTests` uses, for the same reason.
let probePackagesDir =
    Path.Combine(Path.GetTempPath(), "fsgg-api-surface-mirror-packages")

let restorePins () =
    let work =
        Path.Combine(Path.GetTempPath(), "fsgg-api-surface-mirror-" + Guid.NewGuid().ToString("N"))

    Directory.CreateDirectory work |> ignore

    try
        // Inside the release window the bumped pins are deliberately absent from this probe: the feed
        // cannot serve them yet, and asking it to is precisely the NU1102 that deadlocked the release.
        let refs =
            pins
            |> Map.toList
            |> List.filter (fun (id, _) -> not (srcBackedPins |> Map.containsKey id))
            |> List.map (fun (id, v) -> $"""    <PackageReference Include="{id}" Version="{v}" />""")
            |> String.concat "\n"

        // `ManagePackageVersionsCentrally=false` — this probe carries its own versions inline and must
        // not inherit this REPO's central pins, which are `src/`'s versions, not the template's.
        File.WriteAllText(
            Path.Combine(work, "probe.fsproj"),
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
{refs}
  </ItemGroup>
</Project>
"""
        )

        // Pass this path explicitly below. NuGet's implicit config discovery is filename/casing
        // sensitive on Linux; relying on it allowed an enclosing hostile package-source mapping to
        // win even though this probe had authored an isolated source list (#1069).
        let nugetConfigPath = Path.Combine(work, "NuGet.Config")

        File.WriteAllText(
            nugetConfigPath,
            """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"""
        )

        let psi =
            ApiSurfaceRestore.startInfo work "probe.fsproj" probePackagesDir nugetConfigPath

        use p = Process.Start psi
        let out = p.StandardOutput.ReadToEnd()
        let err = p.StandardError.ReadToEnd()

        // Bounded: an unbounded wait turns a feed outage into a hung gate rather than a red one.
        if not (p.WaitForExit(10 * 60 * 1000)) then
            (try p.Kill true with _ -> ())
            fail "restore of the pinned packages timed out after 10 minutes"

        if p.ExitCode <> 0 then
            fail $"restore of the pinned packages failed:\n{out}\n{err}"
    finally
        try Directory.Delete(work, true) with _ -> ()

// ---------------------------------------------------------------------------------------------
// #1101 AC3 — WHEN A PIN CARRIES NO `api-surface/`, SAY WHAT CAN ACTUALLY BE DONE ABOUT IT.
// ---------------------------------------------------------------------------------------------
//
// The old message was a single fixed sentence: "it predates #782. Bump the pin to a release that packs
// its .fsi." For FS.GG.Contracts that advice was UNFOLLOWABLE for months — 7.0.0, 7.1.0, 7.2.0 and
// 7.3.0 were every published version and not one of them packed `api-surface/`. A hard error that
// prescribes an impossible remedy costs the reader the whole investigation (open four nupkgs) before
// they can learn the remedy is not theirs to apply, and it is precisely what pushed #1094 into
// extending the hand-copy bridge instead. The diagnostic must distinguish the two cases, so it asks
// the feed rather than assuming:
//
//   * a packing release the pin can move UP to EXISTS -> name it, and the bump is genuinely the fix;
//   * no such release exists -> say so plainly. The remedy is the PRODUCER's (#782), a pin bump cannot
//     reach it, and hand-copying the surface is explicitly not the answer (#1101);
//   * the feed could not answer -> say that too, and still fail. Fail closed (#266/#606): "I could not
//     check" must never render as "no packing release exists".
//
// SCOPED TO "AT OR ABOVE THE PIN", which is the only scope that answers the reader's actual question.
// "Does any version anywhere pack it" is the wrong question twice over: a packing release BELOW the pin
// is not a remedy (nobody moves a pin backwards to fix a mirror), and scanning a package's whole
// version list is unbounded work for an answer the reader cannot act on. Candidates are therefore the
// stable versions >= the pinned one, newest-first — which for the motivating case (FS.GG.Contracts
// pinned at 7.2.0, nothing above it packing) is a ONE-element probe that returns a definitive "none".
//
// Bounded by construction: stable-only, >= the pin, at most `maxProbes` nupkgs, one short timeout. This
// runs ONLY on the failure path, so a healthy generate makes zero extra requests.

let private probeHttp =
    lazy (new Net.Http.HttpClient(Timeout = TimeSpan.FromSeconds 30.0))

/// Stable-release ordering. Prereleases are filtered out before this is reached, so the numeric core is
/// the whole comparison; a non-numeric segment sorts as 0 rather than throwing, because a diagnostic
/// helper must not turn one failure into a different, less informative one.
let private versionKey (v: string) =
    let part (s: string) =
        match Int32.TryParse s with
        | true, n -> n
        | _ -> 0

    let seg = v.Split('.')
    (part (Array.tryItem 0 seg |> Option.defaultValue "0"),
     part (Array.tryItem 1 seg |> Option.defaultValue "0"),
     part (Array.tryItem 2 seg |> Option.defaultValue "0"))

/// The newest published stable version of `id` **at or above `pinned`** that packs `api-surface/*.fsi`.
/// `Ok None` means the probe looked at every such version and none packs it — an earned "no". `Error`
/// carries the reason the feed could not answer, and is NEVER conflated with `Ok None`.
let packingReleaseAtOrAbove (id: string) (pinned: string) : Result<string option, string> =
    let maxProbes = 12

    try
        let http = probeHttp.Force()
        let indexUrl = $"https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/index.json"
        let resp = http.GetAsync(indexUrl).GetAwaiter().GetResult()

        if not resp.IsSuccessStatusCode then
            Error $"nuget.org returned {int resp.StatusCode} for {indexUrl}"
        else
            let body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            use doc = Text.Json.JsonDocument.Parse body

            let floor = versionKey pinned

            // Sorted here rather than trusting the feed's documented ascending order: the ordering is
            // load-bearing for "newest that packs", and re-deriving it costs nothing.
            let candidates =
                doc.RootElement.GetProperty("versions").EnumerateArray()
                |> Seq.map (fun e -> e.GetString())
                |> Seq.filter (fun v -> not (v.Contains "-") && versionKey v >= floor)
                |> Seq.sortByDescending versionKey
                |> Seq.toList

            let packs (v: string) =
                let url =
                    $"https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/{v}/{id.ToLowerInvariant()}.{v}.nupkg"

                let r = http.GetAsync(url).GetAwaiter().GetResult()

                if not r.IsSuccessStatusCode then
                    false
                else
                    let bytes = r.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
                    use ms = new MemoryStream(bytes)
                    use zip = new Compression.ZipArchive(ms, Compression.ZipArchiveMode.Read)

                    zip.Entries
                    |> Seq.exists (fun e ->
                        e.FullName.StartsWith("api-surface/", StringComparison.OrdinalIgnoreCase)
                        && e.FullName.EndsWith(".fsi", StringComparison.OrdinalIgnoreCase))

            match candidates |> List.truncate maxProbes |> List.tryFind packs with
            | Some v -> Ok(Some v)
            | None when candidates.Length > maxProbes ->
                // Did not look at every candidate, so "none" would be a claim the probe did not earn.
                Error
                    $"probed the newest {maxProbes} of {candidates.Length} stable versions at or above {pinned} and none packs api-surface/"
            | None -> Ok None
    with ex ->
        Error $"could not reach nuget.org: {ex.Message}"

/// The hard error for a pin that carries no `api-surface/`, with advice that is true for THIS package.
let noSurfaceFailure (id: string) (version: string) : string =
    let head = $"{id} {version} carries no api-surface/ — it predates #782."

    match packingReleaseAtOrAbove id version with
    | Ok(Some newest) when newest = version ->
        // The feed says this very version packs it, but the restored package does not carry it: the
        // restore, not the pin, is what is wrong. Never advise a bump to the version already pinned.
        $"{head} But nuget.org reports that {id} {newest} DOES pack api-surface/, so the restored copy \
under {probePackagesDir} is not what the feed serves — clear it and restore again rather than moving \
the pin."
    | Ok(Some newest) ->
        $"{head} {id} {newest} is the newest published stable release at or above the pin that DOES \
pack its .fsi — bump this package's pin in {pinsProps} to it."
    | Ok None ->
        $"{head} NO published stable release of {id} at or above {version} packs api-surface/, so \
BUMPING THE PIN CANNOT FIX THIS — there is nothing to bump to. The remedy belongs to the PRODUCING \
repository, which owes #782's half (pack $(Compile) filtered to .fsi under api-surface/); file it \
there and leave this pin alone. Do NOT hand-copy the surface into this repo instead: that bridge \
existed for FS.GG.Contracts, it blinded the member-level coverage rule for the whole time it was \
there, and #1101 deleted it."
    | Error why ->
        $"{head} Could not determine whether any published release of {id} packs its .fsi ({why}), so \
this cannot tell you whether bumping the pin is even possible — failing closed rather than guessing. \
Re-run when the feed is reachable, or open the published nupkgs by hand."

/// `PackageId -> (sourceFileName -> parsed nodes)`, read from the restored package's `api-surface/`.
///
/// An absent `api-surface/` is a HARD ERROR, not an empty map: it means the pinned version predates
/// #782 and carries no signature files, and the honest response is to say which package and which
/// version rather than emit a mirror with that package's members silently missing.
let loadPinSurface () =
    pins
    |> Map.toList
    |> List.map (fun (id, version) ->
        // In the window, this package's `api-surface/` does not exist to be read — it is packed from
        // `src/` by the release this commit performs — so read the same `.fsi` at their source paths.
        let sources =
            match srcBackedPins |> Map.tryFind id with
            | Some projectDir -> ReleaseWindow.projectFsi projectDir
            | None ->
                let dir =
                    Path.Combine(probePackagesDir, id.ToLowerInvariant(), version, "api-surface")

                let surfaceDir =
                    if Directory.Exists dir then dir else fail (noSurfaceFailure id version)

                Directory.GetFiles(surfaceDir, "*.fsi", SearchOption.AllDirectories)
                |> Array.toList
                |> List.map (fun f -> Path.GetRelativePath(surfaceDir, f).Replace('\\', '/'), f)

        let files =
            sources
            |> List.map (fun (rel, f) ->
                let nodes, unparsed = parseFsi (File.ReadAllLines f |> Array.toList)

                if not unparsed.IsEmpty then
                    fail $"{id}/{rel}: {unparsed.Length} line(s) the .fsi reader could not classify, first: {unparsed.Head.Trim()}"

                rel, nodes)
            |> Map.ofList

        if files.IsEmpty then
            fail $"{id} {version}: api-surface/ exists but carries no .fsi"

        id, files)
    |> Map.ofList

// ---------------------------------------------------------------------------------------------
// The manifest — the curation this repo owns.
// ---------------------------------------------------------------------------------------------

type Include =
    { Source: string // the pin's `.fsi`, e.g. "Types.fsi"
      Kind: string
      Path: string } // dotted path within that file

type Stanza =
    { File: string // mirror-relative, e.g. "Scene/Scene.fsi"
      Skill: string option
      Header: string list // verbatim extra header lines (the "Mirrored from" note)
      Namespace: string
      Package: string
      Opens: string list
      Includes: Include list
      /// (source, kind, path) -> the doc lead that REPLACES the pin's. The mirror-only prose.
      Prose: Map<string * string * string, string list>
      /// (source, kind, path) -> (anchor line, the doc block to splice in ABOVE it).
      ///
      /// A type is emitted VERBATIM — the mirror has never curated the inside of one, which is what
      /// makes that safe — so prose that teaches a single UNION CASE has nowhere to attach as a lead.
      /// 63 doc lines are exactly this: `KeyboardEffect`'s "Issue 456: NOT INTERPRETED BY ANY HOST"
      /// warning sits on the `| RequestHostKeyCapture` case and would otherwise be dropped on the
      /// floor by a generator that copies the pin's type text and nothing else.
      ///
      /// Anchored on the TEXT of the line it precedes rather than on a line number: a line number
      /// silently re-targets the moment the pin re-orders a case, and would splice a warning onto the
      /// wrong case — worse than losing it. An anchor that no longer matches is a hard error.
      BodyProse: Map<string * string * string, (string * string list) list> }

let parseManifest (path: string) : Stanza list * Coverage.MemberKey list =
    if not (File.Exists path) then
        fail $"no manifest at {path}"

    let lines = File.ReadAllLines path
    let stanzas = ResizeArray<Stanza>()
    // Waivers are file-global (`waive <pkg> <source> <kind> <path>`), collected independent of any `file`
    // stanza — a deliberately-omitted member belongs to no teaching file, which is the whole point of it.
    let waivers = ResizeArray<Coverage.MemberKey>()
    let mutable cur: Stanza option = None
    let mutable proseKey: (string * string * string) option = None
    let mutable bodyKey: (string * string * string * string) option = None

    let flush () =
        match cur with
        | Some s -> stanzas.Add s
        | None -> ()

    for raw in lines do
        let line = raw.TrimEnd()
        let s = line.Trim()

        if s = "" || s.StartsWith "#" then
            ()
        elif s.StartsWith "| " || s = "|" then
            // a prose continuation line
            let body = if s = "|" then "" else line.Substring(line.IndexOf "| " + 2)

            match cur, proseKey, bodyKey with
            | Some c, Some k, _ ->
                let existing = c.Prose |> Map.tryFind k |> Option.defaultValue []
                cur <- Some { c with Prose = c.Prose |> Map.add k (existing @ [ body ]) }
            | Some c, None, Some(src, kind, p, anchor) ->
                let key = (src, kind, p)
                let existing = c.BodyProse |> Map.tryFind key |> Option.defaultValue []

                let updated =
                    existing
                    |> List.map (fun (a, ls) -> if a = anchor then a, ls @ [ body ] else a, ls)

                cur <- Some { c with BodyProse = c.BodyProse |> Map.add key updated }
            | _ -> fail $"manifest: prose line outside a `prose`/`proseat` stanza: {s}"
        else
            proseKey <- None
            bodyKey <- None
            let parts = Regex.Split(s, @"\s+") |> Array.toList

            match parts with
            | [ "waive"; pkg; src; kind; p ] ->
                // A global waiver, valid at any position. It touches no stanza — deliberately, so a `waive`
                // block can sit apart from the teaching curation (it lives at the tail of the manifest).
                waivers.Add(Coverage.key pkg src kind p)
            | "waive" :: _ -> fail $"manifest: `waive` needs `<pkg> <source> <kind> <path>`: {s}"
            | "file" :: rest ->
                flush ()

                cur <-
                    Some
                        { File = String.concat " " rest
                          Skill = None
                          Header = []
                          Namespace = ""
                          Package = ""
                          Opens = []
                          Includes = []
                          Prose = Map.empty
                          BodyProse = Map.empty }
            | _ ->
                match cur with
                | None -> fail $"manifest: `{parts.Head}` before any `file` stanza"
                | Some c ->
                    match parts with
                    | [ "skill"; v ] -> cur <- Some { c with Skill = Some v }
                    | "header" :: _ ->
                        let v = s.Substring(s.IndexOf "header" + 6).Trim()
                        cur <- Some { c with Header = c.Header @ [ v ] }
                    | [ "ns"; v ] -> cur <- Some { c with Namespace = v }
                    | [ "pkg"; v ] -> cur <- Some { c with Package = v }
                    | [ "open"; v ] -> cur <- Some { c with Opens = c.Opens @ [ v ] }
                    | [ "+"; src; kind; p ] ->
                        cur <-
                            Some
                                { c with
                                    Includes = c.Includes @ [ { Source = src; Kind = kind; Path = p } ] }
                    | [ "prose"; src; kind; p ] ->
                        proseKey <- Some(src, kind, p)
                        cur <- Some { c with Prose = c.Prose |> Map.add (src, kind, p) [] }
                    | "proseat" :: _ ->
                        // `proseat <src> <kind> <path> ::: <anchor line, verbatim>`
                        let body = s.Substring(s.IndexOf "proseat" + 7).Trim()

                        match body.Split([| " ::: " |], StringSplitOptions.None) |> Array.toList with
                        | [ head; anchor ] ->
                            match Regex.Split(head.Trim(), @"\s+") |> Array.toList with
                            | [ src; kind; p ] ->
                                bodyKey <- Some(src, kind, p, anchor)
                                let key = (src, kind, p)
                                let existing = c.BodyProse |> Map.tryFind key |> Option.defaultValue []
                                cur <- Some { c with BodyProse = c.BodyProse |> Map.add key (existing @ [ (anchor, []) ]) }
                            | _ -> fail $"manifest: malformed proseat target: {head}"
                        | _ -> fail $"manifest: proseat needs `<src> <kind> <path> ::: <anchor>`: {s}"
                    | _ -> fail $"manifest: unrecognised line: {s}"

    flush ()
    List.ofSeq stanzas, List.ofSeq waivers

// ---------------------------------------------------------------------------------------------
// Coverage — is every public member the pin ships either taught or waived?
// ---------------------------------------------------------------------------------------------

/// A declaration's OWN line declares it `internal`/`private` (FsiSurface's `nameOf` strips exactly this).
/// The pin ships twelve `module internal`; that a member is non-public because it, or an ANCESTOR, is
/// internal is `Coverage.publicMembers`' job — this only reads the one line.
let private isInternal (n: Node) =
    Regex.IsMatch(n.Decl.Head, @"^\s*(type|module|val|and)\s+(internal|private)\b")

/// Project a parse `Node` onto the minimal shape the coverage decision consumes.
let rec private toDecl (n: Node) : Coverage.Decl =
    { Kind = n.Kind
      Name = n.Name
      Internal = isInternal n
      Children = n.Children |> List.map toDecl }

/// Every public `type`/`val`/`and` the pinned packages export, as coverage keys. The pruning and the
/// group-accessibility inheritance live in `Coverage.publicMembers` (tested there); this only restores the
/// surface and projects it.
let publicUniverse (surface: Map<string, Map<string, Node list>>) : Coverage.MemberKey list =
    [ for KeyValue(pkg, files) in surface do
        for KeyValue(source, nodes) in files do
            yield! Coverage.publicMembers pkg source (nodes |> List.map toDecl) ]

/// The manifest's taught set, restricted to the kinds the universe carries: each stanza's `+` includes,
/// keyed by that stanza's package. (`+ ... module` lines are structural, like the modules they name, and
/// are not coverage keys.)
let taughtSet (stanzas: Stanza list) : Set<Coverage.MemberKey> =
    [ for st in stanzas do
        for inc in st.Includes do
            if inc.Kind = "type" || inc.Kind = "val" || inc.Kind = "and" then
                yield Coverage.key st.Package inc.Source inc.Kind inc.Path ]
    |> Set.ofList

// ---------------------------------------------------------------------------------------------
// Emit.
// ---------------------------------------------------------------------------------------------

/// Render one mirror file: the curated members, in manifest order, each carrying the pin's own text —
/// with ancestor `module` shells re-emitted around them so a subset of a module is still valid F#.
let render (surface: Map<string, Map<string, Node list>>) (st: Stanza) : string =
    let sb = StringBuilder()

    match st.Skill with
    | Some skill -> sb.AppendLine($"// See skill: {skill}") |> ignore
    | None -> ()

    for h in st.Header do
        sb.AppendLine h |> ignore

    sb.AppendLine($"namespace {st.Namespace}") |> ignore

    if not st.Opens.IsEmpty then
        sb.AppendLine() |> ignore
        for o in st.Opens do
            sb.AppendLine($"open {o}") |> ignore

    let filesOf pkg =
        match surface |> Map.tryFind pkg with
        | Some f -> f
        | None -> fail $"{st.File}: manifest names package {pkg}, which is not pinned by {pinsProps}"

    // Index every declaration of every source file this stanza draws from.
    let lookup (inc: Include) : Node =
        let files = filesOf st.Package

        let nodes =
            match files |> Map.tryFind inc.Source with
            | Some n -> n
            | None -> fail $"{st.File}: {st.Package} carries no api-surface/{inc.Source}"

        match flatten nodes |> List.tryFind (fun (p, n) -> p = inc.Path && n.Kind = inc.Kind) with
        | Some(_, n) -> n
        | None ->
            fail
                $"{st.File}: the pin does not export `{inc.Kind} {inc.Path}` from {st.Package}/{inc.Source} — the manifest names a member the package does not have."

    // Which ancestor modules have already been opened, so a run of members of one module shares a shell.
    let emitted = System.Collections.Generic.HashSet<string>()

    /// Splice each anchored doc block into the pin's verbatim text, above the line it names and at that
    /// line's own indent. An anchor that matches nothing is a HARD ERROR: it means the pin re-worded or
    /// removed the case this prose teaches, and silently dropping the prose is how the teaching rots
    /// while the gate stays green.
    let spliceBody (inc: Include) (text: string list) =
        match st.BodyProse |> Map.tryFind (inc.Source, inc.Kind, inc.Path) with
        | None -> text
        | Some anchors ->
            let mutable out = text

            for (anchor, doc) in anchors do
                let hits = out |> List.filter (fun l -> l.Trim() = anchor) |> List.length

                if hits = 0 then
                    fail
                        $"{st.File}: proseat anchor `{anchor}` matches no line of `{inc.Kind} {inc.Path}` in {st.Package}/{inc.Source} — the pin changed it."

                if hits > 1 then
                    fail
                        $"{st.File}: proseat anchor `{anchor}` matches {hits} lines of `{inc.Kind} {inc.Path}` — it is ambiguous, so it cannot say which case it teaches."

                // REPLACES the doc block above the anchor; it does not prepend to it. The manifest only
                // ever carries a `proseat` for a member the pin says NOTHING about, so there is normally
                // nothing here to replace — but "insert above" is wrong the moment that stops being true,
                // and it fails in the worst way: it renders BOTH, in a teaching document, with no way for
                // a reader to tell which is current. Measured on the first cut of this script, that
                // emitted `Neighbourhood.FourWay` carrying the pin's "costs `baseStep`" AND the mirror's
                // stale "costs 10", one under the other.
                let res = ResizeArray<string>()

                for l in out do
                    if l.Trim() = anchor then
                        while res.Count > 0 && res.[res.Count - 1].Trim().StartsWith "///" do
                            res.RemoveAt(res.Count - 1)

                        let ind = String(' ', l.Length - l.TrimStart().Length)

                        for d in doc do
                            res.Add(if d = "" then "" else ind + d)

                        res.Add l
                    else
                        res.Add l

                out <- List.ofSeq res

            out

    let leadOf (inc: Include) (n: Node) =
        match st.Prose |> Map.tryFind (inc.Source, inc.Kind, inc.Path) with
        | Some prose ->
            // The sidecar replaces the pin's DOC lines and keeps its attributes: an attribute is part of
            // the signature, not of the teaching.
            let attrs = n.Lead |> List.filter (fun l -> l.Trim().StartsWith "[<")
            prose @ attrs
        | None -> n.Lead

    // An `and` is a CONTINUATION, not a declaration — `and Attr<'msg> =` is only F# when a `type` (or
    // another `and`) of the same mutually-recursive group stands immediately above it. The manifest is
    // an ordered list a human edits, so "delete the line for a type you no longer teach" silently
    // orphans its `and`s: measured, dropping `type Control\`1` emitted a bare `and Attr<'msg> =` into
    // Controls/Types.fsi. Nothing downstream catches it — the mirror is documentation, so no compiler
    // ever reads it, and the gate reports only "drift", which reads as "regenerate and commit".
    // The pin declares 8 of these, so this is a live shape rather than a hypothetical one.
    // The group is defined by adjacency in the PIN, not in the manifest — so "some `type` precedes it"
    // is not the check. `and Attr` continues `Control`, and a manifest that dropped `Control` while
    // keeping `AttrCategory` above it would satisfy the loose test and still emit an orphan. The
    // predecessor the pin wrote is the only thing that answers this, so ask the pin.
    st.Includes
    |> List.iteri (fun i inc ->
        if inc.Kind = "and" then
            let nodes =
                match filesOf st.Package |> Map.tryFind inc.Source with
                | Some n -> n
                | None -> fail $"{st.File}: {st.Package} carries no api-surface/{inc.Source}"

            let siblings = nodes |> List.map (fun n -> n.Kind, n.Name)

            match siblings |> List.tryFindIndex (fun (k, nm) -> nm = inc.Path && k = "and") with
            | None | Some 0 ->
                fail $"{st.File}: `and {inc.Path}` is not a continuation in {st.Package}/{inc.Source}."
            | Some idx ->
                let pk, pn = siblings.[idx - 1]

                match (if i = 0 then None else Some st.Includes.[i - 1]) with
                | Some prev when prev.Source = inc.Source && prev.Kind = pk && prev.Path = pn -> ()
                | _ ->
                    fail
                        $"{st.File}: `and {inc.Path}` must be preceded by `{pk} {pn}` (the declaration it continues in {inc.Source}) — an `and` alone is not F#, and nothing compiles the mirror to catch it.")

    for inc in st.Includes do
        let node = lookup inc
        let segments = inc.Path.Split '.' |> Array.toList
        let ancestors = segments |> List.take (segments.Length - 1)

        // Re-emit each ancestor `module` shell once, at its depth.
        ancestors
        |> List.iteri (fun i _ ->
            let apath = ancestors |> List.take (i + 1) |> String.concat "."

            if emitted.Add apath then
                let anode =
                    let files = filesOf st.Package

                    match files |> Map.tryFind inc.Source with
                    | Some nodes ->
                        match flatten nodes |> List.tryFind (fun (p, n) -> p = apath && n.Kind = "module") with
                        | Some(_, n) -> n
                        | None -> fail $"{st.File}: no module `{apath}` in {st.Package}/{inc.Source}"
                    | None -> fail $"{st.File}: {st.Package} carries no api-surface/{inc.Source}"

                let indent = i * 4
                let srcIndent = anode.Decl.Head.Length - anode.Decl.Head.TrimStart().Length

                if anode.Spaced then
                    sb.AppendLine() |> ignore

                // The shell takes its prose from the sidecar too. An ancestor module is emitted here
                // rather than by an `+` line of its own, so without this its teaching prose — `module
                // Physics`'s "Every type here is scoped to `Physics` rather than promoted alongside
                // `Point`/`Rect`" — has no way to reach the page.
                let ancLead =
                    leadOf { Source = inc.Source; Kind = "module"; Path = apath } anode

                for l in reindent (indent - srcIndent) (ancLead @ anode.Decl) do
                    sb.AppendLine l |> ignore)

        if inc.Kind = "module" then
            // An explicitly included module with no members of its own listed: the shell above is it.
            if emitted.Add inc.Path then
                let indent = ancestors.Length * 4
                let srcIndent = node.Decl.Head.Length - node.Decl.Head.TrimStart().Length

                if node.Spaced then
                    sb.AppendLine() |> ignore

                for l in reindent (indent - srcIndent) (leadOf inc node @ node.Decl) do
                    sb.AppendLine l |> ignore
        else
            let indent = ancestors.Length * 4
            let text = leadOf inc node @ spliceBody inc (node.Text |> List.skip node.Lead.Length)
            let srcIndent = node.Decl.Head.Length - node.Decl.Head.TrimStart().Length

            if node.Spaced then
                sb.AppendLine() |> ignore

            for l in reindent (indent - srcIndent) text do
                sb.AppendLine l |> ignore

    // Normalise: exactly one blank between members, no leading blank, single trailing newline.
    let text = sb.ToString().Replace("\r\n", "\n")
    let text = Regex.Replace(text, @"\n{3,}", "\n\n")
    let text = Regex.Replace(text, @"\n+$", "\n")
    text

// ---------------------------------------------------------------------------------------------
// Drive.
// ---------------------------------------------------------------------------------------------

// Say it out loud. A run that silently swapped its own inputs would be indistinguishable from one that
// read the feed, and the whole argument for the swap is that it happens on exactly one commit.
if not srcBackedPins.IsEmpty then
    printfn
        "RELEASE WINDOW: this commit bumps <FsGgUiVersion> to %s, which no feed can serve yet — reading the .fsi of %d package(s) from src/, which is what release.yml packs at that pin. %d pin(s) still restore from the feed."
        (pins |> Map.find (srcBackedPins |> Map.toList |> List.head |> fst))
        srcBackedPins.Count
        (pins.Count - srcBackedPins.Count)

restorePins ()
let surface = loadPinSurface ()

let stanzas, waivers = parseManifest manifestPath

if stanzas.IsEmpty then
    fail $"the manifest at {manifestPath} declares no files"

// #982 — a `+` include a stanza REPEATS is malformed, and it renders as many times as it is listed. This is
// the `Pathfinding.fsi` `Step`/`Reach` TRIPLICATION TowerDefense1#4 saw: the manifest carried three
// `+ type Pathfinding.Step` / `+ type Pathfinding.Reach` lines and the mirror rendered three copies of each,
// inside `module Pathfinding`. Nothing else here caught it — `taughtSet` is a `Set`, so the #925 coverage
// reconcile collapses the repeat and stays green, and the drift check compares the tripled output against the
// tripled committed mirror and agrees. Well-formedness is a property of the manifest, not of a mode, so it is
// checked in EVERY mode (like the coverage reconcile), before a single file is written. Fails closed (#266):
// a type has ONE declaration, so a member named twice in one `file` stanza is never intended.
let malformed =
    stanzas
    |> List.choose (fun st ->
        let includes =
            st.Includes
            |> List.map (fun (inc: Include) -> ({ Source = inc.Source; Kind = inc.Kind; Path = inc.Path }: Coverage.Include))

        match Coverage.duplicateIncludes includes with
        | [] -> None
        | dups -> Some(st.File, dups))

if not (List.isEmpty malformed) then
    for file, dups in malformed do
        for d in dups do
            eprintfn "duplicate include in %s: `+ %s %s %s` is listed more than once — it renders that many times (the Pathfinding Step/Reach triplication, TowerDefense1#4)." file d.Source d.Kind d.Path

    let total = malformed |> List.sumBy (fun (_, dups) -> List.length dups)
    fail $"{total} duplicate `+` include(s) across {List.length malformed} stanza(s) — delete the repeated line(s). Each `+` renders once, so a member listed N times appears N times in the mirror."

// #925 — reconcile the pin's public surface against the manifest's teach + waive decisions. Computed in
// every mode: you cannot "regenerate" your way out of an untaught member — it must be taught or waived, so
// this is a validation like the orphan check below, not a generation step. `--emit-waivers` prints the
// current gap as paste-ready `waive` lines (the seed for, and the regeneration path of, the waiver block)
// and stops before touching the mirror.
let universe = publicUniverse surface
let taught = taughtSet stanzas
let coverage = Coverage.reconcile universe taught (Set.ofList waivers)

if emitWaivers then
    // The WHOLE block, ignoring any waivers already committed — this replaces the block wholesale on a
    // regeneration, so it must reconcile against `taught` alone, not against the block it is re-seeding
    // (which would emit only the delta and drop every existing waiver).
    for m in (Coverage.reconcile universe taught Set.empty).Untaught do
        printfn "%s" (Coverage.waiveLine m)

    exit 0

let mutable drift = 0

for st in stanzas do
    let target = Path.Combine(mirrorRoot, st.File.Replace('/', Path.DirectorySeparatorChar))
    let text = render surface st

    Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore

    let existing =
        if File.Exists target then File.ReadAllText(target).Replace("\r\n", "\n") else ""

    if existing <> text then
        drift <- drift + 1

        if checkOnly then
            eprintfn "drift: %s" st.File
        else
            File.WriteAllText(target, text)

// A mirror file nobody's stanza claims is a file the generator does not own — and after this item the
// generator owns the whole tree. Report it rather than leaving an orphan the gate then blesses forever.
let declared =
    stanzas |> List.map (fun s -> Path.GetFullPath(Path.Combine(mirrorRoot, s.File))) |> Set.ofList

let orphans =
    if Directory.Exists mirrorRoot then
        Directory.GetFiles(mirrorRoot, "*.fsi", SearchOption.AllDirectories)
        |> Array.filter (fun f -> not (declared |> Set.contains (Path.GetFullPath f)))
    else
        [||]

if orphans.Length > 0 then
    for o in orphans do
        eprintfn "orphan (no manifest stanza): %s" (Path.GetRelativePath(mirrorRoot, o))

    fail $"{orphans.Length} mirror file(s) no manifest stanza claims"

// #925 — a waiver that waives nothing (its member left the pin's public surface, or is now taught) is a
// hard error: a dead waiver list accretes entries the next real omission hides behind (#266).
if not (List.isEmpty coverage.StaleWaivers) then
    eprintfn
        "%d waiver(s) in %s waive nothing (member no longer public, or now taught):"
        coverage.StaleWaivers.Length
        (Path.GetRelativePath(repoRoot, manifestPath))

    for m in coverage.StaleWaivers do
        eprintfn "  %s %s %s %s" m.Package m.Source m.Kind m.Path

    fail $"{coverage.StaleWaivers.Length} stale waiver(s) — delete each, or restore the member it names."

// #925 — the gap this item closes: a public member the pin ships that is taught NOWHERE and waived
// nowhere. Silent under the drift check (the mirror and its regeneration agree on omitting it), loud here.
if not (List.isEmpty coverage.Untaught) then
    eprintfn
        "%d public member(s) the pin ships are neither taught nor waived:"
        coverage.Untaught.Length

    for m in coverage.Untaught do
        eprintfn "  %s %s %s %s" m.Package m.Source m.Kind m.Path

    fail
        $"{coverage.Untaught.Length} untaught public member(s) — TEACH each in {Path.GetRelativePath(repoRoot, manifestPath)} (a `+` in a file stanza) or WAIVE it deliberately (a `waive` line). Seed the waivers with: dotnet fsi scripts/refresh-api-surface-mirror.fsx --emit-waivers"

// #984 — a game-profile module the scaffold must vendor that is ENTIRELY waived (zero taught members).
// The member-level reconcile above stays green on this (each member IS a decision — waived), and the drift
// check agrees a fully-waived module has no mirror file, so this is the only thing that reds when a future
// edit re-drops a whole game-profile module. Computed in every mode, like the reconcile.
let profileGaps = Coverage.profileGaps universe taught Coverage.gameProfileModules

if not (List.isEmpty profileGaps) then
    eprintfn
        "%d declared game-profile module(s) are not fully vendored (a game scaffold would carry none of their surface):"
        profileGaps.Length

    for g in profileGaps do
        match g.Reason with
        | "Vanished" ->
            eprintfn
                "  %s/%s — the pin ships no public member under it; the Coverage.gameProfileModules declaration has rotted (module removed or renamed)."
                g.Package
                g.Source
        | _ ->
            eprintfn
                "  %s/%s — every public member is waived; TEACH at least one (a `+` in a `file` stanza) so the scaffold vendors the module."
                g.Package
                g.Source

    fail
        $"{profileGaps.Length} game-profile module(s) entirely dropped — a declared game-profile module (Coverage.gameProfileModules) must teach at least one member."

if checkOnly && drift > 0 then
    fail $"{drift} mirror file(s) differ from what the pin + manifest generate"

printfn
    "api-surface mirror: %d file(s) from %d pinned package(s); %d rewritten"
    stanzas.Length
    pins.Count
    drift
