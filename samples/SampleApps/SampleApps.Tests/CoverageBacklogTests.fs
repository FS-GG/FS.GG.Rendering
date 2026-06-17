module SampleApps.Tests.CoverageBacklogTests

open System
open System.IO
open Expecto
open FS.GG.UI.Controls
open SampleApps.Core
open SampleApps.Core.Harness

/// FR-011 / FR-012 / SC-004 / SC-005: per-sample coverage + the 22-spec backlog are honest and
/// machine-checked — no dangling control id, input union complete, all 22 specs dispositioned,
/// the 6 adopted match the registry, and the committed report matches the rendered text.

/// Walk up from the test assembly to find the committed report (the tree root).
let rec private findUp (dir: DirectoryInfo) (name: string): string option =
    let candidate = Path.Combine(dir.FullName, name)
    if File.Exists candidate then
        Some candidate
    else
        match Option.ofObj dir.Parent with
        | Some parent -> findUp parent name
        | None -> None

[<Tests>]
let coverageBacklogTests =
    testList "CoverageBacklog" [
        test "the honesty check passes (all R-C/R-B rules clean)" {
            let r = Coverage.check ()
            Expect.isEmpty r.DanglingControls "no dangling control ids (R-C2)"
            Expect.isEmpty r.MissingInputs "input union spans keyboard+pointer+timing-step (R-C3)"
            Expect.isEmpty r.UnaccountedSpecs "22 specs, no dup, all dispositioned (R-B1/B2/B4)"
            Expect.isEmpty r.AdoptedMismatch "6 adopted map 1:1 to the registry (R-B3)"
            Expect.isTrue (Coverage.isClean r) "the report is clean"
        }

        test "exactly 22 backlog specs: 12 game + 10 productivity, no duplicates (R-B1/B4)" {
            Expect.equal (List.length Coverage.backlog) 22 "22 specs"
            let games = Coverage.backlog |> List.filter (fun b -> b.Family = "game") |> List.length
            let prod = Coverage.backlog |> List.filter (fun b -> b.Family = "productivity") |> List.length
            Expect.equal games 12 "12 games"
            Expect.equal prod 10 "10 productivity"
            let specs = Coverage.backlog |> List.map (fun b -> b.Spec)
            Expect.equal (List.length (List.distinct specs)) 22 "no duplicate spec"
            for b in Coverage.backlog do
                Expect.isTrue (b.Disposition = "Adopted" || b.Disposition = "Deferred") (sprintf "%s dispositioned" b.Spec)
                Expect.isNotEmpty (b.Reason.Trim()) (sprintf "%s has a reason" b.Spec)
        }

        test "exactly 6 adopted, matching the 6-entry registry (R-B3)" {
            let adopted = Coverage.backlog |> List.filter (fun b -> b.Disposition = "Adopted")
            Expect.equal (List.length adopted) 6 "6 adopted specs"
            Expect.equal (List.length Registry.all) 6 "registry has 6 entries"
        }

        test "every coverage-row control id is a real catalog control (R-C2)" {
            let catalog = Catalog.supportedControls |> List.map (fun d -> d.Id) |> Set.ofList
            for row in Coverage.coverageRows do
                for c in row.Controls do
                    Expect.isTrue (catalog.Contains c) (sprintf "%s renders catalog control '%s'" row.SampleId c)
        }

        test "registry invariants: 6 entries, unique ids, 3 game + 3 productivity (sample-registry.md)" {
            let ids = Registry.all |> List.map (fun (e: SampleEntry) -> e.Id)
            Expect.equal (List.length ids) 6 "six entries"
            Expect.equal (List.length (List.distinct ids)) 6 "unique ids"
            let expectedIds = Set.ofList [ "tetris"; "snake"; "pong"; "kanban"; "todo"; "calendar" ]
            Expect.equal (Set.ofList ids) expectedIds "ids are the curated six"
            let games = Registry.all |> List.filter (fun e -> e.Family = "game") |> List.length
            let prod = Registry.all |> List.filter (fun e -> e.Family = "productivity") |> List.length
            Expect.equal games 3 "3 games"
            Expect.equal prod 3 "3 productivity"
            for e in Registry.all do
                Expect.isNonEmpty e.Outcome.Values (sprintf "%s has a non-empty authored outcome" e.Id)
        }

        test "committed coverage-backlog.md matches Coverage.render () (no drift, T035/SC-005)" {
            match findUp (DirectoryInfo(AppContext.BaseDirectory)) "coverage-backlog.md" with
            | Some path -> Expect.equal (File.ReadAllText path) (Coverage.render ()) "committed report == rendered report"
            | None -> failtest "committed coverage-backlog.md not found alongside the sample tree"
        }
    ]
