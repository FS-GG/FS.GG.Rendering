// Feature 209 — FS.GG.UI version-staleness / coherence guard.
//
// Makes the Feature-204 version-staleness bug class a LOUD, LOCAL, AUTOMATIC failure in this repo's
// own merge-blocking gate, before any consumer scaffolds a product. Mirrors the two-layer shape of
// scripts/validate-bom-consumer.fsx:
//
//   * Structural verdict-core (always, env-free): re-derives, from the repo + pushed git tags, that
//     the single <FsGgUiVersion> literal is well-formed and present exactly once; the pin matches an
//     existing fs-gg-ui/v<V> snapshot tag and does NOT lag the latest such tag (preview-aware
//     SemVer compare, not string); the BOM uses the single [$version$] exact-bracket token; the
//     packable FS.GG.UI.* set == the BOM dependency set; the template's consumed pins all derive
//     through $(FsGgUiVersion) and equal the documented 11-member manifest; and build.fsx's runtime
//     regex still matches the literal. Exits non-zero NAMING the specific mismatch expected-vs-actual.
//
//   * Restore-grounded proof (FS_GG_RUN_VERSION_COHERENCE_SMOKE=1): packs the 16 FS.GG.UI.* members
//     + the BOM from source at the pinned V to a throwaway feed, restores FS.GG.UI@V in a clean
//     consumer, and asserts the COMPLETE member set resolves to exactly V (FR-008, anti-text-grep);
//     a member off V fails loudly with [restore-partial], never a silent partial graph.
//
// Exit codes (contract §1): 0 coherent · 1 drift (>=1 conjunct false) · 2 guard error (inputs
// unreadable / tags not fetched / pack-restore tooling failed) — fails CLOSED, never green-by-absence.
//
// The repo-root <Version> (Directory.Build.props) is DECOUPLED by default (D5) and is NOT compared.

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName
let repo (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))
let live = Environment.GetEnvironmentVariable "FS_GG_RUN_VERSION_COHERENCE_SMOKE" = "1"

/// Raised for any unreadable input / unfetched tags / tooling failure ⇒ exit 2 (fail closed).
exception GuardError of string

// ---- shell helper -----------------------------------------------------------------------------
let run (workDir: string) (exe: string) (args: string list) =
    let psi = ProcessStartInfo(exe)
    psi.WorkingDirectory <- workDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    args |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    let out = proc.StandardOutput.ReadToEnd()
    let err = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    proc.ExitCode, out + err

let readFile (path: string) =
    if not (File.Exists path) then raise (GuardError(sprintf "required input missing: %s" path))
    File.ReadAllText path

// ---- preview-aware SemVer comparator (D7, T008) -----------------------------------------------
// Numeric major.minor.patch compared numerically; then dotted prerelease identifiers per SemVer §11
// (numeric identifiers numerically, alphanumeric lexically, numeric < alphanumeric, fewer < more,
// and a version WITHOUT prerelease outranks the same core WITH prerelease). Hand-rolled so the
// script needs no package reference.
module SemVer =
    type V = { Major: int; Minor: int; Patch: int; Pre: string list }

    let parse (s: string) : V =
        let s = s.Trim()
        let core, pre =
            match s.IndexOf '-' with
            | -1 -> s, ""
            | i -> s.Substring(0, i), s.Substring(i + 1)
        let nums = core.Split('.')
        let n i =
            if i < nums.Length then
                match Int32.TryParse nums.[i] with
                | true, v -> v
                | _ -> raise (GuardError(sprintf "malformed version (non-numeric core): %s" s))
            else 0
        { Major = n 0
          Minor = n 1
          Patch = n 2
          Pre = if pre = "" then [] else pre.Split('.') |> List.ofArray }

    let private cmpId (a: string) (b: string) =
        match Int32.TryParse a, Int32.TryParse b with
        | (true, x), (true, y) -> Operators.compare x y
        | (true, _), (false, _) -> -1 // numeric identifier has lower precedence than alphanumeric
        | (false, _), (true, _) -> 1
        | _ -> String.CompareOrdinal(a, b)

    /// -1 / 0 / +1, preview-aware.
    let cmp (a: V) (b: V) : int =
        let core =
            [ Operators.compare a.Major b.Major
              Operators.compare a.Minor b.Minor
              Operators.compare a.Patch b.Patch ]
            |> List.tryFind ((<>) 0)
            |> Option.defaultValue 0
        if core <> 0 then core
        else
            match a.Pre, b.Pre with
            | [], [] -> 0
            | [], _ -> 1 // no prerelease outranks prerelease
            | _, [] -> -1
            | pa, pb ->
                let rec loop xs ys =
                    match xs, ys with
                    | [], [] -> 0
                    | [], _ -> -1 // fewer identifiers ⇒ lower precedence
                    | _, [] -> 1
                    | x :: xs', y :: ys' ->
                        let c = cmpId x y in if c <> 0 then c else loop xs' ys'
                loop pa pb

    let lt a b = cmp (parse a) (parse b) < 0

// Self-check the exact spec edge pairs (T008) — fail closed if the comparator ever regresses.
do
    if not (SemVer.lt "0.1.9-preview.1" "0.1.10-preview.1") then
        raise (GuardError "comparator regressed: 0.1.9-preview.1 must be < 0.1.10-preview.1")
    if not (SemVer.lt "0.1.51-preview.1" "0.1.51-preview.2") then
        raise (GuardError "comparator regressed: …-preview.1 must be < …-preview.2")

// ---- failure shape + verdict (data-model §8) --------------------------------------------------
type Failure =
    { Rule: string
      Location: string
      Expected: string
      Actual: string
      Fix: string }

let private lineOf (text: string) (needle: string) =
    let lines = text.Replace("\r\n", "\n").Split('\n')
    lines
    |> Array.tryFindIndex (fun l -> l.Contains needle)
    |> Option.map ((+) 1)
    |> Option.defaultValue 0

// ---- pure input readers (T009) — each fails closed on unreadable input ------------------------

// SingleVersionSource
let propsRel = "template/base/Directory.Packages.props"
let propsPath = repo propsRel
let propsText = readFile propsPath
let fsGgUiMatches = Regex.Matches(propsText, "<FsGgUiVersion>([^<]*)</FsGgUiVersion>")
let occurrences = fsGgUiMatches.Count
let pinVersion =
    if occurrences >= 1 then fsGgUiMatches.[0].Groups.[1].Value.Trim()
    else raise (GuardError(sprintf "<FsGgUiVersion> not found in %s — single source of version truth missing" propsRel))
let fsGgUiLine = lineOf propsText "<FsGgUiVersion>"
let propsLoc = sprintf "%s:%d <FsGgUiVersion>" propsRel fsGgUiLine

// CoherentSnapshotTag set (fail closed if tags are unfetched — never green-by-absence)
let tagVersions =
    let ec, out = run repoRoot "git" [ "tag"; "--list"; "fs-gg-ui/v*" ]
    if ec <> 0 then raise (GuardError "git tag --list failed")
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith("fs-gg-ui/v", StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring("fs-gg-ui/v".Length))
    |> Array.toList
if tagVersions.IsEmpty then
    raise (GuardError "no fs-gg-ui/v* tags visible — CI must fetch tags (fetch-depth: 0 / fetch-tags); fail closed rather than green-by-absence")
let latestTag = tagVersions |> List.sortWith (fun a b -> SemVer.cmp (SemVer.parse a) (SemVer.parse b)) |> List.last

// PublishedMemberSet P — packable FS.GG.UI.* under src/** (reuses validate-bom-consumer discovery)
let publishedMembers =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun proj ->
        let t = File.ReadAllText proj
        let m name = Regex.Match(t, sprintf "<%s>([^<]*)</%s>" name name)
        let pid = let g = m "PackageId" in if g.Success then g.Groups.[1].Value.Trim() else ""
        let packable = let g = m "IsPackable" in g.Success && g.Groups.[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
        if packable && pid.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then Some pid else None)
    |> Set.ofArray

// BomDependencySet B
let nuspecRel = "src/Meta/FS.GG.UI.nuspec"
let bomDeps =
    let text = readFile (repo nuspecRel)
    Regex.Matches(text, "<dependency\\s+id=\"([^\"]+)\"\\s+version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList
let bomIds = bomDeps |> List.map fst |> Set.ofList

// TemplateConsumedPinSet T (id + its Version attribute, in file order)
let templatePins =
    Regex.Matches(propsText, "<PackageVersion\\s+Include=\"(FS\\.GG\\.UI\\.[^\"]+)\"\\s+Version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList
let templateIds = templatePins |> List.map fst |> Set.ofList
// The documented consumed manifest (data-model §5, surface-map T004) — 11 product-facing members.
let templateExpected =
    Set.ofList
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing" ]

// RuntimeResolution (build.fsx:60 regex still matches the literal in the current tree)
let buildFsxRel = "template/base/build.fsx"
let runtimeRegexResolves =
    let buildText = readFile (repo buildFsxRel)
    // build.fsx applies this exact regex to Directory.Packages.props at runtime.
    let m = Regex.Match(buildText, "<FsGgUiVersion>\\(\\[\\^<\\]\\+\\)</FsGgUiVersion>")
    let pattern = "<FsGgUiVersion>([^<]+)</FsGgUiVersion>"
    m.Success && Regex.IsMatch(propsText, pattern)

// ---- rules ------------------------------------------------------------------------------------

// US1 — pin must resolve to a published snapshot tag and must not lag the latest (FR-001/002/009)
let us1Failures : Failure list =
    if SemVer.lt pinVersion latestTag then
        [ { Rule = "pin-lags-tag"
            Location = propsLoc
            Expected = sprintf ">= %s (latest fs-gg-ui/v* tag)" latestTag
            Actual = pinVersion
            Fix = sprintf "bump <FsGgUiVersion> to %s (the latest coherent snapshot), or cut a newer fs-gg-ui/v* tag" latestTag } ]
    elif not (List.contains pinVersion tagVersions) then
        [ { Rule = "pin-no-tag"
            Location = propsLoc
            Expected = sprintf "a tag fs-gg-ui/v%s" pinVersion
            Actual = "none"
            Fix = sprintf "cut & push the fs-gg-ui/v%s snapshot tag (and feed), or correct <FsGgUiVersion> to a published version" pinVersion } ]
    else []

// US2 — a half-bump cannot ship, independent of any warnings-as-errors policy (FR-003/004/005)
let bomTokenFailures : Failure list =
    bomDeps
    |> List.collect (fun (id, v) ->
        let notToken = v <> "[$version$]"
        let notExact = not (v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ","))
        [ if notToken then
              { Rule = "bom-pin-not-token"
                Location = sprintf "%s %s" nuspecRel id
                Expected = "[$version$]"
                Actual = v
                Fix = sprintf "restore %s's version to the single token [$version$]" id }
          if notExact then
              { Rule = "bom-exact-bracket"
                Location = sprintf "%s %s" nuspecRel id
                Expected = "an exact [..] bracket with no comma"
                Actual = v
                Fix = sprintf "pin %s with an exact bracket so any deviation fails loudly" id } ])

let bomMemberSkewFailures : Failure list =
    [ for missing in Set.difference publishedMembers bomIds ->
        { Rule = "bom-member-skew"
          Location = nuspecRel
          Expected = sprintf "a <dependency> for every packable FS.GG.UI.* member (%d)" publishedMembers.Count
          Actual = sprintf "missing %s" missing
          Fix = sprintf "add <dependency id=\"%s\" version=\"[$version$]\" /> to the BOM" missing }
      for extra in Set.difference bomIds publishedMembers ->
        { Rule = "bom-member-skew"
          Location = nuspecRel
          Expected = sprintf "only packable FS.GG.UI.* members (%d)" publishedMembers.Count
          Actual = sprintf "extra %s (no packable src/** member)" extra
          Fix = sprintf "remove %s from the BOM, or add the packable src/** member" extra } ]

let templateFailures : Failure list =
    [ // every consumed pin derives through $(FsGgUiVersion) — no hardcoded literal
      for (id, v) in templatePins do
          if v <> "$(FsGgUiVersion)" then
              yield
                  { Rule = "template-pin-hardcoded"
                    Location = sprintf "%s %s" propsRel id
                    Expected = "$(FsGgUiVersion)"
                    Actual = v
                    Fix = sprintf "route %s's Version through $(FsGgUiVersion) (the single source)" id }
      // consumed set ⊆ published, and == the documented 11-member manifest
      for extra in Set.difference templateIds publishedMembers ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "every consumed pin is a packable FS.GG.UI.* member"
            Actual = sprintf "%s is not in the published set" extra
            Fix = sprintf "remove %s from the template, or publish it as a packable member" extra }
      for missing in Set.difference templateExpected templateIds ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "the documented 11-member consumed manifest"
            Actual = sprintf "missing %s" missing
            Fix = sprintf "restore the consumed pin %s" missing }
      for extra in Set.difference templateIds templateExpected ->
          { Rule = "template-consumed-skew"
            Location = propsRel
            Expected = "the documented 11-member consumed manifest"
            Actual = sprintf "unexpected consumed pin %s" extra
            Fix = sprintf "drop %s, or update the documented consumed manifest in surface-map.md" extra } ]

let invariantFailures : Failure list =
    [ if occurrences <> 1 then
          { Rule = "single-source-not-unique"
            Location = propsLoc
            Expected = "exactly 1 <FsGgUiVersion> literal"
            Actual = string occurrences
            Fix = "collapse to a single <FsGgUiVersion> literal (the one source of truth)" }
      if not runtimeRegexResolves then
          { Rule = "runtime-regex-broken"
            Location = sprintf "%s:60" buildFsxRel
            Expected = "build.fsx's <FsGgUiVersion>([^<]+)</FsGgUiVersion> regex matches the literal"
            Actual = "no match (renamed/half-renamed property breaks runtime engine resolution)"
            Fix = "keep the <FsGgUiVersion> element name in lockstep with build.fsx's regex" } ]

let structuralFailures =
    us1Failures @ bomTokenFailures @ bomMemberSkewFailures @ templateFailures @ invariantFailures

// ---- restore-grounded proof (live, US3/T027) --------------------------------------------------
type LiveResult =
    { V: string
      MembersResolved: int
      AtV: int
      Partial: Failure list
      CleanBuild: bool }

let liveProof () : LiveResult =
    let v = pinVersion
    if String.IsNullOrWhiteSpace v then raise (GuardError "pinned version is undefined — cannot run restore proof")
    let tmp = Path.Combine(Path.GetTempPath(), "vcoh209-" + Guid.NewGuid().ToString("N").Substring(0, 8))
    let feed = Path.Combine(tmp, "feed")
    let gpf = Path.Combine(tmp, "gpf")
    Directory.CreateDirectory feed |> ignore

    // pack the coherent snapshot (16 members + BOM) from source at the pinned V
    let pc, po = run repoRoot "dotnet" [ "pack"; "FS.GG.Rendering.slnx"; "-c"; "Release"; sprintf "-p:Version=%s" v; "-o"; feed ]
    if pc <> 0 then raise (GuardError(sprintf "pack-from-source at %s failed:\n%s" v po))

    // clean consumer: ONLY FS.GG.UI@V
    let cdir = Path.Combine(tmp, "consumer")
    Directory.CreateDirectory cdir |> ignore
    let nugetConfig =
        sprintf
            "<configuration><config><add key=\"globalPackagesFolder\" value=\"%s\" /></config><packageSources><clear /><add key=\"local\" value=\"%s\" /><add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" /></packageSources><packageSourceMapping><packageSource key=\"local\"><package pattern=\"FS.GG.UI*\" /></packageSource><packageSource key=\"nuget.org\"><package pattern=\"*\" /></packageSource></packageSourceMapping></configuration>"
            gpf feed
    File.WriteAllText(Path.Combine(cdir, "nuget.config"), nugetConfig)
    File.WriteAllText(Path.Combine(cdir, "Library.fs"), "module Consumer.Library")
    File.WriteAllText(
        Path.Combine(cdir, "Consumer.fsproj"),
        sprintf
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup><ItemGroup><PackageReference Include=\"FS.GG.UI\" Version=\"%s\" /></ItemGroup><ItemGroup><Compile Include=\"Library.fs\" /></ItemGroup></Project>"
            v)

    let rc, ro = run cdir "dotnet" [ "restore"; "Consumer.fsproj" ]
    if rc <> 0 then raise (GuardError(sprintf "clean restore of FS.GG.UI@%s failed:\n%s" v ro))
    let _, listOut = run cdir "dotnet" [ "list"; "Consumer.fsproj"; "package"; "--include-transitive" ]
    let resolved =
        Regex.Matches(listOut, "(FS\\.GG\\.UI[A-Za-z.]*)\\s+(?:[0-9][^\\s]*\\s+)?([0-9][0-9A-Za-z.\\-]*)")
        |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
        |> Seq.distinct
        |> Seq.filter (fun (id, _) -> id.StartsWith "FS.GG.UI." && id <> "FS.GG.UI")
        |> Seq.toList
    let bc, _ = run cdir "dotnet" [ "build"; "Consumer.fsproj"; "-c"; "Release"; "--no-restore" ]

    let offV = resolved |> List.filter (fun (_, rv) -> rv <> v)
    let resolvedIds = resolved |> List.map fst |> Set.ofList
    let partialFailures =
        [ for (id, rv) in offV ->
            { Rule = "restore-partial"
              Location = sprintf "FS.GG.UI@%s clean restore" v
              Expected = sprintf "all members @%s" v
              Actual = sprintf "%s @%s" id rv
              Fix = "republish the lagging member(s) at the pinned V so the snapshot is complete" }
          // a member that did not resolve at all is also a partial graph
          for missing in Set.difference publishedMembers resolvedIds ->
            { Rule = "restore-partial"
              Location = sprintf "FS.GG.UI@%s clean restore" v
              Expected = sprintf "all %d members resolve @%s" publishedMembers.Count v
              Actual = sprintf "%s did not resolve" missing
              Fix = sprintf "publish %s@%s to the feed" missing v } ]

    { V = v
      MembersResolved = resolved.Length
      AtV = resolved |> List.filter (fun (_, rv) -> rv = v) |> List.length
      Partial = partialFailures
      CleanBuild = (bc = 0) }

// ---- aggregate verdict + report (T014/T024/T028) ----------------------------------------------
let reportPath = repo "specs/209-version-staleness-guard/readiness/version-coherence.md"

let writeReport (provenance: string) (failures: Failure list) (liveOpt: LiveResult option) =
    Directory.CreateDirectory(Path.GetDirectoryName reportPath) |> ignore
    let sb = System.Text.StringBuilder()
    let line (s: string) = sb.AppendLine s |> ignore
    let ok = failures.IsEmpty
    line "# FS.GG.UI version coherence — verdict report"
    line ""
    line "Regenerated by `scripts/validate-version-coherence.fsx`. The merge-blocking gate step"
    line "\"Version coherence guard\" re-derives this verdict on every PR."
    line ""
    line "- feature: 209-version-staleness-guard"
    line (sprintf "- result: %s" (if ok then "pass" else "fail"))
    line (sprintf "- provenance: %s" provenance)
    line (sprintf "- single-version-source: %s (`%s`, occurrences=%d)" pinVersion propsLoc occurrences)
    line (sprintf "- latest-snapshot-tag: fs-gg-ui/v%s" latestTag)
    line (sprintf "- published-members: %d · bom-deps: %d · template-consumed-pins: %d" publishedMembers.Count bomIds.Count templateIds.Count)
    line (sprintf "- runtime-regex-resolves: %b" runtimeRegexResolves)
    match liveOpt with
    | Some r ->
        line (sprintf "- resolved-members-at-version: %d/%d at %s" r.AtV r.MembersResolved r.V)
        line (sprintf "- clean-consumer-build: %s" (if r.CleanBuild then "pass" else "fail"))
    | None ->
        line "- resolved-members-at-version: pending-live (run FS_GG_RUN_VERSION_COHERENCE_SMOKE=1)"
    line ""
    if ok then
        line "All lockstep conjuncts hold for the layers that ran."
    else
        line "## Drift — named locations (expected-vs-actual)"
        line ""
        for f in failures do
            line (sprintf "- `DRIFT [%s]` %s — expected `%s`; actual `%s`" f.Rule f.Location f.Expected f.Actual)
    File.WriteAllText(reportPath, sb.ToString())

let printDrift (failures: Failure list) =
    for f in failures do
        eprintfn "DRIFT [%s] %s" f.Rule f.Location
        eprintfn "  expected: %s" f.Expected
        eprintfn "  actual:   %s" f.Actual
        eprintfn "  fix:      %s" f.Fix
    // GitHub step summary (SC-006) — reviewer sees the named location without opening logs.
    match Environment.GetEnvironmentVariable "GITHUB_STEP_SUMMARY" with
    | null | "" -> ()
    | summaryPath ->
        let s = System.Text.StringBuilder()
        s.AppendLine "### Version coherence guard — DRIFT" |> ignore
        s.AppendLine "" |> ignore
        for f in failures do
            s.AppendLine(sprintf "- `DRIFT [%s]` %s — expected `%s`; actual `%s` — fix: %s" f.Rule f.Location f.Expected f.Actual f.Fix) |> ignore
        File.AppendAllText(summaryPath, s.ToString())

// ---- main -------------------------------------------------------------------------------------
let main () =
    if live then
        let r = liveProof ()
        let allFailures = structuralFailures @ r.Partial
        writeReport "live" allFailures (Some r)
        if allFailures.IsEmpty then
            printfn "version coherence: COHERENT (structural + live). %d/%d members @%s; wrote %s" r.AtV r.MembersResolved r.V reportPath
            0
        else
            printDrift allFailures
            eprintfn "version coherence: DRIFT — %d failure(s); wrote %s" allFailures.Length reportPath
            1
    else
        writeReport "verdict-core" structuralFailures None
        if structuralFailures.IsEmpty then
            printfn "version coherence: COHERENT (structural verdict-core). pin %s == latest tag; wrote %s" pinVersion reportPath
            0
        else
            printDrift structuralFailures
            eprintfn "version coherence: DRIFT — %d failure(s); wrote %s" structuralFailures.Length reportPath
            1

let exitCode =
    try
        main ()
    with
    | GuardError msg ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" msg
        2
    | ex ->
        eprintfn "GUARD ERROR (fails closed, exit 2): %s" ex.Message
        2

exit exitCode
