module AntShowcase.Tests.Feature170VisualInspectionAdoptionTests

open Expecto
open AntShowcase.Core
open AntShowcase.Core.Model
open AntShowcase.Tests.VisualTestHelpers

[<Tests>]
let tests =
    testList
        "Feature170 visual inspection adoption"
        [ test "charts-statistical preferred light and dark shells expose structured retained evidence" {
              let lightRender = renderShell preferredSize Light "charts-statistical"
              let darkRender = renderShell preferredSize Dark "charts-statistical"

              let light =
                  Evidence.retainedInspectionEvidence
                      "charts-statistical"
                      "antLight"
                      "preferred"
                      preferredSize.Width
                      preferredSize.Height

              let dark =
                  Evidence.retainedInspectionEvidence
                      "charts-statistical"
                      "antDark"
                      "preferred"
                      preferredSize.Width
                      preferredSize.Height

              Expect.isGreaterThan lightRender.NodeCount 0 "light shell still renders"
              Expect.isGreaterThan darkRender.NodeCount 0 "dark shell still renders"
              Expect.equal light.PageId "charts-statistical" "selected page recorded"
              Expect.equal dark.ThemeId "antDark" "dark theme recorded"
              Expect.equal light.ScreenshotPreferredTargetCount 38 "preferred matrix preserved"
              Expect.equal dark.ScreenshotMinimumTargetCount 12 "minimum matrix preserved"
              Expect.contains light.AffectedRegionIds "content" "affected content region visible"
              Expect.stringContains (Evidence.retainedInspectionToJson light) "\"screenshotPreferredTargetCount\": 38" "json carries count parity"
          } ]
