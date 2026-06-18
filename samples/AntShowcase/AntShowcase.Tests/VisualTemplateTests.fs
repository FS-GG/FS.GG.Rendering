module AntShowcase.Tests.VisualTemplateTests

open Expecto
open AntShowcase.Core
open AntShowcase.Tests.VisualTestHelpers

[<Tests>]
let visualTemplateTests =
    testList "VisualTemplate" [
        test "all template pages render as application compositions" {
            for page in PageRegistry.templatePages do
                let rendered = renderPage page
                Expect.isGreaterThan rendered.NodeCount 0 (sprintf "template renders %s" page.Id)
        }

        test "template pages keep primary actions visible" {
            for page in PageRegistry.templatePages do
                let k = kinds (page.View AntShowcase.Core.DemoState.seed)
                Expect.contains k "button" (sprintf "template %s has a primary/recovery action" page.Id)
        }

        test "result and exception templates include balanced outcome content" {
            for pageId in [ "tpl-result"; "tpl-exception" ] do
                let k = kinds ((PageRegistry.byId pageId).View AntShowcase.Core.DemoState.seed)
                Expect.contains k "result" (sprintf "%s includes result content" pageId)
                Expect.contains k "button" (sprintf "%s includes recovery action" pageId)
        }
    ]
