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

#load "lib/FsiSurface.fsx"
open FsiSurface

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let manifestPath = Path.Combine(repoRoot, "scripts", "api-surface-manifest.txt")
let mirrorRoot = Path.Combine(repoRoot, "template", "base", "docs", "api-surface")
let pinsProps = Path.Combine(repoRoot, "template", "base", "Directory.Packages.props")

let argv = fsi.CommandLineArgs |> Array.toList |> List.tail
let checkOnly = argv |> List.contains "--check"

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
        let refs =
            pins
            |> Map.toList
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

        File.WriteAllText(
            Path.Combine(work, "NuGet.config"),
            """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"""
        )

        let psi = ProcessStartInfo("dotnet", $"restore probe.fsproj --packages \"{probePackagesDir}\"")
        psi.WorkingDirectory <- work
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

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

/// `PackageId -> (sourceFileName -> parsed nodes)`, read from the restored package's `api-surface/`.
///
/// An absent `api-surface/` is a HARD ERROR, not an empty map: it means the pinned version predates
/// #782 and carries no signature files, and the honest response is to say which package and which
/// version rather than emit a mirror with that package's members silently missing.
let loadPinSurface () =
    pins
    |> Map.toList
    |> List.map (fun (id, version) ->
        let dir =
            Path.Combine(probePackagesDir, id.ToLowerInvariant(), version, "api-surface")

        if not (Directory.Exists dir) then
            fail
                $"{id} {version} carries no api-surface/ — it predates #782. Bump the pin to a release that packs its .fsi."

        let files =
            Directory.GetFiles(dir, "*.fsi", SearchOption.AllDirectories)
            |> Array.map (fun f ->
                let rel = Path.GetRelativePath(dir, f).Replace('\\', '/')
                let nodes, unparsed = parseFsi (File.ReadAllLines f |> Array.toList)

                if not unparsed.IsEmpty then
                    fail $"{id}/{rel}: {unparsed.Length} line(s) the .fsi reader could not classify, first: {unparsed.Head.Trim()}"

                rel, nodes)
            |> Map.ofArray

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

let parseManifest (path: string) : Stanza list =
    if not (File.Exists path) then
        fail $"no manifest at {path}"

    let lines = File.ReadAllLines path
    let stanzas = ResizeArray<Stanza>()
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
    List.ofSeq stanzas

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

restorePins ()
let surface = loadPinSurface ()

let stanzas = parseManifest manifestPath

if stanzas.IsEmpty then
    fail $"the manifest at {manifestPath} declares no files"

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

if checkOnly && drift > 0 then
    fail $"{drift} mirror file(s) differ from what the pin + manifest generate"

printfn
    "api-surface mirror: %d file(s) from %d pinned package(s); %d rewritten"
    stanzas.Length
    pins.Count
    drift
