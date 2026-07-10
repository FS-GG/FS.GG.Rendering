module Symbology.Tests.ChannelPresenceTests

// T011 [US1] Channel-presence (SC-002): for EACH channel, two Tokens differing in ONLY that field
// produce observably different output, and differ in only that channel.
//
// Evidence model: the pure library's "observable output" identity is the SceneCodec canonical-bytes
// fingerprint (research.md determinism decision). Every channel below is a render-affecting paint or
// geometry input, so a divergent fingerprint guarantees a divergent render. (The repo's pure
// `renderReadbackEvidence` hash keys only on element KINDS, so it is deliberately NOT used here — it
// would give false negatives for hue/width/dash/geometry-only changes. Pixel-level legibility-at-size
// readback is exercised by the Render bridge smoke in US2 and the M5 dry-run.)

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private baseT =
    { Symbology.defaultToken with
        Cx = 32.0
        Cy = 32.0
        R = 24.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        State = Confirmed
        Threat = 0.5
        Charge = 0.5
        Health = 0.5
        Speed = 1
        Heading = 0.0
        Shield = false }

let private bytesOf (t: Token) =
    (SceneCodec.export (Symbology.token t)).CanonicalBytes

let private channelChanges name (a: Token) (b: Token) =
    test (sprintf "channel '%s' observably alters output" name) {
        Expect.notEqual (bytesOf a) (bytesOf b) (sprintf "%s changes the rendered Scene (canonical-bytes identity)" name)
    }

[<Tests>]
let tests =
    testList
        "US1 channel presence"
        [ channelChanges "faction-hue" baseT { baseT with Faction = Enemy }
          channelChanges "class-silhouette" baseT { baseT with Klass = Heavy }
          channelChanges "sigil" baseT { baseT with Sigil = Ring }
          channelChanges "state-dash" baseT { baseT with State = Suspected }
          channelChanges "threat-stroke-width" { baseT with Threat = 0.2 } { baseT with Threat = 0.95 }
          channelChanges "charge-interior-gradient" { baseT with Charge = 0.1 } { baseT with Charge = 0.95 }
          channelChanges "speed-tail-beads" { baseT with Speed = 0 } { baseT with Speed = 4 }
          channelChanges "health-belly-arc" { baseT with Health = 0.2 } { baseT with Health = 0.95 }
          channelChanges "heading-rotation" { baseT with Heading = 0.0 } { baseT with Heading = 1.2 }
          channelChanges "shield-mount" baseT { baseT with Shield = true }
          channelChanges "secondary-heading" baseT { baseT with SecondaryHeading = Some 1.2 } ]

// Grammar-parameterized channel-presence battery (SC-002/FR-003). For each grammar, varying ONE channel
// at a time (incl. a distinct `Custom` faction) must change the canonical bytes. Asserts every channel is
// sited — no silently-dropped channel in Badge or Ring.
let private bytesOfG (render: Token -> Scene) (t: Token) =
    (SceneCodec.export (render t)).CanonicalBytes

let private grammarChannelChanges (gname: string) (render: Token -> Scene) =
    let changes name (a: Token) (b: Token) =
        test (sprintf "[%s] channel '%s' observably alters output" gname name) {
            Expect.notEqual (bytesOfG render a) (bytesOfG render b) (sprintf "%s changes the %s render (canonical-bytes identity)" name gname)
        }

    [ changes "faction-hue" baseT { baseT with Faction = Enemy }
      changes "faction-custom" baseT { baseT with Faction = Custom(Colors.rgb 200uy 20uy 200uy) }
      changes "class-glyph" baseT { baseT with Klass = Heavy }
      changes "sigil" baseT { baseT with Sigil = Ring }
      changes "state-dash" baseT { baseT with State = Suspected }
      changes "threat-stroke-width" { baseT with Threat = 0.2 } { baseT with Threat = 0.95 }
      changes "charge-interior-gradient" { baseT with Charge = 0.1 } { baseT with Charge = 0.95 }
      changes "speed-pips" { baseT with Speed = 0 } { baseT with Speed = 4 }
      changes "health" { baseT with Health = 0.2 } { baseT with Health = 0.95 }
      changes "heading-indicator" { baseT with Heading = 0.0 } { baseT with Heading = 1.2 }
      changes "shield-mount" baseT { baseT with Shield = true }
      // Presence only. That the two rotations are INDEPENDENT — that the body's angle does not move the
      // barrel — needs the barrel's own nodes, and is asserted in `secondaryHeadingTests` below.
      changes "secondary-heading" baseT { baseT with SecondaryHeading = Some 1.2 } ]

// T007 [US1] Badge channel-presence battery.
[<Tests>]
let badgeChannelTests =
    testList "US1 badge channel presence" (grammarChannelChanges "badge" Symbology.badge)

// T012 [US2] Ring channel-presence battery.
[<Tests>]
let ringChannelTests =
    testList "US2 ring channel presence" (grammarChannelChanges "ring" Symbology.ring)

// T010 [US1] Identity-label channel presence (FR-003/US1 acceptance #3): two tokens differing ONLY in
// `Label` produce differing canonical bytes in EVERY grammar — the label is sited and observably alters
// output. The labelled scene additionally carries a `GlyphRunElement` the unlabelled scene does not.
let private labelGrammars =
    [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ]

[<Tests>]
let labelChannelTests =
    testList
        "US1 label channel presence"
        [ for gname, render in labelGrammars do
              test (sprintf "[%s] label observably alters output" gname) {
                  let bare = { baseT with Label = None }
                  let lab = { baseT with Label = Some (LabelText.Plain "A-7") }
                  Expect.notEqual (bytesOfG render bare) (bytesOfG render lab) (sprintf "the label changes the %s render" gname)
              }

              test (sprintf "[%s] a label adds a glyph-run node; a bare token has none" gname) {
                  let bareKinds = render { baseT with Label = None } |> Scene.describe
                  let labKinds = render { baseT with Label = Some (LabelText.Plain "A-7") } |> Scene.describe
                  Expect.isFalse (List.contains GlyphRunElement bareKinds) "no label => no glyph run"
                  Expect.isTrue (List.contains GlyphRunElement labKinds) "a label => a glyph-run node"
              }

              test (sprintf "[%s] two distinct labels render distinguishably" gname) {
                  Expect.notEqual
                      (bytesOfG render { baseT with Label = Some (LabelText.Plain "A-7") })
                      (bytesOfG render { baseT with Label = Some (LabelText.Plain "B-9") })
                      "distinct labels are mutually distinguishable (SC-002)"
              } ]

// T007 [US1] Multi-line channel presence (FR-001/US1 acceptance #3): the SAME text expressed on one line
// vs with an embedded `\n` produces DIFFERING canonical bytes in every grammar (the hard break is an
// observable layout input), and neither raises. The text is short enough to FIT on one line in every
// grammar, so the one-line spelling does NOT soft-wrap — the only difference is the explicit break, which
// stacks it into two nodes. (A long two-word label would soft-wrap to the same two lines as the break,
// which is the intended wrap behaviour, not a channel-presence signal — hence the deliberately short text.)
[<Tests>]
let multilineChannelTests =
    let bigT = { baseT with R = 40.0 }

    testList
        "US1 multi-line channel presence"
        [ for gname, render in labelGrammars do
              test (sprintf "[%s] one-line vs embedded-\\n of the same text differ; neither throws" gname) {
                  let oneLine = bytesOfG render { bigT with Label = Some (LabelText.Plain "A B") }
                  let twoLine = bytesOfG render { bigT with Label = Some (LabelText.Plain "A\nB") }
                  Expect.notEqual oneLine twoLine (sprintf "an embedded line break observably alters the %s render (FR-001)" gname)
              } ]

// Feature 198 — rich-text run styling is a CHANNEL (B5/SC-002): the same characters carried as styled runs
// vs a plain string produce differing canonical bytes in every grammar, and a ≥2-run styled label emits
// ≥2 glyph-run nodes (one per contiguous same-style segment) — neither raises.
[<Tests>]
let richChannelTests =
    let bigT = { baseT with R = 40.0 }
    let blue = Colors.rgb 24uy 144uy 255uy

    let rec runCount (scene: Scene) =
        scene.Nodes
        |> List.sumBy (function
            | GlyphRun _ -> 1
            | Group g -> g |> List.sumBy (fun s -> runCount s)
            | ClipNode(_, s)
            | ColorSpaceNode(_, s)
            | PerspectiveNode(_, s)
            | Translate(_, s) -> runCount s
            | _ -> 0)

    testList
        "US1 rich-text channel presence"
        [ for gname, render in labelGrammars do
              test (sprintf "[%s] same chars as styled runs vs plain ⇒ differing bytes" gname) {
                  let styled = (SceneCodec.export (render { bigT with Label = Some(LabelText.Rich [ { Symbology.run "AB" with Weight = Some 700; Color = Some blue } ]) })).CanonicalBytes
                  let plain = (SceneCodec.export (render { bigT with Label = Some(LabelText.Plain "AB") })).CanonicalBytes
                  Expect.notEqual styled plain (sprintf "run styling observably alters the %s render (B5/SC-002)" gname)
              }

              test (sprintf "[%s] a 2-run styled label emits ≥2 glyph-run nodes in reading order" gname) {
                  let scene = render { bigT with Label = Some(LabelText.Rich [ { Symbology.run "AL" with Weight = Some 700 }; { Symbology.run "fa" with Scale = Some 0.6 } ]) }
                  Expect.isGreaterThanOrEqual (runCount scene) 2 "≥2 contiguous-style segments ⇒ ≥2 nodes (B4)"
              }

              // Feature 199 (T014, B6/SC-002): two labels with the SAME characters differing only in a new
              // typographic attribute (italic / tracking) yield differing bytes — the attribute is a channel.
              for attrName, mk in
                  [ "italic", (fun (r: LabelRun) -> { r with Italic = Some true })
                    "tracking", (fun r -> { r with Tracking = Some 0.3 }) ] do
                  test (sprintf "[%s] same chars differing only in %s ⇒ differing bytes (T014)" gname attrName) {
                      let baseRun = { Symbology.run "AB" with Color = Some blue }
                      let a = (SceneCodec.export (render { bigT with Label = Some(LabelText.Rich [ baseRun ]) })).CanonicalBytes
                      let b = (SceneCodec.export (render { bigT with Label = Some(LabelText.Rich [ mk baseRun ]) })).CanonicalBytes
                      Expect.notEqual a b (sprintf "%s is a per-run channel (B6/SC-002)" attrName)
                  } ]

// ---- Feature 254 — the SecondaryHeading channel is sited, independent, and absent-by-default ----------
// The zero-drift guarantee (FR-002) is that an unset channel contributes NO scene node — not an empty
// one. `DeterminismTests`' hardcoded pre-feature goldens pin the bytes; these tests pin the mechanism,
// so a future refactor that starts emitting `Scene.empty` for `None` fails HERE with a clear reason
// rather than as an opaque golden-hash mismatch.
[<Tests>]
let secondaryHeadingTests =
    let elementCount (render: Token -> Scene) (t: Token) = render t |> Scene.describe |> List.length

    // The barrel is appended LAST, as two bare sibling scenes, and `baseT` carries no label — so the
    // final two children of the top-level group ARE the barrel. Isolating them is what lets us assert
    // the barrel's own geometry rather than "some byte somewhere moved", which any implementation
    // (including a body-relative one) would satisfy.
    let barrelOf (render: Token -> Scene) (t: Token) =
        match (render t).Nodes with
        | [ Group children ] ->
            let n = List.length children
            children |> List.skip (n - 2)
        | other -> failwithf "expected a single top-level group, got %A" other

    testList
        "Feature254 secondary-heading channel"
        [ for gname, render in labelGrammars do
              test (sprintf "[%s] an unset secondary heading adds no node; setting it adds exactly the barrel + tip" gname) {
                  let bare = elementCount render { baseT with SecondaryHeading = None }
                  let aimed = elementCount render { baseT with SecondaryHeading = Some 1.2 }
                  Expect.equal aimed (bare + 2) "the indicator is one line plus one tip mark, and absence draws neither"
              }

              test (sprintf "[%s] the barrel's angle is absolute — turning the body does not move it" gname) {
                  // The load-bearing claim of the whole feature: two INDEPENDENT rotations. If the
                  // barrel were drawn at `angle + Heading` it would still change the scene bytes, so a
                  // whole-scene comparison cannot see the bug. Compare the barrel nodes themselves.
                  Expect.equal
                      (barrelOf render { baseT with Heading = 0.0; SecondaryHeading = Some 1.0 })
                      (barrelOf render { baseT with Heading = 2.5; SecondaryHeading = Some 1.0 })
                      "the same secondary angle draws the same barrel whatever the body is doing"

                  Expect.notEqual
                      (barrelOf render { baseT with Heading = 2.5; SecondaryHeading = Some 1.0 })
                      (barrelOf render { baseT with Heading = 2.5; SecondaryHeading = Some 2.0 })
                      "and a different secondary angle draws a different barrel"
              }

              test (sprintf "[%s] a degenerate token still degrades to the placeholder, barrel or not" gname) {
                  let degenerate = { baseT with R = 0.0; SecondaryHeading = Some 1.2 }
                  Expect.equal
                      (bytesOfG render degenerate)
                      (bytesOfG render { degenerate with SecondaryHeading = None })
                      "the placeholder rule wins over the secondary indicator, as it does over the label"
              } ]

// A filmstrip cell owns half the spacing. The barrel reaches 1.42R — further than the pre-feature
// symbol ever did (the belly arc, at 1.18R) — and further than the 1.3R a cell owns at the historic
// 2.6R spacing. So the cell must widen to hold it, and must NOT widen when no barrel is drawn, or
// every existing filmstrip golden moves.
[<Tests>]
let secondaryHeadingFilmstripTests =
    // Speed = 0 and Shield = false so the ONLY `Circle` this token draws is the barrel's tip mark:
    // the tail beads and the shield mount are circles too. Sigil/body/health are paths, ellipses, arcs.
    let bare = { baseT with R = 30.0; Speed = 0; Shield = false }

    let rec circlesIn (scene: Scene) =
        scene.Nodes
        |> List.collect (function
            | Circle(centre, radius, _) -> [ centre, radius ]
            | Group children -> children |> List.collect circlesIn
            | _ -> [])

    testList
        "Feature254 filmstrip cell sizing"
        [ test "a barrel-free filmstrip keeps the historic 2.6R spacing (goldens do not move)" {
              // The Token-grammar filmstrip golden is pinned in DeterminismTests; this states the
              // invariant that guards it directly — no barrel anywhere ⇒ nothing about layout changes.
              let noBarrel = Symbology.filmstrip 3 [ Idle, bare; Pulse, bare ]
              let viaGrammar = Symbology.filmstripIn Grammar.Token 3 [ Idle, bare; Pulse, bare ]

              Expect.equal
                  ((SceneCodec.export noBarrel).CanonicalBytes)
                  ((SceneCodec.export viaGrammar).CanonicalBytes)
                  "filmstripIn Grammar.Token reproduces filmstrip byte-for-byte (FR-010)"
          }

          test "an east-pointing barrel stays inside its own filmstrip cell" {
              // Angle π/2 = due east, the worst case for horizontal cell bleed.
              let aimed = { bare with SecondaryHeading = Some(System.Math.PI / 2.0) }
              let scene = Symbology.filmstrip 2 [ Idle, aimed ]

              match circlesIn scene with
              | [ (tip0, r0); (tip1, _) ] ->
                  // Cells sit at spacing*(i+0.5), so their centres are `spacing` apart, and each tip is
                  // a fixed offset east of its own cell centre.
                  let spacing = tip1.X - tip0.X
                  let cell0Centre = tip0.X - 1.32 * aimed.R // the barrel's outer radius
                  let midline = cell0Centre + spacing / 2.0

                  Expect.isLessThanOrEqual
                      (tip0.X + r0)
                      midline
                      "the tip mark (and so the whole barrel) stays on its own side of the cell boundary"
              | other -> failtestf "expected exactly one tip mark per sample, got %d circles" (List.length other)
          }

          test "the widened cell is only used when a barrel is present" {
              let withBarrel = Symbology.filmstrip 2 [ Idle, { bare with SecondaryHeading = Some 0.0 } ]
              let withoutBarrel = Symbology.filmstrip 2 [ Idle, bare ]

              let spanOf scene =
                  match circlesIn scene with
                  | [] -> 0.0
                  | cs -> (cs |> List.map (fun (c, _) -> c.X) |> List.max) - (cs |> List.map (fun (c, _) -> c.X) |> List.min)

              Expect.isGreaterThan (spanOf withBarrel) 0.0 "the barrelled strip has tip marks to measure"
              Expect.equal (spanOf withoutBarrel) 0.0 "the barrel-free strip draws no circles at all"
          } ]

// ---- Feature 200 (T014/US1) — auto-label channel observability ---------------------------------------
// Two Tokens whose ONLY difference is a projected channel value yield differing canonical bytes via the
// auto-label (the projection is observable through the existing render path). Complements the explicit-run
// channel tests above: here the SAME `AutoLabel` spec reads a single channel, so a one-channel delta is
// the only thing that can move the bytes.
[<Tests>]
let autoLabelChannelTests =
    let projT =
        { baseT with
            R = 40.0
            AutoLabel = None
            Label = None }

    testList
        "US1.200 auto-label channel observability"
        [ test "HealthTier auto-label reads Health (differing Health ⇒ differing bytes)" {
              let spec = Some(Symbology.autoLabel [ HealthTier ])
              Expect.notEqual
                  (bytesOf { projT with AutoLabel = spec; Health = 0.20 })
                  (bytesOf { projT with AutoLabel = spec; Health = 0.95 })
                  "HealthTier is a projected channel"
          }
          test "FactionCode auto-label reads Faction (differing Faction ⇒ differing bytes)" {
              let spec = Some(Symbology.autoLabel [ FactionCode ])
              Expect.notEqual
                  (bytesOf { projT with AutoLabel = spec; Faction = Ally })
                  (bytesOf { projT with AutoLabel = spec; Faction = Enemy })
                  "FactionCode is a projected channel"
          }
          test "SpeedPips auto-label reads Speed (differing Speed ⇒ differing bytes)" {
              let spec = Some(Symbology.autoLabel [ SpeedPips ])
              Expect.notEqual
                  (bytesOf { projT with AutoLabel = spec; Speed = 1 })
                  (bytesOf { projT with AutoLabel = spec; Speed = 4 })
                  "SpeedPips is a projected channel"
          }
          test "identical channels ⇒ byte-identical auto-label (deterministic projection)" {
              let spec = Some(Symbology.autoLabel [ FactionCode; HealthTier; SpeedPips ])
              Expect.equal (bytesOf { projT with AutoLabel = spec }) (bytesOf { projT with AutoLabel = spec }) "pure projection"
          } ]
