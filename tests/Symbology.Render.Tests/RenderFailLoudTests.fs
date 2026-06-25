module Symbology.Render.Tests.RenderFailLoudTests

// T025 [US2] Render fail-loud (FR-012): a scene/verdict that does not pass (or ImagePath = None) =>
// `Render.toPng` raises carrying the joined Diagnostics, never returns a blank image as success.

open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

let private dir =
    let d = Path.Combine(Path.GetTempPath(), "fs-gg-symbology-render-tests", "fail")
    Directory.CreateDirectory d |> ignore
    d

[<Tests>]
let tests =
    testList
        "US2 render fail-loud"
        [ test "a non-passing render raises instead of returning a blank image" {
              let scene = Symbology.gallery 2 60.0 [ Symbology.defaultToken ]
              // A zero-area output cannot produce a passing reference image: the helper must raise
              // (covering ReferenceFailed / ReferenceEnvironmentLimited / ImagePath = None), never
              // return a blank PNG path as success.
              Expect.throws
                  (fun () -> Render.toPng { Width = 0; Height = 0 } scene dir |> ignore)
                  "fail-loud on any non-ReferencePassed verdict"
          } ]
