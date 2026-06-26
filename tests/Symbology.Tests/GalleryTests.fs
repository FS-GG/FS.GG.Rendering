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
        mixed |> List.mapi (fun i t -> { t with Label = Some(LabelText.Plain(sprintf "U-%d" i)) })

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
        mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(LabelText.Plain(sprintf "U-%d\nLINE-B" i)) })

    let singleLineRoster =
        mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(LabelText.Plain(sprintf "U-%d" i)) })

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

// Feature 198 [US3] STYLED-run-labelled roster on review boards (FR-011/SC-001/B12): a roster carrying
// per-run colour/weight/size labels renders byte-reproducibly via `render`/`galleryIn`/`filmstripIn` in
// every grammar under a fixed measurement provider, with NO signature change (the boards thread the whole
// Token). A styled board also differs from its plain-labelled twin (the run styling reaches the board).
[<Tests>]
let styledBoardTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

    let styledRoster =
        mixed
        |> List.mapi (fun i t ->
            { t with
                R = 40.0
                Label =
                    Some(
                        LabelText.Rich
                            [ { Symbology.run (sprintf "U-%d" i) with Weight = Some 700; Color = Some(Colors.rgb 24uy 144uy 255uy) }
                              { Symbology.run " ac" with Scale = Some 0.6 } ]
                    ) })

    let plainRoster =
        mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(LabelText.Plain(sprintf "U-%d ac" i)) })

    let strip = styledRoster |> List.map (fun t -> Pulse, t)

    testList
        "US3 styled-run labelled boards"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "styled galleryIn %A is byte-reproducible (every unit's runs drawn)" g) {
                      Expect.equal
                          (bytesOf (Symbology.galleryIn g 2 120.0 styledRoster))
                          (bytesOf (Symbology.galleryIn g 2 120.0 styledRoster))
                          "a styled-labelled board is reproducible per grammar (FR-011/SC-001)"
                  })

          yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "styled filmstripIn %A is byte-reproducible" g) {
                      Expect.equal
                          (bytesOf (Symbology.filmstripIn g 3 strip))
                          (bytesOf (Symbology.filmstripIn g 3 strip))
                          "a styled-labelled motion board is reproducible per grammar (FR-011)"
                  })

          test "a styled board differs from its plain-labelled twin in every grammar" {
              for g in grammars do
                  Expect.notEqual
                      (bytesOf (Symbology.galleryIn g 2 120.0 styledRoster))
                      (bytesOf (Symbology.galleryIn g 2 120.0 plainRoster))
                      "the run styling reaches the review board (B12)"
          } ]

// Feature 199 [US3] (T034/B17/B19) Laid-out / decorated review boards. A fully-laid-out roster renders via
// `galleryIn` (and `render`) in every grammar, byte-reproducibly under a fixed provider, with NO signature
// change to the board entry points; and author-supplied `Color` / `Align` / decoration are used as-is —
// never silently re-mapped or rejected at runtime (FR-013/FR-015/SC-001).
[<Tests>]
let laidBoardTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]
    let authorBlue = Colors.rgb 17uy 99uy 211uy

    let laidRoster =
        mixed
        |> List.mapi (fun i t ->
            { t with
                R = 40.0
                Label =
                    Some(
                        Symbology.laidLabel
                            [ Symbology.align Center [ { Symbology.run (sprintf "U-%d" i) with Weight = Some 700; Color = Some authorBlue } ]
                              Symbology.align Justify [ Symbology.run "alpha bravo charlie"; { Symbology.run " old" with Strike = Some true } ] ]
                    ) })

    let rec collectFills (scene: Scene) =
        scene.Nodes
        |> List.collect (fun node ->
            match node with
            | GlyphRun r -> r.Paint.Fill |> Option.toList
            | Group s -> s |> List.collect collectFills
            | ClipNode(_, s)
            | ColorSpaceNode(_, s)
            | PerspectiveNode(_, s)
            | Translate(_, s) -> collectFills s
            | _ -> [])

    testList
        "US3.199 laid-out labelled boards"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "laid-out galleryIn %A is byte-reproducible (B17)" g) {
                      Expect.equal
                          (bytesOf (Symbology.galleryIn g 2 140.0 laidRoster))
                          (bytesOf (Symbology.galleryIn g 2 140.0 laidRoster))
                          "a laid-out / decorated board is reproducible per grammar (FR-013/SC-001)"
                  })

          test "the laid-out board differs per grammar from its plain twin (layout/decoration reaches the board)" {
              let plain = mixed |> List.mapi (fun i t -> { t with R = 40.0; Label = Some(LabelText.Plain(sprintf "U-%d" i)) })

              for g in grammars do
                  Expect.notEqual
                      (bytesOf (Symbology.galleryIn g 2 140.0 laidRoster))
                      (bytesOf (Symbology.galleryIn g 2 140.0 plain))
                      "the alignment/decoration reaches the review board (B17)"
          }

          test "author-supplied colour is used as-is in the laid-out board (B19/FR-015)" {
              let fills = Symbology.galleryIn Grammar.Token 2 140.0 laidRoster |> collectFills
              Expect.contains fills authorBlue "the exact author colour reaches the node — never re-mapped/rejected (FR-015)"
          }

          test "author-supplied alignment is honoured as-is (Trailing ≠ Leading) (B19/FR-015)" {
              let one a =
                  bytesOf (
                      Symbology.token
                          { Symbology.defaultToken with R = 40.0; Faction = Ally; Health = 0.6
                                                        Label = Some(Symbology.laidLabel [ Symbology.align a [ { Symbology.run "A B" with Color = Some authorBlue } ] ]) }
                  )

              Expect.notEqual (one Leading) (one Trailing) "the author's alignment choice is applied, not normalised away (FR-015)"
          } ]

// Feature 200 [US3] (T031) Auto-labelled / motion-bound rosters on boards (FR-002/FR-017/SC-001). A roster
// mixing AutoLabel and LabelMotion tokens renders on galleryIn / filmstripIn in every grammar, byte-
// reproducible per grammar under a fixed provider + fixed phase sampling, with NO signature change to the
// board/motion entry points; and the projection reads ONLY Token channels — never a game's raw stats.
let private autoMotionRoster =
    [ { roster Ally Mobile with AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier ]); LabelMotion = Some LabelMotion.TypeOn }
      { roster Enemy Heavy with AutoLabel = Some(Symbology.autoLabel [ KlassCode; SpeedPips ]); LabelMotion = Some LabelMotion.Fade; Speed = 3 }
      { roster Neutral Scout with Label = Some(Symbology.plainLabel "ZULU"); LabelMotion = Some LabelMotion.Pulse }
      { roster Ally Scout with AutoLabel = Some(Symbology.autoLabel [ StateCode; ThreatTier ]); State = Suspected } ]

[<Tests>]
let autoMotionBoardTests =
    testList
        "US3.200 auto/motion rosters on boards"
        [ for grammar in [ Grammar.Token; Grammar.Badge; Grammar.Ring ] do
              test (sprintf "[%A] galleryIn of an auto/motion roster is byte-reproducible" grammar) {
                  let a = Symbology.galleryIn grammar 2 80.0 autoMotionRoster
                  let b = Symbology.galleryIn grammar 2 80.0 autoMotionRoster
                  Expect.equal (bytesOf a) (bytesOf b) "the board is reproducible per grammar (FR-017)"
                  Expect.contains (a |> Scene.describe |> List.distinct) GlyphRunElement "the roster draws labels (projected and/or hand-authored)"
              }

              test (sprintf "[%A] filmstripIn of an auto/motion roster is byte-reproducible under fixed sampling" grammar) {
                  let a = Symbology.filmstripIn grammar 4 [ Idle, autoMotionRoster.[0]; Idle, autoMotionRoster.[1] ]
                  let b = Symbology.filmstripIn grammar 4 [ Idle, autoMotionRoster.[0]; Idle, autoMotionRoster.[1] ]
                  Expect.equal (bytesOf a) (bytesOf b) "fixed phase sampling ⇒ byte-reproducible frames (SC-004)"
              }

          test "the projection reads ONLY Token channels (a channel delta moves the board; nothing else does)" {
              let spec = Some(Symbology.autoLabel [ HealthTier ])
              let lo = [ { roster Ally Mobile with AutoLabel = spec; Health = 0.2 } ]
              let hi = [ { roster Ally Mobile with AutoLabel = spec; Health = 0.9 } ]
              Expect.notEqual (bytesOf (Symbology.galleryIn Grammar.Token 1 80.0 lo)) (bytesOf (Symbology.galleryIn Grammar.Token 1 80.0 hi)) "auto-label projects the Token's own Health channel (FR-002)"
              let same1 = [ { roster Ally Mobile with AutoLabel = spec; Health = 0.5 } ]
              let same2 = [ { roster Ally Mobile with AutoLabel = spec; Health = 0.5 } ]
              Expect.equal (bytesOf (Symbology.galleryIn Grammar.Token 1 80.0 same1)) (bytesOf (Symbology.galleryIn Grammar.Token 1 80.0 same2)) "identical channels ⇒ identical projection (per-game stats stay the caller's, FR-021)"
          } ]
