module Feature140ModifierLayerTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private entry effect : Composition.ModifierEntry =
    { Effect = effect
      Source = Composition.AuthoredModifier }

let private allEffects =
    [ Composition.Clip(RectClip { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 })
      Composition.Opacity 0.5
      Composition.Offset(2.0, 3.0)
      Composition.Transform
          { M11 = 1.0
            M12 = 0.0
            M13 = 0.0
            M21 = 0.0
            M22 = 1.0
            M23 = 0.0
            M31 = 4.0
            M32 = 5.0
            M33 = 1.0 }
      Composition.Background(Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 4.0; Height = 4.0 } Colors.black)
      Composition.Overlay(Scene.textAt { X = 1.0; Y = 2.0 } "overlay" Colors.white)
      Composition.CacheBoundary 7UL
      Composition.LocalZOrder 4
      Composition.LayerHint "popup" ]

let private effectName effect =
    match effect with
    | Composition.Clip _ -> "clip"
    | Composition.Opacity _ -> "opacity"
    | Composition.Offset _ -> "offset"
    | Composition.Transform _ -> "transform"
    | Composition.Background _ -> "background"
    | Composition.Overlay _ -> "overlay"
    | Composition.CacheBoundary _ -> "cache-boundary"
    | Composition.LocalZOrder _ -> "local-z-order"
    | Composition.LayerHint _ -> "layer-hint"

[<Tests>]
let tests =
    testList
        "Feature140 modifier effects and retained classification"
        [ test "classification table covers every supported effect category" {
              let names = Composition.classificationTable |> List.map fst

              for expected in [ "clip"; "opacity"; "offset"; "transform"; "background"; "overlay"; "cache-boundary"; "local-z-order"; "layer-hint" ] do
                  Expect.contains names expected (sprintf "classification table contains %s" expected)
          }

          test "layout, paint, and order invalidation categories are distinct and shared with RetainedRender" {
              let clip = Composition.classify (Composition.Clip(RectClip { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }))
              let opacity = Composition.classify (Composition.Opacity 0.7)
              let z = Composition.classify (Composition.LocalZOrder 9)
              let overlay = Composition.classify (Composition.Overlay Scene.empty)

              Expect.isFalse clip.AffectsLayout "clip is paint-affecting, not layout-affecting"
              Expect.isTrue opacity.AffectsPaint "opacity repaints"
              Expect.isFalse opacity.AffectsLayout "opacity does not remeasure"
              Expect.isTrue z.AffectsOrder "local z-order affects order"
              Expect.isFalse z.AffectsPaint "local z-order alone does not repaint"
              Expect.isTrue overlay.AffectsPaint "overlay routes content that repaints"
              Expect.isTrue overlay.AffectsOrder "overlay also changes layer ordering"
              Expect.equal (RetainedRender.classifyModifierEffect (Composition.LocalZOrder 9)) z "retained evidence reads the composition table"
          }

          test "effect order is preserved as authored through normalization when effects do not algebraically combine" {
              let chain = allEffects |> List.map entry |> Composition.normalize
              let normalizedNames = chain.NormalizedEffects |> List.map (fun e -> effectName e.Effect)

              Expect.equal chain.NormalizedEffects.Length allEffects.Length "all non-identity, non-combinable effects remain present"
              Expect.stringContains normalizedNames.[0] "clip" "first authored effect remains first"
              Expect.stringContains normalizedNames.[normalizedNames.Length - 1] "layer" "last authored effect remains last"
          }

          test "malformed payloads produce diagnostics instead of disappearing silently" {
              let chain =
                  [ entry (Composition.Opacity 1.7)
                    entry (Composition.CacheBoundary 0UL)
                    entry (Composition.LayerHint "") ]
                  |> Composition.normalize

              Expect.contains (chain.Diagnostics |> List.map _.Code) "opacity-out-of-range" "bad opacity is diagnosed"
              Expect.contains (chain.Diagnostics |> List.map _.Code) "cache-boundary-zero" "reserved cache id is diagnosed"
              Expect.contains (chain.Diagnostics |> List.map _.Code) "empty-layer-hint" "empty layer target is diagnosed"
          } ]
