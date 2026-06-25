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

// T019 [US3] Grammar-aware motion `animateIn` (FR-014): deterministic for all grammars; Grammar.Token
// reproduces `animate` byte-for-byte; on Badge/Ring only grammar-agnostic overlays (Pulse/Blink/Damage)
// apply, and directional motions (Spin/Moving) degrade to the static base symbol — never throw.
[<Tests>]
let animateInTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]
    let baseOf g = bytesOf (Symbology.render g t)

    testList
        "US3 animateIn"
        [ yield!
              [ for g in grammars do
                    for m in rhythms ->
                        test (sprintf "animateIn %A %A is deterministic" g m) {
                            Expect.equal (bytesOf (Symbology.animateIn g m t 0.33)) (bytesOf (Symbology.animateIn g m t 0.33)) "pure in (grammar, motion, token, phase)"
                        } ]

          yield!
              rhythms
              |> List.map (fun m ->
                  test (sprintf "animateIn Grammar.Token %A reproduces `animate` byte-for-byte" m) {
                      Expect.equal (bytesOf (Symbology.animateIn Grammar.Token m t 0.4)) (bytesOf (Symbology.animate m t 0.4)) "Token path is the existing animate"
                  })

          test "animateIn Grammar.Badge Idle is the static badge base" {
              Expect.equal (bytesOf (Symbology.animateIn Grammar.Badge Idle t 0.5)) (baseOf Grammar.Badge) "Idle = badge base"
          }

          test "a grammar-agnostic overlay (Pulse) draws on top of the Badge base" {
              Expect.notEqual (bytesOf (Symbology.animateIn Grammar.Badge Pulse t 0.1)) (baseOf Grammar.Badge) "Pulse overlays a rhythm on the badge"
          }

          test "a directional motion (Spin) degrades to the static base on Badge" {
              Expect.equal (bytesOf (Symbology.animateIn Grammar.Badge Spin t 0.3)) (baseOf Grammar.Badge) "Spin is not grammar-agnostic — static base on Badge"
          }

          test "a directional motion (Moving) degrades to the static base on Ring" {
              Expect.equal (bytesOf (Symbology.animateIn Grammar.Ring Moving t 0.3)) (baseOf Grammar.Ring) "Moving degrades to the static ring base"
          }

          test "the same overlay differs across grammar bases (Badge vs Ring Pulse)" {
              Expect.notEqual (bytesOf (Symbology.animateIn Grammar.Badge Pulse t 0.2)) (bytesOf (Symbology.animateIn Grammar.Ring Pulse t 0.2)) "the overlay rides a different base per grammar"
          } ]
