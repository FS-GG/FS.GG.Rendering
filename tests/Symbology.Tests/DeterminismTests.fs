module Symbology.Tests.DeterminismTests

// T010 [US1] Determinism / stable-identity (SC-001): identical Token => equal Scene and byte-equal
// canonical bytes across runs; a gallery package identity is stable across runs.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

// T005 [Foundational] Token zero-drift guard (FR-010/SC-006). Pin the SHA-256 of the canonical
// SceneCodec bytes of the existing Token-grammar surface (`token`/`gallery`/`filmstrip`) as a stable
// golden so the Badge/Ring additions provably leave the Token path BYTE-UNCHANGED. These literals must
// only ever move via an INTENTIONAL Token-grammar change (none is in scope for feature 195).
let private sha (b: byte[]) =
    use h = System.Security.Cryptography.SHA256.Create()
    h.ComputeHash b |> Array.map (sprintf "%02x") |> String.concat ""

let private canonicalSha (scene: Scene) = sha (SceneCodec.export scene).CanonicalBytes

let private goldenRoster =
    [ Symbology.defaultToken
      { Symbology.defaultToken with Faction = Enemy; Klass = Heavy }
      { Symbology.defaultToken with Klass = Scout; Sigil = Fang } ]

let private goldenStrip =
    [ Pulse, Symbology.defaultToken
      Moving, { Symbology.defaultToken with Faction = Enemy; Speed = 3 } ]

let private sample =
    { Symbology.defaultToken with
        R = 28.0
        Faction = Ally
        Klass = Heavy
        Sigil = Bolt
        State = Suspected
        Threat = 0.7
        Charge = 0.6
        Health = 0.8
        Speed = 3
        Heading = 0.5
        Shield = true }

[<Tests>]
let tests =
    testList
        "US1 determinism"
        [ test "same Token => equal Scene value" {
              Expect.equal (Symbology.token sample) (Symbology.token sample) "token is a pure function of Token"
          }

          test "same Token => byte-equal canonical bytes across runs" {
              let a = (SceneCodec.export (Symbology.token sample)).CanonicalBytes
              let b = (SceneCodec.export (Symbology.token sample)).CanonicalBytes
              Expect.equal a b "canonical bytes are the determinism identity (SC-001)"
          }

          test "gallery package identity is stable across runs" {
              let board () =
                  Symbology.gallery 3 80.0 [ sample; { sample with Faction = Enemy }; { sample with Klass = Scout } ]

              let id1 = (SceneCodec.export (board ())).PackageIdentity
              let id2 = (SceneCodec.export (board ())).PackageIdentity
              Expect.equal id1 id2 "stable gallery identity"
          }

          // T005 zero-drift goldens — the Token path is BYTE-UNCHANGED by the Badge/Ring additions (FR-010/SC-006).
          test "Token grammar golden: `token defaultToken` canonical bytes are byte-unchanged" {
              Expect.equal
                  (canonicalSha (Symbology.token Symbology.defaultToken))
                  "0dda10bd2c3d018b10f759dc82c346b8c2575f9220ce477da25b2bc58e596c87"
                  "Token grammar drifted — the existing `token` body must not change (FR-010)"
          }

          test "Token grammar golden: fixed gallery canonical bytes are byte-unchanged" {
              Expect.equal
                  (canonicalSha (Symbology.gallery 3 80.0 goldenRoster))
                  "dc19e88d827ed210521d3af6ba2d29d1733a5214d3c2a9cd89f60193288cda57"
                  "gallery drifted — the existing `gallery` body must not change (FR-010)"
          }

          test "Token grammar golden: fixed filmstrip canonical bytes are byte-unchanged" {
              Expect.equal
                  (canonicalSha (Symbology.filmstrip 4 goldenStrip))
                  "6e1edcb3eedad93c121832663d8acb2c5ced007f7c1ade9040818f836fd2856e"
                  "filmstrip drifted — the existing `filmstrip` body must not change (FR-010)"
          } ]

// T008 [US1] Badge determinism (SC-003/FR-004): in-process render-twice equality AND a pinned canonical
// golden (the cross-process proxy — purity guarantees a fixed Token renders byte-identical in any process).
[<Tests>]
let badgeDeterminism =
    testList
        "US1 badge determinism"
        [ test "same Token => byte-equal `badge` canonical bytes" {
              let a = (SceneCodec.export (Symbology.badge sample)).CanonicalBytes
              let b = (SceneCodec.export (Symbology.badge sample)).CanonicalBytes
              Expect.equal a b "badge is a pure function of Token (SC-003)"
          }

          test "badge cross-process golden: fixed Token canonical bytes are pinned" {
              Expect.equal
                  (canonicalSha (Symbology.badge sample))
                  "6b497f5d8ec0c8034978119fa93271e40fb947b8e3de0744bf2d95cdab1cf087"
                  "badge canonical bytes drifted from the pinned cross-process golden (SC-003)"
          } ]

// T013 [US2] Ring determinism (SC-003/FR-004): in-process render-twice equality AND a pinned canonical golden.
[<Tests>]
let ringDeterminism =
    testList
        "US2 ring determinism"
        [ test "same Token => byte-equal `ring` canonical bytes" {
              let a = (SceneCodec.export (Symbology.ring sample)).CanonicalBytes
              let b = (SceneCodec.export (Symbology.ring sample)).CanonicalBytes
              Expect.equal a b "ring is a pure function of Token (SC-003)"
          }

          test "ring cross-process golden: fixed Token canonical bytes are pinned" {
              Expect.equal
                  (canonicalSha (Symbology.ring sample))
                  "70a04792c94bd03a69d21d78480a38a29d6e451f8500919fc5e329ea88920c0f"
                  "ring canonical bytes drifted from the pinned cross-process golden (SC-003)"
          } ]
