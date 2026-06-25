module Symbology.Tests.MotionTests

// T022 [US2] Motion-overlay purity goldens: for each rhythm, `animate m t phase` overlays the rhythm
// on the base symbol and is pure in (m, t, phase) — identical inputs => identical Scene.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private t =
    { Symbology.defaultToken with
        Cx = 50.0
        Cy = 50.0
        R = 26.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        Health = 0.7
        Speed = 2 }

let private rhythms = [ Idle; Pulse; Spin; Blink; Damage; Moving ]

let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes

[<Tests>]
let tests =
    testList
        "US2 motion purity"
        [ yield!
              rhythms
              |> List.map (fun m ->
                  test (sprintf "animate %A is pure in (motion, token, phase)" m) {
                      Expect.equal (bytesOf (Symbology.animate m t 0.33)) (bytesOf (Symbology.animate m t 0.33)) "deterministic overlay"
                  })

          test "Idle overlay is the base symbol" {
              Expect.equal (bytesOf (Symbology.animate Idle t 0.5)) (bytesOf (Symbology.token t)) "Idle = base"
          }

          test "an active rhythm overlays something on top of the base symbol" {
              // Pulse at an early phase adds a fired ring not present in the base symbol.
              Expect.notEqual (bytesOf (Symbology.animate Pulse t 0.1)) (bytesOf (Symbology.token t)) "Pulse overlays a rhythm"
          }

          test "phase drives the overlay (different phase => different frame)" {
              Expect.notEqual (bytesOf (Symbology.animate Pulse t 0.1)) (bytesOf (Symbology.animate Pulse t 0.8)) "phase-dependent"
          } ]
