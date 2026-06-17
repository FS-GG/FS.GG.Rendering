module Feature146PortableSceneRoundTripTests

open Expecto
open FS.GG.UI.Scene

let private samplePaint =
    Paint.fill (Colors.rgb 18uy 96uy 180uy)
    |> Paint.withOpacity 0.92
    |> Paint.withPathEffect (Dash([ 4.0; 2.0 ], 0.0))

let private representativeScene =
    let path =
        Path.create
            EvenOdd
            [ Path.moveTo 0.0 0.0
              Path.lineTo 64.0 0.0
              Path.lineTo 64.0 48.0
              Path.close ]

    let glyph =
        Scene.glyphRunProof
            { X = 8.0; Y = 80.0 }
            "Render anywhere"
            { Family = Some "Noto Sans"; Size = 18.0; Weight = Some 400 }
            (Paint.fill Colors.white)

    Scene.group
        [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 120.0; Height = 90.0 } (Colors.rgb 24uy 28uy 36uy)
          Scene.circle { X = 40.0; Y = 32.0 } 14.0 (Colors.rgb 240uy 120uy 64uy)
          Scene.path path samplePaint
          glyph ]

[<Tests>]
let feature146PortableSceneRoundTripTests =
    testList "Feature146 portable scene round trip" [
        test "exports imports and compares representative scene" {
            let package = SceneCodec.export representativeScene
            let imported = SceneCodec.importPackage package.CanonicalBytes

            match imported with
            | Result.Error diagnostics -> failtestf "package import failed: %A" (SceneCodec.formatDiagnostics diagnostics)
            | Result.Ok restored ->
                let comparison = SceneCodec.compareScenes package.Scene restored.Scene
                Expect.isTrue comparison.Equivalent (String.concat "; " (SceneCodec.formatDiagnostics comparison.Diagnostics))
                Expect.equal restored.PackageIdentity package.PackageIdentity "package identity is computed from canonical bytes"
                Expect.equal restored.Version SceneCodec.supportedVersion "protocol version round-trips"
        }

        test "fifty exports of the same scene are byte identical" {
            let packages =
                [ for _ in 1 .. 50 -> SceneCodec.export representativeScene ]

            let first = packages.Head.CanonicalBytes

            packages
            |> List.iter (fun package ->
                Expect.sequenceEqual package.CanonicalBytes first "canonical bytes are deterministic"
                Expect.equal package.PackageIdentity packages.Head.PackageIdentity "identity is deterministic")
        }

        test "glyph-run proof data is preserved" {
            let package = SceneCodec.export representativeScene

            match SceneCodec.importPackage package.CanonicalBytes with
            | Result.Error diagnostics -> failtestf "package import failed: %A" (SceneCodec.formatDiagnostics diagnostics)
            | Result.Ok restored ->
                let glyphRuns =
                    restored.Scene.Nodes
                    |> List.collect (function
                        | Group scenes -> scenes |> List.collect (fun s -> s.Nodes)
                        | node -> [ node ])
                    |> List.choose (function
                        | GlyphRun run -> Some run.Data
                        | _ -> None)

                Expect.hasLength glyphRuns 1 "representative scene includes glyph-run payload"
                Expect.equal glyphRuns.Head.Text "Render anywhere" "glyph-run text is preserved"
                Expect.equal glyphRuns.Head.Provider.ProviderId "scene-pure-fallback" "provider evidence is preserved"
        }

        test "image sources become package-local resource ids" {
            let scene = Scene.image (0.0, 0.0, 64.0, 64.0) "/producer/local/logo.png"
            let package = SceneCodec.export scene

            Expect.hasLength package.Resources 1 "image manifest has one resource"
            Expect.equal package.Resources.Head.ResourceId "image-0001" "resource id is package local"
            Expect.equal package.Resources.Head.SourceLabel (Some "/producer/local/logo.png") "local source is diagnostic-only"

            match package.Scene.Nodes with
            | [ Image(_, source) ] -> Expect.equal source "image-0001" "scene payload references resource id"
            | other -> failtestf "unexpected scene nodes: %A" other
        }
    ]
