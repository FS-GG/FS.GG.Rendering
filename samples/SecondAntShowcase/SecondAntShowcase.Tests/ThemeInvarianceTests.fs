module SecondAntShowcase.Tests.ThemeInvarianceTests

open Expecto
open FS.GG.UI.Controls
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private size: FS.GG.UI.Scene.Size = { Width = 1024; Height = 768 }
let private modes = [ Light; Dark ]

/// FR-008 / SC-003: for the same page the control-tree shape + accessibility ids are
/// identical across antLight↔antDark; only resolved visuals differ; no control branches
/// on theme identity. Representative pages span families + templates.
let private representative =
    [ "display-typography"; "selection-toggles"; "charts-statistical"; "tpl-form"; "tpl-workbench" ]
    |> List.map PageRegistry.byId

[<Tests>]
let themeInvarianceTests =
    testList "ThemeInvariance" [
        yield test "antLight and antDark resolve to distinct themes (visuals change)" {
            Expect.notEqual AntTheme.antLight AntTheme.antDark "the two Ant variants differ"
        }

        for page in representative do
            yield test (sprintf "page %s: tree shape + accessibility ids invariant across antLight/antDark" page.Id) {
                // `Control<'msg>` carries function-typed handlers (no equality), so shape
                // invariance is asserted via structural projections: node count + the set
                // of bound/accessible control ids must match across both variants, even as
                // resolved colors differ.
                let shapes =
                    modes
                    |> List.map (fun m ->
                        let result = Control.renderTree (AntTheme.resolve m) size (page.View DemoState.seed)
                        result.NodeCount, result.BoundIds)
                Expect.equal (List.distinct shapes) [ List.head shapes ] "node count + bound ids identical across modes"
            }

        yield test "theme switching preserves page, values, selections, overlays, and validation state" {
            let before =
                { Host.initModel with
                    CurrentPage = "tpl-form"
                    PageState =
                        { Host.initModel.PageState with
                            TextValue = "preserve me"
                            ComboSelected = "Product"
                            OverlayOpen = true
                            DrawerOpen = true
                            Form = { Host.initModel.PageState.Form with Phase = Invalid [ "Email", "Enter a valid email address" ] } } }
            let after = Model.update ToggleMode before
            Expect.equal after.CurrentPage before.CurrentPage "page preserved"
            Expect.equal after.PageState.TextValue before.PageState.TextValue "text value preserved"
            Expect.equal after.PageState.ComboSelected before.PageState.ComboSelected "selection preserved"
            Expect.equal after.PageState.OverlayOpen before.PageState.OverlayOpen "overlay state preserved"
            Expect.equal after.PageState.DrawerOpen before.PageState.DrawerOpen "drawer state preserved"
            Expect.equal after.PageState.Form.Phase before.PageState.Form.Phase "validation phase preserved"
        }
    ]
