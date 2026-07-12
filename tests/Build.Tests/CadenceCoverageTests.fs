module CadenceCoverageTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto

// Feature 235 (FR-004/FR-005) — the permanent closure for the "test project in the solution but in
// NO CI cadence" class (repo review P4 / #47). These are straight filesystem assertions over real
// committed artifacts (no mocks): the slnx membership, the required gate workflow, and the two
// auditable cadence docs. Together they guarantee the gate's test list == the slnx test set, so a
// newly added test project cannot silently run in zero cadences again.

let private repoRoot =
    let rec up (dir: DirectoryInfo | null) =
        match dir with
        | null -> failwith "could not locate repo root (FS.GG.Rendering.slnx) walking up from test base dir"
        | d ->
            if File.Exists(Path.Combine(d.FullName, "FS.GG.Rendering.slnx")) then d.FullName
            else up d.Parent
    up (DirectoryInfo(AppContext.BaseDirectory))

let private repoPath (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

// Every `tests/<Name>.Tests` project the slnx builds — the authoritative test set the gate must cover.
// (`TestSupport` is a helper lib, not a `.Tests` project, so it does not appear here.)
//
// #540 — `Package.Tests` IS in this set now, and the comment that used to sit here ("release-only and
// NOT in the slnx, so it does not appear here — exactly the gate's coverage boundary") was naming the
// hole rather than a boundary. This file exists to close the "test project in the solution but in NO CI
// cadence" class; a project OUTSIDE the solution ran in no cadence either, and was invisible to the very
// guard written to catch that. Its ~325 working-tree rules — R-CAT, R-PROF, the manifest digests, the
// capability catalog — all fired after the merge that broke them, never on the PR.
let private slnxTestNames =
    let suffix = ".Tests.fsproj"
    let slnx = File.ReadAllText(repoPath "FS.GG.Rendering.slnx")
    Regex.Matches(slnx, "Path=\"([^\"]+\\.fsproj)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value.Replace('\\', '/'))
    |> Seq.filter (fun p -> p.StartsWith("tests/") && p.EndsWith(suffix))
    // `tests/<Name>.Tests/<Name>.Tests.fsproj` → the bare `<Name>` (GL_TEST_PROJECTS uses bare names).
    |> Seq.map (fun p ->
        let seg = p.Substring(p.LastIndexOf('/') + 1)
        seg.Substring(0, seg.Length - suffix.Length))
    |> Set.ofSeq

// The single GL_TEST_PROJECTS source of truth declared at the gate job level (gate.yml). Both the
// deterministic tier (which excludes them) and the GL step (which runs them) read this one list.
let private gateYml = File.ReadAllText(repoPath ".github/workflows/gate.yml")

let private glTestNames =
    let m = Regex.Match(gateYml, "GL_TEST_PROJECTS:\\s*\"([^\"]*)\"")
    Expect.isTrue m.Success "gate.yml must declare a GL_TEST_PROJECTS env list"
    m.Groups.[1].Value.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries) |> Set.ofArray

// Every `X.Tests` name a doc legitimately names besides the slnx set: the release-only lanes.
//
// `Package` came OFF this list in #540. It is now a slnx member and runs on the gate, so it is no longer
// release-ONLY — it is a project with two tiers: a hermetic default tier that runs on every PR, and a
// release tier its own env flags defer (see tests/Package.Tests/Tests.fs). Leaving it here would have
// been harmless to the assertions and a lie to the reader, which is precisely the failure #540 is about:
// the timing of a rule was a thing you discovered from a comment, days after it did not run for you.
//
// `Product` stays: the generated product's test project is instantiated from the template at release
// time and exists in no checkout of this repo.
let private releaseOnlyTestNames = Set.ofList [ "Product" ]

let private retiredTestNames = Set.ofList [ "Color"; "Input" ]

// All backtick-quoted `X.Tests` tokens a doc mentions, returned as bare names (no `.Tests`).
let private docTestNames (relDoc: string) =
    let text = File.ReadAllText(repoPath relDoc)
    Regex.Matches(text, "`([A-Za-z][A-Za-z.]*)\\.Tests`")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

[<Tests>]
let cadenceCoverageTests =
    testList "Feature 235 — CI cadence coverage" [

        test "every slnx test project is covered by exactly one gate lane (deterministic ∪ GL == slnx)" {
            // GL_TEST_PROJECTS may not name a phantom (a project not in the slnx)...
            let phantomGl = Set.difference glTestNames slnxTestNames
            Expect.isEmpty phantomGl (sprintf "GL_TEST_PROJECTS names non-slnx project(s): %A" phantomGl)

            // ...and the deterministic tier (slnx − GL) plus the GL step exactly reconstitute the
            // slnx test set, with no overlap. This is what "gate's test list == slnx test folder"
            // means: no member is dropped, none is double-run.
            let deterministic = Set.difference slnxTestNames glTestNames
            Expect.isEmpty (Set.intersect deterministic glTestNames) "a project cannot be both deterministic and GL"
            Expect.equal (Set.union deterministic glTestNames) slnxTestNames "deterministic ∪ GL must equal the slnx test set"
            Expect.isNonEmpty deterministic "the deterministic tier must be non-empty"
        }

        test "the deterministic tier is DERIVED from the slnx, not a hardcoded project list" {
            // Guards against a regression back to `for p in Scene Layout ...`: the loop MUST read the
            // slnx so new test projects are covered by construction (the original P4 bug).
            Expect.isTrue
                (gateYml.Contains "grep -oE 'tests/[^\"]+\\.Tests\\.fsproj' FS.GG.Rendering.slnx")
                "gate.yml deterministic tier must derive its project list from FS.GG.Rendering.slnx"
            Expect.isFalse
                (Regex.IsMatch(gateYml, "for p in Scene Layout"))
                "gate.yml must not hardcode the deterministic project list"
        }

        // Both auditable cadence docs must stay coherent with the slnx in BOTH directions (the P4
        // drift): every slnx test project is named, and no name is stale (retired or non-existent).
        for doc in [ "docs/ci/cadence-map.md"; "docs/validation/validation-set.md" ] do
            test (sprintf "%s names every slnx test project and no stale ones" doc) {
                let named = docTestNames doc

                let missing = Set.difference slnxTestNames named
                Expect.isEmpty missing (sprintf "%s omits slnx test project(s): %A" doc missing)

                let retiredStillListed = Set.intersect named retiredTestNames
                Expect.isEmpty retiredStillListed (sprintf "%s still lists retired project(s): %A" doc retiredStillListed)

                // Any other `X.Tests` a doc names must be a real slnx member or a known release-only lane.
                let unknown = Set.difference named (Set.unionMany [ slnxTestNames; releaseOnlyTestNames ])
                Expect.isEmpty unknown (sprintf "%s names unknown/stale test project(s): %A" doc unknown)
            }
    ]
