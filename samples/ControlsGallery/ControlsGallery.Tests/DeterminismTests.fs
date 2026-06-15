module ControlsGallery.Tests.DeterminismTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls.Elmish
open ControlsGallery.Core

let private size: Size = { Width = 1024; Height = 768 }

/// FR-009 / SC-002: same seed ⇒ byte-identical state outcome + run.json across runs.
[<Tests>]
let determinismTests =
    testList "Determinism" [
        test "golden state outcome is byte-identical across two same-seed runs over all pages" {
            for page in Pages.all do
                let script = Scripts.forPage page.Id
                let m1 = ControlsElmish.Perf.runScript Host.defaultHost size script
                let m2 = ControlsElmish.Perf.runScript Host.defaultHost size script
                Expect.equal (Evidence.goldenState m1) (Evidence.goldenState m2) (sprintf "page %s golden state identical" page.Id)
        }

        test "run.json text is byte-identical for identical record inputs" {
            let seed = 4242
            for page in Pages.all do
                let metrics = ControlsElmish.Perf.runScript Host.defaultHost size (Scripts.forPage page.Id)
                let shot = Evidence.degraded "pure suite: no GL capture"
                let j1 = Evidence.toRunJson (Evidence.build page.Id seed metrics shot)
                let j2 = Evidence.toRunJson (Evidence.build page.Id seed metrics shot)
                Expect.equal j1 j2 (sprintf "page %s run.json deterministic" page.Id)
        }
    ]
