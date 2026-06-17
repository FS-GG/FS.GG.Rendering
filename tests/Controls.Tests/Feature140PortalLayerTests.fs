module Feature140PortalLayerTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private bounds = Some { X = 0.0; Y = 0.0; Width = 20.0; Height = 20.0 }

let private contribution id layer z =
    Composition.contribution id 0 z layer [ Scene.textAt { X = 1.0; Y = 1.0 } id Colors.white ] bounds

[<Tests>]
let tests =
    testList
        "Feature140 portals and layers"
        [ test "portal layers paint bottom-to-top and hit top-to-bottom from one stream" {
              let hosts =
                  [ Composition.layerHost "popup" 10 true
                    Composition.layerHost "modal" 20 true ]

              let inFlow = [ contribution "content" "content" 0 ]

              let portals =
                  [ Composition.portal "popup" (Some "button") bounds (contribution "popup" "popup" 0)
                    Composition.portal "modal" (Some "dialog") bounds (contribution "modal" "modal" 0) ]

              let composed = Composition.composeLayers hosts inFlow portals

              Expect.equal (composed.Paint |> List.map _.Id) [ "content"; "popup"; "modal" ] "paint follows host order bottom-to-top"
              Expect.equal (composed.Hit |> List.map _.Id) [ "modal"; "popup"; "content" ] "hit follows reverse layer paint order"
              Expect.isEmpty composed.Diagnostics "valid portals produce no diagnostics"
          }

          test "missing target and missing anchor evidence are actionable diagnostics" {
              let composed =
                  Composition.composeLayers
                      [ Composition.layerHost "popup" 10 true ]
                      []
                      [ Composition.portal "toast" None None (contribution "ghost" "toast" 0) ]

              let codes = composed.Diagnostics |> List.map _.Code
              Expect.contains codes "missing-portal-target" "unknown target is reported"
              Expect.contains codes "missing-portal-anchor" "missing anchor evidence is reported"
              Expect.isEmpty composed.Paint "invalid portal is not silently painted"
          }

          test "empty layers are equivalent to no portal layers" {
              let onlyContent =
                  Composition.composeLayers [ Composition.layerHost "popup" 10 true ] [ contribution "content" "content" 0 ] []

              Expect.equal (onlyContent.Paint |> List.map _.Id) [ "content" ] "empty layer contributes no paint"
              Expect.equal (onlyContent.Hit |> List.map _.Id) [ "content" ] "empty layer contributes no hit priority"
          } ]
