module Symbology.Tests.AutoLabelTests

// Feature 200 — auto-derived labels projected from a Token's OWN encoded channels (US1).
// Exercises the projection ONLY through the public `render`/`token` surface + the public SceneCodec
// canonical-bytes identity (the projection helpers are internal, omitted from the .fsi).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

let private baseT =
    { Symbology.defaultToken with
        Cx = 100.0
        Cy = 100.0
        R = 40.0
        Faction = Enemy
        Klass = Heavy
        State = Suspected
        Threat = 0.6
        Speed = 2
        Health = 0.87
        Shield = true }

let private bytesIn grammar (t: Token) =
    (SceneCodec.export (Symbology.render grammar t)).CanonicalBytes

[<Tests>]
let tests =
    testList
        "AutoLabel"
        [
          // T010 — projection presence: an AutoLabel token with no explicit Label draws a NON-EMPTY label
          // whose bytes DIFFER from the same token with AutoLabel = None, in every grammar; neither raises.
          testList
              "T010 projection presence (C1/SC-001/SC-002)"
              [ for grammar in grammars ->
                    test (sprintf "auto-label adds an observable label node — %A" grammar) {
                        let auto =
                            { baseT with AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier ]); Label = None }

                        let off = { baseT with AutoLabel = None; Label = None }
                        Expect.notEqual (bytesIn grammar auto) (bytesIn grammar off) "the projected label is drawn (bytes differ from no-auto-label)"
                    } ]

          // T011 — channel determinism: two tokens differing in ONE channel a selected AutoField reads yield
          // DIFFERING auto-label bytes; identical channels yield BYTE-IDENTICAL auto-label bytes.
          test "T011 differing projected channel ⇒ differing auto-label (HealthTier, C3/FR-004)" {
              let spec = Some(Symbology.autoLabel [ HealthTier ])
              let lo = { baseT with AutoLabel = spec; Label = None; Health = 0.30 }
              let hi = { baseT with AutoLabel = spec; Label = None; Health = 0.90 }
              Expect.notEqual (bytesIn Grammar.Token lo) (bytesIn Grammar.Token hi) "HealthTier reads Health: differing health ⇒ differing label"
          }

          test "T011 differing projected channel ⇒ differing auto-label (FactionCode, C3)" {
              let spec = Some(Symbology.autoLabel [ FactionCode ])
              let ally = { baseT with AutoLabel = spec; Label = None; Faction = Ally }
              let enemy = { baseT with AutoLabel = spec; Label = None; Faction = Enemy }
              Expect.notEqual (bytesIn Grammar.Token ally) (bytesIn Grammar.Token enemy) "FactionCode reads Faction"
          }

          test "T011 identical channels ⇒ byte-identical auto-label (deterministic pure projection, FR-004)" {
              let spec = Some(Symbology.autoLabel [ FactionCode; KlassCode; StateCode; HealthTier; ThreatTier; SpeedPips; ShieldFlag ])
              let a = { baseT with AutoLabel = spec; Label = None }
              let b = { baseT with AutoLabel = spec; Label = None }
              Expect.equal (bytesIn Grammar.Token a) (bytesIn Grammar.Token b) "identical channels ⇒ identical projection bytes"
          }

          // T012 — explicit overrides auto: a token with BOTH AutoLabel and an explicit Label renders the
          // EXPLICIT label (byte-identical to the same token with AutoLabel = None) and NOT the projection.
          testList
              "T012 explicit overrides auto (C2/FR-003/SC-005)"
              [ for grammar in grammars ->
                    test (sprintf "explicit Label wins over AutoLabel — %A" grammar) {
                        let explicit =
                            { baseT with
                                AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier ])
                                Label = Some(Symbology.plainLabel "BRAVO-6") }

                        let explicitOnly = { explicit with AutoLabel = None }
                        let projectionOnly = { explicit with Label = None }
                        Expect.equal (bytesIn grammar explicit) (bytesIn grammar explicitOnly) "explicit label drawn (AutoLabel ignored when Label present)"
                        Expect.notEqual (bytesIn grammar explicit) (bytesIn grammar projectionOnly) "the projection is NOT what gets drawn"
                    } ]

          // T013 — degenerate projection: empty Fields, and a single dropped field (ShieldFlag w/ Shield=false)
          // ⇒ NO label node (bytes identical to AutoLabel = None), no exception, in every grammar.
          testList
              "T013 degenerate projection ⇒ no label (C4/FR-004/FR-012)"
              [ for grammar in grammars ->
                    test (sprintf "empty fields & all-dropped fields ⇒ no label — %A" grammar) {
                        // Hold every NON-label channel equal so the only possible delta is the label node.
                        let off = { baseT with AutoLabel = None; Label = None }
                        let emptyFields = { off with AutoLabel = Some(Symbology.autoLabel []) }
                        // ShieldFlag drops when Shield = false — keep both at Shield = false so the body matches.
                        let offNoShield = { off with Shield = false }
                        let droppedOnly = { offNoShield with AutoLabel = Some(Symbology.autoLabel [ ShieldFlag ]) }
                        Expect.equal (bytesIn grammar emptyFields) (bytesIn grammar off) "[] fields ⇒ no label node"
                        Expect.equal (bytesIn grammar droppedOnly) (bytesIn grammar offNoShield) "[ShieldFlag] with Shield=false ⇒ projects to nothing ⇒ no label"
                    } ]
        ]
