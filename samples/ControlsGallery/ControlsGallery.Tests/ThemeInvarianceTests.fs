module ControlsGallery.Tests.ThemeInvarianceTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default.Theming
open ControlsGallery.Core
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private size: FS.GG.UI.Scene.Size = { Width = 1024; Height = 768 }

let private variants =
    [ Light, GalleryTheme.indigo
      Light, GalleryTheme.teal
      Dark, GalleryTheme.indigo
      Dark, GalleryTheme.teal ]

/// FR-006 / SC-003: for the same page the control-tree shape is identical across
/// Light/Dark × {Indigo,Teal}; only resolved visuals differ.
[<Tests>]
let themeInvarianceTests =
    testList "ThemeInvariance" [
        test "resolved themes actually differ across variants (visuals change)" {
            let themes = variants |> List.map (fun (m, a) -> GalleryTheme.resolve m a)
            Expect.equal (List.length (List.distinct themes)) 4 "all four variants resolve to distinct themes"
        }

        for page in Pages.all ->
            test (sprintf "page %s: tree shape + accessibility ids invariant across variants" page.Id) {
                // `Control<'msg>` carries function-typed handlers (no equality), so shape
                // invariance is asserted via structural projections: node count + the set
                // of bound/accessible control ids must match across every variant, even as
                // resolved colors differ.
                let shapes =
                    variants
                    |> List.map (fun (m, a) ->
                        let result = Control.renderTree (GalleryTheme.resolve m a) size (page.Build DemoState.seed)
                        result.NodeCount, result.BoundIds)
                Expect.equal (List.distinct shapes) [ List.head shapes ] "node count + bound ids identical across variants"
            }
    ]
