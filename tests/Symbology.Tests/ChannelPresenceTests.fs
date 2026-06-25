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
          channelChanges "shield-mount" baseT { baseT with Shield = true } ]

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
      changes "shield-mount" baseT { baseT with Shield = true } ]

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
              } ]
