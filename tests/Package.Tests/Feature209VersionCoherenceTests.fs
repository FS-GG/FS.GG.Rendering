module Feature209VersionCoherenceTests

// Feature 209 — release-lane / local-dev mirror of the version-coherence verdict.
//
// A1 AUTHORITY: this xUnit/Expecto wrapper MIRRORS, never replaces, the canonical documented shell
// scenarios in specs/209-version-staleness-guard/readiness/version-coherence-scenarios.md (the source
// of truth). It re-derives the STRUCTURAL verdict env-free (no pack/restore) so the coherent baseline
// passing + the forced-drift fixtures going red are also enforced in the release lane and locally.
// The deeper generate→restore→build of a product from the template stays in release.yml (T032), not
// duplicated here.

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

// ---- preview-aware SemVer comparator (mirrors the script's D7 comparator) ----------------------
let private parse (s: string) =
    let s = s.Trim()
    let core, pre =
        match s.IndexOf '-' with
        | -1 -> s, ""
        | i -> s.Substring(0, i), s.Substring(i + 1)
    let nums = core.Split('.')
    let n i = if i < nums.Length then int nums.[i] else 0
    (n 0, n 1, n 2), (if pre = "" then [] else pre.Split('.') |> List.ofArray)

let private cmpId (a: string) (b: string) =
    match Int32.TryParse a, Int32.TryParse b with
    | (true, x), (true, y) -> compare x y
    | (true, _), (false, _) -> -1
    | (false, _), (true, _) -> 1
    | _ -> String.CompareOrdinal(a, b)

let private cmp (a: string) (b: string) =
    let (ca, pa), (cb, pb) = parse a, parse b
    if ca <> cb then compare ca cb
    else
        match pa, pb with
        | [], [] -> 0
        | [], _ -> 1
        | _, [] -> -1
        | _ ->
            let rec loop xs ys =
                match xs, ys with
                | [], [] -> 0
                | [], _ -> -1
                | _, [] -> 1
                | x :: xs', y :: ys' -> let c = cmpId x y in if c <> 0 then c else loop xs' ys'
            loop pa pb

// ---- env-free readers (re-derived directly from the repo) --------------------------------------
let private propsText = File.ReadAllText(repo "template/base/Directory.Packages.props")
let private nuspecText = File.ReadAllText(repo "src/Meta/FS.GG.UI.nuspec")

let private pinVersion =
    Regex.Match(propsText, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>").Groups.[1].Value.Trim()

let private pinOccurrences = Regex.Matches(propsText, "<FsGgUiVersion>([^<]*)</FsGgUiVersion>").Count

let private tagVersions () =
    let psi = ProcessStartInfo("git")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    [ "tag"; "--list"; "fs-gg-ui/v*" ] |> List.iter psi.ArgumentList.Add
    let out =
        match Process.Start psi with
        | null -> failwith "git tag could not be started"
        | p ->
            use p = p
            let o = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            o
    out.Replace("\r\n", "\n").Split('\n')
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (fun s -> s.StartsWith("fs-gg-ui/v", StringComparison.Ordinal))
    |> Array.map (fun s -> s.Substring("fs-gg-ui/v".Length))
    |> Array.toList

let private discoveredMembers () =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun proj ->
        let t = File.ReadAllText proj
        let m name = Regex.Match(t, sprintf "<%s>([^<]*)</%s>" name name)
        let pid = let g = m "PackageId" in if g.Success then g.Groups.[1].Value.Trim() else ""
        let packable = let g = m "IsPackable" in g.Success && g.Groups.[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
        if packable && pid.StartsWith("FS.GG.UI.", StringComparison.Ordinal) then Some pid else None)
    |> Set.ofArray

let private bomDeps () =
    Regex.Matches(nuspecText, "<dependency\\s+id=\"([^\"]+)\"\\s+version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList

let private templatePins () =
    Regex.Matches(propsText, "<PackageVersion\\s+Include=\"(FS\\.GG\\.UI\\.[^\"]+)\"\\s+Version=\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value)
    |> Seq.toList

let private templateExpected =
    Set.ofList
        [ "FS.GG.UI.Build"; "FS.GG.UI.Scene"; "FS.GG.UI.SkiaViewer"; "FS.GG.UI.Elmish"
          "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"; "FS.GG.UI.Controls"; "FS.GG.UI.Controls.Elmish"
          "FS.GG.UI.DesignSystem"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Testing" ]

[<Tests>]
let feature209VersionCoherenceTests =
    testList "Feature209 version coherence (structural verdict mirror)" [

        // T008 — comparator self-check on the exact spec edge pairs (preview-aware, not string compare).
        test "preview-aware comparator orders the spec edge pairs" {
            Expect.isTrue (cmp "0.1.9-preview.1" "0.1.10-preview.1" < 0) "0.1.9-preview.1 < 0.1.10-preview.1 (numeric core, not lexical)"
            Expect.isTrue (cmp "0.1.51-preview.1" "0.1.51-preview.2" < 0) "…-preview.1 < …-preview.2"
            Expect.isTrue (cmp "0.1.51-preview.1" "0.1.51-preview.1" = 0) "equal versions compare equal"
        }

        // Scenario A / US1 #3 — the coherent baseline: single literal, pin == an existing tag and not
        // lagging the latest.
        test "coherent baseline: single literal, pin matches latest snapshot tag (no lag, no phantom)" {
            let tags = tagVersions ()
            Expect.equal pinOccurrences 1 "exactly one <FsGgUiVersion> literal"
            Expect.isNonEmpty tags "fs-gg-ui/v* tags must be visible (fetch-depth: 0); empty ⇒ fail closed"
            let latest = tags |> List.sortWith cmp |> List.last
            Expect.isFalse (cmp pinVersion latest < 0) (sprintf "pin %s must not lag latest tag %s (pin-lags-tag)" pinVersion latest)
            Expect.isTrue (List.contains pinVersion tags) (sprintf "pin %s must match an existing fs-gg-ui/v* tag (pin-no-tag)" pinVersion)
        }

        // Scenario B / T013 — the forced 204-lag fixture goes red (preview-aware).
        test "fixture: a lagging pin is detected as pin-lags-tag" {
            let tags = tagVersions ()
            let latest = tags |> List.sortWith cmp |> List.last
            Expect.isTrue (cmp "0.1.0-preview.1" latest < 0) "the 204 stale pin lags the latest tag"
        }

        // Scenario E / T012 — a phantom pin (ahead of every tag) has no snapshot tag.
        test "fixture: a phantom pin has no snapshot tag" {
            let tags = tagVersions ()
            Expect.isFalse (List.contains "0.1.99-preview.1" tags) "0.1.99-preview.1 is a phantom (no fs-gg-ui/v tag)"
        }

        // US2 / FR-003/004 — BOM token + bracket + member parity (policy-independent, structural).
        test "BOM: single [$version$] token, exact bracket, B.ids == P.members" {
            let deps = bomDeps ()
            let ids = deps |> List.map fst |> Set.ofList
            let members = discoveredMembers ()
            Expect.equal ids members "BOM dependency-id set must equal the discovered packable FS.GG.UI.* set"
            for id, v in deps do
                Expect.equal v "[$version$]" (sprintf "%s must use the single [$version$] token" id)
                Expect.isTrue (v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ",")) (sprintf "%s must be exact-bracket" id)
        }

        // US2 / FR-005/D6 — template pins all derive, ⊆ published, == the 11-member manifest.
        test "template pins all derive through $(FsGgUiVersion) and equal the 11-member manifest" {
            let pins = templatePins ()
            let ids = pins |> List.map fst |> Set.ofList
            let members = discoveredMembers ()
            for id, v in pins do
                Expect.equal v "$(FsGgUiVersion)" (sprintf "%s must derive through $(FsGgUiVersion), not a hardcoded literal" id)
            Expect.isTrue (Set.isSubset ids members) "consumed pins ⊆ published members"
            Expect.equal ids templateExpected "consumed set must equal the documented 11-member manifest"
        }

        // FR-005 — build.fsx's runtime regex still matches the literal (208 half-rename class).
        test "build.fsx runtime regex still resolves the literal" {
            let buildText = File.ReadAllText(repo "template/base/build.fsx")
            Expect.isTrue (Regex.IsMatch(buildText, "<FsGgUiVersion>\\(\\[\\^<\\]\\+\\)</FsGgUiVersion>")) "build.fsx keeps the resolution regex"
            Expect.isTrue (Regex.IsMatch(propsText, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")) "the literal still matches that regex"
        }
    ]
