module Symbology.Tests.GalleryTests

// T014 [US1] Gallery layout + legibility-at-size (SC-007): `gallery cols spacing tokens` lays out a
// reproducible grid; each rendered symbol is non-blank and faction + class are separable at the
// target on-board size.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private roster faction klass =
    { Symbology.defaultToken with
        R = 24.0
        Faction = faction
        Klass = klass
        Sigil = Bolt
        Health = 0.7 }

let private mixed =
    [ roster Ally Mobile; roster Enemy Heavy; roster Neutral Scout; roster Ally Scout ]

let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes

[<Tests>]
let tests =
    testList
        "US1 gallery"
        [ test "gallery layout is reproducible (byte-identical across builds)" {
              let a = Symbology.gallery 2 80.0 mixed
              let b = Symbology.gallery 2 80.0 mixed
              Expect.equal (bytesOf a) (bytesOf b) "pure grid layout"
          }

          test "every symbol in the gallery is non-blank" {
              let kinds = Symbology.gallery 2 80.0 mixed |> Scene.describe |> List.distinct
              Expect.contains kinds PathElement "gallery draws symbol silhouettes"
          }

          test "faction is separable on the board (all-Ally differs from all-Enemy)" {
              let allAlly = Symbology.gallery 2 80.0 [ roster Ally Mobile; roster Ally Heavy ]
              let allEnemy = Symbology.gallery 2 80.0 [ roster Enemy Mobile; roster Enemy Heavy ]
              Expect.notEqual (bytesOf allAlly) (bytesOf allEnemy) "faction hue is separable"
          }

          test "class is separable on the board (all-Mobile differs from all-Heavy)" {
              let allMobile = Symbology.gallery 2 80.0 [ roster Ally Mobile; roster Enemy Mobile ]
              let allHeavy = Symbology.gallery 2 80.0 [ roster Ally Heavy; roster Enemy Heavy ]
              Expect.notEqual (bytesOf allMobile) (bytesOf allHeavy) "class silhouette is separable"
          } ]

// T018 [US3] Grammar-parameterized gallery (FR-008/FR-010): `galleryIn g` lays out a reproducible grid in
// the selected grammar; Grammar.Token reproduces the existing `gallery` byte-for-byte; empty and single
// rosters render reproducibly; selecting a different grammar produces a different board.
[<Tests>]
let galleryInTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

    testList
        "US3 galleryIn"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "galleryIn %A is byte-reproducible" g) {
                      Expect.equal (bytesOf (Symbology.galleryIn g 2 80.0 mixed)) (bytesOf (Symbology.galleryIn g 2 80.0 mixed)) "pure grid layout per grammar"
                  })

          test "galleryIn Grammar.Token reproduces `gallery` byte-for-byte (FR-010)" {
              Expect.equal
                  (bytesOf (Symbology.galleryIn Grammar.Token 2 80.0 mixed))
                  (bytesOf (Symbology.gallery 2 80.0 mixed))
                  "the Token path is the existing gallery, unchanged"
          }

          test "selecting a different grammar produces a different board" {
              let tok = bytesOf (Symbology.galleryIn Grammar.Token 2 80.0 mixed)
              let bdg = bytesOf (Symbology.galleryIn Grammar.Badge 2 80.0 mixed)
              let rng = bytesOf (Symbology.galleryIn Grammar.Ring 2 80.0 mixed)
              Expect.notEqual tok bdg "Badge board differs from Token board"
              Expect.notEqual tok rng "Ring board differs from Token board"
              Expect.notEqual bdg rng "Badge board differs from Ring board"
          }

          test "empty roster renders reproducibly in every grammar" {
              for g in grammars do
                  Expect.equal (bytesOf (Symbology.galleryIn g 2 80.0 [])) (bytesOf (Symbology.galleryIn g 2 80.0 [])) "empty roster is reproducible"
          }

          test "single-unit roster renders reproducibly in every grammar" {
              let one = [ roster Ally Mobile ]

              for g in grammars do
                  Expect.equal (bytesOf (Symbology.galleryIn g 2 80.0 one)) (bytesOf (Symbology.galleryIn g 2 80.0 one)) "single roster is reproducible"
          } ]

// T023 [US3] Labelled-roster board reproducibility (FR-010): a roster carrying identity labels renders on a
// review board (`galleryIn g`) byte-reproducibly per grammar, with no signature change — the boards already
// thread the whole Token, so the label flows through by construction. A labelled board also differs from its
// unlabelled twin (the label reaches the board), and Grammar.Token still reproduces `gallery` for label-free.
[<Tests>]
let labelledBoardTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

    let labelledRoster =
        mixed |> List.mapi (fun i t -> { t with Label = Some(sprintf "U-%d" i) })

    testList
        "US3 labelled gallery"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "labelled galleryIn %A is byte-reproducible" g) {
                      Expect.equal
                          (bytesOf (Symbology.galleryIn g 2 80.0 labelledRoster))
                          (bytesOf (Symbology.galleryIn g 2 80.0 labelledRoster))
                          "a labelled board is reproducible per grammar (FR-010)"
                  })

          test "a labelled board differs from its unlabelled twin in every grammar" {
              for g in grammars do
                  Expect.notEqual
                      (bytesOf (Symbology.galleryIn g 2 80.0 labelledRoster))
                      (bytesOf (Symbology.galleryIn g 2 80.0 mixed))
                      "the label reaches the review board"
          } ]

// T014 [US3] Multi-line-labelled roster on review boards (FR-010/SC-001): a roster carrying `\n`-bearing
// labels renders byte-reproducibly via `galleryIn`/`filmstripIn` per grammar under a fixed measurement
// provider, with NO signature change (the boards already thread the whole Token). A multi-line board also
// differs from its single-line twin (the extra lines reach the board).
[<Tests>]
let multilineBoardTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

    let multilineRoster =
        mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(sprintf "U-%d\nLINE-B" i) })

    let singleLineRoster =
        mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(sprintf "U-%d" i) })

    let strip =
        multilineRoster |> List.map (fun t -> Pulse, t)

    testList
        "US3 multi-line labelled boards"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "multi-line galleryIn %A is byte-reproducible" g) {
                      Expect.equal
                          (bytesOf (Symbology.galleryIn g 2 120.0 multilineRoster))
                          (bytesOf (Symbology.galleryIn g 2 120.0 multilineRoster))
                          "a multi-line-labelled board is reproducible per grammar (FR-010/SC-001)"
                  })

          yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "multi-line filmstripIn %A is byte-reproducible" g) {
                      Expect.equal
                          (bytesOf (Symbology.filmstripIn g 3 strip))
                          (bytesOf (Symbology.filmstripIn g 3 strip))
                          "a multi-line-labelled motion board is reproducible per grammar (FR-010)"
                  })

          test "a multi-line board differs from its single-line twin in every grammar" {
              for g in grammars do
                  Expect.notEqual
                      (bytesOf (Symbology.galleryIn g 2 120.0 multilineRoster))
                      (bytesOf (Symbology.galleryIn g 2 120.0 singleLineRoster))
                      "the extra label lines reach the review board"
          } ]
