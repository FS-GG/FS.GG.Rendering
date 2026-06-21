module Feature183CodecSymmetryTests

// Feature 183 (US2 / FR-002 / SC-002) — the read-side symmetry guard for the SceneNode binary codec.
//
// The write side is an exhaustive `match node` (FS0025-as-error = compile gate); the read side is now a
// per-case table (`SceneCodec.fs`). This test is the runtime half: one value of ALL 25 cases must
// round-trip identically through the public package codec, the canonical bytes must be deterministic,
// and (frozen wire format) must equal the captured baseline. A missing/forgotten read row would drop or
// mis-read a case here; a wire-format change would break the byte hash.

open System.Security.Cryptography
open Expecto
open FS.GG.UI.Scene

let private color = Colors.rgb 18uy 96uy 180uy
let private paint = Paint.fill color
let private rect: Rect = { X = 1.0; Y = 2.0; Width = 10.0; Height = 12.0 }
let private font: FontSpec = { Family = Some "Noto Sans"; Size = 14.0; Weight = Some 400 }

let private samplePath =
    Path.create Winding [ Path.moveTo 0.0 0.0; Path.lineTo 8.0 0.0; Path.lineTo 8.0 8.0; Path.close ]

let private inner: Scene = { Nodes = [ Empty ] }

/// One value of every one of the 25 SceneNode cases, in tag order (0..24) for readability.
let private allCases: SceneNode list =
    [ Empty // 0
      Group [ inner ] // 1
      Rectangle((0.0, 0.0, 10.0, 10.0), color) // 2
      PaintedRectangle(rect, paint) // 3
      Circle({ X = 5.0; Y = 5.0 }, 3.0, color) // 4
      FilledEllipse(rect, color) // 5
      Ellipse(rect, paint) // 6
      Line({ X = 0.0; Y = 0.0 }, { X = 10.0; Y = 10.0 }, paint) // 7
      SceneNode.Path(samplePath, paint) // 8
      Points([ { X = 1.0; Y = 1.0 }; { X = 2.0; Y = 2.0 } ], paint) // 9
      Vertices(Triangles, [ { Position = { X = 0.0; Y = 0.0 }; Color = Some color }
                            { Position = { X = 4.0; Y = 0.0 }; Color = None }
                            { Position = { X = 2.0; Y = 4.0 }; Color = Some color } ], paint) // 10
      Arc(rect, 0.0, 90.0, paint) // 11
      Text((1.0, 2.0), "hi", color) // 12
      TextRun({ Text = "run"; Position = { X = 0.0; Y = 0.0 }; Font = font; Paint = paint }) // 13
      Image((0.0, 0.0, 8.0, 8.0), "img.png") // 14
      ClipNode(RectClip rect, inner) // 15
      RegionNode({ Bounds = [ rect ]; Operation = Replace }, paint) // 16
      ColorSpaceNode(DisplayP3, inner) // 17
      PerspectiveNode({ M11 = 1.0; M12 = 0.0; M13 = 0.0
                        M21 = 0.0; M22 = 1.0; M23 = 0.0
                        M31 = 0.0; M32 = 0.0; M33 = 1.0 }, inner) // 18
      PictureNode({ Name = "pic"; Scene = inner }) // 19
      Chart([ 1.0; 2.0; 3.0 ]) // 20
      Translate((4.0, 5.0), inner) // 21
      SizedText((1.0, 2.0), "sized", 12.0, color) // 22
      GlyphRun({ Data = Scene.buildGlyphRun "glyph" font; Position = { X = 0.0; Y = 0.0 }; Paint = paint }) // 23
      CachedSubtree({ CacheId = 7UL; Fingerprint = 11UL; Scene = inner }) ] // 24

let private allCasesScene: Scene = { Nodes = allCases }

let private sha256Hex (bytes: byte[]) =
    use sha = SHA256.Create()
    sha.ComputeHash bytes |> Array.map (fun b -> b.ToString "x2") |> String.concat ""

// Frozen baseline: SHA-256 of the canonical package bytes for `allCasesScene`. Captured from HEAD
// before the codec refactor (the write path is byte-unchanged, so this is the pre-edit oracle). A
// wire-format regression changes this hash.
let private baselineSha = "be1397bcc3783746c769791383549cd4fc063474e202dd3358fd3fda539eb96b"

[<Tests>]
let tests =
    testList "Feature183CodecSymmetry" [
        test "every SceneNode case is present (25 cases, tags 0..24)" {
            Expect.equal (List.length allCases) 25 "the corpus must cover all 25 SceneNode cases"
        }

        test "all 25 cases round-trip through the read table — semantic equivalence (SC-002)" {
            // The read side is now table-driven; a missing/forgotten row would drop or mis-read a tag,
            // failing import or the semantic compare here. (Glyph-run shaping evidence is normalized by
            // the codec — pre-existing, so semantic compare per the Feature146 precedent.)
            let package = SceneCodec.export allCasesScene

            match SceneCodec.importPackage package.CanonicalBytes with
            | Result.Error diagnostics ->
                failtestf "package import failed: %A" (SceneCodec.formatDiagnostics diagnostics)
            | Result.Ok restored ->
                let comparison = SceneCodec.compareScenes package.Scene restored.Scene
                Expect.isTrue comparison.Equivalent (String.concat "; " (SceneCodec.formatDiagnostics comparison.Diagnostics))
        }

        test "each case individually round-trips through its table reader (SC-002)" {
            // Per-case: a single-node scene must import (the table has a reader for the tag) and compare
            // semantically equivalent. A missing/mis-mapped row desyncs the byte stream → import fails
            // or the kinds diverge here. Covers all 25 tags individually.
            allCases
            |> List.iter (fun node ->
                let single = { Nodes = [ node ] }
                let package = SceneCodec.export single

                match SceneCodec.importPackage package.CanonicalBytes with
                | Result.Error diagnostics ->
                    failtestf "import failed for %A: %A" node (SceneCodec.formatDiagnostics diagnostics)
                | Result.Ok restored ->
                    let comparison = SceneCodec.compareScenes package.Scene restored.Scene
                    Expect.isTrue comparison.Equivalent
                        (sprintf "case did not round-trip: %A — %s" node
                            (String.concat "; " (SceneCodec.formatDiagnostics comparison.Diagnostics))))
        }

        test "canonical codec bytes are deterministic" {
            let a = (SceneCodec.export allCasesScene).CanonicalBytes
            let b = (SceneCodec.export allCasesScene).CanonicalBytes
            Expect.sequenceEqual a b "canonical bytes are deterministic"
        }

        test "canonical codec bytes match the frozen baseline (wire format unchanged)" {
            let actual = sha256Hex (SceneCodec.export allCasesScene).CanonicalBytes
            Expect.equal actual baselineSha "SceneNode codec wire bytes must be byte-identical to baseline"
        }
    ]
