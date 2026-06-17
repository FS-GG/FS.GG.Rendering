module Feature146RenderAnywhereEvidenceTests

open Expecto
open Rendering.Harness
open FS.GG.UI.Scene

[<Tests>]
let feature146RenderAnywhereEvidenceTests =
    testList "Feature146 render-anywhere evidence" [
        test "corpus exposes three representative packages" {
            let corpus = RenderAnywhere.corpus ()
            Expect.hasLength corpus 3 "browser feasibility has at least three scenes"
            Expect.equal (corpus |> List.map _.ScenarioId) [ "basic-primitives"; "layered-portal"; "shaped-text" ] "scenario ids are stable"
            corpus |> List.iter (fun item -> Expect.stringStarts item.Package.PackageIdentity "sha256:" "package identity is sha256")
        }

        test "reference command writes summary evidence" {
            let out = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fs-gg-feature146-reference-test")
            if System.IO.Directory.Exists out then System.IO.Directory.Delete(out, true)

            let evidence = RenderAnywhere.runReferenceCommand out

            Expect.hasLength evidence 3 "one reference evidence record per corpus item"
            Expect.isTrue (System.IO.File.Exists(System.IO.Path.Combine(out, "summary.md"))) "summary is written"
        }
    ]
