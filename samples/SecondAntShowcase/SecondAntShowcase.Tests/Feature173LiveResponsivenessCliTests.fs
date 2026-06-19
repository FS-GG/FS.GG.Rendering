module SecondAntShowcase.Tests.Feature173LiveResponsivenessCliTests

open Expecto
open SecondAntShowcase.App

[<Tests>]
let tests =
    testList "Feature173 live responsiveness CLI" [
        test "parser accepts live all-interactive request" {
            match Responsiveness.parse [ "--script"; "representative"; "--theme"; "dark"; "--all-interactive"; "--require-live"; "--json" ] with
            | Ok request ->
                Expect.equal request.Theme SecondAntShowcase.Core.Model.Dark "dark theme parsed"
                Expect.isTrue request.RequireLive "live requirement parsed"
                Expect.isTrue request.PrintJson "json flag parsed"
                match request.Scope with
                | Responsiveness.AllInteractive -> ()
                | _ -> failtest "expected all-interactive scope"
            | Error error -> failtest error
        }

        test "invalid request returns exit code 2" {
            let code = Responsiveness.run [ "--script"; "missing"; "--all-interactive" ]

            Expect.equal code 2 "unknown script is invalid request"
        }

        test "headless require-live returns live-unavailable exit code 4" {
            let code, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir

            Expect.equal code 4 "live unavailable exit code"
            Expect.equal (doc.RootElement.GetProperty("overallReadiness").GetString()) "environment-limited" "non-accepted readiness"
        }
    ]
