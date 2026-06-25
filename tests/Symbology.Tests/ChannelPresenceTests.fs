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
