module ControlsGallery.Tests.CoverageTests

open Expecto
open ControlsGallery.Core

/// FR-003 / SC-001: the 52 catalog controls map 1:1 onto the 10 pages.
[<Tests>]
let coverageTests =
    testList "Coverage" [
        test "every catalog control maps to exactly one page (0 unreferenced, 0 duplicated)" {
            let result = CoverageMap.check ()
            Expect.isEmpty result.Unreferenced "no unreferenced catalog controls"
            Expect.isEmpty result.Duplicated "no duplicated controls"
            Expect.isTrue (CoverageMap.isClean result) "coverage is a clean bijection"
        }

        test "52 catalog controls across exactly 10 pages, bijective" {
            let catalog = CoverageMap.catalogIds ()
            let assigned = CoverageMap.assignedIds ()
            Expect.equal (List.length catalog) 52 "52 catalog controls"
            Expect.equal (List.length Pages.all) 10 "exactly 10 pages"
            Expect.equal (List.length assigned) 52 "52 assigned slots"
            Expect.equal (List.length (List.distinct assigned)) 52 "52 distinct assigned ids"
            Expect.equal (Set.ofList catalog) (Set.ofList assigned) "catalog set equals assigned set"
        }

        test "page indices are exactly 1..10, contiguous and unique" {
            let indices = Pages.all |> List.map (fun p -> p.Index) |> List.sort
            Expect.equal indices [ 1 .. 10 ] "indices are 1..10"
        }

        test "every page has a non-empty control list" {
            for page in Pages.all do
                Expect.isNonEmpty page.ControlIds (sprintf "page %s has controls" page.Id)
        }
    ]
