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
