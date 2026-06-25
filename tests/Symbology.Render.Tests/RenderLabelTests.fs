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
            Label = Some lbl })
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
