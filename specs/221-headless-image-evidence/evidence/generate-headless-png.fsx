// Feature 221 — produce committable headless-PNG evidence from the SAME public surface the tests use.
// Renders the pinned representative game scene through `SceneEvidence.renderPng` (SkiaViewer CPU
// rasterizer injected, no GPU/GL/X/display), writes the PNG, a second-run byte-identity check, and a
// timing measurement. Run from repo root AFTER building tests/SkiaViewer.Tests:
//   dotnet fsi specs/221-headless-image-evidence/evidence/generate-headless-png.fsx
#r "/home/developer/projects/FS.GG.Rendering/tests/SkiaViewer.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "/home/developer/projects/FS.GG.Rendering/tests/SkiaViewer.Tests/bin/Debug/net10.0/FS.GG.UI.Scene.dll"
#r "/home/developer/projects/FS.GG.Rendering/tests/SkiaViewer.Tests/bin/Debug/net10.0/FS.GG.UI.SkiaViewer.dll"

open System
open System.IO
open System.Security.Cryptography
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

Text.installPngRasterizer ()

let size: Size = { Width = 800; Height = 600 }

let scene () : Scene =
    Scene.group
        [ Scene.rectangle (0.0, 0.0, 800.0, 600.0) (Colors.rgb 18uy 22uy 30uy)
          Scene.circle { X = 400.0; Y = 300.0 } 120.0 (Colors.rgb 220uy 80uy 60uy)
          Scene.rectangle (40.0, 40.0, 260.0, 64.0) (Colors.rgba 255uy 255uy 255uy 48uy)
          Scene.sizedText (56.0, 84.0) "HP 100 / SCORE 42" 24.0 Colors.white ]

let sha (bytes: byte[]) = SHA256.HashData bytes |> Convert.ToHexString |> fun s -> s.ToLowerInvariant()

let outDir = "specs/221-headless-image-evidence/evidence"
Directory.CreateDirectory outDir |> ignore

match SceneEvidence.renderPng size (scene ()), SceneEvidence.renderPng size (scene ()) with
| Result.Ok a, Result.Ok b ->
    let png = Path.Combine(outDir, "representative-game-scene.png")
    File.WriteAllBytes(png, a)
    // timing: median of 5 single renders
    let times =
        [ for _ in 1..5 ->
              let sw = Diagnostics.Stopwatch.StartNew()
              SceneEvidence.renderPng size (scene ()) |> ignore
              sw.Stop()
              sw.Elapsed.TotalMilliseconds ]
        |> List.sort
    let median = times.[2]
    printfn "PNG bytes: %d" a.Length
    printfn "sha256(run1): %s" (sha a)
    printfn "sha256(run2): %s" (sha b)
    printfn "byte-identical: %b" (a = b)
    printfn "render times ms (sorted): %A" times
    printfn "median ms: %.1f" median
    printfn "under 5s bound: %b" (median < 5000.0)
| other -> eprintfn "render failed: %A" other; exit 1
