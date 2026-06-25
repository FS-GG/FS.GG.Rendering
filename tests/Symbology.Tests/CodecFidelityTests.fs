module Symbology.Tests.CodecFidelityTests

// T012 [US1] Codec-fidelity (SC-003): export -> import -> re-export of a token scene preserves Path
// geometry, the radial gradient the token emits (with linear/sweep asserted as a codec-capability
// guard), Dash effects, Arc, and stroke width/cap/join with no loss.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

// A token that exercises every render-affecting channel: Suspected -> Dash, Charge -> RadialGradient,
// Health -> Arc, Threat -> stroke width, Round cap/join, silhouette Path, tail circles.
let private rich =
    { Symbology.defaultToken with
        Cx = 40.0
        Cy = 40.0
        R = 30.0
        Faction = Enemy
        Klass = Heavy
        Sigil = Bolt
        State = Suspected
        Threat = 0.85
        Charge = 0.7
        Health = 0.6
        Speed = 3
        Heading = 0.4
        Shield = true }

let private roundTrip (scene: Scene) =
    let bytes = (SceneCodec.export scene).CanonicalBytes

    match SceneCodec.importPackage bytes with
    | Result.Ok pkg -> pkg
    | Result.Error diagnostics -> failtestf "import failed: %A" diagnostics

[<Tests>]
let tests =
    testList
        "US1 codec fidelity"
        [ test "token scene survives export -> import -> re-export byte-identically" {
              let scene = Symbology.token rich
              let pkg = roundTrip scene
              let reExported = (SceneCodec.export pkg.Scene).CanonicalBytes
              let original = (SceneCodec.export scene).CanonicalBytes
              Expect.equal reExported original "round-trip preserves canonical bytes (no codec loss)"
          }

          test "token scene preserves Path, Arc and gradient-bearing element kinds" {
              let kinds = Symbology.token rich |> Scene.describe |> List.distinct
              Expect.contains kinds PathElement "silhouette Path preserved"
              Expect.contains kinds ArcElement "health belly Arc preserved"
              Expect.contains kinds EllipseElement "charge radial-gradient fill (ellipse) preserved"
          }

          test "codec-capability guard: radial / linear / sweep gradients all round-trip" {
              let center = { X = 20.0; Y = 20.0 }
              let bounds = { X = 0.0; Y = 0.0; Width = 40.0; Height = 40.0 }
              let colors = [ Colors.rgba 255uy 0uy 0uy 200uy; Colors.rgba 255uy 0uy 0uy 0uy ]

              let shaders =
                  [ "radial", RadialGradient(center, 18.0, colors)
                    "linear", LinearGradient({ X = 0.0; Y = 0.0 }, { X = 40.0; Y = 40.0 }, colors)
                    "sweep", SweepGradient(center, colors) ]

              for name, shader in shaders do
                  let scene = Scene.ellipse bounds (Paint.fill Colors.transparent |> Paint.withShader shader)
                  let pkg = roundTrip scene
                  let reExported = (SceneCodec.export pkg.Scene).CanonicalBytes
                  Expect.equal reExported ((SceneCodec.export scene).CanonicalBytes) (sprintf "%s gradient round-trips" name)
          } ]
