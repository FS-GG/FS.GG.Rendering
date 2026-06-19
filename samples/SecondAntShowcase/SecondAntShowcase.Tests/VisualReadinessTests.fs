module SecondAntShowcase.Tests.VisualReadinessTests

open Expecto
open FS.GG.UI.Testing
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model
open SecondAntShowcase.Core.VisualReadinessWorkflow
open SecondAntShowcase.Tests.VisualTestHelpers

[<Tests>]
let visualReadinessTests =
    testList "VisualReadiness" [
        test "accepted showcase sizes are declared" {
            Expect.equal (VisualConfig.sizeText VisualConfig.preferredSize) "1600x1000" "preferred size"
            Expect.equal (VisualConfig.sizeText VisualConfig.minimumSize) "1280x800" "minimum size"
            Expect.equal (VisualConfig.classifySize VisualConfig.preferredSize) VisualConfig.Preferred "preferred role"
            Expect.equal (VisualConfig.classifySize VisualConfig.minimumSize) VisualConfig.Minimum "minimum role"
        }

        test "CLI aliases resolve to canonical theme ids" {
            match VisualConfig.resolveThemeList "light,dark" with
            | Ok themes ->
                Expect.equal (themes |> List.map snd) [ "antLight"; "antDark" ] "aliases resolve"
            | Error error -> failtest error
        }

        test "theme switching preserves current page and page state" {
            let before = { Host.initModel with CurrentPage = "tpl-form"; PageState = { Host.initModel.PageState with TextValue = "preserved" } }
            let after = Model.update ToggleMode before
            Expect.equal after.CurrentPage before.CurrentPage "page preserved"
            Expect.equal after.PageState.TextValue "preserved" "page state preserved"
        }

        test "full shell renders in both canonical themes at accepted sizes" {
            for size in [ preferredSize; minimumSize ] do
                for mode in [ Light; Dark ] do
                    let rendered = renderShell size mode "data-collections"
                    Expect.isGreaterThan rendered.NodeCount 0 (sprintf "renders %A at %s" mode (VisualConfig.sizeText size))
        }

        test "shared visual readiness target parity covers preferred and minimum matrices" {
            let preferred, _ = init 1 VisualConfig.preferredSize VisualConfig.supportedThemeIds (PageRegistry.all |> List.map _.Id) "out"
            let minimum, _ = init 1 VisualConfig.minimumSize VisualConfig.supportedThemeIds VisualConfig.minimumRepresentativePageIds "out"

            Expect.equal preferred.Targets.Length 38 "preferred 19 pages x 2 themes"
            Expect.equal minimum.Targets.Length 38 "minimum 19 pages x 2 themes"
            Expect.equal (preferred.Targets |> List.map _.SharedTarget.TargetId |> List.distinct |> List.length) 38 "preferred shared target ids unique"
            Expect.equal (minimum.Targets |> List.map _.SharedTarget.TargetId |> List.distinct |> List.length) 38 "minimum shared target ids unique"
            Expect.isTrue (preferred.Targets |> List.forall (fun target -> target.SharedTarget.RelativePath = target.RelativePath)) "workflow exposes shared-compatible relative paths"
            Expect.isTrue (minimum.Targets |> List.forall (fun target -> target.SharedTarget.Required)) "minimum shared targets are required"
        }
    ]
