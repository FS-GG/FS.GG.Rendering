module Feature140ModifierNormalizationTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private entry effect : Composition.ModifierEntry =
    { Effect = effect
      Source = Composition.AuthoredModifier }

let private fp effects = effects |> List.map entry |> Composition.fingerprint
let private normalized effects = effects |> List.map entry |> Composition.normalize

[<Tests>]
let tests =
    testList
        "Feature140 modifier normalization"
        [ test "normalization is idempotent" {
              let first =
                  [ Composition.Offset(1.0, 2.0)
                    Composition.Offset(3.0, 4.0)
                    Composition.Opacity 0.5
                    Composition.Opacity 0.5
                    Composition.LocalZOrder 1
                    Composition.LocalZOrder 8 ]
                  |> normalized

              let second = first.NormalizedEffects |> Composition.normalize
              Expect.equal second.NormalizedEffects first.NormalizedEffects "normalizing a normalized chain is stable"
              Expect.equal second.FingerprintInput first.FingerprintInput "fingerprint input is stable after idempotence"
          }

          test "at least twelve representative equivalent chains have byte-stable fingerprints" {
              let cases =
                  [ [ Composition.Offset(1.0, 2.0); Composition.Offset(3.0, 4.0) ], [ Composition.Offset(4.0, 6.0) ]
                    [ Composition.Offset(1.0, 0.0); Composition.Offset(-1.0, 0.0) ], []
                    [ Composition.Opacity 0.5; Composition.Opacity 0.5 ], [ Composition.Opacity 0.25 ]
                    [ Composition.Opacity 1.0; Composition.Offset(2.0, 2.0) ], [ Composition.Offset(2.0, 2.0) ]
                    [ Composition.Offset(0.0, 0.0); Composition.Opacity 0.8 ], [ Composition.Opacity 0.8 ]
                    [ Composition.LocalZOrder 0 ], []
                    [ Composition.LocalZOrder 2; Composition.LocalZOrder 5 ], [ Composition.LocalZOrder 5 ]
                    [ Composition.LayerHint "content" ], []
                    [ Composition.LayerHint ""; Composition.LayerHint "popup" ], [ Composition.LayerHint "popup" ]
                    [ Composition.CacheBoundary 1UL; Composition.CacheBoundary 2UL ], [ Composition.CacheBoundary 2UL ]
                    [ Composition.Opacity 1.0 ], []
                    [ Composition.Offset(4.0, 1.0); Composition.Offset(0.0, 0.0) ], [ Composition.Offset(4.0, 1.0) ] ]

              for index, (left, right) in cases |> List.indexed do
                  Expect.equal (fp left) (fp right) (sprintf "case %d equivalent normalized chains fingerprint equally" (index + 1))
          }

          test "cache-enabled and cache-disabled parity reads the same normalized fingerprint input" {
              let authored =
                  [ Composition.CacheBoundary 10UL
                    Composition.Offset(2.0, 0.0)
                    Composition.Offset(3.0, 0.0)
                    Composition.Opacity 1.0 ]

              let enabled = authored |> normalized
              let disabled = [ Composition.CacheBoundary 10UL; Composition.Offset(5.0, 0.0) ] |> normalized

              Expect.equal enabled.FingerprintInput disabled.FingerprintInput "normalization gives cache-on/cache-off parity the same replay input"
              Expect.equal (fp authored) (fp [ Composition.CacheBoundary 10UL; Composition.Offset(5.0, 0.0) ]) "fingerprint parity follows the normalized input"
          } ]
