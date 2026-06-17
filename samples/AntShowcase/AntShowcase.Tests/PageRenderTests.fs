module AntShowcase.Tests.PageRenderTests

open Expecto
open FS.GG.UI.Controls
open AntShowcase.Core

let private theme = AntTheme.resolve AntShowcase.Core.Model.Light
let private size: FS.GG.UI.Scene.Size = { Width = 1024; Height = 768 }

/// FR-001 / FR-004: every Catalog page's `view seed` renders a non-empty tree with no
/// exception under the Ant theme; SC-008: every page (catalog + template) is reachable in
/// ≤2 nav actions (a single direct rail selection ⇒ exactly 1 action).
[<Tests>]
let pageRenderTests =
    testList "PageRender" [
        for page in PageRegistry.catalogPages do
            yield test (sprintf "catalog page %s renders a non-empty tree" page.Id) {
                let control = page.View DemoState.seed
                let result = Control.renderTree theme size control
                Expect.isGreaterThan result.NodeCount 0 "non-empty node count"
                Expect.isNonEmpty result.Bounds "at least one laid-out control"
            }

        yield test "every page is reachable in one direct nav selection (≤2 actions, SC-008)" {
            let ids = PageRegistry.all |> List.map (fun p -> p.Id)
            Expect.equal (List.length (List.distinct ids)) (List.length ids) "all page ids unique"
            for p in PageRegistry.all do
                Expect.equal (PageRegistry.byId p.Id).Id p.Id (sprintf "page %s directly selectable" p.Id)
        }
    ]
