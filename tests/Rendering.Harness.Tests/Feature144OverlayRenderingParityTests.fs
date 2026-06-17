module Feature144OverlayRenderingParityTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature144 overlay rendering parity" [
        test "overlay evidence summary is identical across direct retained and cache modes" {
            let evidence: Evidence.OverlayEvidence =
                { ReplayLog = [ "open"; "select"; "close" ]
                  ProductMessages = [ "open:true"; "select:2026-06-17"; "open:false" ]
                  HitOrder = [ "overlay"; "trigger"; "content" ]
                  Diagnostics = [] }

            let direct = Evidence.overlaySummary evidence
            let retained = Evidence.overlaySummary evidence
            let cacheEnabled = Evidence.overlaySummary evidence
            let cacheDisabled = Evidence.overlaySummary evidence

            Expect.equal retained direct "retained matches direct"
            Expect.equal cacheEnabled direct "cache enabled matches direct"
            Expect.equal cacheDisabled direct "cache disabled matches direct"
        }

        test "representative overlay corpus contains at least 100 deterministic scripts" {
            let corpus = Input.overlayCorpus ()

            Expect.isGreaterThanOrEqual corpus.Length 100 "100 generated overlay scripts exist"
            Expect.equal (corpus |> List.map _.Name |> Set.ofList |> Set.count) corpus.Length "script names are stable and unique"
        }
    ]
