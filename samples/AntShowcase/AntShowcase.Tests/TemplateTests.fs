module AntShowcase.Tests.TemplateTests

open Expecto
open FS.GG.UI.Controls
open AntShowcase.Core
open AntShowcase.Core.Model

/// Recursively collect every node's `Kind` in an authored control tree.
let rec private kinds (c: Control<'msg>): string list =
    c.Kind :: (c.Children |> List.collect kinds)

let private catalogIds = Catalog.supportedControls |> List.map (fun d -> d.Id)
let private catalogSet = Set.ofList catalogIds

/// A node kind is a legitimate catalog composition if it IS a catalog id, or it is an
/// intrinsic structural sub-part of a catalog control (e.g. `data-grid-cell` under
/// `data-grid`) — never a bespoke new control type (SC-002 / contracts/enterprise-templates.md).
let private isCatalogComposed (k: string): bool =
    catalogSet.Contains k || catalogIds |> List.exists (fun c -> k.StartsWith(c + "-"))

let private applyAll (msgs: AntShowcaseMsg list) (m: AntShowcaseModel): AntShowcaseModel =
    List.fold (fun acc msg -> Model.update msg acc) m msgs

let private formPage = PageRegistry.byId "tpl-form"

/// FR-005 / FR-006 / SC-002 / SC-009: each template is composed only of catalog control
/// types; the form rejects invalid input (validation messages, no success result) and
/// shows a success result on valid submit.
[<Tests>]
let templateTests =
    testList "Template" [
        for page in PageRegistry.templatePages do
            yield test (sprintf "template %s is composed only of catalog control types (SC-002)" page.Id) {
                let tree = page.View DemoState.seed
                let nonCatalog = kinds tree |> List.filter (fun k -> not (isCatalogComposed k)) |> List.distinct
                Expect.isEmpty nonCatalog (sprintf "every node maps to a catalog id; offenders: %A" nonCatalog)
            }

        yield test "form: invalid submit ⇒ Invalid with validation messages and NO success result" {
            let invalidModel = applyAll (List.take 4 Scripts.formInvalidThenValid) Host.initModel
            match invalidModel.PageState.Form.Phase with
            | Invalid errors -> Expect.isNonEmpty errors "invalid submit yields field errors"
            | other -> failtestf "expected Invalid, got %A" other
            let tree = formPage.View invalidModel.PageState
            let k = kinds tree
            Expect.contains k "validation-message" "validation-message nodes appear"
            Expect.isFalse (List.contains "result" k) "no success result on invalid submit"
        }

        yield test "form: valid submit ⇒ Submitted with a success result node" {
            let validModel = applyAll Scripts.formInvalidThenValid Host.initModel
            Expect.equal validModel.PageState.Form.Phase Submitted "valid submit transitions to Submitted"
            let tree = formPage.View validModel.PageState
            Expect.contains (kinds tree) "result" "a success result node appears"
        }

        yield test "form: field edits return to Editing (no premature validation)" {
            let m = Model.update (PageMsg(FormFieldChanged("Name", "Grace"))) Host.initModel
            Expect.equal m.PageState.Form.Phase Editing "editing a field is the Editing phase"
            Expect.equal m.PageState.Form.Name "Grace" "field value updated"
        }
    ]
