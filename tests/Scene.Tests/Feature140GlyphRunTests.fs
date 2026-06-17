module Feature140GlyphRunTests

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }
let private paint = Paint.fill Colors.white
let private samples = [ "Stable"; "Cache 42"; "Symbols #@"; "mono"; "Fallback proof" ]

[<Tests>]
let tests =
    testList
        "Feature140 glyph-run proof data"
        [ test "five deterministic samples produce stable fingerprints" {
              for sample in samples do
                  let a = Scene.buildGlyphRun sample font
                  let b = Scene.buildGlyphRun sample font
                  Expect.equal a.Fingerprint b.Fingerprint (sprintf "fingerprint stable for %s" sample)
                  Expect.equal (Scene.glyphRunFingerprint a) a.Fingerprint "stored fingerprint matches recomputed fingerprint"
          }

          test "measured advance equals the sum of glyph advances" {
              let data = Scene.buildGlyphRun "Stable" font
              let sum = data.Glyphs |> List.sumBy _.Advance
              let measured = Scene.measureGlyphRun data

              Expect.floatClose Accuracy.medium measured.Width sum "glyph advances sum to measured width"
              Expect.equal measured.Height data.Metrics.Height "height is carried through"
              Expect.equal measured.Baseline data.Metrics.Baseline "baseline is carried through"
          }

          test "glyph-run proof is an explicit public Scene element and legacy text remains separate" {
              let data = Scene.buildGlyphRun "Proof" font
              let proof = Scene.glyphRun { X = 8.0; Y = 20.0 } data paint
              let legacy = Scene.textAt { X = 8.0; Y = 20.0 } "Proof" Colors.white

              Expect.contains (Scene.describe proof) GlyphRunElement "glyph-run proof describes explicitly"
              Expect.contains (Scene.describe legacy) TextElement "legacy text still describes as TextElement"
              Expect.isFalse (Scene.describe legacy |> List.contains GlyphRunElement) "non-opt-in text does not become a glyph-run proof"
          }

          test "public constructor exercise builds proof data, node, diagnostics, and fingerprint" {
              let scene = Scene.glyphRunProof { X = 2.0; Y = 24.0 } "FSI" font paint
              Expect.contains (Scene.describe scene) GlyphRunElement "constructor emits glyph-run node"
              Expect.isEmpty (Scene.diagnostics scene) "simple ASCII proof has no fallback diagnostics"

              let data = Scene.buildGlyphRun "FSI" font
              Expect.isNonEmpty data.Fingerprint "fingerprint is available to F# Interactive callers"
              Expect.equal data.Glyphs.Length 3 "one proof glyph per deterministic sample character"
          } ]
