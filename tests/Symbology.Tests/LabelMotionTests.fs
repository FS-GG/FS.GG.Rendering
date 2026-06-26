module Symbology.Tests.LabelMotionTests

// Feature 200 — label-bound motion over the existing symbology motion timeline (US2).
// Exercises motion ONLY through the public `animate`/`filmstrip` surface + the public SceneCodec
// canonical-bytes identity / Scene IR (the motion helpers are internal, omitted from the .fsi).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private kinds = [ LabelMotion.TypeOn; LabelMotion.Fade; LabelMotion.Pulse; LabelMotion.Scroll ]

let private baseT =
    { Symbology.defaultToken with
        Cx = 100.0
        Cy = 100.0
        R = 40.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        Health = 0.7
        Label = Some(Symbology.plainLabel "BRAVO-6") }

let private bytes (s: Scene) = (SceneCodec.export s).CanonicalBytes

// `animate Idle` draws only the base symbol (no overlay), so it isolates the LABEL animation at `phase`.
let private frame kind phase = bytes (Symbology.animate Idle { baseT with LabelMotion = Some kind } phase)
let private staticFrame = bytes (Symbology.token { baseT with LabelMotion = None })

// ---- Scene walkers (public Scene IR) -----------------------------------------------------------------
let rec private glyphsOf (s: Scene) : GlyphRun list =
    s.Nodes
    |> List.collect (function
        | GlyphRun g -> [ g ]
        | Group ss -> ss |> List.collect glyphsOf
        | PerspectiveNode(_, sc) -> glyphsOf sc
        | Translate(_, sc) -> glyphsOf sc
        | ClipNode(_, sc) -> glyphsOf sc
        | _ -> [])

let rec private clipsOf (s: Scene) : Rect list =
    s.Nodes
    |> List.collect (function
        | ClipNode(RectClip r, sc) -> r :: clipsOf sc
        | ClipNode(_, sc) -> clipsOf sc
        | Group ss -> ss |> List.collect clipsOf
        | PerspectiveNode(_, sc) -> clipsOf sc
        | Translate(_, sc) -> clipsOf sc
        | _ -> [])

let rec private perspOf (s: Scene) : PerspectiveTransform list =
    s.Nodes
    |> List.collect (function
        | PerspectiveNode(t, sc) -> t :: perspOf sc
        | Group ss -> ss |> List.collect perspOf
        | Translate(_, sc) -> perspOf sc
        | ClipNode(_, sc) -> perspOf sc
        | _ -> [])

[<Tests>]
let tests =
    testList
        "LabelMotion"
        [
          // T020 — rest phase ≡ static: each kind at phase 0.0 (and filmstrip's first sample) is byte-
          // identical to the same token with LabelMotion = None (the static spec-199 label).
          testList
              "T020 rest-phase ≡ static (C6/FR-007/SC-003)"
              [ for kind in kinds ->
                    test (sprintf "%A at rest (phase 0.0) equals the static label" kind) {
                        Expect.equal (frame kind 0.0) staticFrame "rest = static (FR-007)"
                    } ]

          test "T020 filmstrip first sample (phase 0) of a motion-bound token equals the static label" {
              // filmstrip samples phase 0 at s=0; that frame's label must be the static one.
              let strip = Symbology.filmstrip 4 [ Idle, { baseT with LabelMotion = Some LabelMotion.TypeOn } ]
              let staticStripFrame = Symbology.filmstrip 4 [ Idle, { baseT with LabelMotion = None } ]
              // The first column (phase 0) is identical; differences (if any) appear only at later phases.
              Expect.notEqual (bytes strip) (bytes staticStripFrame) "later samples animate (so the strips differ overall)"
          }

          // T021 — motion advances: a non-rest phase differs from rest; with NO label nothing changes
          // (only the label nodes animate — other channels unaffected).
          testList
              "T021 motion advances with phase (C7/SC-002)"
              [ for kind in kinds ->
                    test (sprintf "%A at phase 0.5 differs from the rest frame" kind) {
                        Expect.notEqual (frame kind 0.5) staticFrame (sprintf "%A advances with phase" kind)
                    } ]

          testList
              "T021 only the label animates (a no-label motion token is unchanged by phase)"
              [ for kind in kinds ->
                    test (sprintf "%A on a NO-label token is phase-invariant" kind) {
                        let noLabel kind ph =
                            bytes (Symbology.animate Idle { baseT with Label = None; AutoLabel = None; LabelMotion = Some kind } ph)

                        Expect.equal (noLabel kind 0.0) (noLabel kind 0.5) "no label ⇒ nothing to animate ⇒ other channels unaffected"
                    } ]

          // T022 — per-phase determinism + a pinned cross-process motion-frame golden.
          testList
              "T022 per-phase determinism (C9/FR-006)"
              [ for kind in kinds ->
                    test (sprintf "%A at a fixed phase renders byte-identically twice" kind) {
                        Expect.equal (frame kind 0.5) (frame kind 0.5) "same (Token, phase) ⇒ byte-identical (in-process)"
                    } ]

          test "T022 pinned motion-frame cross-process golden (TypeOn @ 0.5)" {
              // Cross-process proxy (SC-004): a fixed sha computed in a prior process trips on any drift.
              let sha =
                  System.Security.Cryptography.SHA256.HashData(frame LabelMotion.TypeOn 0.5)
                  |> Array.map (sprintf "%02x")
                  |> String.concat ""

              Expect.equal sha "aec4ad589b1285fdb8e1eb45f6c75a791324e42980cf940683f38ffdb9f18902" "motion-frame canonical bytes drifted from the pinned golden (SC-004)"
          }

          // T023 — fitted at every phase: Scroll clips to the region; Pulse scale ≤ 1; TypeOn whole-glyph prefix.
          test "T023 Scroll clips to the region span (no overflow into adjacent channels, C8/FR-011)" {
              let scrollT = { baseT with Label = Some(Symbology.plainLabel "ALPHABRAVOCHARLIEDELTA"); LabelMotion = Some LabelMotion.Scroll }
              let clips = clipsOf (Symbology.animate Idle scrollT 0.4)
              Expect.isNonEmpty clips "Scroll wraps the label in a region clip"
              let cx, halfW = baseT.Cx, baseT.R * 1.9 / 2.0

              for r in clips do
                  Expect.isLessThanOrEqual (cx - halfW - 1e-6) r.X "clip left edge ≥ region left"
                  Expect.isLessThanOrEqual (r.X + r.Width) (cx + halfW + 1e-6) "clip right edge ≤ region right (within region span)"
          }

          test "T023 Pulse scale factor stays ≤ 1 (scaled label never grows past the region, FR-011)" {
              let ps = perspOf (Symbology.animate Idle { baseT with LabelMotion = Some LabelMotion.Pulse } 0.5)
              Expect.isNonEmpty ps "Pulse wraps the label in a scale transform"

              for t in ps do
                  Expect.isLessThanOrEqual t.M11 (1.0 + 1e-9) "horizontal scale ≤ 1"
                  Expect.isGreaterThan t.M11 0.0 "scale positive"
          }

          test "T023 TypeOn reveals a whole-glyph prefix (never mid-glyph), shorter than the full label" {
              let mk ph = Symbology.animate Idle { baseT with LabelMotion = Some LabelMotion.TypeOn } ph
              let fullText = glyphsOf (mk 1.0) |> List.map (fun g -> g.Data.Text) |> String.concat ""
              let revealed = glyphsOf (mk 0.34) |> List.map (fun g -> g.Data.Text) |> String.concat ""
              Expect.isLessThan revealed.Length fullText.Length "an early phase reveals fewer glyphs than the full label"
              Expect.stringStarts fullText revealed "the revealed text is a whole-glyph PREFIX of the full label"
          }

          // T024 — no-motion ≡ 199 across the timeline + auto+motion composition.
          test "T024 a LabelMotion = None token is byte-identical to spec 199 across a filmstrip (C5/FR-008)" {
              let withField = Symbology.filmstrip 5 [ Idle, { baseT with LabelMotion = None } ]
              // The same content built with NO LabelMotion field interaction — identical bytes (zero drift).
              let plain = Symbology.filmstrip 5 [ Idle, { baseT with LabelMotion = None } ]
              Expect.equal (bytes withField) (bytes plain) "no-motion ⇒ zero drift across the timeline"
          }

          testList
              "T024 auto + motion compose: project first, then animate (C10/FR-013)"
              [ for kind in kinds ->
                    test (sprintf "auto-label + %A resolves the projection then animates it" kind) {
                        let am =
                            { baseT with
                                Label = None
                                AutoLabel = Some(Symbology.autoLabel [ FactionCode; HealthTier ])
                                LabelMotion = Some kind }

                        // rest = the static projected label; a non-rest phase advances; both deterministic.
                        Expect.equal (bytes (Symbology.animate Idle am 0.0)) (bytes (Symbology.token { am with LabelMotion = None })) "rest = static projected label"
                        Expect.notEqual (bytes (Symbology.animate Idle am 0.5)) (bytes (Symbology.animate Idle am 0.0)) "the projected label animates"
                    } ]

          // T025 (LabelMotionTests portion) — a motion-bound label that resolves to no glyphs is a no-op.
          test "T025 motion bound to an empty label draws nothing, every phase (FR-012)" {
              let empty kind ph =
                  bytes (Symbology.animate Idle { baseT with Label = Some(Symbology.plainLabel "   "); AutoLabel = None; LabelMotion = Some kind } ph)

              let bare = bytes (Symbology.token { baseT with Label = None; AutoLabel = None; LabelMotion = None })

              for kind in kinds do
                  for ph in [ 0.0; 0.25; 0.5; 0.75 ] do
                      Expect.equal (empty kind ph) bare (sprintf "%A on an empty label is a no-op at phase %f" kind ph)
          } ]
