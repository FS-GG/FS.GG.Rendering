module SecondAntShowcase.Tests.Feature172ResponsivenessCliContractTests

open Expecto
open SecondAntShowcase.App

[<Tests>]
let tests =
    testList "Feature172 responsiveness CLI contract" [
        test "parser accepts all-interactive scope" {
            match Responsiveness.parse [ "--script"; "representative"; "--theme"; "dark"; "--all-interactive" ] with
            | Ok request ->
                match request.Scope with
                | Responsiveness.AllInteractive -> ()
                | _ -> failtest "expected all-interactive scope"
            | Error error -> failtest error
        }

        test "parser rejects page and all-interactive together" {
            match Responsiveness.parse [ "--page"; "buttons"; "--all-interactive" ] with
            | Error error -> Expect.stringContains error "mutually exclusive" "mutual exclusion is explicit"
            | Ok _ -> failtest "invalid combined scope parsed"
        }

        test "parser rejects unknown page and script with invalid request status" {
            match Responsiveness.parse [ "--page"; "missing-page" ] with
            | Error error -> Expect.stringContains error "unknown page" "unknown page rejected"
            | Ok _ -> failtest "unknown page parsed"

            match Responsiveness.parse [ "--script"; "unknown" ] with
            | Error error -> Expect.stringContains error "unknown responsiveness script" "unknown script rejected"
            | Ok _ -> failtest "unknown script parsed"
        }

        test "all-interactive require-live run fails closed with exit code 4 in headless substitute mode" {
            let outDir = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.tempDir ()
            let code =
                SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.withForcedSubstitute
                    (fun () ->
                        Responsiveness.run
                            [ "--script"; "representative"
                              "--theme"; "light"
                              "--all-interactive"
                              "--require-live"
                              "--out"; outDir
                              "--json" ])

            use doc = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement

            Expect.equal code 4 "live evidence unavailable is environment-limited"
            Expect.equal (root.GetProperty("overallReadiness").GetString()) "environment-limited" "summary does not claim accepted readiness"
            Expect.stringContains (root.GetProperty("scope").GetString()) "all-interactive" "scope names all-interactive run"
        }
    ]
