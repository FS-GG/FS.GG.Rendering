module AntShowcase.Tests.VisualPageTests

open Expecto
open AntShowcase.Core
open AntShowcase.Tests.VisualTestHelpers

[<Tests>]
let visualPageTests =
    testList "VisualPage" [
        test "every live page has a visual profile" {
            for page in PageRegistry.all do
                Expect.isTrue (PageProfiles.hasProfile page.Id) (sprintf "profile exists for %s" page.Id)
        }

        test "large and dense pages declare dedicated demonstration regions" {
            let expected =
                [ "data-collections"
                  "charts-statistical"
                  "charts-advanced"
                  "graphs-custom"
                  "overlays"
                  "feedback-status" ]
            for pageId in expected do
                Expect.contains PageProfiles.largeRegionPageIds pageId (sprintf "%s has a large region" pageId)
        }

        test "every catalog page renders with readable profile metadata" {
            for page in PageRegistry.catalogPages do
                let profile = PageProfiles.byPageId page.Id
                let result = renderPage page
                Expect.isGreaterThan result.NodeCount 0 (sprintf "catalog page renders %s" page.Id)
                Expect.isGreaterThanOrEqual profile.SectionColumns 1 (sprintf "columns declared %s" page.Id)
        }

        test "minimum-size representative pages are declared" {
            for pageId in VisualConfig.minimumRepresentativePageIds do
                let profile = PageProfiles.byPageId pageId
                Expect.isTrue profile.MinimumSizeRepresentative (sprintf "%s is minimum representative" pageId)
        }
    ]
