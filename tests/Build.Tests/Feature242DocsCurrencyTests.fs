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

        test "README/usage.md reflect the shipped release pipeline (nuget.org, no stale feed/version)" {
            for doc in frontDoorDocs do
                let f = flat doc
                Expect.stringContains f "nuget.org" (sprintf "%s must disclose the public nuget.org feed" doc)
                Expect.isFalse (f.Contains "0.1.0-preview.1") (sprintf "%s pins the stale 0.1.0-preview.1 version" doc)
                Expect.isFalse (f.Contains "not on a public feed yet") (sprintf "%s still says not on a public feed" doc)
                Expect.isFalse (f.Contains "not yet on a public nuget feed") (sprintf "%s still says not yet on a public feed" doc)
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
