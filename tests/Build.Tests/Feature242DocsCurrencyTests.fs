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
// written in the doc), or None if the pointer prose is absent. Scoped to the machine-managed
// <!-- SPECKIT START -->..<!-- SPECKIT END --> block so an unrelated specs/*/plan.md reference
// elsewhere in the doc cannot be mistaken for the current-plan pointer.
let private claudeMdPlanPointer =
    let text = File.ReadAllText(repoPath "CLAUDE.md")
    let block =
        let b = Regex.Match(text, "<!--\\s*SPECKIT START\\s*-->(.*?)<!--\\s*SPECKIT END\\s*-->", RegexOptions.Singleline)
        if b.Success then b.Groups.[1].Value else text
    let m = Regex.Match(block, "(specs/\\S+?/plan\\.md)")
    if m.Success then Some(m.Groups.[1].Value) else None

// The BOM metapackage (`FS.GG.UI`, source module `Meta`) is a packable product but not a library —
// the docs phrase it as "N libraries plus the BOM metapackage". So the library count the front-door
// prose must state is every packable id EXCEPT the BOM, derived from the slnx (not a frozen literal).
let private bomPackageId = "FS.GG.UI"
let private libraryCount = packableIds |> Set.remove bomPackageId |> Set.count

// The library count each front-door doc states in its "N libraries plus/+ the … BOM (meta)package"
// prose, tolerant of both phrasings (README/usage "N libraries plus the `FS.GG.UI` BOM"; module-map
// "N libraries + the BOM metapackage") and of a backtick-quoted BOM id between the two.
let private libraryCountsIn (text: string) =
    [ for m in Regex.Matches(text, "(\\d+)\\s+librar\\w+\\s+(?:plus|\\+)\\s+the\\b[^\\n]{0,40}?\\bBOM") ->
        int m.Groups.[1].Value ]

let private docLibraryCounts (relDoc: string) = libraryCountsIn (File.ReadAllText(repoPath relDoc))

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
let private versionMentionsIn (text: string) =
    [ for m in Regex.Matches(text, "framework version `([^`]+)`") -> m.Groups.[1].Value
      for m in Regex.Matches(text, "dotnet add package FS\\.GG\\.UI\\.\\S+\\s+--version\\s+(\\S+)") -> m.Groups.[1].Value ]

let private docVersionMentions (relDoc: string) = versionMentionsIn (File.ReadAllText(repoPath relDoc))

// #966 / #968 option 2 — the front-door version/count are no longer HAND-TYPED literals: they are
// rendered from source into `<!-- BEGIN/END GENERATED: fsgg-doc:<name> -->` regions by
// scripts/generate-doc-fragments.fsx (mirroring the M6 render-from-registry pattern). The re-aim of
// F-DOCS-1/F-DOCS-2 below asserts the value lives INSIDE such a generated fragment and is current
// (generated-from-source AND current), not merely that some literal is present. The anti-drift
// guarantee is not weakened — it is strengthened: a value stated OUTSIDE a generated fragment now
// FAILS, because that is exactly the hand-typed literal M2 forbids and this closes.
let private generatedRegionRx =
    Regex("<!--\\s*BEGIN GENERATED: fsgg-doc:[^\\n]*?-->(.*?)<!--\\s*END GENERATED: fsgg-doc:[^\\n]*?-->", RegexOptions.Singleline)

// The concatenated interiors of every generated `fsgg-doc:` fragment in the doc.
let private generatedRegionText (relDoc: string) =
    let text = File.ReadAllText(repoPath relDoc)
    [ for m in generatedRegionRx.Matches(text) -> m.Groups.[1].Value ] |> String.concat "\n"

// The doc with every generated fragment (its markers and interior) removed — the hand-authored
// residue, which must state no version/count of its own.
let private docOutsideGeneratedRegions (relDoc: string) =
    generatedRegionRx.Replace(File.ReadAllText(repoPath relDoc), "\n")

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

        // F-DOCS-1 (re-aimed by #966 / #968 option 2): the front-door version must be RENDERED FROM
        // SOURCE into a generated fragment, not hand-typed. The old gate asserted `doc == source` for
        // any version literal present; that caught drift but still let a HUMAN re-type the literal on
        // every bump (the anti-pattern M2 forbids). Now: (a) the version is stated INSIDE a generated
        // `fsgg-doc:` fragment and equals the pin, and (b) NO version is stated outside a fragment —
        // so the value can only come from scripts/generate-doc-fragments.fsx, never a hand edit.
        test "README/usage.md quote the shipped FS.GG.UI version via a generated fragment (not a hand-typed literal)" {
            for doc in frontDoorDocs do
                // (a) present-and-current: the version lives inside a generated fragment and equals the pin.
                let regionVersions = versionMentionsIn (generatedRegionText doc)
                Expect.isNonEmpty regionVersions
                    (sprintf "%s states no FS.GG.UI version inside a generated fsgg-doc fragment (scripts/generate-doc-fragments.fsx) for the currency gate to check" doc)
                for v in regionVersions do
                    Expect.equal v fsGgUiVersion
                        (sprintf "%s's generated fragment states version %s but the pin is $(FsGgUiVersion)=%s — run scripts/generate-doc-fragments.fsx and commit" doc v fsGgUiVersion)
                // (b) no hand-typed literal: nothing outside a generated fragment may state a version.
                let strayVersions = versionMentionsIn (docOutsideGeneratedRegions doc)
                Expect.isEmpty strayVersions
                    (sprintf "%s states FS.GG.UI version(s) %A OUTSIDE a generated fragment — move them into a fsgg-doc region so they render from source (#966/#968)" doc strayVersions)
        }

        // F-DOCS-2 (re-aimed by #966 / #968 option 2): the front-door "N libraries plus the BOM"
        // count must be RENDERED FROM the slnx into a generated fragment, not hand-typed. Same shape
        // as F-DOCS-1: (a) the count lives inside a generated `fsgg-doc:` fragment and equals the
        // slnx-derived count, and (b) no count is stated outside a fragment. Adding/retiring a library
        // moves the count via scripts/generate-doc-fragments.fsx, and a hand-typed count now FAILS.
        for doc in frontDoorDocs do
            test (sprintf "%s states the library count via a generated fragment (derived from the slnx)" doc) {
                let regionCounts = libraryCountsIn (generatedRegionText doc)
                Expect.isNonEmpty regionCounts
                    (sprintf "%s states no 'N libraries plus the BOM' count inside a generated fsgg-doc fragment for the currency gate to check" doc)
                for n in regionCounts do
                    Expect.equal n libraryCount
                        (sprintf "%s's generated fragment states %d libraries but the slnx ships %d (packable minus the %s BOM) — run scripts/generate-doc-fragments.fsx and commit"
                            doc n libraryCount bomPackageId)
                let strayCounts = libraryCountsIn (docOutsideGeneratedRegions doc)
                Expect.isEmpty strayCounts
                    (sprintf "%s states library count(s) %A OUTSIDE a generated fragment — move them into a fsgg-doc region so they render from source (#966/#968)" doc strayCounts)
            }

        // docs/product/module-map.md is a PRODUCT doc, not one of the front-door docs #966 converts to
        // generated fragments, so it keeps the derive-and-check gate: its count must equal the
        // slnx-derived count (whether or not it is behind a generated fragment).
        test "docs/product/module-map.md states the shipped library count (derived from the slnx)" {
            let counts = docLibraryCounts "docs/product/module-map.md"
            Expect.isNonEmpty counts
                "docs/product/module-map.md states no 'N libraries plus the BOM' count for the currency gate to check"
            for n in counts do
                Expect.equal n libraryCount
                    (sprintf "docs/product/module-map.md states %d libraries but the slnx ships %d (packable minus the %s BOM)"
                        n libraryCount bomPackageId)
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
