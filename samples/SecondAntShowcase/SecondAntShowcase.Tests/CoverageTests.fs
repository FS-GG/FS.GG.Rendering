module SecondAntShowcase.Tests.CoverageTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

/// FR-003 / SC-001: every catalog control maps to exactly one Catalog-kind page, and the
/// assigned ids equal `Catalog.supportedControls` (96), with zero unreferenced/duplicated.
[<Tests>]
let coverageTests =
    testList "Coverage" [
        test "every catalog control maps to exactly one page (0 unreferenced, 0 duplicated)" {
            let result = CoverageMap.check ()
            Expect.isEmpty result.Unreferenced "no unreferenced catalog controls"
            Expect.isEmpty result.Duplicated "no duplicated controls"
            Expect.isTrue (CoverageMap.isClean result) "coverage is a clean bijection"
        }

        test "the catalog (live count) maps bijectively onto the Catalog pages" {
            let catalog = CoverageMap.catalogIds ()
            let assigned = CoverageMap.assignedIds ()
            Expect.equal (List.length catalog) 96 "96 catalog controls"
            Expect.equal (List.length assigned) (List.length catalog) "assigned slots == catalog count"
            Expect.equal (List.length (List.distinct assigned)) (List.length assigned) "all assigned ids distinct"
            Expect.equal (Set.ofList catalog) (Set.ofList assigned) "catalog set equals assigned set"
        }

        test "13 catalog pages + 6 template pages = 19 pages" {
            Expect.equal (List.length PageRegistry.catalogPages) 13 "13 catalog pages"
            Expect.equal (List.length PageRegistry.templatePages) 6 "6 template pages"
            Expect.equal (List.length PageRegistry.all) 19 "19 pages total"
        }

        test "template pages are exempt: they carry no ControlIds" {
            for p in PageRegistry.templatePages do
                Expect.isEmpty p.ControlIds (sprintf "template %s has empty ControlIds" p.Id)
        }

        test "every catalog page has a non-empty control list" {
            for page in PageRegistry.catalogPages do
                Expect.isNonEmpty page.ControlIds (sprintf "catalog page %s has controls" page.Id)
        }
    ]
