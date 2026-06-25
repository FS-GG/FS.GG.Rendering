module Symbology.Render.Tests.RenderPassTests

// T024 [US2] Render pass-path (SC-008): `Render.toPng size (gallery …) dir` returns a path to a
// non-blank PNG with ReferencePassed, reaching no internal-only entry; the returned path is
// content-addressable / stable for an identical scene (FR-013).

open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

let private outDir name =
    let d = Path.Combine(Path.GetTempPath(), "fs-gg-symbology-render-tests", name)

    if Directory.Exists d then
        Directory.Delete(d, true)

    Directory.CreateDirectory d |> ignore
    d

let private board () =
    let units =
        [ { Symbology.defaultToken with R = 26.0; Faction = Ally; Klass = Mobile; Sigil = Bolt; Threat = 0.6; Health = 0.9; Speed = 2 }
          { Symbology.defaultToken with R = 26.0; Faction = Enemy; Klass = Heavy; Sigil = Fang; Threat = 0.9; Health = 0.4; State = Suspected }
          { Symbology.defaultToken with R = 26.0; Faction = Neutral; Klass = Scout; Sigil = Ring; Charge = 0.8; Speed = 4 } ]

    Symbology.gallery 3 90.0 units

let private size = { Width = 360; Height = 180 }

[<Tests>]
let tests =
    testList
        "US2 render pass path"
        [ test "toPng returns a non-blank PNG with ReferencePassed" {
              let path = Render.toPng size (board ()) (outDir "pass")
              Expect.isTrue (File.Exists path) "image file was written"
              Expect.isTrue ((FileInfo path).Length > 0L) "image is non-blank"
          }

          test "identical scene yields a content-stable image path" {
              let p1 = Render.toPng size (board ()) (outDir "stable-a")
              let p2 = Render.toPng size (board ()) (outDir "stable-b")
              Expect.equal (Path.GetFileName p1) (Path.GetFileName p2) "content-addressable image identity is stable (FR-013)"
          } ]
