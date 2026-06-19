module AntShowcase.Tests.Feature167ResponsivenessCliTests

open System.IO
open System.Text.Json
open Expecto
open AntShowcase.App

[<Tests>]
let tests =
    testList "Feature167 AntShowcase responsiveness CLI" [
        test "responsiveness command writes environment-limited summary output" {
            let outDir = AntShowcase.Tests.Feature167ResponsivenessFixtures.tempDir ()

            let code =
                Responsiveness.run
                    [ "--page"; "buttons"
                      "--theme"; "light"
                      "--script"; "representative"
                      "--out"; outDir
                      "--json" ]

            let summary =
                Directory.GetFiles(outDir, "summary.json", SearchOption.AllDirectories)
                |> Array.exactlyOne

            use doc = JsonDocument.Parse(File.ReadAllText summary)

            Expect.equal code 4 "headless deterministic substitute is environment-limited"
            Expect.equal (doc.RootElement.GetProperty("overallReadiness").GetString()) "environment-limited" "summary does not claim accepted live readiness"
        }

        test "responsiveness parser rejects unknown script" {
            let parsed = Responsiveness.parse [ "--script"; "unknown" ]

            match parsed with
            | Error _ -> ()
            | Ok _ -> failtest "unknown script is invalid request"
        }
    ]
