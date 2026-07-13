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

/// The committed report. `SampleApps.Tests.fsproj` copies `../coverage-backlog.md` — the one at the
/// sample tree root, next to `Directory.Build.props` — into the build output, so the file this suite
/// judges is PLACED by the build rather than hunted for at runtime.
///
/// This used to ascend from `AppContext.BaseDirectory` looking for the nearest ancestor holding a
/// file called `coverage-backlog.md` (#725). Two reasons that is gone, and the second is the one that
/// matters:
///
///   1. A sample must not hand-roll a root walk. `tests/TestSupport/RepositoryRoot.fs` is the repo's
///      one finder, and a sample structurally cannot consume it — the sample references the published
///      packages, not this repo's projects, and a `ProjectReference` to reach the finder would
///      puncture the shadowing isolation the sample exists to prove. The way out of that bind is not
///      a second finder; it is not needing one.
///   2. A marker-file walk fails loud when it finds NOTHING, but it cannot fail loud about finding
///      the WRONG file — it binds to the nearest ancestor copy and compares against that. The name is
///      not unique in this repo (`specs/134-sample-apps-g2/contracts/coverage-backlog.md` is another),
///      so the walk's correctness rested on no copy ever appearing between the test binary and the
///      sample root. An MSBuild copy names the file it means, once, and cannot drift onto another.
let private committedReport = Path.Combine(AppContext.BaseDirectory, "coverage-backlog.md")

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
            // Fail-loud on the COPY, before the comparison. If the `<None Include="..\coverage-backlog.md"
            // CopyToOutputDirectory="PreserveNewest" />` item is ever dropped from the fsproj, this suite
            // must say so in one line — not throw a bare FileNotFoundException, and above all not tempt
            // the next reader into "fixing" it by re-adding an ancestor walk.
            Expect.isTrue
                (File.Exists committedReport)
                (sprintf
                    "coverage-backlog.md was not copied next to the test binary (looked in %s). The fsproj item that places it is missing — restore it: <None Include=\"..\\coverage-backlog.md\" Link=\"coverage-backlog.md\" CopyToOutputDirectory=\"PreserveNewest\" />. Do NOT replace it with a walk up the tree."
                    AppContext.BaseDirectory)

            Expect.equal (File.ReadAllText committedReport) (Coverage.render ()) "committed report == rendered report"
        }
    ]
