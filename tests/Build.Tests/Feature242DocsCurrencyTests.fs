module Feature242DocsCurrencyTests

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

// Feature 242 (repo review P11 / X1, X2, #54) — the permanent closure for the "front-door narrative
// drifted from the shipped pipeline" class. The 2026-07-02 review's meta-observation was that
// documents WITH a gate stayed true while narrative snapshots rotted; README/usage.md/module-map.md
// had no gate and rotted (stale feed status and version, retired Color/Input modules, 8 of 17
// packages listed). This test is that missing gate: straight filesystem assertions over the slnx's
// packable projects and the three front-door docs, so the package map and theme/feed status cannot
// silently fall out of sync with what the repo actually ships again.

let private repoRoot = RepositoryRoot.value

let private repoPath (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

// Every `src/**/*.fsproj` the slnx builds — parsed the same way CadenceCoverageTests parses the test set.
let private slnxSrcPaths =
    let slnx = File.ReadAllText(repoPath "FS.GG.Rendering.slnx")
    Regex.Matches(slnx, "Path=\"([^\"]+\\.fsproj)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value.Replace('\\', '/'))
    |> Seq.filter (fun p -> p.StartsWith("src/") && p.EndsWith(".fsproj"))
    |> List.ofSeq

// The PackageId of a packable src project. Non-packable projects (e.g. the internal `ColorPolicy`
// preserved by feature 179) return None; a packable project WITHOUT an explicit <PackageId> is a
// fail-loud, not a silent skip.
let private packageIdOf (relFsproj: string) : string option =
    let text = File.ReadAllText(repoPath relFsproj)
    if not (text.Contains "<IsPackable>true</IsPackable>") then None
    else
        let m = Regex.Match(text, "<PackageId>([^<]+)</PackageId>")
        if m.Success then Some(m.Groups.[1].Value.Trim())
        else failwithf "packable project %s declares no <PackageId>" relFsproj

// The authoritative set of shipped package ids — what the front-door docs must account for.
let private packableIds = slnxSrcPaths |> List.choose packageIdOf |> Set.ofList

// Feature 179 retired these two orphaned packages. They may appear only in module-map's explicit
// "Retired modules" section, never as a live package in README/usage.md.
let private retiredIds = Set.ofList [ "FS.GG.UI.Color"; "FS.GG.UI.Input" ]

// Whitespace-collapsed, lower-cased document text so a wrapped/edited phrase still matches.
let private flat (relDoc: string) =
    Regex.Replace(File.ReadAllText(repoPath relDoc), "\\s+", " ").ToLowerInvariant()

// A backtick-quoted token is present verbatim (the package-map convention in both docs).
let private mentions (relDoc: string) (token: string) =
    File.ReadAllText(repoPath relDoc).Contains(sprintf "`%s`" token)

let private frontDoorDocs = [ "README.md"; "docs/usage.md" ]

// F-DOCS-3: CLAUDE.md's SPECKIT-managed pointer ("read the current plan at specs/<id>/plan.md")
// is a narrative snapshot with no gate — it drifted to 251 while 253 had landed. Rather than pin a
// literal spec id, DERIVE the expected pointer from source: the highest-numbered spec directory that
// actually has a plan.md (specs are numbered monotonically by speckit-git-feature, so the highest is
// the most recently planned). This keeps the pointer both current AND non-dangling — a spec dir with
// no plan.md (e.g. 254) can never be the target.
let private specsDir = Path.Combine(repoRoot, "specs")

let private leadingNumber (dirName: string) =
    match Regex.Match(dirName, "^(\\d+)-") with
    | m when m.Success -> Some(int m.Groups.[1].Value, dirName)
    | _ -> None

// The relative "specs/<id>/plan.md" pointer the current plan lives behind, from source of truth.
let private latestPlannedSpecPointer =
    Directory.GetDirectories(specsDir)
    |> Array.choose (fun d ->
        match Path.GetFileName(d) with
        | null | "" -> None
        | name -> leadingNumber name)
    |> Array.filter (fun (_, name) -> File.Exists(Path.Combine(specsDir, name, "plan.md")))
    |> Array.sortByDescending fst
    |> Array.tryHead
    |> Option.map (fun (_, name) -> sprintf "specs/%s/plan.md" name)

// The "specs/<id>/plan.md" path CLAUDE.md's SPECKIT block currently points at (forward slashes as
// written in the doc), or None if the pointer prose is absent.
let private claudeMdPlanPointer =
    let text = File.ReadAllText(repoPath "CLAUDE.md")
    let m = Regex.Match(text, "(specs/\\S+?/plan\\.md)")
    if m.Success then Some(m.Groups.[1].Value) else None

// The BOM metapackage (`FS.GG.UI`, source module `Meta`) is a packable product but not a library —
// the docs phrase it as "N libraries plus the BOM metapackage". So the library count the front-door
// prose must state is every packable id EXCEPT the BOM, derived from the slnx (not a frozen literal).
let private bomPackageId = "FS.GG.UI"
let private libraryCount = packableIds |> Set.remove bomPackageId |> Set.count

// The library count each front-door doc states in its "N libraries plus/+ the … BOM (meta)package"
// prose, tolerant of both phrasings (README/usage "N libraries plus the `FS.GG.UI` BOM"; module-map
// "N libraries + the BOM metapackage") and of a backtick-quoted BOM id between the two.
let private docLibraryCounts (relDoc: string) =
    let text = File.ReadAllText(repoPath relDoc)
    [ for m in Regex.Matches(text, "(\\d+)\\s+librar\\w+\\s+(?:plus|\\+)\\s+the\\b[^\\n]{0,40}?\\bBOM") ->
        int m.Groups.[1].Value ]

// The authoritative FS.GG.UI version — the pin the release actually publishes, read from the same
// source of truth the template hands a scaffolded product. The front-door docs must quote THIS, so
// the version prose cannot drift from what the repo ships (F-DOCS-1).
let private fsGgUiVersion =
    let props = File.ReadAllText(repoPath "template/base/Directory.Packages.props")
    let m = Regex.Match(props, "<FsGgUiVersion>([^<]+)</FsGgUiVersion>")
    if m.Success then m.Groups.[1].Value.Trim()
    else failwith "<FsGgUiVersion> not found in template/base/Directory.Packages.props"

// Every FS.GG.UI version string the front-door docs state, in the two forms they use: the
// backtick-quoted "framework version `X`" prose and the copy-paste "dotnet add package
// FS.GG.UI.… --version X" command. Scoped to FS.GG.UI so an Audio/Game install example (a
// different release axis) is not wrongly held to $(FsGgUiVersion).
let private docVersionMentions (relDoc: string) =
    let text = File.ReadAllText(repoPath relDoc)
    [ for m in Regex.Matches(text, "framework version `([^`]+)`") -> m.Groups.[1].Value
      for m in Regex.Matches(text, "dotnet add package FS\\.GG\\.UI\\.\\S+\\s+--version\\s+(\\S+)") -> m.Groups.[1].Value ]

[<Tests>]
let docsCurrencyTests =
    testList "Feature 242 — front-door docs currency" [

        test "the packable set is non-empty and complete (sanity on the slnx parse)" {
            Expect.isNonEmpty packableIds "no packable src projects parsed from the slnx"
            // The BOM metapackage and both theme packages are the ones the review flagged as missing.
            [ "FS.GG.UI"; "FS.GG.UI.Themes.Default"; "FS.GG.UI.Themes.AntDesign" ]
            |> List.iter (fun id -> Expect.isTrue (Set.contains id packableIds) (sprintf "expected %s in the packable set" id))
        }

        // The core anti-"8 of 17" gate: both the consumer package map and the ownership map must list
        // EVERY packable package by id, so a new (or removed) product forces a doc edit.
        for doc in [ "docs/usage.md"; "docs/product/module-map.md" ] do
            test (sprintf "%s lists every packable package id" doc) {
                let missing = packableIds |> Set.filter (fun id -> not (mentions doc id))
                Expect.isEmpty missing (sprintf "%s omits packable package(s): %A" doc missing)
            }

        test "README/usage.md do not present a retired package as live" {
            for doc in frontDoorDocs do
                let text = File.ReadAllText(repoPath doc)
                for id in retiredIds do
                    Expect.isFalse (text.Contains id) (sprintf "%s references retired package %s" doc id)
        }

        test "README/usage.md reflect the shipped release pipeline (nuget.org, no stale feed)" {
            for doc in frontDoorDocs do
                let f = flat doc
                Expect.stringContains f "nuget.org" (sprintf "%s must disclose the public nuget.org feed" doc)
                Expect.isFalse (f.Contains "not on a public feed yet") (sprintf "%s still says not on a public feed" doc)
                Expect.isFalse (f.Contains "not yet on a public nuget feed") (sprintf "%s still says not yet on a public feed" doc)
        }

        // F-DOCS-1: the version prose must be DERIVED from the pin, not checked against a frozen
        // known-bad literal. The old gate banned exactly `0.1.0-preview.1`; the docs drifted to a
        // different stale string (`0.1.58-preview.1`) and sailed through green. Per the review's
        // meta-observation — assert `doc == source`, never `doc != known-bad-literal`.
        test "README/usage.md quote the shipped FS.GG.UI version, not a frozen literal" {
            for doc in frontDoorDocs do
                let mentions = docVersionMentions doc
                // Non-vacuous PER DOC: each front-door doc must actually state a version, so a doc
                // that silently drops its version literal cannot pass this gate by leaning on the other.
                Expect.isNonEmpty mentions (sprintf "%s states no FS.GG.UI version for the currency gate to check" doc)
                for v in mentions do
                    Expect.equal v fsGgUiVersion
                        (sprintf "%s states version %s but the pin is $(FsGgUiVersion)=%s" doc v fsGgUiVersion)
        }

        // F-DOCS-2: the "N libraries plus the BOM" count must be DERIVED from the slnx's packable
        // set, not a frozen literal — the prose had drifted to `17 libraries` while the repo ships 16
        // (17 packable products, one of which is the BOM). Per the meta-observation: assert
        // `doc == source`, so adding or retiring a library forces the count prose to move with it.
        for doc in [ "README.md"; "docs/usage.md"; "docs/product/module-map.md" ] do
            test (sprintf "%s states the shipped library count (derived from the slnx)" doc) {
                let counts = docLibraryCounts doc
                Expect.isNonEmpty counts
                    (sprintf "%s states no 'N libraries plus the BOM' count for the currency gate to check" doc)
                for n in counts do
                    Expect.equal n libraryCount
                        (sprintf "%s states %d libraries but the slnx ships %d (packable minus the %s BOM)"
                            doc n libraryCount bomPackageId)
            }

        // F-DOCS-3: CLAUDE.md points every agent at "the current plan"; the pointer must name the
        // latest-planned spec (highest-numbered spec dir with a plan.md), derived from source — not a
        // frozen id that rots as new features land. Per the meta-observation: assert `doc == source`.
        test "CLAUDE.md points at the latest planned spec's plan.md (derived from specs/)" {
            let expected =
                match latestPlannedSpecPointer with
                | Some p -> p
                | None -> failwith "no specs/<id>/plan.md found to derive the current-plan pointer from"
            // Non-vacuous: CLAUDE.md must actually state a plan pointer for the gate to check.
            let actual =
                match claudeMdPlanPointer with
                | Some p -> p
                | None -> failwith "CLAUDE.md states no 'specs/<id>/plan.md' pointer for the currency gate to check"
            Expect.isTrue (File.Exists(repoPath actual))
                (sprintf "CLAUDE.md points at %s, which does not exist" actual)
            Expect.equal actual expected
                (sprintf "CLAUDE.md points at %s but the latest planned spec is %s" actual expected)
        }

        test "the Ant Design theme is disclosed on the front door (README + usage + module map)" {
            for doc in [ "README.md"; "docs/usage.md"; "docs/product/module-map.md" ] do
                let f = flat doc
                Expect.stringContains f "ant design" (sprintf "%s must mention the Ant Design theme" doc)
                Expect.isTrue
                    (File.ReadAllText(repoPath doc).Contains "FS.GG.UI.Themes.AntDesign")
                    (sprintf "%s must name the FS.GG.UI.Themes.AntDesign package" doc)
        }
    ]
