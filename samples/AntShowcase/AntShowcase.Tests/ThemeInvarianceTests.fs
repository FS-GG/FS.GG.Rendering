module AntShowcase.Tests.ThemeInvarianceTests

open Expecto
open FS.GG.UI.Controls
open AntShowcase.Core
open AntShowcase.Core.Model

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
    ]
