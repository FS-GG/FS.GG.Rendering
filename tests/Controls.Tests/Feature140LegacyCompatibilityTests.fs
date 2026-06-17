module Feature140LegacyCompatibilityTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private perspective =
    { M11 = 1.0
      M12 = 0.0
      M13 = 0.0
      M21 = 0.0
      M22 = 1.0
      M23 = 0.0
      M31 = 0.0
      M32 = 0.0
      M33 = 1.0 }

[<Tests>]
let tests =
    testList
        "Feature140 legacy compatibility lowering"
        [ test "legacy clipping, translation, and perspective lower into modifier values" {
              let lowered =
                  [ Composition.LegacyClipping(RectClip { X = 0.0; Y = 0.0; Width = 12.0; Height = 12.0 })
                    Composition.LegacyTranslation(4.0, 5.0)
                    Composition.LegacyPerspective perspective ]
                  |> List.collect Composition.legacyLower
                  |> List.map _.Effect

              Expect.isTrue (lowered |> List.exists (function Composition.Clip _ -> true | _ -> false)) "clip lowers to Clip"
              Expect.isTrue (lowered |> List.exists (function Composition.Offset(4.0, 5.0) -> true | _ -> false)) "translation lowers to Offset"
              Expect.isTrue (lowered |> List.exists (function Composition.Transform _ -> true | _ -> false)) "perspective lowers to Transform"
          }

          test "legacy forms remain supported unchanged unless explicitly documented otherwise" {
              for form in
                  [ Composition.LegacyClipping(RectClip { X = 0.0; Y = 0.0; Width = 12.0; Height = 12.0 })
                    Composition.LegacyTranslation(4.0, 5.0)
                    Composition.LegacyPerspective perspective
                    Composition.LegacyCachedSubtree 44UL
                    Composition.LegacyText
                    Composition.LegacyOverlay ] do
                  let status, note = Composition.compatibilityEvidence form
                  Expect.equal status Composition.SupportedUnchanged "legacy form is compatible by default"
                  Expect.isNonEmpty note "compatibility note is recorded"
          } ]
