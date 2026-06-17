module Feature146BrowserEvidenceFormatterTests

open Expecto
open Rendering.Harness

[<Tests>]
let feature146BrowserEvidenceFormatterTests =
    testList "Feature146 browser evidence formatters" [
        test "browser report formatter includes candidate verdicts and fallback" {
            let report = RenderAnywhere.buildBrowserFeasibilityReport (RenderAnywhere.corpus ()) [] "canvaskit-command-stream/proof"
            let text = RenderAnywhere.formatBrowserReport report |> String.concat "\n"

            Expect.stringContains text "candidate-backend: canvaskit-command-stream/proof" "candidate backend is formatted"
            Expect.stringContains text "decision: fallback:" "fallback decision is formatted"
            Expect.stringContains text "basic-primitives: environment-limited" "per-scene verdict is formatted"
            Expect.stringContains text "Environment-limited browser results cannot count as accepted candidate evidence." "diagnostics prevent overclaiming"
        }

        test "browser report writer persists markdown" {
            let out = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fs-gg-feature146-browser-test")
            if System.IO.Directory.Exists out then System.IO.Directory.Delete(out, true)

            let report = RenderAnywhere.runBrowserFeasibilityCommand out
            let path = System.IO.Path.Combine(out, "browser-feasibility.md")

            Expect.isTrue (System.IO.File.Exists path) "browser feasibility markdown is written"
            Expect.hasLength report.Comparisons 3 "report covers corpus"
        }
    ]
