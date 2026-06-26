module Symbology.Render.Tests.RenderLabelTests

// T011 [US1] Tofu-free label at the render edge (FR-004/SC-002). Tofu-free is a RENDER-EDGE property: the
// pure library emits a deterministic glyph-run proof node; legible, non-tofu glyphs come from the real
// bundled-font registry the renderer draws through (`SceneRenderer.drawFallbackText` → `Fonts.resolveText`,
// which renders each covered character from a real embedded typeface and a genuinely-uncovered one as a
// DISCLOSED tofu box — never a plausible-wrong glyph).
//
// This battery is REAL evidence (Constitution V), not synthetic:
//   1. A labelled board rasterises through `Render.toPng` to a non-blank PNG (the label draws real pixels).
//   2. The label's characters resolve NON-TOFU under the same `Fonts` registry the renderer uses
//      (`report.TofuCount = 0`) — the label glyphs are covered, so they draw as real glyphs.
//   3. A roster of distinct labels is mutually distinguishable.

open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

let private outDir name =
    let d = Path.Combine(Path.GetTempPath(), "fs-gg-symbology-render-tests", name)

    if Directory.Exists d then
        Directory.Delete(d, true)

    Directory.CreateDirectory d |> ignore
    d

let private labelFont =
    { Family = None; Size = 13.0; Weight = None } // matches the label channel (default sans, screen-aligned)

let private labelledBoard labels =
    labels
    |> List.map (fun lbl ->
        { Symbology.defaultToken with
            R = 30.0
            Faction = Ally
            Klass = Mobile
            Sigil = Bolt
            Health = 0.8
            Label = Some (LabelText.Plain lbl) })
    |> Symbology.gallery 4 96.0

let private size = { Width = 384; Height = 96 }

[<Tests>]
let tests =
    testList
        "US1 render label tofu-free"
        [ test "a labelled board rasterises to a non-blank PNG" {
              let path = Render.toPng size (labelledBoard [ "A-7"; "HMR"; "K9"; "ZULU" ]) (outDir "label-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the labelled board is non-blank"
          }

          test "label characters resolve NON-TOFU under the renderer's real font registry (FR-004)" {
              // Same registry path the renderer draws through. Covered chars => real glyphs; only a genuinely
              // uncovered char would become a disclosed tofu box. Latin callsigns must be fully covered.
              for label in [ "A-7"; "HAMMER"; "K9"; "ZULU-1" ] do
                  let report = Fonts.resolveText labelFont label |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "label %s draws real glyphs (no tofu) under the bundled font" label)
          }

          test "a roster of distinct labels is mutually distinguishable" {
              let a = (SceneCodec.export (labelledBoard [ "A-7"; "B-8" ])).CanonicalBytes
              let b = (SceneCodec.export (labelledBoard [ "C-1"; "D-2" ])).CanonicalBytes
              Expect.notEqual a b "distinct labels produce observably distinct output (SC-002)"
          } ]

// T009 [US1] Multi-line label tofu-free at the render edge (FR-004/SC-002). A `\n`-bearing labelled token
// rasterises to a non-blank PNG, and EVERY line (not just the first) resolves NON-TOFU under the same real
// font registry the renderer draws through — multi-line draws real glyphs on every line, never a tofu box.
let private multilineToken lbl =
    { Symbology.defaultToken with
        R = 40.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        Health = 0.8
        Label = Some (LabelText.Plain lbl) }

[<Tests>]
let multilineTests =
    testList
        "US1 render multi-line label tofu-free"
        [ test "a multi-line labelled token rasterises to a non-blank PNG" {
              let board = Symbology.gallery 1 120.0 [ multilineToken "ALPHA\nBRAVO\nCHARLIE" ]
              let path = Render.toPng { Width = 160; Height = 160 } board (outDir "multiline-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the multi-line labelled board is non-blank"
          }

          test "EVERY line of a multi-line label resolves non-tofu under the renderer's real font registry (FR-004)" {
              // Each `\n`-delimited line is shaped/drawn independently; assert each draws real glyphs, no tofu.
              for line in [ "ALPHA"; "BRAVO"; "CHARLIE"; "HOTEL-1"; "K9" ] do
                  let report = Fonts.resolveText labelFont line |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "multi-line line %s draws real glyphs (no tofu) under the bundled font" line)
          } ]

// Feature 198 [US1] Styled-run label tofu-free at the render edge (FR-005/SC-002/B4). A MULTI-RUN styled
// token (per-run colour / weight / size) rasterises to a non-blank PNG, and EVERY run resolves NON-TOFU
// under the same real `Fonts` registry the renderer draws through — each styled run draws real glyphs in
// its own weight/size, never a tofu box. Two same-character / different-styling labels differ in output.
let private blue = Colors.rgb 24uy 144uy 255uy
let private amber = Colors.rgb 250uy 173uy 20uy

let private styledToken (runs: LabelRun list) =
    { Symbology.defaultToken with
        R = 40.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        Health = 0.8
        Label = Some(LabelText.Rich runs) }

[<Tests>]
let styledTests =
    testList
        "US1 render styled-run label tofu-free"
        [ test "a styled multi-run labelled token rasterises to a non-blank PNG" {
              let board =
                  Symbology.gallery
                      1
                      140.0
                      [ styledToken
                            [ { Symbology.run "BRAVO" with Weight = Some 700; Color = Some blue }
                              { Symbology.run " ac12" with Scale = Some 0.7; Color = Some amber } ] ]

              let path = Render.toPng { Width = 200; Height = 160 } board (outDir "styled-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the styled board is non-blank (real glyphs drawn)"
          }

          test "EVERY styled run resolves non-tofu in its own weight/size under the real font registry (FR-005)" {
              // base label size for the Token grammar is R * 0.5 = 20.0; each run is measured in its resolved font.
              let runs =
                  [ "BRAVO", Some 700, 1.0
                    "ac12", None, 0.7
                    "HOTEL-1", Some 600, 1.0
                    "K9", None, 0.5 ]

              for text, weight, scale in runs do
                  let font = { Family = None; Size = max 1.0 (20.0 * scale); Weight = weight }
                  let report = Fonts.resolveText font text |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "styled run %s (weight=%A scale=%f) draws real glyphs, no tofu" text weight scale)
          }

          test "same characters / different run styling produce distinguishable rasterised output (SC-002)" {
              let styled =
                  Symbology.gallery 1 140.0 [ styledToken [ { Symbology.run "ALFA" with Weight = Some 700; Color = Some blue } ] ]

              let plain =
                  Symbology.gallery 1 140.0 [ { Symbology.defaultToken with R = 40.0; Faction = Ally; Klass = Mobile; Sigil = Bolt; Health = 0.8; Label = Some(LabelText.Plain "ALFA") } ]

              Expect.notEqual (SceneCodec.export styled).CanonicalBytes (SceneCodec.export plain).CanonicalBytes "run styling observably changes the output (B5/SC-002)"
          } ]

// Feature 199 [US1] (T015) Decorated-run label tofu-free at the render edge (FR-006/SC-002/B5). A token whose
// runs set italic / underline / strike / tracking rasterises to a non-blank PNG, and EVERY run resolves
// NON-TOFU under the same real `Fonts` registry the renderer draws through — synthetic slant wraps real
// glyphs, decoration is a non-text rule, tracking splits into per-char real glyphs: never a tofu box.
[<Tests>]
let decoratedTests =
    testList
        "US1.199 render decorated-run label tofu-free"
        [ test "a decorated (italic/underline/strike/tracking) labelled token rasterises to a non-blank PNG" {
              let board =
                  Symbology.gallery
                      1
                      160.0
                      [ styledToken
                            [ { Symbology.run "QUOTED" with Italic = Some true; Color = Some blue }
                              { Symbology.run " TAG" with Underline = Some true }
                              { Symbology.run " OLD" with Strike = Some true; Color = Some amber }
                              { Symbology.run " S P A C E D" with Tracking = Some 0.25 } ] ]

              let path = Render.toPng { Width = 240; Height = 180 } board (outDir "decorated-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the decorated board is non-blank (real glyphs drawn)"
          }

          test "EVERY decorated run resolves non-tofu under the real font registry (FR-006)" {
              // Each run's characters must draw as real glyphs regardless of slant/decoration/tracking.
              for text in [ "QUOTED"; "TAG"; "OLD"; "SPACED"; "K9-1" ] do
                  let report = Fonts.resolveText labelFont text |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "decorated run %s draws real glyphs (no tofu) under the bundled font" text)
          } ]

// Feature 199 [US3] (T036) Full-layout render-bridge tofu test (FR-006/SC-002). EXTENDS the decorated-run
// case above with paragraph layout + justification: a LAID-OUT (justified, multi-paragraph) + DECORATED
// (italic/underline/strike/tracking) labelled token rasterises to a non-blank PNG and EVERY run resolves
// NON-TOFU under the real font registry — alignment / justification / decoration never produce a tofu box.
let private laidToken paragraphs =
    { Symbology.defaultToken with
        R = 44.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        Health = 0.8
        Label = Some(Symbology.laidLabel paragraphs) }

[<Tests>]
let laidLayoutTests =
    testList
        "US3.199 render laid-out label tofu-free"
        [ test "a laid-out (justified, multi-paragraph, decorated) labelled token rasterises to a non-blank PNG" {
              let board =
                  Symbology.gallery
                      1
                      200.0
                      [ laidToken
                            [ Symbology.align Justify [ { Symbology.run "ALPHA BRAVO CHARLIE" with Italic = Some true; Color = Some blue } ]
                              Symbology.align Trailing [ { Symbology.run "RETIRED" with Strike = Some true; Underline = Some true }; { Symbology.run " S P" with Tracking = Some 0.2 } ] ] ]

              let path = Render.toPng { Width = 240; Height = 220 } board (outDir "laid-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the laid-out board is non-blank (real glyphs drawn)"
          }

          test "EVERY run of a justified / multi-paragraph / decorated label resolves non-tofu (FR-006)" {
              for text in [ "ALPHA"; "BRAVO"; "CHARLIE"; "RETIRED"; "SP" ] do
                  let report = Fonts.resolveText labelFont text |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "laid-out run %s draws real glyphs (no tofu) under the bundled font" text)
          } ]

// Feature 200 [US1] (T015) Auto-label tofu-free at the render edge (FR-010/SC-002). A Token with
// AutoLabel = Some _ and Label = None projects a channel-derived label; rasterise it through Render.toPng
// under the real measurer and assert the board is non-blank and EVERY character the projection can emit
// resolves NON-TOFU under the same `Fonts` registry the renderer draws through.
let private autoToken fields =
    { Symbology.defaultToken with
        R = 36.0
        Faction = Enemy
        Klass = Heavy
        State = Suspected
        Threat = 0.7
        Speed = 3
        Health = 0.87
        Shield = true
        Label = None
        AutoLabel = Some(Symbology.autoLabel fields) }

[<Tests>]
let autoLabelTofuTests =
    testList
        "US1.200 render auto-label tofu-free"
        [ test "an auto-labelled token (AutoLabel=Some, Label=None) rasterises to a non-blank PNG" {
              let board =
                  Symbology.gallery 1 140.0 [ autoToken [ FactionCode; KlassCode; HealthTier; SpeedPips; ShieldFlag ] ]

              let path = Render.toPng { Width = 200; Height = 160 } board (outDir "auto-label-pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "the auto-labelled board is non-blank (projected glyphs drawn)"
          }

          test "every code the projection can emit resolves non-tofu under the real font registry (FR-010)" {
              // The full game-agnostic code alphabet the projection draws: faction / class / state codes,
              // tier prefixes, and digits. All Latin/ASCII ⇒ fully covered ⇒ zero tofu.
              let codes =
                  [ "ALY"; "ENY"; "NEU"; "CUS"; "MOB"; "HVY"; "SCT"; "CFM"; "SUS"; "SHD"; "H87"; "T4"; "S3"; "ENY H87 HVY" ]

              for code in codes do
                  let report = Fonts.resolveText labelFont code |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "projected code %s draws real glyphs (no tofu)" code)
          } ]

// Feature 200 [US3] (T033) Auto + motion render-bridge tofu test (FR-010/SC-002). EXTENDS the US1 auto-label
// case (T015) with a BOUND LabelMotion sampled at NON-REST phases: rasterise an auto-derived + motion-bound
// token through `animateIn` → Render.toPng under the real measurer at sampled phases; assert the board is
// non-blank and EVERY projected code resolves NON-TOFU under the same font registry the renderer draws through.
let private autoMotionToken =
    { Symbology.defaultToken with
        R = 36.0
        Faction = Enemy
        Klass = Heavy
        State = Suspected
        Threat = 0.7
        Speed = 3
        Health = 0.87
        Shield = true
        Label = None
        AutoLabel = Some(Symbology.autoLabel [ FactionCode; KlassCode; HealthTier ])
        LabelMotion = Some LabelMotion.TypeOn }

[<Tests>]
let autoMotionTofuTests =
    testList
        "US3.200 render auto+motion tofu-free"
        [ test "an auto+motion token rasterises to a non-blank PNG at sampled non-rest phases" {
              for i, phase in List.indexed [ 0.25; 0.5; 0.75 ] do
                  let board = Symbology.filmstripIn Grammar.Token 4 [ Idle, autoMotionToken ]
                  let path = Render.toPng { Width = 240; Height = 120 } board (outDir (sprintf "auto-motion-%d" i))
                  Expect.isTrue (File.Exists path) "image file was written"
                  Expect.isTrue ((FileInfo path).Length > 0L) (sprintf "the auto+motion filmstrip is non-blank (phase sample %f)" phase)
          }

          test "a single non-rest animateIn frame of an auto+motion token is non-blank" {
              let centred = { autoMotionToken with Cx = 80.0; Cy = 60.0 }
              let scene = Symbology.animateIn Grammar.Token Idle centred 0.5
              let path = Render.toPng { Width = 160; Height = 120 } scene (outDir "auto-motion-frame")
              Expect.isTrue ((FileInfo path).Length > 0L) "the animated projected label draws real pixels at a non-rest phase"
          }

          test "every projected code the auto+motion label can reveal resolves non-tofu (FR-010)" {
              // TypeOn reveals whole-glyph PREFIXES of the projected codes; every prefix is still Latin/ASCII.
              for code in [ "ENY"; "HVY"; "H87"; "E"; "EN"; "ENY HVY"; "ENY HVY H8" ] do
                  let report = Fonts.resolveText labelFont code |> Fonts.report
                  Expect.equal report.TofuCount 0 (sprintf "revealed prefix %s draws real glyphs (no tofu)" code)
          } ]
